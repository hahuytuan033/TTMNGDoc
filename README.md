# TTMNGDoc — AmobearTTMNG

Project phát triển: `TikTokMiniGame/TikTokMiniGame` (Unity 2022.3.62f2).
Mã nguồn SDK: [Assets/AmobearTTMNG](TikTokMiniGame/TikTokMiniGame/Assets/AmobearTTMNG/README.md).

## Phân phối SDK làm submodule trong Assets

### Lệnh chạy từ project game nhận SDK

Mở terminal tại thư mục có `Assets` và `Packages`, rồi chạy:

```powershell
powershell -NoProfile -File C:/Developer/TTMNGDoc/tools/add-ttmng.ps1 -Url <SDK_REPOSITORY_URL>
```

Script mặc định dùng project đang đứng, luôn gắn vào `Assets/AmobearTTMNG` và kiểm tra đích sau khi thêm.
Hoặc dùng Git trực tiếp:

```bash
git submodule add -b sdk-assets <SDK_REPOSITORY_URL> Assets/AmobearTTMNG
```

Phải giữ đối số cuối `Assets/AmobearTTMNG`. Lệnh `git submodule add <URL>` không có đích sẽ tạo thư mục theo tên repo
ở vị trí hiện tại; repo SDK không thể tự thay đổi hành vi này của Git.

### Chuẩn bị repo SDK để phân phối

Repo này chứa tài liệu và Unity project nên **không gắn nguyên nhánh phát triển vào Assets của game**.
Export thư mục SDK thành một repo Git độc lập bằng:

```powershell
powershell -NoProfile -File tools/export-ttmng-sdk.ps1
```

Kết quả mặc định: `.artifacts/AmobearTTMNG-SDK`, nhánh `sdk-assets`, có commit snapshot riêng.
Lệnh không commit repo phát triển và không push remote. Output phải chưa tồn tại; dùng `-Output` để xuất snapshot tiếp theo.
Không export cấu hình game, TMP Essentials, DevTools hay tài liệu/project lồng nhau.

Đẩy repo SDK này lên remote SDK riêng, rồi cài vào game:

```powershell
powershell -NoProfile -File tools/add-ttmng.ps1 -GameProject D:/Games/MyGame -Url <SDK_REPOSITORY_URL>
```

macOS/Linux: `tools/add-ttmng.sh <game-path> <SDK-url> [branch]` (cần PowerShell 7), hoặc dùng lệnh git thủ công trong README SDK.
SDK sẽ là submodule thật tại `Assets/AmobearTTMNG`; Unity đọc trực tiếp code tại đó.
Script từ chối project còn UPM package cũ để tránh trùng assembly và giữ nguyên các phiên bản dependency đã có.

## Phát hành tiếp theo

Sửa/test trong project dev. Export sang một thư mục mới, kiểm tra diff rồi đưa thay đổi vào clone repo SDK hiện có,
giữ nguyên `.git` của clone đó để tiếp tục lịch sử. Commit/push repo SDK và cập nhật con trỏ submodule ở từng game.
Không thay remote của repo phát triển và không force-push snapshot mới lên lịch sử SDK cũ.

## Ngoại lệ đường dẫn TikTok

TTSDK vendor được import vào `Assets/Plugins/com.tiktok.minigame`; builder setting ở `Assets/Editor/StarkBuilderSetting.asset`.
Cấu hình riêng của game được tạo tại `Assets/AmobearTTMNGConfig`, không nằm trong submodule dùng chung.
