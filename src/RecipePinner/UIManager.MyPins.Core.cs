using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public partial class UIManager
    {
        // My Pins UI references (persistent - survive overlay rebuild)
        private MyPinsPanelUI _myPinsPanel;
        private GroupNameDialog _groupNameDialog;
        private ConfirmDialog _confirmDialog;
        private GameObject _modalOverlay;
        private Button _myPinsButton;
        private bool _isSelectionMode = false;
        private readonly List<string> _selectedForGroup = new List<string>();
        private readonly HashSet<string> _expandedGroups = new HashSet<string>();
        private bool _inventoryWasOpen = false;
        private int _lastKnownPinCount = -1;
        private int _lastKnownGroupCount = -1;

        /// <summary>
        /// True while the My Pins panel is visible; Harmony patches use this to intercept Escape.
        /// </summary>
        public bool IsMyPinsPanelOpen => _myPinsPanel != null && _myPinsPanel.gameObject.activeSelf;

        /// <summary>
        /// Clears stale Unity/ZInput state after focus changes while the modal My Pins UI is open.
        /// </summary>
        public void ResetMyPinsInputState(string reason)
        {
            if (!IsMyPinsPanelOpen && !ControlsInfoPanel.IsOpen && !GroupNameDialog.IsDialogOpen && !ConfirmDialog.IsDialogOpen)
                return;

            try
            {
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
            }
            catch (System.Exception ex)
            {
                DebugLogger.Warning($"ResetMyPinsInputState: EventSystem reset failed ({ex.Message})");
            }

            try
            {
                Input.ResetInputAxes();
            }
            catch (System.Exception ex)
            {
                DebugLogger.Warning($"ResetMyPinsInputState: Unity input reset failed ({ex.Message})");
            }

            try
            {
                if (ZInput.instance != null)
                {
                    ZInput.ResetButtonStatus("Forward");
                    ZInput.ResetButtonStatus("Backward");
                    ZInput.ResetButtonStatus("Left");
                    ZInput.ResetButtonStatus("Right");
                    ZInput.ResetButtonStatus("Jump");
                    ZInput.ResetButtonStatus("Crouch");
                    ZInput.ResetButtonStatus("Run");
                    ZInput.ResetButtonStatus("Use");
                    ZInput.ResetButtonStatus("Attack");
                    ZInput.ResetButtonStatus("SecondAttack");
                    ZInput.ResetButtonStatus("Block");
                    ZInput.ResetButtonStatus("Inventory");
                    ZInput.ResetButtonStatus("Hide");
                    ZInput.ResetButtonStatus("Sit");
                    ZInput.ResetButtonStatus("GPower");
                    ZInput.ResetButtonStatus("Emote1");
                    ZInput.ResetButtonStatus("Emote2");
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.Warning($"ResetMyPinsInputState: ZInput reset failed ({ex.Message})");
            }

            DebugLogger.Verbose($"My Pins input state reset ({reason})");
        }

        /// <summary>
        /// Keeps the My Pins UI in sync with inventory visibility and pin/group changes.
        /// </summary>
        public void UpdateMyPinsInventoryState()
        {
            if (RecipePinnerPlugin.EnableMod != null && !RecipePinnerPlugin.EnableMod.Value)
            {
                HideMyPinsPanel();
                if (_myPinsButton != null)
                    _myPinsButton.gameObject.SetActive(false);
                _inventoryWasOpen = false;
                return;
            }

            bool isInventoryOpen = InventoryGui.instance != null && InventoryGui.IsVisible();

            if (isInventoryOpen && !_inventoryWasOpen)
            {
                // The button is parented to the crafting panel, so it follows Valheim's slide animation.
                EnsureMyPinsButton();
                if (_myPinsButton != null)
                    _myPinsButton.gameObject.SetActive(true);
            }
            else if (!isInventoryOpen && _inventoryWasOpen)
            {
                HideMyPinsPanel();
            }

            if (isInventoryOpen && _myPinsPanel != null && _myPinsPanel.gameObject.activeSelf)
            {
                var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
                if (recipeMgr != null)
                {
                    int currentPinCount = recipeMgr.PinnedRecipes.Count;
                    int currentGroupCount = recipeMgr.PinGroups.Count;

                    if (currentPinCount != _lastKnownPinCount || currentGroupCount != _lastKnownGroupCount)
                    {
                        _lastKnownPinCount = currentPinCount;
                        _lastKnownGroupCount = currentGroupCount;
                        RefreshMyPinsList();
                    }
                }
            }

            _inventoryWasOpen = isInventoryOpen;
        }

        /// <summary>
        /// Ensures the inventory My Pins button exists, recreating it after scene/UI rebuilds.
        /// </summary>
        private void EnsureMyPinsButton()
        {
            if (_myPinsButton != null) return;

            if (InventoryGui.instance == null) return;

            Transform invRoot = InventoryGui.instance.transform;
            if (invRoot == null)
            {
                DebugLogger.Warning("EnsureMyPinsButton: InventoryGui root is null");
                return;
            }

            // MLU (MyLittleUI) uses m_craftButton.parent — that's the container
            // that slides in with the crafting panel animation.
            Transform buttonParent = invRoot;

            try
            {
                Button craftBtn = InventoryGui.instance.m_craftButton;
                if (craftBtn != null && craftBtn.transform.parent != null)
                {
                    buttonParent = craftBtn.transform.parent;
                    DebugLogger.Log($"EnsureMyPinsButton: Using m_craftButton.parent = '{buttonParent.name}'");
                }
                else
                    DebugLogger.Warning("EnsureMyPinsButton: m_craftButton or its parent is null, falling back to invRoot");
            }
            catch (System.Exception ex)
            {
                DebugLogger.Warning($"EnsureMyPinsButton: m_craftButton lookup failed: {ex.Message}");
            }

            _myPinsButton = UIBuilder.CreateMyPinsButton(buttonParent, _cachedFont);

            if (_myPinsButton == null)
            {
                DebugLogger.Error("EnsureMyPinsButton: Failed to create button");
                return;
            }

            RectTransform btnRect = _myPinsButton.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.pivot = new Vector2(1, 1);
            Vector2 btnPos = RecipePinnerPlugin.MyPinsButtonPosition?.Value ?? new Vector2(-10f, -10f);
            btnRect.anchoredPosition = btnPos;

            _myPinsButton.onClick.AddListener(ToggleMyPinsPanel);

            // Override outline to 3px for the Pins button (panel buttons use 1px)
            var pinsBtnOutline = _myPinsButton.GetComponentInChildren<UnityEngine.UI.Outline>();
            if (pinsBtnOutline != null)
                pinsBtnOutline.effectDistance = new UnityEngine.Vector2(3f, -3f);

            DebugLogger.Log("My Pins button ensured on inventory");
        }

        /// <summary>
        /// Lazily creates the persistent My Pins panel and dialogs.
        /// </summary>
        private void EnsureMyPinsPanel()
        {
            if (_myPinsPanel != null) return;

            if (InventoryGui.instance == null) return;

            Transform invRoot = InventoryGui.instance.transform;

            DebugLogger.Log("Creating My Pins panel (persistent)");

            _myPinsPanel = UIBuilder.CreateMyPinsPanel(invRoot, _cachedFont);

            if (_myPinsPanel == null)
            {
                DebugLogger.Error("EnsureMyPinsPanel: UIBuilder returned null");
                return;
            }

            // Pivot + anchor at center → symmetric resize, position from config
            // invRoot is a full-screen canvas so anchor (0.5, 0.5) = screen center
            _myPinsPanel.PanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            _myPinsPanel.PanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            _myPinsPanel.PanelRect.pivot = new Vector2(0.5f, 0.5f);
            _myPinsPanel.PanelRect.anchoredPosition = RecipePinnerPlugin.MyPinsPanelPosition?.Value ?? Vector2.zero;

            if (_myPinsPanel.GroupButton != null)
            {
                _myPinsPanel.GroupButton.onClick.AddListener(OnGroupButtonClicked);
            }

            if (_myPinsPanel.ConfirmButton != null)
            {
                _myPinsPanel.ConfirmButton.onClick.AddListener(() =>
                {
                    DebugLogger.Log("Confirm button clicked - confirming group selection");
                    OnGroupButtonClicked(); // Reuse group logic which checks _isSelectionMode
                });
            }

            if (_myPinsPanel.CancelButton != null)
            {
                _myPinsPanel.CancelButton.onClick.AddListener(() =>
                {
                    DebugLogger.Log("Cancel button clicked - exiting selection mode");
                    ExitSelectionMode();
                    RefreshMyPinsList();
                });
            }

            if (_myPinsPanel.CloseButton != null)
            {
                _myPinsPanel.CloseButton.onClick.AddListener(() =>
                {
                    DebugLogger.Log("Close button clicked");
                    ToggleMyPinsPanel();
                });
            }

            if (_myPinsPanel.ClearButton != null)
            {
                _myPinsPanel.ClearButton.onClick.AddListener(OnClearButtonClicked);
            }

            _myPinsPanel.SetActive(false);

            _modalOverlay = UIBuilder.CreateModalOverlay(invRoot, () =>
            {
                DebugLogger.Log("Modal overlay clicked - closing My Pins panel");
                ToggleMyPinsPanel();
            });

            // Overlay must stay behind the panel but above the rest of InventoryGui.
            _modalOverlay.transform.SetAsLastSibling();
            _myPinsPanel.transform.SetAsLastSibling();

            _groupNameDialog = UIBuilder.CreateGroupNameDialog(invRoot, _cachedFont);
            if (_groupNameDialog != null)
            {
                _groupNameDialog.OnConfirm = OnGroupNameConfirmed;
                _groupNameDialog.OnCancel = OnGroupNameCancelled;
                _groupNameDialog.SetActive(false);
            }

            _confirmDialog = UIBuilder.CreateConfirmDialog(invRoot, _cachedFont);
            if (_confirmDialog != null)
                _confirmDialog.SetActive(false);

            DebugLogger.Log("My Pins panel, overlay, and dialogs created (persistent)");
        }

        /// <summary>
        /// Toggles the My Pins panel open/closed.
        /// </summary>
        public void ToggleMyPinsPanel()
        {
            DebugLogger.Log("ToggleMyPinsPanel called");

            if (RecipePinnerPlugin.EnableMod != null && !RecipePinnerPlugin.EnableMod.Value)
            {
                HideMyPinsPanel();
                if (_myPinsButton != null)
                    _myPinsButton.gameObject.SetActive(false);
                DebugLogger.Verbose("ToggleMyPinsPanel ignored - mod disabled");
                return;
            }

            EnsureMyPinsPanel();

            if (_myPinsPanel == null)
            {
                DebugLogger.Error("ToggleMyPinsPanel: Panel is null after ensure");
                return;
            }

            bool newState = !_myPinsPanel.gameObject.activeSelf;

            // If closing, explicitly hide the controls panel before deactivating
            // so activeSelf is cleared and it won't re-appear on next open.
            if (!newState)
                _myPinsPanel.ControlsPanel?.Hide();

            _myPinsPanel.SetActive(newState);

            if (_modalOverlay != null)
                _modalOverlay.SetActive(newState);

            if (newState)
            {
                if (_modalOverlay != null) _modalOverlay.transform.SetAsLastSibling();
                _myPinsPanel.transform.SetAsLastSibling();
                if (_groupNameDialog != null) _groupNameDialog.transform.SetAsLastSibling();

                RefreshMyPinsList();
                DebugLogger.Log("My Pins panel opened (with overlay)");
            }
            else
            {
                if (_isSelectionMode)
                    ExitSelectionMode();
                DebugLogger.Log("My Pins panel closed");
            }
        }

        /// <summary>
        /// Hides the panel, overlay and dialogs without destroying them (inventory close).
        /// The button is left untouched so it slides out with the game's own close animation.
        /// </summary>
        private void HideMyPinsPanel()
        {
            if (_isSelectionMode)
                ExitSelectionMode();

            // Don't hide the button explicitly — it's parented to the crafting
            // panel container (m_craftButton.parent), so it slides out naturally
            // with the game's own close animation.

            if (_modalOverlay != null)
                _modalOverlay.SetActive(false);

            if (_myPinsPanel != null)
            {
                // Only call Hide() when actually open to avoid spurious "closed" logs.
                if (_myPinsPanel.ControlsPanel != null && ControlsInfoPanel.IsOpen)
                    _myPinsPanel.ControlsPanel.Hide();
                _myPinsPanel.SetActive(false);
            }

            if (_groupNameDialog != null)
                _groupNameDialog.SetActive(false);

            if (_confirmDialog != null)
                _confirmDialog.SetActive(false);

            // Reset dirty tracking so next open forces a refresh
            _lastKnownPinCount = -1;
            _lastKnownGroupCount = -1;

            DebugLogger.Verbose("My Pins button+panel hidden (inventory closed)");
        }

        /// <summary>
        /// Called externally on session change. Also closes the dialog if open.
        /// </summary>
        public void CloseMyPinsPanel()
        {
            HideMyPinsPanel();
        }

    }
}
