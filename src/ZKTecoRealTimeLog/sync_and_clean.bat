@echo off
REM ZKTeco Attendance - Batch Sync & Clean
REM This script clears the database and re-syncs all history from devices.

echo ===================================================
echo   ZKTeco Attendance - Batch Sync & Clean
echo ===================================================
echo.
echo WARNING: This will DELETE all existing WorkRecord data!
echo It will then re-sync everything from the devices.
echo.

cd /d "%~dp0"
if exist "ZKTecoRealTimeLog.exe" (
    ZKTecoRealTimeLog.exe --batch-sync
) else (
    echo Error: ZKTecoRealTimeLog.exe not found!
    echo Please run this script from the installation directory.
    pause
)

echo.
echo Done.
pause
