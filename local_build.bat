@echo off

echo:
echo:## Starting: RESTORE and BUILD...
echo: 

dotnet clean -v:m
dotnet build -c:Release -v:m -p:DevMode=false;LocalBuild=true
if %ERRORLEVEL% neq 0 goto :error

echo:## Finished: RESTORE and BUILD
echo: 
echo:## Starting: TESTS...
echo: 

dotnet test -c:Release -p:GeneratePackageOnBuild=false;DevMode=false;LocalBuild=true

if %ERRORLEVEL% neq 0 goto :error
echo:## Finished: TESTS

echo: 
echo:## Finished: TESTS
echo: 
call BuildScripts\NugetPack.bat
if %ERRORLEVEL% neq 0 goto :error
echo:
echo:## Finished: PACKAGING ##
echo: 
echo:## Finished: ALL ##
echo:
exit /b 0

:error
echo:
echo:## :-( Failed with ERROR: %ERRORLEVEL%
echo:
exit /b %ERRORLEVEL%
