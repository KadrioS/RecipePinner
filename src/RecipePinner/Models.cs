using System.Collections.Generic;
using UnityEngine;

namespace ValheimRecipePinner
{
    public class PinnedRecipeData
    {
        public Recipe RecipeRef;
        public string RawName;
        public string CachedHeader;
        public Sprite Icon;
        public List<PinnedResData> Resources = new List<PinnedResData>();
        public bool IsDirty = true;
        public bool IsGroup = false;
        public PinGroupData GroupRef = null;
    }

    public class PinnedResData
    {
        public string ItemName;
        public string CachedName;
        public Sprite Icon;
        public int RequiredAmount;
        /// <summary>
        /// What one unit of the recipe costs, before the pinned count multiplies it. 0 means
        /// "no meaningful single-unit value" — group rows leave it 0 on purpose, because a
        /// group's RequiredAmount sums several different recipes.
        /// </summary>
        public int SingleAmount;
        public int LastKnownAmount;
        public int LastKnownInvAmount;
        public string CachedAmountString;
    }

    public class PinGroupData
    {
        public string GroupName;
        public List<string> MemberRecipeKeys = new List<string>();
        /// <summary>
        /// Per-member claim counts. Key = recipe key, Value = how many this group claims.
        /// Default 1 per member on group creation. Modified by sub-item +/-.
        /// </summary>
        public Dictionary<string, int> MemberCounts = new Dictionary<string, int>();
        public List<PinnedRecipeData> MemberPins = new List<PinnedRecipeData>();
        public List<PinnedResData> MergedResources = new List<PinnedResData>();
        public List<Sprite> MemberIcons = new List<Sprite>();
    }
}