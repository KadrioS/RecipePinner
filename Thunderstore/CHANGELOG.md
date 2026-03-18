### 1.2.0
- ⚠️ IMPORTANT: The configuration structure has been heavily overhauled. Please delete your old com.Kadrio.RecipePinner.cfg file before launching the game!
- 🌍 LOCALIZATION UPDATE: Added new translation keys for the Gathering List (`gathering_title`, `gathering_opened`, `gathering_closed`, `gathering_empty`, `gathering_hint`). The 13 default languages are already updated, but if you use a custom language file, please update it!
- Added a new 'Gathering List' panel (Toggle: F8) that aggregates total required materials across all pinned recipes.
- Added automatic visibility logic for the Gathering List (auto-opens when 2+ recipes are pinned, auto-closes when below 2).
- Added a 'Craft Readiness Indicator' to pins, displaying a colored accent bar (green = ready to craft, red = missing materials).
- Added Auto-Unpin functionality that automatically removes recipes from the screen after crafting or building them.
- Inventory/Chest Awareness: When opening your inventory or a chest, the pins will now automatically temporarily switch to the existing 'Bottom Right Horizontal' layout so they don't block the screen.
- The Gathering List will also reposition itself next to the chest panel for easy comparison.
- Implemented real-time live-updating for the Gathering List while interacting with chest contents.
- Improved UI aesthetics by dynamically aligning all pin heights on each page to match the longest panel, preventing jagged layouts and ensuring a clean, uniform look.
- Optimized overall performance by significantly reducing unnecessary UI rebuilds and per-frame calls.
- Improved general codebase stability through extensive cleanup.

### 1.1.4
- In the 1.1.3 update, I put in an old DLL lol (sorry).

### 1.1.3
- Fixed upgrade recipes (★3, ★4) showing incorrect material amounts — now correctly calculates costs based on upgrade level.
- Optimized container scanning with a movement cooldown to prevent excessive scans.
- Reduced memory allocations by reusing container snapshot buffers instead of creating new lists each scan.
- Improved dictionary access efficiency across TogglePin, AutoUnpinHook, and AutoUnpinBuildHook using TryGetValue pattern.
- Removed unused legacy PinnedRecipe.cs file.

### 1.1.2
- Added full support for tracking Item Upgrades (pinning directly from the Upgrade tab).
- Added smart auto-unpin logic to correctly distinguish between crafting new items and upgrading existing ones.
- Added new localization keys (max_level, no_upgrade_cost) for upgrade notifications.
- Optimized internal logic with reflection caching to improve performance and reduce overhead.
- Note: Users with custom language files must update them to include new keys to avoid missing text.

### 1.1.1
- Updated installation instructions in README. No code changes.

### 1.1.0
- Added pagination system to handle large numbers of pins (configurable 'PinsPerPage').
- Added visual pagination indicators (diamond dots) with configurable size, spacing, and opacity.
- Added 'Cycle Page' hotkey (Default: LeftAlt) to switch between pages.
- Fixed critical issue where auto-unpinning did not work for construction/hammer placement (added Player.PlacePiece hook).
- Fixed 'Save path invalid' and 'ObjectDB null' warnings in logs during startup/shutdown.
- Optimized UI rendering with dirty-check mechanism to improve performance.
- Updated configuration handling.

### 1.0.2
- Fixed an issue where using the Middle Mouse Button to remove/deconstruct build pieces would accidentally pin the recipe. Pinning is now restricted to hovering over HUD icons only.

### 1.0.1
- Fixed README images

### 1.0.0
- Initial Release