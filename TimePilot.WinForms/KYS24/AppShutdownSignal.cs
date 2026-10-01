namespace TimePilot.WinForms.KYS24
{
    internal static class AppShutdownSignal
    {
        public const string ShutdownArgument = "--shutdown";

        public const string DefaultShutdownEventName = "ActiveLogbook.ShutdownRequested";

        public static EventWaitHandle CreateListener()
        {
            return CreateListener(DefaultShutdownEventName);
        }

        internal static EventWaitHandle CreateListener(string eventName)
        {
            return new EventWaitHandle(
                initialState: false,
                mode: EventResetMode.AutoReset,
                name: eventName);
        }

        public static bool RequestShutdown()
        {
            return RequestShutdown(DefaultShutdownEventName);
        }

        internal static bool RequestShutdown(string eventName)
        {
            try
            {
                using var shutdownEvent = EventWaitHandle.OpenExisting(eventName);
                shutdownEvent.Set();
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
