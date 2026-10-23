using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class ForestProgressionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private bool hadBest;
        private int previousBest;
        private MethodInfo step;

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            hadBest = PlayerPrefs.HasKey("DinoRush.Forest.HighScore");
            previousBest = PlayerPrefs.GetInt("DinoRush.Forest.HighScore");
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.StartRun();
            game.enabled = false;
            step = typeof(ForestRunnerGame).GetMethod("Step", Private);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestorePrefs()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (hadBest) PlayerPrefs.SetInt("DinoRush.Forest.HighScore", previousBest);
            else PlayerPrefs.DeleteKey("DinoRush.Forest.HighScore");
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator ActualPooledCourseRemainsPlayableThroughAllSixLevels()
        {
            var sprites = (List<Sprite>)Get("obstacleSprites");
            var aspects = new float[sprites.Count];
            for (int i = 0; i < aspects.Length; i++) aspects[i] = sprites[i].bounds.size.x / sprites[i].bounds.size.y;
            Set("encounterDirector", new RunnerEncounterDirector(731, aspects));
            var oracle = new RunnerEncounterDirector(731, aspects);
            var targets = new List<float>();
            var patterns = new HashSet<RunnerPattern>();
            var player = (RunnerPlayer)Get("player");
            var hud = (RunnerHud)Get("hud");
            Transform pool = game.transform.Find("Obstacle pool");
            SpriteRenderer[] renderers = pool.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(renderers.Length, Is.EqualTo(10));
            int target = 0, peak = 0, encounters = 0;
            bool captured = false;
            var parameters = new object[] { RunnerRules.SimulationStep };
            for (int i = 0; i < 180 * 120; i++)
            {
                float distance = (float)Get("distance");
                float elapsed = (float)Get("elapsed");
                float apex = RunnerRules.JumpSpeed / RunnerRules.Gravity;
                float lead = (RunnerRules.Speed(elapsed) + RunnerRules.Speed(elapsed + apex)) * 0.5f * apex;
                if (target < targets.Count && player.Grounded && targets[target] - distance <= lead)
                {
                    game.Jump();
                    target++;
                }
                float previousNext = (float)Get("nextEncounterDistance");
                step.Invoke(game, parameters);
                if (game.State != ForestRunnerGame.RunState.Running)
                    Assert.Fail($"Actual course collision at t={Get("elapsed")}, target={target}, cleared={game.ObstaclesCleared}.");
                if ((float)Get("nextEncounterDistance") != previousNext)
                {
                    elapsed = (float)Get("elapsed");
                    RunnerEncounter encounter = oracle.Next(elapsed);
                    encounters++;
                    patterns.Add(encounter.Pattern);
                    float origin = (float)Get("distance") + 11.7f - RunnerRules.PlayerX - RunnerRules.Speed(elapsed) * RunnerRules.SimulationStep;
                    if (encounter.Pattern == RunnerPattern.QuickSteps || encounter.Pattern == RunnerPattern.Burst)
                        foreach (RunnerObstacleSpawn item in encounter.Obstacles) targets.Add(origin + item.Offset);
                    else
                    {
                        RunnerObstacleSpawn first = encounter.Obstacles[0], last = encounter.Obstacles[encounter.Obstacles.Length - 1];
                        targets.Add(origin + (first.Offset - first.Width * 0.5f + last.Offset + last.Width * 0.5f) * 0.5f);
                    }
                }
                int live = 0;
                foreach (SpriteRenderer renderer in renderers) if (renderer.gameObject.activeSelf) live++;
                peak = Mathf.Max(peak, live);
                if (!captured && game.Level == 4 && player.Grounded && live >= 3)
                {
                    player.Render(0f, true, false, 1f);
                    hud.UpdateScore(game.Score, game.HighScore, game.ObstaclesCleared, RunnerRules.Speed((float)Get("elapsed")));
                    yield return ForestRunTests.Capture("forest-progression-level-4");
                    captured = true;
                }
            }
            Assert.That(game.Level, Is.EqualTo(6));
            Assert.That(game.Lives, Is.EqualTo(5), "A clean course must not consume any hearts.");
            Assert.That(game.transform.Find("Forest runner UI/16 by 9 safe content/Top left badge/Map").GetComponent<Text>().text,
                Is.EqualTo("LEVEL 06"));
            Assert.That(patterns.Count, Is.EqualTo(6));
            Assert.That(game.ObstaclesCleared, Is.GreaterThan(150));
            Assert.That(encounters, Is.GreaterThan(80));
            Assert.That(peak, Is.InRange(3, 9), "Patterns must not exhaust the existing obstacle pool.");
            Assert.That(pool.childCount, Is.EqualTo(10), "The level director must reuse saved objects.");
        }

        [UnityTest]
        public IEnumerator LevelBadgeTransitionsPauseAndRestartStayInSync()
        {
            Set("spawnTimer", float.MaxValue);
            Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            Text badge = root.Find("Top left badge/Map").GetComponent<Text>();
            Text toast = root.Find("Milestone toast").GetComponent<Text>();
            foreach (float boundary in new[] { 12f, 28f, 48f, 75f, 105f })
            {
                Set("elapsed", boundary);
                step.Invoke(game, new object[] { RunnerRules.SimulationStep });
                Assert.That(badge.text, Is.EqualTo($"LEVEL {game.Level:00}"));
                Assert.That(toast.text, Does.StartWith($"LEVEL {game.Level:00}"));
            }
            object director = Get("encounterDirector");
            float elapsed = (float)Get("elapsed"), next = (float)Get("nextEncounterDistance");
            game.TogglePause();
            game.enabled = true;
            yield return new WaitForSeconds(0.15f);
            game.enabled = false;
            Assert.That(Get("elapsed"), Is.EqualTo(elapsed));
            Assert.That(Get("nextEncounterDistance"), Is.EqualTo(next));
            game.TogglePause();
            Assert.That(badge.text, Is.EqualTo("LEVEL 06"));
            game.RestartRun();
            Assert.That(game.Level, Is.EqualTo(1));
            Assert.That(badge.text, Is.EqualTo("LEVEL 01"));
            Assert.That(Get("nextEncounterDistance"), Is.EqualTo(0f));
            Assert.That(Get("encounterDirector"), Is.Not.SameAs(director));
            game.ReturnToMenu();
            Assert.That(badge.text, Is.EqualTo("ENDLESS"));
        }

        [UnityTest]
        public IEnumerator ProgressMeterTracksStagesLapsPauseRestartAndResponsiveResizing()
        {
            Set("spawnTimer", float.MaxValue);
            Transform root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            Transform progress = root.Find("Stage progress");
            Text title = progress.Find("Stage name").GetComponent<Text>();
            Text countdown = progress.Find("Next stage").GetComponent<Text>();
            var fill = (RectTransform)progress.Find("Progress fill");
            Assert.That(progress.gameObject.activeInHierarchy, Is.True);
            Assert.That(title.text, Is.EqualTo("01 / FIRST STEPS"));
            Set("elapsed", 6f);
            step.Invoke(game, new object[] { RunnerRules.SimulationStep });
            Assert.That(fill.sizeDelta.x, Is.EqualTo(208f).Within(1f));
            Assert.That(countdown.text, Is.EqualTo("06s"));
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(360, 800, new Rect(0, 24, 360, 752)));
            Assert.That(fill.sizeDelta.x, Is.EqualTo(208f).Within(1f), "Resizing cannot reset the progress geometry.");
            game.TogglePause();
            Assert.That(progress.gameObject.activeInHierarchy, Is.False);
            float width = fill.sizeDelta.x;
            game.enabled = true;
            yield return new WaitForSecondsRealtime(0.15f);
            game.enabled = false;
            Assert.That(fill.sizeDelta.x, Is.EqualTo(width));
            game.TogglePause();
            Assert.That(progress.gameObject.activeInHierarchy, Is.True);
            Set("elapsed", 135f);
            Set("distance", 99.99f); // A distance milestone must not overwrite a stage/lap celebration.
            step.Invoke(game, new object[] { RunnerRules.SimulationStep });
            Assert.That(title.text, Is.EqualTo("WILD RUN / LAP 02"));
            Assert.That(countdown.text, Is.EqualTo("30s"));
            Assert.That(root.Find("Milestone toast").GetComponent<Text>().text, Does.StartWith("LEVEL 06"));
            Set("elapsed", 165f);
            step.Invoke(game, new object[] { RunnerRules.SimulationStep });
            Assert.That(root.Find("Milestone toast").GetComponent<Text>().text, Is.EqualTo("LAP 02 COMPLETE / WILD RUN"));
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            yield return ForestRunTests.Capture("progress-wild-run");
            game.RestartRun();
            Assert.That(title.text, Is.EqualTo("01 / FIRST STEPS"));
            Assert.That(fill.sizeDelta.x, Is.Zero);
            game.ReturnToMenu();
            Assert.That(progress.gameObject.activeInHierarchy, Is.False);
        }

        private object Get(string field) => typeof(ForestRunnerGame).GetField(field, Private).GetValue(game);
        private void Set(string field, object value) => typeof(ForestRunnerGame).GetField(field, Private).SetValue(game, value);
    }
}
