@echo off
setlocal
cd /d "%~dp0"
echo ========================================
echo RaceDay Part 2 - Run the REST API
echo ========================================
echo Checking .NET 8 SDK...
dotnet --list-sdks
if errorlevel 1 (
  echo ERROR: Install the .NET 8 SDK before running RaceDay.
  pause
  exit /b 1
)
echo Restoring packages...
dotnet restore RaceDay.sln
if errorlevel 1 goto failed
echo Building all projects...
dotnet build RaceDay.sln --no-restore
if errorlevel 1 goto failed
echo Starting the API. Swagger should open at https://localhost:7157/swagger
 dotnet run --project RaceDay.Api\RaceDay.Api.csproj --launch-profile RaceDay.Api
if errorlevel 1 goto failed
exit /b 0
:failed
echo.
echo Run failed. Read the error above; check SQL Server and appsettings.json.
pause
exit /b 1
