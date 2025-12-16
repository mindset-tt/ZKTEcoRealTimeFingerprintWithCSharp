Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "C:\Services\ZKTecoAttendance\start_zkteco_hidden.bat", 0, False
Set WshShell = Nothing
