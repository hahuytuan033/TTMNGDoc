#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Editor-only overlay of the TikTok mini game capsule ("•••  ✕") drawn over the Game view, so HUD in the top-right
// corner can be laid out clear of it. Never included in builds.
// Measured on Samsung A52 (1080x2400, 420dpi, status bar 88px): capsule 212x79px at x 852..1063, y 116..194.
[ExecuteAlways]
[AddComponentMenu("")]
public class TikTokCapsuleGuide : MonoBehaviour
{
    const string PrefKey = "TTMiniGame.ShowCapsuleGuide";
    const string MenuPath = "Tools/TT MiniGame/Show TikTok Capsule Guide";

    // Logical (dp) values from the measurement above. Screen logical width 1080 / 2.625 = 411.4dp.
    const float ReferenceWidthDp = 411.4f;
    const float CapsuleWidthDp = 81f;
    const float CapsuleHeightDp = 30f;
    const float CapsuleRightDp = 6f;
    const float CapsuleGapBelowStatusBarDp = 11f;
    const float FallbackStatusBarDp = 33.5f;
    // Keep-out margin around the capsule for game UI.
    const float KeepOutMarginDp = 8f;

    static TikTokCapsuleGuide instance;
    static Texture2D circleTexture;
    static Texture2D solidTexture;
    static GUIStyle glyphStyle, labelStyle;

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        Enabled = !Enabled;
        Ensure();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    [InitializeOnLoadMethod]
    static void InitEditor()
    {
        EditorApplication.delayCall += Ensure;
        EditorApplication.playModeStateChanged += _ => EditorApplication.delayCall += Ensure;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (_, __) => Ensure();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitPlayMode() => Ensure();

    static void Ensure()
    {
        // HideAndDontSave objects survive domain reloads but the static field does not, so look them up again
        // instead of trusting `instance`; otherwise every reload leaks one more overlay.
        var existing = Resources.FindObjectsOfTypeAll<TikTokCapsuleGuide>();
        if (!Enabled)
        {
            foreach (var guide in existing) DestroyImmediate(guide.gameObject);
            instance = null;
            return;
        }
        instance = existing.Length > 0 ? existing[0] : null;
        for (int i = 1; i < existing.Length; i++) DestroyImmediate(existing[i].gameObject);
        if (instance) return;
        var go = new GameObject("[TikTok Capsule Guide]") { hideFlags = HideFlags.HideAndDontSave };
        if (Application.isPlaying) DontDestroyOnLoad(go);
        instance = go.AddComponent<TikTokCapsuleGuide>();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void OnGUI()
    {
        if (!Enabled || Event.current.type != EventType.Repaint) return;
        EnsureStyles();
        GUI.depth = -1000;

        float dp = Screen.width / ReferenceWidthDp;
        // Device Simulator fills Screen.safeArea; otherwise use the measured status bar height.
        float statusBarPx = Screen.height - Screen.safeArea.yMax;
        if (statusBarPx < 1f) statusBarPx = FallbackStatusBarDp * dp;

        var capsule = new Rect(
            Screen.width - (CapsuleRightDp + CapsuleWidthDp) * dp,
            statusBarPx + CapsuleGapBelowStatusBarDp * dp,
            CapsuleWidthDp * dp,
            CapsuleHeightDp * dp);

        float margin = KeepOutMarginDp * dp;
        var keepOut = new Rect(capsule.x - margin, 0f, Screen.width - capsule.x + margin, capsule.yMax + margin);

        DrawSolid(keepOut, new Color(1f, 0.15f, 0.15f, 0.18f));
        DrawOutline(keepOut, new Color(1f, 0.2f, 0.2f, 0.9f), Mathf.Max(1f, dp * 0.5f));

        DrawPill(capsule, new Color(0.12f, 0.12f, 0.14f, 0.55f));

        var left = new Rect(capsule.x, capsule.y, capsule.width * 0.5f, capsule.height);
        var right = new Rect(capsule.center.x, capsule.y, capsule.width * 0.5f, capsule.height);
        DrawSolid(new Rect(capsule.center.x - 0.5f, capsule.y + capsule.height * 0.25f, 1f, capsule.height * 0.5f), new Color(1f, 1f, 1f, 0.35f));
        glyphStyle.fontSize = Mathf.RoundToInt(capsule.height * 0.55f);
        GUI.Label(left, "•••", glyphStyle);
        GUI.Label(right, "✕", glyphStyle);

        labelStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(dp * 7f));
        GUI.Label(new Rect(0f, keepOut.yMax + 2f, Screen.width - margin, labelStyle.fontSize * 1.6f), "TikTok capsule — keep UI out", labelStyle);
    }

    // Half-circle caps + middle strip, so the pill stays round at any Game view size.
    static void DrawPill(Rect r, Color c)
    {
        float cap = r.height * 0.5f;
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, cap, r.height), circleTexture, new Rect(0f, 0f, 0.5f, 1f));
        GUI.DrawTextureWithTexCoords(new Rect(r.xMax - cap, r.y, cap, r.height), circleTexture, new Rect(0.5f, 0f, 0.5f, 1f));
        GUI.DrawTexture(new Rect(r.x + cap, r.y, r.width - 2f * cap, r.height), solidTexture);
        GUI.color = old;
    }

    static void DrawSolid(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, solidTexture);
        GUI.color = old;
    }

    static void DrawOutline(Rect r, Color c, float t)
    {
        DrawSolid(new Rect(r.x, r.y, r.width, t), c);
        DrawSolid(new Rect(r.x, r.yMax - t, r.width, t), c);
        DrawSolid(new Rect(r.x, r.y, t, r.height), c);
        DrawSolid(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    static void EnsureStyles()
    {
        if (!solidTexture)
        {
            solidTexture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            solidTexture.SetPixel(0, 0, Color.white);
            solidTexture.Apply();
        }
        if (!circleTexture)
        {
            const int size = 64;
            const float radius = size * 0.5f;
            circleTexture = new Texture2D(size, size) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - radius, y + 0.5f - radius).magnitude;
                circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d)));
            }
            circleTexture.Apply();
        }
        if (glyphStyle == null)
        {
            glyphStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            glyphStyle.normal.textColor = Color.white;
            labelStyle = new GUIStyle { alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold };
            labelStyle.normal.textColor = new Color(1f, 0.3f, 0.3f, 1f);
        }
    }
}
#endif
