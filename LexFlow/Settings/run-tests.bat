@echo off
cd /d "%~dp0.."
dotnet test LexFlow.slnx --logger "console;verbosity=normal"
echo.
pause
