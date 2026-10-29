using UnityEngine;

namespace AmobearTTMNG
{
	/// <summary>
	/// Gắn vào RectTransform gốc của UI (con trực tiếp của Canvas, stretch full). Co anchor theo
	/// <see cref="TikTokSafeArea"/> để UI không bị tai thỏ / thanh home / nút capsule của TikTok che.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class SafeAreaFitter : MonoBehaviour
	{
		[Tooltip("Chừa cả vùng nút capsule của TikTok ở trên cùng (nên bật cho HUD có nút góc trên).")]
		[SerializeField] private bool avoidCapsule = true;

		[SerializeField] private bool applyTop = true;
		[SerializeField] private bool applyBottom = true;
		[SerializeField] private bool applySides = true;

		private void OnEnable() => TikTokSafeArea.OnChanged += Apply;

		private void OnDisable() => TikTokSafeArea.OnChanged -= Apply;

		private void Apply()
		{
			var rect = (RectTransform)transform;
			float top = applyTop ? (avoidCapsule ? TikTokSafeArea.NormalizedTopWithCapsule : TikTokSafeArea.NormalizedTop) : 0f;
			float bottom = applyBottom ? TikTokSafeArea.NormalizedBottom : 0f;
			float left = applySides ? TikTokSafeArea.NormalizedLeft : 0f;
			float right = applySides ? TikTokSafeArea.NormalizedRight : 0f;

			rect.anchorMin = new Vector2(left, bottom);
			rect.anchorMax = new Vector2(1f - right, 1f - top);
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}
	}
}
