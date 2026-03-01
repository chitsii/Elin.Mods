@echo off
setlocal

call "%~dp0config.bat"

set BUILD_CONFIG=Debug
set DRAMA_ONLY=0
for %%A in (%*) do (
    if /I "%%~A"=="release" set BUILD_CONFIG=Release
    if /I "%%~A"=="debug" set BUILD_CONFIG=Debug
    if /I "%%~A"=="drama" set DRAMA_ONLY=1
)

echo [1/6] Generate drama key interfaces...
python "%~dp0tools\drama\schema\generate_keys.py" --write
if %ERRORLEVEL% NEQ 0 (
    echo Drama key generation failed.
    exit /b 1
)

echo [2/6] Run drama tooling tests...
python -m unittest discover -s "%~dp0tools\drama\tests" -t "%~dp0." -v
if %ERRORLEVEL% NEQ 0 (
    echo Drama tooling tests failed.
    exit /b 1
)

echo [3/6] Generate drama Excel...
python "%~dp0tools\drama\create_drama_excel.py"
if %ERRORLEVEL% NEQ 0 (
    echo Drama Excel generation failed.
    exit /b 1
)

if "%DRAMA_ONLY%"=="1" goto :DRAMA_ONLY_DEPLOY
goto :BUILD_DLL

:DRAMA_ONLY_DEPLOY
if not exist "%~dp0LangMod" (
    echo LangMod folder not found after drama generation.
    exit /b 1
)

echo [4/4] Drama-only deploy (LangMod)...
set LINK_PACKAGE_DIR=%~dp0elin_link\Package\%MOD_FOLDER%
if not exist "%LINK_PACKAGE_DIR%" mkdir "%LINK_PACKAGE_DIR%"
xcopy "%~dp0LangMod" "%LINK_PACKAGE_DIR%\LangMod\" /E /Y /I >nul
if exist "%~dp0Texture" (
    xcopy "%~dp0Texture" "%LINK_PACKAGE_DIR%\Texture\" /E /Y /I >nul
)
if exist "%~dp0Portrait" (
    xcopy "%~dp0Portrait" "%LINK_PACKAGE_DIR%\Portrait\" /E /Y /I >nul
)
if exist "%~dp0Sound" (
    xcopy "%~dp0Sound" "%LINK_PACKAGE_DIR%\Sound\" /E /Y /I >nul
)

if not exist "%STEAM_PACKAGE_DIR%" mkdir "%STEAM_PACKAGE_DIR%"
xcopy "%~dp0LangMod" "%STEAM_PACKAGE_DIR%\LangMod\" /E /Y /I >nul
if exist "%~dp0Texture" (
    xcopy "%~dp0Texture" "%STEAM_PACKAGE_DIR%\Texture\" /E /Y /I >nul
)
if exist "%~dp0Portrait" (
    xcopy "%~dp0Portrait" "%STEAM_PACKAGE_DIR%\Portrait\" /E /Y /I >nul
)
if exist "%~dp0Sound" (
    xcopy "%~dp0Sound" "%STEAM_PACKAGE_DIR%\Sound\" /E /Y /I >nul
)
if %ERRORLEVEL% NEQ 0 (
    echo Drama-only deploy to Steam package failed.
    exit /b 1
)

echo Drama-only deploy complete.
endlocal
exit /b 0

:BUILD_DLL

echo [4/6] dotnet build (%BUILD_CONFIG%)...
dotnet build "%~dp0Elin_QuestMod.csproj" -c %BUILD_CONFIG%
if %ERRORLEVEL% NEQ 0 (
    echo Build failed.
    exit /b 1
)

echo [5/6] Copy to linked package...
set LINK_PACKAGE_DIR=%~dp0elin_link\Package\%MOD_FOLDER%
if not exist "%LINK_PACKAGE_DIR%" mkdir "%LINK_PACKAGE_DIR%"
copy /Y "%~dp0_bin\%MOD_DLL%" "%LINK_PACKAGE_DIR%\%MOD_DLL%" >nul
copy /Y "%~dp0package.xml" "%LINK_PACKAGE_DIR%\package.xml" >nul
if exist "%~dp0LangMod" (
    xcopy "%~dp0LangMod" "%LINK_PACKAGE_DIR%\LangMod\" /E /Y /I >nul
)
if exist "%~dp0Texture" (
    xcopy "%~dp0Texture" "%LINK_PACKAGE_DIR%\Texture\" /E /Y /I >nul
)
if exist "%~dp0Portrait" (
    xcopy "%~dp0Portrait" "%LINK_PACKAGE_DIR%\Portrait\" /E /Y /I >nul
)
if exist "%~dp0Sound" (
    xcopy "%~dp0Sound" "%LINK_PACKAGE_DIR%\Sound\" /E /Y /I >nul
)

echo [6/6] Copy to Steam package...
if not exist "%STEAM_PACKAGE_DIR%" mkdir "%STEAM_PACKAGE_DIR%"
copy /Y "%~dp0_bin\%MOD_DLL%" "%STEAM_PACKAGE_DIR%\%MOD_DLL%" >nul
copy /Y "%~dp0package.xml" "%STEAM_PACKAGE_DIR%\package.xml" >nul
if exist "%~dp0LangMod" (
    xcopy "%~dp0LangMod" "%STEAM_PACKAGE_DIR%\LangMod\" /E /Y /I >nul
)
if exist "%~dp0Texture" (
    xcopy "%~dp0Texture" "%STEAM_PACKAGE_DIR%\Texture\" /E /Y /I >nul
)
if exist "%~dp0Portrait" (
    xcopy "%~dp0Portrait" "%STEAM_PACKAGE_DIR%\Portrait\" /E /Y /I >nul
)
if exist "%~dp0Sound" (
    xcopy "%~dp0Sound" "%STEAM_PACKAGE_DIR%\Sound\" /E /Y /I >nul
)
if %ERRORLEVEL% NEQ 0 (
    echo Copy to Steam package failed. Game may be running and DLL locked.
    exit /b 1
)

echo Build complete.
endlocal
