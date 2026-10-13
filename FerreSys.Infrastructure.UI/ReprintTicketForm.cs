using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.UI.Printing;

namespace FerreSys.Infrastructure.UI;

public sealed class ReprintTicketForm : Form
{
    private readonly Sale _sale;
    private readonly IAppSettingsStore _settings;
    private readonly BusinessTicketProfile _profile;
    private readonly Font _previewFont = new("Consolas", 9f);
    private readonly ZoomImage _logo = new() { BackColor = Color.White, Height = 72, Visible = false };

    public ReprintTicketForm(Sale sale, IAppSettingsStore settings)
    {
        _sale = sale;
        _settings = settings;
        _profile = settings.GetTicketProfile();

        string shortId = sale.Id.ToString()[..8].ToUpper();
        Text = "Reimprimir ticket - Posly";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 560);
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var question = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"¿Imprimir de nuevo el ticket {shortId}?",
            Font = UiTheme.UiBold,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var host = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.WindowBg };
        int paperWidth = _profile.PaperWidth == TicketPaperWidth.Mm80 ? 320 : 250;
        var paper = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Width = paperWidth,
            Dock = DockStyle.Fill
        };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(8)
        };
        _logo.Width = paperWidth - 20;
        _logo.Margin = new Padding(0, 0, 0, 6);
        string? logoPath = _profile.ShowsLogo ? settings.ResolveTicketLogoPath() : null;
        _logo.Picture = LoadLogo(logoPath, paperWidth - 24, 80);
        _logo.Visible = _logo.Picture != null;

        var text = new TextBox
        {
            Text = TicketFormatter.GenerateTicketText(sale, _profile),
            Font = _previewFont,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Width = paperWidth - 24,
            Height = 420,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = false,
            TabStop = false
        };
        flow.Controls.Add(_logo);
        flow.Controls.Add(text);
        paper.Controls.Add(flow);
        host.Controls.Add(paper);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        var close = new Button { Text = "Cerrar", Width = 100, Height = 32, DialogResult = DialogResult.Cancel };
        var print = new Button { Text = "Imprimir", Width = 100, Height = 32, Font = UiTheme.UiBold };
        print.Click += (_, _) => Print();
        buttons.Controls.Add(close);
        buttons.Controls.Add(print);

        root.Controls.Add(question, 0, 0);
        root.Controls.Add(host, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
        CancelButton = close;
        AcceptButton = print;
        FormClosed += (_, _) =>
        {
            _logo.ReleaseImage();
            _previewFont.Dispose();
        };
    }

    private void Print()
    {
        PeripheralSettings devices = _settings.GetPeripheralSettings();
        IThermalPrinter? printer = null;
        if (devices.HasPrinterConfigured)
        {
            try
            {
                printer = ThermalPrinterFactory.Create(devices);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        byte[]? logo = TicketLogoPrinter.TryEncode(_settings, _profile);
        PrintSaleTicketResult result = new PrintSaleTicketUseCase().Execute(
            _sale,
            _profile,
            devices,
            printer,
            logo,
            openCashDrawer: false);

        if (!result.Success)
        {
            MessageBox.Show(
                string.IsNullOrWhiteSpace(result.Error) ? "No se pudo imprimir el ticket." : result.Error,
                "Ticket",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static Bitmap? LoadLogo(string? path, int maxWidth, int maxHeight)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            using var stream = new MemoryStream(File.ReadAllBytes(path), writable: false);
            using var source = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
            if (source.Width < 1 || source.Height < 1)
                return null;

            double scale = Math.Min(1d, Math.Min(maxWidth / (double)source.Width, maxHeight / (double)source.Height));
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            graphics.DrawImage(source, new Rectangle(0, 0, width, height));
            return bitmap;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
