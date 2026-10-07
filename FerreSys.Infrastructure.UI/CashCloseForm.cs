using FerreSys.Application;

namespace FerreSys.Infrastructure.UI;

public class CashCloseForm : Form
{
    private readonly GetDailyCashSummaryUseCase _getDailyCashSummaryUseCase;
    private readonly SaveOpeningCashUseCase _saveOpeningCashUseCase;
    private readonly CloseCashDayUseCase _closeCashDayUseCase;

    private DateTimePicker dtpDate = null!;
    private NumericUpDown numOpening = null!;
    private NumericUpDown numCounted = null!;
    private Label lblCash = null!;
    private Label lblCard = null!;
    private Label lblTotal = null!;
    private Label lblEntries = null!;
    private Label lblExits = null!;
    private Label lblMovementsNet = null!;
    private Label lblExpected = null!;
    private Label lblDifference = null!;
    private Label lblStatus = null!;
    private Button btnCloseDay = null!;

    private DailyCashSummary? _summary;
    private bool _loading;

    public CashCloseForm(
        GetDailyCashSummaryUseCase getDailyCashSummaryUseCase,
        SaveOpeningCashUseCase saveOpeningCashUseCase,
        CloseCashDayUseCase closeCashDayUseCase)
    {
        _getDailyCashSummaryUseCase = getDailyCashSummaryUseCase;
        _saveOpeningCashUseCase = saveOpeningCashUseCase;
        _closeCashDayUseCase = closeCashDayUseCase;
        InitializeComponent();
        LoadDay();
    }

    private void InitializeComponent()
    {
        Text = "Corte de caja - FerreSys";
        Size = new Size(460, 720);
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
            RowCount = 6,
            Padding = new Padding(10, 8, 10, 10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));

