# Masked Murderer AR Prototype

## Play Flow
1. Launch the app: the Welcome screen shows the backstory from `story.txt`.
2. Press **Start** to enter boundary placement.
3. Tap real-world surfaces to place boundary markers. The UI shows `Placed X/Y`.
4. Press **Begin Investigation** once at least 1 marker is placed.
5. Clues spawn in world space near boundary markers. Tap a clue to unlock it.
6. Press **Journal** to view story text, unlocked clues, and progress.

## Clue Prefabs & Auto-Binding
- **Folder:** `Assets/Prefabs/Clues/`
- **Naming convention:** Prefab name must start with the clue ID, e.g. `C01_cut_lanyard`.
- **Library asset:** `Assets/Resources/CluePrefabLibrary.asset`
- **Auto-populate:** Use the menu `MaskedMurderer/Refresh Prefab Library` to rescan and update the library.

## Adding / Replacing Clue Prefabs
1. Create or replace a prefab in `Assets/Prefabs/Clues/`.
2. Ensure the prefab name starts with the clue ID (e.g. `C05_power_spire`).
3. In Unity, run **MaskedMurderer → Refresh Prefab Library**.
4. Verify the clue appears in the journal and spawns after **Begin Investigation**.

## Story & Case Data
- **Backstory text:** `story.txt` (copied into `Assets/StreamingAssets/story.txt`).
- **Case data:** `case.json` (copied into `Assets/StreamingAssets/case.json`).
- The runtime loader reads `StreamingAssets` first; keep those files in sync.

## Notes
- Boundary placement and anchors are untouched; clue spawning is additive and world-space only.
- The safe spawn rule allows multiple clues per boundary marker when markers are fewer than clues.
