using System;
using UnityEngine;
#if !UNITY_EDITOR && UNITY_WEBGL
using TTSDK;
#endif

namespace AmobearTTMNG
{
	/// <summary>
	/// Rung qua TTSDK. Port từ pocket-2. Trên WebGL đây là đường DUY NHẤT có tác dụng: các plugin haptic
	/// (Nice Vibrations...) không có implementation WebGL nên gọi vào là no-op im lặng.
	///
	/// Cần TikTok client >= 43.8.0 (doc mục 16.5). Dưới ngưỡng đó CanIUse trả false và mọi lệnh gọi ở đây
	/// tự bỏ qua — KHÔNG ném exception, call site không phải bọc if.
	///
	/// Thời lượng do nền tảng quy định: short = 30ms (Android) / 15ms (iOS), long = 400ms.
	/// </summary>
	public static class TikTokVibrate
	{
		private const string LogPrefix = "[Vibrate]";

		/// <summary>
		/// Doc mục 15: "tránh gọi lệnh rung liên tục". Cap áp cho CẢ short lẫn long dùng chung một mốc: hai
		/// loại rung chồng nhau trên máy thật cho cảm giác hỏng.
		/// </summary>
		public const float CappingTime = 0.1f;

		/// <summary>Game gán false khi người chơi tắt rung trong Settings.</summary>
		public static bool Enabled = true;

		private static float lastVibrateAt = float.NegativeInfinity;

		/// <summary>Nền tảng hỗ trợ rung. Dùng để ẩn nút bật/tắt rung trong Settings.</summary>
		public static bool IsSupported
		{
			get
			{
#if !UNITY_EDITOR && UNITY_WEBGL
				return TikTokBootstrap.IsReady && CanIUse.VibrateShort && CanIUse.VibrateLong;
#else
				return true;
#endif
			}
		}

		/// <summary>Rung ngắn — phản hồi bấm nút, xác nhận thao tác, lỗi nhẹ.</summary>
		public static void Short()
		{
			if (!ConsumeCap())
				return;

#if !UNITY_EDITOR && UNITY_WEBGL
			if (!TikTokBootstrap.IsReady || !CanIUse.VibrateShort)
				return;
			try
			{
				TT.VibrateShort(new VibrateShortParam
				{
					Fail = error => Log($"short failed: {error?.ErrMsg}")
				});
			}
			catch (Exception e)
			{
				Log($"short exception: {e.Message}");
			}
#else
			Log("short (editor)");
#endif
		}

		/// <summary>Rung dài — nhận thưởng, hoàn thành nhiệm vụ, mua hàng thành công.</summary>
		public static void Long()
		{
			if (!ConsumeCap())
				return;

#if !UNITY_EDITOR && UNITY_WEBGL
			if (!TikTokBootstrap.IsReady || !CanIUse.VibrateLong)
				return;
			try
			{
				TT.VibrateLong(new VibrateLongParam
				{
					Fail = error => Log($"long failed: {error?.ErrMsg}")
				});
			}
			catch (Exception e)
			{
				Log($"long exception: {e.Message}");
			}
#else
			Log("long (editor)");
#endif
		}

		// Time.unscaledTime: rung phải chạy cả khi game pause (timeScale = 0), vd bấm nút trong dialog kết thúc màn.
		private static bool ConsumeCap()
		{
			if (!Enabled || Time.unscaledTime - lastVibrateAt < CappingTime)
				return false;
			lastVibrateAt = Time.unscaledTime;
			return true;
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
