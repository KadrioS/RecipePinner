using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class UIManager
    {
        private Transform _pinListRoot;
        private List<PinSlotUI> _pinPool = new List<PinSlotUI>();
        private Font _cachedFont;
        private static Dictionary<string, int> _reusableInvCounts = new Dictionary<string, int>();

        private int _currentPage = 0;
        private GameObject _paginationRoot;

        public void DestroyUI()
        {
            DebugLogger.Verbose("Destroying UI...");
            if (_pinListRoot != null)
            {
                Object.Destroy(_pinListRoot.gameObject);
                _pinListRoot = null;
            }

            if (_pinPool != null)
                _pinPool.Clear();

            _pageDots.Clear();
            _paginationRoot = null;

            DebugLogger.Log("UI destroyed successfully");
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
                DebugLogger.Log($"Pin limit changed (Pool: {_pinPool.Count}, Config: {RecipePinnerPlugin.MaximumPins.Value}). Rebuilding UI...");
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

            bool shouldShow = isVisible && !InputHelper.IsInputBlocked() && recipeMgr.CachedPins.Count > 0;
            if (_pinListRoot.gameObject.activeSelf != shouldShow)
            {
                _pinListRoot.gameObject.SetActive(shouldShow);
            }

            if (!shouldShow) return;

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

            int totalPages = 1;
            if (activePinCount > 0)
            {
                totalPages = Mathf.CeilToInt((float)activePinCount / perPage);
            }
            UpdatePageDots(totalPages);
        }

        private List<Image> _pageDots = new List<Image>();

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
            bool shouldUseHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || isSailing;

            if (shouldUseHorizontal)
            {
                float colWidth = (isForcedBottomRight || isSailing)
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : RecipePinnerPlugin.HorizontalColumnWidth.Value;

                float centerOffset = -(colWidth / 2f);

                if (isForcedBottomRight || isSailing)
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

            // Update width for horizontal mode
            bool isBottomRight = (RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal);
            bool isSailing = (Player.m_localPlayer.GetControlledShip() != null);
            bool shouldBeHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || isSailing;

            if (shouldBeHorizontal)
            {
                RectTransform slotRect = uiSlot.Rect ?? uiSlot.GetComponent<RectTransform>();

                float targetWidth = (isBottomRight || isSailing)
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : RecipePinnerPlugin.HorizontalColumnWidth.Value;

                if (Mathf.Abs(slotRect.sizeDelta.x - targetWidth) > 1f)
                    slotRect.sizeDelta = new Vector2(targetWidth, slotRect.sizeDelta.y);
            }

            bool dataChanged = (uiSlot.CurrentData != pinData);
            uiSlot.CurrentData = pinData;

            // Update resource counts and colors
            foreach (var res in pinData.Resources)
            {
                int invCount = 0;
                _reusableInvCounts.TryGetValue(res.ItemName, out invCount);

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

            // Refresh UI if data changed OR specifically marked dirty
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

            DebugLogger.Log("Creating canvas UI...");

            Transform parentTransform = Hud.instance.m_rootObject.transform;
            GameObject rootObj = new GameObject("PinListRoot", typeof(RectTransform));
            rootObj.layer = 5;
            rootObj.transform.SetParent(parentTransform, false);
            _pinListRoot = rootObj.transform;
            _pinListRoot.localScale = Vector3.one * RecipePinnerPlugin.UIScale.Value;

            RectTransform rect = rootObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);

            bool isSailing = Player.m_localPlayer != null && Player.m_localPlayer.GetControlledShip() != null;
            bool isForcedBottomRight = RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal;
            bool isBottomRightMode = isSailing || isForcedBottomRight;
            bool shouldUseHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || isSailing;

            DebugLogger.Verbose($"UI Layout - Horizontal: {shouldUseHorizontal}, BottomRight: {isBottomRightMode}, Sailing: {isSailing}");

            if (shouldUseHorizontal)
            {
                HorizontalLayoutGroup hlg = rootObj.AddComponent<HorizontalLayoutGroup>();
                hlg.childControlHeight = true;
                hlg.childControlWidth = false;
                hlg.childForceExpandHeight = false;
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

            DebugLogger.Log($"Canvas UI created with {_pinPool.Count} pin slots");
        }

        private void UpdateLayout()
        {
            if (_pinListRoot == null) return;

            var instance = RecipePinnerPlugin.Instance;
            bool isForcedBottomRight = RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal;
            bool isSailing = !isForcedBottomRight && Player.m_localPlayer.GetControlledShip() != null;
            bool isBottomRightMode = isForcedBottomRight || isSailing;
            bool shouldBeHorizontal = instance.IsHorizontalMode || isSailing;

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