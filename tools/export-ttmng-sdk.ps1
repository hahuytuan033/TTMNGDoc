<# Export a standalone local SDK Git repo; does not commit the dev repo or push. #>
param([string]$Output = (Join-Path $PSScriptRoot '../.artifacts/AmobearTTMNG-SDK'))
$ErrorActionPreference = 'Stop'
$source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../TikTokMiniGame/TikTokMiniGame/Assets/AmobearTTMNG'))
$destination = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $destination) { throw "Output exists; use a new -Output: $destination" }
foreach ($name in @('Runtime','Editor','TTSDK~','Demo')) {
    if (!(Test-Path -LiteralPath (Join-Path $source $name))) { throw "Missing SDK source: $name" }
}
if (Test-Path -LiteralPath (Join-Path $source 'Editor/StarkBuilderSetting.asset')) { throw 'Move game-specific settings out of SDK first.' }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach ($name in @('Runtime','Editor','TTSDK~','Demo','Runtime.meta','Editor.meta','Demo.meta','README.md','README.md.meta','CHANGELOG.md','CHANGELOG.md.meta')) {
    $path = Join-Path $source $name
    if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination (Join-Path $destination $name) -Recurse }
}
[IO.File]::WriteAllText((Join-Path $destination '.gitignore'), "*.csproj`n*.sln`n.DS_Store`n")
$gitExecutable = (Get-Command git -CommandType Application).Source
function Git { & $gitExecutable -C $destination @args; if ($LASTEXITCODE -ne 0) { throw "git failed: $args" } }
Git init -b sdk-assets
Git add --all
Git -c user.name='Amobear SDK Export' -c user.email='sdk-export@localhost' commit -m 'Export Assets-based AmobearTTMNG SDK'
Write-Host "SDK repository ready: $destination"
Write-Host 'Publish this SDK repository (sdk-assets branch) before teammates add it as a submodule.'
