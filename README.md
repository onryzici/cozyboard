# Cozy Board

A cozy, painterly keyboard assembly game built in **Unity 6000.5.6f1 / URP**.

This repository contains only the Unity version. Open this folder in Unity Hub, then open `Assets/CozyBoard/Scenes/Workbench.unity` and press Play.

![Workshop](Verification/unity-completed-4k.png)

## Build the keyboard

- Four open cardboard supply boxes: PCB, plate, switches and keycaps.
- Assembly follows the physical dependency order: PCB, plate, all switches, then keycaps.
- Pick a cap from its box: its width, shape and legend adapt to the destination socket.
- Hold the left mouse button and sweep over sockets to place successive parts. An occupied socket never consumes another part.
- Alternatively, drag and release one piece, or click to pick up and click again to place.
- Parts align, press against a little resistance, click into place and settle.
- The subtle dashed guide appears only at a valid target while holding a part.
- Test installed keys with mouse clicks or your physical keyboard, including before the board is complete.

### Controls

| Input / icon | Action |
|---|---|
| Left mouse on a box or loose part | Pick up |
| Hold and sweep over keyboard | Repeated placement |
| Escape | Return the held part / close a panel |
| Right mouse drag | Small camera orbit |
| Middle mouse drag | Small camera pan |
| Mouse wheel | Zoom |
| Arrow icon | Place parts |
| Four-arrow icon | Move the keyboard case or tools |
| Circular arrow icon | Rotate a case/tool by clicking it |
| Paint-brush icon | Select an installed key and paint directly on its 3D surface |
| Mouse wheel in paint mode | Change brush size |
| Right / middle drag in paint studio | Orbit the 3D keycap, including its sides |
| Shift + mouse wheel in paint studio | Zoom |
| Alt + left mouse in paint mode | Sample a color from a keycap |
| F in paint studio | Reset the view |
| Ctrl / Cmd + Z or Y | Undo / redo paint |
| Curved arrow icon | Undo the latest placement |
| Floppy disk icon | Save the current order and assembly |
| Envelope icon | Open customer order / deliver completed board |
| Gear icon | Independent music and effect volumes |

The save file is `workshop-save.json` in Unity's `Application.persistentDataPath`. A saved order is restored on launch. Assembly progress is saved explicitly using the disk icon; delivery also saves. Undo history lasts for the current session. Parts use direct manipulation, not rigidbody simulation.

The paint studio renders the actual sculpted keycap with studio lighting. Orbit the model to paint the top, bevels and side walls directly. Six separate 512-pixel texture islands keep opposite faces independent; brush strokes are evaluated on the 3D surface and interpolated along the pointer path. A surface-aligned brush ring shows the contact area. The color wheel, value slider, brush tips, fill and undo/redo controls remain available beside the model. Raised legends can be shown or hidden. Saved artwork includes side paint; older flat artwork migrates to the top face.

Verify the 3D paint and save migration with `unity run . -- -executeMethod CozyBoard.Editor.WorkshopStudioVerify.Run`. The Editor menu **Cozy Board → Preview 3D paint studio** temporarily assembles the keyboard and opens a key for inspection in Play mode; it does not write the player save.

## Art and audio

Warm amber ground with restrained marks, an imperfect teal cutting mat, rounded cream keyboard case, sculpted keycaps and simple painted boxes. Dynamic projected mesh silhouettes produce colored, offset shadows. Background light and the parts share the same window-shadow coordinates.

Game output defaults to **3840×2160**, with Retina support and 4× MSAA. The actual window size depends on the display. Procedural surfaces, geometry and text render at native resolution. Raster sources are documented honestly in `Documentation/GeneratedArt.md`: the 1672×941 icon atlas and 1024×1536 clipboard are displayed at or below their source pixel density in the UHD interface. A 4K render does not imply every source image is 4K.

Recorded Cherry MX Red PBT switches and the “Chill lofi inspired” music were reused from the user's existing `keyboard/LittleSwitch` project. That project also supplied the installation timing. The `keyboardV2/KeyboardWorkshop` folder was inspected; its walking-only scene had no assembly interaction or recorded keyboard sounds.

See [third-party notices](THIRD_PARTY_NOTICES.md) for the MIT sound license and CC0 music credit.

## Review images

All are **3840×2160 images rendered by Unity**, not concept mockups:

- [Initial workbench](Verification/unity-game-4k.png)
- [Installed switches](Verification/unity-switches-4k.png)
- [Completed keyboard](Verification/unity-completed-4k.png)
- [Customer order](Verification/unity-order-4k.png)

## Verification and build

```sh
# Automated checks: enforced assembly order, destination-adaptive caps, continuous sweep,
# per-pixel drawing, drawing save/restore, recorded audio and shader compilation.
unity run . -- -nographics -executeMethod CozyBoard.Editor.WorkshopGameBuild.Verify

# macOS player (output is excluded from Git)
unity run . -- -nographics -executeMethod CozyBoard.Editor.WorkshopGameBuild.BuildMac

# Native UHD engine review images
unity run . -- -executeMethod CozyBoard.Editor.WorkshopRenderReview.Render
```

