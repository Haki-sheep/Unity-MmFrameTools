@echo off
setlocal

set "TOOLS_ROOT=%~dp0"
set "LUBAN_REPO=%TOOLS_ROOT%LubanExamples"
set "LUBAN_COMMIT=3ddebdc75a67f76cab830608bfaf3b8806e05175"

if exist "%LUBAN_REPO%\Tools\Luban\Luban.dll" goto verify_commit

git clone --depth 1 --filter=blob:none --sparse https://github.com/focus-creative-games/luban_examples.git "%LUBAN_REPO%"
if errorlevel 1 exit /b 1

git -c safe.directory="%LUBAN_REPO%" -C "%LUBAN_REPO%" sparse-checkout set Tools/Luban MiniTemplate
if errorlevel 1 exit /b 1

git -c safe.directory="%LUBAN_REPO%" -C "%LUBAN_REPO%" fetch --depth 1 origin %LUBAN_COMMIT%
if errorlevel 1 exit /b 1

git -c safe.directory="%LUBAN_REPO%" -C "%LUBAN_REPO%" checkout --detach %LUBAN_COMMIT%
if errorlevel 1 exit /b 1

:verify_commit
set "CURRENT_COMMIT="
for /f "usebackq delims=" %%I in (`git -c safe.directory^="%LUBAN_REPO%" -C "%LUBAN_REPO%" rev-parse HEAD 2^>nul`) do set "CURRENT_COMMIT=%%I"
if /I not "%CURRENT_COMMIT%"=="%LUBAN_COMMIT%" (
    echo Luban checkout version mismatch
    echo Expected %LUBAN_COMMIT%
    echo Actual   %CURRENT_COMMIT%
    exit /b 1
)

:verify
dotnet "%LUBAN_REPO%\Tools\Luban\Luban.dll" --version
exit /b 0
