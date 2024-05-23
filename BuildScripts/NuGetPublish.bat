@echo off

set PACKAGES=.dist\packages
set SOURCE=https://api.nuget.org/v3/index.json
set /p APIKEY=<"..\ApiKey.txt"

dotnet nuget push "%PACKAGES%\ImTools.dll.5.0.0-preview-01.nupkg" -k %APIKEY% -s %SOURCE%
dotnet nuget push "%PACKAGES%\ImTools.dll.5.0.0-preview-01.snupkg" -k %APIKEY% -s %SOURCE%

echo:
echo:Publishing completed.

pause
