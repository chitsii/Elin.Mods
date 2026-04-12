@echo off

rem Set your Elin game directory here
set ELIN_DIR=C:\Program Files (x86)\Steam\steamapps\common\Elin

rem Set reference paths
set REFERENCES=%ELIN_DIR%\Elin_Data\Managed
set BEPINEX=%ELIN_DIR%\BepInEx\core

rem Create symbolic link if it doesn't exist
if not exist "%~dp0elin_link" (
    echo Creating junction to Elin folder...
    mklink /J "%~dp0elin_link" "%ELIN_DIR%"
)
