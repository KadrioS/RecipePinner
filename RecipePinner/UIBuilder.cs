using ValheimRecipePinner;
using System.Linq;
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

            _cachedUiSprite = allSprites.FirstOrDefault(x => x.name == "UISprite")
                           ?? allSprites.FirstOrDefault(x => x.name == "Knob");

            _spriteSearchDone = true;

            if (_cachedUiSprite != null)
                DebugLogger.Verbose($"Found background sprite: {_cachedUiSprite.name}");
            else
                DebugLogger.Warning("No suitable background sprite found");

            return _cachedUiSprite;
        }

        public static PinSlotUI CreatePinSlot(Transform parent, Font font)
        {
            GameObject go = new GameObject("PinSlot", typeof(RectTransform));
            go.layer = 5;
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

            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 5;
            vlg.padding = new RectOffset(8, 8, 8, 8);

            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // HEADER
            GameObject headerObj = new GameObject("HeaderRow", typeof(RectTransform));
            headerObj.layer = 5;
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
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.layer = 5;
            iconObj.transform.SetParent(headerObj.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            slot.IconImage = icon;
            LayoutElement leIcon = iconObj.AddComponent<LayoutElement>();
            leIcon.minWidth = 28; leIcon.minHeight = 28;
            leIcon.preferredWidth = 28; leIcon.preferredHeight = 28; leIcon.flexibleWidth = 0;

            // TITLE
            GameObject textObj = new GameObject("Title", typeof(RectTransform));
            textObj.layer = 5;
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
            GameObject divObj = new GameObject("Divider", typeof(RectTransform));
            divObj.layer = 5;
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
            GameObject resListObj = new GameObject("ResourceList", typeof(RectTransform));
            resListObj.layer = 5;
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
            GameObject go = new GameObject("ResSlot", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            ResourceSlotUI slot = go.AddComponent<ResourceSlotUI>();

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 6;

            LayoutElement mainLe = go.AddComponent<LayoutElement>();
            mainLe.minHeight = 22; mainLe.flexibleHeight = 0;

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.layer = 5;
            iconObj.transform.SetParent(go.transform, false);
            slot.ResIcon = iconObj.AddComponent<Image>();
            slot.ResIcon.raycastTarget = false;
            slot.ResIcon.preserveAspect = true;
            LayoutElement leIcon = iconObj.AddComponent<LayoutElement>();
            leIcon.minWidth = 20; leIcon.minHeight = 20;
            leIcon.preferredWidth = 20; leIcon.preferredHeight = 20; leIcon.flexibleWidth = 0;

            // Name
            GameObject nameObj = new GameObject("Name", typeof(RectTransform));
            nameObj.layer = 5;
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
            GameObject amObj = new GameObject("Amount", typeof(RectTransform));
            amObj.layer = 5;
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
    }
}