namespace FerreSys.Infrastructure.UI;

public class SeedOfferForm : Form
{
    private CheckBox chkLoad = null!;

    public bool LoadSampleCatalog => chkLoad.Checked;

    public SeedOfferForm()
    {
        Text = "Catálogo inicial";
        Size = new Size(480, 220);
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
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

        var lbl = new Label
        {
            Text = "La base está vacía. Puedes empezar con un catálogo de ejemplo o registrar tus productos desde cero.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text
        };

        chkLoad = new CheckBox
        {
            Text = "Cargar catálogo inicial de ejemplo (aproximadamente 150 productos frecuentes de ferretería)",
            Dock = DockStyle.Fill,
            Checked = true,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnContinue = new Button
        {
            Text = "Continuar",
            DialogResult = DialogResult.OK,
            Width = 110,
            Height = 34,
            Font = UiTheme.Ui
        };

        buttons.Controls.Add(btnContinue);
        root.Controls.Add(lbl, 0, 0);
        root.Controls.Add(chkLoad, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
        AcceptButton = btnContinue;
    }
}
