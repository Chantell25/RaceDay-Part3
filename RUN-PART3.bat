@echo off
setlocal
start "RaceDay API" cmd /k "dotnet run --project RaceDay.Api\RaceDay.Api.csproj"
timeout /t 3 /nobreak >nul
start "RaceDay MVC" cmd /k "dotnet run --project RaceDay.Mvc\RaceDay.Mvc.csproj"
echo Started API and MVC in separate windows. Open the MVC URL shown in its window.
endlocal
