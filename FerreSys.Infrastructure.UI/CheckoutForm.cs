using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;

namespace FerreSys.Infrastructure.UI;

public partial class CheckoutForm : Form
{
    public string SelectedPaymentMethod { get; private set; } = "EFECTIVO";
    public string CardReference { get; private set; } = string.Empty;
    public decimal AmountReceived { get; private set; }
    public decimal CardAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public LoyaltyCheckoutRequest? LoyaltyRequest { get; private set; }

    private readonly decimal _subtotal;
    private readonly ILoyaltyRepository _loyalty;
    private readonly LoyaltyProgramSettings _loyaltySettings;
    private decimal _totalAmount;
    private bool _syncingDiscount;
    private RadioButton rbCash = null!;
    private RadioButton rbCard = null!;
    private RadioButton rbMixed = null!;
    private TextBox txtCardRef = null!;
    private Label lblRef = null!;
    private TextBox txtCardAmount = null!;
    private Label lblCardAmount = null!;
    private Label lblCashNeeded = null!;
    private TextBox txtReceived = null!;
    private Panel _changeBanner = null!;
    private Label lblChangeCaption = null!;
    private Label lblChange = null!;
    private Label lblChangeBreakdown = null!;
    private Button btnConfirm = null!;
    private readonly List<Button> _billButtons = new();
    private ComboBox cboTicketDiscount = null!;
    private NumericUpDown numTicketDiscount = null!;
    private Label lblSubtotal = null!;
    private Label lblTicketDiscount = null!;
    private Label lblPayable = null!;
    private ComboBox cboLoyaltyCustomer = null!;
    private Button btnClearLoyalty = null!;
    private Label lblLoyaltyStatus = null!;
    private ComboBox cboLoyaltyReward = null!;
    private Customer? _loyaltyCustomer;
    private int _loyaltyBalance;
    private bool _suppressLoyalty;

    private static readonly decimal[] Bills = [20m, 50m, 100m, 200m, 500m, 1000m];

    public CheckoutForm(decimal totalAmount, ILoyaltyRepository? loyalty = null, LoyaltyProgramSettings? settings = null)
    {
        if (loyalty == null || settings == null)
        {
            var db = new SqliteDbContext();
            loyalty ??= new SqliteLoyaltyRepository(db);
            settings ??= new SqliteAppSettingsStore(db).GetLoyaltySettings();
        }

        _loyalty = loyalty;
        _loyaltySettings = settings;
        _subtotal = totalAmount;
        _totalAmount = totalAmount;
        AmountReceived = totalAmount;
        InitializeComponent(totalAmount);
    }

