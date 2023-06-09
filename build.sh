#!/usr/bin/env bash
set -o errexit
set -o pipefail
set -o nounset

cd "$(dirname "$0")"

declare -a TargetsWithGUI=("win-x64" "win-x86")
declare -a TargetsWithoutGUI=("linux-x64" "linux-arm" "linux-arm64" "osx-x64" "osx-arm64")
declare PublishOptionsGUI="-c Release -p:PublishSingleFile=true --nologo"
declare PublishOptions="${PublishOptionsGUI} -p:PublishTrimmed=true"
declare TargetFramework="net6.0"

function build() {
	for t in "$@"; do
		echo "Building ${t}..."

		if [[ " ${TargetsWithGUI[*]} " == *" ${t} "* ]]; then
			dotnet publish Prisma/Prisma.csproj -r "$t" --self-contained ${PublishOptions} -p:PublishReadyToRun=true
			dotnet publish PrismaGUI/PrismaGUI.csproj -r "$t" --self-contained ${PublishOptionsGUI} -p:PublishReadyToRun=true
		else
			dotnet publish Prisma/Prisma.csproj -r "$t" --self-contained ${PublishOptions}
		fi
	done

	echo "Gathering binaries..."

	for t in "$@"; do
		if [[ " ${TargetsWithGUI[*]} " == *" ${t} "* ]]; then
			cp "Prisma/bin/Release/${TargetFramework}/${t}/publish/Prisma.exe" "publish/Prisma-${t}.exe"
			cp "PrismaGUI/bin/Release/${TargetFramework}-windows/${t}/publish/PrismaGUI.exe" "publish/PrismaGUI-${t}.exe"
		else
			cp "Prisma/bin/Release/${TargetFramework}/${t}/publish/Prisma" "publish/prisma-${t}"
		fi
	done

	echo "Binaries gathered."
}

if [ $# -eq 0 ]; then
	AllTargets=("${TargetsWithGUI[@]}" "${TargetsWithoutGUI[@]}")

	build "${AllTargets[@]}"
else
	build "$@"
fi
