### 1.2.4
- Fixed Gathering List positioning issues across all layout modes when pins are hidden via toggle (F7).
- Fixed Gathering List not repositioning to chest location when pins are hidden and a chest is opened.
- Fixed Gathering List visibility being reset when changing config settings while only the Gathering List is visible.
- Fixed an issue where the Gathering List wouldn't update inventory amounts while pins were hidden (F7 mode).
- Fixed a potential bug where clearing all pins while a chest is open could cause the Gathering List panel to remain orphaned on the screen.
- Fixed reflection initialization log reporting inaccurate success/fail counts.
- Fixed inconsistent logging in MyLittleUI config reader.
- Added value range (8-40) to font size config entries to prevent invalid values.
- Removed dead code and cleaned up leftover code artifacts.
- Refactored UIManager into partial classes for better code organization.

### 1.2.3
⚠️ **IMPORTANT NOTE FOR EXISTING USERS:** Due to layout improvements, the default position for the Gathering List while a chest is open has changed. Please delete your old .cfg file or manually update the InventoryGatheringListPosition setting to x: -400, y: 320 in the config file properly align it.

**Developer Note:** When I released version 1.2.0, I didn’t have the chance to thoroughly test the mod, and while trying to fix some issues, I ended up breaking other things. If you encounter any bugs, please don’t hesitate to report them via Discord (kadrio) or GitHub. Thank you for your patience!
- Fixed Gathering List incorrectly multiplying material amounts when the same recipe was pinned multiple times.
- Fixed Gathering List opening empty on the first pin instead of waiting for 2+ pins.
- Fixed Gathering List not clearing its data when all pins were removed.
- Fixed an issue where the Gathering List's position while a chest is open.
- Please report bugs. 

### 1.2.2
- Optimized memory usage and eliminated unnecessary log outputs by ensuring the game's containers are no longer tracked in the background when the Chest Scanner feature is disabled.
- A new “Work in Progress” section and new features for “Future Plans” have been added to the README file.
- No need to delete config file.

### 1.2.1
- README file fixed only. No need to delete config file.

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

**<details><summary>OLD VERSIONS</summary>**
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
</details>