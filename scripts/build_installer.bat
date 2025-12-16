@echo off
:: ZKTeco Attendance Complete Installer Builder
:: Creates MSI + Bundle with .NET Runtime
:: Requires: WiX Toolset v4+

title ZKTeco Attendance - Complete Installer Builder
color 0B

echo ===========================================
echo    ZKTeco Attendance Installer Builder
echo    (MSI + .NET Runtime Bundle)
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
    echo WiX Toolset not found! Installing...
    echo.
    dotnet tool install --global wix
    
    if %errorLevel% neq 0 (
        echo.
        echo Failed to install WiX. Please install manually:
        echo   dotnet tool install --global wix
        echo.
        pause
        exit /b 1
    )
    
    :: Add WiX extensions
    echo Installing WiX extensions...
    wix extension add -g WixToolset.UI.wixext
    wix extension add -g WixToolset.Bal.wixext
    wix extension add -g WixToolset.NetFx.wixext
)

echo WiX Toolset ready!
echo.

:: Create directories
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%PUBLISH_DIR%" mkdir "%PUBLISH_DIR%"
if not exist "%INSTALLER_DIR%" mkdir "%INSTALLER_DIR%"

:: Step 1: Build the application
echo Step 1: Building application (x86)...
echo Step 1: Building application (x86)...
dotnet publish "%SRC_DIR%\ZKTecoRealTimeLog.csproj" -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_DIR%"

if %errorLevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo Application built successfully!
echo.

:: Check for zkemkeeper.dll
if not exist "%PUBLISH_DIR%\zkemkeeper.dll" (
    echo.
    echo =====================================================
    echo WARNING: zkemkeeper.dll not found!
    echo.
    echo Please copy zkemkeeper.dll to:
    echo   %PUBLISH_DIR%\
    echo.
    echo The installer will work but users will need to
    echo manually add zkemkeeper.dll after installation.
    echo =====================================================
    echo.
    pause
)

:: Step 2: Build MSI package
echo Step 2: Building MSI package...
cd "%INSTALLER_DIR%"

wix build "%SRC_DIR%\Installer\Package.wxs" ^
    -ext WixToolset.UI.wixext ^
    -d ProjectDir="%SRC_DIR%" ^
    -d PublishDir="%PUBLISH_DIR%" ^
    -o "%OUTPUT_DIR%\ZKTecoAttendance-Setup.msi"

if %errorLevel% neq 0 (
    echo.
    echo ERROR: MSI build failed!
    echo.
    echo Trying simplified build...
    
    :: Create a simpler WXS file
    call :CreateSimpleWxs
    
    wix build Simple.wxs ^
        -d ProjectDir="%SRC_DIR%" ^
        -d PublishDir="%PUBLISH_DIR%" ^
        -o "%OUTPUT_DIR%\ZKTecoAttendance-Setup.msi"
    
    if %errorLevel% neq 0 (
        echo MSI build still failed. Check WiX installation.
        cd "%PROJECT_DIR%"
        pause
        exit /b 1
    )
)

cd "%PROJECT_DIR%"
echo MSI package created!
echo.

:: Step 3: Create Bundle (optional - requires .NET runtime download)
echo Step 3: Creating standalone bundle...
echo.
echo To create a bundle with embedded .NET Runtime:
echo 1. Download .NET 9.0 Desktop Runtime (x86) from:
echo    https://dotnet.microsoft.com/download/dotnet/9.0
echo 2. Save as: %INSTALLER_DIR%\windowsdesktop-runtime-9.0.0-win-x86.exe
echo 3. Run: wix build Installer\Bundle.wxs -o dist\ZKTecoAttendance-Installer.exe
echo.

echo ===========================================
echo    Build Complete!
echo ===========================================
echo.
echo Output files:
echo   MSI: %OUTPUT_DIR%\ZKTecoAttendance-Setup.msi
echo.
echo To install:
echo   msiexec /i ZKTecoAttendance-Setup.msi
echo.
echo To reduce antivirus false positives:
echo 1. Purchase a code signing certificate ($200-500/year)
echo 2. Sign with: signtool sign /f cert.pfx /p PASSWORD /tr http://timestamp.digicert.com /td sha256 ZKTecoAttendance-Setup.msi
echo.
pause
exit /b 0

:CreateSimpleWxs
:: Create a simplified WXS file if the main one fails
echo Creating simplified installer definition...
(
echo ^<?xml version="1.0" encoding="UTF-8"?^>
echo ^<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"^>
echo   ^<Package Name="ZKTeco Attendance" Version="2.0.0.0" Manufacturer="Mlfts" UpgradeCode="A1B2C3D4-E5F6-7890-ABCD-EF1234567890"^>
echo     ^<MajorUpgrade DowngradeErrorMessage="A newer version is installed." /^>
echo     ^<MediaTemplate EmbedCab="yes" /^>
echo     ^<Feature Id="Main" Level="1"^>
echo       ^<ComponentGroupRef Id="ProductFiles" /^>
echo     ^</Feature^>
echo     ^<StandardDirectory Id="ProgramFilesFolder"^>
echo       ^<Directory Id="INSTALLFOLDER" Name="ZKTeco Attendance"^>
echo         ^<Component Id="MainExe" Guid="*"^>
echo           ^<File Source="$(var.PublishDir)\ZKTecoRealTimeLog.exe" /^>
echo           ^<ServiceInstall Name="ZKTecoAttendance" DisplayName="ZKTeco Attendance" Type="ownProcess" Start="auto" ErrorControl="normal" /^>
echo           ^<ServiceControl Id="ZKTecoSvc" Name="ZKTecoAttendance" Start="install" Stop="both" Remove="uninstall" /^>
echo         ^</Component^>
echo       ^</Directory^>
echo     ^</StandardDirectory^>
echo     ^<ComponentGroup Id="ProductFiles" Directory="INSTALLFOLDER"^>
echo       ^<ComponentRef Id="MainExe" /^>
echo     ^</ComponentGroup^>
echo   ^</Package^>
echo ^</Wix^>
) > "%INSTALLER_DIR%\Simple.wxs"
exit /b 0
