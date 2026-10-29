# Amobear TikTok Mini Game (`com.amobear.ttmng`)

Package dùng chung để đưa game Unity lên TikTok Mini Game: module runtime (InitSDK, storage, ads, shortcut / sidebar,
share, safe area, mạng, rung, HS SDK tùy chọn) + công cụ Editor Setup → Validate → Build → `ttmg`. Cấu hình và luồng
lấy từ game đã lên TikTok thật (pocket-2): Unity **2022.3.62f2**, TikTok Unity SDK **1.1.5-Release**, HS SDK C#
**1.0.4** (login / IAP / cloud save). Quảng cáo gọi thẳng API TikTok theo Placement ID.

Mọi code nằm trong `namespace AmobearTTMNG` (ads: `AmobearTTMNG.Ads`, công cụ Editor: `AmobearTTMNG.EditorTools`).

---

## 1. Đưa vào game mới

**Yêu cầu:** Unity 2022.3 (đã kiểm chứng 2022.3.62f2; Unity 6 chưa phải baseline của TikTok).

1. **Import TTSDK TRƯỚC** — `com.tiktok.minigame@1.1.5-Release.unitypackage` (bản lưu trong repo TTMNGDoc ở
   `Package/`). Package này tham chiếu assembly `TTWebGL` của TTSDK nên thiếu TTSDK là không biên dịch được, kể cả
   menu Setup.
2. *(Tùy chọn)* import HS SDK `com.hs.tiktok-sdk` nếu game dùng login / IAP bằng beans / cloud save.
3. Thêm vào `Packages/manifest.json`:
   ```json
   "com.amobear.ttmng": "https://svn.as.local/tuanvhh/ttmngdoc.git?path=/TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng#ttmng-v1.0.0"
   ```
4. Menu **`AmobearTTMNG/Setup Project`** (chạy lại thoải mái):
   - chuyển sang WebGL + IL2CPP, bộ nhớ 128MB, tắt nén / data caching, bật bulk-memory;
   - `Assets/Editor/StarkBuilderSetting.asset`: output `<project>/tt-minigame`, WebGL2, bulk memory;
   - tạo `Assets/TikTokConfig/Resources/TikTokAdsConfig.asset` nếu chưa có;
   - có HS SDK → bật define `AMOBEAR_TTMNG_HS` (assembly `AmobearTTMNG.HS`), không có → gỡ;
   - scene 0 chưa có `TikTokBootstrap` → đề nghị tạo `Assets/TikTokConfig/TikTokSetup.unity` làm scene 0, xong InitSDK
     thì chuyển sang scene game cũ (field `nextScene`);
   - App ID còn trống → mở cửa sổ **`AmobearTTMNG/Set App ID`**.
5. Điền phần riêng của game (xem bảng mục 1.1): Placement ID quảng cáo (mục 5), HS `gameId` / `sdkKey` trên
   `HS_TikTokSDK` ở scene 0 (menu **`IAP TikTok Setup/Setup SDK in Scene`**), sản phẩm IAP trên console HS (mẫu:
   `Documentation~/tiktok_products.sample.json`), whitelist domain backend HS trên TikTok Developer Portal (login lỗi
   `21100`).
6. **`AmobearTTMNG/Validate Project`** — lỗi (✗) chặn build, cảnh báo (!) nên sửa.
7. **`AmobearTTMNG/Build and Validate`** — gọi `TTSDK.Tool.Builder.BuildWebGL` rồi kiểm gói `tt-minigame/tt-minigame`:
   có `game.js` / `game.json` / `wasmcode/` / `data-package/`, `gameEngine = "unity"`, `sdkVersion`, `wasmFuncCount`
   (≥ 80000 → phải tách Wasm), tổng gói ≤ **60MB**.
8. **`AmobearTTMNG/ttmg dev (QR preview)`** mở terminal tại thư mục gói để quét QR test máy thật;
   **`AmobearTTMNG/ttmg upload`** tạo bản test trên Developer Portal (luôn hỏi lại). Cần `npm i -g @ttmg/cli`.
   Chỉ upload thư mục gói này — không upload project Unity hay thư mục WebGL thường.

> Mỗi game một bộ key riêng — không dùng lại App ID / Placement ID / Game ID / SDK Key của game khác.

### 1.1. Phần nào thuộc package, phần nào thuộc game

