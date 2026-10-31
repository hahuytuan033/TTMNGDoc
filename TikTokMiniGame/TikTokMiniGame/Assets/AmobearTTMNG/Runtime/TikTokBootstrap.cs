using System;
using System.Collections.Generic;
using TTSDK;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmobearTTMNG
{
	/// <summary>
	/// Đặt trong scene khởi động (Setup, build index 0). Gọi TT.InitSDK một lần, đăng ký
	/// OnShow/OnHide của container rồi chuyển sang scene game.
	///
	/// Game code chờ SDK sẵn sàng qua <see cref="WhenReady"/> trước khi gọi API TT.* khác.
	/// Ads (TikTokAds) chờ InitSDK qua <see cref="WhenReady"/> rồi mới gọi TT.Create*Ad.
	/// </summary>
	public class TikTokBootstrap : MonoBehaviour
	{
		[Tooltip("Scene load sau khi InitSDK xong (kể cả khi lỗi). Để trống = ở lại scene hiện tại.")]
		[SerializeField] private string nextScene = "MainMenu";

		[Tooltip("Tắt âm thanh khi game bị đưa xuống nền (OnHide), bật lại khi OnShow.")]
		[SerializeField] private bool muteAudioWhenHidden = true;

		[Tooltip("Hết thời gian chờ callback InitSDK thì vẫn vào game (Editor không bao giờ gọi callback).")]
		[SerializeField] private float initTimeoutSeconds = 8f;

		[Tooltip("Gọi Silent Login (tt.login) khi SDK sẵn sàng. Checklist duyệt của TikTok yêu cầu — chỉ tắt nếu game tự gọi TikTokLogin.Login.")]
		[SerializeField] private bool silentLogin = true;

		[SerializeField] private int targetFrameRate = 60;

		public static TikTokBootstrap Instance { get; private set; }

		/// <summary>InitSDK đã có kết quả (thành công hoặc lỗi).</summary>
		public static bool IsInitDone { get; private set; }

		/// <summary>InitSDK trả code 0 — được phép gọi API TT.*.</summary>
		public static bool IsReady { get; private set; }

		/// <summary>Mã trả về của InitSDK. 1 = SDK không hỗ trợ, 2 = Unity không hỗ trợ, -1 = exception, -2 = timeout.</summary>
		public static int InitCode { get; private set; } = -1;

		/// <summary>
		/// Hết thời gian chờ InitSDK, game đang chạy tiếp ở chế độ không SDK. Callback vẫn có thể đến muộn và chuyển
		/// sang <see cref="IsReady"/> — ai cần SDK thì đăng ký <see cref="WhenSdkReady"/> để chạy lại lúc đó.
		/// </summary>
		public static bool IsInitTimedOut => IsInitDone && InitCode == -2;

		/// <summary>Game bị đưa xuống nền.</summary>
		public static event Action OnHide;

		/// <summary>Game quay lại foreground.</summary>
		public static event Action OnShow;

		/// <summary>Như <see cref="OnShow"/>, kèm tham số container (vd "query" khi mở từ tin nhắn share).</summary>
		public static event Action<Dictionary<string, object>> OnShowWithParams;

		private static event Action<bool> InitCompleted;

		private static event Action SdkReady;

		/// <summary>
		/// Gọi <paramref name="callback"/>(IsReady) ngay nếu InitSDK đã xong, hoặc khi nó xong. Hết timeout thì nhận
		/// false dù sau đó SDK có thể sẵn sàng muộn — tác vụ khởi động cần SDK nên dùng <see cref="WhenSdkReady"/>.
		/// </summary>
		public static void WhenReady(Action<bool> callback)
		{
			if (callback == null)
				return;
			if (IsInitDone)
				callback(IsReady);
			else
				InitCompleted += callback;
		}

		/// <summary>
		/// Gọi <paramref name="callback"/> một lần khi SDK sẵn sàng: ngay nếu đã sẵn sàng, hoặc khi InitSDK thành công —
		/// kể cả callback đến SAU timeout. InitSDK lỗi thật thì không bao giờ gọi.
		/// </summary>
		public static void WhenSdkReady(Action callback)
		{
			if (callback == null)
				return;
			if (IsReady)
				callback();
			else
				SdkReady += callback;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);

			if (targetFrameRate > 0)
				Application.targetFrameRate = targetFrameRate;
		}

		private void Start()
		{
			if (IsInitDone)
				return;

			Debug.Log($"[TikTokBootstrap] InitSDK... InContainerEnv={TT.InContainerEnv}");
			try
			{
				TT.InitSDK((code, env) => OnInitResult(code));
			}
			catch (Exception e)
			{
				// Editor với SDK < 1.1.1 ném lỗi đồng bộ; vẫn cho game chạy tiếp ở chế độ không SDK.
				Debug.LogError($"[TikTokBootstrap] InitSDK threw: {e.Message}");
				FinishInit(-1);
				return;
			}

			if (!IsInitDone)
				Invoke(nameof(OnInitTimeout), InitTimeout);
		}

		// Editor không có container nên không chờ lâu.
		private float InitTimeout => Application.isEditor ? 1f : initTimeoutSeconds;

		private void OnInitTimeout()
		{
			if (IsInitDone)
				return;
			Debug.LogWarning($"[TikTokBootstrap] InitSDK no callback after {InitTimeout}s, continuing without SDK.");
			FinishInit(-2);
		}

		private void OnInitResult(int code)
		{
			CancelInvoke(nameof(OnInitTimeout));

			// Callback đến sau timeout: game đã vào rồi, chỉ cập nhật trạng thái SDK. Subscriber của WhenReady đã nhận
			// false; ai cần SDK thì đã đăng ký WhenSdkReady và được gọi trong OnSdkReady.
			if (IsInitDone)
			{
				if (code == 0 && !IsReady)
				{
					Debug.Log("[TikTokBootstrap] InitSDK succeeded after timeout.");
					InitCode = 0;
					IsReady = true;
					OnSdkReady();
				}
				return;
			}

			FinishInit(code);
		}

		private void FinishInit(int code)
		{
			InitCode = code;
			IsReady = code == 0;
			IsInitDone = true;

			if (IsReady)
				OnSdkReady();
			else if (code != -2)
				// Theo doc: init lỗi thì không gọi tiếp API nền tảng.
				Debug.LogError($"[TikTokBootstrap] InitSDK failed, code={code}. TT.* APIs are disabled.");

			Action<bool> callbacks = InitCompleted;
			InitCompleted = null;
			callbacks?.Invoke(IsReady);

			LoadNextScene();
		}

		private void OnSdkReady()
		{
			Debug.Log($"[TikTokBootstrap] InitSDK ok. TTSDK={TT.TTSDKVersion} Container={TT.GetContainerVersion()}");
			RegisterLifecycle();
			LogLaunchOptions();
			if (silentLogin)
				TikTokLogin.Login();

			Action callbacks = SdkReady;
			SdkReady = null;
			if (callbacks == null)
				return;
			// Mỗi subscriber một try/catch: một module lỗi không được chặn các module còn lại.
			foreach (Action callback in callbacks.GetInvocationList())
			{
				try { callback(); }
				catch (Exception e) { Debug.LogError($"[TikTokBootstrap] WhenSdkReady callback threw: {e}"); }
			}
		}

		private void RegisterLifecycle()
		{
			var lifeCycle = TT.GetAppLifeCycle();
			if (lifeCycle == null)
			{
				Debug.LogWarning("[TikTokBootstrap] GetAppLifeCycle returned null");
				return;
			}

			lifeCycle.OnShow += param =>
			{
				if (muteAudioWhenHidden)
					AudioListener.pause = false;
				OnShow?.Invoke();
				OnShowWithParams?.Invoke(param);
			};
			lifeCycle.OnHide += () =>
			{
				if (muteAudioWhenHidden)
					AudioListener.pause = true;
				// Container gọi onHide ĐỒNG BỘ: exception của một handler sẽ huỷ cả cú flush bên dưới.
				try { OnHide?.Invoke(); }
				catch (Exception e) { Debug.LogError($"[TikTokBootstrap] OnHide handler threw: {e}"); }
				// Cuối cùng, sau khi mọi subscriber đã ghi xong state của nó.
				TikTokStorage.Save();
			};
		}

		private static void LogLaunchOptions()
		{
			try
			{
				var options = TT.GetLaunchOptionsSync();
				if (options != null)
					Debug.Log($"[TikTokBootstrap] Launch scene={options.Scene} path={options.Path}");
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[TikTokBootstrap] GetLaunchOptionsSync failed: {e.Message}");
			}
		}

		private void LoadNextScene()
		{
			if (string.IsNullOrEmpty(nextScene) || SceneManager.GetActiveScene().name == nextScene)
				return;

			if (!Application.CanStreamedLevelBeLoaded(nextScene))
			{
				Debug.LogError($"[TikTokBootstrap] Scene '{nextScene}' is not in Build Settings.");
				return;
			}

			SceneManager.LoadScene(nextScene);
		}
	}
}
