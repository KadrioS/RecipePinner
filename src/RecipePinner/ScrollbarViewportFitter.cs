using UnityEngine;
using UnityEngine.UI;

namespace ValheimRecipePinner
{
    /// <summary>
    /// Dynamically adjusts viewport right-edge offset based on scrollbar visibility.
    /// When scrollbar is visible: viewport shrinks by scrollbarWidth so content doesn't hide behind bar.
    /// When scrollbar is hidden: viewport fills full width (no dead space).
    /// </summary>
    public class ScrollbarViewportFitter : MonoBehaviour
    {
        public Scrollbar VerticalScrollbar;
        public RectTransform Viewport;
        public float ScrollbarWidth = 10f;

        private bool _lastVisible = false;

        private void LateUpdate()
        {
            if (VerticalScrollbar == null || Viewport == null) return;

            // Scrollbar is visible when its size < 1 (content overflows)
            bool isVisible = VerticalScrollbar.gameObject.activeInHierarchy
                             && VerticalScrollbar.size < 0.999f;

            if (isVisible == _lastVisible) return;
            _lastVisible = isVisible;

            Vector2 offset = Viewport.offsetMax;
            offset.x = isVisible ? -ScrollbarWidth : 0f;
            Viewport.offsetMax = offset;
        }
    }
}
