#!/usr/bin/bash
runtimes=("win-x64" "win10-x64" "linux-x64" "linux-arm64" "osx.12-x64" "osx.12-arm64")
buildpath="src/bin/Release/net6.0"
ROOT="$(pwd)"          # 记下项目根，打包时要回来
version=$(cat src/GICutscenes.csproj | grep -oP "<Version>\K([0-9]\.[0-9]\.[0-9])")
SEVENZ="/c/Program Files/7-Zip/7z.exe"

for r in "${runtimes[@]}";
do
	echo "Building single-file self-contained $r"
	rm -rf "$buildpath/$r/publish"
	dotnet publish -c Release -r $r --self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:EnableCompressionInSingleFile=true

	echo "Packing $r"
	cd "$buildpath/$r/publish" || exit 1   # 进入 publish 目录
	"$SEVENZ" a -tzip -bso0 -bsp0 \
		"$ROOT/GICutscenes-$version-$r-standalone.zip" "*"
	cd "$ROOT"                              # 回到项目根，继续下个平台
done