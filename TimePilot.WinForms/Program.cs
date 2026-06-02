namespace TimePilot.WinForms
{
    internal static class Program
    {
        internal const string SingleInstanceMutexName = "TimePilot.SingleInstance";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (TryApplyUiLanguageArgument(args))
                return;

            if (args.Contains("--seed-sample-data"))
            {
                KYS24.SampleDataSeeder.SeedDefault();
                return;
            }

            if (args.Contains("--seed-large-sample-data"))
            {
                KYS24.SampleDataSeeder.SeedLarge();
                return;
            }

            if (args.Contains("--clear-sample-data"))
            {
                KYS24.SampleDataSeeder.Clear();
                return;
            }

            if (args.Contains("--check-sample-data"))
            {
                Console.WriteLine(KYS24.SampleDataSeeder.GetStatusText());
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var settings = KYS24.AppSettings.LoadDefault();
            KYS24.UiText.UseLanguage(settings.UiLanguage);

            using var singleInstanceMutex = new Mutex(
                initiallyOwned: true,
                name: SingleInstanceMutexName,
                createdNew: out var isFirstInstance);
            if (!isFirstInstance)
            {
                if (!args.Contains(KYS24.WindowsStartupRegistration.TrayStartupArgument))
                {
                    MessageBox.Show(
                        KYS24.UiText.Main.DuplicateInstanceMessage,
                        KYS24.UiText.AppName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return;
            }

            Application.Run(new Form1(args.Contains(KYS24.WindowsStartupRegistration.TrayStartupArgument)));
        }

        private static bool TryApplyUiLanguageArgument(string[] args)
        {
            var languageIndex = Array.IndexOf(args, "--set-ui-language");
            if (languageIndex < 0)
                return false;

            if (languageIndex + 1 >= args.Length)
                return true;

            var language = args[languageIndex + 1].Trim();
            try
            {
                var settings = KYS24.AppSettings.LoadDefault();
                settings.SetUiLanguage(language.Equals("english", StringComparison.OrdinalIgnoreCase)
                    ? KYS24.UiLanguage.English
                    : KYS24.UiLanguage.Korean);
            }
            catch
            {
                // The installer may run this while another copy is still closing; keep setup non-blocking.
            }

            return true;
        }
    }
}
