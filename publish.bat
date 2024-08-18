@echo off

cd balatro-mobile-builder

echo Clearing bin, publish, and obj folders.
rmdir bin /s /q
rmdir obj /s /q
rmdir publish /s /q

mkdir publish

echo Building win-x64
dotnet publish -o bin\publish\win-x64 --self-contained -f net8.0 --runtime win-x64
move .\bin\publish\win-x64\balatro-mobile-builder.exe .\publish\balatro-mobile-builder-win-x64.exe

echo Building win-arm64
dotnet publish -o bin\publish\win-arm64 --self-contained -f net8.0 --runtime win-arm64
move .\bin\publish\win-arm64\balatro-mobile-builder.exe .\publish\balatro-mobile-builder-win-arm64.exe

echo Building osx-x64
dotnet publish -o bin\publish\osx-x64 --self-contained -f net8.0 --runtime osx-x64
move .\bin\publish\osx-x64\balatro-mobile-builder .\publish\balatro-mobile-builder-osx-x64

echo Building osx-arm64
dotnet publish -o bin\publish\osx-arm64 --self-contained -f net8.0 --runtime osx-arm64
move .\bin\publish\osx-arm64\balatro-mobile-builder .\publish\balatro-mobile-builder-osx-arm64

echo Building linux-x64
dotnet publish -o bin\publish\linux-x64 --self-contained -f net8.0 --runtime linux-x64
move .\bin\publish\linux-x64\balatro-mobile-builder .\publish\balatro-mobile-builder-linux-x64

echo Building linux-arm64
dotnet publish -o bin\publish\linux-arm64 --self-contained -f net8.0 --runtime linux-arm64
move .\bin\publish\linux-arm64\balatro-mobile-builder .\publish\balatro-mobile-builder-linux-arm64