@echo off
setlocal

set MOD_NAME=Elin_ArsMoriendi
set STEAM_ELIN_DIR=C:\Program Files (x86)\Steam\steamapps\common\Elin
set BUILD_CONFIG=Release
set REGEN_KEYS=0

for %%A in (%*) do (
    if /I "%%~A"=="debug" set BUILD_CONFIG=Debug
    if /I "%%~A"=="regen" set REGEN_KEYS=1
)

echo ============================================
echo  Building %MOD_NAME%
echo ============================================

rem --- Step 0: Validation tests (fail fast) ---
echo.
echo [Step 0.0] Validating generated key interfaces...
if %REGEN_KEYS% EQU 1 (
    python "%~dp0tools\drama\schema\generate_keys.py" --write
) else (
    python "%~dp0tools\drama\schema\generate_keys.py" --check
)
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Key interface generation/check failed!
    exit /b 1
)

echo [Step 0.1] Running drama tooling tests...
python -m unittest discover -s "%~dp0tools\drama\tests" -t "%~dp0." -v
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Drama tooling tests failed!
    exit /b 1
)

echo [Step 0.2] Running C# unit tests...
dotnet test "%~dp0tests\Elin_ArsMoriendi.Tests\Elin_ArsMoriendi.Tests.csproj" -nologo
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] C# unit tests failed!
    exit /b 1
)

rem --- Step 1: Generate Excel data from Python definitions ---
echo.
echo [Step 1] Generating SourceThing TSV...
python "%~dp0tools\builder\create_thing_excel.py"
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] Thing TSV generation failed - continuing build
)

echo [Step 1.5] Generating SourceChara TSV...
python "%~dp0tools\builder\create_chara_excel.py"
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] Chara TSV generation failed - continuing build
)

echo [Step 2] Generating SourceElement TSV...
python "%~dp0tools\builder\create_element_excel.py"
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] Element TSV generation failed - continuing build
)

echo [Step 3] Generating SourceStat TSV...
python "%~dp0tools\builder\create_stat_excel.py"
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] Stat TSV generation failed - continuing build
)

echo [Step 3.7] Generating SourceQuest TSV...
python "%~dp0tools\builder\create_quest_excel.py"
if %ERRORLEVEL% NEQ 0 (
    echo [WARN] Quest TSV generation failed - continuing build
)

echo [Step 3.5] Generating Drama Excel...
python "%~dp0tools\drama\create_drama_excel.py"
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Drama Excel generation failed!
    exit /b 1
)

rem --- Step 4: Convert TSV to XLSX ---
echo.
echo [Step 4] Converting TSV to XLSX...
set SOFFICE="C:\Program Files\LibreOffice\program\soffice.exe"
if not exist %SOFFICE% (
    set SOFFICE="C:\Program Files (x86)\LibreOffice\program\soffice.exe"
)

for %%L in (JP EN CN) do (
    if exist "%~dp0LangMod\%%L" (
        for %%F in ("%~dp0LangMod\%%L\*.tsv") do (
            echo   Converting %%~nxF [%%L]...
            %SOFFICE% --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" --outdir "%~dp0LangMod\%%L" "%%F" >nul 2>&1
            if %ERRORLEVEL% NEQ 0 (
                echo   [WARN] Failed to convert %%~nxF
            )
        )
    )
)

rem --- Step 4.5: Validate Excel headers against vanilla ---
echo.
echo [Step 4.5] Validating Excel headers...
python "%~dp0tools\builder\validate_source_headers.py" compare -q
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Excel header validation failed!
    echo Run: python tools\builder\validate_source_headers.py compare
    echo for details, or: python tools\builder\validate_source_headers.py sync
    echo to update the vanilla cache.
    exit /b 1
)

rem --- Step 5: Build C# ---
echo.
echo [Step 5] Building C#...
dotnet build "%~dp0%MOD_NAME%.csproj" -c %BUILD_CONFIG%
if %ERRORLEVEL% NEQ 0 (
    echo Build Failed!
    exit /b 1
)

echo Build Successful!

rem --- Step 6: Deploy ---
echo.
echo [Step 6] Deploying...

set STEAM_PACKAGE_DIR=%STEAM_ELIN_DIR%\Package\%MOD_NAME%
if not exist "%STEAM_PACKAGE_DIR%" mkdir "%STEAM_PACKAGE_DIR%"

echo Copying DLL, package.xml, and preview.jpg...
xcopy "%~dp0_bin\%MOD_NAME%.dll" "%STEAM_PACKAGE_DIR%\" /Y
xcopy "%~dp0package.xml" "%STEAM_PACKAGE_DIR%\" /Y
if exist "%~dp0preview.jpg" xcopy "%~dp0preview.jpg" "%STEAM_PACKAGE_DIR%\" /Y

echo Copying LangMod...
if exist "%~dp0LangMod" (
    xcopy "%~dp0LangMod" "%STEAM_PACKAGE_DIR%\LangMod\" /E /Y /I
)

echo Copying Portrait...
if exist "%~dp0Portrait" (
    xcopy "%~dp0Portrait" "%STEAM_PACKAGE_DIR%\Portrait\" /E /Y /I
)

echo Copying Texture...
if exist "%~dp0Texture" (
    xcopy "%~dp0Texture" "%STEAM_PACKAGE_DIR%\Texture\" /E /Y /I
)

echo Copying Sound...
if exist "%~dp0Sound" (
    xcopy "%~dp0Sound" "%STEAM_PACKAGE_DIR%\Sound\" /E /Y /I
)

echo Copying Asset...
if exist "%~dp0Asset" (
    xcopy "%~dp0Asset" "%STEAM_PACKAGE_DIR%\Asset\" /E /Y /I
)

echo.
echo ============================================
echo  Build complete: %MOD_NAME%
echo ============================================

endlocal
