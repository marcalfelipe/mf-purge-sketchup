param(
    [Parameter(Mandatory = $true)]
    [string]$SketchUpRuntimeDirectory,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$runtimeSource = (Resolve-Path -LiteralPath $SketchUpRuntimeDirectory).Path
$version = (Get-Content -LiteralPath (Join-Path $projectRoot "VERSION") -Raw).Trim()

$requiredDlls = @(
    "SketchUpAPI.dll",
    "SketchUpCommonPreferences.dll",
    "MSVCP140.dll",
    "MSVCP140_CODECVT_IDS.dll",
    "VCRUNTIME140.dll",
    "VCRUNTIME140_1.dll"
)

foreach ($dll in $requiredDlls) {
    if (-not (Test-Path -LiteralPath (Join-Path $runtimeSource $dll))) {
        throw "Required runtime file not found: $dll"
    }
}

$python = Get-Command python -ErrorAction Stop
$tools = Join-Path $projectRoot ".build-tools"
$workerDist = Join-Path $projectRoot "worker_onedir_console"
$workerWork = Join-Path $projectRoot ".pyinstaller-build-onedir-console"
$runtimeStage = Join-Path $projectRoot ".runtime-stage"
$runtimeZip = Join-Path $projectRoot "embedded_runtime.zip"
$publishDir = Join-Path $projectRoot "dist\publish"
$releaseDir = Join-Path $projectRoot "dist\release"

& $python.Source -m pip install --disable-pip-version-check --target $tools "pyinstaller==6.22.3"
$env:PYTHONPATH = $tools
& $python.Source -m PyInstaller `
    (Join-Path $projectRoot ".pyinstaller-spec\MFPurgeWorkerConsole.spec") `
    --distpath $workerDist --workpath $workerWork --noconfirm

if (Test-Path -LiteralPath $runtimeStage) {
    Remove-Item -LiteralPath $runtimeStage -Recurse -Force
}
New-Item -ItemType Directory -Path $runtimeStage | Out-Null
Copy-Item -Path (Join-Path $workerDist "MFPurgeWorkerConsole\*") -Destination $runtimeStage -Recurse
foreach ($dll in $requiredDlls) {
    Copy-Item -LiteralPath (Join-Path $runtimeSource $dll) -Destination $runtimeStage
}

if (Test-Path -LiteralPath $runtimeZip) {
    Remove-Item -LiteralPath $runtimeZip -Force
}
Compress-Archive -Path (Join-Path $runtimeStage "*") -DestinationPath $runtimeZip -CompressionLevel Optimal

& dotnet restore (Join-Path $projectRoot "MFPurgeSketchUp.csproj") -r win-x64
& dotnet publish (Join-Path $projectRoot "MFPurgeSketchUp.csproj") `
    -c $Configuration -r win-x64 --self-contained true --no-restore `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=embedded `
    -o $publishDir

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
$exeName = "MF-Purge-SketchUp-v$version-Windows-x64.exe"
$exePath = Join-Path $releaseDir $exeName
Copy-Item -LiteralPath (Join-Path $publishDir "MF Purge SketchUp.exe") -Destination $exePath -Force

$hash = Get-FileHash -LiteralPath $exePath -Algorithm SHA256
Set-Content -LiteralPath (Join-Path $releaseDir "SHA256SUMS.txt") `
    -Value "$($hash.Hash)  $exeName" -Encoding ascii

Write-Host "Release created: $exePath"
Write-Host "SHA-256: $($hash.Hash)"
