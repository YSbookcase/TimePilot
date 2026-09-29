namespace TimePilot.WinForms.Refresh
{
    internal sealed class ViewRefreshGeneration
    {
        private long current;

        public long Capture()
        {
            return Interlocked.Read(ref current);
        }

        public void Invalidate()
        {
            Interlocked.Increment(ref current);
        }

        public bool IsCurrent(long captured)
        {
            return captured == Interlocked.Read(ref current);
        }
    }
}
