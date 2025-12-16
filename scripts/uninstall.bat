@echo off
:: ZKTeco Attendance Uninstaller
:: Run as Administrator

title ZKTeco Attendance - Uninstaller
color 0C

echo ===========================================
echo    ZKTeco Attendance Uninstaller
echo ===========================================
echo.

:: Check for admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Please run this uninstaller as Administrator!
    pause
    exit /b 1
)

set INSTALL_DIR=C:\Services\ZKTecoAttendance
set SERVICE_NAME=ZKTeco Attendance

echo This will remove the ZKTeco Attendance service and files.
echo Installation directory: %INSTALL_DIR%
echo.
set /p CONFIRM=Are you sure you want to uninstall? (Y/N): 
if /i not "%CONFIRM%"=="Y" (
    echo Uninstallation cancelled.
    pause
    exit /b 0
)

echo.
echo Stopping service...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 3 >nul

echo Removing service...
sc delete "%SERVICE_NAME%" >nul 2>&1

echo Unregistering zkemkeeper.dll...
if exist "%INSTALL_DIR%\zkemkeeper.dll" (
    regsvr32 /u /s "%INSTALL_DIR%\zkemkeeper.dll"
)

echo.
set /p REMOVE_FILES=Remove all files including logs and config? (Y/N): 
if /i "%REMOVE_FILES%"=="Y" (
    echo Removing files...
    rmdir /s /q "%INSTALL_DIR%" 2>nul
    echo Files removed.
) else (
    echo Files preserved at %INSTALL_DIR%
)

:: Remove from startup
echo Removing from startup...
del /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\start_zkteco.vbs" 2>nul

echo.
echo ===========================================
echo    Uninstallation Complete!
echo ===========================================
echo.
pause
