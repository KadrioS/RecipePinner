using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public static partial class UIBuilder
    {
        public static PinSlotUI CreatePinSlot(Transform parent, Font font)
        {
            GameObject go = new GameObject("PinSlot", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);

            PinSlotUI slot = go.AddComponent<PinSlotUI>();
            slot.Rect = go.GetComponent<RectTransform>();

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

            // ACCENT BAR
            GameObject accentObj = new GameObject("AccentBar", typeof(RectTransform)) { layer = 5 };
            accentObj.transform.SetParent(go.transform, false);
            Image accentImg = accentObj.AddComponent<Image>();
            accentImg.raycastTarget = false;
            accentImg.color = new Color(0.9f, 0.25f, 0.25f, 0.5f);

            RectTransform accentRect = accentObj.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0);
            accentRect.anchorMax = new Vector2(0, 1);
            accentRect.pivot = new Vector2(0, 0.5f);
            accentRect.sizeDelta = new Vector2(4f, 0);
            accentRect.anchoredPosition = Vector2.zero;

            LayoutElement accentLe = accentObj.AddComponent<LayoutElement>();
            accentLe.ignoreLayout = true;

            slot.AccentBar = accentImg;

            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 5;
            vlg.padding = new RectOffset(14, 8, 8, 8);

            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // HEADER
            GameObject headerObj = new GameObject("HeaderRow", typeof(RectTransform)) { layer = 5 };
            headerObj.transform.SetParent(go.transform, false);

            HorizontalLayoutGroup hlg = headerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 8;

            LayoutElement leHead = headerObj.AddComponent<LayoutElement>();
            leHead.minHeight = 30;
            leHead.flexibleHeight = 0;
            leHead.flexibleWidth = 1;

            // ICON (single recipe)
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform)) { layer = 5 };
            iconObj.transform.SetParent(headerObj.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            slot.IconImage = icon;
            LayoutElement leIcon = iconObj.AddComponent<LayoutElement>();
            int recipeIconSz = RecipePinnerPlugin.HudRecipeIconSize?.Value ?? 28;
            leIcon.minWidth = recipeIconSz; leIcon.minHeight = recipeIconSz;
            leIcon.preferredWidth = recipeIconSz; leIcon.preferredHeight = recipeIconSz; leIcon.flexibleWidth = 0;
            // GROUP ICON — stacked-cards widget, hidden by default
            int groupIconSz = RecipePinnerPlugin.HudGroupIconSize?.Value ?? 28;
            var (groupIconObj, countTxt) = CreateGroupIconWidget(headerObj.transform, font, groupIconSz);
            groupIconObj.SetActive(false);
            slot.GroupIconContainer = groupIconObj.transform;
            slot.GroupCountText = countTxt;

            // TITLE
            GameObject textObj = new GameObject("Title", typeof(RectTransform)) { layer = 5 };
            textObj.transform.SetParent(headerObj.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.raycastTarget = false;
            txt.font = font;
            txt.fontSize = 18;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.color = ValheimOrange;
            slot.HeaderText = txt;
            LayoutElement leText = textObj.AddComponent<LayoutElement>();
            leText.minHeight = 24; leText.flexibleWidth = 1;

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

            // RESOURCE LIST
            GameObject resListObj = new GameObject("ResourceList", typeof(RectTransform)) { layer = 5 };
            resListObj.transform.SetParent(go.transform, false);

            VerticalLayoutGroup rvlg = resListObj.AddComponent<VerticalLayoutGroup>();
            rvlg.childControlHeight = true;
            rvlg.childControlWidth = true;
            rvlg.childForceExpandHeight = false;
            rvlg.spacing = 3;

            ContentSizeFitter rcsf = resListObj.AddComponent<ContentSizeFitter>();
            rcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            slot.ResourceListRoot = resListObj.transform;

            // COMPACT RESOURCE LIST (4-col grid, identical to Gathering List — hidden by default)
            GameObject compactListObj = new GameObject("CompactResourceList", typeof(RectTransform)) { layer = 5 };
            compactListObj.transform.SetParent(go.transform, false);
            compactListObj.SetActive(false);

            GridLayoutGroup compactGrid = compactListObj.AddComponent<GridLayoutGroup>();
            compactGrid.cellSize = new Vector2(58, 65);   // Identical to Gathering List
            compactGrid.spacing = new Vector2(4, 3);       // Identical to Gathering List
            compactGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            compactGrid.constraintCount = 4;
            compactGrid.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter compactCsf = compactListObj.AddComponent<ContentSizeFitter>();
            compactCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            slot.CompactResourceRoot = compactListObj.transform;

            DebugLogger.Verbose("Created pin slot UI");
            return slot;
        }

        /// <summary>
        /// Creates a reusable stacked-cards group icon widget.
        /// Two overlapping parchment cards with a member count in the center.
        /// </summary>
        public static (GameObject root, Text countText) CreateGroupIconWidget(Transform parent, Font font, int size = 28)
        {
            GameObject root = new GameObject("GroupIconWidget", typeof(RectTransform)) { layer = 5 };
            root.transform.SetParent(parent, false);

            // Set sizeDelta explicitly — required for parents with childControlWidth=false (e.g. My Pins IconRoot HLG)
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(size, size);

            LayoutElement le = root.AddComponent<LayoutElement>();
            le.minWidth = size; le.minHeight = size;
            le.preferredWidth = size; le.preferredHeight = size; le.flexibleWidth = 0;

            Sprite roundedSprite = GetBackgroundSprite();
            bool isSliced = roundedSprite != null && roundedSprite.border != Vector4.zero;

            // Back card (rotated -10°)
            GameObject backCard = new GameObject("BackCard", typeof(RectTransform)) { layer = 5 };
            backCard.transform.SetParent(root.transform, false);
            Image backImg = backCard.AddComponent<Image>();
            backImg.sprite = roundedSprite;
            backImg.type = isSliced ? Image.Type.Sliced : Image.Type.Simple;
            backImg.color = new Color(0.65f, 0.52f, 0.34f, 0.92f);
            backImg.raycastTarget = false;
            RectTransform backRect = backCard.GetComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = new Vector2(1f, -2f);
            backRect.offsetMax = new Vector2(3f, -1f);
            backRect.localRotation = Quaternion.Euler(0f, 0f, -10f);

            // Front card (straight)
            GameObject frontCard = new GameObject("FrontCard", typeof(RectTransform)) { layer = 5 };
            frontCard.transform.SetParent(root.transform, false);
            Image frontImg = frontCard.AddComponent<Image>();
            frontImg.sprite = roundedSprite;
            frontImg.type = isSliced ? Image.Type.Sliced : Image.Type.Simple;
            frontImg.color = new Color(0.92f, 0.82f, 0.62f, 0.97f);
            frontImg.raycastTarget = false;
            RectTransform frontRect = frontCard.GetComponent<RectTransform>();
            frontRect.anchorMin = new Vector2(0.05f, 0.07f);
            frontRect.anchorMax = new Vector2(0.93f, 0.95f);
            frontRect.offsetMin = Vector2.zero;
            frontRect.offsetMax = Vector2.zero;

            // Count text
            GameObject countObj = new GameObject("GroupCount", typeof(RectTransform)) { layer = 5 };
            countObj.transform.SetParent(root.transform, false);
            Text countTxt = countObj.AddComponent<Text>();
            countTxt.raycastTarget = false;
            countTxt.font = font;
            countTxt.fontSize = RecipePinnerPlugin.GroupIconFontSize?.Value ?? 16;
            countTxt.fontStyle = FontStyle.Bold;
            countTxt.alignment = TextAnchor.MiddleCenter;
            countTxt.color = new Color(0.22f, 0.13f, 0.04f, 1f);
            countTxt.text = "?";
            RectTransform countRect = countObj.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;

            return (root, countTxt);
        }

        public static ResourceSlotUI CreateResourceSlot(Transform parent, Font font)
        {
            GameObject go = new GameObject("ResSlot", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);
            ResourceSlotUI slot = go.AddComponent<ResourceSlotUI>();

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 6;

            LayoutElement mainLe = go.AddComponent<LayoutElement>();
            mainLe.minHeight = 22; mainLe.flexibleHeight = 0;

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform)) { layer = 5 };
            iconObj.transform.SetParent(go.transform, false);
            slot.ResIcon = iconObj.AddComponent<Image>();
            slot.ResIcon.raycastTarget = false;
            slot.ResIcon.preserveAspect = true;
            LayoutElement leIcon = iconObj.AddComponent<LayoutElement>();
            int matIconSz = RecipePinnerPlugin.HudMaterialIconSize?.Value ?? 20;
            leIcon.minWidth = matIconSz; leIcon.minHeight = matIconSz;
            leIcon.preferredWidth = matIconSz; leIcon.preferredHeight = matIconSz; leIcon.flexibleWidth = 0;

            // Name
            GameObject nameObj = new GameObject("Name", typeof(RectTransform)) { layer = 5 };
            nameObj.transform.SetParent(go.transform, false);
            slot.ResName = nameObj.AddComponent<Text>();
            slot.ResName.raycastTarget = false;
            slot.ResName.font = font;
            slot.ResName.fontSize = 15;
            slot.ResName.alignment = TextAnchor.MiddleLeft;
            slot.ResName.horizontalOverflow = HorizontalWrapMode.Wrap;
            slot.ResName.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            LayoutElement leName = nameObj.AddComponent<LayoutElement>();
            leName.flexibleWidth = 1;

            // Amount
            GameObject amObj = new GameObject("Amount", typeof(RectTransform)) { layer = 5 };
            amObj.transform.SetParent(go.transform, false);
            slot.ResAmount = amObj.AddComponent<Text>();
            slot.ResAmount.raycastTarget = false;
            slot.ResAmount.font = font;
            slot.ResAmount.fontSize = 15;
            slot.ResAmount.alignment = TextAnchor.MiddleRight;
            LayoutElement leAmount = amObj.AddComponent<LayoutElement>();
            leAmount.minWidth = 40;

            DebugLogger.Verbose("Created resource slot UI");
            return slot;
        }

        public static GameObject CreatePaginationContainer(Transform parent)
        {
            GameObject go = new GameObject("PaginationDots", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = false;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;

            hlg.spacing = RecipePinnerPlugin.PaginationDotSpacing.Value;

            hlg.childAlignment = TextAnchor.MiddleCenter;

            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go;
        }

        public static Image CreatePageDot(Transform parent)
        {
            GameObject go = new GameObject("PageDot", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            int size = RecipePinnerPlugin.PaginationDotSize.Value;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);

            rect.localRotation = Quaternion.Euler(0, 0, 45f);

            return img;
        }

    }
}