using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class GroupNameDialog : MonoBehaviour
    {
        public RectTransform DialogRect;
        public Image BgImage;
        public Image OverlayBg;  // semi-transparent fullscreen background
        public InputField NameInput;
        public Button ConfirmButton;
        public Button CancelButton;

        /// <summary>
        /// Return false to keep the dialog open and let the user correct the group name.
        /// </summary>
        public System.Func<string, bool> OnConfirm;

        /// <summary>
        /// Callback invoked when Cancel is clicked or dialog is dismissed.
        /// </summary>
        public System.Action OnCancel;

        /// <summary>
        /// Set while Harmony patches should block game input behind the dialog.
        /// </summary>
        public static bool IsDialogOpen = false;

        private bool _inputLocked = false;
        private bool _listenersWired = false;

        public void SetActive(bool active)
        {
            if (active && NameInput == null)
            {
                gameObject.SetActive(false);
                UnlockGameInput();
                IsDialogOpen = false;
                DebugLogger.Error("GroupNameDialog: NameInput is null, dialog closed to avoid input lock");
                return;
            }

            gameObject.SetActive(active);

            if (active)
            {
                WireButtonListeners();
                LockGameInput();
                ClearInput();
            }
            else
            {
                UnlockGameInput();
            }
        }

        private void WireButtonListeners()
        {
            if (_listenersWired) return;

            if (ConfirmButton != null)
            {
                ConfirmButton.onClick.RemoveAllListeners();
                ConfirmButton.onClick.AddListener(OnConfirmClicked);
                ConfirmButton.onClick.AddListener(UIBuilder.PlayButtonSFX);
            }

            if (CancelButton != null)
            {
                CancelButton.onClick.RemoveAllListeners();
                CancelButton.onClick.AddListener(OnCancelClicked);
                CancelButton.onClick.AddListener(UIBuilder.PlayButtonSFX);
            }

            // InputField submit catches Enter paths that Update can miss.
            if (NameInput != null)
            {
                NameInput.onEndEdit.RemoveAllListeners();
                NameInput.onEndEdit.AddListener(OnInputEndEdit);
            }

            _listenersWired = true;
        }

        private void OnInputEndEdit(string text)
        {
            if (!IsDialogOpen || !gameObject.activeSelf) return;

            // Ignore focus-loss end-edit events; only Enter should confirm.
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                DebugLogger.Log("GroupNameDialog: Enter via onEndEdit");
                OnConfirmClicked();
            }
        }

        private void OnConfirmClicked()
        {
            if (!IsDialogOpen || !gameObject.activeSelf) return;

            string name = NameInput?.text?.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                DebugLogger.Log($"GroupNameDialog: Confirm clicked with name '{name}'");
                bool confirmed = OnConfirm?.Invoke(name) ?? true;
                if (confirmed)
                {
                    SetActive(false);
                }
                else
                {
                    ClearInput();
                }
            }
            else
            {
                var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
                string msg = locMgr?.GetText("group_name_empty") ?? "Group name cannot be empty";
                if (Player.m_localPlayer != null)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
            }
        }

        private void OnCancelClicked()
        {
            if (!IsDialogOpen || !gameObject.activeSelf) return;

            DebugLogger.Log("GroupNameDialog: Cancel clicked");
            SetActive(false);
            OnCancel?.Invoke();
        }

        private void ClearInput()
        {
            if (NameInput != null)
            {
                NameInput.text = "";
                NameInput.ActivateInputField();
                NameInput.Select();
            }
        }

        private void LockGameInput()
        {
            if (_inputLocked) return;

            IsDialogOpen = true;
            _inputLocked = true;
            DebugLogger.Log("GroupNameDialog: Game input locked (Harmony patches active)");
        }

        private void UnlockGameInput()
        {
            if (!_inputLocked) return;

            IsDialogOpen = false;
            _inputLocked = false;
            DebugLogger.Log("GroupNameDialog: Game input unlocked");
        }

        private void OnDestroy()
        {
            if (_inputLocked)
            {
                UnlockGameInput();
                DebugLogger.Warning("GroupNameDialog: Force-unlocked input on destroy");
            }
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;

            // Cache shortcuts before ResetInputAxes(), which clears this frame's GetKeyDown states.
            bool escapeDown = Input.GetKeyDown(KeyCode.Escape);
            bool enterDown  = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

            // Harmony blocks the main paths; this swallows residual Unity/ZInput state.
            if (IsDialogOpen)
            {
                Input.ResetInputAxes();

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
                catch (System.Exception) { /* ZInput may not be ready */ }
            }

            // Use cached shortcuts so Escape/Enter work even when the text field lost focus.
            if (IsDialogOpen)
            {
                if (escapeDown)
                {
                    DebugLogger.Log("GroupNameDialog: Escape pressed, cancelling");
                    OnCancelClicked();
                }
                else if (enterDown)
                {
                    DebugLogger.Log("GroupNameDialog: Enter pressed, confirming");
                    OnConfirmClicked();
                }
            }
        }
    }
}
