using System;
using System.Collections;
using System.Collections.Generic;
using HS.TikTokSDK;
using HS.TikTokSDK.Auth;
using HS.TikTokSDK.IAP;
using UnityEngine;

namespace AmobearTTMNG
{
	/// <summary>
	/// Nối HS TikTok SDK (Đăng nhập + Mua hàng bằng beans + Đồng bộ save lên cloud) vào game. Port từ
	/// HSTikTokIntegration của pocket-2, bỏ hết phần dính vào game — game nghe event / gán delegate.
	///
	/// Phân vai: TTSDK lo ads, storage, share, shortcut/sidebar, safe area, rung, mạng. HS SDK lo đăng nhập (bên
	/// trong vẫn dùng code của TT.Login), IAP, cloud save, chống cheat.
	///
	/// THIẾT LẬP:
	///   1. Scene đầu tiên (Setup) có GameObject HS_TikTokSDK (menu "IAP TikTok Setup/Setup SDK in Scene"), điền
	///      gameId + sdkKey của ĐÚNG game trên console HS.
	///   2. Khai báo sản phẩm trên console HS (mẫu ở tiktok_products.json ở thư mục gốc project).
	///   3. Game nghe <see cref="OnRewardsGranted"/> / <see cref="OnRewardsRevoked"/> để cộng / trừ tài nguyên, và
	///      (nếu dùng cloud save) gán <see cref="SerializeForCloud"/> + <see cref="ApplyCloudData"/>, gọi
	///      <see cref="MarkCloudDirty"/> sau mỗi lần save.
	///
	/// Quà KHÔNG được cấp khi gọi <see cref="BuyProduct"/> — nền tảng gọi ngược sau vài giây, hoặc ở lần đăng nhập
	/// sau nếu người chơi thoát giữa chừng (source = "pending").
	/// </summary>
	public class TikTokHS : MonoBehaviour, ITikTokRewardHandler, ITikTokRefundHandler, ITikTokSyncProvider
	{
		private const string LogPrefix = "[HS]";
		private const string GrantedKeyPrefix = "hs_granted_";
		private const float SdkWaitTimeoutSeconds = 60f;
		private const long DuplicateWindowSeconds = 30;

		public const string SourcePurchase = "purchase";
		public const string SourcePending = "pending";

		private static TikTokHS instance;

		/// <summary>
		/// Quà của một giao dịch cần vào tài khoản: (productId, rewards, source "purchase" | "pending").
		/// Game đổi TikTokReward.type (chuỗi tự do do console đặt, vd "coin", "no_ads") sang tài nguyên của mình.
		/// Đã chặn callback lặp của cùng một đơn ở đây — handler không cần tự chặn. Gói non-consumable (vd no_ads)
	/// thì game tự kiểm "đã có chưa" trước khi bật.
		/// </summary>
		public static event Action<string, List<TikTokReward>, string> OnRewardsGranted;

		/// <summary>TikTok đã hoàn tiền: (productId, rewards cần thu hồi). Backend đã trừ phía server.</summary>
		public static event Action<string, List<TikTokReward>> OnRewardsRevoked;

		/// <summary>Mua thất bại / người chơi huỷ: (thông báo lỗi).</summary>
		public static event Action<string> OnPurchaseFailed;

		/// <summary>Đăng nhập HS xong: (true = ok, lỗi nếu có).</summary>
		public static event Action<bool, string> OnLoginResult;

		/// <summary>Cloud save: trả JSON đọc lại được bởi <see cref="ApplyCloudData"/>. Null = không dùng cloud save.</summary>
		public static Func<string> SerializeForCloud;

		/// <summary>
		/// Server có bản save mới hơn: (json, serverTimestamp). SDK đã so mốc thời gian phía server rồi — chỉ áp dụng.
		/// </summary>
		public static Action<string, long> ApplyCloudData;

		/// <summary>Đã có GameObject HS_TikTokSDK và đã đăng ký handler.</summary>
		public static bool IsReady { get; private set; }

		/// <summary>Đã có phiên đăng nhập HS (cần cho IAP + cloud sync).</summary>
		public static bool IsLoggedIn => TikTokSDK.Instance != null && TikTokSDK.Instance.IsAuthenticated;

