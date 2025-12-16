@echo off
cd /d %~dp0

:: Mimic SDKinstall32.bat logic
if /i "%PROCESSOR_IDENTIFIER:~0,3%"=="X86" (
    echo System is x86
    copy /Y "%~dp0zkemkeeper.dll" "%windir%\system32\"
    copy /Y "%~dp0*.dll" "%windir%\system32\" >nul 2>&1
    regsvr32 /s /c "%windir%\system32\zkemkeeper.dll"
) else (
    echo System is x64
    copy /Y "%~dp0zkemkeeper.dll" "%windir%\SysWOW64\"
    copy /Y "%~dp0*.dll" "%windir%\SysWOW64\" >nul 2>&1
    regsvr32 /s /c "%windir%\SysWOW64\zkemkeeper.dll"
)
exit /b 0
