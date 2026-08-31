using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public static partial class UIBuilder
    {
        private static ControlsInfoPanel CreateControlsInfoPanel(Transform panelParent, Font font, Font titleFont)
        {
            GameObject go = new GameObject("ControlsInfoPanel", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(panelParent, false);

            LayoutElement lePanel = go.AddComponent<LayoutElement>();
            lePanel.ignoreLayout = true;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // Slightly extend beyond My Pins to give the controls content room.
            // Kept generous on the horizontal axis so long translated labels and
            // instruction lines do not wrap or clip.
            rt.offsetMin = new Vector2(-80, -125); // 80px wider left, 125px taller bottom
            rt.offsetMax = new Vector2(80, 20);    // 80px wider right, 20px taller top

            Image bg = go.AddComponent<Image>();
            bg.raycastTarget = true;
            if (TryGetTrophiesPanelBackground(out Sprite trophySprite, out Material trophyMaterial))
            {
                bg.sprite   = trophySprite;
                bg.material = trophyMaterial;
                bg.type     = trophySprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                bg.color    = Color.white;
            }
            else
            {
                Sprite uiBg = GetBackgroundSprite();
                if (uiBg != null) { bg.sprite = uiBg; bg.type = Image.Type.Sliced; }
                bg.color = new Color(0f, 0f, 0f, 0.92f);
            }

            ControlsInfoPanel panel = go.AddComponent<ControlsInfoPanel>();

            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight     = true;
            vlg.childControlWidth      = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = true;
            vlg.spacing = 6;
            vlg.padding = new RectOffset(20, 20, 14, 14);

            // Match the MY PINS title style.
            GameObject titleGo = new GameObject("Title", typeof(RectTransform)) { layer = 5 };
            titleGo.transform.SetParent(go.transform, false);
            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("controls_title") ?? "CONTROLS";
            titleTxt.font      = titleFont;
            titleTxt.fontSize  = 28;
            titleTxt.fontStyle = (titleFont != font) ? FontStyle.Normal : FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color     = new Color(1f, 0.718f, 0.357f, 1f); // #ffb75b — same as MY PINS
            titleTxt.raycastTarget = false;
            Outline titleOutline = titleGo.AddComponent<Outline>();
            titleOutline.effectColor    = Color.black;
            titleOutline.effectDistance = new Vector2(2f, -2f);
            LayoutElement leTitle = titleGo.AddComponent<LayoutElement>();
            leTitle.minHeight    = 36;
            leTitle.flexibleWidth = 1;

            // Simple VLG container for HOW TO USE + KEY BINDINGS rows.
            // No ScrollRect needed; the panel is tall enough for all content.
            GameObject rowsGo = new GameObject("Rows", typeof(RectTransform)) { layer = 5 };
            rowsGo.transform.SetParent(go.transform, false);
            VerticalLayoutGroup rowsVlg = rowsGo.AddComponent<VerticalLayoutGroup>();
            rowsVlg.childControlHeight     = true;
            rowsVlg.childControlWidth      = true;
            rowsVlg.childForceExpandHeight = false;
            rowsVlg.childForceExpandWidth  = true;
            rowsVlg.spacing = 4;
            LayoutElement leRows = rowsGo.AddComponent<LayoutElement>();
            leRows.flexibleWidth  = 1;
            leRows.flexibleHeight = 1;
            panel.RowsParent = rowsGo.transform;

            GameObject noteGo = new GameObject("Note", typeof(RectTransform)) { layer = 5 };
            noteGo.transform.SetParent(go.transform, false);
            Text noteTxt = noteGo.AddComponent<Text>();
            noteTxt.text = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("controls_config_note_single")
                           ?? "Controls can be changed in the config file.";
            noteTxt.font              = font;
            noteTxt.fontSize          = 14;
            noteTxt.fontStyle         = FontStyle.Italic;
            noteTxt.alignment         = TextAnchor.MiddleCenter;
            noteTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            noteTxt.color             = new Color(0.70f, 0.67f, 0.58f, 1f);
            noteTxt.raycastTarget     = false;
            LayoutElement leNote = noteGo.AddComponent<LayoutElement>();
            leNote.flexibleWidth = 1;
            leNote.minHeight     = 22;

            GameObject closeCtr = new GameObject("CloseButtonContainer", typeof(RectTransform)) { layer = 5 };
            closeCtr.transform.SetParent(go.transform, false);
            HorizontalLayoutGroup closeHlg = closeCtr.AddComponent<HorizontalLayoutGroup>();
            closeHlg.childControlHeight    = true;
            closeHlg.childControlWidth     = false;
            closeHlg.childForceExpandWidth = false;
            closeHlg.childAlignment        = TextAnchor.MiddleCenter;
            LayoutElement leCloseCtr = closeCtr.AddComponent<LayoutElement>();
            leCloseCtr.minHeight      = 47;
            leCloseCtr.preferredHeight = 47;
            leCloseCtr.flexibleHeight  = 0;
            leCloseCtr.flexibleWidth   = 1;

            string closeLbl = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("close_button") ?? "Close";
            Button closeBtn = CreateVanillaButton(closeCtr.transform, closeLbl,
                minWidth: 175, minHeight: 47);
            closeBtn.gameObject.name = "ControlsCloseButton";
            Text closeTxt2 = closeBtn.GetComponentInChildren<Text>();
            if (closeTxt2 != null) closeTxt2.fontSize = 20;
            LayoutElement leClose = closeBtn.GetComponent<LayoutElement>();
            leClose.flexibleWidth   = 0;
            leClose.minWidth        = 175;
            leClose.preferredWidth  = 175;
            leClose.minHeight       = 47;
            leClose.preferredHeight = 47;
            // Explicitly set RectTransform width to match MY PINS close button exactly.
            // Required because the container uses childControlWidth=false, so the vanilla
            // button's own RectTransform width is what gets rendered, not the LayoutElement.
            closeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(175, 47);
            panel.CloseButton = closeBtn;

            panel.Initialize(font, GetBackgroundSprite());

            go.SetActive(false);
            DebugLogger.Log("ControlsInfoPanel created");
            return panel;
        }

        /// <summary>
        /// Creates the group name input dialog for naming a new pin group.
        /// </summary>
        public static GroupNameDialog CreateGroupNameDialog(Transform parent, Font font)
        {
            DebugLogger.Log("Creating Group Name Dialog");

            GameObject overlayObj = new GameObject("GroupNameDialogOverlay", typeof(RectTransform)) { layer = 5 };
            overlayObj.transform.SetParent(parent, false);

            GroupNameDialog dialog = overlayObj.AddComponent<GroupNameDialog>();

            RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayBg = overlayObj.AddComponent<Image>();
            overlayBg.color = new Color(0, 0, 0, 0.5f);
            overlayBg.raycastTarget = true; // Block clicks behind
            dialog.OverlayBg = overlayBg;

            GameObject dialogBox = new GameObject("DialogBox", typeof(RectTransform)) { layer = 5 };
            dialogBox.transform.SetParent(overlayObj.transform, false);
            dialog.DialogRect = dialogBox.GetComponent<RectTransform>();

            dialog.DialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialog.DialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialog.DialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialog.DialogRect.sizeDelta = new Vector2(320, 160);

            Image dialogBg = dialogBox.AddComponent<Image>();
            if (TryGetTrophiesPanelBackground(out Sprite dSprite, out Material dMaterial))
            {
                dialogBg.sprite = dSprite;
                dialogBg.material = dMaterial;
                dialogBg.type = (dSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
                dialogBg.color = Color.white;
            }
            else
            {
                Sprite fallback = GetBackgroundSprite();
                dialogBg.sprite = fallback;
                dialogBg.type = Image.Type.Simple;
                dialogBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            }
            dialogBg.raycastTarget = true;
            dialog.BgImage = dialogBg;

            VerticalLayoutGroup dlgVlg = dialogBox.AddComponent<VerticalLayoutGroup>();
            dlgVlg.childControlHeight = true;
            dlgVlg.childControlWidth = true;
            dlgVlg.childForceExpandHeight = false;
            dlgVlg.spacing = 10;
            dlgVlg.padding = new RectOffset(20, 20, 16, 16);

            GameObject promptObj = new GameObject("Prompt", typeof(RectTransform)) { layer = 5 };
            promptObj.transform.SetParent(dialogBox.transform, false);
            Text promptText = promptObj.AddComponent<Text>();
            promptText.raycastTarget = false;
            promptText.font = font;
            promptText.fontSize = 16;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = ValheimOrange;
            string prompt = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_name_prompt") ?? "Enter group name:";
            promptText.text = prompt;

            LayoutElement lePrompt = promptObj.AddComponent<LayoutElement>();
            lePrompt.minHeight = 24;
            lePrompt.flexibleWidth = 1;

            GameObject inputObj = new GameObject("InputField", typeof(RectTransform)) { layer = 5 };
            inputObj.transform.SetParent(dialogBox.transform, false);

            Image inputBg = inputObj.AddComponent<Image>();
            if (TryGetVanillaInputFieldStyle(out Sprite inputSprite, out Material inputMaterial, out Color inputColor, out Selectable inputSource))
            {
                inputBg.sprite = inputSprite;
                inputBg.material = inputMaterial;
                inputBg.type = (inputSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
                inputBg.color = inputColor;
            }
            else
            {
                inputBg.color = new Color(0.15f, 0.12f, 0.08f, 0.9f);
            }

            InputField inputField = inputObj.AddComponent<InputField>();
            inputField.characterLimit = 30;

            // A Selectable added at runtime has no targetGraphic, so it never shows hover or
            // press feedback. Point it at the background, then copy the vanilla field's states.
            inputField.targetGraphic = inputBg;
            if (inputSource != null)
            {
                inputField.transition = inputSource.transition;
                inputField.colors = inputSource.colors;
                inputField.spriteState = inputSource.spriteState;
            }

            GameObject inputTextObj = new GameObject("Text", typeof(RectTransform)) { layer = 5 };
            inputTextObj.transform.SetParent(inputObj.transform, false);
            Text inputText = inputTextObj.AddComponent<Text>();
            inputText.font = font;
            inputText.fontSize = 16;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            inputText.supportRichText = false;

            RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = new Vector2(8, 2);
            inputTextRect.offsetMax = new Vector2(-8, -2);

            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform)) { layer = 5 };
            placeholderObj.transform.SetParent(inputObj.transform, false);
            Text placeholder = placeholderObj.AddComponent<Text>();
            placeholder.font = font;
            placeholder.fontSize = 16;
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            placeholder.text = "...";

            RectTransform phRect = placeholderObj.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(8, 2);
            phRect.offsetMax = new Vector2(-8, -2);

            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            dialog.NameInput = inputField;

            LayoutElement leInput = inputObj.AddComponent<LayoutElement>();
            leInput.minHeight = 32;
            leInput.flexibleWidth = 1;

            GameObject btnRow = new GameObject("ButtonRow", typeof(RectTransform)) { layer = 5 };
            btnRow.transform.SetParent(dialogBox.transform, false);

            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.childControlHeight = true;
            btnHlg.childControlWidth = true;
            btnHlg.childForceExpandWidth = true;
            btnHlg.spacing = 12;

            LayoutElement leBtnRow = btnRow.AddComponent<LayoutElement>();
            leBtnRow.minHeight = 35;
            leBtnRow.flexibleWidth = 1;

            string cancelLabel = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_cancel") ?? "Cancel";
            Button cancelBtn = CreateVanillaButton(btnRow.transform, cancelLabel);
            dialog.CancelButton = cancelBtn;

            string confirmLabel = RecipePinnerPlugin.Instance?.LocalizationMgr?.GetText("group_confirm") ?? "Confirm";
            Button confirmBtn = CreateVanillaButton(btnRow.transform, confirmLabel);
            dialog.ConfirmButton = confirmBtn;

            DebugLogger.Log("Group Name Dialog created");
            return dialog;
        }

        /// <summary>
        /// Creates a simple confirm/cancel dialog for delete confirmations.
        /// </summary>
        public static ConfirmDialog CreateConfirmDialog(Transform parent, Font font)
        {
            DebugLogger.Log("Creating Confirm Dialog");

            GameObject overlayObj = new GameObject("ConfirmDialogOverlay", typeof(RectTransform)) { layer = 5 };
            overlayObj.transform.SetParent(parent, false);

            ConfirmDialog dialog = overlayObj.AddComponent<ConfirmDialog>();

            RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayBg = overlayObj.AddComponent<Image>();
            overlayBg.color = new Color(0, 0, 0, 0.5f);
            overlayBg.raycastTarget = true;
            dialog.OverlayBg = overlayBg;

            GameObject dialogBox = new GameObject("DialogBox", typeof(RectTransform)) { layer = 5 };
            dialogBox.transform.SetParent(overlayObj.transform, false);
            dialog.DialogRect = dialogBox.GetComponent<RectTransform>();

            dialog.DialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialog.DialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialog.DialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialog.DialogRect.sizeDelta = new Vector2(320, 140);

            Image dialogBg = dialogBox.AddComponent<Image>();
            if (TryGetTrophiesPanelBackground(out Sprite dSprite, out Material dMaterial))
            {
                dialogBg.sprite = dSprite;
                dialogBg.material = dMaterial;
                dialogBg.type = (dSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
                dialogBg.color = Color.white;
            }
            else
            {
                Sprite fallback = GetBackgroundSprite();
                dialogBg.sprite = fallback;
                dialogBg.type = Image.Type.Simple;
                dialogBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            }
            dialogBg.raycastTarget = true;
            dialog.BgImage = dialogBg;

            VerticalLayoutGroup vlg = dialogBox.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 12;
            vlg.padding = new RectOffset(20, 20, 20, 16);
            vlg.childAlignment = TextAnchor.MiddleCenter;

            GameObject msgObj = new GameObject("Message", typeof(RectTransform)) { layer = 5 };
            msgObj.transform.SetParent(dialogBox.transform, false);
            Text msgText = msgObj.AddComponent<Text>();
            msgText.raycastTarget = false;
            msgText.font = font;
            msgText.fontSize = 16;
            msgText.alignment = TextAnchor.MiddleCenter;
            msgText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            msgText.horizontalOverflow = HorizontalWrapMode.Wrap;
            msgText.verticalOverflow = VerticalWrapMode.Overflow;
            dialog.MessageText = msgText;

            LayoutElement leMsgText = msgObj.AddComponent<LayoutElement>();
            leMsgText.minHeight = 40;
            leMsgText.flexibleWidth = 1;

            GameObject btnRow = new GameObject("ButtonRow", typeof(RectTransform)) { layer = 5 };
            btnRow.transform.SetParent(dialogBox.transform, false);

            HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = true;
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            LayoutElement leBtnRow = btnRow.AddComponent<LayoutElement>();
            leBtnRow.minHeight = 32;

            var locMgr = RecipePinnerPlugin.Instance?.LocalizationMgr;
            string cancelText = locMgr?.GetText("cancel_button") ?? "Cancel";
            string confirmText = locMgr?.GetText("confirm_button") ?? "Confirm";
            dialog.CancelButton = CreateVanillaButton(btnRow.transform, cancelText, minHeight: 40);

            dialog.ConfirmButton = CreateVanillaButton(btnRow.transform, confirmText, minHeight: 40);

            overlayObj.SetActive(false);
            DebugLogger.Log("Confirm Dialog created");
            return dialog;
        }
    }
}
