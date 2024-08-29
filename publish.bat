@echo off

echo Cleaning bin, publish, and obj folders.

rmdir publish /s /q
mkdir publish

cd balatro-mobile-builder

rmdir bin\Release /s /q
rmdir bin\publish /s /q
rmdir obj /s /q

echo Building win-x64
dotnet publish "balatro-mobile-builder.csproj" /p:PublishProfile="win-x64"
move .\bin\publish\win-x64\balatro-mobile-builder.exe ..\publish\balatro-mobile-builder-win-x64.exe

echo Building win-arm64
dotnet publish "balatro-mobile-builder.csproj" /p:PublishProfile="win-arm64"
move .\bin\publish\win-arm64\balatro-mobile-builder.exe ..\publish\balatro-mobile-builder-win-arm64.exe

echo Building osx-x64
dotnet publish "balatro-mobile-builder.csproj" /p:PublishProfile="osx-x64"
move .\bin\publish\osx-x64\balatro-mobile-builder ..\publish\balatro-mobile-builder-osx-x64

echo Building osx-arm64
dotnet publish "balatro-mobile-builder.csproj" /p:PublishProfile="osx-arm64"
move .\bin\publish\osx-arm64\balatro-mobile-builder ..\publish\balatro-mobile-builder-osx-arm64

echo Building linux-x64
dotnet publish "balatro-mobile-builder.csproj" /p:PublishProfile="linux-x64"
move .\bin\publish\linux-x64\balatro-mobile-builder ..\publish\balatro-mobile-builder-linux-x64

echo Building linux-arm64
dotnet publish "balatro-mobile-builder.csproj" /p:PublishProfile="linux-arm64"
move .\bin\publish\linux-arm64\balatro-mobile-builder ..\publish\balatro-mobile-builder-linux-arm64