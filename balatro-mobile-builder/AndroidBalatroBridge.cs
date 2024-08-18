using System.Runtime.InteropServices;

namespace BalatroMobileBuilder
{
    public class AndroidBalatroBridge
    {
        private static Dictionary<OSPlatform, string> savePaths = new Dictionary<OSPlatform, string> {
            { OSPlatform.Windows, @$"{Environment.GetEnvironmentVariable("AppData")}\Balatro" },
            { OSPlatform.Linux, $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.local/share/Steam/steamapps/compatdata/2379780/pfx/drive_c/users/steamuser/AppData/Roaming/Balatro" },
            { OSPlatform.OSX, $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Library/Application Support/Balatro" } };
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
            if (adb.wasDownloaded && MiscUtils.askQuestion("Delete ADB", silentMode, true)) {
                adb.deleteTool();
            }
        }

        public void installApk(string signedApk) {
            Console.WriteLine("Establishing connection to a device... Please ensure that your smartphone is connected and that USB debugging is enabled.");
            adb.waitFor();
            Console.WriteLine("Connected. Installing...");
            int result = adb.install(signedApk);
            if (result != 0) {
                MiscUtils.printError($"{adb.name} returned {result}");
                Environment.Exit(result);
            }
            adb.killServer();
        }

        public void copySavesToDevice() {
            string? savePath = null;
            foreach (OSPlatform platform in savePaths.Keys) {
                if (RuntimeInformation.IsOSPlatform(platform)) {
                    savePath = savePaths[platform];
                    break;
                }
            }
            if (!Directory.Exists(savePath)) {
                MiscUtils.printError("Couldn't find save directory.");
                return;
            }

            adb.push($"{savePath}/.", "/data/local/tmp/balatro/saves/");
            adb.runShell("am force-stop com.unofficial.balatro");
            adb.runShell("cp -r /data/local/tmp/balatro/saves/ ./files/save/game/", "com.unofficial.balatro");
            adb.killServer();
        }
    }
}
