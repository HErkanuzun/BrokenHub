@echo off
rem Broken Hub - derleme betigi. Windows'un icindeki .NET Framework derleyicisini kullanir, ekstra kurulum gerekmez.
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo .NET Framework 4 derleyicisi bulunamadi.
  if "%1"=="" pause
  exit /b 1
)

if not exist bin mkdir bin
"%CSC%" /nologo /codepage:65001 /target:winexe /optimize+ /platform:anycpu ^
  /win32icon:src\app.ico /win32manifest:src\app.manifest ^
  /resource:src\app.ico,BrokenHub.app.ico /resource:src\logo.png,BrokenHub.logo.png ^
  /resource:src\tool-screen.png,BrokenHub.tool-screen.png /resource:src\tool-disk.png,BrokenHub.tool-disk.png /resource:src\tool-space.png,BrokenHub.tool-space.png /resource:src\tool-mic.png,BrokenHub.tool-mic.png ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:System.Web.Extensions.dll ^
  /out:bin\BrokenHub.exe src\*.cs
if errorlevel 1 (
  echo.
  echo DERLEME HATASI
  if "%1"=="" pause
  exit /b 1
)
echo.
echo TAMAM: bin\BrokenHub.exe
if "%1"=="" pause
