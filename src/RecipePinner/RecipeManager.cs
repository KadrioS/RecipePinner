using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public partial class RecipeManager
    {
        public Dictionary<string, int> PinnedRecipes = new Dictionary<string, int>();
        public List<string> PinnedRecipeOrder = new List<string>(); // Tracks insertion order
        public List<PinnedRecipeData> CachedPins = new List<PinnedRecipeData>();
        public Dictionary<string, PinGroupData> PinGroups = new Dictionary<string, PinGroupData>();

        private readonly Dictionary<string, Recipe> _fakeRecipeCache = new Dictionary<string, Recipe>();

        private static readonly Regex CleanNameRegex = new Regex("<.*?>", RegexOptions.Compiled);
        private static readonly Regex AmountSuffixRegex = new Regex(@"\s*[xX]?\s*\d+$", RegexOptions.Compiled);
        private static readonly Regex UpgradeStarRegex = new Regex(@"\s*★(\d+)$", RegexOptions.Compiled);

        private static readonly Dictionary<System.Type, FieldInfo> _cachedRecipeFields = new Dictionary<System.Type, FieldInfo>();
        private static readonly Dictionary<System.Type, PropertyInfo> _cachedRecipeProps = new Dictionary<System.Type, PropertyInfo>();
        private static readonly Dictionary<System.Type, FieldInfo> _cachedItemFields = new Dictionary<System.Type, FieldInfo>();
        private static readonly Dictionary<System.Type, PropertyInfo> _cachedItemProps = new Dictionary<System.Type, PropertyInfo>();
        private static readonly Dictionary<System.Type, PropertyInfo> _cachedElementProps = new Dictionary<System.Type, PropertyInfo>();
        private static readonly Dictionary<System.Type, FieldInfo> _cachedElementFields = new Dictionary<System.Type, FieldInfo>();
        private static readonly HashSet<System.Type> _elementLookupFailed = new HashSet<System.Type>();

        public void Cleanup()
        {
            DebugLogger.Log("RecipeManager cleanup");
            int count = _fakeRecipeCache.Count;
            foreach (var recipe in _fakeRecipeCache.Values)
            {
                if (recipe != null) UnityEngine.Object.Destroy(recipe);
            }
            _fakeRecipeCache.Clear();
            DebugLogger.Log($"Cleaned {count} fake recipes");

            PinGroups.Clear();

            _cachedRecipeFields.Clear();
            _cachedRecipeProps.Clear();
            _cachedItemFields.Clear();
            _cachedItemProps.Clear();
            _cachedElementProps.Clear();
            _cachedElementFields.Clear();
            _elementLookupFailed.Clear();
        }

        public void RefreshRecipeCache()
        {
            DebugLogger.Verbose("Refreshing cache");
            CachedPins.Clear();

            if (ObjectDB.instance == null)
            {
                DebugLogger.Warning("ObjectDB null, can't refresh");
                return;
            }

            int successCount = 0;
            int failCount = 0;

            // Collect group claim counts per recipe key (using MemberCounts)
            Dictionary<string, int> groupClaimCounts = new Dictionary<string, int>();
            foreach (var grp in PinGroups.Values)
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

            // Pre-build group pin data so we can insert them at the right position
            Dictionary<string, PinnedRecipeData> groupPinData = new Dictionary<string, PinnedRecipeData>();
            int groupSuccessCount = 0;

            foreach (var grpKvp in PinGroups)
            {
                PinGroupData grp = grpKvp.Value;
                grp.MemberPins.Clear();
                grp.MergedResources.Clear();
                grp.MemberIcons.Clear();
                grp.IsDirty = true;

                Dictionary<string, PinnedResData> mergedMap = new Dictionary<string, PinnedResData>();

                foreach (string memberKey in grp.MemberRecipeKeys)
                {
                    int memberCount = grp.MemberCounts.TryGetValue(memberKey, out int mc) ? mc : 1;

                    Recipe mr = GetRecipeByName(memberKey);
                    if (mr == null)
                    {
                        DebugLogger.Warning($"Group '{grp.GroupName}' member not found: {memberKey}");
                        continue;
                    }

                    PinnedRecipeData memberData = BuildPinnedRecipeData(mr, memberKey, memberCount);
                    if (memberData == null) continue;

                    grp.MemberPins.Add(memberData);

                    if (memberData.Icon != null && grp.MemberIcons.Count < 4)
                        grp.MemberIcons.Add(memberData.Icon);

                    foreach (var res in memberData.Resources)
                    {
                        if (mergedMap.TryGetValue(res.ItemName, out PinnedResData existingRes))
                        {
                            existingRes.RequiredAmount += res.RequiredAmount;
                        }
                        else
                        {
                            mergedMap[res.ItemName] = new PinnedResData
                            {
                                ItemName = res.ItemName,
                                CachedName = res.CachedName,
                                Icon = res.Icon,
                                RequiredAmount = res.RequiredAmount,
                                LastKnownAmount = -1,
                                LastKnownInvAmount = -1
                            };
                        }
                    }
                }

                foreach (var mres in mergedMap.Values)
                    grp.MergedResources.Add(mres);

                PinnedRecipeData groupPin = new PinnedRecipeData
                {
                    IsDirty = true,
                    RecipeRef = null,
                    RawName = grp.GroupName,
                    CachedHeader = grp.GroupName,
                    Icon = grp.MemberIcons.Count > 0 ? grp.MemberIcons[0] : null,
                    StackCount = 1,
                    Resources = grp.MergedResources,
                    IsGroup = true,
                    GroupRef = grp
                };

                groupPinData[grpKvp.Key] = groupPin;
                groupSuccessCount++;
                DebugLogger.Verbose($"Group pin built: {grp.GroupName} ({grp.MemberPins.Count} members, {grp.MergedResources.Count} resources)");
            }

            // Build CachedPins in display order while preserving PinnedRecipeOrder as canonical order.
            foreach (string entry in GetDisplayPinOrder())
            {
                // Handle group markers: "GROUP:groupName"
                if (entry.StartsWith("GROUP:"))
                {
                    string groupName = entry.Substring(6);
                    if (groupPinData.TryGetValue(groupName, out PinnedRecipeData gp))
                    {
                        CachedPins.Add(gp);
                    }
                    continue;
                }

                // Handle regular recipe keys
                if (!PinnedRecipes.TryGetValue(entry, out int count)) continue;

                // Add individual pin if there's excess beyond group claims
                if (groupClaimCounts.TryGetValue(entry, out int claims))
                {
                    int excessCount = count - claims;
                    if (excessCount <= 0)
                    {
                        DebugLogger.Verbose($"Skipping grouped recipe (no excess): {entry} (claims={claims})");
                        continue;
                    }
                    count = excessCount;
                    DebugLogger.Verbose($"Grouped recipe excess for overlay: {entry} x{excessCount} (claims={claims})");
                }

                Recipe r = GetRecipeByName(entry);

                if (r != null)
                {
                    PinnedRecipeData data = BuildPinnedRecipeData(r, entry, count);
                    if (data != null)
                    {
                        CachedPins.Add(data);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                else
                {
                    DebugLogger.Warning($"Recipe not found: {entry}");
                    failCount++;
                }
            }

            DebugLogger.Log($"Cache refreshed: {successCount} pins, {groupSuccessCount} groups, {failCount} failed");

            if (Player.m_localPlayer != null && RecipePinnerPlugin.Instance != null)
            {
                RecipePinnerPlugin.Instance.UIMgr.UpdateUI(RecipePinnerPlugin.IsUiVisible);
                // Also refresh My Pins panel if it's currently visible
                RecipePinnerPlugin.Instance.UIMgr.RefreshMyPinsList();
            }
        }

        public List<string> GetDisplayPinOrder()
        {
            List<string> displayOrder = new List<string>();
            List<string> deferredExcess = new List<string>();
            HashSet<string> deferredSet = new HashSet<string>();
            Dictionary<string, string> lastClaimingGroup = new Dictionary<string, string>();

            foreach (string entry in PinnedRecipeOrder)
            {
                if (!entry.StartsWith("GROUP:")) continue;

                string groupName = entry.Substring(6);
                if (!PinGroups.TryGetValue(groupName, out PinGroupData group)) continue;

                foreach (string memberKey in group.MemberRecipeKeys)
                    lastClaimingGroup[memberKey] = groupName;
            }

            foreach (string entry in PinnedRecipeOrder)
            {
                if (entry.StartsWith("GROUP:"))
                {
                    string groupName = entry.Substring(6);
                    if (PinGroups.ContainsKey(groupName))
                    {
                        displayOrder.Add(entry);
                        AppendDeferredExcessForGroup(displayOrder, deferredExcess, deferredSet, lastClaimingGroup, groupName);
                    }
                    continue;
                }

                if (!PinnedRecipes.TryGetValue(entry, out int count)) continue;

                int claims = GetGroupClaimCount(entry);
                if (claims <= 0)
                {
                    displayOrder.Add(entry);
                    continue;
                }

                if (count <= claims)
                    continue;

                if (lastClaimingGroup.ContainsKey(entry))
                {
                    if (deferredSet.Add(entry))
                        deferredExcess.Add(entry);
                }
                else
                {
                    displayOrder.Add(entry);
                }
            }

            foreach (string entry in deferredExcess)
                displayOrder.Add(entry);

            return displayOrder;
        }

        private static void AppendDeferredExcessForGroup(
            List<string> displayOrder,
            List<string> deferredExcess,
            HashSet<string> deferredSet,
            Dictionary<string, string> lastClaimingGroup,
            string groupName)
        {
            for (int i = 0; i < deferredExcess.Count;)
            {
                string recipeKey = deferredExcess[i];
                if (lastClaimingGroup.TryGetValue(recipeKey, out string lastGroup) && lastGroup == groupName)
                {
                    displayOrder.Add(recipeKey);
                    deferredSet.Remove(recipeKey);
                    deferredExcess.RemoveAt(i);
                    continue;
                }

                i++;
            }
        }

        public Recipe GetRecipeByName(string name)
        {
            if (ObjectDB.instance == null) return null;

            if (_fakeRecipeCache.TryGetValue(name, out Recipe foundCachedRecipe))
            {
                DebugLogger.Verbose($"Found cached fake recipe: {name}");
                return foundCachedRecipe;
            }

            // Upgrade Check
            Match starMatch = UpgradeStarRegex.Match(name);
            if (starMatch.Success)
            {
                string baseName = name.Substring(0, starMatch.Index).Trim();
                if (!int.TryParse(starMatch.Groups[1].Value, out int targetLevel))
                {
                    DebugLogger.Warning($"Invalid upgrade level in recipe key: {name}");
                    return null;
                }

                Recipe baseRecipe = GetRecipeByName(baseName);
                if (baseRecipe != null)
                {
                    if (!IsValidUpgradeTarget(baseRecipe, targetLevel, name))
                        return null;

                    Recipe upgradeRecipe = CreateFakeUpgradeRecipe(baseRecipe, targetLevel, name);
                    if (upgradeRecipe != null) return upgradeRecipe;
                }
            }

            // Standard Item Check
            ItemDrop itemDrop = ObjectDB.instance.GetItemPrefab(name)?.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                Recipe result = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
                if (result != null)
                {
                    DebugLogger.Verbose($"Found standard recipe: {name}");
                    return result;
                }
            }

            // Recipe List Check
            Recipe standardRecipe = null;
            foreach (var r2 in ObjectDB.instance.m_recipes)
            {
                if (r2.name == name)
                {
                    standardRecipe = r2;
                    break;
                }
            }
            if (standardRecipe != null)
            {
                DebugLogger.Verbose($"Found recipe in ObjectDB: {name}");
                return standardRecipe;
            }

            // Piece Check
            GameObject prefab = ZNetScene.instance?.GetPrefab(name);
            if (prefab != null)
            {
                Piece piece = prefab.GetComponent<Piece>();
                if (piece != null && piece.m_resources != null && piece.m_resources.Length > 0)
                {
                    Recipe fakeRecipe = ScriptableObject.CreateInstance<Recipe>();
                    fakeRecipe.hideFlags = HideFlags.HideAndDontSave;
                    fakeRecipe.name = name;
                    fakeRecipe.m_item = prefab.GetComponent<ItemDrop>();
                    fakeRecipe.m_resources = (Piece.Requirement[])piece.m_resources.Clone();
                    _fakeRecipeCache[name] = fakeRecipe;
                    DebugLogger.Verbose($"Created fake recipe for piece: {name}");
                    return fakeRecipe;
                }
            }

            DebugLogger.Warning($"Recipe not found anywhere: {name}");
            return null;
        }

        private Recipe CreateFakeUpgradeRecipe(Recipe baseRecipe, int targetLevel, string customName)
        {
            if (baseRecipe == null) return null;
            if (!IsValidUpgradeTarget(baseRecipe, targetLevel, customName)) return null;

            Recipe fakeRecipe = ScriptableObject.CreateInstance<Recipe>();
            fakeRecipe.hideFlags = HideFlags.HideAndDontSave;
            fakeRecipe.name = customName;
            fakeRecipe.m_item = baseRecipe.m_item;
            fakeRecipe.m_amount = 1;

            int levelMultiplier = Mathf.Max(1, targetLevel - 1);

            List<Piece.Requirement> upgradeReqs = new List<Piece.Requirement>();
            foreach (var req in baseRecipe.m_resources)
            {
                if (req.m_amountPerLevel > 0)
                {
                    Piece.Requirement newReq = new Piece.Requirement
                    {
                        m_resItem = req.m_resItem,
                        m_amount = req.m_amountPerLevel * levelMultiplier,
                        m_amountPerLevel = 0,
                        m_recover = req.m_recover
                    };
                    upgradeReqs.Add(newReq);
                }
            }

            if (upgradeReqs.Count == 0)
            {
                UnityEngine.Object.Destroy(fakeRecipe);
                return null;
            }

            fakeRecipe.m_resources = upgradeReqs.ToArray();
            _fakeRecipeCache[customName] = fakeRecipe;
            DebugLogger.Verbose($"Created fake upgrade recipe: {customName}");
            return fakeRecipe;
        }

        private bool IsValidUpgradeTarget(Recipe baseRecipe, int targetLevel, string customName)
        {
            if (targetLevel < 2)
            {
                DebugLogger.Warning($"Invalid upgrade level for '{customName}': target level must be at least 2");
                return false;
            }

            ItemDrop item = baseRecipe.m_item;
            ItemDrop.ItemData itemData = item?.m_itemData;
            var sharedData = itemData?.m_shared;
            if (sharedData == null)
            {
                DebugLogger.Warning($"Cannot validate upgrade level for '{customName}' - item data is missing");
                return false;
            }

            int maxQuality = sharedData.m_maxQuality;
            if (maxQuality < 2 || targetLevel > maxQuality)
            {
                DebugLogger.Warning($"Invalid upgrade level for '{customName}': target={targetLevel}, max={maxQuality}");
                return false;
            }

            return true;
        }

        public void ValidateAndCleanPins()
        {
            if (ObjectDB.instance == null)
            {
                DebugLogger.Warning("Cannot validate pins - ObjectDB.instance is null");
                return;
            }

            DebugLogger.Log("Validating pins");
            List<string> keysToRemove = new List<string>();
            foreach (var recipeName in PinnedRecipes.Keys)
            {
                if (GetRecipeByName(recipeName) == null) keysToRemove.Add(recipeName);
            }

            if (keysToRemove.Count > 0)
            {
                foreach (string key in keysToRemove)
                {
                    PinnedRecipes.Remove(key);
                    PinnedRecipeOrder.Remove(key);
                    DebugLogger.Warning($"Removed invalid recipe: {key}");
                }

                DebugLogger.Log($"Removed {keysToRemove.Count} invalid pins");
            }
            else
            {
                DebugLogger.Log("All individual pins valid");
            }

            int removedGroupMembers = CleanInvalidGroupMembers();
            if (keysToRemove.Count > 0 || removedGroupMembers > 0)
            {
                RecipePinnerPlugin.Instance?.DataMgr.SavePins();
            }
        }

        private int CleanInvalidGroupMembers()
        {
            int removedMembers = 0;
            List<string> groupsToRemove = new List<string>();

            foreach (var groupKvp in PinGroups)
            {
                string groupName = groupKvp.Key;
                PinGroupData group = groupKvp.Value;
                List<string> validMembers = new List<string>();

                foreach (string memberKey in group.MemberRecipeKeys)
                {
                    if (GetRecipeByName(memberKey) == null)
                    {
                        group.MemberCounts.Remove(memberKey);
                        removedMembers++;
                        DebugLogger.Warning($"Removed invalid group member: {memberKey} from group '{groupName}'");
                        continue;
                    }

                    validMembers.Add(memberKey);
                }

                if (validMembers.Count != group.MemberRecipeKeys.Count)
                {
                    group.MemberRecipeKeys.Clear();
                    group.MemberRecipeKeys.AddRange(validMembers);
                }

                List<string> staleCountKeys = new List<string>();
                foreach (string countKey in group.MemberCounts.Keys)
                {
                    if (!group.MemberRecipeKeys.Contains(countKey))
                        staleCountKeys.Add(countKey);
                }

                foreach (string staleKey in staleCountKeys)
                    group.MemberCounts.Remove(staleKey);

                if (group.MemberRecipeKeys.Count < 2)
                    groupsToRemove.Add(groupName);
            }

            foreach (string groupName in groupsToRemove)
            {
                PinGroups.Remove(groupName);
                PinnedRecipeOrder.Remove($"GROUP:{groupName}");
                DebugLogger.Warning($"Removed group '{groupName}' because it has less than 2 valid members");
            }

            if (removedMembers > 0 || groupsToRemove.Count > 0)
                DebugLogger.Log($"Removed {removedMembers} invalid group member(s) and {groupsToRemove.Count} invalid group(s)");

            return removedMembers + groupsToRemove.Count;
        }

        public void TryPinHoveredRecipe(InventoryGui gui)
        {
            Transform listRoot = ReflectionHelper.GetRecipeListRoot(gui);

            if (!(ReflectionHelper.GetAvailableRecipes(gui) is System.Collections.IList availableRecipes) || listRoot == null)
            {
                DebugLogger.Verbose("Cannot pin - listRoot or availableRecipes is null");
                return;
            }

            ScrollRect scrollRect = listRoot.GetComponentInParent<ScrollRect>();
            bool isUpgradeTab = !gui.m_tabUpgrade.interactable;

            foreach (Transform child in listRoot)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                RectTransform itemRect = child as RectTransform;
                if (itemRect == null) continue;
                if (!IsVisibleInScroll(itemRect, scrollRect)) continue;

                if (InputHelper.IsMouseOverRect(itemRect, false))
                {
                    string foundText = ExtractTextFromUI(child);
                    if (!string.IsNullOrEmpty(foundText))
                    {
                        string cleanScreenName = CleanNameRegex.Replace(foundText, string.Empty).Trim();
                        cleanScreenName = cleanScreenName.Replace("\r", "").Replace("\n", "");
                        string pureName = AmountSuffixRegex.Replace(cleanScreenName, "").Trim();

                        // Prefer the exact UI row reference: two recipes producing the same item
                        // (Bronze 1x and Bronze 5x) share a display name, so name matching always
                        // picks the first one. Stays -1 when the game version does not expose the
                        // reference, in which case the name matching below is used unchanged.
                        int hoveredIndex = -1;
                        for (int ri = 0; ri < availableRecipes.Count; ri++)
                        {
                            GameObject element = GetInterfaceElementFromObject(availableRecipes[ri]);
                            if (element == null) continue;

                            if (element == child.gameObject || element.transform.IsChildOf(child))
                            {
                                hoveredIndex = ri;
                                break;
                            }
                        }

                        int recipeIndex = -1;
                        foreach (object itemObj in availableRecipes)
                        {
                            recipeIndex++;
                            if (hoveredIndex >= 0 && recipeIndex != hoveredIndex) continue;

                            Recipe r = GetRecipeFromObject(itemObj);

                            if (r != null)
                            {
                                bool isMatch = hoveredIndex >= 0;

                                if (!isMatch)
                                {
                                    string rawName = GetRawRecipeName(r);
                                    if (string.IsNullOrEmpty(rawName)) continue;

                                    string localizedRecipeName = rawName;
                                    if (Localization.instance != null)
                                        localizedRecipeName = Localization.instance.Localize(rawName);

                                    localizedRecipeName = localizedRecipeName.Replace("\r", "").Replace("\n", "");

                                    isMatch = localizedRecipeName.Equals(pureName, System.StringComparison.OrdinalIgnoreCase) ||
                                              localizedRecipeName.Equals(cleanScreenName, System.StringComparison.OrdinalIgnoreCase);
                                }

                                if (isMatch)
                                {
                                    if (isUpgradeTab)
                                    {
                                        ItemDrop.ItemData itemData = GetItemDataFromObject(itemObj)
                                            ?? ReflectionHelper.GetCraftUpgradeItem(gui);

                                        if (itemData != null)
                                        {
                                            int currentQ = itemData.m_quality;
                                            int nextQ = currentQ + 1;
                                            int maxQ = itemData.m_shared.m_maxQuality;

                                            if (currentQ >= maxQ)
                                            {
                                                string msg = RecipePinnerPlugin.Instance.LocalizationMgr.GetText("max_level");
                                                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                                                return;
                                            }

                                            string prefabName = r.m_item.name;
                                            string upgradeId = $"{prefabName} ★{nextQ}";

                                            if (IsUnpinHotkeyHeld() && !PinnedRecipes.ContainsKey(upgradeId)) return;

                                            DebugLogger.Verbose("Attempting to pin hovered recipe...");
                                            DebugLogger.Verbose($"Hovered: '{pureName}' (UpgradeTab: {isUpgradeTab})");
                                            DebugLogger.Log($"Attempting to pin upgrade: {upgradeId} (Base: {prefabName})");

                                            if (GetRecipeByName(upgradeId) != null)
                                            {
                                                TogglePin(upgradeId);
                                            }
                                            else
                                            {
                                                string msg = RecipePinnerPlugin.Instance.LocalizationMgr.GetText("no_upgrade_cost");
                                                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                                            }
                                        }
                                        else
                                        {
                                            DebugLogger.Warning($"Matched name but could not get ItemData for upgrade.");
                                        }
                                    }
                                    else
                                    {
                                        if (IsUnpinHotkeyHeld() && !PinnedRecipes.ContainsKey(r.name)) return;

                                        DebugLogger.Verbose("Attempting to pin hovered recipe...");
                                        DebugLogger.Verbose($"Hovered: '{pureName}' (UpgradeTab: {isUpgradeTab})");
                                        DebugLogger.Log($"Matched recipe: {r.name}");
                                        TogglePin(r.name);
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void TryPinHoveredPiece()
        {
            if (Hud.instance == null) return;
            Piece targetPiece = ReflectionHelper.GetHoveredPiece(Hud.instance);
            if (targetPiece != null && targetPiece.m_resources != null && targetPiece.m_resources.Length > 0)
            {
                if (IsUnpinHotkeyHeld() && !PinnedRecipes.ContainsKey(targetPiece.name)) return;

                DebugLogger.Verbose("Attempting to pin hovered piece...");
                DebugLogger.Log($"Pinning piece: {targetPiece.name}");
                TogglePin(targetPiece.name);
            }
        }

        private bool IsUnpinHotkeyHeld()
        {
            KeyCode unpinKey = RecipePinnerPlugin.HotkeyUnpin?.Value ?? KeyCode.LeftShift;
            return unpinKey != KeyCode.None && Input.GetKey(unpinKey);
        }

        private void TogglePin(string recipeName)
        {
            bool isUnpinHeld = IsUnpinHotkeyHeld();
            var locMgr = RecipePinnerPlugin.Instance.LocalizationMgr;

            if (PinnedRecipes.TryGetValue(recipeName, out int currentCount))
            {
                if (isUnpinHeld)
                {
                    // Use claim count for multi-group support
                    int claimCount = GetGroupClaimCount(recipeName);
                    int minCount = claimCount; // Can't go below total group claims

                    currentCount--;
                    if (currentCount < minCount)
                    {
                        // Can't go below group's minimum claim
                        if (claimCount > 0)
                        {
                            currentCount = claimCount;
                            PinnedRecipes[recipeName] = currentCount;
                            string firstGroup = GetGroupContainingRecipe(recipeName);
                            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                                $"Cannot remove: in group \"{firstGroup}\"");
                            DebugLogger.Log($"Hotkey unpin blocked: {recipeName} min={claimCount}");
                        }
                        else
                        {
                            PinnedRecipes.Remove(recipeName);
                            PinnedRecipeOrder.Remove(recipeName);
                            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("unpinned"));
                            DebugLogger.Log($"Unpinned: {recipeName}");
                        }
                    }
                    else if (currentCount == 0)
                    {
                        PinnedRecipes.Remove(recipeName);
                        PinnedRecipeOrder.Remove(recipeName);
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("unpinned"));
                        DebugLogger.Log($"Unpinned: {recipeName}");
                    }
                    else
                    {
                        PinnedRecipes[recipeName] = currentCount;
                        // Show individual count (total minus all group claims)
                        int displayCount = currentCount - claimCount;
                        if (displayCount > 0)
                        {
                            string msg = string.Format(locMgr.GetText("decreased"), displayCount);
                            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                        }
                        else
                        {
                            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("unpinned"));
                        }
                        DebugLogger.Log($"Decreased pin count: {recipeName} = {currentCount}");
                    }
                }
                else
                {
                    currentCount++;
                    PinnedRecipes[recipeName] = currentCount;

                    // Show notification based on individual count (not total)
                    int claimCount = GetGroupClaimCount(recipeName);
                    if (claimCount > 0)
                    {
                        int individualCount = currentCount - claimCount; // Subtract all group claims
                        if (individualCount == 1)
                        {
                            // First excess pin for a grouped recipe should keep its original global order.
                            if (!PinnedRecipeOrder.Contains(recipeName))
                                PinnedRecipeOrder.Add(recipeName);
                            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("pinned"));
                        }
                        else
                        {
                            string msg = string.Format(locMgr.GetText("added_more"), individualCount);
                            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                        }
                    }
                    else
                    {
                        string msg = string.Format(locMgr.GetText("added_more"), currentCount);
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                    }
                    DebugLogger.Log($"Increased pin count: {recipeName} = {currentCount}");
                }
            }
            else
            {
                if (isUnpinHeld) return;
                if (GetEffectivePinCount() < RecipePinnerPlugin.MaximumPins.Value)
                {
                    PinnedRecipes.Add(recipeName, 1);
                    if (!PinnedRecipeOrder.Contains(recipeName))
                        PinnedRecipeOrder.Add(recipeName);
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("pinned"));
                    DebugLogger.Log($"Pinned new recipe: {recipeName}");
                }
                else
                {
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("list_full"));
                    DebugLogger.Warning($"Cannot pin {recipeName} - max pins reached ({RecipePinnerPlugin.MaximumPins.Value})");
                }
            }
            RefreshRecipeCache();
            RecipePinnerPlugin.Instance?.DataMgr.SavePins();
        }


        private Recipe GetRecipeFromObject(object data)
        {
            if (data == null) return null;
            if (data is Recipe r) return r;

            System.Type type = data.GetType();

            if (_cachedRecipeFields.TryGetValue(type, out FieldInfo cachedField))
            {
                return cachedField.GetValue(data) as Recipe;
            }
            if (_cachedRecipeProps.TryGetValue(type, out PropertyInfo cachedProp))
            {
                return cachedProp.GetValue(data, null) as Recipe;
            }
            PropertyInfo keyProp = type.GetProperty("Key");
            if (keyProp != null && keyProp.GetValue(data, null) is Recipe keyRecipe)
            {
                _cachedRecipeProps[type] = keyProp;
                return keyRecipe;
            }

            FieldInfo mRecipeField = type.GetField("m_recipe", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mRecipeField != null && mRecipeField.GetValue(data) is Recipe fRecipe)
            {
                _cachedRecipeFields[type] = mRecipeField;
                return fRecipe;
            }

            foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(Recipe))
                {
                    _cachedRecipeFields[type] = f;
                    return f.GetValue(data) as Recipe;
                }
            }

            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.PropertyType == typeof(Recipe) && p.CanRead)
                {
                    _cachedRecipeProps[type] = p;
                    return p.GetValue(data, null) as Recipe;
                }
            }

            return null;
        }

        private ItemDrop.ItemData GetItemDataFromObject(object data)
        {
            if (data == null) return null;

            System.Type type = data.GetType();

            if (_cachedItemFields.TryGetValue(type, out FieldInfo cachedField))
                return cachedField.GetValue(data) as ItemDrop.ItemData;
            if (_cachedItemProps.TryGetValue(type, out PropertyInfo cachedProp))
                return cachedProp.GetValue(data, null) as ItemDrop.ItemData;

            PropertyInfo valProp = type.GetProperty("Value");
            if (valProp != null && valProp.GetValue(data, null) is ItemDrop.ItemData valItem)
            {
                _cachedItemProps[type] = valProp;
                return valItem;
            }

            PropertyInfo item2Prop = type.GetProperty("Item2");
            if (item2Prop != null && item2Prop.GetValue(data, null) is ItemDrop.ItemData item2Item)
            {
                _cachedItemProps[type] = item2Prop;
                return item2Item;
            }

            foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(ItemDrop.ItemData))
                {
                    _cachedItemFields[type] = f;
                    return f.GetValue(data) as ItemDrop.ItemData;
                }
            }

            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.PropertyType == typeof(ItemDrop.ItemData) && p.CanRead)
                {
                    _cachedItemProps[type] = p;
                    return p.GetValue(data, null) as ItemDrop.ItemData;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the UI row GameObject that a recipe list entry belongs to.
        /// Valheim's `RecipeDataPair` exposes this as `InterfaceElement`; matching on it is exact,
        /// unlike name matching, which cannot tell a 1x recipe apart from its 5x variant.
        /// Returns null when the running game version does not expose such a member.
        /// </summary>
        private GameObject GetInterfaceElementFromObject(object data)
        {
            if (data == null) return null;

            System.Type type = data.GetType();

            if (_elementLookupFailed.Contains(type)) return null;

            if (_cachedElementProps.TryGetValue(type, out PropertyInfo cachedProp))
                return cachedProp.GetValue(data, null) as GameObject;
            if (_cachedElementFields.TryGetValue(type, out FieldInfo cachedField))
                return cachedField.GetValue(data) as GameObject;

            PropertyInfo namedProp = type.GetProperty("InterfaceElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (namedProp != null && namedProp.PropertyType == typeof(GameObject) && namedProp.CanRead)
            {
                _cachedElementProps[type] = namedProp;
                return namedProp.GetValue(data, null) as GameObject;
            }

            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.PropertyType == typeof(GameObject) && p.CanRead)
                {
                    _cachedElementProps[type] = p;
                    return p.GetValue(data, null) as GameObject;
                }
            }

            foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(GameObject))
                {
                    _cachedElementFields[type] = f;
                    return f.GetValue(data) as GameObject;
                }
            }

            // Remember the miss so a 300-entry recipe list does not re-scan the type on every press.
            _elementLookupFailed.Add(type);
            DebugLogger.Warning($"GetInterfaceElementFromObject: no GameObject member on '{type.Name}' - falling back to name matching");
            return null;
        }

        private string ExtractTextFromUI(Transform child)
        {
            Text classicText = child.GetComponentInChildren<Text>();
            if (classicText != null) return classicText.text;

            Component[] allComps = child.GetComponentsInChildren<Component>(true);
            foreach (var comp in allComps)
            {
                if (comp.GetType().Name.Contains("TextMeshPro") || comp.GetType().Name.Contains("TMP_Text"))
                {
                    PropertyInfo textProp = comp.GetType().GetProperty("text");
                    if (textProp != null)
                    {
                        string foundText = textProp.GetValue(comp, null) as string;
                        if (!string.IsNullOrEmpty(foundText)) return foundText;
                    }
                }
            }
            return null;
        }

        private string GetRawRecipeName(Recipe r)
        {
            if (r.m_item != null && r.m_item.m_itemData != null)
                return r.m_item.m_itemData.m_shared.m_name;

            GameObject prefab = ZNetScene.instance?.GetPrefab(r.name);
            if (prefab != null)
            {
                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop != null) return drop.m_itemData.m_shared.m_name;
                Piece piece = prefab.GetComponent<Piece>();
                if (piece != null) return piece.m_name;
            }
            return null;
        }

        private bool IsVisibleInScroll(RectTransform item, ScrollRect scrollRect)
        {
            if (item == null || !item.gameObject.activeInHierarchy) return false;
            if (scrollRect == null || scrollRect.viewport == null) return true;

            Vector3[] vC = new Vector3[4];
            scrollRect.viewport.GetWorldCorners(vC);
            Rect vRect = new Rect(vC[0].x, vC[0].y, vC[2].x - vC[0].x, vC[2].y - vC[0].y);

            Vector3[] iC = new Vector3[4];
            item.GetWorldCorners(iC);
            Vector3 center = (iC[0] + iC[2]) / 2;

            return vRect.Contains(center);
        }

        // ============================================================
        // Helper: Build a PinnedRecipeData from a Recipe
        // ============================================================
        private PinnedRecipeData BuildPinnedRecipeData(Recipe r, string recipeName, int count)
        {
            if (r == null) return null;

            PinnedRecipeData data = new PinnedRecipeData
            {
                IsDirty = true,
                RecipeRef = r,
                StackCount = count
            };

            if (r.m_item != null && r.m_item.m_itemData != null)
            {
                data.Icon = r.m_item.m_itemData.GetIcon();
                data.RawName = r.m_item.m_itemData.m_shared?.m_name;
            }
            else if (r.m_item != null)
            {
                DebugLogger.Warning($"BuildPinnedRecipeData: recipe '{recipeName}' has item without itemData, using fallback name");
            }
            else
            {
                GameObject prefab = ZNetScene.instance?.GetPrefab(r.name);
                if (prefab != null)
                {
                    Piece p = prefab.GetComponent<Piece>();
                    if (p != null)
                    {
                        data.Icon = p.m_icon;
                        data.RawName = p.m_name;
                    }
                }
            }

            if (string.IsNullOrEmpty(data.RawName)) data.RawName = r.name;

            string displayName = data.RawName;

            if (Localization.instance != null)
            {
                Match starMatch = UpgradeStarRegex.Match(recipeName);
                if (starMatch.Success)
                {
                    string baseName = Localization.instance.Localize(data.RawName);
                    displayName = baseName + starMatch.Value;
                }
                else
                {
                    displayName = Localization.instance.Localize(data.RawName);
                }
            }

            displayName = displayName.Replace("\r", "").Replace("\n", "");

            if (r.m_amount > 1) displayName += $" (x{r.m_amount})";
            if (count > 1) displayName = $"{count}x {displayName}";

            data.CachedHeader = displayName;

            if (r.m_resources == null)
            {
                DebugLogger.Warning($"BuildPinnedRecipeData: recipe '{recipeName}' has null resources, skipping");
                return null;
            }

            foreach (var res in r.m_resources)
            {
                if (res == null || res.m_amount <= 0)
                {
                    continue;
                }

                if (res.m_resItem == null || res.m_resItem.m_itemData == null)
                {
                    DebugLogger.Warning($"BuildPinnedRecipeData: skipping invalid resource in '{recipeName}'");
                    continue;
                }

                PinnedResData resData = new PinnedResData
                {
                    ItemName = res.m_resItem.m_itemData.m_shared?.m_name,
                    Icon = res.m_resItem.m_itemData.GetIcon(),
                    RequiredAmount = res.m_amount * count,
                    LastKnownAmount = -1,
                    LastKnownInvAmount = -1
                };

                if (string.IsNullOrEmpty(resData.ItemName))
                {
                    DebugLogger.Warning($"BuildPinnedRecipeData: skipping resource with empty item name in '{recipeName}'");
                    continue;
                }

                string matName = resData.ItemName;
                if (Localization.instance != null) matName = Localization.instance.Localize(resData.ItemName);
                matName = matName.Replace("\r", "").Replace("\n", "");
                resData.CachedName = matName;
                data.Resources.Add(resData);
            }

            return data;
        }
    }
}
