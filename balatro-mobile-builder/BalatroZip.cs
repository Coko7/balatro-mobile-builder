using ICSharpCode.SharpZipLib.Zip;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BalatroMobileBuilder
{
    public class BalatroZip
    {
        private static Dictionary<OSPlatform, string> exeLocations = new Dictionary<OSPlatform, string> {
            { OSPlatform.Windows, @"C:\Program Files (x86)\Steam\steamapps\common\Balatro\Balatro.exe" },
            { OSPlatform.Linux, $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.local/share/Steam/steamapps/common/Balatro/Balatro.exe" },
            { OSPlatform.OSX, $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Library/Application Support/Steam/steamapps/common/Balatro/Balatro.app/Contents/Resources/Balatro.love" }
        };
        private FastZip fastZip = new FastZip();

        public string? exePath { get; }
        public string extractPath { get; }

        /// <summary>Automatically searches for a Balatro executable.</summary>
        public BalatroZip(string extractPath = ".balatro.d") {
            this.extractPath = extractPath;

            if (File.Exists($"{Environment.CurrentDirectory}/Balatro.exe")) {
                this.exePath = $"{Environment.CurrentDirectory}/Balatro.exe";
                return;
            }
            if (File.Exists($"{Environment.CurrentDirectory}/game.love")) {
                this.exePath = $"{Environment.CurrentDirectory}/game.love";
                return;
            }
            foreach (var platform in exeLocations.Keys) {
                if (RuntimeInformation.IsOSPlatform(platform)) {
                    if (File.Exists(exeLocations[platform]))
                        this.exePath = exeLocations[platform];
                    break;
                }
            }
        }
        public BalatroZip(string exePath, string extractPath = ".balatro.d") {
            this.exePath = exePath;
            this.extractPath = extractPath;
        }

        public void extract() {
            if (this.exePath == null) throw new FileNotFoundException("Balatro not found");
            deleteExtractFolder();
            fastZip.ExtractZip(exePath, extractPath, null);
        }

        public bool deleteExtractFolder() {
            if (Directory.Exists(extractPath)) {
                Directory.Delete(extractPath, true);
                return true;
            }
            return false;
        }

        public void compress(string zipFile) {
            fastZip.CreateZip(zipFile, extractPath, true, null);
        }

        public string getStringVersion() {
            return File.ReadLines($"{this.extractPath}/version.jkr").FirstOrDefault("0.0.0a");
        }

        /**
         * <summary>
         * Returns the Balatro version in X.X.X.X format.
         * <example>Example: 1.0.1f -> 1.0.1.6</example>
         * </summary>
         */
        public Version getVersion() {
            string strVer = getStringVersion();
            Match match = new Regex(@"(\d+)\.(\d+)\.(\d+)(\w)?").Match(strVer);
            return new Version(
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value),
                match.Groups.Count > 4 ? match.Groups[4].Value[0] % 32 : 0
            );
        }
    }
}
