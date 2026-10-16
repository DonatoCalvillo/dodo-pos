using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal sealed class SupplierEditorForm : Form
{
    private readonly Supplier? _existing;
    private readonly ISupplierRepository _suppliers;
    private readonly TextBox _name = new() { Dock = DockStyle.Fill };
    private readonly TextBox _contact = new() { Dock = DockStyle.Fill };
    private readonly TextBox _phone = new() { Dock = DockStyle.Fill };
    private readonly TextBox _email = new() { Dock = DockStyle.Fill };
    private readonly TextBox _address = new() { Dock = DockStyle.Fill };
    private readonly TextBox _notes = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _active = new() { Text = "Activo", AutoSize = true, Checked = true };

    public SupplierEditorForm(Supplier? existing, ISupplierRepository suppliers)
    {
        _existing = existing;
        _suppliers = suppliers;
        Text = existing == null ? "Nuevo proveedor" : "Editar proveedor";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 420);
        Font = UiTheme.Ui;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.WindowBg;

        if (existing != null)
        {
            _name.Text = existing.Name;
            _contact.Text = existing.ContactName;
            _phone.Text = existing.Phone;
            _email.Text = existing.Email;
            _address.Text = existing.Address;
            _notes.Text = existing.Notes;
            _active.Checked = existing.IsActive;
        }

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(16),
            BackColor = UiTheme.WindowBg
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(grid, 0, "Nombre", _name);
        AddRow(grid, 1, "Contacto", _contact);
        AddRow(grid, 2, "Teléfono", _phone);
        AddRow(grid, 3, "Correo", _email);
        AddRow(grid, 4, "Dirección", _address);
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.Controls.Add(LabelOf("Notas"), 0, 5);
        grid.Controls.Add(_notes, 1, 5);
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        grid.Controls.Add(_active, 1, 6);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8)
        };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(100, 30) };
        var save = new Button { Text = "Guardar", AutoSize = true, MinimumSize = new Size(100, 30), FlatStyle = FlatStyle.System };
        save.Click += (_, _) => Save();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);

        Controls.Add(grid);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void Save()
    {
        try
        {
            new SaveSupplierUseCase(_suppliers).Execute(new SupplierDraft
            {
                Id = _existing?.Id,
                Name = _name.Text,
                ContactName = _contact.Text,
                Phone = _phone.Text,
                Email = _email.Text,
                Address = _address.Text,
                Notes = _notes.Text,
                IsActive = _active.Checked
            });
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "Proveedor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void AddRow(TableLayoutPanel grid, int row, string caption, Control editor)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.Controls.Add(LabelOf(caption), 0, row);
        grid.Controls.Add(editor, 1, row);
    }

    private static Label LabelOf(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = UiTheme.Text
    };
}
