
@echo off
setlocal EnableExtensions
title ADL Release Builder

set "ROOT=%~dp0"
set "OUT=%ROOT%release\ADL-Win10-x64"
set "TEMP=%ROOT%build_temp"

set "CONFIG=%ROOT%AdlpdsConfig\AdlpdsConfig.csproj"
set "DB=%ROOT%AdlpDBConfigurator\AdlpDBConfigurator.csproj"
set "EMULATOR=%ROOT%ADLDeviceEmulator\ADLDeviceEmulator.csproj"

echo ==========================================
echo          ADL RELEASE BUILDER
echo ==========================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found.
    goto failed
)

if not exist "%CONFIG%" (
    echo ERROR: AdlpdsConfig.csproj not found.
    goto failed
)

if not exist "%DB%" (
    echo ERROR: AdlpDBConfigurator.csproj not found.
    goto failed
)

if not exist "%EMULATOR%" (
    echo ERROR: ADLDeviceEmulator.csproj not found.
    goto failed
)

echo Cleaning previous build...
if exist "%OUT%" rmdir /s /q "%OUT%"
if exist "%TEMP%" rmdir /s /q "%TEMP%"

mkdir "%OUT%"
mkdir "%TEMP%"
mkdir "%OUT%\Projects\Configurator\Libraries"
mkdir "%OUT%\Projects\DBConfigurator\Libraries"
mkdir "%OUT%\Projects\ADLDeviceEmulator\Libraries"

echo.
echo [1/3] Publishing Configurator...

dotnet publish "%CONFIG%" ^
 -c Release ^
 -r win-x64 ^
 --self-contained true ^
 -p:PublishSingleFile=true ^
 -p:IncludeNativeLibrariesForSelfExtract=true ^
 -o "%TEMP%\Configurator"

if errorlevel 1 goto failed

echo.
echo Copying Configurator files...

for %%F in ("%TEMP%\Configurator\*.exe") do (
    copy /y "%%F" "%OUT%\" >nul
)

for %%F in ("%TEMP%\Configurator\*") do (
    if not exist "%%F\" (
        if /i not "%%~xF"==".exe" (
            copy /y "%%F" "%OUT%\Projects\Configurator\Libraries\" >nul
        )
    )
)

echo.
echo [2/3] Publishing DBConfigurator...

dotnet publish "%DB%" ^
 -c Release ^
 -r win-x64 ^
 --self-contained true ^
 -p:PublishSingleFile=true ^
 -p:IncludeNativeLibrariesForSelfExtract=true ^
 -o "%TEMP%\DBConfigurator"

if errorlevel 1 goto failed

echo.
echo Copying DBConfigurator files...

for %%F in ("%TEMP%\DBConfigurator\*.exe") do (
    copy /y "%%F" "%OUT%\" >nul
)

for %%F in ("%TEMP%\DBConfigurator\*") do (
    if not exist "%%F\" (
        if /i not "%%~xF"==".exe" (
            copy /y "%%F" "%OUT%\Projects\DBConfigurator\Libraries\" >nul
        )
    )
)

echo.
echo [3/3] Publishing ADLDeviceEmulator...

dotnet publish "%EMULATOR%" ^
 -c Release ^
 -r win-x64 ^
 --self-contained true ^
 -p:PublishSingleFile=true ^
 -p:IncludeNativeLibrariesForSelfExtract=true ^
 -o "%TEMP%\ADLDeviceEmulator"

if errorlevel 1 goto failed

echo.
echo Copying ADLDeviceEmulator files...

for %%F in ("%TEMP%\ADLDeviceEmulator\*.exe") do (
    copy /y "%%F" "%OUT%\" >nul
)

for %%F in ("%TEMP%\ADLDeviceEmulator\*") do (
    if not exist "%%F\" (
        if /i not "%%~xF"==".exe" (
            copy /y "%%F" "%OUT%\Projects\ADLDeviceEmulator\Libraries\" >nul
        )
    )
)

echo.
echo Creating Configurator data folders...

mkdir "%OUT%\Projects\Configurator\LibFirmwareDevice"
mkdir "%OUT%\Projects\Configurator\LibFirmwareDevice\adlfw"
mkdir "%OUT%\Projects\Configurator\LibFirmwareDevice\hex"
mkdir "%OUT%\Projects\Configurator\Configurations"

echo.
echo ==========================================
echo BUILD COMPLETED
echo ==========================================
echo Output: %OUT%
goto finish

:failed
echo.
echo ==========================================
echo BUILD FAILED
echo ==========================================
set "RESULT=1"
goto end

:finish
set "RESULT=0"
goto end

:end
if exist "%TEMP%" rmdir /s /q "%TEMP%"
echo.
pause
exit /b %RESULT%
