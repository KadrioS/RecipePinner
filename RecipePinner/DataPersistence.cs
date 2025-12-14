using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ValheimRecipePinner
{
    public class DataPersistence
    {
        public void SavePins()
        {
            try
            {
                string savePath = GetSavePath();
                if (string.IsNullOrEmpty(savePath))
                {
                    DebugLogger.Warning("Cannot save - save path is invalid");
                    return;
                }

                var recipeMgr = RecipePinnerPlugin.Instance.RecipeMgr;

                List<string> lines = new List<string>();
                foreach (var kvp in recipeMgr.PinnedRecipes)
                {
                    lines.Add($"{kvp.Key}:{kvp.Value}");
                }

                File.WriteAllLines(savePath, lines);
                DebugLogger.Log($"Saved {lines.Count} pinned recipes to: {savePath}");
            }
            catch (System.Exception ex)
            {
                DebugLogger.Error("Failed to save pins", ex);
            }
        }

        public void LoadPins()
        {
            string savePath = GetSavePath();
            if (string.IsNullOrEmpty(savePath))
            {
                DebugLogger.Warning("Cannot load - save path is invalid");
                return;
            }

            var recipeMgr = RecipePinnerPlugin.Instance.RecipeMgr;

            if (!File.Exists(savePath))
            {
                DebugLogger.Log($"No save file found at: {savePath}");
                return;
            }

            try
            {
                recipeMgr.PinnedRecipes.Clear();
                string[] lines = File.ReadAllLines(savePath);

                int loadedCount = 0;
                int errorCount = 0;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.Contains(":"))
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string valString = parts[1].Trim();

                            if (int.TryParse(valString, out int count))
                            {
                                recipeMgr.PinnedRecipes[key] = count;
                                loadedCount++;
                            }
                            else
                            {
                                DebugLogger.Warning($"Invalid count value in save file: {line}");
                                errorCount++;
                            }
                        }
                    }
                    else
                    {
                        // Legacy format without count
                        if (!recipeMgr.PinnedRecipes.ContainsKey(line))
                        {
                            recipeMgr.PinnedRecipes[line] = 1;
                            loadedCount++;
                        }
                    }
                }

                // Enforce maximum pins limit
                if (recipeMgr.PinnedRecipes.Count > RecipePinnerPlugin.MaximumPins.Value)
                {
                    int originalCount = recipeMgr.PinnedRecipes.Count;
                    recipeMgr.PinnedRecipes = recipeMgr.PinnedRecipes
                        .Take(RecipePinnerPlugin.MaximumPins.Value)
                        .ToDictionary(k => k.Key, v => v.Value);

                    DebugLogger.Warning($"Exceeded max pins limit - trimmed from {originalCount} to {recipeMgr.PinnedRecipes.Count}");
                }

                DebugLogger.Log($"Loaded {loadedCount} recipes from: {savePath} (Errors: {errorCount})");
            }
            catch (System.Exception ex)
            {
                DebugLogger.Error("Failed to load pins", ex);
            }
        }

        private string GetSavePath()
        {
            if (Player.m_localPlayer == null)
            {
                DebugLogger.Verbose("Cannot get save path - local player is null");
                return null;
            }

            string playerName = Player.m_localPlayer.GetPlayerName();
            if (string.IsNullOrWhiteSpace(playerName))
            {
                DebugLogger.Warning("Cannot get save path - player name is empty");
                return null;
            }

            string baseDir = Path.Combine(Paths.ConfigPath, "RecipePinner_Data");

            if (!Directory.Exists(baseDir))
            {
                try
                {
                    Directory.CreateDirectory(baseDir);
                    DebugLogger.Log($"Created save directory: {baseDir}");
                }
                catch (System.Exception ex)
                {
                    DebugLogger.Error($"Failed to create save directory: {baseDir}", ex);
                    return null;
                }
            }

            // Sanitize player name for file system
            string sanitizedName = playerName;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                sanitizedName = sanitizedName.Replace(c, '_');
            }

            string fullPath = Path.Combine(baseDir, $"{sanitizedName}.txt");
            DebugLogger.Verbose($"Save path: {fullPath}");
            return fullPath;
        }
    }
}