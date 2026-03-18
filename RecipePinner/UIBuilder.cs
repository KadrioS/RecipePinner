using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public static class UIBuilder
    {
        private static Color ValheimOrange = new Color(1f, 0.77f, 0.31f, 1f); // #FFC550
        private static Color DividerColor = new Color(1f, 1f, 1f, 0.10f);

        private static Sprite _cachedUiSprite;
        private static bool _spriteSearchDone = false;

        private static Sprite GetBackgroundSprite()
        {
            if (_cachedUiSprite != null) return _cachedUiSprite;

            if (_spriteSearchDone) return null;

            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();

            Sprite fallback = null;
            foreach (var s in allSprites)
            {
                if (s == null) continue;
                if (s.name == "UISprite") { _cachedUiSprite = s; break; }
                if (fallback == null && s.name == "Knob") fallback = s;
            }
            if (_cachedUiSprite == null) _cachedUiSprite = fallback;

            _spriteSearchDone = true;

            if (_cachedUiSprite != null)
                DebugLogger.Verbose($"Found background sprite: {_cachedUiSprite.name}");
            else
                DebugLogger.Warning("No suitable background sprite found");

            return _cachedUiSprite;
        }

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

            // ICON
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform)) { layer = 5 };
            iconObj.transform.SetParent(headerObj.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            slot.IconImage = icon;
            LayoutElement leIcon = iconObj.AddComponent<LayoutElement>();
            leIcon.minWidth = 28; leIcon.minHeight = 28;
            leIcon.preferredWidth = 28; leIcon.preferredHeight = 28; leIcon.flexibleWidth = 0;

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

            DebugLogger.Verbose("Created pin slot UI");
            return slot;
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
            leIcon.minWidth = 20; leIcon.minHeight = 20;
            leIcon.preferredWidth = 20; leIcon.preferredHeight = 20; leIcon.flexibleWidth = 0;

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
            headerText.color = ValheimOrange;
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
    }
}