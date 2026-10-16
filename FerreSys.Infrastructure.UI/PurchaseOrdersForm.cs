using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;

namespace FerreSys.Infrastructure.UI;

internal sealed class PurchaseOrdersForm : Form
{
    private readonly ISupplierRepository _suppliers;
    private readonly IProductRepository _products;
    private readonly IPurchaseOrderRepository _orders;
    private readonly GetReorderSuggestionsUseCase _suggestions;
    private readonly SqliteAppSettingsStore _settings;
    private readonly string _actor;
    private readonly ComboBox _supplier = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly ComboBox _status = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Short, Width = 120 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Short, Width = 120 };
    private readonly DataGridView _grid = new();

    public PurchaseOrdersForm(
        ISupplierRepository suppliers,
        IProductRepository products,
        IPurchaseOrderRepository orders,
        GetReorderSuggestionsUseCase suggestions,
        SqliteAppSettingsStore settings,
        string actor)
    {
        _suppliers = suppliers;
        _products = products;
        _orders = orders;
        _suggestions = suggestions;
        _settings = settings;
        _actor = actor;

        Text = "Órdenes de compra - Posly";
        Size = new Size(980, 560);
        MinimumSize = new Size(800, 420);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        _from.Value = DateTime.Today.AddMonths(-3);
        _to.Value = DateTime.Today;
        _supplier.Items.Add(new SupplierFilter(null));
        foreach (Supplier supplier in suppliers.List())
            _supplier.Items.Add(new SupplierFilter(supplier));
        _supplier.SelectedIndex = 0;
        _status.Items.Add("Todos");
        foreach (PurchaseOrderStatus status in Enum.GetValues<PurchaseOrderStatus>())
            _status.Items.Add(status.DisplayName());
        _status.SelectedIndex = 0;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        filters.Controls.Add(Inline("Desde", _from));
        filters.Controls.Add(Inline("Hasta", _to));
        filters.Controls.Add(Inline("Proveedor", _supplier));
        filters.Controls.Add(Inline("Estado", _status));
        var search = new Button { Text = "Consultar", AutoSize = true, MinimumSize = new Size(100, 28), FlatStyle = FlatStyle.System };
        search.Click += (_, _) => LoadRows();
        filters.Controls.Add(search);

        SuppliersForm.StyleGrid(_grid);
        _grid.Columns.Add("Number", "Folio");
        _grid.Columns.Add("Date", "Fecha");
        _grid.Columns.Add("Supplier", "Proveedor");
        _grid.Columns.Add("Status", "Estado");
        _grid.Columns.Add("Total", "Total");
        _grid.Columns.Add("Sent", "Enviada");
        _grid.Columns["Supplier"]!.FillWeight = 180;
        _grid.Columns["Total"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _grid.CellDoubleClick += (_, _) => OpenSelected();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var close = ActionButton("Cerrar");
        close.Click += (_, _) => Close();
        var response = ActionButton("Registrar respuesta");
        response.Click += (_, _) => DecideSelected();
        var email = ActionButton("Enviar por correo");
        email.Click += (_, _) => EmailSelected();
        var print = ActionButton("Imprimir / PDF…");
        print.Click += (_, _) => PrintSelected();
        var open = ActionButton("Abrir");
        open.Click += (_, _) => OpenSelected();
        var create = ActionButton("Nueva");
        create.Click += (_, _) => OpenEditor(null, null);
        buttons.Controls.AddRange([close, response, email, print, open, create]);

        root.Controls.Add(filters, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
        LoadRows();
    }

    private void LoadRows()
    {
        Guid? supplierId = _supplier.SelectedItem is SupplierFilter filter ? filter.Supplier?.Id : null;
        PurchaseOrderStatus? status = _status.SelectedIndex <= 0
            ? null
            : Enum.GetValues<PurchaseOrderStatus>()[_status.SelectedIndex - 1];

        _grid.Rows.Clear();
        foreach (PurchaseOrder order in _orders.List(supplierId, status, _from.Value.Date, _to.Value.Date))
        {
            int index = _grid.Rows.Add(
                order.Number,
                order.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                order.SupplierName,
                order.Status.DisplayName(),
                order.Total.ToString("C2"),
                order.SentAt?.ToString("dd/MM/yyyy HH:mm") ?? "");
            _grid.Rows[index].Tag = order.Id;
        }
    }

    private void OpenEditor(PurchaseOrder? order, IReadOnlyList<PurchaseOrderSeedLine>? seeds)
    {
        using var editor = new PurchaseOrderEditorForm(
            order,
            seeds,
            _suppliers,
            _products,
            _orders,
            _suggestions,
            _settings,
            _actor);
        editor.ShowDialog(this);
        LoadRows();
    }

    private void OpenSelected()
    {
        PurchaseOrder? order = Selected();
        if (order != null)
            OpenEditor(order, null);
    }

    private void PrintSelected()
    {
        PurchaseOrder? order = Selected();
        if (order == null)
            return;
        PurchaseOrderDocuments.Preview(this, order, _suppliers.Get(order.SupplierId), _settings);
    }

    private void EmailSelected()
    {
        PurchaseOrder? order = Selected();
        if (order == null)
            return;
        try
        {
            PurchaseOrderDocuments.SendEmail(
                this,
                order,
                _suppliers.Get(order.SupplierId),
                _settings,
                new MarkPurchaseOrderSentUseCase(_orders));
            LoadRows();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "Orden de compra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void DecideSelected()
    {
        PurchaseOrder? order = Selected();
        if (order == null)
            return;
        if (!order.IsDraft)
        {
            MessageBox.Show(this, "Esta orden ya tiene respuesta.", "Orden de compra",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new PurchaseOrderDecisionForm(order, new DecidePurchaseOrderUseCase(_orders), _actor);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            LoadRows();
    }

    private PurchaseOrder? Selected()
    {
        if (_grid.CurrentRow?.Tag is not Guid id)
        {
            MessageBox.Show(this, "Selecciona una orden.", "Órdenes de compra",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return _orders.Get(id);
    }

    private static Control Inline(string caption, Control editor)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 12, 0) };
        panel.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 6, 6, 0), ForeColor = UiTheme.Text });
        panel.Controls.Add(editor);
        return panel;
    }

    private static Button ActionButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(110, 32),
        Margin = new Padding(6, 0, 0, 0),
        FlatStyle = FlatStyle.System
    };

    private sealed class SupplierFilter
    {
        public SupplierFilter(Supplier? supplier) => Supplier = supplier;

        public Supplier? Supplier { get; }

        public override string ToString() => Supplier?.Name ?? "Todos los proveedores";
    }
}
