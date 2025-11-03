REM publish for raspi
REM danach ist der x bit nicht gesetzt. In linux mit "chmod +x hyper ClientTCP" setzen
SET sevenZ="C:\Program Files\7-Zip\7z.exe"
SET RuntimeId=linux-arm64
SET Framework=net8
PUSHD ..
if exist publish%RuntimeId%.zip del publish%RuntimeId%.zip
dotnet publish -c Release --self-contained -r %RuntimeId%
robocopy ClientTCP\bin\Release\%Framework%\%RuntimeId%\publish publish%RuntimeId%\
robocopy hyper\bin\Release\%Framework%\%RuntimeId%\publish publish%RuntimeId%\

if exist %sevenZ% %sevenZ% a -tzip publish%RuntimeId%.zip publish%RuntimeId%\*
explorer .
POPD
ECHO This window will close automatically in 60 seconds...
timeout /t 60