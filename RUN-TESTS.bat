@echo off
setlocal
cd /d "%~dp0"
dotnet restore RaceDay.sln
if errorlevel 1 goto failed
dotnet test RaceDay.Tests\RaceDay.Tests.csproj --verbosity normal
if errorlevel 1 goto failed
echo All tests passed.
pause
exit /b 0
:failed
echo Tests did not complete successfully. Review the error above.
pause
exit /b 1
