using System;
using System.Collections.Generic;
using TTSDK;
using UnityEngine;

namespace AmobearTTMNG.Ads
{
	/// <summary>
	/// Gọi thẳng API quảng cáo của TikTok theo Placement ID (doc In-App Ads + TikTok SDK for Unity):
	///   - Rewarded    : TT.CreateRewardedVideoAd(new CreateRewardedVideoAdParam { AdUnitId })
	///                   OnClose(isEnded) — CHỈ thưởng khi isEnded = true. OnError(code, msg).
	///   - Interstitial: TT.CreateInterstitialAd(new CreateInterstitialAdParam { InterstitialAdId })
	///                   OnClose() / OnError(code, msg).
	/// Mỗi instance chỉ show được MỘT lần (lỗi 11003 nếu dùng lại) → show xong là Destroy và tạo instance mới.
	///
	/// Quy định interstitial của TikTok được chặn ngay ở client (tránh gọi rồi ăn lỗi 11004):
	///   không show trong 15s đầu sau khi module ads khởi động, hai lần cách nhau ≥ 30s.
	///
	/// Cam kết: mỗi lần Show gọi ĐÚNG MỘT callback (kể cả khi container im lặng — có timeout), để lọt là game
	/// treo ở popup chờ thưởng. Chạy trên container hoặc UI mock của TTSDK trong Editor.
	/// </summary>
	internal static class TTAdsProvider
	{
		private const string LogPrefix = "[Ads]";

		private static float moduleStartedAt = -1f;
		private static float lastInterstitialClosedAt = float.NegativeInfinity;
		private static readonly Dictionary<string, RewardedSlot> rewardedSlots = new Dictionary<string, RewardedSlot>();

		/// <summary>Doc: "gọi bất kỳ API ads nào là module ads khởi động" — mốc của luật 15s.</summary>
		private static void MarkModuleStarted()
		{
			if (moduleStartedAt < 0f)
				moduleStartedAt = Time.realtimeSinceStartup;
		}

		// ---------------------------------------------------------------- rewarded

		/// <summary>Tạo sẵn instance để lần bấm đầu hiện nhanh. Gọi lại nhiều lần vô hại.</summary>
		public static void PreloadRewarded(string placementId)
		{
			if (string.IsNullOrEmpty(placementId) || rewardedSlots.ContainsKey(placementId))
				return;
			var slot = RewardedSlot.Create(placementId);
			if (slot != null)
				rewardedSlots[placementId] = slot;
		}

		public static void ShowRewarded(string label, string placementId, float timeoutSeconds, Action onReward, Action<string> onUnavailable)
		{
			// Instance dùng một lần: lấy ra khỏi cache, show xong tạo cái mới cho lần sau.
			if (!rewardedSlots.TryGetValue(placementId, out var slot) || slot.Failed)
			{
				slot?.Destroy();
				slot = RewardedSlot.Create(placementId);
			}
			rewardedSlots.Remove(placementId);

			if (slot == null)
			{
				onUnavailable?.Invoke("create_failed");
				return;
			}

			var once = new Once(timeoutSeconds, () =>
			{
				Debug.LogWarning($"{LogPrefix} rewarded '{label}' timeout {timeoutSeconds}s — container không gọi lại");
				slot.Destroy();
				onUnavailable?.Invoke("timeout");
			});

			slot.Show(
				onClose: isEnded => once.Run(() =>
				{
					slot.Destroy();
					PreloadRewarded(placementId);
					if (isEnded)
					{
						onReward?.Invoke();
					}
					else
					{
						Log($"rewarded '{label}' closed early — no reward");
						onUnavailable?.Invoke("closed_early");
					}
				}),
				onError: (code, msg) => once.Run(() =>
				{
					slot.Destroy();
					// Không tạo lại ngay sau lỗi: gọi cùng ad unit quá dày sẽ dính 30001.
					Debug.LogWarning($"{LogPrefix} rewarded '{label}' error code={code} msg={msg} → {Describe(code)}");
					onUnavailable?.Invoke("error_" + code);
				}));
		}

		// ---------------------------------------------------------------- interstitial

