using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public sealed class BusinessProfileForm : Form
{
    private readonly IAppSettingsStore _settings;
    private readonly LocalSubscriptionService _subscription;
    private readonly Font _previewFont = new("Consolas", 8.25f);

    private ZoomImage picLogo = null!;
    private TextBox txtBusinessName = null!;
    private TextBox txtRfc = null!;
    private TextBox txtAddress = null!;
    private TextBox txtPhone = null!;
    private TextBox txtEmail = null!;
    private TextBox txtWebsite = null!;
    private TextBox txtThankYou = null!;
    private TextBox txtReturnPolicy = null!;
    private TextBox txtNote = null!;
    private ComboBox cmbPaper = null!;
    private CheckedListBox lstHeader = null!;
    private CheckedListBox lstFooter = null!;
    private Panel paperHost = null!;
    private Panel paper = null!;
    private FlowLayoutPanel previewFlow = null!;

    private string? _pendingLogoPath;
    private bool _clearLogo;
    private bool _loading;
    private bool _refreshing;

    public BusinessProfileForm(IAppSettingsStore settings, LocalSubscriptionService subscription)
    {
        _settings = settings;
        _subscription = subscription;
        BuildUi();
        LoadProfile();
        FormClosed += (_, _) =>
        {
            picLogo.ReleaseImage();
            DisposePreviewImages();
            previewFlow.Controls.Clear();
            _previewFont.Dispose();
        };
    }

    private void BuildUi()
    {
        Text = "Datos del negocio - Posly";
        Size = new Size(980, 800);
        MinimumSize = new Size(900, 740);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10),
            BackColor = UiTheme.WindowBg
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = UiTheme.WindowBg
        };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.Controls.Add(CreateIdentity(), 0, 0);
        left.Controls.Add(CreateContact(), 0, 1);
        left.Controls.Add(CreateMessages(), 0, 2);
        left.Controls.Add(CreateLicense(), 0, 3);
        left.Controls.Add(CreateOrder(), 0, 4);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(CreatePreview(), 1, 0);
        var actions = CreateActions();
        root.Controls.Add(actions, 0, 1);
        root.SetColumnSpan(actions, 2);
        Controls.Add(root);
        Shown += (_, _) => RefreshPreview();
    }

    private Control CreateIdentity()
    {
        var gb = UiHelp.CreateGroupBox("Identidad", SectionHelpText.BusinessProfile);
        var grid = Grid(2, 3, 118);
        picLogo = new ZoomImage
        {
            Dock = DockStyle.Fill,
            ShowBorder = true,
            BackColor = Color.White,
            Margin = new Padding(0, 2, 8, 2)
        };
        var logoActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        var btnPick = new Button { Text = "Elegir logo", AutoSize = true, Font = UiTheme.Ui };
        btnPick.Click += (_, _) => PickLogo();
        var btnClear = new Button { Text = "Quitar", AutoSize = true, Font = UiTheme.Ui };
        btnClear.Click += (_, _) => ClearLogo();
        logoActions.Controls.Add(btnPick);
        logoActions.Controls.Add(btnClear);

        txtBusinessName = Field();
        cmbPaper = new ComboBox { Dock = DockStyle.Left, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Ui };
        cmbPaper.Items.Add(new Choice("58 mm", TicketPaperWidth.Mm58));
        cmbPaper.Items.Add(new Choice("80 mm", TicketPaperWidth.Mm80));
        cmbPaper.SelectedIndexChanged += (_, _) => RefreshPreview();

        var logoRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        logoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        logoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        logoRow.Controls.Add(picLogo, 0, 0);
        logoRow.Controls.Add(logoActions, 1, 0);

        grid.RowStyles[0] = new RowStyle(SizeType.Absolute, 84);
        grid.RowStyles[1] = new RowStyle(SizeType.Absolute, 32);
        grid.RowStyles[2] = new RowStyle(SizeType.Absolute, 32);
        AddRow(grid, 0, "Logo", logoRow);
        AddRow(grid, 1, "Nombre", txtBusinessName);
        AddRow(grid, 2, "Ancho de papel", cmbPaper);
        gb.Controls.Add(grid);
        return gb;
    }

    private Control CreateContact()
    {
        var gb = UiHelp.CreateGroupBox("Contacto y ubicación");
        var grid = Grid(2, 5, 118);
        txtRfc = Field();
        txtAddress = Field();
        txtPhone = Field();
        txtEmail = Field();
        txtWebsite = Field();
        AddRow(grid, 0, "RFC", txtRfc);
        AddRow(grid, 1, "Dirección", txtAddress);
        AddRow(grid, 2, "Teléfono", txtPhone);
        AddRow(grid, 3, "Correo", txtEmail);
        AddRow(grid, 4, "Sitio web", txtWebsite);
        gb.Controls.Add(grid);
        return gb;
    }

    private Control CreateMessages()
    {
        var gb = UiHelp.CreateGroupBox("Mensajes");
        var grid = Grid(2, 3, 118);
        txtThankYou = Field();
        txtReturnPolicy = Field();
        txtNote = Field();
        AddRow(grid, 0, "Agradecimiento", txtThankYou);
        AddRow(grid, 1, "Devoluciones", txtReturnPolicy);
        AddRow(grid, 2, "Nota al pie", txtNote);
        gb.Controls.Add(grid);
        return gb;
    }

    private Control CreateLicense()
    {
        var gb = UiHelp.CreateGroupBox("Licencia");
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 14, 8, 4),
            BackColor = UiTheme.PanelBg
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        var summary = new Label
        {
            Text = LicensePresentation.Title(_subscription) + "\n" + LicensePresentation.Detail(_subscription),
            Dock = DockStyle.Fill,
            AutoSize = false,
            Font = UiTheme.Ui
        };
        var button = new Button { Text = "Administrar licencia", AutoSize = true, Font = UiTheme.Ui, Anchor = AnchorStyles.Left };
        button.Click += (_, _) =>
        {
            using var form = new LicenseForm(_subscription);
            form.ShowDialog(this);
            summary.Text = LicensePresentation.Title(_subscription) + "\n" + LicensePresentation.Detail(_subscription);
        };
        host.Controls.Add(summary, 0, 0);
        host.Controls.Add(button, 0, 1);
        gb.Controls.Add(host);
        return gb;
    }

    private Control CreateOrder()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.WindowBg,
            Margin = new Padding(0)
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        lstHeader = CreateList();
        lstFooter = CreateList();
        host.Controls.Add(OrderGroup("Encabezado", lstHeader), 0, 0);
        host.Controls.Add(OrderGroup("Pie", lstFooter), 1, 0);
        return host;
    }

    private Control OrderGroup(string title, CheckedListBox list)
    {
        var gb = UiHelp.CreateGroupBox(title);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(6, 14, 6, 4),
            BackColor = UiTheme.PanelBg
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        list.Dock = DockStyle.Fill;
        layout.Controls.Add(list, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        var up = new Button { Text = "Subir", Width = 72, Height = 26, Font = UiTheme.Ui };
        var down = new Button { Text = "Bajar", Width = 72, Height = 26, Font = UiTheme.Ui };
        up.Click += (_, _) => MoveBlock(list, -1);
        down.Click += (_, _) => MoveBlock(list, 1);
        buttons.Controls.Add(up);
        buttons.Controls.Add(down);
        layout.Controls.Add(buttons, 0, 1);
        gb.Controls.Add(layout);
        return gb;
    }

    private Control CreatePreview()
    {
        var gb = UiHelp.CreateGroupBox("Vista previa");
        paperHost = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.PanelBg };
        paper = new Panel
        {
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AutoScroll = true
        };
        previewFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            BackColor = Color.White,
            Padding = new Padding(8, 8, 8, 12)
        };
        paper.Controls.Add(previewFlow);
        paperHost.Controls.Add(paper);
        paperHost.Resize += (_, _) => RefreshPreview();
        gb.Controls.Add(paperHost);
        return gb;
    }

    private Panel CreateActions()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.WindowBg };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Width = 240
        };
        var btnCancel = new Button { Text = "Cancelar", Width = 100, Height = 32, Font = UiTheme.Ui, DialogResult = DialogResult.Cancel };
        var btnSave = new Button { Text = "Guardar", Width = 100, Height = 32, Font = UiTheme.UiBold };
        btnSave.Click += (_, _) => SaveAndClose();
        buttons.Controls.Add(btnCancel);
        buttons.Controls.Add(btnSave);
        panel.Controls.Add(buttons);
        CancelButton = btnCancel;
        AcceptButton = btnSave;
        return panel;
    }

    private void LoadProfile()
    {
        _loading = true;
        BusinessTicketProfile profile = _settings.GetTicketProfile();
        txtBusinessName.Text = profile.BusinessName;
        txtRfc.Text = profile.Rfc;
        txtAddress.Text = profile.Address;
        txtPhone.Text = profile.Phone;
        txtEmail.Text = profile.Email;
        txtWebsite.Text = profile.Website;
        txtThankYou.Text = profile.ThankYouMessage;
        txtReturnPolicy.Text = profile.ReturnPolicy;
        txtNote.Text = profile.FooterNote;
        SelectPaper(profile.PaperWidth);
        FillList(lstHeader, TicketLayout.HeaderCatalog, profile.HeaderLayout);
        FillList(lstFooter, TicketLayout.FooterCatalog, profile.FooterLayout);
        ShowIdentityLogo(_settings.ResolveTicketLogoPath());
        _loading = false;
        RefreshPreview();
    }

    private void PickLogo()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Logo del ticket",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp;*.gif"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var stream = new MemoryStream(File.ReadAllBytes(dialog.FileName));
            using var probe = Image.FromStream(stream);
            _ = probe.Width;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Logo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _pendingLogoPath = dialog.FileName;
        _clearLogo = false;
        ShowIdentityLogo(_pendingLogoPath);
        RefreshPreview();
    }

    private void ClearLogo()
    {
        _pendingLogoPath = null;
        _clearLogo = true;
        ShowIdentityLogo(null);
        RefreshPreview();
    }

    private void SaveAndClose()
    {
        if (string.IsNullOrWhiteSpace(txtBusinessName.Text))
        {
            MessageBox.Show("Escribe el nombre del negocio.", "Datos del negocio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtBusinessName.Focus();
            return;
        }

        try
        {
            string logoFile = _settings.ResolveTicketLogoPath() == null
                ? string.Empty
                : BusinessTicketProfile.LogoFileName;

            if (_pendingLogoPath != null)
            {
                _settings.SaveTicketLogo(_pendingLogoPath);
                logoFile = BusinessTicketProfile.LogoFileName;
            }
            else if (_clearLogo)
            {
                _settings.RemoveTicketLogo();
                logoFile = string.Empty;
            }

            _settings.SaveTicketProfile(ReadProfile(logoFile));
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private BusinessTicketProfile ReadProfile(string logoFile) => new()
    {
        BusinessName = txtBusinessName.Text.Trim(),
        Rfc = txtRfc.Text.Trim(),
        Address = txtAddress.Text.Trim(),
        Phone = txtPhone.Text.Trim(),
        Email = txtEmail.Text.Trim(),
        Website = txtWebsite.Text.Trim(),
        ThankYouMessage = txtThankYou.Text.Trim(),
        ReturnPolicy = txtReturnPolicy.Text.Trim(),
        FooterNote = txtNote.Text.Trim(),
        LogoFile = logoFile,
        PaperWidth = SelectedPaper(),
        HeaderLayout = ReadChecked(lstHeader),
        FooterLayout = ReadChecked(lstFooter)
    };

    private void RefreshPreview()
    {
        if (_loading || _refreshing || previewFlow == null || paperHost.ClientSize.Width <= 0)
            return;

        _refreshing = true;
        try
        {
            RenderPreview();
        }
        catch (Exception)
        {
            // La vista previa no debe tumbar el formulario si la imagen o el texto fallan.
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void RenderPreview()
    {

        string logoFile = CurrentLogoPath() == null ? string.Empty : BusinessTicketProfile.LogoFileName;
        BusinessTicketProfile profile = ReadProfile(logoFile);
        TicketDocument document = TicketFormatter.GeneratePreview(profile);

        int width = profile.PaperWidth == TicketPaperWidth.Mm80 ? 320 : 236;
        paper.Width = width;
        paper.Height = Math.Max(120, paperHost.ClientSize.Height - 12);
        paper.Left = Math.Max(0, (paperHost.ClientSize.Width - paper.Width) / 2);
        paper.Top = 4;

        int inner = Math.Max(40, paper.ClientSize.Width - 8);
        previewFlow.Padding = new Padding(8, 8, 8, 12);

        DisposePreviewImages();
        previewFlow.Controls.Clear();

        int contentWidth = Math.Max(40, inner - 16);
        Bitmap? logo = LoadBitmap(profile.HeaderLayout.Contains(TicketBlock.Logo) ? CurrentLogoPath() : null, contentWidth, 96);
        foreach (TicketPiece piece in document.Pieces)
        {
            if (piece.IsLogo)
            {
                if (logo == null)
                    continue;

                previewFlow.Controls.Add(new ZoomImage
                {
                    Picture = logo,
                    Width = contentWidth,
                    Height = Math.Min(96, Math.Max(logo.Height, 1)),
                    BackColor = Color.White,
                    Margin = new Padding(0, 0, 0, 6)
                });
                logo = null;
                continue;
            }

            if (string.IsNullOrWhiteSpace(piece.Text))
                continue;

            string text = piece.Text.Replace("\r\n", "\n").TrimEnd('\n');
            Size measured = TextRenderer.MeasureText(
                text,
                _previewFont,
                new Size(contentWidth, 4000),
                TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl);
            previewFlow.Controls.Add(new Label
            {
                Text = text,
                Font = _previewFont,
                AutoSize = false,
                Size = new Size(contentWidth, Math.Max(measured.Height, _previewFont.Height)),
                BackColor = Color.White,
                ForeColor = Color.Black,
                UseMnemonic = false,
                Margin = new Padding(0)
            });
        }

        logo?.Dispose();
    }

    private void DisposePreviewImages()
    {
        if (previewFlow == null)
            return;

        foreach (Control control in previewFlow.Controls)
        {
            if (control is ZoomImage box)
                box.ReleaseImage();
        }
    }

    private static Bitmap? LoadBitmap(string? path, int maxWidth, int maxHeight)
    {
        if (string.IsNullOrWhiteSpace(path) || maxWidth < 1 || maxHeight < 1)
            return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes, writable: false);
            using var source = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
            if (source.Width < 1 || source.Height < 1)
                return null;

            double scale = Math.Min(1d, Math.Min(maxWidth / (double)source.Width, maxHeight / (double)source.Height));
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, width, height));
            return bitmap;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string? CurrentLogoPath()
    {
        if (_pendingLogoPath != null)
            return _pendingLogoPath;
        if (_clearLogo)
            return null;
        return _settings.ResolveTicketLogoPath();
    }

    private void ShowIdentityLogo(string? path)
    {
        picLogo.ReleaseImage();
        if (string.IsNullOrWhiteSpace(path))
            return;

        picLogo.Picture = LoadBitmap(path, 220, 84);
    }

    private CheckedListBox CreateList()
    {
        var list = new CheckedListBox
        {
            Font = UiTheme.Ui,
            CheckOnClick = true,
            IntegralHeight = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        list.ItemCheck += (_, _) =>
        {
            if (IsHandleCreated)
                BeginInvoke(RefreshPreview);
        };
        return list;
    }

    private static void FillList(CheckedListBox list, IReadOnlyList<TicketBlock> catalog, IReadOnlyList<TicketBlock> enabled)
    {
        list.Items.Clear();
        foreach (TicketBlock block in enabled)
            list.Items.Add(new LayoutRow(block), true);
        foreach (TicketBlock block in catalog)
        {
            if (!enabled.Contains(block))
                list.Items.Add(new LayoutRow(block), false);
        }
    }

    private static IReadOnlyList<TicketBlock> ReadChecked(CheckedListBox list)
    {
        var blocks = new List<TicketBlock>();
        for (int i = 0; i < list.Items.Count; i++)
        {
            if (list.GetItemChecked(i) && list.Items[i] is LayoutRow row)
                blocks.Add(row.Block);
        }

        return blocks;
    }

    private void MoveBlock(CheckedListBox list, int delta)
    {
        int index = list.SelectedIndex;
        int target = index + delta;
        if (index < 0 || target < 0 || target >= list.Items.Count)
            return;

        object item = list.Items[index]!;
        bool isChecked = list.GetItemChecked(index);
        list.Items.RemoveAt(index);
        list.Items.Insert(target, item);
        list.SetItemChecked(target, isChecked);
        list.SelectedIndex = target;
        RefreshPreview();
    }

    private TextBox Field()
    {
        var box = new TextBox { Dock = DockStyle.Fill, Font = UiTheme.Ui };
        box.TextChanged += (_, _) => RefreshPreview();
        return box;
    }

    private static TableLayoutPanel Grid(int columns, int rows, int labelWidth)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columns,
            RowCount = rows,
            Padding = new Padding(8, 16, 8, 6),
            BackColor = UiTheme.PanelBg
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < rows; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        return grid;
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control editor)
    {
        grid.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui
        }, 0, row);
        grid.Controls.Add(editor, 1, row);
    }

    private void SelectPaper(TicketPaperWidth width)
    {
        for (int i = 0; i < cmbPaper.Items.Count; i++)
        {
            if (cmbPaper.Items[i] is Choice choice && choice.Value == width)
            {
                cmbPaper.SelectedIndex = i;
                return;
            }
        }

        cmbPaper.SelectedIndex = 0;
    }

    private TicketPaperWidth SelectedPaper() =>
        cmbPaper.SelectedItem is Choice choice ? choice.Value : TicketPaperWidth.Mm58;

    private sealed class LayoutRow
    {
        public LayoutRow(TicketBlock block) => Block = block;
        public TicketBlock Block { get; }
        public override string ToString() => TicketLayout.Label(Block);
    }

    private sealed class Choice(string label, TicketPaperWidth value)
    {
        public TicketPaperWidth Value { get; } = value;
        public override string ToString() => label;
    }
}
