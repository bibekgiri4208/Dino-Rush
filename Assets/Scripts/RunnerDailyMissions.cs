using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Three local-calendar missions. Rewards are manual, saved once, and never count as pickups.</summary>
    public sealed class RunnerDailyMissions
    {
        public const string SaveKey = "DinoRush.DailyMissions.v1";
        public const int Count = 3;
        public enum ClaimResult { Success, Incomplete, AlreadyClaimed, WalletFull, InvalidMission }
        [Serializable]
        private sealed class Snapshot
        {
            public long day;
            public float metres;
            public int eggs, cleared, claimed;
        }
        private readonly Func<DateTime> clock;
        private Snapshot state;
        private bool dirty;

        public RunnerDailyMissions(Func<DateTime> localClock = null)
        {
            clock = localClock ?? (() => DateTime.Now);
            state = Read();
            EnsureDay();
        }
        public static int Goal(int index) => index == 0 ? 500 : index == 1 ? 30 : index == 2 ? 25 : 0;
        public static int Reward(int index) => Valid(index) ? (index + 1) * 100 : 0;
        public static string Title(int index) => index == 0 ? "RUN 500 METRES" : index == 1 ? "COLLECT 30 EGGS" : "CLEAR 25 OBSTACLES";
        public long Day { get { EnsureDay(); return state.day; } }
        private static bool Valid(int index) => index >= 0 && index < Count;
        public int Progress(int index)
        {
            EnsureDay();
            return index == 0 ? Mathf.FloorToInt(state.metres) : index == 1 ? state.eggs : index == 2 ? state.cleared : 0;
        }
        public bool Claimed(int index) { EnsureDay(); return Valid(index) && (state.claimed & (1 << index)) != 0; }
        public bool Complete(int index) => Valid(index) && Progress(index) >= Goal(index);
        public TimeSpan UntilReset
        {
            get
            {
                EnsureDay();
                DateTime day = new DateTime(state.day);
                DateTime next = day < DateTime.MaxValue.Date ? day.AddDays(1) : DateTime.MaxValue;
                return TimeSpan.FromTicks(Math.Max(0, (next - clock()).Ticks));
            }
        }
        public void RecordDistance(float metres)
        {
            EnsureDay();
            if (float.IsNaN(metres) || float.IsInfinity(metres) || metres <= 0 || state.metres >= Goal(0)) return;
            state.metres = Mathf.Min(Goal(0), state.metres + metres); dirty = true;
        }
        public void RecordEgg()
        {
            EnsureDay(); if (state.eggs >= Goal(1)) return;
            state.eggs++; dirty = true;
        }
        public void RecordClear()
        {
            EnsureDay(); if (state.cleared >= Goal(2)) return;
            state.cleared++; dirty = true;
        }
        private void EnsureDay()
        {
            long today = clock().Date.Ticks;
            if (today <= state.day) return; // Turning the clock backwards cannot recreate a claimed day.
            state = new Snapshot { day = today }; dirty = true;
        }
        private Snapshot Read()
        {
            Snapshot saved = null;
            try { saved = JsonUtility.FromJson<Snapshot>(PlayerPrefs.GetString(SaveKey, "")); }
            catch (ArgumentException) { }
            if (saved == null || saved.day <= 0 || saved.day > DateTime.MaxValue.Date.Ticks || saved.day % TimeSpan.TicksPerDay != 0)
                return new Snapshot { day = clock().Date.Ticks };
            saved.metres = float.IsNaN(saved.metres) || float.IsInfinity(saved.metres) ? 0 : Mathf.Clamp(saved.metres, 0, Goal(0));
            saved.eggs = Mathf.Clamp(saved.eggs, 0, Goal(1));
            saved.cleared = Mathf.Clamp(saved.cleared, 0, Goal(2));
            saved.claimed &= (1 << Count) - 1;
            return saved;
        }
        public void Refresh()
        {
            Flush();
            Snapshot latest = Read();
            if (latest.day >= state.day) state = latest;
            EnsureDay();
            if (dirty) Flush();
        }
        public void ResetProgress()
        {
            // Replace the live snapshot too, so later Flush/focus/quit cannot restore old claims.
            state = new Snapshot { day = clock().Date.Ticks };
            Write();
        }
        public void Flush()
        {
            EnsureDay();
            if (!dirty) return;
            Snapshot saved = Read();
            if (saved.day > state.day) state = saved;
            else if (saved.day == state.day)
            {
                // A stale menu/session must never unclaim rewards or roll saved progress backwards.
                state.metres = Mathf.Max(state.metres, saved.metres);
                state.eggs = Mathf.Max(state.eggs, saved.eggs);
                state.cleared = Mathf.Max(state.cleared, saved.cleared);
                state.claimed |= saved.claimed;
            }
            Write();
        }
        private void Write()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save(); dirty = false;
        }
        public ClaimResult Claim(int index)
        {
            Refresh();
            if (!Valid(index)) return ClaimResult.InvalidMission;
            if (Claimed(index)) return ClaimResult.AlreadyClaimed;
            if (!Complete(index)) return ClaimResult.Incomplete;
            int balance = Mathf.Max(0, PlayerPrefs.GetInt(RunnerShopProgress.WalletKey));
            if (balance > int.MaxValue - Reward(index)) return ClaimResult.WalletFull;
            state.claimed |= 1 << index;
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, balance + Reward(index));
            Write(); // Save reward and claim together, before any feedback or duplicate button events.
            return ClaimResult.Success;
        }
    }
}
