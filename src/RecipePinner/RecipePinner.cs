using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace ValheimRecipePinner
{
    [BepInPlugin("com.Kadrio.RecipePinner", "Recipe Pinner", "1.3.1")]
    public partial class RecipePinnerPlugin : BaseUnityPlugin
    {
        public static RecipePinnerPlugin Instance;

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
        private const float ClearAllConfirmWindow = 2f;
        private float _clearAllArmedUntil = 0f;
        private static bool _isUiVisible = true;
        public static bool IsUiVisible => _isUiVisible;
        internal bool IsPinDataLoaded => _startupInitialized;

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

        private void Start()
        {
            DebugLogger.Log("Start()");

            LocalizationMgr.LoadTranslations();
            ReadMyLittleUIConfig();

            // InitializeContainers only runs if EnableChestScanning is true (guarded internally)
            ContainerMgr.InitializeContainers();

            DebugLogger.Log("Start done");
        }

        private void OnDestroy()
        {
            DebugLogger.Log("OnDestroy");

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer != null && !string.IsNullOrEmpty(localPlayer.GetPlayerName()))
            {
                if (EnableMod == null || !EnableMod.Value)
                {
                    DebugLogger.Verbose("OnDestroy save skipped - mod is disabled");
                }
                else if (!_startupInitialized)
                {
                    DebugLogger.Verbose("OnDestroy save skipped - pin data not loaded yet");
                }
                else
                {
                    DataMgr.SavePins();
                }
            }

            RecipeMgr.Cleanup();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                UIMgr?.ResetMyPinsInputState("application focus restored");
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
            bool recipePinnerHotkeyPressed =
                Input.GetKeyDown(HotkeyToggleVisibility.Value) ||
                Input.GetKeyDown(HotkeyPin.Value) ||
                Input.GetKeyDown(HotkeyClearAll.Value) ||
                Input.GetKeyDown(HotkeyPageSwitch.Value) ||
                Input.GetKeyDown(HotkeyGatheringList.Value);
            bool hotkeysBlocked = recipePinnerHotkeyPressed && AreRecipePinnerHotkeysBlocked();
            if (Input.GetKeyDown(HotkeyToggleVisibility.Value))
            {
                if (!hotkeysBlocked)
                {
                    _isUiVisible = !_isUiVisible;
                    DebugLogger.Log($"UI visibility toggled: {_isUiVisible}");
                }
            }

            // Update cache and UI
            if (Player.m_localPlayer != null)
                UpdatePlayerSession();

            bool inBuildMode = Player.m_localPlayer != null && Player.m_localPlayer.InPlaceMode();

            // Pin hotkey (blocked while modal or non-crafting inventory panels are open)
            if (Input.GetKeyDown(HotkeyPin.Value) && !hotkeysBlocked)
            {
                bool inventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();

                if (inventoryOpen)
                {
                    // In inventory: TogglePin handles shift internally (shift = decrease, no shift = increase)
                    RecipeMgr.TryPinHoveredRecipe(InventoryGui.instance);
                }
                else if (inBuildMode)
                {
                    // In build mode: TogglePin handles shift internally (shift = decrease, no shift = increase)
                    RecipeMgr.TryPinHoveredPiece();
                }
            }

            // Clear all pins hotkey (requires a confirming second press)
            if (Input.GetKeyDown(HotkeyClearAll.Value) && !hotkeysBlocked)
            {
                if (RecipeMgr.PinnedRecipes.Count > 0 || RecipeMgr.PinGroups.Count > 0)
                {
                    if (Time.unscaledTime <= _clearAllArmedUntil)
                    {
                        _clearAllArmedUntil = 0f;

                        int pinCount = RecipeMgr.PinnedRecipes.Count;
                        int groupCount = RecipeMgr.PinGroups.Count;
                        RecipeMgr.PinnedRecipes.Clear();
                        RecipeMgr.PinnedRecipeOrder.Clear();
                        RecipeMgr.PinGroups.Clear();
                        _isUiVisible = true; // Reset visibility so next pin appears immediately
                        RecipeMgr.RefreshRecipeCache();
                        UIMgr.CloseGatheringList();
                        UIMgr.RefreshMyPinsList();
                        DataMgr.SavePins();
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, LocalizationMgr.GetText("cleared"));
                        DebugLogger.Log($"Cleared {pinCount} pinned recipes and {groupCount} groups");
                    }
                    else
                    {
                        _clearAllArmedUntil = Time.unscaledTime + ClearAllConfirmWindow;
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                            LocalizationMgr.GetText("clear_confirm_hotkey"));
                        DebugLogger.Log("Clear-all armed - press again to confirm");
                    }
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
                    UIMgr?.DestroyMyPinsUI(); // DestroyUI skips the My Pins panel, so its labels would keep the old language
                }
            }

            if (Input.GetKeyDown(HotkeyPageSwitch.Value))
            {
                if (_isUiVisible && !hotkeysBlocked)
                {
                    UIMgr?.CyclePage();
                }
            }


            // Gathering list toggle
            if (Input.GetKeyDown(HotkeyGatheringList.Value) && !hotkeysBlocked)
            {
                if (EnableGatheringList.Value)
                    UIMgr?.ToggleGatheringList();
            }

            // My Pins button/panel lifecycle
            if (Player.m_localPlayer != null)
                UIMgr?.UpdateMyPinsInventoryState();
        }

        private bool AreRecipePinnerHotkeysBlocked()
        {
            if (InputHelper.IsInputBlocked())
                return true;

            if (UIMgr != null && UIMgr.IsMyPinsPanelOpen)
                return true;

            if (ControlsInfoPanel.IsOpen)
                return true;

            InventoryGui gui = InventoryGui.instance;
            if (gui != null && InventoryGui.IsVisible() && ReflectionHelper.IsBlockingInventoryPanelOpen(gui))
                return true;

            return false;
        }

        private void UpdatePlayerSession()
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead())
            {
                UIMgr?.UpdateUI(false);
                return;
            }

            string activePlayerName = Player.m_localPlayer.GetPlayerName();

            if (string.IsNullOrEmpty(activePlayerName)) return;

            if (_currentSessionPlayer != activePlayerName)
            {
                DebugLogger.Log($"Player session changed from '{_currentSessionPlayer}' to '{activePlayerName}'");

                RecipeMgr.PinnedRecipes.Clear();
                RecipeMgr.PinnedRecipeOrder.Clear();
                RecipeMgr.CachedPins.Clear();
                RecipeMgr.PinGroups.Clear();
                UIMgr.DestroyUI();
                UIMgr.DestroyMyPinsUI(); // Full destroy on session change

                _currentSessionPlayer = activePlayerName;

                DataMgr.LoadPins();
                if (ObjectDB.instance != null && ObjectDB.instance.m_recipes.Count > 0)
                    RecipeMgr.ValidateAndCleanPins();
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

                    if (TryReadBoolConfigValue(trimmed, "Enable", out bool isEnabled))
                    {
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
                DebugLogger.Error($"Error reading MyLittleUI config", ex);
            }
        }

        private static bool TryReadBoolConfigValue(string line, string key, out bool value)
        {
            value = false;

            int equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
                return false;

            string parsedKey = line.Substring(0, equalsIndex).Trim();
            if (!string.Equals(parsedKey, key, System.StringComparison.OrdinalIgnoreCase))
                return false;

            string parsedValue = line.Substring(equalsIndex + 1);
            int commentIndex = parsedValue.IndexOf('#');
            if (commentIndex >= 0)
                parsedValue = parsedValue.Substring(0, commentIndex);

            return bool.TryParse(parsedValue.Trim(), out value);
        }

        // ============================================================
        // HARMONY PATCHES
        // ============================================================

        [HarmonyPatch(typeof(Game), "SavePlayerProfile")]
        [HarmonyPostfix]
        public static void AutoSavePinsHook()
        {
            if (Player.m_localPlayer == null || Instance == null) return;

            if (EnableMod == null || !EnableMod.Value)
            {
                DebugLogger.Verbose("Auto-save skipped - mod is disabled");
                return;
            }

            if (!Instance.IsPinDataLoaded)
            {
                DebugLogger.Verbose("Auto-save skipped - pin data not loaded yet");
                return;
            }

            DebugLogger.Log("Auto-saving pins");
            Instance.DataMgr.SavePins();
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
                    if (upgradeItem != null && craftedRecipe.m_item != null)
                    {
                        string prefabName = craftedRecipe.m_item.name;
                        int currentReadingLevel = upgradeItem.m_quality;
                        int targetLevelKey = currentReadingLevel + 1;

                        keyToRemove = $"{prefabName} ★{targetLevelKey}";
                        DebugLogger.Log($"Upgrade crafted: Unpinning target {keyToRemove} (Base Level: {currentReadingLevel})");
                    }
                }
                else
                    keyToRemove = Instance.RecipeMgr.BuildRecipeKey(craftedRecipe);

                if (keyToRemove != null && Instance.RecipeMgr.PinnedRecipes.TryGetValue(keyToRemove, out int currentCount))
                {
                    // Consume an ungrouped excess copy first; only touch group claims when every
                    // remaining copy belongs to a group. Compared BEFORE the decrement below.
                    bool hasUngroupedExcess = currentCount > Instance.RecipeMgr.GetGroupClaimCount(keyToRemove);

                    currentCount--;
                    DebugLogger.Log($"Auto-unpin: {keyToRemove}, remaining count: {currentCount}");

                    if (hasUngroupedExcess)
                    {
                        DebugLogger.Verbose($"Auto-unpin: consumed ungrouped copy of '{keyToRemove}', group claims untouched");
                    }
                    else
                    {
                        Instance.RecipeMgr.DecrementGroupMemberCounts(keyToRemove);
                    }

                    // Re-read claim count after group decrement (groups may have changed)
                    if (currentCount <= 0)
                    {
                        Instance.RecipeMgr.PinnedRecipes.Remove(keyToRemove);
                        Instance.RecipeMgr.PinnedRecipeOrder.Remove(keyToRemove);
                        DebugLogger.Log($"Recipe {keyToRemove} fully unpinned");
                    }
                    else
                    {
                        Instance.RecipeMgr.PinnedRecipes[keyToRemove] = currentCount;
                    }

                    Instance.RecipeMgr.RefreshRecipeCache();
                    Instance.DataMgr.SavePins();

                    if (Instance.RecipeMgr.GetEffectivePinCount() < 2)
                        Instance.UIMgr.CloseGatheringList();
                }
                else if (keyToRemove != null)
                {
                    // Key not found — log all current pinned keys to help diagnose mismatches
                    // (e.g. crafting base recipe when an upgrade pin is in PinnedRecipes)
                    string pinnedKeys = string.Join(", ", Instance.RecipeMgr.PinnedRecipes.Keys);
                    DebugLogger.Verbose($"Auto-unpin: '{keyToRemove}' not found in PinnedRecipes. Current keys: [{pinnedKeys}]");
                }
            }
        }

        [HarmonyPatch(typeof(Player), "PlacePiece")]
        [HarmonyPostfix]
        public static void AutoUnpinBuildHook(Piece piece)
        {
            DebugLogger.Verbose("AutoUnpinBuildHook fired (PlacePiece postfix)");

            if (Instance == null || !EnableMod.Value || !AutoUnpinAfterBuilding.Value)
            {
                DebugLogger.Verbose($"AutoUnpinBuildHook early exit: Instance={Instance != null}, EnableMod={EnableMod?.Value}, AutoUnpin={AutoUnpinAfterBuilding?.Value}");
                return;
            }

            if (piece == null) { DebugLogger.Verbose("AutoUnpinBuildHook: piece is null"); return; }

            string pieceName = piece.name.Replace("(Clone)", "").Trim();
            if (!Instance.RecipeMgr.PinnedRecipes.TryGetValue(pieceName, out int buildCount))
            {
                DebugLogger.Verbose($"AutoUnpinBuildHook: '{pieceName}' not pinned");
                return;
            }

            bool hasUngroupedExcess = buildCount > Instance.RecipeMgr.GetGroupClaimCount(pieceName);

            buildCount--;
            DebugLogger.Log($"Auto-unpin (Build): {pieceName}, remaining count: {buildCount}");

            if (hasUngroupedExcess)
            {
                DebugLogger.Verbose($"Auto-unpin (Build): consumed ungrouped copy of '{pieceName}', group claims untouched");
            }
            else
            {
                Instance.RecipeMgr.DecrementGroupMemberCounts(pieceName);
            }

            if (buildCount <= 0)
            {
                Instance.RecipeMgr.PinnedRecipes.Remove(pieceName);
                Instance.RecipeMgr.PinnedRecipeOrder.Remove(pieceName);
                DebugLogger.Log($"Build recipe {pieceName} fully unpinned");
            }
            else
            {
                Instance.RecipeMgr.PinnedRecipes[pieceName] = buildCount;
            }

            Instance.RecipeMgr.RefreshRecipeCache();
            Instance.DataMgr.SavePins();

            if (Instance.RecipeMgr.GetEffectivePinCount() < 2)
                Instance.UIMgr.CloseGatheringList();
        }

        // ============================================================
        // INPUT BLOCKING PATCHES (GroupNameDialog + MyPins Modal)
        // ============================================================

        /// <summary>
        /// Blocks ALL player input (movement, emotes, actions, etc.)
        /// while a modal dialog (GroupNameDialog or ConfirmDialog) is open.
        /// Player.TakeInput is the central input gate for the player character.
        /// </summary>
        [HarmonyPatch(typeof(Player), "TakeInput")]
        [HarmonyPrefix]
        public static bool Player_TakeInput_BlockDuringDialog()
        {
            if (GroupNameDialog.IsDialogOpen || ConfirmDialog.IsDialogOpen)
                return false; // Skip original - no player input while a modal dialog is open

            if (Instance?.UIMgr != null && Instance.UIMgr.IsMyPinsPanelOpen)
                return false; // My Pins is modal; block player input behind the panel

            return true;
        }

        /// <summary>
        /// Prevents inventory from closing while a modal panel is active.
        /// 1. GroupNameDialog or ConfirmDialog open → block completely
        /// 2. My Pins panel open → close panel first, block inventory close
        ///    (mimics Valheim's Escape behavior for Skills/Trophies/Compendium)
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), "Hide")]
        [HarmonyPrefix]
        public static bool InventoryGui_Hide_BlockDuringDialog()
        {
            if (GroupNameDialog.IsDialogOpen)
            {
                DebugLogger.Verbose("InventoryGui.Hide blocked - GroupNameDialog is open");
                return false;
            }

            if (ConfirmDialog.IsDialogOpen)
            {
                DebugLogger.Verbose("InventoryGui.Hide blocked - ConfirmDialog is open");
                return false;
            }

            // If the controls info panel is open:
            //   ESC → close controls panel only, keep inventory open
            if (ControlsInfoPanel.IsOpen)
            {
                ControlsInfoPanel.Instance?.Hide();
                DebugLogger.Log("InventoryGui.Hide intercepted (ESC) - closing ControlsInfoPanel only");
                return false;
            }

            // If My Pins panel is open:
            //   ESC → close panel only, keep inventory open
            //   Tab → close panel AND inventory
            if (Instance?.UIMgr != null && Instance.UIMgr.IsMyPinsPanelOpen)
            {
                bool isEscape = Input.GetKeyDown(KeyCode.Escape);
                Instance.UIMgr.ToggleMyPinsPanel();

                if (isEscape)
                {
                    DebugLogger.Log("InventoryGui.Hide intercepted (ESC) - closing My Pins panel only");
                    return false; // Block inventory close
                }
                else
                {
                    DebugLogger.Log("InventoryGui.Hide intercepted (Tab) - closing My Pins panel + inventory");
                    return true; // Let inventory close too
                }
            }

            return true;
        }
    }
}
