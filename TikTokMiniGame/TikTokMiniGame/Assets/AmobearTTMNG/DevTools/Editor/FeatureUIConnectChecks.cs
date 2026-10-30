using System;
using System.Linq;
using System.Reflection;
using AmobearTTMNG.UI;
using AmobearTTMNG.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FeatureUIConnectChecks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static TikTokFeatureUI Connect(TikTokFeatureUI ui, RectTransform parent, TikTokFeatureConnect.Binding[] rows) =>
        (TikTokFeatureUI)typeof(TikTokFeatureConnect).GetMethod("Connect", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { ui, parent, rows });
    static void Invoke(TikTokFeatureUI ui, string method) => typeof(TikTokFeatureUI).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
    public static string Execute()
    {
        var original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        TikTokFeatureUI ui = null;
        var probe = ScriptableObject.CreateInstance<FeatureUIRewardProbe>();
        string folder = "Assets/__ConnectUITest_" + Guid.NewGuid().ToString("N");
        try
        {
            SceneManager.SetActiveScene(scene);
            var canvas = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvas, scene);
            var parent = (RectTransform)canvas.transform;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AmobearTTMNG/UI/Prefabs/TikTokFeatureUI.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(parent, false);
            ui = instance.GetComponent<TikTokFeatureUI>();
            Check(ui.Features.Count == 3 && ui.Features.All(v => v.HasReferences), "Complete prefab lost references");
            Check(ui.Features.All(v => v.Popup.transform.IsChildOf(instance.transform) && v.EntryButton.transform.IsChildOf(instance.transform)), "External prefab reference");
            ui.AutoOpenClaim = false;
            Invoke(ui, "OnEnable");
            foreach (var v in ui.Features)
            {
                v.EntryButton.onClick.Invoke();
                Check(v.Popup.activeSelf && ui.Features.Count(x => x.Popup.activeSelf) == 1, "Entry click does not open exactly one popup");
                v.CloseButton.onClick.Invoke(); Check(!v.Popup.activeSelf, "Close click failed");
            }
            Invoke(ui, "OnDisable");
            // Simulate a developer copying only the Demo UI, without its controller or views.
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var rows = ui.Features.Select(v => { var row = new TikTokFeatureConnect.Binding { Feature = v.Feature }; row.Read(v); return row; }).ToArray();
            foreach (var v in ui.Features.ToArray()) UnityEngine.Object.DestroyImmediate(v);
            UnityEngine.Object.DestroyImmediate(ui); ui = null;
            var position = rows[0].EntryButton.transform.localPosition;
            ui = Connect(null, parent, rows);
            Check(ui.Features.Count == 3 && ui.Features.All(v => v.HasReferences), "Connect failed");
            ui.Features[0].RewardDescription = "Custom game reward";
            UnityEventTools.AddPersistentListener(ui.Features[0].OnRewardClaimed, probe.Grant);
            Connect(ui, parent, rows);
            Check(ui.Features.Count == 3 && ui.GetComponentsInChildren<TikTokFeatureView>(true).Length == 3, "Repeated Connect duplicated bindings");
            Check(ui.Features[0].RewardDescription == "Custom game reward" && rows[0].EntryButton.transform.localPosition == position, "Game customization changed");
            Check(ui.Features[0].OnRewardClaimed.GetPersistentEventCount() == 1 && ui.Features[0].OnRewardClaimed.GetPersistentTarget(0) == probe, "Reward handler overwritten");
            // Repair a missing reference and verify Undo/Redo keeps all serialized references.
            var originalTitle = rows[0].Title; ui.Features[0].Title = null;
            Connect(ui, parent, new[] { rows[0] });
            Check(ui.Features[0].Title == originalTitle, "Repair failed");
            Undo.PerformUndo(); Check(!ui.Features[0].Title, "Repair Undo failed");
            Undo.PerformRedo(); Check(ui.Features[0].Title == originalTitle && ui.Features.All(v => v.HasReferences), "Repair Redo failed");
            var before = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).Count();
            var bad = new TikTokFeatureConnect.Binding { Feature = TikTokUIFeature.Share }; bad.Read(ui.Features[2]); bad.EntryButton = rows[0].EntryButton;
            bool rejected = false;
            try { Connect(ui, parent, new[] { bad }); } catch (TargetInvocationException) { rejected = true; }
            Check(rejected && before == scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).Count(), "Duplicate button changed scene");
            // Delete controller, recreate binding, and undo the full first Connect operation.
            UnityEngine.Object.DestroyImmediate(ui.gameObject); ui = null;
            ui = Connect(null, parent, rows);
            Undo.PerformUndo(); Check(!canvas.GetComponentInChildren<TikTokFeatureUI>(true), "Initial Connect Undo failed");
            Undo.PerformRedo(); ui = canvas.GetComponentInChildren<TikTokFeatureUI>(true);
            Check(ui && ui.PopupsRoot && ui.Features.Count == 3 && ui.Features.All(v => v.HasReferences), "Initial Connect Redo lost references");
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            EditorSceneManager.SaveScene(scene, folder + "/Connected.unity");
            EditorSceneManager.CloseScene(scene, true);
            scene = EditorSceneManager.OpenScene(folder + "/Connected.unity", OpenSceneMode.Additive);
            ui = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TikTokFeatureUI>(true)).Single();
            Check(ui.PopupsRoot && ui.Features.Count == 3 && ui.Features.All(v => v.HasReferences), "Save/reload lost connected references");
            return "PASS: complete prefab references and click/close; connect copied UI; repeat/repair preserves layout/reward event; duplicate button rejected atomically; initial and repair Undo/Redo; save/reload.";
        }
        finally
        {
            if (ui) Invoke(ui, "OnDisable");
            EditorSceneManager.CloseScene(scene, true);
            UnityEngine.Object.DestroyImmediate(probe);
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }
}
