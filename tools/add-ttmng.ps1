<# Run with Unity closed. Url must reference the exported SDK-only repository. #>
param(
    [string]$GameProject = (Get-Location).Path,
    [Parameter(Mandatory = $true)][string]$Url,
    [string]$Ref = 'sdk-assets'
)
$ErrorActionPreference = 'Stop'
$gitExecutable = (Get-Command git -CommandType Application).Source
function Git { & $gitExecutable @args; if ($LASTEXITCODE -ne 0) { throw "git failed: $args" } }
$project = (Resolve-Path -LiteralPath $GameProject).Path
if (!(Test-Path -LiteralPath (Join-Path $project 'Assets') -PathType Container)) {
    throw 'Run this command from the Unity project folder containing Assets, or specify -GameProject.'
}
$manifestPath = Join-Path $project 'Packages/manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.dependencies.PSObject.Properties['com.amobear.ttmng'] -or (Test-Path -LiteralPath (Join-Path $project 'Packages/com.amobear.ttmng'))) {
    throw 'Remove the old com.amobear.ttmng UPM package first to avoid duplicate assemblies.'
}
$target = Join-Path $project 'Assets/AmobearTTMNG'
if (Test-Path -LiteralPath $target) { throw "Destination exists: $target. Update existing submodules with git instead." }
Git -C $project rev-parse --show-toplevel
$preview = Join-Path ([IO.Path]::GetTempPath()) ('ttmng-preview-' + [guid]::NewGuid().ToString('N'))
try {
    Git clone --depth 1 --branch $Ref -- $Url $preview
    foreach ($name in @('Runtime/AmobearTTMNG.Runtime.asmdef','Editor/Installer/TikTokSdkInstaller.cs','TTSDK~')) {
        if (!(Test-Path -LiteralPath (Join-Path $preview $name))) { throw "Not an SDK-only repository: missing $name" }
    }
    Git -C $project submodule add -b $Ref -- $Url Assets/AmobearTTMNG
    if (!(Test-Path -LiteralPath (Join-Path $target '.git'))) {
        throw "Submodule was not created at the required destination: $target"
    }
} finally {
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ([IO.Path]::GetFullPath($preview).StartsWith($tempRoot) -and (Test-Path -LiteralPath $preview)) {
        Remove-Item -LiteralPath $preview -Recurse -Force
    }
}
foreach ($entry in @(@('com.unity.ugui','1.0.0'), @('com.unity.textmeshpro','3.0.6'))) {
    if (!$manifest.dependencies.PSObject.Properties[$entry[0]]) {
        $manifest.dependencies | Add-Member -NotePropertyName $entry[0] -NotePropertyValue $entry[1]
    }
}
$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 50) + "`n", $utf8)
Write-Host 'Ready: Assets/AmobearTTMNG. Open Unity, import TikTok SDK, then AmobearTTMNG/Setup Project.'
Write-Host "Installed at: $target"
Write-Host 'Commit .gitmodules, submodule pointer, Assets/AmobearTTMNG.meta, Packages and generated game settings.'
