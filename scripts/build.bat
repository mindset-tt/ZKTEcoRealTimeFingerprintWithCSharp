@echo off
:: ZKTeco Attendance Build Script
:: Creates a distributable package

title ZKTeco Attendance - Build
color 0B

echo ===========================================
echo    ZKTeco Attendance Build Script
echo ===========================================
echo.

set PROJECT_DIR=%~dp0
set SRC_DIR=%PROJECT_DIR%..\src\ZKTecoRealTimeLog
set DOCS_DIR=%PROJECT_DIR%..\docs
set OUTPUT_DIR=%PROJECT_DIR%..\dist
set PUBLISH_DIR=%PROJECT_DIR%..\publish

:: Clean output directories
echo Cleaning output directories...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
mkdir "%OUTPUT_DIR%"
mkdir "%PUBLISH_DIR%"

:: Build for x86 (default - works with standard zkemkeeper.dll)
echo.
echo Building for x86 (32-bit)...
echo Building for x86 (32-bit)...
dotnet publish "%SRC_DIR%\ZKTecoRealTimeLog.csproj" -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -o "%PUBLISH_DIR%\x86"

if %errorLevel% neq 0 (
    echo ERROR: x86 build failed!
    pause
    exit /b 1
)

:: Build for x64 (requires 64-bit zkemkeeper.dll)
echo.
echo Building for x64 (64-bit)...
echo Building for x64 (64-bit)...
dotnet publish "%SRC_DIR%\ZKTecoRealTimeLog.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PlatformTarget=x64 -o "%PUBLISH_DIR%\x64"

if %errorLevel% neq 0 (
    echo WARNING: x64 build failed. Continuing...
)

:: Build for ARM64 (requires ARM64 zkemkeeper.dll)
echo.
echo Building for ARM64...
dotnet publish "%SRC_DIR%\ZKTecoRealTimeLog.csproj" -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:PlatformTarget=ARM64 -o "%PUBLISH_DIR%\ARM64"

if %errorLevel% neq 0 (
    echo WARNING: ARM64 build failed. Continuing...
)

:: Create distribution packages
echo.
echo Creating distribution packages...

:: x86 Package
echo Creating x86 package...
mkdir "%OUTPUT_DIR%\ZKTecoAttendance-x86"
mkdir "%OUTPUT_DIR%\ZKTecoAttendance-x86\publish"
copy "%PUBLISH_DIR%\x86\ZKTecoRealTimeLog.exe" "%OUTPUT_DIR%\ZKTecoAttendance-x86\publish\" >nul
copy "%SRC_DIR%\.env.example" "%OUTPUT_DIR%\ZKTecoAttendance-x86\" >nul
copy "%PROJECT_DIR%install.bat" "%OUTPUT_DIR%\ZKTecoAttendance-x86\" >nul
copy "%PROJECT_DIR%uninstall.bat" "%OUTPUT_DIR%\ZKTecoAttendance-x86\" >nul
copy "%DOCS_DIR%\README.md" "%OUTPUT_DIR%\ZKTecoAttendance-x86\" >nul

:: x64 Package
if exist "%PUBLISH_DIR%\x64\ZKTecoRealTimeLog.exe" (
    echo Creating x64 package...
    mkdir "%OUTPUT_DIR%\ZKTecoAttendance-x64"
    mkdir "%OUTPUT_DIR%\ZKTecoAttendance-x64\publish"
    copy "%PUBLISH_DIR%\x64\ZKTecoRealTimeLog.exe" "%OUTPUT_DIR%\ZKTecoAttendance-x64\publish\" >nul
    copy "%SRC_DIR%\.env.example" "%OUTPUT_DIR%\ZKTecoAttendance-x64\" >nul
    copy "%PROJECT_DIR%install.bat" "%OUTPUT_DIR%\ZKTecoAttendance-x64\" >nul
    copy "%PROJECT_DIR%uninstall.bat" "%OUTPUT_DIR%\ZKTecoAttendance-x64\" >nul
    copy "%DOCS_DIR%\README.md" "%OUTPUT_DIR%\ZKTecoAttendance-x64\" >nul
)

:: ARM64 Package
if exist "%PUBLISH_DIR%\ARM64\ZKTecoRealTimeLog.exe" (
    echo Creating ARM64 package...
    mkdir "%OUTPUT_DIR%\ZKTecoAttendance-ARM64"
    mkdir "%OUTPUT_DIR%\ZKTecoAttendance-ARM64\publish"
    copy "%PUBLISH_DIR%\ARM64\ZKTecoRealTimeLog.exe" "%OUTPUT_DIR%\ZKTecoAttendance-ARM64\publish\" >nul
    copy "%SRC_DIR%\.env.example" "%OUTPUT_DIR%\ZKTecoAttendance-ARM64\" >nul
    copy "%PROJECT_DIR%install.bat" "%OUTPUT_DIR%\ZKTecoAttendance-ARM64\" >nul
    copy "%PROJECT_DIR%uninstall.bat" "%OUTPUT_DIR%\ZKTecoAttendance-ARM64\" >nul
    copy "%DOCS_DIR%\README.md" "%OUTPUT_DIR%\ZKTecoAttendance-ARM64\" >nul
)

echo.
echo ===========================================
echo    Build Complete!
echo ===========================================
echo.
echo Distribution packages created in: %OUTPUT_DIR%
echo.
echo Packages:
dir /b "%OUTPUT_DIR%"
echo.
echo IMPORTANT: 
echo - Copy zkemkeeper.dll into each package's publish folder
echo - x86 package works with standard ZKTeco SDK
echo - x64/ARM64 require matching zkemkeeper.dll from ZKTeco
echo.
pause
