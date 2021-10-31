# Prisma Windows installer

## `installer.iss`
This is the Inno Setup file used for the Windows installer for Prisma.
The compiler will output the installer to `publish/`, where the other binaries are placed.

Note that you must run the `build.sh` script first, to compile the binaries used by the install script. By default `build.sh` compiles for targets including architectures not supported by PrismaGUI. The installer only needs `win-x64` and `win-x86`, which can be compiled manually by running `build.sh` with the arguments `win-x64` and `win-x86`.

## `Languages/`
This directory contains the localizations for the messages in `SetupMessages.isl`.
