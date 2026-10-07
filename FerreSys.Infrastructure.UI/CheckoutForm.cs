using System.Globalization;
using FerreSys.Application;

namespace FerreSys.Infrastructure.UI;

public partial class CheckoutForm : Form
{
    public string SelectedPaymentMethod { get; private set; } = "EFECTIVO";
    public string CardReference { get; private set; } = string.Empty;
    public decimal AmountReceived { get; private set; }
    public decimal CardAmount { get; private set; }

    private readonly decimal _totalAmount;
    private RadioButton rbCash = null!;
    private RadioButton rbCard = null!;
    private RadioButton rbMixed = null!;
    private TextBox txtCardRef = null!;
    private Label lblRef = null!;
    private TextBox txtCardAmount = null!;
    private Label lblCardAmount = null!;
    private Label lblCashNeeded = null!;
    private TextBox txtReceived = null!;
    private Label lblChangeCaption = null!;
    private Label lblChange = null!;
    private Label lblChangeBreakdown = null!;
    private Button btnConfirm = null!;
    private readonly List<Button> _billButtons = new();

    private static readonly decimal[] Bills = [20m, 50m, 100m, 200m, 500m, 1000m];

    public CheckoutForm(decimal totalAmount)
    {
        _totalAmount = totalAmount;
        AmountReceived = totalAmount;
        InitializeComponent(totalAmount);
    }

    private void InitializeComponent(decimal totalAmount)
    {
        Text = "Procesar Pago - FerreSys";
        Size = new Size(520, 640);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;
        KeyPreview = true;
        KeyDown += CheckoutForm_KeyDown;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10, 8, 10, 10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        root.Controls.Add(CreateTotalSection(totalAmount), 0, 0);
        root.Controls.Add(CreatePaymentSection(), 0, 1);
        root.Controls.Add(CreateCashSection(), 0, 2);
        root.Controls.Add(CreateActionsSection(), 0, 3);

        Controls.Add(root);
        txtReceived.Text = totalAmount.ToString("N2");
        UpdatePaymentState();
        Shown += (_, _) =>
        {
            if (rbCash.Checked)
            {
                txtReceived.Focus();
                txtReceived.SelectAll();
            }
        };
    }

    private GroupBox CreateTotalSection(decimal totalAmount)
    {
        var gb = UiHelp.CreateGroupBox("Total a cobrar", SectionHelpText.CheckoutTotal);

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 10, 8, 4),
            BackColor = UiTheme.PanelBg
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

        var lblCaption = new Label
        {
            Text = "TOTAL",
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.Total,
            BackColor = UiTheme.PanelBg,
            TextAlign = ContentAlignment.BottomCenter,
            Margin = new Padding(0)
        };

        var lblAmount = new Label
        {
            Text = $"${totalAmount:N2}",
            Dock = DockStyle.Fill,
            Font = UiTheme.TotalFont,
            ForeColor = UiTheme.Total,
            BackColor = UiTheme.PanelBg,
            TextAlign = ContentAlignment.TopCenter,
            Margin = new Padding(0)
        };

