using FerreSys.Application;

namespace FerreSys.Infrastructure.UI;

public class GrossProfitReportForm : Form
{
    private readonly GetGrossProfitSummaryUseCase _getGrossProfitSummaryUseCase;

    private DateTimePicker dtpStart = null!;
    private DateTimePicker dtpEnd = null!;
    private Label lblTotalSold = null!;
    private Label lblTotalCost = null!;
    private Label lblNetProfit = null!;
    private Label lblMargin = null!;
    private bool _loading;

    public GrossProfitReportForm(GetGrossProfitSummaryUseCase getGrossProfitSummaryUseCase)
    {
        _getGrossProfitSummaryUseCase = getGrossProfitSummaryUseCase;
        InitializeComponent();
        SetDatesAndLoad(DateTime.Today, DateTime.Today);
    }

    private void InitializeComponent()
    {
        Text = "Utilidad bruta y ganancia neta - Posly";
        Size = new Size(480, 460);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10, 8, 10, 10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        root.Controls.Add(CreateDateSection(), 0, 0);
        root.Controls.Add(CreateSalesCostSection(), 0, 1);
        root.Controls.Add(CreateProfitSection(), 0, 2);
        root.Controls.Add(CreateCloseSection(), 0, 3);
        Controls.Add(root);
    }

    private Control CreateDateSection()
    {
        var gb = UiHelp.CreateGroupBox("Rango de fechas", SectionHelpText.GrossProfitReport);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 18, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var dates = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false
        };

        var lblStart = new Label
        {
            Text = "Desde:",
            AutoSize = true,
            Margin = new Padding(0, 5, 4, 0),
            ForeColor = UiTheme.Text
        };
        dtpStart = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 110,
            Margin = new Padding(0, 2, 12, 0)
        };
        dtpStart.ValueChanged += (_, _) => LoadSummary();

        var lblEnd = new Label
        {
            Text = "Hasta:",
            AutoSize = true,
            Margin = new Padding(0, 5, 4, 0),
            ForeColor = UiTheme.Text
        };
        dtpEnd = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 110,
            Margin = new Padding(0, 2, 0, 0)
        };
        dtpEnd.ValueChanged += (_, _) => LoadSummary();

        dates.Controls.AddRange([lblStart, dtpStart, lblEnd, dtpEnd]);

        var presets = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(0, 2, 0, 0)
        };

        var btnToday = CreatePresetButton("Hoy");
        btnToday.Click += (_, _) => SetDatesAndLoad(DateTime.Today, DateTime.Today);

        var btnYesterday = CreatePresetButton("Ayer");
        btnYesterday.Click += (_, _) =>
        {
            var yesterday = DateTime.Today.AddDays(-1);
            SetDatesAndLoad(yesterday, yesterday);
        };

        var btnThisWeek = CreatePresetButton("Esta semana");
        btnThisWeek.Click += (_, _) =>
        {
            int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = DateTime.Today.AddDays(-diff);
            SetDatesAndLoad(startOfWeek, DateTime.Today);
        };

        presets.Controls.AddRange([btnToday, btnYesterday, btnThisWeek]);

        layout.Controls.Add(dates, 0, 0);
        layout.Controls.Add(presets, 0, 1);
        gb.Controls.Add(layout);
        return gb;
    }

    private Control CreateSalesCostSection()
    {
        var gb = UiHelp.CreateGroupBox("Ventas y costo", SectionHelpText.GrossProfitSalesCost);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8, 18, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        lblTotalSold = CreateAmountLabel();
        lblTotalCost = CreateAmountLabel();

        stack.Controls.Add(CreateCaption("Total vendido"), 0, 0);
        stack.Controls.Add(lblTotalSold, 1, 0);
        stack.Controls.Add(CreateCaption("Costo total vendido"), 0, 1);
        stack.Controls.Add(lblTotalCost, 1, 1);
        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateProfitSection()
    {
        var gb = UiHelp.CreateGroupBox("Ganancia neta real", SectionHelpText.GrossProfitNet);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8, 18, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        lblNetProfit = CreateAmountLabel();
        lblNetProfit.Font = UiTheme.UiBold;
        lblMargin = CreateAmountLabel();
        lblMargin.Font = UiTheme.UiBold;

        stack.Controls.Add(CreateCaption("Ganancia neta"), 0, 0);
        stack.Controls.Add(lblNetProfit, 1, 0);
        stack.Controls.Add(CreateCaption("Margen"), 0, 1);
        stack.Controls.Add(lblMargin, 1, 1);
        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateCloseSection()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.WindowBg
        };

        var btnClose = new Button
        {
            Text = "Cerrar",
            Size = new Size(100, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            DialogResult = DialogResult.Cancel
        };
        btnClose.Location = new Point(panel.ClientSize.Width - btnClose.Width, 6);
        panel.Resize += (_, _) =>
            btnClose.Location = new Point(panel.ClientSize.Width - btnClose.Width, 6);

        panel.Controls.Add(btnClose);
        CancelButton = btnClose;
        return panel;
    }

    private void SetDatesAndLoad(DateTime start, DateTime end)
    {
        _loading = true;
        dtpStart.Value = start;
        dtpEnd.Value = end;
        _loading = false;
        LoadSummary();
    }

    private void LoadSummary()
    {
        if (_loading)
            return;

        var summary = _getGrossProfitSummaryUseCase.Execute(dtpStart.Value, dtpEnd.Value);

        lblTotalSold.Text = summary.TotalSold.ToString("C2");
        lblTotalCost.Text = summary.TotalCost.ToString("C2");
        lblNetProfit.Text = summary.NetProfit.ToString("C2");
        lblMargin.Text = summary.MarginPercent.ToString("N2") + " %";

        lblNetProfit.ForeColor = summary.NetProfit >= 0 ? UiTheme.StatusOk : Color.DarkRed;
        lblMargin.ForeColor = summary.MarginPercent >= 0 ? UiTheme.StatusOk : Color.DarkRed;
    }

    private static Button CreatePresetButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 8, 0),
            MinimumSize = new Size(0, 28),
            Padding = new Padding(8, 2, 8, 2)
        };
    }

    private static Label CreateAmountLabel()
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0)
        };
    }

    private static Label CreateCaption(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0)
        };
    }
}
