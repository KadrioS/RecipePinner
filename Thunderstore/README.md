# 📌 Recipe Pinner

**Stop running back and forth between chests just to check how much Iron you need!**

**Recipe Pinner** is a quality-of-life mod that lets you "pin" any crafting recipe to your HUD. It tracks materials in your inventory (and nearby chests!) in real-time, helping you focus on crafting, not memorizing numbers.

---

## 🌟 Key Features

* **📍 Pin & Forget:** Hover over any recipe (Crafting Table, Cauldron) or **Construction Piece (Hammer)** and press **Middle Mouse** to pin it.
    * *New:* Now fully supports auto-unpinning when building structures with the Hammer!
* **📄 Pagination System:** Pinned too many recipes? No problem! The list automatically splits into pages to keep your screen clean.
    * Includes stylish diamond-shaped indicators to show which page you are on.
* **📦 Smart Chest Scanner:** Automatically counts items in nearby chests.
    * <span style="color:green">**Green Text:**</span> You have enough in your Inventory.
    * <span style="color:yellow">**Yellow Text:**</span> You have enough combined (Inventory + Nearby Chests). (If ChestScanner enable)
    * <span style="color:red">**Red Text:**</span> Missing materials. Time to farm!
* **🎨 Fully Customizable:** Change colors, font sizes, positions, opacity, and **pins per page** via config.
* **🧩 Multiple Layouts:** Auto-detects MyLittleUI mod or lets you choose your style.

---

## 🖼️ Visuals & Layouts

The mod supports 3 different layout modes to fit your screen perfectly.

### 1. Vertical Mode (Standard)
*Placed under the minimap. Good for vanilla UI.*
![Vertical Layout Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/Images/Vertical.png?raw=true)

### 2. Horizontal Mode (Map Side)
*Perfect if you use **MyLittleUI**. Places pins to the left of the map.*
![Horizontal Layout Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/Images/Horizontal.png?raw=true)

### 3. Horizontal Mode (Bottom Right)
*Keeps the top screen clean. Places pins near your ammo/hotbar.*
![Bottom Right Horizontal Screenshot](https://github.com/KadrioS/RecipePinner/blob/main/Images/BottomRightHorizontal.png?raw=true)

If you don't like these 3 layouts, you can set your own layout using [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/).

---

## ⚙️ Configuration

I strongly recommend using [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/) to edit settings in-game (Press F1).

You can tweak:
* **Colors** (Hex & RGBA)
* **UI Position** (X, Y coordinates) & Scales
* **Pagination Settings** (Pins per Page, Dot Size, Spacing)
* **Fonts** and **Opacity**

![Config Menu](https://github.com/KadrioS/RecipePinner/blob/main/Images/ConfigurationManager.png?raw=true)

---

## 🔧 Installation

1.  Download and install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2.  Extract the `RecipePinner.dll` and `RecipePinner_languages` folder into `Valheim/BepInEx/plugins/`.
3.  Done!

**⚠️ IMPORTANT:** Ensure the `RecipePinner_languages` folder is next to the `.dll` file for translations to work.

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

---

## 🌍 Supported Languages

Auto-detected based on your game language.

🇺🇸 English, 🇹🇷 Turkish, 🇩🇪 German, 🇷🇺 Russian, 🇪🇸 Spanish, 🇫🇷 French, 🇧🇷 Portuguese, 🇵🇱 Polish, 🇨🇳 Chinese, 🇯🇵 Japanese, 🇰🇷 Korean, 🇮🇹 Italian, 🇺🇦 Ukrainian.

*Missing a language? You can easily add your language!*

## 🌍 How to Add Your Language

1.  Open your Valheim folder and navigate to:
    `BepInEx/plugins/RecipePinner/RecipePinner_languages/`
2.  Create a new text file and name it `YourLanguage.json` (e.g., `Italian.json`).
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
      "no_upgrade_cost": "No upgrade cost found"
    }
    ```
4.  Translate the sentences on the right side.
    * *Important: Do not change the `{0}x` part, as it shows the numbers!*
5.  Save the file.
6.  **To use it:** Open the mod settings (F1) or config file, and set **LanguageOverride** to your file name (e.g., `Italian`).

---

## Mirror

[NexusMods](https://www.nexusmods.com/valheim/mods/3195)

---

## 📞 Support & Feedback

Found a bug? Have a feature request?
Ping me on Discord: **kadrio** or create an Issue on GitHub.

**Enjoy crafting!** 🛠️