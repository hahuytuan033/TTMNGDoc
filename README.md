# TTMNGDoc — Template TikTok Mini Game cho Unity

Template đưa game Unity lên TikTok Mini Game, **chỉ kiếm tiền bằng quảng cáo (IAA)**.

- Package: [`TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng`](TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng/README.md)
  — module runtime (InitSDK, storage, ads, shortcut / sidebar, share, safe area…), menu `AmobearTTMNG/*`, TTSDK
  1.1.5-Release đi kèm, Sample Demo. Cách dùng API xem README của package.
- Project dev / demo: `TikTokMiniGame/TikTokMiniGame` (Unity 2022.3.62f2).

Game dùng template bằng cách **gắn repo này làm git submodule** (thư mục `TTMNG/` trong repo game) và trỏ Unity tới
package bên trong bằng `file:`. Mỗi game ghim một phiên bản (tag `ttmng-vX.Y.Z`), sửa template ngay trong game được.

---

## 1. Gắn template vào một game

**Cần có:** Unity 2022.3 (đã kiểm chứng 2022.3.62f2), repo game dùng git, quyền đọc repo
`https://svn.as.local/tuanvhh/ttmngdoc.git`, một bản clone TTMNGDoc bất kỳ trên máy (để lấy script).

### 1.1. Chạy script (khuyên dùng)

Windows (PowerShell):
```powershell
powershell -ExecutionPolicy Bypass -File C:\Developer\TTMNGDoc\tools\add-ttmng.ps1 -GameProject D:\Games\MyGame
```

macOS:
```bash
~/Developer/TTMNGDoc/tools/add-ttmng.sh ~/Games/MyGame
```

`-GameProject` / đối số đầu là **thư mục project Unity** của game (thư mục có `Assets/`, `Packages/`), không cần là
gốc repo. Script sẽ:

1. Tìm gốc repo git của game, thêm submodule **nông** (`--depth 1`, `shallow = true`) vào `<gốc repo>/TTMNG`.
2. Checkout tag `ttmng-v*` mới nhất (hoặc ref chỉ định).
3. Thêm vào `Packages/manifest.json` của game (đường dẫn tương đối tự tính):
   ```json
   "com.amobear.ttmng": "file:../TTMNG/TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng",
   ```
4. `git add` `.gitmodules` và `TTMNG`.

Tham số thêm:

| PowerShell | bash (đối số thứ) | Mặc định | Ý nghĩa |
|---|---|---|---|
| `-Ref ttmng-v1.1.0` | 2 | tag `ttmng-v*` mới nhất | Tag / nhánh muốn dùng (`master` = bản mới nhất chưa phát hành) |
| `-Url <url>` | 3 | `https://svn.as.local/tuanvhh/ttmngdoc.git` | Repo template |
| `-Name <thư mục>` | 4 | `TTMNG` | Tên thư mục submodule ở gốc repo game |

### 1.2. Làm tay (không dùng script)

Ở gốc repo game:
```bash
git submodule add --depth 1 https://svn.as.local/tuanvhh/ttmngdoc.git TTMNG
git config -f .gitmodules submodule.TTMNG.shallow true
git -C TTMNG fetch --depth 1 origin tag ttmng-v1.1.0
git -C TTMNG checkout ttmng-v1.1.0
```
Rồi thêm dòng `"com.amobear.ttmng": "file:…"` vào `dependencies` của `Packages/manifest.json`. Đường dẫn tính từ thư
mục `Packages/` của game tới `TTMNG/TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng`:

| Project Unity nằm ở | Giá trị |
|---|---|
| Gốc repo (`MyGame/Assets`) | `file:../TTMNG/TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng` |
| Thư mục con (`MyGame/Client/Assets`) | `file:../../TTMNG/TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng` |

> Để submodule **ngoài `Assets/`** — trong `TTMNG` có cả project Unity demo, đặt trong `Assets/` là Unity import nhầm.

### 1.3. Mở Unity

1. Mở project game bằng Unity 2022.3. Hộp thoại **"Project chưa có TikTok Unity SDK"** → **Import**. TTSDK
   1.1.5-Release được import vào `Assets/Plugins/com.tiktok.minigame`, define `AMOBEAR_TTMNG_TTSDK` tự bật, menu
   **`AmobearTTMNG`** xuất hiện. Lỡ bấm "Để sau" → menu **`AmobearTTMNG/Install TikTok SDK`**.
2. **`AmobearTTMNG/Setup Project`** — chuyển WebGL + IL2CPP, tạo StarkBuilderSetting, `Assets/TikTokConfig/…`, scene 0
   có `TikTokBootstrap`, hỏi **App ID** (Client Key trên TikTok Developer Portal).
3. Điền Placement ID quảng cáo vào `Assets/TikTokConfig/Resources/TikTokAdsConfig.asset`.
4. **`AmobearTTMNG/Validate Project`** → **`AmobearTTMNG/Build and Validate`** → **`AmobearTTMNG/ttmg dev (QR preview)`**.

