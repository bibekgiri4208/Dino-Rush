# Dino Rush — Forest

A single-player endless runner using the supplied blue dino, forest parallax layers, and obstacle sprites. Requires Unity **6000.4.1f1** and the packages already listed in this project.

## Play

Open **Assets/Scenes/ForestRun.unity** (or **Dino Rush → Open Forest Run**) and press **Play**. ForestRun is also the first scene in Build Settings. The original SampleScene and unused character/map art are left intact.

| Action | Controls |
| --- | --- |
| Start / retry | Space, Enter, click/tap the forest, or the menu button |
| Jump | Space, Up, W, click/tap the forest, or JUMP |
| Pause / resume | Esc, P, or the pause button |
| Mute sound cues | M or SFX |

Jump over rocks, stumps, and crates. Your distance is your score; speed gradually increases to a cap. There is one jump (no double jump), with a small pre-landing input buffer. Obstacle sizes and spacing allow time to jump and land between obstacles. Switching away from the game automatically pauses it.

Your best forest score and sound preference are saved locally with PlayerPrefs. Restarting a run does not erase your best. The game uses a 16:9 viewport with letterboxing for other window shapes.

## Structure

- `ForestRunnerGame.cs`: scene setup, game states, pooled obstacles/particles, spawning, scoring, input, persistence.
- `RunnerPlayer.cs`: jump integration, input buffer, sprite animation, landing squash.
- `RunnerRules.cs`: movement constants, hitboxes, capped speed and fair spawn intervals.
- `RunnerHud.cs`: start/retry/pause cards, score HUD, touch controls.
- `RunnerAudio.cs`: short generated jump/crash/milestone cues (no external audio required).

The ForestRun scene contains the art references and one controller. Its camera, scrolling layers, player, and UI are created at runtime, so the scene hierarchy fills in when you press Play. Art stays in its original directories; the dino sheet is sliced into uniform animation frames at runtime without changing your existing sprite slices. `ForestSprites.mat` keeps the supplied art unlit under URP.

## Tests

Open **Window → General → Test Runner**. EditMode tests check pace limits, jump feasibility, obstacle spacing, collision bounds, and no double jump. PlayMode tests load the actual forest scene and check start/pause/jump, collision, saved best, retry, and clearing multiple obstacles. Tests restore the prior high score when finished.
