using System;
using System.Linq;
using System.Reflection;
using AmobearTTMNG.UI;
using AmobearTTMNG.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FeatureUIEditorChecks
{
    static TikTokFeatureUI Add(RectTransform parent, params TikTokUIFeature[] features) => (TikTokFeatureUI)typeof(TikTokFeatureSetup)
        .GetMethod("AddToScene",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{parent,features,true});
    static void Assert(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static string Execute()
    {
        var original=SceneManager.GetActiveScene();
        var build=EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled).ToArray();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var ui=Add(null,TikTokUIFeature.Shortcut);
            Assert(ui.Features.Count==1,"Initial feature missing");
            Assert(ui.Features.All(v=>v.HasReferences && !v.Popup.activeSelf),"Invalid references/popups");
            Undo.PerformUndo();
            Assert(!scene.GetRootGameObjects().Any(g => g.GetComponentInChildren<TikTokFeatureUI>(true)), "Initial Undo left controller");
            Undo.PerformRedo();
            ui=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TikTokFeatureUI>(true)).Single();
            Assert(ui.PopupsRoot && ui.Features.Count==1 && ui.Features.All(v=>v && v.HasReferences), "Initial Redo lost references");
            var canvas=ui.GetComponentInParent<Canvas>();
            var scaler=canvas.GetComponent<CanvasScaler>();
            Assert(scaler.referenceResolution==new Vector2(1080,1920) && canvas.renderMode==RenderMode.ScreenSpaceOverlay,"Initial Redo lost Canvas configuration");
            scaler.referenceResolution=new Vector2(720,1280);
            ui.Features[0].RewardDescription="CUSTOM REWARD";
            ui.Features[0].EntryButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(5,17);
            Add((RectTransform)canvas.transform,TikTokUIFeature.Shortcut,TikTokUIFeature.Sidebar,TikTokUIFeature.Share);
            Assert(ui.Features.Count==3,"Repeat did not add missing features");
            Assert(ui.Features[0].RewardDescription=="CUSTOM REWARD" && ui.Features[0].EntryButton.GetComponent<RectTransform>().anchoredPosition==new Vector2(5,17),"Existing customization overwritten");
            Assert(scaler.referenceResolution==new Vector2(720,1280),"Canvas scaler changed");
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert(ui.Features.Count==1,"Undo did not remove added features");
            Undo.PerformRedo();
            Assert(ui.Features.Count==3 && ui.Features.All(v=>v && v.HasReferences),"Redo refs count="+ui.Features.Count+" "+string.Join(";",ui.Features.Select(v=>!v?"null":v.Feature+" Entry="+(bool)v.EntryButton+" Popup="+(bool)v.Popup+" Title="+(bool)v.Title+" Desc="+(bool)v.Description+" Action="+(bool)v.ActionButton+" Bg="+(bool)v.BackgroundButton+" Close="+(bool)v.CloseButton)));
            Add((RectTransform)canvas.transform,TikTokUIFeature.Share);
            Assert(ui.Features.Count==3,"Unselected features removed/duplicates created");
            Assert(build.SequenceEqual(EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled)),"Build settings changed");
            Assert(scene.path=="" && scene.isDirty,"Scene saved unexpectedly");
            string folder="Assets/__FeatureUITest_"+Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets",folder.Substring(7));
            EditorSceneManager.SaveScene(scene,folder+"/UI.unity");
            EditorSceneManager.CloseScene(scene,true);
            scene=EditorSceneManager.OpenScene(folder+"/UI.unity",OpenSceneMode.Additive);
            var restored=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TikTokFeatureUI>(true)).Single();
            Assert(restored.Features.Count==3 && restored.Features.All(v=>v.HasReferences),"Reload lost references");
            EditorSceneManager.CloseScene(scene,true);
            AssetDatabase.DeleteAsset(folder);
            // No controller and two canvases must require an explicit selection.
            scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            foreach(var name in new[]{"Canvas A","Canvas B"}) { var go=new GameObject(name,typeof(Canvas)); SceneManager.MoveGameObjectToScene(go,scene); }
            bool rejected=false;
            try { Add(null,TikTokUIFeature.Share); } catch(TargetInvocationException e) { rejected=e.InnerException.Message.Contains("nhiều Canvas"); }
            Assert(rejected,"Ambiguous canvas not rejected");
            var chosen=scene.GetRootGameObjects().First(g=>g.name=="Canvas B").GetComponent<RectTransform>();
            var eventObject=new GameObject("Existing EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(eventObject,scene);
            var selectedUI=Add(chosen,TikTokUIFeature.Sidebar);
            Assert(selectedUI.transform.parent==chosen,"Explicit Canvas ignored");
            Assert(scene.GetRootGameObjects().Count(g=>g.GetComponent<UnityEngine.EventSystems.EventSystem>())==1,"Existing EventSystem duplicated");
            Undo.PerformUndo(); Undo.PerformRedo();
            selectedUI=chosen.GetComponentInChildren<TikTokFeatureUI>(true);
            Assert(selectedUI && selectedUI.PopupsRoot && selectedUI.Features.All(v=>v && v.HasReferences),"Existing Canvas initial Redo lost popup references");
            return "PASS: empty scene, reuse canvas, repeated setup/customizations, single and all features, Undo/Redo, save/reload, ambiguous canvas, build settings unchanged.";
        }
        finally
        {
            if(scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
            if(original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }
}
