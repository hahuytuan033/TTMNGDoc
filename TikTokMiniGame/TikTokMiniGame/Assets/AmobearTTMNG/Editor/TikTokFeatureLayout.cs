using TMPro;
using UnityEngine;

namespace AmobearTTMNG.EditorTools
{
    // Single source of the entry-button layout (1080x1920 reference): 160x160 squares in a column at the
    // top-right, 40px apart. The group is stretched over the screen and fitted to the safe area below the
    // TikTok capsule at runtime, so the first button never sits under the capsule on tall screens.
    // Used by Setup Feature UI and by the DevTools that regenerate the UI prefabs, so both stay identical.
    public static class TikTokFeatureLayout
    {
        public const float ButtonSize = 160f;
        public const float Spacing = 40f;
        public const float EdgeMargin = 20f;

        // Group that holds the buttons (controller root in Setup, "Buttons" in the prefabs).
        // SafeAreaFitter works in whole-screen ratios, so only fit when the group is a direct child of the Canvas.
        public static void ApplyGroup(RectTransform group, bool fitSafeArea)
        {
            group.anchorMin = Vector2.zero;
            group.anchorMax = Vector2.one;
            group.pivot = new Vector2(0.5f, 0.5f);
            group.offsetMin = group.offsetMax = Vector2.zero;
            if (fitSafeArea && !group.GetComponent<SafeAreaFitter>())
                group.gameObject.AddComponent<SafeAreaFitter>();
        }

        // Holder of the index-th feature (its EntryButton is stretched to fill it).
        public static void ApplyEntry(RectTransform holder, int index)
        {
            holder.anchorMin = holder.anchorMax = Vector2.one;
            holder.pivot = Vector2.one;
            holder.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            holder.anchoredPosition = new Vector2(-EdgeMargin, -EdgeMargin - index * (ButtonSize + Spacing));
        }

        // Entry button caption: wraps ("Add / Shortcut") and auto-sizes inside the square.
        public static void ApplyLabel(TMP_Text label)
        {
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 72f;
            label.enableWordWrapping = true;
            label.margin = Vector4.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
        }
    }
}
