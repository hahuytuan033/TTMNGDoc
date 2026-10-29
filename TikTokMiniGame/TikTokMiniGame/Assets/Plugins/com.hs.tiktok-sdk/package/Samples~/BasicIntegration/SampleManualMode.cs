using System.Collections.Generic;
using UnityEngine;
using HS.TikTokSDK.IAP;

/// <summary>
/// SAMPLE: MANUAL MODE — Full control over order lifecycle.
///
/// In this mode YOU query orders, grant/revoke rewards, and confirm delivery.
/// The SDK doesn't auto-deliver anything — you drive the entire flow.
///
/// ⚠️ PREREQUISITE: Set your game to "manual" mode in the Admin Dashboard:
///    Admin Console → Games → [Your Game] → Refund Mode → "manual"
///    Without this, the backend auto-marks orders as delivered on login.
///
/// Use this when you need:
///   - Custom order history UI
///   - Deferred reward granting (e.g., wait for animation to finish)
///   - Custom refund approval/rejection logic
///   - Background polling for order status changes
///
/// Available order statuses for GetOrders(status):
///   "PENDING"          — Order created, payment not yet confirmed
///   "PAID"             — Payment confirmed, rewards ready to deliver
///   "FAILED"           — Payment failed or expired
///   "REFUND_PENDING"   — Refund requested, waiting for game to review
///   "REFUND_APPROVED"  — Refund approved (by webhook/admin), game should revoke rewards
///   "REFUNDED"         — Refund fully processed (terminal state)
///   "REFUND_REJECTED"  — Refund rejected by game, rewards kept (terminal state)
///   null               — All orders (no filter)
///
/// Typical flows:
///
///   PURCHASE:
///     BuyProduct() → GetOrders("PAID") → grant rewards → MarkOrderDelivered()
///
///   PENDING DELIVERY (crash recovery):
///     GetOrders("PAID") → filter IsPendingDelivery → grant rewards → MarkOrderDelivered()
///
///   REFUND:
///     GetOrders("REFUND_PENDING") → review → SetRefundStatus("REFUNDED" or "REFUND_REJECTED")
///
///   REFUND APPROVED (by admin/webhook):
///     GetOrders("REFUND_APPROVED") → revoke rewards → SetRefundStatus("REFUNDED")
///
/// Setup:
///   1. Set game to "manual" mode in Admin Dashboard
///   2. Attach this script to any GameObject
///   3. Call methods from your game UI / game loop
///   4. Do NOT register ITikTokRewardHandler — it auto-grants rewards on purchase/recovery,
///      which conflicts with your manual GetOrders → grant → MarkOrderDelivered flow
///      (rewards would be granted twice)
/// </summary>
public class SampleManualMode : MonoBehaviour
{
    // ═══════════════════════════════════════════════════
    //  BUY
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Start a purchase. Listen to OnPurchaseSuccess to know when to deliver.
    /// </summary>
    public void BuyProduct(string productId)
    {
        var iap = TikTokIAP.Instance;
        iap.OnPurchaseSuccess += OnPurchaseComplete;
        iap.OnPurchaseFailed += OnPurchaseFail;
        iap.BuyProduct(productId);
    }

    private void OnPurchaseComplete(string productId, List<HS.TikTokSDK.TikTokReward> rewards)
    {
        Debug.Log($"[ManualMode] Purchase confirmed: {productId}");

        // 1. Grant rewards in your game
        foreach (var r in rewards)
        {
            Debug.Log($"  → Grant: {r.type} x{r.amount}");
            // YourGame.GrantReward(r.type, r.amount);
        }

        // 2. Mark delivered — IMPORTANT: always call this after granting rewards
        //    Without this, the order stays as "pending delivery" forever
        // Note: You need the order_id. Fetch it via GetOrders("PAID") or store it.
        DeliverPendingOrders();

        // Unsubscribe
        TikTokIAP.Instance.OnPurchaseSuccess -= OnPurchaseComplete;
        TikTokIAP.Instance.OnPurchaseFailed -= OnPurchaseFail;
    }

    private void OnPurchaseFail(string error)
    {
        Debug.LogWarning($"[ManualMode] Purchase failed: {error}");
        // YourGame.ShowPurchaseFailedPopup(error);

        TikTokIAP.Instance.OnPurchaseSuccess -= OnPurchaseComplete;
        TikTokIAP.Instance.OnPurchaseFailed -= OnPurchaseFail;
    }

