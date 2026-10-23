# Dino Rush

A single-player endless runner with four selectable dinos and five maps: Forest, Desert, Jungle, Rural, and Snow Mountain, using the supplied sprite sets. Requires Unity **6000.4.1f1** and the packages already listed in this project.

## Play

Open **Assets/Scenes/ForestRun.unity** (or **Dino Rush → Open Forest Run**) and enter Play mode. In the main menu, click **Play**, choose a map preview, then click **Continue**. The selected map opens with an idle dino and **TAP TO START**. Tap/click anywhere in the game window (including portrait borders), or press Space/Enter, to begin running. ForestRun is also the first scene in Build Settings; all five maps use this shared scene. The original SampleScene and supplied artwork are left intact.

| Action | Controls |
| --- | --- |
| Choose map / prepare | Play, Space, or Enter opens the map selector; choose a card, then Continue |
| Start the prepared run | Tap/click anywhere, Space, Enter, Up, or W; the starting press does not also jump |
| Cancel before starting | Esc or P returns to the main menu, keeping the chosen map/dino |
| Retry | Space, Enter, click/tap the playfield, or Run Again |
| Jump | Space, Up, W, or click/tap the playfield |
| Duck / fast drop | Hold Down or S; a brief press in midair drops quickly |
| Pause / resume | Esc, P, or the pause button |
| Restart / leave run | Pause menu: Restart or Quit to Menu |
| Mute sound cues | M or SFX |

Jump over rocks, stumps, and crates. Each run starts with **five hearts**, shown in the top-left corner instead of the former map/level panel. An obstacle hit removes one heart; the run ends only when all five are gone. A short grace period protects against overlapping obstacles, and the same obstacle cannot damage you twice. Hits flash the selected dino's supplied white silhouette and crying pose, with a floating **−1 + heart** above it. Hit obstacles do not count as cleared. Pause freezes damage feedback and grace time; Retry/Restart restores all five hearts and clears the effects. Your distance is your score; speed gradually increases to a cap. There is one jump (no double jump), with a small pre-landing input buffer. Obstacle sizes and spacing allow time to jump and land between obstacles. Switching away from the game automatically pauses it.

The trail advances through **six designed stages** at 0, 12, 28, 48, 75, and 105 seconds of active running:

| Stage | Focus |
| --- | --- |
| First Steps | Three small, spaced introductory hurdles, then double clusters |
| Long Leaps | Wider two-obstacle jumps |
| Cluster Crossing | Triple clusters cleared in one jump |
| Quick Rhythm | Separate hurdles requiring two timed jumps |
| Burst Trail | Three-jump sequences with safe landing windows |
| Wild Run | Mixed skills at capped speed, with repeatable 30-second laps |

Stage changes open with a lighter recovery encounter, introduce the new signature, offer an easy practice jump, then repeat the signature. Six-encounter sections subsequently build pressure, combine challenges, ease off, and finish with a guaranteed recovery encounter. Teaching/recovery obstacles are smaller and teaching gaps more generous. Advanced patterns never appear before their teaching stage; the first three hurdles stay easy even if a custom run starts at a later stage. Seeded artwork, size, group gaps, and recovery gaps keep sections varied, with repetition/rapid-streak limits. Every retry uses a fresh course.

A compact **stage name, progress bar, and countdown** sit between the hearts and score in landscape, or beneath the hearts in portrait; the retired top-left map/level panel remains hidden. A banner and sound celebrate each new stage. Wild Run counts laps indefinitely without raising the speed cap, and each lap reopens with recovery. Progress uses active simulation time: pause freezes it, resizing preserves it, and Restart starts at First Steps. Stage/lap banners take priority over nearby distance-milestone messages. The progress HUD hides on the title, tap-to-start, pause, and results screens to keep them focused.

