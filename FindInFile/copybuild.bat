@echo off
setlocal enabledelayedexpansion

REM ==== Set defaults for configuration and platform ====

IF "%CONFIG%"=="" (
    set "CONFIG=%1"
)
IF "%CONFIG%"=="" (
    REM Default to Debug if not provided
    set "CONFIG=Debug"
)

set "PLATFORM=x64"
set "TARGETFRAMEWORK=net8.0-windows"

REM ==== Set the correct relative paths based on batch file location ====
set "SRC=%~dp0PlugIns"
set "DEST=%~dp0bin\%PLATFORM%\%CONFIG%\%TARGETFRAMEWORK%\PlugIns"

REM ==== Create DEST if it doesn't exist ====
if not exist "%DEST%" (
    echo [INFO] Destination directory "%DEST%" doesn't exist. Creating it...
    mkdir "%DEST%"
)

echo [DEBUG] SRC  = "%SRC%"
echo [DEBUG] DEST = "%DEST%"

REM ==== Check if SRC exists ====
if not exist "%SRC%" (
    echo [ERROR] Source directory "%SRC%" does not exist.
    exit /b 1
)

REM ==== Copy folders and files recursively ====
echo [INFO] Copying folders and files from "%SRC%" to "%DEST%"

xcopy "%SRC%\*" "%DEST%\" /E /I /Y /Q >nul

if errorlevel 1 (
    echo [ERROR] Copy failed.
    exit /b 1
)

echo [DONE] Copy complete.

endlocal
