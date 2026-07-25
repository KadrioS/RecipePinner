using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public partial class RecipeManager
    {
        /// <summary>
        /// Creates a new pin group from selected recipe keys.
        /// Member pins stay in PinnedRecipes but are hidden from overlay.
        /// </summary>
        public bool CreateGroup(string groupName, List<string> selectedKeys)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                DebugLogger.Warning("CreateGroup: group name is empty");
                return false;
            }

            if (selectedKeys == null || selectedKeys.Count < 2)
            {
                DebugLogger.Warning($"CreateGroup: need at least 2 pins, got {selectedKeys?.Count ?? 0}");
                return false;
            }

            if (PinGroups.ContainsKey(groupName))
            {
                DebugLogger.Warning($"CreateGroup: group '{groupName}' already exists");
                return false;
            }

            PinGroupData group = new PinGroupData
            {
                GroupName = groupName
            };

            foreach (string key in selectedKeys)
            {
                if (PinnedRecipes.TryGetValue(key, out int existingCount))
                {
                    int existingClaims = GetGroupClaimCount(key);
                    int availableExcess = existingCount - existingClaims;
                    if (availableExcess <= 0)
                    {
                        DebugLogger.Warning($"CreateGroup: recipe key '{key}' has no ungrouped excess to claim (total={existingCount}, claims={existingClaims}), skipping");
                        continue;
                    }

                    group.MemberRecipeKeys.Add(key);
                    group.MemberCounts[key] = availableExcess; // Claim every copy not already claimed by another group
                    DebugLogger.Verbose($"CreateGroup: added member '{key}' to group '{groupName}' (claim={availableExcess}, total={existingCount}, previousClaims={existingClaims})");
                }
                else
                {
                    DebugLogger.Warning($"CreateGroup: recipe key '{key}' not found in PinnedRecipes, skipping");
                }
            }

            if (group.MemberRecipeKeys.Count < 2)
            {
                DebugLogger.Warning($"CreateGroup: only {group.MemberRecipeKeys.Count} valid members, need at least 2");
                return false;
            }

            PinGroups[groupName] = group;

            // Group is a new entity — add marker to the end of the list
            string groupMarker = $"GROUP:{groupName}";
            PinnedRecipeOrder.Add(groupMarker);

            DebugLogger.Log($"Group created: '{groupName}' with {group.MemberRecipeKeys.Count} members (added to end)");

            RefreshRecipeCache();
            RecipePinnerPlugin.Instance?.DataMgr.SavePins();
            return true;
        }

        /// <summary>
        /// Disbands a group, restoring member pins as individual entries.
        /// </summary>
        public bool DisbandGroup(string groupName)
        {
            if (!PinGroups.TryGetValue(groupName, out PinGroupData group))
            {
                DebugLogger.Warning($"DisbandGroup: group '{groupName}' not found");
                return false;
            }

            PinGroups.Remove(groupName);
            PinnedRecipeOrder.Remove($"GROUP:{groupName}");
            DebugLogger.Log($"Group disbanded: '{groupName}' ({group.MemberRecipeKeys.Count} members restored)");

            RefreshRecipeCache();
            RecipePinnerPlugin.Instance?.DataMgr.SavePins();
            return true;
        }
        /// <summary>
        /// Decrements a single group's claim on the recipe: the last group in pin order
        /// that contains it. When that claim reaches 0, the member is removed from the group.
        /// If a group drops below 2 members, it is auto-disbanded.
        /// Called by AutoUnpin hooks after crafting/building.
        /// </summary>
        public void DecrementGroupMemberCounts(string recipeKey)
        {
            string targetGroupName = null;
            foreach (string entry in PinnedRecipeOrder)
            {
                if (!entry.StartsWith("GROUP:")) continue;

                string candidateName = entry.Substring(6);
                if (!PinGroups.TryGetValue(candidateName, out PinGroupData candidate)) continue;
                if (!candidate.MemberCounts.ContainsKey(recipeKey)) continue;

                targetGroupName = candidateName;
            }

            if (targetGroupName == null) return;

            string gn = targetGroupName;
            if (!PinGroups.TryGetValue(gn, out PinGroupData grp)) return;

            int oldClaim = grp.MemberCounts[recipeKey];
            int newClaim = oldClaim - 1;

            if (newClaim <= 0)
            {
                // Member fully consumed — remove from group
                grp.MemberCounts.Remove(recipeKey);
                grp.MemberRecipeKeys.Remove(recipeKey);
                DebugLogger.Log($"AutoUnpin: '{recipeKey}' claim reached 0, removed from group '{gn}' ({grp.MemberRecipeKeys.Count} remaining)");

                // Auto-disband if below 2 members
                if (grp.MemberRecipeKeys.Count < 2)
                {
                    DebugLogger.Log($"AutoUnpin: Group '{gn}' auto-disbanded ({grp.MemberRecipeKeys.Count} members)");
                    PinGroups.Remove(gn);
                    PinnedRecipeOrder.Remove($"GROUP:{gn}");
                }
            }
            else
            {
                grp.MemberCounts[recipeKey] = newClaim;
                DebugLogger.Log($"AutoUnpin: '{recipeKey}' claim decremented in group '{gn}': {oldClaim}->{newClaim}");
            }
        }

        /// <summary>
        /// Removes a specific recipe from a group and deletes its pin entirely.
        /// If group drops below 2 members, auto-disbands the group.
        /// Used by the sub-item X button in the dropdown.
        /// </summary>
        public void RemoveMemberFromGroup(string groupName, string recipeKey)
        {
            if (!PinGroups.TryGetValue(groupName, out PinGroupData group))
            {
                DebugLogger.Warning($"RemoveMemberFromGroup: group '{groupName}' not found");
                return;
            }

            if (!group.MemberRecipeKeys.Remove(recipeKey))
            {
                DebugLogger.Warning($"RemoveMemberFromGroup: '{recipeKey}' not in group '{groupName}'");
                return;
            }

            // Get the member's claim count before removing
            int memberClaim = group.MemberCounts.TryGetValue(recipeKey, out int mc) ? mc : 1;
            group.MemberCounts.Remove(recipeKey);

            DebugLogger.Log($"Removed '{recipeKey}' from group '{groupName}' (claim={memberClaim}, {group.MemberRecipeKeys.Count} remaining)");

            // Reduce PinnedRecipes by the group's claim for this member
            if (PinnedRecipes.TryGetValue(recipeKey, out int currentCount))
            {
                int newCount = currentCount - memberClaim;
                if (newCount <= 0)
                {
                    PinnedRecipes.Remove(recipeKey);
                    PinnedRecipeOrder.Remove(recipeKey);
                }
                else
                {
                    PinnedRecipes[recipeKey] = newCount;
                }
            }

            // If group drops below 2 members, auto-disband
            if (group.MemberRecipeKeys.Count < 2)
            {
                DebugLogger.Log($"Group '{groupName}' has {group.MemberRecipeKeys.Count} member(s), auto-disbanding");
                PinGroups.Remove(groupName);
                PinnedRecipeOrder.Remove($"GROUP:{groupName}");
            }

            RefreshRecipeCache();
            RecipePinnerPlugin.Instance?.DataMgr.SavePins();

            if (GetEffectivePinCount() < 2)
                RecipePinnerPlugin.Instance?.UIMgr.CloseGatheringList();
        }

        /// <summary>
        /// Removes a pin or group from the My Pins panel.
        /// If key matches a group name, removes the group and all its member pins.
        /// If key is a regular pin, removes it and also from any group containing it.
        /// </summary>
        public void RemovePinFromMyPinsPanel(string key)
        {
            // Check if it's a group
            if (PinGroups.TryGetValue(key, out PinGroupData group))
            {
                // Remove group's claim (1 each) from member pins, but preserve excess
                foreach (string memberKey in group.MemberRecipeKeys)
                {
                    if (PinnedRecipes.TryGetValue(memberKey, out int memberCount))
                    {
                        // Use the actual claim recorded in MemberCounts — may be > 1
                        int memberClaim = group.MemberCounts.TryGetValue(memberKey, out int mc) ? mc : 1;
                        int remaining = memberCount - memberClaim;
                        if (remaining <= 0)
                        {
                            PinnedRecipes.Remove(memberKey);
                            PinnedRecipeOrder.Remove(memberKey);
                            DebugLogger.Verbose($"Removed group member pin entirely: {memberKey}");
                        }
                        else
                        {
                            PinnedRecipes[memberKey] = remaining;
                            DebugLogger.Verbose($"Group member pin kept as individual: {memberKey} x{remaining}");
                        }
                    }
                }
                PinGroups.Remove(key);
                PinnedRecipeOrder.Remove($"GROUP:{key}");
                DebugLogger.Log($"Removed group: {key} (member excess pins preserved)");
            }
            else
            {
                // Remove individual pin
                int claimCount = GetGroupClaimCount(key);
                if (claimCount > 0)
                {
                    // Recipe is in group(s) - only remove individual excess, keep group claims
                    if (PinnedRecipes.TryGetValue(key, out int totalCount) && totalCount > claimCount)
                    {
                        PinnedRecipes[key] = claimCount;
                        DebugLogger.Log($"Removed individual excess for grouped pin: {key} (kept {claimCount} for groups)");
                    }
                    else
                    {
                        DebugLogger.Log($"No individual excess to remove for: {key} (claims={claimCount})");
                    }
                }
                else
                {
                    PinnedRecipes.Remove(key);
                    PinnedRecipeOrder.Remove(key);
                    DebugLogger.Log($"Removed pin: {key}");
                }
            }

            RefreshRecipeCache();
            RecipePinnerPlugin.Instance?.DataMgr.SavePins();

            if (GetEffectivePinCount() < 2)
                RecipePinnerPlugin.Instance?.UIMgr.CloseGatheringList();
        }

        /// <summary>
        /// Adjusts the count of a pinned recipe by delta (+1 or -1).
        /// If count reaches 0, the pin is removed.
        /// </summary>
        public void AdjustPinCount(string key, int delta, bool showMessage = true)
        {
            if (!PinnedRecipes.TryGetValue(key, out int currentCount))
            {
                DebugLogger.Warning($"AdjustPinCount: recipe '{key}' not found");
                return;
            }

            // Determine minimum: total group claims (same semantics as TogglePin)
            int claimCount = GetGroupClaimCount(key);
            int minCount = claimCount > 0 ? (claimCount + 1) : 1;

            currentCount += delta;
            DebugLogger.Log($"AdjustPinCount: {key} -> {currentCount} (min={minCount}, claims={claimCount})");

            if (currentCount < minCount)
            {
                currentCount = minCount;
                DebugLogger.Log($"AdjustPinCount: clamped to minimum {minCount} for '{key}'");
                return; // No change needed
            }

            PinnedRecipes[key] = currentCount;

            // Show notification using individual count (subtract group claims)
            if (showMessage)
            {
                var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
                if (locMgr != null)
                {
                    int displayCount = currentCount - claimCount;
                    if (delta > 0)
                    {
                        string msg = string.Format(locMgr.GetText("added_more"), displayCount);
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                    }
                    else
                    {
                        string msg = string.Format(locMgr.GetText("decreased"), displayCount);
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                    }
                }
            }
            RefreshRecipeCache();
            RecipePinnerPlugin.Instance?.DataMgr.SavePins();
        }

        /// <summary>
        /// Returns the group name that contains the given recipe key, or null if not grouped.
        /// </summary>
        public string GetGroupContainingRecipe(string recipeKey)
        {
            foreach (var grpKvp in PinGroups)
            {
                if (grpKvp.Value.MemberRecipeKeys.Contains(recipeKey))
                    return grpKvp.Key;
            }
            return null;
        }

        /// <summary>
        /// Returns how many total claims all groups make on this recipe.
        /// Uses MemberCounts (variable per-group claims) instead of flat 1-per-group.
        /// </summary>
        public int GetGroupClaimCount(string recipeKey)
        {
            int count = 0;
            foreach (var grp in PinGroups.Values)
            {
                if (grp.MemberCounts.TryGetValue(recipeKey, out int mc))
                    count += mc;
                else if (grp.MemberRecipeKeys.Contains(recipeKey))
                    count += 1; // Fallback for legacy data without MemberCounts
            }
            return count;
        }

        public int TrimToMaximumPins(int maxEffectivePins)
        {
            int removed = 0;

            while (GetEffectivePinCount() > maxEffectivePins)
            {
                bool trimmedOne = false;

                for (int i = PinnedRecipeOrder.Count - 1; i >= 0; i--)
                {
                    string entry = PinnedRecipeOrder[i];

                    if (entry.StartsWith("GROUP:"))
                    {
                        string groupName = entry.Substring(6);
                        PinnedRecipeOrder.RemoveAt(i);

                        if (PinGroups.TryGetValue(groupName, out PinGroupData group))
                        {
                            foreach (string memberKey in group.MemberRecipeKeys)
                            {
                                if (!PinnedRecipes.TryGetValue(memberKey, out int totalCount)) continue;

                                int claim = group.MemberCounts.TryGetValue(memberKey, out int memberClaim) ? memberClaim : 1;
                                int remaining = totalCount - claim;

                                if (remaining > 0)
                                {
                                    PinnedRecipes[memberKey] = remaining;
                                }
                                else
                                {
                                    PinnedRecipes.Remove(memberKey);
                                    PinnedRecipeOrder.Remove(memberKey);
                                }
                            }

                            PinGroups.Remove(groupName);
                        }

                        removed++;
                        trimmedOne = true;
                        break;
                    }

                    if (PinnedRecipes.TryGetValue(entry, out int count))
                    {
                        int claims = GetGroupClaimCount(entry);
                        if (count > claims)
                        {
                            if (claims > 0)
                            {
                                PinnedRecipes[entry] = claims;
                            }
                            else
                            {
                                PinnedRecipes.Remove(entry);
                                PinnedRecipeOrder.RemoveAt(i);
                            }

                            removed++;
                            trimmedOne = true;
                            break;
                        }
                    }
                }

                if (!trimmedOne)
                    break;
            }

            return removed;
        }

        /// <summary>
        /// Returns the effective (visual) pin count:
        /// each group = 1 pin, each recipe with excess beyond group claims = 1 pin.
        /// Used for limit checks instead of raw PinnedRecipes.Count.
        /// </summary>
        public int GetEffectivePinCount()
        {
            int count = PinGroups.Count;
            foreach (var kvp in PinnedRecipes)
            {
                int claimCount = GetGroupClaimCount(kvp.Key);
                if (kvp.Value > claimCount)
                    count++;
            }
            return count;
        }
    }
}
