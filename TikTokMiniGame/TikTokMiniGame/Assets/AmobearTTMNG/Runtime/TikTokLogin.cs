using System;
using TTSDK;
using UnityEngine;

namespace AmobearTTMNG
{
	/// <summary>
	/// Silent Login (tt.login / starkLogin): checklist Acceptance Testing của TikTok coi là bắt buộc
	/// ("silent ID authorization capability is displayed correctly"), kể cả game chỉ có quảng cáo.
	///
	/// Template KHÔNG có backend: <see cref="Code"/> chỉ được giữ lại, không đổi lấy open_id. Game có server thì
	/// nghe <see cref="OnLoggedIn"/> rồi gửi code lên server đổi open_id (code sống ngắn, dùng một lần).
	/// Login lỗi không chặn game — chỉ log; không tự gọi lại để tránh hiện thông báo đăng nhập liên tục.
	///
	/// <see cref="TikTokBootstrap"/> tự gọi <see cref="Login"/> khi SDK sẵn sàng (tắt bằng ô Silent Login trên
	/// component). Editor không có container nên coi như đăng nhập thành công với code giả.
	/// </summary>
	public static class TikTokLogin
	{
		private const string LogPrefix = "[Login]";

		/// <summary>Đã login thành công trong phiên này.</summary>
		public static bool IsLoggedIn { get; private set; }

		/// <summary>Đang chờ container trả kết quả.</summary>
		public static bool IsLoggingIn { get; private set; }

		/// <summary>Code tạm thời của lần login gần nhất (server đổi lấy open_id). Null nếu chưa login.</summary>
		public static string Code { get; private set; }

		/// <summary>Login thành công, kèm code. Bắn mỗi lần <see cref="Login"/> thành công.</summary>
		public static event Action<string> OnLoggedIn;

		/// <summary>Login lỗi / không hỗ trợ, kèm thông báo lỗi.</summary>
		public static event Action<string> OnLoginFailed;

		/// <summary>Client hỗ trợ tt.login. Chỉ đúng sau khi InitSDK xong.</summary>
		public static bool IsSupported
		{
			get
			{
#if UNITY_EDITOR
				return true;
#else
				return TikTokBootstrap.IsReady && TikTokShortcut.SafeCanIUse(() => CanIUse.Login);
#endif
			}
		}

		/// <summary>
		/// Gọi silent login. <paramref name="onDone"/>(true) = có code. Đang chờ kết quả thì lời gọi mới chỉ đăng ký
		/// callback, không gọi container lần nữa.
		/// </summary>
		public static void Login(Action<bool> onDone = null)
		{
			if (onDone != null)
				pendingCallbacks += onDone;
			if (IsLoggingIn)
				return;
			IsLoggingIn = true;

#if UNITY_EDITOR
			Succeed("editor_code");
#else
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				// SDK không hỗ trợ thì TT.Login chỉ hiện toast và KHÔNG gọi callback nào (đã kiểm ttsdk.dll 1.1.5)
				// — phải chặn ở đây, nếu không IsLoggingIn kẹt true cả phiên.
				if (!IsSupported)
				{
					Fail($"unsupported sdkReady={sdkReady}");
					return;
				}

				try
				{
					// SDK tự có timeout 60s và gọi failedCallback khi hết giờ.
					TT.Login(code => Succeed(code), errMsg => Fail(errMsg));
				}
				catch (Exception e)
				{
					Fail("threw: " + e.Message);
				}
			});
#endif
		}

		private static event Action<bool> pendingCallbacks;

		private static void Succeed(string code)
		{
			IsLoggingIn = false;
			IsLoggedIn = true;
			Code = code;
			Debug.Log($"{LogPrefix} ok");
			OnLoggedIn?.Invoke(code);
			Complete(true);
		}

		private static void Fail(string message)
		{
			IsLoggingIn = false;
			Debug.LogWarning($"{LogPrefix} fail {message} {TikTokShortcut.DeviceInfo()}");
			OnLoginFailed?.Invoke(message);
			Complete(false);
		}

		private static void Complete(bool ok)
		{
			Action<bool> callbacks = pendingCallbacks;
			pendingCallbacks = null;
			callbacks?.Invoke(ok);
		}
	}
}
