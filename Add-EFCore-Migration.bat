@echo off
setlocal

rem Place this file in the solution root, next to the ADL folder's project folders.
set "ROOT=%~dp0"
set "LIB_PROJECT=%ROOT%AdlpLib\AdlpLib.csproj"
set "STARTUP_PROJECT=%ROOT%AdlpdsConfig\AdlpdsConfig.csproj"

if not exist "%LIB_PROJECT%" (
    echo ERROR: Could not find "%LIB_PROJECT%".
    echo Put this .bat file in the solution root, next to AdlpLib and AdlpdsConfig.
    pause
    exit /b 1
)
if not exist "%STARTUP_PROJECT%" (
    echo ERROR: Could not find "%STARTUP_PROJECT%".
    echo Put this .bat file in the solution root, next to AdlpLib and AdlpdsConfig.
    pause
    exit /b 1
)

set /p MIGRATION_NAME=Enter migration name (e.g. AddDeviceMulticastAddresses):
if "%MIGRATION_NAME%"=="" (
    echo Migration name cannot be empty.
    pause
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or dotnet.exe is not on PATH.
    pause
    exit /b 1
)

dotnet ef --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: EF command-line tools are not installed.
    echo Install them once by running:
    echo dotnet tool install --global dotnet-ef --version 8.0.32
    pause
    exit /b 1
)

rem The design-time factory defaults to SQLite. Keep the same provider as the current model snapshot.
set "ADLP_DB_PROVIDER=SQLite"

dotnet ef migrations add "%MIGRATION_NAME%" ^
  --project "%LIB_PROJECT%" ^
  --startup-project "%STARTUP_PROJECT%" ^
  --context AdlpLib.Persistence.AdlpDbContext ^
  --output-dir Persistence\Migrations

if errorlevel 1 (
    echo.
    echo Migration creation failed. Review the error above.
    pause
    exit /b 1
)

echo.
echo Migration created successfully.
echo Review the generated migration before relying on automatic startup migration.
echo This project supports SQLite and MySQL; ensure the Up/Down operations work for both providers.
pause
