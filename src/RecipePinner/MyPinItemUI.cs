using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    /// <summary>
    /// One "icon + amount" cell on a My Pins row's materials strip. The amount is rich text, so
    /// the colours the HUD already computes carry over unchanged.
    /// </summary>
    public class MyPinMaterialUI : MonoBehaviour
    {
        public Image Icon;
        public Text AmountText;
    }

    public class MyPinItemUI : MonoBehaviour
    {
        public Image Icon;
        public Transform IconRoot;  // HorizontalLayoutGroup root for group multi-icons

        /// <summary>Parent of the materials strip drawn under the name. Hidden when empty.</summary>
        public Transform MaterialsRoot;

        /// <summary>Pooled cells for that strip. Grown on demand, surplus deactivated, never destroyed.</summary>
        public List<MyPinMaterialUI> MaterialCells = new List<MyPinMaterialUI>();

        /// <summary>The row's outer vertical layout. Cached so the refresh never looks it up.</summary>
        public VerticalLayoutGroup RowLayout;

        /// <summary>The horizontal layout holding the icon, name, count and buttons.</summary>
        public HorizontalLayoutGroup TopRowLayout;

        /// <summary>The row's own LayoutElement, whose minHeight both methods below drive.</summary>
        public LayoutElement RowHeight;

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
        /// Height the materials strip adds to a row that has one. Read rather than stored, so the
        /// setting takes effect on the next refresh without the row caching a stale number.
        /// </summary>
        public static float MaterialsStripHeight
        {
            get
            {
                ConfigEntry<float> configured = RecipePinnerPlugin.MaterialStripHeight;
                return (configured == null) ? 24f : configured.Value;
            }
        }

        /// <summary>
        /// Shows or hides the materials strip and sizes the row for it. Call this AFTER
        /// SetSubItemStyle, which sets the base height this adds to.
        /// </summary>
        public void SetMaterialsVisible(bool visible)
        {
            if (MaterialsRoot != null && MaterialsRoot.gameObject.activeSelf != visible)
                MaterialsRoot.gameObject.SetActive(visible);

            if (RowHeight != null)
                RowHeight.minHeight = (IsSubItem ? 32f : 38f) + (visible ? MaterialsStripHeight : 0f);
        }

        /// <summary>
        /// Applies sub-item visual style: indented, smaller text, lighter background.
        /// </summary>
        public void SetSubItemStyle(bool isSubItem)
        {
            IsSubItem = isSubItem;

            // The outer layout owns the row's padding now; the top strip sits inside it.
            if (RowLayout != null)
                RowLayout.padding = isSubItem
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