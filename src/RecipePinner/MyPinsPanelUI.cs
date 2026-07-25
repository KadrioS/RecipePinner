using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class MyPinsPanelUI : MonoBehaviour
    {
        public RectTransform PanelRect;
        public Image BgImage;
        public Text TitleText;
        public Transform PinListRoot;
        public ScrollRect ScrollView;
        public Button GroupButton;
        public Button ConfirmButton;
        public Button CancelButton;
        public Button ClearButton;
        public Button CloseButton;
        public Button InfoButton;
        public ControlsInfoPanel ControlsPanel;
        public Text GroupButtonText;
        public Text EmptyText;

        public List<MyPinItemUI> PinItems = new List<MyPinItemUI>();

        private Coroutine _layoutCoroutine;

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void RefreshLayout()
        {
            if (gameObject.activeInHierarchy)
            {
                if (_layoutCoroutine != null) StopCoroutine(_layoutCoroutine);
                _layoutCoroutine = StartCoroutine(FixLayout());
            }
        }

        private void OnDisable()
        {
            _layoutCoroutine = null;
        }

        private System.Collections.IEnumerator FixLayout()
        {
            yield return null;

            if (PinListRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(PinListRoot as RectTransform);
            if (PanelRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(PanelRect);
        }
    }
}