        root.Controls.Add(CreateDateSection(), 0, 0);
        root.Controls.Add(CreateOpeningSection(), 0, 1);
        root.Controls.Add(CreateSalesSection(), 0, 2);
        root.Controls.Add(CreateMovementsSection(), 0, 3);
        root.Controls.Add(CreateCountSection(), 0, 4);
        root.Controls.Add(CreateActionsSection(), 0, 5);
        Controls.Add(root);
    }

    private Control CreateDateSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.PanelBg,
            Margin = new Padding(0, 0, 0, 6)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var lbl = new Label
        {
            Text = "Fecha",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.Text,
            Margin = new Padding(4, 0, 0, 0)
        };

        dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Dock = DockStyle.Left,
            Width = 140,
            Margin = new Padding(0, 14, 0, 14)
        };
        dtpDate.ValueChanged += (_, _) => LoadDay();

        panel.Controls.Add(lbl, 0, 0);
        panel.Controls.Add(dtpDate, 1, 0);
        return panel;
    }

    private Control CreateOpeningSection()
    {
        var gb = UiHelp.CreateGroupBox("Fondo de caja", SectionHelpText.CashCloseOpening);
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8, 18, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        numOpening = CreateMoneyInput();
        numOpening.ValueChanged += (_, _) => RefreshDifference();

        var btnSave = new Button
        {
            Text = "Guardar fondo",
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.Ui
        };
        btnSave.Click += BtnSaveOpening_Click;

        row.Controls.Add(numOpening, 0, 0);
        row.Controls.Add(btnSave, 1, 0);
        gb.Controls.Add(row);
        return gb;
    }

    private Control CreateSalesSection()
    {
        var gb = UiHelp.CreateGroupBox("Cobrado en el día", SectionHelpText.CashCloseSales);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 16, 8, 6),
            BackColor = UiTheme.PanelBg
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

        lblCash = CreateAmountLabel();
        lblCard = CreateAmountLabel();
        lblTotal = CreateAmountLabel();
        lblTotal.Font = UiTheme.UiBold;

        stack.Controls.Add(lblCash, 0, 0);
        stack.Controls.Add(lblCard, 0, 1);
        stack.Controls.Add(lblTotal, 0, 2);
        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateMovementsSection()
    {
        var gb = UiHelp.CreateGroupBox("Movimientos de efectivo", SectionHelpText.CashCloseMovements);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 16, 8, 6),
            BackColor = UiTheme.PanelBg
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

        lblEntries = CreateAmountLabel();
        lblExits = CreateAmountLabel();
        lblMovementsNet = CreateAmountLabel();
        lblMovementsNet.Font = UiTheme.UiBold;

        stack.Controls.Add(lblEntries, 0, 0);
        stack.Controls.Add(lblExits, 0, 1);
        stack.Controls.Add(lblMovementsNet, 0, 2);
        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateCountSection()
    {
        var gb = UiHelp.CreateGroupBox("Arqueo", SectionHelpText.CashCloseCount);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(8, 16, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33));

        lblExpected = CreateAmountLabel();
        numCounted = CreateMoneyInput();
        numCounted.Margin = new Padding(0, 4, 0, 4);
        numCounted.ValueChanged += (_, _) => RefreshDifference();
        lblDifference = CreateAmountLabel();
        lblDifference.Font = UiTheme.UiBold;

        stack.Controls.Add(CreateCaption("Efectivo esperado"), 0, 0);
        stack.Controls.Add(lblExpected, 1, 0);
        stack.Controls.Add(CreateCaption("Efectivo contado"), 0, 1);
        stack.Controls.Add(numCounted, 1, 1);
        stack.Controls.Add(CreateCaption("Diferencia"), 0, 2);
        stack.Controls.Add(lblDifference, 1, 2);
        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateActionsSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiTheme.WindowBg,
            Margin = new Padding(0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            Margin = new Padding(2, 0, 0, 0)
        };

        btnCloseDay = new Button
        {
            Text = "Cerrar día",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.UiBold,
            Margin = new Padding(0, 4, 0, 0)
        };
        btnCloseDay.Click += BtnCloseDay_Click;

        panel.Controls.Add(lblStatus, 0, 0);
        panel.Controls.Add(btnCloseDay, 0, 1);
        return panel;
    }

    private void LoadDay()
    {
        if (_loading)
            return;

        _loading = true;
        try
        {
            _summary = _getDailyCashSummaryUseCase.Execute(dtpDate.Value);
            numOpening.Value = ClampMoney(_summary.OpeningCash);
            numCounted.Value = ClampMoney(_summary.CountedCash ?? 0m);

            lblCash.Text = $"Efectivo: {_summary.CashSales:C2}   ({TicketLabel(_summary.CashTicketCount)})";
            lblCard.Text = $"Tarjeta: {_summary.CardSales:C2}   ({TicketLabel(_summary.CardTicketCount)})";
            lblTotal.Text = $"Total vendido: {_summary.TotalSales:C2}";
            lblEntries.Text = $"Entradas: {_summary.CashEntries:C2}";
            lblExits.Text = $"Salidas: {_summary.CashExits:C2}";
            lblMovementsNet.Text = $"Neto movimientos: {_summary.CashMovementsNet:C2}";

            bool closed = _summary.ClosedAt != null;
            btnCloseDay.Text = closed ? "Actualizar cierre" : "Cerrar día";
            numOpening.Enabled = !closed;
            lblStatus.Text = BuildStatus(_summary);
            lblStatus.ForeColor = _summary.IsStale ? Color.DarkOrange : UiTheme.Text;
        }
        finally
        {
            _loading = false;
        }

        RefreshDifference();
    }

    private void RefreshDifference()
    {
        if (_loading || _summary == null)
            return;

        decimal expected = numOpening.Value + _summary.CashSales + _summary.CashEntries - _summary.CashExits;
        decimal difference = numCounted.Value - expected;

        lblExpected.Text = expected.ToString("C2");
        if (difference == 0)
        {
            lblDifference.Text = "Cuadra  " + 0m.ToString("C2");
            lblDifference.ForeColor = UiTheme.StatusOk;
        }
        else if (difference > 0)
        {
            lblDifference.Text = "Sobrante  " + difference.ToString("C2");
            lblDifference.ForeColor = UiTheme.GroupTitle;
        }
        else
        {
            lblDifference.Text = "Faltante  " + Math.Abs(difference).ToString("C2");
            lblDifference.ForeColor = Color.Firebrick;
        }
    }

    private void BtnSaveOpening_Click(object? sender, EventArgs e)
    {
        try
        {
            _saveOpeningCashUseCase.Execute(dtpDate.Value, numOpening.Value);
            LoadDay();
            lblStatus.Text = "Fondo guardado. " + lblStatus.Text;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fondo de caja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnCloseDay_Click(object? sender, EventArgs e)
    {
        if (_summary == null)
            return;

        if (!_summary.HasOpeningCash)
        {
            MessageBox.Show(
                "Guarda el fondo de caja antes de cerrar el día.",
                "Corte de caja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (Math.Round(numOpening.Value, 2) != Math.Round(_summary.OpeningCash, 2))
        {
            MessageBox.Show(
                "El fondo en pantalla no está guardado. Pulsa Guardar fondo y después cierra el día.",
                "Corte de caja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        bool updating = _summary.ClosedAt != null;
        string question = updating
            ? "¿Actualizar el cierre de este día con los montos actuales?"
            : "¿Cerrar el día con el efectivo contado? Puedes seguir cobrando y volver a cerrar.";

        var confirm = MessageBox.Show(question, "Corte de caja", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        try
        {
            _closeCashDayUseCase.Execute(dtpDate.Value, numCounted.Value);
            LoadDay();
            MessageBox.Show(
                updating ? "Cierre actualizado." : "Día cerrado.",
                "Corte de caja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Corte de caja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string BuildStatus(DailyCashSummary summary)
    {
        if (summary.ClosedAt is not DateTime closedAt)
            return "Día abierto. Esperado = fondo + ventas en efectivo + entradas − salidas.";

        string closed = $"Último cierre: {closedAt:dd/MM/yyyy HH:mm}. Puedes seguir cobrando y volver a cerrar.";
        if (!summary.IsStale)
            return closed;

        return $"Hay ventas posteriores al cierre de las {closedAt:HH:mm}. Los totales son del día completo. {closed}";
    }

    private static string TicketLabel(int count) => count == 1 ? "1 ticket" : $"{count} tickets";

    private static decimal ClampMoney(decimal amount)
    {
        if (amount < 0)
            return 0;
        if (amount > 9999999.99m)
            return 9999999.99m;
        return amount;
    }

    private static NumericUpDown CreateMoneyInput()
    {
        return new NumericUpDown
        {
            DecimalPlaces = 2,
            Maximum = 9999999.99m,
            Minimum = 0,
            ThousandsSeparator = true,
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            TextAlign = HorizontalAlignment.Right
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
