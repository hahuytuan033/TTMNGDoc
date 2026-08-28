using System;
using UnityEngine;

namespace AmobearTTMNG.Ads
{
	/// <summary>
	/// Cổng quảng cáo TỰ CHỨA cho TikTok Mini Game — game gọi thẳng class này, không qua ad manager cũ.
	///
	/// MÔ HÌNH: chỉ gửi một place label (<see cref="AdPlacement"/>) → <see cref="TTAdsProvider"/> →
	/// <see cref="HSBridge"/> → HS JS SDK <c>ads.showAdsRemote(label)</c>. BACKEND CONSOLE giữ mapping
	/// label→Ad ID, cooldown, frequency cap, và cả loại ad. Client không hard-code Ad ID, không đếm cooldown.
	///
	/// PORTABLE: module này KHÔNG biết type nào của game. Thứ đặc thù game được "cắm ngoài" qua hook:
	///   - <see cref="IsAdsActive"/> : game gán = () => !Profile.Instance.IsRemmoveAds
	///   - <see cref="OnRewardGranted"/> : game nối vào analytics (vd Analytic.RewardVideoClaimed)
	/// Xem file glue phía game: <c>TikTokAdsBootstrap.cs</c>.
	/// </summary>
	public static class TikTokAds
	{
		private const string AdLogPrefix = "[Ads]";

		// ---------------------------------------------------------------- hook game cắm vào

		/// <summary>Ads có đang bật không (mặc định: luôn bật). Game gán = () =&gt; !Profile.Instance.IsRemmoveAds.</summary>
		public static Func<bool> IsAdsActive = () => true;

		/// <summary>Bắn khi rewarded xem HẾT (trao thưởng thành công), kèm label. Game nối vào analytics.</summary>
		public static event Action<string> OnRewardGranted;

		// ---------------------------------------------------------------- event/cờ tương thích

		/// <summary>Bắn một lần khi subsystem sẵn sàng (giữ tương thích LoadingRewardShield).</summary>
		public static event Action OnRewardLoaded;
		public static event Action OnRewardUnavailable;
		public static event Action OnInterstitialUnavailable;

		public static bool IsShowingReward { get; private set; }
		public static bool IsShowingInterstitial { get; private set; }
		public static bool IsShowingFullAds => IsShowingReward || IsShowingInterstitial;

		/// <summary>Không còn dùng (cooldown do backend), nhưng giữ để code cũ set được mà không vỡ.</summary>
		public static float BetweenAllowTimeForInterstitial = 45;

		// ---------------------------------------------------------------- init

		private static bool initialized;

		/// <summary>
		/// TỰ CHẠY lúc khởi động (không cần glue) — nhờ vậy game nào chỉ copy folder này + gọi
		/// ShowVideoReward/ShowInterstitial là ads chạy được, KHÔNG bắt buộc viết TikTokAdsBootstrap.
		/// Gọi lại nhiều lần vô hại (đã chặn bằng cờ initialized). Log trạng thái SDK JS và báo sẵn sàng.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void Initialize()
		{
			if (initialized)
				return;
			initialized = true;
#if !UNITY_EDITOR && UNITY_WEBGL
			TTAdsProvider.Prime();
#endif
			LogAdEvent("init", "sdk", errorCode: "ready");
			OnRewardLoaded?.Invoke();
		}

		/// <summary>Báo cho UI (nút reward shield) refresh trạng thái sẵn sàng.</summary>
		public static void NotifyReady()
		{
			OnRewardLoaded?.Invoke();
		}

		// ---------------------------------------------------------------- no-op giữ tương thích

		public static void LoadAds() { }
		public static void PreCachedAds() { }
		public static void RequestLoadReward() { }
		public static void RequestLoadReward(string placement) { }
		public static void PrewarmRewarded(string placement) { }
		public static void PrewarmInterstitial(string placement) { }

		/// <summary>Readiness do backend quyết định lúc show — luôn cho phép thử, kết quả biết khi show.</summary>
		public static bool IsRewardReady() => true;
		public static bool IsRewardReady(string placement) => true;
		public static bool IsReadyInter() => true;
		public static bool IsReadyInter(string placement) => true;

