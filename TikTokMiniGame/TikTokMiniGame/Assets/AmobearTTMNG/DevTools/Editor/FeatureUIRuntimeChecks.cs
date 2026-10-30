using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmobearTTMNG;
using AmobearTTMNG.UI;
using AmobearTTMNG.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class FeatureUIRuntimeChecks
{
    static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
    static void Invoke(TikTokFeatureUI ui,string method) => typeof(TikTokFeatureUI).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
    public static string Execute()
    {
        var original=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var strings=new Dictionary<string,string>();
        var ints=new Dictionary<string,int>();
        var missing=new List<string>();
        var oldShortcut=TikTokShortcut.ClaimPeriod; var oldSidebar=TikTokSidebar.ClaimPeriod; var oldShare=TikTokShare.ClaimPeriod;
        var oldServerClaim=TikTokShare.ServerClaim;
        var probe=ScriptableObject.CreateInstance<FeatureUIRewardProbe>();
        TikTokFeatureUI ui=null;
        string period="feature-ui-test-"+Guid.NewGuid().ToString("N");
        try
        {
            foreach(string prefix in new[]{"amobear_tt_shortcut","amobear_tt_sidebar","amobear_tt_share"})
                foreach(string suffix in new[]{"_pending_v2","_entitled_v2","_claimed_v2"})
                {
                    string key=prefix+suffix;
                    if(!TikTokStorage.HasKey(key)) missing.Add(key);
                    else if(suffix=="_pending_v2") ints[key]=TikTokStorage.GetInt(key);
                    else strings[key]=TikTokStorage.GetString(key);
                    TikTokStorage.DeleteKey(key);
                }
            TikTokShortcut.ClaimPeriod=()=>period; TikTokSidebar.ClaimPeriod=()=>period; TikTokShare.ClaimPeriod=()=>period;
            ui=(TikTokFeatureUI)typeof(TikTokFeatureSetup).GetMethod("AddToScene",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{null,new[]{TikTokUIFeature.Shortcut,TikTokUIFeature.Sidebar,TikTokUIFeature.Share},true});
            Invoke(ui,"OnEnable");
            foreach(string prefix in new[]{"amobear_tt_shortcut","amobear_tt_sidebar","amobear_tt_share"}) TikTokStorage.SetString(prefix+"_entitled_v2",period);
            Invoke(ui,"PendingChanged");
            var shortcut=ui.Features.Single(v=>v.Feature==TikTokUIFeature.Shortcut);
            var sidebar=ui.Features.Single(v=>v.Feature==TikTokUIFeature.Sidebar);
            var share=ui.Features.Single(v=>v.Feature==TikTokUIFeature.Share);
            Assert(shortcut.Popup.activeSelf && !sidebar.Popup.activeSelf && !share.Popup.activeSelf,"Auto-claim priority failed");
            Assert(!shortcut.ActionButton.interactable,"Missing handler did not lock Claim");
            ui.Act(shortcut);
            Assert(TikTokShortcut.HasPendingReward && probe.Count==0,"Claim without handler consumed reward");
            ui.ClosePopup(); Assert(sidebar.Popup.activeSelf && !shortcut.Popup.activeSelf,"Dismiss reopened shortcut or skipped sidebar");
            ui.ClosePopup(); Assert(share.Popup.activeSelf,"Share priority failed");
            ui.ClosePopup(); Invoke(ui,"PendingChanged");
            Assert(ui.Features.All(v=>!v.Popup.activeSelf),"Dismissed popup reopened immediately");
            foreach(var view in ui.Features)
            {
                UnityEventTools.AddPersistentListener(view.OnRewardClaimed,probe.Grant);
                view.OnRewardClaimed.SetPersistentListenerState(0,UnityEventCallState.EditorAndRuntime);
                ui.Open(view); ui.Act(view); ui.Act(view);
            }
            Assert(probe.Count==3 && ui.Features.All(v=>!v.HasPendingReward),"Claim not exactly once per feature");
            Invoke(ui,"OnDisable"); Invoke(ui,"OnEnable");
            TikTokShare.ClaimPeriod=()=>period+"-next";
            ui.Open(share); ui.Act(share);
            Assert(share.HasPendingReward && share.Popup.activeSelf && share.ActionButton.interactable,"Share action did not enter Claim mode");
            ui.Act(share); Assert(probe.Count==4,"Re-enable duplicated listeners or reward callback");
            TikTokShare.ClaimPeriod=()=>period+"-failure";
            ui.Open(share); ui.Act(share);
            TikTokShare.ServerClaim=(key,done)=>done(false);
            ui.Act(share);
            Assert(!share.HasPendingReward && probe.Count==4 && share.Popup.activeSelf && !share.ActionButton.interactable,"Server rejection incorrectly granted reward or allowed Claim");
            TikTokShare.ServerClaim=null;
            TikTokShare.ClaimPeriod=()=>period+"-after-rejection";
            ui.ClosePopup(); ui.Open(share); ui.Act(share);
            ui.Act(share);
            Assert(probe.Count==5,"Next period Claim did not deliver reward");
            typeof(TikTokFeatureView).GetField("Supported",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(share,false);
            ui.Open(share);
            Assert(!share.ActionButton.interactable,"Unsupported feature action enabled");
            ui.Act(share);
            Assert(probe.Count==5 && !share.HasPendingReward,"Unsupported action created reward");
            return "PASS: missing handler preserves entitlement, popup priority/dismissal, Claim once, Share-to-Claim, disable/re-enable, server rejection and unsupported action guard.";
        }
        finally
        {
            if(ui) Invoke(ui,"OnDisable");
            TikTokShortcut.ClaimPeriod=oldShortcut; TikTokSidebar.ClaimPeriod=oldSidebar; TikTokShare.ClaimPeriod=oldShare;
            TikTokShare.ServerClaim=oldServerClaim;
            foreach(var key in missing) TikTokStorage.DeleteKey(key);
            foreach(var pair in strings) TikTokStorage.SetString(pair.Key,pair.Value);
            foreach(var pair in ints) TikTokStorage.SetInt(pair.Key,pair.Value);
            TikTokStorage.Save();
            UnityEngine.Object.DestroyImmediate(probe);
            EditorSceneManager.CloseScene(scene,true);
            if(original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }
}