`RunnerEncounterDirector.cs` uses its own seeded random stream, so dust effects cannot change the course. Encounters are spaced by distance rather than a fixed repeating timer. Cluster spans are limited by jump clearance, separate jumps reserve landing/recovery time at the predicted arrival pace, and combinations spawn atomically from the existing pool. Difficulty, upcoming spacing, and the course freeze while paused and reset on Restart. Tests simulate thousands of encounters and complete accelerating three-minute courses using the real jump/collision rules.

Duck uses the dino's crouched running frames and a shorter collision box. In midair it immediately cuts upward momentum and accelerates the descent until landing, even if you release early. Hold to remain crouched on landing; release to stand and jump again. Ducking cancels buffered jumps, so a fast drop never causes an accidental bounce. It does not let you pass through ground obstacles. The on-screen Jump and Duck buttons are removed from the presentation on every map and screen size; tap/click-to-jump and keyboard jump/duck remain available. Legacy button objects stay inactive to preserve saved scene bindings.

Your best score, sound preference, chosen dino, and last map are saved locally with PlayerPrefs. Restarting a run does not erase your best or change your dino/map.

### Screen sizes and orientation

The display adapts automatically to resolution changes, desktop window resizing, portrait/landscape rotation, and phone safe areas (notches and home indicators). Landscape keeps the existing layout; portrait moves the readouts to the top and reflows map and dino choices into two columns. Jump/Duck buttons stay hidden in both orientations. Menus and scrims fit the usable area, and returning to landscape restores the authored layout without resetting the run.

The world always retains the complete original course width. Taller landscape ratios, including **16:10 and 4:3**, fill the usable screen with extra sky and soil instead of horizontal letterbox bars; HUD controls stay at the top/bottom edges. Portrait and ultrawide layouts retain a framed world view where necessary, without stretching or cropping. This preserves the same dino size relative to obstacles, reaction distance, jump timing, and difficulty. Heart-loss popups and menu thoughts follow the camera's actual viewport. No resolution is forced at startup; native-resolution/fullscreen behavior is retained, and desktop windows are resizable.

## Maps

**Play → Choose Your Map → Continue → Tap to Start** offers five preview cards: **Forest**, **Desert**, **Jungle**, **Rural** (the riverside village), and **Snow Mountain**. Selecting a card highlights and remembers it but does not start gameplay. Continue loads the selected map and dino into a waiting state: the course stays still, no obstacles or running dirt spawn, and score/stage timers stay at zero until a fresh tap or keyboard start. The confirmation click cannot dismiss the prompt, and the starting tap cannot also jump. Only ambient leaves/motes and the dino's idle animation continue. Gameplay controls/readouts stay hidden until running. Back or Esc/P returns to the title without starting a run; Esc/P also cancels the waiting screen without losing your choices. Customize also shows the selected map, and the pause heading uses its name. Retry/Restart retains the same map and chosen dino and still starts immediately. Returning to the title restores its original forest backdrop while remembering your choice. Invalid saved map IDs fall back to Forest.

Desert uses `Assets/2D Sprites/desert bg`, Jungle uses `Assets/2D Sprites/jungle bg`, Rural uses `Assets/2D Sprites/rural area bg`, and Snow Mountain uses `Assets/2D Sprites/skies bg`. Each has layered parallax, a matching running surface/soil, and themed dust/ambient colors. They share the same movement, collision bounds, obstacle pool, difficulty progression, and personal best; the existing forest high-score preference is retained.

**Snow Mountain** layers the supplied blue sky, moon, distant clouds, two snowy mountain ranges, drifting single cloud, and low cloud banks behind a frosted running surface. Blue-gray frozen subsoil replaces warm earth/roots, with pale powder and ambient flecks. Forest leaves remain hidden. Derived `snow_*` assets preserve source resolution; a one-pixel transparent gutter is padded in the derived loops only. The five-card selector expands in landscape and uses a two-column, three-row portrait grid with the fifth card centered. Tap to Start, saved choices, retries, pause, and responsive views work identically to the other maps; removed Jump/Duck buttons stay hidden.

