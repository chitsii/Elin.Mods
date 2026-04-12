@echo off
setlocal

call "%~dp0config.bat"

set MOD_NAME=Elin_Elinikki
set CONFIG=Release
if /I "%~1"=="debug" set CONFIG=Debug

echo Compiling %MOD_NAME% (%CONFIG%)...
dotnet build "%~dp0%MOD_NAME%.csproj" -c %CONFIG%

if %ERRORLEVEL% NEQ 0 (
    echo Build Failed!
    exit /b 1
)

echo Build Successful!
echo Deploying to Package folder...

set DEPLOY_DIR=%ELIN_DIR%\Package\%MOD_NAME%
if not exist "%DEPLOY_DIR%" mkdir "%DEPLOY_DIR%"

xcopy "%~dp0_bin\%MOD_NAME%.dll" "%DEPLOY_DIR%\" /Y
xcopy "%~dp0package.xml" "%DEPLOY_DIR%\" /Y
if exist "%~dp0preview.jpg" xcopy "%~dp0preview.jpg" "%DEPLOY_DIR%\" /Y

echo Done.
endlocal
