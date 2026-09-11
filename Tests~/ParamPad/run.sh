#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
# Release: the shape that ships, and the one where a try block can start at instruction 0
dotnet build libA/libA.csproj -c Release -v q --nologo
dotnet build libB/libB.csproj -c Release -v q --nologo
dotnet build fixture/fixture.csproj -c Release -v q --nologo
dotnet build caller/caller.csproj -c Release -v q --nologo
dotnet build probe.csproj -v q --nologo
dotnet bin/Debug/net7.0/probe.dll

ilverify=$(command -v ilverify || echo "$HOME/.dotnet/tools/ilverify")
if [ -x "$ilverify" ]; then
    refs=$(dirname "$(find /usr/share/dotnet/shared/Microsoft.NETCore.App/7.* -name System.Private.CoreLib.dll 2>/dev/null | head -1)")
    for dll in out/fixture.dll out/caller.dll; do
        "$ilverify" "$dll" -r "$refs/*.dll" -r "out/*.dll"
    done
else
    echo "SKIP  ilverify not installed (dotnet tool install -g dotnet-ilverify --version 7.0.0)"
fi
