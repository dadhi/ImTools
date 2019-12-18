@echo off
setlocal EnableDelayedExpansion

set NUGET=.nuget\NuGet.exe
set NUSPECS=nuspecs
set PACKAGEDIR=.dist\packages

echo:
echo:Packing NuGet packages into %PACKAGEDIR% . . .
echo:
if not exist %PACKAGEDIR% md %PACKAGEDIR% 

%NUGET% pack %NUSPECS%\ImTools.nuspec -OutputDirectory %PACKAGEDIR% -NonInteractive

REM if not "%1"=="-nopause" pause 
REM goto:eof

REM set VERFILE=%~1
REM for /f "usebackq tokens=2,3 delims=:() " %%A in ("%VERFILE%") do (
REM 	if "%%A"=="AssemblyInformationalVersion" set VER=%%~B
REM exit /b