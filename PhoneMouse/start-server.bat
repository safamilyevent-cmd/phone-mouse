@echo off
setlocal
cd /d "%~dp0PhoneMouse.Server"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK не найден. Для запуска исходной версии установите .NET 8 SDK.
  echo Готовая автономная сборка будет поставляться отдельно после Windows-сборки.
  pause
  exit /b 1
)
dotnet run -c Release
if errorlevel 1 (
  echo Не удалось запустить сервер. Проверьте сообщения выше и порт 8765.
  pause
)