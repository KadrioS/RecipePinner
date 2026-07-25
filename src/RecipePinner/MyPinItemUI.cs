using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class MyPinItemUI : MonoBehaviour
    {
        public Image Icon;
        public Transform IconRoot;  // HorizontalLayoutGroup root for group multi-icons
        public Text NameText;
        public Text CountText;
        public Button DeleteButton;
        public Button PlusButton;
        public Button MinusButton;
        public Button ExpandButton;
        public Text ExpandButtonText;
        public Button DisbandButton;
        public Toggle SelectToggle;

        /// <summary>
        /// The recipe key (PinnedRecipes key) for individual pins,
        /// or the group name (PinGroups key) for group items.
        /// </summary>
        public string RecipeKey;

        /// <summary>
        /// True if this item represents a group pin.
        /// </summary>
        public bool IsGroupItem;

        /// <summary>
        /// True if this item is a sub-item inside an expanded group dropdown.
        /// </summary>
        public bool IsSubItem;

        /// <summary>
        /// The parent group name if this is a sub-item.
        /// </summary>
        public string ParentGroupName;

        public void SetActive(bool active) => gameObject.SetActive(active);

        /// <summary>
        /// Applies sub-item visual style: indented, smaller text, lighter background.
        /// </summary>
        public void SetSubItemStyle(bool isSubItem)
        {
            IsSubItem = isSubItem;

            var hlg = GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
                hlg.padding = isSubItem
                    ? new RectOffset(24, 6, 2, 2)
                    : new RectOffset(6, 6, 4, 4);

            var le = GetComponent<LayoutElement>();
            if (le != null)
                le.minHeight = isSubItem ? 32 : 38;

            if (NameText != null)
                NameText.fontSize = isSubItem ? 13 : 15;

            if (CountText != null)
                CountText.fontSize = isSubItem ? 13 : 15;

            var bg = GetComponent<Image>();
            if (bg != null)
                bg.color = isSubItem
                    ? new Color(0.15f, 0.15f, 0.15f, 0.3f)
                    : new Color(0, 0, 0, 0.25f);
        }

        /// <summary>
        /// Shows or hides the selection toggle (used in grouping mode).
        /// </summary>
        public void SetSelectionMode(bool selectionMode)
        {
            if (SelectToggle != null)
            {
                SelectToggle.gameObject.SetActive(selectionMode && !IsGroupItem && !IsSubItem);
                if (!selectionMode)
                    SelectToggle.isOn = false;
            }

            // Hide +/- and X buttons during selection mode (but don't force-show them;
            // RefreshMyPinsList manages minus visibility based on count)
            if (DeleteButton != null && selectionMode) DeleteButton.gameObject.SetActive(false);
            if (PlusButton != null && selectionMode) PlusButton.gameObject.SetActive(false);
            if (MinusButton != null && selectionMode) MinusButton.gameObject.SetActive(false);

            // Hide expand and disband buttons during selection mode
            if (ExpandButton != null && IsGroupItem)
                ExpandButton.gameObject.SetActive(!selectionMode);
            if (DisbandButton != null && IsGroupItem)
                DisbandButton.gameObject.SetActive(!selectionMode);
        }
    }
}