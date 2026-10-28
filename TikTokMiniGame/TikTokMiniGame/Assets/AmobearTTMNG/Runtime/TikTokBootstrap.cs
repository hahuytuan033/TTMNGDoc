using System;
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
	/// Ads (TikTokAds/HSBridge) tự init riêng, không phụ thuộc class này.
	/// </summary>
	public class TikTokBootstrap : MonoBehaviour
	{
		[Tooltip("Scene load sau khi InitSDK xong (kể cả khi lỗi). Để trống = ở lại scene hiện tại.")]
		[SerializeField] private string nextScene = "MainMenu";

		[Tooltip("Tắt âm thanh khi game bị đưa xuống nền (OnHide), bật lại khi OnShow.")]
		[SerializeField] private bool muteAudioWhenHidden = true;

		[Tooltip("Hết thời gian chờ callback InitSDK thì vẫn vào game (Editor không bao giờ gọi callback).")]
		[SerializeField] private float initTimeoutSeconds = 8f;

		[SerializeField] private int targetFrameRate = 60;

		public static TikTokBootstrap Instance { get; private set; }

		/// <summary>InitSDK đã có kết quả (thành công hoặc lỗi).</summary>
		public static bool IsInitDone { get; private set; }

		/// <summary>InitSDK trả code 0 — được phép gọi API TT.*.</summary>
		public static bool IsReady { get; private set; }

		/// <summary>Mã trả về của InitSDK. 1 = SDK không hỗ trợ, 2 = Unity không hỗ trợ, -1 = exception, -2 = timeout.</summary>
		public static int InitCode { get; private set; } = -1;

		/// <summary>Game bị đưa xuống nền.</summary>
		public static event Action OnHide;

		/// <summary>Game quay lại foreground.</summary>
		public static event Action OnShow;

		private static event Action<bool> InitCompleted;

		/// <summary>Gọi <paramref name="callback"/>(IsReady) ngay nếu InitSDK đã xong, hoặc khi nó xong.</summary>
		public static void WhenReady(Action<bool> callback)
		{
			if (callback == null)
				return;
			if (IsInitDone)
				callback(IsReady);
			else
				InitCompleted += callback;
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

			// Callback đến sau timeout: game đã vào rồi, chỉ cập nhật trạng thái SDK.
			if (IsInitDone)
			{
				if (code == 0 && !IsReady)
				{
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
		}

		private void RegisterLifecycle()
		{
			var lifeCycle = TT.GetAppLifeCycle();
			if (lifeCycle == null)
			{
				Debug.LogWarning("[TikTokBootstrap] GetAppLifeCycle returned null");
				return;
			}

			lifeCycle.OnShow += _ =>
			{
				if (muteAudioWhenHidden)
					AudioListener.pause = false;
				OnShow?.Invoke();
			};
			lifeCycle.OnHide += () =>
			{
				if (muteAudioWhenHidden)
					AudioListener.pause = true;
				OnHide?.Invoke();
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
