using BalatroMobileBuilder.Properties;
using System.Text;
using System.Text.RegularExpressions;

namespace BalatroMobileBuilder
{
    public static class BalatroPatches
    {
        /**
         * <summary>
         * A "BalatroPatch" is actually a list of .patch files that will
         * be applied to different files. An id and a name must be specified for
         * each BalatroPatch.
         * </summary>
         */
        public static List<BalatroPatch> patchList = [
            new BalatroPatch("commonfixes", "Common Fixes", new Dictionary<string, string> {
                { "commonfixes_globals", "globals.lua" },
                { "commonfixes_button_callbacks", "functions/button_callbacks.lua" } }, true, true),
            new BalatroPatch("shaderfixes", "Shader Fixes", new Dictionary<string, string> {
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

            new BalatroPatch("externalstorage", "External Storage", new Dictionary<string, string> {
                { "externalstorage", "conf.lua" } }, false, true),

            new BalatroPatch("fpscap", "FPS cap", new Dictionary<string, string> {
                { "fpscap", "main.lua" } }),
            new BalatroPatch("landscape", "Landscape", new Dictionary<string, string> {
                { "landscape", "functions/button_callbacks.lua" } }),
            new BalatroPatch("highdpi", "High DPI", new Dictionary<string, string> {
                { "highdpi_conf", "conf.lua" },
                { "highdpi_button_callbacks", "functions/button_callbacks.lua" } })
        ];

        public static bool applyPatch(BalatroPatch patch, BalatroZip balaZip) {
            bool err = false;
            foreach (var patchInfo in patch.pathAssignedPatches) {
                // Get file content
                string filePath = $"{balaZip.extractPath}/{patchInfo.Value}";
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                // Get patch file from assembly resources
                object? resource = Resources.ResourceManager.GetObject(patchInfo.Key);
                ArgumentNullException.ThrowIfNull(resource);
                string patchFile = UTF8Encoding.UTF8.GetString((byte[])resource);

                // Patch the specified file
                List<UnifiedPatch.Hunk> hunks = UnifiedPatch.parseText(patchFile);
                (fileContent, bool[] results) = UnifiedPatch.apply(fileContent, hunks);
                File.WriteAllText(filePath, fileContent);

                err |= results.Contains(false);
            }
            return err;
        }

        public static void setReleaseMode(bool value, BalatroZip balaZip) {
            string luaCode = File.ReadAllText($"{balaZip.extractPath}/conf.lua");
            luaCode = Regex.Replace(luaCode, @"_RELEASE_MODE\s*=.+", $"_RELEASE_MODE = {value.ToString().ToLower()}");
            File.WriteAllText($"{balaZip.extractPath}/conf.lua", luaCode);
        }
    }

    public readonly struct BalatroPatch
    {
        public readonly string id;
        public readonly string name;
        public readonly Dictionary<string, string> pathAssignedPatches;
        public readonly bool defaultPromptAns;
        public readonly bool hidden;

        public BalatroPatch(string id, string name, Dictionary<string, string> pathAssignedPatches, bool defaultPromptAns = true, bool hidden = false) {
            this.id = id;
            this.name = name;
            this.pathAssignedPatches = pathAssignedPatches;
            this.defaultPromptAns = defaultPromptAns;
            this.hidden = hidden;
        }
    }
}
