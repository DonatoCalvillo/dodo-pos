using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal sealed class LoyaltyForm : Form
{
    private readonly ILoyaltyRepository _loyalty;
    private readonly IAppSettingsStore _settings;
    private readonly DataGridView _customers = CreateGrid();
    private readonly DataGridView _rewards = CreateGrid();
    private readonly CheckBox _enabled = new() { Text = "Programa activo", AutoSize = true };
    private readonly RadioButton _percentMode = new() { Text = "Porcentaje de la venta", AutoSize = true, Checked = true };
    private readonly RadioButton _blockMode = new() { Text = "Puntos por cada monto", AutoSize = true };
    private readonly NumericUpDown _percent = new() { DecimalPlaces = 2, Minimum = 0.01m, Maximum = 1000, Value = 10, Width = 100 };
    private readonly NumericUpDown _pointsPerBlock = new() { Minimum = 1, Maximum = 100000, Value = 1, Width = 100 };
    private readonly NumericUpDown _amountPerBlock = new() { DecimalPlaces = 2, Minimum = 0.01m, Maximum = 999999, Value = 10, Width = 100 };
    private readonly RadioButton _paidBase = new() { Text = "Total cobrado", AutoSize = true, Checked = true };
    private readonly RadioButton _subtotalBase = new() { Text = "Subtotal", AutoSize = true };
    private readonly Label _example = new() { AutoSize = true, ForeColor = UiTheme.TextMuted, MaximumSize = new Size(520, 0) };
    private IReadOnlyList<CustomerBalance> _customerRows = [];
    private IReadOnlyList<LoyaltyReward> _rewardRows = [];

    public LoyaltyForm(ILoyaltyRepository loyalty, IAppSettingsStore settings)
    {
        _loyalty = loyalty;
        _settings = settings;

        Text = "Fidelidad";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 520);
        Font = UiTheme.Ui;
        MinimizeBox = false;
        BackColor = UiTheme.WindowBg;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CustomersPage());
        tabs.TabPages.Add(RewardsPage());
        tabs.TabPages.Add(SettingsPage());
        Controls.Add(tabs);

        Load += (_, _) =>
        {
            ReloadCustomers();
            ReloadRewards();
            LoadSettings();
        };
    }

    private TabPage CustomersPage()
    {
        ConfigureCustomerColumns();
        _customers.CellDoubleClick += (_, _) => EditCustomer(SelectedCustomer(), required: true);

        var page = new TabPage("Clientes") { BackColor = UiTheme.WindowBg, Padding = new Padding(8) };
        page.Controls.Add(_customers);
        page.Controls.Add(Buttons(
            Action("Nuevo", (_, _) => EditCustomer(null, required: false)),
            Action("Editar", (_, _) => EditCustomer(SelectedCustomer(), required: true)),
            Action("Activar / Desactivar", (_, _) => ToggleCustomer()),
            Action("Cerrar", (_, _) => Close())));
        return page;
    }

    private TabPage RewardsPage()
    {
        ConfigureRewardColumns();
        _rewards.CellDoubleClick += (_, _) => EditReward(SelectedReward(), required: true);

        var page = new TabPage("Premios") { BackColor = UiTheme.WindowBg, Padding = new Padding(8) };
        page.Controls.Add(_rewards);
        page.Controls.Add(Buttons(
            Action("Nuevo", (_, _) => EditReward(null, required: false)),
            Action("Editar", (_, _) => EditReward(SelectedReward(), required: true)),
            Action("Activar / Desactivar", (_, _) => ToggleReward()),
            Action("Cerrar", (_, _) => Close())));
        return page;
    }

    private TabPage SettingsPage()
    {
        var page = new TabPage("Puntos") { BackColor = UiTheme.WindowBg, Padding = new Padding(16) };
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8)
        };

        stack.Controls.Add(_enabled);
        stack.Controls.Add(SectionLabel("Cómo se ganan los puntos"));
        stack.Controls.Add(_percentMode);
        stack.Controls.Add(Inline("Porcentaje", _percent, "Ejemplo: 10 en una venta de $500 da 50 puntos."));
        stack.Controls.Add(_blockMode);
        stack.Controls.Add(BlockRow());
        stack.Controls.Add(SectionLabel("Base del cálculo"));
        stack.Controls.Add(_paidBase);
        stack.Controls.Add(_subtotalBase);
        stack.Controls.Add(_example);

        _percentMode.CheckedChanged += (_, _) => RefreshExample();
        _blockMode.CheckedChanged += (_, _) => RefreshExample();
        _paidBase.CheckedChanged += (_, _) => RefreshExample();
        _subtotalBase.CheckedChanged += (_, _) => RefreshExample();
        _enabled.CheckedChanged += (_, _) => RefreshExample();
        _percent.ValueChanged += (_, _) => RefreshExample();
        _pointsPerBlock.ValueChanged += (_, _) => RefreshExample();
        _amountPerBlock.ValueChanged += (_, _) => RefreshExample();

        var save = new Button { Text = "Guardar reglas", AutoSize = true, Margin = new Padding(0, 16, 0, 0) };
        save.Click += (_, _) => SaveSettings();
        stack.Controls.Add(save);
        page.Controls.Add(stack);
        return page;
    }

    private void ConfigureCustomerColumns()
    {
        _customers.Columns.Add("Name", "Nombre");
        _customers.Columns.Add("Phone", "Teléfono");
        _customers.Columns.Add("Email", "Correo");
        _customers.Columns.Add("Points", "Puntos");
        _customers.Columns.Add("Active", "Activo");
        _customers.Columns["Points"]!.FillWeight = 50;
        _customers.Columns["Active"]!.FillWeight = 40;
    }

    private void ConfigureRewardColumns()
    {
        _rewards.Columns.Add("Name", "Premio");
        _rewards.Columns.Add("Points", "Puntos");
        _rewards.Columns.Add("Kind", "Tipo");
        _rewards.Columns.Add("Value", "Valor");
        _rewards.Columns.Add("Active", "Activo");
        _rewards.Columns["Points"]!.FillWeight = 40;
        _rewards.Columns["Active"]!.FillWeight = 40;
    }

    private void ReloadCustomers()
    {
        _customerRows = _loyalty.ListCustomers();
        _customers.Rows.Clear();
        foreach (CustomerBalance row in _customerRows)
        {
            int index = _customers.Rows.Add(
                row.Customer.Name,
                row.Customer.Phone,
                row.Customer.Email,
                row.Points,
                row.Customer.IsActive ? "Sí" : "No");
            _customers.Rows[index].Tag = row.Customer.Id;
        }
    }

    private void ReloadRewards()
    {
        _rewardRows = _loyalty.ListRewards();
        _rewards.Rows.Clear();
        foreach (LoyaltyReward reward in _rewardRows)
        {
            int index = _rewards.Rows.Add(
                reward.Name,
                reward.PointsCost,
                KindLabel(reward.Kind),
                reward.ValueLabel,
                reward.IsActive ? "Sí" : "No");
            _rewards.Rows[index].Tag = reward.Id;
        }
    }

    private void EditCustomer(Customer? customer, bool required)
    {
        if (required && customer == null)
        {
            MessageBox.Show("Selecciona un cliente.", "Fidelidad", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var editor = new CustomerEditorForm(customer, _loyalty);
        if (editor.ShowDialog(this) == DialogResult.OK)
            ReloadCustomers();
    }

    private void EditReward(LoyaltyReward? reward, bool required)
    {
        if (required && reward == null)
        {
            MessageBox.Show("Selecciona un premio.", "Fidelidad", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var editor = new RewardEditorForm(reward, _loyalty);
        if (editor.ShowDialog(this) == DialogResult.OK)
            ReloadRewards();
    }

    private void ToggleCustomer()
    {
        Customer? customer = SelectedCustomer();
        if (customer == null)
        {
            MessageBox.Show("Selecciona un cliente.", "Fidelidad", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            new SaveCustomerUseCase(_loyalty).Execute(new CustomerDraft
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                Notes = customer.Notes,
                IsActive = !customer.IsActive,
                Rfc = customer.Rfc,
                LegalName = customer.LegalName,
                TaxRegime = customer.TaxRegime,
                TaxZipCode = customer.TaxZipCode,
                CfdiUse = customer.CfdiUse
            });
            ReloadCustomers();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Cliente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ToggleReward()
    {
        LoyaltyReward? reward = SelectedReward();
        if (reward == null)
        {
            MessageBox.Show("Selecciona un premio.", "Fidelidad", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            new SaveLoyaltyRewardUseCase(_loyalty).Execute(new LoyaltyRewardDraft
            {
                Id = reward.Id,
                Name = reward.Name,
                Description = reward.Description,
                PointsCost = reward.PointsCost,
                Kind = reward.Kind,
                Value = reward.Value,
                IsActive = !reward.IsActive
            });
            ReloadRewards();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Premio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private Customer? SelectedCustomer()
    {
        if (_customers.CurrentRow?.Tag is not Guid id)
            return null;

        return _customerRows.FirstOrDefault(row => row.Customer.Id == id)?.Customer;
    }

    private LoyaltyReward? SelectedReward()
    {
        if (_rewards.CurrentRow?.Tag is not Guid id)
            return null;

        return _rewardRows.FirstOrDefault(reward => reward.Id == id);
    }

    private Control BlockRow()
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(16, 0, 0, 4)
        };
        row.Controls.Add(_pointsPerBlock);
        row.Controls.Add(new Label
        {
            Text = "punto(s) por cada $",
            AutoSize = true,
            Padding = new Padding(8, 6, 8, 0)
        });
        row.Controls.Add(_amountPerBlock);
        return row;
    }

    private void LoadSettings()
    {
        LoyaltyProgramSettings current = _settings.GetLoyaltySettings();
        _enabled.Checked = current.Enabled;
        _percentMode.Checked = current.EarnMode == LoyaltyEarnMode.PercentOfSale;
        _blockMode.Checked = current.EarnMode == LoyaltyEarnMode.PointsPerAmount;
        _percent.Value = Clamp(_percent, current.EarnPercent);
        _pointsPerBlock.Value = Clamp(_pointsPerBlock, current.PointsPerBlock);
        _amountPerBlock.Value = Clamp(_amountPerBlock, current.AmountPerBlock);
        _paidBase.Checked = current.EarnBase == LoyaltyEarnBase.PaidTotal;
        _subtotalBase.Checked = current.EarnBase == LoyaltyEarnBase.Subtotal;
        RefreshExample();
    }

    private void SaveSettings()
    {
        var draft = new LoyaltyProgramSettings
        {
            Enabled = _enabled.Checked,
            EarnMode = _blockMode.Checked ? LoyaltyEarnMode.PointsPerAmount : LoyaltyEarnMode.PercentOfSale,
            EarnPercent = _percent.Value,
            PointsPerBlock = (int)_pointsPerBlock.Value,
            AmountPerBlock = _amountPerBlock.Value,
            EarnBase = _subtotalBase.Checked ? LoyaltyEarnBase.Subtotal : LoyaltyEarnBase.PaidTotal
        };

        try
        {
            new SaveLoyaltySettingsUseCase(_settings).Execute(draft);
            MessageBox.Show("Las reglas de puntos quedaron guardadas.", "Fidelidad", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Fidelidad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshExample()
    {
        var draft = new LoyaltyProgramSettings
        {
            Enabled = true,
            EarnMode = _blockMode.Checked ? LoyaltyEarnMode.PointsPerAmount : LoyaltyEarnMode.PercentOfSale,
            EarnPercent = _percent.Value,
            PointsPerBlock = (int)_pointsPerBlock.Value,
            AmountPerBlock = _amountPerBlock.Value,
            EarnBase = LoyaltyEarnBase.PaidTotal
        };
        int points = draft.CalculatePoints(500m, 500m);
        string mode = _blockMode.Checked
            ? $"{_pointsPerBlock.Value:0} punto(s) por cada ${_amountPerBlock.Value:N2}"
            : $"{_percent.Value:0.##}% de la venta";
        string state = _enabled.Checked ? "Programa activo." : "Programa apagado: las ventas no suman puntos.";
        _example.Text = $"{state} Con las reglas de ahora, una venta de $500 genera {points} puntos ({mode}).";
    }

    private static decimal Clamp(NumericUpDown input, decimal value) =>
        Math.Min(input.Maximum, Math.Max(input.Minimum, value));

    private static string KindLabel(LoyaltyRewardKind kind) => kind switch
    {
        LoyaltyRewardKind.Percent => "Porcentaje",
        LoyaltyRewardKind.FixedAmount => "Monto fijo",
        _ => "Beneficio"
    };

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Color.White,
        Font = UiTheme.Ui
    };

    private static FlowLayoutPanel Buttons(params Button[] buttons)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.LeftToRight
        };
        foreach (Button button in buttons)
            panel.Controls.Add(button);
        return panel;
    }

    private static Button Action(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(4) };
        button.Click += click;
        return button;
    }

    private static Label SectionLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = UiTheme.UiBold,
        ForeColor = UiTheme.GroupTitle,
        Margin = new Padding(0, 14, 0, 4)
    };

    private static FlowLayoutPanel Inline(string caption, Control editor, string hint)
    {
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(16, 0, 0, 4)
        };
        row.Controls.Add(new Label
        {
            Text = caption,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 6, 8, 0)
        });
        row.Controls.Add(editor);
        if (hint.Length > 0)
        {
            row.Controls.Add(new Label
            {
                Text = hint,
                AutoSize = true,
                ForeColor = UiTheme.TextMuted,
                Padding = new Padding(8, 6, 0, 0)
            });
        }

        return row;
    }
}
