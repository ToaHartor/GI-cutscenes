#!/usr/bin/bash

runtimes=("win-x64" "win10-x64" "linux-x64" "linux-arm64" "osx.12-x64" "osx.12-arm64")
buildpath="src/bin/Release/net6.0"

# Retrieving project version
version=$(cat src/GICutscenes.csproj | grep -oP "<Version>\K([0-9]\.[0-9]\.[0-9])")

for r in "${runtimes[@]}";
do
	# Self contained
	echo "Building self contained $r"
	rm -r "$buildpath/$r/publish"
	dotnet publish -c Release -r $r --self-contained
	zip -jr "GICutscenes-$version-$r-standalone.zip" "$buildpath/$r/publish"

	# Framework dependant
	echo "Building framework dependant $r"
	rm -r "$buildpath/$r/publish"
	dotnet publish -c Release -r $r --self-contained false -p:PublishTrimmed=false
	zip -jr "GICutscenes-$version-$r.zip" "$buildpath/$r/publish"
done