| Thuộc package (cập nhật bằng cách đổi tag) | Thuộc từng game (nằm trong Assets / scene của game) |
|---|---|
| `Runtime/` (assembly `AmobearTTMNG.Runtime`) | `Assets/Plugins/com.tiktok.minigame` (TTSDK), `Assets/Plugins/com.hs.tiktok-sdk` (HS) |
| `Runtime/HS/` (assembly `AmobearTTMNG.HS`, chỉ build khi có define) | `Assets/Editor/StarkBuilderSetting.asset` (App ID, output — TTSDK chỉ đọc đúng đường dẫn này) |
| `Editor/` (menu `AmobearTTMNG/...`) | `Assets/TikTokConfig/Resources/TikTokAdsConfig.asset` (Placement ID) |
| | Scene 0 có `TikTokBootstrap` (+ `HS_TikTokSDK` với gameId / sdkKey) |
| | Phần thưởng: handler `OnRewardClaimed`, `ClaimPeriod`, `ServerClaim` |

### 1.2. Nâng / cố định phiên bản

Đổi tag cuối URL trong `manifest.json` (`#ttmng-v1.0.0` → tag mới) rồi đọc `CHANGELOG.md`. Package chỉ chạy với
TTSDK **1.1.5-Release**; Setup / Validate báo lỗi nếu khác. Đổi bản TTSDK = test lại toàn bộ module trên máy thật.

---

## 2. Module

| Module | Dùng để | Ghi chú |
|---|---|---|
| `TikTokBootstrap` | InitSDK, `WhenReady`, `WhenSdkReady`, `OnShow`/`OnHide`/`OnShowWithParams` | Xem mục 2.1. OnHide tự `TikTokStorage.Save()`. |
| `TikTokStorage` | Key-value bền (`tt.setStorageSync`) | Probe ghi→đọc lúc khởi động, hỏng thì fallback PlayerPrefs. Chuỗi rỗng = xoá key. |
| `TikTokFileSystem` | Ghi/đọc file trong container | `persistentDataPath` / C# file IO KHÔNG bền trên TikTok. Editor trả false/null. |
| `TikTokHS` *(tùy chọn)* | Đăng nhập + IAP (beans) + cloud save | Chỉ có khi bật `AMOBEAR_TTMNG_HS`. Tự login lúc mở game. Quà tới qua `OnRewardsGranted`, KHÔNG cấp lúc gọi `BuyProduct`. |
| `TikTokAds` + `TikTokAdsConfig` | Rewarded / Interstitial theo label | Gọi `TT.CreateRewardedVideoAd` / `TT.CreateInterstitialAd` với Placement ID trong config. Chặn sẵn luật 15s / 30s và ad chồng nhau. |
| `TikTokShortcut` | Desktop Shortcut Revisit | Nhận quà hai bước. Mặc định nhận **một lần**. |
| `TikTokSidebar` | Sidebar Revisit | Nhận quà hai bước. Mặc định **mỗi ngày một lần**. |
| `TikTokShare` | `shareAppMessage` + đọc query khi bạn bè mở | Quà share tuỳ chọn, mỗi ngày một lần. |
| `TikTokSafeArea` / `SafeAreaFitter` | Vùng an toàn + vùng nút capsule | WebGL `Screen.safeArea` luôn trả toàn màn hình — phải dùng module này. |
| `TikTokNetwork` | Loại mạng, mất mạng, mạng yếu | `OnChanged` replay-on-subscribe. |
| `TikTokVibrate` | Rung ngắn / dài | Chặn gọi dồn 0.1s; `Enabled = false` khi người chơi tắt rung. |

### 2.1. InitSDK chậm

`TikTokBootstrap` chờ InitSDK tối đa 8s rồi cho game chạy tiếp ở chế độ không SDK; callback vẫn có thể đến muộn.

- `WhenReady(ok => ...)`: trả lời NGAY khi init có kết quả — sau timeout là `false`. Dùng cho lệnh người chơi bấm.
- `WhenSdkReady(() => ...)`: chạy khi SDK sẵn sàng, **kể cả thành công sau timeout**. Dùng cho việc lúc khởi động cần
  SDK (preload, gắn listener, đăng nhập). Các module trong package đã dùng nó: thưởng Shortcut / Sidebar được hỏi lại,
  ads preload, mạng, safe area, HS login đều tự chạy lại.
- `CheckSupported` (Shortcut / Sidebar / Share) có thể gọi callback **hai lần** (`false` rồi `true`) — UI bật / tắt
  nút theo giá trị mới nhất. `TikTokHS.OnLoginResult` tương tự (`ttsdk_init_timeout` rồi kết quả thật).

