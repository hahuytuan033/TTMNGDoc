// Cầu nối C# <-> HS TikTok SDK (bản JavaScript 1.0.5) — CHỈ LO ADS.
//
// Ads-only bolt-on: ads đi qua bản JS 1.0.5 (ads.showAdsRemote(label)) vì bản C# HS.TikTokSDK.dll
// 1.0.4 KHÔNG có module ads. Nếu game dùng bản C# 1.0.4 cho Auth/IAP/CloudSave thì phần đó KHÔNG
// đi qua bridge này (nên ở đây không có HS_BuyProduct / HS_GetShopItems / reward handler).
//
// Quy ước: mọi lệnh đều bất đồng bộ. C# truyền reqId, JS trả kết quả về ĐÚNG MỘT LẦN qua
// SendMessage("HSBridge", "OnHSMessage", json) với { kind: 'result', id: reqId, ... }.
//
// LUẬT BẤT DI BẤT DỊCH: không có đường nào được phép thoát mà không trả kết quả. C# có
// watchdog timeout đỡ phía sau, nhưng để nó phải đợi 180 giây mới biết là tệ — mọi nhánh
// lỗi ở đây đều trả về ngay.
mergeInto(LibraryManager.library, {

  // C# gọi khi GameObject "HSBridge" đã tồn tại. Xả hàng đợi event đã tích lại từ lúc
  // bootstrap chạy trong game.js (khi đó Unity chưa load, chưa SendMessage được).
  HS_BridgeReady: function () {
    var B = GameGlobal.__HSBridge;
    if (!B) {
      console.error('[HSBridge] __HSBridge chưa có — game.js chưa nhúng bootstrap. ' +
                    'Chạy "Tools/TikTok/5. Tạo-cập nhật CustomizeTemplate" rồi build lại.');
      return;
    }
    B.send = function (json) {
      try {
        SendMessage('HSBridge', 'OnHSMessage', json);
      } catch (e) {
        console.error('[HSBridge] SendMessage thất bại (GameObject "HSBridge" bị xoá/đổi tên?):', e);
      }
    };
    B.ready = true;
    B.flush();
    console.log('[HSBridge] C# đã sẵn sàng, đã xả hàng đợi.');
  },

  HS_ShowAdsRemote: function (reqId, labelPtr) {
    var label = UTF8ToString(labelPtr);
    var B = GameGlobal.__HSBridge;

    function done(success, completed, code, message) {
      var evt = {
        kind: 'result', op: 'ads', id: reqId, label: label,
        success: !!success, completed: !!completed,
        error_code: code || 0, error_message: message || ''
      };
      if (B) B.emit(evt);
      else try { SendMessage('HSBridge', 'OnHSMessage', JSON.stringify(evt)); } catch (e) { }
    }

    if (!B || typeof HSTikTokSDK === 'undefined') {
      console.error('[HSBridge] showAdsRemote("' + label + '") gọi khi SDK/bootstrap chưa sẵn sàng.');
      done(false, false, -1, 'SDK chưa nạp (bootstrap không chạy?)');
      return;
    }

    console.log('[HSBridge] showAdsRemote("' + label + '") ...');

    try {
      HSTikTokSDK.ads.showAdsRemote(label)
        .then(function (r) {
          r = r || {};
          console.log('[HSBridge] ads "' + label + '" -> success=' + r.success +
                      ' completed=' + r.completed + ' code=' + r.error_code +
                      ' msg=' + (r.error_message || ''));
          // Doc: chỉ trao thưởng khi completed. success = có ad chạy, completed = xem hết.
          done(r.success, r.completed, r.error_code, r.error_message);
        })
        .catch(function (e) {
          console.error('[HSBridge] ads "' + label + '" reject:', e);
          done(false, false, -1, String(e));
        });
    } catch (e) {
      console.error('[HSBridge] ads "' + label + '" ném exception đồng bộ:', e);
      done(false, false, -1, String(e));
    }
  },

  // Trạng thái để chẩn đoán. Doc bảo khi bí thì gửi platform/hostName/version.
  HS_RequestStatus: function (reqId) {
    var B = GameGlobal.__HSBridge;

    function send(evt) {
      if (B) B.emit(evt);
      else try { SendMessage('HSBridge', 'OnHSMessage', JSON.stringify(evt)); } catch (e) { }
    }

    if (typeof HSTikTokSDK === 'undefined') {
      send({ kind: 'result', op: 'status', id: reqId, success: false, error_message: 'SDK chưa nạp' });
      return;
    }

    try {
      send({
        kind: 'result', op: 'status', id: reqId, success: true,
        isAuthenticated: !!HSTikTokSDK.isAuthenticated,
        isTestMode: !!HSTikTokSDK.isTestMode,
        openId: HSTikTokSDK.openId || '',
        version: HSTikTokSDK.version || '',
        platform: HSTikTokSDK.platform || '',
        hostName: HSTikTokSDK.hostName || ''
      });
    } catch (e) {
      send({ kind: 'result', op: 'status', id: reqId, success: false, error_message: String(e) });
    }
  }
});
