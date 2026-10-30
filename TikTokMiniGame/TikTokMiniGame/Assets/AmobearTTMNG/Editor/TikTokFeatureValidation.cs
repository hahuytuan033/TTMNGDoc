using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AmobearTTMNG.Ads;
using UnityEditor.Compilation;

namespace AmobearTTMNG.EditorTools
{
    // Source evidence is advisory: aliases, reflection, DLLs and runtime UI wiring need manual verification.
    internal static class TikTokFeatureValidation
    {
        internal static void CheckAdsPlacements(TikTokValidationReport report, TikTokAdsConfig config, string path)
        {
            if (config == null || config.Placements == null)
            {
                report.Error("[Ads] Không đọc được danh sách placement: " + path);
                return;
            }
            var valid = config.Placements.Where(p => p != null).ToArray();
            if (valid.Length != config.Placements.Count) report.Error("[Ads] Có placement null trong " + path);
            foreach (var p in valid)
            {
                if (string.IsNullOrWhiteSpace(p.Label) || string.IsNullOrWhiteSpace(p.PlacementId))
                    report.Error($"[Ads] Placement '{p.Label}' thiếu Label hoặc Placement ID ({path}).");
                if (!Enum.IsDefined(typeof(TikTokAdType), p.Type)) report.Error($"[Ads] '{p.Label}' có loại quảng cáo không hợp lệ.");
            }
            foreach (var group in valid.Where(p => !string.IsNullOrWhiteSpace(p.Label)).GroupBy(p => p.Label).Where(g => g.Count() > 1))
                report.Error($"[Ads] Label trùng '{group.Key}' — runtime chỉ dùng một mapping.");
            foreach (var group in valid.Where(p => !string.IsNullOrWhiteSpace(p.PlacementId)).GroupBy(p => p.PlacementId).Where(g => g.Select(p => p.Type).Distinct().Count() > 1))
                report.Error($"[Ads] Placement ID '{group.Key}' đang dùng cho cả Rewarded và Interstitial.");
            foreach (TikTokAdType type in new[] { TikTokAdType.Rewarded, TikTokAdType.Interstitial })
            {
                if (!valid.Any(p => p.Type == type && !string.IsNullOrWhiteSpace(p.Label) && !string.IsNullOrWhiteSpace(p.PlacementId)))
                    report.Error($"[Ads/{type}] Chưa có placement hợp lệ trong {path}.");
                else report.Info($"[Ads/{type}] Có cấu hình placement; cần xác nhận ID đúng loại và Active trên Portal.");
            }
            if (!AtLeast(config.InterstitialStartDelaySeconds, 15) || !AtLeast(config.InterstitialIntervalSeconds, 30))
                report.Error("[Ads/Interstitial] Start delay phải >= 15s và interval >= 30s, không dùng NaN/Infinity.");
            if (!AtLeast(config.ShowTimeoutSeconds, 30)) report.Error("[Ads] Show timeout phải hữu hạn và >= 30s.");
        }

        private static bool AtLeast(float value, float minimum) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum;

        internal static void Check(TikTokValidationReport report)
        {
            var sources = new Dictionary<string, string>();
            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
            {
                if (assembly.name.StartsWith("AmobearTTMNG.", StringComparison.Ordinal)) continue;
                foreach (string rawPath in assembly.sourceFiles)
                {
                    string path = rawPath.Replace('\\', '/');
                    string full = Path.GetFullPath(rawPath).Replace('\\', '/');
                    string assets = Path.GetFullPath("Assets").Replace('\\', '/') + "/";
                    if (!full.StartsWith(assets, StringComparison.OrdinalIgnoreCase)) continue;
                    path = "Assets/" + full.Substring(assets.Length);
                    if (path.StartsWith("Assets/AmobearTTMNG/", StringComparison.OrdinalIgnoreCase)
                        || path.StartsWith("Assets/Plugins/", StringComparison.OrdinalIgnoreCase)
                        || path.StartsWith("Assets/Samples/", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!File.Exists(rawPath)) continue;
                    try { sources[path] = StripNonCode(File.ReadAllText(rawPath)); }
                    catch (IOException e) { report.Warning("[Features] Không đọc được " + path + ": " + e.Message); }
                }
            }
            CheckSources(report, sources);
            TikTokFeatureUIValidation.CheckBuildScenes(report);
        }

        // Remove comments/string literals so documentation and log messages don't count as integrations.
        internal static string StripNonCode(string source) => Regex.Replace(source,
            @"//[^\r\n]*|/\*[\s\S]*?\*/|@""(?:""""|[^""])*""|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'", " ");

        internal static void CheckSources(TikTokValidationReport report, IDictionary<string, string> sources)
        {
            Evidence(report, sources, "Ads/Rewarded", @"\b(?:TikTokAds\s*\.\s*ShowVideoReward|TT\s*\.\s*CreateRewardedVideoAd)\s*\(", "TikTokAds.ShowVideoReward");
            Evidence(report, sources, "Ads/Interstitial", @"\b(?:TikTokAds\s*\.\s*ShowInterstitial|TT\s*\.\s*CreateInterstitialAd)\s*\(", "TikTokAds.ShowInterstitial");
            Evidence(report, sources, "Revisit Sidebar", @"\b(?:TikTokSidebar\s*\.\s*Start|TT\s*\.\s*StartEntranceMission)\s*\(", "TikTokSidebar.Start");
            Evidence(report, sources, "Shortcut", @"\b(?:TikTokShortcut\s*\.\s*Add|TT\s*\.\s*AddShortcut)\s*\(", "TikTokShortcut.Add");
            Evidence(report, sources, "Share", @"\b(?:TikTokShare\s*\.\s*Share|TT\s*\.\s*ShareAppMessage)\s*\(", "TikTokShare.Share");
            report.Info("[Features] Đây là kiểm tra tĩnh: loại code SDK/Editor/Demo đi kèm khỏi bằng chứng. Có lời gọi API không chứng minh nút UI được nối hoặc code nằm trong scene build; alias/DLL/reflection có thể không được phát hiện.");
            report.Info("[Test máy thật/Ads] Kiểm tra rewarded xem hết mới thưởng, đóng sớm không thưởng, lỗi/no-fill không kẹt; interstitial đóng xong tiếp tục game.");
            report.Info("[Test máy thật/Revisit Sidebar] Kiểm tra nút mở nhiệm vụ, quay lại từ sidebar, kiểm tra điều kiện và cấp thưởng đúng một lần theo kỳ nếu game có thưởng.");
            report.Info("[Test máy thật/Shortcut] Kiểm tra thêm shortcut và mở lại từ shortcut; nếu có thưởng, kiểm tra điều kiện và chống nhận lặp.");
            report.Info("[Test máy thật/Share] Kiểm tra nút chia sẻ, callback thành công/hủy/lỗi và dữ liệu query khi mở game từ link share.");
        }

        private static void Evidence(TikTokValidationReport report, IDictionary<string, string> sources, string feature, string pattern, string api)
        {
            var hits = sources.Where(p => Regex.IsMatch(p.Value, pattern)).Select(p => p.Key).ToArray();
            if (hits.Length == 0)
                report.Warning($"[{feature}] Chưa tìm thấy lời gọi API trong code game. Tích hợp {api} vào luồng/UI game hoặc kiểm tra thủ công nếu dùng wrapper, alias hoặc DLL.");
            else report.Info($"[{feature}] Có dấu vết gọi API: {string.Join(", ", hits.Take(3))}. Chưa xác nhận hoạt động runtime.");
        }
    }
}
