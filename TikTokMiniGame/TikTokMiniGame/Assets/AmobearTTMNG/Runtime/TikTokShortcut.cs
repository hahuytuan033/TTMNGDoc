using System;
using TTSDK;
using UnityEngine;

namespace AmobearTTMNG
{
	/// <summary>
	/// Desktop Shortcut Revisit: người chơi thêm icon game ra màn hình chính, lần sau mở game từ icon
	/// đó thì được thưởng. Đây KHÔNG phải Sidebar Revisit (StartEntranceMission/GetEntranceMissionReward)
	/// — hai luồng có nguồn vào và API kiểm tra riêng, không dùng chung.
	///
	/// LUỒNG ĐÚNG:
	///   1. Hiện nút "Thêm ra màn hình chính" chỉ khi <see cref="IsSupported"/>, rồi gọi <see cref="Add"/>.
	///      Add thành công CHỈ có nghĩa là hộp thoại đã hiện — KHÔNG phát thưởng ở đây.
	///   2. Người chơi thoát, mở lại game TỪ ICON trên màn hình chính.
	///   3. Ở phiên mới, gọi <see cref="TryClaim"/> (hoặc <see cref="CheckReward"/> nếu tự quản lý thưởng).
	///      Gọi ngay sau Add trong cùng phiên luôn ra false.
	///
	/// PHÁT THƯỞNG IDEMPOTENT: <see cref="TryClaim"/> chặn trùng bằng sổ cục bộ theo <see cref="ClaimPeriod"/>
	/// (mặc định một lần/ngày). Game có backend nên phát thưởng ở server theo open_id + mission + ngày
	/// và dùng <see cref="CheckReward"/> thay vì TryClaim.
	///
	/// KIỂM THỬ: DevTool chọn launch mode <c>scene_shortcut</c> để giả lập mở từ icon. Icon thật trên điện
	/// thoại chỉ mở BẢN ĐÃ PHÁT HÀNH — bản preview/debug không mở được từ icon. Trong Editor dùng
	/// <see cref="EditorSimulateShortcutLaunch"/>. Không có API reset canReceiveReward phía nền tảng.
	/// </summary>
	public static class TikTokShortcut
	{
		private const string LogPrefix = "[Shortcut]";
		private const string LedgerKey = "amobear_tt_shortcut_claimed";

		/// <summary>
		/// Khóa kỳ nhận thưởng; cùng giá trị = đã nhận rồi. Mặc định theo ngày (UTC) → một lần/ngày.
		/// Thưởng một lần duy nhất: gán = () =&gt; "once".
		/// </summary>
		public static Func<string> ClaimPeriod = () => DateTime.UtcNow.ToString("yyyyMMdd");

		/// <summary>Chỉ Editor: giả lập phiên hiện tại được mở từ icon màn hình chính.</summary>
		public static bool EditorSimulateShortcutLaunch;

		/// <summary>Bắn khi <see cref="TryClaim"/> trao thưởng, kèm khóa kỳ. Game nối vào analytics.</summary>
		public static event Action<string> OnRewardGranted;

		/// <summary>
		/// Client hỗ trợ addShortcut (TikTok 41.0.0+, iOS 41.40+). Chỉ đúng sau khi InitSDK xong —
		/// UI nên dùng <see cref="CheckSupported"/> để quyết định hiện nút.
		/// </summary>
		public static bool IsSupported
		{
			get
			{
#if UNITY_EDITOR
				return true;
#else
				return TikTokBootstrap.IsReady && SafeCanIUse(() => CanIUse.AddShortcut);
#endif
			}
		}

		/// <summary>Chờ InitSDK xong rồi trả <see cref="IsSupported"/>. False thì ẩn nút thêm.</summary>
		public static void CheckSupported(Action<bool> onResult)
		{
#if UNITY_EDITOR
			onResult?.Invoke(true);
#else
			TikTokBootstrap.WhenReady(_ =>
			{
				bool supported = IsSupported;
				Log($"supported={supported} {DeviceInfo()}");
				onResult?.Invoke(supported);
			});
#endif
		}

		/// <summary>Đã nhận thưởng trong kỳ hiện tại chưa (theo sổ cục bộ).</summary>
		public static bool HasClaimedThisPeriod => GetPrefs(LedgerKey) == CurrentPeriod();

		/// <summary>
		/// Mở hộp thoại thêm icon. <paramref name="onDone"/>(true) = hộp thoại đã kích hoạt, chưa phải đã
		/// quay lại từ icon. Không hỗ trợ / lỗi → false.
		/// </summary>
		public static void Add(Action<bool> onDone = null, bool showToastTipsIfFailed = true)
		{
#if UNITY_EDITOR
			Log("add (editor) ok");
			onDone?.Invoke(true);
#else
			TikTokBootstrap.WhenReady(_ =>
			{
				if (!IsSupported)
				{
					Debug.LogWarning($"{LogPrefix} add unsupported sdkReady={TikTokBootstrap.IsReady} {DeviceInfo()}");
					onDone?.Invoke(false);
					return;
				}

				try
				{
					TT.AddShortcut(ok =>
					{
						if (ok)
							Log("add ok");
						else
							Debug.LogWarning($"{LogPrefix} add failed {DeviceInfo()}");
						onDone?.Invoke(ok);
					}, showToastTipsIfFailed);
				}
				catch (Exception e)
				{
					Debug.LogError($"{LogPrefix} AddShortcut threw: {e.Message} {DeviceInfo()}");
					onDone?.Invoke(false);
				}
			});
#endif
		}

