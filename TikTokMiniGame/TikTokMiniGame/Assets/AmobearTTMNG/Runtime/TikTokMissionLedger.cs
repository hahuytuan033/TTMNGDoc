using System;
using UnityEngine;

namespace AmobearTTMNG
{
	/// <summary>
	/// Sổ nhận quà HAI BƯỚC dùng chung cho Shortcut / Sidebar / Share (port luồng TikTokMissions của pocket-2).
	///
	///   pending   : người chơi đã kích hoạt luồng (addShortcut / startEntranceMission) — đang chờ vào lại.
	///   entitled  : nền tảng đã xác nhận đủ điều kiện → GHI BỀN NGAY, chưa cộng gì. Từ đây quà không thể mất
	///               kể cả khi người chơi thoát game trước lúc bấm Claim.
	///   claimed   : người chơi bấm Claim → ghi đã-nhận TRƯỚC, bắn event cấp quà SAU (cộng trước rồi chết giữa
	///               chừng = cấp đúp lặp lại được).
	///
	/// Kỳ nhận (period) do module cấp: "once" = một lần duy nhất, "yyyyMMdd" = mỗi ngày một lần...
	/// Phần thưởng là gì do game tự quyết trong handler của event Claimed.
	/// </summary>
	internal sealed class TikTokMissionLedger
	{
		// Người chơi bấm nút rồi tắt dialog hệ thống thì canReceiveReward mãi false. Không có cap thì mỗi OnShow
		// là một round trip mạng, mãi mãi.
		private const int MaxPendingChecks = 20;

		private readonly string logPrefix;
		private readonly string pendingKey;
		private readonly string entitledKey;
		private readonly string claimedKey;
		private readonly Func<string> period;
		private readonly Func<Action<string, Action<bool>>> serverClaim;
		private readonly Action<Action<bool>> checkReward;
		private readonly bool checkOnLaunch;

		private bool checkedThisSession;
		private bool checking;
		private bool claiming;

		/// <summary>Có entitlement mới xuất hiện hoặc vừa bị claim. UI bật/tắt popup Claim theo đây.</summary>
		public event Action PendingChanged;

		/// <summary>Quà đã được ghi là đã nhận, kèm kỳ nhận. Game cấp phần thưởng ở đây.</summary>
		public event Action<string> Claimed;

		/// <param name="checkReward">Hỏi nền tảng có đủ điều kiện không. Null = không có bước hỏi (vd Share).</param>
		/// <param name="checkOnLaunch">Hỏi một lần mỗi lần mở game kể cả khi chưa pending (bắt trường hợp người chơi
		/// tự thêm shortcut / vào từ sidebar mà không bấm nút trong game).</param>
		public TikTokMissionLedger(string logPrefix, string keyPrefix, Func<string> period,
			Func<Action<string, Action<bool>>> serverClaim, Action<Action<bool>> checkReward, bool checkOnLaunch)
		{
			this.logPrefix = logPrefix;
			pendingKey = keyPrefix + "_pending_v2";
			entitledKey = keyPrefix + "_entitled_v2";
			claimedKey = keyPrefix + "_claimed_v2";
			this.period = period;
			this.serverClaim = serverClaim;
			this.checkReward = checkReward;
			this.checkOnLaunch = checkOnLaunch;
		}

		public bool HasPendingReward => !string.IsNullOrEmpty(TikTokStorage.GetString(entitledKey));

		public bool HasClaimedThisPeriod => IsClaimed(CurrentPeriod());

		public bool IsArmed => TikTokStorage.GetInt(pendingKey, 0) > 0;

		/// <summary>Luồng đã kích hoạt (dialog hiện / bong bóng dẫn đã mở) — chờ người chơi vào lại.</summary>
		public void ArmPending()
		{
			if (HasPendingReward || HasClaimedThisPeriod)
				return;
			TikTokStorage.SetInt(pendingKey, 1);
			TikTokStorage.Save();
			Log("pending armed");
		}

		/// <summary>
		/// Gọi lúc mở game (<paramref name="launch"/> = true) và mỗi lần OnShow. Chỉ hỏi nền tảng khi đang pending,
		/// hoặc một lần mỗi phiên nếu <c>checkOnLaunch</c>.
		/// </summary>
		public void Refresh(bool launch)
		{
			if (HasPendingReward)
			{
				// Entitlement còn treo từ phiên trước — nhắc UI hiện popup Claim.
				RaisePendingChanged();
				return;
			}
			if (checkReward == null || HasClaimedThisPeriod)
				return;

			bool launchCheck = launch && checkOnLaunch && !checkedThisSession;
			if (launch)
				checkedThisSession = true;
			if (!IsArmed && !launchCheck)
				return;
			if (!ConsumePendingCheck())
				return;
			if (checking)
				return;

			checking = true;
			try
			{
				checkReward(can =>
				{
					checking = false;
					if (can)
						Entitle();
					else
						Log("not eligible yet");
				});
			}
			catch (Exception e)
			{
				checking = false;
				Debug.LogError($"{logPrefix} check reward threw: {e}");
			}
		}

		/// <summary>
		/// Chạy lại lần hỏi lúc mở game. Dùng khi InitSDK thành công SAU timeout: lần hỏi đầu đã bị bỏ qua vì SDK chưa
		/// sẵn sàng, nếu không hỏi lại thì người chơi vào từ icon / sidebar sẽ mất quà của phiên này.
		/// </summary>
		public void RetryLaunchCheck()
		{
			checkedThisSession = false;
			Refresh(launch: true);
		}

