@echo off
setlocal
cd /d "%~dp0"
echo.
echo   NOOK / NOISEWATER LABS
echo   ========================
echo [1/3] Locating the Windows C# compiler...
set "FX=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if not exist "%FX%\csc.exe" set "FX=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
if not exist "%FX%\csc.exe" goto missing
if not exist "%FX%\WPF\PresentationFramework.dll" goto missing
if not exist "dist" mkdir "dist"
echo [2/3] Compiling Nook...
"%FX%\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /codepage:65001 /out:"dist\Nook.exe" /resource:"Assets\Theme.xaml",Nook.Theme.xaml /win32icon:"Assets\Nook.ico" /win32manifest:"app.manifest" /reference:"%FX%\WPF\PresentationFramework.dll" /reference:"%FX%\WPF\PresentationCore.dll" /reference:"%FX%\WPF\WindowsBase.dll" /reference:"%FX%\System.Xaml.dll" /reference:"%FX%\System.Windows.Forms.dll" /reference:"%FX%\System.Drawing.dll" /reference:"%FX%\System.Runtime.Serialization.dll" /reference:"%FX%\System.Xml.dll" /reference:"%FX%\System.Core.dll" Source\*.cs > build.log 2>&1
if errorlevel 1 goto failed
echo [3/3] Complete. Your single application is dist\Nook.exe
echo No asset folders are needed beside the EXE.
echo.
if /i "%~1"=="-run" start "" "dist\Nook.exe"
if /i not "%~1"=="-fast" pause
exit /b 0
:missing
echo Windows .NET Framework 4.8 is required. Install it, then try again.
goto endfail
:failed
echo BUILD FAILED. If Nook is running, quit it from the tray and retry.
type build.log
:endfail
if /i not "%~1"=="-fast" pause
exit /b 1
