using AmobearTTMNG.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

namespace AmobearTTMNG.EditorTools
{
    [CustomEditor(typeof(TikTokFeatureUI))]
    public sealed class TikTokFeatureUIInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var ui = (TikTokFeatureUI)target;
            if (GUILayout.Button("Connect existing UI / Repair References")) TikTokFeatureConnect.OpenWindow(ui);
            bool sceneObject = ui.gameObject.scene.IsValid();
            if (sceneObject)
            {
                if (TikTokFeatureSetup.Components<TikTokFeatureUI>(ui.gameObject.scene).Length > 1)
                    EditorGUILayout.HelpBox("Có nhiều controller trong scene. Chỉ bật một bộ UI.", MessageType.Error);
                var events = UnityEngine.Object.FindObjectOfType<EventSystem>();
                if (!events || !events.GetComponents<BaseInputModule>().Any(m => m.isActiveAndEnabled))
                    EditorGUILayout.HelpBox("Thiếu EventSystem/input module đang hoạt động. Kiểm tra input trước khi thử click.", MessageType.Warning);
                if (!ui.GetComponentInParent<Canvas>(true))
                    EditorGUILayout.HelpBox("Đặt bộ UI dưới Canvas của game.", MessageType.Error);
                else if (!ui.GetComponentInParent<Canvas>(true).GetComponent<GraphicRaycaster>())
                    EditorGUILayout.HelpBox("Canvas thiếu GraphicRaycaster nên button không nhận click. Thêm GraphicRaycaster vào Canvas.", MessageType.Warning);
                if (!TikTokEditorPaths.SceneHasScript(TikTokEditorPaths.FirstBuildScene(), typeof(TikTokBootstrap)))
                    EditorGUILayout.HelpBox("Scene đầu trong Build Settings thiếu TikTokBootstrap. Chạy Setup Project riêng trước khi build.", MessageType.Warning);
            }
            EditorGUILayout.HelpBox("Gán hàm thưởng của game vào từng OnRewardClaimed. Claim bị khóa khi chưa có handler hợp lệ.", MessageType.Info);
            if (ui.Features == null) return;
            foreach (var view in ui.Features)
            {
                if (!view) continue;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(view.Feature.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(view.HasReferences ? "Button / popup / text: đã nối đủ." : "Thiếu tham chiếu. Mở Repair References để kiểm tra và nối lại.", view.HasReferences ? MessageType.Info : MessageType.Error);
                if (!view.HasValidRewardHandler()) EditorGUILayout.HelpBox("Chưa có handler thưởng hợp lệ: Claim đang bị khóa.", MessageType.Warning);
                var data = new SerializedObject(view);
                data.Update();
                EditorGUILayout.PropertyField(data.FindProperty("RewardDescription"));
                EditorGUILayout.PropertyField(data.FindProperty("OnRewardClaimed"));
                if (view.Feature == TikTokUIFeature.Share)
                {
                    EditorGUILayout.PropertyField(data.FindProperty("ShareQuery"));
                    EditorGUILayout.PropertyField(data.FindProperty("ShareTitle"));
                    EditorGUILayout.PropertyField(data.FindProperty("ShareImageUrl"));
                }
                data.ApplyModifiedProperties();
                if (GUILayout.Button("Select " + view.Feature + " UI")) Selection.activeGameObject = view.gameObject;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Select Button") && view.EntryButton) Selection.activeGameObject = view.EntryButton.gameObject;
                    if (GUILayout.Button("Select Popup") && view.Popup) Selection.activeGameObject = view.Popup;
                }
            }
        }
    }
}
