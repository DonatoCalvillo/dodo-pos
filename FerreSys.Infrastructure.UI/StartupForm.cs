using FerreSys.Infrastructure.Persistence;

namespace FerreSys.Infrastructure.UI;

internal sealed class StartupForm : Form
{
    private readonly string[] _args;
    private readonly Label _status;
    private readonly PictureBox _logo;
    private bool _started;

    public StartupForm(string[] args)
    {
        _args = args;
        Color background = SampleBackground();

        Text = "FerreSys";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(720, 460);
        BackColor = background;
        Icon = Branding.AppIcon;

        _logo = new PictureBox
        {
            Image = Branding.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(300, 300),
            BackColor = background
        };
        _status = new Label
        {
            Text = "Buscando actualización...",
            ForeColor = Color.FromArgb(226, 230, 234),
            Font = new Font("Tahoma", 11f),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Height = 56,
            Dock = DockStyle.Bottom,
            BackColor = background
        };

        Controls.Add(_logo);
        Controls.Add(_status);
        KeyPreview = true;
        KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Escape && _status.ForeColor == Color.FromArgb(255, 180, 160))
                Close();
        };
        Resize += (_, _) => CenterLogo();
        CenterLogo();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_started)
            return;

        _started = true;
        _ = StartAsync();
    }

    private async Task StartAsync()
    {
        StartupUpdateResult update = await AppUpdateService.ApplyAvailableUpdateAsync(SetStatus);
        if (update == StartupUpdateResult.Restarting || IsDisposed)
            return;

        SetStatus("Preparando datos...");
        try
        {
            await Task.Run(static () => _ = new SqliteDbContext());
        }
        catch (Exception ex)
        {
            _status.ForeColor = Color.FromArgb(255, 180, 160);
            SetStatus($"No se pudo preparar la base de datos. {ex.Message}");
            return;
        }

        if (IsDisposed)
            return;

        Hide();
        var main = new Form1(_args);
        main.FormClosed += (_, _) => Close();
        main.Show();
    }

    private void SetStatus(string text)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(text));
            return;
        }

        _status.Text = text;
    }

    private void CenterLogo()
    {
        _logo.Left = (ClientSize.Width - _logo.Width) / 2;
        _logo.Top = Math.Max(24, (ClientSize.Height - _status.Height - _logo.Height) / 2);
    }

    private static Color SampleBackground()
    {
        if (Branding.Logo is Bitmap bitmap)
        {
            for (int y = 0; y < bitmap.Height; y += 8)
            {
                for (int x = 0; x < bitmap.Width; x += 8)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    if (pixel.A == 255)
                        return Color.FromArgb(pixel.R, pixel.G, pixel.B);
                }
            }
        }

        return Color.FromArgb(47, 55, 64);
    }
}
