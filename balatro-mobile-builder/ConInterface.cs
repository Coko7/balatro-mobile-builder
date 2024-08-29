using System.Runtime.InteropServices;

namespace BalatroMobileBuilder
{
    internal interface ConInterface
    {
        public static void saveManager(bool silentMode, string? platformParam, string savesTransferMode = "auto") {
            if (platformParam == "ios") {
                printError("iOS saves copying isn't supported.");
                return;
            }
            if (RuntimeInformation.OSArchitecture != Architecture.X64) {
                printError("Saves copying is supported only on x64 devices.");
                return;
            }

            AndroidBalatroBridge balaBridge = new AndroidBalatroBridge();
            try {
                balaBridge.downloadMissing().Wait();
            } catch (AggregateException e) {
                printError(e.InnerException is HttpRequestException ?
                    "Download interrupted." : e.ToString());
                return;
            }

            Console.WriteLine("Establishing connection to device... Ensure that your smartphone is connected and that USB debugging is enabled.");
            balaBridge.adb.waitFor();

            bool copySuccess = true;
            if (ask("Sync saves between devices based on overall progression", silentMode, savesTransferMode == "auto")) {
                balaBridge.adb.waitFor();
                for (int i = 1; i <= 3; i++) {
                    double? localProgress = BalatroSaveReader.local(i, "profile")?.getOverallProgress();
                    double? deviceProgress = balaBridge.readSaveFile(i, "profile")?.getOverallProgress();

                    // Copy to device if local overall progress is higher, else copy from device
                    if (localProgress == deviceProgress) continue;
                    if (localProgress != null && (deviceProgress == null || localProgress > deviceProgress)) {
                        copySuccess &= balaBridge.copySaveToDevice(i, false);
                    } else if (deviceProgress != null) {
                        copySuccess &= balaBridge.copySaveFromDevice(i);
                    }
                }
            } else if (ask("Copy local saves to device", silentMode, savesTransferMode == "device")) {
                balaBridge.adb.waitFor();
                for (int i = 1; i <= 3; i++) {
                    copySuccess &= balaBridge.copySaveToDevice(i);
                }
            } else if (ask("Copy device saves locally", silentMode, savesTransferMode == "local")) {
                balaBridge.adb.waitFor();
                for (int i = 1; i <= 3; i++) {
                    copySuccess &= balaBridge.copySaveFromDevice(i);
                }
            }

            if (copySuccess)
                Console.WriteLine("Done!");
            else
                printError("Couldn't copy properly.");

            balaBridge.adb.killServer();
            balaBridge.askToDeleteTools(silentMode);
        }

