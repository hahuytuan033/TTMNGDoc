using AmobearTTMNG;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TikTokMiniGameDemo
{
    /// <summary>
    /// Scene Demo — luồng Desktop Shortcut Revisit theo doc của TikTok:
    ///   1. Chờ InitSDK, chỉ hiện nút "Add Shortcut" khi client hỗ trợ.
    ///   2. Nút mở popup giải thích hành động + điều kiện thưởng; Start gọi <see cref="TikTokShortcut.Add"/>.
    ///   3. Add thành công chỉ nhắc người chơi mở lại từ icon — KHÔNG phát thưởng.
    ///   4. Mỗi lần vào game (và khi quay lại foreground) gọi <see cref="TikTokShortcut.TryClaim"/>:
    ///      chỉ phiên mở từ icon mới đủ điều kiện, sổ cục bộ chặn nhận trùng trong ngày.
    /// Các tham chiếu được gán sẵn trong scene Demo.unity.
    /// </summary>
    public class ShortcutDemo : MonoBehaviour
    {
        private const int RewardCoins = 100;
        private const string CoinsKey = "demo_shortcut_coins";

        public Button AddShortcutButton;
        public GameObject Popup;
        public TMP_Text PopupText;
        public TMP_Text PopupDescription;
        public Button StartButton;
        /// <summary>Nền tối của popup — chạm ra ngoài panel để đóng.</summary>
        public Button PopupBackground;
        public TMP_Text StatusText;
        public Button ApiDemoButton;

        private void Start()
        {
            Popup.SetActive(false);
            AddShortcutButton.gameObject.SetActive(false);
            StatusText.text = $"Coins: {PlayerPrefs.GetInt(CoinsKey, 0)}";

            AddShortcutButton.onClick.AddListener(OpenPopup);
            StartButton.onClick.AddListener(OnStartClicked);
            PopupBackground.onClick.AddListener(() => Popup.SetActive(false));
            ApiDemoButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            // Bước 2 doc: không hỗ trợ thì ẩn lối vào, không gọi addShortcut.
            TikTokShortcut.CheckSupported(supported =>
            {
                if (this != null)
                    AddShortcutButton.gameObject.SetActive(supported);
            });

            // Bước 5 doc: kiểm tra thưởng ở phiên được mở (từ icon thì canReceiveReward=true).
            ClaimShortcutReward();
            TikTokBootstrap.OnShow += ClaimShortcutReward;
        }

        private void OnDestroy()
        {
            TikTokBootstrap.OnShow -= ClaimShortcutReward;
        }

        private void OpenPopup()
        {
            // Bước 3 doc: lời dẫn phải nói rõ hành động và điều kiện nhận thưởng.
            PopupText.text = "Add Shortcut";
            PopupDescription.text = $"Add the game to your home screen.\nOpen it from the icon next time to get {RewardCoins} coins!";
            StartButton.interactable = true;
            Popup.SetActive(true);
        }

        private void OnStartClicked()
        {
            StartButton.interactable = false;
            // Bước 4 doc: thành công chỉ là hộp thoại thêm icon đã chạy — chỉ nhắc mở lại từ icon.
            TikTokShortcut.Add(ok =>
            {
                if (this == null)
                    return;
                Popup.SetActive(false);
                StatusText.text = ok
                    ? $"Shortcut added! Close the game and open it from the home-screen icon to get {RewardCoins} coins."
                    : "Could not add the shortcut on this device.";
            });
        }

        private void ClaimShortcutReward()
        {
            // Bước 6 doc: TryClaim ghi sổ trước khi gọi grant → không phát trùng.
            TikTokShortcut.TryClaim(grant: () =>
            {
                int coins = PlayerPrefs.GetInt(CoinsKey, 0) + RewardCoins;
                PlayerPrefs.SetInt(CoinsKey, coins);
                PlayerPrefs.Save();
                if (this != null)
                    StatusText.text = $"Welcome back from the home screen! +{RewardCoins} coins\nCoins: {coins}";
            });
        }
    }
}
