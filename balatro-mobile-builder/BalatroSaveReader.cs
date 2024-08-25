using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace BalatroMobileBuilder
{
    /**
     * <summary>
     * Balatro save files contain raw DEFLATE-compressed strings of Lua code
     * that return a table containing the data. This class has a simple
     * decoder and parser of its content.
     * </summary>
     */
    public class BalatroSaveReader
    {
        public static readonly Dictionary<OSPlatform, string> savePaths = new Dictionary<OSPlatform, string> {
            { OSPlatform.Windows, @$"{Environment.GetEnvironmentVariable("AppData")}\Balatro" },
            { OSPlatform.Linux, $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.local/share/Steam/steamapps/compatdata/2379780/pfx/drive_c/users/steamuser/AppData/Roaming/Balatro" },
            { OSPlatform.OSX, $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Library/Application Support/Balatro" } };

        public readonly string saveContent;

        public BalatroSaveReader(Stream stream) {
            saveContent = decompressDeflate(stream);
        }

        public string? getField(string name, string? parent = null) {
            string pattern = $@"\[""{name}""\]=(.+?),";
            if (parent != null)
                pattern = $@"\[""{parent}""\]={{.*?" + pattern;
            Match match = new Regex(pattern).Match(saveContent);
            return match.Groups.Count > 1 ? match.Groups[1].Value : null;
        }

        public double? getNumber(string name, string? parent = null) {
            string? field = getField(name, parent);
            double num;
            return double.TryParse(field?.Replace('.', ','), out num) ? num : null;
        }

        public double? getOverallProgress() {
            double? tally = getNumber("overall_tally");
            double? of = getNumber("overall_of");
            if (tally != null && of != null && of != 0.0) {
                return tally / of;
            }
            return null;
        }

        public static BalatroSaveReader? local(int saveNum, string type) {
            string filePath = $"{getLocalSavePath()}/{saveNum}/{type}.jkr";
            if (!File.Exists(filePath)) return null;
            BalatroSaveReader saveReader;
            using (FileStream compressStream = File.OpenRead(filePath)) {
                saveReader = new BalatroSaveReader(compressStream);
            }
            return saveReader;
        }

        public static string? getLocalSavePath() {
            foreach (OSPlatform platform in savePaths.Keys) {
                if (RuntimeInformation.IsOSPlatform(platform)) {
                    return savePaths[platform];
                }
            }
            return null;
        }

        public static string decompressDeflate(Stream compressStream) {
            byte[] decompressedArray;
            using (MemoryStream decompressedStream = new MemoryStream()) {
                using (DeflateStream deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress)) {
                    deflateStream.CopyTo(decompressedStream);
                }
                decompressedArray = decompressedStream.ToArray();
            }
            return Encoding.UTF8.GetString(decompressedArray);
        }
    }
}
