using WinLabel = System.Windows.Forms.Label;
using FerreSys.Application;
using FerreSys.Domain;
using ScottPlot.WinForms;

namespace FerreSys.Infrastructure.UI;

public class ReportsForm : Form
{
    private static readonly (string Id, string Title)[] Reports =
    [
        ("sales-summary", "Resumen de ventas"),
        ("sales-detail", "Resumen de ventas con detalle"),
        ("cash-close", "Corte de caja"),
        ("inventory", "Inventario"),
        ("best-sellers", "Productos más vendidos")
    ];

    private static readonly Dictionary<string, (string Key, string Label)[]> SeriesCatalog = new()
    {
        ["sales-summary"] = SalesSeries(),
        ["sales-detail"] = SalesSeries(),
        ["cash-close"] =
        [
            ("opening", "Apertura"),
            ("cashSales", "Ventas en efectivo"),
            ("entries", "Entradas"),
            ("exits", "Salidas"),
            ("expected", "Esperado"),
            ("counted", "Contado")
        ],
        ["inventory"] =
        [
            ("status", "Estado de stock"),
            ("top", "Top por valor")
        ],
        ["best-sellers"] =
        [
            ("quantity", "Cantidad"),
            ("amount", "Importe"),
            ("profit", "Utilidad")
        ]
    };

    private readonly ReportsUseCase _reports;
    private readonly IAppSettingsStore _settings;
    private ReportPreferences _preferences;
    private ReportDocument? _document;

    private ListBox lstReports = null!;
    private DateTimePicker dtpStart = null!;
    private DateTimePicker dtpEnd = null!;
    private ComboBox cboGrouping = null!;
    private NumericUpDown numTop = null!;
    private ComboBox cboRank = null!;
    private ComboBox cboMetric = null!;
    private CheckedListBox lstSeries = null!;
    private WinLabel lblTotals = null!;
    private WinLabel lblNote = null!;
    private DataGridView grid = null!;
    private FlowLayoutPanel chartHost = null!;
    private readonly List<FormsPlot> _plots = [];

    public ReportsForm(ReportsUseCase reports, IAppSettingsStore settings)
    {
        _reports = reports;
        _settings = settings;
        _preferences = settings.GetReportPreferences();
        BuildUi();
        SelectSavedReport();
    }

    private void BuildUi()
    {
        Text = "Reportes - Posly";
        Size = new Size(1180, 760);
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        lstReports = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        foreach ((string _, string title) in Reports)
            lstReports.Items.Add(title);
        lstReports.SelectedIndexChanged += (_, _) => OnReportChanged();

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        right.Controls.Add(BuildFilters(), 0, 0);
        right.Controls.Add(BuildSummary(), 0, 1);
        grid = BuildGrid();
        right.Controls.Add(grid, 0, 2);
        chartHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            BackColor = Color.White
        };
        right.Controls.Add(chartHost, 0, 3);

