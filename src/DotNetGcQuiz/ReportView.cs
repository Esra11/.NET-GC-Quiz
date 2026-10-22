using System.Text;

namespace DotNetGcQuiz;

// Keep report rows as lightweight data, not hundreds of native controls.
// Measure once per width/DPI change and paint only the rows in the viewport.
internal sealed class ReportView : UserControl
{
    internal sealed record Row(string Text, Color ForeColor, Color BackColor, bool Bold, bool Answer)
    {
        internal Rectangle Bounds { get; set; }
        internal int Width => Bounds.Width;
    }

    private const TextFormatFlags TextFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix
        | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping;
    private readonly List<Row> rows = [];
    private readonly StringBuilder plainText = new();
    private string reportText = string.Empty;
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text { get => reportText; set => reportText = value ?? string.Empty; }
    private readonly Font regularFont = new("Segoe UI", 11);
    private readonly Font boldFont = new("Segoe UI", 11, FontStyle.Bold);
    private bool complete;
    private CancellationTokenSource? layoutCancellation;
    private int measuredWidth = -1;
    internal bool IsLayoutReady { get; private set; }
    internal List<Row> AnswerRows { get; } = [];
    internal int LastPaintedRowCount { get; private set; }

    internal ReportView(Color surface)
    {
        AutoScroll = true;
        BackColor = surface;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    internal void Append(string text, Color foreground, bool bold = false,
        Color? background = null, bool answer = false)
    {
        var row = new Row(text.TrimEnd('\r', '\n'), foreground, background ?? BackColor, bold, answer);
        rows.Add(row);
        if (answer) AnswerRows.Add(row);
        plainText.Append(text);
    }

    internal void Complete()
    {
        Text = plainText.ToString();
        complete = true;
        MeasureRows();
    }

    private async void MeasureRows()
    {
        if (!complete || !IsHandleCreated || IsDisposed || Disposing) return;
        int width = Math.Max(100, Width - SystemInformation.VerticalScrollBarWidth - 24);
        if (width == measuredWidth) return;
        measuredWidth = width;
        layoutCancellation?.Cancel();
        layoutCancellation?.Dispose();
        var cancellation = layoutCancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var snapshot = rows.ToArray();
        int dpi = DeviceDpi;
        IsLayoutReady = false;
        Invalidate();
        try
        {
            // Independent graphics/fonts on the worker: never touch UI controls or
            // shared GDI resources off the UI thread. Width changes/navigation cancel stale work.
            var bounds = await Task.Run(() =>
            {
                using var bitmap = new Bitmap(1, 1);
                bitmap.SetResolution(dpi, dpi);
                using var graphics = Graphics.FromImage(bitmap);
                using var normal = new Font("Segoe UI", 11);
                using var bold = new Font("Segoe UI", 11, FontStyle.Bold);
                var result = new Rectangle[snapshot.Length];
                int top = 12;
                for (int i = 0; i < snapshot.Length; i++)
                {
                    token.ThrowIfCancellationRequested();
                    var row = snapshot[i];
                    int horizontalPadding = row.Answer ? 12 : 0;
                    int verticalPadding = row.Answer ? 9 : 5;
                    if (row.Answer) top += 3;
                    int height = TextRenderer.MeasureText(graphics, row.Text, row.Bold ? bold : normal,
                        new Size(Math.Max(1, width - 2 * horizontalPadding), int.MaxValue), TextFlags).Height;
                    result[i] = new Rectangle(12, top, width, height + 2 * verticalPadding);
                    top = result[i].Bottom + (row.Answer ? 3 : 0);
                }
                return result;
            }, token);
            if (IsDisposed || token.IsCancellationRequested) return;
            for (int i = 0; i < rows.Count; i++) rows[i].Bounds = bounds[i];
            // Suppress a redundant resize relayout when the vertical scrollbar appears.
            AutoScrollMinSize = new Size(0, bounds.Length == 0 ? 0 : bounds[^1].Bottom + 15);
            if (token.IsCancellationRequested) return;
            IsLayoutReady = true;
            AutoScrollPosition = Point.Empty;
            Invalidate();
        }
        catch (OperationCanceledException) { }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        measuredWidth = -1;
        MeasureRows();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        MeasureRows();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        measuredWidth = -1;
        MeasureRows();
    }

    protected override void OnScroll(ScrollEventArgs e)
    {
        base.OnScroll(e);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        base.OnMouseDown(e);
    }

    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is Keys.Up or Keys.Down
        or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int current = -AutoScrollPosition.Y;
        int? target = e.KeyCode switch
        {
            Keys.Up => current - 40,
            Keys.Down => current + 40,
            Keys.PageUp => current - ClientSize.Height,
            Keys.PageDown => current + ClientSize.Height,
            Keys.Home => 0,
            Keys.End => AutoScrollMinSize.Height,
            _ => null
        };
        if (target is { } value)
        {
            AutoScrollPosition = new Point(0, Math.Max(0, value));
            Invalidate();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!IsLayoutReady)
        {
            TextRenderer.DrawText(e.Graphics, "Preparing report…", regularFont,
                new Point(12, 16), ForeColor, BackColor);
            LastPaintedRowCount = 0;
            return;
        }
        int offset = AutoScrollPosition.Y;
        int firstVisibleY = e.ClipRectangle.Top - offset;
        // Binary-search to the first possibly visible row even near the end of a long exam.
        int low = 0, high = rows.Count;
        while (low < high)
        {
            int mid = low + (high - low) / 2;
            if (rows[mid].Bounds.Bottom < firstVisibleY) low = mid + 1;
            else high = mid;
        }
        LastPaintedRowCount = 0;
        for (int i = low; i < rows.Count; i++)
        {
            var row = rows[i];
            var bounds = row.Bounds;
            bounds.Offset(0, offset);
            if (bounds.Top > e.ClipRectangle.Bottom) break;
            using var fill = new SolidBrush(row.BackColor);
            e.Graphics.FillRectangle(fill, bounds);
            bounds.Inflate(row.Answer ? -12 : 0, row.Answer ? -9 : -5);
            TextRenderer.DrawText(e.Graphics, row.Text, row.Bold ? boldFont : regularFont, bounds, row.ForeColor, TextFlags);
            LastPaintedRowCount++;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            layoutCancellation?.Cancel();
            layoutCancellation?.Dispose();
        }
        base.Dispose(disposing);
        if (disposing)
        {
            regularFont.Dispose();
            boldFont.Dispose();
        }
    }
}
