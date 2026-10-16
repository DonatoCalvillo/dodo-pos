using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal sealed class SuppliersForm : Form
{
    private readonly ISupplierRepository _suppliers;
    private readonly DataGridView _grid = new();

    public SuppliersForm(ISupplierRepository suppliers)
    {
        _suppliers = suppliers;
        Text = "Proveedores - Posly";
        Size = new Size(860, 520);
        MinimumSize = new Size(680, 400);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        StyleGrid(_grid);
        _grid.Columns.Add("Name", "Proveedor");
        _grid.Columns.Add("Contact", "Contacto");
        _grid.Columns.Add("Phone", "Teléfono");
        _grid.Columns.Add("Email", "Correo");
        _grid.Columns.Add("Active", "Estado");
        _grid.Columns["Name"]!.FillWeight = 160;
        _grid.Columns["Email"]!.FillWeight = 160;
        _grid.CellDoubleClick += (_, _) => EditSelected();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var close = ButtonOf("Cerrar", false);
        close.Click += (_, _) => Close();
        var toggle = ButtonOf("Activar / Desactivar", false);
        toggle.Click += (_, _) => ToggleSelected();
        var edit = ButtonOf("Editar", true);
        edit.Click += (_, _) => EditSelected();
        var create = ButtonOf("Nuevo", true);
        create.Click += (_, _) => CreateSupplier();
        buttons.Controls.AddRange([close, toggle, edit, create]);

        root.Controls.Add(_grid, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        Controls.Add(root);
        LoadRows();
    }

    private void LoadRows()
    {
        _grid.Rows.Clear();
        foreach (Supplier supplier in _suppliers.List())
        {
            int index = _grid.Rows.Add(
                supplier.Name,
                supplier.ContactName,
                supplier.Phone,
                supplier.Email,
                supplier.IsActive ? "Activo" : "Inactivo");
            _grid.Rows[index].Tag = supplier.Id;
        }
    }

    private void CreateSupplier()
    {
        using var editor = new SupplierEditorForm(null, _suppliers);
        if (editor.ShowDialog(this) == DialogResult.OK)
            LoadRows();
    }

    private void EditSelected()
    {
        Supplier? supplier = Selected();
        if (supplier == null)
            return;

        using var editor = new SupplierEditorForm(supplier, _suppliers);
        if (editor.ShowDialog(this) == DialogResult.OK)
            LoadRows();
    }

    private void ToggleSelected()
    {
        Supplier? supplier = Selected();
        if (supplier == null)
            return;

        var updated = new Supplier(
            supplier.Id,
            supplier.Name,
            supplier.ContactName,
            supplier.Phone,
            supplier.Email,
            supplier.Address,
            supplier.Notes,
            !supplier.IsActive,
            supplier.CreatedAt);
        _suppliers.Save(updated);
        LoadRows();
    }

    private Supplier? Selected()
    {
        if (_grid.CurrentRow?.Tag is not Guid id)
        {
            MessageBox.Show(this, "Selecciona un proveedor.", "Proveedores", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return _suppliers.Get(id);
    }

    private static Button ButtonOf(string text, bool primary) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(120, 32),
        Margin = new Padding(6, 0, 0, 0),
        BackColor = primary ? Color.FromArgb(220, 235, 255) : SystemColors.Control,
        FlatStyle = FlatStyle.System
    };

    internal static void StyleGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0, 70, 180),
            ForeColor = Color.White,
            Font = UiTheme.UiBold,
            SelectionBackColor = Color.FromArgb(0, 70, 180)
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = UiTheme.Ui,
            SelectionBackColor = Color.FromArgb(210, 225, 245),
            SelectionForeColor = Color.Black
        };
    }
}
