using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public partial class UIManager
    {
        private void OnClearButtonClicked()
        {
            DebugLogger.Log("Clear button clicked");

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null || (recipeMgr.PinnedRecipes.Count == 0 && recipeMgr.PinGroups.Count == 0))
            {
                if (Player.m_localPlayer != null)
                {
                    var loc = RecipePinnerPlugin.Instance?.LocalizationMgr;
                    string noMsg = loc?.GetText("mypins_empty") ?? "No Recipes Pinned";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, noMsg);
                }
                return;
            }

            if (_confirmDialog == null)
            {
                DebugLogger.Error("Clear: ConfirmDialog is null");
                return;
            }

            var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
            string msg = locMgr?.GetText("clear_confirm_msg") ?? "Remove all pins?";
            _confirmDialog.Show(msg, () =>
            {
                DebugLogger.Log("Clear confirmed - removing all pins");
                if (recipeMgr != null)
                {
                    recipeMgr.PinnedRecipes.Clear();
                    recipeMgr.PinnedRecipeOrder.Clear();
                    recipeMgr.PinGroups.Clear();
                    recipeMgr.RefreshRecipeCache();
                    RecipePinnerPlugin.Instance?.DataMgr.SavePins();
                }
                RefreshMyPinsList();
            }, null);
        }

        private void OnGroupButtonClicked()
        {
            if (_isSelectionMode)
            {
                ConfirmGroupSelection();
                return;
            }

            // Check if there are enough ungrouped pins to group
            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr != null)
            {
                int ungrouped = 0;
                foreach (var kvp in recipeMgr.PinnedRecipes)
                {
                    int claimCount = recipeMgr.GetGroupClaimCount(kvp.Key);
                    if (kvp.Value > claimCount)
                        ungrouped++;
                }

                if (ungrouped < 2)
                {
                    if (Player.m_localPlayer != null)
                    {
                        var loc = RecipePinnerPlugin.Instance?.LocalizationMgr;
                        string noMsg = loc?.GetText("group_need_more") ?? "At least 2 ungrouped pins are needed to create a group";
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, noMsg);
                    }
                    return;
                }
            }

            EnterSelectionMode();
        }

        public void EnterSelectionMode()
        {
            _isSelectionMode = true;
            _selectedForGroup.Clear();

            // Hide entire top button row (Group+Clear both gone in selection mode)
            if (_myPinsPanel?.GroupButton != null)
                _myPinsPanel.GroupButton.transform.parent.gameObject.SetActive(false);
            if (_myPinsPanel?.ConfirmButton != null)
                _myPinsPanel.ConfirmButton.transform.parent.gameObject.SetActive(true);
            if (_myPinsPanel?.CloseButton != null)
                _myPinsPanel.CloseButton.transform.parent.gameObject.SetActive(false);

            // Rebuild instead of re-styling the existing rows: the list contents differ in
            // selection mode (groups are hidden), and the rebuild applies the selection styling.
            RefreshMyPinsList();

            if (Player.m_localPlayer != null)
            {
                string msg = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_select_hint") ?? "Select pins to group";
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
            }

            DebugLogger.Log("Entered selection mode");
        }

        public void ExitSelectionMode()
        {
            _isSelectionMode = false;
            _selectedForGroup.Clear();

            // Show top button row back (Group+Clear), hide Confirm+Cancel row, show Close
            if (_myPinsPanel?.GroupButton != null)
                _myPinsPanel.GroupButton.transform.parent.gameObject.SetActive(true);
            if (_myPinsPanel?.ConfirmButton != null)
                _myPinsPanel.ConfirmButton.transform.parent.gameObject.SetActive(false);
            if (_myPinsPanel?.CloseButton != null)
                _myPinsPanel.CloseButton.transform.parent.gameObject.SetActive(true);
            // Re-apply Clear button visibility based on pin count (handled by RefreshMyPinsList)
            if (_myPinsPanel?.ClearButton != null)
                _myPinsPanel.ClearButton.gameObject.SetActive(true);

            if (_myPinsPanel != null)
            {
                foreach (var item in _myPinsPanel.PinItems)
                {
                    if (item.gameObject.activeSelf)
                        item.SetSelectionMode(false);
                }
            }

            DebugLogger.Log("Exited selection mode");
        }

        private void ConfirmGroupSelection()
        {
            _selectedForGroup.Clear();

            foreach (var item in _myPinsPanel.PinItems)
            {
                if (!item.gameObject.activeSelf || item.IsGroupItem) continue;
                if (item.SelectToggle != null && item.SelectToggle.isOn)
                    _selectedForGroup.Add(item.RecipeKey);
            }

            if (_selectedForGroup.Count < 2)
            {
                if (Player.m_localPlayer != null)
                {
                    string msg = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_min_select") ?? "Select at least 2 pins";
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
                }
                DebugLogger.Log($"Group selection rejected: only {_selectedForGroup.Count} selected");
                return;
            }

            DebugLogger.Log($"Group selection confirmed: {_selectedForGroup.Count} pins selected");
            ShowGroupNameDialog();
        }

        // ============================================================
        // Group Name Dialog
        // ============================================================

        private void ShowGroupNameDialog()
        {
            if (_groupNameDialog == null)
            {
                DebugLogger.Error("ShowGroupNameDialog: Dialog is null");
                return;
            }

            _groupNameDialog.SetActive(true);
            DebugLogger.Log("Group name dialog shown");
        }

        private bool OnGroupNameConfirmed(string groupName)
        {
            DebugLogger.Log($"Group name confirmed: '{groupName}'");

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return false;

            bool success = recipeMgr.CreateGroup(groupName, _selectedForGroup);

            if (success && Player.m_localPlayer != null)
            {
                string msg = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_created") ?? "Group Created: {0}";
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(msg, groupName));
            }

            if (!success)
            {
                if (Player.m_localPlayer != null)
                {
                    string msg;
                    if (recipeMgr.PinGroups.ContainsKey(groupName))
                    {
                        string template = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_name_exists") ?? "Group '{0}' already exists";
                        msg = string.Format(template, groupName);
                    }
                    else
                    {
                        msg = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_create_failed") ?? "Group could not be created";
                    }
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
                }
                return false;
            }

            ExitSelectionMode();
            RefreshMyPinsList();
            return true;
        }

        private void OnGroupNameCancelled()
        {
            DebugLogger.Log("Group name cancelled");
            _groupNameDialog?.SetActive(false);
        }

        // ============================================================
        // Pin Actions
        // ============================================================

        private void OnPinDelete(string key, string displayName, bool isGroup)
        {
            DebugLogger.Log($"OnPinDelete requested: {key}");

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return;

            var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
            string message = isGroup
                ? string.Format(locMgr?.GetText("confirm_delete_group") ?? "Delete group \"{0}\" and all member pins?", displayName)
                : string.Format(locMgr?.GetText("confirm_delete_pin") ?? "Delete \"{0}\"?", displayName);

            if (_confirmDialog != null)
            {
                _confirmDialog.Show(message, () =>
                {
                    DebugLogger.Log($"OnPinDelete confirmed: {key}");
                    recipeMgr.RemovePinFromMyPinsPanel(key);
                    if (isGroup) _expandedGroups.Remove(key);
                    RefreshMyPinsList();
                });
            }
            else
            {
                // Fallback: no dialog available, delete directly
                recipeMgr.RemovePinFromMyPinsPanel(key);
                if (isGroup) _expandedGroups.Remove(key);
                RefreshMyPinsList();
            }
        }

        private void OnPinCountChange(string key, int delta)
        {
            DebugLogger.Log($"OnPinCountChange: {key} delta={delta}");

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return;

            recipeMgr.AdjustPinCount(key, delta, showMessage: false);
            RefreshMyPinsList();
        }

        /// <summary>
        /// Separate handler for sub-item +/- buttons.
        /// Modifies both MemberCounts (group claim) and PinnedRecipes (total) together.
        /// </summary>
        private void OnSubItemCountChange(string groupName, string key, int delta)
        {
            DebugLogger.Log($"OnSubItemCountChange: group={groupName} key={key} delta={delta}");

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return;

            if (string.IsNullOrEmpty(groupName) || !recipeMgr.PinGroups.TryGetValue(groupName, out PinGroupData targetGroup))
            {
                DebugLogger.Warning($"OnSubItemCountChange: group '{groupName}' not found for key '{key}'");
                return;
            }

            if (!targetGroup.MemberRecipeKeys.Contains(key))
            {
                DebugLogger.Warning($"OnSubItemCountChange: key '{key}' not found in group '{groupName}'");
                return;
            }

            int currentClaim = targetGroup.MemberCounts.TryGetValue(key, out int mc) ? mc : 1;

            if (!recipeMgr.PinnedRecipes.TryGetValue(key, out int currentTotal))
            {
                DebugLogger.Warning($"SubItem: {key} missing from PinnedRecipes while adjusting group claim; ignoring change");
                return;
            }

            int newClaim = currentClaim + delta;
            if (newClaim < 1) newClaim = 1; // Minimum 1 — recipe must exist for the group

            int claimDelta = newClaim - currentClaim;
            targetGroup.MemberCounts[key] = newClaim;

            // Also adjust PinnedRecipes by the same delta
            int totalAfterChange = System.Math.Max(1, currentTotal + claimDelta);
            recipeMgr.PinnedRecipes[key] = totalAfterChange;

            DebugLogger.Log($"SubItem: {groupName}/{key} claim {currentClaim}->{newClaim}, total={totalAfterChange}");

            recipeMgr.RefreshRecipeCache();
            RecipePinnerPlugin.Instance?.DataMgr.SavePins();
            RefreshMyPinsList();
        }

        private void OnExpandToggle(string groupName)
        {
            if (_expandedGroups.Contains(groupName))
            {
                _expandedGroups.Remove(groupName);
                DebugLogger.Log($"Group collapsed: {groupName}");
            }
            else
            {
                _expandedGroups.Add(groupName);
                DebugLogger.Log($"Group expanded: {groupName}");
            }
            RefreshMyPinsList();
        }

        private void OnDisbandGroup(string groupName, string displayName)
        {
            DebugLogger.Log($"OnDisbandGroup requested: {groupName}");

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return;

            var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
            string message = string.Format(
                locMgr?.GetText("confirm_disband_group") ?? "Disband group \"{0}\"? Members will become individual pins.",
                displayName);

            if (_confirmDialog != null)
            {
                _confirmDialog.Show(message, () =>
                {
                    DebugLogger.Log($"OnDisbandGroup confirmed: {groupName}");
                    recipeMgr.DisbandGroup(groupName);
                    _expandedGroups.Remove(groupName);
                    RefreshMyPinsList();

                    if (Player.m_localPlayer != null)
                    {
                        string msg = locMgr?.GetText("group_disbanded") ?? "Group Disbanded: {0}";
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(msg, displayName));
                    }
                });
            }
            else
            {
                recipeMgr.DisbandGroup(groupName);
                _expandedGroups.Remove(groupName);
                RefreshMyPinsList();
            }
        }

        private void OnSubItemDelete(string groupName, string recipeKey, string displayName)
        {
            DebugLogger.Log($"OnSubItemDelete requested: {recipeKey} from group {groupName}");

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null) return;

            var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
            string message = string.Format(
                locMgr?.GetText("confirm_remove_member") ?? "Remove \"{0}\" from group \"{1}\"?",
                displayName, groupName);

            if (_confirmDialog != null)
            {
                _confirmDialog.Show(message, () =>
                {
                    DebugLogger.Log($"OnSubItemDelete confirmed: {recipeKey} from group {groupName}");
                    recipeMgr.RemoveMemberFromGroup(groupName, recipeKey);

                    // If group was auto-disbanded, remove from expanded set
                    if (!recipeMgr.PinGroups.ContainsKey(groupName))
                        _expandedGroups.Remove(groupName);

                    RefreshMyPinsList();
                });
            }
            else
            {
                recipeMgr.RemoveMemberFromGroup(groupName, recipeKey);
                if (!recipeMgr.PinGroups.ContainsKey(groupName))
                    _expandedGroups.Remove(groupName);
                RefreshMyPinsList();
            }
        }

        // ============================================================
        // Full Destroy (only on session change / app quit)
        // ============================================================

        /// <summary>
        /// Destroys all My Pins UI elements.
        /// ONLY called on player session change or application quit.
        /// NOT called during overlay rebuilds (layout mode change etc.).
        /// </summary>
        public void DestroyMyPinsUI()
        {
            DebugLogger.Verbose("DestroyMyPinsUI");

            if (_myPinsButton != null)
            {
                Object.Destroy(_myPinsButton.gameObject);
                _myPinsButton = null;
            }

            if (_modalOverlay != null)
            {
                Object.Destroy(_modalOverlay);
                _modalOverlay = null;
            }

            if (_myPinsPanel != null)
            {
                Object.Destroy(_myPinsPanel.gameObject);
                _myPinsPanel = null;
            }

            if (_groupNameDialog != null)
            {
                Object.Destroy(_groupNameDialog.gameObject);
                _groupNameDialog = null;
            }

            if (_confirmDialog != null)
            {
                Object.Destroy(_confirmDialog.gameObject);
                _confirmDialog = null;
            }

            _isSelectionMode = false;
            _selectedForGroup.Clear();
            _inventoryWasOpen = false;
            _expandedGroups.Clear();
            _lastKnownPinCount = -1;
            _lastKnownGroupCount = -1;

            DebugLogger.Log("My Pins UI destroyed");
        }
    }
}
