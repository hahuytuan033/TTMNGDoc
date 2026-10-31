using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AmobearTTMNG.Ads;
using TTSDK.Tool;
using UnityEditor;
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
			if (settings != null)
				CheckBulkMemory(report, settings.enableBulkMemory);

			CheckRootProjectConfig(report, appId);
			CheckBootstrapScene(report);
			CheckAdsConfig(report);
			TikTokFeatureValidation.Check(report);

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

		// TTSDK chỉ gỡ cờ IL2CPP khi tắt bulk memory, cờ trong emscriptenArgs ở lại → bản build vẫn có bulk memory.
		private static void CheckBulkMemory(TikTokValidationReport report, bool enabled)
		{
			bool emscripten = (PlayerSettings.WebGL.emscriptenArgs ?? string.Empty).Contains(TikTokProjectSetup.BulkMemoryFlag);
			bool il2cpp = (PlayerSettings.GetAdditionalIl2CppArgs() ?? string.Empty).Contains(TikTokProjectSetup.BulkMemoryIl2CppArg);
			if (emscripten != enabled || il2cpp != enabled)
				report.Warning($"Bulk Memory {(enabled ? "bật" : "tắt")} nhưng PlayerSettings chưa khớp (emscriptenArgs={emscripten}, il2cppArgs={il2cpp}) — chạy lại AmobearTTMNG/Setup Project.");
			else
				report.Info($"Bulk Memory {(enabled ? "bật" : "tắt")}.");
		}

		private static void CheckBootstrapScene(TikTokValidationReport report)
		{
			string scene0 = TikTokEditorPaths.FirstBuildScene();
			if (scene0 == null)
				report.Error("Build Settings không có scene nào.");
			else if (!TikTokEditorPaths.SceneHasScript(scene0, typeof(TikTokBootstrap)))
				report.Error($"Scene 0 ({scene0}) không có TikTokBootstrap — InitSDK phải chạy ở scene đầu. Chạy AmobearTTMNG/Setup Project.");
			else
				report.Info($"Scene 0 {scene0} có TikTokBootstrap.");
		}

		private static void CheckAdsConfig(TikTokValidationReport report)
		{
			var paths = AssetDatabase.FindAssets($"t:{nameof(TikTokAdsConfig)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(TikTokProjectSetup.IsLoadableAdsConfig).ToArray();
			if (paths.Length == 0)
			{
				report.Error("[Ads] Không có Resources/TikTokAdsConfig — chạy Setup Project và khai báo Rewarded + Interstitial.");
				return;
			}
			if (paths.Length > 1)
			{
				report.Error("[Ads] Có nhiều Resources/TikTokAdsConfig, không xác định được cấu hình runtime: " + string.Join(", ", paths));
				return;
			}
			string path = paths[0];
			if (path.StartsWith("Packages/"))
				report.Warning($"{path} nằm trong package — Placement ID là của từng game, chuyển sang Assets.");

			var config = AssetDatabase.LoadAssetAtPath<TikTokAdsConfig>(path);
			TikTokFeatureValidation.CheckAdsPlacements(report, config, path);
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
					else if (game.wasmFuncCount >= TikTokEditorPaths.WasmSplitThreshold && StarkBuilderSettings.LoadSettings(false)?.enableBulkMemory == true)
						report.Warning($"wasmFuncCount={game.wasmFuncCount} ≥ {TikTokEditorPaths.WasmSplitThreshold} — cần tách Wasm, nhưng bản build có Bulk Memory có thể chưa hỗ trợ subpackage. " +
							"Tắt Bulk Memory(beta) trong cửa sổ build TTSDK → chạy AmobearTTMNG/Setup Project → build lại → tách Wasm bằng menu Wasm split của TTSDK (StarkWebGLBuilder) → test lại tutorial + level đầu.");
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
