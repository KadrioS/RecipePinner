# Recipe Pinner

### Your crafting companion for Valheim

[![Version](https://img.shields.io/badge/version-1.3.0-brightgreen?style=for-the-badge)](https://thunderstore.io/c/valheim/p/KadrioS/RecipePinner/)
[![Thunderstore](https://img.shields.io/badge/Thunderstore-Download-blue?style=for-the-badge)](https://thunderstore.io/c/valheim/p/KadrioS/RecipePinner/)
[![NexusMods](https://img.shields.io/badge/NexusMods-Download-orange?style=for-the-badge)](https://www.nexusmods.com/valheim/mods/3195)

**Pin recipes. Track materials. Group projects. Craft smarter.**

Stop running back and forth between chests just to remember how much Iron, Wood, Resin, or Black Metal you still need.

---

## 📌 What Is Recipe Pinner?

Recipe Pinner is a quality-of-life mod for Valheim that lets you pin crafting recipes and building pieces directly to your HUD.

It tracks required materials in real time, shows what you already have, can include nearby chest contents, and gives you a clean overview of everything you are trying to craft or build.

With Recipe Pinner, you can:

* Pin crafting recipes and Hammer building pieces to your HUD.
* Track required materials from your inventory.
* Optionally include nearby chest contents with Chest Scanner.
* See a combined Gathering List for all pinned recipes.
* Group multiple pins into named projects.
* Manage all pins from the new My Pins panel.
* Automatically unpin recipes after crafting or building.
* Customize layout, colors, icon sizes, hotkeys, and more.

Less menu checking. Less guessing. More building, crafting, and exploring.

---

## 🔄 Updating From Older Versions

If you are updating from an older version, especially 1.2.x, please review your config after launching the game.

Version 1.3.0 reorganizes config categories to make settings easier to understand. Because the category names changed, some existing config values may need to be re-applied.

Recommended:

* Launch the game once after updating.
* Open the config with Configuration Manager.
* Review layout, hotkeys, Chest Scanner, and Gathering List settings.
* Make sure the `RecipePinner_languages` folder is next to `RecipePinner.dll`.

Your existing pinned recipe data is still supported, and 1.3.0 adds new saved data for groups and ordering.

---

## ⭐ Key Features

### 📍 Pin Recipes And Building Pieces

Hover over a crafting recipe or Hammer build piece and press the Pin hotkey.

By default:

* `Mouse Wheel Click` pins or increases count.
* `Left Shift + Mouse Wheel Click` decreases or removes a pin.

The unpin modifier is now configurable with `HotkeyUnpin`.

### 🧩 My Pins And Pin Groups

Version 1.3.0 adds a new **My Pins** panel and **Pin Groups**.

Use them to view active pins, remove pins, select multiple pins, create named groups, expand groups, adjust member counts, and disband groups back into individual pins.

Groups are ideal for armor sets, building projects, portal kits, food prep, and other multi-recipe goals. A group appears as one HUD pin with a merged material list and a stacked-cards group icon.

Duplicate pins can also be split between groups or kept as individual pins, so one recipe can belong to more than one project.

When a group needs many different materials, its HUD pin switches to a compact grid to save space. If the list is still too long, the remaining materials collapse into a `+N` cell. A group pinned on its own always shows every material, which turns it into a permanent gathering list. Both limits are configurable.

### 📋 Gathering List And Chest Scanner

The Gathering List shows the combined total of all materials required by your active pins.

Chest Scanner can include nearby chest contents in material counts.

Color meaning:

* Green: enough materials in your inventory.
* Yellow: enough materials when nearby chests are included.
* Red: missing materials.

Default Gathering List key: `F8`

Chest Scanner is disabled by default and only reads nearby chest contents. It does not move items.

### 🖥️ Smart HUD Layout

Each pin can show a colored accent bar so you can quickly tell whether the recipe is ready to craft.

Recipe Pinner can reposition pins depending on what you are doing.

* Normal HUD layout while exploring.
* Bottom-right layout while inventory or containers are open.
* Horizontal layout support for MyLittleUI users.
* Better placement while sailing.
* Automatic pages when you pin more recipes than fit on one HUD page.

### 💾 Safer Saves And Ordering

Pinned recipes, Pin Groups, group member counts, and pin/group order are saved together. Version 1.3.0 also improves save reliability with safer temp-file replacement and backup behavior.

### 🌍 Languages And Customization

Recipe Pinner includes language files for every Valheim-supported language. Some translations were AI-assisted, so please report incorrect or awkward translations.

Almost everything is configurable: hotkeys, layout mode and position, HUD scale, fonts, colors, background opacity, recipe/material/group icon sizes, pin limit and pins per page, compact group behavior, Gathering List columns, My Pins panel size and position, Chest Scanner range and interval, and whether recipes auto-unpin after crafting or building.

---

## 🕹️ How To Use

### Pin Something

1. Open a crafting station or Hammer build menu.
2. Hover over a recipe or building piece.
3. Press `Mouse Wheel Click`.

The recipe appears on your HUD with its required materials.

### Add More Of The Same Recipe

Press the Pin hotkey again on the same recipe.

Recipe Pinner increases the count and updates the required materials.

### Decrease Or Remove A Pin

Hold `Left Shift` and press `Mouse Wheel Click` over the recipe or building piece.

You can change `Left Shift` in the config with `HotkeyUnpin`.

### Create A Group

1. Open your inventory.
2. Open the My Pins panel.
3. Click **Group**.
4. Select at least two pins.
5. Confirm and enter a group name.

The selected pins become one grouped project with a merged material list.

### Manage A Group

In the My Pins panel, expand a group to view its members.

You can adjust member counts, remove members, delete the group, or disband it back into individual pins.

---

## 🎮 Controls

Default controls are configurable.

| Action | Default |
| :--- | :--- |
| Pin recipe / add count | `Mouse Wheel Click` |
| Decrease / remove pin | `Left Shift` + `Mouse Wheel Click` |
| Toggle HUD overlay | `F7` |
| Toggle Gathering List | `F8` |
| Cycle HUD pages | `Left Alt` |
| Clear all pins | `P` (press twice) |
| Open My Pins panel | Inventory screen button |

Clearing all pins asks for confirmation: press the key once to arm it, then again within two seconds.

The My Pins panel also includes an info button that shows current keybindings in-game.

---

## 🖼️ Layouts And Screenshots

<details>
<summary><b>Click to expand screenshots</b></summary>

### Vertical Mode

Placed under the minimap. Good for vanilla UI. This is what the default Auto-Detect mode uses when MyLittleUI is not installed.

![Vertical Layout Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Vertical.jpg?raw=true)

### Horizontal Mode

Useful for MyLittleUI users. Places pins near the map side. When the layout setting is left on Auto-Detect and MyLittleUI is installed, this layout is triggered automatically.

![Horizontal Layout Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Horizontal.jpg?raw=true)

### Bottom Right Horizontal Mode

Keeps the top of the screen cleaner and works well when inventory or containers are open.

![Bottom Right Horizontal Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/BottomRightHorizontal.jpg?raw=true)

### My Pins Panel

Manage, remove, group, disband, and inspect your active pins from the inventory screen.

![My Pins Panel](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/MyPinsPanel.jpg?raw=true)

### Pin Groups

Group multiple recipes into one named project with a merged material list.

![Pin Groups](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/PinGroups.jpg?raw=true)

### Expanded Group Members

Expand a group in My Pins to manage individual members and group claim counts.

![Expanded Group Members](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/GroupMembers.jpg?raw=true)

### Compact Group Layout

When a group needs many different materials, its HUD pin switches to a compact grid to save space.

![Compact Group Layout](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/GroupCompactPin.jpg?raw=true)

### Overflow Cell

If a compact group is still too long, the remaining materials collapse into a single `+N` cell.

![Group Overflow Cell](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/GroupPinN.jpg?raw=true)

### Controls Info Panel

View current keybindings and basic usage instructions from inside the My Pins panel.

![Controls Info Panel](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/ControlsInfo.jpg?raw=true)

### MyLittleUI Compatibility

Recipe Pinner can automatically adjust its layout when MyLittleUI is installed.

![Using MyLittleUI](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/MyLittleUI.jpg?raw=true)

### Inventory Open

When inventory is open, pins can move to a cleaner bottom-right layout.

![When Inventory Open](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Inventory.jpg?raw=true)

### Gathering List

The Gathering List shows combined material requirements across all pins.

![Gathering List](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/GatheringList.jpg?raw=true)

### Chest-Side Gathering List

The Gathering List snaps next to an open chest, so you can see what you still need while moving items around.

![Chest-Side Gathering List](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Chest.jpg?raw=true)

### Chest Scanner

When Chest Scanner is enabled, nearby chest materials can be included in the displayed counts.

![Chest Scanner](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/ChestScanner.jpg?raw=true)

### HUD Pages

When you pin more recipes than fit at once, they split into pages. Cycle through them with the page hotkey.

![HUD Pages](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Pages.jpg?raw=true)

### Sailing

Recipe Pinner can move pins while sailing to reduce HUD overlap.

![When Sailing](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Sailing.jpg?raw=true)

</details>

---

## ⚙️ Configuration

Using Configuration Manager is strongly recommended.

Recommended config manager:

[Official BepInEx ConfigurationManager](https://valheim.hexium.gg/mods/Azumatt/Official_BepInEx_ConfigurationManager)

Press `F1` in-game to edit settings.

![Config Menu](https://github.com/KadrioS/RecipePinner/blob/main/assets/images/ConfigurationManager.jpg?raw=true)

---

## 🔧 Installation

### Mod Manager

Install with Thunderstore Mod Manager or r2modman.

This is the easiest option and handles dependencies automatically.

### Manual Installation

1. Install [BepInExPack Valheim](https://valheim.hexium.gg/mods/denikson/BepInExPack_Valheim).
2. Download Recipe Pinner.
3. Place `RecipePinner.dll` inside your `Valheim/BepInEx/plugins/RecipePinner/` folder.
4. Place the `RecipePinner_languages` folder next to `RecipePinner.dll`.
5. Launch the game.

Your folder should look like this:

```text
BepInEx/
└── plugins/
    └── RecipePinner/
        ├── RecipePinner.dll
        └── RecipePinner_languages/
            ├── English.json
            ├── Turkish.json
            └── ...
```

Important: `RecipePinner_languages` must stay next to `RecipePinner.dll`.

---

## 🌍 Languages

Recipe Pinner auto-detects your Valheim language.

You can also force a language with the `LanguageOverride` config option.

Version 1.3.0 includes language files for every Valheim-supported language.

Note: Some translations may be imperfect because they were AI-assisted. If you find a bad translation, please report it.

You can also fix one yourself: open the matching `.json` file in `BepInEx/plugins/RecipePinner/RecipePinner_languages/`, edit the text on the right side of each line, and save. Leave placeholders like `{0}` untouched, since they are replaced with numbers and key names at runtime.

---

## ❓ FAQ

<details>
<summary><b>Does Recipe Pinner craft items automatically?</b></summary>

No. Recipe Pinner only tracks recipes and materials. You still craft and build normally.

</details>

<details>
<summary><b>Does Chest Scanner move items from chests?</b></summary>

No. It only reads nearby chest contents and includes them in the displayed material counts.

</details>

<details>
<summary><b>Is Chest Scanner enabled by default?</b></summary>

No. Chest Scanner is disabled by default. You can enable it from the config if you want nearby chest contents to count toward requirements.

</details>

<details>
<summary><b>Can I pin building pieces?</b></summary>

Yes. Hammer build pieces can be pinned like crafting recipes.

</details>

<details>
<summary><b>What happens if I pin the same recipe again?</b></summary>

The pin count increases, and the displayed material requirements update for the new count.

</details>

<details>
<summary><b>How do I remove or decrease a pin?</b></summary>

By default, hold `Left Shift` and press `Mouse Wheel Click` over the recipe or build piece. You can change the modifier with `HotkeyUnpin`.

</details>

<details>
<summary><b>How do I open My Pins?</b></summary>

Open your inventory and use the My Pins button. From there, you can view, remove, group, disband, and manage pins.

</details>

<details>
<summary><b>Can I group pins?</b></summary>

Yes. Starting with 1.3.0, you can create named Pin Groups from the My Pins panel.

</details>

<details>
<summary><b>Can a recipe belong to more than one group?</b></summary>

Yes. Recipe Pinner supports per-group member claim counts, so duplicate pins can be split between groups or kept as individual pins.

</details>

<details>
<summary><b>Can I remove a group without losing the recipes?</b></summary>

Yes. Use Disband to turn group members back into individual pins.

</details>

<details>
<summary><b>Why does my group pin show "+N" instead of all materials?</b></summary>

Groups with many different materials switch to a compact grid, and that grid is capped so one large group cannot stretch every pin next to it. The materials that do not fit are collapsed into a `+N` cell.

You can raise the row cap with `GroupCompactMaxRows`, or change when compact mode starts with `GroupCompactThreshold`. Both apply immediately. A group pinned on its own never shows `+N`.

</details>

<details>
<summary><b>Are pins shared between my characters?</b></summary>

No. Each character has its own pins, saved separately. Switching characters loads that character's own list.

</details>

<details>
<summary><b>Does Recipe Pinner work with MyLittleUI?</b></summary>

Yes. AutoDetect can adjust the HUD layout when MyLittleUI is installed to reduce overlap with other UI elements.

</details>

<details>
<summary><b>Can I change the controls?</b></summary>

Yes. Hotkeys can be changed from the config. Using Configuration Manager is recommended.

</details>

<details>
<summary><b>Why did some config settings reset after updating?</b></summary>

Version 1.3.0 reorganized config categories. If a setting moved to a new category, you may need to review or reapply it.

</details>

<details>
<summary><b>Why are some translations imperfect?</b></summary>

Version 1.3.0 includes language files for every Valheim-supported language, but some translations were AI-assisted. Please report incorrect or awkward translations.

</details>

---

## 🧭 Future Ideas

These are features I may consider for future updates:

* **Group readiness bar:** split a group's accent bar into one segment per member, so a single glance tells you which recipes in the project are already craftable. The third recipe in the group lights up the third segment.
* **Chest Scanner support in the readiness bar:** turn the accent bar yellow when a recipe is craftable only with nearby chest contents, matching how material counts already behave.
* **Pinned recipe highlight:** draw a border around pinned recipes inside the crafting panel so you can see what is already pinned at a glance. This one may not be feasible.
* **Hold-to-pin quantity preview:** hold the Pin hotkey to see how many copies you are about to pin before releasing.
* **Pin keybind hint:** show the Pin control in the crafting menu's hint bar, next to Move and Use, so new players can discover the pin key without opening the config.
* Material gathered notification.
* Auto-sorting craftable pins to the top.
* Visual chest highlighting for required materials.
* More compatibility improvements with other UI and crafting mods.
* VNEI compatibility.

---

## 🤖 AI-Assisted Development

Starting with version 1.3.0, I want to be transparent that Recipe Pinner was developed with significant AI assistance. In practice, I think **AI-Assisted** describes the situation more accurately than **AI-Generated**.

Earlier versions also used AI assistance, but in a much more limited way: mainly to help locate the relevant parts of the Valheim/BepInEx APIs and referenced Valheim game DLLs, understand which classes or methods were useful for the mod, investigate possible bugs, and reason about edge cases.

Version 1.3.0 was different. I originally tried to build the new systems without heavy AI help, but I could not get them to a release-ready state on my own. In the Work in Progress section, I had said the update might be released within about a month; it is now close to three months, and this is the main reason it took longer. Development started well, but as My Pins, Pin Groups, expanded group management, save/order handling, and release-blocker fixes grew more complex, I started getting stuck and chose to rely on AI assistance much more than before.

That does not mean the mod was released without human review. The final decisions, testing, in-game validation, release judgment, and responsibility for the mod are still mine. I reviewed the changes, tested the behavior in Valheim, and validated the release manually before publishing.

Thank you for using Recipe Pinner.

---

## ☕ Support My Work

If Recipe Pinner makes your Valheim life easier, you can support development here:

[![Buy Me A Coffee](https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png)](https://buymeacoffee.com/kadrio)

<br>

<img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/BMC_QRCode.png?raw=true" alt="Buy Me A Coffee QR" width="150">

---

## 🔗 Mirrors

* [Thunderstore](https://thunderstore.io/c/valheim/p/KadrioS/RecipePinner/)
* [NexusMods](https://www.nexusmods.com/valheim/mods/3195)

---

## 💬 Support And Feedback

Found a bug, translation issue, or compatibility problem?

Contact me on Discord:

**kadrio**

Or create an issue on GitHub:

[GitHub Issues](https://github.com/KadrioS/RecipePinner/issues)

---

**Pin it. Group it. Gather it. Build it.**

See you in Valheim.
