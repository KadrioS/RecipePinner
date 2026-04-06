# 📌 Recipe Pinner

**Stop running back and forth between chests just to check how much Iron you need!**

**Recipe Pinner** is a quality-of-life mod that lets you "pin" any crafting recipe to your HUD. It tracks materials in your inventory (and nearby chests!) in real-time, helping you focus on crafting, not memorizing numbers.

⚠️ **IMPORTANT FOR v1.2.0 UPDATE:** The configuration structure has been heavily overhauled. **Please delete your old `com.Kadrio.RecipePinner.cfg` file** before launching the game to let a fresh config generate! 

---

## 🌟 Key Features

* **🛍️ Master Gathering List (*NEW*):** Press `F8` to view a combined total of all required materials across ALL your pinned recipes! Auto-opens when you pin multiple items and smartly aligns next to open chests for effortless material gathering.
* **🚦 Craft Readiness Indicator (*NEW*):** A sleek color-coded accent bar next to each pin tells you instantly if you can craft it (Green = Ready, Red = Missing materials).
* **📍 Pin & Forget:** Hover over any recipe (Crafting Table, Cauldron) or **Construction Piece (Hammer)** and press `Middle Mouse` to pin it. Automatically unpins when you craft or build the item!
* **📦 Smart Chest Scanner:** Automatically counts items in nearby chests. (You must enable it in the configuration to use it.)
    * <span style="color:green">**Green Text:**</span> You have enough in your Inventory.
    * <span style="color:yellow">**Yellow Text:**</span> You have enough combined (Inventory + Nearby Chests).
    * <span style="color:red">**Red Text:**</span> Missing materials. Time to farm!
* **🔄 Dynamic UI Repositioning:** Pins intelligently slide to the bottom right when you open your inventory or a chest, keeping your screen clutter-free.
* **📄 Pagination System:** Pinned too many recipes? No problem! The list automatically splits into pages with stylish diamond-shaped indicators. You can navigate between pages by pressing the `ALT` key.
* **🎨 Fully Customizable:** Change colors, font sizes, positions, opacity, and **pins per page** via config.

---

<details>
<summary><b>🖼️ Mod Photos & Layouts (Click to Expand)</b></summary>
<br>

The mod supports 4 different layout modes to fit your screen perfectly. *(Note: UI automatically shifts to Bottom Right when inventory/chests are opened!)*

### 1. Auto-Detect Mode
*This is the default setting. If you are using the MyLittleUI mod, this setting automatically switches the pins to Horizontal Mode; if you are not using it, it switches them to Vertical Mode.*

### 2. Vertical Mode (Standard)
*Placed under the minimap. Good for vanilla UI.*
![Vertical Layout Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/Images/Vertical.png?raw=true)

### 3. Horizontal Mode (Map Side)
*Perfect if you use **MyLittleUI**. Places pins to the left of the map.*
![Horizontal Layout Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/Images/Horizontal.png?raw=true)

### 4. Horizontal Mode (Bottom Right)
*Keeps the top screen clean. Places pins near your ammo/hotbar.*
![Bottom Right Horizontal Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/Images/BottomRightHorizontal.png?raw=true)

**If you don't like these 3 layouts, you can set your own layout using Configuration Manager.**

