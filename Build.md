# Building Prisma
A build script `build.sh` is included. This will build Prisma for Windows, Linux and OSX. PrismaGUI will only be built for Windows.<br>
The script takes [RIDs used by `dotnet`](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) as arguments. If none is given, all targets listed in that file will be built.

## `publish/`
This directory is populated by `build.sh` when no arguments are given. It contains all the compiled binaries. The Inno Setup Compiler will also compile the installer to this directory.

## `Setup/`
This directory contains the files used by Inno Setup to compile the installer.

## Build notes
MSBuild will mention that FastCGI 1.3.0 targets the .NET Framework instead of the expected .NET Core. You can ignore this warning, the package is framework agnostic enough to still work.

Prisma targets .NET Core 3.1. Building should be possible on .NET Core 3.1, but cross-compilation isn't fully supported until .NET 6. This may result in errors when running `build.sh` on .NET Core 3.1.
