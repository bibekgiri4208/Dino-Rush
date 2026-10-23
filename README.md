# Dino Rush

A single-player endless runner with four selectable dinos and Forest, Desert, Jungle, and Rural maps using the supplied sprite sets. Requires Unity **6000.4.1f1** and the packages already listed in this project.

## Play

Open **Assets/Scenes/ForestRun.unity** (or **Dino Rush → Open Forest Run**) and enter Play mode. In the main menu, click **Play**, choose a map preview, then click **Start Run**. ForestRun is also the first scene in Build Settings; all four maps use this shared scene. The original SampleScene and supplied artwork are left intact.

| Action | Controls |
| --- | --- |
| Choose map / start | Play, Space, or Enter opens the map selector; choose a card, then Start Run |
| Retry | Space, Enter, click/tap the playfield, or Run Again |
| Jump | Space, Up, W, click/tap the playfield, or JUMP |
| Duck / fast drop | Hold Down, S, or DUCK; tap in midair to drop quickly |
| Pause / resume | Esc, P, or the pause button |
| Restart / leave run | Pause menu: Restart or Quit to Menu |
| Mute sound cues | M or SFX |

Jump over rocks, stumps, and crates. Each run starts with **five hearts**, shown in the top-left corner instead of the former map/level panel. An obstacle hit removes one heart; the run ends only when all five are gone. A short grace period protects against overlapping obstacles, and the same obstacle cannot damage you twice. Hits flash the selected dino's supplied white silhouette and crying pose, with a floating **−1 + heart** above it. Hit obstacles do not count as cleared. Pause freezes damage feedback and grace time; Retry/Restart restores all five hearts and clears the effects. Your distance is your score; speed gradually increases to a cap. There is one jump (no double jump), with a small pre-landing input buffer. Obstacle sizes and spacing allow time to jump and land between obstacles. Switching away from the game automatically pauses it.

The trail advances through **six difficulty levels** at 0, 12, 28, 48, 75, and 105 seconds of active running. A short banner announces each increase; the permanent level indicator stays hidden. After three introductory hurdles, a per-run encounter director mixes singles, double/triple clusters, longer leaps, and rapid two/three-jump follow-ups as they unlock. Obstacle artwork, size, group gaps, and recovery gaps vary; occasional longer breaks prevent nonstop pressure. Later levels favor combinations and tighter recovery windows rather than only speeding up the same pattern. Every retry uses a fresh course.

`RunnerEncounterDirector.cs` uses its own seeded random stream, so dust effects cannot change the course. Encounters are spaced by distance rather than a fixed repeating timer. Cluster spans are limited by jump clearance, separate jumps reserve landing/recovery time at the predicted arrival pace, and combinations spawn atomically from the existing pool. Difficulty, upcoming spacing, and the course freeze while paused and reset on Restart. Tests simulate thousands of encounters and complete accelerating three-minute courses using the real jump/collision rules.

Duck uses the dino's crouched running frames and a shorter collision box. In midair it immediately cuts upward momentum and accelerates the descent until landing, even if you release early. Hold to remain crouched on landing; release to stand and jump again. Ducking cancels buffered jumps, so a fast drop never causes an accidental bounce. It does not let you pass through ground obstacles. The on-screen Duck control supports mouse/touch holds and hides with Jump on menus, pause, and results. **Dino Rush → Add Duck Controls** upgrades an older HUD in place.

Your best score, sound preference, chosen dino, and last map are saved locally with PlayerPrefs. Restarting a run does not erase your best or change your dino/map. The game uses a 16:9 viewport with letterboxing for other window shapes.

## Maps

**Play → Choose Your Map → Start Run** offers four preview cards: **Forest**, **Desert**, **Jungle**, and **Rural** (the riverside village). Selecting a card highlights and remembers it but does not start gameplay. Back or Esc/P returns to the title without starting a run. Customize also shows the selected map, and the pause heading uses its name. Retry/Restart retains the same map and chosen dino. Returning to the title restores its original forest backdrop while remembering your choice. Invalid saved map IDs fall back to Forest.

Desert uses `Assets/2D Sprites/desert bg`, Jungle uses `Assets/2D Sprites/jungle bg`, and Rural uses `Assets/2D Sprites/rural area bg`. Each has layered parallax, a matching running surface/soil, and themed dust/ambient colors. They share the same movement, collision bounds, obstacle pool, difficulty progression, and personal best; the existing forest high-score preference is retained.

