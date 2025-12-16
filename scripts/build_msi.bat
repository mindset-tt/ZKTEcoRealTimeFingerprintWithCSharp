@echo off
:: ZKTeco Attendance MSI Installer Builder
:: Requires: WiX Toolset v4+ (https://wixtoolset.org/)

title ZKTeco Attendance - MSI Builder
color 0B

echo ===========================================
echo    ZKTeco Attendance MSI Builder
echo ===========================================
echo.

set PROJECT_DIR=%~dp0
set SRC_DIR=%PROJECT_DIR%..\src\ZKTecoRealTimeLog
set PUBLISH_DIR=%PROJECT_DIR%..\publish\x86
set INSTALLER_DIR=%SRC_DIR%\Installer
set OUTPUT_DIR=%PROJECT_DIR%..\dist

:: Check if WiX is installed
where wix >nul 2>&1
if %errorLevel% neq 0 (
    echo WiX Toolset not found!
    echo.
    echo Please install WiX Toolset:
    echo   dotnet tool install --global wix
    echo.
    echo Or download from: https://wixtoolset.org/
    echo.
    pause
    exit /b 1
)

echo WiX Toolset found!
echo.

:: Build the application first
echo Step 1: Building application...
echo Step 1: Building application...
dotnet publish "%SRC_DIR%\ZKTecoRealTimeLog.csproj" -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_DIR%"

if %errorLevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

:: Check if zkemkeeper.dll exists
if not exist "%PUBLISH_DIR%\zkemkeeper.dll" (
    echo.
    echo WARNING: zkemkeeper.dll not found in publish folder!
    echo Please copy zkemkeeper.dll to: %PUBLISH_DIR%
    echo.
    set /p CONTINUE=Continue anyway? ^(Y/N^): 
    if /i not "%CONTINUE%"=="Y" (
        exit /b 1
    )
)

:: Create output directory
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

:: Build MSI
echo.
echo Step 2: Building MSI installer...
cd "%INSTALLER_DIR%"

wix build "%SRC_DIR%\Installer\Package.wxs" -d ProjectDir="%SRC_DIR%" -d PublishDir="%PUBLISH_DIR%" -o "%OUTPUT_DIR%\ZKTecoAttendance-Setup.msi"

if %errorLevel% neq 0 (
    echo.
    echo ERROR: MSI build failed!
    echo.
    echo Common issues:
    echo 1. Missing files referenced in Package.wxs
    echo 2. WiX syntax errors
    echo 3. Missing WiX UI extension
    echo.
    echo Try installing WiX extensions:
    echo   wix extension add WixToolset.UI.wixext
    echo.
    pause
    exit /b 1
)

cd "%PROJECT_DIR%"

echo.
echo ===========================================
echo    MSI Build Complete!
echo ===========================================
echo.
echo Installer created: %OUTPUT_DIR%\ZKTecoAttendance-Setup.msi
echo.
echo To reduce antivirus detection:
echo 1. Get a code signing certificate
echo 2. Sign the MSI: signtool sign /f cert.pfx /p password /t http://timestamp.digicert.com "%OUTPUT_DIR%\ZKTecoAttendance-Setup.msi"
echo.
pause
