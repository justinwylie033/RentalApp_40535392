[CmdletBinding()]
param(
    # Change this only if Android Device Manager gives the emulator a new name.
    [string]$AvdName = "pixel_7_-_api_36_0",

    # Use -Rebuild after changing mobile code. Normal starts reuse the installed APK.
    [switch]$Rebuild
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
$composeFile = Join-Path $repoRoot "docker-compose.yml"
$projectFile = Join-Path $repoRoot "RentalApp\RentalApp.csproj"

function Get-FreeSystemDriveSpaceGb {
    return [math]::Round(
        (New-Object System.IO.DriveInfo "C:\").AvailableFreeSpace / 1GB,
        2)
}

function Invoke-SafeBuildCacheCleanup {
    Write-Host "Low disk space detected. Clearing disposable build caches..." -ForegroundColor Yellow

    # Delete only the current user's Windows Temp contents. Files still required
    # by Docker, the emulator, or another running program remain locked and skip.
    $userTemp = Join-Path (
        [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "Temp"
    if (Test-Path $userTemp) {
        Get-ChildItem $userTemp -Force -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }

    # NuGet packages are a download cache, not project source. Clearing them can
    # recover several GB; dotnet publish restores only the packages it needs.
    & dotnet nuget locals all --clear
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "NuGet cache cleanup did not complete, but startup will recheck available space."
    }
}

Write-Host "[1/5] Finding the Android SDK..." -ForegroundColor Cyan

# Some Windows installations contain two Android SDKs. Select one that has both
# the emulator program and a system image, avoiding a broken AVD system path.
$localSdk = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "Android\Sdk"
$programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
$legacySdk = Join-Path $programFilesX86 "Android\android-sdk"
$sdkCandidates = @($env:ANDROID_SDK_ROOT, $legacySdk, $localSdk) |
    Where-Object { $_ } |
    Select-Object -Unique

$sdkRoot = $sdkCandidates |
    Where-Object {
        $systemImage = Get-ChildItem (Join-Path $_ "system-images") `
            -Filter "system.img" -File -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1
        (Test-Path (Join-Path $_ "emulator\emulator.exe")) -and
        ($null -ne $systemImage)
    } |
    Select-Object -First 1

if (-not $sdkRoot) {
    throw "No complete Android SDK was found. Install an emulator system image in Android Device Manager."
}

# These values apply only to this script and ensure the emulator uses the SDK
# selected above rather than a stale machine-wide environment variable.
$env:ANDROID_SDK_ROOT = $sdkRoot
$env:ANDROID_HOME = $sdkRoot
$adb = Join-Path $sdkRoot "platform-tools\adb.exe"
$emulator = Join-Path $sdkRoot "emulator\emulator.exe"

Write-Host "[2/5] Starting PostgreSQL/PostGIS and the ASP.NET Core API..." -ForegroundColor Cyan

# A normal start reuses the existing API image. -Rebuild rebuilds it first.
$composeArguments = @("compose", "-f", $composeFile, "up", "-d")
if ($Rebuild) {
    $composeArguments += "--build"
}

& docker @composeArguments
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose could not start RentalApp. Ensure Docker Desktop is running."
}

# Do not open Android until the API has confirmed it is ready to receive calls.
$apiHealthy = $false
for ($attempt = 1; $attempt -le 45; $attempt++) {
    try {
        $health = Invoke-RestMethod -Uri "http://127.0.0.1:8080/health" -TimeoutSec 3
        if ($health.status -eq "healthy") {
            $apiHealthy = $true
            break
        }
    }
    catch {
        Start-Sleep -Seconds 2
    }
}

if (-not $apiHealthy) {
    throw "The API did not become healthy. Run: docker compose logs --tail 80 api"
}

Write-Host "[3/5] Starting the Android emulator when required..." -ForegroundColor Cyan
& $adb start-server | Out-Null

$deviceLine = & $adb devices |
    Select-String -Pattern '^emulator-[0-9]+\s+device$' |
    Select-Object -First 1

if (-not $deviceLine) {
    # A cold boot avoids broken saved snapshots while keeping installed app data.
    Start-Process -FilePath $emulator -ArgumentList @(
        "-avd", $AvdName,
        "-no-snapshot-load",
        "-gpu", "swiftshader_indirect"
    )

    for ($attempt = 1; $attempt -le 90; $attempt++) {
        Start-Sleep -Seconds 2
        $deviceLine = & $adb devices |
            Select-String -Pattern '^emulator-[0-9]+\s+device$' |
            Select-Object -First 1
        if ($deviceLine) {
            break
        }
    }
}

if (-not $deviceLine) {
    throw "The emulator did not connect. Open Android Device Manager and check the AVD."
}

$deviceSerial = ($deviceLine.ToString() -split '\s+')[0]

# ADB can see a device before Android's package service has completed booting.
$androidReady = $false
for ($attempt = 1; $attempt -le 120; $attempt++) {
    $bootOutput = & $adb -s $deviceSerial shell getprop sys.boot_completed 2>$null
    $bootCompleted = ($bootOutput -join "").Trim()
    if ($bootCompleted -eq "1") {
        $androidReady = $true
        break
    }
    Start-Sleep -Seconds 2
}

if (-not $androidReady) {
    throw "Android did not finish booting within four minutes."
}

# Android can report boot completion before its virtual network is ready. Wait
# until the emulator itself can open the API port, preventing early sign-in
# attempts from seeing a short-lived "socket closed" failure.
$emulatorApiReady = $false
for ($attempt = 1; $attempt -le 30; $attempt++) {
    & $adb -s $deviceSerial shell toybox nc -z -w 2 10.0.2.2 8080 2>$null
    if ($LASTEXITCODE -eq 0) {
        $emulatorApiReady = $true
        break
    }
    Start-Sleep -Seconds 1
}

if (-not $emulatorApiReady) {
    throw "Android cannot reach the RentalApp API at 10.0.2.2:8080. Check Docker and emulator networking."
}

Write-Host "[4/5] Checking the RentalApp APK..." -ForegroundColor Cyan

if ($Rebuild) {
    # Publish an ordinary self-contained APK and install it without ADB's fragile
    # incremental installer. This may take several minutes after a clean build.
    $freeSpaceGb = Get-FreeSystemDriveSpaceGb
    if ($freeSpaceGb -lt 5) {
        Invoke-SafeBuildCacheCleanup
        $freeSpaceGb = Get-FreeSystemDriveSpaceGb
    }

    if ($freeSpaceGb -lt 5) {
        throw "Android packaging needs about 5 GB free on C:. Safe cleanup left only $freeSpaceGb GB. Use Windows Storage settings to free more space, then retry."
    }

    Write-Host "C: has $freeSpaceGb GB free; continuing with Android publish." -ForegroundColor Green

    & dotnet publish $projectFile `
        -c Debug `
        -f net10.0-android `
        -p:AndroidPackageFormats=apk `
        -p:EmbedAssembliesIntoApk=true

    if ($LASTEXITCODE -ne 0) {
        throw "The Android publish failed. Review the compiler or packaging error shown immediately above this message."
    }

    $apk = Get-ChildItem (Join-Path $repoRoot "RentalApp\bin\Debug\net10.0-android") `
        -Filter "*-Signed.apk" -File -Recurse |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $apk) {
        throw "The signed APK was not found after publishing."
    }

    & $adb -s $deviceSerial install --no-incremental -r $apk.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "APK installation failed. Restart the emulator and run this script again."
    }
}
else {
    # Fast startup expects the APK to have been installed previously.
    $installedPackage = & $adb -s $deviceSerial shell pm path com.justinwylie.rentalapp
    if (-not ($installedPackage -match '^package:')) {
        throw "RentalApp is not installed. Run this script once with -Rebuild."
    }
}

Write-Host "[5/5] Opening RentalApp..." -ForegroundColor Cyan
& $adb -s $deviceSerial shell monkey `
    -p com.justinwylie.rentalapp `
    -c android.intent.category.LAUNCHER 1 | Out-Null

Write-Host "RentalApp is ready." -ForegroundColor Green
Write-Host "Demo account 1: sarah@example.com / Rental123!"
Write-Host "Demo account 2: mike@example.com / Rental123!"
