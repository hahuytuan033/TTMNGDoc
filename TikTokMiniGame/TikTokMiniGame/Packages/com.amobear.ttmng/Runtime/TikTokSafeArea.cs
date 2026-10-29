using System;
using System.Globalization;
using UnityEngine;
#if !UNITY_EDITOR && UNITY_WEBGL
using TTSDK;
#endif

namespace AmobearTTMNG
{
	/// <summary>
	/// Vùng an toàn + vùng cấm dưới nút capsule của TikTok, biểu diễn bằng TỈ LỆ (0..1) của màn hình. Port từ pocket-2.
	///
	/// Vì sao không dùng Screen.safeArea trên thiết bị: Unity WebGL player trả Rect(0,0,Screen.width,Screen.height)
	/// vô điều kiện — inset bằng 0 im lặng đúng trên nền tảng đang ship. Đường thiết bị bắt buộc là TT.GetSystemInfo().
	///
	/// Vì sao trả tỉ lệ chứ không phải pixel: TTSystemInfo tính bằng logical px (CSS px), Screen.* là physical px, và
	/// container được phép clamp DPR nên quy đổi qua pixelRatio sẽ sai âm thầm. Tỉ lệ thì độc lập DPR.
	///
	/// Không có API public lấy vị trí capsule (GetMenuButtonLayout là internal), nên vùng cấm được suy ra từ safe top
	/// + hằng số design token. Dùng nhanh: gắn <see cref="SafeAreaFitter"/> vào RectTransform gốc của UI.
	/// </summary>
	public static class TikTokSafeArea
	{
		private const string LogPrefix = "[SafeArea]";

		// Capsule TikTok là design token cố định ~87x32pt, nằm ngay dưới safe top.
		public const float CapsuleHeightLogicalPx = 32f;
		public const float CapsuleGapLogicalPx = 8f;

		// Chỉ dùng khi chưa có số liệu thật (Editor không bật Simulator, hoặc SDK chưa sẵn sàng).
		public const float FallbackTopLogicalPx = 44f;
		public const float FallbackScreenHeightLogicalPx = 852f;

		/// <summary>Đã lấy được số liệu thật từ SDK (hoặc Simulator ở Editor).</summary>
		public static bool IsResolved { get; private set; }

		/// <summary>Đang chạy trên giá trị fallback.</summary>
		public static bool UsedFallback { get; private set; } = true;

		public static float NormalizedTop { get; private set; }
		/// <summary>Top + vùng capsule: đặt nút/HUD góc trên phải dưới mức này để không bị capsule che.</summary>
		public static float NormalizedTopWithCapsule { get; private set; }
		public static float NormalizedBottom { get; private set; }
		public static float NormalizedLeft { get; private set; }
		public static float NormalizedRight { get; private set; }

		private static Action changedHandlers;
		private static bool initialized;
		private static string description = "unresolved";

		/// <summary>Bắn mỗi lần số liệu đổi. Replay-on-subscribe: subscriber vào muộn được gọi ngay với giá trị hiện tại.</summary>
		public static event Action OnChanged
		{
			add
			{
				if (value == null)
					return;
				changedHandlers += value;
				if (initialized)
					InvokeHandler(value);
			}
			remove => changedHandlers -= value;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void EnsureInitialized()
		{
			if (initialized)
				return;
			initialized = true;

			// Cài fallback TRƯỚC để không ai đọc phải giá trị rác ở những frame đầu.
			ApplyFallback();
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				Refresh();
				// InitSDK hết timeout: đang dùng fallback — đọc lại số liệu thật nếu SDK sẵn sàng muộn.
				if (!sdkReady)
					TikTokBootstrap.WhenSdkReady(Refresh);
			});
		}

		/// <summary>Đọc lại số liệu. Gọi sau khi xoay màn hình, đổi thiết bị trong DevTool, hoặc container resize.</summary>
		public static void Refresh()
		{
			if (!initialized)
			{
				EnsureInitialized();
				return;
			}
			if (TryReadPlatform())
				Log(description);
			RaiseChanged();
		}

		public static string Describe() => description;