		/// <summary>Luật 15s / 30s của TikTok. False kèm lý do nếu chưa được phép show.</summary>
		public static bool CanShowInterstitial(TikTokAdsConfig config, out string reason)
		{
			float now = Time.realtimeSinceStartup;
			float startDelay = config != null ? config.InterstitialStartDelaySeconds : 15f;
			float interval = config != null ? config.InterstitialIntervalSeconds : 30f;

			if (moduleStartedAt < 0f || now - moduleStartedAt < startDelay)
			{
				MarkModuleStarted();
				reason = $"within first {startDelay:0}s of ads module";
				return false;
			}
			if (now - lastInterstitialClosedAt < interval)
			{
				reason = $"interval {now - lastInterstitialClosedAt:0}s < {interval:0}s";
				return false;
			}
			reason = null;
			return true;
		}

		public static void ShowInterstitial(string label, string placementId, float timeoutSeconds, Action onClosed, Action<string> onUnavailable)
		{
			MarkModuleStarted();

			TTInterstitialAd ad;
			try
			{
				ad = TT.CreateInterstitialAd(new CreateInterstitialAdParam { InterstitialAdId = placementId });
			}
			catch (Exception e)
			{
				Debug.LogError($"{LogPrefix} CreateInterstitialAd threw: {e.Message}");
				onUnavailable?.Invoke("create_exception");
				return;
			}
			if (ad == null)
			{
				onUnavailable?.Invoke("create_null");
				return;
			}

			var once = new Once(timeoutSeconds, () =>
			{
				Debug.LogWarning($"{LogPrefix} interstitial '{label}' timeout {timeoutSeconds}s");
				SafeDestroy(ad);
				onUnavailable?.Invoke("timeout");
			});

			ad.OnClose += () => once.Run(() =>
			{
				lastInterstitialClosedAt = Time.realtimeSinceStartup;
				SafeDestroy(ad);
				onClosed?.Invoke();
			});
			ad.OnError += (code, msg) => once.Run(() =>
			{
				SafeDestroy(ad);
				Debug.LogWarning($"{LogPrefix} interstitial '{label}' error code={code} msg={msg} → {Describe(code)}");
				onUnavailable?.Invoke("error_" + code);
			});

			try
			{
				ad.Show();
			}
			catch (Exception e)
			{
				once.Run(() =>
				{
					SafeDestroy(ad);
					Debug.LogError($"{LogPrefix} interstitial Show threw: {e.Message}");
					onUnavailable?.Invoke("show_exception");
				});
			}
		}

		// ---------------------------------------------------------------- helpers

		/// <summary>Mã lỗi chính thức (Error Code Reference → IAA).</summary>
		internal static string Describe(int code)
		{
			switch (code)
			{
				case 11001: return "placement ID rỗng — kiểm tra TikTokAdsConfig";
				case 10001: return "tham số không hợp lệ";
				case 20000: return "lỗi mạng khi xác thực placement — thử lại sau";
				case 20001: return "placement không tồn tại — tạo trên Developer Portal";
				case 20002: return "placement không thuộc game này — sai Placement ID";
				case 20003: return "placement chưa Active — bật Active trên Portal";
				case 20004: return "game chưa bật IAA — bật trên Portal";
				case 20007: return "sai loại placement (rewarded/interstitial)";
				case 30001: return "gọi quá dày / người chơi đóng rewarded trước khi xem hết";
				case 11002: return "instance không hợp lệ";
				case 11003: return "instance đã show rồi — phải tạo mới";
				case 11004: return "interstitial chưa đủ khoảng cách (15s đầu / 30s giữa hai lần)";
				case 11005: return "interstitial vượt số lần hiển thị";
				case 11006: return "đang có ad khác hiện — show tuần tự";
				case 50000: return "lỗi hệ thống — gửi support kèm inner_error_code";
				default: return "xem Error Code Reference";
			}
		}

