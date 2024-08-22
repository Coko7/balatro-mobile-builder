using BalatroMobileBuilder;

Console.WriteLine("[Balatro Mobile Builder]");

// Additional parameters
bool silentMode = false;
string? platformParam = null;
string? outFilePath = null;
List<string> selectedPatches = new List<string>(0);
for (int i = 0; i < args.Length; i++) {
    switch (args[i]) {
    case "/s":
    case "-s":
        silentMode = true;
        if (args.Length > i + 1
            && !args[i + 1].StartsWith('-') && !args[i + 1].StartsWith('/')) {
            platformParam = args[i + 1].ToLower();
        }
        break;
    case "/o":
    case "-o":
        if (args.Length > i + 1) {
            outFilePath = args[i + 1];
        }
        break;
    case "/p":
    case "-p":
        for (int j = i + 1; j < args.Length; j++) {
            if (args[j].StartsWith('-') || args[j].StartsWith('/'))
                break;
            selectedPatches.Add(args[j]);
        }
        break;
    }
}

if (ConInterface.askQuestion("Open the Android save manager", silentMode, false))
    ConInterface.saveManager(silentMode, platformParam);
else
    ConInterface.buildManager(silentMode, selectedPatches, platformParam, outFilePath);