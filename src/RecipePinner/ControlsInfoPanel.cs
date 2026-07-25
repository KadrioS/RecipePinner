using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    /// <summary>
    /// Overlay panel that displays all mod hotkeys, read live from BepInEx config.
    /// Shown when the user clicks the "i" button on the My Pins panel header.
    /// </summary>
    public class ControlsInfoPanel : MonoBehaviour
    {
        public static ControlsInfoPanel Instance { get; private set; }
        public static bool IsOpen { get; private set; }

        public Button CloseButton;
        public Transform RowsParent;
        /// <summary>The "i" button on the panel header, hidden while controls are visible.</summary>
        public Button InfoButton;

        private Font _font;
        private Sprite _badgeSprite;
        private bool _listenersWired;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            // Fired when parent (My Pins panel) is deactivated via SetActive(false).
            // Ensures IsOpen is cleared even if Hide() was not called explicitly.
            IsOpen = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            IsOpen = false;
        }

        /// <summary>Stores references needed to build rows at runtime.</summary>
        public void Initialize(Font font, Sprite badgeSprite)
        {
            _font = font;
            _badgeSprite = badgeSprite;
            WireListeners();
        }

        /// <summary>Rebuilds rows from current config and shows the panel.</summary>
        public void Show()
        {
            // Activate BEFORE building content so Unity layout can run on active objects.
            gameObject.SetActive(true);

            RefreshContent();

            // ContentSizeFitter only resizes during layout passes on active objects.
            // ForceRebuildLayoutImmediate ensures the scroll content has the correct
            // height in the same frame before anything is rendered.
            if (RowsParent != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
                    GetComponent<RectTransform>());

            IsOpen = true;
            if (InfoButton != null) InfoButton.gameObject.SetActive(false);
            DebugLogger.Log("ControlsInfoPanel: opened");
        }

        /// <summary>Hides the panel without destroying it.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            IsOpen = false;
            if (InfoButton != null) InfoButton.gameObject.SetActive(true);
            DebugLogger.Log("ControlsInfoPanel: closed");
        }

        private void WireListeners()
        {
            if (_listenersWired) return;
            if (CloseButton != null)
            {
                CloseButton.onClick.RemoveAllListeners();
                CloseButton.onClick.AddListener(Hide);
                CloseButton.onClick.AddListener(UIBuilder.PlayButtonSFX);
            }
            _listenersWired = true;
        }

        private void RefreshContent()
        {
            if (RowsParent == null) return;

            foreach (Transform child in RowsParent)
                Destroy(child.gameObject);

            var loc = RecipePinnerPlugin.Instance?.LocalizationMgr;

            string pinKey   = FormatKey(RecipePinnerPlugin.HotkeyPin?.Value              ?? KeyCode.Mouse2);
            string unpinKey = FormatKey(RecipePinnerPlugin.HotkeyUnpin?.Value            ?? KeyCode.LeftShift);
            string hudKey   = FormatKey(RecipePinnerPlugin.HotkeyToggleVisibility?.Value ?? KeyCode.F7);
            string listKey  = FormatKey(RecipePinnerPlugin.HotkeyGatheringList?.Value    ?? KeyCode.F8);
            string pageKey  = FormatKey(RecipePinnerPlugin.HotkeyPageSwitch?.Value       ?? KeyCode.LeftAlt);
            string clearKey = FormatKey(RecipePinnerPlugin.HotkeyClearAll?.Value         ?? KeyCode.P);

            CreateSectionHeader(loc?.GetText("howto_header") ?? "HOW TO USE");

            CreateInstructionRow(string.Format(
                loc?.GetText("howto_pin") ?? "Hover over a recipe in the crafting menu and press [{0}] to pin it.",
                pinKey));
            CreateInstructionRow(string.Format(
                loc?.GetText("howto_unpin") ?? "Hold [{0}] and press [{1}] to unpin a recipe.",
                unpinKey, pinKey));
            CreateInstructionRow(string.Format(
                loc?.GetText("howto_toggle_hud") ?? "Press [{0}] to show or hide the pinned recipe overlay.",
                hudKey));
            CreateInstructionRow(string.Format(
                loc?.GetText("howto_gathering") ?? "Press [{0}] to open or close the gathering list.",
                listKey));
            CreateInstructionRow(string.Format(
                loc?.GetText("howto_next_page") ?? "Press [{0}] to cycle through HUD pages.",
                pageKey));
            CreateInstructionRow(string.Format(
                loc?.GetText("howto_clear_all") ?? "Press [{0}] to remove all pinned recipes.",
                clearKey));

            CreateSectionHeader(loc?.GetText("keybindings_header") ?? "KEY BINDINGS");

            CreateRow(loc?.GetText("ctrl_pin")        ?? "Pin Recipe",              pinKey);
            CreateRow(loc?.GetText("ctrl_unpin")      ?? "Unpin  (hold + Pin key)", unpinKey);
            CreateRow(loc?.GetText("ctrl_toggle_hud") ?? "Toggle HUD Visibility",   hudKey);
            CreateRow(loc?.GetText("ctrl_gathering")  ?? "Toggle Gathering List",   listKey);
            CreateRow(loc?.GetText("ctrl_next_page")  ?? "Next HUD Page",           pageKey);
            CreateRow(loc?.GetText("ctrl_clear_all")  ?? "Clear All Pins",          clearKey);
        }

        /// <summary>Adds a gold section header row to the scroll content.</summary>
        private void CreateSectionHeader(string text)
        {
            if (_font == null) return;

            GameObject hdrGo = new GameObject("SectionHeader", typeof(RectTransform)) { layer = 5 };
            hdrGo.transform.SetParent(RowsParent, false);

            Text hdrTxt = hdrGo.AddComponent<Text>();
            hdrTxt.text          = text;
            hdrTxt.font          = _font;
            hdrTxt.fontSize      = 15;
            hdrTxt.fontStyle     = FontStyle.Bold;
            hdrTxt.alignment     = TextAnchor.MiddleLeft;
            hdrTxt.color         = new Color(1f, 0.718f, 0.357f, 1f); // gold — same as title
            hdrTxt.raycastTarget = false;

            LayoutElement le = hdrGo.AddComponent<LayoutElement>();
            le.flexibleWidth   = 1;
            le.minHeight       = 22;
            le.preferredHeight = 22;
        }

        /// <summary>Adds a wrapped instruction text row to the scroll content.</summary>
        private void CreateInstructionRow(string text)
        {
            if (_font == null) return;

            GameObject rowGo = new GameObject("InstructionRow", typeof(RectTransform)) { layer = 5 };
            rowGo.transform.SetParent(RowsParent, false);

            Text rowTxt = rowGo.AddComponent<Text>();
            rowTxt.text               = "\u2022 " + text; // bullet
            rowTxt.font               = _font;
            rowTxt.fontSize           = 14;
            rowTxt.fontStyle          = FontStyle.Normal;
            rowTxt.alignment          = TextAnchor.UpperLeft;
            rowTxt.color              = new Color(0.85f, 0.82f, 0.70f, 1f);
            rowTxt.raycastTarget      = false;
            rowTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            rowTxt.verticalOverflow   = VerticalWrapMode.Overflow;

            LayoutElement le = rowGo.AddComponent<LayoutElement>();
            le.flexibleWidth   = 1;
            le.minHeight       = 18;
            le.preferredHeight = 42; // 2 lines at fontSize 14 ≈ 34px + 8px breathing room
        }

        private void CreateRow(string label, string keyText)
        {
            if (_font == null) return;

            GameObject rowGo = new GameObject("ControlRow", typeof(RectTransform)) { layer = 5 };
            rowGo.transform.SetParent(RowsParent, false);

            HorizontalLayoutGroup hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight     = true;
            hlg.childControlWidth      = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 8;
            hlg.padding = new RectOffset(0, 0, 2, 2);

            LayoutElement leRow = rowGo.AddComponent<LayoutElement>();
            leRow.flexibleWidth = 1;
            leRow.minHeight     = 26;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform)) { layer = 5 };
            labelGo.transform.SetParent(rowGo.transform, false);

            Text labelTxt = labelGo.AddComponent<Text>();
            labelTxt.text         = label;
            labelTxt.font         = _font;
            labelTxt.fontSize     = 15;
            labelTxt.color        = new Color(0.88f, 0.84f, 0.72f, 1f);
            labelTxt.alignment    = TextAnchor.MiddleLeft;
            labelTxt.raycastTarget = false;

            LayoutElement leLabel = labelGo.AddComponent<LayoutElement>();
            leLabel.flexibleWidth = 1;

            GameObject badgeGo = new GameObject("KeyBadge", typeof(RectTransform)) { layer = 5 };
            badgeGo.transform.SetParent(rowGo.transform, false);

            Image badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.color         = new Color(0.10f, 0.08f, 0.04f, 0.90f);
            badgeImg.raycastTarget = false;
            if (_badgeSprite != null)
            {
                badgeImg.sprite = _badgeSprite;
                badgeImg.type   = _badgeSprite.border != Vector4.zero
                    ? Image.Type.Sliced : Image.Type.Simple;
            }

            LayoutElement leBadge = badgeGo.AddComponent<LayoutElement>();
            leBadge.preferredWidth = 130;
            leBadge.flexibleWidth  = 0;
            leBadge.minHeight      = 22;

            GameObject keyTextGo = new GameObject("KeyText", typeof(RectTransform)) { layer = 5 };
            keyTextGo.transform.SetParent(badgeGo.transform, false);

            RectTransform keyTR = keyTextGo.GetComponent<RectTransform>();
            keyTR.anchorMin  = Vector2.zero;
            keyTR.anchorMax  = Vector2.one;
            keyTR.offsetMin  = new Vector2(4f,  2f);
            keyTR.offsetMax  = new Vector2(-4f, -2f);

            Text keyTxt = keyTextGo.AddComponent<Text>();
            keyTxt.text          = keyText;
            keyTxt.font          = _font;
            keyTxt.fontSize      = 13;
            keyTxt.fontStyle     = FontStyle.Bold;
            keyTxt.color         = new Color(1f, 0.78f, 0.38f, 1f);
            keyTxt.alignment     = TextAnchor.MiddleCenter;
            keyTxt.raycastTarget = false;
        }

        public static string FormatKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Mouse0:       return "Left Mouse";
                case KeyCode.Mouse1:       return "Right Mouse";
                case KeyCode.Mouse2:       return "Middle Mouse";
                case KeyCode.Mouse3:       return "Mouse 4";
                case KeyCode.Mouse4:       return "Mouse 5";
                case KeyCode.LeftShift:    return "Left Shift";
                case KeyCode.RightShift:   return "Right Shift";
                case KeyCode.LeftAlt:      return "Left Alt";
                case KeyCode.RightAlt:     return "Right Alt";
                case KeyCode.LeftControl:  return "Left Ctrl";
                case KeyCode.RightControl: return "Right Ctrl";
                case KeyCode.Return:       return "Enter";
                case KeyCode.KeypadEnter:  return "Numpad Enter";
                case KeyCode.Space:        return "Space";
                case KeyCode.Backspace:    return "Backspace";
                case KeyCode.Delete:       return "Delete";
                case KeyCode.Tab:          return "Tab";
                case KeyCode.Escape:       return "Escape";
                case KeyCode.UpArrow:      return "Up Arrow";
                case KeyCode.DownArrow:    return "Down Arrow";
                case KeyCode.LeftArrow:    return "Left Arrow";
                case KeyCode.RightArrow:   return "Right Arrow";
                case KeyCode.Keypad0:      return "Numpad 0";
                case KeyCode.Keypad1:      return "Numpad 1";
                case KeyCode.Keypad2:      return "Numpad 2";
                case KeyCode.Keypad3:      return "Numpad 3";
                case KeyCode.Keypad4:      return "Numpad 4";
                case KeyCode.Keypad5:      return "Numpad 5";
                case KeyCode.Keypad6:      return "Numpad 6";
                case KeyCode.Keypad7:      return "Numpad 7";
                case KeyCode.Keypad8:      return "Numpad 8";
                case KeyCode.Keypad9:      return "Numpad 9";
                case KeyCode.PageUp:       return "Page Up";
                case KeyCode.PageDown:     return "Page Down";
                case KeyCode.Home:         return "Home";
                case KeyCode.End:          return "End";
                case KeyCode.Insert:       return "Insert";
                default:                   return key.ToString();
            }
        }
    }
}
