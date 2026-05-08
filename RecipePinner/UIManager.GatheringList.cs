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

            Transform hudRoot = Hud.instance?.m_rootObject?.transform;
            if (hudRoot != null && _gatheringListPanel.transform.parent != hudRoot)
            {
                _gatheringListPanel.transform.SetParent(hudRoot, false);
            }

            LayoutElement le = _gatheringListPanel.GetComponent<LayoutElement>()
                ?? _gatheringListPanel.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            RectTransform panelRect = _gatheringListPanel.PanelRect;
            float columnWidth = RecipePinnerPlugin.BottomRightColumnWidth?.Value ?? 265f;

            Vector2 offset = RecipePinnerPlugin.InventoryGatheringListPosition?.Value ?? new Vector2(-460f, 0f);

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = offset;
            panelRect.sizeDelta = new Vector2(columnWidth, panelRect.sizeDelta.y);
            panelRect.localScale = Vector3.one * RecipePinnerPlugin.UIScale.Value;

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

            _gatheringAggregator.Clear();
            _gatheringData.Clear();

            foreach (var pin in recipeMgr.CachedPins)
            {
                foreach (var res in pin.Resources)
                {
                    if (_gatheringAggregator.TryGetValue(res.ItemName, out GatheringItemData existing))
                    {
                        existing.TotalRequired += res.RequiredAmount;
                    }
                    else
                    {
                        var item = new GatheringItemData
                        {
                            ItemName = res.ItemName,
                            DisplayName = res.CachedName ?? res.ItemName,
                            Icon = res.Icon,
                            TotalRequired = res.RequiredAmount
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

            if (panel.HintText != null)
            {
                string keyName = RecipePinnerPlugin.HotkeyGatheringList.Value.ToString();
                if (_lastHintKey != keyName)
                {
                    _lastHintKey = keyName;
                    var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
                    string hintTemplate = locMgr?.GetText("gathering_hint") ?? "Open/Close: {0}";
                    panel.HintText.text = string.Format(hintTemplate, keyName);
                }
                panel.HintText.transform.SetAsLastSibling();
            }

            panel.RefreshLayout();
        }
    }
}
