<h1 align="center">Recipe Pinner</h1>

<h3 align="center">Your crafting companion for Valheim</h3>

<p align="center">
  <img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/hero.gif?raw=true" alt="Recipe Pinner tracking materials on the HUD">
  <br>
  <sub>Shown with a larger HUD and a solid background, both of which are settings. By default the pins are smaller and half-transparent.</sub>
</p>

**Valheim only tells you what you are missing while you are standing at the bench. Recipe Pinner tells you in the forest.**

Pin any recipe or build piece to your HUD and what it still needs stays in front of you — counting down as you gather, turning green the moment you can craft it. Group a whole project into a single pin, and let it count the chests around you too.

> **Built for Valheim 1.0.** This build cannot run on 0.221.x. If you are still on an older game version, stay on Recipe Pinner 1.3.2.

---

## ⭐ What It Does

### 📍 Pin it, then watch it fill up

Hover a crafting recipe or a Hammer build piece, press the Pin key, and it lands on your HUD with everything it needs. From then on the numbers follow you: they climb as you pick things up, and the row turns green the moment that material is covered. Nothing to open, nothing to check.

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Vertical.png?raw=true" alt="The HUD pin list"></p>

Press the key again on the same recipe to pin a second copy, a third, a tenth — every total scales with it.

Pin something more than once and each row also shows what **one** of them costs, in brackets. Ten stone axes need fifty wood, so with eight in your bag the row reads:

> `8/50(5)` — **8** you have · **50** for all ten · **(5)** for one

The total stays red because fifty is far off, while `(5)` turns green the moment you can afford a single axe. That is the difference between "I can't do this yet" and "I can make one right now", and you get it without doing arithmetic.

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/SingleUnit.png?raw=true" alt="A recipe pinned ten times, showing what one of them costs"></p>

Your Pin and Unpin keys appear in Valheim's own button-hint bar, next to Move and Use, and follow your keys if you rebind them — so the control is discoverable without opening a config file. The Unpin hint stays hidden until there is actually something it can remove.

Upgrades can be pinned the same way, and the pin shows what that next level actually costs. Pin one at the **Forge of Potential** and it asks for the Idol the Forge charges instead of the ordinary materials — including the levels past an item's usual cap that only the Forge can reach.

### 🧩 Turn a whole project into one pin

An armour set is not one recipe. Pin each piece separately and your HUD fills up while the thing you actually want to know — what the whole project costs — is written nowhere.

Select them in the **My Pins** panel and combine them into a named **group**. The group becomes a single HUD pin with one merged material list — the running total for the entire project, in one place.

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/PinGroups.png?raw=true" alt="A group on the HUD"></p>

Expand the group in the **My Pins** panel and every member shows its own materials underneath its name, which answers the question a merged list never can: **which one of these can I make right now?**

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/GroupMembers.png?raw=true" alt="A group expanded in the My Pins panel"></p>

From the panel you can also:

* Change how many copies of a recipe you want, or remove it.
* Adjust an individual member's count inside a group.
* Disband a group back into separate pins, keeping every recipe.
* Put the same recipe in more than one project — duplicate pins can be split between groups.

Groups earn their keep on armour sets, station upgrades, building projects and food prep. A big one switches to a compact grid so it does not swallow the HUD, and whatever still does not fit is folded into a single `+N` cell — N more materials than the pin has room to show. When a group is your only pin, it skips that limit and shows everything, which quietly turns it into a permanent shopping list.

### 📋 Count the chests, not just your pockets

The **Gathering List** adds up everything your pins need and shows one combined total — what to bring back from the trip, rather than what each recipe wants separately. It snaps alongside an open chest, so you can see what you still need while you are moving things around.

Turn on the **Chest Scanner** in the config (`EnableChestScanning`) and the nearby chests count too. Then the colours on your pins tell you three different things:

* 🟢 **Green** — you have it on you.
* 🟡 **Yellow** — you have it, but it is in a chest nearby.
* 🔴 **Red** — you do not have it.

Yellow is the useful one: it is the difference between "go mine more copper" and "walk ten steps".

It is off by default, and it only ever reads — it never moves, takes or touches anything in a chest.

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Chest.png?raw=true" alt="The Gathering List beside an open chest"></p>

