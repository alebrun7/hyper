REM publish for raspi
REM danach ist der x bit nicht gesetzt. In linux mit "chmod +x hyper ClientTCP" setzen
SET sevenZ="C:\Program Files\7-Zip\7z.exe"
PUSHD ..
if exist publishlinux-arm.zip del publishlinux-arm.zip
dotnet publish -c Release --self-contained -r linux-arm -o ./publishlinux-arm
if exist %sevenZ% %sevenZ% a -tzip publishlinux-arm.zip publishlinux-arm\*
explorer .
POPD
ECHO This window will close automatically in 60 seconds...
ping -n 61 localhost > /nul