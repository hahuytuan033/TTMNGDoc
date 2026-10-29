using System;
using System.Diagnostics;
using System.IO;
using TTSDK.Tool;
using UnityEditor;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;

namespace AmobearTTMNG.EditorTools
{
	/// <summary>
	/// Build gói tt-minigame bằng TTSDK (không cần bấm nút trong cửa sổ TikTokGame/Build Minigame), kiểm tra gói, rồi
	/// mở terminal chạy ttmg dev (QR test máy thật) / ttmg upload (tạo bản test trên Developer Portal).
	/// </summary>
	public static class TikTokBuildTools
	{
		private const string LogPrefix = "[TikTokBuild]";

		[MenuItem("AmobearTTMNG/Build and Validate", false, 40)]
		public static void BuildAndValidate()
		{
			TikTokValidationReport pre = TikTokValidator.ValidateProject();
			pre.Log("Pre-build");
			if (pre.HasErrors)
			{
				EditorUtility.DisplayDialog("Build cancelled", TikTokValidator.Summary(pre, string.Empty), "OK");
				return;
			}
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				return;

			StarkBuilderSettings settings = StarkBuilderSettings.Instance ?? StarkBuilderSettings.LoadSettings();
			// Bước chuyển đổi của TTSDK đọc lại Instance.OutputDir: outputPath truyền vào phải trùng giá trị này.
			string output = settings.OutputDir;

			var watch = Stopwatch.StartNew();
			bool ok = Builder.BuildWebGL(settings, output, out bool cancelled);
			watch.Stop();
			if (!ok)
			{
				string reason = cancelled ? "Build bị huỷ." : "Build lỗi — xem Console (log [Builder] / lỗi biên dịch IL2CPP).";
				Debug.LogError($"{LogPrefix} {reason}");
				EditorUtility.DisplayDialog("Build failed", reason, "OK");
				return;
			}

			string packageDir = TikTokEditorPaths.PackageDir(settings);
			Debug.Log($"{LogPrefix} Built {packageDir} in {watch.Elapsed.TotalMinutes:F1} min.");
			TikTokValidationReport post = TikTokValidator.ValidatePackage(packageDir);
			post.Log("Package");
			if (post.HasErrors)
			{
				EditorUtility.DisplayDialog("Build xong — gói CHƯA đạt", TikTokValidator.Summary(post, string.Empty), "OK");
				return;
			}

			int choice = EditorUtility.DisplayDialogComplex("Build xong",
				TikTokValidator.Summary(post, "Gói đạt yêu cầu."), "Chạy ttmg dev", "Đóng", "Mở thư mục");
			if (choice == 0)
				RunTtmg("dev");
			else if (choice == 2)
				EditorUtility.RevealInFinder(Path.Combine(packageDir, "game.json"));
		}

		[MenuItem("AmobearTTMNG/Open Output Folder", false, 42)]
		private static void OpenOutputFolder()
		{
			string dir = TikTokEditorPaths.PackageDir(StarkBuilderSettings.LoadSettings());
			if (Directory.Exists(dir))
				EditorUtility.RevealInFinder(Path.Combine(dir, "game.json"));
			else
				EditorUtility.DisplayDialog("Open Output Folder", $"Chưa có {dir} — chạy AmobearTTMNG/Build and Validate.", "OK");
		}

		[MenuItem("AmobearTTMNG/ttmg dev (QR preview)", false, 60)]
		private static void RunDev() => RunTtmg("dev");

		[MenuItem("AmobearTTMNG/ttmg upload", false, 61)]
		private static void RunUpload()
		{
			// Upload tạo bản trên Developer Portal của App ID này — luôn hỏi lại.
			string appId = TikTokEditorPaths.ReadAppId(StarkBuilderSettings.LoadSettings());
			if (EditorUtility.DisplayDialog("ttmg upload",
					$"Upload gói hiện tại lên TikTok Developer Portal (App ID {appId})?\n\nNên chạy Validate Last Build trước.", "Upload", "Huỷ"))
				RunTtmg("upload");
		}

		/// <summary>
		/// Mở terminal NGOÀI tại thư mục gói rồi chạy ttmg (ttmg dev in QR và chạy server lâu dài nên không chạy ngầm
		/// trong Editor). Máy test và máy dev phải cùng mạng LAN — xem checklist mạng trong README.
		/// </summary>
		public static void RunTtmg(string command)
		{
			StarkBuilderSettings settings = StarkBuilderSettings.LoadSettings();
			string dir = TikTokEditorPaths.PackageDir(settings);
			if (!File.Exists(Path.Combine(dir, "game.json")))
			{
				EditorUtility.DisplayDialog($"ttmg {command}", $"Chưa có gói ở {dir} — chạy AmobearTTMNG/Build and Validate.", "OK");
				return;
			}
			// Gói TTSDK 1.1.5 không có project.config.json → truyền Client Key cho ttmg (-ck) thay vì để nó hỏi.
			string appId = TikTokEditorPaths.ReadAppId(settings);
			if (!string.IsNullOrEmpty(appId))
				command += $" -ck {appId}";
			if (!IsTtmgInstalled())
			{
				EditorUtility.DisplayDialog($"ttmg {command}", "Không tìm thấy ttmg trên PATH.\n\nCài bằng: npm i -g @ttmg/cli", "OK");
				return;
			}

			try
			{
#if UNITY_EDITOR_WIN
				Process.Start(new ProcessStartInfo("cmd.exe", $"/k ttmg {command}") { WorkingDirectory = dir, UseShellExecute = true });
#elif UNITY_EDITOR_OSX
				// File .command mở bằng Terminal; login shell để có PATH của npm global.
				string scriptPath = Path.Combine(Path.GetTempPath(), $"ttmg_{command.Split(' ')[0]}.command");
				File.WriteAllText(scriptPath, $"#!/bin/zsh -l\ncd '{dir.Replace("'", "'\\''")}'\nttmg {command}\n");
				Process.Start("chmod", $"+x \"{scriptPath}\"")?.WaitForExit(5000);
				Process.Start("open", $"\"{scriptPath}\"");
#else
				EditorUtility.DisplayDialog($"ttmg {command}", $"Mở terminal tại {dir} rồi chạy: ttmg {command}", "OK");
				return;
#endif
				Debug.Log($"{LogPrefix} ttmg {command} in {dir}");
			}
			catch (Exception e)
			{
				Debug.LogError($"{LogPrefix} Could not start ttmg {command}: {e.Message}");
			}
		}

		private static bool IsTtmgInstalled()
		{
			try
			{
#if UNITY_EDITOR_WIN
				var info = new ProcessStartInfo("where", "ttmg");
#else
				// Login shell để có PATH của npm global (Unity mở từ Dock không có PATH đó).
				var info = new ProcessStartInfo("/bin/zsh", "-lc \"command -v ttmg\"");
#endif
				info.UseShellExecute = false;
				info.CreateNoWindow = true;
				info.RedirectStandardOutput = true;
				info.RedirectStandardError = true;
				using (Process process = Process.Start(info))
				{
					if (process == null || !process.WaitForExit(5000))
						return true; // Không kiểm được thì cứ để terminal báo lỗi.
					return process.ExitCode == 0;
				}
			}
			catch
			{
				return true;
			}
		}
	}
}
