using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class UIManager
    {
        private Transform _pinListRoot;
        private readonly List<PinSlotUI> _pinPool = new List<PinSlotUI>();
        private Font _cachedFont;
        private readonly Dictionary<string, int> _reusableInvCounts = new Dictionary<string, int>();

        private int _currentPage = 0;
        private GameObject _paginationRoot;

        private GatheringListUI _gatheringListPanel;
        private bool _gatheringListVisible = false;
        private bool _gatheringListRepositioned = false;
        private readonly List<GatheringItemData> _gatheringData = new List<GatheringItemData>();
        private readonly Dictionary<string, GatheringItemData> _gatheringAggregator = new Dictionary<string, GatheringItemData>();
        private int _previousPinCount = 0;
        private string _lastHintKey = null;

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

            _lastHintKey = null;
            _previousPinCount = 0;

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
            int perPage = RecipePinnerPlugin.PinsPerPage.Value;

            if (totalPins <= perPage) return;

            int totalPages = Mathf.CeilToInt((float)totalPins / perPage);

            _currentPage++;

            if (_currentPage >= totalPages)
            {
                _currentPage = 0;
            }

            DebugLogger.Log($"Switched to Page: {_currentPage + 1}/{totalPages}");
            UpdateUI(true);
        }

        public void ToggleGatheringList()
        {
            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;

            if (!_gatheringListVisible && (recipeMgr == null || recipeMgr.CachedPins.Count == 0))
            {
                DebugLogger.Log("Gathering list toggle blocked: no pins");
                if (Player.m_localPlayer != null)
                {
                    var loc = RecipePinnerPlugin.Instance?.LocalizationMgr;
                    string msg = loc?.GetText("gathering_empty") ?? "No Recipes Pinned";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
                }
                return;
            }

            _gatheringListVisible = !_gatheringListVisible;
            DebugLogger.Log($"Gathering list toggled: {_gatheringListVisible}");

            _gatheringListPanel?.SetActive(_gatheringListVisible);

            if (Player.m_localPlayer != null)
            {
                var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
                string msg = _gatheringListVisible
                    ? locMgr?.GetText("gathering_opened") ?? "Gathering List Opened"
                    : locMgr?.GetText("gathering_closed") ?? "Gathering List Closed";
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
            }
        }

        public void CloseGatheringList()
        {
            _gatheringListVisible = false;
            _gatheringListPanel?.SetActive(false);
        }

        public void UpdateUI(bool isVisible)
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead()) return;

            Inventory pInv = Player.m_localPlayer.GetInventory();
            if (pInv == null) return;

            var instance = RecipePinnerPlugin.Instance;
            var recipeMgr = instance.RecipeMgr;
            var containerMgr = instance.ContainerMgr;

            if (_pinPool.Count < RecipePinnerPlugin.MaximumPins.Value)
            {
                DebugLogger.Log($"Pin limit changed ({_pinPool.Count} -> {RecipePinnerPlugin.MaximumPins.Value}), rebuilding");
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
            bool shouldShow = isVisible && !InputHelper.IsInputBlocked() && recipeMgr.CachedPins.Count > 0;
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
                float columnWidth = glBottomRight
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : (RecipePinnerPlugin.Instance.IsHorizontalMode ? RecipePinnerPlugin.HorizontalColumnWidth.Value : RecipePinnerPlugin.VerticalListWidth.Value);

                panelRect.sizeDelta = new Vector2(columnWidth, panelRect.sizeDelta.y);
                panelRect.anchoredPosition = Vector2.zero;

                if (_gatheringListRepositioned)
                {
                    _gatheringListRepositioned = false;
                }

                UpdateGatheringList();
                return;
            }

            if (_pinListRoot.gameObject.activeSelf != shouldShow)
                _pinListRoot.gameObject.SetActive(shouldShow);

            if (!shouldShow)
            {
                if (_gatheringListRepositioned && _gatheringListPanel != null)
                {
                    _gatheringListRepositioned = false;
                    _gatheringListPanel.SetActive(_gatheringListVisible);
                }
                return;
            }

            if (isInventoryOpen && _gatheringListPanel != null && recipeMgr.CachedPins.Count > 0)
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
                _gatheringListRepositioned = false;
                _gatheringListPanel.SetActive(_gatheringListVisible);
            }

            int currentPinCount = recipeMgr.CachedPins.Count;
            if (RecipePinnerPlugin.AutoOpenGatheringList.Value &&
                RecipePinnerPlugin.EnableGatheringList.Value &&
                !_gatheringListVisible &&
                currentPinCount >= 2 &&
                _previousPinCount < 2)
            {
                _gatheringListVisible = true;
                _gatheringListPanel?.SetActive(true);
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

            if (_gatheringListPanel != null && _gatheringListPanel.HintText != null)
            {
                string keyName = RecipePinnerPlugin.HotkeyGatheringList.Value.ToString();
                if (_lastHintKey != keyName)
                {
                    _lastHintKey = keyName;
                    var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
                    string hintTemplate = locMgr?.GetText("gathering_hint") ?? "Open/Close: {0}";
                    _gatheringListPanel.HintText.text = string.Format(hintTemplate, keyName);
                }
            }

            _reusableInvCounts.Clear();
            foreach (var item in pInv.GetAllItems())
            {
                string iName = item.m_shared.m_name;
                if (_reusableInvCounts.TryGetValue(iName, out int existing))
                    _reusableInvCounts[iName] = existing + item.m_stack;
                else
                    _reusableInvCounts[iName] = item.m_stack;
            }

            int activePinCount = recipeMgr.CachedPins.Count;
            int perPage = RecipePinnerPlugin.PinsPerPage.Value;

            int startIndex = _currentPage * perPage;

            if (startIndex >= activePinCount && _currentPage > 0)
            {
                _currentPage--;
                startIndex = _currentPage * perPage;
            }

            int endIndex = Mathf.Min(startIndex + perPage, activePinCount);

            for (int i = 0; i < _pinPool.Count; i++)
            {
                if (_pinPool[i] == null) continue;

                int dataIndex = startIndex + i;

                if (dataIndex < endIndex)
                {
                    UpdatePinSlot(i, recipeMgr.CachedPins[dataIndex], containerMgr);
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

        private void UpdatePinSlot(int index, PinnedRecipeData pinData, ContainerScanner containerMgr)
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

            if (shouldBeHorizontal)
            {
                RectTransform slotRect = uiSlot.Rect ?? uiSlot.GetComponent<RectTransform>();

                float targetWidth = (isBottomRight || isSailing || isInvOpen)
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : RecipePinnerPlugin.HorizontalColumnWidth.Value;

                if (Mathf.Abs(slotRect.sizeDelta.x - targetWidth) > 1f)
                    slotRect.sizeDelta = new Vector2(targetWidth, slotRect.sizeDelta.y);
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

            if (dataChanged || pinData.IsDirty)
            {
                uiSlot.UpdateData(pinData, _cachedFont);
                pinData.IsDirty = false;
            }
        }

        private void CreateCanvasUI()
        {
            if (_pinListRoot != null) return;

            if (Hud.instance == null || Hud.instance.m_rootObject == null)
            {
                DebugLogger.Warning("Cannot create canvas - Hud.instance is null");
                return;
            }

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

        private void UpdateGatheringListPosition(bool isHorizontal, bool isBottomRight)
        {
            if (_gatheringListPanel == null) return;

            LayoutElement le = _gatheringListPanel.GetComponent<LayoutElement>()
                ?? _gatheringListPanel.gameObject.AddComponent<LayoutElement>();

            RectTransform panelRect = _gatheringListPanel.PanelRect;

            if (isHorizontal)
            {
                le.ignoreLayout = true;

                float columnWidth = isBottomRight
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : RecipePinnerPlugin.HorizontalColumnWidth.Value;

                panelRect.sizeDelta = new Vector2(columnWidth, panelRect.sizeDelta.y);

                if (isBottomRight)
                {
                    panelRect.anchorMin = new Vector2(1, 1);
                    panelRect.anchorMax = new Vector2(1, 1);
                    panelRect.pivot = new Vector2(1, 0);
                    panelRect.anchoredPosition = new Vector2(0, 10f);
                }
                else
                {
                    panelRect.anchorMin = new Vector2(1, 0);
                    panelRect.anchorMax = new Vector2(1, 0);
                    panelRect.pivot = new Vector2(1, 1);
                    panelRect.anchoredPosition = new Vector2(0, -10f);
                }
            }
            else
            {
                le.ignoreLayout = false;
            }
        }

        private void RepositionGatheringListForInventory()
        {
            if (_gatheringListPanel == null) return;

            LayoutElement le = _gatheringListPanel.GetComponent<LayoutElement>()
                ?? _gatheringListPanel.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            RectTransform panelRect = _gatheringListPanel.PanelRect;
            float columnWidth = RecipePinnerPlugin.BottomRightColumnWidth?.Value ?? 265f;

            Vector2 offset = RecipePinnerPlugin.InventoryGatheringListPosition?.Value ?? new Vector2(-460f, 0f);

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = offset;
            panelRect.sizeDelta = new Vector2(columnWidth, panelRect.sizeDelta.y);

            _gatheringListRepositioned = true;
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

            Vector2 targetPos = RecipePinnerPlugin.VerticalPosition.Value;

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

        private void UpdateGatheringList()
        {
            if (_gatheringListPanel == null || (!_gatheringListVisible && !_gatheringListRepositioned)) return;

            if (_gatheringListPanel.BgImage != null)
            {
                float currentAlpha = _gatheringListPanel.BgImage.color.a;
                if (Mathf.Abs(currentAlpha - RecipePinnerPlugin.BackgroundOpacity.Value) > 0.01f)
                    _gatheringListPanel.BgImage.color = new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
            }

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return;

            var containerMgr = RecipePinnerPlugin.Instance.ContainerMgr;

            _gatheringAggregator.Clear();
            _gatheringData.Clear();

            foreach (var pin in recipeMgr.CachedPins)
            {
                int pinCount = 1;
                if (recipeMgr.PinnedRecipes.TryGetValue(pin.RawName, out int c))
                    pinCount = Mathf.Max(1, c);

                foreach (var res in pin.Resources)
                {
                    if (_gatheringAggregator.TryGetValue(res.ItemName, out GatheringItemData existing))
                    {
                        existing.TotalRequired += res.RequiredAmount * pinCount;
                    }
                    else
                    {
                        var item = new GatheringItemData
                        {
                            ItemName = res.ItemName,
                            DisplayName = res.CachedName ?? res.ItemName,
                            Icon = res.Icon,
                            TotalRequired = res.RequiredAmount * pinCount
                        };
                        _gatheringAggregator[res.ItemName] = item;
                        _gatheringData.Add(item);
                    }
                }
            }

            // Calculate totals
            foreach (var item in _gatheringData)
            {
                _reusableInvCounts.TryGetValue(item.ItemName, out int invCount);

                int chestCount = 0;
                if (RecipePinnerPlugin.EnableChestScanning.Value)
                    containerMgr.ContainerCache.TryGetValue(item.ItemName, out chestCount);

                item.TotalHave = invCount + chestCount;
                item.IsComplete = item.TotalHave >= item.TotalRequired;
            }

            // Update UI slots
            var panel = _gatheringListPanel;
            while (panel.ItemSlots.Count < _gatheringData.Count)
            {
                panel.ItemSlots.Add(UIBuilder.CreateGatheringItemSlot(panel.ItemListRoot, _cachedFont));
            }

            for (int i = 0; i < panel.ItemSlots.Count; i++)
            {
                if (i < _gatheringData.Count)
                {
                    var slot = panel.ItemSlots[i];
                    var data = _gatheringData[i];

                    if (!slot.gameObject.activeSelf) slot.SetActive(true);

                    slot.Icon.sprite = data.Icon;

                    Color amountColor = data.IsComplete
                        ? RecipePinnerPlugin.ColorEnoughInInventory.Value
                        : RecipePinnerPlugin.ColorMissing.Value;

                    string hexColor = "#" + ColorUtility.ToHtmlStringRGBA(amountColor);
                    slot.AmountText.text = $"<color={hexColor}>{data.TotalHave}/{data.TotalRequired}</color>";

                }
                else
                {
                    if (panel.ItemSlots[i].gameObject.activeSelf)
                        panel.ItemSlots[i].SetActive(false);
                }
            }

            if (panel.ItemSlots.Count == _gatheringData.Count)
                panel.HintText?.transform.SetAsLastSibling();

            panel.RefreshLayout();
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