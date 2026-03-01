@echo off
setlocal EnableDelayedExpansion

set MOD_NAME=Elin_SukutsuArena

REM 環境変数が未設定ならデフォルト値を使用
if not defined SOFFICE set "SOFFICE=C:\Program Files\LibreOffice\program\soffice.exe"
if not defined STEAM_PACKAGE_DIR set "STEAM_PACKAGE_DIR=C:\Program Files (x86)\Steam\steamapps\common\Elin\Package\%MOD_NAME%"

REM ビルド構成の設定（引数で debug を指定するとDebugビルド）
set BUILD_CONFIG=Release
if /i "%~1"=="debug" set BUILD_CONFIG=Debug

echo.
if "%BUILD_CONFIG%"=="Debug" (
    echo === %MOD_NAME% Build [DEBUG] ===
    echo   * Enemy levels fixed to 1 ^(only with --boss-lv1^)
    echo   * Debug keys enabled: F9/F11/F12
) else (
    echo === %MOD_NAME% Build [Release] ===
)
echo.

REM ============================================================
REM Step 0: Backup Excel files for diff
REM ============================================================
pushd tools
uv run python builder/excel_diff_manager.py backup >nul 2>&1
popd

REM Ensure CN directory exists
if not exist "%~dp0LangMod\CN" mkdir "%~dp0LangMod\CN" >nul 2>&1

REM ============================================================
REM Step 1: Validation
REM ============================================================
<nul set /p "=[1/15] Validation... "
pushd tools
uv run python -c "from arena.validation import run_all_validations; import sys; sys.exit(0 if run_all_validations() else 1)" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    echo.
    echo ============================================================
    echo VALIDATION ERROR - Details:
    echo ============================================================
    uv run python -c "from arena.validation import run_all_validations; run_all_validations()"
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 1.5: Dependency Extraction
REM ============================================================
<nul set /p "=[1.5/15] Dependency Check... "
pushd tools
uv run python builder/extract_dependencies.py -q
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
uv run python builder/verify_game_api.py --ci >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    echo.
    echo ============================================================
    echo GAME API CHECK ERROR - Details:
    echo ============================================================
    uv run python builder/verify_game_api.py --ci
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 2: Zone Excel (EN + CN)
REM ============================================================
<nul set /p "=[2/15] Zone Excel... "
pushd tools
uv run python builder/create_zone_excel.py >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
REM EN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\EN\Zone.tsv" --outdir "%~dp0LangMod\EN" >nul 2>&1
if not exist "%~dp0LangMod\EN\Zone.xlsx" (
    echo FAILED - LibreOffice conversion failed
    goto :error
)
move /Y "%~dp0LangMod\EN\Zone.xlsx" "%~dp0LangMod\EN\SourceSukutsu.xlsx" >nul 2>&1
del "%~dp0LangMod\EN\Zone.tsv" >nul 2>&1
REM CN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\CN\Zone.tsv" --outdir "%~dp0LangMod\CN" >nul 2>&1
move /Y "%~dp0LangMod\CN\Zone.xlsx" "%~dp0LangMod\CN\SourceSukutsu.xlsx" >nul 2>&1
del "%~dp0LangMod\CN\Zone.tsv" >nul 2>&1
echo OK

REM ============================================================
REM Step 3: Chara Excel (EN + CN)
REM ============================================================
<nul set /p "=[3/15] Chara Excel... "
pushd tools
if "%BUILD_CONFIG%"=="Debug" (
    set BOSS_LV1_FLAG=
    for %%A in (%*) do (
        if /i "%%A"=="--boss-lv1" set BOSS_LV1_FLAG=--boss-lv1
    )
    uv run python builder/create_chara_excel.py --debug !BOSS_LV1_FLAG! >nul 2>&1
) else (
    uv run python builder/create_chara_excel.py >nul 2>&1
)
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
REM EN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\EN\Chara.tsv" --outdir "%~dp0LangMod\EN" >nul 2>&1
move /Y "%~dp0LangMod\EN\Chara.xlsx" "%~dp0LangMod\EN\SourceChara.xlsx" >nul 2>&1
del "%~dp0LangMod\EN\Chara.tsv" >nul 2>&1
REM CN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\CN\Chara.tsv" --outdir "%~dp0LangMod\CN" >nul 2>&1
move /Y "%~dp0LangMod\CN\Chara.xlsx" "%~dp0LangMod\CN\SourceChara.xlsx" >nul 2>&1
del "%~dp0LangMod\CN\Chara.tsv" >nul 2>&1
echo OK

