@echo off
setlocal
title Dawn Local AI Setup

set "OLLAMA_EXE=%LOCALAPPDATA%\Programs\Ollama\ollama.exe"
if not exist "%OLLAMA_EXE%" set "OLLAMA_EXE=%ProgramFiles%\Ollama\ollama.exe"

echo.
echo Dawn Local AI Setup
echo =======================
echo.
echo This installs Ollama if needed and downloads the default local model: llama3.2
echo No API key is needed.
echo.
pause

if not exist "%OLLAMA_EXE%" (
  echo Installing Ollama...
  winget install --id Ollama.Ollama -e
  if errorlevel 1 exit /b 1
)

set "OLLAMA_EXE=%LOCALAPPDATA%\Programs\Ollama\ollama.exe"
if not exist "%OLLAMA_EXE%" set "OLLAMA_EXE=%ProgramFiles%\Ollama\ollama.exe"

if not exist "%OLLAMA_EXE%" (
  echo.
  echo I still cannot find ollama.exe.
  echo Install Ollama from https://ollama.com/download and run this setup again.
  pause
  exit /b 1
)

echo.
echo Found Ollama:
echo %OLLAMA_EXE%

echo.
echo Starting Ollama...
start "" /min "%OLLAMA_EXE%" serve

echo.
echo Downloading llama3.2. This can take several minutes.
"%OLLAMA_EXE%" pull llama3.2
if errorlevel 1 exit /b 1

echo.
echo Installed models:
"%OLLAMA_EXE%" list

echo.
echo Done. Run dotnet run --project Dawn\Dawn.csproj from the repository root.
pause
