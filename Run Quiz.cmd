@echo off
cd /d "%~dp0"
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
dotnet run --project "src\DotNetGcQuiz"
if errorlevel 1 pause
