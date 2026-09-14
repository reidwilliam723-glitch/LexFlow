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
#
# Every run first downloads the current release so vpk can emit a delta package
# alongside the full one. The very first release has nothing to download; that is
# expected and the run continues with a full package only.
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

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

# vpk can only diff against a previous release that is already sitting in the
# output directory, so fetch the current one before packing. Without this every
# release ships as a full package and testers re-download the whole app.
Write-Host "Fetching previous release for delta generation"
$downloadArgs = @(
    "download", "github",
    "--repoUrl", $RepoUrl,
    "--outputDir", $releaseDir
)
if ($env:GITHUB_TOKEN) {
    $downloadArgs += @("--token", $env:GITHUB_TOKEN)
}

# A repo with no releases yet is the expected first-run case, so this step must
# never abort the run. Both ways PowerShell could turn it into a terminating error
# are suppressed: $ErrorActionPreference is Stop above, and 7.4+ promotes native
# command failures on its own unless $PSNativeCommandUseErrorActionPreference is off.
try {
    $ErrorActionPreference = "Continue"
    $PSNativeCommandUseErrorActionPreference = $false

    vpk @downloadArgs
}
catch {
    Write-Host "Could not fetch the previous release: $($_.Exception.Message)"
}
finally {
    $ErrorActionPreference = "Stop"
}

# vpk warns and still exits 0 when it finds nothing to download, so decide whether
# a delta is possible from what actually landed on disk rather than the exit code.
$priorFull = @(Get-ChildItem -Path $releaseDir -Filter "*-full.nupkg" -ErrorAction SilentlyContinue)
if ($priorFull.Count -gt 0) {
    Write-Host "Previous release available ($($priorFull.Count) full package). vpk pack will generate a delta."
}
else {
    Write-Host "No previous release to diff against. Packing a full release only."
}

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
