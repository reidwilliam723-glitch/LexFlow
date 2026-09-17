@echo off
REM Double-click this file to build, pack, and publish a Lexon release.
REM Bump <Version> in Lexon.Settings\Lexon.Settings.csproj BEFORE running this,
REM and make sure GITHUB_TOKEN is set as a permanent user environment variable
REM (this window won't have it otherwise, since it's a fresh process each time).

cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File ".\release.ps1" -Upload

echo.
echo ============================================
echo  Done. Review the output above.
echo ============================================
pause
