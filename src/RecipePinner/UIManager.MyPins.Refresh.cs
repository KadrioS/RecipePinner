using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public partial class UIManager
    {
        public void RefreshMyPinsList()
        {
            if (_myPinsPanel == null || !_myPinsPanel.gameObject.activeSelf) return;

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return;

            List<MyPinDisplayItem> displayItems = new List<MyPinDisplayItem>();

            // Group claims are subtracted so only excess copies appear as individual rows.
            Dictionary<string, int> groupClaimCounts = new Dictionary<string, int>();
            foreach (var grp in recipeMgr.PinGroups.Values)
            {
                foreach (string mk in grp.MemberRecipeKeys)
                {
                    int memberClaim = grp.MemberCounts.TryGetValue(mk, out int mc) ? mc : 1;
                    if (groupClaimCounts.ContainsKey(mk))
                        groupClaimCounts[mk] += memberClaim;
                    else
                        groupClaimCounts[mk] = memberClaim;
                }
            }

            Dictionary<string, MyPinDisplayItem> groupDisplayItems = new Dictionary<string, MyPinDisplayItem>();
            foreach (var grpKvp in recipeMgr.PinGroups)
            {
                PinGroupData grp = grpKvp.Value;
                groupDisplayItems[grpKvp.Key] = new MyPinDisplayItem
                {
                    Key = grp.GroupName,
                    DisplayName = grp.GroupName,
                    Icon = null,
                    Count = grp.MemberRecipeKeys.Count,
                    IsGroup = true,
                    GroupData = grp
                };
            }

            // Build display items in the same order as the HUD cache.
            foreach (string entry in recipeMgr.GetDisplayPinOrder())
            {
                if (entry.StartsWith("GROUP:"))
                {
                    // Groups cannot be selected for a new group, so hide them entirely while
                    // selection mode is active instead of showing rows that do nothing.
                    if (_isSelectionMode) continue;

                    string groupName = entry.Substring(6);
                    if (groupDisplayItems.TryGetValue(groupName, out MyPinDisplayItem gdi))
                    {
                        displayItems.Add(gdi);

                        if (_expandedGroups.Contains(groupName) && gdi.GroupData != null)
                        {
                            foreach (string memberKey in gdi.GroupData.MemberRecipeKeys)
                            {
                                int memberCount = gdi.GroupData.MemberCounts.TryGetValue(memberKey, out int mc) ? mc : 1;

                                string memberName = memberKey;
                                Sprite memberIcon = null;
                                PinnedRecipeData memberData = null;

                                foreach (var candidate in gdi.GroupData.MemberPins)
                                {
                                    if (candidate == null) continue;

                                    if ((candidate.RecipeRef != null && candidate.RecipeRef.name == memberKey) || candidate.RawName == memberKey)
                                    {
                                        memberData = candidate;
                                        break;
                                    }
                                }

                                if (memberData != null)
                                {
                                    memberName = string.IsNullOrEmpty(memberData.CachedHeader) ? memberKey : memberData.CachedHeader;
                                    string countPrefix = memberCount + "x ";
                                    if (memberName.StartsWith(countPrefix))
                                        memberName = memberName.Substring(countPrefix.Length);
                                    memberIcon = memberData.Icon;
                                }
                                else
                                {
                                    Recipe memberRecipe = recipeMgr.GetRecipeByName(memberKey);
                                    if (memberRecipe != null && memberRecipe.m_item != null && memberRecipe.m_item.m_itemData != null)
                                    {
                                        memberIcon = memberRecipe.m_item.m_itemData.GetIcon();
                                        string rawToken = memberRecipe.m_item.m_itemData.m_shared != null ? memberRecipe.m_item.m_itemData.m_shared.m_name : null;
                                        if (Localization.instance != null && !string.IsNullOrEmpty(rawToken))
                                            memberName = Localization.instance.Localize(rawToken);

                                        if (memberRecipe.m_amount > 1) memberName += $" (x{memberRecipe.m_amount})";
                                    }
                                    else
                                    {
                                        foreach (var cached in recipeMgr.CachedPins)
                                        {
                                            if (!cached.IsGroup && cached.RecipeRef != null && cached.RecipeRef.name == memberKey)
                                            {
                                                memberName = cached.CachedHeader;
                                                memberIcon = cached.Icon;
                                                break;
                                            }
                                        }
                                    }
                                }

                                displayItems.Add(new MyPinDisplayItem
                                {
                                    Key = memberKey,
                                    DisplayName = memberName,
                                    Icon = memberIcon,
                                    Count = memberCount,
                                    IsGroup = false,
                                    IsSubItem = true,
                                    ParentGroupName = groupName,
                                    GroupData = null
                                });
                            }
                        }
                    }
                    continue;
                }

                if (!recipeMgr.PinnedRecipes.TryGetValue(entry, out int individualCount)) continue;

                if (groupClaimCounts.TryGetValue(entry, out int claims))
                {
                    individualCount = individualCount - claims;
                    if (individualCount <= 0) continue; // No excess, skip
                }

                string displayName = entry;
                Sprite icon = null;

                Recipe recipe = recipeMgr.GetRecipeByName(entry);
                if (recipe != null && recipe.m_item != null && recipe.m_item.m_itemData != null)
                {
                    icon = recipe.m_item.m_itemData.GetIcon();
                    string rawToken = recipe.m_item.m_itemData.m_shared != null ? recipe.m_item.m_itemData.m_shared.m_name : null;
                    if (Localization.instance != null && !string.IsNullOrEmpty(rawToken))
                        displayName = Localization.instance.Localize(rawToken);

                    // Same suffix the HUD uses, so a 1x and a 5x recipe of the same item are
                    // not two identical-looking rows.
                    if (recipe.m_amount > 1) displayName += $" (x{recipe.m_amount})";
                }
                else
                {
                    foreach (var cached in recipeMgr.CachedPins)
                    {
                        if (!cached.IsGroup && cached.RecipeRef != null && cached.RecipeRef.name == entry)
                        {
                            displayName = cached.CachedHeader;
                            icon = cached.Icon;
                            break;
                        }
                    }
                }

                displayItems.Add(new MyPinDisplayItem
                {
                    Key = entry,
                    DisplayName = displayName,
                    Icon = icon,
                    Count = individualCount,
                    IsGroup = false,
                    IsSubItem = false,
                    ParentGroupName = null,
                    GroupData = null
                });
            }

            // Pool rows are only added; extra rows are hidden and reset below.
            while (_myPinsPanel.PinItems.Count < displayItems.Count)
            {
                MyPinItemUI newItem = UIBuilder.CreateMyPinItem(_myPinsPanel.PinListRoot, _cachedFont);
                _myPinsPanel.PinItems.Add(newItem);
                DebugLogger.Log($"Created pool item #{_myPinsPanel.PinItems.Count} under {_myPinsPanel.PinListRoot.name}");
            }

            for (int i = 0; i < _myPinsPanel.PinItems.Count; i++)
            {
                MyPinItemUI slot = _myPinsPanel.PinItems[i];

                if (i < displayItems.Count)
                {
                    var data = displayItems[i];
                    if (!slot.gameObject.activeSelf) slot.SetActive(true);

                    slot.RecipeKey = data.Key;
                    slot.IsGroupItem = data.IsGroup;
                    slot.IsSubItem = data.IsSubItem;
                    slot.ParentGroupName = data.ParentGroupName;

                    slot.SetSubItemStyle(data.IsSubItem);

                    if (slot.NameText != null)
                        slot.NameText.text = data.DisplayName;

                    if (slot.CountText != null)
                    {
                        if (data.IsGroup)
                            slot.CountText.text = $"({data.Count})";
                        else
                            slot.CountText.text = data.Count > 1 ? $"x{data.Count}" : "";
                    }

                    // Fully reset dynamic icon children to prevent stale group/normal visuals.
                    if (slot.IconRoot != null)
                    {
                        for (int c = slot.IconRoot.childCount - 1; c >= 0; c--)
                        {
                            GameObject child = slot.IconRoot.GetChild(c).gameObject;
                            if (child.name == "Icon")
                                continue;
                            Object.Destroy(child);
                        }

                        LayoutElement iconRootLe = slot.IconRoot.GetComponent<LayoutElement>();
                        if (iconRootLe != null)
                        {
                            iconRootLe.minWidth = 30;
                            iconRootLe.preferredWidth = 30;
                            iconRootLe.minHeight = 30;
                            iconRootLe.preferredHeight = 30;
                        }
                    }

                    if (data.IsGroup && data.GroupData != null)
                    {
                        if (slot.Icon != null) slot.Icon.gameObject.SetActive(false);
                        var (widget, countTxt) = UIBuilder.CreateGroupIconWidget(slot.IconRoot, _cachedFont, 30);
                        countTxt.text = data.GroupData.MemberRecipeKeys.Count.ToString();
                    }
                    else
                    {
                        if (slot.Icon != null)
                        {
                            slot.Icon.gameObject.SetActive(data.Icon != null);
                            slot.Icon.sprite = data.Icon;
                        }
                    }

                    string capturedKey = data.Key;
                    string capturedName = data.DisplayName;
                    bool capturedIsGroup = data.IsGroup;
                    bool capturedIsSubItem = data.IsSubItem;
                    string capturedParent = data.ParentGroupName;
                    int capturedCount = data.Count;

                    if (slot.ExpandButton != null)
                    {
                        slot.ExpandButton.gameObject.SetActive(capturedIsGroup);
                        if (capturedIsGroup)
                        {
                            bool isExpanded = _expandedGroups.Contains(capturedKey);
                            if (slot.ExpandButtonText != null)
                                slot.ExpandButtonText.text = isExpanded ? "\u25BC" : "\u25BA"; // ▼ or ►

                            slot.ExpandButton.onClick.RemoveAllListeners();
                            slot.ExpandButton.onClick.AddListener(() => OnExpandToggle(capturedKey));
                            slot.ExpandButton.onClick.AddListener(UIBuilder.PlayButtonSFX);
                        }
                    }

                    if (slot.DisbandButton != null)
                    {
                        slot.DisbandButton.gameObject.SetActive(capturedIsGroup);
                        if (capturedIsGroup)
                        {
                            slot.DisbandButton.onClick.RemoveAllListeners();
                            slot.DisbandButton.onClick.AddListener(() => OnDisbandGroup(capturedKey, capturedName));
                            slot.DisbandButton.onClick.AddListener(UIBuilder.PlayButtonSFX);
                        }
                    }

                    if (slot.DeleteButton != null)
                    {
                        slot.DeleteButton.onClick.RemoveAllListeners();
                        if (capturedIsSubItem)
                        {
                            slot.DeleteButton.onClick.AddListener(() => OnSubItemDelete(capturedParent, capturedKey, capturedName));
                            slot.DeleteButton.gameObject.SetActive(true);
                        }
                        else
                        {
                            slot.DeleteButton.onClick.AddListener(() => OnPinDelete(capturedKey, capturedName, capturedIsGroup));
                            slot.DeleteButton.gameObject.SetActive(true);
                        }
                        slot.DeleteButton.onClick.AddListener(UIBuilder.PlayButtonSFX);
                    }

                    if (slot.PlusButton != null)
                    {
                        slot.PlusButton.onClick.RemoveAllListeners();
                        if (!capturedIsGroup)
                        {
                            if (capturedIsSubItem)
                                slot.PlusButton.onClick.AddListener(() => OnSubItemCountChange(capturedParent, capturedKey, 1));
                            else
                                slot.PlusButton.onClick.AddListener(() => OnPinCountChange(capturedKey, 1));
                        }
                        slot.PlusButton.onClick.AddListener(UIBuilder.PlayButtonSFX);
                        slot.PlusButton.gameObject.SetActive(!capturedIsGroup);
                    }

                    if (slot.MinusButton != null)
                    {
                        slot.MinusButton.onClick.RemoveAllListeners();
                        bool showMinus = !capturedIsGroup && capturedCount > 1;
                        if (showMinus)
                        {
                            if (capturedIsSubItem)
                                slot.MinusButton.onClick.AddListener(() => OnSubItemCountChange(capturedParent, capturedKey, -1));
                            else
                                slot.MinusButton.onClick.AddListener(() => OnPinCountChange(capturedKey, -1));
                        }
                        slot.MinusButton.onClick.AddListener(UIBuilder.PlayButtonSFX);
                        slot.MinusButton.gameObject.SetActive(showMinus);
                    }

                    slot.SetSelectionMode(_isSelectionMode);
                }
                else
                {
                    if (slot.gameObject.activeSelf)
                    {
                        slot.SetActive(false);
                        if (slot.NameText != null) slot.NameText.text = "";
                        if (slot.CountText != null) slot.CountText.text = "";
                        if (slot.Icon != null) slot.Icon.sprite = null;
                        slot.RecipeKey = null;
                    }
                }
            }

            if (_myPinsPanel.EmptyText != null)
            {
                _myPinsPanel.EmptyText.gameObject.SetActive(displayItems.Count == 0);
            }

            int ungroupedCount = 0;
            foreach (var item in displayItems)
            {
                if (!item.IsGroup && !item.IsSubItem) ungroupedCount++;
            }
            // Force layout recalculation after pool visibility changes.
            Canvas.ForceUpdateCanvases();
            _myPinsPanel.RefreshLayout();
            DebugLogger.Verbose($"My Pins list refreshed: {displayItems.Count} items ({ungroupedCount} individual)");
        }
    }

    /// <summary>
    /// Internal display data for My Pins list items.
    /// </summary>
    internal class MyPinDisplayItem
    {
        public string Key;
        public string DisplayName;
        public Sprite Icon;
        public int Count;
        public bool IsGroup;
        public bool IsSubItem;
        public string ParentGroupName;
        public PinGroupData GroupData;
    }
}
