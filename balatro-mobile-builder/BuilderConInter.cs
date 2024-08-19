using System.Runtime.InteropServices;

namespace BalatroMobileBuilder
{
    internal static class BuilderConInter
    {
        public static void buildPrompts(bool silentMode, string? outFilePath, string? platformParam) {
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
            BalatroPatches.setReleaseMode(false, balaZip);
            if (!silentMode) {
                while (askQuestion("Run a test", silentMode, false)) {
                    try {
                        ExternalTool.startAndWaitPrc(new("love", balaZip.extractPath));
                    } catch (Exception) {
                        printError("Couldn't execute love.");
                    }
                }
            }
            BalatroPatches.setReleaseMode(true, balaZip);
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

                    balaBridge.installApk(outFilePath);

                    if (askQuestion("Copy saves to your device", silentMode, true)) {
                        if (balaBridge.copySavesToDevice())
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
