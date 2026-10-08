namespace FerreSys.Infrastructure.UI;

/// <summary>
/// Dibuja una imagen sin PictureBox. PictureBox pregunta a GDI+ si anima
/// y ese llamado lanza "Parameter is not valid" con mapas creados en memoria.
/// </summary>
internal sealed class ZoomImage : Control
{
    private Image? _image;

    public ZoomImage()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);
        BackColor = Color.White;
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowBorder { get; set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Image? Picture
    {
        get => _image;
        set
        {
            _image = value;
            Invalidate();
        }
    }

    public void ReleaseImage()
    {
        Image? image = _image;
        _image = null;
        image?.Dispose();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        if (ShowBorder)
        {
            using var pen = new Pen(SystemColors.ControlDark);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        }

        Image? image = _image;
        if (image == null || ClientSize.Width < 1 || ClientSize.Height < 1)
            return;

        try
        {
            int sourceWidth = image.Width;
            int sourceHeight = image.Height;
            if (sourceWidth < 1 || sourceHeight < 1)
                return;

            float scale = Math.Min(ClientSize.Width / (float)sourceWidth, ClientSize.Height / (float)sourceHeight);
            int width = Math.Max(1, (int)(sourceWidth * scale));
            int height = Math.Max(1, (int)(sourceHeight * scale));
            var dest = new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(image, dest);
        }
        catch (ArgumentException)
        {
        }
    }
}