`Assets/Generated/RunnerMaps/MapCatalog.asset` holds editable source references, sprites, layer heights/bottoms/speeds, and colors. New map layers are created lazily on first use and reused thereafter, rather than duplicated on retry. The original saved forest objects remain intact. Loop variants preserve native source pixels and use reflected extensions with matching edge pixels, no compression/mipmaps, and one-texel tile overlap. Desert/Rural scenery uses bilinear filtering; Jungle and the running surfaces retain crisp point-filtered pixel art. Import size limits accommodate the full reflected width (up to 7600 pixels), avoiding hidden downscaling. Skies extend below their supplied transparent horizons; moons use native-resolution cropped, stationary sprites. Running surfaces recolor the existing ground texture without changing its size or collision height. All source PNGs and import settings are preserved.

**Dino Rush → Add Map Selector and Maps** authors the catalog and editable submenu without rebuilding the world; reapplication preserves existing generated art/catalog edits. **Dino Rush → Refresh Map Previews** explicitly redraws only the four previews from catalog framing. The selector inherits the shared hover, click, and entrance effects.

**Dino Rush → Restore Map Background Source Resolution** explicitly replaces the generated Desert/Jungle/Rural scenery and refreshes their previews from the original sources, keeping their asset GUIDs and catalog framing. Use this to upgrade older 256-pixel-high generated backgrounds. It leaves the forest, shared running surfaces, custom artwork references, and original source files unchanged. Full-resolution backgrounds use more texture memory than the old reduced-resolution versions.

New maps use individual scenic framing rather than one full-screen frame for every layer. Desert dunes sit just behind the trail with the mesas farther back; Jungle's five registered layers place the roots behind the running surface and keep the canopy overhead. Rural's smaller distant mountain leaves open sky, and the village/river/reflection/near bank share a canvas so the bank meets the trail instead of hiding below it. Moons sit in the open area between the HUD cards. **Dino Rush → Refine New Map Layer Placement** explicitly reapplies these defaults and refreshes the three map previews; normal map-selector rebuilding preserves manual catalog framing edits. The forest and gameplay collision height are unchanged.

## Structure

- `ForestRunnerGame.cs`: scene setup, game states, pooled obstacles/particles, spawning, scoring, input, persistence.
- `RunnerPlayer.cs`: jump/fast-drop integration, crouch hitbox, input buffer, sprite animation, landing squash.
- `RunnerHealth.cs`, `RunnerHeartGraphic.cs`: five-heart health, damage grace time, and pixel-heart UI.
- `RunnerDuckControl.cs`: mouse/touch hold and release handling for the Duck button.
- `RunnerRules.cs`: movement constants, hitboxes, capped speed and fair spawn intervals.
- `RunnerEncounterDirector.cs`: progressive, randomized obstacle combinations and safe encounter spacing.
- `RunnerDinoRoster.cs`: the four supplied dino sheets, portraits, and stable selection IDs.
- `RunnerMapCatalog.cs`: four maps, source/loop sprites, previews, parallax framing, and environment colors.
- `RunnerUiButtonFeedback.cs`, `RunnerUiReveal.cs`, `RunnerUiEffects.cs`: shared button feedback, panel entrances, and motion styling.
- `RunnerHud.cs`: start/retry/pause cards, score HUD, touch controls.
- `RunnerAudio.cs`: short generated jump/crash/milestone cues (no external audio required).

The ForestRun scene contains its **saved camera, forest layers, player, obstacle pool, effects, and complete UI**. They are visible and editable in the Hierarchy, Scene view, and Game view without pressing Play. Expand `Dino Rush - Forest` to find the grouped objects. Select the camera to see its frame, or use the Game tab for the start-screen preview. UI layout, colors, and fonts can be edited normally. Menu/score text still updates according to the game state.

The title screen recreates the supplied reference's centered stone/amber logo, moss-covered buttons, pixel lettering, and top-right SFX toggle. The original forest background remains unobstructed by extra corner branches or canopy overlays, and the trail has no extra UI logs, grass clumps, rocks, or floating leaf fragments. Its objects are under `Forest runner UI/16 by 9 safe content/Forest Main Menu`. **Customize** offers clickable Blue, Red, Yellow, and Green dino portraits, highlights the selected card, and saves the choice immediately; **Options** provides the persistent SFX toggle and control help; **Quit** asks for confirmation (and stops Play mode in the Editor). Esc closes these panels. The title-screen camera framing and smaller idle dino switch back to the gameplay framing on Play.

The idle title dino occasionally thinks aloud in a small cream pixel-art puff ("What a beautiful day!", "It's so peaceful here...", "I should stretch my legs.", and other quiet thoughts). It fades in/out above the selected dino, leaves pauses between lines, never repeats a line consecutively, and does not intercept clicks. It hides in every submenu and throughout running, pause, and results. Edit the lines and unscaled timing on `RunnerMenuThought` under `Forest Main Menu/Main menu buttons/Dino thought puff`; **Dino Rush → Add Menu Dino Thoughts** adds it to an older scene without rebuilding the menu or overwriting artwork. Normal main-menu authoring also preserves it.

