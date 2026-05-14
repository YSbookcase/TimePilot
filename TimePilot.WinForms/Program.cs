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
            if (args.Contains("--seed-sample-data"))
            {
                KYS24.SampleDataSeeder.SeedDefault();
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
            using var singleInstanceMutex = new Mutex(
                initiallyOwned: true,
                name: SingleInstanceMutexName,
                createdNew: out var isFirstInstance);
            if (!isFirstInstance)
            {
                if (!args.Contains(KYS24.WindowsStartupRegistration.TrayStartupArgument))
                {
                    MessageBox.Show(
                        "TimePilot이 이미 실행 중입니다. 트레이 아이콘을 확인해 주세요.",
                        "TimePilot",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                return;
            }

            Application.Run(new Form1(args.Contains(KYS24.WindowsStartupRegistration.TrayStartupArgument)));
        }
    }
}
