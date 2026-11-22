using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public partial class UIManager
    {
        private GatheringListUI _gatheringListPanel;
        // Starts as the last launch left it; UpdateUI keeps the config value in step.
        private bool _gatheringListVisible = RecipePinnerPlugin.GatheringListOpen?.Value ?? false;
        private bool _gatheringListRepositioned = false;
        private readonly List<GatheringItemData> _gatheringData = new List<GatheringItemData>();
        private readonly Dictionary<string, GatheringItemData> _gatheringAggregator = new Dictionary<string, GatheringItemData>();
        private int _previousPinCount = 0;
        private bool _pinCountSeeded = false;
        private string _lastHintKey = null;
        private int _lastGatheringSlotCount = -1;
        private int _gatheringStamp = 0;
        private static readonly Vector2 DefaultContainerGap = new Vector2(90f, 10f);
        // The game's own gap between stacked inventory windows, in canvas units: between the player
        // window and a chest's, and between the info panel and the crafting window.
        private const float InventoryWindowGap = 10f;
        private readonly Vector3[] _containerCorners = new Vector3[4];
        private readonly Vector3[] _sideButtonCorners = new Vector3[4];
        private readonly Vector3[] _playerCorners = new Vector3[4];
        private readonly Vector3[] _listCorners = new Vector3[4];
        // As the compact grid's "+N" cell.
        private const int GatheringOverflowFontSize = 22;
        private const float ContainerSideButtonGap = 8f;
        private float _loggedSideButtonPush = 0f;
        private bool _warnedLegacyContainerOffset = false;
        private string _hexEnough = null;
        private string _hexMissing = null;
        private string _hexChests = null;
        private Color _hexEnoughSource;
        private Color _hexMissingSource;
        private Color _hexChestsSource;

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

            // This runs after the frame's UpdateUI. With the inventory open the next UpdateUI shows
            // the panel in its place; shown here it would draw for a frame at its old one.
            bool inventoryOpen = ReflectionHelper.IsInventoryShown();
            if (_gatheringListPanel != null && !(_gatheringListVisible && inventoryOpen))
                _gatheringListPanel.SetActive(_gatheringListVisible);
            _lastGatheringSlotCount = -1;

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
            if (_gatheringListPanel != null)
                _gatheringListPanel.SetActive(false);

            if (_gatheringListRepositioned)
            {
                RestoreGatheringListParent();
                _gatheringListRepositioned = false;
            }

            _gatheringData.Clear();
            _gatheringAggregator.Clear();

            if (_gatheringListPanel != null)
            {
                foreach (var slot in _gatheringListPanel.ItemSlots)
                {
                    if (slot != null && slot.gameObject.activeSelf)
                        slot.SetActive(false);
                }
            }
        }

        private void UpdateGatheringListPosition(bool isHorizontal, bool isBottomRight)
        {
            if (_gatheringListPanel == null) return;

            // While the inventory is open RepositionGatheringListForInventory places the panel.
            if (_gatheringListRepositioned) return;

            LayoutElement le = _gatheringListPanel.GetComponent<LayoutElement>()
                ?? _gatheringListPanel.gameObject.AddComponent<LayoutElement>();

            RectTransform panelRect = _gatheringListPanel.PanelRect;
            int glCols = RecipePinnerPlugin.GatheringListColumns?.Value ?? 4;

            if (isHorizontal)
            {
                le.ignoreLayout = true;
                _gatheringListPanel.ApplyColumns(glCols);

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
                // Vertical mode: always 4 columns, ignore config
                le.ignoreLayout = false;
                _gatheringListPanel.ApplyColumns(4);
            }
        }

        /// <summary>
        /// The gap between the container window's top-right corner and the Gathering List's
        /// top-left corner, in canvas units. Before this the setting was an offset from the screen
        /// centre whose default was (-400, 320), so a negative X means a value left over from that
        /// scheme; those fall back to the new default rather than throwing the panel off-screen.
        /// </summary>
        private Vector2 GetContainerGatheringListGap()
        {
            Vector2 configured = RecipePinnerPlugin.ContainerGatheringListPosition?.Value ?? DefaultContainerGap;

            if (configured.x < 0f)
            {
                if (!_warnedLegacyContainerOffset)
                {
                    _warnedLegacyContainerOffset = true;
                    DebugLogger.Log($"ContainerGatheringListPosition {configured} is in the pre-1.3.1 screen-centre format; using the default gap {DefaultContainerGap} instead. Set a positive X to choose your own.");
                }
                return DefaultContainerGap;
            }

            return configured;
        }

        /// <summary>
        /// World position of the top-left corner the Gathering List (or a lone pin) takes while the
        /// inventory is open. With a chest's window showing, beside it. Without one, in that window's
        /// own place: the game only hides it, so its corners still hold. It is also kept below the
        /// player inventory's bottom edge, which extra inventory rows move down.
        /// </summary>
        private Vector3 GetInventorySpot(RectTransform panelRect, RectTransform containerRect, float canvasScale)
        {
            containerRect.GetWorldCorners(_containerCorners);

            if (containerRect.gameObject.activeSelf)
            {
                Vector3 containerTopRight = _containerCorners[2];
                Vector2 gap = GetContainerGatheringListGap();
                float left = containerTopRight.x + gap.x * canvasScale;
                float top = containerTopRight.y + gap.y * canvasScale;
                left = ClearContainerSideButtons(panelRect, left, top, containerTopRight.x, canvasScale);
                return new Vector3(left, top, 0f);
            }

            // Without a chest the panel's own edges line up with what the player sees, so this
            // measures the windows' visible edges rather than their rects.
            VisibleEdge(containerRect).GetWorldCorners(_containerCorners);
            Vector3 containerTopLeft = _containerCorners[1];
            float spotTop = containerTopLeft.y;
            RectTransform playerRect = InventoryGui.instance != null ? InventoryGui.instance.m_player : null;
            if (playerRect != null)
            {
                VisibleEdge(playerRect).GetWorldCorners(_playerCorners);
                float belowPlayer = _playerCorners[0].y - InventoryWindowGap * canvasScale;
                if (belowPlayer < spotTop)
                    spotTop = belowPlayer;
            }
            return new Vector3(containerTopLeft.x, spotTop, 0f);
        }

        // A game window's visible edge: its "Bkg" image, which reaches past the window's own rect.
        // Falls back to the rect if a mod has replaced the image.
        private static RectTransform VisibleEdge(RectTransform window)
        {
            RectTransform bkg = window.Find("Bkg") as RectTransform;
            if (bkg == null)
                return window;
            return bkg;
        }

        private void RepositionGatheringListForInventory()
        {
            if (_gatheringListPanel == null) return;

            Transform hudRoot = Hud.instance?.m_rootObject?.transform;
            RectTransform panelRect = _gatheringListPanel.PanelRect;

            // This method runs on every frame the chest stays open, so the parts that produce
            // the same result every time are done once. Reparenting, the LayoutElement lookup and
            // the column count cannot change while the chest is open. The positioning below is
            // deliberately left per-frame: the container window slides in when the chest opens, so
            // placing the panel only once would freeze it at the animation's starting point.
            if (!_gatheringListRepositioned)
            {
                if (hudRoot != null && _gatheringListPanel.transform.parent != hudRoot)
                {
                    _gatheringListPanel.transform.SetParent(hudRoot, false);
                }

                LayoutElement le = _gatheringListPanel.GetComponent<LayoutElement>()
                    ?? _gatheringListPanel.gameObject.AddComponent<LayoutElement>();
                le.ignoreLayout = true;

                // The inventory layouts are horizontal, so the column setting applies here too.
                _gatheringListPanel.ApplyColumns(RecipePinnerPlugin.GatheringListColumns?.Value ?? 4);
            }

            // The container window is anchored to the screen's top-left and this panel used to be
            // anchored to the screen centre. Both are scaled by the game's GUI setting, but from
            // different origins, so they drift apart at any scale other than the one the old
            // default was tuned at. Position from the window's own corner instead and the panel
            // follows it at every scale and resolution.
            RectTransform hudRect = hudRoot as RectTransform;
            RectTransform containerRect = InventoryGui.instance != null ? InventoryGui.instance.m_container : null;
            bool placed = false;

            if (hudRect != null && containerRect != null)
            {
                Vector3 wantedPosition = GetInventorySpot(panelRect, containerRect, hudRect.lossyScale.x);

                // Setting position rather than anchoredPosition keeps this independent of whatever
                // anchors and pivot the HUD root happens to use. Both are assigned only when they
                // differ, for the same reason as localScale below.
                Vector2 topLeftPivot = new Vector2(0f, 1f);
                if (panelRect.pivot != topLeftPivot)
                    panelRect.pivot = topLeftPivot;
                if (panelRect.position != wantedPosition)
                    panelRect.position = wantedPosition;
                placed = true;
            }

            if (!placed)
            {
                // The container rect is unavailable: fall back to the old screen-centre placement
                // rather than dropping the panel in a corner.
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 1f);
                panelRect.anchoredPosition = new Vector2(-400f, 320f);
            }

            // UIScale is a live setting, so this stays per-frame - but only assign when it actually
            // differs, since writing localScale dirties the transform even with an identical value.
            Vector3 wantedScale = Vector3.one * RecipePinnerPlugin.UIScale.Value;
            if (panelRect.localScale != wantedScale)
                panelRect.localScale = wantedScale;

            _gatheringListRepositioned = true;
        }

        /// <summary>
        /// Moves the list's left edge past anything beside the Take All button that sticks out of
        /// the container window. Quick Stack Store parents its buttons there; measuring the siblings
        /// instead of detecting the mod also covers its hidden-button settings and any other mod.
        /// </summary>
        private float ClearContainerSideButtons(RectTransform panelRect, float left, float top, float windowRight, float canvasScale)
        {
            Button takeAll = InventoryGui.instance != null ? InventoryGui.instance.m_takeAllButton : null;
            if (takeAll == null) return left;
            Transform group = takeAll.transform.parent;
            if (group == null) return left;

            float bottom = top - panelRect.rect.height * panelRect.lossyScale.y;
            float cleared = left;

            for (int i = 0; i < group.childCount; i++)
            {
                RectTransform child = group.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy) continue;

                child.GetWorldCorners(_sideButtonCorners);
                if (_sideButtonCorners[2].x <= windowRight) continue;
                if (_sideButtonCorners[0].y >= top || _sideButtonCorners[1].y <= bottom) continue;

                float needed = _sideButtonCorners[2].x + ContainerSideButtonGap * canvasScale;
                if (needed > cleared) cleared = needed;
            }

            float push = canvasScale > 0f ? (cleared - left) / canvasScale : 0f;
            if (Mathf.Abs(push - _loggedSideButtonPush) > 0.5f)
            {
                _loggedSideButtonPush = push;
                DebugLogger.Log($"Gathering List beside a chest: pushed {push:F0} units right to clear buttons beside Take All.");
            }

            return cleared;
        }

        private void RestoreGatheringListParent()
        {
            if (_gatheringListPanel == null || _pinListRoot == null) return;

            if (_gatheringListPanel.transform.parent != _pinListRoot)
            {
                _gatheringListPanel.transform.SetParent(_pinListRoot, false);
                _gatheringListPanel.transform.localScale = Vector3.one;
            }

            // Placed for the root's layout at once: handed back to the layout for a frame, a
            // horizontal row takes the list for one more pin and stretches every pin to its height.
            _gatheringListRepositioned = false;
            bool horizontal = _pinListRoot.GetComponent<HorizontalLayoutGroup>() != null;
            bool forcedBottomRight = RecipePinnerPlugin.LayoutModeConfig.Value == RecipePinnerPlugin.PinLayoutMode.ForceBottomRightHorizontal;
            bool sailing = !forcedBottomRight && Player.m_localPlayer != null && Player.m_localPlayer.GetControlledShip() != null;
            UpdateGatheringListPosition(horizontal, forcedBottomRight || sailing || ReflectionHelper.IsInventoryShown());
        }

        private void UpdateGatheringList()
        {
            if (_gatheringListPanel == null) return;
            bool panelActive = _gatheringListPanel.gameObject.activeSelf;
            if (!_gatheringListVisible && !_gatheringListRepositioned && !panelActive) return;

            if (_gatheringListPanel.BgImage != null)
            {
                bool plank = UseInventoryBackground();
                if (plank != _gatheringListPanel.HasInventoryBackground)
                {
                    _gatheringListPanel.HasInventoryBackground = plank;
                    ApplyPanelBackground(_gatheringListPanel.BgImage, plank);
                    _gatheringListPanel.PlankInset = SetPlankInset(_gatheringListPanel.gameObject, _gatheringListPanel.PlankInset, plank);
                    // The panel's width includes the padding the plank just changed; applied here, or
                    // the list keeps the old width until its placement next runs.
                    bool rowLayout = _gatheringListRepositioned
                        || (_pinListRoot != null && _pinListRoot.GetComponent<HorizontalLayoutGroup>() != null);
                    _gatheringListPanel.ApplyColumns(rowLayout ? (RecipePinnerPlugin.GatheringListColumns?.Value ?? 4) : 4);
                }

                if (!plank)
                {
                    float currentAlpha = _gatheringListPanel.BgImage.color.a;
                    if (Mathf.Abs(currentAlpha - RecipePinnerPlugin.BackgroundOpacity.Value) > 0.01f)
                        _gatheringListPanel.BgImage.color = new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
                }
                else if (_gatheringListPanel.PlankInset != null
                    && Mathf.Abs(_gatheringListPanel.PlankInset.color.a - RecipePinnerPlugin.BackgroundOpacity.Value) > 0.01f)
                {
                    _gatheringListPanel.PlankInset.color = new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
                }
            }

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return;

            var containerMgr = RecipePinnerPlugin.Instance.ContainerMgr;

            // The aggregator keeps its entries between frames; Stamp marks the ones seen this frame.
            // _gatheringData.Clear() keeps the list's backing array, so refilling it allocates nothing.
            _gatheringStamp++;
            _gatheringData.Clear();

            foreach (var pin in recipeMgr.CachedPins)
            {
                // A recipe crafted from any one requirement counts as its summary row.
                if (pin.AnyOneRow != null)
                {
                    AddGatheringResource(pin.AnyOneRow);
                    continue;
                }

                foreach (var res in pin.Resources)
                    AddGatheringResource(res);
            }

            // Calculate totals
            foreach (var item in _gatheringData)
            {
                if (item.Alternatives != null)
                {
                    CountAnyOne(item.Alternatives, containerMgr, out int invUnits, out int totalUnits, out Sprite bestIcon);
                    item.TotalHave = totalUnits;
                    item.IsComplete = totalUnits >= item.TotalRequired;
                    item.InInventory = invUnits >= item.TotalRequired;
                    if (bestIcon != null)
                        item.Icon = bestIcon;
                    continue;
                }

                _reusableInvCounts.TryGetValue(item.ItemName, out int invCount);

                int chestCount = 0;
                if (RecipePinnerPlugin.EnableChestScanning.Value)
                    containerMgr.ContainerCache.TryGetValue(item.ItemName, out chestCount);

                item.TotalHave = invCount + chestCount;
                item.IsComplete = item.TotalHave >= item.TotalRequired;
                item.InInventory = invCount >= item.TotalRequired;
            }

            // Update UI slots
            var panel = _gatheringListPanel;

            // The three colours come from config and almost never change, so cache their hex strings.
            // When one does change, every cached slot string is stale, so force them all to rebuild.
            Color enoughColor = RecipePinnerPlugin.ColorEnoughInInventory.Value;
            Color missingColor = RecipePinnerPlugin.ColorMissing.Value;
            Color chestsColor = RecipePinnerPlugin.ColorEnoughWithChests.Value;
            bool coloursChanged = false;

            if (_hexEnough == null || enoughColor != _hexEnoughSource)
            {
                _hexEnoughSource = enoughColor;
                _hexEnough = "#" + ColorUtility.ToHtmlStringRGBA(enoughColor);
                coloursChanged = true;
            }

            if (_hexMissing == null || missingColor != _hexMissingSource)
            {
                _hexMissingSource = missingColor;
                _hexMissing = "#" + ColorUtility.ToHtmlStringRGBA(missingColor);
                coloursChanged = true;
            }

            if (_hexChests == null || chestsColor != _hexChestsSource)
            {
                _hexChestsSource = chestsColor;
                _hexChests = "#" + ColorUtility.ToHtmlStringRGBA(chestsColor);
                coloursChanged = true;
            }

            if (coloursChanged)
            {
                for (int c = 0; c < panel.ItemSlots.Count; c++)
                {
                    if (panel.ItemSlots[c] != null)
                        panel.ItemSlots[c].LastHave = int.MinValue;
                }
            }
            int cellCap = GetGatheringListCellCap();
            int visibleCount = _gatheringData.Count;
            int overflow = 0;
            if (cellCap > 0 && _gatheringData.Count > cellCap)
            {
                // The counter needs a cell of its own, so one fewer material is shown.
                visibleCount = cellCap - 1;
                overflow = _gatheringData.Count - visibleCount;
            }
            int shownSlots = visibleCount + (overflow > 0 ? 1 : 0);

            while (panel.ItemSlots.Count < shownSlots)
            {
                panel.ItemSlots.Add(UIBuilder.CreateGatheringItemSlot(panel.ItemListRoot, _cachedFont));
            }

            for (int i = 0; i < panel.ItemSlots.Count; i++)
            {
                if (i < visibleCount)
                {
                    var slot = panel.ItemSlots[i];
                    if (slot == null) continue;

                    var data = _gatheringData[i];

                    if (!slot.gameObject.activeSelf) slot.SetActive(true);
                    if (slot.IsOverflow) RestoreMaterialCell(slot);

                    if (slot.Icon != null)
                        slot.Icon.sprite = data.Icon;

                    if (data.TotalHave != slot.LastHave
                        || data.TotalRequired != slot.LastRequired
                        || data.IsComplete != slot.LastComplete
                        || data.InInventory != slot.LastInInventory)
                    {
                        slot.LastHave = data.TotalHave;
                        slot.LastRequired = data.TotalRequired;
                        slot.LastComplete = data.IsComplete;
                        slot.LastInInventory = data.InInventory;

                        string hexColor = data.InInventory ? _hexEnough : (data.IsComplete ? _hexChests : _hexMissing);
                        if (slot.AmountText != null)
                            slot.AmountText.text = $"<color={hexColor}>{data.TotalHave}/{data.TotalRequired}</color>";
                    }

                }
                else if (overflow > 0 && i == visibleCount)
                {
                    var slot = panel.ItemSlots[i];
                    if (slot == null) continue;
                    if (!slot.gameObject.activeSelf) slot.SetActive(true);
                    ShowOverflowCell(slot, overflow);
                }
                else
                {
                    var slot = panel.ItemSlots[i];
                    if (slot != null && slot.gameObject.activeSelf)
                        slot.SetActive(false);
                }
            }

            if (panel.HintText != null)
            {
                string keyName = RecipePinnerPlugin.HotkeyGatheringList.Value.ToString();
                if (_lastHintKey != keyName)
                {
                    _lastHintKey = keyName;
                    var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
                    string hintTemplate = locMgr?.GetText("gathering_hint") ?? "Open/Close: {0}";
                    panel.HintText.text = string.Format(hintTemplate, keyName);
                    panel.HintText.transform.SetAsLastSibling();
                }
            }

            if (_lastGatheringSlotCount != shownSlots)
            {
                _lastGatheringSlotCount = shownSlots;
                panel.RefreshLayout();
            }
        }

        // How many cells fit between the list's top and the lowest point it may reach while it is
        // placed for the inventory (GetLowerBound); 0 for no limit. Measured from the last frame's
        // layout: the title and the hint do not change with the row count, so a change settles in
        // one frame.
        private int GetGatheringListCellCap()
        {
            if (!_gatheringListRepositioned || _gatheringListPanel == null) return 0;

            RectTransform panelRect = _gatheringListPanel.PanelRect;
            RectTransform gridRect = _gatheringListPanel.ItemListRoot as RectTransform;
            GridLayoutGroup grid = gridRect != null ? gridRect.GetComponent<GridLayoutGroup>() : null;
            if (panelRect == null || grid == null) return 0;
            float scale = panelRect.lossyScale.y;
            if (scale <= 0f) return 0;

            panelRect.GetWorldCorners(_listCorners);
            float room = (_listCorners[1].y - GetLowerBound(_listCorners[0].x, _listCorners[2].x)) / scale;
            float aroundGrid = panelRect.rect.height - gridRect.rect.height;
            float gridRoom = room - aroundGrid - grid.padding.vertical;
            int rows = Mathf.FloorToInt((gridRoom + grid.spacing.y) / (grid.cellSize.y + grid.spacing.y));
            return Mathf.Max(1, rows) * Mathf.Max(1, grid.constraintCount);
        }

        // One "+N" cell for the materials that do not fit, drawn as the compact grid's: no icon and a
        // larger amount stretched to the cell's height, so it reads centred.
        private void ShowOverflowCell(GatheringItemUI slot, int overflow)
        {
            if (!slot.IsOverflow)
            {
                slot.IsOverflow = true;
                slot.LastOverflow = -1;
                if (slot.Icon != null)
                    slot.Icon.gameObject.SetActive(false);
                if (slot.AmountText != null)
                {
                    slot.AmountText.fontSize = GatheringOverflowFontSize;
                    LayoutElement le = slot.AmountText.GetComponent<LayoutElement>();
                    GridLayoutGroup grid = _gatheringListPanel.ItemListRoot.GetComponent<GridLayoutGroup>();
                    if (le != null && grid != null)
                        le.minHeight = grid.cellSize.y;
                }
            }

            if (slot.LastOverflow != overflow)
            {
                slot.LastOverflow = overflow;
                if (slot.AmountText != null)
                    slot.AmountText.text = "+" + overflow;
            }
        }

        // Undoes ShowOverflowCell when the cell holds a material again.
        private static void RestoreMaterialCell(GatheringItemUI slot)
        {
            slot.IsOverflow = false;
            if (slot.Icon != null)
                slot.Icon.gameObject.SetActive(true);
            if (slot.AmountText != null)
            {
                slot.AmountText.fontSize = RecipePinnerPlugin.GatheringListFontSizeMaterials != null
                    ? RecipePinnerPlugin.GatheringListFontSizeMaterials.Value : 15;
                LayoutElement le = slot.AmountText.GetComponent<LayoutElement>();
                if (le != null)
                    le.minHeight = 16f;
            }
            // The text is the counter's; force the amount to be written again.
            slot.LastHave = int.MinValue;
        }

        // The aggregator keeps its entries between frames; Stamp marks the ones seen this frame.
        private void AddGatheringResource(PinnedResData res)
        {
            if (!_gatheringAggregator.TryGetValue(res.ItemName, out GatheringItemData item))
            {
                item = new GatheringItemData { ItemName = res.ItemName };
                _gatheringAggregator[res.ItemName] = item;
            }

            if (item.Stamp != _gatheringStamp)
            {
                item.Stamp = _gatheringStamp;
                item.Icon = res.Icon;
                item.TotalRequired = res.RequiredAmount;
                item.Alternatives = res.Alternatives;
                _gatheringData.Add(item);
            }
            else
            {
                item.TotalRequired += res.RequiredAmount;
            }
        }
    }
}
