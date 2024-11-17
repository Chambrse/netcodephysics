@echo off
setlocal

:: Define paths
set UNITY_PATH="C:\Path\To\Unity\Editor\Unity.exe"  :: Adjust this path
set PROJECT_PATH=%cd%
set LOG_DIR=%PROJECT_PATH%\BuildLogs
set CLIENT_BUILD_PATH=%PROJECT_PATH%\Builds\Client
set SERVER_BUILD_PATH=%PROJECT_PATH%\Builds\Server

:: Create directories if they don't exist
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"
if not exist "%CLIENT_BUILD_PATH%" mkdir "%CLIENT_BUILD_PATH%"
if not exist "%SERVER_BUILD_PATH%" mkdir "%SERVER_BUILD_PATH%"

:: Build function
:BuildUnity
set BUILD_TYPE=%1
set LOG_FILE=%2
%UNITY_PATH% -batchmode -quit -projectPath "%PROJECT_PATH%" -executeMethod "CommandLineBuildScript.%BUILD_TYPE%" -logFile "%LOG_FILE%"
exit /b

:: Parse arguments
if "%1" == "client" (
    echo Building client...
    call :BuildUnity BuildClient "%LOG_DIR%\ClientBuildLog.txt"
    echo Client build completed.
) else if "%1" == "server" (
    echo Building server...
    call :BuildUnity BuildServer "%LOG_DIR%\ServerBuildLog.txt"
    echo Server build completed.
) else (
    echo Usage: build.bat [client^|server]
    exit /b 1
)

endlocal
