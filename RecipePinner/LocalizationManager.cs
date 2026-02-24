using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ValheimRecipePinner
{
    public class LocalizationManager
    {
        private RecipePinnerPlugin _plugin;
        private Dictionary<string, string> _localizedText = new Dictionary<string, string>();

        private static readonly Dictionary<string, string> _defaultEnglish = new Dictionary<string, string>
        {
            { "pinned", "Recipe Pinned!" },
            { "unpinned", "Pin Removed" },
            { "list_full", "List Full!" },
            { "added_more", "Added More: {0}x" },
            { "decreased", "Decreased: {0}x" },
            { "cleared", "Pinned Recipes Cleared" },
            { "max_level", "Max Level Reached" },
            { "no_upgrade_cost", "No upgrade cost found" }
        };

        public LocalizationManager(RecipePinnerPlugin plugin)
        {
            _plugin = plugin;
            DebugLogger.Log("LocalizationManager initialized");
        }

        public void LoadTranslations()
        {
            _localizedText.Clear();

            string targetLang = RecipePinnerPlugin.LanguageOverride.Value.Trim();

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

            string pluginPath = Path.GetDirectoryName(_plugin.Info.Location);
            string langPath = Path.Combine(pluginPath, "RecipePinner_languages", $"{targetLang}.json");

            if (!File.Exists(langPath))
            {
                DebugLogger.Log($"Language file not found: {langPath} - Using default English");
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(langPath);
                int loadedCount = 0;

                foreach (string line in jsonContent.Split('\n'))
                {
                    if (line.Contains(":"))
                    {
                        string[] parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().Trim(',', '\"', ' ', '\t', '\r');
                            string val = parts[1].Trim().Trim(',', '\"', ' ', '\t', '\r');

                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                            {
                                _localizedText[key] = val;
                                loadedCount++;
                            }
                        }
                    }
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
            // Try localized text first
            if (_localizedText.TryGetValue(key, out string val))
            {
                DebugLogger.Verbose($"Translation found for '{key}': {val}");
                return val;
            }

            // Fall back to default English
            if (_defaultEnglish.TryGetValue(key, out string defVal))
            {
                DebugLogger.Verbose($"Using default English for '{key}': {defVal}");
                return defVal;
            }

            // Return key if no translation found
            DebugLogger.Warning($"No translation found for key: {key}");
            return key;
        }
    }
}