@echo off
pushd "..\src"

set NUGET=".nuget\NuGet.exe"
set PACKAGEDIR="bin\NuGetPackages"

echo:
echo:Packing NuGet packages into %PACKAGEDIR% . . .

if exist %PACKAGEDIR% rd /s /q %PACKAGEDIR%
md %PACKAGEDIR% 

echo:
call :ParseVersion "ImTools\Properties\AssemblyInfo.cs"
echo:ImTools v%VER%
echo:================
%NUGET% pack "..\NuGet\ImTools.dll.nuspec" -Version %VER% -OutputDirectory %PACKAGEDIR% -NonInteractive -Symbols
%NUGET% pack "..\NuGet\ImTools.nuspec" -Version %VER% -OutputDirectory %PACKAGEDIR% -NonInteractive

echo: 
echo:Packaging succeeded.
popd

if not "%1"=="-nopause" pause 
goto:eof

:ParseVersion
set VERFILE=%~1
for /f "usebackq tokens=2,3 delims=:() " %%A in ("%VERFILE%") do (
    if "%%A"=="AssemblyInformationalVersion" set VER=%%~B
)
exit /b