        stack.Controls.Add(lblCaption, 0, 0);
        stack.Controls.Add(lblAmount, 0, 1);
        gb.Controls.Add(stack);
        return gb;
    }

    private GroupBox CreatePaymentSection()
    {
        var gb = UiHelp.CreateGroupBox("Método de pago", SectionHelpText.CheckoutPayment);

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(8, 14, 8, 6),
            BackColor = UiTheme.PanelBg
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var methods = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        methods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        methods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        methods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

        rbCash = new RadioButton
        {
            Text = "Efectivo (F1)",
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.Text,
            Checked = true,
            Margin = new Padding(0)
        };
        rbCard = new RadioButton
        {
            Text = "Tarjeta (F2)",
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0)
        };
        rbMixed = new RadioButton
        {
            Text = "Mixto (F3)",
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0)
        };
        rbCash.CheckedChanged += (_, _) => UpdatePaymentState();
        rbCard.CheckedChanged += (_, _) => UpdatePaymentState();
        rbMixed.CheckedChanged += (_, _) => UpdatePaymentState();

        methods.Controls.Add(rbCash, 0, 0);
        methods.Controls.Add(rbCard, 1, 0);
        methods.Controls.Add(rbMixed, 2, 0);

        lblCardAmount = new Label
        {
            Text = "Monto tarjeta:",
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        txtCardAmount = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Search,
            ForeColor = UiTheme.Text,
            TextAlign = HorizontalAlignment.Right,
            Margin = new Padding(0, 2, 0, 0)
        };
        txtCardAmount.TextChanged += (_, _) => RefreshChange();

        var cardAmountRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        cardAmountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        cardAmountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cardAmountRow.Controls.Add(lblCardAmount, 0, 0);
        cardAmountRow.Controls.Add(txtCardAmount, 1, 0);

        lblRef = new Label
        {
            Text = "Últimos 4 dígitos / Folio terminal:",
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0, 2, 0, 0)
        };

        txtCardRef = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Search,
            ForeColor = UiTheme.Text,
            MaxLength = 10,
            Margin = new Padding(0, 2, 0, 0),
            PlaceholderText = "Ej. 1234 o folio de terminal"
        };

        stack.Controls.Add(methods, 0, 0);
        stack.Controls.Add(cardAmountRow, 0, 1);
        stack.Controls.Add(lblRef, 0, 2);
        stack.Controls.Add(txtCardRef, 0, 3);

        gb.Controls.Add(stack);
        return gb;
    }

    private GroupBox CreateCashSection()
    {
        var gb = UiHelp.CreateGroupBox("Cobro con cambio", SectionHelpText.CheckoutCash);

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(8, 14, 8, 6),
            BackColor = UiTheme.PanelBg
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        lblCashNeeded = new Label
        {
            Text = "Efectivo a cubrir: —",
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0)
        };

        var lblPay = new Label
        {
            Text = "El cliente paga con:",
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0)
        };

        txtReceived = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Search,
            ForeColor = UiTheme.Text,
            TextAlign = HorizontalAlignment.Right,
            Margin = new Padding(0, 2, 0, 0)
        };
        txtReceived.TextChanged += (_, _) => RefreshChange();
        txtReceived.Enter += (_, _) => txtReceived.SelectAll();

        var bills = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 0),
            BackColor = UiTheme.PanelBg,
            Padding = new Padding(0)
        };

        bills.Controls.Add(CreateBillButton("Exacto", null));
        foreach (decimal bill in Bills)
            bills.Controls.Add(CreateBillButton($"${bill:0}", bill));

        var changePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 2, 0, 0),
            BackColor = UiTheme.PanelBg
        };
        changePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        changePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        changePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        lblChangeCaption = new Label
        {
            Text = "CAMBIO",
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.StatusOk,
            BackColor = UiTheme.PanelBg,
            TextAlign = ContentAlignment.BottomCenter,
            Margin = new Padding(0)
        };

        lblChange = new Label
        {
            Text = "$0.00",
            Dock = DockStyle.Fill,
            Font = UiTheme.TotalFont,
            ForeColor = UiTheme.StatusOk,
            BackColor = UiTheme.PanelBg,
            TextAlign = ContentAlignment.TopCenter,
            Margin = new Padding(0)
        };

        lblChangeBreakdown = new Label
        {
            Text = string.Empty,
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            BackColor = UiTheme.PanelBg,
            TextAlign = ContentAlignment.TopCenter,
            Margin = new Padding(0)
        };

        changePanel.Controls.Add(lblChangeCaption, 0, 0);
        changePanel.Controls.Add(lblChange, 0, 1);
        changePanel.Controls.Add(lblChangeBreakdown, 0, 2);

        stack.Controls.Add(lblCashNeeded, 0, 0);
        stack.Controls.Add(lblPay, 0, 1);
        stack.Controls.Add(txtReceived, 0, 2);
        stack.Controls.Add(bills, 0, 3);
        stack.Controls.Add(changePanel, 0, 4);
        gb.Controls.Add(stack);
        return gb;
    }

    private Button CreateBillButton(string text, decimal? amount)
    {
        var button = new Button
        {
            Text = text,
            Width = 58,
            Height = 28,
            Margin = new Padding(0, 0, 4, 0),
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.Ui,
            Tag = amount
        };
        button.Click += (_, _) =>
        {
            decimal value = amount ?? GetCashNeeded();
            txtReceived.Text = value.ToString("N2");
            txtReceived.Focus();
            txtReceived.SelectAll();
        };
        _billButtons.Add(button);
        return button;
    }

    private Panel CreateActionsSection()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.WindowBg,
            Padding = new Padding(0, 6, 0, 0)
        };

        btnConfirm = new Button
        {
            Text = "  Confirmar y emitir ticket (Enter)",
            Image = UiIcons.Checkout(),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.Text,
            Padding = new Padding(10, 0, 10, 0)
        };
        btnConfirm.Click += BtnConfirm_Click;

        panel.Controls.Add(btnConfirm);
        return panel;
    }

    private bool UsesCard => rbCard.Checked || rbMixed.Checked;
    private bool UsesCash => rbCash.Checked || rbMixed.Checked;

    private decimal GetCashNeeded()
    {
        if (rbCash.Checked)
            return _totalAmount;

        if (rbMixed.Checked && TryParseMoney(txtCardAmount.Text, out decimal card))
        {
            card = decimal.Round(card, 2, MidpointRounding.AwayFromZero);
            if (card > 0 && card < _totalAmount)
                return decimal.Round(_totalAmount - card, 2, MidpointRounding.AwayFromZero);
        }

        return 0m;
    }

    private void UpdatePaymentState()
    {
        bool cardOnly = rbCard.Checked;
        bool mixed = rbMixed.Checked;
        bool cash = rbCash.Checked;

        txtCardRef.Enabled = UsesCard;
        lblRef.ForeColor = UsesCard ? UiTheme.Text : Color.DimGray;

        txtCardAmount.Enabled = mixed;
        lblCardAmount.ForeColor = mixed ? UiTheme.Text : Color.DimGray;
        if (!mixed)
            txtCardAmount.Clear();
        else if (string.IsNullOrWhiteSpace(txtCardAmount.Text))
            txtCardAmount.Text = Math.Min(100m, Math.Max(0m, _totalAmount - 1m)).ToString("N2");

        txtReceived.Enabled = UsesCash;
        foreach (Button button in _billButtons)
            button.Enabled = UsesCash;

        if (cardOnly)
        {
            txtCardRef.Focus();
            lblCashNeeded.Text = "Efectivo a cubrir: $0.00";
            lblChangeCaption.Text = "TARJETA";
            lblChangeCaption.ForeColor = UiTheme.Text;
            lblChange.Text = "Sin cambio";
            lblChange.ForeColor = UiTheme.Text;
            lblChangeBreakdown.Text = string.Empty;
            return;
        }

        if (cash && string.IsNullOrWhiteSpace(txtReceived.Text))
            txtReceived.Text = _totalAmount.ToString("N2");

        if (!UsesCard)
            txtCardRef.Clear();

        if (UsesCash)
        {
            txtReceived.Focus();
            txtReceived.SelectAll();
        }

        RefreshChange();
    }

    private void RefreshChange()
    {
        if (rbCard.Checked)
            return;

        decimal cashNeeded = GetCashNeeded();
        lblCashNeeded.Text = $"Efectivo a cubrir: ${cashNeeded:N2}";

        if (rbMixed.Checked)
        {
            if (!TryParseMoney(txtCardAmount.Text, out decimal card))
            {
                SetChangeError("CAMBIO", "—");
                lblChangeBreakdown.Text = "Indica el monto de tarjeta";
                return;
            }

            card = decimal.Round(card, 2, MidpointRounding.AwayFromZero);
            if (card <= 0m || card >= _totalAmount)
            {
                SetChangeError("CAMBIO", "—");
                lblChangeBreakdown.Text = "Tarjeta debe ser > 0 y < total";
                return;
            }
        }

        if (!TryParseMoney(txtReceived.Text, out decimal received))
        {
            SetChangeError("CAMBIO", "—");
            lblChangeBreakdown.Text = string.Empty;
            return;
        }

        decimal difference = decimal.Round(received - cashNeeded, 2, MidpointRounding.AwayFromZero);
        if (difference < 0)
        {
            SetChangeError("FALTA", $"${Math.Abs(difference):N2}");
            lblChangeBreakdown.Text = string.Empty;
            return;
        }

        lblChangeCaption.Text = "CAMBIO";
        lblChangeCaption.ForeColor = UiTheme.StatusOk;
        lblChange.Text = $"${difference:N2}";
        lblChange.ForeColor = UiTheme.StatusOk;
        lblChangeBreakdown.Text = difference > 0
            ? string.Join("   ", ChangeCalculator.FormatLines(difference))
            : "Sin cambio";
    }

    private void SetChangeError(string caption, string amount)
    {
        lblChangeCaption.Text = caption;
        lblChangeCaption.ForeColor = Color.FromArgb(180, 40, 40);
        lblChange.Text = amount;
        lblChange.ForeColor = Color.FromArgb(180, 40, 40);
    }

    private void CheckoutForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F1)
        {
            rbCash.Checked = true;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.F2)
        {
            rbCard.Checked = true;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.F3)
        {
            rbMixed.Checked = true;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            btnConfirm.PerformClick();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            e.Handled = true;
        }
    }

    private void BtnConfirm_Click(object? sender, EventArgs e)
    {
        if (rbCard.Checked)
        {
            SelectedPaymentMethod = "TARJETA";
            CardReference = txtCardRef.Text.Trim();
            AmountReceived = 0m;
            CardAmount = _totalAmount;

            if (string.IsNullOrWhiteSpace(CardReference))
            {
                MessageBox.Show(
                    "Por seguridad, introduce los últimos 4 dígitos o folio de la terminal para pagos con tarjeta.",
                    "Datos Faltantes",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtCardRef.Focus();
                return;
            }
        }
        else if (rbMixed.Checked)
        {
            if (!TryParseMoney(txtCardAmount.Text, out decimal card))
            {
                MessageBox.Show(
                    "Escribe el monto que se cobra con tarjeta.",
                    "Cobro mixto",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtCardAmount.Focus();
                return;
            }

            card = decimal.Round(card, 2, MidpointRounding.AwayFromZero);
            if (card <= 0m || card >= _totalAmount)
            {
                MessageBox.Show(
                    "En cobro mixto, el monto de tarjeta debe ser mayor a $0 y menor al total.",
                    "Cobro mixto",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtCardAmount.Focus();
                txtCardAmount.SelectAll();
                return;
            }

            CardReference = txtCardRef.Text.Trim();
            if (string.IsNullOrWhiteSpace(CardReference))
            {
                MessageBox.Show(
                    "Por seguridad, introduce los últimos 4 dígitos o folio de la terminal para la parte con tarjeta.",
                    "Datos Faltantes",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtCardRef.Focus();
                return;
            }

            if (!TryParseMoney(txtReceived.Text, out decimal received))
            {
                MessageBox.Show(
                    "Escribe con cuánto efectivo paga el cliente, o elige el billete.",
                    "Cobro mixto",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtReceived.Focus();
                txtReceived.SelectAll();
                return;
            }

            received = decimal.Round(received, 2, MidpointRounding.AwayFromZero);
            decimal cashNeeded = decimal.Round(_totalAmount - card, 2, MidpointRounding.AwayFromZero);
            if (received < cashNeeded)
            {
                decimal missing = cashNeeded - received;
                MessageBox.Show(
                    $"El efectivo no alcanza. Faltan ${missing:N2}.",
                    "Cobro con cambio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtReceived.Focus();
                txtReceived.SelectAll();
                return;
            }

            SelectedPaymentMethod = "MIXTO";
            CardAmount = card;
            AmountReceived = received;
        }
        else
        {
            if (!TryParseMoney(txtReceived.Text, out decimal received))
            {
                MessageBox.Show(
                    "Escribe con cuánto paga el cliente, o elige el billete.",
                    "Efectivo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtReceived.Focus();
                txtReceived.SelectAll();
                return;
            }

            received = decimal.Round(received, 2, MidpointRounding.AwayFromZero);
            if (received < _totalAmount)
            {
                decimal missing = _totalAmount - received;
                MessageBox.Show(
                    $"El efectivo no alcanza. Faltan ${missing:N2}.",
                    "Cobro con cambio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtReceived.Focus();
                txtReceived.SelectAll();
                return;
            }

            SelectedPaymentMethod = "EFECTIVO";
            CardReference = string.Empty;
            AmountReceived = received;
            CardAmount = 0m;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static bool TryParseMoney(string text, out decimal value)
    {
        text = text.Trim().TrimStart('$').Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0m;
            return false;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
