@echo off
cd /d "%~dp0.."
dotnet test Lexon.slnx --logger "console;verbosity=normal"
echo.
pause
