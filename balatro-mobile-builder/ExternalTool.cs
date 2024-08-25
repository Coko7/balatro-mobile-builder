using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BalatroMobileBuilder
{
    public abstract class ExternalTool
    {
        public abstract string name { get; }
        public string? path { get; protected set; }
        public bool wasDownloaded { get; protected set; } = false;

        public abstract Task downloadTool();

        /// <summary>Deletes the tool if the wasDownloaded flag is set to true.</summary>
        public abstract void deleteTool();

        public static Process? startAndWaitPrc(ProcessStartInfo info, out string? output, out string? errors, bool printOut = true, bool printErr = true) {
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Process? prc = Process.Start(info);

            string? localOut = null, localErr = null;
            if (prc != null) {
                prc.OutputDataReceived += new DataReceivedEventHandler((sender, e) => {
                    localOut += e.Data;
                    if (printOut && !string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                });
                prc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {
                    localErr += e.Data;
                    if (printErr && !string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                });
                prc.BeginErrorReadLine();
                prc.BeginOutputReadLine();
            }

            prc?.WaitForExit();
            Console.ResetColor();

            output = localOut;
            errors = localErr;
            return prc;
        }

        public static Process? startAndWaitPrc(ProcessStartInfo info, bool printOut = true, bool printErr = true) {
            return startAndWaitPrc(info, out _, out _, printOut, printErr);
        }

        public static async Task downloadFile(string url, string path) {
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)) {
                using (FileStream fStream = File.Create(path))
                using (Stream dlStream = await response.Content.ReadAsStreamAsync()) {
                    await dlStream.CopyToAsync(fStream);
                }
            }
        }

        public class JDK : ExternalTool
        {
            public override string name { get; } = "OpenJDK";
            public string? homePath { get; private set; } = null;

            private FastZip fastZip = new FastZip();
            private static Dictionary<OSPlatform, Dictionary<Architecture, string>> urls = new Dictionary<OSPlatform, Dictionary<Architecture, string>> {
                { OSPlatform.Windows, new Dictionary<Architecture, string> {
                    { Architecture.X64, "https://aka.ms/download-jdk/microsoft-jdk-21.0.3-windows-x64.zip" },
                    { Architecture.Arm64, "https://aka.ms/download-jdk/microsoft-jdk-21.0.3-windows-aarch64.zip" }
                } },
                { OSPlatform.Linux, new Dictionary<Architecture, string> {
                    { Architecture.X64, "https://aka.ms/download-jdk/microsoft-jdk-21.0.3-linux-x64.tar.gz" },
                    { Architecture.Arm64, "https://aka.ms/download-jdk/microsoft-jdk-21.0.3-linux-aarch64.tar.gz" }
                } },
                { OSPlatform.OSX, new Dictionary<Architecture, string> {
                    { Architecture.X64, "https://aka.ms/download-jdk/microsoft-jdk-21.0.3-macos-x64.tar.gz" },
                    { Architecture.Arm64, "https://aka.ms/download-jdk/microsoft-jdk-21.0.3-macos-aarch64.tar.gz" }
                } }
            };

            public JDK() {
                if (Directory.Exists($"{Environment.CurrentDirectory}/.jdk/bin")) {
                    this.path = $"{Environment.CurrentDirectory}/.jdk/bin/java";
                    return;
                }

                // Check in the JAVA_HOME env var
                string? javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (Directory.Exists(javaHome)) {
                    this.homePath = javaHome;
                    this.path = javaHome + "/bin/java";
                }
            }
            public JDK(string path) { this.path = path; }

            public Process? runJar(string jar, string javaArgs = "") {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");
                return startAndWaitPrc(new(this.path, $"{javaArgs} -jar {jar}"));
            }

            public override async Task downloadTool() {
                string? dlUrl = null;
                foreach (OSPlatform platform in urls.Keys) {
                    if (RuntimeInformation.IsOSPlatform(platform)) {
                        dlUrl = urls[platform][RuntimeInformation.OSArchitecture];
                        break;
                    }
                }
                if (dlUrl == null) throw new PlatformNotSupportedException();

                string dlPath = $"{Environment.CurrentDirectory}/.jdkdl";
                await downloadFile(dlUrl, $"{dlPath}.tmp");

                if (Directory.Exists(dlPath)) Directory.Delete(dlPath, true);
                try {
                    // Extract zip file
                    fastZip.ExtractZip($"{dlPath}.tmp", dlPath, null);
                } catch (ZipException) {
                    // Must be a .tar.gz then
                    using (FileStream gzFile = File.OpenRead($"{dlPath}.tmp"))
                    using (Stream gzipStream = new GZipInputStream(gzFile)) {
                        TarArchive tar = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8);
                        tar.ExtractContents(dlPath);
                        tar.Close();
                    }
                }

                string homePath = $"{Environment.CurrentDirectory}/.jdk";
                if (Directory.Exists(homePath)) Directory.Delete(homePath, true);

                // Move downloaded folder
                string? dlJdkPath = Directory.GetDirectories(dlPath)
                    .Where((pth) => new DirectoryInfo(pth).Name.StartsWith("jdk"))?.First();
                if (dlJdkPath == null) throw new DirectoryNotFoundException();
                Directory.Move(dlJdkPath, homePath);
                this.homePath = homePath;
                this.path = homePath + "/bin/java";
                this.wasDownloaded = true;

                // Cleanup
                Directory.Delete(dlPath, true);
                File.Delete($"{dlPath}.tmp");
            }

            public override void deleteTool() {
                if (!this.wasDownloaded) return;
                if (Directory.Exists(this.homePath))
                    Directory.Delete(this.homePath, true);
            }
        }

        public class Apktool : ExternalTool
        {
            public override string name { get; } = "Apktool";
            public JDK jdk { get; }
            private static string url = "https://github.com/iBotPeaches/Apktool/releases/download/v2.9.3/apktool_2.9.3.jar";

            public Apktool(JDK jdk) {
                this.jdk = jdk;
                if (this.path == null && File.Exists($"{Environment.CurrentDirectory}/apktool.jar")) {
                    this.path = $"{Environment.CurrentDirectory}/apktool.jar";
                }
            }
            public Apktool(string path, JDK jdk) {
                this.path = path;
                this.jdk = jdk;
            }

            public int decodeApk(string dir, string apkFile, bool overwrite = true, bool decodeSrc = false) {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");
                if (overwrite && Directory.Exists(dir)) Directory.Delete(dir, true);

                string command = @$"""{this.path}"" d";
                if (!decodeSrc) command += " -s";
                if (overwrite) command += " -f";
                command += @$" -o ""{dir}"" ""{apkFile}""";

                Process? proc = jdk.runJar(command);
                return proc?.ExitCode ?? 1;
            }

            public int buildApk(string apkFile, string dir) {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");
                string command = @$"""{this.path}"" b -o ""{apkFile}"" ""{dir}""";

                Process? proc = jdk.runJar(command);
                return proc?.ExitCode ?? 1;
            }

            public override async Task downloadTool() {
                string filePath = $"{Environment.CurrentDirectory}/apktool.jar";
                await downloadFile(url, filePath);
                this.path = filePath;
                this.wasDownloaded = true;
            }

            public override void deleteTool() {
                if (!this.wasDownloaded) return;
                if (File.Exists(this.path))
                    File.Delete(this.path);
            }
        }

        public class UberApkSigner : ExternalTool
        {
            public override string name { get; } = "UberApkSigner";
            public JDK jdk { get; }
            private static string url = "https://github.com/patrickfav/uber-apk-signer/releases/download/v1.3.0/uber-apk-signer-1.3.0.jar";

            public UberApkSigner(JDK jdk) {
                this.jdk = jdk;
                if (this.path == null && File.Exists($"{Environment.CurrentDirectory}/uber-apk-signer.jar")) {
                    this.path = $"{Environment.CurrentDirectory}/uber-apk-signer.jar";
                }
            }
            public UberApkSigner(string path, JDK jdk) {
                this.path = path;
                this.jdk = jdk;
            }

            public int sign(string inApkFile, string? outApkDir = null) {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");

                string command = @$"""{this.path}"" -a ""{inApkFile}""";
                if (outApkDir != null)
                    command += @$" -o ""{outApkDir.TrimEnd('/', '\\')}""";

                Process? proc = jdk.runJar(command);
                return proc?.ExitCode ?? 1;
            }

            public int signOverwrite(string inApkFile) {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");

                Process? proc = jdk.runJar(@$"""{this.path}"" -a ""{inApkFile}"" --overwrite");
                return proc?.ExitCode ?? 1;
            }

            public override async Task downloadTool() {
                string filePath = $"{Environment.CurrentDirectory}/uber-apk-signer.jar";
                await downloadFile(url, filePath);
                this.path = filePath;
                this.wasDownloaded = true;
            }

            public override void deleteTool() {
                if (!this.wasDownloaded) return;
                if (File.Exists(this.path))
                    File.Delete(this.path);
            }
        }

        public class ADB : ExternalTool
        {
            public override string name { get; } = "AndroidDebugBridge";
            public string? homePath { get; private set; } = null;

            private FastZip fastZip = new FastZip();
            private static Dictionary<OSPlatform, string> urls = new Dictionary<OSPlatform, string> {
                { OSPlatform.Windows, "https://dl.google.com/android/repository/platform-tools-latest-windows.zip" },
                { OSPlatform.Linux, "https://dl.google.com/android/repository/platform-tools-latest-linux.zip" },
                { OSPlatform.OSX, "https://dl.google.com/android/repository/platform-tools-latest-darwin.zip" }
            };

            public ADB() {
                if (Directory.Exists($"{Environment.CurrentDirectory}/platform-tools")) {
                    this.homePath = $"{Environment.CurrentDirectory}/platform-tools";
                    this.path = $"{Environment.CurrentDirectory}/platform-tools/adb";
                }
            }
            public ADB(string path) { this.path = path; }

            public int waitFor(string arg = "device") {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");
                Process? prc = startAndWaitPrc(new(this.path, $"wait-for-{arg}"));
                return prc?.ExitCode ?? 1;
            }

            public int killServer() {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");
                Process? prc = startAndWaitPrc(new(this.path, $"kill-server"), false, false);
                return prc?.ExitCode ?? 1;
            }

            public int install(string apkFile, bool replace = true, bool allowDowngrade = true) {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");

                string command = "install";
                if (replace) command += " -r";
                if (allowDowngrade) command += " -d";
                command += $@" ""{apkFile}""";

                Process? prc = startAndWaitPrc(new(this.path, command));
                return prc?.ExitCode ?? 1;
            }

            public Process? runShell(string command, out string? output, string? package = null, bool printOut = false) {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");

                if (package == null)
                    command = $@"shell ""{command}""";
                else
                    command = $@"shell ""run-as {package} {command}""";

                return startAndWaitPrc(new(this.path, command), out output, out _, printOut);
            }

            public Process? runShell(string command, string? package = null) {
                return runShell(command, out _, package, true);
            }

            public int push(string localPath, string remotePath) {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");
                Process? prc = startAndWaitPrc(new(this.path, @$"push ""{localPath}"" ""{remotePath}"""));
                return prc?.ExitCode ?? 1;
            }

            public int pull(string remotePath, string localPath) {
                if (this.path == null) throw new FileNotFoundException($"{this.name} not found");
                Process? prc = startAndWaitPrc(new(this.path, @$"pull ""{remotePath}"" ""{localPath}"""));
                return prc?.ExitCode ?? 1;
            }

            public override async Task downloadTool() {
                if (RuntimeInformation.OSArchitecture != Architecture.X64)
                    throw new PlatformNotSupportedException();

                string? dlUrl = null;
                foreach (OSPlatform platform in urls.Keys) {
                    if (RuntimeInformation.IsOSPlatform(platform)) {
                        dlUrl = urls[platform];
                        break;
                    }
                }
                if (dlUrl == null) throw new PlatformNotSupportedException();

                string dlPath = $"{Environment.CurrentDirectory}/platform-tools";
                await downloadFile(dlUrl, $"{dlPath}.tmp");

                if (Directory.Exists(dlPath)) Directory.Delete(dlPath, true);
                fastZip.ExtractZip($"{dlPath}.tmp", Environment.CurrentDirectory, null);
                this.homePath = dlPath;
                this.path = dlPath + "/adb";
                this.wasDownloaded = true;

                // Cleanup
                File.Delete($"{dlPath}.tmp");
            }

            public override void deleteTool() {
                if (!this.wasDownloaded) return;
                killServer();
                if (Directory.Exists(this.homePath))
                    Directory.Delete(this.homePath, true);
            }
        }

        public class LoveEmbedApk : ExternalTool
        {
            public override string name { get; } = "love-android-embed";
            private static string url = "https://github.com/love2d/love-android/releases/download/11.5a/love-11.5-android-embed.apk";

            public LoveEmbedApk() {
                if (File.Exists($"{Environment.CurrentDirectory}/love-android-embed.apk"))
                    this.path = $"{Environment.CurrentDirectory}/love-android-embed.apk";
            }
            public LoveEmbedApk(string path) { this.path = path; }

            public override async Task downloadTool() {
                string filePath = $"{Environment.CurrentDirectory}/love-android-embed.apk";
                await downloadFile(url, filePath);
                this.path = filePath;
                this.wasDownloaded = true;
            }

            public override void deleteTool() {
                if (!this.wasDownloaded) return;
                if (File.Exists(this.path))
                    File.Delete(this.path);
            }
        }

        public class BaseIPA : ExternalTool
        {
            public override string name { get; } = "base-ipa";
            private static string url = "https://raw.githubusercontent.com/PGgamer2/balatro-mobile-builder/main/resources/base.ipa";

            public BaseIPA() {
                if (File.Exists($"{Environment.CurrentDirectory}/base.ipa"))
                    this.path = $"{Environment.CurrentDirectory}/base.ipa";
            }
            public BaseIPA(string path) { this.path = path; }

            public override async Task downloadTool() {
                string filePath = $"{Environment.CurrentDirectory}/base.ipa";
                await downloadFile(url, filePath);
                this.path = filePath;
                this.wasDownloaded = true;
            }

            public override void deleteTool() {
                if (!this.wasDownloaded) return;
                if (File.Exists(this.path))
                    File.Delete(this.path);
            }
        }
    }
}
