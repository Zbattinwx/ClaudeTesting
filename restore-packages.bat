@echo off
echo ========================================
echo Ohio Weather - NuGet Package Restore
echo ========================================
echo.

echo Checking .NET SDK...
dotnet --version
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo.
echo Cleaning build artifacts...
if exist bin rd /s /q bin
if exist obj rd /s /q obj
if exist .vs rd /s /q .vs

echo.
echo Restoring NuGet packages...
dotnet restore OhioWeather.csproj
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed!
    pause
    exit /b 1
)

echo.
echo Building project...
dotnet build OhioWeather.csproj
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo SUCCESS! Project is ready to run.
echo ========================================
echo.
echo Next steps:
echo 1. Open OhioWeather.sln in Visual Studio 2022
echo 2. Press F5 to run the application
echo 3. Click "Radar" button to load radar view
echo.
pause
