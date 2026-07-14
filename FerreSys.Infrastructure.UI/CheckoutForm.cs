namespace FerreSys.Infrastructure.UI;

public partial class CheckoutForm : Form
{
    public string SelectedPaymentMethod { get; private set; } = "EFECTIVO";
    public string CardReference { get; private set; } = string.Empty;

    private RadioButton rbCash = null!;
    private RadioButton rbCard = null!;
    private TextBox txtCardRef = null!;
    private Label lblRef = null!;
    private Button btnConfirm = null!;

    public CheckoutForm(decimal totalAmount)
    {
        InitializeComponent(totalAmount);
    }

    private void InitializeComponent(decimal totalAmount)
    {
        Text = "Procesar Pago - FerreSys";
        Size = new Size(420, 400);
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
            RowCount = 3,
            Padding = new Padding(10, 8, 10, 10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        root.Controls.Add(CreateTotalSection(totalAmount), 0, 0);
        root.Controls.Add(CreatePaymentSection(), 0, 1);
        root.Controls.Add(CreateActionsSection(), 0, 2);

        Controls.Add(root);
        UpdateCardRefState();
    }

    private GroupBox CreateTotalSection(decimal totalAmount)
    {
        var gb = CreateGroupBox("Total a cobrar");

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 16, 8, 8),
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
        var gb = CreateGroupBox("Método de pago");

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 14, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var methods = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        methods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        methods.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

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
        rbCash.CheckedChanged += (_, _) => UpdateCardRefState();
        rbCard.CheckedChanged += (_, _) => UpdateCardRefState();

        methods.Controls.Add(rbCash, 0, 0);
        methods.Controls.Add(rbCard, 1, 0);

        lblRef = new Label
        {
            Text = "Últimos 4 dígitos / Folio terminal:",
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0, 4, 0, 0)
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
        stack.Controls.Add(lblRef, 0, 1);
        stack.Controls.Add(txtCardRef, 0, 2);

        gb.Controls.Add(stack);
        return gb;
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

    private static GroupBox CreateGroupBox(string title)
    {
        return new GroupBox
        {
            Text = " " + title + " ",
            Dock = DockStyle.Fill,
            Font = UiTheme.GroupFont,
            ForeColor = UiTheme.GroupTitle,
            BackColor = UiTheme.PanelBg,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 6)
        };
    }

    private void UpdateCardRefState()
    {
        bool card = rbCard.Checked;
        txtCardRef.Enabled = card;
        lblRef.ForeColor = card ? UiTheme.Text : Color.DimGray;
        if (card)
            txtCardRef.Focus();
        else
            txtCardRef.Clear();
    }

    private void CheckoutForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F1)
        {
            rbCash.Checked = true;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F2)
        {
            rbCard.Checked = true;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            btnConfirm.PerformClick();
            e.Handled = true;
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
        else
        {
            SelectedPaymentMethod = "EFECTIVO";
            CardReference = string.Empty;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
