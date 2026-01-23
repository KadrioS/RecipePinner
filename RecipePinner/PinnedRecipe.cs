using System.Collections.Generic;
using UnityEngine;

namespace RecipePinner
{
    public class PinnedRecipe
    {
        public Recipe Recipe { get; private set; }
        public int CurrentCount { get; set; }
        public string OverrideName { get; private set; }
        public List<Piece.Requirement> OverrideRequirements { get; private set; }
        public int TargetQuality { get; private set; }

        public PinnedRecipe(Recipe recipe, int count = 1, string overrideName = null, List<Piece.Requirement> overrideRequirements = null, int targetQuality = 0)
        {
            Recipe = recipe;
            CurrentCount = count;
            OverrideName = overrideName;
            OverrideRequirements = overrideRequirements;
            TargetQuality = targetQuality;
        }

        public string GetName()
        {
            if (!string.IsNullOrEmpty(OverrideName)) return OverrideName;
            return Recipe.m_item.m_itemData.m_shared.m_name;
        }

        public Sprite GetIcon()
        {
            return Recipe.m_item.m_itemData.m_shared.m_icons[0];
        }

        public Piece.Requirement[] GetRequirements()
        {
            if (OverrideRequirements != null) return OverrideRequirements.ToArray();
            return Recipe.m_resources;
        }
    }
}