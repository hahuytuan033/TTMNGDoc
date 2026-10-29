using System;
using TTSDK;
using UnityEngine;

namespace AmobearTTMNG
{
	/// <summary>
	/// Sidebar Revisit: dẫn người chơi ra sidebar trên trang cá nhân TikTok, người chơi vào lại game TỪ
	/// SIDEBAR thì được thưởng. Đây KHÔNG phải Desktop Shortcut (addShortcut/getShortcutMissionReward)
	/// — nguồn vào, launch mode và API kiểm tra khác nhau, không dùng chung (<see cref="TikTokShortcut"/>).
	///
	/// LUỒNG (doc + sequence diagram, nhận quà hai bước như pocket-2):
	///   1. Chỉ hiện lối vào khi <see cref="IsSupported"/> (canIUse cả startEntranceMission và
	///      getEntranceMissionReward). Lời dẫn phải nói rõ: vào lại từ sidebar mới nhận được thưởng.
	///   2. Người chơi bấm lối vào → <see cref="Start"/> (startEntranceMission) → TikTok dẫn ra sidebar.
	///      Thành công CHỈ có nghĩa là luồng dẫn đã chạy — KHÔNG phát thưởng, chỉ đánh dấu đang chờ.
	///   3. Người chơi bấm game trên sidebar → phiên vào lại. Module tự gọi getEntranceMissionReward lúc mở game
	///      và mỗi lần OnShow khi đang chờ. canReceiveReward=true → lưu quyền nhận quà (<see cref="HasPendingReward"/>)
	///      → <see cref="OnPendingRewardChanged"/> → game hiện popup Claim.
	///   4. Người chơi bấm Claim → <see cref="Claim"/> → <see cref="OnRewardClaimed"/>: GAME cấp phần thưởng ở đây.
	///
	/// Kỳ nhận theo <see cref="ClaimPeriod"/> (mặc định một lần/ngày, doc gợi ý open_id + sidebar_revisit + yyyyMMdd).
	/// Game có backend gán <see cref="ServerClaim"/>; không gán thì dùng sổ cục bộ.
	///
	/// KIỂM THỬ: DevTool launch mode Sidebar Revisit / <c>scene_center</c> giả lập phiên mở từ sidebar.
	/// Luồng thật (vào game bình thường → Start → vào lại từ sidebar) phải test riêng. Khả năng này còn
	/// phụ thuộc phiên bản TikTok, khu vực tài khoản và phạm vi rollout. Trong Editor dùng
	/// <see cref="EditorSimulateSidebarLaunch"/> rồi gọi <see cref="RefreshReward"/>.
	/// </summary>
	public static class TikTokSidebar
	{
		private const string LogPrefix = "[Sidebar]";

		/// <summary>
		/// Khóa kỳ nhận thưởng; cùng giá trị = đã nhận rồi. Mặc định theo ngày (UTC) → một lần/ngày.
		/// Thưởng theo sự kiện: gán = () =&gt; "campaign_id:mission_id".
		/// </summary>
		public static Func<string> ClaimPeriod = () => DateTime.UtcNow.ToString("yyyyMMdd");

		/// <summary>
		/// Hook backend (diagram: Request sidebar revisit reward → Granted or already granted): (khóa kỳ, done).
		/// Server kiểm user + mission + khóa idempotent và gọi done(true) chỉ ở lần nhận đầu tiên.
		/// Để null = dùng sổ cục bộ (chỉ chặn trùng trên một máy).
		/// </summary>
		public static Action<string, Action<bool>> ServerClaim;

		/// <summary>Chỉ Editor: giả lập phiên hiện tại được mở từ sidebar trang cá nhân TikTok.</summary>
		public static bool EditorSimulateSidebarLaunch;

		private static readonly TikTokMissionLedger Ledger = new TikTokMissionLedger(
			LogPrefix, "amobear_tt_sidebar", () => ClaimPeriod?.Invoke(), () => ServerClaim, CheckReward, checkOnLaunch: true);

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
		/// Client hỗ trợ cả startEntranceMission và getEntranceMissionReward. Chỉ đúng sau khi InitSDK xong —
		/// UI nên dùng <see cref="CheckSupported"/> để quyết định hiện lối vào.
		/// </summary>
		public static bool IsSupported
		{
			get
			{
#if UNITY_EDITOR
				return true;
#else
				return TikTokBootstrap.IsReady
					&& TikTokShortcut.SafeCanIUse(() => CanIUse.StartEntranceMission)
					&& TikTokShortcut.SafeCanIUse(() => CanIUse.GetEntranceMissionReward);
#endif
			}
		}

		/// <summary>
		/// Chờ InitSDK xong rồi trả <see cref="IsSupported"/>. False thì ẩn lối vào. Nếu InitSDK hết timeout rồi mới
		/// thành công, <paramref name="onResult"/> được gọi LẦN HAI với true — UI phải chịu được hai lần gọi.
		/// </summary>
		public static void CheckSupported(Action<bool> onResult) =>
			TikTokShortcut.CheckSupported(onResult, () => IsSupported, Log);

