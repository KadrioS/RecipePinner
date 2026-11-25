using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    /// <summary>
    /// Puts the mod's pin and unpin shortcuts into Valheim's own button-hint bars.
    ///
    /// The bars' children are input modes rather than entries, and each entry carries its label on
    /// a child named "Text" and each key on a child named "Key" - a two-key entry has two of them,
    /// modifier first. Entries are matched by those names rather than by position, and a bar whose
    /// shape does not match is left alone: showing no hint is far better than disturbing the
    /// game's HUD.
    /// </summary>
    public static class KeyHintInjector
    {
        private const string PinEntryName = "RecipePinnerPin";
        private const string UnpinEntryName = "RecipePinnerUnpin";

        private static KeyHints _injectedInto;

        // Smallest scale a bar is shrunk to; past it the game's layout squeezes the labels again.
        private const float MinBarScale = 0.6f;

        /// <summary>A bar mode's placement before it was first shrunk, so it can be put back.</summary>
        private sealed class SavedPlacement
        {
            public Vector2 Pivot;
            public Vector2 SizeDelta;
            public Vector3 Scale;
            public float AppliedScale;
        }

        private static readonly Dictionary<RectTransform, SavedPlacement> _savedPlacements = new Dictionary<RectTransform, SavedPlacement>();

        /// <summary>
        /// Adds the entries once per KeyHints instance. UpdateHints only toggles the bars' active
        /// state and never rebuilds their children, so what is added here survives; a new instance
        /// after a scene change is detected by the comparison below and gets its own copy.
        /// </summary>
        public static void EnsureInjected()
        {
            KeyHints hints = KeyHints.instance;
            if (hints == null) return;
            if (_injectedInto == hints) return;

            _injectedInto = hints;
            _savedPlacements.Clear();

            InjectInto(hints.m_inventoryHints);
            InjectInto(hints.m_inventoryWithContainerHints);
            InjectInto(hints.m_buildHints);
        }

        /// <summary>Re-reads the configured keys into entries that already exist.</summary>
        public static void RefreshKeys()
        {
            KeyHints hints = KeyHints.instance;
            if (hints == null) return;

            RefreshBar(hints.m_inventoryHints);
            RefreshBar(hints.m_inventoryWithContainerHints);
            RefreshBar(hints.m_buildHints);
        }

        /// <summary>
        /// Shows an entry only where its press would do something: the build bar needs the
        /// piece-selection window open, and Unpin needs a pin the shortcut can remove.
        /// </summary>
        public static void UpdateHintVisibility()
        {
            KeyHints hints = KeyHints.instance;
            if (hints == null) return;

            // A None key disables its shortcut, so its hint would show an empty key box. Unpin is
            // the pin key pressed with the unpin key held, so it needs both.
            bool canPin = GetPinKey() != KeyCode.None;
            bool canUnpin = canPin && GetUnpinKey() != KeyCode.None && HasHotkeyRemovablePin();
            bool buildMenuOpen = IsBuildMenuOpen();

            SetEntriesActive(hints.m_inventoryHints, canPin, canUnpin);
            SetEntriesActive(hints.m_inventoryWithContainerHints, canPin, canUnpin);
            SetEntriesActive(hints.m_buildHints, buildMenuOpen && canPin, buildMenuOpen && canUnpin);

            FitBar(hints.m_inventoryHints);
            FitBar(hints.m_inventoryWithContainerHints);
            FitBar(hints.m_buildHints);
        }

        /// <summary>
        /// Hides every injected entry. UpdateUI, which keeps them in step, stops while the mod is
        /// off, so switching it off must hide them itself.
        /// </summary>
        public static void HideAll()
        {
            RestoreBarScales();

            KeyHints hints = KeyHints.instance;
            if (hints == null) return;

            SetEntriesActive(hints.m_inventoryHints, false, false);
            SetEntriesActive(hints.m_inventoryWithContainerHints, false, false);
            SetEntriesActive(hints.m_buildHints, false, false);
        }

        /// <summary>
        /// The game's layout group squeezes every entry once the row prefers more width than the
        /// bar has, and the key boxes then overflow past the screen edge. Instead the mode is made
        /// wider by what a scale takes away and shrunk about its right edge, so its contents get
        /// the room they prefer and the bar keeps its place and width on screen.
        /// </summary>
        private static void FitBar(GameObject bar)
        {
            if (bar == null || !bar.activeInHierarchy) return;

            RectTransform mode = bar.transform.Find("Keyboard") as RectTransform;
            if (mode == null) return;

            RectTransform parent = mode.parent as RectTransform;
            if (parent == null) return;

            float available = parent.rect.width;
            float preferred = LayoutUtility.GetPreferredWidth(mode);

            float scale = 1f;
            bool fitEnabled = RecipePinnerPlugin.FitKeyHintBar == null || RecipePinnerPlugin.FitKeyHintBar.Value;
            if (fitEnabled && available > 0f && preferred > available)
                scale = Mathf.Max(MinBarScale, available / preferred);

            ApplyBarScale(mode, available, scale);
        }

        private static void ApplyBarScale(RectTransform mode, float available, float scale)
        {
            SavedPlacement saved;
            if (!_savedPlacements.TryGetValue(mode, out saved))
            {
                if (scale >= 1f) return;

                // Only the measured shape - stretched over its parent with no offset - is changed.
                if (mode.anchorMin != Vector2.zero || mode.anchorMax != Vector2.one ||
                    mode.sizeDelta != Vector2.zero || mode.anchoredPosition != Vector2.zero)
                    return;

                saved = new SavedPlacement
                {
                    Pivot = mode.pivot,
                    SizeDelta = mode.sizeDelta,
                    Scale = mode.localScale,
                    AppliedScale = 1f
                };
                _savedPlacements[mode] = saved;
            }

            if (Mathf.Abs(saved.AppliedScale - scale) < 0.001f) return;
            saved.AppliedScale = scale;

            if (scale >= 1f)
            {
                RestorePlacement(mode, saved);
                return;
            }

            mode.pivot = new Vector2(1f, 0.5f);
            mode.sizeDelta = new Vector2(available / scale - available, saved.SizeDelta.y);
            mode.localScale = new Vector3(saved.Scale.x * scale, saved.Scale.y * scale, saved.Scale.z);
        }

        private static void RestoreBarScales()
        {
            foreach (KeyValuePair<RectTransform, SavedPlacement> entry in _savedPlacements)
            {
                if (entry.Key == null || entry.Value.AppliedScale >= 1f) continue;

                entry.Value.AppliedScale = 1f;
                RestorePlacement(entry.Key, entry.Value);
            }
        }

        private static void RestorePlacement(RectTransform mode, SavedPlacement saved)
        {
            mode.pivot = saved.Pivot;
            mode.sizeDelta = saved.SizeDelta;
            mode.localScale = saved.Scale;
        }

        // Group rows do not count: the shortcut refuses to remove a group member, so a player
        // holding nothing but groups has nothing for it to act on.
        private static bool HasHotkeyRemovablePin()
        {
            RecipePinnerPlugin plugin = RecipePinnerPlugin.Instance;
            if (plugin == null) return false;

            RecipeManager recipeMgr = plugin.RecipeMgr;
            if (recipeMgr == null) return false;

            foreach (PinnedRecipeData pin in recipeMgr.CachedPins)
            {
                if (pin != null && !pin.IsGroup) return true;
            }

            return false;
        }

        // Ask Hud.InBuildUi() rather than reading a window off Hud directly. Valheim 1.0 moved the
        // build menu into BuildUi, and Hud.m_pieceSelectionWindow is what stayed behind: the whole
        // class touches it in exactly one place, to switch it off, and never turns it on. Reading
        // it therefore answers "closed" forever - the same kind of vestigial field that made the
        // build-piece hover look broken once before.
        private static bool IsBuildMenuOpen()
        {
            return Hud.InBuildUi();
        }

        private static void SetEntriesActive(GameObject bar, bool pinVisible, bool unpinVisible)
        {
            if (bar == null) return;

            Transform keyboard = bar.transform.Find("Keyboard");
            if (keyboard == null) return;

            SetEntryActive(keyboard.Find(PinEntryName), pinVisible);
            SetEntryActive(keyboard.Find(UnpinEntryName), unpinVisible);
        }

        private static void SetEntryActive(Transform entry, bool visible)
        {
            if (entry == null) return;
            if (entry.gameObject.activeSelf == visible) return;

            entry.gameObject.SetActive(visible);
        }

        private static void InjectInto(GameObject bar)
        {
            if (bar == null) return;

            Transform keyboard = bar.transform.Find("Keyboard");
            if (keyboard == null) return;
            if (keyboard.Find(PinEntryName) != null) return;

            Transform singleKeyTemplate = FindTemplate(keyboard, 1);
            if (singleKeyTemplate != null)
            {
                AddEntry(keyboard, singleKeyTemplate, PinEntryName, "hint_pin",
                    GetPinKey(), KeyCode.None);
            }

            Transform twoKeyTemplate = FindTemplate(keyboard, 2);
            if (twoKeyTemplate != null)
            {
                AddEntry(keyboard, twoKeyTemplate, UnpinEntryName, "hint_unpin",
                    GetUnpinKey(), GetPinKey());
            }
        }

        private static void RefreshBar(GameObject bar)
        {
            if (bar == null) return;

            Transform keyboard = bar.transform.Find("Keyboard");
            if (keyboard == null) return;

            Transform pin = keyboard.Find(PinEntryName);
            if (pin != null) ApplyTexts(pin.gameObject, "hint_pin", GetPinKey(), KeyCode.None);

            Transform unpin = keyboard.Find(UnpinEntryName);
            if (unpin != null) ApplyTexts(unpin.gameObject, "hint_unpin", GetUnpinKey(), GetPinKey());
        }

        /// <summary>
        /// The first entry carrying a label and exactly the wanted number of keys. Our own entries
        /// are skipped so a refresh never clones a clone.
        /// </summary>
        private static Transform FindTemplate(Transform mode, int wantedKeys)
        {
            // A two-key entry is cloned from one that draws a "+" between its keys where the bar
            // has one. The "+" is matched by its text: its object name differs between bars.
            Transform firstMatch = null;

            for (int i = 0; i < mode.childCount; i++)
            {
                Transform child = mode.GetChild(i);
                if (child.name == PinEntryName || child.name == UnpinEntryName) continue;

                int keys = 0;
                bool hasLabel = false;
                bool hasPlus = false;

                TMP_Text[] texts = child.GetComponentsInChildren<TMP_Text>(true);
                for (int t = 0; t < texts.Length; t++)
                {
                    string objectName = texts[t].gameObject.name;
                    if (objectName == "Key") keys++;
                    else if (objectName == "Text") hasLabel = true;
                    else if (texts[t].text != null && texts[t].text.Trim() == "+") hasPlus = true;
                }

                if (!hasLabel || keys != wantedKeys) continue;
                if (wantedKeys < 2 || hasPlus) return child;
                if (firstMatch == null) firstMatch = child;
            }

            return firstMatch;
        }

        private static void AddEntry(Transform parent, Transform template, string entryName, string labelKey, KeyCode firstKey, KeyCode secondKey)
        {
            GameObject clone = Object.Instantiate(template.gameObject, parent);
            clone.name = entryName;
            if (!clone.activeSelf) clone.SetActive(true);

            // The template's label width was chosen for its own text; ours sizes to its own.
            Transform label = clone.transform.Find("Text");
            LayoutElement labelLayout = (label == null) ? null : label.GetComponent<LayoutElement>();
            if (labelLayout != null) labelLayout.preferredWidth = -1f;

            ApplyTexts(clone, labelKey, firstKey, secondKey);
            DebugLogger.Log($"Added key hint '{entryName}' to '{parent.parent.name}'");
        }

        private static void ApplyTexts(GameObject entry, string labelKey, KeyCode firstKey, KeyCode secondKey)
        {
            string label = GetLabel(labelKey);
            int keyIndex = 0;

            TMP_Text[] texts = entry.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                string objectName = text.gameObject.name;

                if (objectName == "Text")
                {
                    text.text = label;
                }
                else if (objectName == "Key")
                {
                    text.text = DescribeKey((keyIndex == 0) ? firstKey : secondKey);
                    keyIndex++;
                }
            }
        }

        /// <summary>
        /// Asks the game what a key is called. Deriving these by hand would be a trap rather than
        /// a nicety: Valheim numbers mouse buttons from one, so its own bar calls the right button
        /// "Mouse-2", while Unity's KeyCode.Mouse2 is the wheel. A hand-written table would put
        /// "Mouse-2" beside a shortcut that right-clicking does not trigger, two rows under the
        /// game's own "Use/equip  Mouse-2".
        /// </summary>
        private static string DescribeKey(KeyCode code)
        {
            if (code == KeyCode.None) return string.Empty;

            string token;
            if (GameKeyTokens.TryGetValue(code, out token) && Localization.instance != null)
            {
                string localized = Localization.instance.Localize(token);
                if (!string.IsNullOrEmpty(localized) && !localized.Contains("MISSING KEY"))
                    return localized;
            }

            string name = ZInput.KeyCodeToDisplayName(code);

            // The game answers an unresolvable KeyCode with a sentence rather than a key name.
            if (string.IsNullOrEmpty(name) || name.Contains("did not have corresponding"))
                return code.ToString();

            return name;
        }

        // The tokens the game's own hints use for these keys: ZInput.GetBoundKeyString maps a
        // binding to them through its private s_keyLocalizationMap, with the same KeyCode-to-button
        // mapping as KeyCodeToDisplayName, which stays the fallback for every other key.
        private static readonly Dictionary<KeyCode, string> GameKeyTokens = new Dictionary<KeyCode, string>
        {
            { KeyCode.Mouse0, "$button_mouse0" },
            { KeyCode.Mouse1, "$button_mouse1" },
            { KeyCode.Mouse2, "$button_mouse2" },
            { KeyCode.Space, "$button_space" },
            { KeyCode.Return, "$button_return" },
            { KeyCode.LeftShift, "$button_lshift" },
            { KeyCode.RightShift, "$button_rshift" },
            { KeyCode.LeftAlt, "$button_lalt" },
            { KeyCode.RightAlt, "$button_ralt" },
            { KeyCode.LeftControl, "$button_lctrl" },
            { KeyCode.RightControl, "$button_rctrl" },
        };

        private static string GetLabel(string labelKey)
        {
            RecipePinnerPlugin plugin = RecipePinnerPlugin.Instance;
            LocalizationManager loc = (plugin == null) ? null : plugin.LocalizationMgr;
            if (loc == null) return labelKey;

            return loc.GetText(labelKey);
        }

        private static KeyCode GetPinKey()
        {
            return (RecipePinnerPlugin.HotkeyPin == null) ? KeyCode.Mouse2 : RecipePinnerPlugin.HotkeyPin.Value;
        }

        private static KeyCode GetUnpinKey()
        {
            return (RecipePinnerPlugin.HotkeyUnpin == null) ? KeyCode.LeftShift : RecipePinnerPlugin.HotkeyUnpin.Value;
        }
    }
}
