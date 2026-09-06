using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public static partial class UIBuilder
    {
        public static Button CreateMyPinsButton(Transform parent, Font font)
        {
            DebugLogger.Log("Creating My Pins button");

            int size = RecipePinnerPlugin.MyPinsButtonSize?.Value ?? 50;

            Sprite pinIcon = LoadPinIconSprite();

            // Icon mode stays square; text fallback needs room for the label.
            Vector2 effectiveSize = (pinIcon != null)
                ? new Vector2(size, size)
                : new Vector2(120f, size);

            string buttonText = (pinIcon != null)
                ? string.Empty
                : (RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("mypins_button") ?? "\uD83D\uDCCC");

            Button btn = CreateVanillaButton(parent, buttonText, minWidth: (int)effectiveSize.x, minHeight: (int)effectiveSize.y);
            btn.gameObject.name = "MyPinsButton";

            LayoutElement le = btn.GetComponent<LayoutElement>();
            le.flexibleWidth = 0;
            le.preferredWidth = effectiveSize.x;

            RectTransform btnRect = btn.GetComponent<RectTransform>();
            btnRect.sizeDelta = effectiveSize;

            Text txt = btn.GetComponentInChildren<Text>();

            if (pinIcon != null)
            {
                if (txt != null) txt.gameObject.SetActive(false);

                GameObject iconGo = new GameObject("PinIcon", typeof(RectTransform)) { layer = 5 };
                iconGo.transform.SetParent(btn.transform, false);

                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                float pad = 6f;
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(pad, pad);
                iconRect.offsetMax = new Vector2(-pad, -pad);

                Image iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = pinIcon;
                iconImg.preserveAspect = true;
                Color iconColor = RecipePinnerPlugin.ButtonTextColor?.Value
                    ?? (_vanillaBtnTextColor != default ? _vanillaBtnTextColor : Color.white);
                iconImg.color = iconColor;
                iconImg.raycastTarget = false;

                DebugLogger.Log("My Pins button: icon mode");
            }
            else
            {
                if (txt != null) txt.fontSize = 16;
                DebugLogger.Log("My Pins button: text fallback mode");
            }

            // Hover label above the button, in the font Valheim's own button tooltips use. Those
            // draw with TextMeshPro, so a UnityEngine.UI.Text can never match them; the font asset
            // is read off the prefab they render with - the same "Tooltip" prefab the Compendium,
            // Skills and Trophies buttons point at. UITooltip is used only to find that prefab. No
            // UITooltip component is added and none of its shared statics are touched: an exception
            // inside that class stops every tooltip in the game, which is exactly what happened
            // when this feature was first built on top of it. (U14)
            TMP_FontAsset labelFont = null;
            float labelFontSize = 16f;
            Color labelColor = Color.white;
            foreach (UITooltip donor in Resources.FindObjectsOfTypeAll<UITooltip>())
            {
                if (donor == null || donor.m_tooltipPrefab == null || donor.m_tooltipPrefab.name != "Tooltip")
                    continue;

                TMP_Text sample = donor.m_tooltipPrefab.GetComponentInChildren<TMP_Text>(true);
                if (sample == null || sample.font == null)
                    continue;

                labelFont = sample.font;
                labelFontSize = sample.fontSize;
                labelColor = sample.color;
                DebugLogger.Log($"My Pins button: hover label font '{labelFont.name}' size {labelFontSize}");
                break;
            }

            GameObject labelGo = new GameObject("MyPinsHoverLabel", typeof(RectTransform)) { layer = 5 };
            labelGo.transform.SetParent(btn.transform, false);

            // Switched off before the text component is added, not after. TextMeshPro looks for its
            // own default font asset in Awake, and Valheim does not ship the TMP Essentials that
            // asset comes from - so adding the component to a live object logs "The LiberationSans
            // SDF Font Asset was not found" once per launch, in every player's log, before our own
            // font assignment on the next line ever runs. An inactive object does not Awake, and by
            // the time HoverLabel switches it on the font is set. The label starts hidden anyway,
            // so this only moves the line; do not tidy it back down.
            labelGo.SetActive(false);

            // Anchored to the button's top-left corner and pivoted at its own bottom-left, so the
            // text sits directly above the button - clear of the pointer - and grows to the right,
            // into the panel rather than off its left edge.
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 4f);
            labelRect.sizeDelta = new Vector2(200f, 22f);

            string labelText = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("mypins_title") ?? "MY PINS";

            if (labelFont != null)
            {
                TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
                labelTmp.raycastTarget = false;
                labelTmp.font = labelFont;
                labelTmp.fontSize = labelFontSize;
                labelTmp.color = labelColor;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
                labelTmp.overflowMode = TextOverflowModes.Overflow;
                labelTmp.text = labelText;
            }
            else
            {
                // Nothing to copy from: fall back to the mod's own button font so the label still
                // appears, just not identical to vanilla's.
                Text labelTxt = labelGo.AddComponent<Text>();
                labelTxt.raycastTarget = false;
                labelTxt.alignment = TextAnchor.MiddleLeft;
                labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                labelTxt.verticalOverflow = VerticalWrapMode.Overflow;
                labelTxt.color = Color.white;
                labelTxt.text = labelText;
                labelTxt.font = _vanillaBtnFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                labelTxt.fontSize = _vanillaBtnFont != null ? _vanillaBtnFontSize : 16;
                labelTxt.fontStyle = _vanillaBtnFont != null ? _vanillaBtnFontStyle : FontStyle.Bold;

                Outline labelOutline = labelGo.AddComponent<Outline>();
                labelOutline.effectColor = Color.black;
                labelOutline.effectDistance = new Vector2(1f, -1f);

                DebugLogger.Log("My Pins button: no vanilla tooltip font found, hover label uses the mod's button font");
            }

            btn.gameObject.AddComponent<HoverLabel>().Label = labelGo;
            DebugLogger.Log($"My Pins button: hover label '{labelText}'");

            DebugLogger.Log("My Pins button created");
            return btn;
        }

        /// <summary>
        /// Creates the persistent My Pins panel using Valheim's trophies-panel styling where available.
        /// </summary>
        public static MyPinsPanelUI CreateMyPinsPanel(Transform parent, Font font)
        {
            DebugLogger.Log("Creating My Pins panel");

            GameObject go = new GameObject("MyPinsPanel", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);

            MyPinsPanelUI panel = go.AddComponent<MyPinsPanelUI>();
            panel.PanelRect = go.GetComponent<RectTransform>();

            float panelW = RecipePinnerPlugin.MyPinsPanelWidth?.Value ?? 340f;
            float panelH = RecipePinnerPlugin.MyPinsPanelHeight?.Value ?? 450f;
            panel.PanelRect.sizeDelta = new Vector2(panelW, panelH);

            Image bgImage = go.AddComponent<Image>();
            if (TryGetTrophiesPanelBackground(out Sprite trophySprite, out Material trophyMaterial))
            {
                bgImage.sprite = trophySprite;
                bgImage.material = trophyMaterial;
                bgImage.type = (trophySprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
                bgImage.color = Color.white;
                DebugLogger.Log("My Pins panel: Using trophies panel background");
            }
            else
            {
                Sprite fallbackSprite = GetBackgroundSprite();
                bgImage.sprite = fallbackSprite;
                if (fallbackSprite != null && fallbackSprite.border != Vector4.zero)
                    bgImage.type = Image.Type.Sliced;
                else
                    bgImage.type = Image.Type.Simple;
                bgImage.color = new Color(0, 0, 0, 0.85f);
                DebugLogger.Warning("My Pins panel: Using fallback background");
            }
            bgImage.raycastTarget = true;
            panel.BgImage = bgImage;

            VerticalLayoutGroup mainVlg = go.AddComponent<VerticalLayoutGroup>();
            mainVlg.childControlHeight = true;
            mainVlg.childControlWidth = true;
            mainVlg.childForceExpandHeight = false;
            mainVlg.spacing = 6;
            mainVlg.padding = new RectOffset(14, 14, 12, 12);

            GameObject titleObj = new GameObject("Title", typeof(RectTransform)) { layer = 5 };
            titleObj.transform.SetParent(go.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.raycastTarget = false;
            // Prefer Valheim's Norse title font; fall back to the supplied game font.
            Font norseBold = null;
            foreach (Font f in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (f != null && f.name.ToLowerInvariant().Contains("norse"))
                {
                    DebugLogger.Log($"Found Norse font variant: '{f.name}'");
                    if (norseBold == null || f.name.ToLowerInvariant().Contains("bold"))
                        norseBold = f;
                }
            }
            if (norseBold != null)
                DebugLogger.Log($"Using Norse font: '{norseBold.name}'");
            else
                DebugLogger.Warning("Norse font not found, using fallback game font");
            titleText.font = norseBold ?? font;
            titleText.fontSize = 28;
            titleText.fontStyle = norseBold != null ? FontStyle.Normal : FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.718f, 0.357f, 1f); // #ffb75b
            string title = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("mypins_title") ?? "MY PINS";
            titleText.text = title;

            Outline titleOutline = titleObj.AddComponent<Outline>();
            titleOutline.effectColor = Color.black;
            titleOutline.effectDistance = new Vector2(2f, -2f);

            LayoutElement leTitle = titleObj.AddComponent<LayoutElement>();
            leTitle.minHeight = 36;
            leTitle.flexibleWidth = 1;

            GameObject topBtnRow = new GameObject("TopButtonRow", typeof(RectTransform)) { layer = 5 };
            topBtnRow.transform.SetParent(go.transform, false);
            HorizontalLayoutGroup topBtnHlg = topBtnRow.AddComponent<HorizontalLayoutGroup>();
            topBtnHlg.spacing = 6;
            topBtnHlg.childControlHeight = true;
            topBtnHlg.childControlWidth = true;
            topBtnHlg.childForceExpandWidth = true;
            topBtnHlg.childForceExpandHeight = false;
            LayoutElement leTopBtnRow = topBtnRow.AddComponent<LayoutElement>();
            leTopBtnRow.minHeight = 47;
            leTopBtnRow.preferredHeight = 47;
            leTopBtnRow.flexibleHeight = 0;
            leTopBtnRow.flexibleWidth = 1;

            string grpLabel = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_button") ?? "Group";
            Button grpBtn = CreateVanillaButton(topBtnRow.transform, grpLabel, minHeight: 47);
            grpBtn.gameObject.name = "GroupButton";
            panel.GroupButton = grpBtn;
            Text grpTxt = grpBtn.GetComponentInChildren<Text>();
            if (grpTxt != null) grpTxt.fontSize = 20;
            LayoutElement leGrpBtn = grpBtn.GetComponent<LayoutElement>();
            if (leGrpBtn != null) { leGrpBtn.minHeight = 47; leGrpBtn.preferredHeight = 47; leGrpBtn.flexibleHeight = 0; }

            string clearLabel = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("clear_button") ?? "Clear";
            Button clearBtn = CreateVanillaButton(topBtnRow.transform, clearLabel, minHeight: 47);
            clearBtn.gameObject.name = "ClearButton";
            panel.ClearButton = clearBtn;
            Text clearTxt = clearBtn.GetComponentInChildren<Text>();
            if (clearTxt != null) clearTxt.fontSize = 20;
            LayoutElement leClearBtn = clearBtn.GetComponent<LayoutElement>();
            if (leClearBtn != null) { leClearBtn.minHeight = 47; leClearBtn.preferredHeight = 47; leClearBtn.flexibleHeight = 0; }

            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform)) { layer = 5 };
            scrollObj.transform.SetParent(go.transform, false);

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.35f);
            scrollBg.raycastTarget = true;

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 200f;

            LayoutElement leScroll = scrollObj.AddComponent<LayoutElement>();
            leScroll.flexibleHeight = 1;
            leScroll.flexibleWidth = 1;

            float scrollbarWidth = 10f;
            GameObject scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform)) { layer = 5 };
            scrollbarObj.transform.SetParent(scrollObj.transform, false);
            RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(scrollbarWidth, 0);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image scrollbarBg = scrollbarObj.AddComponent<Image>();
            scrollbarBg.color = new Color(0, 0, 0, 0.3f);

            GameObject handleArea = new GameObject("HandleArea", typeof(RectTransform)) { layer = 5 };
            handleArea.transform.SetParent(scrollbarObj.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handleObj = new GameObject("Handle", typeof(RectTransform)) { layer = 5 };
            handleObj.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = new Vector2(1, 0.3f);
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = Color.white; // White base — ColorBlock handles actual colors

            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImg;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.transition = Selectable.Transition.ColorTint;
            ColorBlock scrollbarColors = scrollbar.colors;
            scrollbarColors.normalColor = new Color(0.89f, 0.62f, 0.33f, 1f);      // #e39e53
            scrollbarColors.highlightedColor = new Color(0.96f, 0.76f, 0.08f, 1f);  // #f5c114
            scrollbarColors.pressedColor = new Color(0.75f, 0.58f, 0.05f, 1f);      // #c0930c
            scrollbarColors.selectedColor = new Color(0.89f, 0.62f, 0.33f, 1f);     // #e39e53 (same as normal)
            scrollbarColors.disabledColor = new Color(0.89f, 0.62f, 0.33f, 0.5f);
            scrollbarColors.colorMultiplier = 1f;
            scrollbarColors.fadeDuration = 0.1f;
            scrollbar.colors = scrollbarColors;

            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform)) { layer = 5 };
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero; // ScrollbarViewportFitter adjusts this dynamically

            // RectMask2D is more reliable than Mask+Image for clipping scroll content.
            viewportObj.AddComponent<RectMask2D>();

            scrollRect.viewport = viewportRect;

            ScrollbarViewportFitter fitter = scrollObj.AddComponent<ScrollbarViewportFitter>();
            fitter.VerticalScrollbar = scrollbar;
            fitter.Viewport = viewportRect;
            fitter.ScrollbarWidth = scrollbarWidth;

            GameObject contentObj = new GameObject("Content", typeof(RectTransform)) { layer = 5 };
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = new Vector2(0, 0);
            contentRect.offsetMax = new Vector2(0, 0);

            VerticalLayoutGroup contentVlg = contentObj.AddComponent<VerticalLayoutGroup>();
            contentVlg.childControlHeight = true;
            contentVlg.childControlWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = 4;

            ContentSizeFitter contentCsf = contentObj.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            panel.PinListRoot = contentObj.transform;

            GameObject emptyObj = new GameObject("EmptyText", typeof(RectTransform)) { layer = 5 };
            emptyObj.transform.SetParent(contentObj.transform, false);
            Text emptyText = emptyObj.AddComponent<Text>();
            emptyText.raycastTarget = false;
            emptyText.font = font;
            emptyText.fontSize = 16;
            emptyText.alignment = TextAnchor.MiddleCenter;
            emptyText.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            string emptyMsg = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("mypins_empty") ?? "No Recipes Pinned";
            emptyText.text = emptyMsg;
            panel.EmptyText = emptyText;

            LayoutElement leEmpty = emptyObj.AddComponent<LayoutElement>();
            leEmpty.minHeight = 40;
            leEmpty.flexibleWidth = 1;

            GameObject confirmRowContainer = new GameObject("ConfirmRowContainer", typeof(RectTransform)) { layer = 5 };
            confirmRowContainer.transform.SetParent(go.transform, false);
            HorizontalLayoutGroup confirmRowHlg = confirmRowContainer.AddComponent<HorizontalLayoutGroup>();
            confirmRowHlg.childControlHeight = true;
            confirmRowHlg.childControlWidth = true;
            confirmRowHlg.childForceExpandWidth = true;
            confirmRowHlg.childForceExpandHeight = false;
            confirmRowHlg.spacing = 6;
            LayoutElement leConfirmRow = confirmRowContainer.AddComponent<LayoutElement>();
            leConfirmRow.minHeight = 47;
            leConfirmRow.preferredHeight = 47;
            leConfirmRow.flexibleHeight = 0;
            leConfirmRow.flexibleWidth = 1;

            string cancelLabel = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_cancel") ?? "Cancel";
            Button cancelBtn = CreateVanillaButton(confirmRowContainer.transform, cancelLabel, minHeight: 47);
            cancelBtn.gameObject.name = "CancelButton";
            panel.CancelButton = cancelBtn;
            Text cancelTxt = cancelBtn.GetComponentInChildren<Text>();
            if (cancelTxt != null) cancelTxt.fontSize = 20;
            LayoutElement leCancelBtn = cancelBtn.GetComponent<LayoutElement>();
            if (leCancelBtn != null) { leCancelBtn.minHeight = 47; leCancelBtn.preferredHeight = 47; leCancelBtn.flexibleHeight = 0; }

            string confirmLabel = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_confirm") ?? "Confirm";
            Button confirmBtn = CreateVanillaButton(confirmRowContainer.transform, confirmLabel, minHeight: 47);
            confirmBtn.gameObject.name = "ConfirmButton";
            panel.ConfirmButton = confirmBtn;
            Text confirmTxt = confirmBtn.GetComponentInChildren<Text>();
            if (confirmTxt != null) confirmTxt.fontSize = 20;
            LayoutElement leConfirmBtn = confirmBtn.GetComponent<LayoutElement>();
            if (leConfirmBtn != null) { leConfirmBtn.minHeight = 47; leConfirmBtn.preferredHeight = 47; leConfirmBtn.flexibleHeight = 0; }

            confirmRowContainer.SetActive(false); // Hidden by default

            GameObject closeBtnContainer = new GameObject("CloseButtonContainer", typeof(RectTransform)) { layer = 5 };
            closeBtnContainer.transform.SetParent(go.transform, false);
            HorizontalLayoutGroup closeHlg = closeBtnContainer.AddComponent<HorizontalLayoutGroup>();
            closeHlg.childControlHeight = true;
            closeHlg.childControlWidth = false;
            closeHlg.childForceExpandWidth = false;
            closeHlg.childAlignment = TextAnchor.MiddleCenter;
            LayoutElement leCloseContainer = closeBtnContainer.AddComponent<LayoutElement>();
            leCloseContainer.minHeight = 47;
            leCloseContainer.preferredHeight = 47;
            leCloseContainer.flexibleHeight = 0;
            leCloseContainer.flexibleWidth = 1;

            string closeLabel = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("close_button") ?? "Close";
            Button closeBtn = CreateVanillaButton(closeBtnContainer.transform, closeLabel, minWidth: 175, minHeight: 47);
            closeBtn.gameObject.name = "CloseButton";
            panel.CloseButton = closeBtn;
            Text closeTxt = closeBtn.GetComponentInChildren<Text>();
            if (closeTxt != null) closeTxt.fontSize = 20;
            LayoutElement leClose = closeBtn.GetComponent<LayoutElement>();
            leClose.flexibleWidth = 0;
            leClose.minWidth = 175;
            leClose.minHeight = 47;
            leClose.preferredWidth = 175;
            leClose.preferredHeight = 47;
            RectTransform closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.sizeDelta = new Vector2(175, 47);

            Button infoBtn = CreateInfoButton(go.transform);
            infoBtn.onClick.AddListener(() =>
            {
                panel.ControlsPanel?.Show();
            });

            // Pass the same Norse font so the controls overlay title matches MY PINS.
            ControlsInfoPanel controlsPanel = CreateControlsInfoPanel(go.transform, font, norseBold ?? font);
            panel.ControlsPanel = controlsPanel;
            controlsPanel.InfoButton = infoBtn;

            // Controls overlay and info button must render above the scroll/list content.
            controlsPanel.transform.SetAsLastSibling();
            infoBtn.transform.SetAsLastSibling();

            DebugLogger.Log("My Pins panel created");
            return panel;
        }

        /// <summary>
        /// Creates the overlaid info button; ignoreLayout keeps it out of the panel layout.
        /// </summary>
        private static Button CreateInfoButton(Transform panelParent)
        {
            const float btnSize = 32f;

            Button btn = CreateVanillaButton(panelParent, "i",
                minWidth: (int)btnSize, minHeight: (int)btnSize);
            btn.gameObject.name = "InfoButton";

            Text txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.fontStyle = FontStyle.Italic;
                txt.fontSize  = 18;
            }

            LayoutElement le = btn.GetComponent<LayoutElement>();
            le.ignoreLayout   = true;
            le.preferredWidth  = btnSize;
            le.preferredHeight = btnSize;
            le.minWidth        = btnSize;
            le.minHeight       = btnSize;
            le.flexibleWidth   = 0;

            RectTransform btnRect = btn.GetComponent<RectTransform>();
            btnRect.anchorMin       = new Vector2(1f, 1f);
            btnRect.anchorMax       = new Vector2(1f, 1f);
            btnRect.pivot           = new Vector2(1f, 1f);
            btnRect.sizeDelta       = new Vector2(btnSize, btnSize);
            btnRect.anchoredPosition = new Vector2(-8f, -8f);

            return btn;
        }

        /// <summary>
        /// Creates one pooled My Pins row. Refresh code decides whether it is a pin, group, or sub-item.
        /// </summary>
        public static MyPinItemUI CreateMyPinItem(Transform parent, Font font)
        {
            GameObject go = new GameObject("MyPinItem", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);

            MyPinItemUI item = go.AddComponent<MyPinItemUI>();

            Image rowBg = go.AddComponent<Image>();
            rowBg.color = new Color(0, 0, 0, 0f);
            rowBg.raycastTarget = true;

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 10;
            hlg.padding = new RectOffset(6, 6, 4, 4);

            LayoutElement rowLe = go.AddComponent<LayoutElement>();
            rowLe.minHeight = 38;
            rowLe.flexibleWidth = 1;

            Button expandBtn = CreateSmallVanillaButton(go.transform, "\u25BA", size: 26);
            expandBtn.gameObject.name = "ExpandBtn";
            Text expandTxt = expandBtn.GetComponentInChildren<Text>();
            // Keep group expand/collapse glyphs visually stable.
            if (expandTxt != null) expandTxt.fontSize = 11;

            expandBtn.gameObject.SetActive(false);
            item.ExpandButton = expandBtn;
            item.ExpandButtonText = expandTxt;

            GameObject toggleObj = new GameObject("SelectToggle", typeof(RectTransform)) { layer = 5 };
            toggleObj.transform.SetParent(go.transform, false);

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            Image toggleBg = toggleObj.AddComponent<Image>();
            toggleBg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            toggle.targetGraphic = toggleBg;

            GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform)) { layer = 5 };
            checkObj.transform.SetParent(toggleObj.transform, false);
            Image checkImg = checkObj.AddComponent<Image>();
            checkImg.color = ValheimOrange;
            RectTransform checkRect = checkObj.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            toggle.graphic = checkImg;

            LayoutElement leTgl = toggleObj.AddComponent<LayoutElement>();
            leTgl.minWidth = 22; leTgl.minHeight = 22;
            leTgl.preferredWidth = 22; leTgl.preferredHeight = 22;
            toggleObj.SetActive(false);
            item.SelectToggle = toggle;

            GameObject iconRoot = new GameObject("IconRoot", typeof(RectTransform)) { layer = 5 };
            iconRoot.transform.SetParent(go.transform, false);
            item.IconRoot = iconRoot.transform;

            HorizontalLayoutGroup iconHlg = iconRoot.AddComponent<HorizontalLayoutGroup>();
            iconHlg.childControlHeight = false;
            iconHlg.childControlWidth = false;
            iconHlg.childForceExpandHeight = false;
            iconHlg.childForceExpandWidth = false;
            iconHlg.childAlignment = TextAnchor.MiddleCenter;
            iconHlg.spacing = 2;

            LayoutElement leIconRoot = iconRoot.AddComponent<LayoutElement>();
            leIconRoot.minWidth = 30; leIconRoot.minHeight = 30;
            leIconRoot.preferredWidth = 30; leIconRoot.preferredHeight = 30;

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform)) { layer = 5 };
            iconObj.transform.SetParent(iconRoot.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(28, 28);
            item.Icon = iconImg;

            GameObject nameObj = new GameObject("Name", typeof(RectTransform)) { layer = 5 };
            nameObj.transform.SetParent(go.transform, false);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.raycastTarget = false;
            nameText.font = font;
            nameText.fontSize = 15;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            item.NameText = nameText;

            LayoutElement leName = nameObj.AddComponent<LayoutElement>();
            leName.flexibleWidth = 1;
            leName.minWidth = 60;

            GameObject countObj = new GameObject("Count", typeof(RectTransform)) { layer = 5 };
            countObj.transform.SetParent(go.transform, false);
            Text countText = countObj.AddComponent<Text>();
            countText.raycastTarget = false;
            countText.font = font;
            countText.fontSize = 15;
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = ValheimOrange;
            item.CountText = countText;

            LayoutElement leCount = countObj.AddComponent<LayoutElement>();
            leCount.minWidth = 30;

            Button minusBtn = CreateSmallVanillaButton(go.transform, "-");
            item.MinusButton = minusBtn;

            Button plusBtn = CreateSmallVanillaButton(go.transform, "+");
            item.PlusButton = plusBtn;

            Button disbandBtn = CreateSmallVanillaButton(go.transform, "\u2298");
            disbandBtn.gameObject.SetActive(false);
            item.DisbandButton = disbandBtn;

            Button delBtn = CreateSmallVanillaButton(go.transform, "X");
            item.DeleteButton = delBtn;

            return item;
        }
    }
}
