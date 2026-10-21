using System.Drawing.Drawing2D;

namespace MFPurgeSketchUp;

internal static class UiGeometry
{
    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class RoundedPanel : Panel
{
    public int CornerRadius { get; set; } = 12;
    public Color BorderColor { get; set; } = Color.FromArgb(220, 227, 233);

    public RoundedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        if (Width <= 0 || Height <= 0)
            return;

        using var path = UiGeometry.RoundedRectangle(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region?.Dispose();
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UiGeometry.RoundedRectangle(bounds, CornerRadius);
        using var background = new SolidBrush(BackColor);
        using var border = new Pen(BorderColor);
        e.Graphics.FillPath(background, path);
        e.Graphics.DrawPath(border, path);
    }
}

internal sealed class RoundedButton : Button
{
    public int CornerRadius { get; set; } = 10;
    public Color DisabledBackColor { get; set; } = Color.FromArgb(205, 210, 215);

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Width <= 0 || Height <= 0)
            return;

        using var path = UiGeometry.RoundedRectangle(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region?.Dispose();
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UiGeometry.RoundedRectangle(bounds, CornerRadius);
        using var background = new SolidBrush(Enabled ? BackColor : DisabledBackColor);
        e.Graphics.FillPath(background, path);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            bounds,
            Enabled ? ForeColor : Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class SmoothProgressBar : Control
{
    private int _value;

    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 100);
            Invalidate();
        }
    }

    public Color TrackColor { get; set; } = Color.FromArgb(225, 231, 236);
    public Color FillColor { get; set; } = Color.FromArgb(42, 177, 88);
    public int CornerRadius { get; set; } = 8;

    public SmoothProgressBar()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var trackPath = UiGeometry.RoundedRectangle(bounds, CornerRadius);
        using var track = new SolidBrush(TrackColor);
        e.Graphics.FillPath(track, trackPath);

        var fillWidth = (int)Math.Round(bounds.Width * Value / 100d);
        if (fillWidth <= 0)
            return;

        var fillBounds = new Rectangle(bounds.X, bounds.Y, Math.Max(fillWidth, CornerRadius * 2), bounds.Height);
        fillBounds.Width = Math.Min(fillBounds.Width, bounds.Width);
        using var fillPath = UiGeometry.RoundedRectangle(fillBounds, CornerRadius);
        using var fill = new SolidBrush(FillColor);
        e.Graphics.FillPath(fill, fillPath);
    }
}