		// ---------------------------------------------------------------- show

		/// <summary>
		/// Show rewarded ở place label <paramref name="placement"/>.
		/// <paramref name="complete"/> CHỈ chạy khi xem HẾT. Không có ad / tắt sớm / lỗi → <see cref="OnRewardUnavailable"/>.
		/// KHÔNG kiểm Remove Ads: rewarded là người chơi tự chọn xem để lấy thưởng.
		/// </summary>
		public static void ShowVideoReward(Action complete, string placement)
		{
#if UNITY_EDITOR || !UNITY_WEBGL
			complete?.Invoke();
#else
			if (IsShowingReward)
			{
				LogAdEvent("show_unavailable", "rewarded", placement, "already_showing");
				OnRewardUnavailable?.Invoke();
				return;
			}

			IsShowingReward = true;
			LogAdEvent("show", "rewarded", placement);

			TTAdsProvider.ShowRewarded(
				placement,
				onReward: () =>
				{
					IsShowingReward = false;
					complete?.Invoke();
					SafeRaiseRewardGranted(placement);
				},
				onUnavailable: () =>
				{
					IsShowingReward = false;
					LogAdEvent("show_unavailable", "rewarded", placement, "no_ad");
					OnRewardUnavailable?.Invoke();
				});
#endif
		}

		/// <summary>
		/// Show interstitial ở place label <paramref name="placement"/>.
		/// GIAO KÈO: <paramref name="complete"/> LUÔN chạy đúng một lần (dù ad hiện, bị bỏ qua, hết cooldown
		/// backend, hay đã mua Remove Ads) — nhiều call site dùng để chuyển scene/về đảo, không gọi là kẹt.
		/// </summary>
		public static void ShowInterstitial(Action complete, string placement)
		{
#if UNITY_EDITOR || !UNITY_WEBGL
			complete?.Invoke();
#else
			if (!IsAdsActiveSafe() || IsShowingInterstitial)
			{
				LogAdEvent("show_unavailable", "interstitial", placement,
					!IsAdsActiveSafe() ? "ads_disabled" : "already_showing");
				complete?.Invoke();
				return;
			}

			IsShowingInterstitial = true;
			LogAdEvent("show", "interstitial", placement);

			TTAdsProvider.ShowInterstitial(
				placement,
				onClosed: () =>
				{
					IsShowingInterstitial = false;
					complete?.Invoke();
				},
				onUnavailable: () =>
				{
					IsShowingInterstitial = false;
					LogAdEvent("show_unavailable", "interstitial", placement, "no_ad");
					complete?.Invoke();
					OnInterstitialUnavailable?.Invoke();
				});
#endif
		}

		// ---------------------------------------------------------------- helpers

		private static bool IsAdsActiveSafe()
		{
			try { return IsAdsActive == null || IsAdsActive(); }
			catch (Exception e) { Debug.LogError($"[Ads] IsAdsActive hook ném exception: {e}"); return true; }
		}

		private static void SafeRaiseRewardGranted(string placement)
		{
			try { OnRewardGranted?.Invoke(placement); }
			catch (Exception e) { Debug.LogError($"[Ads] OnRewardGranted handler ném exception: {e}"); }
		}

		private static void LogAdEvent(string lifecycle, string format, string placement = null, string errorCode = null, string errorMessage = null)
		{
			string normalizedPlacement = string.IsNullOrWhiteSpace(placement) ? "none" : placement;
			string normalizedCode = string.IsNullOrWhiteSpace(errorCode) ? "none" : errorCode;
			string normalizedMessage = string.IsNullOrWhiteSpace(errorMessage) ? "none" : errorMessage.Replace('\n', ' ').Replace('\r', ' ');
			Debug.Log($"{AdLogPrefix} lifecycle={lifecycle} format={format} placement={normalizedPlacement} error_code={normalizedCode} error_message={normalizedMessage}");
		}
	}
}
