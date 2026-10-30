using System;
using System.IO;
using System.Linq;
using TTSDK.Tool;
using UnityEditor;
using UnityEngine;

namespace AmobearTTMNG.EditorTools
{
	/// <summary>Đường dẫn và phép kiểm tra dùng chung cho Setup / Validate / Build.</summary>
	internal static class TikTokEditorPaths
	{
		/// <summary>Bản TTSDK mà package này đã chạy thật trên TikTok (pocket-2) và đóng kèm trong TTSDK~/. Đổi bản = test lại toàn bộ module.</summary>
		public const string ExpectedSdkVersion = Installer.TikTokSdkInstaller.SdkVersion;

		/// <summary>Mọi thứ riêng của game mà package tạo ra nằm ở đây (không nằm trong package).</summary>
		public const string ConfigFolder = "Assets/AmobearTTMNGConfig";

		public const string SdkPackageJson = "Assets/Plugins/com.tiktok.minigame/package.json";

		/// <summary>TikTok giới hạn tổng gói Unity 60MB (Unity Integration Guide).</summary>
		public const long PackageLimitBytes = 60L * 1024 * 1024;

		/// <summary>Từ ngưỡng này build check của TikTok đòi tách Wasm.</summary>
		public const int WasmSplitThreshold = 80000;

		public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

		public static string DefaultOutputDir => Path.Combine(ProjectRoot, "tt-minigame");

		public static string RootProjectConfigJson => Path.Combine(ProjectRoot, "project.config.json");

		/// <summary>"version" trong package.json của TTSDK; null nếu chưa import.</summary>
		public static string ReadSdkVersion() => Installer.TikTokSdkInstaller.ReadInstalledVersion();

		/// <summary>Scene đầu tiên được bật trong Build Settings (scene chạy InitSDK); null nếu không có.</summary>
		public static string FirstBuildScene() => EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled)?.path;

		/// <summary>Scene có component của <paramref name="type"/> không (đọc YAML, không cần mở scene).</summary>
		public static bool SceneHasScript(string scenePath, Type type)
		{
			if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
				return false;
			MonoScript script = MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(s => s.GetClass() == type);
			if (script == null)
				return false;
			string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script));
			return File.ReadAllText(scenePath).Contains($"guid: {guid}");
		}

		/// <summary>Thư mục gói tt-minigame (thứ upload lên TikTok) của lần build gần nhất.</summary>
		public static string PackageDir(StarkBuilderSettings settings)
		{
			if (settings != null && !string.IsNullOrEmpty(settings.webglPackagePath) && Directory.Exists(settings.webglPackagePath))
				return settings.webglPackagePath;
			string output = settings == null || string.IsNullOrEmpty(settings.OutputDir) ? DefaultOutputDir : settings.OutputDir;
			return Path.Combine(output, "tt-minigame");
		}

		public static void EnsureFolder(string assetFolder)
		{
			if (AssetDatabase.IsValidFolder(assetFolder))
				return;
			string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
			EnsureFolder(parent);
			AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolder));
		}

		/// <summary>App ID đang lưu trong StarkBuilderSetting (field _appId — TTSDK đọc từ đây khi build).</summary>
		public static string ReadAppId(StarkBuilderSettings settings) =>
			settings == null ? null : new SerializedObject(settings).FindProperty("_appId")?.stringValue;
	}
}