`Assets/Generated/RunnerMaps/MapCatalog.asset` holds editable source references, sprites, layer heights/bottoms/speeds, and colors. New map layers are created lazily on first use and reused thereafter, rather than duplicated on retry. The original saved forest objects remain intact. Loop variants preserve native source pixels and use reflected extensions with matching edge pixels, no compression/mipmaps, and one-texel tile overlap. Desert/Rural/Snow scenery uses bilinear filtering; Jungle and the running surfaces retain crisp point-filtered pixel art. Import size limits accommodate the full reflected width (up to 7600 pixels), avoiding hidden downscaling. Skies extend below their supplied transparent horizons; moons use native-resolution cropped, stationary sprites. Running surfaces recolor the existing ground texture without changing its size or collision height. All source PNGs and import settings are preserved.

**Dino Rush → Add Map Selector and Maps** authors the catalog and editable submenu without rebuilding the world; reapplication appends a missing Snow Mountain entry while preserving existing generated art/catalog edits. **Dino Rush → Refresh Map Previews** explicitly redraws all catalog previews from their framing. The selector inherits the shared hover, click, and entrance effects.

**Dino Rush → Restore Map Background Source Resolution** explicitly replaces the generated Desert/Jungle/Rural/Snow scenery and refreshes their previews from the original sources, keeping their asset GUIDs and catalog framing. Use this to upgrade older 256-pixel-high generated backgrounds. It leaves the forest, shared running surfaces, custom artwork references, and original source files unchanged. Full-resolution backgrounds use more texture memory than the old reduced-resolution versions.

New maps use individual scenic framing rather than one full-screen frame for every layer. Desert dunes sit just behind the trail with the mesas farther back; Jungle's five registered layers place the roots behind the running surface and keep the canopy overhead. Rural's smaller distant mountain leaves open sky, and the village/river/reflection/near bank share a canvas so the bank meets the trail instead of hiding below it. Snow's low cloud bank meets the frosted trail, with two mountain depths above it. Moons sit in the open area between the HUD cards. **Dino Rush → Refine New Map Layer Placement** explicitly reapplies these defaults and refreshes nonforest map previews; normal map-selector rebuilding preserves manual catalog framing edits. The forest and gameplay collision height are unchanged.

### Soil beneath the trail

The flat fill below the grass is replaced with layered pixel-art earth: subtle sediment bands, branching roots, and small shaded stones, darkening with depth. All five maps have matching palettes in `Assets/Generated/TrailSoil` (Snow uses root-free frozen rock); the supplied grass and background artwork are unchanged. The saved **Forest environment/Earth below the trail** renderer uses a tiled sprite at the grass strip's native pixel density, extending to the bottom on 16:10 and 4:3 without stretching. It scrolls with the trail and stays still during Tap to Start, pause, and game over. The title retains its gentle scenery scroll. No colliders or gameplay changes are involved.

**Dino Rush → Regenerate Trail Soil Textures** rebuilds only these derived textures. `RunnerSoilArt.cs` uses a fixed private seed; older scenes without saved soil bindings generate and cache the same artwork at runtime.

## Structure

