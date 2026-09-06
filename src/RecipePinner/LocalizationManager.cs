using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValheimRecipePinner
{
    public class LocalizationManager
    {
        private readonly RecipePinnerPlugin _plugin;
        private readonly Dictionary<string, string> _localizedText = new Dictionary<string, string>();

        private static readonly Dictionary<string, string> _defaultEnglish = new Dictionary<string, string>
        {
            { "pinned", "Recipe Pinned!" },
            { "unpinned", "Pin Removed" },
            { "list_full", "List Full!" },
            { "added_more", "Added More: {0}x" },
            { "decreased", "Decreased: {0}x" },
            { "cleared", "Pinned Recipes Cleared" },
            { "clear_confirm_hotkey", "Press again to clear all pins" },
            { "max_level", "Max Level Reached" },
            { "no_upgrade_cost", "No upgrade cost found" },
            { "gathering_title", "GATHERING LIST" },
            { "gathering_opened", "Gathering List Opened" },
            { "gathering_closed", "Gathering List Closed" },
            { "gathering_empty", "No Recipes Pinned" },
            { "gathering_hint", "Open/Close: {0}" },
            { "mypins_title", "MY PINS" },
            { "mypins_button", "Pins" },
            { "mypins_empty", "No Recipes Pinned" },
            { "group_button", "Group" },
            { "group_confirm", "Confirm" },
            { "group_cancel", "Cancel" },
            { "group_name_prompt", "Enter group name:" },
            { "group_name_empty", "Group name cannot be empty" },
            { "group_created", "Group Created: {0}" },
            { "group_disbanded", "Group Disbanded: {0}" },
            { "group_select_hint", "Select pins to group" },
            { "group_min_select", "Select at least 2 pins" },
            { "group_need_more", "At least 2 pins needed to create a group" },
            { "group_create_failed", "Group could not be created" },
            { "group_name_exists", "Group '{0}' already exists" },
            { "confirm_delete_group", "Delete group \"{0}\" and all member pins?" },
            { "confirm_delete_pin", "Delete \"{0}\"?" },
            { "confirm_remove_member", "Remove \"{0}\" from group \"{1}\"?" },
            { "confirm_button", "Confirm" },
            { "cancel_button", "Cancel" },
            { "confirm_disband_group", "Disband group \"{0}\"? Members will become individual pins." },
            { "clear_button", "Clear" },
            { "clear_confirm_msg", "Remove all pins?" },
            { "close_button", "Close" },
            { "controls_title", "CONTROLS" },
            { "controls_config_note_single", "Controls can be changed in the config file." },
            { "howto_header", "HOW TO USE" },
            { "howto_pin", "Hover over a recipe in the crafting menu and press [{0}] to pin it." },
            { "howto_unpin", "Hold [{0}] and press [{1}] to unpin a recipe." },
            { "howto_toggle_hud", "Press [{0}] to show or hide the pinned recipe overlay." },
            { "howto_gathering", "Press [{0}] to open or close the gathering list." },
            { "howto_next_page", "Press [{0}] to cycle through HUD pages." },
            { "howto_clear_all", "Press [{0}] to remove all pinned recipes." },
            { "keybindings_header", "KEY BINDINGS" },
            { "ctrl_pin", "Pin Recipe" },
            { "ctrl_unpin", "Unpin  (hold + Pin Recipe key)" },
            { "ctrl_toggle_hud", "Toggle HUD Visibility" },
            { "ctrl_gathering", "Toggle Gathering List" },
            { "ctrl_next_page", "Next HUD Page" },
            { "ctrl_clear_all", "Clear All Pins" }
        };

        public LocalizationManager(RecipePinnerPlugin plugin)
        {
            _plugin = plugin;
            DebugLogger.Log("LocalizationManager init");
        }

        public void LoadTranslations()
        {
            _localizedText.Clear();

            string targetLang = RecipePinnerPlugin.LanguageOverride?.Value?.Trim();

            if (string.IsNullOrEmpty(targetLang) || targetLang.ToLower() == "auto")
            {
                if (Localization.instance != null)
                    targetLang = Localization.instance.GetSelectedLanguage();
                else
                    targetLang = "English";

                DebugLogger.Log($"Auto-detected language: {targetLang}");
            }
            else
            {
                DebugLogger.Log($"Using forced language: {targetLang}");
            }

            string safeLang = targetLang;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safeLang = safeLang.Replace(c, '_');
            }

            string pluginPath = Path.GetDirectoryName(_plugin.Info.Location);
            string langPath = Path.Combine(pluginPath, "RecipePinner_languages", $"{safeLang}.json");

            if (!File.Exists(langPath))
            {
                DebugLogger.Log($"Language file not found: {langPath} - Using default English");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(langPath);
                int loadedCount = 0;

                // Split on all common line endings (Windows \r\n, Unix \n, old Mac \r)
                foreach (string line in jsonContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed == "{" || trimmed == "}" || !trimmed.Contains(":"))
                        continue;

                    string[] parts = trimmed.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim(',', '"', ' ', '\t', '\r');
                        string val = parts[1].Trim(',', '"', ' ', '\t', '\r');

                        // Unescape basic JSON escape sequences
                        val = val.Replace("\\\"", "\"")
                                 .Replace("\\n", "\n")
                                 .Replace("\\t", "\t")
                                 .Replace("\\\\", "\\");

                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                        {
                            _localizedText[key] = val;
                            loadedCount++;
                        }
                    }
                }

                // A file the parser cannot read still falls back to English, silently. Warn when the
                // result is far below the expected key count - that is what a format it does not
                // understand looks like (see C14). Warning is not gated by EnableDebugLogging.
                if (loadedCount < _defaultEnglish.Count / 2)
                {
                    DebugLogger.Warning($"Only {loadedCount} of {_defaultEnglish.Count} translations were read from {safeLang}.json - the file format may not be supported (one \"key\": \"value\" pair per line is expected). The missing texts fall back to English.");
                }

                DebugLogger.Log($"Loaded {loadedCount} translations from: {targetLang}.json");
            }
            catch (System.Exception ex)
            {
                DebugLogger.Error($"Failed to load language file: {langPath}", ex);
            }
        }

        public string GetText(string key)
        {
            if (_localizedText.TryGetValue(key, out string val))
            {
                DebugLogger.Verbose($"Translation found for '{key}': {val}");
                return val;
            }

            if (_defaultEnglish.TryGetValue(key, out string defVal))
            {
                DebugLogger.Verbose($"Using default English for '{key}': {defVal}");
                return defVal;
            }

            DebugLogger.Warning($"No translation found for key: {key}");
            return key;
        }
    }
}
