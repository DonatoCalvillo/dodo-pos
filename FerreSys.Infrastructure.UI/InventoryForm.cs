using ClosedXML.Excel;
using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public class InventoryForm : Form
{
    private readonly ListInventoryUseCase _listInventoryUseCase;
    private readonly IProductRepository _productRepository;
    private readonly UpdateProductUseCase _updateProductUseCase;
    private readonly AdjustStockUseCase _adjustStockUseCase;
    private readonly DeactivateProductUseCase _deactivateProductUseCase;
    private readonly GetStockMovementsUseCase _getStockMovementsUseCase;

    private TextBox txtSearch = null!;
    private Label lblSummary = null!;
    private DataGridView dgv = null!;
    private Label lblDetail = null!;
    private Button btnExport = null!;
    private Button btnRefresh = null!;
    private Button btnClose = null!;

    private List<Product> _all = [];
    private List<Product> _visible = [];

    public bool CatalogChanged { get; private set; }

    public InventoryForm(
        ListInventoryUseCase listInventoryUseCase,
        IProductRepository productRepository,
        UpdateProductUseCase updateProductUseCase,
        AdjustStockUseCase adjustStockUseCase,
        DeactivateProductUseCase deactivateProductUseCase,
        GetStockMovementsUseCase getStockMovementsUseCase)
    {
        _listInventoryUseCase = listInventoryUseCase;
        _productRepository = productRepository;
        _updateProductUseCase = updateProductUseCase;
        _adjustStockUseCase = adjustStockUseCase;
        _deactivateProductUseCase = deactivateProductUseCase;
        _getStockMovementsUseCase = getStockMovementsUseCase;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Text = "Inventario - Posly";
        Size = new Size(980, 640);
        MinimumSize = new Size(760, 480);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;
        KeyPreview = true;
        KeyDown += InventoryForm_KeyDown;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10, 8, 10, 10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        root.Controls.Add(CreateHeaderSection(), 0, 0);
        root.Controls.Add(CreateGridSection(), 0, 1);
        root.Controls.Add(CreateDetailSection(), 0, 2);
        root.Controls.Add(CreateButtonsSection(), 0, 3);
        Controls.Add(root);
    }

    private Control CreateHeaderSection()
    {
        var gb = UiHelp.CreateGroupBox("Consulta de inventario", SectionHelpText.Inventory);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8, 16, 8, 4),
            BackColor = UiTheme.PanelBg
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblSearch = new Label
        {
            Text = "Buscar:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.Text
        };

        txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Search
        };
        txtSearch.TextChanged += (_, _) => ApplyFilter();
        txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        lblSummary = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.Text,
            Font = UiTheme.UiBold
        };

        layout.Controls.Add(lblSearch, 0, 0);
        layout.Controls.Add(txtSearch, 1, 0);
        layout.Controls.Add(lblSummary, 0, 1);
        layout.SetColumnSpan(lblSummary, 2);
        gb.Controls.Add(layout);
        return gb;
    }

    private Control CreateGridSection()
    {
        var gb = UiHelp.CreateGroupBox("Productos en existencia", SectionHelpText.InventoryList);

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(0, 70, 180),
                ForeColor = Color.White,
                Font = UiTheme.UiBold,
                SelectionBackColor = Color.FromArgb(0, 70, 180)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = UiTheme.Ui,
                SelectionBackColor = Color.FromArgb(210, 225, 245),
                SelectionForeColor = Color.Black
            }
        };

        ProductImages.AddPhotoColumn(dgv);
        dgv.Columns.Add("Barcode", "Código");
        dgv.Columns.Add("Name", "Descripción");
        dgv.Columns.Add("Unit", "Unidad");
        dgv.Columns.Add("Stock", "Stock");
        dgv.Columns.Add("MinStock", "Mínimo");
        dgv.Columns.Add("DesiredStock", "Deseado");
        dgv.Columns.Add("RetailPrice", "Precio");
        dgv.Columns.Add("CostPrice", "Costo");

        dgv.Columns["Barcode"]!.FillWeight = 90;
        dgv.Columns["Name"]!.FillWeight = 180;
        dgv.Columns["Unit"]!.FillWeight = 55;
        dgv.Columns["Stock"]!.FillWeight = 55;
        dgv.Columns["MinStock"]!.FillWeight = 50;
        dgv.Columns["DesiredStock"]!.FillWeight = 50;
        dgv.Columns["RetailPrice"]!.FillWeight = 60;
        dgv.Columns["CostPrice"]!.FillWeight = 60;

        foreach (var colName in new[] { "Stock", "MinStock", "DesiredStock", "RetailPrice", "CostPrice" })
            dgv.Columns[colName]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        dgv.SelectionChanged += (_, _) => ShowSelectedDetail();
        dgv.ContextMenuStrip = CreateProductMenu();
        dgv.CellMouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                return;

            dgv.ClearSelection();
            dgv.Rows[e.RowIndex].Selected = true;
            dgv.CurrentCell = dgv.Rows[e.RowIndex].Cells["Barcode"];
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 20, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        panel.Controls.Add(dgv);
        gb.Controls.Add(panel);
        return gb;
    }

    private Control CreateDetailSection()
    {
        var gb = UiHelp.CreateGroupBox("Ficha del producto", SectionHelpText.InventoryDetail);

        lblDetail = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 16, 8, 4),
            ForeColor = UiTheme.Text,
            Font = UiTheme.Ui
        };

        gb.Controls.Add(lblDetail);
        return gb;
    }

    private Control CreateButtonsSection()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };

        btnClose = CreateButton("Cerrar", false);
        btnClose.Click += (_, _) => Close();

        btnExport = CreateButton("Exportar Excel", true);
        btnExport.Click += (_, _) => ExportExcel();

        btnRefresh = CreateButton("Actualizar", false);
        btnRefresh.Click += (_, _) => LoadData();

        panel.Controls.AddRange([btnClose, btnExport, btnRefresh]);
        return panel;
    }

    private static Button CreateButton(string text, bool primary)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(120, 32),
            Margin = new Padding(6, 0, 0, 0),
            BackColor = primary ? Color.FromArgb(220, 235, 255) : SystemColors.Control,
            FlatStyle = FlatStyle.System
        };
    }

    private ContextMenuStrip CreateProductMenu()
    {
        var menu = new ContextMenuStrip { Font = UiTheme.Ui };
        menu.Items.Add("Editar Datos del Producto", null, (_, _) => EditSelectedProduct());
        menu.Items.Add("Auditar / Ajustar Stock", null, (_, _) => AuditSelectedProduct());
        menu.Items.Add("Ver Kárdex", null, (_, _) => OpenSelectedKardex());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitar del mostrador", null, (_, _) => HideSelectedProduct());
        return menu;
    }

    private Product? SelectedProduct()
    {
        if (dgv.CurrentRow?.Tag is not Product listed)
            return null;

        return _productRepository.GetByBarcode(listed.Barcode);
    }

    private void EditSelectedProduct()
    {
        Product? product = SelectedProduct();
        if (product == null)
            return;

        using var editForm = new AddProductForm(_updateProductUseCase, product);
        if (editForm.ShowDialog(this) != DialogResult.OK)
            return;

        CatalogChanged = true;
        LoadData();
    }

    private void AuditSelectedProduct()
    {
        Product? product = SelectedProduct();
        if (product == null)
            return;

        using var adjustForm = new AdjustStockForm(_adjustStockUseCase, product);
        if (adjustForm.ShowDialog(this) != DialogResult.OK)
            return;

        CatalogChanged = true;
        LoadData();
    }

    private void OpenSelectedKardex()
    {
        Product? product = SelectedProduct();
        if (product == null)
            return;

        using var kardex = new KardexForm(_getStockMovementsUseCase, _productRepository, product);
        kardex.ShowDialog(this);
    }

    private void HideSelectedProduct()
    {
        Product? product = SelectedProduct();
        if (product == null || !product.IsActive)
            return;

        var confirm = MessageBox.Show(
            $"¿Quitar \"{product.Name}\" del mostrador?\n\nDejará de aparecer en búsquedas y en el lector. Las ventas anteriores se conservan.",
            "Quitar del mostrador",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        try
        {
            _deactivateProductUseCase.Execute(product.Id);
            CatalogChanged = true;
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "No se pudo ocultar el producto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void InventoryForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Escape)
            return;

        Close();
        e.Handled = true;
    }

    private void LoadData()
    {
        _all = _listInventoryUseCase.Execute();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string term = txtSearch.Text.Trim();
        _visible = string.IsNullOrEmpty(term)
            ? _all.ToList()
            : _all.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        ProductImages.ClearRows(dgv);
        foreach (var product in _visible)
        {
            int idx = dgv.Rows.Add(
                ProductImages.ForGrid(product.Image),
                product.Barcode,
                product.Name,
                product.Unit.DisplayName(),
                product.Unit.FormatQuantity(product.Stock),
                product.Unit.FormatQuantity(product.MinStock),
                product.Unit.FormatQuantity(product.DesiredStock),
                $"${product.RetailPrice:N2}",
                $"${product.CostPrice:N2}");

            var row = dgv.Rows[idx];
            row.Tag = product;
            row.DefaultCellStyle.BackColor = product.IsOutOfStock
                ? UiTheme.StockDanger
                : product.AlertLevel == StockAlertLevel.Warning
                    ? UiTheme.StockWarn
                    : Color.White;
        }

        int outOfStock = _visible.Count(p => p.IsOutOfStock);
        int belowMin = _visible.Count(p => !p.IsOutOfStock && p.IsBelowMinimum);
        string scope = string.IsNullOrEmpty(term)
            ? $"{_visible.Count} producto(s)"
            : $"{_visible.Count} de {_all.Count} producto(s)";
        lblSummary.Text = $"{scope} · {outOfStock} agotado(s) · {belowMin} bajo mínimo";

        btnExport.Enabled = _visible.Count > 0;
        if (dgv.Rows.Count == 0)
            ShowDetail(null);
    }

    private void ShowSelectedDetail()
    {
        if (dgv.CurrentRow?.Tag is Product product)
            ShowDetail(product);
        else
            ShowDetail(null);
    }

    private void ShowDetail(Product? product)
    {
        if (product == null)
        {
            lblDetail.Text = "Selecciona un producto para ver su ficha. Esta consulta no agrega nada al carrito.";
            return;
        }

        string status = product.IsOutOfStock
            ? "Agotado"
            : product.IsBelowMinimum
                ? "Bajo mínimo"
                : "En existencia";

        lblDetail.Text =
            $"{product.Name}   ·   Código: {product.Barcode}   ·   {product.Unit.DisplayName()}   ·   {status}\n" +
            $"Stock: {product.Unit.FormatQuantity(product.Stock)}   ·   " +
            $"Mínimo: {product.Unit.FormatQuantity(product.MinStock)}   ·   " +
            $"Deseado: {product.Unit.FormatQuantity(product.DesiredStock)}\n" +
            $"Precio de venta: ${product.RetailPrice:N2}   ·   Costo: ${product.CostPrice:N2}";
    }

    private void ExportExcel()
    {
        if (_visible.Count == 0)
            return;

        using var dialog = new SaveFileDialog
        {
            Filter = "Libro de Excel (*.xlsx)|*.xlsx",
            Title = "Exportar inventario",
            FileName = $"inventario-{DateTime.Now:yyyyMMdd-HHmm}.xlsx"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Inventario");
            string[] headers =
            [
                "Barcode", "Name", "Unit", "Stock", "MinStock", "DesiredStock", "CostPrice", "RetailPrice"
            ];

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = sheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
            }

            for (int i = 0; i < _visible.Count; i++)
            {
                var product = _visible[i];
                int row = i + 2;
                sheet.Cell(row, 1).Value = product.Barcode;
                sheet.Cell(row, 2).Value = product.Name;
                sheet.Cell(row, 3).Value = product.Unit.ToString();
                sheet.Cell(row, 4).Value = product.Stock;
                sheet.Cell(row, 5).Value = product.MinStock;
                sheet.Cell(row, 6).Value = product.DesiredStock;
                sheet.Cell(row, 7).Value = product.CostPrice;
                sheet.Cell(row, 8).Value = product.RetailPrice;
            }

            sheet.Columns(4, 6).Style.NumberFormat.Format = "#,##0.000";
            sheet.Columns(7, 8).Style.NumberFormat.Format = "#,##0.00";
            sheet.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);

            MessageBox.Show(this, "Inventario exportado correctamente.", "Inventario",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"No se pudo exportar el inventario.\n\n{ex.Message}", "Inventario",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
