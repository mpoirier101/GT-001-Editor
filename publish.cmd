cls

cd /d C:\Apps\GT-001

set DOTNET=C:\Users\Michel\.dotnet-sdk\dotnet.exe
set DOTNET_ROOT=C:\Users\Michel\.dotnet-sdk
set DOTNET_CLI_HOME=C:\Apps\GT-001\.dotnet-cli-home
set NUGET_PACKAGES=C:\Apps\GT-001\.nuget\packages
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_NOLOGO=1
set PATH=C:\Users\Michel\.dotnet-sdk;%PATH%
set SEVENZIP=C:\Program Files\7-Zip\7z.exe
set GH=C:\Program Files\GitHub CLI\gh.exe
set VERSION=0.2.0
set TAG=v%VERSION%
set RELEASE=artifacts\release
set OUT=%RELEASE%\GT001.Editor-%VERSION%-win-x64
set ZIP=%CD%\%RELEASE%\GT001.Editor-%VERSION%-win-x64.zip

if not exist "%RELEASE%" mkdir "%RELEASE%"
if exist "%ZIP%" del "%ZIP%"
if not exist "%SEVENZIP%" (
  echo 7-Zip was not found at "%SEVENZIP%".
  pause
  exit /b 1
)

"%DOTNET%" restore GT001.Editor.sln --configfile NuGet.Config
if errorlevel 1 goto error

"%DOTNET%" publish src\GT001.Editor.App\GT001.Editor.App.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:WindowsPackageType=None ^
  -p:WindowsAppSDKSelfContained=true ^
  -p:DebugType=none ^
  -p:DebugSymbols=false ^
  -o %OUT%
if errorlevel 1 goto error

pushd "%OUT%"
"%SEVENZIP%" a -tzip "%ZIP%" .\*
popd
if errorlevel 1 goto error

git status
git add .
git commit -m "Release %VERSION%"
if errorlevel 1 (
  echo No Git commit was created. This is OK if there were no source changes.
)
git push
if errorlevel 1 goto error

if exist "%GH%" (
  "%GH%" release view "%TAG%" >nul 2>nul
  if errorlevel 1 (
    "%GH%" release create "%TAG%" "%ZIP%" --title "GT-001 Editor %VERSION%" --notes "Release %VERSION%"
  ) else (
    "%GH%" release upload "%TAG%" "%ZIP%" --clobber
  )
  if errorlevel 1 goto error
) else (
  echo GitHub CLI was not found at "%GH%".
  echo Code was pushed to GitHub, but the ZIP was not uploaded as a GitHub Release asset.
  echo Install GitHub CLI to enable automatic release upload.
)

echo Publish complete.
pause
exit /b 0

:error
echo Publish failed.
pause
exit /b 1
