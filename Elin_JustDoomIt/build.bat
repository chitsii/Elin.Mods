@echo off
setlocal

call "%~dp0config.bat"

echo Generating CWL Thing TSV...
python "%~dp0tools\builder\create_thing_excel.py"
if %ERRORLEVEL% NEQ 0 (
    echo Thing TSV generation failed!
    exit /b 1
)

echo Converting TSV to XLSX with soffice...
set "SOFFICE_EXE=C:\Program Files\LibreOffice\program\soffice.exe"
if not exist "%SOFFICE_EXE%" (
    set "SOFFICE_EXE=C:\Program Files (x86)\LibreOffice\program\soffice.exe"
)
if not exist "%SOFFICE_EXE%" (
    echo [ERROR] soffice.exe not found. Install LibreOffice.
    exit /b 1
)

for %%L in (EN JP CN) do (
    if exist "%~dp0LangMod\%%L\Thing.tsv" (
        "%SOFFICE_EXE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" --outdir "%~dp0LangMod\%%L" "%~dp0LangMod\%%L\Thing.tsv" >nul 2>&1
        if errorlevel 1 (
            echo [ERROR] soffice conversion failed for %%L\Thing.tsv
            exit /b 1
        )
        if not exist "%~dp0LangMod\%%L\Thing.xlsx" (
            echo [ERROR] Thing.xlsx not generated for %%L
            exit /b 1
        )
    ) else (
        echo [ERROR] Missing TSV: LangMod\%%L\Thing.tsv
        exit /b 1
    )
)

echo Compiling with dotnet...
dotnet build Elin_ModTemplate.csproj -c Release


if %ERRORLEVEL% NEQ 0 (
    echo Build Failed!
    exit /b 1
)

echo Build Successful!
echo Copying to Package folder...

xcopy "%~dp0_bin\Elin_JustDoomIt.dll" "%~dp0elin_link\Package\Elin_JustDoomIt\" /Y
xcopy "%~dp0_bin\DoomNetFrameworkEngine.dll" "%~dp0elin_link\Package\Elin_JustDoomIt\" /Y
xcopy "%~dp0package.xml" "%~dp0elin_link\Package\Elin_JustDoomIt\" /Y
if exist "%~dp0LangMod" xcopy "%~dp0LangMod" "%~dp0elin_link\Package\Elin_JustDoomIt\LangMod\" /E /I /Y
if exist "%~dp0Sound" xcopy "%~dp0Sound" "%~dp0elin_link\Package\Elin_JustDoomIt\Sound\" /E /I /Y
if exist "%~dp0LangMod\EN\SourceThing.xlsx" del /f /q "%~dp0LangMod\EN\SourceThing.xlsx"
if exist "%~dp0LangMod\JP\SourceThing.xlsx" del /f /q "%~dp0LangMod\JP\SourceThing.xlsx"
if exist "%~dp0LangMod\CN\SourceThing.xlsx" del /f /q "%~dp0LangMod\CN\SourceThing.xlsx"
if exist "%~dp0elin_link\Package\Elin_JustDoomIt\LangMod\EN\SourceThing.xlsx" del /f /q "%~dp0elin_link\Package\Elin_JustDoomIt\LangMod\EN\SourceThing.xlsx"
if exist "%~dp0elin_link\Package\Elin_JustDoomIt\LangMod\JP\SourceThing.xlsx" del /f /q "%~dp0elin_link\Package\Elin_JustDoomIt\LangMod\JP\SourceThing.xlsx"
if exist "%~dp0elin_link\Package\Elin_JustDoomIt\LangMod\CN\SourceThing.xlsx" del /f /q "%~dp0elin_link\Package\Elin_JustDoomIt\LangMod\CN\SourceThing.xlsx"
if not exist "%~dp0elin_link\Package\Elin_JustDoomIt\wad" mkdir "%~dp0elin_link\Package\Elin_JustDoomIt\wad"
if exist "%~dp0wad\freedoom1.wad" xcopy "%~dp0wad\freedoom1.wad" "%~dp0elin_link\Package\Elin_JustDoomIt\wad\" /Y

echo Copying to Steam game folder...
set STEAM_PACKAGE_DIR="C:\Program Files (x86)\Steam\steamapps\common\Elin\Package\Elin_JustDoomIt"
if not exist %STEAM_PACKAGE_DIR% mkdir %STEAM_PACKAGE_DIR%

xcopy "%~dp0_bin\Elin_JustDoomIt.dll" %STEAM_PACKAGE_DIR% /Y
xcopy "%~dp0_bin\DoomNetFrameworkEngine.dll" %STEAM_PACKAGE_DIR% /Y
xcopy "%~dp0package.xml" %STEAM_PACKAGE_DIR% /Y
if exist "%~dp0LangMod" xcopy "%~dp0LangMod" %STEAM_PACKAGE_DIR%\LangMod\ /E /I /Y
if exist "%~dp0Sound" xcopy "%~dp0Sound" %STEAM_PACKAGE_DIR%\Sound\ /E /I /Y
if exist %STEAM_PACKAGE_DIR%\LangMod\EN\SourceThing.xlsx del /f /q %STEAM_PACKAGE_DIR%\LangMod\EN\SourceThing.xlsx
if exist %STEAM_PACKAGE_DIR%\LangMod\JP\SourceThing.xlsx del /f /q %STEAM_PACKAGE_DIR%\LangMod\JP\SourceThing.xlsx
if exist %STEAM_PACKAGE_DIR%\LangMod\CN\SourceThing.xlsx del /f /q %STEAM_PACKAGE_DIR%\LangMod\CN\SourceThing.xlsx
if not exist %STEAM_PACKAGE_DIR%\wad mkdir %STEAM_PACKAGE_DIR%\wad
if exist "%~dp0wad\freedoom1.wad" xcopy "%~dp0wad\freedoom1.wad" %STEAM_PACKAGE_DIR%\wad\ /Y

endlocal
