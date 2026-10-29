<#
.SYNOPSIS
  Thêm template TikTok Mini Game (repo TTMNGDoc) vào một game Unity dưới dạng git submodule.

.DESCRIPTION
  1. git submodule add --depth 1 <Url> <gốc repo game>/<Name>, checkout tag (mặc định tag ttmng-v* mới nhất).
  2. Thêm "com.amobear.ttmng": "file:<đường dẫn tương đối>" vào Packages/manifest.json của game.
  Sau đó mở Unity: chọn Import khi được hỏi TikTok SDK, rồi chạy AmobearTTMNG/Setup Project.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File add-ttmng.ps1 -GameProject D:\Games\MyGame
  powershell -ExecutionPolicy Bypass -File add-ttmng.ps1 -GameProject D:\Games\MyGame -Ref master
#>
param(
    [Parameter(Mandatory = $true)][string]$GameProject,
    [string]$Ref = "",
    [string]$Url = "https://svn.as.local/tuanvhh/ttmngdoc.git",
    [string]$Name = "TTMNG"
)

$ErrorActionPreference = "Stop"
$PackageSubPath = "TikTokMiniGame/TikTokMiniGame/Packages/com.amobear.ttmng"

function Invoke-Git {
    & git @args
    if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed ($LASTEXITCODE)" }
}

$project = (Resolve-Path $GameProject).Path
$manifest = Join-Path $project "Packages/manifest.json"
if (-not (Test-Path $manifest)) { throw "Không thấy $manifest — GameProject phải là thư mục project Unity (có Assets/, Packages/)." }

$gitRoot = (& git -C $project rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) { throw "$project chưa nằm trong git repo — chạy git init trước." }
$gitRoot = (Resolve-Path $gitRoot).Path
$submodule = Join-Path $gitRoot $Name

if (-not $Ref) {
    # Tag ttmng-v* mới nhất trên remote (so theo version), không có thì dùng master.
    $tags = & git ls-remote --tags --refs $Url "ttmng-v*" | ForEach-Object { ($_ -split "refs/tags/")[1] }
    $Ref = $tags | Sort-Object { [version]($_ -replace "^ttmng-v", "") } | Select-Object -Last 1
    if (-not $Ref) { $Ref = "master" }
}
Write-Host "Template: $Url @ $Ref -> $submodule"

Push-Location $gitRoot
try {
    if (Test-Path (Join-Path $submodule ".git")) {
        Write-Host "Submodule $Name đã có — chỉ chuyển sang $Ref."
    } else {
        Invoke-Git submodule add --depth 1 $Url $Name
        # Không kéo cả lịch sử (~100MB) khi clone game: git clone --recurse-submodules sẽ clone nông.
        Invoke-Git config -f .gitmodules "submodule.$Name.shallow" true
    }
    Invoke-Git -C $Name fetch --depth 1 origin $Ref
    Invoke-Git -C $Name checkout --quiet FETCH_HEAD
    Invoke-Git add .gitmodules $Name
} finally {
    Pop-Location
}

# Đường dẫn file: tính từ thư mục Packages của game (UPM đọc tương đối từ đó).
$packageDir = Join-Path $submodule $PackageSubPath
if (-not (Test-Path (Join-Path $packageDir "package.json"))) { throw "Ref $Ref không có $PackageSubPath/package.json." }
$from = New-Object System.Uri((Join-Path $project "Packages") + [IO.Path]::DirectorySeparatorChar)
$relative = [System.Uri]::UnescapeDataString($from.MakeRelativeUri((New-Object System.Uri($packageDir))).ToString())
$entry = "`"com.amobear.ttmng`": `"file:$relative`""

# Sửa bằng chuỗi để giữ nguyên format manifest.json.
$text = [IO.File]::ReadAllText($manifest)
if ($text -match '"com\.amobear\.ttmng"\s*:\s*"[^"]*"') {
    $text = [regex]::Replace($text, '"com\.amobear\.ttmng"\s*:\s*"[^"]*"', $entry.Replace('$', '$$'))
} else {
    $text = [regex]::Replace($text, '("dependencies"\s*:\s*\{)(\r?\n)(\s*)', ('$1$2$3' + $entry.Replace('$', '$$') + ',$2$3'), 1)
}
[IO.File]::WriteAllText($manifest, $text, (New-Object System.Text.UTF8Encoding($false)))

Write-Host ""
Write-Host "Xong. manifest.json: $entry"
Write-Host "Tiếp theo:"
Write-Host "  1. Mở project trong Unity 2022.3 -> chọn Import khi được hỏi TikTok SDK 1.1.5."
Write-Host "  2. Menu AmobearTTMNG/Setup Project -> Validate Project -> Build and Validate."
Write-Host "  3. Commit .gitmodules, $Name và Packages/manifest.json của game."