REM ============================================================
REM Step 4: Thing Excel (EN + CN)
REM ============================================================
<nul set /p "=[4/15] Thing Excel... "
pushd tools
if "%BUILD_CONFIG%"=="Debug" (
    uv run python builder/create_thing_excel.py --debug >nul 2>&1
) else (
    uv run python builder/create_thing_excel.py >nul 2>&1
)
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
REM EN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\EN\Thing.tsv" --outdir "%~dp0LangMod\EN" >nul 2>&1
move /Y "%~dp0LangMod\EN\Thing.xlsx" "%~dp0LangMod\EN\SourceThing.xlsx" >nul 2>&1
del "%~dp0LangMod\EN\Thing.tsv" >nul 2>&1
REM CN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\CN\Thing.tsv" --outdir "%~dp0LangMod\CN" >nul 2>&1
move /Y "%~dp0LangMod\CN\Thing.xlsx" "%~dp0LangMod\CN\SourceThing.xlsx" >nul 2>&1
del "%~dp0LangMod\CN\Thing.tsv" >nul 2>&1
echo OK

REM ============================================================
REM Step 5: Merchant Stock JSON
REM ============================================================
<nul set /p "=[5/15] Merchant Stock... "
pushd tools
uv run python builder/create_merchant_stock.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 6: Element Excel (EN + CN)
REM ============================================================
<nul set /p "=[6/15] Element Excel... "
pushd tools
uv run python builder/create_element_excel.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
REM EN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\EN\Element.tsv" --outdir "%~dp0LangMod\EN" >nul 2>&1
move /Y "%~dp0LangMod\EN\Element.xlsx" "%~dp0LangMod\EN\SourceElement.xlsx" >nul 2>&1
del "%~dp0LangMod\EN\Element.tsv" >nul 2>&1
REM CN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\CN\Element.tsv" --outdir "%~dp0LangMod\CN" >nul 2>&1
move /Y "%~dp0LangMod\CN\Element.xlsx" "%~dp0LangMod\CN\SourceElement.xlsx" >nul 2>&1
del "%~dp0LangMod\CN\Element.tsv" >nul 2>&1
echo OK

REM ============================================================
REM Step 7: Stat Excel (EN + CN)
REM ============================================================
<nul set /p "=[7/15] Stat Excel... "
pushd tools
uv run python builder/create_stat_excel.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
REM EN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\EN\Stat.tsv" --outdir "%~dp0LangMod\EN" >nul 2>&1
move /Y "%~dp0LangMod\EN\Stat.xlsx" "%~dp0LangMod\EN\SourceStat.xlsx" >nul 2>&1
del "%~dp0LangMod\EN\Stat.tsv" >nul 2>&1
REM CN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\CN\Stat.tsv" --outdir "%~dp0LangMod\CN" >nul 2>&1
move /Y "%~dp0LangMod\CN\Stat.xlsx" "%~dp0LangMod\CN\SourceStat.xlsx" >nul 2>&1
del "%~dp0LangMod\CN\Stat.tsv" >nul 2>&1
echo OK

REM ============================================================
REM Step 7.5: Quest Excel (EN + CN)
REM ============================================================
<nul set /p "=[7.5/15] Quest Excel... "
pushd tools
uv run python builder/create_quest_excel.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
REM EN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\EN\Quest.tsv" --outdir "%~dp0LangMod\EN" >nul 2>&1
move /Y "%~dp0LangMod\EN\Quest.xlsx" "%~dp0LangMod\EN\SourceQuest.xlsx" >nul 2>&1
del "%~dp0LangMod\EN\Quest.tsv" >nul 2>&1
REM CN版
"%SOFFICE%" --headless --convert-to "xlsx:Calc MS Excel 2007 XML" --infilter="CSV:9,34,76" "%~dp0LangMod\CN\Quest.tsv" --outdir "%~dp0LangMod\CN" >nul 2>&1
move /Y "%~dp0LangMod\CN\Quest.xlsx" "%~dp0LangMod\CN\SourceQuest.xlsx" >nul 2>&1
del "%~dp0LangMod\CN\Quest.tsv" >nul 2>&1
echo OK

