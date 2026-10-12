using FerreSys.Application;
using FerreSys.Infrastructure.Persistence;

namespace FerreSys.Infrastructure.UI;

public enum CashCutMode
{
    X,
    Z
}

public class CashCloseForm : Form
{
    private readonly GetDailyCashSummaryUseCase _getDailyCashSummaryUseCase;
    private readonly SaveOpeningCashUseCase _saveOpeningCashUseCase;
    private readonly CloseCashDayUseCase _closeCashDayUseCase;
    private readonly CloudBackupService? _cloudBackup;
    private readonly string? _premiumLicenseId;
    private readonly CashCutMode _mode;

    private DateTimePicker dtpDate = null!;
    private NumericUpDown numOpening = null!;
    private NumericUpDown numCounted = null!;
    private Button btnSaveOpening = null!;
    private Label lblOpeningReadonly = null!;
    private Label lblCash = null!;
    private Label lblCard = null!;
    private Label lblTotal = null!;
    private Label lblEntries = null!;
    private Label lblExits = null!;
    private Label lblMovementsNet = null!;
    private Label lblExpected = null!;
    private Label lblCountedReadonly = null!;
    private Label lblDifference = null!;
    private Label lblLastClose = null!;
    private Label lblStatus = null!;
    private Button btnPrimary = null!;
    private Button btnBack = null!;

    private Panel? _panelBlind;
    private Panel? _panelReport;
    private Control? _openingSection;
    private Control? _salesSection;
    private Control? _movementsSection;
    private Control? _lastCloseSection;
    private Control? _countSection;

    private DailyCashSummary? _summary;
    private bool _loading;
    private bool _zRevealed;

    public CashCloseForm(
        GetDailyCashSummaryUseCase getDailyCashSummaryUseCase,
        SaveOpeningCashUseCase saveOpeningCashUseCase,
        CloseCashDayUseCase closeCashDayUseCase,
        CashCutMode mode,
        CloudBackupService? cloudBackup = null,
        string? premiumLicenseId = null)
    {
        _getDailyCashSummaryUseCase = getDailyCashSummaryUseCase;
        _saveOpeningCashUseCase = saveOpeningCashUseCase;
        _closeCashDayUseCase = closeCashDayUseCase;
        _mode = mode;
        _cloudBackup = cloudBackup;
        _premiumLicenseId = premiumLicenseId;
        InitializeComponent();
        LoadDay();
    }

    private void InitializeComponent()
    {
        Text = _mode == CashCutMode.X
            ? "Corte X - Estado de caja - FerreSys"
            : "Corte Z - Cierre de caja - FerreSys";
        Size = _mode == CashCutMode.X ? new Size(480, 760) : new Size(480, 720);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        if (_mode == CashCutMode.X)
            BuildCorteXLayout();
        else
            BuildCorteZLayout();
    }

    private void BuildCorteXLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(10, 8, 10, 10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _openingSection = CreateOpeningSection(editable: true);
        _salesSection = CreateSalesSection();
        _movementsSection = CreateMovementsSection();
        _countSection = CreateExpectedOnlySection();
        _lastCloseSection = CreateLastCloseSection();

        root.Controls.Add(CreateDateSection(), 0, 0);
        root.Controls.Add(_openingSection, 0, 1);
        root.Controls.Add(_salesSection, 0, 2);
        root.Controls.Add(_movementsSection, 0, 3);
        root.Controls.Add(_countSection, 0, 4);
        root.Controls.Add(_lastCloseSection, 0, 5);
        root.Controls.Add(CreateStatusOnlySection(), 0, 6);
        Controls.Add(root);
    }

