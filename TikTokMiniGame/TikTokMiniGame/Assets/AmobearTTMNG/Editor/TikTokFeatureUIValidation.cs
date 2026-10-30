using System;
using System.Collections.Generic;
using System.Linq;
using AmobearTTMNG.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace AmobearTTMNG.EditorTools
{
    internal static class TikTokFeatureUIValidation
    {
        internal static void CheckBuildScenes(TikTokValidationReport report)
        {
            var active = SceneManager.GetActiveScene();
            var evidence = new HashSet<TikTokUIFeature>();
            var controllerScenes = new List<string>();
            foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var scene = SceneManager.GetSceneByPath(entry.path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                try
                {
                    if (opened) scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
                    var controllers = TikTokFeatureSetup.Components<TikTokFeatureUI>(scene);
                    if (controllers.Length > 0) controllerScenes.Add(entry.path);
                    CheckControllers(report, controllers, entry.path, evidence);
                }
                catch (Exception e) { report.Error("[Feature UI] Không kiểm tra được scene " + entry.path + ": " + e.Message); }
                finally { if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
            }
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            if (controllerScenes.Count > 1)
                report.Warning("[Feature UI] Nhiều scene build có controller; tránh bật đồng thời khi load additive: " + string.Join(", ", controllerScenes));
            foreach (var feature in evidence)
            {
                string label = feature == TikTokUIFeature.Sidebar ? "Revisit Sidebar" : feature.ToString();
                report.Items.RemoveAll(i => i.level == TikTokIssueLevel.Warning && i.message.StartsWith("[" + label + "] Chưa tìm thấy lời gọi API"));
                report.Info("[" + label + "] Có TikTok Feature UI được nối tham chiếu trong scene build (bao gồm prefab instance). Cần test runtime/máy thật.");
            }
        }

        internal static void CheckControllers(TikTokValidationReport report, TikTokFeatureUI[] controllers, string path, HashSet<TikTokUIFeature> evidence)
        {
            if (controllers.Length > 1) report.Error("[Feature UI] Controller trùng trong " + path + ". Chỉ giữ một TikTokFeatureUI.");
            foreach (var ui in controllers)
            {
                if (!ui.enabled || !ui.gameObject.activeInHierarchy) report.Warning("[Feature UI] Controller đang tắt: " + path);
                if (!ui.PopupsRoot) report.Error("[Feature UI] Thiếu PopupsRoot: " + path);
                if (ui.Features == null || ui.Features.Count == 0) { report.Error("[Feature UI] Chưa gán tính năng: " + path); continue; }
                foreach (var duplicate in ui.Features.Where(v => v).GroupBy(v => v.Feature).Where(g => g.Count() > 1))
                    report.Error("[Feature UI] Tính năng trùng " + duplicate.Key + ": " + path);
                foreach (var view in ui.Features)
                {
                    if (!view || !view.HasReferences) { report.Error("[Feature UI] Thiếu button/popup/text hoặc view: " + path); continue; }
                    if (view.gameObject.scene != ui.gameObject.scene || view.Popup.scene != ui.gameObject.scene)
                    { report.Error("[Feature UI] Tham chiếu khác scene: " + path); continue; }
                    if (!view.HasValidRewardHandler()) report.Warning("[Feature UI/" + view.Feature + "] Chưa gán OnRewardClaimed hợp lệ; Claim bị khóa để giữ quyền nhận quà. " + path);
                    if (ui.enabled && ui.gameObject.activeInHierarchy && view.gameObject.activeInHierarchy) evidence.Add(view.Feature);
                }
            }
            if (controllers.Length > 0 && !TikTokEditorPaths.SceneHasScript(TikTokEditorPaths.FirstBuildScene(), typeof(TikTokBootstrap)))
                report.Error("[Feature UI] Scene đầu tiên chưa có TikTokBootstrap. Chạy Setup Project để khởi tạo SDK trước khi dùng UI.");
        }
    }
}
