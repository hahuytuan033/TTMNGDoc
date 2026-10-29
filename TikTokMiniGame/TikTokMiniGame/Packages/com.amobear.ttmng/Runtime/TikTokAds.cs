using System;
using UnityEngine;

namespace AmobearTTMNG.Ads
{
	/// <summary>
	/// Cổng quảng cáo DUY NHẤT game gọi. Theo hướng dẫn chính thức của TikTok (In-App Ads):
	///   1. Trên Developer Portal: app → Operation → Monetization → In-App Ads → Add ad placement (Rewarded /
	///      Interstitial) → chuyển Active → copy Placement ID.
	///   2. Điền vào <see cref="TikTokAdsConfig"/> (Resources/TikTokAdsConfig): label → loại → Placement ID.
	///   3. Game gọi theo label:
	///        TikTokAds.ShowVideoReward(() =&gt; GiveReward(), "rw_revive");     // chỉ chạy khi xem HẾT
	///        TikTokAds.ShowInterstitial(() =&gt; NextScene(), "it_level_end");  // LUÔN chạy đúng một lần
	///
	/// LUẬT ĐƯỢC CHẶN SẴN Ở ĐÂY:
	///   - Mỗi lúc chỉ một ad (rewarded và interstitial không đè nhau — lỗi 11006).
	///   - Interstitial: không trong 15s đầu sau khi module ads khởi động, cách nhau ≥ 30s (lỗi 11004).
	///   - Rewarded chỉ thưởng khi isEnded = true (đóng sớm = lỗi 30001, không thưởng).
	/// Vị trí đặt ad là việc của game: interstitial chỉ ở chỗ nghỉ tự nhiên (hết màn, chuyển scene), KHÔNG giữa lúc
	/// đang chơi; rewarded chỉ khi người chơi tự bấm nút có ghi rõ phần thưởng.
	///
	/// Editor: không có container — giả lập "xem hết" (hoặc "không có ad" khi bật <see cref="EditorSimulateNoFill"/>).
	/// Test thật: DevTool → tắt Enable IAA Mock.
	/// </summary>
	public static class TikTokAds
	{
		private const string LogPrefix = "[Ads]";

		/// <summary>Ads có bật không (mặc định luôn bật). Game gán = () =&gt; !Profile.NoAds. Chỉ chặn interstitial.</summary>
		public static Func<bool> IsAdsActive = () => true;

		/// <summary>Tắt tiếng game trong lúc ad hiện, bật lại khi đóng.</summary>
		public static bool PauseAudioDuringAds = true;

		/// <summary>Chỉ Editor: giả lập không có ad để test nhánh unavailable.</summary>
		public static bool EditorSimulateNoFill;

		/// <summary>Rewarded xem HẾT (đã trao thưởng), kèm label. Game nối vào analytics.</summary>
		public static event Action<string> OnRewardGranted;

		/// <summary>Rewarded không trao thưởng (không có ad / đóng sớm / lỗi / chưa cấu hình): (label, lý do).</summary>
		public static event Action<string, string> OnRewardUnavailable;

		/// <summary>Interstitial không hiện (bị luật chặn / không có ad / lỗi): (label, lý do). Callback vẫn đã chạy.</summary>
		public static event Action<string, string> OnInterstitialUnavailable;

		public static bool IsShowingReward { get; private set; }
		public static bool IsShowingInterstitial { get; private set; }
		public static bool IsShowingFullAds => IsShowingReward || IsShowingInterstitial;

		private static bool initialized;
		private static bool audioWasPaused;

		/// <summary>Tự chạy lúc khởi động: chờ InitSDK rồi tải sẵn các placement rewarded trong config.</summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void Initialize()
		{
			if (initialized)
				return;
			initialized = true;

			var config = TikTokAdsConfig.Load();
			if (config == null)
			{
				Debug.LogWarning($"{LogPrefix} thiếu Resources/{TikTokAdsConfig.ResourcePath} — mọi ad coi như không có.");
				return;
			}

#if !UNITY_EDITOR && UNITY_WEBGL
			// WhenSdkReady: InitSDK hết timeout rồi thành công muộn vẫn preload.
			TikTokBootstrap.WhenSdkReady(() =>
			{
				if (!config.PreloadRewarded)
					return;
				foreach (var p in config.Placements)
					if (p != null && p.Type == TikTokAdType.Rewarded && !string.IsNullOrEmpty(p.PlacementId))
						TTAdsProvider.PreloadRewarded(p.PlacementId);
			});
#endif
			Log($"init placements={config.Placements.Count}");
		}

		/// <summary>Label đã khai báo đúng loại và có Placement ID chưa (dùng để ẩn nút rewarded khi chưa cấu hình).</summary>
		public static bool IsConfigured(string label, TikTokAdType type)
		{
			var p = TikTokAdsConfig.Load()?.Find(label);
			return p != null && p.Type == type && !string.IsNullOrEmpty(p.PlacementId);
		}

