using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal sealed class RewardEditorForm : Form
{
    private readonly LoyaltyReward? _existing;
    private readonly ILoyaltyRepository _loyalty;
    private readonly TextBox _name = new() { Dock = DockStyle.Fill };
    private readonly TextBox _description = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _points = new() { Dock = DockStyle.Left, Width = 120, Minimum = 1, Maximum = 1000000, Value = 100 };
    private readonly ComboBox _kind = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _value = new() { Dock = DockStyle.Left, Width = 120, DecimalPlaces = 2, Maximum = 99999, Value = 10 };
    private readonly CheckBox _active = new() { Text = "Activo", AutoSize = true, Checked = true };

    public RewardEditorForm(LoyaltyReward? existing, ILoyaltyRepository loyalty)
    {
        _existing = existing;
        _loyalty = loyalty;

        Text = existing == null ? "Nuevo premio" : "Editar premio";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 320);
        Font = UiTheme.Ui;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.WindowBg;

        _kind.Items.AddRange(["Descuento en porcentaje (%)", "Descuento en monto fijo ($)", "Beneficio sin dinero"]);
        _kind.SelectedIndex = 0;
        _kind.SelectedIndexChanged += (_, _) => UpdateValueState();

        if (existing != null)
        {
            _name.Text = existing.Name;
            _description.Text = existing.Description;
            _points.Value = existing.PointsCost;
            _kind.SelectedIndex = existing.Kind switch
            {
                LoyaltyRewardKind.FixedAmount => 1,
                LoyaltyRewardKind.Benefit => 2,
                _ => 0
            };
            _value.Value = Math.Min(_value.Maximum, existing.Value);
            _active.Checked = existing.IsActive;
        }

        UpdateValueState();

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(16)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        AddRow(grid, 0, "Nombre", _name);
        AddRow(grid, 1, "Descripción", _description);
        AddRow(grid, 2, "Puntos", _points);
        AddRow(grid, 3, "Tipo", _kind);
        AddRow(grid, 4, "Valor", _value);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(8)
        };
        var cancel = new Button { Text = "Cancelar", Width = 100, DialogResult = DialogResult.Cancel };
        var save = new Button { Text = "Guardar", Width = 100 };
        save.Click += (_, _) => SaveReward();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        buttons.Controls.Add(_active);

        Controls.Add(grid);
        Controls.Add(buttons);
        CancelButton = cancel;
        AcceptButton = save;
    }

    private void UpdateValueState()
    {
        bool benefit = _kind.SelectedIndex == 2;
        _value.Enabled = !benefit;
        if (benefit)
            _value.Value = 0;
        else if (_value.Value <= 0)
            _value.Value = _kind.SelectedIndex == 0 ? 10 : 50;
    }

    private void SaveReward()
    {
        LoyaltyRewardKind kind = _kind.SelectedIndex switch
        {
            1 => LoyaltyRewardKind.FixedAmount,
            2 => LoyaltyRewardKind.Benefit,
            _ => LoyaltyRewardKind.Percent
        };

        try
        {
            new SaveLoyaltyRewardUseCase(_loyalty).Execute(new LoyaltyRewardDraft
            {
                Id = _existing?.Id,
                Name = _name.Text,
                Description = _description.Text,
                PointsCost = (int)_points.Value,
                Kind = kind,
                Value = kind == LoyaltyRewardKind.Benefit ? 0m : _value.Value,
                IsActive = _active.Checked
            });
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Premio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control editor)
    {
        grid.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.Text
        }, 0, row);
        grid.Controls.Add(editor, 1, row);
    }
}
