using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public class ProductPricingForm : Form
{
    public DiscountKind DiscountKind { get; private set; }
    public decimal DiscountValue { get; private set; }
    public DateOnly? DiscountStartsOn { get; private set; }
    public DateOnly? DiscountEndsOn { get; private set; }
    public decimal? WholesalePrice { get; private set; }
    public decimal? WholesaleMinQuantity { get; private set; }

    private readonly Product _source;
    private readonly decimal _costPrice;
    private readonly decimal _retailPrice;
    private readonly UnitOfMeasure _unit;

    private ComboBox cboDiscount = null!;
    private NumericUpDown numDiscount = null!;
    private RadioButton rbIndefinite = null!;
    private RadioButton rbDated = null!;
    private DateTimePicker dtStart = null!;
    private DateTimePicker dtEnd = null!;
    private TableLayoutPanel discountGrid = null!;
    private FlowLayoutPanel datesPanel = null!;
    private TableLayoutPanel root = null!;
    private CheckBox chkWholesale = null!;
    private NumericUpDown numWholesalePrice = null!;
    private NumericUpDown numWholesaleQty = null!;
    private Label lblPreview = null!;

    public ProductPricingForm(
        Product source,
        decimal costPrice,
        decimal retailPrice,
        UnitOfMeasure unit,
        DiscountKind discountKind,
        decimal discountValue,
        DateOnly? discountStartsOn,
        DateOnly? discountEndsOn,
        decimal? wholesalePrice,
        decimal? wholesaleMinQuantity)
    {
        _source = source;
        _costPrice = costPrice;
        _retailPrice = retailPrice;
        _unit = unit;
        DiscountKind = discountKind;
        DiscountValue = discountValue;
        DiscountStartsOn = discountStartsOn;
        DiscountEndsOn = discountEndsOn;
        WholesalePrice = wholesalePrice;
        WholesaleMinQuantity = wholesaleMinQuantity;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Precios especiales";
        Size = new Size(460, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        root.Controls.Add(CreateDiscountSection(), 0, 0);
        root.Controls.Add(CreateWholesaleSection(), 0, 1);
        root.Controls.Add(CreatePreviewSection(), 0, 2);
        root.Controls.Add(CreateActions(), 0, 3);
        Controls.Add(root);

        LoadValues();
        RefreshPreview();
    }

    private Control CreateDiscountSection()
    {
        var gb = UiHelp.CreateGroupBox("Descuento del producto");
        discountGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(8, 8, 8, 4),
            BackColor = UiTheme.PanelBg
        };
        discountGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        discountGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        discountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        discountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        discountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        discountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        var grid = discountGrid;

        cboDiscount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        cboDiscount.Items.AddRange(["Ninguno", "Porcentaje", "Monto fijo"]);
        cboDiscount.SelectedIndexChanged += (_, _) => SyncDiscountInputs();

        numDiscount = new NumericUpDown
        {
            DecimalPlaces = 2,
            Maximum = 99999,
            Dock = DockStyle.Left,
            Width = 120
        };
        numDiscount.ValueChanged += (_, _) => RefreshPreview();

        rbIndefinite = new RadioButton { Text = "Indeterminado", AutoSize = true, Checked = true };
        rbDated = new RadioButton { Text = "Por fechas", AutoSize = true };
        rbIndefinite.CheckedChanged += (_, _) => SyncDiscountInputs();
        rbDated.CheckedChanged += (_, _) => SyncDiscountInputs();

        var vigencia = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.PanelBg,
            Margin = new Padding(0)
        };
        vigencia.Controls.Add(rbIndefinite);
        vigencia.Controls.Add(rbDated);

        dtStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Height = 23 };
        dtEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Height = 23 };
        dtStart.ValueChanged += (_, _) => RefreshPreview();
        dtEnd.ValueChanged += (_, _) => RefreshPreview();

        datesPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = UiTheme.PanelBg,
            Margin = new Padding(0),
            Visible = false
        };
        datesPanel.Controls.Add(new Label { Text = "Desde", AutoSize = true, Padding = new Padding(0, 4, 4, 0) });
        datesPanel.Controls.Add(dtStart);
        datesPanel.Controls.Add(new Label { Text = "Hasta", AutoSize = true, Padding = new Padding(8, 4, 4, 0) });
        datesPanel.Controls.Add(dtEnd);
        var dates = datesPanel;

        grid.Controls.Add(new Label { Text = "Tipo", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        grid.Controls.Add(cboDiscount, 1, 0);
        grid.Controls.Add(new Label { Text = "Valor", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        grid.Controls.Add(numDiscount, 1, 1);
        grid.Controls.Add(new Label { Text = "Vigencia", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        grid.Controls.Add(vigencia, 1, 2);
        grid.Controls.Add(dates, 1, 3);
        gb.Controls.Add(grid);
        return gb;
    }

    private Control CreateWholesaleSection()
    {
        var gb = UiHelp.CreateGroupBox("Mayoreo");
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(8, 8, 8, 4),
            BackColor = UiTheme.PanelBg
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        chkWholesale = new CheckBox { Text = "Activar precio de mayoreo", AutoSize = true };
        chkWholesale.CheckedChanged += (_, _) => SyncWholesaleInputs();

        numWholesalePrice = new NumericUpDown { DecimalPlaces = 2, Maximum = 99999, Width = 120, Dock = DockStyle.Left };
        numWholesaleQty = new NumericUpDown { Maximum = 999999, Width = 120, Dock = DockStyle.Left };
        numWholesalePrice.ValueChanged += (_, _) => RefreshPreview();
        numWholesaleQty.ValueChanged += (_, _) => RefreshPreview();

        int places = _unit.MaxDecimalPlaces();
        numWholesaleQty.DecimalPlaces = places;
        numWholesaleQty.Increment = places > 0 ? 0.001m : 1m;

        grid.Controls.Add(chkWholesale, 0, 0);
        grid.SetColumnSpan(chkWholesale, 2);
        grid.Controls.Add(new Label { Text = "Precio por unidad", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        grid.Controls.Add(numWholesalePrice, 1, 1);
        grid.Controls.Add(new Label { Text = $"A partir de ({_unit.DisplaySuffix()})", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        grid.Controls.Add(numWholesaleQty, 1, 2);
        gb.Controls.Add(grid);
        return gb;
    }

    private Control CreatePreviewSection()
    {
        lblPreview = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = UiTheme.Text,
            Padding = new Padding(4, 8, 4, 0)
        };
        return lblPreview;
    }

    private Control CreateActions()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = UiTheme.WindowBg
        };
        var btnOk = new Button { Text = "Aplicar", Width = 110, Height = 32 };
        var btnCancel = new Button { Text = "Cancelar", Width = 110, Height = 32, DialogResult = DialogResult.Cancel };
        btnOk.Click += BtnOk_Click;
        panel.Controls.Add(btnOk);
        panel.Controls.Add(btnCancel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
        return panel;
    }

    private void LoadValues()
    {
        cboDiscount.SelectedIndex = DiscountKind switch
        {
            DiscountKind.Percent => 1,
            DiscountKind.FixedAmount => 2,
            _ => 0
        };
        if (DiscountValue > 0m)
            numDiscount.Value = Math.Min(numDiscount.Maximum, DiscountValue);

        bool dated = DiscountEndsOn.HasValue || DiscountStartsOn.HasValue;
        rbDated.Checked = dated;
        rbIndefinite.Checked = !dated;
        if (DiscountStartsOn is DateOnly start)
            dtStart.Value = start.ToDateTime(TimeOnly.MinValue);
        if (DiscountEndsOn is DateOnly end)
            dtEnd.Value = end.ToDateTime(TimeOnly.MinValue);

        chkWholesale.Checked = WholesalePrice.HasValue;
        if (WholesalePrice is decimal wholesale)
            numWholesalePrice.Value = Math.Min(numWholesalePrice.Maximum, wholesale);
        if (WholesaleMinQuantity is decimal minQty)
            numWholesaleQty.Value = Math.Min(numWholesaleQty.Maximum, minQty);

        SyncDiscountInputs();
        SyncWholesaleInputs();
    }

    private void SyncDiscountInputs()
    {
        bool enabled = cboDiscount.SelectedIndex > 0;
        numDiscount.Enabled = enabled;
        rbIndefinite.Enabled = enabled;
        rbDated.Enabled = enabled;
        bool dates = enabled && rbDated.Checked;
        dtStart.Enabled = dates;
        dtEnd.Enabled = dates;
        datesPanel.Visible = dates;
        discountGrid.RowStyles[3].Height = dates ? 34 : 0;
        root.RowStyles[0].Height = dates ? 214 : 176;
        if (cboDiscount.SelectedIndex == 1)
        {
            numDiscount.Maximum = 99.99m;
            numDiscount.DecimalPlaces = 2;
        }
        else
        {
            numDiscount.Maximum = 99999m;
            numDiscount.DecimalPlaces = 2;
        }

        RefreshPreview();
    }

    private void SyncWholesaleInputs()
    {
        numWholesalePrice.Enabled = chkWholesale.Checked;
        numWholesaleQty.Enabled = chkWholesale.Checked;
        RefreshPreview();
    }

    private bool TryRead(
        out DiscountKind kind,
        out decimal value,
        out DateOnly? start,
        out DateOnly? end,
        out decimal? wholesale,
        out decimal? minQty,
        out string? error)
    {
        kind = cboDiscount.SelectedIndex switch
        {
            1 => DiscountKind.Percent,
            2 => DiscountKind.FixedAmount,
            _ => DiscountKind.None
        };
        value = kind == DiscountKind.None ? 0m : numDiscount.Value;
        start = null;
        end = null;
        if (kind != DiscountKind.None && rbDated.Checked)
        {
            start = DateOnly.FromDateTime(dtStart.Value);
            end = DateOnly.FromDateTime(dtEnd.Value);
        }

        wholesale = null;
        minQty = null;
        if (chkWholesale.Checked)
        {
            wholesale = numWholesalePrice.Value;
            minQty = numWholesaleQty.Value;
        }

        try
        {
            _ = new Product(
                _source.Id,
                _source.Barcode,
                _source.Name,
                _costPrice,
                _retailPrice,
                _source.Stock,
                _unit,
                _source.MinStock,
                _source.DesiredStock,
                _source.IsActive,
                _source.Image,
                kind,
                value,
                start,
                end,
                wholesale,
                minQty);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private void RefreshPreview()
    {
        if (lblPreview == null || cboDiscount == null)
            return;

        if (!TryRead(out DiscountKind kind, out decimal value, out DateOnly? start, out DateOnly? end, out decimal? wholesale, out decimal? minQty, out string? error))
        {
            lblPreview.Text = error ?? "Revisa los precios especiales.";
            lblPreview.ForeColor = UiTheme.StatusDanger;
            return;
        }

        var probe = new Product(
            _source.Id,
            _source.Barcode,
            _source.Name,
            _costPrice,
            _retailPrice,
            _source.Stock,
            _unit,
            _source.MinStock,
            _source.DesiredStock,
            _source.IsActive,
            _source.Image,
            kind,
            value,
            start,
            end,
            wholesale,
            minQty);

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        decimal one = probe.ResolveUnitPrice(1m, today);
        string text = $"Hoy, 1 {_unit.DisplaySuffix()}: ${one:N2}";
        if (probe.WholesaleMinQuantity is decimal bulkQty)
        {
            decimal bulk = probe.ResolveUnitPrice(bulkQty, today);
            text += $"\nA partir de {probe.Unit.FormatQuantity(bulkQty)}: ${bulk:N2}";
        }

        if (probe.DiscountKind != DiscountKind.None && !probe.IsDiscountActive(today))
            text += "\nEl descuento todavía no está vigente.";

        lblPreview.Text = text;
        lblPreview.ForeColor = UiTheme.Text;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (!TryRead(out DiscountKind kind, out decimal value, out DateOnly? start, out DateOnly? end, out decimal? wholesale, out decimal? minQty, out string? error))
        {
            MessageBox.Show(error, "Precios especiales", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DiscountKind = kind;
        DiscountValue = value;
        DiscountStartsOn = start;
        DiscountEndsOn = end;
        WholesalePrice = wholesale;
        WholesaleMinQuantity = minQty;

        DialogResult = DialogResult.OK;
        Close();
    }
}
