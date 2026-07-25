using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public static partial class UIBuilder
    {
        private static Color ValheimOrange => RecipePinnerPlugin.ButtonTextColor?.Value ?? new Color(1f, 0.631f, 0.239f, 1f); // #ffa13d
        private static Color DividerColor = new Color(1f, 1f, 1f, 0.10f);

        private static Sprite _cachedUiSprite;
        private static bool _spriteSearchDone = false;
        private static Font _cachedNorseFont;

        private static Sprite _cachedPinIcon;
        private static bool _pinIconLoadAttempted = false;

        /// <summary>
        /// Loads the embedded pin icon once; returns null if the resource is missing or invalid.
        /// </summary>
        private static Sprite LoadPinIconSprite()
        {
            if (_pinIconLoadAttempted) return _cachedPinIcon;
            _pinIconLoadAttempted = true;

            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("RecipePinner.pinIcon.png"))
                {
                    if (stream == null)
                    {
                        DebugLogger.Warning("pin.png embedded resource not found");
                        return null;
                    }

                    byte[] data;
                    using (var buffer = new System.IO.MemoryStream())
                    {
                        stream.CopyTo(buffer);
                        data = buffer.ToArray();
                    }

                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Bilinear;

                    // Avoid a compile-time ImageConversionModule dependency; that path has
                    // ReadOnlySpan<T> issues with the current .NET 4.7.2 toolchain.
                    bool loaded = false;

                    var loadImageDirect = typeof(Texture2D).GetMethod(
                        "LoadImage",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                        null, new[] { typeof(byte[]) }, null);

                    if (loadImageDirect != null)
                    {
                        loaded = (bool)(loadImageDirect.Invoke(tex, new object[] { data }) ?? false);
                    }
                    else
                    {
                        var convType = System.Type.GetType(
                            "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                        if (convType != null)
                        {
                            var staticLoad = convType.GetMethod("LoadImage",
                                new[] { typeof(Texture2D), typeof(byte[]) });
                            if (staticLoad != null)
                                loaded = (bool)(staticLoad.Invoke(null, new object[] { tex, data }) ?? false);
                        }
                    }

                    if (!loaded)
                    {
                        DebugLogger.Warning("pin.png: LoadImage failed (reflection)");
                        return null;
                    }

                    _cachedPinIcon = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f);

                    DebugLogger.Log($"pin.png loaded: {tex.width}x{tex.height}");
                    return _cachedPinIcon;
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.Warning($"pin.png load error: {ex.Message}");
                return null;
            }
        }

        private static bool _vanillaBtnCached = false;
        private static Sprite _vanillaBtnSprite;
        private static Material _vanillaBtnMaterial;
        private static ColorBlock _vanillaBtnColors;
        private static Font _vanillaBtnFont;
        private static int _vanillaBtnFontSize;
        private static FontStyle _vanillaBtnFontStyle;
        private static Color _vanillaBtnTextColor;
        private static bool _vanillaBtnHasOutline;
        private static Color _vanillaBtnOutlineColor;
        private static Vector2 _vanillaBtnOutlineDistance;
        private static Selectable.Transition _vanillaBtnTransition;
        private static SpriteState _vanillaBtnSpriteState;
        private static AnimationTriggers _vanillaBtnAnimTriggers;

        private static GameObject _cachedButtonSfxPrefab;

        /// <summary>
        /// Reuses the trophies panel background so custom panels match Valheim's native UI.
        /// </summary>
        private static bool TryGetTrophiesPanelBackground(out Sprite sprite, out Material material)
        {
            sprite = null;
            material = null;

            if (InventoryGui.instance == null || InventoryGui.instance.m_trophiesPanel == null)
            {
                DebugLogger.Warning("TryGetTrophiesPanelBackground: InventoryGui or m_trophiesPanel is null");
                return false;
            }

            GameObject trophiesPanel = InventoryGui.instance.m_trophiesPanel;

            Image rootImage = trophiesPanel.GetComponent<Image>();
            if (rootImage != null && rootImage.sprite != null)
            {
                sprite = rootImage.sprite;
                material = rootImage.material;
                DebugLogger.Verbose($"TryGetTrophiesPanelBackground: Found on root - sprite={sprite.name}, material={(material != null ? material.name : "null")}");
                return true;
            }

            Transform bgChild = trophiesPanel.transform.Find("background")
                ?? trophiesPanel.transform.Find("Background")
                ?? trophiesPanel.transform.Find("bg");

            if (bgChild != null)
            {
                Image bgImage = bgChild.GetComponent<Image>();
                if (bgImage != null && bgImage.sprite != null)
                {
                    sprite = bgImage.sprite;
                    material = bgImage.material;
                    DebugLogger.Verbose($"TryGetTrophiesPanelBackground: Found on child '{bgChild.name}' - sprite={sprite.name}");
                    return true;
                }
            }

            Image[] childImages = trophiesPanel.GetComponentsInChildren<Image>(true);
            foreach (var img in childImages)
            {
                if (img != null && img.sprite != null && img.gameObject != trophiesPanel)
                {
                    sprite = img.sprite;
                    material = img.material;
                    DebugLogger.Verbose($"TryGetTrophiesPanelBackground: Found on child '{img.gameObject.name}' via iteration");
                    return true;
                }
            }

            DebugLogger.Warning("TryGetTrophiesPanelBackground: No suitable background found");
            return false;
        }

        /// <summary>
        /// Reuses the vanilla sign/tombstone naming text box — background sprite plus the
        /// Selectable's hover/press state — so the group name dialog's input field matches
        /// Valheim's native look and reacts the same way.
        /// </summary>
        private static bool TryGetVanillaInputFieldStyle(out Sprite sprite, out Material material, out Color color, out Selectable source)
        {
            sprite = null;
            material = null;
            color = Color.white;
            source = null;

            TextInput textInput = TextInput.instance;
            if (textInput == null || textInput.m_inputField == null)
            {
                DebugLogger.Verbose("TryGetVanillaInputFieldStyle: TextInput or m_inputField is null");
                return false;
            }

            Image bg = textInput.m_inputField.targetGraphic as Image;
            if (bg == null)
                bg = textInput.m_inputField.GetComponent<Image>();

            if (bg == null || bg.sprite == null)
            {
                DebugLogger.Verbose("TryGetVanillaInputFieldStyle: no background Image with a sprite");
                return false;
            }

            sprite = bg.sprite;
            material = bg.material;
            color = bg.color;
            source = textInput.m_inputField; // GuiInputField derives from Selectable
            DebugLogger.Verbose($"TryGetVanillaInputFieldStyle: sprite={sprite.name}, material={(material != null ? material.name : "null")}");
            return true;
        }

    }
}