- `ForestRunnerGame.cs`: scene setup, game states, pooled obstacles/particles, spawning, scoring, input, persistence.
- `RunnerPlayer.cs`: jump/fast-drop integration, crouch hitbox, input buffer, sprite animation, landing squash.
- `RunnerHealth.cs`, `RunnerHeartGraphic.cs`: five-heart health, damage grace time, and pixel-heart UI.
- `ForestLeafWind.cs`: forest-only pooled windblown leaves using the supplied leaf texture.
- `RunnerScreenLayout.cs`, `RunnerResponsiveUi.cs`: safe-area camera fitting, portrait reflow, and resize/rotation layout restoration.
- `RunnerDuckControl.cs`: legacy hold/release handling retained for inactive scene bindings.
- `RunnerRules.cs`: movement constants, hitboxes, stage timing/progress, endless laps, capped speed and fair spawn intervals.
- `RunnerEncounterDirector.cs`: staged teaching sequences, challenge/recovery sections, randomized obstacle combinations and safe encounter spacing.
- `RunnerDinoRoster.cs`: the four supplied dino sheets, portraits, and stable selection IDs.
- `RunnerMapCatalog.cs`: five maps, source/loop sprites, previews, parallax framing, and environment colors.
- `RunnerUiButtonFeedback.cs`, `RunnerUiReveal.cs`, `RunnerUiEffects.cs`: shared button feedback, panel entrances, and motion styling.
- `RunnerHud.cs`: start/retry/pause cards and gameplay HUD.
- `RunnerAudio.cs`: short generated jump/crash/milestone cues (no external audio required).

The ForestRun scene contains its **saved camera, forest layers, player, obstacle pool, effects, and complete UI**. They are visible and editable in the Hierarchy, Scene view, and Game view without pressing Play. Expand `Dino Rush - Forest` to find the grouped objects. Select the camera to see its frame, or use the Game tab for the start-screen preview. UI layout, colors, and fonts can be edited normally. Menu/score text still updates according to the game state.

The title screen recreates the supplied reference's centered stone/amber logo, moss-covered buttons, pixel lettering, and top-right SFX toggle. The original forest background remains unobstructed by extra corner branches or canopy overlays, and the trail has no extra UI logs, grass clumps, or rocks. A subtle world-space leaf breeze animates the forest rather than adding static leaf decorations to the UI. Menu objects are under `Forest runner UI/16 by 9 safe content/Forest Main Menu`. **Customize** offers clickable Blue, Red, Yellow, and Green dino portraits, highlights the selected card, and saves the choice immediately; **Options** provides the persistent SFX toggle and control help; **Quit** asks for confirmation (and stops Play mode in the Editor). Esc closes these panels. The title-screen camera framing and smaller idle dino switch back to the gameplay framing on Play.

The idle title dino occasionally thinks aloud in a small cream pixel-art puff ("What a beautiful day!", "It's so peaceful here...", "I should stretch my legs.", and other quiet thoughts). It fades in/out above the selected dino, leaves pauses between lines, never repeats a line consecutively, and does not intercept clicks. It hides in every submenu and throughout running, pause, and results. Edit the lines and unscaled timing on `RunnerMenuThought` under `Forest Main Menu/Main menu buttons/Dino thought puff`; **Dino Rush → Add Menu Dino Thoughts** adds it to an older scene without rebuilding the menu or overwriting artwork. Normal main-menu authoring also preserves it.

All four dinos are unlocked cosmetic choices using `Assets/2D Sprites/dino/DinoSprites - 1.png` through `DinoSprites - 4.png`. The selected sheet drives idle, run, jump, duck, and crash frames with identical sizing, pivots, and hitboxes. Selection persists across restarts and scene/app reloads; unknown saved IDs safely fall back to Blue. `Assets/Generated/ForestDinoRoster.asset` references the original sheets and existing portrait slices without modifying their importers. Additional 24-frame animation sets are created once per selected color, cached for the current session, and cleaned up on scene exit. **Dino Rush → Add Dino Selector** authors the four cards and bindings in place; menu creation and layout repair also preserve the selector.

Gameplay obstacles stay hidden in the main-menu preview and activate through the pool when a run starts. The game reuses the saved hierarchy rather than generating another copy when Play starts.

Pause and game-over screens keep their compact centered layout, generous spacing, and minimal text while matching the game: deep forest panels, restrained moss/stone corner details, beveled green buttons, cream/sage pixel lettering, and gold record feedback. Pause shows the map, score/best, **Resume**, **Restart**, and **Main menu**. Game over shows score/best, an optional **New best** label, **Play again**, and **Main menu**. Repeated slogans, shortcut hints, and footer instructions stay hidden. The gameplay HUD hides behind either modal so it does not duplicate the card's stats. Resume preserves the exact run, Restart resets its score and obstacle pool, and Main menu saves your best before returning to the title. The existing keyboard shortcuts and retry/touch behavior remain supported.