### 🖥️ Gets out of the way

Pins move themselves depending on what you are doing: a normal spot while you are exploring, the bottom right when your inventory or a chest is open, moved aside while you are sailing, and clear of Valheim's button-hint bar when that is on. Pin more than fits on one screen and the list pages instead of growing.

If you use **MyLittleUI**, the layout goes horizontal on its own to stay out of its way. If you prefer a different spot entirely, every layout's position is a setting.

Each pin can also carry a coloured bar along its edge — ready or not ready — so the answer is available from the corner of your eye, without reading a single number.

Almost none of this is fixed. The settings cover the hotkeys, the layouts and where they sit, HUD scale, fonts, colours, icon sizes, how many pins fit on a page, when a group goes compact, and whether a recipe unpins itself once you have crafted it — and **nearly every one applies the moment you change it.** Open the config with the HUD on screen and you can watch a slider move the pins.

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Inventory.png?raw=true" alt="Pins repositioned with the inventory open"></p>

### 🌍 Every language Valheim speaks

Recipe Pinner follows the language you play in. There is nothing to set and nothing to download — pick your language in Valheim, and the pins, the panel, the dialogs and the hints all follow.

Some of those translations were AI-assisted, so one may read awkwardly in your language. Please report it if it does — or fix it yourself and keep the fix. The language files sit in a plain folder next to the mod, one line per piece of text, and a text editor is the only tool you need.

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Languages.png?raw=true" alt="The My Pins panel in German"></p>

---

## ✅ Requirements And Compatibility

| | |
| :--- | :--- |
| **Game version** | **Valheim 1.0 or newer** — see the note at the top if you are on an older one. |
| **Needs** | [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) 5.4.2351 or newer, and nothing else. |
| **Install on** | Your own game only. Recipe Pinner draws a HUD and reads your own inventory — it sends nothing over the network, so it works in multiplayer whether or not anyone else has it, and a dedicated server does not need it. |
| **Optional** | [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/), to change every setting in-game. |
| **Other UI mods** | The horizontal HUD layout exists for MyLittleUI users, and the My Pins button can be moved out of another mod's way. If something fights with it, please report it and name the other mod. |

---

## 🔧 Installation

