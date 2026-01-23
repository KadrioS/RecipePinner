using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class RecipeManager
    {
        public Dictionary<string, int> PinnedRecipes = new Dictionary<string, int>();
        public List<PinnedRecipeData> CachedPins = new List<PinnedRecipeData>();

        private Dictionary<string, Recipe> _fakeRecipeCache = new Dictionary<string, Recipe>();

        // Regexler (Compiled = Performanslı)
        private static readonly Regex CleanNameRegex = new Regex("<.*?>", RegexOptions.Compiled);
        private static readonly Regex ShadowCleanRegex = new Regex("<color=.*?>|</color>", RegexOptions.Compiled);
        private static readonly Regex AmountSuffixRegex = new Regex(@"\s*[xX]?\s*\d+$", RegexOptions.Compiled);
        private static readonly Regex UpgradeStarRegex = new Regex(@"\s*★(\d+)$", RegexOptions.Compiled);

        private static Dictionary<System.Type, FieldInfo> _cachedRecipeFields = new Dictionary<System.Type, FieldInfo>();
        private static Dictionary<System.Type, PropertyInfo> _cachedRecipeProps = new Dictionary<System.Type, PropertyInfo>();
        private static Dictionary<System.Type, FieldInfo> _cachedItemFields = new Dictionary<System.Type, FieldInfo>();
        private static Dictionary<System.Type, PropertyInfo> _cachedItemProps = new Dictionary<System.Type, PropertyInfo>();

        public void Cleanup()
        {
            DebugLogger.Log("RecipeManager cleanup started");
            if (_fakeRecipeCache != null)
            {
                int count = _fakeRecipeCache.Count;
                foreach (var recipe in _fakeRecipeCache.Values)
                {
                    if (recipe != null) UnityEngine.Object.Destroy(recipe);
                }
                _fakeRecipeCache.Clear();
                DebugLogger.Log($"Cleaned up {count} fake recipes");
            }

            _cachedRecipeFields.Clear();
            _cachedRecipeProps.Clear();
            _cachedItemFields.Clear();
            _cachedItemProps.Clear();
        }

        public void RefreshRecipeCache()
        {
            DebugLogger.Verbose("Refreshing recipe cache...");
            CachedPins.Clear();

            if (ObjectDB.instance == null)
            {
                DebugLogger.Warning("Cannot refresh recipe cache - ObjectDB.instance is null");
                return;
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var kvp in PinnedRecipes)
            {
                string recipeName = kvp.Key;
                int count = kvp.Value;
                Recipe r = GetRecipeByName(recipeName);

                if (r != null)
                {
                    PinnedRecipeData data = new PinnedRecipeData
                    {
                        IsDirty = true,
                        RecipeRef = r,
                        StackCount = count
                    };

                    if (r.m_item != null)
                    {
                        data.Icon = r.m_item.m_itemData.GetIcon();
                        data.RawName = r.m_item.m_itemData.m_shared.m_name;
                    }
                    else
                    {
                        GameObject prefab = ZNetScene.instance?.GetPrefab(r.name);
                        if (prefab != null)
                        {
                            Piece p = prefab.GetComponent<Piece>();
                            if (p != null)
                            {
                                data.Icon = p.m_icon;
                                data.RawName = p.m_name;
                            }
                        }
                        else if (UpgradeStarRegex.IsMatch(recipeName) && r.m_item != null)
                        {
                            data.Icon = r.m_item.m_itemData.GetIcon();
                            data.RawName = r.name;
                        }
                    }

                    if (string.IsNullOrEmpty(data.RawName)) data.RawName = r.name;

                    string displayName = data.RawName;

                    if (Localization.instance != null)
                    {
                        Match starMatch = UpgradeStarRegex.Match(recipeName);
                        if (starMatch.Success)
                        {
                            string baseName = data.RawName;
                            if (Localization.instance != null) baseName = Localization.instance.Localize(data.RawName);
                            displayName = baseName + starMatch.Value;
                        }
                        else
                        {
                            displayName = Localization.instance.Localize(data.RawName);
                        }
                    }

                    displayName = displayName.Replace("\r", "").Replace("\n", "");

                    if (r.m_amount > 1) displayName += $" (x{r.m_amount})";
                    if (count > 1) displayName = $"{count}x {displayName}";

                    data.CachedHeader = displayName;
                    data.CachedShadowHeader = displayName;

                    foreach (var res in r.m_resources)
                    {
                        if (res == null || res.m_resItem == null || res.m_amount <= 0) continue;

                        PinnedResData resData = new PinnedResData
                        {
                            ItemName = res.m_resItem.m_itemData.m_shared.m_name,
                            Icon = res.m_resItem.m_itemData.GetIcon(),
                            RequiredAmount = res.m_amount * count,
                            LastKnownAmount = -1,
                            LastKnownInvAmount = -1
                        };

                        string matName = resData.ItemName;
                        if (Localization.instance != null) matName = Localization.instance.Localize(resData.ItemName);
                        matName = matName.Replace("\r", "").Replace("\n", "");
                        resData.CachedName = matName;
                        resData.CachedShadowName = ShadowCleanRegex.Replace(matName, string.Empty);
                        data.Resources.Add(resData);
                    }
                    CachedPins.Add(data);
                    successCount++;
                }
                else
                {
                    DebugLogger.Warning($"Recipe not found: {recipeName}");
                    failCount++;
                }
            }

            DebugLogger.Log($"Recipe cache refreshed: {successCount} successful, {failCount} failed");

            if (Player.m_localPlayer != null && RecipePinnerPlugin.Instance != null)
                RecipePinnerPlugin.Instance.UIMgr.UpdateUI(true);
        }

        public Recipe GetRecipeByName(string name)
        {
            if (ObjectDB.instance == null) return null;

            if (_fakeRecipeCache.TryGetValue(name, out Recipe foundCachedRecipe))
            {
                DebugLogger.Verbose($"Found cached fake recipe: {name}");
                return foundCachedRecipe;
            }

            // Upgrade Check
            Match starMatch = UpgradeStarRegex.Match(name);
            if (starMatch.Success)
            {
                string baseName = name.Substring(0, starMatch.Index).Trim();
                int targetLevel = int.Parse(starMatch.Groups[1].Value);

                Recipe baseRecipe = GetRecipeByName(baseName);
                if (baseRecipe != null)
                {
                    Recipe upgradeRecipe = CreateFakeUpgradeRecipe(baseRecipe, targetLevel, name);
                    if (upgradeRecipe != null) return upgradeRecipe;
                }
            }

            // Standard Item Check
            ItemDrop itemDrop = ObjectDB.instance.GetItemPrefab(name)?.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                Recipe result = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
                if (result != null)
                {
                    DebugLogger.Verbose($"Found standard recipe: {name}");
                    return result;
                }
            }

            // Recipe List Check
            Recipe standardRecipe = ObjectDB.instance.m_recipes.FirstOrDefault(r => r.name == name);
            if (standardRecipe != null)
            {
                DebugLogger.Verbose($"Found recipe in ObjectDB: {name}");
                return standardRecipe;
            }

            // Piece Check
            GameObject prefab = ZNetScene.instance?.GetPrefab(name);
            if (prefab != null)
            {
                Piece piece = prefab.GetComponent<Piece>();
                if (piece != null && piece.m_resources != null && piece.m_resources.Length > 0)
                {
                    Recipe fakeRecipe = ScriptableObject.CreateInstance<Recipe>();
                    fakeRecipe.hideFlags = HideFlags.HideAndDontSave;
                    fakeRecipe.name = name;
                    fakeRecipe.m_item = prefab.GetComponent<ItemDrop>();
                    List<Piece.Requirement> reqs = piece.m_resources.ToList();
                    fakeRecipe.m_resources = reqs.ToArray();
                    _fakeRecipeCache[name] = fakeRecipe;
                    DebugLogger.Verbose($"Created fake recipe for piece: {name}");
                    return fakeRecipe;
                }
            }

            DebugLogger.Warning($"Recipe not found anywhere: {name}");
            return null;
        }

        private Recipe CreateFakeUpgradeRecipe(Recipe baseRecipe, int targetLevel, string customName)
        {
            if (baseRecipe == null) return null;

            Recipe fakeRecipe = ScriptableObject.CreateInstance<Recipe>();
            fakeRecipe.hideFlags = HideFlags.HideAndDontSave;
            fakeRecipe.name = customName;
            fakeRecipe.m_item = baseRecipe.m_item;
            fakeRecipe.m_amount = 1;

            List<Piece.Requirement> upgradeReqs = new List<Piece.Requirement>();
            foreach (var req in baseRecipe.m_resources)
            {
                if (req.m_amountPerLevel > 0)
                {
                    Piece.Requirement newReq = new Piece.Requirement
                    {
                        m_resItem = req.m_resItem,
                        m_amount = req.m_amountPerLevel,
                        m_amountPerLevel = 0,
                        m_recover = req.m_recover
                    };
                    upgradeReqs.Add(newReq);
                }
            }

            if (upgradeReqs.Count == 0)
            {
                UnityEngine.Object.Destroy(fakeRecipe);
                return null;
            }

            fakeRecipe.m_resources = upgradeReqs.ToArray();
            _fakeRecipeCache[customName] = fakeRecipe;
            DebugLogger.Verbose($"Created fake upgrade recipe: {customName}");
            return fakeRecipe;
        }

        public void ValidateAndCleanPins()
        {
            if (ObjectDB.instance == null)
            {
                DebugLogger.Warning("Cannot validate pins - ObjectDB.instance is null");
                return;
            }

            DebugLogger.Log("Validating pinned recipes...");
            List<string> keysToRemove = new List<string>();
            foreach (var recipeName in PinnedRecipes.Keys)
            {
                if (GetRecipeByName(recipeName) == null) keysToRemove.Add(recipeName);
            }

            if (keysToRemove.Count > 0)
            {
                foreach (string key in keysToRemove)
                {
                    PinnedRecipes.Remove(key);
                    DebugLogger.Warning($"Removed invalid recipe: {key}");
                }

                if (RecipePinnerPlugin.Instance != null)
                    RecipePinnerPlugin.Instance.DataMgr.SavePins();

                DebugLogger.Log($"Validation complete: {keysToRemove.Count} invalid recipes removed");
            }
            else
            {
                DebugLogger.Log("All pinned recipes are valid");
            }
        }

        public void TryPinHoveredRecipe(InventoryGui gui)
        {
            DebugLogger.Verbose("Attempting to pin hovered recipe...");

            Transform listRoot = ReflectionHelper.GetRecipeListRoot(gui);
            object rawList = ReflectionHelper.GetAvailableRecipes(gui);
            System.Collections.IList availableRecipes = rawList as System.Collections.IList;

            if (listRoot == null || availableRecipes == null)
            {
                DebugLogger.Verbose("Cannot pin - listRoot or availableRecipes is null");
                return;
            }

            ScrollRect scrollRect = listRoot.GetComponentInParent<ScrollRect>();
            bool isUpgradeTab = !gui.m_tabUpgrade.interactable;

            foreach (Transform child in listRoot)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                RectTransform itemRect = child as RectTransform;
                if (itemRect == null) continue;
                if (!IsVisibleInScroll(itemRect, scrollRect)) continue;

                if (InputHelper.IsMouseOverRect(itemRect))
                {
                    string foundText = ExtractTextFromUI(child);
                    if (!string.IsNullOrEmpty(foundText))
                    {
                        string cleanScreenName = CleanNameRegex.Replace(foundText, string.Empty).Trim();
                        cleanScreenName = cleanScreenName.Replace("\r", "").Replace("\n", "");
                        string pureName = AmountSuffixRegex.Replace(cleanScreenName, "").Trim();

                        DebugLogger.Verbose($"Hovered: '{pureName}' (UpgradeTab: {isUpgradeTab})");

                        foreach (object itemObj in availableRecipes)
                        {
                            Recipe r = GetRecipeFromObject(itemObj);

                            if (r != null)
                            {
                                string rawName = GetRawRecipeName(r);
                                if (string.IsNullOrEmpty(rawName)) continue;

                                string localizedRecipeName = rawName;
                                if (Localization.instance != null)
                                    localizedRecipeName = Localization.instance.Localize(rawName);

                                localizedRecipeName = localizedRecipeName.Replace("\r", "").Replace("\n", "");

                                if (localizedRecipeName.Equals(pureName, System.StringComparison.OrdinalIgnoreCase) ||
                                    localizedRecipeName.Equals(cleanScreenName, System.StringComparison.OrdinalIgnoreCase))
                                {
                                    if (isUpgradeTab)
                                    {
                                        ItemDrop.ItemData itemData = GetItemDataFromObject(itemObj);
                                        if (itemData == null) itemData = ReflectionHelper.GetCraftUpgradeItem(gui);

                                        if (itemData != null)
                                        {
                                            int currentQ = itemData.m_quality;
                                            int nextQ = currentQ + 1;
                                            int maxQ = itemData.m_shared.m_maxQuality;

                                            if (currentQ >= maxQ)
                                            {
                                                string msg = RecipePinnerPlugin.Instance.LocalizationMgr.GetText("max_level");
                                                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                                                return;
                                            }

                                            string prefabName = r.m_item.name;
                                            string upgradeId = $"{prefabName} ★{nextQ}";

                                            DebugLogger.Log($"Attempting to pin upgrade: {upgradeId} (Base: {prefabName})");

                                            if (GetRecipeByName(upgradeId) != null)
                                            {
                                                TogglePin(upgradeId);
                                            }
                                            else
                                            {
                                                string msg = RecipePinnerPlugin.Instance.LocalizationMgr.GetText("no_upgrade_cost");
                                                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                                            }
                                        }
                                        else
                                        {
                                            DebugLogger.Warning($"Matched name but could not get ItemData for upgrade.");
                                        }
                                    }
                                    else
                                    {
                                        DebugLogger.Log($"Matched recipe: {r.name}");
                                        TogglePin(r.name);
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void TryPinHoveredPiece()
        {
            DebugLogger.Verbose("Attempting to pin hovered piece...");
            if (Hud.instance == null) return;
            Piece targetPiece = ReflectionHelper.GetHoveredPiece(Hud.instance);
            if (targetPiece != null && targetPiece.m_resources != null && targetPiece.m_resources.Length > 0)
            {
                DebugLogger.Log($"Pinning piece: {targetPiece.name}");
                TogglePin(targetPiece.name);
            }
            else
            {
                DebugLogger.Verbose("No valid piece to pin (Mouse must be over a recipe icon)");
            }
        }

        private void TogglePin(string recipeName)
        {
            bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var locMgr = RecipePinnerPlugin.Instance.LocalizationMgr;

            if (PinnedRecipes.ContainsKey(recipeName))
            {
                if (isShiftHeld)
                {
                    PinnedRecipes[recipeName]--;
                    if (PinnedRecipes[recipeName] <= 0)
                    {
                        PinnedRecipes.Remove(recipeName);
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("unpinned"));
                        DebugLogger.Log($"Unpinned: {recipeName}");
                    }
                    else
                    {
                        string msg = string.Format(locMgr.GetText("decreased"), PinnedRecipes[recipeName]);
                        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                        DebugLogger.Log($"Decreased pin count: {recipeName} = {PinnedRecipes[recipeName]}");
                    }
                }
                else
                {
                    PinnedRecipes[recipeName]++;
                    string msg = string.Format(locMgr.GetText("added_more"), PinnedRecipes[recipeName]);
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center, msg);
                    DebugLogger.Log($"Increased pin count: {recipeName} = {PinnedRecipes[recipeName]}");
                }
            }
            else
            {
                if (isShiftHeld) return;
                if (PinnedRecipes.Count < RecipePinnerPlugin.MaximumPins.Value)
                {
                    PinnedRecipes.Add(recipeName, 1);
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("pinned"));
                    DebugLogger.Log($"Pinned new recipe: {recipeName}");
                }
                else
                {
                    Player.m_localPlayer?.Message(MessageHud.MessageType.Center, locMgr.GetText("list_full"));
                    DebugLogger.Warning($"Cannot pin {recipeName} - max pins reached ({RecipePinnerPlugin.MaximumPins.Value})");
                }
            }
            RefreshRecipeCache();
        }


        private Recipe GetRecipeFromObject(object data)
        {
            if (data == null) return null;
            if (data is Recipe r) return r;

            System.Type type = data.GetType();

            if (_cachedRecipeFields.TryGetValue(type, out FieldInfo cachedField))
            {
                return cachedField.GetValue(data) as Recipe;
            }
            if (_cachedRecipeProps.TryGetValue(type, out PropertyInfo cachedProp))
            {
                return cachedProp.GetValue(data, null) as Recipe;
            }
            PropertyInfo keyProp = type.GetProperty("Key");
            if (keyProp != null && keyProp.GetValue(data, null) is Recipe keyRecipe)
            {
                _cachedRecipeProps[type] = keyProp;
                return keyRecipe;
            }

            FieldInfo mRecipeField = type.GetField("m_recipe", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mRecipeField != null && mRecipeField.GetValue(data) is Recipe fRecipe)
            {
                _cachedRecipeFields[type] = mRecipeField;
                return fRecipe;
            }

            // Universal Scan
            foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(Recipe))
                {
                    _cachedRecipeFields[type] = f;
                    return f.GetValue(data) as Recipe;
                }
            }

            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.PropertyType == typeof(Recipe) && p.CanRead)
                {
                    _cachedRecipeProps[type] = p;
                    return p.GetValue(data, null) as Recipe;
                }
            }

            return null;
        }

        private ItemDrop.ItemData GetItemDataFromObject(object data)
        {
            if (data == null) return null;

            System.Type type = data.GetType();

            if (_cachedItemFields.TryGetValue(type, out FieldInfo cachedField))
                return cachedField.GetValue(data) as ItemDrop.ItemData;
            if (_cachedItemProps.TryGetValue(type, out PropertyInfo cachedProp))
                return cachedProp.GetValue(data, null) as ItemDrop.ItemData;

            PropertyInfo valProp = type.GetProperty("Value");
            if (valProp != null && valProp.GetValue(data, null) is ItemDrop.ItemData valItem)
            {
                _cachedItemProps[type] = valProp;
                return valItem;
            }

            PropertyInfo item2Prop = type.GetProperty("Item2");
            if (item2Prop != null && item2Prop.GetValue(data, null) is ItemDrop.ItemData item2Item)
            {
                _cachedItemProps[type] = item2Prop;
                return item2Item;
            }

            foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(ItemDrop.ItemData))
                {
                    _cachedItemFields[type] = f;
                    return f.GetValue(data) as ItemDrop.ItemData;
                }
            }

            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (p.PropertyType == typeof(ItemDrop.ItemData) && p.CanRead)
                {
                    _cachedItemProps[type] = p;
                    return p.GetValue(data, null) as ItemDrop.ItemData;
                }
            }

            return null;
        }

        private string ExtractTextFromUI(Transform child)
        {
            Text classicText = child.GetComponentInChildren<Text>();
            if (classicText != null) return classicText.text;

            Component[] allComps = child.GetComponentsInChildren<Component>(true);
            foreach (var comp in allComps)
            {
                if (comp.GetType().Name.Contains("TextMeshPro") || comp.GetType().Name.Contains("TMP_Text"))
                {
                    PropertyInfo textProp = comp.GetType().GetProperty("text");
                    if (textProp != null)
                    {
                        string foundText = textProp.GetValue(comp, null) as string;
                        if (!string.IsNullOrEmpty(foundText)) return foundText;
                    }
                }
            }
            return null;
        }

        private string GetRawRecipeName(Recipe r)
        {
            if (r.m_item != null && r.m_item.m_itemData != null)
                return r.m_item.m_itemData.m_shared.m_name;

            GameObject prefab = ZNetScene.instance?.GetPrefab(r.name);
            if (prefab != null)
            {
                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop != null) return drop.m_itemData.m_shared.m_name;
                Piece piece = prefab.GetComponent<Piece>();
                if (piece != null) return piece.m_name;
            }
            return null;
        }

        private bool IsVisibleInScroll(RectTransform item, ScrollRect scrollRect)
        {
            if (item == null || !item.gameObject.activeInHierarchy) return false;
            if (scrollRect == null || scrollRect.viewport == null) return true;

            Vector3[] vC = new Vector3[4];
            scrollRect.viewport.GetWorldCorners(vC);
            Rect vRect = new Rect(vC[0].x, vC[0].y, vC[2].x - vC[0].x, vC[2].y - vC[0].y);

            Vector3[] iC = new Vector3[4];
            item.GetWorldCorners(iC);
            Vector3 center = (iC[0] + iC[2]) / 2;

            return vRect.Contains(center);
        }
    }
}