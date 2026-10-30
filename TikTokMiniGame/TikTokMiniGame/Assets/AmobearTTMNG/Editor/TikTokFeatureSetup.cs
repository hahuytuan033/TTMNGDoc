using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using AmobearTTMNG.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AmobearTTMNG.EditorTools
{
    public sealed class TikTokFeatureSetup : EditorWindow
    {
        [SerializeField] private RectTransform parent;
        [SerializeField] private bool shortcut = true, sidebar = true, share = true, autoClaim = true;
        private string message;

        [MenuItem("AmobearTTMNG/Setup Feature UI", false, 10)]
        public static void OpenWindow()
        {
            var window = GetWindow<TikTokFeatureSetup>(true, "Setup TikTok Feature UI");
            window.minSize = new Vector2(450, 300);
            window.parent = Selection.activeTransform as RectTransform;
            window.Show();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Select TikTokFeatureCanvas prefab (drag into scene)"))
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(SdkRoot + "/UI/Prefabs/TikTokFeatureCanvas.prefab");
            if (GUILayout.Button("Kết nối UI có sẵn / Repair References")) TikTokFeatureConnect.OpenWindow();
            if (GUILayout.Button("Select complete TikTokFeatureUI prefab"))
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(SdkRoot + "/UI/Prefabs/TikTokFeatureUI.prefab");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene: " + SceneManager.GetActiveScene().name, EditorStyles.boldLabel);
            parent = (RectTransform)EditorGUILayout.ObjectField("Canvas / UI parent", parent, typeof(RectTransform), true);
            shortcut = EditorGUILayout.Toggle("Shortcut", shortcut);
            sidebar = EditorGUILayout.Toggle("Revisit Sidebar", sidebar);
            share = EditorGUILayout.Toggle("Share", share);
            autoClaim = EditorGUILayout.Toggle("Tự mở popup Claim", autoClaim);
            EditorGUILayout.HelpBox("Thêm UI mẫu vào scene đang active. Chỉ thêm tính năng còn thiếu; giữ nguyên các chỉnh sửa cũ. Gán OnRewardClaimed trong Inspector trước khi dùng Claim. Scene sẽ không tự lưu.", MessageType.Info);
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Warning);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                if (GUILayout.Button("Add to Current Scene", GUILayout.Height(32)))
                {
                    try
                    {
                        var kinds = new[] { shortcut, sidebar, share }.Select((on, i) => new { on, i }).Where(x => x.on).Select(x => (TikTokUIFeature)x.i).ToArray();
                        var ui = AddToScene(parent, kinds, autoClaim);
                        Selection.activeGameObject = ui.gameObject;
                        EditorGUIUtility.PingObject(ui.gameObject);
                        message = "Đã setup. Mở từng Feature trong Inspector để gán hàm thưởng; lưu scene khi hoàn tất.";
                    }
                    catch (Exception e) { message = e.Message; Debug.LogWarning("[TikTok UI Setup] " + message); }
                }
        }

        internal static string SdkRoot
        {
            get
            {
                string script = AssetDatabase.FindAssets("TikTokFeatureSetup t:MonoScript").Select(AssetDatabase.GUIDToAssetPath)
                    .First(p => AssetDatabase.LoadAssetAtPath<MonoScript>(p)?.GetClass() == typeof(TikTokFeatureSetup));
                return Path.GetDirectoryName(Path.GetDirectoryName(script)).Replace('\\', '/');
            }
        }

        internal static TikTokFeatureUI AddToScene(RectTransform parent, TikTokUIFeature[] selected, bool autoClaim)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Hãy thoát Play Mode / Prefab Stage và mở scene game.");
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Chưa có scene active.");
            if (selected == null || selected.Length == 0) throw new InvalidOperationException("Chọn ít nhất một tính năng.");
            if (selected.Any(x => !Enum.IsDefined(typeof(TikTokUIFeature), x))) throw new InvalidOperationException("Tính năng không hợp lệ.");
            var controllers = Components<TikTokFeatureUI>(scene);
            if (controllers.Length > 1) throw new InvalidOperationException("Scene đã có nhiều TikTokFeatureUI. Giữ một controller trước khi setup.");
            var existing = controllers.FirstOrDefault();
            if (existing && (existing.Features.Any(v => !v || !v.HasReferences) || !existing.PopupsRoot))
                throw new InvalidOperationException("Bộ UI hiện có thiếu tham chiếu. Sửa lỗi trong Validate Project trước khi thêm.");
            if (existing && existing.Features.GroupBy(v => v.Feature).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Bộ UI hiện có tính năng trùng.");
            if (parent && (EditorUtility.IsPersistent(parent) || parent.gameObject.scene != scene || !parent.GetComponentInParent<Canvas>(true)))
                throw new InvalidOperationException("Parent phải nằm dưới Canvas trong scene active.");
            if (existing) parent = existing.transform.parent as RectTransform;
            Canvas canvas = parent ? parent.GetComponentInParent<Canvas>(true) : null;
            if (!canvas)
            {
                var canvases = Components<Canvas>(scene).Where(c => c.isRootCanvas).ToArray();
                if (canvases.Length > 1) throw new InvalidOperationException("Scene có nhiều Canvas. Hãy chọn Canvas hoặc UI parent.");
                canvas = canvases.FirstOrDefault();
                if (canvas) parent = (RectTransform)canvas.transform;
            }
            var missing = selected.Distinct().Where(k => !existing || !existing.Features.Any(v => v && v.Feature == k)).ToArray();
            var templates = missing.Select(k => AssetDatabase.LoadAssetAtPath<GameObject>($"{SdkRoot}/UI/Prefabs/{k}.prefab")).ToArray();
            foreach (var template in templates)
            {
                if (!template || !template.GetComponent<TikTokFeatureView>() || !template.GetComponent<TikTokFeatureView>().HasReferences)
                    throw new InvalidOperationException("Thiếu prefab UI hoặc tham chiếu. Cập nhật đầy đủ SDK.");
                if (template.GetComponentsInChildren<TMP_Text>(true).Any(t => !t.font || !t.font.material || !t.font.material.shader))
                    throw new InvalidOperationException("Thiếu font/shader TMP. Import TMP Essential Resources (Window > TextMeshPro) rồi thử lại.");
            }
            bool needsEventSystem = Components<EventSystem>(scene).Length == 0 && !UnityEngine.Object.FindObjectOfType<EventSystem>();
            Type inputModule = needsEventSystem ? ResolveInputModule() : null; // Preflight before any scene mutation.
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup TikTok Feature UI");
            var created = new List<GameObject>();
            bool newController = !existing;
            try
            {
                if (!canvas)
                {
                    var go = Create(created, "TikTokCanvas", scene, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvas = go.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = go.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                    scaler.matchWidthOrHeight = 0.5f;
                    parent = (RectTransform)go.transform;
                }
                if (needsEventSystem)
                {
                    var go = Create(created, "EventSystem", scene, typeof(EventSystem));
                    var module = go.AddComponent(inputModule);
                    // New Input System's component initializes its default actions here.
                    inputModule.GetMethod("AssignDefaultActions", Type.EmptyTypes)?.Invoke(module, null);
                }
                if (!existing)
                {
                    var go = Create(created, "AmobearTikTokUI", scene, typeof(RectTransform), typeof(TikTokFeatureUI));
                    go.transform.SetParent(parent, false);
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f);
                    rect.pivot = new Vector2(1, 0.5f);
                    rect.sizeDelta = new Vector2(280, 360);
                    rect.anchoredPosition = new Vector2(-24, 0);
                    existing = go.GetComponent<TikTokFeatureUI>();
                    existing.AutoOpenClaim = autoClaim;
                    var popups = Create(created, "AmobearTikTokPopups", scene, typeof(RectTransform));
                    popups.transform.SetParent(canvas.transform, false);
                    existing.PopupsRoot = (RectTransform)popups.transform;
                    Stretch(existing.PopupsRoot);
                }
                var features = existing.Features.ToList();
                for (int i = 0; i < missing.Length; i++)
                {
                    // Clone as scene-owned objects, then register both independent hierarchies for Undo.
                    // Registering the prefab before detaching its popup loses child references on Redo.
                    var go = UnityEngine.Object.Instantiate(templates[i]);
                    created.Add(go);
                    go.name = templates[i].name;
                    SceneManager.MoveGameObjectToScene(go, scene);
                    go.transform.SetParent(existing.transform, false);
                    var view = go.GetComponent<TikTokFeatureView>();
                    var holder = (RectTransform)go.transform;
                    holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1);
                    holder.pivot = new Vector2(0.5f, 1);
                    holder.sizeDelta = new Vector2(260, 90);
                    holder.anchoredPosition = new Vector2(0, -features.Count * 110);
                    Stretch((RectTransform)view.EntryButton.transform);
                    view.Popup.transform.SetParent(existing.PopupsRoot, false);
                    Stretch((RectTransform)view.Popup.transform);
                    view.Popup.SetActive(false);
                    created.Add(view.Popup);
                    features.Add(view);
                }
                // Snapshot complete new hierarchies only after configuring all their references and settings.
                if (newController) existing.Features = features;
                foreach (var go in created.AsEnumerable().Reverse())
                {
                    bool nested = false;
                    for (var p = go.transform.parent; p; p = p.parent)
                        if (created.Contains(p.gameObject)) { nested = true; break; }
                    if (!nested) Undo.RegisterCreatedObjectUndo(go, "Create TikTok UI");
                }
                if (!newController)
                {
                    Undo.RecordObject(existing, "Connect TikTok features");
                    existing.Features = features;
                }
                EditorUtility.SetDirty(existing);
                Undo.FlushUndoRecordObjects();
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Undo.IncrementCurrentGroup();
                return existing;
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var go in created) if (go) UnityEngine.Object.DestroyImmediate(go);
                throw;
            }
        }

        internal static Type ResolveInputModule()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").First());
            int handling = settings.FindProperty("activeInputHandler")?.intValue ?? 0;
            if (handling != 1) return typeof(StandaloneInputModule);
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule")).FirstOrDefault(t => t != null);
            if (type == null) throw new InvalidOperationException("Project dùng New Input System nhưng thiếu InputSystemUIInputModule. Cài Input System trước.");
            return type;
        }

        private static GameObject Create(List<GameObject> created, string name, Scene scene, params Type[] components)
        {
            var go = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(go, scene);
            created.Add(go);
            return go;
        }
        internal static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
    }
}
