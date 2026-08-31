using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public partial class UIManager
    {
        private GatheringListUI _gatheringListPanel;
        private bool _gatheringListVisible = false;
        private bool _gatheringListRepositioned = false;
        private readonly List<GatheringItemData> _gatheringData = new List<GatheringItemData>();
        private readonly Dictionary<string, GatheringItemData> _gatheringAggregator = new Dictionary<string, GatheringItemData>();
        private int _previousPinCount = 0;
        private string _lastHintKey = null;
        private int _lastGatheringSlotCount = -1;
        private int _gatheringStamp = 0;
        private static readonly Vector2 DefaultContainerGap = new Vector2(90f, 2f);
        private readonly Vector3[] _containerCorners = new Vector3[4];
        private bool _warnedLegacyContainerOffset = false;
        private string _hexEnough = null;
        private string _hexMissing = null;
        private Color _hexEnoughSource;
        private Color _hexMissingSource;

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
            _gatheringListPanel?.SetActive(false);

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

        private void RepositionGatheringListForInventory()
        {
            if (_gatheringListPanel == null) return;

            Transform hudRoot = Hud.instance?.m_rootObject?.transform;
            RectTransform panelRect = _gatheringListPanel.PanelRect;

            // C13: this method runs on every frame the chest stays open, so the parts that produce
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

                // Container mode: always 4 columns like vertical mode
                _gatheringListPanel.ApplyColumns(4);
            }

            Vector2 gap = GetContainerGatheringListGap();

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
                containerRect.GetWorldCorners(_containerCorners);
                Vector3 containerTopRight = _containerCorners[2];
                float canvasScale = hudRect.lossyScale.x;

                // Setting position rather than anchoredPosition keeps this independent of whatever
                // anchors and pivot the HUD root happens to use.
                panelRect.pivot = new Vector2(0f, 1f);
                panelRect.position = new Vector3(containerTopRight.x + gap.x * canvasScale,
                                                 containerTopRight.y + gap.y * canvasScale,
                                                 0f);
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

        private void RestoreGatheringListParent()
        {
            if (_gatheringListPanel == null || _pinListRoot == null) return;

            if (_gatheringListPanel.transform.parent != _pinListRoot)
            {
                _gatheringListPanel.transform.SetParent(_pinListRoot, false);
                _gatheringListPanel.transform.localScale = Vector3.one;
            }

            LayoutElement le = _gatheringListPanel.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = false;
        }

        private void UpdateGatheringList()
        {
            if (_gatheringListPanel == null) return;
            bool panelActive = _gatheringListPanel.gameObject.activeSelf;
            if (!_gatheringListVisible && !_gatheringListRepositioned && !panelActive) return;

            if (_gatheringListPanel.BgImage != null)
            {
                float currentAlpha = _gatheringListPanel.BgImage.color.a;
                if (Mathf.Abs(currentAlpha - RecipePinnerPlugin.BackgroundOpacity.Value) > 0.01f)
                    _gatheringListPanel.BgImage.color = new Color(0, 0, 0, RecipePinnerPlugin.BackgroundOpacity.Value);
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
                foreach (var res in pin.Resources)
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
                        _gatheringData.Add(item);
                    }
                    else
                    {
                        item.TotalRequired += res.RequiredAmount;
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

            // The two colours come from config and almost never change, so cache their hex strings.
            // When one does change, every cached slot string is stale, so force them all to rebuild.
            Color enoughColor = RecipePinnerPlugin.ColorEnoughInInventory.Value;
            Color missingColor = RecipePinnerPlugin.ColorMissing.Value;
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

            if (coloursChanged)
            {
                for (int c = 0; c < panel.ItemSlots.Count; c++)
                {
                    if (panel.ItemSlots[c] != null)
                        panel.ItemSlots[c].LastHave = int.MinValue;
                }
            }
            while (panel.ItemSlots.Count < _gatheringData.Count)
            {
                panel.ItemSlots.Add(UIBuilder.CreateGatheringItemSlot(panel.ItemListRoot, _cachedFont));
            }

            for (int i = 0; i < panel.ItemSlots.Count; i++)
            {
                if (i < _gatheringData.Count)
                {
                    var slot = panel.ItemSlots[i];
                    if (slot == null) continue;

                    var data = _gatheringData[i];

                    if (!slot.gameObject.activeSelf) slot.SetActive(true);

                    if (slot.Icon != null)
                        slot.Icon.sprite = data.Icon;

                    if (data.TotalHave != slot.LastHave
                        || data.TotalRequired != slot.LastRequired
                        || data.IsComplete != slot.LastComplete)
                    {
                        slot.LastHave = data.TotalHave;
                        slot.LastRequired = data.TotalRequired;
                        slot.LastComplete = data.IsComplete;

                        string hexColor = data.IsComplete ? _hexEnough : _hexMissing;
                        if (slot.AmountText != null)
                            slot.AmountText.text = $"<color={hexColor}>{data.TotalHave}/{data.TotalRequired}</color>";
                    }

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

            if (_lastGatheringSlotCount != _gatheringData.Count)
            {
                _lastGatheringSlotCount = _gatheringData.Count;
                panel.RefreshLayout();
            }
        }
    }
}
