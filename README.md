# Dino Rush — Forest

A single-player endless runner using the supplied blue dino, forest parallax layers, and obstacle sprites. Requires Unity **6000.4.1f1** and the packages already listed in this project.

## Play

Open **Assets/Scenes/ForestRun.unity** (or **Dino Rush → Open Forest Run**) and press **Play**. ForestRun is also the first scene in Build Settings. The original SampleScene and unused character/map art are left intact.

| Action | Controls |
| --- | --- |
| Start | Play, Space, or Enter |
| Retry | Space, Enter, click/tap the forest, or Run Again |
| Jump | Space, Up, W, click/tap the forest, or JUMP |
| Pause / resume | Esc, P, or the pause button |
| Restart / leave run | Pause menu: Restart or Quit to Menu |
| Mute sound cues | M or SFX |

Jump over rocks, stumps, and crates. Your distance is your score; speed gradually increases to a cap. There is one jump (no double jump), with a small pre-landing input buffer. Obstacle sizes and spacing allow time to jump and land between obstacles. Switching away from the game automatically pauses it.

Your best forest score and sound preference are saved locally with PlayerPrefs. Restarting a run does not erase your best. The game uses a 16:9 viewport with letterboxing for other window shapes.

## Structure

- `ForestRunnerGame.cs`: scene setup, game states, pooled obstacles/particles, spawning, scoring, input, persistence.
- `RunnerPlayer.cs`: jump integration, input buffer, sprite animation, landing squash.
- `RunnerRules.cs`: movement constants, hitboxes, capped speed and fair spawn intervals.
- `RunnerHud.cs`: start/retry/pause cards, score HUD, touch controls.
- `RunnerAudio.cs`: short generated jump/crash/milestone cues (no external audio required).

The ForestRun scene contains its **saved camera, forest layers, player, obstacle pool, effects, and complete UI**. They are visible and editable in the Hierarchy, Scene view, and Game view without pressing Play. Expand `Dino Rush - Forest` to find the grouped objects. Select the camera to see its frame, or use the Game tab for the start-screen preview. UI layout, colors, and fonts can be edited normally. Menu/score text still updates according to the game state.

The title screen recreates the supplied reference's centered stone/amber logo, moss-covered buttons, pixel lettering, top-right SFX toggle, and upper canopy. The trail uses the original forest artwork without extra UI logs, grass clumps, rocks, or floating leaf fragments. Its objects are under `Forest runner UI/16 by 9 safe content/Forest Main Menu`. **Customize** displays the selected blue dino and forest; **Options** provides the persistent SFX toggle and control help; **Quit** asks for confirmation (and stops Play mode in the Editor). Esc closes these panels. The title-screen camera framing and smaller idle dino switch back to the gameplay framing on Play.

Gameplay obstacles stay hidden in the main-menu preview and activate through the pool when a run starts. The game reuses the saved hierarchy rather than generating another copy when Play starts.

The gameplay HUD and results screen share the title menu's pixel font, cream/sage colors, mossy frames, and icons. Pause freezes the run and opens **Resume**, **Restart**, and **Quit to Menu** controls. Resume preserves the exact run, Restart resets its score and obstacle pool, and Quit returns to the title screen while saving your best. The saved `Forest Pause Menu` object is under the UI safe-content root; enable it for layout editing after hiding `Forest Main Menu`. **Dino Rush → Apply Gameplay UI Theme** authors/refreshes this styling in place, without recreating the forest or restoring the removed foreground decorations.

Running kicks up a short brown pixel-dirt trail behind the dino's feet; landing creates a larger outward burst of dirt clods and dust puffs. Emission stops in midair and on menus, pause freezes the particles, and restarting/returning to the title clears them. The effect reuses the saved 36 sprites under `Forest effects/Trail dust pool` without allocating objects for each burst. Tune frequency/counts under **Pixel dirt effects** on `ForestRunnerGame`. The three point-filtered sprites live in `Assets/Generated/ForestDirtSprites.asset`; **Dino Rush → Apply Dirt Effects** assigns them to an older scene without rebuilding the world or overwriting existing dirt art.

Art stays in its original directories. Uniform dino frames, background sprites, and tiny effect textures are saved as subassets in `Assets/Generated/ForestSceneSprites.asset`, without changing your existing sprite slices. `ForestSprites.mat` keeps the supplied art unlit under URP. The **Dino Rush → Bake Scene Objects (Once)** tool supports converting an older unbaked ForestRun scene; it will not overwrite an already-authored hierarchy.

Original menu sprites are in `Assets/UI/ForestMenu` and can be edited/replaced normally. `PixelMenuArt.cs` provides their deterministic authoring source, and **Dino Rush → Apply Forest Main Menu** upgrades an older scene once without replacing an existing menu. The Press Start 2P font is distributed under the SIL Open Font License; its license and attribution are included in `Assets/UI/Fonts/OFL.txt`.

Menu frames use nine-sliced borders, icons and labels have separate padded areas, and multiline pixel text uses extra line spacing. Only the Options page is enabled inside the inactive dialog for a clean editor preview. **Dino Rush → Repair UI Layout** reapplies these fixes to the existing hierarchy without deleting or regenerating the scene's artwork.

The four main-menu buttons share the same size, symmetric pixel-cut frame, mirrored corner decorations, and equal vertical gaps. Their captions use matching font sizes and two-pixel letter spacing. Each icon and caption is centered together as one group with a 12-pixel visible gap, accounting for transparent icon padding. **Dino Rush → Fix Main Menu Buttons** reapplies this layout and explicitly refreshes only the two button-frame textures; other artwork is preserved.

## Tests

Open **Window → General → Test Runner**. EditMode tests check pace limits, jump feasibility, obstacle spacing, collision bounds, no double jump, saved scene/UI/dirt references, and frame slicing. PlayMode tests load the actual scene and check start/pause/jump, collision, saved best, retry, and clearing obstacles. They also verify the running dirt trail, no midair emission, larger landing bursts, particle pooling/cleanup, that pause freezes the world, Resume preserves progress, Restart clears the previous run, and both pause/results menus return to the title without losing the best score. UI tests raycast buttons (rather than bypassing them), check fonts, label/icon bounds and multiline spacing, and capture menus at normal, tablet, and wide aspect ratios when `DINO_RUSH_CAPTURE_DIR` is set. Tests restore the prior high score and sound preference when finished.
