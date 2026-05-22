namespace TimePilot.WinForms
{
    internal sealed class BufferedDataGridView : DataGridView
    {
        public BufferedDataGridView()
        {
            DoubleBuffered = true;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                var direction = e.Delta > 0 ? -1 : 1;
                var step = Math.Max(80, ClientSize.Width / 6);
                HorizontalScrollingOffset = Math.Max(0, HorizontalScrollingOffset + direction * step);
                return;
            }

            base.OnMouseWheel(e);
        }
    }
}