REM ============================================================
REM Step 8: BGM JSON
REM ============================================================
<nul set /p "=[8/15] BGM JSON... "
pushd tools
uv run python builder/create_bgm_json.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 9: Drama Excel (all languages in single file)
REM ============================================================
<nul set /p "=[9/15] Drama Excel... "
pushd tools
REM -W default enables DeprecationWarning display, >nul hides stdout but keeps stderr
uv run python -W default builder/create_drama_excel.py >nul
set DRAMA_ERROR=!ERRORLEVEL!
if !DRAMA_ERROR! NEQ 0 (
    echo FAILED
    echo.
    echo [Drama Excel Error Details:]
    uv run python -W default builder/create_drama_excel.py
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 9.3: Dialog Excel (Unique sheet for NPC random talk)
REM ============================================================
<nul set /p "=[9.3/15] Dialog Excel... "
pushd tools
uv run python builder/create_dialog_excel.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 9.5: Source Excel Header Validation (uses JSON cache for speed)
REM ============================================================
<nul set /p "=[9.5/15] Source Excel Validation... "
pushd tools
uv run python builder/extract_source_defaults.py compare -q >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    echo.
    echo ============================================================
    echo SOURCE EXCEL VALIDATION ERROR - Details:
    echo ============================================================
    uv run python builder/extract_source_defaults.py compare
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 10: C# Flags + Quest Data
REM ============================================================
<nul set /p "=[10/15] C# Flags... "
pushd tools
uv run python builder/generate_flags.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
uv run python builder/generate_jump_label_mapping.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
uv run python builder/generate_enum_mappings.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
uv run python builder/validate_scenario_flags.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    echo.
    echo ============================================================
    echo FLAG VALIDATION ERROR - Details:
    echo ============================================================
    uv run python builder/validate_scenario_flags.py
    popd
    goto :error
)
uv run python builder/generate_quest_data.py >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 11: Config JSON (Quest + Battle Stages)
REM ============================================================
<nul set /p "=[11/15] Config JSON... "
pushd tools
uv run python -c "from arena.builders import export_quests_to_json; import os; export_quests_to_json(os.path.join('..', 'Package', 'quest_definitions.json'))" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
REM DEBUGビルドの場合は全敵レベル1に設定
if "%BUILD_CONFIG%"=="Debug" (
    uv run python -c "from arena.data import export_stages_to_json; import os; export_stages_to_json(os.path.join('..', 'Package', 'battle_stages.json'), debug_mode=True)" >nul 2>&1
) else (
    uv run python -c "from arena.data import export_stages_to_json; import os; export_stages_to_json(os.path.join('..', 'Package', 'battle_stages.json'), debug_mode=False)" >nul 2>&1
)
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 12: Contract Validation (JSON)
REM ============================================================
<nul set /p "=[12/15] Contract Validation... "
pushd tools
uv run python -c "from cwl_quest_lib.contracts import validate_quest_json; validate_quest_json('../Package/quest_definitions.json')" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    echo.
    echo ============================================================
    echo CONTRACT VALIDATION ERROR - Details:
    echo ============================================================
    uv run python -c "from cwl_quest_lib.contracts import validate_quest_json; validate_quest_json('../Package/quest_definitions.json')"
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 13: C# Type Validation
REM ============================================================
<nul set /p "=[13/15] C# Type Check... "
pushd tools
uv run python -c "from cwl_quest_lib.contracts import validate_quest_data_class, CSharpContractMismatchError; m = validate_quest_data_class('..'); exit(1) if m else exit(0)" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    echo.
    echo ============================================================
    echo C# TYPE MISMATCH ERROR - Details:
    echo ============================================================
    uv run python -c "from cwl_quest_lib.contracts import validate_quest_data_class, CSharpContractMismatchError; m = validate_quest_data_class('..'); print(CSharpContractMismatchError(m, 'src/ArenaQuestManager.cs', 'QuestData')) if m else print('OK')"
    popd
    goto :error
)
popd
echo OK

