using BalatroMobileBuilder;
using System.Runtime.InteropServices;

Console.WriteLine("[Balatro Mobile Builder]");

// Additional parameters
bool silentMode = false;
string? platformParam = null;
string? outFilePath = null;
for (int i = 0; i < args.Length; i++) {
    switch(args[i]) {
    case "-s":
        if (args.Length > i + 1) {
            silentMode = true;
            platformParam = args[i + 1].ToLower();
        }
        break;
    case "-o":
        if (args.Length > i + 1) {
            outFilePath = args[i + 1];
        }
        break;
    }
}

// Search Balatro.exe (or game.love) and extract
BalatroZip balaZip = new BalatroZip();
if (balaZip.exePath == null) {
    MiscUtils.printError("Balatro not found. Please copy Balatro.exe inside the current folder.");
    return;
}

Console.WriteLine("Extracting...");
balaZip.extract();

/* Apply patches */
foreach (BalatroPatch patch in BalatroPatches.patchList) {
    if (MiscUtils.askQuestion(@$"Apply ""{patch.name}"" patch", patch.hidden || silentMode, patch.defaultPromptAns)) {
        BalatroPatches.applyPatch(patch, balaZip);
    }
}

#if DEBUG
BalatroPatches.setReleaseMode(false, balaZip);
if (!silentMode) {
    while (MiscUtils.askQuestion("Run a test", silentMode, false)) {
        try {
            MiscUtils.startAndWaitPrc(new("love", balaZip.extractPath));
        } catch (Exception) {
            MiscUtils.printError("Couldn't execute love.");
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
    buildIOS = MiscUtils.askQuestion("Build for iOS", silentMode, false);

try {
    if (buildIOS) {
        /* iOS Build */
        IOSBuilder iOSBuilder = new IOSBuilder();
        iOSBuilder.downloadMissing().Wait();
        outFilePath = iOSBuilder.build(balaZip, outFilePath);
        if (MiscUtils.askQuestion("Delete downloaded building tools", silentMode, true)) {
            iOSBuilder.deleteTools();
        }
    } else {
        /* Android Build */
        AndroidBuilder apkBuilder = new AndroidBuilder();
        apkBuilder.downloadMissing();
        string builtApk = apkBuilder.build(balaZip);
        outFilePath = apkBuilder.sign(builtApk, outFilePath);
        if (MiscUtils.askQuestion("Delete downloaded building tools", silentMode, true)) {
            apkBuilder.deleteTools();
        }
    }
} catch (AggregateException e) {
    MiscUtils.printError(e.InnerException is HttpRequestException ?
        "Download interrupted." : e.ToString());
    return;
}

balaZip.deleteExtractFolder();
Console.WriteLine($"Done! The app can be found at {outFilePath}");

/* Automatic installation */
if (RuntimeInformation.OSArchitecture == Architecture.X64 && !buildIOS) {
    if (MiscUtils.askQuestion("Install to your Android device through USB", silentMode, !silentMode)) {
        AndroidBalatroBridge balaBridge = new AndroidBalatroBridge(); 
        try {
            balaBridge.downloadMissing().Wait();
        } catch (AggregateException e) {
            MiscUtils.printError(e.InnerException is HttpRequestException ?
                "Download interrupted." : e.ToString());
            return;
        }

        balaBridge.installApk(outFilePath);

        if (MiscUtils.askQuestion("Copy saves to your device", silentMode, true)) {
            balaBridge.copySavesToDevice();
            Console.WriteLine("Done!");
        }

        balaBridge.askToDeleteTools(silentMode);
    }
}