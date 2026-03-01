::---------------.
:: Settings
::---------------.
:: Elin install path (Adjust if needed)
set ELIN_PATH="C:\Program Files (x86)\Steam\steamapps\common\Elin"
:: Lib source path
set LIB_SRC_PATH="%~dp0/../Elin_Libs"
:: Lib destination path
set LIB_DEST_PATH="%~dp0/src/Lib"

::---------------.
:: Create symlink for DLL references
::---------------.
if not exist %~dp0elin_link\ (
    mklink /D %~dp0elin_link\ %ELIN_PATH%
    if errorlevel 1 (
        echo mklink failed. Trying xcopy...
        xcopy %ELIN_PATH% %~dp0elin_link\ /s /e /h /y /i
    )
)

::---------------.
:: Create symlink for Lib references
::---------------.
if not exist %LIB_DEST_PATH% (
    mklink /D %LIB_DEST_PATH% %LIB_SRC_PATH%
    if errorlevel 1 (
        echo mklink failed. Trying xcopy...
        xcopy %LIB_SRC_PATH% %LIB_DEST_PATH% /s /e /h /y /i
    )
)
