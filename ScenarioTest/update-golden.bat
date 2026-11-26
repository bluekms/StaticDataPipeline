@echo off
setlocal

rem Regenerates the Golden\ folder of each subject under Feature\ (School, Rpg, Science) by running the extractor
rem and the header generator on its normal inputs. Scenario tests only read Golden\ and never write it.
rem After running, check "git diff" to confirm that only the intended lines changed, then commit.
rem Keep this file ASCII only: cmd.exe misreads UTF-8 text in batch files.

set "HERE=%~dp0"
set "WORK=%TEMP%\SdpGolden_%RANDOM%"

dotnet run --project "%HERE%..\ExcelColumnExtractor" -- -r "%HERE%Feature\School\Records" -e "%HERE%Feature\School\Excel" -o "%WORK%\csv\School" -s B3
if errorlevel 1 goto failed

dotnet run --project "%HERE%..\ExcelColumnExtractor" -- -r "%HERE%Feature\Rpg\Records" -e "%HERE%Feature\Rpg\Excel" -o "%WORK%\csv\Rpg" -s B3
if errorlevel 1 goto failed

dotnet run --project "%HERE%..\ExcelColumnExtractor" -- -r "%HERE%Feature\Science\Records\Science.cs" -e "%HERE%Feature\Science\Excel" -o "%WORK%\csv\Science"
if errorlevel 1 goto failed

dotnet run --project "%HERE%..\StaticDataHeaderGenerator" -- all-header -r "%HERE%Feature\School\Records" -o "%WORK%\AllHeaders.School.md"
if errorlevel 1 goto failed

dotnet run --project "%HERE%..\StaticDataHeaderGenerator" -- all-header -r "%HERE%Feature\Rpg\Records" -o "%WORK%\AllHeaders.Rpg.md"
if errorlevel 1 goto failed

rem The old golden files are removed only after every run above succeeded.
for %%s in (School Rpg Science) do (
    rmdir /s /q "%HERE%Feature\%%s\Golden"
    mkdir "%HERE%Feature\%%s\Golden"
    copy /y "%WORK%\csv\%%s\*.csv" "%HERE%Feature\%%s\Golden\" >nul
)
copy /y "%WORK%\AllHeaders.School.md" "%HERE%Feature\School\Golden\AllHeaders.md" >nul
copy /y "%WORK%\AllHeaders.Rpg.md" "%HERE%Feature\Rpg\Golden\AllHeaders.md" >nul
rmdir /s /q "%WORK%"

echo.
echo Golden files were regenerated. Changed files:
echo (A file listed here whose "git diff" is empty differs only in line endings.)
git -C "%HERE%." status --short -- Feature\School\Golden Feature\Rpg\Golden Feature\Science\Golden
exit /b 0

:failed
echo.
echo Extraction failed. Golden files were left unchanged.
if exist "%WORK%" rmdir /s /q "%WORK%"
exit /b 1