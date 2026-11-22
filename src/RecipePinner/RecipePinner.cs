using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimRecipePinner
{
    [BepInPlugin("com.Kadrio.RecipePinner", "Recipe Pinner", "1.6.0")]
    public partial class RecipePinnerPlugin : BaseUnityPlugin
    {
        public static RecipePinnerPlugin Instance;

        // Managers
        public LocalizationManager LocalizationMgr;
        public RecipeManager RecipeMgr;
        public ContainerScanner ContainerMgr;
        public UIManager UIMgr;
        public DataPersistence DataMgr;

        // MLUI compat. The entries are MyLittleUI's own, so these follow its live value; a missing
        // entry reads as enabled, which is the right guess for most MyLittleUI users.
        private ConfigEntry<bool> _mluiMapListEntry;
        private ConfigEntry<bool> _mluiNoMapListEntry;
        internal bool _mluiMapListEnabled => _mluiMapListEntry == null || _mluiMapListEntry.Value;
        internal bool _mluiNoMapListEnabled => _mluiNoMapListEntry == null || _mluiNoMapListEntry.Value;
        internal bool _mluiInstalled = false;


        private bool _startupInitialized = false;
        private string _lastLanguage = "";
        private string _currentSessionPlayer = null;
        // The character whose pins are in memory and were read from disk successfully. Null until a
        // read succeeds, and again once the player is gone; SavePins refuses to write without it.
        private string _pinsLoadedFor = null;
        private const float LoadRetryInterval = 5f;
        private float _nextLoadRetry = 0f;
        private const float ClearAllConfirmWindow = 2f;
        private float _clearAllArmedUntil = 0f;
        private const float MaximumPinsSettleDelay = 1f;
        private bool _maximumPinsTrimPending = false;
        private float _maximumPinsChangedAt = 0f;
        private static bool _isUiVisible = true;
        public static bool IsUiVisible => _isUiVisible;
        internal bool IsPinDataLoaded
        {
            get
            {
                if (_pinsLoadedFor == null) return false;
                Player player = Player.m_localPlayer;
                return player != null && player.GetPlayerName() == _pinsLoadedFor;
            }
        }

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

            // Record the language the translations were just loaded for. Without this the change
            // detection in Update() sees "" on its first frame and fires a language change that
            // never happened, reloading everything once per launch. Only when Localization is up:
            // if it is not, LoadTranslations fell back to English and the detection should still
            // fire once the real language becomes available.
            if (Localization.instance != null)
                _lastLanguage = Localization.instance.GetSelectedLanguage();

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
            // The plugin outlives the world. Once the player is gone the pins in memory belong to
            // nobody, and the next character to enter - the same one included - must read its file.
            if (Player.m_localPlayer == null && _currentSessionPlayer != null)
            {
                DebugLogger.Log($"Player '{_currentSessionPlayer}' left - pin session closed");
                _currentSessionPlayer = null;
                _pinsLoadedFor = null;
            }

            if (!EnableMod.Value) return;

            ReflectionHelper.UpdateGuiScale();

            if (!_startupInitialized && Player.m_localPlayer != null && ObjectDB.instance != null && ObjectDB.instance.m_recipes.Count > 0)
            {
                DebugLogger.Log("First init");
                _lastLanguage = Localization.instance.GetSelectedLanguage();
                LoadPinsForPlayer(Player.m_localPlayer.GetPlayerName());
                RecipeMgr.ValidateAndCleanPins();
                RecipeMgr.RefreshRecipeCache();
                _startupInitialized = true;
                DebugLogger.Log($"Init done - {RecipeMgr.PinnedRecipes.Count} pins loaded");
            }

            ApplyPendingMaximumPinsTrim();

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
            // With no player the pins in memory are a previous character's; no hotkey may act on them.
            bool hotkeysBlocked = recipePinnerHotkeyPressed && (Player.m_localPlayer == null || AreRecipePinnerHotkeysBlocked());
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
                    KeyHintInjector.RefreshKeys();

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

        private void ApplyPendingMaximumPinsTrim()
        {
            if (!_maximumPinsTrimPending)
                return;

            // With no character's pins loaded there is nothing to trim; LoadPins trims to the cap itself.
            if (!IsPinDataLoaded)
            {
                _maximumPinsTrimPending = false;
                return;
            }

            // A pause in a drag is not a settled value: wait for the button and the config window too.
            if (Input.GetMouseButton(0) || TextInput.IsVisible())
            {
                _maximumPinsChangedAt = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _maximumPinsChangedAt < MaximumPinsSettleDelay)
                return;

            _maximumPinsTrimPending = false;

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

                LoadPinsForPlayer(activePlayerName);
                if (ObjectDB.instance != null && ObjectDB.instance.m_recipes.Count > 0)
                    RecipeMgr.ValidateAndCleanPins();
                RecipeMgr.RefreshRecipeCache();
            }
            else if (_pinsLoadedFor != activePlayerName && Time.unscaledTime >= _nextLoadRetry)
            {
                // The last read of this character's file failed, and nothing has been saved since, so
                // the file still holds the real pins. A successful read replaces memory by itself.
                LoadPinsForPlayer(activePlayerName);
                if (_pinsLoadedFor == activePlayerName)
                {
                    DebugLogger.Log($"Pins for '{activePlayerName}' loaded on retry");
                    if (ObjectDB.instance != null && ObjectDB.instance.m_recipes.Count > 0)
                        RecipeMgr.ValidateAndCleanPins();
                    RecipeMgr.RefreshRecipeCache();
                }
            }

            UIMgr.UpdateUI(_isUiVisible);
        }

        private void LoadPinsForPlayer(string playerName)
        {
            if (DataMgr.LoadPins())
            {
                _pinsLoadedFor = playerName;
            }
            else
            {
                _pinsLoadedFor = null;
                _nextLoadRetry = Time.unscaledTime + LoadRetryInterval;
                DebugLogger.Warning($"Pins for '{playerName}' could not be loaded - saving is off until a load succeeds, retrying in {LoadRetryInterval:0} s");
            }
        }

        private void ReadMyLittleUIConfig()
        {
            if (!Chainloader.PluginInfos.TryGetValue("shudnal.MyLittleUI", out PluginInfo mlui) || mlui.Instance == null)
            {
                _mluiInstalled = false;
                DebugLogger.Log("MyLittleUI not detected");
                return;
            }

            _mluiInstalled = true;

            // Not the cfg file: BepInEx keeps a renamed section there as an orphan with the old
            // value, and the file never holds the value a server syncs. The entries hold both right.
            ConfigFile config = mlui.Instance.Config;
            if (!config.TryGetEntry("Status effects - Map - List", "Enable", out _mluiMapListEntry))
                DebugLogger.Warning("MyLittleUI has no [Status effects - Map - List] Enable setting; assuming that list is enabled");
            if (!config.TryGetEntry("Status effects - Nomap - List", "Enable", out _mluiNoMapListEntry))
                DebugLogger.Warning("MyLittleUI has no [Status effects - Nomap - List] Enable setting; assuming that list is enabled");

            DebugLogger.Log($"MyLittleUI Config: MapList={_mluiMapListEnabled}, NoMapList={_mluiNoMapListEnabled}");
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

        // An upgrade attempt at the Forge of Potential has three outcomes and this postfix cannot
        // see which one ran: the item can come back one level higher, come back one level LOWER,
        // or be destroyed outright. The game puts the item back into the slot it came from, so
        // that slot holding the target level is the only available proof that the upgrade
        // succeeded. RemoveItem does not clear m_gridPos, so the detached item still knows where
        // it was.
        private static bool UpgradeSucceeded(ItemDrop.ItemData upgradeItem, int targetLevel)
        {
            if (upgradeItem == null || upgradeItem.m_shared == null) return false;

            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null) return false;

            Inventory inventory = localPlayer.GetInventory();
            if (inventory == null) return false;

            ItemDrop.ItemData inSlot = inventory.GetItemAt(upgradeItem.m_gridPos.x, upgradeItem.m_gridPos.y);
            return inSlot != null
                && inSlot.m_shared != null
                && inSlot.m_shared.m_name == upgradeItem.m_shared.m_name
                && inSlot.m_quality == targetLevel;
        }

        // CraftsOrUpgrades rises by one per DoCrafting that made something; Crafts rises by the batch
        // count of a multi-craft and not at all for an upgrade.
        public struct CraftCounters
        {
            public float CraftsOrUpgrades;
            public float Crafts;
        }

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        [HarmonyPrefix]
        public static void AutoUnpinCraftCountHook(out CraftCounters __state)
        {
            __state = ReadCraftCounters();
        }

        // NaN when there is nothing to read; the postfix then leaves every pin alone. Slot 0 is the
        // copy IncrementStat always writes; GetStat's signature differs between game versions.
        // Never throws: an exception in a DoCrafting prefix would stop the craft itself.
        private static CraftCounters ReadCraftCounters()
        {
            CraftCounters unread = new CraftCounters { CraftsOrUpgrades = float.NaN, Crafts = float.NaN };
            try
            {
                if (Game.instance == null) return unread;
                PlayerProfile profile = Game.instance.GetPlayerProfile();
                if (profile == null || profile.m_playerStats == null || profile.m_playerStats.Length == 0) return unread;
                return new CraftCounters
                {
                    CraftsOrUpgrades = profile.m_playerStats[0][PlayerStatType.CraftsOrUpgrades],
                    Crafts = profile.m_playerStats[0][PlayerStatType.Crafts]
                };
            }
            catch (System.Exception ex)
            {
                DebugLogger.Error("Could not read the craft count", ex);
                return unread;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        [HarmonyPostfix]
        public static void AutoUnpinHook(InventoryGui __instance, CraftCounters __state)
        {
            if (!EnableMod.Value || !AutoUnpinAfterCrafting.Value || Instance == null) return;

            // DoCrafting can return early during the craft delay - a material dropped, a full
            // inventory, a moved upgrade item - and this postfix runs either way.
            CraftCounters countsAfter = ReadCraftCounters();
            if (float.IsNaN(__state.CraftsOrUpgrades) || float.IsNaN(countsAfter.CraftsOrUpgrades)
                || countsAfter.CraftsOrUpgrades <= __state.CraftsOrUpgrades)
            {
                DebugLogger.Log("DoCrafting finished without crafting; leaving pins alone");
                return;
            }
            Recipe craftedRecipe = ReflectionHelper.GetCraftRecipe(__instance);

            if (craftedRecipe != null)
            {
                List<string> keysToRemove = new List<string>();

                bool isUpgrade = !__instance.m_tabUpgrade.interactable;
                int batches = 1;

                if (isUpgrade)
                {
                    ItemDrop.ItemData upgradeItem = ReflectionHelper.GetCraftUpgradeItem(__instance);
                    if (upgradeItem != null && craftedRecipe.m_item != null)
                    {
                        string prefabName = craftedRecipe.m_item.name;
                        int currentReadingLevel = upgradeItem.m_quality;
                        int targetLevelKey = currentReadingLevel + 1;

                        // At an upgrader station the craft can fail or destroy the item, and this
                        // postfix runs either way. Unpin nothing unless the result proves the
                        // upgrade happened: a stale row costs the player far less than a pin they
                        // never removed. An ordinary station has no such branch - reaching here
                        // already means it worked.
                        Player craftingPlayer = Player.m_localPlayer;
                        CraftingStation station = (craftingPlayer == null) ? null : craftingPlayer.GetCurrentCraftingStation();
                        if (station != null && station.m_upgrader && !UpgradeSucceeded(upgradeItem, targetLevelKey))
                        {
                            DebugLogger.Log($"Upgrade attempt on {prefabName} did not produce level {targetLevelKey}; leaving pins alone");
                            return;
                        }

                        // Both routes aimed at this level are settled now, so neither key stays.
                        keysToRemove.Add($"{prefabName} ★{targetLevelKey}");
                        keysToRemove.Add($"{prefabName} ★{targetLevelKey}F");
                        DebugLogger.Log($"Upgrade crafted: Unpinning target {prefabName} ★{targetLevelKey} (Base Level: {currentReadingLevel})");
                    }
                }
                else
                {
                    string craftedKey = Instance.RecipeMgr.BuildRecipeKey(craftedRecipe);
                    if (craftedKey != null) keysToRemove.Add(craftedKey);

                    float craftsDelta = countsAfter.Crafts - __state.Crafts;
                    if (!float.IsNaN(craftsDelta) && craftsDelta > 1f)
                        batches = Mathf.RoundToInt(craftsDelta);
                }

                foreach (string keyToRemove in keysToRemove)
                {
                    if (Instance.RecipeMgr.PinnedRecipes.TryGetValue(keyToRemove, out int currentCount))
                    {
                        // One copy per batch, each taken as a single craft would take it.
                        for (int batch = 0; batch < batches && currentCount > 0; batch++)
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
                    else
                    {
                        // Key not found — log all current pinned keys to help diagnose mismatches
                        // (e.g. crafting base recipe when an upgrade pin is in PinnedRecipes)
                        string pinnedKeys = string.Join(", ", Instance.RecipeMgr.PinnedRecipes.Keys);
                        DebugLogger.Verbose($"Auto-unpin: '{keyToRemove}' not found in PinnedRecipes. Current keys: [{pinnedKeys}]");
                    }
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
        /// Blocks player input while a modal dialog (GroupNameDialog or ConfirmDialog)
        /// or the My Pins panel is open.
        ///
        /// This gate does not cover movement. PlayerController has a TakeInput of its
        /// own that never calls this one, so both patches are needed - see below.
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
        /// Blocks movement while the group-name dialog is taking text.
        ///
        /// PlayerController.FixedUpdate consults PlayerController.TakeInput before it
        /// reads any movement input, and that method never calls Player.TakeInput -
        /// the two gates are independent. Vanilla names its own text surfaces here
        /// (chat, the console, the map's text input, the build menu's search field),
        /// which is why typing in one of them does not walk the character. A field
        /// this mod builds is in none of them, so without this patch a key bound to
        /// movement still reaches the player while the field has focus - auto-run is
        /// the case players hit, because it latches and keeps running.
        ///
        /// The test is whether the mod is swallowing keystrokes, not whether something
        /// modal is on screen, so this is deliberately narrower than the
        /// Player.TakeInput patch above. The My Pins panel and the confirm dialogs
        /// (delete, disband, clear) take no typed text and do not appear here: like
        /// Valheim's own Compendium, Skills and Trophies panels, they let the player
        /// keep walking and auto-running. Only the group-name field, which turns a
        /// movement key into a letter, has a reason to stop the character.
        /// </summary>
        [HarmonyPatch(typeof(PlayerController), "TakeInput")]
        [HarmonyPrefix]
        public static bool PlayerController_TakeInput_BlockDuringDialog()
        {
            if (GroupNameDialog.IsDialogOpen)
                return false; // Skip original - no movement while the name field has focus

            return true;
        }

        /// <summary>
        /// Reports the group-name field as Valheim's own text box while the dialog is open.
        /// TextInput.IsVisible() is what the game and well-behaved mods check before acting on a key,
        /// and a field this mod builds is not a TextInput, so without this they never know it is in use.
        /// </summary>
        [HarmonyPatch(typeof(TextInput), "IsVisible")]
        [HarmonyPostfix]
        public static void TextInput_IsVisible_WhileNaming(ref bool __result)
        {
            if (GroupNameDialog.IsDialogOpen)
                __result = true;
        }

        /// <summary>
        /// Stops a held E from stacking into an open container while the group name is typed.
        /// InventoryGui reads that hold every frame the inventory is visible, behind no text-input check.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), "UseButtonHeld")]
        [HarmonyPostfix]
        public static void InventoryGui_UseButtonHeld_WhileNaming(ref bool __result)
        {
            if (GroupNameDialog.IsDialogOpen)
                __result = false;
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
            // InventoryGui.Update forces Hide every frame for these; blocking it would keep the
            // inventory and our dialogs on screen through a death or a portal.
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene() || localPlayer.IsTeleporting())
            {
                UIManager uiMgr = Instance != null ? Instance.UIMgr : null;
                if (GroupNameDialog.IsDialogOpen || ConfirmDialog.IsDialogOpen || ControlsInfoPanel.IsOpen
                    || (uiMgr != null && uiMgr.IsMyPinsPanelOpen))
                {
                    if (uiMgr != null)
                        uiMgr.CloseMyPinsPanel();
                    DebugLogger.Log("InventoryGui.Hide forced - My Pins panel and dialogs closed");
                }
                return true;
            }

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
