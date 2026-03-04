@echo off
setlocal

call "%~dp0config.bat"

set BUILD_CONFIG=Release
if /I "%~1"=="debug" set BUILD_CONFIG=Debug

echo Compiling with dotnet... (%BUILD_CONFIG%)
dotnet build Elin_NiComment.csproj -c %BUILD_CONFIG%


if %ERRORLEVEL% NEQ 0 (
    echo Build Failed!
    exit /b 1
)

echo Build Successful!
echo Copying to Package folder...
xcopy "%~dp0_bin\Elin_NiComment.dll" "%~dp0elin_link\Package\Elin_NiComment\" /Y
xcopy "%~dp0package.xml" "%~dp0elin_link\Package\Elin_NiComment\" /Y

echo Copying to Steam game folder...
set STEAM_PACKAGE_DIR="C:\Program Files (x86)\Steam\steamapps\common\Elin\Package\Elin_NiComment"
if not exist %STEAM_PACKAGE_DIR% mkdir %STEAM_PACKAGE_DIR%
xcopy "%~dp0_bin\Elin_NiComment.dll" %STEAM_PACKAGE_DIR% /Y
xcopy "%~dp0package.xml" %STEAM_PACKAGE_DIR% /Y

endlocal
