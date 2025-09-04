@echo off
echo Building EndGame Target Colony Mod...

REM Путь к игре относительно build.bat
set STATIONEERS_INSTALL=..\..\

REM Сборка проекта (всегда выполняется)
dotnet build EndGameTargetColony.csproj -c Release -p:STATIONEERS_INSTALL="%STATIONEERS_INSTALL%"


if %ERRORLEVEL% EQU 0 (
    echo Build successful!
    echo Output: bin\Release\EndGameTargetColony.dll
    
    REM Проверяем существование игры для копирования
    if not exist "%STATIONEERS_INSTALL%\rocketstation_Data" (
        echo WARNING: Stationeers installation not found at %STATIONEERS_INSTALL%
        echo Expected to find rocketstation_Data folder
        echo Build completed but mod not copied to game directory
        pause
        exit /b 0
    )
    
    REM Создаем папку мода если её нет
    if not exist "%STATIONEERS_INSTALL%\mods\EndGameTargetColony\" (
        mkdir "%STATIONEERS_INSTALL%\mods\EndGameTargetColony\"
        echo Created mod directory
    )
    
    REM Копируем только dll файл
    copy "bin\Release\EndGameTargetColony.dll" "%STATIONEERS_INSTALL%\mods\EndGameTargetColony\"
    
    echo Mod DLL copied to: %STATIONEERS_INSTALL%\mods\EndGameTargetColony\
) else (
    echo Build failed!
)