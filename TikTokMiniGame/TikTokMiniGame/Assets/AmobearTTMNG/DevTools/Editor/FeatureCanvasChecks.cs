using System;
using System.Linq;
using System.Reflection;
using AmobearTTMNG.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FeatureCanvasChecks
{
    public static string Execute()
    {
        var original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        TikTokFeatureUI ui = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AmobearTTMNG/UI/Prefabs/TikTokFeatureCanvas.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var canvas = go.GetComponent<Canvas>();
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay || !go.GetComponent<GraphicRaycaster>() || go.GetComponent<CanvasScaler>().referenceResolution != new Vector2(1080, 1920))
                throw new Exception("Canvas configuration invalid");
            ui = go.GetComponent<TikTokFeatureUI>();
            if (ui.Features.Count != 3 || ui.Features.Any(v => !v.HasReferences || !v.Popup.transform.IsChildOf(go.transform))) throw new Exception("Missing internal references");
            var input = go.GetComponent<TikTokUIInput>();
            var ensure = typeof(TikTokUIInput).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            var before = UnityEngine.Object.FindObjectsOfType<EventSystem>();
            ensure.Invoke(input, null);
            var after = UnityEngine.Object.FindObjectsOfType<EventSystem>();
            if (after.Length != Math.Max(1, before.Length)) throw new Exception("EventSystem not created/reused correctly");
            ensure.Invoke(input, null);
            if (UnityEngine.Object.FindObjectsOfType<EventSystem>().Length != after.Length) throw new Exception("Duplicate EventSystem on re-enable");
            ui.AutoOpenClaim = false;
            typeof(TikTokFeatureUI).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            foreach (var view in ui.Features)
            {
                view.EntryButton.onClick.Invoke();
                if (!view.Popup.activeSelf || ui.Features.Count(v => v.Popup.activeSelf) != 1) throw new Exception("Button click failed");
                view.CloseButton.onClick.Invoke();
                if (view.Popup.activeSelf) throw new Exception("Popup close failed");
            }
            return "PASS: Canvas prefab instantiated as scene root, complete internal references, input reuse/creation and no duplicate on re-enable, all three buttons open/close popups. Existing EventSystems before test: " + before.Length;
        }
        finally
        {
            if (ui) typeof(TikTokFeatureUI).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            EditorSceneManager.CloseScene(scene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }
}
