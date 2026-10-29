mergeInto(LibraryManager.library, {
    HS_TikTokPay: function (tradeOrderIdPtr) {
        var tradeOrderId = UTF8ToString(tradeOrderIdPtr);
        
        // SendMessage target — SDK uses "TikTokIAP" as the GameObject name.
        // If you need a different target, change this line.
        var UNITY_TARGET = "TikTokIAP";
        
        console.log("[HS.TikTokSDK] Requesting payment for order: " + tradeOrderId);
        
        if (typeof TTMinis !== 'undefined' && TTMinis.game && TTMinis.game.pay) {
            try {
                TTMinis.game.pay({
                    trade_order_id: tradeOrderId,
                    success: function () {
                        console.log("[HS.TikTokSDK] Pay UI success — polling starts.");
                        SendMessage(UNITY_TARGET, "OnTikTokPaySuccess");
                    },
                    fail: function (err) {
                        console.error("[HS.TikTokSDK] Pay UI failed:", err);
                        SendMessage(UNITY_TARGET, "OnTikTokPayFailed", JSON.stringify(err));
                    },
                    complete: function () {
                        console.log("[HS.TikTokSDK] Pay UI closed.");
                    }
                });
            } catch (e) {
                console.error("[HS.TikTokSDK] Exception in TTMinis.game.pay:", e);
                SendMessage(UNITY_TARGET, "OnTikTokPayFailed", e.message || "Unknown JS Error");
            }
        } else if (typeof tt !== 'undefined' && tt.pay) {
            try {
                tt.pay({
                    orderInfo: { order_id: tradeOrderId },
                    success: function () {
                        console.log("[HS.TikTokSDK] Legacy tt.pay success.");
                        SendMessage(UNITY_TARGET, "OnTikTokPaySuccess");
                    },
                    fail: function (err) {
                        console.error("[HS.TikTokSDK] Legacy tt.pay failed:", err);
                        SendMessage(UNITY_TARGET, "OnTikTokPayFailed", JSON.stringify(err));
                    }
                });
            } catch (e) {
                console.error("[HS.TikTokSDK] Exception in tt.pay:", e);
                SendMessage(UNITY_TARGET, "OnTikTokPayFailed", e.message || "Unknown JS Error");
            }
        } else {
            console.error("[HS.TikTokSDK] TikTok payment API not available.");
            SendMessage(UNITY_TARGET, "OnTikTokPayFailed", "TikTok environment not detected");
        }
    }
});
