using System.Runtime.InteropServices;

namespace BalatroMobileBuilder
{
    internal static class ConsoleInter
    {
        public static void saveManager(bool silentMode, string? platformParam) {
            if (platformParam == "ios") {
                printError("iOS saves copying isn't supported.");
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
            if (askQuestion("Sync saves between devices based on overall progression", silentMode, true)) {
                balaBridge.adb.waitFor();
                for (int i = 1; i <= 3; i++) {
                    double? localProgress = BalatroSaveReader.local(i, "profile")?.getOverallProgress();
                    double? deviceProgress = balaBridge.readSaveFile(i, "profile")?.getOverallProgress();
                    if (localProgress == deviceProgress) continue;

                    if (localProgress != null && (deviceProgress == null || localProgress > deviceProgress)) {
                        copySuccess &= balaBridge.copySaveToDevice(i, false);
                    } else if (deviceProgress != null) {
                        copySuccess &= balaBridge.copySaveFromDevice(i);
                    }
                }
            } else if (askQuestion("Copy local saves to device", silentMode)) {
                balaBridge.adb.waitFor();
                for (int i = 1; i <= 3; i++) {
                    copySuccess &= balaBridge.copySaveToDevice(i);
                }
            } else if (askQuestion("Copy device saves locally", silentMode)) {
                balaBridge.adb.waitFor();
                for (int i = 1; i <= 3; i++) {
                    copySuccess &= balaBridge.copySaveFromDevice(i);
                }
            }

            if (copySuccess)
                Console.WriteLine("Done!");
            else
                printError("Couldn't copy properly.");
            
            balaBridge.askToDeleteTools(silentMode);
        }

        public static void buildManager(bool silentMode, string? platformParam, string? outFilePath) {
            // Search Balatro.exe (or game.love) and extract
            BalatroZip balaZip = new BalatroZip();
            if (balaZip.exePath == null) {
                printError("Balatro not found. Please copy Balatro.exe inside the current folder.");
                return;
            }

            Console.WriteLine("Extracting...");
            balaZip.extract();

            /* Apply patches */
            foreach (BalatroPatch patch in BalatroPatches.patchList) {
                if (askQuestion(@$"Apply ""{patch.name}"" patch", patch.hidden || silentMode, patch.defaultPromptAns)) {
                    BalatroPatches.applyPatch(patch, balaZip);
                }
            }

#if DEBUG
            if (!silentMode) {
                BalatroPatches.setReleaseMode(false, balaZip);
                while (askQuestion("Run a test", silentMode, false)) {
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
                buildIOS = askQuestion("Build for iOS", silentMode, false);

            try {
                if (buildIOS) {
                    /* iOS Build */
                    IOSBuilder iOSBuilder = new IOSBuilder();
                    iOSBuilder.downloadMissing().Wait();
                    outFilePath = iOSBuilder.build(balaZip, outFilePath);
                    if (askQuestion("Delete downloaded building tools", silentMode, true)) {
                        iOSBuilder.deleteTools();
                    }
                } else {
                    /* Android Build */
                    AndroidBuilder apkBuilder = new AndroidBuilder();
                    apkBuilder.downloadMissing();
                    string builtApk = apkBuilder.build(balaZip);
                    outFilePath = apkBuilder.sign(builtApk, outFilePath);
                    if (askQuestion("Delete downloaded building tools", silentMode, true)) {
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
                if (askQuestion("Install to your Android device through USB", silentMode, !silentMode)) {
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

                    Console.WriteLine("Connected. Installing...");
                    balaBridge.installApk(outFilePath);

                    if (askQuestion("Copy saves to your device", silentMode, true)) {
                        bool copySuccess = true;
                        for (int i = 1; i <= 3; i++) {
                            copySuccess &= balaBridge.copySaveToDevice(i);
                        }
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

        public static bool askQuestion(string question, bool silentMode, bool? defaultAnswer = null) {
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
