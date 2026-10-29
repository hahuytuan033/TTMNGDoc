using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TTStorage = TTSDK.TTStorage;

namespace AmobearTTMNG
{
	/// <summary>
	/// Lưu key-value bền vững trên container TikTok (tt.setStorageSync qua TTStorage). Port từ GameStorage
	/// của pocket-2 (đã chạy thật trên TikTok).
	///
	/// Vì sao không dùng thẳng UnityEngine.PlayerPrefs: Integration Guide nói PlayerPrefs / file IO mặc định
	/// của Unity KHÔNG đảm bảo bền trong runtime mini game. TTStorage ghi bền ngay lúc ghi.
	///
	/// Lúc khởi động chạy probe ghi → đọc lại; nếu container storage hỏng thì cả phiên chuyển sang PlayerPrefs
	/// (còn hơn mất sạch dữ liệu). Trong Editor TTStorage tự dùng PlayerPrefs bên dưới.
	///
	/// Quy ước: chuỗi rỗng == không có key (container getStorageSync không phân biệt được hai trường hợp).
	/// </summary>
	public static class TikTokStorage
	{
		private const string LogPrefix = "[Storage]";
		private const string ProbeKey = "tt_storage_probe_v1";
		private const string AbsentToken = "__tt_absent_9f3c__";

		private static readonly Dictionary<string, bool> presenceCache = new Dictionary<string, bool>();
		private static readonly Dictionary<string, string> stringCache = new Dictionary<string, string>();

		private static bool initialized;
		private static bool useDirectPlayerPrefs;

		/// <summary>Đang chạy trên container WebGL của TikTok.</summary>
		public static bool IsContainerStorage { get; private set; }

		/// <summary>Probe ghi → đọc lại đã khứ hồi thành công.</summary>
		public static bool IsHealthy { get; private set; }

		/// <summary>Container storage hỏng, phiên này đang ghi vào PlayerPrefs.</summary>
		public static bool IsFallback => useDirectPlayerPrefs;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void EnsureInitialized()
		{
			if (initialized)
				return;

			initialized = true;
			IsContainerStorage = Application.platform == RuntimePlatform.WebGLPlayer;
			RunProbe();
		}

		// ---------------------------------------------------------------- read / write

		public static int GetInt(string key, int defaultValue = 0)
		{
			EnsureInitialized();
			return useDirectPlayerPrefs ? PlayerPrefs.GetInt(key, defaultValue) : TTStorage.GetIntSync(key, defaultValue);
		}

		public static void SetInt(string key, int value)
		{
			EnsureInitialized();
			if (useDirectPlayerPrefs)
				PlayerPrefs.SetInt(key, value);
			else
				TTStorage.SetIntSync(key, value);
			MarkWritten(key);
		}

		public static float GetFloat(string key, float defaultValue = 0f)
		{
			EnsureInitialized();
			return useDirectPlayerPrefs ? PlayerPrefs.GetFloat(key, defaultValue) : TTStorage.GetFloatSync(key, defaultValue);
		}

		public static void SetFloat(string key, float value)
		{
			EnsureInitialized();
			if (useDirectPlayerPrefs)
				PlayerPrefs.SetFloat(key, value);
			else
				TTStorage.SetFloatSync(key, value);
			MarkWritten(key);
		}

		/// <summary>Không bao giờ trả null, kể cả khi defaultValue là null.</summary>
		public static string GetString(string key, string defaultValue = "")
		{
			EnsureInitialized();

			if (!stringCache.TryGetValue(key, out string cached))
			{
				cached = NormalizeRead(ReadStringRaw(key));
				stringCache[key] = cached;
				if (cached != null)
					presenceCache[key] = true;
			}

			return cached ?? defaultValue ?? string.Empty;
		}

		/// <summary>Ghi chuỗi rỗng / null = xoá key (để Editor hành xử giống container).</summary>
		public static void SetString(string key, string value)
		{
			EnsureInitialized();

			if (string.IsNullOrEmpty(value))
			{
				DeleteKey(key);
				return;
			}

			if (useDirectPlayerPrefs)
				PlayerPrefs.SetString(key, value);
			else
				TTStorage.SetStringSync(key, value);

			stringCache[key] = value;
			presenceCache[key] = true;
		}

		public static bool HasKey(string key)
		{
			EnsureInitialized();

			if (presenceCache.TryGetValue(key, out bool present))
				return present;

			present = ReadPresence(key);
			presenceCache[key] = present;
			return present;
		}

		public static void DeleteKey(string key)
		{
			EnsureInitialized();
			if (useDirectPlayerPrefs)
				PlayerPrefs.DeleteKey(key);
			else
				TTStorage.DeleteKeySync(key);
			stringCache[key] = null;
			presenceCache[key] = false;
		}

		public static void DeleteAll()
		{
			EnsureInitialized();
			if (useDirectPlayerPrefs)
				PlayerPrefs.DeleteAll();
			else
				TTStorage.DeleteAllSync();
			stringCache.Clear();
			presenceCache.Clear();
		}

		/// <summary>
		/// Trên container là no-op (setStorageSync đã bền ngay lúc ghi); chỉ nhánh PlayerPrefs (Editor / fallback)
		/// mới thật sự flush. TikTokBootstrap gọi hàm này khi game xuống nền.
		/// </summary>
		public static void Save()
		{
			EnsureInitialized();
			if (IsContainerStorage && !useDirectPlayerPrefs)
				return;
			PlayerPrefs.Save();
		}

		// ---------------------------------------------------------------- internals

		private static void MarkWritten(string key)
		{
			presenceCache[key] = true;
			// Giá trị số vừa ghi đè cùng key — bản chuỗi đang cache không còn đúng.
			stringCache.Remove(key);
		}

		private static string ReadStringRaw(string key)
		{
			return useDirectPlayerPrefs ? PlayerPrefs.GetString(key, AbsentToken) : TTStorage.GetStringSync(key, AbsentToken);
		}

		private static string NormalizeRead(string raw)
		{
			return raw == null || raw == AbsentToken || raw.Length == 0 ? null : raw;
		}

		private static bool ReadPresence(string key)
		{
			if (IsContainerStorage && !useDirectPlayerPrefs)
				return TTStorage.GetStringSync(key, AbsentToken) != AbsentToken;

			// Editor: PlayerPrefs.HasKey đúng với mọi kiểu. Không đọc sentinel vì GetString trên key kiểu int
			// trả default kèm log lỗi type → báo nhầm là vắng.
			return useDirectPlayerPrefs ? PlayerPrefs.HasKey(key) : TTStorage.HasKeySync(key);
		}

		private static void RunProbe()
		{
			string token = "p" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);

			try
			{
				TTStorage.SetStringSync(ProbeKey, token);
				string readBack = TTStorage.GetStringSync(ProbeKey, AbsentToken);
				IsHealthy = readBack == token;

				string state = readBack == null ? "null" : readBack == AbsentToken ? "absent" : readBack == token ? "value" : "mismatch";
				Log($"probe {(IsHealthy ? "ok" : "FAILED")} platform={Application.platform} container={IsContainerStorage} read_back={state}");
			}
			catch (Exception e)
			{
				IsHealthy = false;
				Log($"probe exception: {e.Message}");
			}

			useDirectPlayerPrefs = IsContainerStorage && !IsHealthy;
			if (useDirectPlayerPrefs)
				Debug.LogWarning($"{LogPrefix} container storage unhealthy — session degraded to PlayerPrefs");
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
