using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public static class ReflectionHelper
    {
        private static Func<float> _getGuiScale;
        private static Func<InventoryGui, Transform> _getRecipeListRoot;
        private static Func<InventoryGui, object> _getAvailableRecipes;
        private static Func<InventoryGui, Container> _getCurrentContainer;
        private static Func<InventoryGui, Recipe> _getCraftRecipe;
        private static Func<InventoryGui, ItemDrop.ItemData> _getCraftUpgradeItem;
        private static Func<Hud, Piece> _getHoveredPiece;
        public static Func<Container, long, bool> CheckContainerAccess;
        private static FieldInfo _f_buildPieces;

        public static float currentGuiScaleValue = 1.0f;

        static ReflectionHelper()
        {
            InitializeReflection();
        }

        public static void InitializeReflection()
        {
            DebugLogger.Log("Reflection init");
            int successCount = 0;
            int failCount = 0;

            try
            {
                // GuiScaler
                FieldInfo f_scale = AccessTools.Field(typeof(GuiScaler), "m_largeGuiScale");
                if (f_scale != null && f_scale.IsStatic)
                {
                    _getGuiScale = System.Linq.Expressions.Expression.Lambda<Func<float>>(
                        System.Linq.Expressions.Expression.Field(null, f_scale)
                    ).Compile();
                    successCount++;
                    DebugLogger.Verbose("✓ GuiScaler.m_largeGuiScale");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ GuiScaler.m_largeGuiScale not found");
                }

                // InventoryGui - Recipe List Root
                FieldInfo f_root = AccessTools.Field(typeof(InventoryGui), "m_recipeListRoot");
                if (f_root != null)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(InventoryGui), "arg");
                    _getRecipeListRoot = System.Linq.Expressions.Expression.Lambda<Func<InventoryGui, Transform>>(
                        System.Linq.Expressions.Expression.Field(param, f_root), param
                    ).Compile();
                    successCount++;
                    DebugLogger.Verbose("✓ InventoryGui.m_recipeListRoot");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ InventoryGui.m_recipeListRoot not found");
                }

                // InventoryGui - Available Recipes
                FieldInfo f_recipes = AccessTools.Field(typeof(InventoryGui), "m_availableRecipes");
                if (f_recipes != null)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(InventoryGui), "arg");
                    _getAvailableRecipes = System.Linq.Expressions.Expression.Lambda<Func<InventoryGui, object>>(
                        System.Linq.Expressions.Expression.Field(param, f_recipes), param
                    ).Compile();
                    successCount++;
                    DebugLogger.Verbose("✓ InventoryGui.m_availableRecipes");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ InventoryGui.m_availableRecipes not found");
                }

                // InventoryGui - Current Container
                FieldInfo f_currCont = AccessTools.Field(typeof(InventoryGui), "m_currentContainer");
                if (f_currCont != null)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(InventoryGui), "arg");
                    _getCurrentContainer = System.Linq.Expressions.Expression.Lambda<Func<InventoryGui, Container>>(
                        System.Linq.Expressions.Expression.Field(param, f_currCont), param
                    ).Compile();
                    successCount++;
                    DebugLogger.Verbose("✓ InventoryGui.m_currentContainer");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ InventoryGui.m_currentContainer not found");
                }

                // InventoryGui - Craft Recipe
                FieldInfo f_craftRecipe = AccessTools.Field(typeof(InventoryGui), "m_craftRecipe");
                if (f_craftRecipe != null)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(InventoryGui), "arg");
                    _getCraftRecipe = System.Linq.Expressions.Expression.Lambda<Func<InventoryGui, Recipe>>(
                        System.Linq.Expressions.Expression.Field(param, f_craftRecipe), param
                    ).Compile();
                    successCount++;
                    DebugLogger.Verbose("✓ InventoryGui.m_craftRecipe");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ InventoryGui.m_craftRecipe not found");
                }

                FieldInfo f_upgradeItem = AccessTools.Field(typeof(InventoryGui), "m_craftUpgradeItem");
                if (f_upgradeItem != null)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(InventoryGui), "arg");
                    _getCraftUpgradeItem = System.Linq.Expressions.Expression.Lambda<Func<InventoryGui, ItemDrop.ItemData>>(
                        System.Linq.Expressions.Expression.Field(param, f_upgradeItem), param
                    ).Compile();
                    successCount++;
                    DebugLogger.Verbose("✓ InventoryGui.m_craftUpgradeItem");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ InventoryGui.m_craftUpgradeItem not found");
                }

                // Hud - Hovered Piece
                FieldInfo f_hovered = AccessTools.Field(typeof(Hud), "m_hoveredPiece");
                if (f_hovered != null)
                {
                    var param = System.Linq.Expressions.Expression.Parameter(typeof(Hud), "arg");
                    _getHoveredPiece = System.Linq.Expressions.Expression.Lambda<Func<Hud, Piece>>(
                        System.Linq.Expressions.Expression.Field(param, f_hovered), param
                    ).Compile();
                    successCount++;
                    DebugLogger.Verbose("✓ Hud.m_hoveredPiece");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ Hud.m_hoveredPiece not found");
                }

                // Container - Check Access
                MethodInfo m_check = AccessTools.Method(typeof(Container), "CheckAccess", new Type[] { typeof(long) });
                if (m_check != null)
                {
                    CheckContainerAccess = AccessTools.MethodDelegate<Func<Container, long, bool>>(m_check);
                    successCount++;
                    DebugLogger.Verbose("✓ Container.CheckAccess");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ Container.CheckAccess not found");
                }

                // Player - Build Pieces (PieceTable)
                _f_buildPieces = AccessTools.Field(typeof(Player), "m_buildPieces");
                if (_f_buildPieces != null)
                {
                    successCount++;
                    DebugLogger.Verbose("✓ Player.m_buildPieces");
                }
                else
                {
                    failCount++;
                    DebugLogger.Warning("✗ Player.m_buildPieces not found");
                }

                DebugLogger.Log($"Reflection done: {successCount} ok, {failCount} failed");

                if (failCount > 0)
                {
                    DebugLogger.Warning("Some reflection targets failed");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error("Reflection init failed", ex);
            }
        }

        public static void UpdateGuiScale()
        {
            if (_getGuiScale != null)
                currentGuiScaleValue = _getGuiScale();
            else
                currentGuiScaleValue = 1.0f;
        }

        public static Transform GetRecipeListRoot(InventoryGui gui)
        {
            if (_getRecipeListRoot == null)
            {
                DebugLogger.Warning("GetRecipeListRoot delegate is null");
                return null;
            }
            return _getRecipeListRoot(gui);
        }

        public static object GetAvailableRecipes(InventoryGui gui)
        {
            if (_getAvailableRecipes == null)
            {
                DebugLogger.Warning("GetAvailableRecipes delegate is null");
                return null;
            }
            return _getAvailableRecipes(gui);
        }

        public static Container GetCurrentContainer(InventoryGui gui)
        {
            if (_getCurrentContainer == null)
            {
                DebugLogger.Verbose("GetCurrentContainer delegate is null");
                return null;
            }
            return _getCurrentContainer(gui);
        }

        public static Recipe GetCraftRecipe(InventoryGui gui)
        {
            if (_getCraftRecipe == null)
            {
                DebugLogger.Warning("GetCraftRecipe delegate is null");
                return null;
            }
            return _getCraftRecipe(gui);
        }

        public static ItemDrop.ItemData GetCraftUpgradeItem(InventoryGui gui)
        {
            return _getCraftUpgradeItem?.Invoke(gui);
        }

        public static Piece GetHoveredPiece(Hud hud)
        {
            if (_getHoveredPiece == null)
            {
                DebugLogger.Verbose("GetHoveredPiece delegate is null");
                return null;
            }
            return _getHoveredPiece(hud);
        }

        public static PieceTable GetPieceTable(Player player)
        {
            if (_f_buildPieces == null || player == null) return null;
            return _f_buildPieces.GetValue(player) as PieceTable;
        }
    }

    public static class InputHelper
    {
        public static bool IsInputBlocked()
        {
            if (Console.IsVisible())
            {
                return true;
            }

            if (Chat.instance != null && Chat.instance.HasFocus())
            {
                return true;
            }

            if (TextInput.IsVisible())
            {
                DebugLogger.Verbose("Input blocked: TextInput is visible");
                return true;
            }

            return false;
        }

        public static bool IsMouseOverRect(RectTransform rect)
        {
            if (rect == null)
            {
                DebugLogger.Verbose("IsMouseOverRect: rect is null");
                return false;
            }

            bool result = RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition);

            if (result)
                DebugLogger.Verbose($"Mouse over rect: {rect.gameObject.name}");

            return result;
        }
    }
}