---

## 3. Nhận quà Shortcut / Sidebar / Share (template chỉ làm luồng, GAME quyết phần thưởng)

```csharp
using AmobearTTMNG;

// 1. Lối vào
TikTokShortcut.CheckSupported(ok => shortcutButton.SetActive(ok));
shortcutButton.onClick → TikTokShortcut.Add(ok => ShowHint("Mở lại game từ icon để nhận quà"));

// 2. Module tự hỏi nền tảng lúc mở game + mỗi OnShow. Đủ điều kiện → quyền nhận quà được LƯU NGAY.
TikTokShortcut.OnPendingRewardChanged += () => claimPopup.SetActive(TikTokShortcut.HasPendingReward);

// 3. Người chơi bấm Claim → event → game cấp quà của mình.
claimButton.onClick → TikTokShortcut.Claim();
TikTokShortcut.OnRewardClaimed += period => Wallet.Add(500);
```

- Lúc mở scene, kiểm `HasPendingReward` (quà có thể còn treo từ phiên trước).
- Đổi chu kỳ: `TikTokShortcut.ClaimPeriod = () => DateTime.UtcNow.ToString("yyyyMMdd");`
- **Chống nhận trùng:** mặc định chỉ có sổ cục bộ (chặn trùng trên một máy). Doc Shortcut / Sidebar của TikTok yêu cầu
  backend ghi sổ theo `open_id + mission + kỳ`; package chưa có backend chung — game có server thì gán
  `ServerClaim = (period, done) => { /* gọi server, done(true) chỉ lần đầu */ };`
- Sidebar / Share dùng API giống hệt (`TikTokSidebar.Start`, `TikTokShare.Share`).
- Test: DevTool launch mode `scene_shortcut` / `scene_center`. Icon thật chỉ mở **bản đã phát hành**.
  Editor: tick `Simulate … In Editor` trên `ShortcutDemo` khi đang Play.

## 4. IAP (HS SDK)

```csharp
TikTokHS.OnRewardsGranted += (productId, rewards, source) =>
{
    foreach (var r in rewards)
        switch (r.type) { case "coin": Wallet.Add(r.amount); break; case "no_ads": Profile.NoAds = true; break; }
};
TikTokHS.OnRewardsRevoked += (productId, rewards) => { /* trừ lại khi TikTok hoàn tiền */ };
buyButton.onClick → TikTokHS.BuyProduct("coin_pack_small");

// Cloud save (tuỳ chọn)
TikTokHS.SerializeForCloud = () => JsonUtility.ToJson(save);
TikTokHS.ApplyCloudData = (json, ts) => save = JsonUtility.FromJson<Save>(json);
// sau mỗi lần save: TikTokHS.MarkCloudDirty();
```

- `source == "pending"`: đơn đã trả tiền ở phiên trước nhưng chưa kịp giao — vẫn phải cấp.
- `TikTokReward.type` là chuỗi do console đặt — thêm case khi console thêm loại quà mới.

## 5. Quảng cáo (theo https://developers.tiktok.com/docs/en/in-app-ads)

**Lấy Placement ID (mỗi game một bộ riêng):**
1. Organization đã bật + được duyệt **IAA** (Monetization → Feature enablement, chỉ admin; duyệt 1–2 ngày).
2. Developer Portal → app → **Operation → Monetization → In-App Ads (IAAs)** → bật **Ad placements**.
3. **Add ad placement** → đặt tên, chọn **Rewarded** hoặc **Interstitial** → **Add**.
4. Chuyển placement sang **Active** (mặc định inactive → lỗi `20003`).
5. Copy **Placement ID** → điền vào `Resources/TikTokAdsConfig.asset`:

| Label (game dùng) | Type | Placement ID |
|---|---|---|
| `rw_revive` | Rewarded | `<copy từ Portal>` |
| `it_level_end` | Interstitial | `<copy từ Portal>` |

**Gọi trong game:**
```csharp
using AmobearTTMNG.Ads;
TikTokAds.ShowVideoReward(() => GiveReward(), "rw_revive");        // chỉ chạy khi xem HẾT
TikTokAds.OnRewardUnavailable += (label, reason) => ShowToast("Chưa có quảng cáo, thử lại sau");
TikTokAds.ShowInterstitial(() => ContinueFlow(), "it_level_end");  // LUÔN chạy đúng một lần
TikTokAds.IsAdsActive = () => !Profile.NoAds;                      // Remove Ads chỉ tắt interstitial
```

