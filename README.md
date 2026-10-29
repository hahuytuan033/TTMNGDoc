# I luv TikTok

Template đưa game Unity lên TikTok Mini Game (chỉ quảng cáo — IAA).

- Package: [`TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng`](TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng/README.md)
  — đọc README của package để biết cách dùng.
- Project dev / demo: `TikTokMiniGame/TikTokMiniGame` (Unity 2022.3.62f2).

## Thêm vào một game

Repo này được thêm vào game dưới dạng **git submodule** (thư mục `TTMNG/` ở gốc repo game):

```powershell
powershell -ExecutionPolicy Bypass -File tools\add-ttmng.ps1 -GameProject D:\Games\MyGame   # Windows
```
```bash
tools/add-ttmng.sh ~/Games/MyGame                                                          # macOS
```

Rồi mở game trong Unity → **Import** TikTok SDK khi được hỏi → `AmobearTTMNG/Setup Project`.

## Phát hành template

1. Sửa trong project dev, test máy thật.
2. `AmobearTTMNG/Dev/Sync Demo → Samples~` nếu có sửa `Assets/Demo`.
3. Nâng `version` trong `package.json` + `CHANGELOG.md`, commit lên `master`, gắn tag `ttmng-vX.Y.Z`, push cả tag.
