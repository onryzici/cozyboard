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
| Paint-brush icon | Select an installed key, enlarge it in the paint studio, then draw freely |
| Mouse wheel in paint mode | Change brush size |
| Alt + left mouse in paint mode | Sample a color from a keycap |
| Curved arrow icon | Undo the latest placement |
| Floppy disk icon | Save the current order and assembly |
| Envelope icon | Open customer order / deliver completed board |
| Gear icon | Independent music and effect volumes |

The save file is `workshop-save.json` in Unity's `Application.persistentDataPath`. A saved order is restored on launch. Assembly progress is saved explicitly using the disk icon; delivery also saves. Undo history lasts for the current session. Parts use direct manipulation, not rigidbody simulation.

The focused keycap studio uses a 512-pixel drawing surface per key (up to 2048 pixels wide for long keys), with continuous stroke interpolation for detailed lettering and small symbols. Painting replaces the original molded legend so a design can cover the full key face.

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
