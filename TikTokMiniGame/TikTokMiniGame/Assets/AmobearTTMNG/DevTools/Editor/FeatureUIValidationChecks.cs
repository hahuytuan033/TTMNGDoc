using System;
using System.Linq;
using System.Reflection;
using AmobearTTMNG.UI;
using AmobearTTMNG.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FeatureUIValidationChecks
{
    public static string Execute()
    {
        var original = SceneManager.GetActiveScene();
        var build = EditorBuildSettings.scenes;
        string folder = "Assets/__UIValidation_" + Guid.NewGuid().ToString("N");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var ui = (TikTokFeatureUI)typeof(TikTokFeatureSetup).GetMethod("AddToScene", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { null, new[] { TikTokUIFeature.Shortcut, TikTokUIFeature.Sidebar, TikTokUIFeature.Share }, true });
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            PrefabUtility.SaveAsPrefabAssetAndConnect(ui.GetComponentInParent<Canvas>().gameObject, folder + "/UI.prefab", InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene, folder + "/Test.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene.path, true) };
            var check = typeof(TikTokFeatureSetup).Assembly.GetType("AmobearTTMNG.EditorTools.TikTokFeatureUIValidation")
                .GetMethod("CheckBuildScenes", BindingFlags.Static | BindingFlags.NonPublic);
            var report = new TikTokValidationReport();
            check.Invoke(null, new object[] { report });
            if (report.Items.Count(i => i.message.Contains("Có TikTok Feature UI")) != 3) throw new Exception("Prefab UI evidence missing");
            if (report.Items.Count(i => i.message.Contains("Chưa gán OnRewardClaimed")) != 3) throw new Exception("Missing-handler warnings missing");
            if (!report.Items.Any(i => i.message.Contains("chưa có TikTokBootstrap"))) throw new Exception("Missing bootstrap not reported");
            ui.Features[0].Title = null;
            new GameObject("Duplicate", typeof(TikTokFeatureUI));
            var broken = new TikTokValidationReport();
            check.Invoke(null, new object[] { broken });
            if (!broken.Items.Any(i => i.message.Contains("Controller trùng")) || !broken.Items.Any(i => i.message.Contains("Thiếu button/popup/text")))
                throw new Exception("Broken references/duplicates not reported");
            return "PASS: prefab instance build-scene evidence, missing reward handlers/bootstrap, broken reference and duplicate controller detection.";
        }
        finally
        {
            EditorBuildSettings.scenes = build;
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            AssetDatabase.DeleteAsset(folder);
        }
    }
}