**With a mod manager** — [Thunderstore Mod Manager](https://www.overwolf.com/app/thunderstore-thunderstore_mod_manager), [r2modman](https://thunderstore.io/c/valheim/p/ebkr/r2modman/) or [Gale](https://thunderstore.io/c/valheim/p/Kesomannen/GaleModManager/). Easiest, and it handles the dependency for you.

**By hand** — install [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) first, then put `RecipePinner.dll` and the `RecipePinner_languages` folder together in `Valheim/BepInEx/plugins/RecipePinner/`:

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

`RecipePinner_languages` has to stay next to the DLL.

**Updating** — install the new version over the old one. Your pins, groups and their order load unchanged. Anything worth knowing for the version you are coming from is in [CHANGELOG.md](docs/CHANGELOG.md).

---

## 🕹️ Getting Started

**Pin something.** Open a crafting station or the Hammer's build menu, hover a recipe or a piece, and press `Mouse Wheel Click`. It lands on your HUD with its materials. Press the same key again on the same recipe to pin a second copy; hold `Left Shift` and press it to take one back off.

**Open the panel.** With your inventory open, click the pin icon just left of the crafting panel's **CRAFT** tab — hover it and it reads **MY PINS**. That is everything you have pinned, each row with its own materials, and the controls to change a count or remove a pin.

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/MyPinsButton.png?raw=true" alt="The My Pins button, beside the CRAFT tab"></p>

**Group them into a project.** In My Pins, click **Group**, select at least two pins, confirm, and give the group a name. They collapse into a single HUD pin with one merged material list. Expand the group again and each member shows its own materials; **Disband** turns them back into separate pins whenever you want.

That is all you need to start. Everything below is controls, settings and the finer details.

---

## 🎮 Controls

| Action | Default |
| :--- | :--- |
| Pin recipe / add count | `Mouse Wheel Click` |
| Decrease / remove pin | `Left Shift` + `Mouse Wheel Click` |
| Toggle HUD overlay | `F7` |
| Toggle Gathering List | `F8` |
| Cycle HUD pages | `Left Alt` |
| Clear all pins | `P` (press twice) |
| Open My Pins panel | Pin icon beside the **CRAFT** tab, inventory open |

Every one of these can be rebound in the config. They are keyboard and mouse only for now — gamepad support is on the ideas list, so a controller player will not be able to pin anything yet.

Clearing all pins asks before it does anything: press the key once to arm it, then again within two seconds. The My Pins panel also has an info button that shows your current keys in-game, so you never have to come back here for them.

---

## 🖼️ More Screenshots

The screenshots above show the core loop. These cover the rest — layouts, group shapes, a build piece, a Forge upgrade and panels.

<details>
<summary><b>Click to expand</b></summary>

<br>

<p align="center"><b>Layouts</b></p>

<p align="center">Horizontal, near the map side. Auto-Detect picks this when MyLittleUI is installed.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Horizontal.png?raw=true" alt="Horizontal layout"></p>

<p align="center">Alongside MyLittleUI, with the layout adjusted around it.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/MyLittleUI.png?raw=true" alt="Running with MyLittleUI"></p>

<p align="center">Out at sea, moved clear of the sailing HUD.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Sailing.png?raw=true" alt="Pins while sailing"></p>

<p align="center">Pin more than fits and the list pages instead of growing. The dots on top show which page you are on.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Pages1.png?raw=true" alt="First page of pins"> <img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/Pages2.png?raw=true" alt="Second page of pins"></p>

<br>

<p align="center"><b>Groups</b></p>

<p align="center">A group needing many different materials switches to a compact grid.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/GroupCompactPin.png?raw=true" alt="Compact group layout"></p>

<p align="center">Still too long, and the remainder collapses into a single <code>+N</code> cell.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/GroupPinN.png?raw=true" alt="Group overflow cell"></p>

<br>

<p align="center"><b>Building</b></p>

<p align="center">Hammer build pieces pin exactly like recipes, straight from the build menu.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/BuildPiecePin.png?raw=true" alt="A Hammer build piece pinned from the build menu"></p>

<br>

<p align="center"><b>Upgrades</b></p>

<p align="center">An upgrade pinned at the Forge of Potential asks for the Idol instead of the ordinary materials.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/ForgeUpgradePin.png?raw=true" alt="An upgrade pinned at the Forge of Potential"></p>

<br>

<p align="center"><b>Panels and lists</b></p>

<p align="center">The info button in the My Pins panel, showing your current keys without leaving the game.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/ControlsInfo.png?raw=true" alt="The controls info panel"></p>

<p align="center">The Gathering List on its own, totalling every pin at once.</p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/GatheringList.png?raw=true" alt="The Gathering List"></p>
</details>

---

## ⚙️ Configuration

Install [Official BepInEx ConfigurationManager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/) and press `F1` in-game to reach everything with the HUD still on screen. Editing `BepInEx/config/com.Kadrio.RecipePinner.cfg` by hand works too, and deleting that file restores every default on the next launch.

<details>
<summary><b>Every category, and what it covers</b></summary>

<br>

| Section | What it covers |
| :--- | :--- |
| **General** | Enable the mod, language override, layout mode, pin limit, pins per page, auto-unpin after crafting or building |
| **Controls** | Every hotkey: pin, unpin, toggle visibility, Gathering List, page switch, clear all |
| **Chest Scanner** | Whether nearby chests count toward your materials, plus scan range and interval |
| **HUD Appearance** | UI scale, background opacity, font sizes, recipe/material/group icon sizes, the craft-readiness bar, the single-unit bracket |
| **Colors** | The three material states, the recipe name, and both readiness-bar colors |
| **Pagination** | Page dot size, spacing, color and inactive opacity |
| **Gathering List** | Turn it on, open it automatically, column count, font sizes, and where it sits beside an open chest |
| **Groups** | When a group switches to the compact grid, how many rows it may use, and the group icon's font size |
| **My Pins Panel** | Panel width, height and position, the button's position, size and icon color, and the size of the materials shown on each row |
| **Layout (Vertical)** | List width, pin spacing, position |
| **Layout (Horizontal - Map Side)** | Column width, pin spacing, position |
| **Layout (Horizontal - Bottom Right)** | Column width, pin spacing, position, and whether the list lifts clear of Valheim's button-hint bar |
| **Debug** | Verbose logging. Off by default; turn it on before reporting a bug |

</details>

---

## ❓ FAQ

<details>
<summary><b>Will this mod disable my achievements?</b></summary>

Valheim switches achievements off while **any** mod is installed. That is the game's own check and every mod trips it, Recipe Pinner included — but it is not permanent, and it is not something the mod does to you. The modded flag only exists in the running game, so removing your mods turns achievements back on.

What *is* permanent is the game's own cheat marking: console cheats are recorded on your character, spawned items stay marked as cheated for as long as you keep them, and world modifiers are recorded on the world. Recipe Pinner does none of these — it reads your inventory and draws a HUD. It runs no console commands, spawns nothing and changes no world setting.

Since Valheim 1.0.12 you can also opt back in while still modded. With the console enabled, run:

`yesiuseddevcommandsbutiwantmyachievementsanyway`

The setting is saved on your character, so once is enough.

</details>

<details>
<summary><b>Do I need this on a server, or do my friends need it?</b></summary>

No to both. Recipe Pinner draws a HUD and reads your own inventory, and it sends nothing over the network. Install it on your own game only: it works in multiplayer whether or not anyone else has it, and a dedicated server does not need it.

</details>

<details>
<summary><b>Does it work with modded recipes?</b></summary>

Yes. Anything that shows up in a crafting station's list or the Hammer's build menu can be pinned, including recipes and pieces other mods add — Recipe Pinner reads what the game shows you, not a list of vanilla items. If a modded recipe pins wrongly, please report it and name the mod.

</details>

<details>
<summary><b>The Pins button overlaps something. Can I move it?</b></summary>

Yes. `MyPinsButtonPosition` sets where it sits and `MyPinsButtonSize` how big it is, and both apply the moment you change them — open the config while the inventory is on screen and you can watch the button move. X grows to the left and Y grows upward.

If another mod rearranges the crafting panel, this is the setting that gets the button out of its way.

</details>

<details>
<summary><b>More questions</b></summary>

<br>

**Does it craft from chests, like the craft-from-container mods?** No. The Chest Scanner only counts what is in nearby chests, so your numbers tell the whole story. Crafting still takes from your inventory exactly as it normally does.

**Will it slow my game down?** It is light. The counting only reruns when your inventory changes, and the Chest Scanner — the only part that looks at the world around you — is off by default and checks on an interval rather than every frame.

**Is it safe to remove?** Yes. Your pins are kept in their own files under `BepInEx/config/RecipePinner_Data/`, and the mod adds nothing to your world or your character, so uninstalling it leaves both untouched.

**Does the Chest Scanner touch what is in my chests?** No. It only reads — it never moves, takes or rearranges anything — and it stays off until you turn it on. Its range and interval are both settings.

**Are pins shared between my characters?** No. Each character has its own pins, saved separately. Switching characters loads that character's own list.

**Does it work with MyLittleUI?** Yes. Auto-Detect switches to the horizontal layout when MyLittleUI is installed, reading MyLittleUI's own settings to decide.

**Why does my group pin show `+N` instead of all its materials?** A group with many different materials switches to a compact grid, and that grid is capped so one large group cannot stretch the pins beside it. Raise the cap with `GroupCompactMaxRows`, or change when compact mode starts with `GroupCompactThreshold`. When a group is your only pin, it never shows `+N`.

**Can I force a particular language?** Yes, with the `LanguageOverride` setting. Left alone, the mod follows Valheim's language.

**How do I fix a bad translation myself?** Open the matching `.json` in `BepInEx/plugins/RecipePinner/RecipePinner_languages/`, edit the text on the right of each line, and save. Leave placeholders like `{0}` alone — they are replaced with numbers and key names at runtime.

**Why did some config settings reset after updating?** Version 1.3.0 reorganized the config categories, so a setting that moved may not have kept its old value. If you are coming from 1.2.x, launch once and glance over the layout, hotkey, Chest Scanner and Gathering List sections. Nothing is broken if you skip it — you may just be running a default you did not choose.

</details>

---

## 🐞 Known Issues

**With Quick Stack Store installed, its container buttons land on the Gathering List.** QSS places its buttons to the right of the container window, which is exactly where the list snaps when you open a chest, so the two end up on top of each other. Until it is fixed properly, raising the X value of `ContainerGatheringListPosition` moves the list clear of them.

Anything else, please report it — see **Support And Feedback** below.

---

## 🧭 Future Ideas

Things I may build next. No promises on any of them, and the one at the bottom shows how this list is meant to work.

* **Group readiness bar:** split a group's accent bar into one segment per member, so a single glance tells you which recipes in the project are already craftable.
* **Chest Scanner in the readiness bar:** turn the bar yellow when a recipe is craftable only with nearby chest contents, matching how the material counts already behave.
* **Hold-to-pin quantity:** hold the Pin key to choose how many copies to pin, instead of pressing it once per copy.
* **Station requirement on the pin:** show which crafting station a recipe needs and at what level, so a pin can tell you the materials are ready but the station is not.
* **Ready-made pin sets:** pin a whole kit at once — an iron set, a bronze set — instead of picking the recipes one by one.
* **Share a group to chat:** send a group's material list into the chat window so a co-op party knows what to collect.
* **Pinned recipe highlight:** mark pinned recipes inside the crafting panel. This one may not be feasible.
* **Gamepad support**, material-gathered notifications, auto-sorting craftable pins to the top, chest highlighting, VNEI, and more compatibility work with other UI and crafting mods.
* ✅ ~~**Pin keybind hint:** show the Pin control in the game's own hint bar, next to Move and Use.~~ Shipped in 1.5.0.

---

## 🤖 AI-Assisted Development

Recipe Pinner is marked **AI-Generated** on Thunderstore, from 1.3.0 onward. That label is there by choice: AI assistance is a real part of how this mod is built, most heavily from 1.3.0, when My Pins and Pin Groups grew past what I could finish on my own.

What it is not is unattended. Every release is reviewed, tested in game and judged by me before it goes out, and anything that breaks is mine to answer for.

---

## ☕ Support My Work

<p align="center">If Recipe Pinner makes your Valheim life easier, you can support development here:</p>

<p align="center"><a href="https://buymeacoffee.com/kadrio"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee"></a></p>

<p align="center"><img src="https://github.com/KadrioS/RecipePinner/blob/main/assets/images/BMC_QRCode.png?raw=true" alt="Buy Me A Coffee QR" width="150"></p>

---

## 💬 Support And Feedback

Found a bug, a bad translation, or a mod this one fights with? Tell me and name the other mod — that is usually the whole diagnosis.

Discord: **kadrio** · [GitHub Issues](https://github.com/KadrioS/RecipePinner/issues)

---

## 🔗 Mirrors

Same mod, same version — every site is updated at the same time.

[![Thunderstore](https://img.shields.io/badge/Thunderstore-Download-blue?style=for-the-badge)](https://thunderstore.io/c/valheim/p/KadrioS/RecipePinner/) [![Hexium](https://img.shields.io/badge/Hexium-Download-purple?style=for-the-badge)](https://valheim.hexium.gg/mods/Kadrio/RecipePinner) [![NexusMods](https://img.shields.io/badge/NexusMods-Download-orange?style=for-the-badge)](https://www.nexusmods.com/valheim/mods/3195)

---

## 📄 Documentation

* [Changelog](docs/CHANGELOG.md) — full version history

### Building From Source

Requires the .NET SDK and a Valheim install with BepInEx.

```
dotnet build src/RecipePinner/RecipePinner.csproj -c Release
```

Output lands in `src/RecipePinner/bin/Release/` as `RecipePinner.dll` plus the `RecipePinner_languages/` folder. If Valheim is not in the default Steam location, create `Directory.Build.props.user` in the repository root:

```xml
<Project>
  <PropertyGroup>
    <ValheimDir>D:\Games\Valheim</ValheimDir>
  </PropertyGroup>
</Project>
```

---

**Pin it. Group it. Gather it. Build it.**

See you in Valheim.
