using System.Diagnostics;

namespace BalatroMobileBuilder
{
    internal class MiscUtils
    {
        public static void printError(string err) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {err}");
            Console.ResetColor();
        }

        public static Process? startAndWaitPrc(ProcessStartInfo info) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Process? prc = Process.Start(info);
            prc?.WaitForExit();
            Console.ResetColor();
            return prc;
        }

        public static bool askQuestion(string question, bool silentMode, bool? defaultAnswer = null) {
            if (silentMode && defaultAnswer != null)
                return (bool)defaultAnswer;

            if (defaultAnswer == null)
                question += " (y/n)? ";
            else
                question += (bool)defaultAnswer ? " ([y]/n)? " : " (y/[n])? ";

            string? ans;
            do {
                Console.WriteLine(question);
                ans = Console.ReadLine()?.ToLower();
                if (ans == "y" || ans == "yes") return true;
                if (ans == "n" || ans == "no") return false;
            } while (!string.IsNullOrWhiteSpace(ans) || defaultAnswer == null);
            return (bool)defaultAnswer;
        }
    }
}
