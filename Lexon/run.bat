@echo off
cd /d "%~dp0"
set "EXE=%~dp0Run\Lexon.exe"
if not exist "%EXE%" (
    echo Lexon.exe was not found in the Run folder.
    echo Ask Liam to run Settings\publish-portable.ps1 first.
    pause
    exit /b 1
)

tasklist /FI "IMAGENAME eq Lexon.exe" 2>nul | find /I "Lexon.exe" >nul
if not errorlevel 1 (
    echo Stopping the running Lexon instance...
    taskkill /IM Lexon.exe /F >nul 2>&1
    timeout /t 2 /nobreak >nul
)

tasklist /FI "IMAGENAME eq Lexon.Settings.exe" 2>nul | find /I "Lexon.Settings.exe" >nul
if not errorlevel 1 (
    taskkill /IM Lexon.Settings.exe /F >nul 2>&1
    timeout /t 1 /nobreak >nul
)

start "" "%EXE%"
