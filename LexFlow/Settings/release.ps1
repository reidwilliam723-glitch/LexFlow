# Builds, packs, and uploads a LexFlow release with Velopack.
#
# One-time setup:
#   dotnet tool install -g vpk
#   $env:GITHUB_TOKEN = "<a token with write access to the releases repo>"
#
# Usage:
#   .\release.ps1                 # pack only, review .\releases first
#   .\release.ps1 -Upload         # pack, then publish the GitHub release
#
# The version comes from <Version> in LexFlow.Settings\LexFlow.Settings.csproj.
# Bump it there and nowhere else.
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$RepoUrl = "https://github.com/reidwilliam723-glitch/LexFlow",
    [switch]$Upload,
    [switch]$Prerelease
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "LexFlow.Settings\LexFlow.Settings.csproj"
$publishDir = Join-Path $root "publish"
$releaseDir = Join-Path $root "releases"

function Get-LexFlowVersion {
    param([string]$ProjectPath)

    $xml = [xml](Get-Content -Path $ProjectPath -Raw)
    $version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)
    if (-not $version) {
        throw "No <Version> element found in $ProjectPath. Add one, e.g. <Version>1.0.0</Version>."
    }

    return ([string]$version).Trim()
}

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    throw "The 'vpk' CLI was not found. Install it with: dotnet tool install -g vpk"
}

$version = Get-LexFlowVersion -ProjectPath $project
Write-Host "LexFlow release version $version (from LexFlow.Settings.csproj)"

& (Join-Path $PSScriptRoot "publish.ps1") -Configuration $Configuration -Runtime $Runtime -OutputDir $publishDir | Out-Null

Write-Host "Packing with vpk"
$packArgs = @(
    "pack",
    "--packId", "LexFlow",
    "--packVersion", $version,
    "--packTitle", "LexFlow",
    "--packAuthors", "LexFlow",
    "--packDir", $publishDir,
    "--mainExe", "LexFlow.Settings.exe",
    "--outputDir", $releaseDir
)

vpk @packArgs
if ($LASTEXITCODE -ne 0) {
    throw "vpk pack failed with exit code $LASTEXITCODE."
}

Write-Host "Packed into $releaseDir"

if (-not $Upload) {
    Write-Host "Skipping upload. Re-run with -Upload to publish the GitHub release."
    return
}

if (-not $env:GITHUB_TOKEN) {
    throw "GITHUB_TOKEN is not set. Set it to a token with write access to $RepoUrl."
}

Write-Host "Uploading release $version to $RepoUrl"
$uploadArgs = @(
    "upload", "github",
    "--repoUrl", $RepoUrl,
    "--token", $env:GITHUB_TOKEN,
    "--outputDir", $releaseDir,
    "--tag", "v$version",
    "--releaseName", "LexFlow $version",
    "--publish"
)

if ($Prerelease) {
    $uploadArgs += "--pre"
}

vpk @uploadArgs
if ($LASTEXITCODE -ne 0) {
    throw "vpk upload failed with exit code $LASTEXITCODE."
}

Write-Host "Released LexFlow $version. Testers install from Setup.exe on the release page."