		private static bool TryReadPlatform()
		{
#if !UNITY_EDITOR && UNITY_WEBGL
			// GetSystemInfo trước khi init xong có thể ném — chỉ đọc khi SDK sẵn sàng, vẫn bọc try/catch.
			if (!TikTokBootstrap.IsReady)
				return false;

			try
			{
				TTSystemInfo info = TT.GetSystemInfo();
				if (info == null || info.screenHeight <= 0)
				{
					Log("system info invalid");
					return false;
				}

				float screenH = info.screenHeight;
				float screenW = info.screenWidth > 0 ? info.screenWidth : screenH;

				// Android thường chỉ báo một trong hai trường, iOS báo cả hai — lấy cái lớn hơn.
				float topLogical = Mathf.Max(info.safeArea.top, info.statusBarHeight);
				float leftLogical = Mathf.Max(0f, info.safeArea.left);
				// Chuẩn mini game: bottom/right là TỌA ĐỘ cạnh dưới/phải của vùng an toàn, không phải lề.
				// Giá trị lớn hơn nửa màn hình chắc chắn là tọa độ → quy về lề; nhỏ thì coi như lề sẵn.
				float bottomLogical = ToInset(info.safeArea.bottom, screenH);
				float rightLogical = ToInset(info.safeArea.right, screenW);

				Assign(topLogical, bottomLogical, leftLogical, rightLogical, screenW, screenH, false);
				description =
					$"screen={info.screenWidth}x{info.screenHeight};statusBar={info.statusBarHeight}" +
					$";safe={info.safeArea.left},{info.safeArea.top},{info.safeArea.right},{info.safeArea.bottom}" +
					$";inset={topLogical},{rightLogical},{bottomLogical},{leftLogical}" +
					$";normTopCapsule={NormalizedTopWithCapsule.ToString("F4", CultureInfo.InvariantCulture)}";
				return true;
			}
			catch (Exception e)
			{
				Log($"GetSystemInfo exception: {e.Message}");
				return false;
			}
#else
			// Editor: Game view Simulator có populate Screen.safeArea; không bật Simulator thì safeArea == toàn màn
			// hình và rơi về hằng số fallback.
			Rect safe = Screen.safeArea;
			float screenH = Screen.height;
			float screenW = Screen.width;
			if (screenH <= 0f || screenW <= 0f)
				return false;

			float topLogical = screenH - (safe.y + safe.height);
			float bottomLogical = safe.y;
			if (topLogical <= 0.5f && bottomLogical <= 0.5f)
			{
				ApplyFallback();
				return true;
			}

			Assign(topLogical, bottomLogical, safe.x, screenW - (safe.x + safe.width), screenW, screenH, false);
			description =
				$"screen={(int)screenW}x{(int)screenH};simulator=1;inset={(int)topLogical},{(int)(screenW - (safe.x + safe.width))},{(int)bottomLogical},{(int)safe.x}" +
				$";normTopCapsule={NormalizedTopWithCapsule.ToString("F4", CultureInfo.InvariantCulture)}";
			return true;
#endif
		}

		private static float ToInset(float edge, float size)
		{
			if (edge <= 0f)
				return 0f;
			return edge > size * 0.5f ? Mathf.Max(0f, size - edge) : edge;
		}

		private static void ApplyFallback()
		{
			Assign(FallbackTopLogicalPx, 0f, 0f, 0f, FallbackScreenHeightLogicalPx, FallbackScreenHeightLogicalPx, true);
			description =
				$"fallback=1;assumedScreenH={FallbackScreenHeightLogicalPx};assumedTop={FallbackTopLogicalPx}" +
				$";normTopCapsule={NormalizedTopWithCapsule.ToString("F4", CultureInfo.InvariantCulture)}";
		}

		private static void Assign(float topLogical, float bottomLogical, float leftLogical, float rightLogical,
			float screenW, float screenH, bool fallback)
		{
			NormalizedTop = Mathf.Clamp01(topLogical / screenH);
			NormalizedTopWithCapsule = Mathf.Clamp01((topLogical + CapsuleGapLogicalPx + CapsuleHeightLogicalPx) / screenH);
			NormalizedBottom = Mathf.Clamp01(bottomLogical / screenH);
			NormalizedLeft = Mathf.Clamp01(leftLogical / screenW);
			NormalizedRight = Mathf.Clamp01(rightLogical / screenW);
			UsedFallback = fallback;
			IsResolved = !fallback;
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

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
