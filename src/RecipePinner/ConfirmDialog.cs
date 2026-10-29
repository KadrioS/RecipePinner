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

            bool escapeDown = Input.GetKeyDown(KeyCode.Escape);
            bool enterDown  = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

            // Nothing is reset here. This dialog takes no typed text, so like Valheim's own panels it
            // leaves input to the gates: the Player.TakeInput patch for actions, the inventory for the rest.

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
