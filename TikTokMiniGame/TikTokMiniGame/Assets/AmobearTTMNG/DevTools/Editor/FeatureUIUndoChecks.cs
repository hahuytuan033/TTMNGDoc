using System;
using System.Linq;
using System.Reflection;
using AmobearTTMNG.UI;
using AmobearTTMNG.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class FeatureUIUndoChecks
{
    static Scene scene, original;
    static TikTokFeatureUI ui;
    public static string Begin()
    {
        original=SceneManager.GetActiveScene();
        scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var add=typeof(TikTokFeatureSetup).GetMethod("AddToScene",BindingFlags.NonPublic|BindingFlags.Static);
        ui=(TikTokFeatureUI)add.Invoke(null,new object[]{null,new[]{TikTokUIFeature.Shortcut},true});
        add.Invoke(null,new object[]{null,new[]{TikTokUIFeature.Shortcut,TikTokUIFeature.Sidebar,TikTokUIFeature.Share},true});
        return State();
    }
    public static string UndoStep() { Undo.PerformUndo(); return State(); }
    public static string RedoStep() { Undo.PerformRedo(); return State(); }
    public static string State() { return "count="+ui.Features.Count+" refs="+ui.Features.All(v=>v && v.HasReferences)+" objects="+scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TikTokFeatureView>(true)).Count(); }
    public static void Cleanup() { EditorSceneManager.CloseScene(scene,true); SceneManager.SetActiveScene(original); }
}
