using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public class CashMovementsForm : Form
{
    private readonly GetCashMovementsUseCase _getCashMovementsUseCase;
    private readonly RegisterCashMovementUseCase _registerCashMovementUseCase;
    private readonly GetDailyCashSummaryUseCase _getDailyCashSummaryUseCase;

    private DateTimePicker dtpDate = null!;
    private ComboBox cboKind = null!;
    private NumericUpDown numAmount = null!;
    private TextBox txtConcept = null!;
    private DataGridView dgvMovements = null!;
    private Label lblEntries = null!;
    private Label lblExits = null!;
    private Label lblNet = null!;
    private Label lblStatus = null!;
    private Button btnRegister = null!;

    public CashMovementsForm(
        GetCashMovementsUseCase getCashMovementsUseCase,
        RegisterCashMovementUseCase registerCashMovementUseCase,
        GetDailyCashSummaryUseCase getDailyCashSummaryUseCase)
    {
        _getCashMovementsUseCase = getCashMovementsUseCase;
        _registerCashMovementUseCase = registerCashMovementUseCase;
        _getDailyCashSummaryUseCase = getDailyCashSummaryUseCase;
        InitializeComponent();
        LoadDay();
    }

    private void InitializeComponent()
    {
        Text = "Movimientos de caja - FerreSys";
        Size = new Size(640, 600);
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

        root.Controls.Add(CreateDateSection(), 0, 0);
        root.Controls.Add(CreateCaptureSection(), 0, 1);
        root.Controls.Add(CreateGridSection(), 0, 2);
        root.Controls.Add(CreateTotalsSection(), 0, 3);
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

    private Control CreateCaptureSection()
    {
        var gb = UiHelp.CreateGroupBox("Registrar movimiento", SectionHelpText.CashMovementsRegister);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(8, 18, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        cboKind = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            Margin = new Padding(0, 4, 0, 4)
        };
        cboKind.Items.AddRange(["Entrada", "Salida"]);
        cboKind.SelectedIndex = 0;

        numAmount = new NumericUpDown
        {
            DecimalPlaces = 2,
            Maximum = 9999999.99m,
            Minimum = 0.01m,
            ThousandsSeparator = true,
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            TextAlign = HorizontalAlignment.Right,
            Margin = new Padding(0, 4, 0, 4),
            Value = 0.01m
        };

        txtConcept = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            Margin = new Padding(0, 4, 0, 4),
            MaxLength = 200
        };

        btnRegister = new Button
        {
            Text = "Registrar movimiento",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true,
            Font = UiTheme.UiBold,
            Margin = new Padding(0, 4, 0, 0),
            Height = 36
        };
        btnRegister.Click += BtnRegister_Click;

        stack.Controls.Add(CreateCaption("Tipo"), 0, 0);
        stack.Controls.Add(cboKind, 1, 0);
        stack.Controls.Add(CreateCaption("Monto"), 0, 1);
        stack.Controls.Add(numAmount, 1, 1);
        stack.Controls.Add(CreateCaption("Concepto"), 0, 2);
        stack.Controls.Add(txtConcept, 1, 2);
        stack.Controls.Add(btnRegister, 0, 3);
        stack.SetColumnSpan(btnRegister, 2);
        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateGridSection()
    {
        var gb = UiHelp.CreateGroupBox("Movimientos del día", SectionHelpText.CashMovementsList);
        dgvMovements = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = UiTheme.Ui,
            Margin = new Padding(8, 18, 8, 8)
        };
        dgvMovements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Hora",
            HeaderText = "Hora",
            FillWeight = 18
        });
        dgvMovements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Tipo",
            HeaderText = "Tipo",
            FillWeight = 16
        });
        dgvMovements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Concepto",
            HeaderText = "Concepto",
            FillWeight = 46
        });
        dgvMovements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Monto",
            HeaderText = "Monto",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        gb.Controls.Add(dgvMovements);
        return gb;
    }

    private Control CreateTotalsSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiTheme.WindowBg,
            Margin = new Padding(0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

        var totals = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = UiTheme.PanelBg
        };
        totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

        lblEntries = CreateAmountLabel();
        lblExits = CreateAmountLabel();
        lblNet = CreateAmountLabel();
        lblNet.Font = UiTheme.UiBold;

        totals.Controls.Add(lblEntries, 0, 0);
        totals.Controls.Add(lblExits, 1, 0);
        totals.Controls.Add(lblNet, 2, 0);

        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            Margin = new Padding(2, 2, 0, 0)
        };

        panel.Controls.Add(totals, 0, 0);
        panel.Controls.Add(lblStatus, 0, 1);
        return panel;
    }

    private void LoadDay()
    {
        var summary = _getDailyCashSummaryUseCase.Execute(dtpDate.Value);
        var movements = _getCashMovementsUseCase.Execute(dtpDate.Value);

        dgvMovements.Rows.Clear();
        foreach (var m in movements)
        {
            dgvMovements.Rows.Add(
                m.CreatedAt.ToString("HH:mm:ss"),
                m.Kind == CashMovementKind.Entrada ? "Entrada" : "Salida",
                m.Concept,
                m.Amount.ToString("C2"));
        }

        lblEntries.Text = $"Entradas: {summary.CashEntries:C2}";
        lblExits.Text = $"Salidas: {summary.CashExits:C2}";
        lblNet.Text = $"Neto: {summary.CashMovementsNet:C2}";

        bool closed = summary.ClosedAt != null;
        btnRegister.Enabled = !closed;
        txtConcept.Enabled = !closed;
        numAmount.Enabled = !closed;
        cboKind.Enabled = !closed;

        lblStatus.Text = closed
            ? $"Día cerrado ({summary.ClosedAt:dd/MM/yyyy HH:mm}). Solo consulta."
            : "Solo movimientos manuales (inyección/retiro). Las ventas se ven en Corte de caja.";
        lblStatus.ForeColor = closed ? Color.DarkOrange : UiTheme.Text;
    }

    private void BtnRegister_Click(object? sender, EventArgs e)
    {
        string concept = txtConcept.Text.Trim();
        if (string.IsNullOrWhiteSpace(concept))
        {
            MessageBox.Show(
                "El concepto es obligatorio.",
                "Movimientos de caja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            txtConcept.Focus();
            return;
        }

        var kind = cboKind.SelectedIndex == 0
            ? CashMovementKind.Entrada
            : CashMovementKind.Salida;

        try
        {
            _registerCashMovementUseCase.Execute(dtpDate.Value, kind, numAmount.Value, concept);
            txtConcept.Clear();
            numAmount.Value = 0.01m;
            LoadDay();
            lblStatus.Text = "Movimiento registrado.";
            lblStatus.ForeColor = UiTheme.StatusOk;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Movimientos de caja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static Label CreateCaption(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = UiTheme.Ui,
        ForeColor = UiTheme.Text,
        Margin = new Padding(0)
    };

    private static Label CreateAmountLabel() => new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = UiTheme.Ui,
        ForeColor = UiTheme.Text,
        Margin = new Padding(8, 0, 0, 0)
    };

}
