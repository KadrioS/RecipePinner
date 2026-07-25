using BepInEx.Configuration;
using UnityEngine;

namespace ValheimRecipePinner
{
    // ================================================================
    // RecipePinnerPlugin — Config partial
    // All ConfigEntry declarations, BindConfigs(), and ConfigManagerAttributes
    // ================================================================
    public partial class RecipePinnerPlugin
    {
        // ── 01 - General ─────────────────────────────────────────
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<string> LanguageOverride;
        public static ConfigEntry<PinLayoutMode> LayoutModeConfig;
        public static ConfigEntry<int> MaximumPins;
        public static ConfigEntry<int> PinsPerPage;
        public static ConfigEntry<bool> AutoUnpinAfterCrafting;
        public static ConfigEntry<bool> AutoUnpinAfterBuilding;

        // ── 02 - Controls ────────────────────────────────────────
        public static ConfigEntry<KeyCode> HotkeyPin;
        public static ConfigEntry<KeyCode> HotkeyUnpin;
        public static ConfigEntry<KeyCode> HotkeyClearAll;
        public static ConfigEntry<KeyCode> HotkeyToggleVisibility;
        public static ConfigEntry<KeyCode> HotkeyPageSwitch;
        public static ConfigEntry<KeyCode> HotkeyGatheringList;

        // ── 03 - Chest Scanner ───────────────────────────────────
        public static ConfigEntry<bool> EnableChestScanning;
        public static ConfigEntry<float> ChestScanRange;
        public static ConfigEntry<float> ChestScanInterval;

        // ── 04 - HUD Appearance ──────────────────────────────────
        public static ConfigEntry<float> UIScale;
        public static ConfigEntry<float> BackgroundOpacity;
        public static ConfigEntry<int> FontSizeRecipeName;
        public static ConfigEntry<int> FontSizeMaterials;
        public static ConfigEntry<int> HudRecipeIconSize;
        public static ConfigEntry<int> HudMaterialIconSize;
        public static ConfigEntry<int> HudGroupIconSize;
        public static ConfigEntry<bool> EnableCraftReadiness;

        // ── 05 - Colors ─────────────────────────────────────────
        public static ConfigEntry<Color> ColorHeader;
        public static ConfigEntry<Color> ColorEnoughInInventory;
        public static ConfigEntry<Color> ColorEnoughWithChests;
        public static ConfigEntry<Color> ColorMissing;
        public static ConfigEntry<Color> ColorCraftReady;
        public static ConfigEntry<Color> ColorCraftNotReady;

        // ── 06 - Pagination ──────────────────────────────────────
        public static ConfigEntry<Color> ColorPaginationActive;
        public static ConfigEntry<float> PaginationInactiveOpacity;
        public static ConfigEntry<int> PaginationDotSize;
        public static ConfigEntry<int> PaginationDotSpacing;

        // ── 07 - Gathering List ──────────────────────────────────
        public static ConfigEntry<bool> EnableGatheringList;
        public static ConfigEntry<bool> AutoOpenGatheringList;
        public static ConfigEntry<int> GatheringListColumns;
        public static ConfigEntry<int> GatheringListFontSizeTitle;
        public static ConfigEntry<int> GatheringListFontSizeMaterials;
        public static ConfigEntry<Vector2> ContainerGatheringListPosition;

        // ── 08 - Groups ──────────────────────────────────────────
        public static ConfigEntry<int> GroupCompactThreshold;
        public static ConfigEntry<int> GroupCompactMaxRows;
        public static ConfigEntry<int> GroupIconFontSize;

        // ── 09 - My Pins Panel ───────────────────────────────────
        public static ConfigEntry<float> MyPinsPanelWidth;
        public static ConfigEntry<float> MyPinsPanelHeight;
        public static ConfigEntry<Vector2> MyPinsPanelPosition;
        public static ConfigEntry<Color> ButtonTextColor;
        public static ConfigEntry<Vector2> MyPinsButtonPosition;
        public static ConfigEntry<int> MyPinsButtonSize;

