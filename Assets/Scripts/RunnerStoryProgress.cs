using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Versioned campaign save. Rewards are journaled before wallet/progress are changed.</summary>
    public sealed class RunnerStoryProgress
    {
        public const string SaveKey = "DinoRush.Story.v1";
        public const string TransactionKey = "DinoRush.Story.RewardTransaction.v1";
        public const string CompletionTitle = "HERD GUARDIAN";
        [Serializable] private sealed class SaveData
        {
            public int version = 1;
            public int[] stars = new int[RunnerStoryCampaign.LevelCount];
            public int rewarded, openingsSeen, endingsSeen;
        }
        [Serializable] private sealed class Transaction
        {
            public int version = 1, walletAfter;
            public string saveAfter;
        }
        private SaveData data;
        public RunnerStoryProgress() { Reload(); }
        private static bool Valid(int index) => index >= 0 && index < RunnerStoryCampaign.LevelCount;
        public bool Unlocked(int index) => Valid(index) && (index == 0 || Completed(index - 1));
        public bool Completed(int index) => Valid(index) && (data.stars[index] & 1) != 0;
        public int StarMask(int index) => Valid(index) ? data.stars[index] & 7 : 0;
        public int Stars(int index)
        {
            int mask = StarMask(index);
            return (mask & 1) + ((mask >> 1) & 1) + ((mask >> 2) & 1);
        }
        public bool RewardClaimed(int index) => Valid(index) && (data.rewarded & (1 << index)) != 0;
        public bool SceneSeen(int index, bool ending) => Valid(index) &&
            ((ending ? data.endingsSeen : data.openingsSeen) & (1 << index)) != 0;
        public int CompletedCount
        {
            get { int count = 0; for (int i = 0; i < RunnerStoryCampaign.LevelCount; i++) if (Completed(i)) count++; return count; }
        }
        public bool CampaignComplete => CompletedCount == RunnerStoryCampaign.LevelCount;
        public int ContinueIndex
        {
            get { for (int i = 0; i < RunnerStoryCampaign.LevelCount; i++) if (!Completed(i)) return i; return RunnerStoryCampaign.LevelCount - 1; }
        }
        public void Reload()
        {
            RecoverReward();
            try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, "")); }
            catch (ArgumentException) { data = null; }
            if (data == null || data.version != 1) data = new SaveData();
            if (data.stars == null) data.stars = new int[RunnerStoryCampaign.LevelCount];
            if (data.stars.Length != RunnerStoryCampaign.LevelCount) Array.Resize(ref data.stars, RunnerStoryCampaign.LevelCount);
            for (int i = 0; i < data.stars.Length; i++) data.stars[i] = (data.stars[i] & 1) == 0 ? 0 : data.stars[i] & 7;
        }
        public static int EarnedMask(int hearts, int tokens) => 1 | (hearts >= 2 ? 2 : 0) | (tokens >= 3 ? 4 : 0);
        public int Complete(int index, int hearts, int tokens)
        {
            if (!Unlocked(index) || hearts <= 0) return 0;
            data.stars[index] |= EarnedMask(hearts, tokens);
            Persist();
            return ClaimReward(index);
        }
        public int ClaimReward(int index)
        {
            if (!Completed(index) || RewardClaimed(index)) return 0;
            int reward = RunnerStoryCampaign.Levels[index].Reward;
            int balance = Mathf.Max(0, PlayerPrefs.GetInt(RunnerShopProgress.WalletKey));
            if (balance > int.MaxValue - reward) return 0; // Keep the claim available; never lose or truncate a reward.
            data.rewarded |= 1 << index;
            var transaction = new Transaction { walletAfter = balance + reward, saveAfter = JsonUtility.ToJson(data) };
            PlayerPrefs.SetString(TransactionKey, JsonUtility.ToJson(transaction));
            PlayerPrefs.Save(); // Durable intent. Recovery finishes the transaction before any new story operation.
            Apply(transaction);
            return reward;
        }
        private static void Apply(Transaction transaction)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, transaction.walletAfter);
            PlayerPrefs.SetString(SaveKey, transaction.saveAfter);
            PlayerPrefs.Save();
            PlayerPrefs.DeleteKey(TransactionKey);
            PlayerPrefs.Save();
        }
        private static void RecoverReward()
        {
            if (!PlayerPrefs.HasKey(TransactionKey)) return;
            try
            {
                var transaction = JsonUtility.FromJson<Transaction>(PlayerPrefs.GetString(TransactionKey));
                if (transaction != null && transaction.version == 1 && transaction.walletAfter >= 0 &&
                    !string.IsNullOrEmpty(transaction.saveAfter)) { Apply(transaction); return; }
            }
            catch (ArgumentException) { }
            PlayerPrefs.DeleteKey(TransactionKey);
            PlayerPrefs.Save();
        }
        public void MarkSceneSeen(int index, bool ending)
        {
            if (!Unlocked(index)) return;
            if (ending) data.endingsSeen |= 1 << index; else data.openingsSeen |= 1 << index;
            Persist();
        }
        private void Persist() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
    }

    /// <summary>Input-independent conversation cursor, also used by skip and replay.</summary>
    public sealed class RunnerStoryConversation
    {
        public RunnerStoryScene Scene { get; private set; }
        public int Index { get; private set; }
        public float Characters { get; private set; }
        public bool Playing { get; private set; }
        public RunnerStoryLine Line => Playing ? Scene.Lines[Index] : null;
        public bool Revealed => !Playing || Characters >= Line.Text.Length;
        private Action finished;
        public void Play(RunnerStoryScene scene, Action complete)
        {
            Cancel();
            Scene = scene; Index = 0; Characters = 0; finished = complete;
            Playing = scene != null && scene.Lines != null && scene.Lines.Length > 0;
            if (!Playing) Finish();
        }
        public void Tick(float dt) { if (Playing) Characters = Mathf.Min(Line.Text.Length, Characters + Mathf.Max(0, dt) * 42f); }
        public void Advance()
        {
            if (!Playing) return;
            if (!Revealed) { Characters = Line.Text.Length; return; }
            if (++Index >= Scene.Lines.Length) { Finish(); return; }
            Characters = 0;
        }
        public void Skip() { if (Playing) Finish(); }
        private void Finish() { Playing = false; Action callback = finished; finished = null; callback?.Invoke(); }
        public void Cancel() { Playing = false; finished = null; }
    }
}
