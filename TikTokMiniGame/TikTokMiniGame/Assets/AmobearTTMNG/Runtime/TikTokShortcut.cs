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
	/// LUỒNG (doc + sequence diagram, nhận quà hai bước như pocket-2):
	///   1. Hiện nút "Thêm ra màn hình chính" chỉ khi <see cref="IsSupported"/>, rồi gọi <see cref="Add"/>.
	///      Add thành công CHỈ có nghĩa là hộp thoại đã hiện — KHÔNG phát thưởng, chỉ đánh dấu đang chờ.
	///   2. Người chơi thoát, mở lại game TỪ ICON. Module tự gọi getShortcutMissionReward lúc mở game (một lần
	///      mỗi phiên — bắt cả trường hợp người chơi tự thêm shortcut từ UI TikTok) và mỗi lần OnShow khi đang chờ.
	///   3. canReceiveReward=true → lưu quyền nhận quà NGAY (<see cref="HasPendingReward"/>) và bắn
	///      <see cref="OnPendingRewardChanged"/> → game hiện popup Claim.
	///   4. Người chơi bấm Claim → <see cref="Claim"/> → <see cref="OnRewardClaimed"/>: GAME cấp phần thưởng ở đây.
	///
	/// Kỳ nhận theo <see cref="ClaimPeriod"/> (mặc định một lần duy nhất). Game có backend gán <see cref="ServerClaim"/>
	/// để server ghi sổ theo open_id + mission + kỳ; không gán thì dùng sổ cục bộ (<see cref="TikTokStorage"/>).
	///
	/// KIỂM THỬ: DevTool chọn launch mode <c>scene_shortcut</c> để giả lập mở từ icon. Icon thật trên điện
	/// thoại chỉ mở BẢN ĐÃ PHÁT HÀNH — bản preview/debug không mở được từ icon. Trong Editor dùng
	/// <see cref="EditorSimulateShortcutLaunch"/> rồi gọi <see cref="RefreshReward"/>.
	/// </summary>
	public static class TikTokShortcut
	{
		private const string LogPrefix = "[Shortcut]";

		/// <summary>
		/// Khóa kỳ nhận thưởng; cùng giá trị = đã nhận rồi. Mặc định "once" → một lần duy nhất.
		/// Mỗi ngày một lần: gán = () =&gt; DateTime.UtcNow.ToString("yyyyMMdd").
		/// </summary>
		public static Func<string> ClaimPeriod = () => "once";

		/// <summary>
		/// Hook backend (bước "Claim reward → Idempotent reward result" của diagram): (khóa kỳ, done).
		/// Server ghi sổ theo open_id + mission + kỳ và gọi done(true) chỉ ở lần nhận đầu tiên.
		/// Để null = dùng sổ cục bộ (chỉ chặn trùng trên một máy).
		/// </summary>
		public static Action<string, Action<bool>> ServerClaim;

		/// <summary>Chỉ Editor: giả lập phiên hiện tại được mở từ icon màn hình chính.</summary>
		public static bool EditorSimulateShortcutLaunch;

		private static readonly TikTokMissionLedger Ledger = new TikTokMissionLedger(
			LogPrefix, "amobear_tt_shortcut", () => ClaimPeriod?.Invoke(), () => ServerClaim, CheckReward, checkOnLaunch: true);

		/// <summary>Có quà mới chờ Claim, hoặc quà vừa được Claim. UI bật/tắt popup Claim theo <see cref="HasPendingReward"/>.</summary>
		public static event Action OnPendingRewardChanged
		{
			add => Ledger.PendingChanged += value;
			remove => Ledger.PendingChanged -= value;
		}

		/// <summary>Người chơi đã Claim (kèm khóa kỳ). GAME cấp phần thưởng ở đây.</summary>
		public static event Action<string> OnRewardClaimed
		{
			add => Ledger.Claimed += value;
			remove => Ledger.Claimed -= value;
		}

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

		/// <summary>
		/// Chờ InitSDK xong rồi trả <see cref="IsSupported"/>. False thì ẩn nút thêm. Nếu InitSDK hết timeout rồi mới
		/// thành công, <paramref name="onResult"/> được gọi LẦN HAI với true — UI phải chịu được hai lần gọi.
		/// </summary>
		public static void CheckSupported(Action<bool> onResult) => CheckSupported(onResult, () => IsSupported, Log);

		// Dùng chung cho Shortcut / Sidebar / Share.
		internal static void CheckSupported(Action<bool> onResult, Func<bool> isSupported, Action<string> log)
		{
#if UNITY_EDITOR
			onResult?.Invoke(true);
#else
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				bool supported = isSupported();
				log($"supported={supported} {DeviceInfo()}");
				onResult?.Invoke(supported);
				if (sdkReady)
					return;

				TikTokBootstrap.WhenSdkReady(() =>
				{
					bool late = isSupported();
					log($"supported={late} after late init {DeviceInfo()}");
					if (late)
						onResult?.Invoke(true);
				});
			});