		/// <summary>Mốc thời gian server của lần đồng bộ cloud gần nhất (0 = chưa).</summary>
		public static long LastCloudTimestamp { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (instance != null)
				return;
			var host = new GameObject(nameof(TikTokHS));
			DontDestroyOnLoad(host);
			instance = host.AddComponent<TikTokHS>();
		}

		/// <summary>Đánh dấu save cần đẩy lên cloud. Gọi sau mỗi lần game ghi save; SDK tự chọn thời điểm upload.</summary>
		public static void MarkCloudDirty()
		{
			if (instance != null)
				instance.IsDirtyForCloud = true;
		}

		/// <summary>
		/// Đăng nhập. WebGL: TT.Login lấy code tạm → đổi lấy session ở backend HS. Editor: đường mock của SDK
		/// (backend nhận diện qua header X-Is-Test, tạo user test theo device hash). Module tự gọi lúc khởi động.
		/// </summary>
		public static void StartLogin()
		{
			TikTokAuth auth = TikTokAuth.Instance;
			if (auth == null)
			{
				Debug.LogWarning($"{LogPrefix} login skipped: TikTokAuth chưa có (thiếu HS_TikTokSDK trong scene đầu?)");
				return;
			}

#if !UNITY_EDITOR && UNITY_WEBGL
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				if (!sdkReady)
				{
					bool timedOut = TikTokBootstrap.IsInitTimedOut;
					Debug.LogWarning($"{LogPrefix} login skipped: TTSDK init {(timedOut ? "timed out — retrying if it succeeds late" : "failed")}");
					RaiseLogin(false, timedOut ? "ttsdk_init_timeout" : "ttsdk_init_failed");
					// OnLoginResult có thể bắn lần hai (true) nếu InitSDK thành công sau timeout.
					if (timedOut)
						TikTokBootstrap.WhenSdkReady(StartLogin);
					return;
				}
				try
				{
					TTSDK.TT.Login(
						successCallback: code => auth.LoginWithCode(code),
						failedCallback: error =>
						{
							Debug.LogWarning($"{LogPrefix} TT.Login failed: {error}");
							RaiseLogin(false, error);
						});
				}
				catch (Exception e)
				{
					Debug.LogError($"{LogPrefix} TT.Login threw: {e.Message}");
					RaiseLogin(false, e.Message);
				}
			});
#else
			auth.EditorMockLogin();
#endif
		}

		/// <summary>
		/// Mở luồng mua bằng beans. Quà tới qua <see cref="OnRewardsGranted"/>, KHÔNG cấp ở đây.
		/// </summary>
		public static void BuyProduct(string productId)
		{
			if (string.IsNullOrWhiteSpace(productId))
				return;

			TikTokIAP iap = TikTokIAP.Instance;
			if (iap == null)
			{
				Debug.LogWarning($"{LogPrefix} buy skipped: TikTokIAP chưa có product={productId}");
				RaisePurchaseFailed("iap_not_ready");
				return;
			}

			Log($"buy started product={productId}");
			iap.BuyProduct(productId);
		}

		/// <summary>Lấy danh sách sản phẩm đã khai báo trên console HS: (ok, products, error).</summary>
		public static void GetShopItems(Action<bool, List<TikTokProduct>, string> callback)
		{
			TikTokIAP iap = TikTokIAP.Instance;
			if (iap == null)
			{
				callback?.Invoke(false, null, "iap_not_ready");
				return;
			}
			iap.GetShopItems(callback);
		}

		/// <summary>Đẩy save lên cloud ngay (vd trước khi đổi tài khoản). Bình thường SDK tự đồng bộ theo chu kỳ.</summary>
		public static void SyncNow(Action<bool> onComplete = null)
		{
			var sync = HS.TikTokSDK.Sync.TikTokSync.Instance;
			if (sync == null)
			{
				onComplete?.Invoke(false);
				return;
			}
			sync.SyncNow(onComplete);
		}

		private void Start()
		{
			StartCoroutine(RegisterWhenSdkReady());
		}

		/// <summary>
		/// Chờ GameObject HS_TikTokSDK xuất hiện rồi mới đăng ký: nếu nó nằm ở scene không phải scene đầu tiên thì
		/// lúc Start TikTokSDK.Instance vẫn null. Bỏ cuộc ngay là mất trắng IAP + cloud sync mà chỉ còn một dòng log.
		/// </summary>
		private IEnumerator RegisterWhenSdkReady()
		{
			float deadline = Time.unscaledTime + SdkWaitTimeoutSeconds;
			while (TikTokSDK.Instance == null)
			{
				if (Time.unscaledTime > deadline)
				{
					Debug.LogError($"{LogPrefix} không thấy HS_TikTokSDK sau {SdkWaitTimeoutSeconds}s — đặt nó vào scene đầu tiên bằng menu \"IAP TikTok Setup/Setup SDK in Scene\".");
					yield break;
				}
				yield return null;
			}

			TikTokSDK sdk = TikTokSDK.Instance;
			if (!sdk.IsConfigured)
				Debug.LogWarning($"{LogPrefix} HS_TikTokSDK chưa điền gameId / sdkKey — login / IAP sẽ lỗi.");

			sdk.RegisterRewardHandler(this);
			sdk.RegisterSyncProvider(this);

			TikTokIAP iap = TikTokIAP.Instance;
			if (iap != null)
				iap.OnPurchaseFailed += HandlePurchaseFailed;

			TikTokAuth auth = TikTokAuth.Instance;
			if (auth != null)
			{
				auth.OnLoginSuccess += HandleLoginSuccess;
				auth.OnLoginFailed += HandleLoginFailed;
			}

			IsReady = true;
			Log("handlers registered");

			// SDK không tự đăng nhập. Không login thì không có IAP lẫn cloud sync, và đơn treo từ phiên trước không
			// bao giờ được phát lại — nên login ngay thay vì chờ người chơi mở shop.
			StartLogin();
		}

		private void OnDestroy()
		{
			TikTokIAP iap = TikTokIAP.Instance;
			if (iap != null)
				iap.OnPurchaseFailed -= HandlePurchaseFailed;

			TikTokAuth auth = TikTokAuth.Instance;
			if (auth != null)
			{
				auth.OnLoginSuccess -= HandleLoginSuccess;
				auth.OnLoginFailed -= HandleLoginFailed;
			}
		}

		private void HandlePurchaseFailed(string error)
		{
			Debug.LogWarning($"{LogPrefix} buy failed: {error}");
			RaisePurchaseFailed(error);
		}

		private void HandleLoginSuccess(string profileJson, long serverTimestamp)
		{
			Log($"login ok server_ts={serverTimestamp}");
			RaiseLogin(true, null);
		}

		private void HandleLoginFailed(string error)
		{
			Debug.LogWarning($"{LogPrefix} login failed: {error}");
			RaiseLogin(false, error);
		}

		// ---------------------------------------------------------------- ITikTokRewardHandler

		public void OnPurchaseRewardsGranted(string productId, List<TikTokReward> rewards)
		{
			ApplyRewards(productId, rewards, SourcePurchase);
		}

		/// <summary>
		/// Đơn đã trả tiền nhưng chưa kịp giao ở phiên trước. SDK cam kết chỉ gọi một lần mỗi đơn, nhưng vẫn qua
		/// hàng rào chống trùng: cam kết đó ở phía server, còn ở client mỗi lần cấp đúp là một lỗ hổng kinh tế.
		/// </summary>
		public void OnPendingRewardsRecovered(string productId, List<TikTokReward> rewards)
		{
			ApplyRewards(productId, rewards, SourcePending);
		}

		// ---------------------------------------------------------------- ITikTokRefundHandler

		public void OnRefundNotified(string productId, List<TikTokReward> revokedRewards)
		{
			// Xoá mốc chống lặp: hoàn tiền rồi mua lại ngay món đó vẫn phải nhận được quà.
			TikTokStorage.DeleteKey(GrantedKeyPrefix + productId);
			TikTokStorage.Save();
			Log($"refund product={productId} rewards={Describe(revokedRewards)}");

			try { OnRewardsRevoked?.Invoke(productId, revokedRewards ?? new List<TikTokReward>()); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnRewardsRevoked handler threw: {e}"); }
		}

		// ---------------------------------------------------------------- ITikTokSyncProvider

		public bool IsDirtyForCloud { get; set; }

		// Explicit: tên SerializeForCloud đã dùng cho delegate static mà game gán.
		string ITikTokSyncProvider.SerializeForCloud()
		{
			if (SerializeForCloud == null)
				return "{}";
			try { return SerializeForCloud() ?? "{}"; }
			catch (Exception e)
			{
				Debug.LogError($"{LogPrefix} SerializeForCloud threw: {e}");
				return "{}";
			}
		}

		/// <summary>Đẩy lên thành công. KHÔNG bật lại IsDirtyForCloud — nếu không sẽ thành vòng lặp đồng bộ vô tận.</summary>
		public void OnCloudSyncSuccess(long serverTimestamp)
		{
			LastCloudTimestamp = serverTimestamp;
			Log($"cloud sync ok server_ts={serverTimestamp}");
		}

		public void OnCloudDataReceived(string profileJson, long serverTimestamp)
		{
			if (string.IsNullOrWhiteSpace(profileJson) || profileJson == "{}")
			{
				Log("cloud: tài khoản chưa có save trên cloud");
				return;
			}
			if (ApplyCloudData == null)
			{
				Log("cloud: có bản save mới hơn nhưng game chưa gán ApplyCloudData — bỏ qua");
				return;
			}

			try
			{
				ApplyCloudData(profileJson, serverTimestamp);
				LastCloudTimestamp = serverTimestamp;
				Log($"cloud applied server_ts={serverTimestamp}");
			}
			catch (Exception e)
			{
				// Cloud hỏng thì giữ local. Lần save sau sẽ đẩy bản local đè lên.
				Debug.LogError($"{LogPrefix} ApplyCloudData threw: {e}");
			}
		}

		// ---------------------------------------------------------------- grant

		private static void ApplyRewards(string productId, List<TikTokReward> rewards, string source)
		{
			if (string.IsNullOrWhiteSpace(productId))
			{
				Debug.LogWarning($"{LogPrefix} reward skipped: missing productId source={source}");
				return;
			}

			// Chặn callback lặp của CÙNG một đơn (purchase + pending tới sát nhau). KHÔNG chặn vĩnh viễn theo productId
			// như bản pocket-2: gói tiêu hao (coin pack) mua lần hai sẽ không bao giờ nhận được quà. Chống trùng giữa
			// các phiên do SDK đảm bảo (mỗi đơn chỉ gọi một lần).
			string grantKey = GrantedKeyPrefix + productId;
			long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			long last = long.TryParse(TikTokStorage.GetString(grantKey), out long parsed) ? parsed : 0;
			if (last > 0 && now - last >= 0 && now - last < DuplicateWindowSeconds)
			{
				Log($"reward duplicate ignored product={productId} source={source} ({now - last}s after previous)");
				return;
			}

			// Ghi mốc TRƯỚC, bắn event cấp quà SAU — cộng trước rồi chết giữa chừng là cấp đúp lặp lại được.
			TikTokStorage.SetString(grantKey, now.ToString());
			TikTokStorage.Save();
			Log($"reward granted product={productId} source={source} rewards={Describe(rewards)}");

			try { OnRewardsGranted?.Invoke(productId, rewards ?? new List<TikTokReward>(), source); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnRewardsGranted handler threw: {e}"); }
		}

		/// <summary>Chỉ để test: xoá mốc chống lặp của một sản phẩm.</summary>
		public static void ResetGrantedFlag(string productId)
		{
			TikTokStorage.DeleteKey(GrantedKeyPrefix + productId);
			TikTokStorage.Save();
		}

		private static string Describe(List<TikTokReward> rewards)
		{
			if (rewards == null || rewards.Count == 0)
				return "none";
			var parts = new List<string>(rewards.Count);
			foreach (var r in rewards)
				if (r != null)
					parts.Add($"{r.type}:{r.amount}");
			return string.Join(",", parts);
		}

		private static void RaiseLogin(bool ok, string error)
		{
			try { OnLoginResult?.Invoke(ok, error); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnLoginResult handler threw: {e}"); }
		}

		private static void RaisePurchaseFailed(string error)
		{
			try { OnPurchaseFailed?.Invoke(error); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnPurchaseFailed handler threw: {e}"); }
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
