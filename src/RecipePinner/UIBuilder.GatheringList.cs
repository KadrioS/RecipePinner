using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public static partial class UIBuilder
    {
        public static GatheringListUI CreateGatheringListPanel(Transform parent, Font font, string title)
        {
            GameObject go = new GameObject("GatheringListPanel", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);

            GatheringListUI panel = go.AddComponent<GatheringListUI>();
            panel.PanelRect = go.GetComponent<RectTransform>();

            // BACKGROUND
            Image bg = go.AddComponent<Image>();
            Sprite bgSprite = GetBackgroundSprite();
            bg.sprite = bgSprite;

            if (bgSprite != null && bgSprite.border != Vector4.zero)
                bg.type = Image.Type.Sliced;
            else
                bg.type = Image.Type.Simple;

            float alpha = RecipePinnerPlugin.BackgroundOpacity != null ? RecipePinnerPlugin.BackgroundOpacity.Value : 0.45f;
            bg.color = new Color(0, 0, 0, alpha);
            bg.raycastTarget = false;
            panel.BgImage = bg;

            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4;
            vlg.padding = new RectOffset(14, 8, 8, 8);

            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // HEADER
            GameObject headerObj = new GameObject("Header", typeof(RectTransform)) { layer = 5 };
            headerObj.transform.SetParent(go.transform, false);
            Text headerText = headerObj.AddComponent<Text>();
            headerText.raycastTarget = false;
            headerText.font = font;
            int titleSize = RecipePinnerPlugin.GatheringListFontSizeTitle != null
                ? RecipePinnerPlugin.GatheringListFontSizeTitle.Value : 15;
            headerText.fontSize = titleSize;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.color = RecipePinnerPlugin.ColorHeader?.Value ?? ValheimOrange;
            headerText.text = title;
            panel.TitleText = headerText;

            LayoutElement leHeader = headerObj.AddComponent<LayoutElement>();
            leHeader.minHeight = 24;
            leHeader.flexibleWidth = 1;

            // DIVIDER
            GameObject divObj = new GameObject("Divider", typeof(RectTransform)) { layer = 5 };
            divObj.transform.SetParent(go.transform, false);
            Image divImg = divObj.AddComponent<Image>();
            divImg.sprite = GetBackgroundSprite();
            if (bgSprite != null && bgSprite.border != Vector4.zero)
                divImg.type = Image.Type.Sliced;
            else
                divImg.type = Image.Type.Simple;
            divImg.color = DividerColor;
            divImg.raycastTarget = false;

            LayoutElement leDiv = divObj.AddComponent<LayoutElement>();
            leDiv.minHeight = 2;
            leDiv.preferredHeight = 2;
            leDiv.flexibleWidth = 1;

            // ITEM LIST ROOT
            GameObject listObj = new GameObject("ItemList", typeof(RectTransform)) { layer = 5 };
            listObj.transform.SetParent(go.transform, false);

            GridLayoutGroup grid = listObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(58, 65);
            grid.spacing = new Vector2(4, 3);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter listCsf = listObj.AddComponent<ContentSizeFitter>();
            listCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            panel.ItemListRoot = listObj.transform;

            // HINT TEXT
            GameObject hintObj = new GameObject("HintText", typeof(RectTransform)) { layer = 5 };
            hintObj.transform.SetParent(go.transform, false);
            Text hintText = hintObj.AddComponent<Text>();
            hintText.raycastTarget = false;
            hintText.font = font;
            hintText.fontSize = 20;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            hintText.text = "";
            panel.HintText = hintText;

            LayoutElement leHint = hintObj.AddComponent<LayoutElement>();
            leHint.minHeight = 22;
            leHint.flexibleWidth = 1;

            DebugLogger.Verbose("Created gathering list panel");
            return panel;
        }

        public static GatheringItemUI CreateGatheringItemSlot(Transform parent, Font font)
        {
            GameObject go = new GameObject("GatherItem", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            GatheringItemUI slot = go.AddComponent<GatheringItemUI>();

            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 1;
            vlg.padding = new RectOffset(0, 0, 1, 0);

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform)) { layer = 5 };
            iconObj.transform.SetParent(go.transform, false);
            slot.Icon = iconObj.AddComponent<Image>();
            slot.Icon.raycastTarget = false;
            slot.Icon.preserveAspect = true;
            LayoutElement leIcon = iconObj.AddComponent<LayoutElement>();
            leIcon.minWidth = 45; leIcon.minHeight = 45;
            leIcon.preferredWidth = 45; leIcon.preferredHeight = 45;

            // Amount
            GameObject amObj = new GameObject("Amount", typeof(RectTransform)) { layer = 5 };
            amObj.transform.SetParent(go.transform, false);
            slot.AmountText = amObj.AddComponent<Text>();
            slot.AmountText.raycastTarget = false;
            slot.AmountText.font = font;
            int matSize = RecipePinnerPlugin.GatheringListFontSizeMaterials != null
                ? RecipePinnerPlugin.GatheringListFontSizeMaterials.Value : 15;
            slot.AmountText.fontSize = matSize;
            slot.AmountText.alignment = TextAnchor.MiddleCenter;
            slot.AmountText.horizontalOverflow = HorizontalWrapMode.Overflow;
            LayoutElement leAmount = amObj.AddComponent<LayoutElement>();
            leAmount.minHeight = 16;

            return slot;
        }

        // ============================================================
        // My Pins Panel Builder Methods
        // ============================================================

        /// <summary>
        /// Tries to extract the background sprite and material from the trophies panel.
        /// Checks both the root object's Image and a child named "background".
        /// </summary>
    }
}