# Publishes LexFlow as a folder of loose files for Velopack packaging.
#
# Velopack needs individual files on disk (not a bundled single-file exe) so it
# can diff files between versions and build delta packages. Do not add
# PublishSingleFile here — use publish-portable.ps1 for a one-file dev build.
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $OutputDir) {
    $OutputDir = Join-Path $root "publish"
}

$project = Join-Path $root "LexFlow.Settings\LexFlow.Settings.csproj"

Write-Host "Publishing LexFlow ($Configuration, $Runtime) to $OutputDir"

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:DebugType=none `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Publish output: $OutputDir"
return $OutputDir
