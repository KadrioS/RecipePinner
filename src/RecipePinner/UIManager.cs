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
        private bool _mapOrderChecked = false;
        private bool _rootRaised = false;
        private readonly Dictionary<string, int> _reusableInvCounts = new Dictionary<string, int>();
        private int _invCountsSignature = 0;
        private bool _invCountsBuilt = false;
        private Sprite _inventoryBgSprite;
        private Material _inventoryBgMaterial;
        private bool _inventoryBgLookupFailed = false;
        // How far the dark inset sits inside the plank's edge, in the panel's own units. The content
        // padding grows by the same amount on the plank so it stays inside the inset.
        private const float PlankInset = 20f;

        // Space kept between a group's card and the crafting window above it, in the card's units.
        private const float CraftingWindowClearance = 10f;
        private readonly Vector3[] _craftingCorners = new Vector3[4];
        private readonly Vector3[] _slotCorners = new Vector3[4];
        private readonly Vector3[] _gridCorners = new Vector3[4];
        private readonly Vector3[] _healthBarCorners = new Vector3[4];
        private readonly Vector3[] _rootCorners = new Vector3[4];
        private CanvasGroup _pinRootGroup;
        private bool _pinsMaskedForRoom = false;

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
            // _previousPinCount is kept: a rebuild changes no pins, and resetting it would make the
            // auto-open treat every rebuild as the pin count reaching its threshold.
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
            // Setting handlers and the cache refresh still reach here while the mod is off;
            // drawing then would bring back a HUD that nothing updates.
            if (RecipePinnerPlugin.EnableMod != null && !RecipePinnerPlugin.EnableMod.Value)
                return;

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

            EnsurePinsBehindMap();
            UpdateLayout();
            UpdateCraftingRoomMask();
            KeyHintInjector.EnsureInjected();
            KeyHintInjector.UpdateHintVisibility();

            if (_pinListRoot == null) return;

            bool isInventoryOpen = ReflectionHelper.IsInventoryShown();
            bool shouldShow = isVisible && recipeMgr.CachedPins.Count > 0;

            // Auto-open/close gathering list — must run before early returns so state stays in sync
            int currentPinCount = recipeMgr.CachedPins.Count;
            // The first count after the pins load is where the session starts, not a change. Only a
            // list closed with the hotkey is seeded that way and stays closed; one closed because the
            // count was under the threshold is seeded from 0, so the auto-open below opens it if this
            // character's count reaches the threshold.
            if (!_pinCountSeeded && instance.IsPinDataLoaded)
            {
                _previousPinCount = (_gatheringListVisible || RecipePinnerPlugin.GatheringListClosedByHand.Value)
                    ? currentPinCount
                    : 0;
                _pinCountSeeded = true;
            }
            int autoOpenAt = RecipePinnerPlugin.AutoOpenGatheringListAt.Value;
            if (autoOpenAt > 0 &&
                RecipePinnerPlugin.EnableGatheringList.Value &&
                !_gatheringListVisible &&
                currentPinCount >= autoOpenAt &&
                _previousPinCount < autoOpenAt)
            {
                _gatheringListVisible = true;
                RecipePinnerPlugin.GatheringListClosedByHand.Value = false;
                // Activated here because the inventory branch below does it only while the inventory is open.
                if (_gatheringListPanel != null)
                    _gatheringListPanel.SetActive(true);
                _lastGatheringSlotCount = -1;
                DebugLogger.Log($"Gathering list auto-opened ({currentPinCount} pins, threshold {autoOpenAt})");
            }
            if (autoOpenAt > 0 &&
                _gatheringListVisible &&
                currentPinCount < autoOpenAt &&
                _previousPinCount >= autoOpenAt)
            {
                _gatheringListVisible = false;
                if (_gatheringListPanel != null)
                    _gatheringListPanel.SetActive(false);
                DebugLogger.Log($"Gathering list auto-closed (below {autoOpenAt} pins)");
            }
            // Pin removals leave closing to this method, so AutoOpenGatheringListAt is the only
            // threshold. Whatever it is, removing the last pin closes the list, or the next pin
            // would bring it back.
            if (_pinCountSeeded && _gatheringListVisible && currentPinCount == 0 && _previousPinCount > 0)
            {
                CloseGatheringList();
                DebugLogger.Log("Gathering list closed (no pins left)");
            }
            _previousPinCount = currentPinCount;

            // Kept in the config file so the list starts the next launch as it was left.
            if (RecipePinnerPlugin.GatheringListOpen.Value != _gatheringListVisible)
                RecipePinnerPlugin.GatheringListOpen.Value = _gatheringListVisible;

            // Only a mask: it changes no state, so F7, F8 and the auto-open keep their own rules under it.
            if (RecipePinnerPlugin.ShowPinsOnlyWithInventory.Value && !isInventoryOpen && !Player.m_localPlayer.InPlaceMode())
            {
                if (_gatheringListRepositioned && _gatheringListPanel != null)
                {
                    RestoreGatheringListParent();
                    _gatheringListRepositioned = false;
                }
                if (_pinListRoot.gameObject.activeSelf)
                    _pinListRoot.gameObject.SetActive(false);
                return;
            }

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

                if (isInventoryOpen)
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

                    // The horizontal layouts keep the width ApplyColumns gave them, or columns past
                    // the fourth fall outside the background.
                    if (!glHorizontal)
                    {
                        panelRect.sizeDelta = new Vector2(columnWidth, panelRect.sizeDelta.y);

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
                _gatheringListPanel.SetActive(true);
                RepositionGatheringListForInventory();
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
            bool isInvOpen = ReflectionHelper.IsInventoryShown();
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

        private bool UseInventoryBackground()
        {
            if (!RecipePinnerPlugin.InventoryBackground.Value) return false;
            if (!ReflectionHelper.IsInventoryShown()) return false;

            // Looked up once: the lookup warns when it fails, and this runs every frame.
            if (_inventoryBgSprite == null && !_inventoryBgLookupFailed)
            {
                if (!TryGetInventoryPanelBackground())
                    _inventoryBgLookupFailed = true;
            }
            return _inventoryBgSprite != null;
        }

        // The player inventory window's own plank, so the pins match the window they sit under.
        private bool TryGetInventoryPanelBackground()
        {
            InventoryGui gui = InventoryGui.instance;
            Transform bkg = (gui != null && gui.m_player != null) ? gui.m_player.Find("Bkg") : null;
            Image image = bkg != null ? bkg.GetComponent<Image>() : null;
            if (image == null || image.sprite == null)
            {
                DebugLogger.Warning("Inventory panel background not found; the pins keep their own boxes");
                return false;
            }

            _inventoryBgSprite = image.sprite;
            _inventoryBgMaterial = image.material;
            DebugLogger.Log($"Inventory panel background: {_inventoryBgSprite.name}");
            return true;
        }

        // Switches a pin's or the Gathering List's Image between the inventory window's plank and the
        // mod's own dark box. Callers use it only when the wanted state changes.
        private void ApplyPanelBackground(Image image, bool plank)
        {
            Sprite sprite = plank ? _inventoryBgSprite : UIBuilder.GetBackgroundSprite();
            image.sprite = sprite;
            image.material = plank ? _inventoryBgMaterial : null;
            image.type = (sprite != null && sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            // The pins are scaled by UIScale; this draws the plank's frame as thick as the game's windows.
            image.pixelsPerUnitMultiplier = plank ? RecipePinnerPlugin.UIScale.Value : 1f;
            image.color = plank ? Color.white : new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
        }

        // On the plank a dark inset, like the crafting window's inner panels, keeps icons and text
        // readable. 14/8/8/8 is the padding UIBuilder gives both a pin and the Gathering List.
        private Image SetPlankInset(GameObject panel, Image inset, bool plank)
        {
            if (plank && inset == null)
            {
                GameObject insetObj = new GameObject("PlankInset", typeof(RectTransform)) { layer = 5 };
                insetObj.transform.SetParent(panel.transform, false);
                insetObj.transform.SetAsFirstSibling();
                insetObj.AddComponent<LayoutElement>().ignoreLayout = true;
                RectTransform insetRect = insetObj.GetComponent<RectTransform>();
                insetRect.anchorMin = Vector2.zero;
                insetRect.anchorMax = Vector2.one;
                insetRect.offsetMin = new Vector2(PlankInset, PlankInset);
                insetRect.offsetMax = new Vector2(-PlankInset, -PlankInset);
                inset = insetObj.AddComponent<Image>();
                inset.raycastTarget = false;
                Sprite sprite = UIBuilder.GetBackgroundSprite();
                inset.sprite = sprite;
                inset.type = (sprite != null && sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
            }

            if (inset != null)
            {
                inset.color = new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
                if (inset.gameObject.activeSelf != plank)
                    inset.gameObject.SetActive(plank);
            }

            VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                int grow = plank ? (int)PlankInset : 0;
                vlg.padding = new RectOffset(14 + grow, 8 + grow, 8 + grow, 8 + grow);
            }

            return inset;
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
            bool isInvOpen = ReflectionHelper.IsInventoryShown();
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

        // The HUD and the My Pins panel both need these strings, and the HUD alone cannot produce
        // them for both: UpdatePinSlot runs only for the pins on the currently visible page, and
        // never for a group's members, because the HUD draws a group as one merged row. Callers
        // must have refreshed the inventory counts first.
        private void RefreshResourceAmountStrings(PinnedRecipeData pinData, ContainerScanner containerMgr)
        {
            if (pinData == null || pinData.Resources == null) return;

            foreach (var res in pinData.Resources)
            {
                // A group's row for a recipe crafted from any one requirement.
                if (res.Alternatives != null)
                {
                    if (RefreshAnyOneRow(res, containerMgr))
                        pinData.IsDirty = true;
                    continue;
                }

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

                    // A recipe pinned more than once turns its totals red long before a single
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

            if (pinData.AnyOneRow != null && RefreshAnyOneRow(pinData.AnyOneRow, containerMgr))
                pinData.IsDirty = true;
        }

        // A summary row counts how many crafts its alternatives cover together and shows the icon
        // of the one the player has most of. Returns true when its text changed.
        private bool RefreshAnyOneRow(PinnedResData row, ContainerScanner containerMgr)
        {
            CountAnyOne(row.Alternatives, containerMgr, out int invUnits, out int totalUnits, out Sprite bestIcon);
            if (bestIcon == null)
                bestIcon = row.Icon;

            if (totalUnits == row.LastKnownAmount && invUnits == row.LastKnownInvAmount
                && row.Icon == bestIcon && row.CachedAmountString != null)
                return false;

            row.LastKnownAmount = totalUnits;
            row.LastKnownInvAmount = invUnits;
            row.Icon = bestIcon;

            Color color = (invUnits >= row.RequiredAmount) ?
                           RecipePinnerPlugin.ColorEnoughInInventory.Value :
                           (totalUnits >= row.RequiredAmount) ?
                           RecipePinnerPlugin.ColorEnoughWithChests.Value :
                           RecipePinnerPlugin.ColorMissing.Value;

            string hex = "#" + ColorUtility.ToHtmlStringRGBA(color);
            row.CachedAmountString = $"<color={hex}>{totalUnits}/{row.RequiredAmount}</color>";
            return true;
        }

        // Shared with the Gathering List, so a summary cell there counts the same way as on a pin.
        private void CountAnyOne(List<PinnedResData> alternatives, ContainerScanner containerMgr,
            out int invUnits, out int totalUnits, out Sprite bestIcon)
        {
            invUnits = 0;
            totalUnits = 0;
            bestIcon = null;
            int bestAmount = -1;

            foreach (var alt in alternatives)
            {
                _reusableInvCounts.TryGetValue(alt.ItemName, out int inv);
                int chest = 0;
                if (RecipePinnerPlugin.EnableChestScanning.Value)
                    containerMgr.ContainerCache.TryGetValue(alt.ItemName, out chest);

                int total = inv + chest;
                int single = alt.SingleAmount > 0 ? alt.SingleAmount : 1;
                invUnits += inv / single;
                totalUnits += total / single;
                if (total > bestAmount)
                {
                    bestAmount = total;
                    bestIcon = alt.Icon;
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
                bool plank = UseInventoryBackground();
                if (plank != uiSlot.HasInventoryBackground)
                {
                    uiSlot.HasInventoryBackground = plank;
                    ApplyPanelBackground(uiSlot.BgImage, plank);
                    uiSlot.PlankInset = SetPlankInset(uiSlot.gameObject, uiSlot.PlankInset, plank);

                    // On the plank the readiness bar sits on the inset's left edge, inside the frame.
                    if (uiSlot.AccentBar != null)
                    {
                        RectTransform accentRect = uiSlot.AccentBar.rectTransform;
                        accentRect.anchoredPosition = new Vector2(plank ? PlankInset : 0f, 0f);
                        accentRect.sizeDelta = new Vector2(4f, plank ? -2f * PlankInset : 0f);
                    }
                }

                if (!plank)
                {
                    float currentAlpha = uiSlot.BgImage.color.a;
                    if (Mathf.Abs(currentAlpha - RecipePinnerPlugin.BackgroundOpacity.Value) > 0.01f)
                        uiSlot.BgImage.color = new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
                }
                else if (uiSlot.PlankInset != null
                    && Mathf.Abs(uiSlot.PlankInset.color.a - RecipePinnerPlugin.BackgroundOpacity.Value) > 0.01f)
                {
                    uiSlot.PlankInset.color = new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
                }
            }

            bool isBottomRight = (RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal);
            bool isSailing = (Player.m_localPlayer.GetControlledShip() != null);
            bool isInvOpen = ReflectionHelper.IsInventoryShown();
            bool shouldBeHorizontal = RecipePinnerPlugin.Instance.IsHorizontalMode || isSailing || isInvOpen;

            // Width of the pin box in the current layout. Horizontal mode applies it to the rect
            // below; both layouts pass it to UpdateData so the compact grid can size its columns.
            float slotWidth = shouldBeHorizontal
                ? ((isBottomRight || isSailing || isInvOpen)
                    ? RecipePinnerPlugin.BottomRightColumnWidth.Value
                    : RecipePinnerPlugin.HorizontalColumnWidth.Value)
                : RecipePinnerPlugin.VerticalListWidth.Value;

            // In the Gathering List's place the pin takes the list's width: the same cells and
            // column count, plus the 14 + 8 padding both are built with.
            GridLayoutGroup listCells = uiSlot.CompactResourceRoot != null ? uiSlot.CompactResourceRoot.GetComponent<GridLayoutGroup>() : null;
            if (listCells != null && isInvOpen && ShouldPlaceSinglePinBesideChest())
            {
                int listCols = RecipePinnerPlugin.GatheringListColumns?.Value ?? 4;
                slotWidth = listCols * (listCells.cellSize.x + listCells.spacing.x) - listCells.spacing.x + 22f;
            }

            // On the plank the padding grows by the inset on both sides, as the Gathering List's
            // width does; the box grows with it so the content keeps its width. UpdateData subtracts
            // that padding, so it gets this width too, or the compact grid loses a column.
            float rectWidth = uiSlot.HasInventoryBackground ? slotWidth + 2f * PlankInset : slotWidth;

            if (shouldBeHorizontal)
            {
                RectTransform slotRect = uiSlot.Rect ?? uiSlot.GetComponent<RectTransform>();
                // Half a unit, not one: the pin in the list's place is exactly one unit wider.
                if (Mathf.Abs(slotRect.sizeDelta.x - rectWidth) > 0.5f)
                    slotRect.sizeDelta = new Vector2(rectWidth, slotRect.sizeDelta.y);
            }

            bool dataChanged = (uiSlot.CurrentData != pinData);
            uiSlot.CurrentData = pinData;

            RefreshResourceAmountStrings(pinData, containerMgr);

            if (uiSlot.AccentBar != null)
            {
                if (RecipePinnerPlugin.EnableCraftReadiness.Value)
                {
                    if (!uiSlot.AccentBar.gameObject.activeSelf)
                        uiSlot.AccentBar.gameObject.SetActive(true);

                    bool allReady = true;
                    if (pinData.AnyOneRow != null)
                    {
                        allReady = pinData.AnyOneRow.LastKnownAmount >= pinData.AnyOneRow.RequiredAmount;
                    }
                    else
                    {
                        for (int r = 0; r < pinData.Resources.Count; r++)
                        {
                            if (pinData.Resources[r].LastKnownAmount < pinData.Resources[r].RequiredAmount)
                            {
                                allReady = false;
                                break;
                            }
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
            bool widthChanged = Mathf.Abs(rectWidth - uiSlot.LastSlotWidth) > 0.5f;
            bool uncapChanged = uncapCompact != uiSlot.LastUncap;
            int fitRows = GetFitRows(uiSlot);
            bool fitChanged = fitRows != uiSlot.LastFitRows;
            if (dataChanged || pinData.IsDirty || widthChanged || uncapChanged || fitChanged)
            {
                uiSlot.UpdateData(pinData, _cachedFont, rectWidth, uncapCompact, fitRows);
                uiSlot.LastSlotWidth = rectWidth;
                uiSlot.LastUncap = uncapCompact;
                uiSlot.LastFitRows = fitRows;
                pinData.IsDirty = false;
            }
        }

        // Rows a compact group may show so its card stays clear of what is around it while the
        // inventory is open: under the crafting window at the bottom-right, or above the health bar
        // (GetLowerBound) for a lone pin in the Gathering List's place; 0 for no limit. Measured
        // from the last frame's layout: the part of the card above the grid does not change with the
        // row count, so a change settles in one frame at any resolution and GUI scale.
        private int GetFitRows(PinSlotUI uiSlot)
        {
            if (!ReflectionHelper.IsInventoryShown()) return 0;
            bool belowInventory = ShouldPlaceSinglePinBesideChest();

            RectTransform crafting = InventoryGui.instance.m_crafting;
            RectTransform grid = uiSlot.CompactResourceRoot as RectTransform;
            RectTransform slotRect = uiSlot.Rect;
            if (slotRect == null)
                slotRect = uiSlot.GetComponent<RectTransform>();
            if (!belowInventory && (crafting == null || !crafting.gameObject.activeInHierarchy)) return 0;
            if (grid == null || !grid.gameObject.activeInHierarchy || slotRect == null) return 0;

            GridLayoutGroup glg = grid.GetComponent<GridLayoutGroup>();
            VerticalLayoutGroup slotVlg = uiSlot.GetComponent<VerticalLayoutGroup>();
            float scale = slotRect.lossyScale.y;
            if (glg == null || slotVlg == null || scale <= 0f) return 0;

            slotRect.GetWorldCorners(_slotCorners);
            grid.GetWorldCorners(_gridCorners);

            float cardRoom;
            if (belowInventory)
            {
                cardRoom = (_slotCorners[1].y - GetLowerBound(_slotCorners[0].x, _slotCorners[2].x)) / scale;
            }
            else
            {
                crafting.GetWorldCorners(_craftingCorners);
                // A pin left of the window has nothing above it.
                if (_slotCorners[2].x <= _craftingCorners[0].x) return 0;
                cardRoom = (_craftingCorners[0].y - _slotCorners[0].y) / scale - CraftingWindowClearance;
            }

            float aboveGrid = (_slotCorners[1].y - _gridCorners[1].y) / scale;
            float gridRoom = cardRoom - aboveGrid - slotVlg.padding.bottom - glg.padding.vertical;
            int rows = Mathf.FloorToInt((gridRoom + glg.spacing.y) / (glg.cellSize.y + glg.spacing.y));
            return Mathf.Max(1, rows);
        }

        // The lowest world y a panel spanning left..right may reach while the inventory is open:
        // above the game's health bar where the two overlap sideways, else the screen's bottom edge,
        // with the windows' own gap either way.
        private float GetLowerBound(float left, float right)
        {
            float bound = 0f;
            Hud hud = Hud.instance;
            RectTransform bar = hud != null ? hud.m_healthBarRoot : null;
            if (bar != null && bar.gameObject.activeInHierarchy)
            {
                bar.GetWorldCorners(_healthBarCorners);
                // The bar is rotated, so its corners are not in the usual order.
                float barLeft = Mathf.Min(Mathf.Min(_healthBarCorners[0].x, _healthBarCorners[1].x), Mathf.Min(_healthBarCorners[2].x, _healthBarCorners[3].x));
                float barRight = Mathf.Max(Mathf.Max(_healthBarCorners[0].x, _healthBarCorners[1].x), Mathf.Max(_healthBarCorners[2].x, _healthBarCorners[3].x));
                float barTop = Mathf.Max(Mathf.Max(_healthBarCorners[0].y, _healthBarCorners[1].y), Mathf.Max(_healthBarCorners[2].y, _healthBarCorners[3].y));
                if (barRight > left && barLeft < right)
                    bound = barTop;
            }

            float hudScale = hud != null && hud.m_rootObject != null ? hud.m_rootObject.transform.lossyScale.y : 1f;
            return bound + InventoryWindowGap * hudScale;
        }

        // At a large GUI scale not even one row of the bottom-right pins fits under the crafting
        // window. They are then made transparent while the inventory is open: only a mask, like
        // ShowPinsOnlyWithInventory, since the Gathering List and a lone pin sit under the inventory.
        private void UpdateCraftingRoomMask()
        {
            bool mask = false;
            InventoryGui gui = InventoryGui.instance;
            RectTransform rootRect = _pinListRoot as RectTransform;
            if (gui != null && ReflectionHelper.IsInventoryShown() && rootRect != null && !ShouldPlaceSinglePinBesideChest())
            {
                RectTransform crafting = gui.m_crafting;
                if (crafting != null && crafting.gameObject.activeInHierarchy)
                {
                    crafting.GetWorldCorners(_craftingCorners);
                    rootRect.GetWorldCorners(_rootCorners);
                    float clearance = CraftingWindowClearance * rootRect.lossyScale.y;
                    mask = _rootCorners[2].x > _craftingCorners[0].x
                        && _rootCorners[1].y > _craftingCorners[0].y - clearance;
                }
            }

            // A root rebuilt this frame replaces one whose destruction is still pending.
            if (_pinRootGroup == null || _pinRootGroup.transform != _pinListRoot)
            {
                if (_pinListRoot == null) return;
                _pinRootGroup = _pinListRoot.GetComponent<CanvasGroup>();
                if (_pinRootGroup == null)
                    _pinRootGroup = _pinListRoot.gameObject.AddComponent<CanvasGroup>();
            }

            float alpha = mask ? 0f : 1f;
            if (_pinRootGroup.alpha != alpha)
                _pinRootGroup.alpha = alpha;

            if (mask != _pinsMaskedForRoom)
            {
                _pinsMaskedForRoom = mask;
                DebugLogger.Log(mask
                    ? "Pins hidden while the inventory is open: no room under the crafting window"
                    : "Pins shown under the crafting window again");
            }
        }

        // Unity draws a parent's children in sibling order and SetParent appends, so the pin root
        // would land after the large map and draw over it, where vanilla's own HUD draws behind it.
        // Which root the map lives under is prefab data, so it is found here rather than assumed.
        private void EnsurePinsBehindMap()
        {
            if (_mapOrderChecked || _pinListRoot == null) return;

            Minimap map = Minimap.instance;
            if (map == null || map.m_largeRoot == null) return;

            _mapOrderChecked = true;

            Transform hudRoot = _pinListRoot.parent;
            if (hudRoot == null) return;

            Transform mapBranch = map.m_largeRoot.transform;
            while (mapBranch != null && mapBranch.parent != hudRoot)
                mapBranch = mapBranch.parent;

            if (mapBranch == null)
            {
                DebugLogger.Log("Large map is not under the pins' HUD root; draw order left unchanged");
                return;
            }

            int mapIndex = mapBranch.GetSiblingIndex();
            int pinIndex = _pinListRoot.GetSiblingIndex();
            if (pinIndex > mapIndex)
            {
                _pinListRoot.SetSiblingIndex(mapIndex);
                DebugLogger.Log($"Pins moved behind the large map: '{mapBranch.name}' was at {mapIndex}, pins were at {pinIndex}");
            }
            else
            {
                DebugLogger.Log($"Pins already behind the large map: '{mapBranch.name}' at {mapIndex}, pins at {pinIndex}");
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
            _mapOrderChecked = false;
            _rootRaised = false;
            _pinListRoot.localScale = Vector3.one * RecipePinnerPlugin.UIScale.Value;

            RectTransform rect = rootObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);

            bool isSailing = Player.m_localPlayer != null && Player.m_localPlayer.GetControlledShip() != null;
            bool isForcedBottomRight = RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal;
            bool isInventoryOpen = ReflectionHelper.IsInventoryShown();
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
            _pageDots.Clear();

            // Gathering List Panel
            if (RecipePinnerPlugin.EnableGatheringList.Value)
            {
                string title = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("gathering_title") ?? "TOTAL NEEDS";
                _gatheringListPanel = UIBuilder.CreateGatheringListPanel(_pinListRoot, _cachedFont, title);
                _gatheringListPanel.SetActive(_gatheringListVisible);

                // These three describe the panel, not the manager: a return to the main menu
                // replaces the panel without DestroyUI running, and they would carry over.
                _lastHintKey = null;
                _lastGatheringSlotCount = -1;
                _gatheringListRepositioned = false;
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
            bool isInventoryOpen = ReflectionHelper.IsInventoryShown();
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

            if (_rootRaised && !ShouldPlaceSinglePinBesideChest())
            {
                // Back behind the large map; EnsurePinsBehindMap measures it again.
                _rootRaised = false;
                _mapOrderChecked = false;
            }

            RectTransform rootRect = _pinListRoot.GetComponent<RectTransform>();

            if (shouldBeHorizontal)
            {
                if (ShouldPlaceSinglePinBesideChest())
                    PlaceSinglePinBesideChest(rootRect);
                else
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
                if (rootRect.anchorMin != new Vector2(1, 0) || rootRect.pivot != new Vector2(1, 0))
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

                // The offset rides on top of whatever the player has, so a customised position is
                // lifted clear of the bar too - and no value becomes unselectable.
                if (RecipePinnerPlugin.AvoidKeyHintBar.Value && IsKeyHintBarVisible())
                    targetPos.y += RecipePinnerPlugin.KeyHintBarOffset.Value;

                if (rootRect.anchoredPosition != targetPos)
                    rootRect.anchoredPosition = targetPos;
            }
            else
            {
                if (rootRect.anchorMin != new Vector2(1, 1) || rootRect.pivot != new Vector2(1, 1))
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

        // With exactly one pin and the Gathering List closed, the pin takes the list's place
        // (GetInventorySpot), with or without a chest: above the pins a lone group's grid would grow
        // into the crafting window. An open list keeps that place.
        private bool ShouldPlaceSinglePinBesideChest()
        {
            if (!RecipePinnerPlugin.SinglePinBesideChest.Value) return false;
            if (!ReflectionHelper.IsInventoryShown()) return false;

            var recipeMgr = RecipePinnerPlugin.Instance.RecipeMgr;
            if (recipeMgr == null || recipeMgr.CachedPins.Count != 1) return false;

            return !(_gatheringListVisible && _gatheringListPanel != null);
        }

        // Same place as RepositionGatheringListForInventory. The whole root moves, so the pin keeps
        // its layout; UpdateHorizontalLayout restores the pivot once this stops applying.
        private void PlaceSinglePinBesideChest(RectTransform rootRect)
        {
            RectTransform hudRect = _pinListRoot.parent as RectTransform;
            RectTransform containerRect = InventoryGui.instance != null ? InventoryGui.instance.m_container : null;
            if (hudRect == null || containerRect == null)
            {
                UpdateHorizontalLayout(rootRect, true);
                return;
            }

            Vector3 wantedPosition = GetInventorySpot(rootRect, containerRect, hudRect.lossyScale.x);

            // In the chest window's place the pin draws over the HUD as that window does; the large
            // map cannot open while the inventory is open.
            if (!_rootRaised)
            {
                _pinListRoot.SetAsLastSibling();
                _rootRaised = true;
            }

            Vector2 topLeftPivot = new Vector2(0f, 1f);
            if (rootRect.pivot != topLeftPivot)
                rootRect.pivot = topLeftPivot;
            if (rootRect.position != wantedPosition)
                rootRect.position = wantedPosition;
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

        // Valheim's button-hint bar runs along the bottom of the screen, and the game's own
        // setting for it defaults to on, so it covers a pin list anchored down there for most
        // players rather than a few. Read whether a bar is genuinely on screen instead of reading
        // the preference: the game also hides these during chat, while paused and while dead, and
        // the pins should not drift upward at those moments.
        private static bool IsKeyHintBarVisible()
        {
            KeyHints hints = KeyHints.instance;
            if (hints == null) return false;

            return IsHintObjectActive(hints.m_inventoryHints)
                || IsHintObjectActive(hints.m_inventoryWithContainerHints)
                || IsHintObjectActive(hints.m_combatHints)
                || IsHintObjectActive(hints.m_buildHints)
                || IsHintObjectActive(hints.m_fishingHints)
                || IsHintObjectActive(hints.m_barberHints);
        }

        private static bool IsHintObjectActive(GameObject hintObject)
        {
            if (hintObject == null) return false;
            return hintObject.activeInHierarchy;
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
