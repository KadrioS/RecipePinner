using BepInEx;
using System.Collections.Generic;
using System.IO;
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

                var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
                if (recipeMgr == null)
                {
                    DebugLogger.Warning("Cannot save - RecipeMgr is null");
                    return;
                }

                List<string> lines = new List<string>();
                HashSet<string> savedEntries = new HashSet<string>();
                // Save in PinnedRecipeOrder which interleaves recipe keys and GROUP: markers
                foreach (string entry in recipeMgr.PinnedRecipeOrder)
                {
                    if (!savedEntries.Add(entry))
                    {
                        DebugLogger.Warning($"Skipping duplicate pin order entry while saving: {entry}");
                        continue;
                    }

                    if (entry.StartsWith("GROUP:"))
                    {
                        string groupName = entry.Substring(6);
                        if (recipeMgr.PinGroups.TryGetValue(groupName, out PinGroupData grpData))
                        {
                            // Save format: escapedKey1:count1,escapedKey2:count2
                            var memberParts = new List<string>();
                            foreach (string mk in grpData.MemberRecipeKeys)
                            {
                                int mc = grpData.MemberCounts.TryGetValue(mk, out int c) ? c : 1;
                                memberParts.Add($"{EscapeSaveValue(mk)}:{mc}");
                            }
                            string members = string.Join(",", memberParts);
                            lines.Add($"GROUP:{EscapeSaveValue(groupName)}|{members}");
                            DebugLogger.Verbose($"Saved group: {groupName} with {grpData.MemberRecipeKeys.Count} members");
                        }
                    }
                    else if (recipeMgr.PinnedRecipes.TryGetValue(entry, out int count))
                    {
                        lines.Add($"{EscapeSaveValue(entry)}:{count}");
                    }
                }

                WriteAllLinesAtomically(savePath, lines);
                int groupCount = recipeMgr.PinGroups.Count;
                DebugLogger.Log($"Saved {lines.Count} entries ({lines.Count - groupCount} pins, {groupCount} groups) to: {savePath}");
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

            var recipeMgr = RecipePinnerPlugin.Instance?.RecipeMgr;
            if (recipeMgr == null)
            {
                DebugLogger.Warning("Cannot load - RecipeMgr is null");
                return;
            }

            if (!File.Exists(savePath))
            {
                DebugLogger.Log($"No save file found at: {savePath}");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(savePath);
                var loadedPins = new Dictionary<string, int>();
                var loadedGroups = new Dictionary<string, PinGroupData>();
                var loadedOrder = new List<string>();
                var loadedOrderEntries = new HashSet<string>();

                int loadedCount = 0;
                int groupCount = 0;
                int errorCount = 0;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Parse group lines: GROUP:GroupName|key1,key2,key3
                    if (line.StartsWith("GROUP:"))
                    {
                        string groupContent = line.Substring(6); // after "GROUP:"
                        int pipeIndex = FindGroupSeparator(groupContent);
                        if (pipeIndex > 0 && pipeIndex < groupContent.Length - 1)
                        {
                            string groupName = UnescapeSaveValue(groupContent.Substring(0, pipeIndex).Trim());
                            string membersStr = groupContent.Substring(pipeIndex + 1).Trim();
                            string[] memberKeys = membersStr.Split(',');

                            if (!string.IsNullOrEmpty(groupName) && memberKeys.Length >= 2)
                            {
                                PinGroupData groupData = new PinGroupData
                                {
                                    GroupName = groupName
                                };
                                foreach (string mk in memberKeys)
                                {
                                    string trimmedEntry = mk.Trim();
                                    if (string.IsNullOrEmpty(trimmedEntry)) continue;

                                    // Parse key:count or just key (legacy, count=1)
                                    int colonIdx = trimmedEntry.LastIndexOf(':');
                                    if (colonIdx > 0 && colonIdx < trimmedEntry.Length - 1)
                                    {
                                        string trimmedKey = UnescapeSaveValue(trimmedEntry.Substring(0, colonIdx));
                                        int memberCount = 1;
                                        int.TryParse(trimmedEntry.Substring(colonIdx + 1), out memberCount);
                                        if (memberCount < 1) memberCount = 1;
                                        groupData.MemberRecipeKeys.Add(trimmedKey);
                                        groupData.MemberCounts[trimmedKey] = memberCount;
                                    }
                                    else
                                    {
                                        string memberKey = UnescapeSaveValue(trimmedEntry);
                                        groupData.MemberRecipeKeys.Add(memberKey);
                                        groupData.MemberCounts[memberKey] = 1; // Legacy fallback
                                    }
                                }

                                if (groupData.MemberRecipeKeys.Count >= 2)
                                {
                                    string groupOrderEntry = $"GROUP:{groupName}";
                                    if (!loadedOrderEntries.Add(groupOrderEntry))
                                    {
                                        DebugLogger.Warning($"Duplicate group entry in save file, keeping first order position and latest data: {groupName}");
                                        errorCount++;
                                    }
                                    else
                                    {
                                        loadedOrder.Add(groupOrderEntry);
                                    }

                                    loadedGroups[groupName] = groupData;
                                    groupCount++;
                                    DebugLogger.Verbose($"Loaded group: {groupName} with {groupData.MemberRecipeKeys.Count} members");
                                }
                                else
                                {
                                    DebugLogger.Warning($"Group '{groupName}' has less than 2 members, skipping");
                                    errorCount++;
                                }
                            }
                            else
                            {
                                DebugLogger.Warning($"Invalid group format: {line}");
                                errorCount++;
                            }
                        }
                        else
                        {
                            DebugLogger.Warning($"Invalid group line (missing pipe): {line}");
                            errorCount++;
                        }
                        continue;
                    }

                    // Parse regular pin lines: key:count
                    int lastColon = line.LastIndexOf(':');
                    if (lastColon > 0 && lastColon < line.Length - 1)
                    {
                        string key = UnescapeSaveValue(line.Substring(0, lastColon).Trim());
                        string valString = line.Substring(lastColon + 1).Trim();

                        if (int.TryParse(valString, out int count))
                        {
                            if (!loadedOrderEntries.Add(key))
                            {
                                DebugLogger.Warning($"Duplicate pin entry in save file, keeping first order position and latest count: {key}");
                                errorCount++;
                            }
                            else
                            {
                                loadedOrder.Add(key);
                            }

                            loadedPins[key] = count;
                            loadedCount++;
                        }
                        else
                        {
                            DebugLogger.Warning($"Invalid count value in save file: {line}");
                            errorCount++;
                        }
                    }
                    else
                    {
                        string key = UnescapeSaveValue(line.Trim());
                        if (!loadedOrderEntries.Add(key))
                        {
                            DebugLogger.Warning($"Duplicate legacy pin entry in save file, keeping first order position and latest count: {key}");
                            errorCount++;
                        }
                        else
                        {
                            loadedOrder.Add(key);
                        }

                        loadedPins[key] = 1;
                        loadedCount++;
                    }
                }

                recipeMgr.PinnedRecipes.Clear();
                recipeMgr.PinGroups.Clear();
                recipeMgr.PinnedRecipeOrder.Clear();

                foreach (var kvp in loadedPins)
                    recipeMgr.PinnedRecipes[kvp.Key] = kvp.Value;
                foreach (var kvp in loadedGroups)
                    recipeMgr.PinGroups[kvp.Key] = kvp.Value;
                recipeMgr.PinnedRecipeOrder.AddRange(loadedOrder);

                // Enforce the cap the same way the MaximumPins config handler does, so a save made
                // with a higher limit cannot silently exceed it. TrimToMaximumPins also handles
                // groups, which the old inline loop never did.
                int effectiveCount = recipeMgr.GetEffectivePinCount();
                if (effectiveCount > RecipePinnerPlugin.MaximumPins.Value)
                {
                    int trimmed = recipeMgr.TrimToMaximumPins(RecipePinnerPlugin.MaximumPins.Value);
                    DebugLogger.Warning($"Loaded save exceeded max effective pins ({effectiveCount} > {RecipePinnerPlugin.MaximumPins.Value}) - trimmed {trimmed} effective pin(s)");
                }

                DebugLogger.Log($"Loaded {loadedCount} pins and {groupCount} groups from: {savePath} (Errors: {errorCount})");
            }
            catch (System.Exception ex)
            {
                DebugLogger.Error("Failed to load pins", ex);
            }
        }

        private void WriteAllLinesAtomically(string savePath, List<string> lines)
        {
            string directory = Path.GetDirectoryName(savePath);
            if (string.IsNullOrEmpty(directory))
                throw new IOException($"Invalid save directory for path: {savePath}");

            string fileName = Path.GetFileName(savePath);
            string tempPath = Path.Combine(directory, $"{fileName}.{System.Guid.NewGuid():N}.tmp");
            string backupPath = savePath + ".bak";

            try
            {
                File.WriteAllLines(tempPath, lines);

                if (File.Exists(savePath))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Replace(tempPath, savePath, backupPath, true);
                }
                else
                {
                    File.Move(tempPath, savePath);
                }
            }
            catch
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (System.Exception cleanupEx)
                {
                    DebugLogger.Warning($"Failed to delete temp save file '{tempPath}': {cleanupEx.Message}");
                }

                throw;
            }
        }

        private static int FindGroupSeparator(string groupContent)
        {
            int lastPipe = groupContent.LastIndexOf('|');
            if (lastPipe >= 0)
                return lastPipe;

            return -1;
        }

        private static string EscapeSaveValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value
                .Replace("%", "%25")
                .Replace("|", "%7C")
                .Replace(",", "%2C")
                .Replace("\r", "%0D")
                .Replace("\n", "%0A");
        }

        private static string UnescapeSaveValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('%') < 0)
                return value;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '%' && i + 2 < value.Length && IsHexDigit(value[i + 1]) && IsHexDigit(value[i + 2]))
                {
                    string hex = value.Substring(i + 1, 2);
                    sb.Append((char)System.Convert.ToInt32(hex, 16));
                    i += 2;
                }
                else
                {
                    sb.Append(value[i]);
                }
            }
            return sb.ToString();
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
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