        public static void buildManager(bool silentMode, List<string> selectedPatches, string? platformParam, string? outFilePath) {
            // Search Balatro.exe (or game.love) and extract
            BalatroZip balaZip = new BalatroZip();
            if (balaZip.exePath == null) {
                printError("Balatro not found. Please copy Balatro.exe inside the current folder.");
                return;
            }

            Console.WriteLine("Extracting...");
            balaZip.extract();

            /* Apply patches */
            if (silentMode)
                Console.WriteLine("Applying patches...");
            if (selectedPatches.Count > 0) {
                foreach (BalatroPatch patch in BalatroPatches.patchList) {
                    if (selectedPatches.Contains(patch.id)) {
                        if (BalatroPatches.applyPatch(patch, balaZip))
                            printError($"Couldn't apply {patch.id} patch properly.");
                    }
                }
            } else {
                foreach (BalatroPatch patch in BalatroPatches.patchList) {
                    if (ask($"Apply {patch.name} patch", patch.hidden || silentMode, patch.defaultPromptAns)) {
                        if (BalatroPatches.applyPatch(patch, balaZip))
                            printError($"Couldn't apply {patch.id} patch properly.");
                    }
                }
            }

#if DEBUG
            if (!silentMode) {
                BalatroPatches.setReleaseMode(false, balaZip);
                while (ask("Run a test", silentMode, false)) {
                    try {
                        ExternalTool.startAndWaitPrc(new("love", balaZip.extractPath));
                    } catch (Exception) {
                        printError("Couldn't execute love.");
                    }
                }
                BalatroPatches.setReleaseMode(true, balaZip);
            }
#endif

            // Build app
            bool buildIOS = false;
            if (platformParam == "ios")
                buildIOS = true;
            else if (platformParam != "android")
                buildIOS = ask("Build for iOS", silentMode, false);

            try {
                if (buildIOS) {
                    /* iOS Build */
                    IOSBuilder iOSBuilder = new IOSBuilder();
                    iOSBuilder.downloadMissing().Wait();
                    outFilePath = iOSBuilder.build(balaZip, outFilePath);
                    if (ask("Delete downloaded building tools", silentMode, true)) {
                        iOSBuilder.deleteTools();
                    }
                } else {
                    /* Android Build */
                    AndroidBuilder apkBuilder = new AndroidBuilder();
                    apkBuilder.downloadMissing();
                    string builtApk = apkBuilder.build(balaZip);
                    outFilePath = apkBuilder.sign(builtApk, outFilePath);
                    if (ask("Delete downloaded building tools", silentMode, true)) {
                        apkBuilder.deleteTools();
                    }
                }
            } catch (AggregateException e) {
                printError(e.InnerException is HttpRequestException ?
                    "Download interrupted." : e.ToString());
                return;
            }

            balaZip.deleteExtractFolder();
            Console.WriteLine($"Done! The app can be found at {outFilePath}");

            /* Automatic installation */
            if (RuntimeInformation.OSArchitecture == Architecture.X64 && !buildIOS) {
                if (ask("Install to your Android device through USB", silentMode, !silentMode)) {
                    AndroidBalatroBridge balaBridge = new AndroidBalatroBridge();
                    try {
                        balaBridge.downloadMissing().Wait();
                    } catch (AggregateException e) {
                        printError(e.InnerException is HttpRequestException ?
                            "Download interrupted." : e.ToString());
                        return;
                    }

                    Console.WriteLine("Establishing connection to device... Ensure that your smartphone is connected and that USB debugging is enabled.");
                    balaBridge.adb.waitFor();

                    string? oldPackagesList = null;
                    balaBridge.adb.runShell("pm list packages com.unofficial.balatro", out oldPackagesList);

                    Console.WriteLine("Installing...");
                    balaBridge.installApk(outFilePath);

                    if (ask("Copy local saves to your device", silentMode, !oldPackagesList?.Contains("com.unofficial.balatro"))) {
                        balaBridge.adb.waitFor();
                        bool copySuccess = true;
                        for (int i = 1; i <= 3; i++) {
                            copySuccess &= balaBridge.copySaveToDevice(i);
                        }
                        balaBridge.adb.killServer();
                        if (copySuccess)
                            Console.WriteLine("Done!");
                        else
                            printError("Couldn't copy properly.");
                    }

                    balaBridge.askToDeleteTools(silentMode);
                    Console.WriteLine($"Deleting {new FileInfo(outFilePath).Name}...");
                    File.Delete(outFilePath);
                }
            }
        }

        public static void printError(string err) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {err}");
            Console.ResetColor();
        }

        public static bool ask(string question, bool silentMode, bool? defaultAnswer = null) {
            if (silentMode && defaultAnswer != null)
                return (bool)defaultAnswer;

            if (defaultAnswer == null)
                question += " (y/n)? ";
            else
                question += (bool)defaultAnswer ? " ([y]/n)? " : " (y/[n])? ";

            string? ans;
            do {
                Console.WriteLine(question);
                ans = Console.ReadLine()?.ToLower();
                if (ans == "y" || ans == "yes") return true;
                if (ans == "n" || ans == "no") return false;
            } while (!string.IsNullOrWhiteSpace(ans) || defaultAnswer == null);
            return (bool)defaultAnswer;
        }
    }
}
