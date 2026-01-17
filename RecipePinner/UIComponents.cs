using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class PinSlotUI : MonoBehaviour
    {
        public RectTransform Rect;
        public Image IconImage;
        public Text HeaderText;
        public Transform ResourceListRoot;

        public PinnedRecipeData CurrentData;

        private Coroutine _layoutCoroutine;

        private Image _cachedBg;
        public Image BgImage => _cachedBg ? _cachedBg : (_cachedBg = GetComponent<Image>());

        private ContentSizeFitter _cachedCsf;
        public ContentSizeFitter Csf => _cachedCsf ? _cachedCsf : (_cachedCsf = GetComponent<ContentSizeFitter>());

        public List<ResourceSlotUI> ResourceSlots = new List<ResourceSlotUI>();

        public void SetActive(bool active) => this.gameObject.SetActive(active);

        public void UpdateData(PinnedRecipeData data, Font font)
        {
            IconImage.sprite = data.Icon;

            HeaderText.text = data.CachedHeader;

            HeaderText.font = font;
            HeaderText.fontSize = RecipePinnerPlugin.FontSizeRecipeName.Value;

            HeaderText.color = RecipePinnerPlugin.ColorHeader.Value;

            int resCount = data.Resources.Count;

            while (ResourceSlots.Count < resCount)
            {
                ResourceSlots.Add(UIBuilder.CreateResourceSlot(ResourceListRoot, font));
            }

            for (int i = 0; i < ResourceSlots.Count; i++)
            {
                if (i < resCount)
                {
                    ResourceSlots[i].SetActive(true);
                    ResourceSlots[i].UpdateResource(data.Resources[i]);
                }
                else
                    ResourceSlots[i].SetActive(false);
            }

            if (this.gameObject.activeInHierarchy)
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

            if (ResourceListRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(ResourceListRoot as RectTransform);
            if (Rect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
            if (transform.parent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
        }
    }

    public class ResourceSlotUI : MonoBehaviour
    {
        public Image ResIcon;
        public Text ResName;
        public Text ResAmount;

        public void SetActive(bool active) => this.gameObject.SetActive(active);

        public void UpdateResource(PinnedResData res)
        {
            ResIcon.sprite = res.Icon;

            ResName.text = res.CachedName;
            ResAmount.text = res.CachedAmountString;

            if (RecipePinnerPlugin.FontSizeMaterials != null)
            {
                int newSize = RecipePinnerPlugin.FontSizeMaterials.Value;
                ResName.fontSize = newSize;
                ResAmount.fontSize = newSize;
            }
        }
    }
}