The saved `Forest Pause Menu` and `Menu card` objects are under the UI safe-content root; enable either for layout editing after hiding `Forest Main Menu`. **Dino Rush → Match Minimal UI to Game Theme** (also **Apply Minimal Pause and Results UI**) reapplies the focused forest presentation in place, without rebuilding the world or changing the title/map/dino menus. `ForestMinimalUiBuilder.cs` reuses the existing nine-sliced `DialogFrame`, `ButtonPlay`, and `ButtonForest` art and the bundled Press Start 2P font. No source artwork is overwritten. The earlier rounded-surface asset is retained but unused. Gameplay-theme and duck-control authoring preserve this presentation once enabled.

The in-game HUD has top-left hearts, compact stage progress, forest-style score/best cards, icon-only sound/pause buttons, and a pace meter. The former Forest Run/map and level panel stays hidden, including after pause/resume and restart. Jump/Duck buttons also stay hidden through all transitions, leaving the playfield clear for tap-to-jump. The permanent keyboard-keycap strip, duplicate controls footer, and FAST DROP caption are hidden; the full controls guide remains available in **Options**. HUD-only forest sprites remain editable under `Assets/UI/ForestHud`; `ForestHudArt.cs` generates missing art without overwriting existing sprites.

Running kicks up a short brown pixel-dirt trail behind the dino's feet; landing creates a larger outward burst of dirt clods and dust puffs. Emission stops in midair and on menus, pause freezes the particles, and restarting/returning to the title clears them. The effect reuses the saved 36 sprites under `Forest effects/Trail dust pool` without allocating objects for each burst. Tune frequency/counts under **Pixel dirt effects** on `ForestRunnerGame`. The three point-filtered sprites live in `Assets/Generated/ForestDirtSprites.asset`; **Dino Rush → Apply Dirt Effects** assigns them to an older scene without rebuilding the world or overwriting existing dirt art.

### Forest leaf breeze

The forest map and its title preview have **24 windblown leaves** using `Assets/2D Sprites/leaf.png`. The supplied white/alpha leaf is tinted in sage, green, and warm gold without modifying the PNG, its existing sprite slice, or import settings. During a run, leaves sweep right-to-left with the scrolling course, speeding up with the run and repeatedly passing behind the dino. Shared gusts, restrained vertical flutter, tumbling rotation, and edge-on turns complement that backward travel rather than making leaves hover in place. Smaller, slower, translucent leaves add depth; both depth layers render behind the player and obstacles so hazards remain readable. Leaves fade at the edges/above the grass and recycle from the right, ahead of the runner. The idle title retains a slower ambient breeze.

Select **Dino Rush - Forest → Forest Leaf Wind** in the Inspector to tune **Leaf Count**, **Drift Speed**, **Gust Strength**, **Fall Speed**, **Run Scroll Influence**, and the cosmetic random seed. Run Scroll Influence controls how strongly leaves move backward with the course (default 1). The component is already wired into `ForestRun.unity`; its runtime pool appears at `Forest effects/Windblown leaves`. It runs on the forest title (even if another map is selected), hides on all other maps, freezes during pause/game over, and reuses the same pool on restart/map changes. The camera's actual view determines its bounds, including taller landscape ratios. It has no colliders, never consumes the course's random stream, and cleans up its runtime sprite/pool when removed or the scene closes.

Art stays in its original directories. Uniform dino frames, background sprites, and tiny effect textures are saved as subassets in `Assets/Generated/ForestSceneSprites.asset`, without changing your existing sprite slices. `ForestSprites.mat` keeps the supplied art unlit under URP. The **Dino Rush → Bake Scene Objects (Once)** tool supports converting an older unbaked ForestRun scene; it will not overwrite an already-authored hierarchy.