All four dinos are unlocked cosmetic choices using `Assets/2D Sprites/dino/DinoSprites - 1.png` through `DinoSprites - 4.png`. The selected sheet drives idle, run, jump, duck, and crash frames with identical sizing, pivots, and hitboxes. Selection persists across restarts and scene/app reloads; unknown saved IDs safely fall back to Blue. `Assets/Generated/ForestDinoRoster.asset` references the original sheets and existing portrait slices without modifying their importers. Additional 24-frame animation sets are created once per selected color, cached for the current session, and cleaned up on scene exit. **Dino Rush → Add Dino Selector** authors the four cards and bindings in place; menu creation and layout repair also preserve the selector.

Gameplay obstacles stay hidden in the main-menu preview and activate through the pool when a run starts. The game reuses the saved hierarchy rather than generating another copy when Play starts.

Pause and game-over screens keep their compact centered layout, generous spacing, and minimal text while matching the game: deep forest panels, restrained moss/stone corner details, beveled green buttons, cream/sage pixel lettering, and gold record feedback. Pause shows the map, score/best, **Resume**, **Restart**, and **Main menu**. Game over shows score/best, an optional **New best** label, **Play again**, and **Main menu**. Repeated slogans, shortcut hints, and footer instructions stay hidden. The gameplay HUD hides behind either modal so it does not duplicate the card's stats. Resume preserves the exact run, Restart resets its score and obstacle pool, and Main menu saves your best before returning to the title. The existing keyboard shortcuts and retry/touch behavior remain supported.

The saved `Forest Pause Menu` and `Menu card` objects are under the UI safe-content root; enable either for layout editing after hiding `Forest Main Menu`. **Dino Rush → Match Minimal UI to Game Theme** (also **Apply Minimal Pause and Results UI**) reapplies the focused forest presentation in place, without rebuilding the world or changing the title/map/dino menus. `ForestMinimalUiBuilder.cs` reuses the existing nine-sliced `DialogFrame`, `ButtonPlay`, and `ButtonForest` art and the bundled Press Start 2P font. No source artwork is overwritten. The earlier rounded-surface asset is retained but unused. Gameplay-theme and duck-control authoring preserve this presentation once enabled.

The in-game HUD has top-left hearts, forest-style score/best cards, icon-only sound/pause buttons, and a pace meter. The former Forest Run/map and level panel stays hidden, including after pause/resume and restart. Jump/Duck use matching pixel-cut green controls, with the same large touch hit areas and multitouch support. The permanent keyboard-keycap strip, duplicate controls footer, and FAST DROP caption are hidden; the full controls guide remains available in **Options**. These hints stay hidden through pause/resume/retry/menu transitions. HUD-only forest sprites remain editable under `Assets/UI/ForestHud`; `ForestHudArt.cs` generates missing art without overwriting existing sprites.

Running kicks up a short brown pixel-dirt trail behind the dino's feet; landing creates a larger outward burst of dirt clods and dust puffs. Emission stops in midair and on menus, pause freezes the particles, and restarting/returning to the title clears them. The effect reuses the saved 36 sprites under `Forest effects/Trail dust pool` without allocating objects for each burst. Tune frequency/counts under **Pixel dirt effects** on `ForestRunnerGame`. The three point-filtered sprites live in `Assets/Generated/ForestDirtSprites.asset`; **Dino Rush → Apply Dirt Effects** assigns them to an older scene without rebuilding the world or overwriting existing dirt art.

Art stays in its original directories. Uniform dino frames, background sprites, and tiny effect textures are saved as subassets in `Assets/Generated/ForestSceneSprites.asset`, without changing your existing sprite slices. `ForestSprites.mat` keeps the supplied art unlit under URP. The **Dino Rush → Bake Scene Objects (Once)** tool supports converting an older unbaked ForestRun scene; it will not overwrite an already-authored hierarchy.

Scrolling layers use cleaned variants in `Assets/Generated/ForestLoops`, leaving the supplied PNGs and sprite slices untouched. The variants repair transparent edge gutters, match the first/last pixel columns, and use uncompressed point filtering without mipmaps. Adjacent tiles share one repeat stride with a one-texel overlap to prevent hairline gaps during movement and wrapping. **Dino Rush → Repair Forest Scrolling Seams** reapplies these bindings to the existing tiles; it does not rebuild the forest or UI.

Original menu sprites are in `Assets/UI/ForestMenu` and can be edited/replaced normally. `PixelMenuArt.cs` provides their deterministic authoring source, and **Dino Rush → Apply Forest Main Menu** upgrades an older scene once without replacing an existing menu. The Press Start 2P font is distributed under the SIL Open Font License; its license and attribution are included in `Assets/UI/Fonts/OFL.txt`.

