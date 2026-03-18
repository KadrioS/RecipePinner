using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class GatheringItemUI : MonoBehaviour
    {
        public Image Icon;
        public Text AmountText;

        public void SetActive(bool active) => gameObject.SetActive(active);
    }

    public class GatheringListUI : MonoBehaviour
    {
        public RectTransform PanelRect;
        public Image BgImage;
        public Text TitleText;
        public Transform ItemListRoot;
        public Text HintText;
        public List<GatheringItemUI> ItemSlots = new List<GatheringItemUI>();

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

            if (ItemListRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(ItemListRoot as RectTransform);
            if (PanelRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(PanelRect);
        }
    }

    public class GatheringItemData
    {
        public string ItemName;
        public string DisplayName;
        public Sprite Icon;
        public int TotalRequired;
        public int TotalHave;
        public bool IsComplete;
    }
}