    private void BuildCorteZLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10, 8, 10, 10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));

        _panelBlind = CreateBlindPanel();
        _panelReport = CreateReportPanel();
        _panelReport.Visible = false;

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.WindowBg
        };
        contentHost.Controls.Add(_panelReport);
        contentHost.Controls.Add(_panelBlind);

        root.Controls.Add(CreateDateSection(), 0, 0);
        root.Controls.Add(contentHost, 0, 1);
        root.Controls.Add(CreateZActionsSection(), 0, 2);
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
        dtpDate.ValueChanged += (_, _) =>
        {
            if (_mode == CashCutMode.Z)
                ResetZToBlind();
            LoadDay();
        };

        panel.Controls.Add(lbl, 0, 0);
        panel.Controls.Add(dtpDate, 1, 0);
        return panel;
    }

    private Control CreateOpeningSection(bool editable)
    {
        var gb = UiHelp.CreateGroupBox("Fondo inicial", SectionHelpText.CashCloseOpening);
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = editable ? 2 : 1,
            RowCount = 1,
            Padding = new Padding(8, 18, 8, 8),
            BackColor = UiTheme.PanelBg
        };

        if (editable)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            numOpening = CreateMoneyInput();
            btnSaveOpening = new Button
            {
                Text = "Guardar fondo",
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0),
                FlatStyle = FlatStyle.Standard,
                UseVisualStyleBackColor = true,
                Font = UiTheme.Ui
            };
            btnSaveOpening.Click += BtnSaveOpening_Click;

            row.Controls.Add(numOpening, 0, 0);
            row.Controls.Add(btnSaveOpening, 1, 0);
        }
        else
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            lblOpeningReadonly = CreateAmountLabel();
            lblOpeningReadonly.Font = UiTheme.UiBold;
            row.Controls.Add(lblOpeningReadonly, 0, 0);
        }

        gb.Controls.Add(row);
        return gb;
    }

    private Control CreateSalesSection()
    {
        var gb = UiHelp.CreateGroupBox("Ventas del día", SectionHelpText.CashCloseSales);
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
        var gb = UiHelp.CreateGroupBox("Entradas y salidas", SectionHelpText.CashCloseMovements);
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

    private Control CreateExpectedOnlySection()
    {
        var gb = UiHelp.CreateGroupBox("Efectivo esperado", SectionHelpText.CashCloseCountX);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(8, 18, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        lblExpected = CreateAmountLabel();
        lblExpected.Font = UiTheme.UiBold;
        stack.Controls.Add(lblExpected, 0, 0);
        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateLastCloseSection()
    {
        var gb = UiHelp.CreateGroupBox("Último Corte Z", SectionHelpText.CashCloseLastZ);
        lblLastClose = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            Padding = new Padding(8, 18, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        gb.Controls.Add(lblLastClose);
        return gb;
    }

    private Control CreateStatusOnlySection()
    {
        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            Margin = new Padding(2, 0, 0, 0)
        };
        return lblStatus;
    }

    private Panel CreateBlindPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.WindowBg
        };

        var gb = UiHelp.CreateGroupBox("Conteo ciego", SectionHelpText.CashCloseCountZ);
        gb.Dock = DockStyle.Top;
        gb.Height = 160;

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12, 22, 12, 12),
            BackColor = UiTheme.PanelBg
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var hint = new Label
        {
            Text = "Ingresa el efectivo físico del cajón. El sistema no mostrará lo esperado hasta que confirmes.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text
        };

        numCounted = CreateMoneyInput();
        numCounted.Dock = DockStyle.Top;
        numCounted.Height = 28;

        var note = new Label
        {
            Text = "No se muestran ventas, movimientos ni efectivo esperado en este paso.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = UiTheme.Ui,
            ForeColor = Color.DimGray
        };

        stack.Controls.Add(hint, 0, 0);
        stack.Controls.Add(numCounted, 0, 1);
        stack.Controls.Add(note, 0, 2);
        gb.Controls.Add(stack);
        panel.Controls.Add(gb);
        return panel;
    }

    private Panel CreateReportPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = UiTheme.WindowBg
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));

        _openingSection = CreateOpeningSection(editable: false);
        _salesSection = CreateSalesSection();
        _movementsSection = CreateMovementsSection();
        _countSection = CreateZRevealCountSection();

        root.Controls.Add(_openingSection, 0, 0);
        root.Controls.Add(_salesSection, 0, 1);
        root.Controls.Add(_movementsSection, 0, 2);
        root.Controls.Add(_countSection, 0, 3);
        panel.Controls.Add(root);
        return panel;
    }

    private Control CreateZRevealCountSection()
    {
        var gb = UiHelp.CreateGroupBox("Descuadre", SectionHelpText.CashCloseCountZReveal);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(8, 16, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 33));

        lblExpected = CreateAmountLabel();
        lblCountedReadonly = CreateAmountLabel();
        lblDifference = CreateAmountLabel();
        lblDifference.Font = UiTheme.UiBold;

        stack.Controls.Add(CreateCaption("Efectivo esperado"), 0, 0);
        stack.Controls.Add(lblExpected, 1, 0);
        stack.Controls.Add(CreateCaption("Efectivo declarado"), 0, 1);
        stack.Controls.Add(lblCountedReadonly, 1, 1);
        stack.Controls.Add(CreateCaption("Diferencia"), 0, 2);
        stack.Controls.Add(lblDifference, 1, 2);
        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateZActionsSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = UiTheme.WindowBg,
            Margin = new Padding(0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            Margin = new Padding(2, 0, 0, 0)
        };

        btnBack = new Button
        {
            Text = "Volver al conteo",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.Ui,
            Visible = false,
            Margin = new Padding(0, 4, 4, 0)
        };
        btnBack.Click += (_, _) => ResetZToBlind();

        btnPrimary = new Button
        {
            Text = "Continuar",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.UiBold,
            Margin = new Padding(4, 4, 0, 0)
        };
        btnPrimary.Click += BtnPrimary_Click;

        panel.Controls.Add(lblStatus, 0, 0);
        panel.SetColumnSpan(lblStatus, 2);
        panel.Controls.Add(btnBack, 0, 1);
        panel.Controls.Add(btnPrimary, 1, 1);
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

            if (_mode == CashCutMode.X)
                BindCorteX();
            else if (_zRevealed)
                BindCorteZReport();
            else
                BindCorteZBlind();
        }
        finally
        {
            _loading = false;
        }
    }

    private void BindCorteX()
    {
        if (_summary == null)
            return;

        bool closed = _summary.ClosedAt != null;
        numOpening.Value = ClampMoney(_summary.OpeningCash);
        numOpening.Enabled = !closed;
        btnSaveOpening.Enabled = !closed;

        BindLiveBreakdown(_summary);

        decimal expected = _summary.ExpectedCash;
        lblExpected.Text = expected.ToString("C2");

        if (_summary.HasLastCloseReport)
        {
            lblLastClose.Text =
                $"Cerrado: {_summary.ClosedAt:dd/MM/yyyy HH:mm}\n" +
                $"Fondo: {_summary.OpeningCash:C2}\n" +
                $"Ventas efectivo: {_summary.LastCloseCashSales:C2}   |   Tarjeta: {_summary.LastCloseCardSales:C2}\n" +
                $"Entradas: {_summary.LastCloseCashEntries:C2}   |   Salidas: {_summary.LastCloseCashExits:C2}\n" +
                $"Esperado: {_summary.LastCloseExpectedCash:C2}   |   Declarado: {_summary.LastCloseCountedCash:C2}\n" +
                FormatDifferenceLine(_summary.LastCloseDifference ?? 0m);
            lblLastClose.ForeColor = DifferenceColor(_summary.LastCloseDifference ?? 0m);
        }
        else
        {
            lblLastClose.Text = "Aún no hay Corte Z guardado para este día.";
            lblLastClose.ForeColor = UiTheme.Text;
        }

        lblStatus.Text = BuildStatusX(_summary);
        lblStatus.ForeColor = _summary.IsStale ? Color.DarkOrange : UiTheme.Text;
    }

    private void BindCorteZBlind()
    {
        if (_summary == null)
            return;

        btnBack.Visible = false;
        btnPrimary.Text = "Continuar";
        lblStatus.Text = BuildStatusZBlind(_summary);
        lblStatus.ForeColor = _summary.IsStale ? Color.DarkOrange : UiTheme.Text;
    }

    private void BindCorteZReport()
    {
        if (_summary == null)
            return;

        lblOpeningReadonly.Text = $"Fondo inicial: {_summary.OpeningCash:C2}";
        BindLiveBreakdown(_summary);

        decimal expected = _summary.OpeningCash + _summary.CashSales + _summary.CashEntries - _summary.CashExits;
        decimal counted = numCounted.Value;
        decimal difference = counted - expected;

        lblExpected.Text = expected.ToString("C2");
        lblCountedReadonly.Text = counted.ToString("C2");
        ApplyDifferenceLabel(lblDifference, difference);

        bool updating = _summary.ClosedAt != null;
        btnBack.Visible = true;
        btnPrimary.Text = updating ? "Actualizar Corte Z" : "Guardar Corte Z";
        lblStatus.Text = BuildStatusZReport(_summary);
        lblStatus.ForeColor = _summary.IsStale ? Color.DarkOrange : UiTheme.Text;
    }

    private void BindLiveBreakdown(DailyCashSummary summary)
    {
        lblCash.Text = $"Ventas en efectivo: {summary.CashSales:C2}   ({TicketLabel(summary.CashTicketCount)})";
        lblCard.Text = $"Ventas en tarjeta: {summary.CardSales:C2}   ({TicketLabel(summary.CardTicketCount)})";
        lblTotal.Text = $"Total vendido: {summary.TotalSales:C2}";
        lblEntries.Text = $"Entradas: {summary.CashEntries:C2}";
        lblExits.Text = $"Salidas: {summary.CashExits:C2}";
        lblMovementsNet.Text = $"Neto movimientos: {summary.CashMovementsNet:C2}";
    }

    private void BtnPrimary_Click(object? sender, EventArgs e)
    {
        if (_mode != CashCutMode.Z || _summary == null)
            return;

        if (!_zRevealed)
        {
            RevealZReport();
            return;
        }

        SaveCorteZ();
    }

    private void RevealZReport()
    {
        if (_summary == null)
            return;

        if (!_summary.HasOpeningCash)
        {
            MessageBox.Show(
                "Guarda el fondo de caja (Corte X o al abrir el día) antes de hacer el Corte Z.",
                "Corte Z",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _zRevealed = true;
        if (_panelBlind != null)
            _panelBlind.Visible = false;
        if (_panelReport != null)
            _panelReport.Visible = true;

        // Refresh live totals at the moment of reveal
        _summary = _getDailyCashSummaryUseCase.Execute(dtpDate.Value);
        BindCorteZReport();
    }

    private void ResetZToBlind()
    {
        _zRevealed = false;
        if (_panelReport != null)
            _panelReport.Visible = false;
        if (_panelBlind != null)
            _panelBlind.Visible = true;
        LoadDay();
    }

    private void SaveCorteZ()
    {
        if (_summary == null)
            return;

        if (!_summary.HasOpeningCash)
        {
            MessageBox.Show(
                "Guarda el fondo de caja antes de cerrar el día.",
                "Corte Z",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        bool updating = _summary.ClosedAt != null;
        string question = updating
            ? "¿Actualizar el Corte Z con los montos actuales?"
            : "¿Guardar el Corte Z con el efectivo declarado? Puedes seguir cobrando y volver a cerrar.";

        var confirm = MessageBox.Show(question, "Corte Z", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        try
        {
            var report = _closeCashDayUseCase.Execute(dtpDate.Value, numCounted.Value);
            LoadDay();
            ApplyDifferenceLabel(lblDifference, report.Difference);
            if (_cloudBackup != null)
                _ = _cloudBackup.EnqueueAndUploadAsync(_premiumLicenseId);
            MessageBox.Show(
                updating
                    ? $"Corte Z actualizado. {FormatDifferenceLine(report.Difference)}"
                    : $"Corte Z guardado. {FormatDifferenceLine(report.Difference)}",
                "Corte Z",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Corte Z", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    private static string BuildStatusX(DailyCashSummary summary)
    {
        if (summary.ClosedAt is not DateTime closedAt)
            return "Corte X (consulta). Esperado = fondo + ventas en efectivo + entradas − salidas.";

        string closed = $"Último Corte Z: {closedAt:dd/MM/yyyy HH:mm}. Puedes seguir cobrando.";
        if (!summary.IsStale)
            return closed;

        return $"Hay ventas posteriores al Corte Z de las {closedAt:HH:mm}. Los totales arriba son en vivo. {closed}";
    }

    private static string BuildStatusZBlind(DailyCashSummary summary)
    {
        if (summary.ClosedAt is DateTime closedAt)
            return $"Ya hay un Corte Z ({closedAt:dd/MM HH:mm}). Puedes volver a contar y actualizar.";

        return "Conteo ciego: ingresa solo el efectivo físico del cajón.";
    }

    private static string BuildStatusZReport(DailyCashSummary summary)
    {
        if (summary.ClosedAt is DateTime closedAt)
            return $"Revisa el descuadre y actualiza el Corte Z (último: {closedAt:dd/MM HH:mm}).";

        return "Revisa el descuadre y guarda el Corte Z. Puedes seguir cobrando después.";
    }

    private static void ApplyDifferenceLabel(Label label, decimal difference)
    {
        label.Text = FormatDifferenceLine(difference);
        label.ForeColor = DifferenceColor(difference);
    }

    private static string FormatDifferenceLine(decimal difference)
    {
        if (difference == 0)
            return "Cuadra  " + 0m.ToString("C2");
        if (difference > 0)
            return "Sobrante  " + difference.ToString("C2");
        return "Faltante  " + Math.Abs(difference).ToString("C2");
    }

    private static Color DifferenceColor(decimal difference)
    {
        if (difference == 0)
            return UiTheme.StatusOk;
        if (difference > 0)
            return UiTheme.GroupTitle;
        return Color.Firebrick;
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