		private static void SafeDestroy(TTInterstitialAd ad)
		{
			try { ad?.Destroy(); }
			catch (Exception) { /* SDK tự huỷ sau khi show / lỗi — gọi lại vô hại */ }
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");

		/// <summary>Instance rewarded kèm handler gắn từ lúc tạo (lỗi tải trước khi show cũng bắt được).</summary>
		private sealed class RewardedSlot
		{
			private TTRewardedVideoAd ad;
			private Action<bool> pendingClose;
			private Action<int, string> pendingError;

			public bool Failed { get; private set; }

			public static RewardedSlot Create(string placementId)
			{
				MarkModuleStarted();
				try
				{
					var ad = TT.CreateRewardedVideoAd(new CreateRewardedVideoAdParam { AdUnitId = placementId });
					if (ad == null)
					{
						Debug.LogWarning($"{LogPrefix} CreateRewardedVideoAd returned null id={placementId}");
						return null;
					}
					var slot = new RewardedSlot { ad = ad };
					ad.OnClose += isEnded => slot.pendingClose?.Invoke(isEnded);
					ad.OnError += (code, msg) =>
					{
						if (slot.pendingError != null)
						{
							slot.pendingError(code, msg);
							return;
						}
						// Lỗi lúc tải sẵn: đánh dấu hỏng, lần show sau tạo instance mới.
						slot.Failed = true;
						Debug.LogWarning($"{LogPrefix} rewarded preload error code={code} msg={msg} → {Describe(code)}");
					};
					return slot;
				}
				catch (Exception e)
				{
					Debug.LogError($"{LogPrefix} CreateRewardedVideoAd threw: {e.Message}");
					return null;
				}
			}

			public void Show(Action<bool> onClose, Action<int, string> onError)
			{
				pendingClose = onClose;
				pendingError = onError;
				try
				{
					ad.Show();
				}
				catch (Exception e)
				{
					Debug.LogError($"{LogPrefix} rewarded Show threw: {e.Message}");
					onError(-1, e.Message);
				}
			}

			public void Destroy()
			{
				pendingClose = null;
				pendingError = null;
				try { ad?.Destroy(); }
				catch (Exception) { /* đã huỷ */ }
				ad = null;
			}
		}

		/// <summary>Đảm bảo chỉ một trong các nhánh (close / error / timeout) chạy.</summary>
		private sealed class Once
		{
			private bool done;
			private readonly int timeoutId;

			public Once(float timeoutSeconds, Action onTimeout)
			{
				timeoutId = AdsTimeoutRunner.Schedule(timeoutSeconds, () => Run(onTimeout));
			}

			public void Run(Action action)
			{
				if (done)
					return;
				done = true;
				AdsTimeoutRunner.Cancel(timeoutId);
				try { action?.Invoke(); }
				catch (Exception e) { Debug.LogError($"{LogPrefix} ad callback threw: {e}"); }
			}
		}
	}

	/// <summary>Bộ hẹn giờ realtime (chạy cả khi timeScale = 0) cho timeout của ads.</summary>
	internal sealed class AdsTimeoutRunner : MonoBehaviour
	{
		private struct Entry
		{
			public int Id;
			public float Deadline;
			public Action Action;
		}

		private static AdsTimeoutRunner instance;
		private static int nextId;
		private readonly List<Entry> entries = new List<Entry>();
		private readonly List<Action> due = new List<Action>();

		public static int Schedule(float seconds, Action action)
		{
			if (instance == null)
			{
				var go = new GameObject(nameof(AdsTimeoutRunner));
				DontDestroyOnLoad(go);
				instance = go.AddComponent<AdsTimeoutRunner>();
			}
			int id = ++nextId;
			instance.entries.Add(new Entry { Id = id, Deadline = Time.realtimeSinceStartup + seconds, Action = action });
			return id;
		}

		public static void Cancel(int id)
		{
			if (instance != null)
				instance.entries.RemoveAll(e => e.Id == id);
		}

		private void Update()
		{
			if (entries.Count == 0)
				return;
			float now = Time.realtimeSinceStartup;
			// Gom trước rồi mới chạy: action có thể Schedule/Cancel làm đổi danh sách.
			due.Clear();
			for (int i = entries.Count - 1; i >= 0; i--)
			{
				if (entries[i].Deadline > now)
					continue;
				due.Add(entries[i].Action);
				entries.RemoveAt(i);
			}
			foreach (var action in due)
				action?.Invoke();
		}
	}
}
