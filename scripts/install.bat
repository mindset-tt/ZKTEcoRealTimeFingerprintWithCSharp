@echo off
:: ZKTeco Attendance Installer
:: Run as Administrator

title ZKTeco Attendance - Installer
color 0A

echo ===========================================
echo    ZKTeco Attendance Installer v2.0
echo ===========================================
echo.

:: Check for admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Please run this installer as Administrator!
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

set INSTALL_DIR=C:\Services\ZKTecoAttendance
set SERVICE_NAME=ZKTeco Attendance

echo Installation Directory: %INSTALL_DIR%
echo.

:: Check if .NET is installed
echo Checking .NET Runtime...
dotnet --list-runtimes >nul 2>&1
if %errorLevel% neq 0 (
    echo .NET Runtime not found. Downloading...
    echo.
    
    :: Download .NET 9.0 Runtime
    echo Downloading .NET 9.0 Desktop Runtime...
    powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://download.visualstudio.microsoft.com/download/pr/dotnet-runtime-9.0-win-x86.exe' -OutFile '%TEMP%\dotnet-runtime.exe'}" 2>nul
    
    if exist "%TEMP%\dotnet-runtime.exe" (
        echo Installing .NET Runtime...
        start /wait "" "%TEMP%\dotnet-runtime.exe" /install /quiet /norestart
        del "%TEMP%\dotnet-runtime.exe"
        echo .NET Runtime installed successfully!
    ) else (
        echo.
        echo Could not download .NET Runtime automatically.
        echo Please download and install manually from:
        echo https://dotnet.microsoft.com/download/dotnet/9.0
        echo.
        pause
        exit /b 1
    )
) else (
    echo .NET Runtime found!
)
echo.

:: Stop existing service if running
echo Stopping existing service...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 >nul

:: Delete existing service
echo Removing existing service...
sc delete "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 >nul

:: Create installation directory
echo Creating installation directory...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if not exist "%INSTALL_DIR%\logs" mkdir "%INSTALL_DIR%\logs"

:: Copy files
echo Copying application files...
xcopy /Y /E /I "%~dp0publish\*" "%INSTALL_DIR%\" >nul

:: Copy .env if exists
if exist "%~dp0.env" (
    copy /Y "%~dp0.env" "%INSTALL_DIR%\" >nul
    echo Configuration file copied.
) else (
    if exist "%~dp0.env.example" (
        copy /Y "%~dp0.env.example" "%INSTALL_DIR%\.env" >nul
        echo Default configuration file created. Please edit %INSTALL_DIR%\.env
    )
)

:: Register zkemkeeper.dll if present
if exist "%INSTALL_DIR%\zkemkeeper.dll" (
    echo Registering zkemkeeper.dll...
    regsvr32 /s "%INSTALL_DIR%\zkemkeeper.dll"
    echo zkemkeeper.dll registered.
) else (
    echo WARNING: zkemkeeper.dll not found in installation directory!
    echo Please copy zkemkeeper.dll to %INSTALL_DIR%\
    echo Then run: regsvr32 "%INSTALL_DIR%\zkemkeeper.dll"
)

:: Create Windows Service
echo.
echo Creating Windows Service...
sc create "%SERVICE_NAME%" binPath= "%INSTALL_DIR%\ZKTecoRealTimeLog.exe" start= auto DisplayName= "ZKTeco Attendance Service"
sc description "%SERVICE_NAME%" "Real-time attendance monitoring for ZKTeco fingerprint devices"
sc failure "%SERVICE_NAME%" reset= 86400 actions= restart/60000/restart/60000/restart/60000

:: Create startup batch files
echo Creating startup scripts...

:: Hidden startup script
echo @echo off > "%INSTALL_DIR%\start_hidden.bat"
echo cd /d "%INSTALL_DIR%" >> "%INSTALL_DIR%\start_hidden.bat"
echo if exist logs\*.log del /q logs\*.log >> "%INSTALL_DIR%\start_hidden.bat"
echo ZKTecoRealTimeLog.exe --console >> "%INSTALL_DIR%\start_hidden.bat"

:: VBS for hidden startup
echo Set WshShell = CreateObject("WScript.Shell") > "%INSTALL_DIR%\start_zkteco.vbs"
echo WshShell.Run "%INSTALL_DIR%\start_hidden.bat", 0, False >> "%INSTALL_DIR%\start_zkteco.vbs"
echo Set WshShell = Nothing >> "%INSTALL_DIR%\start_zkteco.vbs"

:: Console mode script
echo @echo off > "%INSTALL_DIR%\start_console.bat"
echo title ZKTeco Attendance Console >> "%INSTALL_DIR%\start_console.bat"
echo cd /d "%INSTALL_DIR%" >> "%INSTALL_DIR%\start_console.bat"
echo ZKTecoRealTimeLog.exe --console >> "%INSTALL_DIR%\start_console.bat"
echo pause >> "%INSTALL_DIR%\start_console.bat"

echo.
echo ===========================================
echo    Installation Complete!
echo ===========================================
echo.
echo Installation directory: %INSTALL_DIR%
echo.
echo IMPORTANT: Before starting, please:
echo 1. Edit %INSTALL_DIR%\.env to configure your devices
echo 2. Ensure zkemkeeper.dll is in %INSTALL_DIR%\
echo.
echo To start the service:
echo   sc start "%SERVICE_NAME%"
echo.
echo To run in console mode (for testing):
echo   %INSTALL_DIR%\start_console.bat
echo.
echo To auto-start on login (hidden):
echo   Copy start_zkteco.vbs to your Startup folder
echo   (Run: shell:startup)
echo.
echo Log files location: %INSTALL_DIR%\logs\
echo.
pause
