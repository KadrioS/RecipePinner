using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public partial class UIManager
    {
        private Transform _pinListRoot;
        private readonly List<PinSlotUI> _pinPool = new List<PinSlotUI>();
        private Font _cachedFont;
        private bool _warnedNoHud = false;
        private readonly Dictionary<string, int> _reusableInvCounts = new Dictionary<string, int>();
        private int _invCountsSignature = 0;
        private bool _invCountsBuilt = false;

        private const float DefaultVerticalPositionX = -40f;
        private const float DefaultVerticalPositionY = -250f;
        private const float MluMapListDisabledVerticalY = -275f;
        private const float MluNoMapListDisabledVerticalY = -75f;

        private int _currentPage = 0;
        public int CurrentPage => _currentPage;
        private GameObject _paginationRoot;



        public void DestroyUI()
        {
            DebugLogger.Verbose("DestroyUI");
            if (_pinListRoot != null)
            {
                Object.Destroy(_pinListRoot.gameObject);
                _pinListRoot = null;
            }

            _pinPool?.Clear();

            _pageDots.Clear();
            _paginationRoot = null;

            if (_gatheringListPanel != null)
            {
                Object.Destroy(_gatheringListPanel.gameObject);
                _gatheringListPanel = null;
            }

            _gatheringData.Clear();
            _gatheringAggregator.Clear();
            _lastHintKey = null;
            _lastGatheringSlotCount = -1;
            _gatheringStamp = 0;
            // Required by the one-time setup guard in RepositionGatheringListForInventory: the
            // panel this flag refers to has just been destroyed, so the next one must be reparented
            // and configured again rather than inheriting a stale "already done".
            _gatheringListRepositioned = false;
            _previousPinCount = 0;
            _invCountsBuilt = false;

            // NOTE: DestroyMyPinsUI is NOT called here on purpose.
            // My Pins panel lives on the InventoryGui and is independent of the HUD overlay rebuild.
            // It is only destroyed when inventory closes or player session changes.

            DebugLogger.Log("UI destroyed");
        }

        public void ResetPage()
        {
            _currentPage = 0;
        }

        public void CyclePage()
        {
            var recipeMgr = RecipePinnerPlugin.Instance.RecipeMgr;
            int totalPins = recipeMgr.CachedPins.Count;
            int perPage = Mathf.Min(RecipePinnerPlugin.PinsPerPage.Value, RecipePinnerPlugin.MaximumPins.Value);
            if (perPage < 1) perPage = 1;

            if (totalPins <= perPage) return;

            int totalPages = Mathf.CeilToInt((float)totalPins / perPage);

            _currentPage++;

            if (_currentPage >= totalPages)
            {
                _currentPage = 0;
            }

            DebugLogger.Log($"Switched to Page: {_currentPage + 1}/{totalPages}");
            UpdateUI(RecipePinnerPlugin.IsUiVisible);
        }

        /// <summary>
        /// Refreshes _reusableInvCounts from the player's inventory, skipping the rebuild when the
        /// inventory is unchanged. Called every frame, so the cheap walk stays but the string
        /// hashing and dictionary writes do not. The signature combines each item's shared-data
        /// identity with its stack count, so any real change forces a rebuild; a reorder forces a
        /// harmless one.
        /// </summary>
        private void RefreshInventoryCounts(Inventory inv)
        {
            if (inv == null) return;

            var items = inv.GetAllItems();

            int signature = 17;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || item.m_shared == null) continue;
                signature = signature * 31 + RuntimeHelpers.GetHashCode(item.m_shared);
                signature = signature * 31 + item.m_stack;
            }

            if (_invCountsBuilt && signature == _invCountsSignature) return;

            _invCountsSignature = signature;
            _invCountsBuilt = true;

            _reusableInvCounts.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || item.m_shared == null) continue;
                string iName = item.m_shared.m_name;
                if (_reusableInvCounts.TryGetValue(iName, out int existing))
                    _reusableInvCounts[iName] = existing + item.m_stack;
                else
                    _reusableInvCounts[iName] = item.m_stack;
            }
        }

        public void UpdateUI(bool isVisible)
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead())
            {
                HideHudOverlay();
                return;
            }

            Inventory pInv = Player.m_localPlayer.GetInventory();
            if (pInv == null)
            {
                HideHudOverlay();
                return;
            }

            var instance = RecipePinnerPlugin.Instance;
            var recipeMgr = instance.RecipeMgr;
            var containerMgr = instance.ContainerMgr;

            if (_pinListRoot != null && _pinPool.Count != RecipePinnerPlugin.MaximumPins.Value)
            {
                DebugLogger.Log($"Pin limit changed ({_pinPool.Count} -> {RecipePinnerPlugin.MaximumPins.Value}), rebuilding");
                ResetPage();
                DestroyUI();
            }

            if (_pinListRoot == null)
            {
                _pinPool.Clear();
                CreateCanvasUI();

                if (_pinListRoot == null) return;

                foreach (var pin in recipeMgr.CachedPins)
                    pin.IsDirty = true;
            }

            UpdateLayout();

            if (_pinListRoot == null) return;

            bool isInventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            bool shouldShow = isVisible && recipeMgr.CachedPins.Count > 0;

            // Auto-open/close gathering list — must run before early returns so state stays in sync
            int currentPinCount = recipeMgr.CachedPins.Count;
            if (RecipePinnerPlugin.AutoOpenGatheringList.Value &&
                RecipePinnerPlugin.EnableGatheringList.Value &&
                !_gatheringListVisible &&
                currentPinCount >= 2 &&
                _previousPinCount < 2)
            {
                _gatheringListVisible = true;
                DebugLogger.Log("Gathering list auto-opened (2+ pins detected)");
            }
            if (_gatheringListVisible &&
                currentPinCount < 2 &&
                _previousPinCount >= 2)
            {
                _gatheringListVisible = false;
                _gatheringListPanel?.SetActive(false);
                DebugLogger.Log("Gathering list auto-closed (less than 2 pins)");
            }
            _previousPinCount = currentPinCount;

            bool gatheringListOnly = !shouldShow && _gatheringListVisible && _gatheringListPanel != null && recipeMgr.CachedPins.Count > 0;

            if (gatheringListOnly)
            {
                if (!_pinListRoot.gameObject.activeSelf)
                    _pinListRoot.gameObject.SetActive(true);

                for (int i = 0; i < _pinPool.Count; i++)
                {
                    if (_pinPool[i] != null && _pinPool[i].gameObject.activeSelf)
                        _pinPool[i].SetActive(false);
                }
                if (_paginationRoot != null && _paginationRoot.activeSelf)
                    _paginationRoot.SetActive(false);

                _gatheringListPanel.SetActive(true);

                LayoutElement le = _gatheringListPanel.GetComponent<LayoutElement>()
                    ?? _gatheringListPanel.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;

                RectTransform panelRect = _gatheringListPanel.PanelRect;
                bool glForcedBR = RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal;
                bool glSailing = Player.m_localPlayer.GetControlledShip() != null;
                bool glBottomRight = glForcedBR || glSailing || isInventoryOpen;
                bool glHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || glSailing || isInventoryOpen;
                float columnWidth = glBottomRight
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : (glHorizontal ? RecipePinnerPlugin.HorizontalColumnWidth.Value : RecipePinnerPlugin.VerticalListWidth.Value);

                bool isChestOpen = isInventoryOpen && ReflectionHelper.GetCurrentContainer(InventoryGui.instance) != null;

                if (isChestOpen)
                {
                    RepositionGatheringListForInventory();
                }
                else
                {
                    if (_gatheringListRepositioned)
                    {
                        RestoreGatheringListParent();
                        _gatheringListRepositioned = false;
                    }

                    panelRect.sizeDelta = new Vector2(columnWidth, panelRect.sizeDelta.y);

                    if (!glHorizontal)
                    {
                        // Vertical mode: reset root position to non-paginated state
                        // since dots are hidden, then anchor gathering list to top of root
                        RectTransform rootRect = _pinListRoot.GetComponent<RectTransform>();
                        rootRect.anchoredPosition = GetVerticalLayoutPosition();

                        panelRect.anchorMin = new Vector2(0.5f, 1f);
                        panelRect.anchorMax = new Vector2(0.5f, 1f);
                        panelRect.pivot = new Vector2(0.5f, 1f);
                    }

                    panelRect.anchoredPosition = Vector2.zero;
                }

                RefreshInventoryCounts(Player.m_localPlayer.GetInventory());

                UpdateGatheringList();
                return;
            }

            if (_pinListRoot.gameObject.activeSelf != shouldShow)
                _pinListRoot.gameObject.SetActive(shouldShow);

            if (!shouldShow)
            {
                if (_gatheringListRepositioned && _gatheringListPanel != null)
                {
                    RestoreGatheringListParent();
                    _gatheringListRepositioned = false;
                    _gatheringListPanel.SetActive(_gatheringListVisible);
                }
                return;
            }

            if (isInventoryOpen && _gatheringListPanel != null && recipeMgr.CachedPins.Count > 0 && _gatheringListVisible)
            {
                bool isChestOpen = ReflectionHelper.GetCurrentContainer(InventoryGui.instance) != null;
                _gatheringListPanel.SetActive(true);
                if (isChestOpen)
                {
                    RepositionGatheringListForInventory();
                }
            }
            else if (_gatheringListRepositioned && _gatheringListPanel != null)
            {
                RestoreGatheringListParent();
                _gatheringListRepositioned = false;
                _gatheringListPanel.SetActive(_gatheringListVisible);
            }

            // Auto-open/close logic already handled above (before early returns)

            RefreshInventoryCounts(pInv);

            int activePinCount = recipeMgr.CachedPins.Count;
            int perPage = Mathf.Min(RecipePinnerPlugin.PinsPerPage.Value, _pinPool.Count);
            if (perPage < 1) perPage = 1;
            int totalPagesForClamp = activePinCount > 0 ? Mathf.CeilToInt((float)activePinCount / perPage) : 1;
            if (_currentPage >= totalPagesForClamp)
                _currentPage = totalPagesForClamp - 1;
            if (_currentPage < 0)
                _currentPage = 0;

            int startIndex = _currentPage * perPage;

            if (startIndex >= activePinCount && _currentPage > 0)
            {
                _currentPage--;
                startIndex = _currentPage * perPage;
            }

            int endIndex = Mathf.Min(startIndex + perPage, activePinCount);

            // A lone pinned group needs no "+N" — its compact grid is the same list the
            // Gathering List would show, so render it uncapped (all materials, no overflow cell).
            bool soloGroupUncap = activePinCount == 1
                && recipeMgr.CachedPins[0] != null && recipeMgr.CachedPins[0].IsGroup;

            for (int i = 0; i < _pinPool.Count; i++)
            {
                if (_pinPool[i] == null) continue;

                int dataIndex = startIndex + i;

                if (dataIndex < endIndex)
                {
                    UpdatePinSlot(i, recipeMgr.CachedPins[dataIndex], containerMgr, soloGroupUncap);
                }
                else
                {
                    if (_pinPool[i].gameObject.activeSelf)
                        _pinPool[i].SetActive(false);
                }
            }

            bool isSailing = Player.m_localPlayer.GetControlledShip() != null;
            bool isInvOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            bool shouldBeHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || isSailing || isInvOpen;
            EqualizePinHeights(startIndex, endIndex, shouldBeHorizontal);

            int totalPages = 1;
            if (activePinCount > 0)
            {
                totalPages = Mathf.CeilToInt((float)activePinCount / perPage);
            }
            UpdatePageDots(totalPages);

            UpdateGatheringList();
        }

        private void HideHudOverlay()
        {
            if (_pinListRoot != null && _pinListRoot.gameObject.activeSelf)
                _pinListRoot.gameObject.SetActive(false);

            if (_paginationRoot != null && _paginationRoot.activeSelf)
                _paginationRoot.SetActive(false);

            if (_gatheringListPanel != null && _gatheringListPanel.gameObject.activeSelf)
                _gatheringListPanel.SetActive(false);
        }

        private readonly List<Image> _pageDots = new List<Image>();

        private void UpdatePageDots(int totalPages)
        {
            if (_paginationRoot == null) return;

            HorizontalLayoutGroup hlg = _paginationRoot.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.spacing = RecipePinnerPlugin.PaginationDotSpacing.Value;
            }

            if (totalPages <= 1)
            {
                if (_paginationRoot.activeSelf) _paginationRoot.SetActive(false);
                return;
            }

            if (!_paginationRoot.activeSelf) _paginationRoot.SetActive(true);


            while (_pageDots.Count < totalPages)
            {
                _pageDots.Add(UIBuilder.CreatePageDot(_paginationRoot.transform));
            }

            int baseSize = RecipePinnerPlugin.PaginationDotSize.Value;
            Color baseColor = RecipePinnerPlugin.ColorPaginationActive.Value;

            for (int i = 0; i < _pageDots.Count; i++)
            {
                if (i < totalPages)
                {
                    _pageDots[i].gameObject.SetActive(true);

                    if (i == _currentPage)
                    {
                        _pageDots[i].color = baseColor;
                        _pageDots[i].rectTransform.sizeDelta = new Vector2(baseSize * 1.2f, baseSize * 1.2f);
                    }
                    else
                    {
                        Color fadedColor = baseColor;
                        fadedColor.a = RecipePinnerPlugin.PaginationInactiveOpacity.Value;
                        _pageDots[i].color = fadedColor;
                        _pageDots[i].rectTransform.sizeDelta = new Vector2(baseSize, baseSize);
                    }
                }
                else
                {
                    _pageDots[i].gameObject.SetActive(false);
                }
            }
        }

        private void UpdateDotsPosition()
        {
            if (_paginationRoot == null || _pinListRoot == null) return;

            RectTransform dotsRect = _paginationRoot.GetComponent<RectTransform>();

            bool isForcedBottomRight = RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal;
            bool isSailing = Player.m_localPlayer.GetControlledShip() != null;
            bool isInvOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            bool shouldUseHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || isSailing || isInvOpen;

            if (shouldUseHorizontal)
            {
                bool isBottomRight = isForcedBottomRight || isSailing || isInvOpen;
                float colWidth = isBottomRight
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : RecipePinnerPlugin.HorizontalColumnWidth.Value;

                float centerOffset = -(colWidth / 2f);

                if (isBottomRight)
                {
                    // --- Bottom Right ---
                    dotsRect.anchorMin = new Vector2(1, 0);
                    dotsRect.anchorMax = new Vector2(1, 0);
                    dotsRect.pivot = new Vector2(0.5f, 1);

                    dotsRect.anchoredPosition = new Vector2(centerOffset, -15f);
                }
                else
                {
                    // --- Map Side ---
                    dotsRect.anchorMin = new Vector2(1, 1);
                    dotsRect.anchorMax = new Vector2(1, 1);
                    dotsRect.pivot = new Vector2(0.5f, 0);

                    dotsRect.anchoredPosition = new Vector2(centerOffset, 15f);
                }
            }
            else
            {
                // --- Vertical ---
                dotsRect.anchorMin = new Vector2(0.5f, 1);
                dotsRect.anchorMax = new Vector2(0.5f, 1);
                dotsRect.pivot = new Vector2(0.5f, 0);

                dotsRect.anchoredPosition = new Vector2(0, 20f);
            }
        }

        private void EqualizePinHeights(int startIndex, int endIndex, bool isHorizontal)
        {
            for (int i = 0; i < _pinPool.Count; i++)
            {
                PinSlotUI slot = _pinPool[i];
                if (slot?.Csf == null) continue;

                int dataIndex = startIndex + i;
                bool isVisible = dataIndex < endIndex;

                if (isHorizontal && isVisible)
                {
                    if (slot.Csf.verticalFit != ContentSizeFitter.FitMode.Unconstrained)
                        slot.Csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
                else
                {
                    if (slot.Csf.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                        slot.Csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }
        }

        private void UpdatePinSlot(int index, PinnedRecipeData pinData, ContainerScanner containerMgr, bool uncapCompact)
        {
            PinSlotUI uiSlot = _pinPool[index];
            if (uiSlot == null || uiSlot.gameObject == null) return;

            if (!uiSlot.gameObject.activeSelf)
                uiSlot.SetActive(true);

            if (uiSlot.BgImage != null)
            {
                float currentAlpha = uiSlot.BgImage.color.a;
                if (Mathf.Abs(currentAlpha - RecipePinnerPlugin.BackgroundOpacity.Value) > 0.01f)
                    uiSlot.BgImage.color = new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
            }

            bool isBottomRight = (RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal);
            bool isSailing = (Player.m_localPlayer.GetControlledShip() != null);
            bool isInvOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            bool shouldBeHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || isSailing || isInvOpen;

            // Width of the pin box in the current layout. Horizontal mode applies it to the rect
            // below; both layouts pass it to UpdateData so the compact grid can size its columns.
            float slotWidth = shouldBeHorizontal
                ? ((isBottomRight || isSailing || isInvOpen)
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : RecipePinnerPlugin.HorizontalColumnWidth.Value)
                : RecipePinnerPlugin.VerticalListWidth.Value;

            if (shouldBeHorizontal)
            {
                RectTransform slotRect = uiSlot.Rect ?? uiSlot.GetComponent<RectTransform>();

                if (Mathf.Abs(slotRect.sizeDelta.x - slotWidth) > 1f)
                    slotRect.sizeDelta = new Vector2(slotWidth, slotRect.sizeDelta.y);
            }

            bool dataChanged = (uiSlot.CurrentData != pinData);
            uiSlot.CurrentData = pinData;

            foreach (var res in pinData.Resources)
            {
                _reusableInvCounts.TryGetValue(res.ItemName, out int invCount);

                int chestCount = 0;
                if (RecipePinnerPlugin.EnableChestScanning.Value)
                    containerMgr.ContainerCache.TryGetValue(res.ItemName, out chestCount);

                int total = invCount + chestCount;

                if (total != res.LastKnownAmount || invCount != res.LastKnownInvAmount || res.CachedAmountString == null)
                {
                    res.LastKnownAmount = total;
                    res.LastKnownInvAmount = invCount;

                    Color targetColor = (invCount >= res.RequiredAmount) ?
                                   RecipePinnerPlugin.ColorEnoughInInventory.Value :
                                   (total >= res.RequiredAmount) ?
                                   RecipePinnerPlugin.ColorEnoughWithChests.Value :
                                   RecipePinnerPlugin.ColorMissing.Value;

                    string hexColor = "#" + ColorUtility.ToHtmlStringRGBA(targetColor);
                    string newString = $"<color={hexColor}>{total}/{res.RequiredAmount}</color>";

                    // U12: a recipe pinned more than once turns its totals red long before a single
                    // unit is out of reach, which reads as "I cannot craft even one". Append what one
                    // unit costs and color that bracket by the same three-way rule, so the answer is
                    // one glance instead of mental arithmetic. RequiredAmount != SingleAmount is the
                    // "pinned more than once" test; SingleAmount is 0 on group rows, which have no
                    // single-unit cost, so groups never reach this branch.
                    if (RecipePinnerPlugin.ShowSingleUnitRequirement.Value
                        && res.SingleAmount > 0
                        && res.RequiredAmount != res.SingleAmount)
                    {
                        Color singleColor = (invCount >= res.SingleAmount) ?
                                       RecipePinnerPlugin.ColorEnoughInInventory.Value :
                                       (total >= res.SingleAmount) ?
                                       RecipePinnerPlugin.ColorEnoughWithChests.Value :
                                       RecipePinnerPlugin.ColorMissing.Value;

                        string singleHex = "#" + ColorUtility.ToHtmlStringRGBA(singleColor);
                        newString += $"<color={singleHex}>({res.SingleAmount})</color>";
                    }

                    if (res.CachedAmountString != newString)
                    {
                        res.CachedAmountString = newString;
                        pinData.IsDirty = true;
                    }
                }
            }

            if (uiSlot.AccentBar != null)
            {
                if (RecipePinnerPlugin.EnableCraftReadiness.Value)
                {
                    if (!uiSlot.AccentBar.gameObject.activeSelf)
                        uiSlot.AccentBar.gameObject.SetActive(true);

                    bool allReady = true;
                    for (int r = 0; r < pinData.Resources.Count; r++)
                    {
                        if (pinData.Resources[r].LastKnownAmount < pinData.Resources[r].RequiredAmount)
                        {
                            allReady = false;
                            break;
                        }
                    }

                    Color targetAccent = allReady
                        ? RecipePinnerPlugin.ColorCraftReady.Value
                        : RecipePinnerPlugin.ColorCraftNotReady.Value;

                    if (uiSlot.AccentBar.color != targetAccent)
                        uiSlot.AccentBar.color = targetAccent;
                }
                else
                {
                    if (uiSlot.AccentBar.gameObject.activeSelf)
                        uiSlot.AccentBar.gameObject.SetActive(false);
                }
            }

            // Re-run UpdateData when the pin box width changed, so the compact grid re-fits its
            // columns to a live ColumnWidth change even if the pin's data did not change.
            bool widthChanged = Mathf.Abs(slotWidth - uiSlot.LastSlotWidth) > 0.5f;
            bool uncapChanged = uncapCompact != uiSlot.LastUncap;
            if (dataChanged || pinData.IsDirty || widthChanged || uncapChanged)
            {
                uiSlot.UpdateData(pinData, _cachedFont, slotWidth, uncapCompact);
                uiSlot.LastSlotWidth = slotWidth;
                uiSlot.LastUncap = uncapCompact;
                pinData.IsDirty = false;
            }
        }

        private void CreateCanvasUI()
        {
            if (_pinListRoot != null) return;

            if (Hud.instance == null || Hud.instance.m_rootObject == null)
            {
                // UpdateUI reaches this every frame, and Warning ignores the debug-logging setting,
                // so log the first occurrence only. Cleared below once the HUD is available again.
                if (!_warnedNoHud)
                {
                    _warnedNoHud = true;
                    DebugLogger.Warning("Cannot create canvas - Hud.instance is null");
                }
                return;
            }

            _warnedNoHud = false;

            if (_cachedFont == null)
                _cachedFont = GetGameFont();

            if (_cachedFont == null)
            {
                DebugLogger.Error("Cannot create UI - no valid font found");
                return;
            }

            DebugLogger.Log("CreateCanvasUI");

            Transform parentTransform = Hud.instance.m_rootObject.transform;
            GameObject rootObj = new GameObject("PinListRoot", typeof(RectTransform)) { layer = 5 };
            rootObj.transform.SetParent(parentTransform, false);
            _pinListRoot = rootObj.transform;
            _pinListRoot.localScale = Vector3.one * RecipePinnerPlugin.UIScale.Value;

            RectTransform rect = rootObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);

            bool isSailing = Player.m_localPlayer != null && Player.m_localPlayer.GetControlledShip() != null;
            bool isForcedBottomRight = RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal;
            bool isInventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            bool isBottomRightMode = isSailing || isForcedBottomRight || isInventoryOpen;
            bool shouldUseHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || isSailing || isInventoryOpen;

            DebugLogger.Verbose($"UI Layout - Horizontal: {shouldUseHorizontal}, BottomRight: {isBottomRightMode}, Sailing: {isSailing}");

            if (shouldUseHorizontal)
            {
                HorizontalLayoutGroup hlg = rootObj.AddComponent<HorizontalLayoutGroup>();
                hlg.childControlHeight = true;
                hlg.childControlWidth = false;
                hlg.childForceExpandHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childAlignment = isBottomRightMode ? TextAnchor.LowerRight : TextAnchor.UpperRight;
                hlg.spacing = RecipePinnerPlugin.HorizontalPinSpacing.Value;

                ContentSizeFitter csf = rootObj.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                VerticalLayoutGroup vlg = rootObj.AddComponent<VerticalLayoutGroup>();
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.spacing = 8;

                ContentSizeFitter csf = rootObj.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Create pin slots
            for (int i = 0; i < RecipePinnerPlugin.MaximumPins.Value; i++)
            {
                PinSlotUI slot = UIBuilder.CreatePinSlot(_pinListRoot, _cachedFont);
                if (slot != null)
                {
                    slot.SetActive(false);
                    _pinPool.Add(slot);
                }
            }

            _paginationRoot = UIBuilder.CreatePaginationContainer(_pinListRoot);
            _paginationRoot.transform.SetAsLastSibling();

            // Gathering List Panel
            if (RecipePinnerPlugin.EnableGatheringList.Value)
            {
                string title = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("gathering_title") ?? "TOTAL NEEDS";
                _gatheringListPanel = UIBuilder.CreateGatheringListPanel(_pinListRoot, _cachedFont, title);
                _gatheringListPanel.SetActive(_gatheringListVisible);
                DebugLogger.Log("Gathering list panel ready");
            }

            DebugLogger.Log($"UI ready ({_pinPool.Count} slots)");
        }

        private void UpdateLayout()
        {
            if (_pinListRoot == null) return;

            var instance = RecipePinnerPlugin.Instance;
            bool isForcedBottomRight = RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal;
            bool isSailing = !isForcedBottomRight && Player.m_localPlayer.GetControlledShip() != null;
            bool isInventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();
            bool isBottomRightMode = isForcedBottomRight || isSailing || isInventoryOpen;
            bool shouldBeHorizontal = instance.IsHorizontalMode || isSailing || isInventoryOpen;

            var layoutGroup = _pinListRoot.GetComponent<HorizontalLayoutGroup>();
            bool hasHorizontalComponent = layoutGroup != null;

            if (shouldBeHorizontal != hasHorizontalComponent)
            {
                DebugLogger.Log("Layout mode changed - rebuilding UI");
                DestroyUI();
                return;
            }

            if (_pinListRoot.localScale.x != RecipePinnerPlugin.UIScale.Value)
            {
                _pinListRoot.localScale = Vector3.one * RecipePinnerPlugin.UIScale.Value;
            }

            RectTransform rootRect = _pinListRoot.GetComponent<RectTransform>();

            if (shouldBeHorizontal)
            {
                UpdateHorizontalLayout(rootRect, isBottomRightMode);
            }
            else
            {
                UpdateVerticalLayout(rootRect);
            }

            UpdateDotsPosition();
            UpdateGatheringListPosition(shouldBeHorizontal, isBottomRightMode);
        }

        private void UpdateHorizontalLayout(RectTransform rootRect, bool isBottomRightMode)
        {
            HorizontalLayoutGroup hlg = _pinListRoot.GetComponent<HorizontalLayoutGroup>();
            if (isBottomRightMode)
            {
                if (rootRect.anchorMin != new Vector2(1, 0))
                {
                    rootRect.anchorMin = new Vector2(1, 0);
                    rootRect.anchorMax = new Vector2(1, 0);
                    rootRect.pivot = new Vector2(1, 0);
                }

                if (hlg != null && hlg.childAlignment != TextAnchor.LowerRight)
                    hlg.childAlignment = TextAnchor.LowerRight;

                if (hlg != null && Mathf.Abs(hlg.spacing - RecipePinnerPlugin.BottomRightPinSpacing.Value) > 0.01f)
                    hlg.spacing = RecipePinnerPlugin.BottomRightPinSpacing.Value;

                Vector2 targetPos = RecipePinnerPlugin.BottomRightPosition.Value;
                if (rootRect.anchoredPosition != targetPos)
                    rootRect.anchoredPosition = targetPos;
            }
            else
            {
                if (rootRect.anchorMin != new Vector2(1, 1))
                {
                    rootRect.anchorMin = new Vector2(1, 1);
                    rootRect.anchorMax = new Vector2(1, 1);
                    rootRect.pivot = new Vector2(1, 1);
                }

                if (hlg != null && hlg.childAlignment != TextAnchor.UpperRight)
                    hlg.childAlignment = TextAnchor.UpperRight;

                if (hlg != null && Mathf.Abs(hlg.spacing - RecipePinnerPlugin.HorizontalPinSpacing.Value) > 0.01f)
                    hlg.spacing = RecipePinnerPlugin.HorizontalPinSpacing.Value;

                Vector2 targetPos = RecipePinnerPlugin.HorizontalPosition.Value;

                if (Game.m_noMap && RecipePinnerPlugin.Instance._mluiInstalled && RecipePinnerPlugin.Instance._mluiNoMapListEnabled)
                {
                    if (Mathf.Abs(targetPos.x - (-250f)) < 1f) targetPos.x = -270f;
                    if (Mathf.Abs(targetPos.y - (-40f)) < 1f) targetPos.y = -15f;
                }

                if (rootRect.anchoredPosition != targetPos)
                    rootRect.anchoredPosition = targetPos;
            }
        }

        private void UpdateVerticalLayout(RectTransform rootRect)
        {
            if (rootRect.anchorMin != new Vector2(1, 1))
            {
                rootRect.anchorMin = new Vector2(1, 1);
                rootRect.anchorMax = new Vector2(1, 1);
                rootRect.pivot = new Vector2(1, 1);
            }

            VerticalLayoutGroup vlg = _pinListRoot.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                if (Mathf.Abs(vlg.spacing - RecipePinnerPlugin.VerticalPinSpacing.Value) > 0.01f)
                    vlg.spacing = RecipePinnerPlugin.VerticalPinSpacing.Value;
            }

            Vector2 targetPos = GetVerticalLayoutPosition();

            var recipeMgr = RecipePinnerPlugin.Instance.RecipeMgr;
            if (recipeMgr.CachedPins.Count > RecipePinnerPlugin.PinsPerPage.Value)
            {
                targetPos.y -= 30f;
            }

            if (rootRect.anchoredPosition != targetPos)
                rootRect.anchoredPosition = targetPos;

            if (Mathf.Abs(rootRect.sizeDelta.x - RecipePinnerPlugin.VerticalListWidth.Value) > 1f)
                rootRect.sizeDelta = new Vector2(RecipePinnerPlugin.VerticalListWidth.Value, 0);
        }

        private Vector2 GetVerticalLayoutPosition()
        {
            Vector2 targetPos = RecipePinnerPlugin.VerticalPosition.Value;

            if (RecipePinnerPlugin.LayoutModeConfig.Value != RecipePinnerPlugin.PinLayoutMode.AutoDetect)
                return targetPos;

            var instance = RecipePinnerPlugin.Instance;
            if (instance == null || !instance._mluiInstalled || !IsDefaultVerticalPosition(targetPos))
                return targetPos;

            if (Game.m_noMap)
            {
                if (!instance._mluiNoMapListEnabled)
                    targetPos.y = MluNoMapListDisabledVerticalY;
            }
            else if (!instance._mluiMapListEnabled)
            {
                targetPos.y = MluMapListDisabledVerticalY;
            }

            return targetPos;
        }

        private static bool IsDefaultVerticalPosition(Vector2 position)
        {
            return Mathf.Abs(position.x - DefaultVerticalPositionX) < 1f
                   && Mathf.Abs(position.y - DefaultVerticalPositionY) < 1f;
        }

        private Font GetGameFont()
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            foreach (Font f in fonts)
            {
                if (f != null && f.name == "AveriaSerifLibre-Bold")
                {
                    DebugLogger.Log("Found game font: AveriaSerifLibre-Bold");
                    return f;
                }
            }

            foreach (Font f in fonts)
            {
                if (f != null && f.name == "Arial")
                {
                    DebugLogger.Log("Using fallback font: Arial");
                    return f;
                }
            }

            try
            {
                DebugLogger.Log("Creating dynamic font from OS: Arial");
                return Font.CreateDynamicFontFromOSFont("Arial", 14);
            }
            catch
            {
                DebugLogger.Warning("Failed to create dynamic font, using first available font");
                return fonts.Length > 0 ? fonts[0] : null;
            }
        }
    }
}
