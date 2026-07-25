using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class ConfirmDialog : MonoBehaviour
    {
        public RectTransform DialogRect;
        public Image BgImage;
        public Image OverlayBg;
        public Text MessageText;
        public Button ConfirmButton;
        public Button CancelButton;

        /// <summary>
        /// Callback invoked when Confirm is clicked.
        /// </summary>
        public System.Action OnConfirm;

        /// <summary>
        /// Callback invoked when Cancel is clicked.
        /// </summary>
        public System.Action OnCancel;

        public static bool IsDialogOpen = false;

        private bool _listenersWired = false;

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            IsDialogOpen = active;
        }

        /// <summary>
        /// Shows the dialog with the specified message and callbacks.
        /// </summary>
        public void Show(string message, System.Action onConfirm, System.Action onCancel = null)
        {
            OnConfirm = onConfirm;
            OnCancel = onCancel;

            if (MessageText != null)
                MessageText.text = message;

            WireButtonListeners();
            transform.SetAsLastSibling(); // Ensure on top of everything
            SetActive(true);
            DebugLogger.Log($"ConfirmDialog shown: {message}");
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

            _listenersWired = true;
        }

        private void OnConfirmClicked()
        {
            if (!IsDialogOpen || !gameObject.activeSelf) return;

            DebugLogger.Log("ConfirmDialog: Confirm clicked");
            SetActive(false);
            OnConfirm?.Invoke();
        }

        private void OnCancelClicked()
        {
            if (!IsDialogOpen || !gameObject.activeSelf) return;

            DebugLogger.Log("ConfirmDialog: Cancel clicked");
            SetActive(false);
            OnCancel?.Invoke();
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;

            // Cache shortcuts before ResetInputAxes(), which clears this frame's GetKeyDown states.
            bool escapeDown = Input.GetKeyDown(KeyCode.Escape);
            bool enterDown  = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

            // Dialogs should swallow residual game input even if Harmony blocking misses a path.
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
                }
            }
            catch (System.Exception) { /* ZInput may not be ready */ }

            if (escapeDown)
            {
                OnCancelClicked();
            }
            else if (enterDown)
            {
                OnConfirmClicked();
            }
        }

        private void OnDestroy()
        {
            if (IsDialogOpen)
            {
                IsDialogOpen = false;
                DebugLogger.Warning("ConfirmDialog: Force-closed on destroy");
            }
        }
    }
}
