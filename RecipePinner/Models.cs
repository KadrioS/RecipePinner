using System.Collections.Generic;
using UnityEngine;

namespace ValheimRecipePinner
{
    public class PinnedRecipeData
    {
        public Recipe RecipeRef;
        public string RawName;
        public string CachedHeader;
        public string CachedShadowHeader;
        public Sprite Icon;
        public int StackCount;
        public List<PinnedResData> Resources = new List<PinnedResData>();
        public bool IsDirty = true;
    }

    public class PinnedResData
    {
        public string ItemName;
        public string CachedName;
        public string CachedShadowName;
        public Sprite Icon;
        public int RequiredAmount;
        public int LastKnownAmount;
        public int LastKnownInvAmount;
        public string CachedAmountString;
    }
}