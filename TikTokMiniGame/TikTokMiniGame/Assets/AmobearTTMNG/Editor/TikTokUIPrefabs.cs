using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmobearTTMNG.EditorTools
{
    /// <summary>
    /// UI prefabs của game = Prefab Variant của prefab gốc trong SDK, đặt ở Assets/AmobearTTMNGConfig/UI.
    /// Game sửa variant (override nằm trong project game); SDK cập nhật prefab gốc thì variant tự nhận phần chưa override.
    /// Khi SDK là git submodule: tự tạo variant còn thiếu sau mỗi lần compile, và khoá prefab gốc để không ai sửa nhầm.
    /// </summary>
    [InitializeOnLoad]
    internal static class TikTokUIPrefabs
    {
        public const string GameFolder = TikTokEditorPaths.ConfigFolder + "/UI";
        public static readonly string[] Names = { "TikTokFeatureCanvas", "TikTokFeatureUI", "Shortcut", "Sidebar", "Share" };

        private static string sdkRoot;
        private static bool? submodule;

        static TikTokUIPrefabs() => EditorApplication.delayCall += () => { if (IsSubmodule) EnsureVariants(false); };

        public static string SdkRoot => sdkRoot ??= TikTokFeatureSetup.SdkRoot;

        public static string BaseFolder => SdkRoot + "/UI/Prefabs";

        /// <summary>SDK nằm trong submodule (thư mục SDK có .git) — repo phát triển SDK thì không khoá, không tự tạo.</summary>
        public static bool IsSubmodule
        {
            get
            {
                if (submodule == null)
                {
                    string git = Path.Combine(TikTokEditorPaths.ProjectRoot, SdkRoot, ".git");
                    submodule = File.Exists(git) || Directory.Exists(git);
                }
                return submodule.Value;
            }
        }

        public static string BasePath(string name) => $"{BaseFolder}/{name}.prefab";

        public static string GamePath(string name) => $"{GameFolder}/{name}.prefab";

        /// <summary>Prefab của game nếu có, không thì prefab gốc trong SDK.</summary>
        public static GameObject Load(string name)
        {
            var game = AssetDatabase.LoadAssetAtPath<GameObject>(GamePath(name));
            return game ? game : AssetDatabase.LoadAssetAtPath<GameObject>(BasePath(name));
        }

        [MenuItem("AmobearTTMNG/Create Game UI Prefabs", false, 11)]
        private static void CreateFromMenu()
        {
            int created = EnsureVariants(true);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(GameFolder);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log($"[TikTok UI] {created} prefab mới trong {GameFolder}. Sửa UI ở đây, không sửa prefab trong SDK.");
        }

        /// <summary>Tạo variant còn thiếu (không ghi đè variant đã có). Trả số variant vừa tạo.</summary>
        public static int EnsureVariants(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return 0;
            var missing = Names.Where(n => !File.Exists(GamePath(n)) && AssetDatabase.LoadAssetAtPath<GameObject>(BasePath(n))).ToArray();
            if (missing.Length == 0)
                return 0;
            TikTokEditorPaths.EnsureFolder(GameFolder);
            // Preview scene: không làm bẩn scene đang mở.
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (string name in missing)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath(name)), preview);
                    PrefabUtility.SaveAsPrefabAsset(instance, GamePath(name));
                    Object.DestroyImmediate(instance);
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
            if (!force)
                Debug.Log($"[TikTok UI] Đã tạo {missing.Length} prefab UI cho game trong {GameFolder} (variant của prefab SDK).");
            return missing.Length;
        }

        public static bool IsLocked(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) && IsSubmodule && assetPath.Replace('\\', '/').StartsWith(BaseFolder + "/");

        /// <summary>Khoá prefab gốc trong submodule: Inspector bị khoá và Unity không lưu thay đổi vào đó.</summary>
        internal sealed class Lock : AssetModificationProcessor
        {
            private const string Message = "Prefab UI của SDK (submodule) — sửa variant trong " + GameFolder + " thay vì file này.";

            private static bool IsOpenForEdit(string assetPath, out string message)
            {
                bool locked = IsLocked(assetPath);
                message = locked ? Message : null;
                return !locked;
            }

            private static string[] OnWillSaveAssets(string[] paths)
            {
                var blocked = paths.Where(IsLocked).ToArray();
                if (blocked.Length == 0)
                    return paths;
                Debug.LogWarning("[TikTok UI] Không lưu thay đổi vào prefab SDK: " + string.Join(", ", blocked) + ". " + Message);
                return paths.Except(blocked).ToArray();
            }
        }
    }
}