Scrolling layers use cleaned variants in `Assets/Generated/ForestLoops`, leaving the supplied PNGs and sprite slices untouched. The variants repair transparent edge gutters, match the first/last pixel columns, and use uncompressed point filtering without mipmaps. Adjacent tiles share one repeat stride with a one-texel overlap to prevent hairline gaps during movement and wrapping. **Dino Rush → Repair Forest Scrolling Seams** reapplies these bindings to the existing tiles; it does not rebuild the forest or UI.

Original menu sprites are in `Assets/UI/ForestMenu` and can be edited/replaced normally. `PixelMenuArt.cs` provides their deterministic authoring source, and **Dino Rush → Apply Forest Main Menu** upgrades an older scene once without replacing an existing menu. The Press Start 2P font is distributed under the SIL Open Font License; its license and attribution are included in `Assets/UI/Fonts/OFL.txt`.

The title logo uses mirrored, connected ivy sprigs with broad beveled leaves instead of jagged side spikes. **Dino Rush → Refine Title Logo** explicitly regenerates only `DinoRushLogo.png`, preserving the lettering, sprite reference, scene layout, and other menu art. Normal menu generation continues to preserve edited artwork.

### UI feedback and animation

All menu, dino-selector, settings, pause, results, and gameplay buttons share a soft sage hover glow, a small visual lift, press-down compression, and a short click pulse. The artwork moves rather than the button's RectTransform, keeping the actual click/touch area stable even at its edges. Touch holds support multiple pointers without sticky hover. Removed Jump/Duck buttons remain inactive and do not animate in response to keyboard input. Disabling a control, changing screens, or losing focus clears its visual state.

Menus and dialogs have short fade/slide entrances, gameplay HUD groups ease into view, and level/milestone banners pop in. The title gently floats by whole design pixels to keep its pixel-art edges crisp. Animation uses unscaled time, including on pause screens, and never delays a button action or changes a modal's input blocking. Interrupted transitions restore the exact original positions, scales, and opacity, so repeated opening/closing cannot shift the layout.

The saved scene contains editable `RunnerUiButtonFeedback`, `Outline`, `RunnerUiReveal`, and `CanvasGroup` components. **Dino Rush → Apply UI Motion** upgrades existing UI without rebuilding the scene or artwork. Menu/HUD/selector authoring tools also preserve/install these effects, and runtime initialization fills in missing components for older scenes. No tween package or Burst compilation is required.

Menu frames use nine-sliced borders, icons and labels have separate padded areas, and multiline pixel text uses extra line spacing. Only the Options page is enabled inside the inactive dialog for a clean editor preview. **Dino Rush → Repair UI Layout** reapplies these fixes to the existing hierarchy without deleting or regenerating the scene's artwork.

The four main-menu buttons share the same size, symmetric pixel-cut frame, mirrored corner decorations, and equal vertical gaps. Their captions use matching font sizes and two-pixel letter spacing. Each icon and caption is centered together as one group with a 12-pixel visible gap, accounting for transparent icon padding. **Dino Rush → Fix Main Menu Buttons** reapplies this layout and explicitly refreshes only the two button-frame textures; other artwork is preserved.

## Tests

Open **Window → General → Test Runner**. EditMode tests check pace limits, jump feasibility, obstacle spacing, collision bounds, no double jump, saved scene/UI/dirt references, and frame slicing. PlayMode tests load the actual scene and check start/pause/jump, collision, saved best, retry, and clearing obstacles. They also verify the running dirt trail, no midair emission, larger landing bursts, particle pooling/cleanup, that pause freezes the world, Resume preserves progress, Restart clears the previous run, and both pause/results menus return to the title without losing the best score. UI tests raycast buttons (rather than bypassing them), check fonts, label/icon bounds and multiline spacing, and capture menus at normal, tablet, and wide aspect ratios when `DINO_RUSH_CAPTURE_DIR` is set. Tests restore the prior high score and sound preference when finished.

