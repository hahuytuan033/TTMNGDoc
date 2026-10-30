using System;
using System.Collections;
using System.Collections.Generic;
using TTSDK;
using UnityEngine;

namespace AmobearTTMNG
{
	/// <summary>
	/// Share: mở giao diện chuyển tiếp của TikTok để người chơi gửi game cho bạn bè (shareAppMessage).
	///
	/// LUỒNG (theo JS SDK Reference mục 6):
	///   1. Chỉ hiện nút share khi <see cref="IsSupported"/> (canIUse("shareAppMessage")).
	///   2. Người chơi bấm share → <see cref="Share"/> → TikTok mở màn hình chọn danh bạ / bạn bè.
	///      Theo doc JS, title / imageUrl / subTitle do nền tảng lấy theo cấu hình trên developer console;
	///      module vẫn cho truyền (pocket-2 truyền và vẫn chạy) nhưng đừng dựa vào chúng.
	///   3. Bạn bè mở game từ tin nhắn chuyển tiếp → đọc lại query qua <see cref="GetLaunchQuery"/>
	///      (getLaunchOptionsSync) lúc mở game, hoặc <see cref="OnShareQuery"/> (onShow) nếu game đang chạy nền.
	///
	/// QUÀ SHARE (tùy chọn, như pocket-2): share thành công → lưu quyền nhận quà ngay
	/// (<see cref="HasPendingReward"/>) → game hiện popup Claim → <see cref="Claim"/> → <see cref="OnRewardClaimed"/>.
	/// ⚠️ Success của shareAppMessage chỉ nói luồng đã mở, KHÔNG đảm bảo người chơi đã gửi, và nền tảng không có
	/// API xác thực share — nên giới hạn theo <see cref="ClaimPeriod"/> (mặc định mỗi ngày một lần) là hàng rào
	/// duy nhất chống farm. Game không thưởng share thì bỏ qua phần này.
	///
	/// Lưu ý: SDK Unity hiện chưa có wrapper C# cho shareToStory / onCopyUrl.
	/// Trong Editor không có container: <see cref="Share"/> giả lập thành công, query đọc từ
	/// <see cref="EditorSimulateLaunchQuery"/>.
	/// </summary>
	public static class TikTokShare
	{
		private const string LogPrefix = "[Share]";

		/// <summary>Chỉ Editor: query giả lập như khi game được mở từ tin nhắn share (vd "from=share&amp;level=3").</summary>
		public static string EditorSimulateLaunchQuery = string.Empty;

		/// <summary>
		/// Bắn khi game quay lại foreground với query khác rỗng (onShow) — vd người chơi đang chạy nền rồi bấm
		/// vào tin nhắn share. Lúc mở game lần đầu thì dùng <see cref="GetLaunchQuery"/>.
		/// </summary>
		public static event Action<Dictionary<string, string>> OnShareQuery;

		/// <summary>Khóa kỳ nhận quà share; mặc định theo ngày (UTC) → một lần/ngày.</summary>
		public static Func<string> ClaimPeriod = () => DateTime.UtcNow.ToString("yyyyMMdd");

		/// <summary>Hook backend chặn trùng theo open_id + kỳ (xem <see cref="TikTokShortcut.ServerClaim"/>). Null = sổ cục bộ.</summary>
		public static Action<string, Action<bool>> ServerClaim;

		// Share không có bước "quay lại" để chờ: entitlement cấp ngay khi luồng share thành công.
		private static readonly TikTokMissionLedger Ledger = new TikTokMissionLedger(
			LogPrefix, "amobear_tt_share", () => ClaimPeriod?.Invoke(), () => ServerClaim, null, checkOnLaunch: false);

		/// <summary>Có quà share mới chờ Claim, hoặc vừa được Claim.</summary>
		public static event Action OnPendingRewardChanged
		{
			add => Ledger.PendingChanged += value;
			remove => Ledger.PendingChanged -= value;
		}

		/// <summary>Người chơi đã Claim quà share (kèm khóa kỳ). GAME cấp phần thưởng ở đây.</summary>
		public static event Action<string> OnRewardClaimed
		{
			add => Ledger.Claimed += value;
			remove => Ledger.Claimed -= value;
		}

		/// <summary>Share thành công trong kỳ này, đang chờ người chơi bấm Claim.</summary>
		public static bool HasPendingReward => Ledger.HasPendingReward;

		/// <summary>Đã nhận quà share trong kỳ hiện tại chưa. Vẫn nên cho share tiếp kể cả khi đã nhận.</summary>
		public static bool HasClaimedThisPeriod => Ledger.HasClaimedThisPeriod;

		/// <summary>Người chơi bấm Claim quà share. <paramref name="onDone"/>(true) = vừa nhận.</summary>
		public static void Claim(Action<bool> onDone = null) => Ledger.Claim(onDone);

		/// <summary>Chỉ để test: xóa sổ quà share cục bộ.</summary>
		public static void ResetLocalLedger() => Ledger.Reset();

		/// <summary>Client hỗ trợ shareAppMessage. Chỉ đúng sau khi InitSDK xong — UI dùng <see cref="CheckSupported"/>.</summary>
		public static bool IsSupported
		{
			get
			{
#if UNITY_EDITOR
				return true;
#else
				return TikTokBootstrap.IsReady && TikTokShortcut.SafeCanIUse(() => CanIUse.ShareAppMessage);
#endif
			}
		}

