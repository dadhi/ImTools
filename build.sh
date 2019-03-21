#!/usr/bin/env bash

set -euxo pipefail #https://vaneyckt.io/posts/safer_bash_scripts_with_set_euxo_pipefail/
#dotnet restore

dotnet test test/ImTools.UnitTests/ImTools.UnitTests.csproj -c:Release -f:netcoreapp2.1 -p:Sign=false;
