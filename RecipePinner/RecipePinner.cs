using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace ValheimRecipePinner
{
    [BepInPlugin("com.Kadrio.RecipePinner", "Recipe Pinner", "1.2.1")]
    public class RecipePinnerPlugin : BaseUnityPlugin
    {
        public static RecipePinnerPlugin Instance;

        // General
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<string> LanguageOverride;
        public static ConfigEntry<PinLayoutMode> LayoutModeConfig;
        public static ConfigEntry<int> MaximumPins;
        public static ConfigEntry<int> PinsPerPage;
        public static ConfigEntry<bool> AutoUnpinAfterCrafting;
        public static ConfigEntry<bool> EnableGatheringList;
        public static ConfigEntry<bool> AutoOpenGatheringList;

        // Controls
        public static ConfigEntry<KeyCode> HotkeyPin;
        public static ConfigEntry<KeyCode> HotkeyClearAll;
        public static ConfigEntry<KeyCode> HotkeyToggleVisibility;
        public static ConfigEntry<KeyCode> HotkeyPageSwitch;
        public static ConfigEntry<KeyCode> HotkeyGatheringList;

        // Chest scanning
        public static ConfigEntry<bool> EnableChestScanning;
        public static ConfigEntry<float> ChestScanRange;
        public static ConfigEntry<float> ChestScanInterval;

        // Appearance
        public static ConfigEntry<float> UIScale;
        public static ConfigEntry<int> FontSizeRecipeName;
        public static ConfigEntry<int> FontSizeMaterials;
        public static ConfigEntry<float> BackgroundOpacity;
        public static ConfigEntry<Color> ColorHeader;
        public static ConfigEntry<Color> ColorEnoughInInventory;
        public static ConfigEntry<Color> ColorEnoughWithChests;
        public static ConfigEntry<Color> ColorMissing;
        public static ConfigEntry<Color> ColorPaginationActive;
        public static ConfigEntry<float> PaginationInactiveOpacity;
        public static ConfigEntry<int> PaginationDotSize;
        public static ConfigEntry<int> PaginationDotSpacing;

        // Craft readiness
        public static ConfigEntry<bool> EnableCraftReadiness;
        public static ConfigEntry<Color> ColorCraftReady;
        public static ConfigEntry<Color> ColorCraftNotReady;

        // Gathering list fonts
        public static ConfigEntry<int> GatheringListFontSizeTitle;
        public static ConfigEntry<int> GatheringListFontSizeMaterials;

        // Layout: Vertical
        public static ConfigEntry<float> VerticalListWidth;
        public static ConfigEntry<float> VerticalPinSpacing;
        public static ConfigEntry<Vector2> VerticalPosition;

        // Layout: Horizontal (map side)
        public static ConfigEntry<float> HorizontalColumnWidth;
        public static ConfigEntry<float> HorizontalPinSpacing;
        public static ConfigEntry<Vector2> HorizontalPosition;

        // Layout: Bottom right
        public static ConfigEntry<float> BottomRightColumnWidth;
        public static ConfigEntry<float> BottomRightPinSpacing;
        public static ConfigEntry<Vector2> BottomRightPosition;

        // Debug
        public static ConfigEntry<bool> EnableDebugLogging;

        // Inventory behavior
        public static ConfigEntry<Vector2> InventoryGatheringListPosition;

        // Managers
        public LocalizationManager LocalizationMgr;
        public RecipeManager RecipeMgr;
        public ContainerScanner ContainerMgr;
        public UIManager UIMgr;
        public DataPersistence DataMgr;

        // MLUI compat
        internal bool _mluiMapListEnabled = false;
        internal bool _mluiNoMapListEnabled = false;
        internal bool _mluiInstalled = false;


        private bool _startupInitialized = false;
        private string _lastLanguage = "";
        private string _currentSessionPlayer = null;
        private static bool _isUiVisible = true;

        public enum PinLayoutMode
        {
            AutoDetect,
            ForceVertical,
            ForceHorizontal,
            ForceBottomRightHorizontal
        }

        public bool IsHorizontalMode
        {
            get
            {
                if (LayoutModeConfig.Value == PinLayoutMode.ForceBottomRightHorizontal) return true;
                if (LayoutModeConfig.Value == PinLayoutMode.ForceHorizontal) return true;
                if (LayoutModeConfig.Value == PinLayoutMode.ForceVertical) return false;

                if (!_mluiInstalled) return false;

                if (Game.m_noMap) return _mluiNoMapListEnabled;
                else return _mluiMapListEnabled;
            }
        }

        private void Awake()
        {
            Instance = this;

            BindConfigs();

            DebugLogger.Log("Plugin init");

            // Initialize managers
            LocalizationMgr = new LocalizationManager(this);
            RecipeMgr = new RecipeManager();
            ContainerMgr = new ContainerScanner();
            UIMgr = new UIManager();
            DataMgr = new DataPersistence();

            DebugLogger.Log("Managers ready");

            Harmony harmony = new Harmony("com.Kadrio.RecipePinner");
            harmony.PatchAll(typeof(RecipePinnerPlugin));
            harmony.PatchAll(typeof(ContainerScanner));

            DebugLogger.Log("Patches applied");
        }

        private void BindConfigs()
        {
            // General
            EnableMod = Config.Bind("1 - General", "EnableMod", true,
                new ConfigDescription("Enable or disable the mod completely.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            EnableMod.SettingChanged += (s, e) => { if (!EnableMod.Value) UIMgr?.DestroyUI(); };

            LanguageOverride = Config.Bind("1 - General", "LanguageOverride", "Auto",
                new ConfigDescription("Force a specific language (e.g., 'German', 'Turkish').", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            LanguageOverride.SettingChanged += (s, e) => { LocalizationMgr?.LoadTranslations(); RecipeMgr?.RefreshRecipeCache(); UIMgr?.DestroyUI(); };

            LayoutModeConfig = Config.Bind("1 - General", "LayoutMode", PinLayoutMode.AutoDetect,
                new ConfigDescription("Choose layout position.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            LayoutModeConfig.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            MaximumPins = Config.Bind("1 - General", "MaximumPins", 10,
                new ConfigDescription("Max pins allowed.", new AcceptableValueRange<int>(1, 20),
                new ConfigurationManagerAttributes { Order = 96 }));
            MaximumPins.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            PinsPerPage = Config.Bind("1 - General", "PinsPerPage", 5,
                new ConfigDescription("How many pins to show per page.", new AcceptableValueRange<int>(1, 10),
                new ConfigurationManagerAttributes { Order = 95 }));
            PinsPerPage.SettingChanged += (s, e) =>
            {
                UIMgr?.ResetPage();
                UIMgr?.DestroyUI();
            };

            AutoUnpinAfterCrafting = Config.Bind("1 - General", "AutoUnpinAfterCrafting", true,
                new ConfigDescription("Unpin after crafting.", null,
                new ConfigurationManagerAttributes { Order = 94 }));

            EnableGatheringList = Config.Bind("1 - General", "EnableGatheringList", true,
                new ConfigDescription("Enable the gathering list feature.", null,
                new ConfigurationManagerAttributes { Order = 93 }));
            EnableGatheringList.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            AutoOpenGatheringList = Config.Bind("1 - General", "AutoOpenGatheringList", true,
                new ConfigDescription("Automatically open gathering list when 2+ recipes are pinned.", null,
                new ConfigurationManagerAttributes { Order = 92 }));

            // Controls
            HotkeyPin = Config.Bind("2 - Controls", "HotkeyPin", KeyCode.Mouse2,
                new ConfigDescription("Key to pin recipe.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            HotkeyToggleVisibility = Config.Bind("2 - Controls", "HotkeyToggleVisibility", KeyCode.F7,
                new ConfigDescription("Key to toggle overlay.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            HotkeyGatheringList = Config.Bind("2 - Controls", "HotkeyGatheringList", KeyCode.F8,
                new ConfigDescription("Key to toggle gathering list panel.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            HotkeyPageSwitch = Config.Bind("2 - Controls", "HotkeyPageSwitch", KeyCode.LeftAlt,
                new ConfigDescription("Key to cycle through pin pages.", null,
                new ConfigurationManagerAttributes { Order = 96 }));
            HotkeyClearAll = Config.Bind("2 - Controls", "HotkeyClearAll", KeyCode.P,
                new ConfigDescription("Key to clear all pins.", null,
                new ConfigurationManagerAttributes { Order = 95 }));

            // Chest Scanning
            EnableChestScanning = Config.Bind("3 - Chest Scanner", "EnableChestScanning", false,
                new ConfigDescription("Count materials in nearby chests.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            EnableChestScanning.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ChestScanRange = Config.Bind("3 - Chest Scanner", "ChestScanRange", 20f,
                new ConfigDescription("Scan radius.", new AcceptableValueRange<float>(5f, 100f),
                new ConfigurationManagerAttributes { Order = 98 }));

            ChestScanInterval = Config.Bind("3 - Chest Scanner", "ChestScanInterval", 3.0f,
                new ConfigDescription("Scan frequency (seconds).", new AcceptableValueRange<float>(0.5f, 10f),
                new ConfigurationManagerAttributes { Order = 97 }));

            // Appearance
            UIScale = Config.Bind("4 - Appearance", "UIScale", 0.75f,
                new ConfigDescription("Global UI scale.", new AcceptableValueRange<float>(0.3f, 3.0f),
                new ConfigurationManagerAttributes { Order = 99 }));
            UIScale.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            BackgroundOpacity = Config.Bind("4 - Appearance", "BackgroundOpacity", 0.50f,
                new ConfigDescription("Background opacity.", new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 98 }));

            FontSizeRecipeName = Config.Bind("4 - Appearance", "FontSizeRecipeName", 16,
                new ConfigDescription("Recipe name font size.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            FontSizeRecipeName.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            FontSizeMaterials = Config.Bind("4 - Appearance", "FontSizeMaterials", 15,
                new ConfigDescription("Material font size.", null,
                new ConfigurationManagerAttributes { Order = 96 }));
            FontSizeMaterials.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            GatheringListFontSizeTitle = Config.Bind("4 - Appearance", "GatheringListFontSizeTitle", 20,
                new ConfigDescription("Gathering list title font size.", null,
                new ConfigurationManagerAttributes { Order = 95 }));
            GatheringListFontSizeTitle.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            GatheringListFontSizeMaterials = Config.Bind("4 - Appearance", "GatheringListFontSizeMaterials", 15,
                new ConfigDescription("Gathering list material font size.", null,
                new ConfigurationManagerAttributes { Order = 94 }));
            GatheringListFontSizeMaterials.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            EnableCraftReadiness = Config.Bind("4 - Appearance", "EnableCraftReadiness", true,
                new ConfigDescription("Show a colored accent bar indicating craft readiness.", null,
                new ConfigurationManagerAttributes { Order = 93 }));
            EnableCraftReadiness.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            // Colors
            ColorHeader = Config.Bind("5 - Colors", "ColorHeader", new Color(1f, 0.717f, 0.368f, 1f),
                new ConfigDescription("Recipe title color.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            ColorHeader.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorEnoughInInventory = Config.Bind("5 - Colors", "ColorEnoughInInventory", new Color(0f, 1f, 0f, 1f),
                new ConfigDescription("Color: Enough in inventory.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            ColorEnoughInInventory.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorEnoughWithChests = Config.Bind("5 - Colors", "ColorEnoughWithChests", new Color(1f, 1f, 0f, 1f),
                new ConfigDescription("Color: Enough with chests.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            ColorEnoughWithChests.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorMissing = Config.Bind("5 - Colors", "ColorMissing", new Color(1f, 0.33f, 0.33f, 1f),
                new ConfigDescription("Color: Missing materials.", null,
                new ConfigurationManagerAttributes { Order = 96 }));
            ColorMissing.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorCraftReady = Config.Bind("5 - Colors", "ColorCraftReady",
                new Color(0.2f, 0.9f, 0.3f, 0.85f),
                new ConfigDescription("Accent bar color when all materials are available.", null,
                new ConfigurationManagerAttributes { Order = 95 }));
            ColorCraftReady.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorCraftNotReady = Config.Bind("5 - Colors", "ColorCraftNotReady",
                new Color(0.9f, 0.25f, 0.25f, 0.5f),
                new ConfigDescription("Accent bar color when materials are missing.", null,
                new ConfigurationManagerAttributes { Order = 94 }));
            ColorCraftNotReady.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            // Pagination
            ColorPaginationActive = Config.Bind("6 - Pagination", "ColorPaginationActive", new Color(1f, 0.717f, 0.368f, 1f),
                new ConfigDescription("Active page dot color.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            ColorPaginationActive.SettingChanged += (s, e) => UIMgr?.UpdateUI(true);

            PaginationInactiveOpacity = Config.Bind("6 - Pagination", "PaginationInactiveOpacity", 0.30f,
                new ConfigDescription("Opacity of inactive page dots.", new AcceptableValueRange<float>(0.1f, 1.0f),
                new ConfigurationManagerAttributes { Order = 98 }));
            PaginationInactiveOpacity.SettingChanged += (s, e) => UIMgr?.UpdateUI(true);

            PaginationDotSize = Config.Bind("6 - Pagination", "PaginationDotSize", 10,
                new ConfigDescription("Size of the pagination squares.", new AcceptableValueRange<int>(5, 20),
                new ConfigurationManagerAttributes { Order = 97 }));
            PaginationDotSize.SettingChanged += (s, e) => UIMgr?.UpdateUI(true);

            PaginationDotSpacing = Config.Bind("6 - Pagination", "PaginationDotSpacing", 8,
                new ConfigDescription("Space between pagination squares.", new AcceptableValueRange<int>(0, 20),
                new ConfigurationManagerAttributes { Order = 96 }));
            PaginationDotSpacing.SettingChanged += (s, e) => UIMgr?.UpdateUI(true);

            // Layout: Vertical
            VerticalListWidth = Config.Bind("7 - Layout (Vertical Mode)", "ListWidth", 265f,
                new ConfigDescription("List width.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            VerticalPinSpacing = Config.Bind("7 - Layout (Vertical Mode)", "PinSpacing", 10f,
                new ConfigDescription("Spacing between pins.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            VerticalPosition = Config.Bind("7 - Layout (Vertical Mode)", "Position", new Vector2(-40f, -250f),
                new ConfigDescription("Position (X, Y).", null,
                new ConfigurationManagerAttributes { Order = 97 }));

            // Layout: Horizontal (Map Side)
            HorizontalColumnWidth = Config.Bind("8 - Layout (Horizontal - Map Side)", "ColumnWidth", 265f,
                new ConfigDescription("Column width.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            HorizontalPinSpacing = Config.Bind("8 - Layout (Horizontal - Map Side)", "PinSpacing", 10f,
                new ConfigDescription("Spacing between pins.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            HorizontalPosition = Config.Bind("8 - Layout (Horizontal - Map Side)", "Position", new Vector2(-250f, -40f),
                new ConfigDescription("Position (X, Y).", null,
                new ConfigurationManagerAttributes { Order = 97 }));

            // Layout: Bottom Right Horizontal
            BottomRightColumnWidth = Config.Bind("9 - Layout (Horizontal - Bottom Right)", "ColumnWidth", 265f,
                new ConfigDescription("Column width.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            BottomRightPinSpacing = Config.Bind("9 - Layout (Horizontal - Bottom Right)", "PinSpacing", 10f,
                new ConfigDescription("Spacing between pins.", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            BottomRightPosition = Config.Bind("9 - Layout (Horizontal - Bottom Right)", "Position", new Vector2(-40f, 40f),
                new ConfigDescription("Position (X, Y).", null,
                new ConfigurationManagerAttributes { Order = 97 }));

            // Inventory Behavior
            InventoryGatheringListPosition = Config.Bind("1 - General", "InventoryGatheringListPosition", new Vector2(-1680f, 1150f),
                new ConfigDescription("Gathering list position offset (X, Y) when inventory/chest is open.", null,
                new ConfigurationManagerAttributes { Order = 91 }));

            // Debug
            EnableDebugLogging = Config.Bind("10 - Debug", "EnableDebugLogging", false,
                new ConfigDescription("Enable debug logs.", null,
                new ConfigurationManagerAttributes { Order = 99 }));

            DebugLogger.Log("Config loaded");
        }

        private void Start()
        {
            DebugLogger.Log("Start()");

            LocalizationMgr.LoadTranslations();
            ReadMyLittleUIConfig();
            ContainerMgr.InitializeContainers();

            DebugLogger.Log("Start done");
        }

        private void OnDestroy()
        {
            DebugLogger.Log("OnDestroy");

            if (Player.m_localPlayer != null) DataMgr.SavePins();

            RecipeMgr.Cleanup();
        }

        private void Update()
        {
            if (!EnableMod.Value) return;

            ReflectionHelper.UpdateGuiScale();

            if (!_startupInitialized && Player.m_localPlayer != null && ObjectDB.instance != null && ObjectDB.instance.m_recipes.Count > 0)
            {
                DebugLogger.Log("First init");
                _lastLanguage = Localization.instance.GetSelectedLanguage();
                DataMgr.LoadPins();
                RecipeMgr.ValidateAndCleanPins();
                RecipeMgr.RefreshRecipeCache();
                _startupInitialized = true;
                DebugLogger.Log($"Init done - {RecipeMgr.PinnedRecipes.Count} pins loaded");
            }

            // Container scanning
            if (EnableChestScanning.Value && Player.m_localPlayer != null && RecipeMgr.CachedPins.Count > 0)
            {
                ContainerMgr.UpdateScanning();
            }

            // Toggle visibility hotkey
            if (Input.GetKeyDown(HotkeyToggleVisibility.Value))
            {
                if (!InputHelper.IsInputBlocked())
                {
                    _isUiVisible = !_isUiVisible;
                    DebugLogger.Log($"UI visibility toggled: {_isUiVisible}");
                }
            }

            // Update cache and UI
            if (Player.m_localPlayer != null)
                UpdatePlayerSession();

            // Pin hotkey
            if (Input.GetKeyDown(HotkeyPin.Value))
            {
                if (InventoryGui.instance != null && InventoryGui.IsVisible())
                    RecipeMgr.TryPinHoveredRecipe(InventoryGui.instance);
                else if (Hud.instance != null && Player.m_localPlayer != null && Player.m_localPlayer.InPlaceMode())
                    RecipeMgr.TryPinHoveredPiece();
            }

            // Clear all pins hotkey
            if (Input.GetKeyDown(HotkeyClearAll.Value) && !InputHelper.IsInputBlocked())
            {
                if (RecipeMgr.PinnedRecipes.Count > 0)
                {
                    int count = RecipeMgr.PinnedRecipes.Count;
                    RecipeMgr.PinnedRecipes.Clear();
                    RecipeMgr.RefreshRecipeCache();
                    UIMgr.CloseGatheringList();
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center, LocalizationMgr.GetText("cleared"));
                    DebugLogger.Log($"Cleared {count} pinned recipes");
                }
            }

            // Language change detection
            if (Localization.instance != null)
            {
                string currentLang = Localization.instance.GetSelectedLanguage();
                if (_lastLanguage != currentLang)
                {
                    DebugLogger.Log($"Language changed from {_lastLanguage} to {currentLang}");
                    _lastLanguage = currentLang;
                    LocalizationMgr.LoadTranslations();

                    if (ObjectDB.instance != null) RecipeMgr.RefreshRecipeCache();
                    UIMgr?.DestroyUI();
                }
            }

            if (Input.GetKeyDown(HotkeyPageSwitch.Value))
            {
                if (_isUiVisible && !InputHelper.IsInputBlocked())
                {
                    UIMgr?.CyclePage();
                }
            }

            // Gathering list toggle
            if (Input.GetKeyDown(HotkeyGatheringList.Value) && !InputHelper.IsInputBlocked())
            {
                if (EnableGatheringList.Value)
                    UIMgr?.ToggleGatheringList();
            }
        }

        private void UpdatePlayerSession()
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead()) return;

            string activePlayerName = Player.m_localPlayer.GetPlayerName();

            if (string.IsNullOrEmpty(activePlayerName)) return;

            if (_currentSessionPlayer != activePlayerName)
            {
                DebugLogger.Log($"Player session changed from '{_currentSessionPlayer}' to '{activePlayerName}'");

                RecipeMgr.PinnedRecipes.Clear();
                RecipeMgr.CachedPins.Clear();
                UIMgr.DestroyUI();

                _currentSessionPlayer = activePlayerName;

                if (!string.IsNullOrEmpty(activePlayerName))
                {
                    DataMgr.LoadPins();
                    RecipeMgr.RefreshRecipeCache();
                }
            }

            UIMgr.UpdateUI(_isUiVisible);
        }

        private void ReadMyLittleUIConfig()
        {
            if (!Chainloader.PluginInfos.ContainsKey("shudnal.MyLittleUI"))
            {
                _mluiInstalled = false;
                DebugLogger.Log("MyLittleUI not detected");
                return;
            }

            _mluiInstalled = true;
            _mluiMapListEnabled = true;
            _mluiNoMapListEnabled = true;

            string configPath = Path.Combine(Paths.ConfigPath, "shudnal.MyLittleUI.cfg");
            if (!File.Exists(configPath))
            {
                DebugLogger.Log("MyLittleUI installed but config not found");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(configPath);
                string currentSection = "";

                foreach (string line in lines)
                {
                    string trimmed = line.Trim();

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed;
                        continue;
                    }

                    if (trimmed.StartsWith("Enable"))
                    {
                        bool isEnabled = trimmed.ToLower().Contains("true");

                        if (currentSection == "[Status effects - Map - List]")
                            _mluiMapListEnabled = isEnabled;
                        else if (currentSection == "[Status effects - Nomap - List]")
                            _mluiNoMapListEnabled = isEnabled;
                    }
                }

                DebugLogger.Log($"MyLittleUI Config: MapList={_mluiMapListEnabled}, NoMapList={_mluiNoMapListEnabled}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RecipePinner] Error reading MyLittleUI config: {ex.Message}");
            }
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

        //HARMONY PATCHES

        [HarmonyPatch(typeof(Game), "SavePlayerProfile")]
        [HarmonyPostfix]
        public static void AutoSavePinsHook()
        {
            if (Player.m_localPlayer != null && Instance != null)
            {
                DebugLogger.Log("Auto-saving pins");
                Instance.DataMgr.SavePins();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        [HarmonyPostfix]
        public static void AutoUnpinHook(InventoryGui __instance)
        {
            if (!EnableMod.Value || !AutoUnpinAfterCrafting.Value || Instance == null) return;
            Recipe craftedRecipe = ReflectionHelper.GetCraftRecipe(__instance);

            if (craftedRecipe != null)
            {
                string keyToRemove = null;

                bool isUpgrade = !__instance.m_tabUpgrade.interactable;

                if (isUpgrade)
                {
                    ItemDrop.ItemData upgradeItem = ReflectionHelper.GetCraftUpgradeItem(__instance);
                    if (upgradeItem != null)
                    {
                        string prefabName = craftedRecipe.m_item.name;
                        int currentReadingLevel = upgradeItem.m_quality;
                        int targetLevelKey = currentReadingLevel + 1;

                        keyToRemove = $"{prefabName} ★{targetLevelKey}";
                        DebugLogger.Log($"Upgrade crafted: Unpinning target {keyToRemove} (Base Level: {currentReadingLevel})");
                    }
                }
                else
                    keyToRemove = craftedRecipe.name;

                if (keyToRemove != null && Instance.RecipeMgr.PinnedRecipes.TryGetValue(keyToRemove, out int currentCount))
                {
                    currentCount--;
                    DebugLogger.Log($"Auto-unpin: {keyToRemove}, remaining count: {currentCount}");

                    if (currentCount <= 0)
                    {
                        Instance.RecipeMgr.PinnedRecipes.Remove(keyToRemove);
                        DebugLogger.Log($"Recipe {keyToRemove} fully unpinned");
                    }
                    else
                    {
                        Instance.RecipeMgr.PinnedRecipes[keyToRemove] = currentCount;
                    }

                    Instance.RecipeMgr.RefreshRecipeCache();

                    if (Instance.RecipeMgr.PinnedRecipes.Count < 2)
                        Instance.UIMgr.CloseGatheringList();
                }
            }
        }

        [HarmonyPatch(typeof(Player), "ConsumeResources")]
        [HarmonyPostfix]
        public static void AutoUnpinBuildHook()
        {
            if (Instance == null || !EnableMod.Value || !AutoUnpinAfterCrafting.Value) return;

            Player player = Player.m_localPlayer;
            if (player == null) return;

            PieceTable pieceTable = ReflectionHelper.GetPieceTable(player);

            if (pieceTable == null) return;

            Piece selectedPiece = pieceTable.GetSelectedPiece();
            if (selectedPiece == null) return;

            string pieceName = selectedPiece.name.Replace("(Clone)", "").Trim();

            if (Instance.RecipeMgr.PinnedRecipes.TryGetValue(pieceName, out int buildCount))
            {
                buildCount--;
                DebugLogger.Log($"Auto-unpin (Build): {pieceName}, remaining count: {buildCount}");

                if (buildCount <= 0)
                {
                    Instance.RecipeMgr.PinnedRecipes.Remove(pieceName);
                    DebugLogger.Log($"Build recipe {pieceName} fully unpinned");
                }
                else
                {
                    Instance.RecipeMgr.PinnedRecipes[pieceName] = buildCount;
                }

                Instance.RecipeMgr.RefreshRecipeCache();

                if (Instance.RecipeMgr.PinnedRecipes.Count < 2)
                    Instance.UIMgr.CloseGatheringList();
            }
        }
    }
}