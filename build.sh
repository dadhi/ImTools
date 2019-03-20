#!/usr/bin/env bash

# set -euo pipefail

dotnet restore -p:SourceLink=false

dotnet test test/ImTools.UnitTests/ImTools.UnitTests.csproj -c:Release -f:netcoreapp2.0 -p:Sign=false;SourceLink=false
