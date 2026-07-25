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

        /// <summary>
        /// Applies column configuration to the Gathering List grid.
        /// If configCols > 0: sets that column count and resizes panel width to fit.
        /// If configCols == 0: calculates column count from current panel width.
        /// </summary>
        public void ApplyColumns(int configCols)
        {
            if (ItemListRoot == null) return;
            GridLayoutGroup grid = ItemListRoot.GetComponent<GridLayoutGroup>();
            if (grid == null) return;

            VerticalLayoutGroup vlg = GetComponent<VerticalLayoutGroup>();
            float padH = vlg != null ? vlg.padding.left + vlg.padding.right : 22f;
            float cellW = grid.cellSize.x;
            float spaceW = grid.spacing.x;

            if (configCols > 0)
            {
                // Explicit: set column count and resize panel
                grid.constraintCount = configCols;
                float newWidth = configCols * (cellW + spaceW) - spaceW + padH;
                PanelRect.sizeDelta = new Vector2(newWidth, PanelRect.sizeDelta.y);
            }
            else
            {
                // Auto: calculate columns from current width
                float available = PanelRect.sizeDelta.x - padH;
                int cols = Mathf.Max(1, Mathf.FloorToInt((available + spaceW) / (cellW + spaceW)));
                grid.constraintCount = cols;
            }
        }

        /// <summary>
        /// Returns the panel width needed for the given number of columns.
        /// </summary>
        public static float CalculateWidthForColumns(int cols)
        {
            float cellW = 58f, spaceW = 4f, padH = 22f;
            return cols * (cellW + spaceW) - spaceW + padH;
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