Health tests check all five hearts, one loss per obstacle, cluster grace time, white/crying frames, floating heart-loss text, pause freezing, final-heart game over, and clean restart/menu state. The clean-course regression also requires keeping all five hearts.

Level-design tests verify signature introductions, skill unlock gates, guaranteed recovery sections, endless-lap resets, repetition limits, jump clearance, and full accelerating courses. Progress tests check boundary/countdown math, live bar geometry, resize/pause/restart preservation, and stage/lap banner priority. Optional captures include `forest-progression-level-4` and `progress-wild-run`.

Leaf-wind tests verify the saved source-texture binding, drift/rotation/flutter, grass clearance, long-running pool reuse, repeated backward travel past the player, acceleration with the course, pause/game-over freezing, restart/map isolation, random-stream independence, and runtime cleanup. Optional `forest-wind-*` captures include gameplay, recycled leaves, 16:10, the title preview, and a consecutive `forest-wind-sweep-*` motion sequence.

Tap-to-start tests confirm all five maps/dino choices, idle waiting without simulation or spawns, confirmation-click isolation, mouse/touch/keyboard starts, taps outside the world viewport, no jump on the first press, Escape cancellation, and responsive prompt captures (`tap-to-start-*`).

Soil tests cover saved sprite bindings, opaque textured pixels, deterministic generation independent of gameplay randomness, map palettes/reuse, non-stretching screen coverage, scrolling/wraparound, and waiting/pause/game-over freezing. Optional `soil-*` captures show all maps plus 16:10 and tablet views.

Display tests cover small portrait phones, tall phones, tablets, standard HD/4K, ultrawide screens, asymmetric cutouts, and minimized-window fallbacks. Scene tests raycast controls, check text and button bounds, verify portrait grids/touch sizes, and rotate/resize without losing health, score, pause state, or the original landscape layout. Optional `responsive-*` captures include the portrait title, dino/map selectors, Options, gameplay, pause, and an ultrawide view.

Duck tests cover the crouch animation/hitbox, fast drops during ascent/apex/descent, ground clamping, jump-buffer cancellation, returning to normal jumps, combined Down/S holds, pause/focus/reset cleanup, and hidden movement buttons. Tapping the former button areas now triggers normal world jumps. Optional captures include crouched and midair-drop views.

Dino-selector tests verify the four source sheets, saved menu bindings, raycastable cards, selection feedback/persistence/fallback, frame caching, matching animation colors and hitboxes, restart/menu behavior, and normal/tablet/wide layouts. These tests restore the previous dino selection and high score afterward.

UI-motion tests verify saved coverage and idempotent installation, mouse hover/press/click feedback, stable edge hit targets, disabled/right-click behavior, retired movement controls staying inactive, focus/pause/menu cleanup, unscaled and interrupted modal transitions, and replayable level banners. Optional captures include hover, pressed, pause, tablet, and wide views.

Minimal-UI tests check focused modal visibility, score/best and record feedback, retained map choice, real raycastable resume/restart/retry/menu controls, touch target sizes, hidden repeated instructions across transitions, and controls help remaining in Options. Optional captures cover pause/results on standard, tablet, and wide viewports.

Map tests verify catalog/source references, native dimensions/pixel samples (including cropped moons), full-resolution imports and appropriate filtering, preview bindings, matching loop edges/import settings, real raycastable selector/start/back controls, cancellation, Snow selection persistence/fallback, all five running environments, unchanged jump/duck behavior, pause/results/retry continuity, bounded renderer reuse, and scrolling coverage through repeated wraps. Optional captures include every map, Snow gameplay on 16:10/tablet/portrait, and selector tablet/wide/portrait views. The test suite temporarily defaults to Forest for existing regressions and restores the player's prior map choice afterward.
