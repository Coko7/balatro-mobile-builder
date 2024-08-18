using BalatroMobileBuilder.Properties;
using DiffPatch;
using DiffPatch.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace BalatroMobileBuilder
{
    public static class BalatroPatches
    {
        public static List<BalatroPatch> patchList = [
            new BalatroPatch("common fixes", new Dictionary<string, string> {
                { "commonfixes_globals", "globals.lua" },
                { "commonfixes_button_callbacks", "functions/button_callbacks.lua" } }, true, true),
            new BalatroPatch("shader fixes", new Dictionary<string, string> {
                { "shaderfixes_background", "resources/shaders/background.fs" },
                { "shaderfixes_booster", "resources/shaders/booster.fs" },
                { "shaderfixes_crt", "resources/shaders/CRT.fs" },
                { "shaderfixes_debuff", "resources/shaders/debuff.fs" },
                { "shaderfixes_dissolve", "resources/shaders/dissolve.fs" },
                { "shaderfixes_flame", "resources/shaders/flame.fs" },
                { "shaderfixes_flash", "resources/shaders/flash.fs" },
                { "shaderfixes_foil", "resources/shaders/foil.fs" },
                { "shaderfixes_gold_seal", "resources/shaders/gold_seal.fs" },
                { "shaderfixes_holo", "resources/shaders/holo.fs" },
                { "shaderfixes_hologram", "resources/shaders/hologram.fs" },
                { "shaderfixes_negative", "resources/shaders/negative.fs" },
                { "shaderfixes_negative_shine", "resources/shaders/negative_shine.fs" },
                { "shaderfixes_played", "resources/shaders/played.fs" },
                { "shaderfixes_polychrome", "resources/shaders/polychrome.fs" },
                { "shaderfixes_skew", "resources/shaders/skew.fs" },
                { "shaderfixes_splash", "resources/shaders/splash.fs" },
                { "shaderfixes_vortex", "resources/shaders/vortex.fs" },
                { "shaderfixes_voucher", "resources/shaders/voucher.fs" } }, true, true),

            new BalatroPatch("external storage", new Dictionary<string, string> {
                { "externalstorage", "conf.lua" } }, false, true),

            new BalatroPatch("FPS cap", new Dictionary<string, string> {
                { "fpscap", "main.lua" } }),
            new BalatroPatch("landscape", new Dictionary<string, string> {
                { "landscape", "functions/button_callbacks.lua" } }),
            new BalatroPatch("high-dpi", new Dictionary<string, string> {
                { "highdpi_conf", "conf.lua" },
                { "highdpi_button_callbacks", "functions/button_callbacks.lua" } })
        ];

        public static void applyPatch(BalatroPatch patch, BalatroZip balaZip) {
            foreach (var patchInfo in patch.pathAssignedPatches) {
                object? resource = Resources.ResourceManager.GetObject(patchInfo.Key);
                ArgumentNullException.ThrowIfNull(resource);
                string patchContent = UTF8Encoding.UTF8.GetString((byte[])resource);

                string filePath = $"{balaZip.extractPath}/{patchInfo.Value}";
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                FileDiff[] diffs = DiffParserHelper.Parse(patchContent.ReplaceLineEndings("\n"), "\n").ToArray();
                foreach (FileDiff diff in diffs) {
                    fileContent = PatchHelper.Patch(fileContent.ReplaceLineEndings("\n"), diff.Chunks, "\n");
                }
                File.WriteAllText(filePath, fileContent);
            }
        }

        public static void setReleaseMode(bool value, BalatroZip balaZip) {
            string luaCode = File.ReadAllText($"{balaZip.extractPath}/conf.lua");
            luaCode = Regex.Replace(luaCode, @"_RELEASE_MODE\s*=.+", $"_RELEASE_MODE = {value.ToString().ToLower()}");
            File.WriteAllText($"{balaZip.extractPath}/conf.lua", luaCode);
        }
    }

    public readonly struct BalatroPatch
    {
        public readonly string name;
        public readonly Dictionary<string, string> pathAssignedPatches;
        public readonly bool defaultPromptAns;
        public readonly bool hidden;

        public BalatroPatch(string name, Dictionary<string, string> pathAssignedPatches, bool defaultPromptAns = true, bool hidden = false) {
            this.name = name;
            this.pathAssignedPatches = pathAssignedPatches;
            this.defaultPromptAns = defaultPromptAns;
            this.hidden = hidden;
        }
    }
}
