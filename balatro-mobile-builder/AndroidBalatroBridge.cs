using System.Globalization;

namespace BalatroMobileBuilder
{
    public class AndroidBalatroBridge
    {
        public ExternalTool.ADB adb;

        public AndroidBalatroBridge() {
            this.adb = new ExternalTool.ADB();
        }

        public async Task downloadMissing() {
            if (adb.path != null) return;
            Console.WriteLine($"Starting to download {adb.name}...");
            await adb.downloadTool();
            Console.WriteLine($"Finished downloading {adb.name}.");
        }

        public void askToDeleteTools(bool silentMode) {
            if (adb.wasDownloaded && ConsoleInter.askQuestion("Delete ADB", silentMode, true)) {
                adb.deleteTool();
            }
        }

        public void installApk(string signedApk) {
            int result = adb.install(signedApk);
            if (result != 0) {
                ConsoleInter.printError($"{adb.name} returned {result}");
                Environment.Exit(result);
            }
            adb.killServer();
        }

        public bool copySaveToDevice(int saveNum, bool ignoreNonExistent = true, string? savePath = null) {
            if (savePath == null)
                savePath = BalatroSaveReader.getLocalSavePath();
            if (!Directory.Exists($"{savePath}/{saveNum}")) {
                if (ignoreNonExistent) return true;
                ConsoleInter.printError("Couldn't find save directory.");
                return false;
            }

            // Clean and prepare folders
            adb.runShell("rm -r /data/local/tmp/balatro/");
            adb.runShell($"mkdir -p /data/local/tmp/balatro/{saveNum}/");
            adb.runShell($"mkdir -p ./files/save/game/{saveNum}/", "com.unofficial.balatro");
            // Copy
            int errCheck = adb.push($"{savePath}/{saveNum}/.", $"/data/local/tmp/balatro/{saveNum}/");
            adb.runShell("am force-stop com.unofficial.balatro"); // Stop Balatro process
            adb.runShell($"rm ./files/save/game/{saveNum}/save.jkr", "com.unofficial.balatro"); // Remove save.jkr as it may not be present (and not overwritten)
            errCheck |= adb.runShell($"cp -r /data/local/tmp/balatro/{saveNum} ./files/save/game", "com.unofficial.balatro")?.ExitCode ?? 1;
            adb.killServer();

            return errCheck == 0;
        }

        public bool copySaveFromDevice(int saveNum, string? savePath = null) {
            if (savePath == null)
                savePath = BalatroSaveReader.getLocalSavePath();
            if (!Directory.Exists(savePath)) {
                ConsoleInter.printError("Couldn't find save directory.");
                return false;
            }

            // Clean and prepare folders
            Directory.CreateDirectory($"{savePath}/{saveNum}");
            adb.runShell("rm -r /data/local/tmp/balatro/");
            adb.runShell($"mkdir -p /data/local/tmp/balatro/{saveNum}/");
            adb.runShell($"mkdir -p ./files/save/game/{saveNum}/", "com.unofficial.balatro");
            // Copy
            int errCheck = adb.runShell($"cat ./files/save/game/{saveNum}/profile.jkr > /data/local/tmp/balatro/{saveNum}/profile.jkr", "com.unofficial.balatro")?.ExitCode ?? 1;
            errCheck |= adb.runShell($"cat ./files/save/game/{saveNum}/meta.jkr > /data/local/tmp/balatro/{saveNum}/meta.jkr", "com.unofficial.balatro")?.ExitCode ?? 1;
            adb.runShell($"cat ./files/save/game/{saveNum}/save.jkr > /data/local/tmp/balatro/{saveNum}/save.jkr", "com.unofficial.balatro");
            File.Delete($"{savePath}/{saveNum}/save.jkr"); // Remove save.jkr as it may not be present (and not overwritten)
            errCheck |= adb.pull($"/data/local/tmp/balatro/{saveNum}/.", $"{savePath}/{saveNum}/");
            adb.killServer();

            return errCheck == 0;
        }

        public BalatroSaveReader? readSaveFile(int saveNum, string type) {
            // Read file as hex string using xxd and then convert to binary
            string? hex = null;
            adb.runShell($"xxd -c 0 -p files/save/game/{saveNum}/{type}.jkr", out hex, "com.unofficial.balatro");
            if (hex != null) {
                hex = hex.Trim();
                byte[] fileContent = Enumerable.Range(0, hex.Length / 2)
                    .Select(x => Byte.Parse(hex.Substring(2 * x, 2), NumberStyles.HexNumber))
                    .ToArray();
                using (MemoryStream fileStream = new MemoryStream(fileContent)) {
                    return new BalatroSaveReader(fileStream);
                }
            }
            return null;
        }
    }
}