		/// <summary>
		/// Rewarded ở <paramref name="label"/>. <paramref name="complete"/> CHỈ chạy khi xem HẾT. Mọi trường hợp khác
		/// → <see cref="OnRewardUnavailable"/> (game PHẢI xử lý để nút / popup không kẹt). Không xét Remove Ads.
		/// </summary>
		public static void ShowVideoReward(Action complete, string label)
		{
			if (IsShowingFullAds)
			{
				RewardUnavailable(label, "already_showing");
				return;
			}
			if (!TryResolve(label, TikTokAdType.Rewarded, out var config, out string placementId, out string reason))
			{
				RewardUnavailable(label, reason);
				return;
			}

			IsShowingReward = true;
			BeginAd();
			Log($"show rewarded label={label}");

#if UNITY_EDITOR || !UNITY_WEBGL
			EndAd();
			IsShowingReward = false;
			if (EditorSimulateNoFill)
				RewardUnavailable(label, "editor_no_fill");
			else
				RewardGranted(label, complete);
#else
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				if (!sdkReady)
				{
					EndAd();
					IsShowingReward = false;
					RewardUnavailable(label, "sdk_not_ready");
					return;
				}
				TTAdsProvider.ShowRewarded(label, placementId, config.ShowTimeoutSeconds,
					onReward: () =>
					{
						EndAd();
						IsShowingReward = false;
						RewardGranted(label, complete);
					},
					onUnavailable: why =>
					{
						EndAd();
						IsShowingReward = false;
						RewardUnavailable(label, why);
					});
			});
#endif
		}

		/// <summary>
		/// Interstitial ở <paramref name="label"/>. GIAO KÈO: <paramref name="complete"/> LUÔN chạy đúng một lần (ad hiện,
		/// bị luật chặn, không có ad, Remove Ads...) — dùng để chuyển scene / chạy tiếp luồng game.
		/// </summary>
		public static void ShowInterstitial(Action complete, string label)
		{
			if (!SafeIsAdsActive())
			{
				InterstitialSkipped(label, "ads_disabled", complete);
				return;
			}
			if (IsShowingFullAds)
			{
				InterstitialSkipped(label, "already_showing", complete);
				return;
			}
			if (!TryResolve(label, TikTokAdType.Interstitial, out var config, out string placementId, out string reason))
			{
				InterstitialSkipped(label, reason, complete);
				return;
			}

#if UNITY_EDITOR || !UNITY_WEBGL
			Log($"show interstitial (editor) label={label}");
			SafeInvoke(complete);
#else
			if (!TTAdsProvider.CanShowInterstitial(config, out string ruleReason))
			{
				InterstitialSkipped(label, ruleReason, complete);
				return;
			}

			IsShowingInterstitial = true;
			BeginAd();
			Log($"show interstitial label={label}");
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				if (!sdkReady)
				{
					EndAd();
					IsShowingInterstitial = false;
					InterstitialSkipped(label, "sdk_not_ready", complete);
					return;
				}
				TTAdsProvider.ShowInterstitial(label, placementId, config.ShowTimeoutSeconds,
					onClosed: () =>
					{
						EndAd();
						IsShowingInterstitial = false;
						SafeInvoke(complete);
					},
					onUnavailable: why =>
					{
						EndAd();
						IsShowingInterstitial = false;
						InterstitialSkipped(label, why, complete);
					});
			});
#endif
		}

		// ---------------------------------------------------------------- helpers

		private static bool TryResolve(string label, TikTokAdType type, out TikTokAdsConfig config, out string placementId, out string reason)
		{
			placementId = null;
			config = TikTokAdsConfig.Load();
			if (config == null)
			{
				reason = "no_config";
				return false;
			}
			var p = config.Find(label);
			if (p == null)
			{
				reason = "label_not_configured";
				return false;
			}
			if (p.Type != type)
			{
				reason = $"wrong_type({p.Type})";
				return false;
			}
			if (string.IsNullOrEmpty(p.PlacementId))
			{
				reason = "empty_placement_id";
				return false;
			}
			placementId = p.PlacementId;
			reason = null;
			return true;
		}

		private static void BeginAd()
		{
			if (!PauseAudioDuringAds)
				return;
			audioWasPaused = AudioListener.pause;
			AudioListener.pause = true;
		}

		private static void EndAd()
		{
			if (PauseAudioDuringAds)
				AudioListener.pause = audioWasPaused;
		}

		private static void RewardGranted(string label, Action complete)
		{
			Log($"rewarded granted label={label}");
			SafeInvoke(complete);
			try { OnRewardGranted?.Invoke(label); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnRewardGranted handler threw: {e}"); }
		}

		private static void RewardUnavailable(string label, string reason)
		{
			Log($"rewarded unavailable label={label} reason={reason}");
			try { OnRewardUnavailable?.Invoke(label, reason); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnRewardUnavailable handler threw: {e}"); }
		}

		private static void InterstitialSkipped(string label, string reason, Action complete)
		{
			Log($"interstitial skipped label={label} reason={reason}");
			SafeInvoke(complete);
			try { OnInterstitialUnavailable?.Invoke(label, reason); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnInterstitialUnavailable handler threw: {e}"); }
		}

		private static bool SafeIsAdsActive()
		{
			try { return IsAdsActive == null || IsAdsActive(); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} IsAdsActive hook threw: {e}"); return true; }
		}

		private static void SafeInvoke(Action callback)
		{
			try { callback?.Invoke(); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} game callback threw: {e}"); }
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
