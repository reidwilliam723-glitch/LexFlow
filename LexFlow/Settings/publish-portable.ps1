# DEV ONLY: builds a portable single-file LexFlow.exe into ..\Run.
#
# This is for quick local runs without going through the installer. It is NOT
# what testers get any more — they install from Setup.exe produced by
# release.ps1. A portable build reports itself as "not installed" to Velopack,
# so it will never self-update.
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root "Run"

Write-Host "Publishing LexFlow (portable, single file) to $output"

if (Test-Path $output) {
    Get-ChildItem $output -File | Where-Object { $_.Name -ne "Read me first.txt" } | Remove-Item -Force
}

New-Item -ItemType Directory -Path $output -Force | Out-Null

dotnet publish (Join-Path $root "LexFlow.Settings\LexFlow.Settings.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $output

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed."
    exit 1
}

$published = Join-Path $output "LexFlow.Settings.exe"
$renamed = Join-Path $output "LexFlow.exe"
if (Test-Path $published) {
    if (Test-Path $renamed) { Remove-Item $renamed -Force }
    Move-Item $published $renamed -Force
}

$stamp = Get-Date -Format "yyyy-MM-dd HH:mm"
@"
LexFlow portable dev build
Built: $stamp
Portable builds do not auto-update. Use the installer build for tester releases.
"@ | Set-Content -Path (Join-Path $output "BUILD.txt") -Encoding UTF8

$readme = Join-Path $output "Read me first.txt"
@"
Double-click LexFlow.exe to start.

This is a portable developer build and will not update itself.
You do not need to install .NET or Visual Studio.
"@ | Set-Content -Path $readme -Encoding UTF8

Write-Host "Done. Portable build: $output\LexFlow.exe"
Get-ChildItem $output | ForEach-Object { Write-Host ("  {0}" -f $_.Name) }