		/// <summary>
		/// Hỏi nền tảng phiên hiện tại có đủ điều kiện thưởng shortcut không (mở từ icon). Chỉ là điều kiện
		/// của nền tảng, KHÔNG phải sổ thưởng — game tự chặn trùng. Chờ InitSDK xong rồi mới gọi TT.
		/// </summary>
		public static void CheckReward(Action<bool> onResult)
		{
#if UNITY_EDITOR
			Log($"check (editor) canReceiveReward={EditorSimulateShortcutLaunch}");
			onResult?.Invoke(EditorSimulateShortcutLaunch);
#else
			TikTokBootstrap.WhenReady(ready =>
			{
				if (!ready || !SafeCanIUse(() => CanIUse.GetShortcutMissionReward))
				{
					Log($"check skipped sdkReady={ready}");
					onResult?.Invoke(false);
					return;
				}

				bool answered = false;
				try
				{
					TT.GetShortcutMissionReward(new GetShortcutMissionRewardParam
					{
						Success = r =>
						{
							answered = true;
							bool can = r != null && r.CanReceiveReward;
							Log($"check canReceiveReward={can}");
							onResult?.Invoke(can);
						},
						Fail = err =>
						{
							answered = true;
							Debug.LogWarning($"{LogPrefix} check fail code={err?.ErrorCode} msg={err?.ErrMsg} {DeviceInfo()}");
							onResult?.Invoke(false);
						},
						Complete = () =>
						{
							// Phòng trường hợp container chỉ gọi complete.
							if (!answered)
								onResult?.Invoke(false);
						},
					});
				}
				catch (Exception e)
				{
					Debug.LogError($"{LogPrefix} GetShortcutMissionReward threw: {e.Message}");
					onResult?.Invoke(false);
				}
			});
#endif
		}

		/// <summary>
		/// Kiểm tra điều kiện + sổ cục bộ; đủ thì ghi sổ rồi gọi <paramref name="grant"/> đúng một lần mỗi kỳ.
		/// <paramref name="onDone"/>(true) khi vừa trao thưởng. Gọi ở phiên mở game (vd màn hình chính).
		/// </summary>
		public static void TryClaim(Action grant, Action<bool> onDone = null)
		{
			if (HasClaimedThisPeriod)
			{
				Log("claim skipped: already claimed this period");
				onDone?.Invoke(false);
				return;
			}

			CheckReward(can =>
			{
				// Kiểm lại sổ: hai lần TryClaim chồng nhau không được phát hai lần.
				if (!can || HasClaimedThisPeriod)
				{
					onDone?.Invoke(false);
					return;
				}

				string period = CurrentPeriod();
				SetPrefs(LedgerKey, period);
				Log($"claim granted period={period}");

				try { grant?.Invoke(); }
				catch (Exception e) { Debug.LogError($"{LogPrefix} grant handler threw: {e}"); }
				try { OnRewardGranted?.Invoke(period); }
				catch (Exception e) { Debug.LogError($"{LogPrefix} OnRewardGranted handler threw: {e}"); }

				onDone?.Invoke(true);
			});
		}

		/// <summary>Chỉ để test: xóa sổ cục bộ (không reset được trạng thái phía nền tảng).</summary>
		public static void ResetLocalLedger()
		{
			SetPrefs(LedgerKey, string.Empty);
			Log("local ledger reset");
		}

		// ---------------------------------------------------------------- helpers

		private static string CurrentPeriod()
		{
			try { return ClaimPeriod?.Invoke() ?? string.Empty; }
			catch (Exception e) { Debug.LogError($"{LogPrefix} ClaimPeriod hook threw: {e}"); return string.Empty; }
		}

		// Doc yêu cầu log lỗi kèm nền tảng thiết bị và phiên bản TikTok để gửi platform khi cần.
		private static string DeviceInfo()
		{
#if UNITY_EDITOR
			return "platform=editor";
#else
			try
			{
				var info = TT.GetSystemInfo();
				return $"platform={info?.platform} system={info?.system} tiktok={info?.hostVersion} container={TT.GetContainerVersion()}";
			}
			catch (Exception e)
			{
				return $"platform=unknown ({e.Message})";
			}
#endif
		}

		private static bool SafeCanIUse(Func<bool> check)
		{
			try { return check(); }
			catch (Exception e) { Debug.LogWarning($"{LogPrefix} CanIUse threw: {e.Message}"); return false; }
		}

		// TT.PlayerPrefs lưu vào storage bền của container; Editor dùng PlayerPrefs của Unity.
		private static string GetPrefs(string key)
		{
#if UNITY_EDITOR
			return PlayerPrefs.GetString(key, string.Empty);
#else
			return TT.PlayerPrefs.GetString(key, string.Empty);
#endif
		}

		private static void SetPrefs(string key, string value)
		{
#if UNITY_EDITOR
			PlayerPrefs.SetString(key, value);
			PlayerPrefs.Save();
#else
			TT.PlayerPrefs.SetString(key, value);
			TT.PlayerPrefs.Save();
#endif
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
