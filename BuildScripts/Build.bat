@echo off
setlocal EnableDelayedExpansion

set SLN="..\src\ImTools.sln"
set OUTDIR="..\bin\Release"

rem finding MSBuild.exe
set MSB="C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\bin\MSBuild.exe"
if not exist %MSB% set MSB="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\bin\MSBuild.exe"
if not exist %MSB% for /f "tokens=4 delims='" %%p IN ('.nuget\nuget.exe restore ^| find "MSBuild auto-detection"') do set MSB="%%p\MSBuild.exe"

echo:
echo:## USING MSBUILD: %MSB%
echo:

%MSB% %SLN% /t:Rebuild /v:minimal /m /fl /flp:LogFile=MSBuild.log ^
    /p:OutDir=%OUTDIR% ^
    /p:GenerateProjectSpecificOutputFolder=false ^
    /p:Configuration=Release ^
    /p:RestorePackages=true

find /C "Build succeeded." MSBuild.log

endlocal
if not "%1"=="-nopause" pause