		/// <summary>Nền tảng đã xác nhận, đang chờ người chơi bấm Claim.</summary>
		public static bool HasPendingReward => Ledger.HasPendingReward;

		/// <summary>Đã Claim trong kỳ hiện tại chưa (theo sổ cục bộ).</summary>
		public static bool HasClaimedThisPeriod => Ledger.HasClaimedThisPeriod;

		/// <summary>Đã bấm Start, đang chờ người chơi vào lại từ sidebar.</summary>
		public static bool IsWaitingForRevisit => Ledger.IsArmed;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize()
		{
			TikTokBootstrap.OnShow -= HandleShow;
			TikTokBootstrap.OnShow += HandleShow;
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
		/// Dẫn người chơi ra sidebar trang cá nhân (startEntranceMission). <paramref name="onDone"/>(true) =
		/// luồng dẫn đã chạy (đã đánh dấu đang chờ), CHƯA phải đã vào lại từ sidebar. Không hỗ trợ / lỗi → false.
		/// </summary>
		public static void Start(Action<bool> onDone = null)
		{
#if UNITY_EDITOR
			Log("start (editor) ok");
			Ledger.ArmPending();
			onDone?.Invoke(true);
#else
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				if (!IsSupported)
				{
					Debug.LogWarning($"{LogPrefix} start unsupported sdkReady={TikTokBootstrap.IsReady} {TikTokShortcut.DeviceInfo()}");
					onDone?.Invoke(false);
					return;
				}

				bool answered = false;
				try
				{
					// SDK khai báo kiểu kết quả success là TTCheckBalanceResult; không cần đọc.
					TT.StartEntranceMission(new TTStartEntranceMissionParam
					{
						success = res =>
						{
							answered = true;
							Log("start ok");
							Ledger.ArmPending();
							onDone?.Invoke(true);
						},
						fail = err =>
						{
							answered = true;
							Debug.LogWarning($"{LogPrefix} start fail code={err?.ErrorCode} msg={err?.ErrMsg} {TikTokShortcut.DeviceInfo()}");
							onDone?.Invoke(false);
						},
						complete = res =>
						{
							if (!answered)
								onDone?.Invoke(false);
						},
					});
				}
				catch (Exception e)
				{
					Debug.LogError($"{LogPrefix} StartEntranceMission threw: {e.Message} {TikTokShortcut.DeviceInfo()}");
					onDone?.Invoke(false);
				}
			});
#endif
		}

		/// <summary>Hỏi lại nền tảng ngay. Dùng trong Editor sau khi bật <see cref="EditorSimulateSidebarLaunch"/>.</summary>
		public static void RefreshReward() => Ledger.Refresh(launch: true);

		/// <summary>
		/// Người chơi bấm Claim. <paramref name="onDone"/>(true) = vừa nhận, <see cref="OnRewardClaimed"/> đã bắn.
		/// False nếu không có quà chờ / server từ chối.
		/// </summary>
		public static void Claim(Action<bool> onDone = null) => Ledger.Claim(onDone);

		/// <summary>Chỉ để test: xóa sổ cục bộ (không reset được trạng thái phía nền tảng).</summary>
		public static void ResetLocalLedger() => Ledger.Reset();

		/// <summary>
		/// API thô: hỏi nền tảng phiên hiện tại có đủ điều kiện thưởng sidebar không (getEntranceMissionReward).
		/// Chỉ là điều kiện của nền tảng, KHÔNG phải sổ thưởng. Chờ InitSDK xong rồi mới gọi TT.
		/// </summary>
		public static void CheckReward(Action<bool> onResult)
		{
#if UNITY_EDITOR
			Log($"check (editor) canReceiveReward={EditorSimulateSidebarLaunch}");
			onResult?.Invoke(EditorSimulateSidebarLaunch);
#else
			TikTokBootstrap.WhenReady(ready =>
			{
				if (!ready || !TikTokShortcut.SafeCanIUse(() => CanIUse.GetEntranceMissionReward))
				{
					Log($"check skipped sdkReady={ready}");
					onResult?.Invoke(false);
					return;
				}

				bool answered = false;
				try
				{
					TT.GetEntranceMissionReward(new TTGetEntranceMissionRewardParam
					{
						success = r =>
						{
							answered = true;
							bool can = r != null && r.canReceiveReward;
							Log($"check canReceiveReward={can}");
							onResult?.Invoke(can);
						},
						fail = err =>
						{
							answered = true;
							Debug.LogWarning($"{LogPrefix} check fail code={err?.ErrorCode} msg={err?.ErrMsg} {TikTokShortcut.DeviceInfo()}");
							onResult?.Invoke(false);
						},
						complete = res =>
						{
							if (!answered)
								onResult?.Invoke(false);
						},
					});
				}
				catch (Exception e)
				{
					Debug.LogError($"{LogPrefix} GetEntranceMissionReward threw: {e.Message}");
					onResult?.Invoke(false);
				}
			});
#endif
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
