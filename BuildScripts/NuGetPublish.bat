@echo off

set PACKAGES=..\.dist\packages
set SOURCE=https://api.nuget.org/v3/index.json
set /p APIKEY=<"..\ApiKey.txt"

rem dotnet nuget push "%PACKAGES%\ImTools.dll.2.0.0.nupkg" -k %APIKEY% -s %SOURCE%
rem dotnet nuget push "%PACKAGES%\ImTools.2.0.0.nupkg" -k %APIKEY% -s %SOURCE%

echo:
echo:Publishing completed.

pause