Chi tiết từng bước, cách gọi quảng cáo / shortcut / sidebar: README của package.

> Chưa import TTSDK xong thì **đừng viết code gọi `AmobearTTMNG`** — lỗi biên dịch ở code game chặn bộ cài chạy.

### 1.4. Commit vào repo game

```bash
git add .gitmodules TTMNG Packages/manifest.json Packages/packages-lock.json \
        Assets/Plugins/com.tiktok.minigame Assets/Plugins/com.tiktok.minigame.meta \
        Assets/TikTokConfig Assets/Editor/StarkBuilderSetting.asset ProjectSettings
git commit -m "Add TikTok Mini Game template (TTMNG)"
```
TTSDK trong `Assets/Plugins` thuộc về game — **phải commit**, nếu không người khác clone về sẽ thiếu SDK.

---

## 2. Làm việc hằng ngày với submodule

### Clone game đã có template
```bash
git clone --recurse-submodules <url-repo-game>
# đã clone rồi mà TTMNG trống:
git submodule update --init
```

### Nâng phiên bản template
Đọc `CHANGELOG.md` của package trước, rồi ở gốc repo game:
```bash
git -C TTMNG fetch --depth 1 origin tag ttmng-v1.2.0
git -C TTMNG checkout ttmng-v1.2.0
git add TTMNG && git commit -m "Update TTMNG template to 1.2.0"
```
(Hoặc chạy lại script với `-Ref ttmng-v1.2.0`.) Mở Unity, chạy lại `AmobearTTMNG/Validate Project`.

### Sửa template ngay trong game
Package `file:` sửa được trực tiếp trong Unity. Submodule mặc định ở trạng thái detached HEAD, nên:
```bash
git -C TTMNG checkout master
git -C TTMNG pull
# … sửa trong Unity …
git -C TTMNG add -A && git -C TTMNG commit -m "…" && git -C TTMNG push
git add TTMNG && git commit -m "Bump TTMNG"      # ghim commit mới trong repo game
```
Clone nông vẫn push được; cần lịch sử (blame, rebase) thì `git -C TTMNG fetch --unshallow`.

### Dùng Demo
Package Manager → **Amobear TikTok Mini Game** → **Samples** → **Demo** → Import, rồi
**`AmobearTTMNG/Demo/Add Demo Scenes to Build`**, kéo Setup của demo lên index 0 để test trên máy thật. Xoá Sample và
gỡ scene demo khỏi Build Settings trước khi build bản thật.

### Gỡ template khỏi game
```bash
git submodule deinit -f TTMNG
git rm -f TTMNG
rm -rf .git/modules/TTMNG
```
Rồi xoá dòng `com.amobear.ttmng` trong `Packages/manifest.json`.

---

## 3. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| Unity báo `Package com.amobear.ttmng … cannot be found` | Thư mục `TTMNG` trống (clone thiếu `--recurse-submodules`) → `git submodule update --init`; hoặc đường dẫn `file:` sai số `../`. |
| Không thấy menu `AmobearTTMNG` | TTSDK chưa import → menu `AmobearTTMNG/Install TikTok SDK` (menu này luôn có). Nếu cũng không có: xem Console có lỗi biên dịch ở code game không. |
| Lỗi `The type or namespace name 'TTSDK' could not be found` trong `AmobearTTMNG.*` | Define `AMOBEAR_TTMNG_TTSDK` còn nhưng TTSDK bị xoá ngoài Unity → import lại TTSDK, hoặc gỡ define trong Player Settings → Scripting Define Symbols. |
| Chữ trong scene Demo không hiện | Chưa có TMP Essentials → Window → TextMeshPro → Import TMP Essential Resources. |
| `git submodule add` hỏi mật khẩu / 401 | Cần quyền đọc repo TTMNGDoc trên GitLab (`svn.as.local`); đăng nhập git credential trước. |
| `fatal: transport 'file' not allowed` (test bằng repo local) | Thêm `-c protocol.file.allow=always` cho lệnh git — chỉ khi `-Url` là đường dẫn trên máy. |
| Sửa trong `TTMNG` rồi `git submodule update` làm mất thay đổi | Luôn `git -C TTMNG checkout master` trước khi sửa, commit + push trong `TTMNG` trước khi update. |

---

## 4. Phát hành phiên bản template (người bảo trì)

1. Sửa và test trong project dev `TikTokMiniGame/TikTokMiniGame` (máy thật nếu đụng runtime).
2. Có sửa `Assets/Demo` → **`AmobearTTMNG/Dev/Sync Demo → Samples~`**.
3. Nâng `version` trong `package.json`, ghi `CHANGELOG.md`, commit lên `master`.
4. `git tag -a ttmng-vX.Y.Z -m "…" && git push gitlab master ttmng-vX.Y.Z` — script `add-ttmng` lấy tag mới nhất này.
