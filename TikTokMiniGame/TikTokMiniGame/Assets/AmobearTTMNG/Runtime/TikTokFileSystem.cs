using System;
using UnityEngine;
#if !UNITY_EDITOR && UNITY_WEBGL
using TTSDK;
#endif

namespace AmobearTTMNG
{
	/// <summary>
	/// Hệ thống tập tin của container (doc TikTok Unity SDK mục 8). Port từ pocket-2.
	///
	/// Vì sao cần: Integration Guide nói rõ Application.persistentDataPath và file IO trong C# KHÔNG đảm bảo bền
	/// trong runtime mini game. Đây là đường ghi file được bảo đảm. Dữ liệu nhỏ dạng key-value dùng
	/// <see cref="TikTokStorage"/>.
	///
	/// Đường dẫn: TTFileSystemManager.FixFilePath tự thêm tiền tố "scfile://user/" cho path tương đối, nên chỉ cần
	/// truyền tên file trần ("backup/userdata.json").
	///
	/// Quy ước của SDK: các hàm *Sync trả CHUỖI RỖNG khi thành công và chuỗi lỗi khi thất bại — wrapper quy về bool.
	/// Editor: không có container → mọi hàm trả false / null (game tự fallback).
	/// </summary>
	public static class TikTokFileSystem
	{
		private const string LogPrefix = "[FileSystem]";

#if !UNITY_EDITOR && UNITY_WEBGL
		private static bool resolved;
		private static TTFileSystemManager manager;
#endif

		/// <summary>Container hỗ trợ file system VÀ đã lấy được manager.</summary>
		public static bool IsAvailable
		{
			get
			{
#if !UNITY_EDITOR && UNITY_WEBGL
				return ResolveManager() != null;
#else
				return false;
#endif
			}
		}

#if !UNITY_EDITOR && UNITY_WEBGL
		// Cache instance; chỉ resolve một lần — container không hỗ trợ thì lần sau cũng không, khỏi log lặp.
		private static TTFileSystemManager ResolveManager()
		{
			if (resolved)
				return manager;

			if (!TikTokBootstrap.IsReady)
				return null; // chưa kết luận được, lần sau thử lại

			resolved = true;
			if (!CanIUse.GetFileSystemManager)
			{
				Log("not supported");
				return null;
			}

			try
			{
				// Như pocket-2: tự khởi tạo TTFileSystemManagerWebGL (class public, không giữ state — mọi method là
				// wrapper DllImport) thay vì TT.GetFileSystemManager() mà doc ghi nhưng tên thật không chắc chắn.
				manager = new TTFileSystemManagerWebGL();
				Log("ready");
			}
			catch (Exception e)
			{
				manager = null;
				Log($"get manager exception: {e.Message}");
			}
			return manager;
		}
#endif

		/// <summary>Ghi chuỗi UTF-8 (tự tạo thư mục cha). Trả true khi thành công.</summary>
		public static bool WriteText(string filePath, string contents)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				return false;
#if !UNITY_EDITOR && UNITY_WEBGL
			TTFileSystemManager fs = ResolveManager();
			if (fs == null)
				return false;
			try
			{
				EnsureParentDirectory(fs, filePath);
				string error = fs.WriteFileSync(filePath, contents ?? string.Empty);
				if (!string.IsNullOrEmpty(error))
				{
					Log($"write failed {filePath}: {error}");
					return false;
				}
				return true;
			}
			catch (Exception e)
			{
				Log($"write exception {filePath}: {e.Message}");
				return false;
			}
#else
			return false;
#endif
		}

		/// <summary>Đọc chuỗi UTF-8. Trả null khi file không tồn tại hoặc đọc lỗi.</summary>
		public static string ReadText(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				return null;
#if !UNITY_EDITOR && UNITY_WEBGL
			TTFileSystemManager fs = ResolveManager();
			if (fs == null)
				return null;
			try
			{
				if (!fs.AccessSync(filePath))
					return null;
				// Bắt buộc truyền encoding: overload một tham số trả byte[], hai tham số mới trả string.
				return fs.ReadFileSync(filePath, "utf8");
			}
			catch (Exception e)
			{
				Log($"read exception {filePath}: {e.Message}");
				return null;
			}
#else
			return null;
#endif
		}

		/// <summary>File có tồn tại không.</summary>
		public static bool Exists(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				return false;
#if !UNITY_EDITOR && UNITY_WEBGL
			TTFileSystemManager fs = ResolveManager();
			if (fs == null)
				return false;
			try
			{
				return fs.AccessSync(filePath);
			}
			catch (Exception e)
			{
				Log($"access exception {filePath}: {e.Message}");
				return false;
			}
#else
			return false;
#endif
		}

		/// <summary>Xoá file. Trả true khi file đã không còn (kể cả khi vốn không tồn tại).</summary>
		public static bool Delete(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				return false;
#if !UNITY_EDITOR && UNITY_WEBGL
			TTFileSystemManager fs = ResolveManager();
			if (fs == null)
				return false;
			try
			{
				if (!fs.AccessSync(filePath))
					return true;
				string error = fs.UnlinkSync(filePath);
				if (!string.IsNullOrEmpty(error))
				{
					Log($"unlink failed {filePath}: {error}");
					return false;
				}
				return true;
			}
			catch (Exception e)
			{
				Log($"unlink exception {filePath}: {e.Message}");
				return false;
			}
#else
			return false;
#endif
		}

		/// <summary>Liệt kê file trong thư mục. Trả mảng rỗng khi lỗi, không bao giờ null.</summary>
		public static string[] ListDirectory(string dirPath)
		{
#if !UNITY_EDITOR && UNITY_WEBGL
			TTFileSystemManager fs = ResolveManager();
			if (fs == null)
				return Array.Empty<string>();
			try
			{
				return fs.ReadDirSync(dirPath) ?? Array.Empty<string>();
			}
			catch (Exception e)
			{
				Log($"readdir exception {dirPath}: {e.Message}");
				return Array.Empty<string>();
			}
#else
			return Array.Empty<string>();
#endif
		}

		/// <summary>Dọn toàn bộ cache file của container (doc mục 8.2).</summary>
		public static void CleanAllCache(Action<bool> onCompleted = null)
		{
#if !UNITY_EDITOR && UNITY_WEBGL
			if (!TikTokBootstrap.IsReady)
			{
				onCompleted?.Invoke(false);
				return;
			}
			try
			{
				TT.CleanAllFileCache(success =>
				{
					Log(success ? "cache cleaned" : "cache clean failed");
					onCompleted?.Invoke(success);
				});
			}
			catch (Exception e)
			{
				Log($"clean cache exception: {e.Message}");
				onCompleted?.Invoke(false);
			}
#else
			onCompleted?.Invoke(false);
#endif
		}

#if !UNITY_EDITOR && UNITY_WEBGL
		// WriteFileSync không tự tạo thư mục cha. MkdirSync báo lỗi khi thư mục đã có — trạng thái bình thường.
		private static void EnsureParentDirectory(TTFileSystemManager fs, string filePath)
		{
			int separator = filePath.LastIndexOf('/');
			if (separator <= 0)
				return;
			try { fs.MkdirSync(filePath.Substring(0, separator), true); }
			catch (Exception) { /* đã có sẵn */ }
		}
#endif

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
