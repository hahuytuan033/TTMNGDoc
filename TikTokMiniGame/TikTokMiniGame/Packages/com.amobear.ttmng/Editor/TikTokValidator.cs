using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AmobearTTMNG.Ads;
using TTSDK.Tool;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AmobearTTMNG.EditorTools
{
	public enum TikTokIssueLevel
	{
		Error,
		Warning,
		Info,
	}

	/// <summary>Kết quả kiểm tra: Error chặn build, Warning nên sửa, Info để tham khảo.</summary>
	public sealed class TikTokValidationReport
	{
		public readonly List<(TikTokIssueLevel level, string message)> Items = new List<(TikTokIssueLevel, string)>();

		public bool HasErrors => Items.Any(i => i.level == TikTokIssueLevel.Error);

		public void Error(string message) => Items.Add((TikTokIssueLevel.Error, message));
		public void Warning(string message) => Items.Add((TikTokIssueLevel.Warning, message));
		public void Info(string message) => Items.Add((TikTokIssueLevel.Info, message));

		public string Format()
		{
			var sb = new StringBuilder();
			foreach (var (level, message) in Items.OrderBy(i => i.level))
				sb.AppendLine($"{(level == TikTokIssueLevel.Error ? "✗" : level == TikTokIssueLevel.Warning ? "!" : "·")} {message}");
			return sb.ToString();
		}

		public void Log(string title)
		{
			string text = $"[TikTokValidate] {title}: {Items.Count(i => i.level == TikTokIssueLevel.Error)} error, " +
				$"{Items.Count(i => i.level == TikTokIssueLevel.Warning)} warning\n{Format()}";
			if (HasErrors)
				Debug.LogError(text);
			else if (Items.Any(i => i.level == TikTokIssueLevel.Warning))
				Debug.LogWarning(text);
			else
				Debug.Log(text);
		}
	}

	/// <summary>
	/// Biến checklist của Unity Integration Guide thành kiểm tra tự động. <see cref="ValidateProject"/> chạy trước build,
	/// <see cref="ValidatePackage"/> chạy trên gói tt-minigame sau build.
	/// </summary>
	public static class TikTokValidator
	{
		[MenuItem("AmobearTTMNG/Validate Project", false, 20)]
		private static void ValidateProjectMenu()
		{
			TikTokValidationReport report = ValidateProject();
			report.Log("Validate Project");
			EditorUtility.DisplayDialog("Validate Project", Summary(report, "Project OK — có thể build."), "OK");
		}

		[MenuItem("AmobearTTMNG/Validate Last Build", false, 41)]
		private static void ValidateLastBuildMenu()
		{
			string dir = TikTokEditorPaths.PackageDir(StarkBuilderSettings.LoadSettings());
			TikTokValidationReport report = ValidatePackage(dir);
			report.Log("Validate Last Build");
			EditorUtility.DisplayDialog("Validate Last Build", Summary(report, "Gói OK — có thể ttmg dev / upload."), "OK");
		}

		public static TikTokValidationReport ValidateProject()
		{
			var report = new TikTokValidationReport();

			string sdkVersion = TikTokEditorPaths.ReadSdkVersion();
			if (sdkVersion == null)
				report.Error($"Chưa import TTSDK ({TikTokEditorPaths.SdkPackageJson}).");
			else if (sdkVersion != TikTokEditorPaths.ExpectedSdkVersion)
				report.Error($"TTSDK {sdkVersion} — package đã kiểm chứng với {TikTokEditorPaths.ExpectedSdkVersion}.");
			else
				report.Info($"TTSDK {sdkVersion}.");

			StarkBuilderSettings settings = StarkBuilderSettings.LoadSettings(false);
			string appId = TikTokEditorPaths.ReadAppId(settings);
			if (settings == null)
				report.Error("Chưa có Assets/Editor/StarkBuilderSetting.asset — chạy AmobearTTMNG/Setup Project.");
			else if (string.IsNullOrEmpty(appId))
				report.Error("Chưa điền App ID — menu AmobearTTMNG/Set App ID.");
			else
				report.Info($"App ID {appId}.");
			if (settings != null && string.IsNullOrEmpty(settings.OutputDir))
				report.Warning("StarkBuilderSetting.OutputDir trống — chạy AmobearTTMNG/Setup Project.");

			CheckRootProjectConfig(report, appId);
			string scene0 = CheckBootstrapScene(report);
			CheckAdsConfig(report);
			CheckHs(report, scene0);

			if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
				report.Info("Build target chưa phải WebGL — Build sẽ tự chuyển.");
			return report;
		}

		private static void CheckRootProjectConfig(TikTokValidationReport report, string appId)
		{
			string path = TikTokEditorPaths.RootProjectConfigJson;
			if (string.IsNullOrEmpty(appId) || !File.Exists(path))
				return;
			Match match = Regex.Match(File.ReadAllText(path), "\"appid\"\\s*:\\s*\"([^\"]*)\"");
			if (match.Success && match.Groups[1].Value != appId)
				report.Warning($"project.config.json ở thư mục gốc có appid=\"{match.Groups[1].Value}\" khác App ID — lưu lại qua AmobearTTMNG/Set App ID.");
		}

		private static string CheckBootstrapScene(TikTokValidationReport report)
		{
			string scene0 = TikTokEditorPaths.FirstBuildScene();
			if (scene0 == null)
				report.Error("Build Settings không có scene nào.");
			else if (!TikTokEditorPaths.SceneHasScript(scene0, typeof(TikTokBootstrap)))
				report.Error($"Scene 0 ({scene0}) không có TikTokBootstrap — InitSDK phải chạy ở scene đầu. Chạy AmobearTTMNG/Setup Project.");
			else
				report.Info($"Scene 0 {scene0} có TikTokBootstrap.");
			return scene0;
		}

		private static void CheckAdsConfig(TikTokValidationReport report)
		{
			string path = AssetDatabase.FindAssets($"t:{nameof(TikTokAdsConfig)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.FirstOrDefault(TikTokProjectSetup.IsLoadableAdsConfig);
			if (path == null)
			{
				report.Warning("Không có Resources/TikTokAdsConfig — mọi quảng cáo coi như không có. Chạy AmobearTTMNG/Setup Project.");
				return;
			}
			if (path.StartsWith("Packages/"))
				report.Warning($"{path} nằm trong package — Placement ID là của từng game, chuyển sang Assets.");

			var config = AssetDatabase.LoadAssetAtPath<TikTokAdsConfig>(path);
			var empty = config.Placements.Where(p => p != null && string.IsNullOrEmpty(p.PlacementId)).Select(p => p.Label).ToList();
			if (config.Placements.Count == 0)
				report.Info($"{path}: chưa khai báo placement nào (game không có quảng cáo?).");
			else if (empty.Count > 0)
				report.Warning($"{path}: placement chưa có Placement ID: {string.Join(", ", empty)}.");
			else
				report.Info($"{path}: {config.Placements.Count} placement.");
		}

		private static void CheckHs(TikTokValidationReport report, string scene0)
		{
			bool installed = TikTokEditorPaths.IsHsSdkInstalled();
			PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL, out string[] defines);
			bool enabled = defines.Contains(TikTokEditorPaths.HsDefine);

			if (installed && !enabled)
				report.Warning($"Có HS SDK nhưng chưa bật {TikTokEditorPaths.HsDefine} — TikTokHS không được build. Chạy AmobearTTMNG/Setup Project.");
			if (!installed && enabled)
				report.Error($"Bật {TikTokEditorPaths.HsDefine} nhưng không có HS SDK. Chạy AmobearTTMNG/Setup Project.");
			if (!installed || !enabled || scene0 == null || !File.Exists(scene0))
				return;

			// gameId / sdkKey là field serialize trên component TikTokSDK (object HS_TikTokSDK) trong scene đầu.
			string yaml = File.ReadAllText(scene0);
			Match gameId = Regex.Match(yaml, @"^\s*gameId:[ \t]*(\S*)\s*$", RegexOptions.Multiline);
			Match sdkKey = Regex.Match(yaml, @"^\s*sdkKey:[ \t]*(\S*)\s*$", RegexOptions.Multiline);
			if (!gameId.Success)
				report.Warning($"Scene 0 chưa có HS_TikTokSDK (menu IAP TikTok Setup/Setup SDK in Scene) — login / IAP / cloud save sẽ không chạy.");
			else if (gameId.Groups[1].Value.Length == 0 || !sdkKey.Success || sdkKey.Groups[1].Value.Length == 0)
				report.Warning("HS_TikTokSDK trong scene 0 chưa điền gameId / sdkKey của game trên console HS.");
			else
				report.Info("HS_TikTokSDK đã có gameId + sdkKey.");
		}

		[Serializable]
		private class GameJson
		{
			public string gameEngine;
			public string sdkVersion;
			public int wasmFuncCount;
			public long wasmCodeSize;
		}

		public static TikTokValidationReport ValidatePackage(string dir)
		{
			var report = new TikTokValidationReport();
			if (!Directory.Exists(dir))
			{
				report.Error($"Chưa có gói {dir} — chạy AmobearTTMNG/Build and Validate.");
				return report;
			}

			// TTSDK 1.1.5 không sinh project.config.json trong gói (chỉ có ở thư mục gốc project cho IDE).
			foreach (string file in new[] { "game.js", "game.json" })
				if (!File.Exists(Path.Combine(dir, file)))
					report.Error($"Gói thiếu {file}.");
			foreach (string sub in new[] { "wasmcode", "data-package" })
				if (!Directory.Exists(Path.Combine(dir, sub)))
					report.Error($"Gói thiếu thư mục {sub}/.");

			string gameJsonPath = Path.Combine(dir, "game.json");
			if (File.Exists(gameJsonPath))
			{
				GameJson game = null;
				try { game = JsonUtility.FromJson<GameJson>(File.ReadAllText(gameJsonPath)); }
				catch (Exception e) { report.Error($"game.json không đọc được: {e.Message}"); }

				if (game != null)
				{
					if (game.gameEngine != "unity")
						report.Error($"game.json: gameEngine=\"{game.gameEngine}\", phải là \"unity\".");
					if (game.sdkVersion != TikTokEditorPaths.ExpectedSdkVersion)
						report.Warning($"game.json: sdkVersion=\"{game.sdkVersion}\" (mong đợi {TikTokEditorPaths.ExpectedSdkVersion}).");
					if (game.wasmFuncCount <= 0)
						report.Warning("game.json: wasmFuncCount=0 — TTSDK không đọc được số hàm wasm, kiểm tra log build.");
					else if (game.wasmFuncCount >= TikTokEditorPaths.WasmSplitThreshold)
						report.Warning($"wasmFuncCount={game.wasmFuncCount} ≥ {TikTokEditorPaths.WasmSplitThreshold} — cần tách Wasm (Unity Wasm Code Splitting Guide), test lại tutorial + level đầu.");
					else
						report.Info($"wasmFuncCount={game.wasmFuncCount}, wasmCodeSize={game.wasmCodeSize / 1024f / 1024f:F1}MB.");
				}
			}

			// __TTMG_TEMP__ là file symbol tạm của TTSDK (webgl.symbols.json), không phải code chạy — tính riêng.
			var files = new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).ToList();
			long temp = files.Where(f => f.FullName.Replace('\\', '/').Contains("/__TTMG_TEMP__/")).Sum(f => f.Length);
			long total = files.Sum(f => f.Length) - temp;
			float totalMB = total / 1024f / 1024f;
			string tempNote = temp > 0 ? $" (+{temp / 1024f / 1024f:F1}MB __TTMG_TEMP__ không tính; nếu ttmg upload tính cả thì là {(total + temp) / 1024f / 1024f:F1}MB)" : string.Empty;
			if (total > TikTokEditorPaths.PackageLimitBytes)
				report.Error($"Tổng gói {totalMB:F1}MB > 60MB{tempNote} — bỏ asset thừa, kiểm .data / .wasm, chuyển asset không cần ở màn đầu sang CDN / AssetBundle.");
			else if (total + temp > TikTokEditorPaths.PackageLimitBytes)
				report.Warning($"Tổng gói {totalMB:F1}MB / 60MB{tempNote}.");
			else
				report.Info($"Tổng gói {totalMB:F1}MB / 60MB{tempNote}.");
			return report;
		}

		internal static string Summary(TikTokValidationReport report, string okMessage)
		{
			string text = report.Format();
			string[] lines = text.Split('\n');
			if (lines.Length > 18)
				text = string.Join("\n", lines.Take(18)) + "\n… (xem Console)";
			return report.HasErrors || report.Items.Any(i => i.level == TikTokIssueLevel.Warning) ? text : okMessage + "\n\n" + text;
		}
	}
}