    private void InitializeComponent(decimal totalAmount)
    {
        Text = "Procesar Pago - Posly";
        ClientSize = new Size(640, 816);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;
        DoubleBuffered = true;
        KeyPreview = true;
        KeyDown += CheckoutForm_KeyDown;
        Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(148, 163, 184), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        var header = CreateDialogHeader();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(16, 10, 16, 12),
            BackColor = UiTheme.WindowBg
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 148f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 156f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));

        root.Controls.Add(CreateTotalSection(totalAmount), 0, 0);
        root.Controls.Add(CreateLoyaltySection(), 0, 1);
        root.Controls.Add(CreateDiscountSection(), 0, 2);
        root.Controls.Add(CreatePaymentSection(), 0, 3);
        root.Controls.Add(CreateCashSection(), 0, 4);
        root.Controls.Add(CreateActionsSection(), 0, 5);

        Controls.Add(header);
        Controls.Add(root);
        header.SendToBack();
        root.BringToFront();

        txtReceived.Text = totalAmount.ToString("N2");
        UpdatePaymentState();
        RefreshLoyaltyStatus();

        Shown += (_, _) =>
        {
            ApplyRoundedCorners();
            if (rbCash.Checked)
            {
                txtReceived.Focus();
                txtReceived.SelectAll();
            }
        };
    }

    private Panel CreateDialogHeader()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = UiTheme.HeaderBg,
            Padding = new Padding(16, 0, 10, 0)
        };
        bar.MouseDown += OnDragMouseDown;

        var title = new Label
        {
            Text = "PROCESAR COBRO DE VENTA",
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 11, 0, 0)
        };
        title.MouseDown += OnDragMouseDown;

        var btnClose = new Button
        {
            Text = "✕",
            Dock = DockStyle.Right,
            Width = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.HeaderBg,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TabStop = false,
            UseVisualStyleBackColor = false
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);
        btnClose.MouseEnter += (_, _) => btnClose.ForeColor = Color.White;
        btnClose.MouseLeave += (_, _) => btnClose.ForeColor = Color.FromArgb(148, 163, 184);
        btnClose.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var shortcutHint = new Label
        {
            Text = "F1 Efectivo  ·  F2 Tarjeta  ·  F3 Mixto  ·  Esc Cancelar  ",
            Dock = DockStyle.Right,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 13, 6, 0)
        };
        shortcutHint.MouseDown += OnDragMouseDown;

        bar.Controls.Add(shortcutHint);
        bar.Controls.Add(btnClose);
        bar.Controls.Add(title);
        return bar;
    }

    private Panel CreateTotalSection(decimal totalAmount)
    {
        var visor = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(18, 8, 18, 8),
            BackColor = UiTheme.HeaderBg
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lblSubtotal = new Label
        {
            Text = $"Subtotal: ${_subtotal:N2}",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        var lblCaption = new Label
        {
            Text = "TOTAL A COBRAR",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(56, 189, 248),
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0)
        };

        lblTicketDiscount = new Label
        {
            Text = "Sin descuento de ticket",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(148, 163, 184),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        lblPayable = new Label
        {
            Text = totalAmount.ToString("C2", CultureInfo.GetCultureInfo("es-MX")),
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        grid.Controls.Add(lblSubtotal, 0, 0);
        grid.Controls.Add(lblCaption, 1, 0);
        grid.Controls.Add(lblTicketDiscount, 0, 1);
        grid.Controls.Add(lblPayable, 1, 1);

        visor.Controls.Add(grid);
        return visor;
    }

    private Panel CreateLoyaltySection()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88f));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));

        cboLoyaltyCustomer = new ComboBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Margin = new Padding(0, 2, 8, 0),
            Enabled = _loyaltySettings.Enabled
        };
        cboLoyaltyCustomer.TextUpdate += LoyaltyCustomerTextUpdated;
        cboLoyaltyCustomer.SelectionChangeCommitted += LoyaltyCustomerChosen;

        btnClearLoyalty = new Button
        {
            Text = "Quitar",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 0),
            Enabled = _loyaltySettings.Enabled
        };
        btnClearLoyalty.Click += (_, _) => ClearLoyaltyCustomer();

        lblLoyaltyStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.TextMuted,
            Margin = new Padding(0)
        };

        cboLoyaltyReward = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Margin = new Padding(0, 2, 0, 0),
            Enabled = false
        };
        cboLoyaltyReward.SelectedIndexChanged += (_, _) =>
        {
            if (!_suppressLoyalty)
                ApplyTicketDiscount();
        };

        body.Controls.Add(cboLoyaltyCustomer, 0, 0);
        body.Controls.Add(btnClearLoyalty, 1, 0);
        body.Controls.Add(lblLoyaltyStatus, 0, 1);
        body.SetColumnSpan(lblLoyaltyStatus, 2);
        body.Controls.Add(cboLoyaltyReward, 0, 2);
        body.SetColumnSpan(cboLoyaltyReward, 2);
        FillLoyaltyRewards();

        return CreateSectionCard("CLIENTE DE FIDELIDAD", "Cliente de fidelidad", SectionHelpText.CheckoutLoyalty, body);
    }

    private void LoyaltyCustomerTextUpdated(object? sender, EventArgs e)
    {
        if (_suppressLoyalty || !_loyaltySettings.Enabled)
            return;

        if (cboLoyaltyCustomer.SelectedIndex >= 0 && cboLoyaltyCustomer.SelectedItem is CustomerBalance picked)
        {
            _loyaltyCustomer = picked.Customer;
            _loyaltyBalance = picked.Points;
            FillLoyaltyRewards();
            RefreshLoyaltyStatus();
            ApplyTicketDiscount();
            return;
        }

        string term = cboLoyaltyCustomer.Text;
        _loyaltyCustomer = null;
        _loyaltyBalance = 0;
        FillLoyaltyRewards();

        int caret = cboLoyaltyCustomer.SelectionStart;
        _suppressLoyalty = true;
        cboLoyaltyCustomer.Items.Clear();
        if (term.Trim().Length >= 2)
        {
            foreach (CustomerBalance row in _loyalty.SearchActive(term.Trim()))
                cboLoyaltyCustomer.Items.Add(row);
        }

        cboLoyaltyCustomer.Text = term;
        cboLoyaltyCustomer.SelectionStart = Math.Min(caret, term.Length);
        _suppressLoyalty = false;
        RefreshLoyaltyStatus();
        ApplyTicketDiscount();

        if (cboLoyaltyCustomer.Items.Count > 0 && cboLoyaltyCustomer.Focused)
        {
            string keep = term;
            BeginInvoke(() =>
            {
                if (IsDisposed || cboLoyaltyCustomer.Text != keep || cboLoyaltyCustomer.Items.Count == 0)
                    return;

                cboLoyaltyCustomer.DroppedDown = true;
                Cursor.Current = Cursors.Default;
            });
        }
    }

    private void LoyaltyCustomerChosen(object? sender, EventArgs e)
    {
        if (_suppressLoyalty || cboLoyaltyCustomer.SelectedItem is not CustomerBalance row)
            return;

        _loyaltyCustomer = row.Customer;
        _loyaltyBalance = row.Points;
        FillLoyaltyRewards();
        RefreshLoyaltyStatus();
        ApplyTicketDiscount();
    }

    private void ClearLoyaltyCustomer()
    {
        _suppressLoyalty = true;
        _loyaltyCustomer = null;
        _loyaltyBalance = 0;
        cboLoyaltyCustomer.SelectedIndex = -1;
        cboLoyaltyCustomer.Text = string.Empty;
        cboLoyaltyCustomer.Items.Clear();
        _suppressLoyalty = false;
        FillLoyaltyRewards();
        RefreshLoyaltyStatus();
        ApplyTicketDiscount();
    }

    private void FillLoyaltyRewards()
    {
        if (cboLoyaltyReward == null)
            return;

        Guid? selected = (cboLoyaltyReward.SelectedItem as RewardChoice)?.Reward?.Id;
        _suppressLoyalty = true;
        cboLoyaltyReward.Items.Clear();
        cboLoyaltyReward.Items.Add(new RewardChoice(null, "Sin premio"));
        int index = 0;
        if (_loyaltySettings.Enabled && _loyaltyCustomer != null)
        {
            foreach (LoyaltyReward reward in _loyalty.ListRewards())
            {
                if (!reward.IsActive || reward.PointsCost > _loyaltyBalance)
                    continue;

                cboLoyaltyReward.Items.Add(new RewardChoice(
                    reward,
                    $"{reward.Name} · {reward.PointsCost} pts · {reward.ValueLabel}"));
                if (reward.Id == selected)
                    index = cboLoyaltyReward.Items.Count - 1;
            }
        }

        cboLoyaltyReward.SelectedIndex = cboLoyaltyReward.Items.Count == 0 ? -1 : index;
        cboLoyaltyReward.Enabled = _loyaltySettings.Enabled && _loyaltyCustomer != null;
        _suppressLoyalty = false;
    }

    private decimal ManualTicketDiscount()
    {
        if (cboTicketDiscount == null || numTicketDiscount == null)
            return 0m;

        return cboTicketDiscount.SelectedIndex switch
        {
            1 => decimal.Round(_subtotal * numTicketDiscount.Value / 100m, 2, MidpointRounding.AwayFromZero),
            2 => decimal.Round(numTicketDiscount.Value, 2, MidpointRounding.AwayFromZero),
            _ => 0m
        };
    }

    private decimal CurrentLoyaltyDiscount()
    {
        if (!_loyaltySettings.Enabled || _loyaltyCustomer == null)
            return 0m;

        return (cboLoyaltyReward.SelectedItem as RewardChoice)?.Reward?.DiscountFor(_subtotal) ?? 0m;
    }

    private void RefreshLoyaltyStatus()
    {
        if (lblLoyaltyStatus == null)
            return;

        if (!_loyaltySettings.Enabled)
        {
            lblLoyaltyStatus.Text = "El programa de fidelidad está apagado.";
            return;
        }

        if (_loyaltyCustomer == null)
        {
            lblLoyaltyStatus.Text = "Sin cliente. La venta no suma puntos.";
            return;
        }

        decimal payable = _totalAmount > 0m && _totalAmount <= _subtotal ? _totalAmount : _subtotal;
        int points = _loyaltySettings.CalculatePoints(_subtotal, payable);
        lblLoyaltyStatus.Text = $"Saldo: {_loyaltyBalance} puntos    ·    Esta venta: {points} puntos";
    }

    private sealed class RewardChoice
    {
        public LoyaltyReward? Reward { get; }
        private readonly string _label;

        public RewardChoice(LoyaltyReward? reward, string label)
        {
            Reward = reward;
            _label = label;
        }

        public override string ToString() => _label;
    }

    private Panel CreateDiscountSection()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        cboTicketDiscount = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            FlatStyle = FlatStyle.Standard,
            Margin = new Padding(0, 0, 8, 0)
        };
        cboTicketDiscount.Items.AddRange(["Sin descuento", "Descuento en porcentaje (%)", "Descuento en monto fijo ($)"]);
        cboTicketDiscount.SelectedIndex = 0;
        cboTicketDiscount.SelectedIndexChanged += (_, _) => ApplyTicketDiscount();

        numTicketDiscount = new NumericUpDown
        {
            DecimalPlaces = 2,
            Maximum = 99999,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            TextAlign = HorizontalAlignment.Right,
            Margin = new Padding(8, 0, 0, 0),
            Enabled = false
        };
        numTicketDiscount.ValueChanged += (_, _) => ApplyTicketDiscount();

        body.Controls.Add(cboTicketDiscount, 0, 0);
        body.Controls.Add(numTicketDiscount, 1, 0);

        return CreateSectionCard("DESCUENTO DEL TICKET", "Descuento del ticket", SectionHelpText.CheckoutDiscount, body);
    }

    private Panel CreatePaymentSection()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Row 0: 3 Segmented Method Tiles
        var methods = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = UiTheme.PanelBg
        };
        methods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        methods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        methods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        methods.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        rbCash = CreateMethodTile("Efectivo (F1)", isChecked: true, new Padding(0, 0, 4, 0));
        rbCard = CreateMethodTile("Tarjeta (F2)", isChecked: false, new Padding(4, 0, 4, 0));
        rbMixed = CreateMethodTile("Mixto (F3)", isChecked: false, new Padding(4, 0, 0, 0));

        rbCash.CheckedChanged += (_, _) =>
        {
            StyleMethodTile(rbCash);
            UpdatePaymentState();
        };
        rbCard.CheckedChanged += (_, _) =>
        {
            StyleMethodTile(rbCard);
            UpdatePaymentState();
        };
        rbMixed.CheckedChanged += (_, _) =>
        {
            StyleMethodTile(rbMixed);
            UpdatePaymentState();
        };

        methods.Controls.Add(rbCash, 0, 0);
        methods.Controls.Add(rbCard, 1, 0);
        methods.Controls.Add(rbMixed, 2, 0);

        // Row 1: Side-by-side Card Amount & Card Reference (with proper height so text is never clipped)
        var cardFieldsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 2, 0, 0),
            BackColor = UiTheme.PanelBg
        };
        cardFieldsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        cardFieldsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        cardFieldsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        cardFieldsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lblCardAmount = new Label
        {
            Text = "Monto a cobrar en tarjeta:",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0, 0, 6, 2)
        };

        lblRef = new Label
        {
            Text = "Últimos 4 dígitos o folio terminal:",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(6, 0, 0, 2)
        };

        txtCardAmount = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            TextAlign = HorizontalAlignment.Right,
            PlaceholderText = "0.00"
        };
        txtCardAmount.TextChanged += (_, _) => RefreshChange();

        txtCardRef = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            ForeColor = UiTheme.Text,
            MaxLength = 10,
            PlaceholderText = "Ej. 1234 o folio de voucher"
        };

        cardFieldsGrid.Controls.Add(lblCardAmount, 0, 0);
        cardFieldsGrid.Controls.Add(lblRef, 1, 0);
        cardFieldsGrid.Controls.Add(WrapTextBox(txtCardAmount, new Padding(0, 2, 6, 2)), 0, 1);
        cardFieldsGrid.Controls.Add(WrapTextBox(txtCardRef, new Padding(6, 2, 0, 2)), 1, 1);

        stack.Controls.Add(methods, 0, 0);
        stack.Controls.Add(cardFieldsGrid, 0, 1);

        return CreateSectionCard("MÉTODO DE PAGO", "Método de pago", SectionHelpText.CheckoutPayment, stack);
    }

    private Panel CreateCashSection()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12, 8, 12, 10),
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f)); // Row 0: Cash needed + Customer pays with
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f)); // Row 1: 7 Quick Bill Buttons
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Row 2: Change Visor Box

        // Row 0: Left = Efectivo a cubrir + Right = El cliente paga con [Input]
        var topCashRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = UiTheme.PanelBg
        };
        topCashRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        topCashRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        topCashRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lblCashNeeded = new Label
        {
            Text = "Efectivo a cubrir: $0.00",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        var payBox = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        payBox.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
        payBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        payBox.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblPay = new Label
        {
            Text = "Paga con:",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = UiTheme.TextMuted,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0, 0, 6, 0)
        };

        txtReceived = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = UiTheme.GroupTitle,
            TextAlign = HorizontalAlignment.Right
        };
        txtReceived.TextChanged += (_, _) => RefreshChange();
        txtReceived.Enter += (_, _) => txtReceived.SelectAll();

        payBox.Controls.Add(lblPay, 0, 0);
        payBox.Controls.Add(WrapTextBox(txtReceived, new Padding(0, 1, 0, 1)), 1, 0);

        topCashRow.Controls.Add(lblCashNeeded, 0, 0);
        topCashRow.Controls.Add(payBox, 1, 0);

        // Row 1: 7 Evenly-Spaced Bill Buttons
        var billsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 6),
            BackColor = UiTheme.PanelBg
        };
        for (int i = 0; i < 7; i++)
            billsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7f));
        billsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        billsGrid.Controls.Add(CreateBillButton("Exacto", null, isFirst: true), 0, 0);
        for (int i = 0; i < Bills.Length; i++)
        {
            decimal bill = Bills[i];
            billsGrid.Controls.Add(CreateBillButton($"${bill:0}", bill, isFirst: false), i + 1, 0);
        }

        // Row 2: Dedicated Change Display Banner (Zero overlap between $0.00 and breakdown!)
        _changeBanner = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 0),
            Padding = new Padding(16, 8, 16, 8),
            BackColor = Color.FromArgb(240, 253, 244)
        };
        _changeBanner.Paint += (s, e) =>
        {
            if (s is not Control c) return;
            using var pen = new Pen(Color.FromArgb(187, 247, 208), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        };

        var changeLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        changeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        changeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        changeLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        changeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lblChangeCaption = new Label
        {
            Text = "CAMBIO A ENTREGAR",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = UiTheme.StatusOk,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0)
        };

        lblChangeBreakdown = new Label
        {
            Text = "Sin cambio",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = UiTheme.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.TopLeft,
            Margin = new Padding(0, 2, 0, 0)
        };

        lblChange = new Label
        {
            Text = "$0.00",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = UiTheme.StatusOk,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        changeLayout.Controls.Add(lblChangeCaption, 0, 0);
        changeLayout.Controls.Add(lblChangeBreakdown, 0, 1);
        changeLayout.Controls.Add(lblChange, 1, 0);
        changeLayout.SetRowSpan(lblChange, 2);

        _changeBanner.Controls.Add(changeLayout);

        stack.Controls.Add(topCashRow, 0, 0);
        stack.Controls.Add(billsGrid, 0, 1);
        stack.Controls.Add(_changeBanner, 0, 2);

        return CreateSectionCard("COBRO EN EFECTIVO Y CAMBIO", "Cobro con cambio", SectionHelpText.CheckoutCash, stack);
    }

    private RadioButton CreateMethodTile(string text, bool isChecked, Padding margin)
    {
        var rb = new RadioButton
        {
            Text = text,
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Checked = isChecked,
            Cursor = Cursors.Hand,
            Margin = margin,
            UseVisualStyleBackColor = false
        };
        rb.FlatAppearance.BorderSize = 1;
        StyleMethodTile(rb);
        return rb;
    }

    private static void StyleMethodTile(RadioButton rb)
    {
        if (rb.Checked)
        {
            rb.BackColor = Color.FromArgb(224, 242, 254);
            rb.ForeColor = Color.FromArgb(3, 105, 161);
            rb.FlatAppearance.BorderColor = UiTheme.BrandPrimary;
            rb.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 242, 254);
            rb.FlatAppearance.MouseOverBackColor = Color.FromArgb(186, 230, 253);
        }
        else
        {
            rb.BackColor = Color.FromArgb(248, 250, 252);
            rb.ForeColor = UiTheme.TextMuted;
            rb.FlatAppearance.BorderColor = UiTheme.Border;
            rb.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        }
    }

    private static Panel WrapTextBox(TextBox textBox, Padding margin)
    {
        var box = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = margin,
            BackColor = Color.White,
            Cursor = Cursors.IBeam
        };
        box.Click += (_, _) => textBox.Focus();
        textBox.GotFocus += (_, _) => box.Invalidate();
        textBox.LostFocus += (_, _) => box.Invalidate();
        textBox.EnabledChanged += (_, _) =>
        {
            Color bg = textBox.Enabled ? Color.White : Color.FromArgb(241, 245, 249);
            box.BackColor = bg;
            textBox.BackColor = bg;
            box.Invalidate();
        };

        void LayoutInnerTextBox()
        {
            int th = textBox.PreferredHeight;
            int w = Math.Max(20, box.ClientSize.Width - 16);
            int top = Math.Max(2, (box.ClientSize.Height - th) / 2);
            textBox.SetBounds(8, top, w, th);
        }
        box.Resize += (_, _) => LayoutInnerTextBox();
        box.HandleCreated += (_, _) => LayoutInnerTextBox();

        box.Paint += (s, e) =>
        {
            if (s is not Control c) return;
            Color borderColor = !textBox.Enabled
                ? Color.FromArgb(226, 232, 240)
                : textBox.Focused
                    ? UiTheme.BrandPrimary
                    : Color.FromArgb(203, 213, 225);
            float width = textBox.Focused ? 1.8f : 1f;
            using var pen = new Pen(borderColor, width);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        };
        box.Controls.Add(textBox);
        return box;
    }

    private Button CreateBillButton(string text, decimal? amount, bool isFirst)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(isFirst ? 0 : 3, 0, 3, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = UiTheme.Text,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Tag = amount
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(224, 242, 254);
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
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.WindowBg,
            Margin = new Padding(0),
            Padding = new Padding(0, 2, 0, 0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var btnCancel = new Button
        {
            Text = "Cancelar (Esc)",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = UiTheme.TextMuted,
            Font = UiTheme.UiBold,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            UseVisualStyleBackColor = false
        };
        btnCancel.FlatAppearance.BorderColor = UiTheme.Border;
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        btnConfirm = new Button
        {
            Text = "  Confirmar y emitir ticket (Enter)",
            Image = UiIcons.Checkout(22),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.CheckoutGreen,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0),
            Padding = new Padding(12, 0, 12, 0),
            UseVisualStyleBackColor = false
        };
        btnConfirm.FlatAppearance.BorderSize = 0;
        btnConfirm.FlatAppearance.MouseOverBackColor = UiTheme.CheckoutGreenHover;
        btnConfirm.Click += BtnConfirm_Click;

        grid.Controls.Add(btnCancel, 0, 0);
        grid.Controls.Add(btnConfirm, 1, 0);
        return grid;
    }

    private static Panel CreateSectionCard(string title, string helpTitle, string helpText, Control body)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(1),
            BackColor = UiTheme.PanelBg
        };
        card.Paint += (s, e) =>
        {
            if (s is not Control c) return;
            using var pen = new Pen(UiTheme.Border, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
        };

        var cardGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        cardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cardGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        cardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(12, 0, 34, 0),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        header.Paint += (s, e) =>
        {
            if (s is not Control c) return;
            using var accent = new SolidBrush(UiTheme.BrandPrimary);
            e.Graphics.FillRectangle(accent, 0, 5, 3, c.Height - 10);
            using var line = new Pen(UiTheme.Border, 1f);
            e.Graphics.DrawLine(line, 0, c.Height - 1, c.Width, c.Height - 1);
        };

        var lbl = new Label
        {
            Text = title,
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            ForeColor = UiTheme.GroupTitle,
            Padding = new Padding(0, 5, 0, 0)
        };
        header.Controls.Add(lbl);
        UiHelp.Attach(header, helpTitle, helpText);

        body.Dock = DockStyle.Fill;
        cardGrid.Controls.Add(header, 0, 0);
        cardGrid.Controls.Add(body, 0, 1);
        card.Controls.Add(cardGrid);
        return card;
    }

    private void ApplyTicketDiscount()
    {
        if (_syncingDiscount || cboTicketDiscount == null || numTicketDiscount == null || lblPayable == null)
            return;

        _syncingDiscount = true;
        try
        {
            DiscountKind kind = cboTicketDiscount.SelectedIndex switch
            {
                1 => DiscountKind.Percent,
                2 => DiscountKind.FixedAmount,
                _ => DiscountKind.None
            };

            numTicketDiscount.Enabled = kind != DiscountKind.None;
            decimal cap = kind == DiscountKind.Percent ? 99.99m : 99999m;
            if (numTicketDiscount.Value > cap)
                numTicketDiscount.Value = cap;
            numTicketDiscount.Maximum = cap;

            decimal previous = _totalAmount;
            decimal manual = ManualTicketDiscount();
            decimal loyaltyDiscount = CurrentLoyaltyDiscount();
            decimal raw = manual + loyaltyDiscount;

            if (raw > 0m && raw >= _subtotal)
            {
                _totalAmount = _subtotal;
                lblTicketDiscount.Text = "El descuento debe ser menor al subtotal.";
                lblTicketDiscount.ForeColor = Color.FromArgb(248, 113, 113);
            }
            else
            {
                _totalAmount = _subtotal - raw;
                string prize = loyaltyDiscount > 0m ? $" (premio -${loyaltyDiscount:N2})" : "";
                lblTicketDiscount.Text = raw > 0m ? $"Descuento aplicado: -${raw:N2}{prize}" : "Sin descuento de ticket";
                lblTicketDiscount.ForeColor = raw > 0m ? Color.FromArgb(74, 222, 128) : Color.FromArgb(148, 163, 184);
            }

            lblSubtotal.Text = $"Subtotal: ${_subtotal:N2}";
            lblPayable.Text = _totalAmount.ToString("C2", CultureInfo.GetCultureInfo("es-MX"));

            if (rbCash != null && rbCash.Checked && TryParseMoney(txtReceived.Text, out decimal received) && received == previous)
                txtReceived.Text = _totalAmount.ToString("N2");
        }
        finally
        {
            _syncingDiscount = false;
        }

        if (rbCash != null)
            RefreshChange();

        RefreshLoyaltyStatus();
    }

    private bool TryCommitTicketDiscount()
    {
        if (_loyaltySettings.Enabled
            && _loyaltyCustomer == null
            && cboLoyaltyCustomer.Text.Trim().Length > 0)
        {
            MessageBox.Show(
                "Selecciona un cliente de la lista o borra la búsqueda.",
                "Fidelidad",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            cboLoyaltyCustomer.Focus();
            return false;
        }

        decimal loyaltyDiscount = CurrentLoyaltyDiscount();
        decimal raw = ManualTicketDiscount() + loyaltyDiscount;

        if (raw > 0m && raw >= _subtotal)
        {
            MessageBox.Show(
                "El descuento del ticket debe ser menor al subtotal.",
                "Descuento",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        DiscountAmount = raw;
        _totalAmount = _subtotal - raw;
        if (_loyaltySettings.Enabled && _loyaltyCustomer != null)
        {
            LoyaltyRequest = new LoyaltyCheckoutRequest
            {
                CustomerId = _loyaltyCustomer.Id,
                RewardId = (cboLoyaltyReward.SelectedItem as RewardChoice)?.Reward?.Id,
                LoyaltyDiscountAmount = loyaltyDiscount
            };
        }
        else
        {
            LoyaltyRequest = null;
        }

        return true;
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
        lblRef.ForeColor = UsesCard ? UiTheme.Text : UiTheme.TextMuted;

        txtCardAmount.Enabled = mixed;
        lblCardAmount.ForeColor = mixed ? UiTheme.Text : UiTheme.TextMuted;
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
            _changeBanner.BackColor = Color.FromArgb(248, 250, 252);
            lblChangeCaption.Text = "COBRO CON TARJETA";
            lblChangeCaption.ForeColor = UiTheme.BrandPrimary;
            lblChange.Text = "Sin cambio";
            lblChange.ForeColor = UiTheme.Text;
            lblChangeBreakdown.Text = "Capturar referencia o últimos 4 dígitos";
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
            SetChangeError("FALTA POR CUBRIR", $"${Math.Abs(difference):N2}");
            lblChangeBreakdown.Text = "Monto insuficiente en efectivo";
            return;
        }

        _changeBanner.BackColor = Color.FromArgb(240, 253, 244);
        lblChangeCaption.Text = "CAMBIO A ENTREGAR";
        lblChangeCaption.ForeColor = UiTheme.StatusOk;
        lblChange.Text = $"${difference:N2}";
        lblChange.ForeColor = UiTheme.StatusOk;
        lblChangeBreakdown.Text = difference > 0
            ? string.Join("   ", ChangeCalculator.FormatLines(difference))
            : "Pago exacto (Sin cambio)";
    }

    private void SetChangeError(string caption, string amount)
    {
        _changeBanner.BackColor = Color.FromArgb(254, 242, 242);
        lblChangeCaption.Text = caption;
        lblChangeCaption.ForeColor = UiTheme.StatusDanger;
        lblChange.Text = amount;
        lblChange.ForeColor = UiTheme.StatusDanger;
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
        if (!TryCommitTicketDiscount())
            return;

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

    private void OnDragMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        ReleaseCapture();
        SendMessage(Handle, 0x00A1, 0x0002, 0);
    }

    private void ApplyRoundedCorners()
    {
        try
        {
            int preference = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(Handle, 33, ref preference, sizeof(int));
        }
        catch
        {
            // Ignore on older Windows versions
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

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