The title logo uses mirrored, connected ivy sprigs with broad beveled leaves instead of jagged side spikes. **Dino Rush → Refine Title Logo** explicitly regenerates only `DinoRushLogo.png`, preserving the lettering, sprite reference, scene layout, and other menu art. Normal menu generation continues to preserve edited artwork.

### UI feedback and animation

All menu, dino-selector, settings, pause, results, and gameplay buttons share a soft sage hover glow, a small visual lift, press-down compression, and a short click pulse. The artwork moves rather than the button's RectTransform, keeping the actual click/touch area stable even at its edges. Touch holds support multiple pointers without sticky hover; keyboard jump/duck use the same feedback. Disabling a control, changing screens, or losing focus clears its visual state.

Menus and dialogs have short fade/slide entrances, gameplay HUD groups ease into view, and level/milestone banners pop in. The title gently floats by whole design pixels to keep its pixel-art edges crisp. Animation uses unscaled time, including on pause screens, and never delays a button action or changes a modal's input blocking. Interrupted transitions restore the exact original positions, scales, and opacity, so repeated opening/closing cannot shift the layout.

The saved scene contains editable `RunnerUiButtonFeedback`, `Outline`, `RunnerUiReveal`, and `CanvasGroup` components. **Dino Rush → Apply UI Motion** upgrades existing UI without rebuilding the scene or artwork. Menu/HUD/selector authoring tools also preserve/install these effects, and runtime initialization fills in missing components for older scenes. No tween package or Burst compilation is required.

Menu frames use nine-sliced borders, icons and labels have separate padded areas, and multiline pixel text uses extra line spacing. Only the Options page is enabled inside the inactive dialog for a clean editor preview. **Dino Rush → Repair UI Layout** reapplies these fixes to the existing hierarchy without deleting or regenerating the scene's artwork.

The four main-menu buttons share the same size, symmetric pixel-cut frame, mirrored corner decorations, and equal vertical gaps. Their captions use matching font sizes and two-pixel letter spacing. Each icon and caption is centered together as one group with a 12-pixel visible gap, accounting for transparent icon padding. **Dino Rush → Fix Main Menu Buttons** reapplies this layout and explicitly refreshes only the two button-frame textures; other artwork is preserved.

## Tests

Open **Window → General → Test Runner**. EditMode tests check pace limits, jump feasibility, obstacle spacing, collision bounds, no double jump, saved scene/UI/dirt references, and frame slicing. PlayMode tests load the actual scene and check start/pause/jump, collision, saved best, retry, and clearing obstacles. They also verify the running dirt trail, no midair emission, larger landing bursts, particle pooling/cleanup, that pause freezes the world, Resume preserves progress, Restart clears the previous run, and both pause/results menus return to the title without losing the best score. UI tests raycast buttons (rather than bypassing them), check fonts, label/icon bounds and multiline spacing, and capture menus at normal, tablet, and wide aspect ratios when `DINO_RUSH_CAPTURE_DIR` is set. Tests restore the prior high score and sound preference when finished.

Health tests check all five hearts, one loss per obstacle, cluster grace time, white/crying frames, floating heart-loss text, pause freezing, final-heart game over, and clean restart/menu state. The clean-course regression also requires keeping all five hearts.

Duck tests cover the crouch animation/hitbox, fast drops during ascent/apex/descent, ground clamping, jump-buffer cancellation, returning to normal jumps, combined Down/S and multitouch holds, pause/focus/reset cleanup, menu/results visibility, and raycastable touch input without accidental jumps. Optional captures include crouched and midair-drop views.

Dino-selector tests verify the four source sheets, saved menu bindings, raycastable cards, selection feedback/persistence/fallback, frame caching, matching animation colors and hitboxes, restart/menu behavior, and normal/tablet/wide layouts. These tests restore the previous dino selection and high score afterward.

UI-motion tests verify saved coverage and idempotent installation, mouse hover/press/click feedback, stable edge hit targets, disabled/right-click behavior, multitouch and keyboard feedback, focus/pause/menu cleanup, unscaled and interrupted modal transitions, and replayable level banners. Optional captures include hover, pressed, pause, tablet, and wide views.

Minimal-UI tests check focused modal visibility, score/best and record feedback, retained map choice, real raycastable resume/restart/retry/menu controls, touch target sizes, hidden repeated instructions across transitions, and controls help remaining in Options. Optional captures cover pause/results on standard, tablet, and wide viewports.

Map tests verify catalog/source references, native dimensions/pixel samples (including cropped moons), full-resolution imports and appropriate filtering, preview bindings, matching loop edges/import settings, real raycastable selector/start/back controls, cancellation, persistence/fallback, all four running environments, unchanged jump/duck behavior, pause/results/retry continuity, bounded renderer reuse, and scrolling coverage through repeated wraps. Optional captures include every map and selector tablet/wide views. The test suite temporarily defaults to Forest for existing regressions and restores the player's prior map choice afterward.
