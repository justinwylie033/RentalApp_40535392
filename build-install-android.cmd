@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%RentalApp\RentalApp.csproj"
set "ADB=%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe"

if not exist "%PROJECT%" (
    echo ERROR: RentalApp.csproj was not found beneath:
    echo %ROOT%
    goto :failed
)

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

echo Building the RentalApp debug APK...
dotnet publish "%PROJECT%" -c Debug -f net10.0-android -p:AndroidPackageFormats=apk -p:EmbedAssembliesIntoApk=true
if errorlevel 1 goto :failed

set "APK="
for %%F in ("%ROOT%RentalApp\bin\Debug\net10.0-android\publish\*-Signed.apk") do if exist "%%~fF" if not defined APK set "APK=%%~fF"
if not defined APK for /r "%ROOT%RentalApp\bin\Debug" %%F in (*-Signed.apk) do if not defined APK set "APK=%%F"
if not defined APK for /r "%ROOT%RentalApp\bin\Debug" %%F in (*.apk) do if not defined APK set "APK=%%F"

if not defined APK (
    echo ERROR: The build succeeded but no APK was found.
    goto :failed
)

echo Installing:
echo %APK%
"%ADB%" uninstall com.justinwylie.rentalapp >nul 2>&1
"%ADB%" install -r "%APK%"
if errorlevel 1 goto :failed

echo Launching RentalApp...
"%ADB%" shell monkey -p com.justinwylie.rentalapp -c android.intent.category.LAUNCHER 1 >nul

echo.
echo SUCCESS: RentalApp was built, installed, and launched.
echo Keep Docker Compose running while using the app.
pause
exit /b 0

:failed
echo.
echo The operation did not complete. Review the error shown above.
pause
exit /b 1
