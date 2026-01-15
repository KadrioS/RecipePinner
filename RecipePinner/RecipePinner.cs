using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace ValheimRecipePinner
{
    [BepInPlugin("com.Kadrio.RecipePinner", "Recipe Pinner", "1.0.2")]
    public class RecipePinnerPlugin : BaseUnityPlugin
    {
        public static RecipePinnerPlugin Instance;

        // --- 1. GENERAL SETTINGS ---
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<string> LanguageOverride;
        public static ConfigEntry<PinLayoutMode> LayoutModeConfig;
        public static ConfigEntry<int> MaximumPins;
        public static ConfigEntry<bool> AutoUnpinAfterCrafting;

        // --- 2. CONTROLS / HOTKEYS ---
        public static ConfigEntry<KeyCode> HotkeyPin;
        public static ConfigEntry<KeyCode> HotkeyClearAll;
        public static ConfigEntry<KeyCode> HotkeyToggleVisibility;

        // --- 3. CRAFT FROM CHEST ---
        public static ConfigEntry<bool> EnableChestScanning;
        public static ConfigEntry<float> ChestScanRange;
        public static ConfigEntry<float> ChestScanInterval;

        // --- 4. VISUAL SETTINGS ---
        public static ConfigEntry<float> UIScale;
        public static ConfigEntry<int> FontSizeRecipeName;
        public static ConfigEntry<int> FontSizeMaterials;
        public static ConfigEntry<float> BackgroundOpacity;
        public static ConfigEntry<Color> ColorHeader;
        public static ConfigEntry<Color> ColorEnoughInInventory;
        public static ConfigEntry<Color> ColorEnoughWithChests;
        public static ConfigEntry<Color> ColorMissing;

        // --- 5. LAYOUT: VERTICAL MODE ---
        public static ConfigEntry<float> VerticalListWidth;
        public static ConfigEntry<float> VerticalPinSpacing;
        public static ConfigEntry<Vector2> VerticalPosition;

        // --- 6. LAYOUT: HORIZONTAL MODE ---
        public static ConfigEntry<float> HorizontalColumnWidth;
        public static ConfigEntry<float> HorizontalPinSpacing;
        public static ConfigEntry<Vector2> HorizontalPosition;

        // --- 7. LAYOUT: BOTTOM RIGHT HORIZONTAL ---
        public static ConfigEntry<float> BottomRightColumnWidth;
        public static ConfigEntry<float> BottomRightPinSpacing;
        public static ConfigEntry<Vector2> BottomRightPosition;

        // --- 8. DEBUG SETTINGS ---
        public static ConfigEntry<bool> EnableDebugLogging;

        // --- MANAGERS ---
        public LocalizationManager LocalizationMgr;
        public RecipeManager RecipeMgr;
        public ContainerScanner ContainerMgr;
        public UIManager UIMgr;
        public DataPersistence DataMgr;

        // --- HELPER & MLUI DETECTION ---
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

            DebugLogger.Log("RecipePinner plugin initializing...");

            // Initialize managers
            LocalizationMgr = new LocalizationManager(this);
            RecipeMgr = new RecipeManager();
            ContainerMgr = new ContainerScanner();
            UIMgr = new UIManager();
            DataMgr = new DataPersistence();

            DebugLogger.Log("All managers initialized successfully");

            Harmony harmony = new Harmony("com.Kadrio.RecipePinner");
            harmony.PatchAll(typeof(RecipePinnerPlugin));
            harmony.PatchAll(typeof(ContainerScanner));

            DebugLogger.Log("Harmony patches applied successfully");
        }

        private void BindConfigs()
        {
            // --- 1. GENERAL ---
            EnableMod = Config.Bind("1 - General", "EnableMod", true,
                new ConfigDescription("Enable or disable the mod completely.", null,
                new ConfigurationManagerAttributes { Order = 99 }));
            EnableMod.SettingChanged += (s, e) => { if (!EnableMod.Value) UIMgr?.DestroyUI(); };

            LanguageOverride = Config.Bind("1 - General", "LanguageOverride", "Auto",
                new ConfigDescription("Force a specific language (e.g., 'German', 'Turkish').", null,
                new ConfigurationManagerAttributes { Order = 98 }));
            LanguageOverride.SettingChanged += (s, e) => { LocalizationMgr?.LoadTranslations(); RecipeMgr?.RefreshRecipeCache(); };

            LayoutModeConfig = Config.Bind("1 - General", "LayoutMode", PinLayoutMode.AutoDetect,
                new ConfigDescription("Choose layout position.", null,
                new ConfigurationManagerAttributes { Order = 97 }));
            LayoutModeConfig.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            MaximumPins = Config.Bind("1 - General", "MaximumPins", 5,
                new ConfigDescription("Max pins allowed.", new AcceptableValueRange<int>(1, 20),
                new ConfigurationManagerAttributes { Order = 96 }));
            MaximumPins.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            AutoUnpinAfterCrafting = Config.Bind("1 - General", "AutoUnpinAfterCrafting", true,
                new ConfigDescription("Unpin after crafting.", null,
                new ConfigurationManagerAttributes { Order = 95 }));

            // --- 2. CONTROLS ---
            HotkeyPin = Config.Bind("2 - Controls", "HotkeyPin", KeyCode.Mouse2, "Key to pin recipe.");
            HotkeyClearAll = Config.Bind("2 - Controls", "HotkeyClearAll", KeyCode.P, "Key to clear all pins.");
            HotkeyToggleVisibility = Config.Bind("2 - Controls", "HotkeyToggleVisibility", KeyCode.F7, "Key to toggle overlay.");

            // --- 3. CHEST SCANNER) ---
            EnableChestScanning = Config.Bind("3 - Chest Scanner", "EnableChestScanning", false,
                new ConfigDescription("Count materials in nearby chests.", null, new ConfigurationManagerAttributes { Order = 99 }));
            EnableChestScanning.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ChestScanRange = Config.Bind("3 - Chest Scanner", "ChestScanRange", 20f,
                new ConfigDescription("Scan radius.", new AcceptableValueRange<float>(5f, 100f), new ConfigurationManagerAttributes { Order = 98 }));

            ChestScanInterval = Config.Bind("3 - Chest Scanner", "ChestScanInterval", 3.0f,
                new ConfigDescription("Scan frequency.", new AcceptableValueRange<float>(0.5f, 10f), new ConfigurationManagerAttributes { Order = 97 }));

            // --- 4. VISUALS ---
            UIScale = Config.Bind("4 - Visual Settings", "UIScale", 0.75f,
                new ConfigDescription("Global UI scale.", new AcceptableValueRange<float>(0.3f, 3.0f)));
            UIScale.SettingChanged += (s, e) => UIMgr?.DestroyUI();

            BackgroundOpacity = Config.Bind("4 - Visual Settings", "BackgroundOpacity", 0.45f,
                new ConfigDescription("Background opacity.", new AcceptableValueRange<float>(0f, 1f)));

            FontSizeRecipeName = Config.Bind("4 - Visual Settings", "FontSizeRecipeName", 15, "Recipe name font size.");
            FontSizeRecipeName.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            FontSizeMaterials = Config.Bind("4 - Visual Settings", "FontSizeMaterials", 15, "Material font size.");
            FontSizeMaterials.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorHeader = Config.Bind("4 - Visual Settings", "ColorHeader", new Color(1f, 0.77f, 0.31f, 1f), "Recipe title color.");
            ColorHeader.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorEnoughInInventory = Config.Bind("4 - Visual Settings", "ColorEnoughInInventory", new Color(0f, 1f, 0f, 1f), "Color: Enough in inventory.");
            ColorEnoughInInventory.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorEnoughWithChests = Config.Bind("4 - Visual Settings", "ColorEnoughWithChests", new Color(1f, 1f, 0f, 1f), "Color: Enough with chests.");
            ColorEnoughWithChests.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            ColorMissing = Config.Bind("4 - Visual Settings", "ColorMissing", new Color(1f, 0.33f, 0.33f, 1f), "Color: Missing materials.");
            ColorMissing.SettingChanged += (s, e) => RecipeMgr?.RefreshRecipeCache();

            // --- 5. LAYOUT: VERTICAL ---
            VerticalListWidth = Config.Bind("5 - Layout (Vertical Mode)", "ListWidth", 265f, "List width.");
            VerticalPinSpacing = Config.Bind("5 - Layout (Vertical Mode)", "PinSpacing", 10f, "Spacing between pins.");
            VerticalPosition = Config.Bind("5 - Layout (Vertical Mode)", "Position", new Vector2(-40f, -250f), "Position (X, Y).");

            // --- 6. LAYOUT: HORIZONTAL ---
            HorizontalColumnWidth = Config.Bind("6 - Layout (Horizontal - Map Side)", "ColumnWidth", 250f, "Column width.");
            HorizontalPinSpacing = Config.Bind("6 - Layout (Horizontal - Map Side)", "PinSpacing", 10f, "Spacing between pins.");
            HorizontalPosition = Config.Bind("6 - Layout (Horizontal - Map Side)", "Position", new Vector2(-250f, -40f), "Position (X, Y).");

            // --- 7. LAYOUT: BOTTOM RIGHT HORIZONTAL ---
            BottomRightColumnWidth = Config.Bind("7 - Layout (Horizontal - Bottom Right)", "ColumnWidth", 250f, "Column width.");
            BottomRightPinSpacing = Config.Bind("7 - Layout (Horizontal - Bottom Right)", "PinSpacing", 10f, "Spacing between pins.");
            BottomRightPosition = Config.Bind("7 - Layout (Horizontal - Bottom Right)", "Position", new Vector2(-40f, 40f), "Position (X, Y).");

            // --- 8. DEBUG ---
            EnableDebugLogging = Config.Bind("8 - Debug", "EnableDebugLogging", false, "Enable debug logs.");

            DebugLogger.Log("Configuration loaded successfully");
        }

        private void Start()
        {
            DebugLogger.Log("Start() called - Loading translations and initializing containers");

            LocalizationMgr.LoadTranslations();
            ReadMyLittleUIConfig();
            ContainerMgr.InitializeContainers();

            DebugLogger.Log("Start() completed successfully");
        }

        private void OnDestroy()
        {
            DebugLogger.Log("Plugin destroyed - Saving data and cleaning up");
            DataMgr.SavePins();
            RecipeMgr.Cleanup();
        }

        private void Update()
        {
            if (!EnableMod.Value) return;

            ReflectionHelper.UpdateGuiScale();

            // Startup initialization
            if (!_startupInitialized && Player.m_localPlayer != null && ObjectDB.instance != null && ObjectDB.instance.m_recipes.Count > 0)
            {
                DebugLogger.Log("First-time initialization triggered");
                _lastLanguage = Localization.instance.GetSelectedLanguage();
                DataMgr.LoadPins();
                RecipeMgr.ValidateAndCleanPins();
                RecipeMgr.RefreshRecipeCache();
                _startupInitialized = true;
                DebugLogger.Log($"Initialization complete - {RecipeMgr.PinnedRecipes.Count} recipes loaded");
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
            if (Input.GetKeyDown(HotkeyClearAll.Value))
            {
                if (InputHelper.IsInputBlocked()) return;
                if (RecipeMgr.PinnedRecipes.Count > 0)
                {
                    int count = RecipeMgr.PinnedRecipes.Count;
                    RecipeMgr.PinnedRecipes.Clear();
                    RecipeMgr.RefreshRecipeCache();
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
                    RecipeMgr.RefreshRecipeCache();
                }
            }
        }

        private void UpdatePlayerSession()
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead()) return;

            string activePlayerName = Player.m_localPlayer.GetPlayerName();

            if (_currentSessionPlayer != activePlayerName)
            {
                DebugLogger.Log($"Player session changed from '{_currentSessionPlayer}' to '{activePlayerName}'");

                RecipeMgr.PinnedRecipes.Clear();
                RecipeMgr.CachedPins.Clear();
                UIMgr.DestroyUI();

                _currentSessionPlayer = activePlayerName;
                DataMgr.LoadPins();
                RecipeMgr.RefreshRecipeCache();
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

        // ==================== HARMONY PATCHES ====================

        [HarmonyPatch(typeof(Game), "SavePlayerProfile")]
        [HarmonyPostfix]
        public static void AutoSavePinsHook()
        {
            if (Player.m_localPlayer != null && Instance != null)
            {
                DebugLogger.Log("Auto-saving pins on profile save");
                Instance.DataMgr.SavePins();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        [HarmonyPostfix]
        public static void AutoUnpinHook(InventoryGui __instance)
        {
            if (!EnableMod.Value || !AutoUnpinAfterCrafting.Value || Instance == null) return;

            Recipe craftedRecipe = ReflectionHelper.GetCraftRecipe(__instance);

            if (craftedRecipe != null && Instance.RecipeMgr.PinnedRecipes.ContainsKey(craftedRecipe.name))
            {
                Instance.RecipeMgr.PinnedRecipes[craftedRecipe.name]--;

                DebugLogger.Log($"Auto-unpin: {craftedRecipe.name}, remaining count: {Instance.RecipeMgr.PinnedRecipes[craftedRecipe.name]}");

                if (Instance.RecipeMgr.PinnedRecipes[craftedRecipe.name] <= 0)
                {
                    Instance.RecipeMgr.PinnedRecipes.Remove(craftedRecipe.name);
                    DebugLogger.Log($"Recipe {craftedRecipe.name} fully unpinned");
                }

                Instance.RecipeMgr.RefreshRecipeCache();
            }
        }
    }
}