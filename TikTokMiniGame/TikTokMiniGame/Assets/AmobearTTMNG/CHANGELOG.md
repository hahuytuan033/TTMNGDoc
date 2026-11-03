# Changelog

## [Unreleased]

### Added
- `TikTokLogin`: Silent Login (`tt.login`) theo checklist Acceptance Testing (bắt buộc kể cả game chỉ có ads). Không
  cần backend — chỉ giữ `Code`; `OnLoggedIn` / `OnLoginFailed` cho game có server. Kiểm `CanIUse.Login` trước vì
  TTSDK 1.1.5 chỉ hiện toast và không gọi callback khi không hỗ trợ.
- `TikTokBootstrap`: ô **Silent Login** (mặc định bật) gọi `TikTokLogin.Login()` khi SDK sẵn sàng.
- Validate Project: thêm mục test máy thật cho Silent Login.
- `TikTokShortcut.HasAdded`: ghi nhớ đã thêm shortcut (Add thành công hoặc từng mở game từ icon). Popup Shortcut lần
  sau hiện nội dung "đã tạo shortcut" với nút OK (`AddedTitle` / `AddedDescription` / `AddedClaimedDescription` /
  `AddedAction` sửa trong Inspector). Add thành công thì đóng popup.
- `TikTokUIPrefabs`: khi SDK là submodule, tự tạo Prefab Variant của 5 prefab UI trong `Assets/AmobearTTMNGConfig/UI`
  để game tùy chỉnh mà vẫn nhận cập nhật từ SDK; khóa prefab gốc trong submodule (không sửa / không lưu được).
  Menu `AmobearTTMNG/Create Game UI Prefabs`. Setup Feature UI dùng variant của game nếu có.
- `TikTokCapsuleGuide` (`Assets/Games/Scripts/TikTok`): vẽ lên Game view capsule `••• ✕` của TikTok
  kèm vùng cấm đỏ để đặt HUD góc trên-phải không bị che. Số đo lấy từ Samsung A52; dùng `Screen.safeArea` khi bật
  Device Simulator. Bật / tắt ở menu `Tools/TT MiniGame/Show TikTok Capsule Guide`. Chỉ có trong Editor (cả file nằm
  trong `#if UNITY_EDITOR`), không vào bản build. Sau domain reload không sinh thêm overlay trùng — `Ensure()` tìm lại
  overlay có sẵn thay vì dựa vào biến static.

### Changed
- `TikTokShare`: mặc định mỗi lần share một quà — Claim xong mới share tiếp (`ClaimPeriod` = mỗi lần share một kỳ).
  Muốn một lần/ngày như cũ thì gán `ClaimPeriod = () => DateTime.UtcNow.ToString("yyyyMMdd")`.
- `TikTokNetwork`: `HookListeners` ngoài WebGL bỏ qua khi đã hook.
- Prefab `TikTokFeatureCanvas` / `TikTokFeatureUI` / `Shortcut` / `Sidebar` / `Share` theo layout scene Demo: ba nút
  vuông 160×160 xếp dọc bên phải, cách nhau 40, chữ xuống dòng + tự co 18–72. Sửa tại chỗ nên instance đã có trong game
  vẫn giữ override. Layout gom về `Editor/TikTokFeatureLayout` dùng chung cho Setup Feature UI và công cụ sinh prefab.
- Ba nút neo góc trên-phải; nhóm `Buttons` gắn `SafeAreaFitter` để không bị nút capsule của TikTok che trên màn hình
  dài (Setup Feature UI chỉ gắn khi nhóm nằm trực tiếp dưới Canvas). Scene Demo đổi theo.
- `Setup Project` giữ lựa chọn Bulk Memory có sẵn trong StarkBuilderSetting (chỉ bật mặc định khi tạo mới) và đồng bộ
  cờ `-mbulk-memory` trong emscriptenArgs / IL2CPP args — TTSDK 1.1.5 không gỡ cờ emscripten khi tắt.
- `Validate Project` cảnh báo khi cờ bulk memory lệch với lựa chọn; `Validate Last Build` hướng dẫn tắt bulk memory khi
  `wasmFuncCount` ≥ 80000 cần tách Wasm.

## [1.1.0] — 2026-10-29

Dùng repo TTMNGDoc làm git submodule trong game (`tools/add-ttmng.ps1` / `.sh`).

### Added
- TTSDK 1.1.5-Release đi kèm package (`TTSDK~/`). Assembly `AmobearTTMNG.Installer` (không phụ thuộc TTSDK) hỏi import
  khi project chưa có SDK, menu `AmobearTTMNG/Install TikTok SDK`, `InstallFromCommandLine` cho batch mode.
- Sample **Demo** (`Samples~/Demo`): scene Setup / Demo / MainMenu, menu `AmobearTTMNG/Demo/Add Demo Scenes to Build`.
- Dependency `com.unity.textmeshpro` (Sample dùng TMP).

### Changed
- `AmobearTTMNG.Runtime` / `AmobearTTMNG.Editor` chỉ biên dịch khi có define `AMOBEAR_TTMNG_TTSDK` (bộ cài tự bật / gỡ)
  — game chưa có TTSDK không còn lỗi đỏ.
- Bỏ `DemoSetup` (ghi đè companyName / productName / bundle id của game); WebGL / IL2CPP do `Setup Project` lo.

## [1.0.0] — 2026-10-29

Bản đầu dưới dạng UPM package (trước đây copy thư mục `Assets/AmobearTTMNG`). Kiểm chứng với TTSDK 1.1.5-Release,
Unity 2022.3.62f2.

### Added
- Package `com.amobear.ttmng`, assembly `AmobearTTMNG.Runtime`, `AmobearTTMNG.Editor`.
- Menu `AmobearTTMNG/Setup Project`: WebGL + IL2CPP, StarkBuilderSetting, tạo `TikTokAdsConfig` trong Assets của game,
  tạo scene 0 có `TikTokBootstrap` khi thiếu.
- Menu `AmobearTTMNG/Set App ID`, `Validate Project`, `Build and Validate`, `Validate Last Build`, `Open Output Folder`,
  `ttmg dev (QR preview)`, `ttmg upload`.
- `TikTokBootstrap.WhenSdkReady` và `IsInitTimedOut`.

### Fixed
- InitSDK trả thành công sau timeout 8s: trước đây mọi module nhận "chưa sẵn sàng" và không thử lại cả phiên (mất quà
  Shortcut / Sidebar của phiên vào từ icon / sidebar, không preload ads, không gắn listener mạng, nút Shortcut / Sidebar / Share bị ẩn). Giờ các việc đó chạy lại khi SDK sẵn sàng muộn.

### Changed
- `TikTokAdsConfig.asset` không còn nằm trong module — mỗi game giữ ở `Assets/TikTokConfig/Resources/`.

### Removed
- `TikTokHS` và HS SDK (login / IAP / cloud save). Template chỉ kiếm tiền bằng quảng cáo (IAA); game cần IAP phải tự
  làm luồng server của TikTok (tạo order + webhook).
