using System;
using System.Linq;
using System.Collections.Generic;
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
    // Suggestions are previews only. Connect is the only operation that modifies the scene.
    public sealed class TikTokFeatureConnect : EditorWindow
    {
        [Serializable]
        public sealed class Binding
        {
            public bool Selected = true;
            public TikTokUIFeature Feature;
            public Button EntryButton, ActionButton, BackgroundButton, CloseButton;
            public GameObject Popup;
            public TMP_Text Title, Description;
            public void Read(TikTokFeatureView v)
            {
                EntryButton = v.EntryButton; Popup = v.Popup; Title = v.Title; Description = v.Description;
                ActionButton = v.ActionButton; BackgroundButton = v.BackgroundButton; CloseButton = v.CloseButton;
            }
            public void Write(TikTokFeatureView v)
            {
                v.Feature = Feature; v.EntryButton = EntryButton; v.Popup = Popup; v.Title = Title; v.Description = Description;
                v.ActionButton = ActionButton; v.BackgroundButton = BackgroundButton; v.CloseButton = CloseButton;
            }
            public bool Complete => EntryButton && Popup && Title && Description && ActionButton && BackgroundButton && CloseButton;
        }

        [SerializeField] private TikTokFeatureUI controller;
        [SerializeField] private RectTransform parent;
        [SerializeField] private Binding[] bindings = {
            new Binding { Feature = TikTokUIFeature.Shortcut }, new Binding { Feature = TikTokUIFeature.Sidebar }, new Binding { Feature = TikTokUIFeature.Share }
        };
        private Vector2 scroll;
        private string message;

        public static void OpenWindow(TikTokFeatureUI target = null)
        {
            var window = GetWindow<TikTokFeatureConnect>("Connect TikTok UI");
            window.minSize = new Vector2(480, 540);
            window.parent = Selection.activeTransform as RectTransform;
            if (!target)
            {
                var scene = SceneManager.GetActiveScene();
                var existing = scene.IsValid() && scene.isLoaded ? TikTokFeatureSetup.Components<TikTokFeatureUI>(scene) : new TikTokFeatureUI[0];
                if (existing.Length == 1) target = existing[0];
            }
            window.Load(target);
            window.Show();
        }
        private void Load(TikTokFeatureUI target)
        {
            controller = target;
            if (target) parent = target.transform.parent as RectTransform;
            bindings = Enum.GetValues(typeof(TikTokUIFeature)).Cast<TikTokUIFeature>()
                .Select(k => new Binding { Feature = k, Selected = !target }).ToArray();
            if (!target) return;
            foreach (var row in bindings)
            {
                var candidates = (target.Features ?? new List<TikTokFeatureView>())
                    .Concat(target.GetComponentsInChildren<TikTokFeatureView>(true)).Where(v => v && v.Feature == row.Feature).Distinct().ToArray();
                if (candidates.Length == 1) { row.Read(candidates[0]); row.Selected = true; }
            }
        }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Kéo UI của scene active vào các ô. Suggest chỉ điền ô trống khi có đúng một đối tượng khớp tên trong popup đã chọn. Kiểm tra trước khi Connect. Không đổi layout, nội dung hoặc hàm thưởng.", MessageType.Info);
            var chosen = (TikTokFeatureUI)EditorGUILayout.ObjectField("Controller (optional)", controller, typeof(TikTokFeatureUI), true);
            if (chosen != controller) Load(chosen);
            parent = (RectTransform)EditorGUILayout.ObjectField("Canvas / parent", parent, typeof(RectTransform), true);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var row in bindings)
            {
                row.Selected = EditorGUILayout.ToggleLeft(row.Feature.ToString(), row.Selected, EditorStyles.boldLabel);
                if (!row.Selected) continue;
                row.EntryButton = Field("Entry button", row.EntryButton);
                row.Popup = Field("Popup", row.Popup);
                row.Title = Field("Title (TMP)", row.Title);
                row.Description = Field("Description (TMP)", row.Description);
                row.ActionButton = Field("Start / Share / Claim", row.ActionButton);
                row.BackgroundButton = Field("Background close", row.BackgroundButton);
                row.CloseButton = Field("Close button", row.CloseButton);
                if (GUILayout.Button("Suggest missing references")) Suggest(row);
                if (!row.Complete) EditorGUILayout.HelpBox("Chưa đủ tham chiếu.", MessageType.Warning);
                if (new[] { row.EntryButton, row.ActionButton }.Any(b => b && b.onClick.GetPersistentEventCount() > 0))
                    EditorGUILayout.HelpBox("Button có OnClick Inspector sẵn. Connect giữ lại chúng; kiểm tra để tránh gọi API/thưởng hai lần.", MessageType.Warning);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || PrefabStageUtility.GetCurrentPrefabStage() != null))
                if (GUILayout.Button("Connect / Repair", GUILayout.Height(32)))
                {
                    try
                    {
                        controller = Connect(controller, parent, bindings.Where(b => b.Selected).ToArray());
                        Selection.activeGameObject = controller.gameObject;
                        message = "Đã kết nối. Gán OnRewardClaimed tại controller và tự lưu scene. Có thể Undo.";
                    }
                    catch (Exception e) { message = e.Message; }
                }
        }
        private static T Field<T>(string label, T value) where T : UnityEngine.Object => (T)EditorGUILayout.ObjectField(label, value, typeof(T), true);
        private static T Unique<T>(GameObject root, params string[] names) where T : Component
        {
            var found = root.GetComponentsInChildren<T>(true).Where(c => names.Contains(c.name, StringComparer.OrdinalIgnoreCase)).ToArray();
            return found.Length == 1 ? found[0] : null;
        }
        internal static void Suggest(Binding row)
        {
            if (!row.Popup) return;
            if (!row.Title) row.Title = Unique<TMP_Text>(row.Popup, "Title");
            if (!row.Description) row.Description = Unique<TMP_Text>(row.Popup, "Description");
            if (!row.ActionButton) row.ActionButton = Unique<Button>(row.Popup, "Start Button", "Share Popup Button", "ActionButton");
            if (!row.BackgroundButton) row.BackgroundButton = Unique<Button>(row.Popup, "Background", "BackgroundButton");
            if (!row.CloseButton) row.CloseButton = Unique<Button>(row.Popup, "Close Button", "CloseButton");
        }
        internal static TikTokFeatureUI Connect(TikTokFeatureUI ui, RectTransform parent, Binding[] rows)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Thoát Play Mode / Prefab Stage trước khi kết nối.");
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Mở scene game trước.");
            if (rows == null || rows.Length == 0 || rows.Any(r => r == null || !r.Complete)) throw new InvalidOperationException("Chọn tính năng và điền đủ tham chiếu trước khi Connect.");
            if (rows.Any(r => !Enum.IsDefined(typeof(TikTokUIFeature), r.Feature)) || rows.GroupBy(r => r.Feature).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Tính năng trùng hoặc không hợp lệ.");
            var controllers = TikTokFeatureSetup.Components<TikTokFeatureUI>(scene);
            if (controllers.Length > 1 || (ui && ui.gameObject.scene != scene)) throw new InvalidOperationException("Chỉ dùng một controller trong scene active.");
            if (!ui) ui = controllers.FirstOrDefault();
            if (ui) parent = ui.transform as RectTransform;
            if (!parent || parent.gameObject.scene != scene || EditorUtility.IsPersistent(parent) || !parent.GetComponentInParent<Canvas>(true))
                throw new InvalidOperationException("Chọn Canvas/RectTransform trong scene active.");
            var canvas = parent.GetComponentInParent<Canvas>(true);
            var known = ui ? (ui.Features ?? new List<TikTokFeatureView>()).Concat(ui.GetComponentsInChildren<TikTokFeatureView>(true)).Where(v => v).Distinct().ToList() : new List<TikTokFeatureView>();
            if (known.GroupBy(v => v.Feature).Any(g => g.Count() > 1)) throw new InvalidOperationException("Controller có feature trùng. Sửa danh sách trước.");
            foreach (var row in rows)
            {
                var refs = new Component[] { row.EntryButton, row.Popup.transform, row.Title, row.Description, row.ActionButton, row.BackgroundButton, row.CloseButton };
                if (refs.Any(c => c.gameObject.scene != scene || EditorUtility.IsPersistent(c) || c.GetComponentInParent<Canvas>(true)?.rootCanvas != canvas.rootCanvas))
                    throw new InvalidOperationException(row.Feature + ": các tham chiếu phải thuộc cùng Canvas gốc trong scene active.");
                if (row.EntryButton.transform.IsChildOf(row.Popup.transform) || parent.IsChildOf(row.Popup.transform))
                    throw new InvalidOperationException(row.Feature + ": popup không được chứa entry button/controller.");
                if (new[] { row.Title.transform, row.Description.transform, row.ActionButton.transform, row.BackgroundButton.transform, row.CloseButton.transform }.Any(t => !t.IsChildOf(row.Popup.transform)))
                    throw new InvalidOperationException(row.Feature + ": text/action/close phải nằm trong popup đã chọn.");
                if (new[] { row.EntryButton, row.ActionButton, row.BackgroundButton, row.CloseButton }.Distinct().Count() != 4)
                    throw new InvalidOperationException(row.Feature + ": mỗi vai trò cần một button riêng.");
            }
            var finalButtons = known.Where(v => rows.All(r => r.Feature != v.Feature)).SelectMany(v => new[] { v.EntryButton, v.ActionButton, v.BackgroundButton, v.CloseButton })
                .Concat(rows.SelectMany(r => new[] { r.EntryButton, r.ActionButton, r.BackgroundButton, r.CloseButton })).Where(b => b).ToArray();
            var finalPopups = known.Where(v => rows.All(r => r.Feature != v.Feature)).Select(v => v.Popup).Concat(rows.Select(r => r.Popup)).Where(p => p).ToArray();
            if (finalButtons.Distinct().Count() != finalButtons.Length || finalPopups.Distinct().Count() != finalPopups.Length)
                throw new InvalidOperationException("Không dùng chung button/popup giữa các tính năng.");
            var eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
            Type input = !eventSystem ? TikTokFeatureSetup.ResolveInputModule() : null;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Connect TikTok UI");
            try
            {
                if (!ui)
                {
                    var go = new GameObject("AmobearTikTokUI", typeof(RectTransform));
                    SceneManager.MoveGameObjectToScene(go, scene); go.transform.SetParent(parent, false);
                    Undo.RegisterCreatedObjectUndo(go, "Create UI controller");
                    ui = Undo.AddComponent<TikTokFeatureUI>(go);
                }
                if (!eventSystem)
                {
                    var go = new GameObject("EventSystem", typeof(EventSystem), input);
                    SceneManager.MoveGameObjectToScene(go, scene);
                    input.GetMethod("AssignDefaultActions", Type.EmptyTypes)?.Invoke(go.GetComponent(input), null);
                    Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                }
                foreach (var row in rows)
                {
                    var view = known.FirstOrDefault(v => v.Feature == row.Feature);
                    if (!view)
                    {
                        var go = new GameObject(row.Feature + " Binding");
                        SceneManager.MoveGameObjectToScene(go, scene); go.transform.SetParent(ui.transform, false);
                        Undo.RegisterCreatedObjectUndo(go, "Create feature binding");
                        view = Undo.AddComponent<TikTokFeatureView>(go); known.Add(view);
                    }
                    Undo.RecordObject(view, "Connect feature references"); row.Write(view);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                    EditorUtility.SetDirty(view);
                    // Flush before the next AddComponent/RegisterCreatedObject operation.
                    Undo.FlushUndoRecordObjects();
                }
                Undo.RecordObject(ui, "Connect feature list");
                ui.Features = known;
                if (!ui.PopupsRoot) ui.PopupsRoot = (RectTransform)canvas.transform;
                PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
                EditorUtility.SetDirty(ui); Undo.FlushUndoRecordObjects();
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group); Undo.IncrementCurrentGroup();
                return ui;
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }
    }
}
