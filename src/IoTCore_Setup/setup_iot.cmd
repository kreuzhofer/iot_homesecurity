rem Set the permissions to execute limpet.exe
reg.exe ADD "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\EmbeddedMode\ProcessLauncher" /v AllowedExecutableFilesList /t REG_MULTI_SZ /d "c:\windows\system32\limpet.exe\0"
rem Setup scheduled task to run every minute and check if app is still running
rem schtasks /Create /TN "IoTHomesecurity" /SC MINUTE /MO 1 /TR "c:\deploy\checkapp.cmd > c:\deploy\checkapp.log" /RU SYSTEM