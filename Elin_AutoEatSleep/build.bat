@echo off
setlocal

set "BUILD_ROOT=%~dp0..\.codex-build\Elin_AutoEatSleep"
set "DOTNET_CLI_HOME=%BUILD_ROOT%\dotnet-home"
set "NUGET_PACKAGES=%BUILD_ROOT%\nuget-packages"
set "TEMP=%BUILD_ROOT%\tmp"
set "TMP=%BUILD_ROOT%\tmp"

if not exist "%DOTNET_CLI_HOME%" mkdir "%DOTNET_CLI_HOME%"
if not exist "%NUGET_PACKAGES%" mkdir "%NUGET_PACKAGES%"
if not exist "%TEMP%" mkdir "%TEMP%"

echo Building Elin_AutoEatSleep without deploying to game folders...
dotnet build "%~dp0src\Elin_AutoEatSleep.csproj" -c Release -p:BaseIntermediateOutputPath=%BUILD_ROOT%\obj-base\ -p:MSBuildProjectExtensionsPath=%BUILD_ROOT%\obj-base\ %*
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo Build output is under %BUILD_ROOT%\out\Release\
echo To deploy explicitly, run: dotnet msbuild "%~dp0src\Elin_AutoEatSleep.csproj" /t:Deploy /p:DeployToGame=true

endlocal
