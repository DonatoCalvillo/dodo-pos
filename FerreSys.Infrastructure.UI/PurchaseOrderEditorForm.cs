using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;

namespace FerreSys.Infrastructure.UI;

internal sealed class PurchaseOrderSeedLine
{
    public string Barcode { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
}

internal sealed class PurchaseOrderEditorForm : Form
{
    private readonly ISupplierRepository _suppliers;
    private readonly IProductRepository _products;
    private readonly IPurchaseOrderRepository _orders;
    private readonly GetReorderSuggestionsUseCase _suggestions;
    private readonly SqliteAppSettingsStore _settings;
    private readonly string _actor;
    private bool _readOnly;
    private readonly List<LineRow> _lines = [];
    private PurchaseOrder? _order;

    private readonly ComboBox _supplier = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox _notes = new() { Dock = DockStyle.Fill };
    private readonly TextBox _search = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _alerts = new();
    private readonly DataGridView _results = new();
    private readonly DataGridView _linesGrid = new();
    private readonly Label _total = new() { AutoSize = true, Font = UiTheme.UiBold, ForeColor = UiTheme.Text };
    private bool _dirty;

    public PurchaseOrderEditorForm(
        PurchaseOrder? existing,
        IReadOnlyList<PurchaseOrderSeedLine>? seeds,
        ISupplierRepository suppliers,
        IProductRepository products,
        IPurchaseOrderRepository orders,
        GetReorderSuggestionsUseCase suggestions,
        SqliteAppSettingsStore settings,
        string actor)
    {
        _order = existing;
        _suppliers = suppliers;
        _products = products;
        _orders = orders;
        _suggestions = suggestions;
        _settings = settings;
        _actor = actor;
        _readOnly = existing != null && !existing.IsDraft;

        Text = existing == null ? "Nueva orden de compra" : existing.Number;
        Size = new Size(980, 700);
        MinimumSize = new Size(860, 560);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        BuildLayout();
        LoadSuppliers();
        LoadAlerts();
        if (existing != null)
            LoadExisting(existing);
        else if (seeds != null)
            LoadSeeds(seeds);

        ApplyMode();
        RefreshLines();
        _notes.TextChanged += (_, _) => Touch();
        _supplier.SelectedIndexChanged += (_, _) => Touch();
    }

    private void Touch()
    {
        if (_readOnly)
            return;
        _dirty = true;
        ApplyMode();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        header.Controls.Add(Labeled("Proveedor", _supplier), 0, 0);
        header.Controls.Add(Labeled("Notas", _notes), 1, 0);

        var picks = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        picks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        picks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        picks.Controls.Add(BuildAlerts(), 0, 0);
        picks.Controls.Add(BuildSearch(), 1, 0);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(picks, 0, 1);
        root.Controls.Add(BuildLines(), 0, 2);
        root.Controls.Add(BuildButtons(), 0, 3);
        Controls.Add(root);
    }

    private Control BuildAlerts()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        SuppliersForm.StyleGrid(_alerts);
        _alerts.ReadOnly = false;
        _alerts.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Pick", HeaderText = "", FillWeight = 24 });
        _alerts.Columns.Add("Name", "Recomendados por alerta");
        _alerts.Columns.Add("Qty", "A pedir");
        _alerts.Columns["Name"]!.ReadOnly = true;
        _alerts.Columns["Qty"]!.ReadOnly = true;
        _alerts.Columns["Name"]!.FillWeight = 180;
        _alerts.Columns["Qty"]!.FillWeight = 50;
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var selected = SmallButton("Agregar seleccionados");
        var all = SmallButton("Agregar todos");
        selected.Click += (_, _) => AddSelectedAlerts();
        all.Click += (_, _) => AddAllAlerts();
        actions.Controls.Add(selected);
        actions.Controls.Add(all);
        panel.Controls.Add(_alerts, 0, 0);
        panel.Controls.Add(actions, 0, 1);
        return panel;
    }

