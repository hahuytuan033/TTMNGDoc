using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmobearTTMNG.Ads
{
	public enum TikTokAdType
	{
		Rewarded,
		Interstitial
	}

	/// <summary>
	/// Bảng label → Placement ID của game. Placement ID lấy trên TikTok Developer Portal:
	/// app → Operation → Monetization → In-App Ads (IAAs) → Add ad placement → chuyển Active → copy Placement ID.
	///
	/// Đặt asset tên <c>TikTokAdsConfig</c> trong một thư mục <c>Resources</c> của GAME (menu
	/// AmobearTTMNG/Setup Project tạo sẵn ở Assets/TikTokConfig/Resources) — không đặt trong package vì mỗi game một bộ
	/// Placement ID. Game gọi ads theo label; đổi Placement ID không cần sửa code.
	/// </summary>
	[CreateAssetMenu(fileName = "TikTokAdsConfig", menuName = "AmobearTTMNG/TikTok Ads Config")]
	public class TikTokAdsConfig : ScriptableObject
	{
		public const string ResourcePath = "TikTokAdsConfig";

		[Serializable]
		public class Placement
		{
			[Tooltip("Tên game dùng khi gọi ads, vd rw_revive / it_level_end.")]
			public string Label;

			[Tooltip("Phải khớp loại placement đã tạo trên TikTok Portal (sai loại → lỗi 20007).")]
			public TikTokAdType Type;

			[Tooltip("Placement ID copy từ TikTok Developer Portal (Monetization → In-App Ads).")]
			public string PlacementId;
		}

		[Tooltip("Mỗi label một Placement ID. Label chưa khai báo thì ads coi như không có (rewarded báo unavailable, interstitial bỏ qua).")]
		public List<Placement> Placements = new List<Placement>();

		[Header("Quy định interstitial của TikTok (đừng hạ thấp hơn mặc định)")]
		[Tooltip("Không hiện interstitial trong N giây đầu sau khi module ads khởi động (TikTok: 15s).")]
		[Min(15f)] public float InterstitialStartDelaySeconds = 15f;

		[Tooltip("Khoảng cách tối thiểu giữa hai interstitial (TikTok: 30s). Game muốn thưa hơn thì tăng lên.")]
		[Min(30f)] public float InterstitialIntervalSeconds = 30f;

		[Header("Rewarded")]
		[Tooltip("Tạo sẵn instance rewarded ngay khi SDK sẵn sàng để lần bấm đầu hiện nhanh hơn.")]
		public bool PreloadRewarded = true;

		[Tooltip("Hết thời gian này mà container không gọi lại OnClose/OnError thì coi như lỗi để game không kẹt.")]
		[Min(30f)] public float ShowTimeoutSeconds = 180f;

		private Dictionary<string, Placement> lookup;

		public Placement Find(string label)
		{
			if (string.IsNullOrEmpty(label))
				return null;
			if (lookup == null)
			{
				lookup = new Dictionary<string, Placement>();
				foreach (var p in Placements)
					if (p != null && !string.IsNullOrEmpty(p.Label))
						lookup[p.Label] = p;
			}
			return lookup.TryGetValue(label, out var found) ? found : null;
		}

		private void OnValidate() => lookup = null;

		private static TikTokAdsConfig cached;

		/// <summary>Asset trong Resources; null nếu game chưa tạo.</summary>
		public static TikTokAdsConfig Load()
		{
			if (cached == null)
				cached = Resources.Load<TikTokAdsConfig>(ResourcePath);
			return cached;
		}
	}
}