		/// <summary>
		/// Chờ InitSDK xong rồi trả <see cref="IsSupported"/>. False thì ẩn nút share. Nếu InitSDK hết timeout rồi mới
		/// thành công, <paramref name="onResult"/> được gọi LẦN HAI với true — UI phải chịu được hai lần gọi.
		/// </summary>
		public static void CheckSupported(Action<bool> onResult) =>
			TikTokShortcut.CheckSupported(onResult, () => IsSupported, Log);

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize()
		{
			TikTokBootstrap.OnShowWithParams -= HandleShow;
			TikTokBootstrap.OnShowWithParams += HandleShow;
		}

		/// <summary>
		/// Mở giao diện chuyển tiếp. <paramref name="query"/> dạng "key1=value1&amp;key2=value2" — bạn bè mở game
		/// từ tin nhắn này sẽ nhận lại nó. <paramref name="onDone"/>(true) = luồng share đã mở (và đã lưu quyền
		/// nhận quà share nếu kỳ này chưa nhận). title / subtitle / path / imageUrl để null = theo cấu hình console.
		/// </summary>
		public static void Share(string query, Action<bool> onDone = null,
			string title = null, string subtitle = null, string path = null, string imageUrl = null)
		{
			Action<bool> done = ok =>
			{
				if (ok)
					Ledger.Entitle();
				onDone?.Invoke(ok);
			};
#if UNITY_EDITOR
			Log($"share (editor) ok query={query} title={title}");
			done(true);
#else
			TikTokBootstrap.WhenReady(sdkReady =>
			{
				if (!IsSupported)
				{
					Debug.LogWarning($"{LogPrefix} share unsupported sdkReady={TikTokBootstrap.IsReady} {TikTokShortcut.DeviceInfo()}");
					done(false);
					return;
				}

				bool answered = false;
				try
				{
					var param = new ShareAppMessageParam
					{
						Query = query ?? string.Empty,
						Success = () =>
						{
							answered = true;
							Log($"share ok query={query}");
							done(true);
						},
						Fail = err =>
						{
							answered = true;
							Debug.LogWarning($"{LogPrefix} share fail code={err?.ErrorCode} msg={err?.ErrMsg} {TikTokShortcut.DeviceInfo()}");
							done(false);
						},
						Complete = () =>
						{
							if (!answered)
								done(false);
						},
					};
					if (!string.IsNullOrEmpty(title)) param.Title = title;
					if (!string.IsNullOrEmpty(subtitle)) param.Subtitle = subtitle;
					if (!string.IsNullOrEmpty(path)) param.Path = path;
					if (!string.IsNullOrEmpty(imageUrl)) param.ImageUrl = imageUrl;
					TT.ShareAppMessage(param);
				}
				catch (Exception e)
				{
					Debug.LogError($"{LogPrefix} ShareAppMessage threw: {e.Message} {TikTokShortcut.DeviceInfo()}");
					done(false);
				}
			});
#endif
		}

		/// <summary>Query của lần mở game này (getLaunchOptionsSync). Rỗng nếu không mở từ tin nhắn share.</summary>
		public static Dictionary<string, string> GetLaunchQuery()
		{
#if UNITY_EDITOR
			return ParseQueryString(EditorSimulateLaunchQuery);
#else
			try
			{
				var options = TT.GetLaunchOptionsSync();
				return options?.Query != null
					? new Dictionary<string, string>(options.Query)
					: new Dictionary<string, string>();
			}
			catch (Exception e)
			{
				Debug.LogWarning($"{LogPrefix} GetLaunchOptionsSync failed: {e.Message}");
				return new Dictionary<string, string>();
			}
#endif
		}

		/// <summary>"a=1&amp;b=2" → {a:1, b:2}. Bỏ qua cặp rỗng, giải mã %xx.</summary>
		public static Dictionary<string, string> ParseQueryString(string query)
		{
			var result = new Dictionary<string, string>();
			if (string.IsNullOrEmpty(query))
				return result;
			foreach (string pair in query.TrimStart('?').Split('&'))
			{
				if (string.IsNullOrEmpty(pair))
					continue;
				int eq = pair.IndexOf('=');
				string key = Uri.UnescapeDataString(eq < 0 ? pair : pair.Substring(0, eq));
				string value = eq < 0 ? string.Empty : Uri.UnescapeDataString(pair.Substring(eq + 1));
				if (key.Length > 0)
					result[key] = value;
			}
			return result;
		}

		/// <summary>{a:1, b:2} → "a=1&amp;b=2" (mã hóa %xx). Dùng để dựng query cho <see cref="Share"/>.</summary>
		public static string ToQueryString(Dictionary<string, string> query)
		{
			if (query == null || query.Count == 0)
				return string.Empty;
			var parts = new List<string>(query.Count);
			foreach (var kv in query)
				parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? string.Empty)}");
			return string.Join("&", parts);
		}

		// ---------------------------------------------------------------- helpers

		// onShow trả "query" dạng object (map) hoặc chuỗi tùy container — chấp nhận cả hai.
		private static void HandleShow(Dictionary<string, object> param)
		{
			if (param == null || !param.TryGetValue("query", out object raw) || raw == null)
				return;

			var query = new Dictionary<string, string>();
			if (raw is IDictionary map)
			{
				foreach (DictionaryEntry entry in map)
					query[entry.Key.ToString()] = entry.Value?.ToString() ?? string.Empty;
			}
			else
			{
				query = ParseQueryString(raw.ToString());
			}

			if (query.Count == 0)
				return;
			Log($"show with query={ToQueryString(query)}");
			try { OnShareQuery?.Invoke(query); }
			catch (Exception e) { Debug.LogError($"{LogPrefix} OnShareQuery handler threw: {e}"); }
		}

		private static void Log(string message) => Debug.Log($"{LogPrefix} {message}");
	}
}