REM ============================================================
REM Step 14: dotnet build
REM ============================================================
<nul set /p "=[14/15] dotnet build (%BUILD_CONFIG%)... "
dotnet build "%~dp0%MOD_NAME%.csproj" -c %BUILD_CONFIG% -v q --nologo >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo FAILED
    echo.
    echo ============================================================
    echo BUILD ERROR - Details:
    echo ============================================================
    dotnet build "%~dp0%MOD_NAME%.csproj" -c %BUILD_CONFIG%
    goto :error
)
echo OK

REM ============================================================
REM Step 15: Deploy
REM ============================================================
<nul set /p "=[15/15] Deploy... "

REM elin_link is now a junction to the game directory, so deploy only via STEAM_PACKAGE_DIR.

REM Steam folder
if not exist "%STEAM_PACKAGE_DIR%" mkdir "%STEAM_PACKAGE_DIR%" >nul 2>&1
xcopy "%~dp0_bin\%MOD_NAME%.dll" "%STEAM_PACKAGE_DIR%\" /Y /Q >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo FAILED - DLL copy to Steam failed
    goto :error
)
xcopy "%~dp0package.xml" "%STEAM_PACKAGE_DIR%\" /Y /Q >nul 2>&1
xcopy "%~dp0preview.jpg" "%STEAM_PACKAGE_DIR%\" /Y /Q >nul 2>&1
xcopy "%~dp0Package\quest_definitions.json" "%STEAM_PACKAGE_DIR%\Package\" /Y /I /Q >nul 2>&1
xcopy "%~dp0Package\battle_stages.json" "%STEAM_PACKAGE_DIR%\Package\" /Y /I /Q >nul 2>&1
xcopy "%~dp0LangMod" "%STEAM_PACKAGE_DIR%\LangMod\" /E /Y /I /Q >nul 2>&1
xcopy "%~dp0Texture" "%STEAM_PACKAGE_DIR%\Texture\" /E /Y /I /Q >nul 2>&1
xcopy "%~dp0Portrait" "%STEAM_PACKAGE_DIR%\Portrait\" /E /Y /I /Q >nul 2>&1
xcopy "%~dp0Media" "%STEAM_PACKAGE_DIR%\Media\" /E /Y /I /Q >nul 2>&1
xcopy "%~dp0Sound" "%STEAM_PACKAGE_DIR%\Sound\" /E /Y /I /Q >nul 2>&1

REM Critical file: SourceSukutsu.xlsx - use copy command explicitly
copy /Y "%~dp0LangMod\EN\SourceSukutsu.xlsx" "%STEAM_PACKAGE_DIR%\LangMod\EN\SourceSukutsu.xlsx" >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo FAILED - SourceSukutsu.xlsx copy failed
    goto :error
)

REM Critical files: Data directory JSON files - use xcopy with explicit directory creation
if not exist "%STEAM_PACKAGE_DIR%\LangMod\EN\Data" mkdir "%STEAM_PACKAGE_DIR%\LangMod\EN\Data" >nul 2>&1
xcopy "%~dp0LangMod\EN\Data\*.*" "%STEAM_PACKAGE_DIR%\LangMod\EN\Data\" /Y /Q >nul 2>&1
echo OK

REM ============================================================
REM Show Excel Diff (with build mode for Debug-specific change detection)
REM ============================================================
echo.
echo --- Excel Changes ---
pushd tools
if "%BUILD_CONFIG%"=="Debug" (
    uv run python builder/excel_diff_manager.py diff --build-mode debug
) else (
    uv run python builder/excel_diff_manager.py diff --build-mode release
)
popd

echo.
echo === Build Complete ===
endlocal
exit /b 0

:error
echo.
echo !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
echo !!!                    BUILD FAILED                     !!!
echo !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
echo.
echo Please check the error messages above.
endlocal
exit /b 1
