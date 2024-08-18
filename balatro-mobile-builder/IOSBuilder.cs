using ICSharpCode.SharpZipLib.Zip;
using System.Reflection;

namespace BalatroMobileBuilder
{
    public class IOSBuilder
    {
        private FastZip fastZip = new FastZip();
        public ExternalTool.BaseIPA baseIpa;

        public IOSBuilder() {
            this.baseIpa = new ExternalTool.BaseIPA();
        }
        public IOSBuilder(ExternalTool.BaseIPA baseIpa) {
            this.baseIpa = baseIpa;
        }

        public async Task downloadMissing() {
			Console.WriteLine($"Downloading {this.baseIpa.name}...");
            await this.baseIpa.downloadTool();
        }

		public void deleteTools() {
			this.baseIpa.deleteTool();
		}

        public string build(BalatroZip balaZip, string? pathToIpa = null, string decodeDir = ".ipa.d") {
            if (pathToIpa == null)
                pathToIpa = $"{Environment.CurrentDirectory}/balatro.ipa";

            ArgumentNullException.ThrowIfNull(baseIpa.path);
            Console.WriteLine("Extracting base IPA...");
            fastZip.ExtractZip(baseIpa.path, decodeDir, null);

            Console.WriteLine("Compressing Balatro...");
            balaZip.compress($"{decodeDir}/Payload/Balatro.app/game.love");

            using (StreamWriter writer = new StreamWriter($"{decodeDir}/Payload/Balatro.app/Info.plist", false)) {
                writer.Write(getPlist(balaZip.getVersion()));
            }

            Console.WriteLine("Building IPA...");
            fastZip.CreateZip(pathToIpa, decodeDir, true, null);
			
			// Cleanup
			Directory.Delete(decodeDir, true);
			return pathToIpa;
        }

        public static string getPlist(Version balatroVer) {
            Version builderVer = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
			string shortVersion = balatroVer.ToString(3);
			string bundleVersion = $"{balatroVer.ToString().Replace(".","")}.{builderVer.ToString().Replace(".", "")}.0";

            return @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>BuildMachineOSBuild</key>
	<string>23A344</string>
	<key>CFBundleDevelopmentRegion</key>
	<string>en</string>
	<key>CFBundleDisplayName</key>
	<string>Balatro</string>
	<key>CFBundleDocumentTypes</key>
	<array>
		<dict>
			<key>CFBundleTypeIconFiles</key>
			<array>
				<string>LoveDocument.icns</string>
			</array>
			<key>CFBundleTypeName</key>
			<string>LÖVE Project</string>
			<key>CFBundleTypeRole</key>
			<string>Viewer</string>
			<key>LSHandlerRank</key>
			<string>Owner</string>
			<key>LSItemContentTypes</key>
			<array>
				<string>org.love2d.love-game</string>
			</array>
		</dict>
	</array>
	<key>CFBundleExecutable</key>
	<string>Balatro</string>
	<key>CFBundleIcons</key>
	<dict>
		<key>CFBundlePrimaryIcon</key>
		<dict>
			<key>CFBundleIconFiles</key>
			<array>
				<string>iOS AppIcon60x60</string>
			</array>
			<key>CFBundleIconName</key>
			<string>iOS AppIcon</string>
		</dict>
	</dict>
	<key>CFBundleIcons~ipad</key>
	<dict>
		<key>CFBundlePrimaryIcon</key>
		<dict>
			<key>CFBundleIconFiles</key>
			<array>
				<string>iOS AppIcon60x60</string>
				<string>iOS AppIcon76x76</string>
			</array>
			<key>CFBundleIconName</key>
			<string>iOS AppIcon</string>
		</dict>
	</dict>
	<key>CFBundleIdentifier</key>
	<string>org.htf65jud.balatro</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundleName</key>
	<string>Balatro</string>
	<key>CFBundlePackageType</key>
	<string>APPL</string>
	<key>CFBundleShortVersionString</key>
	<string>{shortVersion}</string>
	<key>CFBundleSignature</key>
	<string>????</string>
	<key>CFBundleSupportedPlatforms</key>
	<array>
		<string>iPhoneOS</string>
	</array>
	<key>CFBundleVersion</key>
	<string>{bundleVersion}</string>
	<key>DTCompiler</key>
	<string>com.apple.compilers.llvm.clang.1_0</string>
	<key>DTPlatformBuild</key>
	<string>21E210</string>
	<key>DTPlatformName</key>
	<string>iphoneos</string>
	<key>DTPlatformVersion</key>
	<string>17.4</string>
	<key>DTSDKBuild</key>
	<string>21E210</string>
	<key>DTSDKName</key>
	<string>iphoneos17.4</string>
	<key>DTXcode</key>
	<string>1530</string>
	<key>DTXcodeBuild</key>
	<string>15E204a</string>
	<key>LSRequiresIPhoneOS</key>
	<true/>
	<key>LSSupportsOpeningDocumentsInPlace</key>
	<true/>
	<key>MinimumOSVersion</key>
	<string>7.0</string>
	<key>UIDeviceFamily</key>
	<array>
		<integer>1</integer>
		<integer>2</integer>
	</array>
	<key>UIFileSharingEnabled</key>
	<true/>
	<key>UILaunchStoryboardName</key>
	<string>Launch Screen</string>
	<key>UIRequiredDeviceCapabilities</key>
	<array>
		<string>opengles-2</string>
		<string>arm64</string>
	</array>
	<key>UIStatusBarStyle</key>
	<string>UIStatusBarStyleLightContent</string>
	<key>UISupportedInterfaceOrientations</key>
	<array>
		<string>UIInterfaceOrientationLandscapeLeft</string>
		<string>UIInterfaceOrientationLandscapeRight</string>
	</array>
	<key>UTExportedTypeDeclarations</key>
	<array>
		<dict>
			<key>UTTypeConformsTo</key>
			<array>
				<string>com.pkware.zip-archive</string>
			</array>
			<key>UTTypeDescription</key>
			<string>LÖVE Project</string>
			<key>UTTypeIdentifier</key>
			<string>org.love2d.love-game</string>
			<key>UTTypeSize320IconFile</key>
			<string>LoveDocument</string>
			<key>UTTypeSize64IconFile</key>
			<string>LoveDocument</string>
			<key>UTTypeTagSpecification</key>
			<dict>
				<key>com.apple.ostype</key>
				<string>LOVE</string>
				<key>public.filename-extension</key>
				<array>
					<string>love</string>
				</array>
				<key>public.mime-type</key>
				<string>application/x-love-game</string>
			</dict>
		</dict>
	</array>
</dict>
</plist>";
        }
    }
}