### Using MyLittleUI
*If you're using the MyLittleUI mod, it automatically switches to Horizontal Mode to prevent your pins from overlapping with the weather panel and effects*
![Using MyLittleUI](https://github.com/KadrioS/RecipePinner/blob/main/Images/MyLittleUI.png?raw=true)

### When Inventory Open
*If you open the inventory, the pins automatically switch to Bottom Right Horizontal mode. This way, you can still see your pins while the inventory is open. When you close the inventory, they return to their original state.*
![When Inventory Open](https://github.com/KadrioS/RecipePinner/blob/main/Images/Inventory.png?raw=true)

### Gathering List (When Chest Open)
*If you pin multiple items, this is a list showing the total required materials. When the chest is opened, it appears next to the chest panel. It can be closed using a button.*
![Gathering List](https://github.com/KadrioS/RecipePinner/blob/main/Images/GatheringList.png?raw=true)

### Chest Scanner
*If you enable the Chest Scanner setting in the config, it will scan nearby chests. If you don’t have enough items in your inventory but the chest contains enough, the numbers will turn yellow.*
![Chest Scanner](https://github.com/KadrioS/RecipePinner/blob/main/Images/ChestScanner.png?raw=true)

### When Sailing
*If you're sailing, your pins will automatically switch to Bottom Right Horizontal Mode*
![When Sailing](https://github.com/KadrioS/RecipePinner/blob/main/Images/Sailing.png?raw=true)
</details>

---

## ⚙️ Configuration

I strongly recommend using [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/) to edit settings in-game (Press `F1`).

You can tweak:
* **Colors** (Hex & RGBA for Headers, Missing Mats, Accent Bars)
* **Controls**
* **UI Position** (X, Y coordinates) & Scales
* **Pagination Settings** (Pins per Page, Dot Size, Spacing)
* **Fonts** and **Opacity**
* **Gathering List Automation**

![Config Menu](https://github.com/KadrioS/RecipePinner/blob/main/Images/ConfigurationManager.png?raw=true)

---

## 🔧 Installation

1.  Download and install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2.  Extract the `RecipePinner.dll` and `RecipePinner_languages` folder into `Valheim/BepInEx/plugins/`.
3.  Done!

⚠️ **IMPORTANT:** Ensure the `RecipePinner_languages` folder is next to the `RecipePinner.dll` file for translations to work.

---

## 🎮 Controls

*Default keys (Changeable in Config):*

| Action | Key |
| :--- | :--- |
| **Pin Recipe / Add Count (+1)** | `Middle Mouse Button` |
| **Unpin / Decrease Count (-1)** | `Shift` + `Middle Mouse Button` |
| **Cycle Pages** | `Left Alt` |
| **Clear All Pins** | `P` |
| **Show/Hide Overlay** | `F7` |
| **Toggle Gathering List** | `F8` |

---

## 🌍 Supported Languages

Auto-detected based on your game language.

🇺🇸 English, 🇹🇷 Turkish, 🇩🇪 German, 🇷🇺 Russian, 🇪🇸 Spanish, 🇫🇷 French, 🇧🇷 Portuguese, 🇵🇱 Polish, 🇨🇳 Chinese, 🇯🇵 Japanese, 🇰🇷 Korean, 🇮🇹 Italian, 🇺🇦 Ukrainian.

⚠️ **Note:** If you can't find your language in the app, please let me know so I can add it in the next update; or you can add it yourself:

## 🌍 How to Add Your Language

1.  Open your Valheim folder and navigate to:
    `BepInEx/plugins/RecipePinner/RecipePinner_languages/`
2.  Create a new JSON file and name it `YourLanguage.json` (e.g., `Italian.json`).
3.  Open the file with a text editor (like Notepad) and paste this template:
    ```json
    {
      "pinned": "Recipe Pinned!",
      "unpinned": "Pin Removed",
      "list_full": "List Full!",
      "added_more": "Added More: {0}x",
      "decreased": "Decreased: {0}x",
      "cleared": "Pinned Recipes Cleared",
      "max_level": "Max Level Reached",
      "no_upgrade_cost": "No upgrade cost found",
      "gathering_title": "GATHERING LIST",
      "gathering_opened": "Gathering List Opened",
      "gathering_closed": "Gathering List Closed",
      "gathering_empty": "No Recipes Pinned",
      "gathering_hint": "Open/Close: {0}"
    }
    ```
4.  Translate the sentences on the right side.
    * *Important: Do not change the `{0}x` or `{0}` parts, as they show the numbers and hotkeys!*
5.  Save the file.
6.  **To use it:** Open the mod settings (F1) or config file, and set **LanguageOverride** to your file name (e.g., `Italian`).

---

## 🚀 Work in Progress
I've been working hard in the background on a massive Quality-of-Life update, and it's almost ready to drop! Here is a sneak peek of what's coming very soon:
* 📋 **"My Pins" Management Interface:** A brand-new, Vanilla-friendly UI panel accessible directly from your inventory. It will allow you to view all your active pins, easily adjust quantities (+/-), and manage them all in one place!
* 🧩 **Project Grouping (Group Pins):** Planning a big build or a full armor set? You'll be able to select multiple pins and merge them into a single "Group Pin" with a combined material cost and a dynamic segmented progress bar!

## 🔮 Future Plans & Roadmap
Beyond the upcoming update, here are some other features I’m considering adding:
* 🎉 **Material Gathered Notification:** Get a satisfying center-screen message the exact moment you've collected all the required materials for a pinned recipe.
* 🔄 **Auto-Sort Pins:** A smart quality-of-life improvement where fully craftable (green) pins will automatically move to the top of your pinned list for quick and easy access.
* 📦 **Visual Chest Highlighting:** If the Chest Scanner is enabled, visually highlight or mark the specific chests in your base that contain the pinned materials, so you know exactly which box to open!
* 🤝 **VNEI Compatibility**

---

## Mirror
 
[NexusMods](https://www.nexusmods.com/valheim/mods/3195)

---

## 📞 Support & Feedback

Found a bug? Have a feature request?
Ping me on Discord: **kadrio** or create an Issue on [GitHub](https://github.com/KadrioS/RecipePinner).

**Enjoy crafting!** 🛠️

---