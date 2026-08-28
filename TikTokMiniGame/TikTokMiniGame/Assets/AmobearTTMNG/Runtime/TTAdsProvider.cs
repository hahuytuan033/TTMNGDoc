#if UNITY_WEBGL
using System;
using UnityEngine;

namespace AmobearTTMNG.Ads
{
/// <summary>
/// Bọc quảng cáo cho TikTok Mini Game, gọi theo <see cref="AdPlacement"/> place label.
///
/// Đi qua <see cref="HSBridge"/> sang HS TikTok SDK bản JavaScript: <c>ads.showAdsRemote(label)</c>.
/// Backend HS giữ bảng mapping label → Ad ID (waterfall), nên game KHÔNG hard-code Ad ID nữa và cũng
/// không tự chọn rewarded hay interstitial — kiểu ad do placement bên backend quyết định.
/// Khác biệt duy nhất giữa hai hàm dưới đây là cách xử lý kết quả:
///   - Rewarded    : chỉ trao thưởng khi <c>completed == true</c> (xem hết ad).
///   - Interstitial: luôn gọi tiếp callback để luồng game không bị chặn.
///
/// LUẬT 15s/30s CỦA TIKTOK GIỜ NẰM Ở BACKEND: bản trước file này tự đếm giờ vì gọi thẳng
/// <c>TT.CreateInterstitialAd</c>, không ai chặn hộ. Nay SDK JS đã có cooldown theo từng placement và
/// frequency cap (mã lỗi <c>AD_FREQUENCY_CAP</c>), nên bỏ đồng hồ nội bộ để tránh chặn hai lần.
/// ĐỔI LẠI: phải cấu hình cooldown cho placement interstitial bên backend HS, không thì không còn ai
/// giữ luật "không show trong 15s đầu" và "hai lần cách nhau tối thiểu 30s" nữa.
///
/// Cam kết giữ nguyên: mọi entry point luôn gọi đúng MỘT callback. Để lọt là game treo ở popup chờ thưởng.
/// </summary>
public static class TTAdsProvider
{
    private static bool rewardedInFlight;
    private static bool interstitialInFlight;
    private static bool statusLogged;

    /// <summary>
    /// Gọi sớm lúc vào game. Không còn preload (SDK tự lo waterfall), chỉ log trạng thái SDK một lần —
    /// doc HS bảo khi cần hỗ trợ thì gửi họ platform / hostName / version.
    /// </summary>
    public static void Prime()
    {
        if (statusLogged)
            return;

        statusLogged = true;
        HSBridge.LogStatus();
    }

    #region Rewarded

    //Show rewarded ở place label <paramref name="place"/>. <paramref name="onReward"/> chỉ gọi khi user
    //xem hết. Mọi trường hợp khác (không có ad, hết hạn mức, tắt sớm, lỗi) → <paramref name="onUnavailable"/>.

    public static void ShowRewarded(string place, Action onReward, Action onUnavailable)
    {
        if (rewardedInFlight)
        {
            Debug.LogWarning($"[TT][Ads] Đang có rewarded hiển thị -> bỏ qua '{place}'.");
            Invoke(onUnavailable);
            return;
        }

        rewardedInFlight = true;

        HSBridge.ShowAdsRemote(place, result =>
        {
            rewardedInFlight = false;

            if (result.Success && result.Completed)
            {
                Invoke(onReward);
                return;
            }

            LogAdFailure("Rewarded", place, result);
            Invoke(onUnavailable);
        });
    }

    #endregion

    #region Interstitial

    //Kiểm tra sơ bộ trước khi show interstitial. Giờ chỉ còn biết được "đang có cái khác chạy" —
    //cooldown/frequency cap do backend quyết định nên client không đoán trước được, phải gọi mới biết.

    public static bool CanShowInterstitial(out string reason)
    {
        if (interstitialInFlight)
        {
            reason = "đang có interstitial hiển thị";
            return false;
        }

        reason = null;
        return true;
    }

    //Show interstitial ở place label <paramref name="place"/>. Đúng một trong <paramref name="onClosed"/> 
    //<paramref name="onUnavailable"/> được gọi.

    public static void ShowInterstitial(string place, Action onClosed, Action onUnavailable)
    {
        if (!CanShowInterstitial(out string reason))
        {
            Debug.Log($"[TT][Ads] Bỏ qua interstitial '{place}': {reason}.");
            Invoke(onUnavailable);
            return;
        }

        interstitialInFlight = true;

        HSBridge.ShowAdsRemote(place, result =>
        {
            interstitialInFlight = false;

            if (result.Success)
            {
                Invoke(onClosed);
                return;
            }

            LogAdFailure("Interstitial", place, result);
            Invoke(onUnavailable);
        });
    }

    #endregion

    /// Log lý do fail đủ để chẩn đoán mà không phải đoán. Phân biệt rõ "cấu hình sai" với "tạm thời
    /// không có ad" — hai thứ này xử lý khác hẳn nhau.
    /// 
    private static void LogAdFailure(string kind, string place, HSAdResult result)
    {
        // success nhưng không completed = user tắt sớm. Bình thường, không phải lỗi.
        if (result.Success && !result.Completed)
        {
            Debug.Log($"[TT][Ads] {kind} '{place}': user tắt sớm -> không trao thưởng.");
            return;
        }

        string hint = DescribeAdError(result.ErrorCode, result.ErrorMessage);
        Debug.LogWarning($"[TT][Ads] {kind} '{place}' không show được. " +
                         $"code={result.ErrorCode} msg='{result.ErrorMessage}' -> {hint}");
    }

    private static string DescribeAdError(int code, string message)
    {
        if (code == 11004)
            return "hết hạn mức quảng cáo, thử lại sau — KHÔNG phải lỗi cấu hình.";

        if (string.IsNullOrEmpty(message))
            return "không rõ lý do; bật vConsole/remote debug của DevTool để xem log JS.";

        if (message.Contains("AD_NO_REMOTE_CONFIG") || message.Contains("remote"))
            return $"label '{message}' chưa được mapping sang Ad ID bên backend HS -> báo bên vận hành ads.";

        if (message.Contains("AD_WATERFALL_EMPTY") || message.Contains("cooldown"))
            return "waterfall rỗng hoặc mọi ad đang trong cooldown -> tạm thời chưa có ad, thử lại sau.";

        if (message.Contains("AD_FREQUENCY_CAP"))
            return "chạm frequency cap của placement -> cấu hình cooldown bên backend đang chặn.";

        if (message.Contains("AD_INVALID_REQUEST"))
            return "place label rỗng hoặc sai định dạng.";

        return "xem error_message ở trên.";
    }

    private static void Invoke(Action callback)
    {
        if (callback == null)
            return;

        try
        {
            callback();
        }
        catch (Exception e)
        {
            Debug.LogError($"[TT][Ads] Callback của game ném exception: {e}");
        }
    }
}
}
#endif