        root.Controls.Add(lstReports, 0, 0);
        root.Controls.Add(right, 1, 0);
        Controls.Add(root);
    }

    private Control BuildFilters()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 2
        };
        for (int i = 0; i < 6; i++)
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
        dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
        cboGrouping = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        cboGrouping.Items.AddRange(["Día", "Semana", "Mes"]);
        cboGrouping.SelectedIndex = (int)_preferences.Grouping;
        numTop = new NumericUpDown { Minimum = 5, Maximum = 50, Increment = 5, Width = 60, Value = _preferences.NormalizedTopN };
        cboRank = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        cboRank.Items.AddRange(["Cantidad", "Importe", "Utilidad"]);
        cboRank.SelectedIndex = _preferences.RankBy switch
        {
            nameof(BestSellerRankBy.Quantity) => 0,
            nameof(BestSellerRankBy.Profit) => 2,
            _ => 1
        };
        cboMetric = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        cboMetric.Items.AddRange(["Valor a precio", "Valor a costo"]);
        cboMetric.SelectedIndex = _preferences.InventoryMetric == nameof(InventoryValueMetric.Cost) ? 1 : 0;
        lstSeries = new CheckedListBox { Width = 180, Height = 118, CheckOnClick = true };

        panel.Controls.Add(Labeled("Desde", dtpStart), 0, 0);
        panel.Controls.Add(Labeled("Hasta", dtpEnd), 1, 0);
        panel.Controls.Add(Labeled("Agrupar", cboGrouping), 2, 0);
        panel.Controls.Add(Labeled("Top", numTop), 3, 0);
        panel.Controls.Add(Labeled("Orden", cboRank), 4, 0);
        panel.Controls.Add(Labeled("Valor", cboMetric), 5, 0);
        panel.Controls.Add(lstSeries, 6, 0);
        panel.SetRowSpan(lstSeries, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 6, 0, 0)
        };
        buttons.Controls.Add(Button("Generar", (_, _) => Generate()));
        buttons.Controls.Add(Button("Excel", (_, _) => ExportExcel()));
        buttons.Controls.Add(Button("PDF", (_, _) => ExportPdf()));
        panel.Controls.Add(buttons, 0, 1);
        panel.SetColumnSpan(buttons, 6);
        return panel;
    }

    private Control BuildSummary()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        lblTotals = new WinLabel { Dock = DockStyle.Fill, AutoEllipsis = true, Font = UiTheme.UiBold };
        lblNote = new WinLabel
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray,
            Text = "El neto descuenta devoluciones del periodo. En el corte, las devoluciones ya van en las salidas de caja."
        };
        panel.Controls.Add(lblTotals, 0, 0);
        panel.Controls.Add(lblNote, 0, 1);
        return panel;
    }

    private static DataGridView BuildGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            BackgroundColor = Color.White
        };
    }

    private void SelectSavedReport()
    {
        int index = Array.FindIndex(Reports, report => report.Id == _preferences.ReportId);
        lstReports.SelectedIndex = index >= 0 ? index : 0;
    }

    private void OnReportChanged()
    {
        string id = CurrentId();
        bool dated = id != "inventory";
        bool grouped = id is "sales-summary" or "sales-detail" or "cash-close";
        bool ranked = id == "best-sellers";
        dtpStart.Enabled = dated;
        dtpEnd.Enabled = dated;
        cboGrouping.Enabled = grouped;
        numTop.Enabled = ranked || id == "inventory";
        cboRank.Enabled = ranked;
        cboMetric.Enabled = id == "inventory";
        FillSeries(id);
        lblNote.Text = id switch
        {
            "sales-detail" => "Las filas con * son devoluciones del periodo de tickets vendidos fuera del periodo.",
            "cash-close" => "Las devoluciones no se restan otra vez: ya están incluidas en las salidas de caja.",
            "inventory" => "Existencia actual de los productos activos.",
            "best-sellers" => "Cantidad, importe y utilidad ya restan las devoluciones del periodo.",
            _ => "El neto descuenta los descuentos del ticket y las devoluciones registradas en el periodo."
        };
    }

    private void FillSeries(string id)
    {
        lstSeries.Items.Clear();
        HashSet<string> saved = _preferences.Series.TryGetValue(id, out List<string>? values)
            ? values.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        bool useSaved = saved.Count > 0;
        foreach ((string key, string label) in SeriesCatalog[id])
        {
            bool check = !useSaved || saved.Contains(key);
            lstSeries.Items.Add(new SeriesItem(key, label), check);
        }
    }

    private void Generate()
    {
        try
        {
            string id = CurrentId();
            var series = SelectedSeries();
            ReportGrouping grouping = (ReportGrouping)cboGrouping.SelectedIndex;
            int top = (int)numTop.Value;
            ReportDocument document = id switch
            {
                "sales-detail" => ReportDocuments.SalesDetail(_reports.SalesDetail(dtpStart.Value, dtpEnd.Value, grouping), series),
                "cash-close" => ReportDocuments.Cash(_reports.CashClose(dtpStart.Value, dtpEnd.Value), dtpStart.Value.Date, dtpEnd.Value.Date, grouping, series),
                "inventory" => ReportDocuments.Inventory(
                    _reports.Inventory(),
                    cboMetric.SelectedIndex == 1 ? InventoryValueMetric.Cost : InventoryValueMetric.Retail,
                    top,
                    series),
                "best-sellers" => ReportDocuments.BestSellers(
                    _reports.BestSellers(dtpStart.Value, dtpEnd.Value, top, SelectedRank()),
                    series),
                _ => ReportDocuments.SalesSummary(_reports.SalesSummary(dtpStart.Value, dtpEnd.Value, grouping), series)
            };
            ShowDocument(document);
            SavePreferences(id, series);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowDocument(ReportDocument document)
    {
        DisposeCharts();
        _document = document;
        lblTotals.Text = document.Title + "  ·  " + document.Period + "  ·  " +
            string.Join("   ", document.Totals.Select(item => item.Label + ": " + item.Value));
        grid.Columns.Clear();
        grid.Rows.Clear();
        foreach (string column in document.Columns)
            grid.Columns.Add(column, column);
        foreach (string[] row in document.Rows)
            grid.Rows.Add(row);

        foreach (ScottPlot.Plot plot in document.Charts)
        {
            var view = new FormsPlot
            {
                Width = Math.Max(640, chartHost.ClientSize.Width - 28),
                Height = 240,
                Margin = new Padding(0, 0, 0, 8)
            };
            view.Reset(plot);
            chartHost.Controls.Add(view);
            _plots.Add(view);
        }
    }

    private void ExportExcel()
    {
        if (_document == null)
        {
            MessageBox.Show(this, "Genera el reporte antes de exportarlo.", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            ReportExport.Excel(this, _document);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Excel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportPdf()
    {
        if (_document == null)
        {
            MessageBox.Show(this, "Genera el reporte antes de exportarlo.", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            ReportExport.Pdf(this, _document, _settings.GetTicketProfile());
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PDF", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SavePreferences(string id, HashSet<string> series)
    {
        _preferences.ReportId = id;
        _preferences.Grouping = (ReportGrouping)Math.Max(0, cboGrouping.SelectedIndex);
        _preferences.TopN = (int)numTop.Value;
        _preferences.RankBy = SelectedRank().ToString();
        _preferences.InventoryMetric = cboMetric.SelectedIndex == 1
            ? nameof(InventoryValueMetric.Cost)
            : nameof(InventoryValueMetric.Retail);
        _preferences.Series[id] = series.ToList();
        _settings.SaveReportPreferences(_preferences);
    }

    private string CurrentId() => Reports[Math.Max(0, lstReports.SelectedIndex)].Id;

    private HashSet<string> SelectedSeries()
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (object? item in lstSeries.CheckedItems)
        {
            if (item is SeriesItem series)
                selected.Add(series.Key);
        }

        return selected;
    }

    private BestSellerRankBy SelectedRank() => cboRank.SelectedIndex switch
    {
        0 => BestSellerRankBy.Quantity,
        2 => BestSellerRankBy.Profit,
        _ => BestSellerRankBy.Amount
    };

    private void DisposeCharts()
    {
        foreach (FormsPlot plot in _plots)
            plot.Dispose();
        _plots.Clear();
        chartHost.Controls.Clear();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        DisposeCharts();
        base.OnFormClosed(e);
    }

    private static (string Key, string Label)[] SalesSeries() =>
    [
        ("gross", "Bruto"),
        ("discounts", "Descuentos"),
        ("returns", "Devoluciones"),
        ("net", "Neto"),
        ("cash", "Efectivo"),
        ("card", "Tarjeta")
    ];

    private static Control Labeled(string caption, Control input)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 8, 0)
        };
        panel.Controls.Add(new WinLabel { Text = caption, AutoSize = true });
        panel.Controls.Add(input);
        return panel;
    }

    private static Button Button(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(88, 28), Margin = new Padding(0, 0, 8, 0) };
        button.Click += click;
        return button;
    }

    private sealed class SeriesItem(string key, string label)
    {
        public string Key { get; } = key;
        public override string ToString() => label;
    }
}
