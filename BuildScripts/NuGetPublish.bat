@echo off

set PACKAGES=.dist\packages
set SOURCE=https://api.nuget.org/v3/index.json
set /p APIKEY=<"..\ApiKey.txt"

dotnet nuget push "%PACKAGES%\ImTools.dll.3.1.0.nupkg" -k %APIKEY% -s %SOURCE%
dotnet nuget push "%PACKAGES%\ImTools.3.1.0.nupkg" -k %APIKEY% -s %SOURCE%

echo:
echo:Publishing completed.

pause
