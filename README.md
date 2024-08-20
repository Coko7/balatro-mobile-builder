# Balatro Mobile Builder

This is a rewrite of [balatro-mobile-maker](https://github.com/blake502/balatro-mobile-maker) that introduces multiple enhancements and fixes.

### Significant changes:
 - Introduced automatic save syncing between devices.
 - Multiple graphic fixes due to DPI and shader issues.
 - Removed dependency that needed to be downloaded.
 - Dynamic code and patch system.

![Balatro on yo dam phone](https://raw.githubusercontent.com/PGgamer2/balatro-mobile-builder/main/resources/screenshot.jpg)

The goal of this project is to allow *Balatro* fans to play *Balatro* on their mobile devices before the official release.
This project provides a **NON-PIRACY** avenue for players to do so,
by converting their legal *Steam* copy of *Balatro* to a mobile app.
Balatro Mobile Builder also supports automatically transferring your saves back and forth between your *Steam* copy of the game and your mobile device
(save transfer features only available on Windows and Android, for now).

Mods are not _officially_ supported, but could work if the base Balatro.exe is already modded.
Please report any bugs you encouter in the [issues section](https://github.com/PGgamer2/balatro-mobile-builder/issues).
If you encounter bugs with the latest release, try the previous release.

## Quick Start Guide
Please review the **Notes** section before you begin.
 - Download or compile [**balatro-mobile-builder**](https://github.com/PGgamer2/balatro-mobile-builder/releases/latest).
 - Run **balatro-mobile-builder**.
 - Follow the prompts to apply optional patches. If you're unsure, always select "y" (or just press enter).
 ### For Android:
 - Copy the resulting `balatro-signed.apk` to your Android device, or allow the program to automatically install using [USB Debugging](https://developer.android.com/studio/debug/dev-options).
 - Optionally, allow the program to automatically transfer your saves from your *Steam* copy of *Balatro* using [USB Debugging](https://developer.android.com/studio/debug/dev-options).
 ### For iOS:
 - Sideload `balatro.ipa` using either [Sideloadly](https://sideloadly.io/) or [AltStore](https://altstore.io/)
 - Optionally, [copy your saves to your iOS device](https://github.com/blake502/balatro-mobile-maker/issues/64#issuecomment-2094660508).

## Optional Patches
 - **FPS Cap** — Caps FPS to the device's native refresh rate (Recommended for performance)
 - **Landscape Orientation** — Locks the game to landscape orientation (Recommended, since portrait orientation does not behave very well)
 - **High DPI** — Enables [High DPI graphics mode](https://love2d.org/wiki/love.window.setMode) (Recommended, fixes some graphical bugs)

## For power users
Useful informations for developers and advanced users
 ### Command Line Args 
 The executable has a few command line parameters:
  - `-s (android/ios)` enables silent mode. This will automatically build the app for the specified platform
  - `-o FILE` specifies the path for the output file
 ### Creating new patches
 Patch files are located in the `balatro-mobile-builder/Resources/patches/` directory and added as assembly resources.
 These files can be generated using the [diff](http://www.gnu.org/software/diffutils/diffutils.html) tool like so:
 `diff -U2 original.lua modified.lua > example.patch`.
 To register a new patch, it must be added to the `BalatroMobileBuilder.BalatroPatches.patchList` list
 with its name and the path of the file it modifies.
 ### Testing
 When running in debug mode, a new prompt for testing will appear.
 It will run LÖVE on the local machine using the path where Balatro was extracted (default to `.balatro.d`)
 and will temporarily disable the `_RELEASE_MODE` flag.
 Balatro will not start correctly if the steam-related DLLs can't be found.

## Notes
 - This script assumes that `Balatro.exe` is located in the default *Steam* directory. If it is not, simply copy your `Balatro.exe` or `game.love` to the same folder as **balatro-mobile-builder**
 ### For Android:
 - This script will automatically download [OpenJDK](https://www.microsoft.com/openjdk)
 - This script will automatically download [APK Tool](https://apktool.org/)
 - This script will automatically download [uber-apk-signer](https://github.com/patrickfav/uber-apk-signer/)
 - This script will automatically download [love-11.5-android-embed.apk](https://github.com/love2d/love-android/)
 - This script can automatically download [Android Developer Bridge](https://developer.android.com/tools/adb) (optional)
 ### For iOS:
 - This script will automatically download [Balatro-IPA-Base](https://github.com/PGgamer2/balatro-mobile-builder/blob/main/resources/base.ipa)

 ## Recognition (in no particular order)
 - [Every contributor](https://github.com/PGgamer2/balatro-mobile-builder/graphs/contributors), especially [blake502](https://github.com/blake502)
 - Delevopers of [Balatro](https://www.playbalatro.com/)
 - Developers of [LÖVE](https://love2d.org/)
 - Developers of [APKTool](https://apktool.org/) and [uber-apk-signer](https://github.com/patrickfav/uber-apk-signer)
 - Developers of [DiffPatch](https://github.com/aberteau/DiffPatch/graphs/contributors) and [SharpZipLib](https://github.com/icsharpcode/SharpZipLib/graphs/contributors)

 ## License
 - This project uses [APKTool](https://github.com/iBotPeaches/Apktool/blob/master/LICENSE.md)
 - This project uses [Uber Apk Signer](https://github.com/patrickfav/uber-apk-signer/blob/main/LICENSE)
 - This project uses [LÖVE](https://github.com/love2d/love/blob/main/license.txt)
 - This project uses [Android Developer Bridge](https://developer.android.com/license)
 - This project uses [OpenJDK](https://openjdk.org/legal/gplv2+ce.html)
 - This project uses [DiffPatch](https://github.com/aberteau/DiffPatch/blob/master/LICENSE)
 - This project uses [SharpZipLib](https://github.com/icsharpcode/SharpZipLib/blob/master/LICENSE.txt)
