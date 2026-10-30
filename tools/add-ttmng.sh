#!/usr/bin/env bash
# Uses PowerShell 7 on macOS/Linux so both platforms share the same validation and manifest handling.
set -euo pipefail
command -v pwsh >/dev/null || { echo 'Install PowerShell 7 (pwsh), or follow the manual submodule instructions in README.md.' >&2; exit 1; }
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
exec pwsh -NoProfile -File "$SCRIPT_DIR/add-ttmng.ps1" -GameProject "${1:?Unity project path required}" -Url "${2:?SDK-only repository URL required}" -Ref "${3:-sdk-assets}"
