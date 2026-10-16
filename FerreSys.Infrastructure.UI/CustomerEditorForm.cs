using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal sealed class CustomerEditorForm : Form
{
    private readonly Customer? _existing;
    private readonly ILoyaltyRepository _loyalty;
    private readonly TextBox _name = new() { Dock = DockStyle.Fill };
    private readonly TextBox _phone = new() { Dock = DockStyle.Fill };
    private readonly TextBox _email = new() { Dock = DockStyle.Fill };
    private readonly TextBox _notes = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _rfc = new() { Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper };
    private readonly TextBox _legalName = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _taxRegime = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _taxZip = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _cfdiUse = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _active = new() { Text = "Activo", AutoSize = true, Checked = true };

    public CustomerEditorForm(Customer? existing, ILoyaltyRepository loyalty)
    {
        _existing = existing;
        _loyalty = loyalty;

        Text = existing == null ? "Nuevo cliente" : "Editar cliente";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 560);
        Font = UiTheme.Ui;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.WindowBg;

        FillCatalog(_taxRegime, FiscalCatalog.TaxRegimes);
        FillCatalog(_cfdiUse, FiscalCatalog.CfdiUses);

        if (existing != null)
        {
            _name.Text = existing.Name;
            _phone.Text = existing.Phone;
            _email.Text = existing.Email;
            _notes.Text = existing.Notes;
            _rfc.Text = existing.Rfc;
            _legalName.Text = existing.LegalName;
            _taxZip.Text = existing.TaxZipCode;
            SelectCode(_taxRegime, existing.TaxRegime);
            SelectCode(_cfdiUse, existing.CfdiUse);
            _active.Checked = existing.IsActive;
        }

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(16, 12, 16, 8)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        AddRow(grid, 0, "Nombre", _name);
        AddRow(grid, 1, "Teléfono", _phone);
        AddRow(grid, 2, "Correo", _email);
        AddRow(grid, 3, "Notas", _notes);
        grid.Controls.Add(new Label
        {
            Text = "Datos fiscales (opcionales)",
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.GroupTitle,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 4);
        grid.SetColumnSpan(grid.GetControlFromPosition(0, 4)!, 2);
        AddRow(grid, 5, "RFC", _rfc);
        AddRow(grid, 6, "Razón social", _legalName);
        AddRow(grid, 7, "Régimen", _taxRegime);
        AddRow(grid, 8, "C.P. fiscal", _taxZip);
        AddRow(grid, 9, "Uso CFDI", _cfdiUse);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(8)
        };
        var cancel = new Button { Text = "Cancelar", Width = 100, DialogResult = DialogResult.Cancel };
        var save = new Button { Text = "Guardar", Width = 100 };
        save.Click += (_, _) => SaveCustomer();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        buttons.Controls.Add(_active);

        Controls.Add(grid);
        Controls.Add(buttons);
        CancelButton = cancel;
        AcceptButton = save;
    }

    private void SaveCustomer()
    {
        try
        {
            new SaveCustomerUseCase(_loyalty).Execute(new CustomerDraft
            {
                Id = _existing?.Id,
                Name = _name.Text,
                Phone = _phone.Text,
                Email = _email.Text,
                Notes = _notes.Text,
                IsActive = _active.Checked,
                Rfc = _rfc.Text,
                LegalName = _legalName.Text,
                TaxRegime = SelectedCode(_taxRegime),
                TaxZipCode = _taxZip.Text,
                CfdiUse = SelectedCode(_cfdiUse)
            });
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Cliente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

    private static void FillCatalog(ComboBox combo, IReadOnlyList<(string Code, string Name)> items)
    {
        combo.Items.Add(new CatalogChoice(string.Empty, "Sin capturar"));
        foreach ((string code, string name) in items)
            combo.Items.Add(new CatalogChoice(code, $"{code} · {name}"));
        combo.SelectedIndex = 0;
    }

    private static void SelectCode(ComboBox combo, string code)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is CatalogChoice choice && choice.Code == code)
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        combo.SelectedIndex = 0;
    }

    private static string SelectedCode(ComboBox combo) =>
        combo.SelectedItem is CatalogChoice choice ? choice.Code : string.Empty;

    private sealed class CatalogChoice
    {
        public string Code { get; }
        private readonly string _label;

        public CatalogChoice(string code, string label)
        {
            Code = code;
            _label = label;
        }

        public override string ToString() => _label;
    }
}