        // ── 10 - Layout: Vertical ────────────────────────────────
        public static ConfigEntry<float> VerticalListWidth;
        public static ConfigEntry<float> VerticalPinSpacing;
        public static ConfigEntry<Vector2> VerticalPosition;

        // ── 11 - Layout: Horizontal (Map Side) ───────────────────
        public static ConfigEntry<float> HorizontalColumnWidth;
        public static ConfigEntry<float> HorizontalPinSpacing;
        public static ConfigEntry<Vector2> HorizontalPosition;

        // ── 12 - Layout: Horizontal (Bottom Right) ───────────────
        public static ConfigEntry<float> BottomRightColumnWidth;
        public static ConfigEntry<float> BottomRightPinSpacing;
        public static ConfigEntry<Vector2> BottomRightPosition;

        // ── 13 - Debug ───────────────────────────────────────────
        public static ConfigEntry<bool> EnableDebugLogging;

        private void BindConfigs()
        {
            // ── 01 - General ──────────────────────────────────────────────
            EnableMod = Config.Bind("01 - General", "EnableMod", true,
                new ConfigDescription("Enable or disable the mod completely.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            EnableMod.SettingChanged += (s, e) =>
            {
                if (!EnableMod.Value)
                {
                    UIMgr?.DestroyUI();
                    UIMgr?.DestroyMyPinsUI();
                }
            };

            LanguageOverride = Config.Bind("01 - General", "LanguageOverride", "Auto",
                new ConfigDescription("Force a specific language (e.g., 'German', 'Turkish'). 'Auto' uses the game language.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            LanguageOverride.SettingChanged += (s, e) => { LocalizationMgr?.LoadTranslations(); RecipeMgr?.RefreshRecipeCache(); UIMgr?.DestroyUI(); UIMgr?.DestroyMyPinsUI(); };

            LayoutModeConfig = Config.Bind("01 - General", "LayoutMode", PinLayoutMode.AutoDetect,
                new ConfigDescription("HUD pin layout position.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            LayoutModeConfig.SettingChanged += (s, e) => { ReadMyLittleUIConfig(); UIMgr?.DestroyUI(); };

            MaximumPins = Config.Bind("01 - General", "MaximumPins", 10,
                new ConfigDescription("Maximum number of pins allowed at once.", new AcceptableValueRange<int>(1, 20),
                new ConfigurationManagerAttributes { Order = 96 }));
            MaximumPins.SettingChanged += (s, e) =>
            {
                int trimmed = RecipeMgr?.TrimToMaximumPins(MaximumPins.Value) ?? 0;
                if (trimmed > 0)
                {
                    DebugLogger.Log($"MaximumPins reduced, trimmed {trimmed} effective pin(s)");
                    RecipeMgr?.RefreshRecipeCache();
                }

                UIMgr?.ResetPage();
                UIMgr?.DestroyUI();

                if (trimmed > 0)
                    DataMgr?.SavePins();
            };

            PinsPerPage = Config.Bind("01 - General", "PinsPerPage", 5,
                new ConfigDescription("How many pins to show per HUD page.", new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { Order = 95 }));
            PinsPerPage.SettingChanged += (s, e) => { UIMgr?.ResetPage(); UIMgr?.DestroyUI(); };

            AutoUnpinAfterCrafting = Config.Bind("01 - General", "AutoUnpinAfterCrafting", true,
                new ConfigDescription("Automatically unpin a recipe after it is crafted.", null,
                new ConfigurationManagerAttributes { Order = 94 }));

            AutoUnpinAfterBuilding = Config.Bind("01 - General", "AutoUnpinAfterBuilding", true,
                new ConfigDescription("Automatically unpin a recipe after placing a building piece.", null,
                new ConfigurationManagerAttributes { Order = 93 }));

            // ── 02 - Controls ─────────────────────────────────────────────
            HotkeyPin = Config.Bind("02 - Controls", "HotkeyPin", KeyCode.Mouse2,
                new ConfigDescription("Hotkey to pin the currently viewed recipe.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            HotkeyUnpin = Config.Bind("02 - Controls", "HotkeyUnpin", KeyCode.LeftShift,
                new ConfigDescription("Hold this key + press the Pin hotkey over a recipe or build piece to decrease/remove that pin.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            HotkeyToggleVisibility = Config.Bind("02 - Controls", "HotkeyToggleVisibility", KeyCode.F7,
                new ConfigDescription("Hotkey to show/hide the HUD pin overlay.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            HotkeyGatheringList = Config.Bind("02 - Controls", "HotkeyGatheringList", KeyCode.F8,
                new ConfigDescription("Hotkey to toggle the Gathering List panel.", null,
                new ConfigurationManagerAttributes { Order = 96 }));
            HotkeyPageSwitch = Config.Bind("02 - Controls", "HotkeyPageSwitch", KeyCode.LeftAlt,
                new ConfigDescription("Press this key to cycle through HUD pages.", null,
                new ConfigurationManagerAttributes { Order = 95 }));
            HotkeyClearAll = Config.Bind("02 - Controls", "HotkeyClearAll", KeyCode.P,
                new ConfigDescription("Hotkey to clear all pinned recipes.", null,
                new ConfigurationManagerAttributes { Order = 94 }));

            // ── 03 - Chest Scanner ────────────────────────────────────────
            EnableChestScanning = Config.Bind("03 - Chest Scanner", "EnableChestScanning", false,
                new ConfigDescription("Count materials found in nearby chests towards requirements.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            EnableChestScanning.SettingChanged += (s, e) =>
            {
                if (EnableChestScanning.Value)
                {
                    // Re-enable: wipe stale/partial data, then do a fresh world scan
                    ContainerScanner.ClearAll();
                    ContainerMgr?.InitializeContainers();
                }
                else
                {
                    // Disabled: free all container references immediately
                    ContainerScanner.ClearAll();
                }
                RecipeMgr?.RefreshRecipeCache();
            };

            ChestScanRange = Config.Bind("03 - Chest Scanner", "ChestScanRange", 20f,
                new ConfigDescription("Radius (meters) in which chests are scanned.", new AcceptableValueRange<float>(5f, 100f),
                new ConfigurationManagerAttributes { Order = 98 }));

            ChestScanInterval = Config.Bind("03 - Chest Scanner", "ChestScanInterval", 3.0f,
                new ConfigDescription("How often (seconds) chests are re-scanned.", new AcceptableValueRange<float>(0.5f, 10f),
                new ConfigurationManagerAttributes { Order = 97 }));

            // ── 04 - HUD Appearance ───────────────────────────────────────
            UIScale = Config.Bind("04 - HUD Appearance", "UIScale", 0.75f,
                new ConfigDescription("Global UI scale multiplier.", new AcceptableValueRange<float>(0.3f, 3.0f),
                new ConfigurationManagerAttributes { Order = 99 }));
            UIScale.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            BackgroundOpacity = Config.Bind("04 - HUD Appearance", "BackgroundOpacity", 0.50f,
                new ConfigDescription("Background panel opacity (0 = transparent, 1 = opaque).", new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 98 }));

            FontSizeRecipeName = Config.Bind("04 - HUD Appearance", "FontSizeRecipeName", 16,
                new ConfigDescription("Font size for recipe/group name in HUD pins.", new AcceptableValueRange<int>(8, 40),
                new ConfigurationManagerAttributes { Order = 97 }));
            FontSizeRecipeName.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            FontSizeMaterials = Config.Bind("04 - HUD Appearance", "FontSizeMaterials", 15,
                new ConfigDescription("Font size for material names and amounts in HUD pins.", new AcceptableValueRange<int>(8, 40),
                new ConfigurationManagerAttributes { Order = 96 }));
            FontSizeMaterials.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            HudRecipeIconSize = Config.Bind("04 - HUD Appearance", "HudRecipeIconSize", 28,
                new ConfigDescription("Size (px) of the recipe icon in the HUD pin header.", new AcceptableValueRange<int>(12, 64),
                new ConfigurationManagerAttributes { Order = 95 }));
            HudRecipeIconSize.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            HudMaterialIconSize = Config.Bind("04 - HUD Appearance", "HudMaterialIconSize", 20,
                new ConfigDescription("Size (px) of the material icons in the HUD pin resource list.", new AcceptableValueRange<int>(10, 48),
                new ConfigurationManagerAttributes { Order = 94 }));
            HudMaterialIconSize.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            HudGroupIconSize = Config.Bind("04 - HUD Appearance", "HudGroupIconSize", 28,
                new ConfigDescription("Size (px) of the group icon (stacked cards) in the HUD pin header.", new AcceptableValueRange<int>(12, 64),
                new ConfigurationManagerAttributes { Order = 93 }));
            HudGroupIconSize.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            EnableCraftReadiness = Config.Bind("04 - HUD Appearance", "EnableCraftReadiness", true,
                new ConfigDescription("Show a colored accent bar indicating whether a recipe can be crafted.", null,
                new ConfigurationManagerAttributes { Order = 92 }));
            EnableCraftReadiness.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            // ── 05 - Colors ───────────────────────────────────────────────
            ColorHeader = Config.Bind("05 - Colors", "ColorHeader", new Color(1f, 0.808f, 0f, 1f),
                new ConfigDescription("Recipe/group name text color.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            ColorHeader.SettingChanged += (s, e) => { RecipeMgr?.RefreshRecipeCache(); UIMgr?.DestroyUI(); };

            ColorEnoughInInventory = Config.Bind("05 - Colors", "ColorEnoughInInventory", new Color(0f, 1f, 0f, 1f),
                new ConfigDescription("Material amount color when you have enough in your inventory.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            ColorEnoughInInventory.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorEnoughWithChests = Config.Bind("05 - Colors", "ColorEnoughWithChests", new Color(1f, 1f, 0f, 1f),
                new ConfigDescription("Material amount color when enough only if chests are counted.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            ColorEnoughWithChests.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorMissing = Config.Bind("05 - Colors", "ColorMissing", new Color(1f, 0.33f, 0.33f, 1f),
                new ConfigDescription("Material amount color when materials are missing.", null,
                new ConfigurationManagerAttributes { Order = 96 }));
            ColorMissing.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorCraftReady = Config.Bind("05 - Colors", "ColorCraftReady",
                new Color(0.2f, 0.9f, 0.3f, 0.85f),
                new ConfigDescription("Accent bar color when all materials are available.", null,
                new ConfigurationManagerAttributes { Order = 95 }));
            ColorCraftReady.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorCraftNotReady = Config.Bind("05 - Colors", "ColorCraftNotReady",
                new Color(0.9f, 0.25f, 0.25f, 0.5f),
                new ConfigDescription("Accent bar color when materials are missing.", null,
                new ConfigurationManagerAttributes { Order = 94 }));
            ColorCraftNotReady.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            // ── 06 - Pagination ───────────────────────────────────────────
            ColorPaginationActive = Config.Bind("06 - Pagination", "ColorPaginationActive", new Color(1f, 0.717f, 0.368f, 1f),
                new ConfigDescription("Active page indicator dot color.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            ColorPaginationActive.SettingChanged += (s, e) => UIMgr?.UpdateUI(true);

            PaginationInactiveOpacity = Config.Bind("06 - Pagination", "PaginationInactiveOpacity", 0.30f,
                new ConfigDescription("Opacity of inactive page indicator dots.", new AcceptableValueRange<float>(0.1f, 1.0f),
                new ConfigurationManagerAttributes { Order = 98 }));
            PaginationInactiveOpacity.SettingChanged += (s, e) => UIMgr?.UpdateUI(true);

            PaginationDotSize = Config.Bind("06 - Pagination", "PaginationDotSize", 10,
                new ConfigDescription("Size of the pagination dot squares.", new AcceptableValueRange<int>(5, 20),
                new ConfigurationManagerAttributes { Order = 97 }));
            PaginationDotSize.SettingChanged += (s, e) => UIMgr?.UpdateUI(true);

            PaginationDotSpacing = Config.Bind("06 - Pagination", "PaginationDotSpacing", 8,
                new ConfigDescription("Space between pagination squares.", new AcceptableValueRange<int>(0, 20),
                new ConfigurationManagerAttributes { Order = 96 }));
            PaginationDotSpacing.SettingChanged += (s, e) => UIMgr?.UpdateUI(true);

            // ── 07 - Gathering List ───────────────────────────────────────
            EnableGatheringList = Config.Bind("07 - Gathering List", "EnableGatheringList", true,
                new ConfigDescription("Enable the Gathering List (aggregated material overview).", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            EnableGatheringList.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            AutoOpenGatheringList = Config.Bind("07 - Gathering List", "AutoOpenGatheringList", true,
                new ConfigDescription("Automatically open the Gathering List when 2+ recipes are pinned.", null,
                new ConfigurationManagerAttributes { Order = 98 }));

            GatheringListColumns = Config.Bind("07 - Gathering List", "GatheringListColumns", 4,
                new ConfigDescription("Number of columns in the Gathering List grid (horizontal modes only). Panel width adjusts proportionally.", new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { Order = 97 }));
            GatheringListColumns.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            GatheringListFontSizeTitle = Config.Bind("07 - Gathering List", "GatheringListFontSizeTitle", 20,
                new ConfigDescription("Font size for the Gathering List title.", new AcceptableValueRange<int>(8, 40),
                new ConfigurationManagerAttributes { Order = 96 }));
            GatheringListFontSizeTitle.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            GatheringListFontSizeMaterials = Config.Bind("07 - Gathering List", "GatheringListFontSizeMaterials", 15,
                new ConfigDescription("Font size for material amounts in the Gathering List.", new AcceptableValueRange<int>(8, 40),
                new ConfigurationManagerAttributes { Order = 95 }));
            GatheringListFontSizeMaterials.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            ContainerGatheringListPosition = Config.Bind("07 - Gathering List", "ContainerGatheringListPosition", new Vector2(-400f, 320f),
                new ConfigDescription("Gathering List position offset (X, Y) when a container (chest/inventory) is open.", null,
                new ConfigurationManagerAttributes { Order = 94 }));

            // ── 08 - Groups ───────────────────────────────────────────────
            GroupCompactThreshold = Config.Bind("08 - Groups", "GroupCompactThreshold", 4,
                new ConfigDescription("Number of unique materials above which a group pin switches to compact (grid) layout. Default: 4 (triggers at 5+).",
                new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { Order = 99 }));
            GroupCompactThreshold.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            GroupCompactMaxRows = Config.Bind("08 - Groups", "GroupCompactMaxRows", 3,
                new ConfigDescription("Number of grid rows a compact group pin shows before collapsing the rest into a \"+N\" cell. Default: 3.",
                new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { Order = 97 }));
            GroupCompactMaxRows.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            GroupIconFontSize = Config.Bind("08 - Groups", "GroupIconFontSize", 16,
                new ConfigDescription("Font size of the member count number displayed on the group icon.",
                new AcceptableValueRange<int>(8, 32),
                new ConfigurationManagerAttributes { Order = 98 }));
            GroupIconFontSize.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            // ── 09 - My Pins Panel ────────────────────────────────────────
            MyPinsPanelWidth = Config.Bind("09 - My Pins Panel", "PanelWidth", 375f,
                new ConfigDescription("Width of the My Pins panel.", new AcceptableValueRange<float>(200f, 600f),
                new ConfigurationManagerAttributes { Order = 99 }));
            MyPinsPanelWidth.SettingChanged += (s, e) => UIMgr?.DestroyMyPinsUI();

            MyPinsPanelHeight = Config.Bind("09 - My Pins Panel", "PanelHeight", 480f,
                new ConfigDescription("Height of the My Pins panel.", new AcceptableValueRange<float>(200f, 800f),
                new ConfigurationManagerAttributes { Order = 98 }));
            MyPinsPanelHeight.SettingChanged += (s, e) => UIMgr?.DestroyMyPinsUI();

            MyPinsPanelPosition = Config.Bind("09 - My Pins Panel", "PanelPosition", Vector2.zero,
                new ConfigDescription("Position offset (X, Y) of the My Pins panel from the screen center. (0, 0) = perfectly centered.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            MyPinsPanelPosition.SettingChanged += (s, e) => UIMgr?.DestroyMyPinsUI();

            ButtonTextColor = Config.Bind("09 - My Pins Panel", "ButtonIconColor", new Color(1f, 0.631f, 0.239f, 1f),
                new ConfigDescription("Tint color of the pin icon on the My Pins button (default: #ffa13d).", null,
                new ConfigurationManagerAttributes { Order = 96 }));
            ButtonTextColor.SettingChanged += (s, e) => UIMgr?.DestroyMyPinsUI();

            MyPinsButtonPosition = Config.Bind("09 - My Pins Panel", "MyPinsButtonPosition", new Vector2(-500f, 570f),
                new ConfigDescription("Position offset (X, Y) of the My Pins button from the inventory.", null,
                new ConfigurationManagerAttributes { Order = 95 }));
            MyPinsButtonPosition.SettingChanged += (s, e) => UIMgr?.DestroyMyPinsUI();

            MyPinsButtonSize = Config.Bind("09 - My Pins Panel", "MyPinsButtonSize", 40,
                new ConfigDescription("Size of the My Pins icon button in pixels.",
                new AcceptableValueRange<int>(20, 200),
                new ConfigurationManagerAttributes { Order = 94 }));
            MyPinsButtonSize.SettingChanged += (s, e) => UIMgr?.DestroyMyPinsUI();


            // ── 10 - Layout (Vertical Mode) ───────────────────────────────
            VerticalListWidth = Config.Bind("10 - Layout (Vertical Mode)", "ListWidth", 265f,
                new ConfigDescription("Width of the pin list panel.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            VerticalPinSpacing = Config.Bind("10 - Layout (Vertical Mode)", "PinSpacing", 10f,
                new ConfigDescription("Vertical spacing between pin cards.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            VerticalPosition = Config.Bind("10 - Layout (Vertical Mode)", "Position", new Vector2(-40f, -250f),
                new ConfigDescription("Anchor position offset (X, Y).", null,
                new ConfigurationManagerAttributes { Order = 97 }));

            // ── 11 - Layout (Horizontal - Map Side) ──────────────────────
            HorizontalColumnWidth = Config.Bind("11 - Layout (Horizontal - Map Side)", "ColumnWidth", 265f,
                new ConfigDescription("Width of each pin column.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            HorizontalPinSpacing = Config.Bind("11 - Layout (Horizontal - Map Side)", "PinSpacing", 10f,
                new ConfigDescription("Spacing between pin cards in the column.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            HorizontalPosition = Config.Bind("11 - Layout (Horizontal - Map Side)", "Position", new Vector2(-250f, -40f),
                new ConfigDescription("Anchor position offset (X, Y).", null,
                new ConfigurationManagerAttributes { Order = 97 }));

            // ── 12 - Layout (Horizontal - Bottom Right) ───────────────────
            BottomRightColumnWidth = Config.Bind("12 - Layout (Horizontal - Bottom Right)", "ColumnWidth", 265f,
                new ConfigDescription("Width of each pin column.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            BottomRightPinSpacing = Config.Bind("12 - Layout (Horizontal - Bottom Right)", "PinSpacing", 10f,
                new ConfigDescription("Spacing between pin cards in the column.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            BottomRightPosition = Config.Bind("12 - Layout (Horizontal - Bottom Right)", "Position", new Vector2(-40f, 40f),
                new ConfigDescription("Anchor position offset (X, Y).", null,
                new ConfigurationManagerAttributes { Order = 97 }));

            // ── 13 - Debug ────────────────────────────────────────────────
            EnableDebugLogging = Config.Bind("13 - Debug", "EnableDebugLogging", false,
                new ConfigDescription("Enable verbose debug logging to the BepInEx console.", null,
                new ConfigurationManagerAttributes { Order = 99 }));

            DebugLogger.Log("Config loaded");
        }

        public class ConfigurationManagerAttributes
        {
            public bool? ShowRangeAsPercent;
            public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;
            public bool? Browsable;
            public string Category;
            public object DefaultValue;
            public bool? HideDefaultButton;
            public bool? HideSettingName;
            public string Description;
            public string DispName;
            public int? Order;
            public bool? ReadOnly;
            public bool? IsAdvanced;
            public System.Func<object, string> ObjToStr;
            public System.Func<string, object> StrToObj;
        }
    }
}