		/// <summary>Nền tảng vừa xác nhận đủ điều kiện. Trả false nếu đã nhận kỳ này hoặc đang có quà chờ.</summary>
		public bool Entitle()
		{
			string current = CurrentPeriod();
			if (HasPendingReward || IsClaimed(current))
				return false;

			TikTokStorage.SetString(entitledKey, string.IsNullOrEmpty(current) ? "default" : current);
			TikTokStorage.DeleteKey(pendingKey);
			TikTokStorage.Save();
			Log($"entitled period={current}");
			RaisePendingChanged();
			return true;
		}

		/// <summary>
		/// Người chơi bấm Claim. <paramref name="onDone"/>(true) khi vừa nhận; false nếu không có quà chờ, đang
		/// claim dở, hoặc server từ chối.
		/// </summary>
		public void Claim(Action<bool> onDone)
		{
			if (!HasPendingReward)
			{
				Log("claim skipped: nothing pending");
				onDone?.Invoke(false);
				return;
			}
			if (claiming)
			{
				Log("claim skipped: another claim in progress");
				onDone?.Invoke(false);
				return;
			}

			claiming = true;
			string entitledPeriod = TikTokStorage.GetString(entitledKey);
			var server = serverClaim?.Invoke();
			if (server == null)
			{
				Commit(entitledPeriod, onDone);
				return;
			}

			try
			{
				server(entitledPeriod, ok =>
				{
					if (ok)
					{
						Commit(entitledPeriod, onDone);
						return;
					}
					// Server đã ghi nhận trên thiết bị khác — bỏ entitlement để popup không hiện mãi.
					claiming = false;
					TikTokStorage.SetString(claimedKey, entitledPeriod);
					TikTokStorage.DeleteKey(entitledKey);
					TikTokStorage.Save();
					Log($"claim rejected by server period={entitledPeriod}");
					RaisePendingChanged();
					onDone?.Invoke(false);
				});
			}
			catch (Exception e)
			{
				claiming = false;
				Debug.LogError($"{logPrefix} ServerClaim hook threw: {e}");
				onDone?.Invoke(false);
			}
		}

		/// <summary>Chỉ để test: xoá sạch sổ cục bộ (không reset được trạng thái phía nền tảng).</summary>
		public void Reset()
		{
			TikTokStorage.DeleteKey(pendingKey);
			TikTokStorage.DeleteKey(entitledKey);
			TikTokStorage.DeleteKey(claimedKey);
			TikTokStorage.Save();
			checkedThisSession = false;
			checking = false;
			claiming = false;
			Log("local ledger reset");
			RaisePendingChanged();
		}

		// ---------------------------------------------------------------- internals

		private void Commit(string entitledPeriod, Action<bool> onDone)
		{
			// Ghi đã-nhận TRƯỚC, bắn event cấp quà SAU — có chủ đích (xem summary).
			TikTokStorage.SetString(claimedKey, entitledPeriod);
			TikTokStorage.DeleteKey(entitledKey);
			TikTokStorage.Save();
			claiming = false;
			Log($"claimed period={entitledPeriod}");

			try { Claimed?.Invoke(entitledPeriod); }
			catch (Exception e) { Debug.LogError($"{logPrefix} OnRewardClaimed handler threw: {e}"); }

			RaisePendingChanged();
			onDone?.Invoke(true);
		}

		private bool IsClaimed(string current)
		{
			string stored = TikTokStorage.GetString(claimedKey);
			if (string.IsNullOrEmpty(stored))
				return false;
			if (stored == (string.IsNullOrEmpty(current) ? "default" : current))
				return true;
			// Kỳ dạng yyyyMMdd: stored nằm ở tương lai (đồng hồ từng bị xoay tới rồi lùi lại) cũng phải chặn.
			return IsDateStamp(stored) && IsDateStamp(current) && string.CompareOrdinal(current, stored) <= 0;
		}

		private static bool IsDateStamp(string value)
		{
			if (value == null || value.Length != 8)
				return false;
			foreach (char c in value)
				if (c < '0' || c > '9')
					return false;
			return true;
		}

		// Tăng bộ đếm lần hỏi. Trả false khi vượt cap (bỏ cuộc, xoá pending).
		private bool ConsumePendingCheck()
		{
			int attempts = TikTokStorage.GetInt(pendingKey, 0);
			if (attempts <= 0)
				return true; // đường mở game, không tính vào cap

			if (attempts >= MaxPendingChecks)
			{
				TikTokStorage.DeleteKey(pendingKey);
				TikTokStorage.Save();
				Log($"pending gave up after {attempts} checks");
				return false;
			}

			TikTokStorage.SetInt(pendingKey, attempts + 1);
			TikTokStorage.Save();
			return true;
		}

		private string CurrentPeriod()
		{
			try { return period?.Invoke() ?? string.Empty; }
			catch (Exception e) { Debug.LogError($"{logPrefix} ClaimPeriod hook threw: {e}"); return string.Empty; }
		}

		private void RaisePendingChanged()
		{
			try { PendingChanged?.Invoke(); }
			catch (Exception e) { Debug.LogError($"{logPrefix} OnPendingRewardChanged handler threw: {e}"); }
		}

		private void Log(string message) => Debug.Log($"{logPrefix} {message}");
	}
}
