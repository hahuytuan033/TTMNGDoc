using System;
using System.Linq;
using AmobearTTMNG.EditorTools;
using AmobearTTMNG.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildCompleteFeaturePrefab
{
    public static string Execute()
    {
        const string folder = "Assets/AmobearTTMNG/UI/Prefabs/";
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("AmobearTikTokUI", typeof(RectTransform), typeof(TikTokFeatureUI));
            SceneManager.MoveGameObjectToScene(root, scene);
            Stretch((RectTransform)root.transform);
            var ui = root.GetComponent<TikTokFeatureUI>();
            var buttons = new GameObject("Buttons", typeof(RectTransform)); buttons.transform.SetParent(root.transform, false);
            TikTokFeatureLayout.ApplyGroup((RectTransform)buttons.transform, fitSafeArea: true);
            var popups = new GameObject("AmobearTikTokPopups", typeof(RectTransform)); popups.transform.SetParent(root.transform, false);
            ui.PopupsRoot = (RectTransform)popups.transform; Stretch(ui.PopupsRoot);
            foreach (TikTokUIFeature kind in Enum.GetValues(typeof(TikTokUIFeature)))
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(folder + kind + ".prefab");
                if (!source) throw new Exception("Missing template " + kind);
                var go = UnityEngine.Object.Instantiate(source, buttons.transform, false); go.name = kind.ToString();
                var view = go.GetComponent<TikTokFeatureView>();
                TikTokFeatureLayout.ApplyEntry((RectTransform)go.transform, (int)kind);
                Stretch((RectTransform)view.EntryButton.transform);
                view.Popup.transform.SetParent(popups.transform, false); Stretch((RectTransform)view.Popup.transform); view.Popup.SetActive(false);
                ui.Features.Add(view);
            }
            if (ui.Features.Any(v => !v.HasReferences)) throw new Exception("Incomplete template");
            PrefabUtility.SaveAsPrefabAsset(root, folder + "TikTokFeatureUI.prefab");
            return "Complete prefab created: three features, all references internal, no Canvas/EventSystem/game reward dependency.";
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; r.localScale = Vector3.one; }

    public static string ExecuteCanvas()
    {
        const string folder = "Assets/AmobearTTMNG/UI/Prefabs/";
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "TikTokFeatureUI.prefab");
            if (!source) throw new Exception("Generate TikTokFeatureUI.prefab first.");
            var go = UnityEngine.Object.Instantiate(source);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.name = "TikTokFeatureCanvas";
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<TikTokUIInput>();
            PrefabUtility.SaveAsPrefabAsset(go, folder + "TikTokFeatureCanvas.prefab");
            return "Created TikTokFeatureCanvas: Canvas/scaler/raycaster, three complete features and automatic runtime input fallback.";
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
