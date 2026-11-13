@echo off
setlocal

rem Regenerates Golden\ by running the extractor and the header generator on the normal inputs
rem (Excel\Main, Excel\Science). Scenario tests only read Golden\ and never write it.
rem After running, check "git diff" to confirm that only the intended lines changed, then commit.
rem Keep this file ASCII only: cmd.exe misreads UTF-8 text in batch files.

set "HERE=%~dp0"
set "WORK=%TEMP%\SdpGolden_%RANDOM%"

dotnet run --project "%HERE%..\ExcelColumnExtractor" -- -r "%HERE%Records\Main" -e "%HERE%Excel\Main" -o "%WORK%\csv\Main" -s B3
if errorlevel 1 goto failed

dotnet run --project "%HERE%..\ExcelColumnExtractor" -- -r "%HERE%Records\Science\Science.cs" -e "%HERE%Excel\Science" -o "%WORK%\csv\Science"
if errorlevel 1 goto failed

dotnet run --project "%HERE%..\StaticDataHeaderGenerator" -- all-header -r "%HERE%Records\Main" -o "%WORK%\AllHeaders.md"
if errorlevel 1 goto failed

rem The old golden files are removed only after every run above succeeded.
rem School.Students.csv goes to Golden\School\ (the folder is the file name up to the first dot).
rmdir /s /q "%HERE%Golden"
mkdir "%HERE%Golden"
for %%f in ("%WORK%\csv\Main\*.csv" "%WORK%\csv\Science\*.csv") do (
    for /f "tokens=1 delims=." %%w in ("%%~nxf") do (
        if not exist "%HERE%Golden\%%w" mkdir "%HERE%Golden\%%w"
        copy /y "%%f" "%HERE%Golden\%%w\" >nul
    )
)
copy /y "%WORK%\AllHeaders.md" "%HERE%Golden\" >nul
rmdir /s /q "%WORK%"

echo.
echo Golden files were regenerated. Changed files:
echo (A file listed here whose "git diff" is empty differs only in line endings.)
git -C "%HERE%." status --short -- Golden
exit /b 0

:failed
echo.
echo Extraction failed. Golden files were left unchanged.
if exist "%WORK%" rmdir /s /q "%WORK%"
exit /b 1