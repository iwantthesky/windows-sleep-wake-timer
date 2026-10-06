@echo off
setlocal
cd /d "%~dp0"
set "TIMER_CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%TIMER_CSC%" (
  echo .NET Framework C# compiler could not be found. Use Windows x64 with .NET Framework 4.8.
  exit /b 1
)
if not exist bin mkdir bin
"%TIMER_CSC%" /nologo /target:winexe /platform:x64 /optimize+ /debug- /utf8output /out:"bin\UykuZamanlayici.exe" /win32manifest:"src\app.manifest" /win32icon:"assets\app.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.ServiceProcess.dll /reference:Microsoft.CSharp.dll "src\Program.cs" "src\Localization.cs" "src\TranslationCatalog.cs" "src\AssemblyInfo.cs"
if errorlevel 1 exit /b 1
echo Build completed: bin\UykuZamanlayici.exe
"%TIMER_CSC%" /nologo /target:winexe /platform:x64 /optimize+ /debug- /utf8output /out:"bin\SleepWakeTimer-Setup.exe" /win32manifest:"src\installer.manifest" /win32icon:"assets\app.ico" /resource:"bin\UykuZamanlayici.exe",ApplicationPayload /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll "src\Installer.cs" "src\Localization.cs" "src\TranslationCatalog.cs" "src\AssemblyInfo.cs"
if errorlevel 1 exit /b 1
echo Build completed: bin\SleepWakeTimer-Setup.exe
