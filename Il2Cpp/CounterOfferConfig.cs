using System.Reflection;
using System.Runtime.CompilerServices;
using MelonLoader;

namespace BetterCounterOffer {
    public static class CounterOfferConfig {

        private static string folderName = "CounterOfferUI";
        private static string fileName = "CounterOffer_Config.ini";

        public static bool disableAllLabels = false;
        public static bool disableSuccessRate = false;
        public static bool disableMaxLimit = false;
        public static bool disableInitialOffer = false;


        public static void LoadConfig() {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string text = Path.Combine(directoryName, folderName);
            bool dirExist = Directory.Exists(text);
            if (!dirExist) {
                Directory.CreateDirectory(text);
            }
            string path = Path.Combine(text, fileName);
            bool fileExist = File.Exists(path);
            if (!fileExist) {
                SaveConfig();
                MelonLogger.Msg(ConsoleColor.Magenta,"CounterOffer Config file created with default values.");
            }
            string[] fileLines = File.ReadAllLines(path);
            foreach (string line in fileLines) {
                bool isConfigSetting = !(string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"));
                if (isConfigSetting) {
                    var pair = line.Split('=');
                    string key = pair[0].Trim();
                    string rawValue = pair[1].Trim();
                    bool value;
                    if (Boolean.TryParse(rawValue, out value)) {
                        if (key == "DisableAllLabels") {
                            CounterOfferConfig.disableAllLabels = value;
                            continue;
                        }

                        if (key == "DisableInitialOffer") {
                            CounterOfferConfig.disableInitialOffer = value;
                            continue;
                        }

                        if (key == "DisableMaxLimit") {
                            CounterOfferConfig.disableMaxLimit = value;
                            continue;
                        }

                        if (key == "DisableSuccessRate") {
                            CounterOfferConfig.disableSuccessRate = value;
                            continue;
                        }
                    }
                }
            }
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(100, 4);
            defaultInterpolatedStringHandler.AppendLiteral("\nCounterOffer Config loaded:");
            defaultInterpolatedStringHandler.AppendLiteral("\n  Disable All Labels=");
            defaultInterpolatedStringHandler.AppendFormatted<bool>(CounterOfferConfig.disableAllLabels);
            defaultInterpolatedStringHandler.AppendLiteral("\n  Disable Initial Offer=");
            defaultInterpolatedStringHandler.AppendFormatted<bool>(CounterOfferConfig.disableInitialOffer);
            defaultInterpolatedStringHandler.AppendLiteral("\n  Disable Max Limit=");
            defaultInterpolatedStringHandler.AppendFormatted<bool>(CounterOfferConfig.disableMaxLimit);
            defaultInterpolatedStringHandler.AppendLiteral("\n  Disable Success Rate=");
            defaultInterpolatedStringHandler.AppendFormatted<bool>(CounterOfferConfig.disableSuccessRate);
            MelonLogger.Msg(ConsoleColor.Magenta, defaultInterpolatedStringHandler.ToStringAndClear());
        }

        // Token: 0x06000004 RID: 4 RVA: 0x00002708 File Offset: 0x00000908
        public static void SaveConfig() {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string text = Path.Combine(directoryName, folderName);
            bool flag = !Directory.Exists(text);
            if (flag) {
                Directory.CreateDirectory(text);
            }
            string path = Path.Combine(text, fileName);
            string[] contents = new string[]
            {
                "# CounterOffer configuration",
                "DisableAllLabels=" + CounterOfferConfig.disableAllLabels.ToString(),
                "DisableInitialOffer=" + CounterOfferConfig.disableInitialOffer.ToString(),
                "DisableMaxLimit=" + CounterOfferConfig.disableMaxLimit.ToString(),
                "DisableSuccessRate=" + CounterOfferConfig.disableSuccessRate.ToString(),
            };
            File.WriteAllLines(path, contents);
        }
    }
}