    private Control BuildSearch()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        var searchRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        var find = SmallButton("Buscar");
        find.Dock = DockStyle.Fill;
        find.Click += (_, _) => SearchProducts();
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SearchProducts();
            }
        };
        searchRow.Controls.Add(_search, 0, 0);
        searchRow.Controls.Add(find, 1, 0);
        SuppliersForm.StyleGrid(_results);
        _results.Columns.Add("Name", "Catálogo");
        _results.Columns.Add("Stock", "Stock");
        var add = SmallButton("Agregar producto");
        add.Click += (_, _) => AddSelectedProduct();
        panel.Controls.Add(searchRow, 0, 0);
        panel.Controls.Add(_results, 0, 1);
        panel.Controls.Add(add, 0, 2);
        return panel;
    }

    private Control BuildLines()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        SuppliersForm.StyleGrid(_linesGrid);
        _linesGrid.ReadOnly = false;
        _linesGrid.Columns.Add("Barcode", "Código");
        _linesGrid.Columns.Add("Name", "Producto");
        _linesGrid.Columns.Add("Unit", "Unidad");
        _linesGrid.Columns.Add("Qty", "Cantidad");
        _linesGrid.Columns.Add("Received", "Recibido");
        _linesGrid.Columns.Add("Cost", "Costo");
        _linesGrid.Columns.Add("Amount", "Importe");
        var remove = new DataGridViewButtonColumn { Name = "Remove", HeaderText = "", Text = "Quitar", UseColumnTextForButtonValue = true, FillWeight = 45 };
        _linesGrid.Columns.Add(remove);
        foreach (string name in new[] { "Barcode", "Name", "Unit", "Received", "Amount" })
            _linesGrid.Columns[name]!.ReadOnly = true;
        _linesGrid.Columns["Qty"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _linesGrid.Columns["Cost"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _linesGrid.Columns["Amount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _linesGrid.CellValidating += Lines_CellValidating;
        _linesGrid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _linesGrid.Columns[e.ColumnIndex].Name == "Remove")
                RemoveLine(e.RowIndex);
        };
        panel.Controls.Add(_linesGrid, 0, 0);
        panel.Controls.Add(_total, 0, 1);
        return panel;
    }

    private Control BuildButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var close = SmallButton("Cerrar");
        close.Click += (_, _) => Close();
        var response = SmallButton("Registrar respuesta");
        response.Name = "response";
        response.Click += (_, _) => Decide();
        var email = SmallButton("Enviar por correo");
        email.Name = "email";
        email.Click += (_, _) => Email();
        var print = SmallButton("Imprimir / PDF…");
        print.Name = "print";
        print.Click += (_, _) => Print();
        var save = SmallButton("Guardar");
        save.Name = "save";
        save.Click += (_, _) => SaveOrder();
        panel.Controls.AddRange([close, response, email, print, save]);
        return panel;
    }

    private void ApplyMode()
    {
        _supplier.Enabled = !_readOnly;
        _notes.ReadOnly = _readOnly;
        _search.Enabled = !_readOnly;
        _alerts.Enabled = !_readOnly;
        _results.Enabled = !_readOnly;
        _linesGrid.Columns["Qty"]!.ReadOnly = _readOnly;
        _linesGrid.Columns["Cost"]!.ReadOnly = _readOnly;
        _linesGrid.Columns["Received"]!.Visible = _readOnly;
        _linesGrid.Columns["Remove"]!.Visible = !_readOnly;
        foreach (Control control in Controls)
            ToggleNamed(control);
    }

    private void ToggleNamed(Control parent)
    {
        if (parent.Name == "save" || parent.Name == "response")
            parent.Visible = !_readOnly;
        if (parent.Name is "print" or "email")
            parent.Enabled = _order != null && !_dirty;
        if (parent.Name == "response")
            parent.Enabled = _order != null && !_dirty;
        foreach (Control child in parent.Controls)
            ToggleNamed(child);
    }

    private void LoadSuppliers()
    {
        _supplier.Items.Clear();
        foreach (Supplier supplier in _suppliers.List().Where(item => item.IsActive || item.Id == _order?.SupplierId))
            _supplier.Items.Add(supplier);
        _supplier.DisplayMember = nameof(Supplier.Name);
        if (_order != null)
        {
            for (int i = 0; i < _supplier.Items.Count; i++)
            {
                if (_supplier.Items[i] is Supplier supplier && supplier.Id == _order.SupplierId)
                    _supplier.SelectedIndex = i;
            }
        }
        else if (_supplier.Items.Count > 0)
            _supplier.SelectedIndex = 0;
    }

    private void LoadAlerts()
    {
        _alerts.Rows.Clear();
        foreach (ReorderSuggestionLine line in _suggestions.Execute().Lines)
        {
            decimal qty = line.SuggestedOrderQty > 0 ? line.SuggestedOrderQty : 1;
            int index = _alerts.Rows.Add(false, line.Name, line.Unit.FormatQuantity(qty));
            _alerts.Rows[index].Tag = line;
        }
    }

    private void LoadExisting(PurchaseOrder order)
    {
        _notes.Text = order.Notes;
        foreach (PurchaseOrderLine line in order.Lines)
        {
            _lines.Add(new LineRow
            {
                ProductId = line.ProductId,
                Barcode = line.Barcode,
                Name = line.Name,
                Unit = line.Unit,
                Qty = line.OrderedQty,
                Received = line.ReceivedQty,
                Cost = line.UnitCost
            });
        }
    }

    private void LoadSeeds(IReadOnlyList<PurchaseOrderSeedLine> seeds)
    {
        foreach (PurchaseOrderSeedLine seed in seeds)
        {
            Product? product = _products.GetByBarcode(seed.Barcode);
            if (product == null || !product.IsActive)
                continue;
            AddProduct(product, seed.Quantity > 0 ? seed.Quantity : 1);
        }
    }

    private void SearchProducts()
    {
        string term = _search.Text.Trim();
        _results.Rows.Clear();
        if (term.Length == 0)
            return;

        var found = new List<Product>();
        Product? exact = _products.GetByBarcode(term);
        if (exact is { IsActive: true })
            found.Add(exact);
        foreach (Product product in _products.SearchByName(term))
        {
            if (found.All(item => item.Id != product.Id))
                found.Add(product);
            if (found.Count >= 30)
                break;
        }

        foreach (Product product in found)
        {
            int index = _results.Rows.Add(product.Name, product.Unit.FormatQuantity(product.Stock));
            _results.Rows[index].Tag = product;
        }
    }

    private void AddSelectedAlerts()
    {
        foreach (DataGridViewRow row in _alerts.Rows)
        {
            if (row.Cells["Pick"].Value is true or CheckState.Checked && row.Tag is ReorderSuggestionLine line)
                AddSuggestion(line);
        }
    }

    private void AddAllAlerts()
    {
        foreach (DataGridViewRow row in _alerts.Rows)
        {
            if (row.Tag is ReorderSuggestionLine line)
                AddSuggestion(line);
        }
    }

    private void AddSuggestion(ReorderSuggestionLine line)
    {
        Product? product = _products.GetByBarcode(line.Barcode);
        if (product == null || !product.IsActive)
            return;
        decimal qty = line.SuggestedOrderQty > 0 ? line.SuggestedOrderQty : 1;
        AddProduct(product, qty);
    }

    private void AddSelectedProduct()
    {
        if (_results.CurrentRow?.Tag is not Product product)
        {
            MessageBox.Show(this, "Selecciona un producto del catálogo.", "Orden de compra",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        AddProduct(product, 1);
    }

    private void AddProduct(Product product, decimal quantity)
    {
        if (_readOnly)
            return;
        if (_lines.Any(line => line.ProductId == product.Id))
            return;

        _lines.Add(new LineRow
        {
            ProductId = product.Id,
            Barcode = product.Barcode,
            Name = product.Name,
            Unit = product.Unit,
            Qty = quantity,
            Cost = product.CostPrice
        });
        _dirty = true;
        RefreshLines();
    }

    private void RemoveLine(int index)
    {
        if (_readOnly || index < 0 || index >= _lines.Count)
            return;
        _lines.RemoveAt(index);
        _dirty = true;
        RefreshLines();
    }

    private void Lines_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (_readOnly || e.RowIndex < 0 || e.RowIndex >= _lines.Count)
            return;

        string column = _linesGrid.Columns[e.ColumnIndex].Name;
        LineRow line = _lines[e.RowIndex];
        string text = Convert.ToString(e.FormattedValue) ?? string.Empty;
        if (column == "Qty")
        {
            if (!TryParse(text, out decimal qty))
            {
                e.Cancel = true;
                Warn($"La cantidad de '{line.Name}' no es válida.");
                return;
            }

            try
            {
                Product.EnsureQuantity(qty, line.Unit, allowZero: false);
            }
            catch (ArgumentException ex)
            {
                e.Cancel = true;
                Warn(ex.Message);
                return;
            }

            line.Qty = qty;
            _dirty = true;
        }
        else if (column == "Cost")
        {
            if (!TryParse(text, out decimal cost) || cost < 0)
            {
                e.Cancel = true;
                Warn($"El costo de '{line.Name}' no es válido.");
                return;
            }

            line.Cost = decimal.Round(cost, 2, MidpointRounding.AwayFromZero);
            _dirty = true;
        }
        else
            return;

        _linesGrid.Rows[e.RowIndex].Cells["Amount"].Value = line.Amount.ToString("C2");
        _total.Text = $"Total { _lines.Sum(item => item.Amount):C2}";
    }

    private void RefreshLines()
    {
        _linesGrid.Rows.Clear();
        foreach (LineRow line in _lines)
        {
            _linesGrid.Rows.Add(
                line.Barcode,
                line.Name,
                line.Unit.DisplayName(),
                FormatQty(line.Unit, line.Qty),
                FormatQty(line.Unit, line.Received),
                line.Cost.ToString("0.00"),
                line.Amount.ToString("C2"),
                "Quitar");
        }

        _total.Text = $"Total {_lines.Sum(item => item.Amount):C2}";
        ApplyMode();
    }

    private void SaveOrder()
    {
        if (_supplier.SelectedItem is not Supplier supplier)
        {
            Warn("Selecciona un proveedor.");
            return;
        }

        try
        {
            _order = new SavePurchaseOrderUseCase(_orders, _suppliers, _products).Execute(new PurchaseOrderDraft
            {
                Id = _order?.Id,
                SupplierId = supplier.Id,
                Notes = _notes.Text,
                CreatedBy = _actor,
                Lines = _lines.Select(line => new PurchaseOrderLineDraft
                {
                    ProductId = line.ProductId,
                    OrderedQty = line.Qty,
                    UnitCost = line.Cost
                }).ToList()
            });
            Text = _order.Number;
            _dirty = false;
            ApplyMode();
            MessageBox.Show(this, $"Orden {_order.Number} guardada.", "Orden de compra",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Warn(ex.Message);
        }
    }

    private void Print()
    {
        if (_order == null)
            return;
        PurchaseOrder? fresh = _orders.Get(_order.Id) ?? _order;
        _order = fresh;
        PurchaseOrderDocuments.Preview(this, fresh, _suppliers.Get(fresh.SupplierId), _settings);
    }

    private void Email()
    {
        if (_order == null)
            return;
        PurchaseOrder? fresh = _orders.Get(_order.Id) ?? _order;
        try
        {
            PurchaseOrderDocuments.SendEmail(
                this,
                fresh,
                _suppliers.Get(fresh.SupplierId),
                _settings,
                new MarkPurchaseOrderSentUseCase(_orders));
            _order = _orders.Get(fresh.Id) ?? fresh;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Warn(ex.Message);
        }
    }

    private void Decide()
    {
        if (_order == null || _dirty)
        {
            Warn("Guarda la orden antes de registrar la respuesta.");
            return;
        }

        PurchaseOrder fresh = _orders.Get(_order.Id) ?? _order;
        using var dialog = new PurchaseOrderDecisionForm(fresh, new DecidePurchaseOrderUseCase(_orders), _actor);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result == null)
            return;

        _order = dialog.Result;
        _readOnly = !_order.IsDraft;
        _lines.Clear();
        LoadExisting(_order);
        RefreshLines();
    }

    private void Warn(string message) =>
        MessageBox.Show(this, message, "Orden de compra", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private static Control Labeled(string caption, Control editor)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(0, 0, 8, 0) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, ForeColor = UiTheme.Text }, 0, 0);
        panel.Controls.Add(editor, 0, 1);
        return panel;
    }

    private static Button SmallButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(110, 30),
        Margin = new Padding(6, 0, 0, 0),
        FlatStyle = FlatStyle.System
    };

    private static bool TryParse(string text, out decimal value) =>
        decimal.TryParse(text, out value)
        || decimal.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out value);

    private static string FormatQty(UnitOfMeasure unit, decimal quantity) =>
        unit.AllowsDecimals() ? quantity.ToString("0.###") : decimal.Truncate(quantity).ToString("0");

    private sealed class LineRow
    {
        public Guid ProductId { get; init; }
        public string Barcode { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public UnitOfMeasure Unit { get; init; }
        public decimal Qty { get; set; }
        public decimal Received { get; set; }
        public decimal Cost { get; set; }
        public decimal Amount => decimal.Round(Qty * Cost, 2, MidpointRounding.AwayFromZero);
    }
}
