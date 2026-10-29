using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SAMPLE: AUTO MODE — Let the SDK handle everything via events.
///
/// In this mode you DON'T need to call GetOrders, MarkOrderDelivered, or SetRefundStatus.
/// The SDK fires events at the right time, and you just respond by granting/revoking rewards.
///
/// Flow:
///   1. Player buys product → OnPurchaseSuccess fires → grant rewards
///   2. Player crashes before delivery → next login → OnPendingRewardRecovered fires → grant rewards
///   3. TikTok issues refund → next login → OnRefundNotified fires → revoke rewards
///
/// Setup:
///   1. Attach this script to any GameObject
///   2. Register handlers in Start()
///   3. That's it — the SDK does the rest
/// </summary>
public class SampleAutoMode : MonoBehaviour, HS.TikTokSDK.ITikTokRewardHandler, HS.TikTokSDK.ITikTokRefundHandler
{
    private void Start()
    {
        // Register this script as the reward + refund handler
        HS.TikTokSDK.TikTokSDK.Instance?.RegisterRewardHandler(this);

        // Subscribe to IAP events for additional UI feedback
        var iap = HS.TikTokSDK.IAP.TikTokIAP.Instance;
        if (iap != null)
        {
            iap.OnPurchaseSuccess += OnPurchaseSuccessEvent;
            iap.OnPurchaseFailed += OnPurchaseFailedEvent;
        }
    }

    // ═══════════════════════════════════════════════════
    //  ITikTokRewardHandler — REQUIRED
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Called immediately after a successful purchase.
    /// Grant the rewards to the player here.
    /// </summary>
    public void OnPurchaseRewardsGranted(string productId, List<HS.TikTokSDK.TikTokReward> rewards)
    {
        Debug.Log($"[AutoMode] ✅ Purchase confirmed: product {productId}");
        GrantRewards(rewards);
    }

    /// <summary>
    /// Called on login if there are paid orders that were never delivered
    /// (e.g., player crashed after payment but before rewards were granted).
    /// Grant the rewards here — SDK ensures this is only called once per order.
    /// </summary>
    public void OnPendingRewardsRecovered(string productId, List<HS.TikTokSDK.TikTokReward> rewards)
    {
        Debug.Log($"[AutoMode] 🔄 Recovered pending rewards: product {productId}");
        GrantRewards(rewards);
    }

    // ═══════════════════════════════════════════════════
    //  ITikTokRefundHandler — OPTIONAL
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Called on login if TikTok refunded an order.
    /// Revoke the rewards from the player here.
    /// The backend already deducted from profile_data.
    /// </summary>
    public void OnRefundNotified(string productId, List<HS.TikTokSDK.TikTokReward> revokedRewards)
    {
        Debug.Log($"[AutoMode] ⚠️ Refund: product {productId}");
        RevokeRewards(revokedRewards);
    }

    // ═══════════════════════════════════════════════════
    //  Event Callbacks — for UI feedback
    // ═══════════════════════════════════════════════════

    private void OnPurchaseSuccessEvent(string productId, List<HS.TikTokSDK.TikTokReward> rewards)
    {
        Debug.Log($"[AutoMode] 💰 Purchase UI: Show success animation for {productId}");
        // YourGame.ShowPurchaseSuccessPopup(productId);
    }

    private void OnPurchaseFailedEvent(string error)
    {
        Debug.LogWarning($"[AutoMode] ❌ Purchase failed: {error}");
        // YourGame.ShowPurchaseFailedPopup(error);
    }

    // ═══════════════════════════════════════════════════
    //  Buy a Product — call this from your shop UI
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Call from your shop button. Everything else is automatic.
    /// </summary>
    public void BuyProduct(string productId)
    {
        HS.TikTokSDK.IAP.TikTokIAP.Instance.BuyProduct(productId);
    }

    // ═══════════════════════════════════════════════════
    //  Helpers — adapt to your game
    // ═══════════════════════════════════════════════════

    private void GrantRewards(List<HS.TikTokSDK.TikTokReward> rewards)
    {
        foreach (var reward in rewards)
        {
            switch (reward.type)
            {
                case "hint":
                    Debug.Log($"  → +{reward.amount} hints");
                    // YourGame.AddHints(reward.amount);
                    break;

                case "ads_removed":
                    Debug.Log($"  → Ads removed: {reward.value}");
                    // YourGame.SetAdsRemoved(true);
                    break;

                default:
                    Debug.Log($"  → +{reward.amount} {reward.type}");
                    // YourGame.AddGenericReward(reward.type, reward.amount);
                    break;
            }
        }
    }

    private void RevokeRewards(List<HS.TikTokSDK.TikTokReward> rewards)
    {
        foreach (var reward in rewards)
        {
            Debug.Log($"  → Revoked: {reward.type} x{reward.amount}");
            // YourGame.DeductReward(reward.type, reward.amount);
            // YourGame.RefreshBalanceDisplay();
        }
    }

    private void OnDestroy()
    {
        var iap = HS.TikTokSDK.IAP.TikTokIAP.Instance;
        if (iap != null)
        {
            iap.OnPurchaseSuccess -= OnPurchaseSuccessEvent;
            iap.OnPurchaseFailed -= OnPurchaseFailedEvent;
        }
    }
}
