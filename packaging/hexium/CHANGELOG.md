### 1.3.0

#### Added

* Added **Pin Groups**: select multiple pins and combine them into a named group with a merged resource list and a stacked-cards group icon.
* Added a new **My Pins** management panel, accessible from the inventory screen, for viewing, selecting, grouping, disbanding, and removing pins.
* Added expandable sub-item views in the My Pins panel for managing individual group members.
* Added per-group member claim counts, allowing recipes to belong to multiple groups or remain partially available as individual pins.
* Added persistent group/order support: Pin Groups, group member counts, and pin/group ordering are now saved and loaded alongside existing pinned recipe data.
* Added Compact Group Layout (`GroupCompactThreshold`): groups with many unique materials automatically switch to a compact grid layout to save HUD space. The threshold applies immediately without restarting, and the grid's column count is derived from your configured pin width.
* Added `GroupCompactMaxRows`: controls how many grid rows a compact group pin shows before the remaining materials collapse into a `+N` cell. Changes apply immediately without restarting.
* Added a full-list mode for lone groups: a group pinned on its own drops the `+N` cell and shows every material, turning it into a permanent gathering list.
* Added confirmation and group-naming dialogs with input locking so game controls do not interfere while typing.
* Added a Controls Info panel, accessible via the info button in the My Pins panel, showing keybindings and usage instructions.
* Added configurable Unpin modifier (`HotkeyUnpin`): hold it while pressing the Pin hotkey over a recipe or build piece to decrease/remove that pin.
* Added a dedicated building auto-unpin setting (`AutoUnpinAfterBuilding`), separate from crafting auto-unpin behavior.
* Added configurable column count for the Gathering List grid, with automatic panel resizing in horizontal layouts.
* Added new HUD icon size settings for recipe, material, and group icons.
* Added new My Pins panel/button configuration options.
* Added missing language files to the `RecipePinner_languages` folder so every Valheim-supported language now has a translation file.

  * Note: Some translations may be imperfect because they were AI-assisted. Please report any incorrect translations.

#### Changed

* Building-piece auto-unpin is now controlled by its own `AutoUnpinAfterBuilding` setting instead of sharing the crafting auto-unpin setting.
* Improved building auto-unpin handling for placed building pieces.
* Improved recipe/build hotkey handling so invalid contexts no longer trigger unintended pin/unpin behavior.
* Improved save/load reliability for pinned recipes and pin order.
* The clear-all hotkey now asks for confirmation instead of wiping every pin on a single press: press it once to arm, then again within two seconds.
* Improved Gathering List positioning and layout behavior in horizontal HUD layouts.
* Improved HUD/Gathering List behavior when pins are hidden or when the Gathering List is shown by itself.
* Improved MyLittleUI AutoDetect compatibility, including better vertical placement when MyLittleUI status lists are disabled.
* Improved MyLittleUI config parsing so only exact `Enable = true/false` values in the relevant sections are read.
* Improved localization parsing, including support for common escaped characters and different line endings.
* Reorganized config categories for clearer ordering and grouping.

  * Note: Because config category names changed, some users may need to review or reapply existing config values.

#### Fixed

* Fixed recipes with multiple output variants (for example Bronze 1x and Bronze 5x) always pinning the first variant. The hovered row is now matched exactly instead of by display name, so the 5x recipe pins its own 5x material cost. This also affects mods that add 1x/5x crafting options.
* Fixed the mod overwriting your pin save file with empty data while it was disabled. With `EnableMod = false`, two consecutive saves could permanently erase every pinned recipe.
* Fixed cleared pins not being saved immediately after using the clear-all hotkey.
* Fixed auto-unpin results being lost when the game closed without a clean exit.
* Fixed pins loading without validation after switching characters within the same session.
* Fixed pin saves to use an atomic temp-file replace with a `.bak` backup instead of writing directly to the final save file.
* Fixed duplicate save entries producing duplicate pin order and UI entries.
* Fixed impossible edited-save upgrade pins such as `★999` being accepted.
* Fixed `MaximumPins` reductions not trimming, rebuilding, and paginating pins consistently.
* Fixed pagination/page-size behavior when `MaximumPins < PinsPerPage`.
* Fixed Recipe Pinner hotkeys firing behind non-crafting inventory panels such as Skills, Trophies, and Compendium.
* Fixed the unpin combination acting or logging outside valid recipe and build contexts.
* Fixed possible crashes, null-reference edge cases, or missing display data when recipes, build pieces, modded resources, Gathering List slots, pin slots, or resource slot UI components contain incomplete data.
* Fixed `RefreshRecipeCache` forcing the HUD visible for a frame even when the overlay was hidden.
* Fixed HUD visibility becoming inconsistent after clearing all pins while the overlay was hidden.
* Fixed player death/null-player UI handling so pinned HUD overlays hide together with the rest of the game HUD.
* Fixed shutdown save-path warnings when the mod is destroyed after the player name has already been cleared.
* Fixed Gathering List positioning when HUD pins are hidden: pagination spacing no longer leaves an empty offset, so the Gathering List can move into the first pin position correctly.
* Fixed an issue in horizontal layout mode where the Gathering List retained an incorrect offset and gap because anchors were not reset correctly in gathering-list-only mode.
* Fixed Gathering List placement in MyLittleUI vertical AutoDetect mode when its status lists are disabled.
* Fixed Chest Scan runtime toggling so enabling or disabling it through a mod manager takes effect without restarting the game.
* Fixed stale container references not being cleared when Chest Scan is disabled.
* Fixed container cleanup so destroyed containers are removed from tracking more reliably.
* Fixed Chest Scan re-enable behavior so active containers are scanned again after toggling the feature back on.
* Fixed inventory item count recalculation occurring every frame during Chest Scan; it is now only evaluated when a container scan trigger condition is met.
* Fixed the `LanguageOverride` config value flowing into a file path without sanitizing.

### 1.2.5

* Updated the changelog structure for Hexium's version history.
* Updated the internal plugin version to 1.2.5.
* No gameplay, feature, or functional changes were made compared to version 1.2.4.

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