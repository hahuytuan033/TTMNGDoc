using AmobearTTMNG.UI;
using UnityEditor;
using UnityEngine;

namespace AmobearTTMNG.EditorTools
{
    [CustomEditor(typeof(TikTokFeatureUI))]
    public sealed class TikTokFeatureUIInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var ui = (TikTokFeatureUI)target;
            EditorGUILayout.HelpBox("Gán hàm thưởng của game vào từng OnRewardClaimed. Claim bị khóa khi chưa có handler hợp lệ.", MessageType.Info);
            foreach (var view in ui.Features)
            {
                if (!view) continue;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(view.Feature.ToString(), EditorStyles.boldLabel);
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
            }
        }
    }
}