- Interstitial chỉ đặt ở chỗ nghỉ tự nhiên (hết màn, chuyển scene) — KHÔNG giữa lúc đang chơi, không hẹn giờ bật ngang.
- Module tự chặn: ad chồng nhau (`11006`), interstitial trong **15s** đầu và cách nhau **< 30s** (`11004`). Tăng
  `InterstitialIntervalSeconds` trong config nếu muốn thưa hơn — không hạ dưới 30.
- Rewarded instance được tạo sẵn khi SDK init (`PreloadRewarded`); mỗi instance show một lần rồi tạo lại.
- Label chưa khai báo / sai loại / Placement ID rỗng → rewarded báo unavailable, interstitial bỏ qua (xem log `[Ads]`).
- Editor: giả lập "xem hết"; bật `TikTokAds.EditorSimulateNoFill` để test nhánh không có ad.
- Test thật: DevTool → **tắt Enable IAA Mock**, bật Show vConsole để xem log. Doanh thu chỉ xem được sau khi game lên chính thức.
- Không có banner / app-open.

Mã lỗi chính thức của TikTok (Error Code Reference, phần IAA):

| Mã | Nghĩa | Xử lý |
|---|---|---|
| `20001` / `20002` / `20003` / `20004` / `20007` | Placement không tồn tại / không thuộc game / chưa Active / IAA chưa bật / sai loại | Sửa trên Developer Portal (Monetization → In-App Ads) |
| `30001` (init) | Gọi cùng ad unit quá dày | Giảm tần suất gọi |
| `11003` | Instance đã show rồi | Mỗi instance chỉ show một lần — tạo mới |
| `11004` | Interstitial chưa đủ khoảng cách | Quy định: không show trong **15s** đầu sau khi module ads khởi động, cách nhau **≥ 30s** |
| `11005` | Interstitial vượt số lần hiển thị | Giảm tần suất |
| `11006` | Đang có ad khác hiện | Gọi show tuần tự |
| `30001` (show) | Người chơi đóng rewarded trước khi xem hết | Không thưởng, cho chơi tiếp |
| `50000` + `inner_error_code` | Lỗi hệ thống (50001 client key sai, 50008 container đang pre-render...) | Gửi support kèm inner code |

Khi duyệt bản phát hành (Acceptance Testing), TikTok yêu cầu có: **rewarded ad, IAP, Silent Login, Home Screen Shortcut, Revisit From Profile**.
Quà shortcut phải **chỉ nhận được một lần**. Test quảng cáo thật thì tắt **Enable IAA Mock** trong DevTool.

---

## 6. Test trên máy thật (không tự động được)

- **Quét QR không vào được** (`ttmg dev` kẹt "connecting" / timeout): tài khoản quét phải nằm trong **Test User** của
  app; điện thoại và máy dev cùng mạng / cùng dải IP (Wi-Fi khách của công ty thường cách ly thiết bị); tắt VPN chế độ
  Global / TUN, nếu cần proxy thì `ttmg config set proxy http://127.0.0.1:<port>` thay vì VPN toàn hệ thống; firewall
  phải cho **Node.js** nhận kết nối ở cả mạng Private và Public (phát hotspot từ điện thoại là mạng Public); máy có
  nhiều card mạng (VM, dây + Wi-Fi) thì IP trên QR có thể sai.
- Mở lần đầu, mở lần hai, ra nền rồi quay lại, mạng, UI trên máy đích. Ghi dữ liệu → thoát → mở lại → còn dữ liệu.
- Shortcut: DevTool launch mode `scene_shortcut`. Icon thật trên màn hình chính chỉ mở **bản đã phát hành** — bản
  preview / debug không mở được từ icon.
- Sidebar: DevTool launch mode `scene_center`, rồi luồng thật: vào bình thường → Revisit → vào lại từ sidebar trang
  cá nhân.
- Quảng cáo thật: tắt **Enable IAA Mock** trong DevTool.

## 7. Khi báo lỗi cho TikTok

Gửi một lần: ClientKey, Unity version, TikTok Unity SDK version, cấu trúc thư mục `tt-minigame`, dung lượng gói,
có bật Wasm split không, TikTok App version, thiết bị + OS, SystemInfo, lỗi đầu tiên, build log, các bước tái hiện,
video. Các module đã log kèm `platform / system / tiktok version` (`TikTokShortcut.DeviceInfo()`).

Khi `game.json.wasmFuncCount` ≥ 80000 → xem Unity Wasm Code Splitting Guide.
