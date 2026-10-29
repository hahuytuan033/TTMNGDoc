using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AmobearTTMNG.Installer
{
	/// <summary>
	/// Cài TTSDK đi kèm package (TTSDK~/) và bật define AMOBEAR_TTMNG_TTSDK.
	///
	/// AmobearTTMNG.Runtime / .Editor tham chiếu assembly TTWebGL của TTSDK nên chỉ được biên dịch khi có define này
	/// (defineConstraints) — thiếu TTSDK thì hai assembly đó bị bỏ qua thay vì báo lỗi đỏ. Assembly này không phụ
	/// thuộc gì nên luôn chạy được, kể cả trong game vừa thêm submodule.
	/// </summary>
	[InitializeOnLoad]
	public static class TikTokSdkInstaller
	{
		public const string Define = "AMOBEAR_TTMNG_TTSDK";
		public const string SdkVersion = "1.1.5-Release";
		private const string SdkPackageJson = "Assets/Plugins/com.tiktok.minigame/package.json";
		private const string BundledFile = "TTSDK~/com.tiktok.minigame@" + SdkVersion + ".unitypackage";
		private const string LogPrefix = "[TikTokInstaller]";
		private const string AskedKey = "AmobearTTMNG.Installer.Asked";
		private const string ExitAfterImportKey = "AmobearTTMNG.Installer.ExitAfterImport";

		private static bool importing;

		// Đặt define trên các target hay dùng để đổi platform trong Editor không làm mất module.
		private static readonly NamedBuildTarget[] Targets =
		{
			NamedBuildTarget.WebGL, NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.iOS,
		};

		static TikTokSdkInstaller()
		{
			// InstallFromCommandLine: import đồng bộ trong batch mode reload domain giữa chừng nên phần sau ImportPackage
			// không chạy tiếp — domain mới kết thúc thay.
			if (SessionState.GetBool(ExitAfterImportKey, false) && ReadInstalledVersion() != null)
			{
				// Không thoát ngay trong lúc domain đang nạp. Import + bật define gây nhiều lần reload liên tiếp, mỗi lần
				// xoá delayCall cũ — nên chỉ xoá cờ khi thật sự thoát; reload nào cũng xếp lại lần thoát này.
				EditorApplication.delayCall += () =>
				{
					SessionState.EraseBool(ExitAfterImportKey);
					SyncDefine(true);
					AssetDatabase.SaveAssets();
					EditorApplication.Exit(0);
				};
				return;
			}

			// Chờ Editor ổn định (AssetDatabase sẵn sàng) rồi mới kiểm.
			EditorApplication.delayCall += () => Check(interactive: !Application.isBatchMode);
		}

		/// <summary>"version" trong package.json của TTSDK; null nếu chưa import.</summary>
		public static string ReadInstalledVersion()
		{
			if (!File.Exists(SdkPackageJson))
				return null;
			Match match = Regex.Match(File.ReadAllText(SdkPackageJson), "\"version\"\\s*:\\s*\"([^\"]+)\"");
			return match.Success ? match.Groups[1].Value : null;
		}

		[MenuItem("AmobearTTMNG/Install TikTok SDK", false, 1)]
		private static void InstallFromMenu()
		{
			if (ReadInstalledVersion() != null
				&& !EditorUtility.DisplayDialog("TikTok SDK", $"Đã có TTSDK {ReadInstalledVersion()}. Import lại bản {SdkVersion}?", "Import", "Huỷ"))
				return;
			Import(interactive: true);
		}

		/// <summary>
		/// Batch mode (KHÔNG kèm -quit — import chạy bất đồng bộ, hàm tự thoát khi xong, exit code 0 / 1):
		/// Unity.exe -batchmode -projectPath … -executeMethod AmobearTTMNG.Installer.TikTokSdkInstaller.InstallFromCommandLine
		/// </summary>
		public static void InstallFromCommandLine()
		{
			if (ReadInstalledVersion() != null)
			{
				SyncDefine(true);
				AssetDatabase.SaveAssets();
				EditorApplication.Exit(0);
				return;
			}
			SessionState.SetBool(ExitAfterImportKey, true);
			if (!Import(interactive: false))
			{
				EditorApplication.Exit(1);
				return;
			}
			// Import xong mà không reload domain (hiếm) — vẫn tới được đây.
			if (ReadInstalledVersion() != null)
			{
				SessionState.EraseBool(ExitAfterImportKey);
				SyncDefine(true);
				AssetDatabase.SaveAssets();
				EditorApplication.Exit(0);
				return;
			}
			// Đăng ký SAU Import để OnImported (bật define) chạy trước khi thoát.
			AssetDatabase.importPackageCompleted += _ => EditorApplication.Exit(ReadInstalledVersion() != null ? 0 : 1);
			AssetDatabase.importPackageFailed += (_, error) =>
			{
				Debug.LogError($"{LogPrefix} Import failed: {error}");
				EditorApplication.Exit(1);
			};
		}

		private static void Check(bool interactive)
		{
			bool installed = ReadInstalledVersion() != null;
			SyncDefine(installed);
			// Đang import (vd. InstallFromCommandLine) thì không hỏi / import lần nữa.
			if (installed || importing)
				return;

			// Mỗi phiên Editor chỉ hỏi một lần; từ chối thì vẫn còn menu AmobearTTMNG/Install TikTok SDK.
			if (interactive && SessionState.GetBool(AskedKey, false))
				return;
			SessionState.SetBool(AskedKey, true);
			if (!interactive || EditorUtility.DisplayDialog("Amobear TikTok Mini Game",
					$"Project chưa có TikTok Unity SDK.\nImport bản {SdkVersion} đi kèm package?\n\n" +
					"Các module AmobearTTMNG chỉ biên dịch khi có SDK.", "Import", "Để sau"))
				Import(interactive: false);
			else
				Debug.LogWarning($"{LogPrefix} Chưa có TTSDK — module AmobearTTMNG tạm tắt. Menu AmobearTTMNG/Install TikTok SDK để cài.");
		}

		private static bool Import(bool interactive)
		{
			string path = BundledPath();
			if (path == null || !File.Exists(path))
			{
				Debug.LogError($"{LogPrefix} Không tìm thấy {BundledFile} trong package com.amobear.ttmng.");
				return false;
			}
			// Chỉ bật define khi TTSDK đã thật sự nằm trong Assets: bật sớm thì Runtime / Editor biên dịch thiếu TTWebGL.
			AssetDatabase.importPackageCompleted -= OnImported;
			AssetDatabase.importPackageCompleted += OnImported;
			importing = true;
			Debug.Log($"{LogPrefix} Importing {path}");
			AssetDatabase.ImportPackage(path, interactive);
			return true;
		}

		private static void OnImported(string packageName)
		{
			AssetDatabase.importPackageCompleted -= OnImported;
			importing = false;
			SyncDefine(ReadInstalledVersion() != null);
		}

		/// <summary>
		/// TTSDK được thêm / xoá trong Assets → bật / gỡ define ngay sau khi import, trước khi Unity biên dịch lại: xoá
		/// TTSDK mà define còn thì Runtime / Editor lỗi thiếu TTWebGL và domain không reload để Check() kịp chạy.
		/// </summary>
		private sealed class SdkWatcher : AssetPostprocessor
		{
			private const string SdkFolder = "Assets/Plugins/com.tiktok.minigame";

			private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
			{
				if (imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(p => p.StartsWith(SdkFolder)))
					SyncDefine(ReadInstalledVersion() != null);
			}
		}

		private static string BundledPath()
		{
			PackageInfo info = PackageInfo.FindForAssembly(typeof(TikTokSdkInstaller).Assembly);
			return info == null ? null : Path.Combine(info.resolvedPath, BundledFile);
		}

		private static void SyncDefine(bool enabled)
		{
			foreach (NamedBuildTarget target in Targets)
			{
				PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
				if (defines.Contains(Define) == enabled)
					continue;
				string[] next = enabled ? defines.Append(Define).ToArray() : defines.Where(d => d != Define).ToArray();
				PlayerSettings.SetScriptingDefineSymbols(target, next);
				Debug.Log($"{LogPrefix} {(enabled ? "Added" : "Removed")} {Define} ({target.TargetName}).");
			}
		}
	}
}