An additional opt-in player smoke runner accepts `--cozy-smoke --cozy-report /absolute/report.txt`. Run the built player with `-batchmode -nographics` to exercise actual mouse input, continuous placement, UI callbacks and animation completion without opening a game window. Normal game sessions do not run this probe.

Reports are in `Verification/`. `Library`, `Temp`, `Logs`, `UserSettings`, standalone builds and IDE caches are excluded from the repository.

## Source map

- `Scripts/WorkshopController.cs`: selection, drag/click carry, sweep placement, targeting, camera and projected shadows.
- `Scripts/WorkshopGameMode.cs`: free assembly, supply, installation motion, keyboard testing, undo and saves.
- `Scripts/WorkshopMenu.cs`: painted menu actions, settings and customer order.
- `Scripts/WorkshopAudio.cs`: recorded sound variation and music fade.
- `Editor/WorkshopFreeBuild.cs`: physical packages, rounded case and painted UI setup.
- `Editor/WorkshopFreeVerify.cs`: deterministic assembly regression checks.
- `Shaders/`: painterly surface, shared window light, dashed guide and silhouette shadows.

These paths are relative to `Assets/CozyBoard/`. Native meshes, materials, prefabs and scene data are included; no Godot or Blender installation is needed. Historical `WorkshopMigration.Build` rebuilds the earlier imported scene and should not be used for the current game. `WorkshopGameBuild.Configure` reapplies the current authored setup and resets the editor scene to a fresh order.

## Cozy workshop experience

The single Workbench scene now starts with a main menu: Continue, New game (confirms replacing a save), replayable illustrated tutorial, settings and quit. The seven tutorial steps cover orders, assembly, painting, inspection and shipping.

- The orbit toolbar icon lifts the whole keyboard. Drag to rotate freely, scroll to zoom, then use **Masaya bırak** or Escape.
- Painting uses the rendered 3D keycap, including its side walls. Right/middle drag rotates it; brush icons have hover hints. Fresh strokes briefly look wet before drying.
- Completed orders enter four packing steps: place keyboard, fold protective paper, close and tape, ship. Cancelling restores the keyboard.
- The tabletop is pale honey oak with subtle grain; the cutting mat is retained. A low-intensity bloom adds soft light.
- Generated illustration prompts and saved asset locations: [Generated art](Documentation/GeneratedArt.md).

Verification: `Verification/3d-paint-checks.txt` and `Verification/experience-checks.txt`.

Latest polish: the preset color tiles are removed; Patrick Hand includes all Turkish letters; the tutorial guide stands behind its dialogue card. Only the current supply carton is visible, with up to 18 loosely piled small parts. Exhausted cartons disappear and a new order restores the first carton. Checks: `Verification/revision-checks.txt`.

## Laptop and Tık

Click the sage laptop to open Atölye Pazarı (or press L outside keyboard typing mode). Buy PCB + plate kits, three switch sound profiles, and PBT keycap palettes. Each small-parts kit contains the 61 parts needed for one keyboard; one kit is consumed per assembly category, not per piece. Unused kits can be returned at their purchase price. Already fitted categories keep their choice until the next keyboard.

The workshop currency is **Tık**, shown as a honey-gold keycap coin. Start with 320 Tık and one basic kit per category; a shipped keyboard earns 240 Tık plus up to 60 Tık for meeting customer wishes. Balance, inventory, selected variants and consumed kits are included in saves (since version 6). Older saves migrate with a starter inventory.

Supply cartons slide in and settle, then leave when empty. Only the active assembly stage's carton remains on the desk. Legacy authored box visuals have been removed from the single Workbench scene.

Headless shop checks: run the built player with `-batchmode -nographics --cozy-shop-smoke`; it uses a separate temporary save and writes `shop-smoke-report.txt` in the application's temporary cache.

The laptop now rests closed on the desk, with a sculpted sage lid, embossed keycap maker mark, rounded metal edges, hinge and ports; clicking it opens the catalogue.

Desk tools are interactive: select the screwdriver and click the four case corner screws to fasten/unfasten them. New orders require all four screws before packing. The keycap puller returns a fitted cap to its supply box; the switch puller works after its cap is removed. Reinstalling recovered parts does not consume another kit. Escape puts a tool down. Screw state is saved; completed legacy keyboards migrate as fastened.

Key presses move the rigid cap into the switch and return it to its top stop without scaling or tilting. Travel varies with the selected switch profile. Both mouse clicks and keyboard testing use the feedback.

Each desk tool now travels to its target and returns to its original pose. The screwdriver seats its tip on the selected screw and rotates around that contact point. Both pullers close their grip, rock and lift the selected part, then return to the desk. Tool actions block overlapping interactions; cancelling/resetting restores the moving tool.

Menu refresh: dedicated full-bleed atelier art, cream wordmark, animated menu entrance/hover, and quieter secondary actions. Laptop footprint is 32% wider/deeper with neutral satin grey metal. The bottom toolbar uses matching 86px icons, cropped palette art, and a gold underline for the active mode. A house mark replaces the three-dot home button. Order/settings panels fade in.

