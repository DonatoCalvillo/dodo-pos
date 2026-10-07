using FerreSys.Application;

namespace FerreSys.Infrastructure.UI;

public class OpeningCashPromptForm : Form
{
    private readonly SaveOpeningCashUseCase _saveOpeningCashUseCase;
    private readonly DateTime _businessDate;
    private NumericUpDown numOpening = null!;

    public OpeningCashPromptForm(SaveOpeningCashUseCase saveOpeningCashUseCase, DateTime businessDate)
    {
        _saveOpeningCashUseCase = saveOpeningCashUseCase;
        _businessDate = businessDate.Date;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Fondo inicial de caja";
        Size = new Size(420, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;
        ShowInTaskbar = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16, 14, 16, 14),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

        var lbl = new Label
        {
            Text = $"Declara el fondo inicial del {_businessDate:dd/MM/yyyy} para dar cambio.\nPuedes cancelar y seguir cobrando.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text
        };

        numOpening = new NumericUpDown
        {
            DecimalPlaces = 2,
            Maximum = 9999999.99m,
            Minimum = 0,
            ThousandsSeparator = true,
            Dock = DockStyle.Left,
            Width = 180,
            Font = UiTheme.Ui,
            TextAlign = HorizontalAlignment.Right,
            Value = 1000m
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnCancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Width = 110,
            Height = 34,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.Ui,
            Margin = new Padding(8, 0, 0, 0)
        };

        var btnSave = new Button
        {
            Text = "Guardar fondo",
            Width = 130,
            Height = 34,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.UiBold,
            Margin = new Padding(0)
        };
        btnSave.Click += BtnSave_Click;

        buttons.Controls.Add(btnCancel);
        buttons.Controls.Add(btnSave);

        root.Controls.Add(lbl, 0, 0);
        root.Controls.Add(numOpening, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        UiHelp.AttachToForm(this, "Fondo inicial de caja", SectionHelpText.OpeningCashPrompt);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            _saveOpeningCashUseCase.Execute(_businessDate, numOpening.Value);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fondo de caja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
