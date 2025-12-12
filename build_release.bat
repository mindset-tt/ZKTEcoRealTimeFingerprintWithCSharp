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
set RELEASES_DIR=%PROJECT_DIR%releases

:: Create releases directory
if not exist "%RELEASES_DIR%" mkdir "%RELEASES_DIR%"

:: Check WiX
where wix >nul 2>&1
if %errorLevel% neq 0 (
    echo Installing WiX Toolset...
    dotnet tool install --global wix
    wix extension add -g WixToolset.UI.wixext
)

echo.
echo [1/4] Building Release version...
echo ----------------------------------------

:: Build Release
dotnet publish ZKTecoRealTimeLog.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o publish\release

if %errorLevel% neq 0 (
    echo ERROR: Release build failed!
    pause
    exit /b 1
)

:: Copy zkemkeeper.dll to release
if exist "C:\Windows\SysWOW64\zkemkeeper.dll" (
    copy /Y "C:\Windows\SysWOW64\zkemkeeper.dll" "publish\release\" >nul
)

echo Release build complete!
echo.

echo [2/4] Building Debug version...
echo ----------------------------------------

:: Build Debug
dotnet publish ZKTecoRealTimeLog.csproj -c Debug -r win-x86 --self-contained true -p:PublishSingleFile=true -o publish\debug

if %errorLevel% neq 0 (
    echo ERROR: Debug build failed!
    pause
    exit /b 1
)

:: Copy zkemkeeper.dll to debug
if exist "C:\Windows\SysWOW64\zkemkeeper.dll" (
    copy /Y "C:\Windows\SysWOW64\zkemkeeper.dll" "publish\debug\" >nul
)

echo Debug build complete!
echo.

echo [3/4] Creating Release MSI...
echo ----------------------------------------

wix build Installer\Package.wxs -ext WixToolset.UI.wixext -d ProjectDir=%PROJECT_DIR% -d PublishDir=%PROJECT_DIR%publish\release\ -o %RELEASES_DIR%\ZKTecoAttendance-Setup.msi

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
wix build Installer\Package.wxs -ext WixToolset.UI.wixext -d ProjectDir=%PROJECT_DIR% -d PublishDir=%PROJECT_DIR%publish\debug\ -o %RELEASES_DIR%\ZKTecoAttendance-Debug.msi

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
