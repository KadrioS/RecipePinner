using TMPro;
using UnityEngine;

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

            bool canUnpin = HasHotkeyRemovablePin();
            bool buildMenuOpen = IsBuildMenuOpen();

            SetEntriesActive(hints.m_inventoryHints, true, canUnpin);
            SetEntriesActive(hints.m_inventoryWithContainerHints, true, canUnpin);
            SetEntriesActive(hints.m_buildHints, buildMenuOpen, buildMenuOpen && canUnpin);
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
            for (int i = 0; i < mode.childCount; i++)
            {
                Transform child = mode.GetChild(i);
                if (child.name == PinEntryName || child.name == UnpinEntryName) continue;

                int keys = 0;
                bool hasLabel = false;

                TMP_Text[] texts = child.GetComponentsInChildren<TMP_Text>(true);
                for (int t = 0; t < texts.Length; t++)
                {
                    string objectName = texts[t].gameObject.name;
                    if (objectName == "Key") keys++;
                    else if (objectName == "Text") hasLabel = true;
                }

                if (hasLabel && keys == wantedKeys) return child;
            }

            return null;
        }

        private static void AddEntry(Transform parent, Transform template, string entryName, string labelKey, KeyCode firstKey, KeyCode secondKey)
        {
            GameObject clone = Object.Instantiate(template.gameObject, parent);
            clone.name = entryName;
            if (!clone.activeSelf) clone.SetActive(true);

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

            string name = ZInput.KeyCodeToDisplayName(code);

            // The game answers an unresolvable KeyCode with a sentence rather than a key name.
            if (string.IsNullOrEmpty(name) || name.Contains("did not have corresponding"))
                return code.ToString();

            return name;
        }

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