#endif
		}

		/// <summary>Nền tảng đã xác nhận, đang chờ người chơi bấm Claim.</summary>
		public static bool HasPendingReward => Ledger.HasPendingReward;

		/// <summary>Đã Claim trong kỳ hiện tại chưa (theo sổ cục bộ).</summary>
		public static bool HasClaimedThisPeriod => Ledger.HasClaimedThisPeriod;

		/// <summary>Đã bấm Add, đang chờ người chơi mở lại từ icon.</summary>
		public static bool IsWaitingForRevisit => Ledger.IsArmed;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize()
		{
			TikTokBootstrap.OnShow -= HandleShow;
			TikTokBootstrap.OnShow += HandleShow;
			// Editor: InitSDK không bao giờ thành công nhưng CheckReward dùng cờ giả lập nên vẫn chạy.
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				Ledger.Refresh(launch: true);
				// InitSDK hết timeout: lần hỏi trên bị bỏ qua — hỏi lại nếu SDK sẵn sàng muộn.
				if (!sdkReady)
					TikTokBootstrap.WhenSdkReady(Ledger.RetryLaunchCheck);
			});
		}

		private static void HandleShow() => Ledger.Refresh(launch: false);

		/// <summary>
		/// Mở hộp thoại thêm icon. <paramref name="onDone"/>(true) = hộp thoại đã kích hoạt (đã đánh dấu đang chờ),
		/// chưa phải đã quay lại từ icon. Không hỗ trợ / lỗi → false.
		/// </summary>
		public static void Add(Action<bool> onDone = null, bool showToastTipsIfFailed = true)
		{
#if UNITY_EDITOR
			Log("add (editor) ok");
			Ledger.ArmPending();
			onDone?.Invoke(true);
#else
			TikTokBootstrap.WhenReady(sdkReady =>
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
						{
							Log("add ok");
							Ledger.ArmPending();
						}
						else
						{
							Debug.LogWarning($"{LogPrefix} add failed {DeviceInfo()}");
						}
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
		/// Hỏi lại nền tảng ngay (bình thường module tự hỏi lúc mở game và OnShow). Dùng trong Editor sau khi bật
		/// <see cref="EditorSimulateShortcutLaunch"/>.
		/// </summary>
		public static void RefreshReward() => Ledger.Refresh(launch: true);

		/// <summary>
		/// Người chơi bấm Claim. <paramref name="onDone"/>(true) = vừa nhận, <see cref="OnRewardClaimed"/> đã bắn.
		/// False nếu không có quà chờ / server từ chối.
		/// </summary>
		public static void Claim(Action<bool> onDone = null) => Ledger.Claim(onDone);

		/// <summary>Chỉ để test: xóa sổ cục bộ (không reset được trạng thái phía nền tảng).</summary>
		public static void ResetLocalLedger() => Ledger.Reset();

		/// <summary>
		/// API thô: hỏi nền tảng phiên hiện tại có đủ điều kiện thưởng shortcut không (getShortcutMissionReward).
		/// Chỉ là điều kiện của nền tảng, KHÔNG phải sổ thưởng. Chờ InitSDK xong rồi mới gọi TT.
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

		// ---------------------------------------------------------------- helpers

		// Doc yêu cầu log lỗi kèm nền tảng thiết bị và phiên bản TikTok để gửi platform khi cần.
		internal static string DeviceInfo()
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

		internal static bool SafeCanIUse(Func<bool> check)
		{
			try { return check(); }
			catch (Exception e) { Debug.LogWarning($"{LogPrefix} CanIUse threw: {e.Message}"); return false; }
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
