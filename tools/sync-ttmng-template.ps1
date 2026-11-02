<# Mirror Assets/AmobearTTMNG into the template repo (sdk-assets branch) that games use as a submodule.
   Commits the SDK folder as of the dev repo's HEAD, reusing the latest SDK commit message. Skips when nothing changed.
   Usage: tools/sync-ttmng-template.ps1 [-NoPush] [-Remote <url>] [-Branch sdk-assets] #>
param(
    [string]$Remote = 'https://tuanvhh@svn.as.local/tuanvhh/ttmng_template.git',
    [string]$Branch = 'sdk-assets',
    [string]$Cache = (Join-Path $PSScriptRoot '../.artifacts/ttmng-template'),
    [switch]$NoPush
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sdkPath = 'TikTokMiniGame/TikTokMiniGame/Assets/AmobearTTMNG'
$source = Join-Path $repo $sdkPath
$cache = [IO.Path]::GetFullPath($Cache)
# Same content as export-ttmng-sdk.ps1: DevTools / Resources / TextMesh Pro stay in the dev repo only.
$items = @('Runtime','Editor','UI','TTSDK~','Demo','Runtime.meta','Editor.meta','UI.meta','Demo.meta',
    'README.md','README.md.meta','CHANGELOG.md','CHANGELOG.md.meta')
$gitExecutable = (Get-Command git -CommandType Application).Source
function Git([string]$dir) { $out = & $gitExecutable -C $dir @args; if ($LASTEXITCODE -ne 0) { throw "git failed: $args" }; $out }

# Only publish committed work, so the template always matches a real dev commit.
if (Git $repo status --porcelain -- $sdkPath) { throw "Uncommitted changes in $sdkPath. Commit them first." }
if (Test-Path -LiteralPath (Join-Path $source 'Editor/StarkBuilderSetting.asset')) { throw 'Move game-specific settings out of SDK first.' }

if (Test-Path -LiteralPath (Join-Path $cache '.git')) {
    Git $cache remote set-url origin $Remote | Out-Null
    Git $cache fetch -q origin $Branch | Out-Null
    Git $cache checkout -q -B $Branch "origin/$Branch" | Out-Null
    Git $cache reset -q --hard "origin/$Branch" | Out-Null
} else {
    Git $repo clone -q --branch $Branch $Remote $cache | Out-Null
}

Get-ChildItem -LiteralPath $cache -Force | Where-Object { $_.Name -notin @('.git','.gitignore') } |
    Remove-Item -Recurse -Force
foreach ($name in $items) {
    $path = Join-Path $source $name
    if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $cache $name) -Recurse }
}

Git $cache add --all | Out-Null
if (!(Git $cache status --porcelain)) { Write-Host "Template already in sync ($Branch)."; return }

$sourceCommit = (Git $repo log -1 --format=%h -- $sdkPath)
$message = (Git $repo log -1 --format=%B -- $sdkPath) -join "`n"
$messageFile = Join-Path $cache '../ttmng-template-msg.txt'
[IO.File]::WriteAllText($messageFile, $message.TrimEnd() + "`n`nSynced-From: TTMNGDoc@$sourceCommit`n")
Git $cache commit -q -F $messageFile | Out-Null
Remove-Item -LiteralPath $messageFile
Git $cache --no-pager show --stat --oneline HEAD | Write-Host

if ($NoPush) { Write-Host "Committed locally in $cache (not pushed)."; return }
Git $cache push -q origin $Branch | Out-Null
Write-Host "Pushed to $Remote ($Branch). Games: git submodule update --remote <path>, then commit the pointer."
