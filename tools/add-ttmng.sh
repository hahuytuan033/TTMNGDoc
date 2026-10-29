#!/usr/bin/env bash
# Thêm template TikTok Mini Game (repo TTMNGDoc) vào một game Unity dưới dạng git submodule.
#   1. git submodule add --depth 1 <url> <gốc repo game>/<name>, checkout tag (mặc định tag ttmng-v* mới nhất).
#   2. Thêm "com.amobear.ttmng": "file:<đường dẫn tương đối>" vào Packages/manifest.json của game.
# Sau đó mở Unity: chọn Import khi được hỏi TikTok SDK, rồi chạy AmobearTTMNG/Setup Project.
#
# Dùng: tools/add-ttmng.sh <game-unity-project> [ref] [url] [name]
#   vd. tools/add-ttmng.sh ~/Games/MyGame
#       tools/add-ttmng.sh ~/Games/MyGame master
set -euo pipefail

GAME_PROJECT="${1:?Dùng: add-ttmng.sh <game-unity-project> [ref] [url] [name]}"
REF="${2:-}"
URL="${3:-https://svn.as.local/tuanvhh/ttmngdoc.git}"
NAME="${4:-TTMNG}"
PACKAGE_SUBPATH="TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng"

PROJECT="$(cd "$GAME_PROJECT" && pwd -P)"
MANIFEST="$PROJECT/Packages/manifest.json"
[ -f "$MANIFEST" ] || { echo "Không thấy $MANIFEST — tham số phải là thư mục project Unity (có Assets/, Packages/)." >&2; exit 1; }

GIT_ROOT="$(git -C "$PROJECT" rev-parse --show-toplevel)" || { echo "$PROJECT chưa nằm trong git repo — chạy git init trước." >&2; exit 1; }
GIT_ROOT="$(cd "$GIT_ROOT" && pwd -P)"
SUBMODULE="$GIT_ROOT/$NAME"

if [ -z "$REF" ]; then
  # Tag ttmng-v* mới nhất trên remote (so theo version), không có thì dùng master.
  REF="$(git ls-remote --tags --refs "$URL" 'ttmng-v*' | sed 's#.*refs/tags/##' | sort -V | tail -n 1)"
  REF="${REF:-master}"
fi
echo "Template: $URL @ $REF -> $SUBMODULE"

cd "$GIT_ROOT"
if [ -e "$SUBMODULE/.git" ]; then
  echo "Submodule $NAME đã có — chỉ chuyển sang $REF."
else
  git submodule add --depth 1 "$URL" "$NAME"
  # Không kéo cả lịch sử (~100MB) khi clone game: git clone --recurse-submodules sẽ clone nông.
  git config -f .gitmodules "submodule.$NAME.shallow" true
fi
git -C "$NAME" fetch --depth 1 origin "$REF"
git -C "$NAME" checkout --quiet FETCH_HEAD
git add .gitmodules "$NAME"

PACKAGE_DIR="$SUBMODULE/$PACKAGE_SUBPATH"
[ -f "$PACKAGE_DIR/package.json" ] || { echo "Ref $REF không có $PACKAGE_SUBPATH/package.json." >&2; exit 1; }

# Đường dẫn tương đối từ thư mục Packages của game (macOS không có realpath --relative-to).
relative_path() {
  local from="$1" to="$2" up=""
  while [ "${to#"$from"/}" = "$to" ] && [ "$to" != "$from" ]; do
    from="$(dirname "$from")"
    up="../$up"
  done
  echo "${up}${to#"$from"/}"
}
RELATIVE="$(relative_path "$PROJECT/Packages" "$PACKAGE_DIR")"
ENTRY="\"com.amobear.ttmng\": \"file:$RELATIVE\""

# Sửa bằng chuỗi để giữ nguyên format manifest.json.
if grep -q '"com\.amobear\.ttmng"' "$MANIFEST"; then
  perl -0pi -e 's#"com\.amobear\.ttmng"\s*:\s*"[^"]*"#'"$ENTRY"'#' "$MANIFEST"
else
  perl -0pi -e 's#("dependencies"\s*:\s*\{)(\r?\n)(\s*)#$1$2$3'"$ENTRY"',$2$3#' "$MANIFEST"
fi

echo
echo "Xong. manifest.json: $ENTRY"
echo "Tiếp theo:"
echo "  1. Mở project trong Unity 2022.3 -> chọn Import khi được hỏi TikTok SDK 1.1.5."
echo "  2. Menu AmobearTTMNG/Setup Project -> Validate Project -> Build and Validate."
echo "  3. Commit .gitmodules, $NAME và Packages/manifest.json của game."
