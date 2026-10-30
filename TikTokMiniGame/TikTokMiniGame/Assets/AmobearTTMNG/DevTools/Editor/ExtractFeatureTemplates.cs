using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using TikTokMiniGameDemo;
using AmobearTTMNG.UI;
public static class ExtractFeatureTemplates
{
    public static string Execute()
    {
        const string root = "Assets/AmobearTTMNG/UI";
        Directory.CreateDirectory(root + "/Prefabs");
        Directory.CreateDirectory(root + "/Art");
        Directory.CreateDirectory(root + "/Fonts");
        AssetDatabase.Refresh();
        foreach (string name in new[] { "LiberationSans.ttf", "LiberationSans - OFL.txt" })
            if (!File.Exists(root + "/Fonts/" + name))
                AssetDatabase.CopyAsset("Assets/AmobearTTMNG/TextMesh Pro/Fonts/" + name, root + "/Fonts/" + name);
        // Copy TMP shader includes as a set so exported fonts do not depend on the dev project's TMP folder.
        string shaderSource = "Assets/AmobearTTMNG/TextMesh Pro/Shaders";
        if (!AssetDatabase.IsValidFolder(root + "/Shaders") && AssetDatabase.IsValidFolder(shaderSource))
            AssetDatabase.CopyAsset(shaderSource, root + "/Shaders");
        var original = SceneManager.GetActiveScene();
        var source = SceneManager.GetSceneByPath("Assets/AmobearTTMNG/Demo/Scenes/Demo.unity");
        bool opened = !source.IsValid() || !source.isLoaded;
        if (opened) source = EditorSceneManager.OpenScene("Assets/AmobearTTMNG/Demo/Scenes/Demo.unity", OpenSceneMode.Additive);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
        var demo = source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ShortcutDemo>(true)).Single();
        Make(root, scene, TikTokUIFeature.Shortcut, demo.AddShortcutButton, demo.Popup, demo.PopupText, demo.PopupDescription, demo.StartButton, demo.PopupBackground, demo.PopupCloseButton);
        Make(root, scene, TikTokUIFeature.Sidebar, demo.RevisitButton, demo.RevisitPopup, demo.RevisitPopupText, demo.RevisitPopupDescription, demo.RevisitStartButton, demo.RevisitPopupBackground, demo.RevisitPopupCloseButton);
        Make(root, scene, TikTokUIFeature.Share, demo.ShareButton, demo.SharePopup, demo.SharePopupText, demo.SharePopupDescription, demo.ShareStartButton, demo.SharePopupBackground, demo.SharePopupCloseButton);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if (opened) EditorSceneManager.CloseScene(source, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
        AssetDatabase.SaveAssets();
        return "Created Shortcut/Sidebar/Share templates from authored Demo, with independent fonts and images.";
    }
    static T Map<T>(T source, Transform sourceRoot, Transform cloneRoot) where T:Component
    {
        if(source.transform==sourceRoot) return cloneRoot.GetComponent<T>();
        return cloneRoot.Find(AnimationUtility.CalculateTransformPath(source.transform,sourceRoot)).GetComponent<T>();
    }
    static void Make(string root, Scene scene, TikTokUIFeature kind, Button entry, GameObject popup, TMP_Text title, TMP_Text description, Button action, Button background, Button close)
    {
        var go = new GameObject(kind.ToString(),typeof(RectTransform),typeof(TikTokFeatureView));
        SceneManager.MoveGameObjectToScene(go,scene);
        var buttonCopy = UnityEngine.Object.Instantiate(entry.gameObject,go.transform);
        buttonCopy.name="EntryButton";
        var popupCopy=UnityEngine.Object.Instantiate(popup,go.transform);
        popupCopy.name="Popup";
        var view=go.GetComponent<TikTokFeatureView>();
        view.Feature=kind;
        view.EntryButton=buttonCopy.GetComponent<Button>();
        view.Popup=popupCopy;
        view.Title=Map(title,popup.transform,popupCopy.transform);
        view.Description=Map(description,popup.transform,popupCopy.transform);
        view.ActionButton=Map(action,popup.transform,popupCopy.transform);
        view.BackgroundButton=Map(background,popup.transform,popupCopy.transform);
        view.CloseButton=Map(close,popup.transform,popupCopy.transform);
        foreach(var b in go.GetComponentsInChildren<Button>(true)) b.onClick=new Button.ButtonClickedEvent();
        foreach(var text in go.GetComponentsInChildren<TMP_Text>(true))
        {
            if(!text.font) throw new Exception("Missing Demo font");
            string from=AssetDatabase.GetAssetPath(text.font);
            string to=root+"/Fonts/"+Path.GetFileName(from);
            if(!AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(to) && !AssetDatabase.CopyAsset(from,to)) throw new Exception("Copy font failed: "+from);
            text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(to);
            text.font.fallbackFontAssetTable.Clear();
            var fontData = new SerializedObject(text.font);
            var sourceFont = fontData.FindProperty("m_SourceFontFile_EditorRef");
            if (sourceFont != null) sourceFont.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(root + "/Fonts/LiberationSans.ttf");
            var sourceGuid = fontData.FindProperty("m_SourceFontFileGUID");
            if (sourceGuid != null) sourceGuid.stringValue = AssetDatabase.AssetPathToGUID(root + "/Fonts/LiberationSans.ttf");
            fontData.ApplyModifiedPropertiesWithoutUndo();
            string shaderName = Path.GetFileName(AssetDatabase.GetAssetPath(text.font.material.shader));
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(root + "/Shaders/" + shaderName);
            if (shader) text.font.material.shader = shader;
            EditorUtility.SetDirty(text.font);
            EditorUtility.SetDirty(text.font.material);
            text.fontSharedMaterial=text.font.material;
        }
        foreach(var image in go.GetComponentsInChildren<Image>(true))
        {
            if(!image.sprite) continue;
            string from=AssetDatabase.GetAssetPath(image.sprite);
            if(!from.StartsWith("Assets/AmobearTTMNG/Demo/")) continue;
            string to=root+"/Art/"+Path.GetFileName(from);
            if(!File.Exists(to)) AssetDatabase.CopyAsset(from,to);
            image.sprite=AssetDatabase.LoadAllAssetsAtPath(to).OfType<Sprite>().First(s=>s.name==image.sprite.name);
        }
        // Instruction copy must not promise the demo's fixed 100-coin reward.
        foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)) text.text=text.text.Replace("100 coins", "your reward").Replace("100 Coins", "your reward");
        foreach (var label in buttonCopy.GetComponentsInChildren<TMP_Text>(true))
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = 18;
            label.enableWordWrapping = false;
            label.margin = new Vector4(12, 0, 12, 0);
        }
        popupCopy.SetActive(false);
        buttonCopy.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(go,root+"/Prefabs/"+kind+".prefab");
        UnityEngine.Object.DestroyImmediate(go);
    }
}
