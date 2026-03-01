@echo off
setlocal

call "%~dp0config.bat"

echo Compiling with dotnet...
dotnet build Elin_RapidFireMagic.csproj -c Release


if %ERRORLEVEL% NEQ 0 (
    echo Build Failed!
    exit /b 1
)

echo Build Successful!
echo Copying to Package folder...
xcopy "%~dp0_bin\Elin_RapidFireMagic.dll" "%~dp0elin_link\Package\Elin_RapidFireMagic\" /Y
xcopy "%~dp0package.xml" "%~dp0elin_link\Package\Elin_RapidFireMagic\" /Y

echo Copying to Steam game folder...
set STEAM_PACKAGE_DIR="C:\Program Files (x86)\Steam\steamapps\common\Elin\Package\Elin_RapidFireMagic"
if not exist %STEAM_PACKAGE_DIR% mkdir %STEAM_PACKAGE_DIR%
xcopy "%~dp0_bin\Elin_RapidFireMagic.dll" %STEAM_PACKAGE_DIR% /Y
xcopy "%~dp0package.xml" %STEAM_PACKAGE_DIR% /Y

endlocal
