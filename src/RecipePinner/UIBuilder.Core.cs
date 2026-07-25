using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public static partial class UIBuilder
    {
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

        /// <summary>Returns the cached background UI sprite — publicly accessible for other UI components.</summary>
        public static Sprite GetUISpritePublic() => GetBackgroundSprite();

        /// <summary>
        /// Plays Valheim's native button click sound; the prefab is cached after the first lookup.
        /// </summary>
        public static void PlayButtonSFX()
        {
            if (_cachedButtonSfxPrefab == null)
            {
                var gui = InventoryGui.instance;
                if (gui != null && gui.m_setActiveGroupEffects?.m_effectPrefabs?.Length > 0)
                {
                    _cachedButtonSfxPrefab = gui.m_setActiveGroupEffects.m_effectPrefabs[0].m_prefab;
                    if (_cachedButtonSfxPrefab != null)
                        DebugLogger.Log($"Cached button SFX prefab: {_cachedButtonSfxPrefab.name}");
                }
            }

            if (_cachedButtonSfxPrefab != null)
            {
                var player = Player.m_localPlayer;
                Vector3 pos = player != null ? player.transform.position : Vector3.zero;
                Object.Instantiate(_cachedButtonSfxPrefab, pos, Quaternion.identity);
            }
        }

        private static void CacheVanillaButtonStyle()
        {
            if (_vanillaBtnCached) return;
            _vanillaBtnCached = true;

            if (InventoryGui.instance == null) return;

            var field = HarmonyLib.AccessTools.Field(typeof(InventoryGui), "m_craftButton");
            if (field == null) { DebugLogger.Warning("Vanilla button style: m_craftButton field not found"); return; }

            Button craftBtn = field.GetValue(InventoryGui.instance) as Button;
            if (craftBtn == null) { DebugLogger.Warning("Vanilla button style: m_craftButton value is null"); return; }

            Image img = craftBtn.GetComponent<Image>();
            if (img != null)
            {
                _vanillaBtnSprite = img.sprite;
                _vanillaBtnMaterial = img.material;
            }

            _vanillaBtnColors = craftBtn.colors;

            Text txt = craftBtn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                _vanillaBtnFont = txt.font;
                _vanillaBtnFontSize = txt.fontSize;
                _vanillaBtnFontStyle = txt.fontStyle;
                _vanillaBtnTextColor = txt.color;
            }

            Outline outline = craftBtn.GetComponentInChildren<Outline>();
            if (outline != null)
            {
                _vanillaBtnHasOutline = true;
                _vanillaBtnOutlineColor = outline.effectColor;
                _vanillaBtnOutlineDistance = outline.effectDistance;
            }

            _vanillaBtnTransition = craftBtn.transition;
            _vanillaBtnSpriteState = craftBtn.spriteState;
            _vanillaBtnAnimTriggers = craftBtn.animationTriggers;

            DebugLogger.Log($"Vanilla button style cached (sprite={_vanillaBtnSprite?.name}, font={_vanillaBtnFont?.name}, transition={_vanillaBtnTransition})");
        }

        /// <summary>
        /// Applies Valheim's vanilla craft button look to a given button.
        /// Creates Image, Button, Text child with matching style.
        /// Returns the created Button component.
        /// </summary>
        public static Button CreateVanillaButton(Transform parent, string label, float minWidth = -1, float minHeight = 35)
        {
            CacheVanillaButtonStyle();

            GameObject go = new GameObject($"Btn_{label}", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();
            if (_vanillaBtnSprite != null)
            {
                bg.sprite = _vanillaBtnSprite;
                bg.material = _vanillaBtnMaterial;
                bg.type = (_vanillaBtnSprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
                bg.color = Color.white;
            }
            else
            {
                Sprite fallback = GetBackgroundSprite();
                bg.sprite = fallback;
                if (fallback != null && fallback.border != Vector4.zero) bg.type = Image.Type.Sliced;
                bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            }
            bg.raycastTarget = true;

            Button btn = go.AddComponent<Button>();
            if (_vanillaBtnSprite != null)
            {
                btn.colors = _vanillaBtnColors;
                // Preserve Valheim's hover/click behavior when the craft button exposes it.
                btn.transition = _vanillaBtnTransition;
                if (_vanillaBtnTransition == Selectable.Transition.SpriteSwap)
                    btn.spriteState = _vanillaBtnSpriteState;
                if (_vanillaBtnTransition == Selectable.Transition.Animation && _vanillaBtnAnimTriggers != null)
                    btn.animationTriggers = _vanillaBtnAnimTriggers;
            }
            else
            {
                ColorBlock colors = btn.colors;
                colors.normalColor = new Color(1f, 0.77f, 0.31f, 0.9f);
                colors.highlightedColor = new Color(1f, 0.85f, 0.5f, 1f);
                colors.pressedColor = new Color(0.8f, 0.6f, 0.2f, 1f);
                btn.colors = colors;
            }
            btn.targetGraphic = bg;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = minHeight;
            if (minWidth > 0) le.minWidth = minWidth;
            le.flexibleWidth = 1;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform)) { layer = 5 };
            txtObj.transform.SetParent(go.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.raycastTarget = false;
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;

            if (_vanillaBtnFont != null)
            {
                txt.font = _vanillaBtnFont;
                txt.fontSize = _vanillaBtnFontSize;
                txt.fontStyle = _vanillaBtnFontStyle;
            }
            else
            {
                Font gameFont = null;
                foreach (Font f in Resources.FindObjectsOfTypeAll<Font>())
                {
                    if (f != null && f.name == "AveriaSerifLibre-Bold") { gameFont = f; break; }
                }
                txt.font = gameFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                txt.fontSize = 16;
                txt.fontStyle = FontStyle.Bold;
            }
            txt.color = ValheimOrange;

            // Always outline mod button text to match Valheim's button style.
            Outline outlineComp = txtObj.AddComponent<Outline>();
            if (_vanillaBtnHasOutline)
            {
                outlineComp.effectColor = _vanillaBtnOutlineColor;
                outlineComp.effectDistance = _vanillaBtnOutlineDistance;
            }
            else
            {
                outlineComp.effectColor = Color.black;
                outlineComp.effectDistance = new Vector2(1f, -1f);
            }

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            btn.onClick.AddListener(PlayButtonSFX);

            return btn;
        }

        /// <summary>
        /// Creates a small square vanilla-styled button (for +, -, X, etc.)
        /// </summary>
        public static Button CreateSmallVanillaButton(Transform parent, string label, float size = 32)
        {
            Button btn = CreateVanillaButton(parent, label, minWidth: -1, minHeight: size);

            LayoutElement le = btn.GetComponent<LayoutElement>();
            le.minWidth = size;
            le.minHeight = size;
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.flexibleWidth = 0;

            return btn;
        }

        public static GameObject CreateModalOverlay(Transform parent, System.Action onClick)
        {
            DebugLogger.Log("Creating modal overlay");

            GameObject go = new GameObject("ModalOverlay", typeof(RectTransform)) { layer = 5 };
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Blocks raycasts behind modal panels.
            Image img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.35f);
            img.raycastTarget = true;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Keep the overlay visually static; it only acts as a click catcher.
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            btn.colors = colors;

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            go.SetActive(false);

            DebugLogger.Log("Modal overlay created");
            return go;
        }
    }
}
