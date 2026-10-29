# Changelog

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
