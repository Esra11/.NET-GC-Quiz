@echo off
cd /d "%~dp0"
if exist "DotNetGcQuiz.exe" (
    start "" "DotNetGcQuiz.exe"
    exit /b
)
if exist "artifacts\app-report-fast-20261022\DotNetGcQuiz.exe" (
    start "" "artifacts\app-report-fast-20261022\DotNetGcQuiz.exe"
    exit /b
)
if exist "artifacts\app-attached-icon-20261022\DotNetGcQuiz.exe" (
    start "" "artifacts\app-attached-icon-20261022\DotNetGcQuiz.exe"
    exit /b
)
if exist "artifacts\app-highlight-20261022\DotNetGcQuiz.exe" (
    start "" "artifacts\app-highlight-20261022\DotNetGcQuiz.exe"
    exit /b
)
if exist "artifacts\app-icon-20261022\DotNetGcQuiz.exe" (
    start "" "artifacts\app-icon-20261022\DotNetGcQuiz.exe"
    exit /b
)
if exist "artifacts\app-reports-20261022\DotNetGcQuiz.exe" (
    start "" "artifacts\app-reports-20261022\DotNetGcQuiz.exe"
    exit /b
)
if exist "artifacts\app-20261022\DotNetGcQuiz.exe" (
    start "" "artifacts\app-20261022\DotNetGcQuiz.exe"
    exit /b
)
if exist "artifacts\app\DotNetGcQuiz.exe" (
    start "" "artifacts\app\DotNetGcQuiz.exe"
    exit /b
)
echo Download the Windows ZIP from https://github.com/Esra11/.NET-GC-Quiz/releases/latest
echo Extract all files, then double-click Run Quiz.cmd in the extracted folder.
echo Developers who modified the source can run: dotnet run --project src/DotNetGcQuiz
pause
exit /b 1
