using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    public class PinSlotUI : MonoBehaviour
    {
        public RectTransform Rect;
        public Image IconImage;
        public Text HeaderText;
        public Transform ResourceListRoot;       // Normal vertical layout (icon + name + amount)
        public Transform CompactResourceRoot;    // Compact 4-col grid layout (icon + amount only)
        public Image AccentBar;
        public Transform GroupIconContainer;     // Replaces icon for groups (stacked cards)
        public Text GroupCountText;              // Shows member count inside the group icon

        public PinnedRecipeData CurrentData;
        public float LastSlotWidth = -1f; // Pin box width UpdateData last sized the compact grid for
        public bool LastUncap = false; // Whether UpdateData last rendered the compact grid uncapped (solo group)

        private Coroutine _layoutCoroutine;

        private Image _cachedBg;
        public Image BgImage => _cachedBg ? _cachedBg : (_cachedBg = GetComponent<Image>());

        private ContentSizeFitter _cachedCsf;
        public ContentSizeFitter Csf => _cachedCsf ? _cachedCsf : (_cachedCsf = GetComponent<ContentSizeFitter>());

        public List<ResourceSlotUI> ResourceSlots = new List<ResourceSlotUI>();
        public List<GatheringItemUI> CompactSlots = new List<GatheringItemUI>();

        private static int CompactThreshold =>
            RecipePinnerPlugin.GroupCompactThreshold?.Value ?? 4; // >threshold resources triggers compact mode

        private static int CompactMaxRows =>
            RecipePinnerPlugin.GroupCompactMaxRows?.Value ?? 3; // Beyond this the grid shows a "+N" cell instead of more rows
        private const int CompactOverflowFontSize = 22; // Larger, centred font for the "+N" overflow cell

        public void SetActive(bool active) => this.gameObject.SetActive(active);

        public void UpdateData(PinnedRecipeData data, Font font, float slotWidth, bool uncap)
        {
            // Icon: group = show stacked-cards container with count; regular = show recipe icon
            if (data.IsGroup && data.GroupRef != null && GroupIconContainer != null)
            {
                if (IconImage != null)
                    IconImage.gameObject.SetActive(false);
                GroupIconContainer.gameObject.SetActive(true);

                if (GroupCountText != null)
                    GroupCountText.text = data.GroupRef.MemberRecipeKeys.Count.ToString();
            }
            else
            {
                if (IconImage != null)
                    IconImage.gameObject.SetActive(true);
                if (GroupIconContainer != null)
                    GroupIconContainer.gameObject.SetActive(false);
                if (IconImage != null)
                    IconImage.sprite = data.Icon;
            }

            HeaderText.text = data.CachedHeader;

            HeaderText.font = font;
            HeaderText.fontSize = RecipePinnerPlugin.FontSizeRecipeName.Value;

            HeaderText.color = RecipePinnerPlugin.ColorHeader.Value;

            int resCount = data.Resources.Count;
            bool useCompact = data.IsGroup && resCount > CompactThreshold && CompactResourceRoot != null;

            // Toggle between normal and compact layouts
            if (ResourceListRoot != null)
                ResourceListRoot.gameObject.SetActive(!useCompact);
            if (CompactResourceRoot != null)
                CompactResourceRoot.gameObject.SetActive(useCompact);

            if (useCompact)
            {
                // Compact mode: grid identical to the Gathering List, but the column count is
                // derived from the pin box width instead of being fixed at 4, and the grid is
                // capped at CompactMaxRows so one large group cannot stretch the whole HUD row.
                int visibleCount = resCount;
                int overflow = 0;
                float compactCellHeight = 65f;
                int compactMaterialFontSize = RecipePinnerPlugin.GatheringListFontSizeMaterials != null
                    ? RecipePinnerPlugin.GatheringListFontSizeMaterials.Value : 15;

                GridLayoutGroup grid = CompactResourceRoot.GetComponent<GridLayoutGroup>();
                if (grid != null && slotWidth > 0f)
                {
                    VerticalLayoutGroup slotVlg = GetComponent<VerticalLayoutGroup>();
                    float padH = slotVlg != null ? slotVlg.padding.left + slotVlg.padding.right : 22f;
                    float cellW = grid.cellSize.x;
                    float spaceW = grid.spacing.x;
                    compactCellHeight = grid.cellSize.y;

                    // Allow a couple of pixels of overhang. Four columns need 244px and the
                    // default 265px box leaves 243px, so an exact fit test would return 3 and
                    // make groups taller than they are today. The overhang is centre-aligned
                    // and not visible.
                    const float fitTolerance = 4f;
                    int cols = Mathf.Max(1, Mathf.FloorToInt((slotWidth - padH + spaceW + fitTolerance) / (cellW + spaceW)));
                    if (grid.constraintCount != cols)
                        grid.constraintCount = cols;

                    int maxCells = cols * CompactMaxRows;
                    if (!uncap && resCount > maxCells)
                    {
                        // The counter needs a cell of its own, so one fewer material is shown.
                        visibleCount = maxCells - 1;
                        overflow = resCount - visibleCount;
                    }
                }

                int neededSlots = visibleCount + (overflow > 0 ? 1 : 0);

                while (CompactSlots.Count < neededSlots)
                {
                    CompactSlots.Add(UIBuilder.CreateGatheringItemSlot(CompactResourceRoot, font));
                }

                for (int i = 0; i < CompactSlots.Count; i++)
                {
                    if (CompactSlots[i] == null) continue;

                    if (i < visibleCount)
                    {
                        var res = data.Resources[i];
                        CompactSlots[i].SetActive(true);
                        if (CompactSlots[i].Icon != null)
                        {
                            // The overflow cell hides this icon, so a recycled slot must re-show it.
                            if (!CompactSlots[i].Icon.gameObject.activeSelf)
                                CompactSlots[i].Icon.gameObject.SetActive(true);
                            CompactSlots[i].Icon.sprite = res.Icon;
                        }
                        if (CompactSlots[i].AmountText != null)
                        {
                            // A recycled overflow cell left the amount enlarged and stretched;
                            // restore the material font size and height before reusing it.
                            if (CompactSlots[i].AmountText.fontSize != compactMaterialFontSize)
                            {
                                CompactSlots[i].AmountText.fontSize = compactMaterialFontSize;
                                LayoutElement amLe = CompactSlots[i].AmountText.GetComponent<LayoutElement>();
                                if (amLe != null)
                                    amLe.minHeight = 16f;
                            }
                            CompactSlots[i].AmountText.text = res.CachedAmountString ?? "";
                        }
                    }
                    else if (overflow > 0 && i == visibleCount)
                    {
                        // Overflow indicator: one "+N" cell. Hide the icon and enlarge the amount
                        // text, stretching it to the full cell height so "+N" reads centred
                        // instead of a small label floating where a material amount would sit.
                        CompactSlots[i].SetActive(true);
                        if (CompactSlots[i].Icon != null)
                            CompactSlots[i].Icon.gameObject.SetActive(false);
                        if (CompactSlots[i].AmountText != null)
                        {
                            CompactSlots[i].AmountText.text = "+" + overflow;
                            if (CompactSlots[i].AmountText.fontSize != CompactOverflowFontSize)
                            {
                                CompactSlots[i].AmountText.fontSize = CompactOverflowFontSize;
                                LayoutElement amLe = CompactSlots[i].AmountText.GetComponent<LayoutElement>();
                                if (amLe != null)
                                    amLe.minHeight = compactCellHeight;
                            }
                        }
                    }
                    else
                    {
                        CompactSlots[i].SetActive(false);
                    }
                }
            }
            else
            {
                // Normal mode: vertical list (icon + name + amount)
                while (ResourceSlots.Count < resCount)
                {
                    ResourceSlots.Add(UIBuilder.CreateResourceSlot(ResourceListRoot, font));
                }

                for (int i = 0; i < ResourceSlots.Count; i++)
                {
                    if (i < resCount)
                    {
                        ResourceSlots[i].SetActive(true);
                        ResourceSlots[i].UpdateResource(data.Resources[i]);
                    }
                    else
                        ResourceSlots[i].SetActive(false);
                }
            }

            if (this.gameObject.activeInHierarchy)
            {
                if (_layoutCoroutine != null) StopCoroutine(_layoutCoroutine);
                _layoutCoroutine = StartCoroutine(FixLayout());
            }
        }

        private void OnDisable()
        {
            _layoutCoroutine = null;
        }

        private System.Collections.IEnumerator FixLayout()
        {
            yield return null;

            if (ResourceListRoot != null && ResourceListRoot.gameObject.activeSelf)
                LayoutRebuilder.ForceRebuildLayoutImmediate(ResourceListRoot as RectTransform);
            if (CompactResourceRoot != null && CompactResourceRoot.gameObject.activeSelf)
                LayoutRebuilder.ForceRebuildLayoutImmediate(CompactResourceRoot as RectTransform);
            if (Rect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
            if (transform.parent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
        }
    }

    public class ResourceSlotUI : MonoBehaviour
    {
        public Image ResIcon;
        public Text ResName;
        public Text ResAmount;

        public void SetActive(bool active) => this.gameObject.SetActive(active);

        public void UpdateResource(PinnedResData res)
        {
            if (ResIcon != null)
                ResIcon.sprite = res.Icon;

            if (ResName != null)
                ResName.text = res.CachedName;
            if (ResAmount != null)
                ResAmount.text = res.CachedAmountString ?? "";

            if (RecipePinnerPlugin.FontSizeMaterials != null)
            {
                int newSize = RecipePinnerPlugin.FontSizeMaterials.Value;
                if (ResName != null)
                    ResName.fontSize = newSize;
                if (ResAmount != null)
                    ResAmount.fontSize = newSize;
            }
        }
    }
}
