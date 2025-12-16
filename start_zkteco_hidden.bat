@echo off
cd /d C:\Services\ZKTecoAttendance

:: Clear old log files
if exist logs\*.log del /q logs\*.log

:: Start the application
ZKTecoRealTimeLog.exe --console
