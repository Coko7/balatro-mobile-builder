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
            if (adb.wasDownloaded && BuilderConInter.askQuestion("Delete ADB", silentMode, true)) {
                adb.deleteTool();
            }
        }

        public static string? getSavePath() {
            foreach (OSPlatform platform in savePaths.Keys) {
                if (RuntimeInformation.IsOSPlatform(platform)) {
                    return savePaths[platform];
                }
            }
            return null;
        }

        public void installApk(string signedApk) {
            Console.WriteLine("Establishing connection to a device... Please ensure that your smartphone is connected and that USB debugging is enabled.");
            adb.waitFor();
            Console.WriteLine("Connected. Installing...");
            int result = adb.install(signedApk);
            if (result != 0) {
                BuilderConInter.printError($"{adb.name} returned {result}");
                Environment.Exit(result);
            }
            adb.killServer();
        }

        public bool copySavesToDevice() {
            string? savePath = getSavePath();
            if (!Directory.Exists(savePath)) {
                BuilderConInter.printError("Couldn't find save directory.");
                return false;
            }

            int errCheck = 0;
            errCheck |= adb.push($"{savePath}/.", "/data/local/tmp/balatro/saves/");
            errCheck |= adb.runShell("am force-stop com.unofficial.balatro");
            errCheck |= adb.runShell("cp -r /data/local/tmp/balatro/saves/ ./files/save/game/", "com.unofficial.balatro");
            adb.killServer();

            return errCheck == 0;
        }
    }
}
