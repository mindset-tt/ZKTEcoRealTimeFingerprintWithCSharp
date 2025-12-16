@echo off
:: ZKTeco Attendance - Build Release & Debug MSI
:: Creates both Release and Debug installers for GitHub

title ZKTeco Attendance - Release Builder
color 0A

echo =============================================
echo    ZKTeco Attendance - Release Builder
echo    Building Release and Debug MSI packages
echo =============================================
echo.

set PROJECT_DIR=%~dp0
set SRC_DIR=%PROJECT_DIR%..\src\ZKTecoRealTimeLog
set RELEASES_DIR=%PROJECT_DIR%..\releases
set PUBLISH_DIR=%PROJECT_DIR%..\publish

:: Create releases directory
if not exist "%RELEASES_DIR%" mkdir "%RELEASES_DIR%"

:: Check WiX
where wix >nul 2>&1
if %errorLevel% neq 0 (
    echo Installing WiX Toolset...
    dotnet tool install --global wix
)

:: Always try to add UI extension to ensure it's present
:: Always try to add UI and Util extension to ensure it's present
wix extension add -g WixToolset.UI.wixext
wix extension add -g WixToolset.Util.wixext

:: Generate Icon
echo.
echo Generaring Icon...
set ICO_PATH=%SRC_DIR%\app.ico
if not exist "%ICO_PATH%" (
    echo.
    echo Generating Icon...
    powershell -ExecutionPolicy Bypass -File "%PROJECT_DIR%convert_icon.ps1"
    
    if not exist "%ICO_PATH%" (
         echo.
         echo WARNING: Icon generation failed or no source image found.
         echo Expected source image at: ..\image\zkteco.png OR ..\image\zkteco.webp
         echo.
         echo Please manually CREATE or COPY an 'app.ico' file to:
         echo %ICO_PATH%
         echo.
         echo The build cannot continue correctly without this file using current configuration.
         pause
    )
)

echo.
echo [1/4] Building Release version...
echo ----------------------------------------

:: Build Release
:: Build Release
dotnet publish "%SRC_DIR%\ZKTecoRealTimeLog.csproj" -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_DIR%\release"

if %errorLevel% neq 0 (
    echo ERROR: Release build failed!
    pause
    exit /b 1
)

:: Copy zkemkeeper.dll to release
if exist "C:\Windows\SysWOW64\zkemkeeper.dll" (
    copy /Y "C:\Windows\SysWOW64\zkemkeeper.dll" "%PUBLISH_DIR%\release\" >nul
)

if not exist "%PUBLISH_DIR%\release\zkemkeeper.dll" (
    echo.
    echo WARNING: zkemkeeper.dll not found in publish\release!
    echo Please copy zkemkeeper.dll to:
    echo %PUBLISH_DIR%\release\
    echo.
    echo The build cannot continue without this file.
    pause
    if not exist "%PUBLISH_DIR%\release\zkemkeeper.dll" exit /b 1
)

echo Release build complete!
echo.

echo [2/4] Building Debug version...
echo ----------------------------------------

:: Build Debug
:: Build Debug
dotnet publish "%SRC_DIR%\ZKTecoRealTimeLog.csproj" -c Debug -r win-x86 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_DIR%\debug"

if %errorLevel% neq 0 (
    echo ERROR: Debug build failed!
    pause
    exit /b 1
)

:: Copy zkemkeeper.dll to debug
if exist "C:\Windows\SysWOW64\zkemkeeper.dll" (
    copy /Y "C:\Windows\SysWOW64\zkemkeeper.dll" "%PUBLISH_DIR%\debug\" >nul
)

if not exist "%PUBLISH_DIR%\debug\zkemkeeper.dll" (
    echo.
    echo WARNING: zkemkeeper.dll not found in publish\debug!
    echo Please copy zkemkeeper.dll to:
    echo %PUBLISH_DIR%\debug\
    echo.
    pause
    if not exist "%PUBLISH_DIR%\debug\zkemkeeper.dll" exit /b 1
)

echo Debug build complete!
echo.

echo [3/4] Creating Release MSI...
echo ----------------------------------------

wix build "%SRC_DIR%\Installer\Package.wxs" -ext WixToolset.UI.wixext -ext WixToolset.Util.wixext -d ProjectDir="%SRC_DIR%" -d PublishDir="%PUBLISH_DIR%\release" -o "%RELEASES_DIR%\ZKTecoAttendance-Setup.msi"

if %errorLevel% neq 0 (
    echo ERROR: Release MSI failed!
    pause
    exit /b 1
)

echo Release MSI created!
echo.

echo [4/4] Creating Debug MSI...
echo ----------------------------------------

:: Update version for debug (add .1 to distinguish)
:: Update version for debug (add .1 to distinguish)
wix build "%SRC_DIR%\Installer\Package.wxs" -ext WixToolset.UI.wixext -ext WixToolset.Util.wixext -d ProjectDir="%SRC_DIR%" -d PublishDir="%PUBLISH_DIR%\debug" -o "%RELEASES_DIR%\ZKTecoAttendance-Debug.msi"

if %errorLevel% neq 0 (
    echo ERROR: Debug MSI failed!
    pause
    exit /b 1
)

echo Debug MSI created!
echo.

:: Clean up wixpdb files
del /q "%RELEASES_DIR%\*.wixpdb" 2>nul

echo =============================================
echo    BUILD COMPLETE!
echo =============================================
echo.
echo Output files in releases folder:
echo.
dir /b "%RELEASES_DIR%\*.msi"
echo.
echo Location: %RELEASES_DIR%
echo.
echo Next steps:
echo 1. Test both installers
echo 2. Commit and push to GitHub
echo 3. Create a new Release on GitHub
echo 4. Upload both MSI files to the Release
echo.
pause