Packing now uses desk gestures instead of a next-step button: drag the keyboard into the carton, drag horizontally across the paper, click the open lid, then drag the parcel upward to ship. Shipping uses a soft curtain transition before the next order card appears. Escape cancels packing and restores the keyboard/camera. Screwdriver detail placement follows its actual longitudinal axis, removing the stray floating stripes.

Laptop placement: choose the move tool, drag the laptop, release to place; Escape restores its previous position. Clicking it in normal placement mode opens the shop. Placement is retained in shop save data. Key travel is now more pronounced, and mouse-held keys stay depressed until released. Menu art uses a flatter painted treatment with simplified shadows.

Windows release: `WorkshopBackgroundBuild.RequestWindows()` queues a non-development Windows x64 Mono build under `Builds/Windows/CozyBoard.exe`. Distribute the entire folder, including `CozyBoard_Data`, `MonoBleedingEdge`, and Unity DLLs.

### Assembly feel

Switch placement now pauses against the plate before a short locking snap and a damped settle. Assembly clicks use a separate sound channel so switch typing profiles do not colour tool sounds. Screws turn in three wrist strokes with quiet friction and a brief local torque vibration at the end; loosening has a softer release. Effects volume controls all these sounds. Continuous placement remains available.

The two short screw sounds in `Assets/Resources/Assembly` are original procedural audio. The shop smoke runner checks resistance, exact resting poses, occupied sockets, audio isolation, screw tightening/loosening and interrupted tool cleanup.

### Customer requests

Orders rotate through Ece (quiet, linear), Deniz (full-bodied sound, tactile feedback) and Mina (audible click, tactile feedback). The order card and switch catalogue show how the chosen set meets both wishes. PCB colour, cap palette and painting remain free creative choices and do not affect evaluation. Delivery pays 240 Tık plus 30 Tık per matched wish, up to 300 Tık. A customer mail explains the result in the laptop inbox while the next order stays accessible. Save version 9 archives read and unread messages; older saves keep their progress and wallet and migrate their last valid receipt.

### Testing and presentation

Completed boards now require a 61-key check before packing. Open the test card, press physical keys or the miniature keyboard, and find the unseated switch. Remove its cap and switch with the corresponding tools, reinstall both, then retest. Repeated presses count only once; removing a part invalidates its key's result. Version 8 saves preserve checked keys and repair state. Older saves require testing but do not acquire an invented fault on load.

Progress sits on a paper card. Ece, Deniz and Mina have distinct painted portraits; the last customer's portrait also appears on their delivery note. Tools are distributed around the bench with a linen rest for pullers. Packing hides desk tools, centres the carton, folds two tissue wings, draws sealing tape and adds a named parcel label. Gesture input and explicit step buttons are both available. Cancelling restores the board, desk props and camera.

Presentation refinement: progress is compact text at the bottom. The paint studio uses a close-up printed cutting-mat backdrop, and tools rest on a separate gridded mat to the right of the main work area. Laptop placement reserves all supply-carton footprints and open lids, the keyboard and the tool mat; invalid legacy placements move to the clear lower-left space.

Key feedback now retriggers on rapid taps and follows physical key holds. The cap stays rigid, travels down 0.145–0.16 units, stays depressed while held, and returns to its top stop in 0.12 seconds.

Customer reactions now arrive as a top-of-screen mail notification. Clicking it or opening the laptop's inbox shows the customer's portrait, letter and delivery summary. Reading marks a message read without changing the reward. Save version 9 archives all new delivery messages; older saves migrate their latest receipt. The order card remains available for the next order without acknowledging mail first.

The compact progress text sits below the mat. The home button uses `Assets/Resources/UI/WorkshopHome.png` (built-in ImageGen, prompt recorded in `Verification/home-art-generation.txt`). The cursor is a small warm-white arrow with a soft outline; available assembly parts use an open hand, and carried parts use a holding hand. The studio mat has low-amplitude fine grain and a soft projected key shadow that follows the viewing angle. The single Workbench scene remains; the Unity splash screen is disabled.

Painting feel prototype (after GitHub checkpoint `checkpoint-2026-09-30-before-painting`): brush contact builds translucent pigment over time, with slight wet-on-wet mixing. Wet highlights fade over seven seconds. Choose **Bant çek**, drag a strip across the 3D cap, then choose **Fırçaya dön** to paint over it. The wheel/size control adjusts tape width. **Bandı sök** peels the strips away to reveal protected colour. Up to eight strips can wrap across the top and sides. Brush strokes and fill respect the mask; undo/redo includes tape placement and peeling. Version 10 saves preserve masking strips alongside existing artwork; earlier artwork loads without tape. UV edge padding prevents thin base-colour seams between faces.

Desk refinements: home-button transparent padding is cropped to match the other header icons. Unread customer mail shows a clickable envelope and count over the laptop, as well as the top notification. The laptop sits further up/left with its left edge partly off-screen; the left switch carton and stock move together to keep the area clear. Existing laptop positions migrate once to the revised layout.
