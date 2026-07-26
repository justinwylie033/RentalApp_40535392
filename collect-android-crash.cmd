@echo off
setlocal

set "ADB=%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe"
set "OUTPUT=%~dp0rentalapp-crash.txt"
set "PACKAGE=com.justinwylie.rentalapp"

if not exist "%ADB%" (
    echo ERROR: adb.exe was not found at:
    echo %ADB%
    goto :failed
)

"%ADB%" get-state >nul 2>&1
if errorlevel 1 (
    echo ERROR: No running Android emulator was detected.
    echo Start the emulator and run this file again.
    goto :failed
)

echo Clearing the old Android log...
"%ADB%" logcat -c

echo Starting RentalApp and waiting for the crash...
"%ADB%" shell am force-stop %PACKAGE%
"%ADB%" shell monkey -p %PACKAGE% -c android.intent.category.LAUNCHER 1 >nul
timeout /t 8 /nobreak >nul

echo Saving the Android crash log...
"%ADB%" logcat -d -t 1200 > "%OUTPUT%"

if not exist "%OUTPUT%" goto :failed

echo.
echo DONE: The crash log was saved here:
echo %OUTPUT%
echo.
echo Attach rentalapp-crash.txt to the ChatGPT conversation.
pause
exit /b 0

:failed
echo.
echo The crash log could not be collected. Review the error above.
pause
exit /b 1
