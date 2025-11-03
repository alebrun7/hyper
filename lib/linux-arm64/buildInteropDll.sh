# build the file libSQLite.Interop.dll on Raspberry PI
# Es wird dann unter ..\lib\linux-arm64 abgelegt
# inspiriert von https://github.com/tkf144/System.Data.SQLite-ARM64/blob/master/dockerfile
# es ist nicht geklärt, warum es mit aktuelleren Pakete von linq2db.SQLite nicht funktioniert
# wir haben jetzt version 1.0.119 und linq2db.SQLite 3.5.1 hat 1.0.115
# aber ab 1.0.115.5 gibt es den Fehler:
# Unhandled exception. System.EntryPointNotFoundException: Unable to find an entry point named 'SIa069da76968b7553' in shared library 'SQLite.Interop.dll'.

# deps-stage
sudo apt-get update
sudo apt-get install unzip wget libtool libz-dev gcc-aarch64-linux-gnu

# get-source-stage
mkdir src
cd src
SQLITE_SOURCE_ZIP_URL="https://system.data.sqlite.org/blobs/1.0.119.0/sqlite-netFx-source-1.0.119.0.zip"
wget -O sqlite-source.zip "$SQLITE_SOURCE_ZIP_URL" && unzip sqlite-source.zip
chmod +x ./Setup/*.sh

# build-interop-stage
cd Setup
sed -i 's|gcc -g -fPIC|aarch64-linux-gnu-gcc -g -fPIC|g' ./compile-interop-assembly-release.sh
./compile-interop-assembly-release.sh
file ../bin/2013/Release/bin/SQLite.Interop.dll \
		&& file ../bin/2013/Release/bin/libSQLite.Interop.so \
		&& ldd --version
