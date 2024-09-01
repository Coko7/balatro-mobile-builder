using BalatroMobileBuilder.Properties;
using System.Reflection;

namespace BalatroMobileBuilder
{
    public class AndroidBuilder
    {
        public ExternalTool.JDK jdk;
        public ExternalTool.Apktool apktool;
        public ExternalTool.UberApkSigner uberApkSigner;
        public ExternalTool.LoveEmbedApk loveEmbedApk;

        public AndroidBuilder() {
            this.jdk = new ExternalTool.JDK();
            this.apktool = new ExternalTool.Apktool(this.jdk);
            this.uberApkSigner = new ExternalTool.UberApkSigner(this.jdk);
            this.loveEmbedApk = new ExternalTool.LoveEmbedApk();
        }

        public List<Task> downloadMissing(bool wait = true) {
            List<Task> tasks = new List<Task>();
            int row = Console.CursorTop + 1;
            foreach (ExternalTool tool in new ExternalTool[] { this.jdk, this.apktool, this.uberApkSigner, this.loveEmbedApk }) {
                if (tool.path != null)
                    continue;
                tasks.Add(Task.Run(async () => {
                    int r = row++;
                    Console.WriteLine();
                    await tool.downloadTool(r);
                }));
            }

            // Wait for every download to finish
            if (wait) Task.WaitAll(tasks.ToArray());
            return tasks;
        }

        public void deleteTools() {
            foreach (ExternalTool tool in new ExternalTool[] { this.jdk, this.apktool, this.uberApkSigner, this.loveEmbedApk }) {
                tool.deleteTool();
            }
        }

        public string build(BalatroZip balaZip, string? pathToApk = null, string decodeDir = ".apk.d") {
            if (pathToApk == null)
                pathToApk = $"{Environment.CurrentDirectory}/balatro.apk";

            ArgumentNullException.ThrowIfNull(loveEmbedApk.path);
            Console.WriteLine("Decoding base APK...");
            int exitCode = apktool.decodeApk(decodeDir, loveEmbedApk.path);
            if (exitCode != 0) {
                ConInterface.printError($"{apktool.name} returned {exitCode}");
                Environment.Exit(exitCode);
            }

            Console.WriteLine("Compressing Balatro...");
            balaZip.compress($"{decodeDir}/assets/game.love");

            Console.WriteLine("Writing app manifest and icons...");
            // Write AndroidManifest.xml
            using (StreamWriter writer = new StreamWriter($"{decodeDir}/AndroidManifest.xml", false)) {
                writer.Write(getManifest(balaZip.getVersion()));
            }
            // Override icons
            foreach (string iconType in new string[] { "drawable-mdpi", "drawable-hdpi", "drawable-xhdpi", "drawable-xxhdpi", "drawable-xxxhdpi" }) {
                // Get icon from resources
                object? resource = Resources.ResourceManager.GetObject(iconType, Resources.Culture);
                ArgumentNullException.ThrowIfNull(resource);
                File.Delete($"{decodeDir}/res/{iconType}/love.png");
                File.WriteAllBytes($"{decodeDir}/res/{iconType}/love.png", (byte[])resource);
            }

            Console.WriteLine("Building APK...");
            exitCode = apktool.buildApk(pathToApk, decodeDir);
            if (exitCode != 0) {
                ConInterface.printError($"{apktool.name} returned {exitCode}");
                Environment.Exit(exitCode);
            }

            // Cleanup
            Directory.Delete(decodeDir, true);
            return pathToApk;
        }

        public string sign(string inApkFile, string? outApkFile = null) {
            if (outApkFile == null)
                outApkFile = $"{Environment.CurrentDirectory}/balatro-signed.apk";

            Console.WriteLine("Signing APK...");
            int exitCode = uberApkSigner.signOverwrite(inApkFile);
            if (exitCode != 0) {
                ConInterface.printError($"{uberApkSigner.name} returned {exitCode}");
                Environment.Exit(exitCode);
            }
            File.Move(inApkFile, outApkFile, true);

            // Cleanup
            File.Delete(inApkFile);
            File.Delete($"{inApkFile}.idsig");
            return outApkFile;
        }

        public static string getManifest(Version balatroVer) {
            Version builderVer = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            /* 
             * The manifest versionCode is an integer.
             * Define it like so: BBBBAAAA, where BBBB is the Balatro version
             * and AAAA is the builder version (Example: 1.0.1f + 0.9.0 -> 10160900).
             */
            int versionCode = builderVer.Major * 1000 + builderVer.Minor * 100 + builderVer.Build * 10 + builderVer.Revision;
            versionCode += balatroVer.Major * (int)1e7 + balatroVer.Minor * (int)1e6 + balatroVer.Build * (int)1e5 + balatroVer.Revision * (int)1e4;

            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest package=""com.unofficial.balatro""
        android:versionCode=""{versionCode}""
        android:versionName=""{balatroVer}""
        android:installLocation=""auto""
        xmlns:android=""http://schemas.android.com/apk/res/android"">
    <uses-permission android:name=""android.permission.INTERNET"" />
    <uses-permission android:name=""android.permission.VIBRATE"" />
    <uses-permission android:name=""android.permission.BLUETOOTH"" />
    <uses-permission android:name=""android.permission.WRITE_EXTERNAL_STORAGE"" android:maxSdkVersion=""18"" />

    <!-- OpenGL ES 2.0 -->
    <uses-feature android:glEsVersion=""0x00020000"" />
    <!-- Touchscreen support -->
    <uses-feature android:name=""android.hardware.touchscreen"" android:required=""false"" />
    <!-- Game controller support -->
    <uses-feature android:name=""android.hardware.bluetooth"" android:required=""false"" />
    <uses-feature android:name=""android.hardware.gamepad"" android:required=""false"" />
    <uses-feature android:name=""android.hardware.usb.host"" android:required=""false"" />
    <!-- External mouse input events -->
    <uses-feature android:name=""android.hardware.type.pc"" android:required=""false"" />
    <!-- Low latency audio -->
    <uses-feature android:name=""android.hardware.audio.low_latency"" android:required=""false"" />
    <uses-feature android:name=""android.hardware.audio.pro"" android:required=""false"" />

    <application
            android:allowBackup=""true""
            android:icon=""@drawable/love""
            android:label=""Balatro""
            android:usesCleartextTraffic=""true""
            android:debuggable=""true"" >
        <activity
                android:name=""org.love2d.android.GameActivity""
                android:exported=""true""
                android:configChanges=""orientation|screenSize|smallestScreenSize|screenLayout|keyboard|keyboardHidden|navigation""
                android:label=""Balatro""
                android:launchMode=""singleInstance""
                android:screenOrientation=""landscape""
                android:resizeableActivity=""true""
                android:theme=""@android:style/Theme.NoTitleBar.Fullscreen"" >
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
                <category android:name=""tv.ouya.intent.category.GAME"" />
            </intent-filter>
            <intent-filter>
                <action android:name=""android.hardware.usb.action.USB_DEVICE_ATTACHED"" />
            </intent-filter>
        </activity>
    </application>
</manifest>";
        }
    }
}
