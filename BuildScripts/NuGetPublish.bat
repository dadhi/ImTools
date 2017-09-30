@echo off
pushd "..\src"

set NUGET=".nuget\NuGet.exe"
set PACKAGEDIR="bin\NuGetPackages"
set /p APIKEY=<"..\ApiKey.txt"

%NUGET% push "%PACKAGEDIR%\ImTools.dll.1.0.0.nupkg" -Source https://nuget.org -ApiKey %APIKEY%
%NUGET% push "%PACKAGEDIR%\ImTools.1.0.0.nupkg" -Source https://nuget.org -ApiKey %APIKEY%

popd
pause

echo: 
echo:Packaging succeeded.