    // ═══════════════════════════════════════════════════
    //  DELIVER PENDING ORDERS
    //  Call on login + after each purchase
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Fetch all PAID orders → find undelivered → grant rewards → mark delivered.
    /// Call this on game start (after login) and after each purchase.
    /// </summary>
    public void DeliverPendingOrders()
    {
        TikTokIAP.Instance.GetOrders((success, orders, error) =>
        {
            if (!success)
            {
                Debug.LogError($"[ManualMode] Failed to fetch orders: {error}");
                return;
            }

            foreach (var order in orders)
            {
                // Skip already-delivered orders
                if (!order.IsPendingDelivery) continue;

                Debug.Log($"[ManualMode] Delivering order #{order.order_id}: {order.product_name}");

                // 1. Grant rewards
                foreach (var reward in order.rewards)
                {
                    Debug.Log($"  → Grant: {reward.type} x{reward.amount}");
                    // YourGame.GrantReward(reward.type, reward.amount);
                }

                // 2. Confirm delivery to backend
                TikTokIAP.Instance.MarkOrderDelivered(order.order_id, (ok, err) =>
                {
                    if (ok) Debug.Log($"  ✅ Order #{order.order_id} delivered");
                    else    Debug.LogError($"  ❌ Delivery failed: {err}");
                });
            }
        }, "PAID");
    }

    // ═══════════════════════════════════════════════════
    //  PROCESS REFUNDS
    //  Call periodically or on login
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Step 1: Check for refunds pending your review.
    /// These come from TikTok webhooks or admin actions.
    /// </summary>
    public void CheckPendingRefunds()
    {
        TikTokIAP.Instance.GetOrders((success, orders, error) =>
        {
            if (!success) return;

            Debug.Log($"[ManualMode] {orders.Count} refund(s) pending review");
            foreach (var order in orders)
            {
                Debug.Log($"  #{order.order_id} | {order.product_name} | {order.refund_amount} beans");
                // YourGame.ShowRefundReviewDialog(order);
            }
        }, "REFUND_PENDING");
    }

    /// <summary>
    /// Step 2a: Accept refund → revoke rewards → set status to REFUNDED.
    /// </summary>
    public void AcceptRefund(int orderId, List<HS.TikTokSDK.TikTokReward> rewards)
    {
        // 1. Revoke rewards from player
        foreach (var r in rewards)
        {
            Debug.Log($"  → Revoke: {r.type} x{r.amount}");
            // YourGame.DeductReward(r.type, r.amount);
        }

        // 2. Confirm to backend
        TikTokIAP.Instance.SetRefundStatus(orderId, "REFUNDED", (ok, err) =>
        {
            if (ok) Debug.Log($"  ✅ Order #{orderId} → REFUNDED");
            else    Debug.LogError($"  ❌ Failed: {err}");
        });
    }

    /// <summary>
    /// Step 2b: Reject refund → keep rewards → set status to REFUND_REJECTED.
    /// </summary>
    public void RejectRefund(int orderId)
    {
        TikTokIAP.Instance.SetRefundStatus(orderId, "REFUND_REJECTED", (ok, err) =>
        {
            if (ok) Debug.Log($"  ✅ Order #{orderId} → REFUND_REJECTED (rewards kept)");
            else    Debug.LogError($"  ❌ Failed: {err}");
        });
    }

    // ═══════════════════════════════════════════════════
    //  PROCESS APPROVED REFUNDS
    //  For webhook-auto-approved refunds
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Fetch refunds already approved (by webhook/admin) → revoke rewards → set REFUNDED.
    /// </summary>
    public void ProcessApprovedRefunds()
    {
        TikTokIAP.Instance.GetOrders((success, orders, error) =>
        {
            if (!success || orders.Count == 0) return;

            Debug.Log($"[ManualMode] Processing {orders.Count} approved refund(s)...");
            foreach (var order in orders)
            {
                // 1. Revoke rewards
                foreach (var reward in order.rewards)
                {
                    Debug.Log($"  → Revoke: {reward.type} x{reward.amount} (order #{order.order_id})");
                    // YourGame.DeductReward(reward.type, reward.amount);
                }

                // 2. Mark as fully refunded
                TikTokIAP.Instance.SetRefundStatus(order.order_id, "REFUNDED", (ok, err) =>
                {
                    if (ok) Debug.Log($"  ✅ Order #{order.order_id} → REFUNDED");
                    else    Debug.LogError($"  ❌ Failed: {err}");
                });
            }
        }, "REFUND_APPROVED");
    }

    // ═══════════════════════════════════════════════════
    //  QUERY HELPERS
    // ═══════════════════════════════════════════════════

    /// <summary>Fetch orders by status for custom UI.</summary>
    public void FetchOrders(string status = null)
    {
        TikTokIAP.Instance.GetOrders((success, orders, error) =>
        {
            if (!success)
            {
                Debug.LogError($"[ManualMode] Fetch failed: {error}");
                return;
            }

            Debug.Log($"[ManualMode] {orders.Count} order(s) with status={status ?? "ALL"}:");
            foreach (var o in orders)
            {
                Debug.Log($"  #{o.order_id} | {o.product_name} | {o.status} | pay_status={o.pay_status}");
            }
        }, status);
    }
}
