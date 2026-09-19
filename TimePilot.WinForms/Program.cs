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
            if (args.Contains(KYS24.AppShutdownSignal.ShutdownArgument))
            {
                KYS24.AppShutdownSignal.RequestShutdown();
                return;
            }

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

            var activationKind = KYS24.WindowsStartupRegistration.IsPackagedApp()
                ? Windows.ApplicationModel.AppInstance.GetActivatedEventArgs()?.Kind
                : null;
            var startInTray = KYS24.WindowsStartupRegistration.IsStartupLaunch(args, activationKind);

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            using var singleInstanceMutex = new Mutex(
                initiallyOwned: true,
                name: SingleInstanceMutexName,
                createdNew: out var isFirstInstance);
            if (!isFirstInstance)
            {
                if (!startInTray)
                {
                    var duplicateInstanceSettings = KYS24.AppSettings.LoadDefault();
                    KYS24.UiText.UseLanguage(duplicateInstanceSettings.UiLanguage);
                    MessageBox.Show(
                        KYS24.UiText.Main.DuplicateInstanceMessage,
                        KYS24.UiText.AppName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return;
            }

            var storageBootstrap = KYS24.DataStorageBootstrapper.Prepare();
            var settings = KYS24.AppSettings.LoadDefault();
            KYS24.UiText.UseLanguage(settings.UiLanguage);
            KYS24.StartupLaunchDiagnostics.Record("program-entry", new
            {
                IsPackaged = KYS24.WindowsStartupRegistration.IsPackagedApp(),
                ActivationKind = activationKind?.ToString(),
                ArgumentCount = args.Length,
                HasTrayArgument = args.Contains(KYS24.WindowsStartupRegistration.TrayStartupArgument),
                StartInTray = startInTray,
                Executable = Application.ExecutablePath,
                StorageDecision = storageBootstrap.Decision.ToString(),
                storageBootstrap.SelectedDirectory,
                storageBootstrap.MigrationAttempted,
                storageBootstrap.MigrationSucceeded,
                storageBootstrap.Error
            });
            ShowStorageMigrationNoticeIfNeeded(storageBootstrap, settings.UiLanguage, startInTray);

            using var shutdownEvent = KYS24.AppShutdownSignal.CreateListener();
            using var mainForm = new Form1(startInTray);
            var shutdownRegistration = ThreadPool.RegisterWaitForSingleObject(
                shutdownEvent,
                (_, _) =>
                {
                    KYS24.StartupLaunchDiagnostics.Record("shutdown-signal-received");
                    if (!mainForm.IsDisposed && mainForm.IsHandleCreated)
                        mainForm.BeginInvoke(new Action(Application.Exit));
                },
                state: null,
                millisecondsTimeOutInterval: -1,
                executeOnlyOnce: false);

            try
            {
                KYS24.StartupLaunchDiagnostics.Record("application-run-started", new
                {
                    StartInTray = startInTray
                });
                Application.Run(mainForm);
            }
            finally
            {
                KYS24.StartupLaunchDiagnostics.Record("application-run-ended");
                shutdownRegistration.Unregister(null);
            }
        }

        private static void ShowStorageMigrationNoticeIfNeeded(
            KYS24.DataStorageBootstrapResult result,
            KYS24.UiLanguage language,
            bool startInTray)
        {
            if (startInTray || !result.RequiresUserAttention)
                return;

            var isEnglish = language == KYS24.UiLanguage.English;
            var message = result.Decision == KYS24.DataStorageMigrationDecisionKind.ConflictRequiresUserChoice
                ? isEnglish
                    ? "TimePilot found data in multiple storage locations. The current location remains in use. Review the storage details in Settings before choosing which data to keep."
                    : "둘 이상의 저장 위치에서 데이터가 발견되었습니다. 현재 위치를 계속 사용합니다. 유지할 데이터를 선택하기 전에 환경 설정에서 저장 위치 상세 정보를 확인하세요."
                : isEnglish
                    ? $"TimePilot could not complete the storage transition, so the previous location remains in use. Review the storage details in Settings.\n\n{result.Error}"
                    : $"저장 위치 전환을 완료하지 못해 이전 위치를 계속 사용합니다. 환경 설정에서 저장 위치 상세 정보를 확인하세요.\n\n{result.Error}";

            MessageBox.Show(
                message.TrimEnd(),
                KYS24.UiText.AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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
