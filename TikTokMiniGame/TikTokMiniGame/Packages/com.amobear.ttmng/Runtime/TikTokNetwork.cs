using System;
using UnityEngine;
#if !UNITY_EDITOR && UNITY_WEBGL
using TTSDK;
#endif

namespace AmobearTTMNG
{
	/// <summary>
	/// Trạng thái mạng của container (doc TikTok Unity SDK mục 10). Port từ pocket-2.
	///
	/// Vì sao không dùng Application.internetReachability: trên WebGL nó suy ra từ navigator.onLine, chỉ biết
	/// "có interface mạng" chứ không biết có ra được Internet không, và không phân biệt wifi/4g/5g.
	/// TT.GetNetWorkType đi thẳng xuống container nên đúng cho cả hai việc.
	///
	/// Giá trị NetworkType: wifi, 2g, 3g, 4g, 5g, none, unknown.
	/// </summary>
	public static class TikTokNetwork
	{
		private const string LogPrefix = "[Network]";

		public const string TypeUnknown = "unknown";
		public const string TypeNone = "none";

		private static bool initialized;
		private static bool listenersHooked;
		private static Action changedHandlers;

		/// <summary>Loại mạng gần nhất container báo về. Không bao giờ null.</summary>
		public static string NetworkType { get; private set; } = TypeUnknown;

		/// <summary>
		/// Có mạng. Trước khi lấy được số liệu đầu tiên thì trả true — lạc quan có chủ đích: chặn nhầm một
		/// request khi thực ra có mạng tệ hơn là thả một request khi thực ra mất mạng.
		/// </summary>
		public static bool IsConnected { get; private set; } = true;

		/// <summary>Container báo mạng yếu. Dùng để hạ chất lượng hoặc hoãn tải tài nguyên nặng.</summary>
		public static bool IsWeak { get; private set; }

		/// <summary>Đã lấy được số liệu thật ít nhất một lần.</summary>
		public static bool IsResolved { get; private set; }

		/// <summary>
		/// Bắn mỗi lần trạng thái đổi. Replay-on-subscribe: subscriber vào muộn được gọi ngay với giá trị hiện tại.
		/// </summary>
		public static event Action OnChanged
		{
			add
			{
				if (value == null)
					return;
				changedHandlers += value;
				InvokeHandler(value);
			}
			remove => changedHandlers -= value;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void EnsureInitialized()
		{
			if (initialized)
				return;
			initialized = true;
			TikTokBootstrap.WhenReady(OnSdkReady);
		}

		private static void OnSdkReady(bool sdkReady)
		{
#if !UNITY_EDITOR && UNITY_WEBGL
			if (!sdkReady)
			{
				Log("sdk not ready — network listeners deferred until SDK is ready");
				TikTokBootstrap.WhenSdkReady(() => OnSdkReady(true));
				return;
			}
#endif
			HookListeners();
			Refresh();
		}

		/// <summary>Hỏi lại container ngay. An toàn khi gọi nhiều lần.</summary>
		public static void Refresh()
		{
#if !UNITY_EDITOR && UNITY_WEBGL
			if (!TikTokBootstrap.IsReady || !CanIUse.GetNetWorkType)
				return;

			try
			{
				// Lưu ý hoa/thường: method là GetNetWorkType (W hoa) nhưng param là GetNetworkTypeParam (w thường).
				TT.GetNetWorkType(new GetNetworkTypeParam
				{
					Success = result =>
					{
						if (result != null)
							ApplyState(result.NetworkType.ToString().ToLowerInvariant(), result.NetworkType != NetWorkType.None, "poll");
					},
					Fail = error => Log($"get type failed: {error?.ErrMsg}")
				});
			}
			catch (Exception e)
			{
				Log($"get type exception: {e.Message}");
			}
#else
			ApplyState("wifi", true, "editor");
#endif
		}

#if !UNITY_EDITOR && UNITY_WEBGL
		private static void HookListeners()
		{
			if (listenersHooked)
				return;

			try
			{
				// Gỡ trước khi đăng ký: container KHÔNG khử trùng callback, hook hai lần = mỗi sự kiện bắn đôi.
				TT.OffNetworkStatusChange();
				TT.OffNetworkWeakChange();

				if (CanIUse.OnNetworkStatusChange)
				{
					TT.OnNetworkStatusChange(result =>
					{
						if (result == null)
							return;
						// SDK cho hẳn cờ IsConnected nên dùng thẳng, không suy ra từ loại mạng.
						ApplyState(result.NetworkType.ToString().ToLowerInvariant(), result.IsConnected, "event");
					});
				}

				if (CanIUse.OnNetworkWeakChange)
				{
					TT.OnNetworkWeakChange(result =>
					{
						if (result == null || result.WeakNet == IsWeak)
							return;
						IsWeak = result.WeakNet;
						Log(IsWeak ? "weak" : "recovered");
						RaiseChanged();
					});
				}

				listenersHooked = true;
				Log("listeners hooked");
			}
			catch (Exception e)
			{
				Log($"hook exception: {e.Message}");
			}
		}

		/// <summary>Gỡ listener khỏi container. Gọi khi teardown để không tích luỹ callback.</summary>
		public static void Unhook()
		{
			if (!listenersHooked)
				return;
			try
			{
				TT.OffNetworkStatusChange();
				TT.OffNetworkWeakChange();
			}
			catch (Exception e)
			{
				Log($"unhook exception: {e.Message}");
			}
			listenersHooked = false;
		}
#else
		private static void HookListeners() => listenersHooked = true;

		/// <summary>Gỡ listener khỏi container. Gọi khi teardown để không tích luỹ callback.</summary>
		public static void Unhook() => listenersHooked = false;
#endif

		// "unknown" = container không xác định được loại mạng, KHÔNG có nghĩa là mất mạng — nên connected được
		// truyền từ nguồn biết chắc (NetWorkType.None hoặc cờ IsConnected của sự kiện).
		private static void ApplyState(string rawType, bool connected, string source)
		{
			string normalized = string.IsNullOrWhiteSpace(rawType) ? TypeUnknown : rawType.Trim().ToLowerInvariant();
			bool changed = !IsResolved || normalized != NetworkType || connected != IsConnected;

			NetworkType = normalized;
			IsConnected = connected;
			IsResolved = true;

			if (!changed)
				return;
			Log($"type={normalized} connected={connected} source={source}");
			RaiseChanged();
		}

		private static void RaiseChanged()
		{
			Action handlers = changedHandlers;
			if (handlers == null)
				return;
			foreach (Delegate handler in handlers.GetInvocationList())
				InvokeHandler((Action)handler);
		}

		private static void InvokeHandler(Action handler)
		{
			try { handler(); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnChanged handler threw: {e}"); }
		}

		/// <summary>Tóm tắt một dòng để đính kèm khi báo lỗi (doc Developer Options).</summary>
		public static string Describe() => $"type={NetworkType};connected={IsConnected};weak={IsWeak};resolved={IsResolved}";

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
