using System.Text;
using System.Text.RegularExpressions;

namespace BalatroMobileBuilder
{
    public static class UnifiedPatch
    {
        public class Hunk
        {
            public required int ogCtxStart, nwCtxStart;
            public required string[] ogCtxLines, nwCtxLines;

            public override string ToString() {
                string res = $"@@ -{ogCtxStart},{ogCtxLines.Length}\n";
                foreach (string line in ogCtxLines) res += line;
                res += $"\n@@ +{nwCtxStart},{nwCtxLines.Length}\n";
                foreach (string line in nwCtxLines) res += line;
                return res;
            }
        }

        public static List<Hunk> parseText(string patchFile) {
            string[] lines = patchFile.ReplaceLineEndings("\n").Split("\n");
            List<Hunk> patchHunks = new List<Hunk>(1);
            int hunkIndex = -1, ogCtxIndex = -1, nwCtxIndex = -1;

            for (int i = 0; i < lines.Length; i++) {
                if (lines[i].Length == 0) continue;
                if (lines[i][0] == '@') {
                    // Create new hunk when a @ line is encountered
                    Match m = Regex.Match(lines[i], @"^@@ -(\d+),(\d+) \+(\d+),(\d+) @@");
                    ogCtxIndex = -1;
                    nwCtxIndex = -1;
                    hunkIndex++;
                    patchHunks.Add(new Hunk() {
                        ogCtxStart = int.Parse(m.Groups[1].Value),
                        ogCtxLines = new string[int.Parse(m.Groups[2].Value)],
                        nwCtxStart = int.Parse(m.Groups[3].Value),
                        nwCtxLines = new string[int.Parse(m.Groups[4].Value)]
                    });
                }
                if (hunkIndex == -1) continue; // Don't continue if there's no hunk

                /* 
                 * Reconstruct original and new context.
                 * Lines starting with ' ' are normal ctx,
                 * lines starting with '-' are removed ctx,
                 * lines starting with '+' are added ctx
                 * and lines starting with '\' indicate that the previous line has no new line.
                 */
                bool addNewLine = i + 1 >= lines.Length || lines[i + 1].Length == 0
                        || lines[i + 1][0] != '\\';
                switch (lines[i][0]) {
                case ' ':
                    patchHunks[hunkIndex].ogCtxLines[++ogCtxIndex] = lines[i].Substring(1);
                    patchHunks[hunkIndex].nwCtxLines[++nwCtxIndex] = lines[i].Substring(1);
                    if (addNewLine) {
                        patchHunks[hunkIndex].ogCtxLines[ogCtxIndex] += '\n';
                        patchHunks[hunkIndex].nwCtxLines[nwCtxIndex] += '\n';
                    }
                    break;
                case '-':
                    patchHunks[hunkIndex].ogCtxLines[++ogCtxIndex] = lines[i].Substring(1);
                    if (addNewLine) {
                        patchHunks[hunkIndex].ogCtxLines[ogCtxIndex] += '\n';
                    }
                    break;
                case '+':
                    patchHunks[hunkIndex].nwCtxLines[++nwCtxIndex] = lines[i].Substring(1);
                    if (addNewLine) {
                        patchHunks[hunkIndex].nwCtxLines[nwCtxIndex] += '\n';
                    }
                    break;
                }
            }

            return patchHunks;
        }

        public static (string, bool) apply(string input, Hunk hunk, int maxOffset = 1000) {
            bool crlf = input.Contains("\r\n");
            string[] inLines = Regex.Split(input.ReplaceLineEndings("\n"), "(?<=\n)");

            StringBuilder output = new StringBuilder(input.Length);
            int linesChecked = 0;
            for (int i = 0; i < inLines.Length; i++) {
                if (i + linesChecked < inLines.Length // Check if reaching eof
                    && Math.Abs(hunk.nwCtxStart - i) < maxOffset // Check if offset is too high
                    && linesChecked < hunk.ogCtxLines.Length // Check if patch was already applied
                    ) {
                    // Check if current and following lines are the same of the old ctx
                    for (linesChecked = 0; linesChecked < hunk.ogCtxLines.Length; linesChecked++) {
                        if (inLines[i + linesChecked] != hunk.ogCtxLines[linesChecked])
                            break;
                    }
                    if (linesChecked == hunk.ogCtxLines.Length) {
                        // Append new context lines and continue
                        for (int j = 0; j < hunk.nwCtxLines.Length; j++) {
                            output.Append(hunk.nwCtxLines[j]);
                        }
                        i += hunk.ogCtxLines.Length - 1;
                        continue;
                    }
                }
                // Append normally
                output.Append(inLines[i]);
            }

            return (crlf ? output.ToString().ReplaceLineEndings("\r\n") : output.ToString(),
                linesChecked >= hunk.ogCtxLines.Length);
        }
        public static (string, bool[]) apply(string value, List<Hunk> hunks, int maxOffset = 1000) {
            bool[] results = new bool[hunks.Count];
            for (int i = 0; i < hunks.Count; i++) {
                (value, results[i]) = apply(value, hunks[i], maxOffset);
            }
            return (value, results);
        }

        internal static void test() {
            string input = "intruder1\nline1\nline2\nline3\nintruder2";
            string patch = "--- test1.txt   2024-08-28 18:08:38.041921900 +0200\r\n+++ test2.txt   2024-08-28 18:08:40.646167100 +0200\r\n@@ -1,3 +1,4 @@\r\n-line1\r\n+newline1\r\n line2\r\n-line3\r\n+sus\r\n+newline3";

            List<Hunk> hunks = parseText(patch);
            foreach (Hunk hunk in hunks)
                Console.WriteLine(hunk);

            Console.WriteLine(apply(input, hunks).Item1);
        }
    }
}
