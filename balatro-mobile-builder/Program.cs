using BalatroMobileBuilder;
using System.Reflection;

Console.WriteLine($"Balatro Mobile Builder {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}");

// Additional parameters
bool silentMode = false;
string? platformParam = null;
string? outFilePath = null;
List<string> selectedPatches = new List<string>(0);
string? savesTransferMode = null;

for (int i = 0; i < args.Length; i++) {
    switch (args[i]) {
    case "/s":
    case "-s":
        // -s [android/ios]
        silentMode = true;
        if (args.Length > i + 1)
            platformParam = args[i + 1].ToLower();
        break;
    case "/o":
    case "-o":
        // -o FILE
        if (args.Length > i + 1) 
            outFilePath = args[i + 1];
        break;
    case "/p":
    case "-p":
        // -p commonfixes externalstorage ...
        for (int j = i + 1; j < args.Length; j++) {
            if (args[j].StartsWith('-') || args[j].StartsWith('/'))
                break;
            selectedPatches.Add(args[j]);
        }
        break;
    case "/save":
    case "-save":
        // -save [auto/device/local]
        if (platformParam == null)
            platformParam = "android";
        if (args.Length > i + 1)
            savesTransferMode = args[i + 1];
        if (savesTransferMode != "device" && savesTransferMode != "local")
            savesTransferMode = "auto";
        break;
    }
}

if (ConInterface.ask("Open the Android save manager", silentMode, savesTransferMode != null))
    ConInterface.saveManager(silentMode, platformParam, savesTransferMode ?? "auto");
else
    ConInterface.buildManager(silentMode, selectedPatches, platformParam, outFilePath);