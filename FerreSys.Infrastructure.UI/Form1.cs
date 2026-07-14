using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;

namespace FerreSys.Infrastructure.UI;

public partial class Form1 : Form
{
    private TextBox txtBuscador;
    private ToolStripButton btnOpenAddProduct;
    private DataGridView dgvSearch;
    private DataGridView dgvCart;
    private Label lblTotal;
    private Label lblCartSummary;
    private ToolStripStatusLabel lblStatus;
    private Button btnCheckout;
    private Button btnPlus;
    private Button btnMinus;
    private Button btnClearCart;
    private ToolStripButton btnViewSales;
    private ToolStripButton btnImportExcel;

    private readonly SqliteDbContext _dbContext;
    private readonly SqliteProductRepository _productRepository;
    private readonly LocalSubscriptionService _subscriptionService;
    private readonly AddProductUseCase _addProductUseCase;
    private readonly SearchProductsUseCase _searchProductsUseCase;
    private readonly ProcessSaleUseCase _processSaleUseCase;
    private readonly UpdateProductUseCase _updateProductUseCase;
    private ContextMenuStrip cmsSearchMenu;
    private readonly ImportProductsUseCase _importProductsUseCase;
    private readonly AdjustStockUseCase _adjustStockUseCase;
    private decimal _saleTotal = 0;

    public Form1()
    {
        _dbContext = new SqliteDbContext();
        _productRepository = new SqliteProductRepository(_dbContext);
        _subscriptionService = new LocalSubscriptionService();
        _addProductUseCase = new AddProductUseCase(_productRepository, _subscriptionService);
        _searchProductsUseCase = new SearchProductsUseCase(_productRepository);
        _processSaleUseCase = new ProcessSaleUseCase(_productRepository);

        // --- CONFIGURACIÓN DE TERMINAL PROFESIONAL (FULL SCREEN) ---
        this.Text = "FerreSys MVP - Terminal de Mostrador";
        this.StartPosition = FormStartPosition.CenterScreen;

        // Eliminamos los bordes de la ventana (maximizar, minimizar, cerrar tradicionales)
        this.FormBorderStyle = FormBorderStyle.None;
        // Forzamos a que se maximice ocupando absolutamente todo el monitor
        this.WindowState = FormWindowState.Maximized;

        // Mantenemos activo el KeyPreview para que escuche el teclado de forma global
        this.KeyPreview = true;
        this.KeyDown += Form1_KeyDown;

        InicializarComponentes();
    }

    private void InicializarComponentes()
    {
        var menu = CreateMenuStrip();
        var toolBar = CreateToolbar();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 6, 8, 4),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

        root.Controls.Add(CreateTopSection(), 0, 0);
        root.Controls.Add(CreateMiddleSection(), 0, 1);
        root.Controls.Add(CreateBottomSection(), 0, 2);

        var statusBar = CreateStatusBar();

        this.Controls.Add(root);
        this.Controls.Add(toolBar);
        this.Controls.Add(menu);
        this.Controls.Add(statusBar);

        SetupSearchContextMenu();
        CalculateTotal();
        txtBuscador.Focus();
    }

    private MenuStrip CreateMenuStrip()
    {
        var menu = new MenuStrip
        {
            Font = UiTheme.Ui,
            BackColor = UiTheme.PanelBg
        };

        var mArchivo = new ToolStripMenuItem("&Archivo");
        var mVentas = new ToolStripMenuItem("Ver &Ventas...", null, (_, _) => OpenSales());
        var mImportar = new ToolStripMenuItem("&Importar productos (CSV)...", null, BtnImportExcel_Click);
        var mSalir = new ToolStripMenuItem("&Salir", null, (_, _) => Close());
        mArchivo.DropDownItems.AddRange([mVentas, mImportar, new ToolStripSeparator(), mSalir]);

        var mProductos = new ToolStripMenuItem("&Productos");
        mProductos.DropDownItems.Add(new ToolStripMenuItem("&Registrar producto...", null, BtnOpenAddProduct_Click));

        var mVenta = new ToolStripMenuItem("&Venta");
        mVenta.DropDownItems.Add(new ToolStripMenuItem("&Cobrar (F5)", null, BtnCheckout_Click));
        mVenta.DropDownItems.Add(new ToolStripMenuItem("&Vaciar carrito", null, BtnClearCart_Click));

        menu.Items.AddRange([mArchivo, mProductos, mVenta]);
        return menu;
    }

    private ToolStrip CreateToolbar()
    {
        var strip = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(24, 24),
            Font = UiTheme.Toolbar,
            BackColor = UiTheme.PanelBg,
            Padding = new Padding(4, 2, 4, 2),
            Height = 56,
            RenderMode = ToolStripRenderMode.System
        };

        btnViewSales = CreateToolItem("Ver Ventas", UiIcons.Sales());
        btnViewSales.Click += (_, _) => OpenSales();

        btnImportExcel = CreateToolItem("Importar CSV", UiIcons.Import());
        btnImportExcel.Click += BtnImportExcel_Click;

        btnOpenAddProduct = CreateToolItem("Registrar", UiIcons.AddProduct());
        btnOpenAddProduct.Click += BtnOpenAddProduct_Click;

        var brand = new ToolStripLabel("  FerreSys — Punto de Venta")
        {
            Font = new Font("Tahoma", 11f, FontStyle.Bold),
            ForeColor = UiTheme.GroupTitle
        };

        strip.Items.AddRange([
            btnViewSales,
            btnImportExcel,
            btnOpenAddProduct,
            new ToolStripSeparator(),
            brand
        ]);

        return strip;
    }

    private static ToolStripButton CreateToolItem(string text, Image icon)
    {
        return new ToolStripButton
        {
            Text = text,
            Image = icon,
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            TextImageRelation = TextImageRelation.ImageAboveText,
            AutoSize = true,
            Padding = new Padding(8, 2, 8, 2)
        };
    }

    private TableLayoutPanel CreateTopSection()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 6),
            BackColor = UiTheme.WindowBg
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        var gbSearch = CreateGroupBox("Búsqueda de producto");
        gbSearch.Margin = new Padding(0, 0, 6, 0);

        txtBuscador = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Search,
            ForeColor = UiTheme.Text,
            PlaceholderText = "Busque o escanee producto..."
        };
        txtBuscador.TextChanged += TxtBuscador_TextChanged;
        txtBuscador.KeyDown += TxtBuscador_KeyDown;

        var searchPad = CreateContentPanel();
        searchPad.Controls.Add(txtBuscador);
        gbSearch.Controls.Add(searchPad);

        var gbHints = CreateGroupBox("Indicaciones");
        gbHints.Margin = new Padding(6, 0, 0, 0);

        var lblHints = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            BackColor = UiTheme.PanelBg,
            ForeColor = UiTheme.Text,
            Font = UiTheme.Ui,
            TextAlign = ContentAlignment.TopLeft,
            Text = "• Doble clic: añadir al carrito\r\n" +
                   "• Clic derecho: editar / auditar stock\r\n" +
                   "• Enter: añadir si hay 1 resultado\r\n" +
                   "• F5: cobrar venta",
            Padding = new Padding(6, 4, 4, 4)
        };

        var hintsPad = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 8, 4, 4),
            BackColor = UiTheme.PanelBg
        };
        hintsPad.Controls.Add(lblHints);
        gbHints.Controls.Add(hintsPad);

        row.Controls.Add(gbSearch, 0, 0);
        row.Controls.Add(gbHints, 1, 0);
        return row;
    }

    private TableLayoutPanel CreateMiddleSection()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 6),
            BackColor = UiTheme.WindowBg
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));

        var gbResults = CreateGroupBox("Resultados del catálogo");
        gbResults.Margin = new Padding(0, 0, 6, 0);

        dgvSearch = CreateGrid();
        dgvSearch.Columns.Add("Barcode", "Código");
        dgvSearch.Columns.Add("Name", "Descripción");
        dgvSearch.Columns.Add("Price", "Precio");
        dgvSearch.Columns.Add("Stock", "Stock");
        dgvSearch.DoubleClick += DgvSearch_DoubleClick;

        var resultsPad = CreateContentPanel();
        resultsPad.Controls.Add(dgvSearch);
        gbResults.Controls.Add(resultsPad);

        var gbSummary = CreateGroupBox("Resumen de venta");
        gbSummary.Margin = new Padding(6, 0, 0, 0);

        var summaryStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 14, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        summaryStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        summaryStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        summaryStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        lblCartSummary = new Label
        {
            Text = "Artículos: 0\nLíneas: 0",
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            BackColor = UiTheme.PanelBg,
            Margin = new Padding(0)
        };

        lblTotal = new Label
        {
            Text = "TOTAL\n$0.00",
            Dock = DockStyle.Fill,
            Font = UiTheme.TotalFont,
            ForeColor = UiTheme.Total,
            BackColor = UiTheme.PanelBg,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0)
        };

        summaryStack.Controls.Add(lblCartSummary, 0, 0);
        summaryStack.Controls.Add(lblTotal, 0, 1);
        summaryStack.Controls.Add(new Label
        {
            Text = "Atajo: F5",
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            ForeColor = Color.DimGray,
            BackColor = UiTheme.PanelBg,
            TextAlign = ContentAlignment.BottomCenter
        }, 0, 2);

        gbSummary.Controls.Add(summaryStack);

        row.Controls.Add(gbResults, 0, 0);
        row.Controls.Add(gbSummary, 1, 0);
        return row;
    }

    private TableLayoutPanel CreateBottomSection()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = UiTheme.WindowBg
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));

        var gbCart = CreateGroupBox("Carrito de compra actual");
        gbCart.Margin = new Padding(0, 0, 6, 0);

        dgvCart = CreateGrid();
        dgvCart.Columns.Add("Barcode", "Código");
        dgvCart.Columns.Add("Name", "Producto");
        dgvCart.Columns.Add("Price", "Precio");
        dgvCart.Columns.Add("Qty", "Cant.");
        dgvCart.Columns.Add("Subtotal", "Subtotal");

        var cartPad = CreateContentPanel();
        cartPad.Controls.Add(dgvCart);
        gbCart.Controls.Add(cartPad);

        var gbActions = CreateGroupBox("Acciones");
        gbActions.Margin = new Padding(6, 0, 0, 0);

        var actionsStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(4, 12, 4, 4),
            BackColor = UiTheme.PanelBg
        };
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        btnPlus = CreateActionButton("Añadir 1", UiIcons.Plus());
        btnPlus.Click += BtnPlus_Click;

        btnMinus = CreateActionButton("Quitar 1", UiIcons.Minus());
        btnMinus.Click += BtnMinus_Click;

        btnClearCart = CreateActionButton("Vaciar carrito", UiIcons.Clear());
        btnClearCart.Click += BtnClearCart_Click;

        btnCheckout = CreateActionButton("Cobrar venta (F5)", UiIcons.Checkout());
        btnCheckout.Font = UiTheme.UiBold;
        btnCheckout.Click += BtnCheckout_Click;

        actionsStack.Controls.Add(btnPlus, 0, 0);
        actionsStack.Controls.Add(btnMinus, 0, 1);
        actionsStack.Controls.Add(btnClearCart, 0, 2);
        actionsStack.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.PanelBg }, 0, 3);
        actionsStack.Controls.Add(btnCheckout, 0, 4);

        gbActions.Controls.Add(actionsStack);

        row.Controls.Add(gbCart, 0, 0);
        row.Controls.Add(gbActions, 1, 0);
        return row;
    }

    private StatusStrip CreateStatusBar()
    {
        var status = new StatusStrip
        {
            BackColor = SystemColors.Control,
            Font = UiTheme.Ui,
            SizingGrip = true
        };

        lblStatus = new ToolStripStatusLabel("Listo")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var connected = new ToolStripStatusLabel(" Conectado")
        {
            Image = UiIcons.StatusDot(UiTheme.StatusOk),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            BorderStyle = Border3DStyle.Etched
        };

        status.Items.Add(lblStatus);
        status.Items.Add(connected);
        status.Items.Add(new ToolStripStatusLabel("Mostrador") { BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched });
        status.Items.Add(new ToolStripStatusLabel("FerreSys MVP") { BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched });

        return status;
    }

    private static GroupBox CreateGroupBox(string title)
    {
        return new GroupBox
        {
            Text = " " + title + " ",
            Dock = DockStyle.Fill,
            Font = UiTheme.GroupFont,
            ForeColor = UiTheme.GroupTitle,
            BackColor = UiTheme.PanelBg,
            Padding = new Padding(8)
        };
    }

    private static Panel CreateContentPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 12, 4, 4),
            BackColor = UiTheme.PanelBg,
            ForeColor = UiTheme.Text
        };
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(225, 225, 225),
                ForeColor = UiTheme.Text,
                Font = UiTheme.UiBold,
                SelectionBackColor = Color.FromArgb(225, 225, 225),
                SelectionForeColor = UiTheme.Text
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(51, 153, 255),
                SelectionForeColor = Color.White
            },
            GridColor = Color.FromArgb(200, 200, 200)
        };
        return grid;
    }

    private static Button CreateActionButton(string text, Image icon)
    {
        return new Button
        {
            Text = "  " + text,
            Image = icon,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(8, 0, 8, 0)
        };
    }

    private void OpenSales()
    {
        using var f = new SalesHistoryForm(_productRepository);
        f.ShowDialog();
    }

    private void SetupSearchContextMenu()
    {
        cmsSearchMenu = new ContextMenuStrip { Font = UiTheme.Ui };

        var menuEdit = new ToolStripMenuItem { Text = "Editar Datos del Producto" };
        menuEdit.Click += MenuEdit_Click;

        var menuAudit = new ToolStripMenuItem { Text = "Auditar / Ajustar Stock" };
        menuAudit.Click += MenuAudit_Click;

        cmsSearchMenu.Items.Add(menuEdit);
        cmsSearchMenu.Items.Add(menuAudit);
        dgvSearch.ContextMenuStrip = cmsSearchMenu;

        dgvSearch.CellMouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvSearch.ClearSelection();
                dgvSearch.Rows[e.RowIndex].Selected = true;
                dgvSearch.CurrentCell = dgvSearch.Rows[e.RowIndex].Cells[0];
            }
        };
    }

    private void SetStatus(string message) => lblStatus.Text = message;

    private void TxtBuscador_TextChanged(object? sender, EventArgs e)
    {
        string term = txtBuscador.Text.Trim();
        dgvSearch.Rows.Clear();

        if (term.Length > 0)
        {
            List<Product> matches = _searchProductsUseCase.Execute(term);
            foreach (var p in matches)
            {
                dgvSearch.Rows.Add(p.Barcode, p.Name, p.RetailPrice, p.Stock);
            }
            SetStatus($"Búsqueda: {matches.Count} resultado(s) para \"{term}\"");
        }
        else
        {
            SetStatus("Listo");
        }
    }

    private void TxtBuscador_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && dgvSearch.Rows.Count == 1)
        {
            AddToCart(dgvSearch.Rows[0]);
            e.SuppressKeyPress = true;
        }
    }

    private void DgvSearch_DoubleClick(object? sender, EventArgs e)
    {
        if (dgvSearch.CurrentRow != null)
        {
            AddToCart(dgvSearch.CurrentRow);
        }
    }

    private void AddToCart(DataGridViewRow row)
    {
        string barcode = row.Cells["Barcode"].Value.ToString()!;
        string name = row.Cells["Name"].Value.ToString()!;
        decimal price = Convert.ToDecimal(row.Cells["Price"].Value);

        foreach (DataGridViewRow cartRow in dgvCart.Rows)
        {
            if (cartRow.Cells[0].Value.ToString() == barcode)
            {
                int currentQty = Convert.ToInt32(cartRow.Cells[3].Value);
                cartRow.Cells[3].Value = currentQty + 1;
                cartRow.Cells[4].Value = (currentQty + 1) * price;
                CalculateTotal();
                SetStatus($"Actualizado en carrito: {name}");
                return;
            }
        }

        dgvCart.Rows.Add(barcode, name, price, 1, price);
        CalculateTotal();
        SetStatus($"Añadido al carrito: {name}");
    }

    private void CalculateTotal()
    {
        _saleTotal = 0;
        int itemCount = 0;
        foreach (DataGridViewRow row in dgvCart.Rows)
        {
            _saleTotal += Convert.ToDecimal(row.Cells["Subtotal"].Value);
            itemCount += Convert.ToInt32(row.Cells["Qty"].Value);
        }
        lblTotal.Text = $"TOTAL\n${_saleTotal:N2}";
        lblCartSummary.Text = $"Artículos: {itemCount}\nLíneas: {dgvCart.Rows.Count}";
    }

    private void BtnCheckout_Click(object? sender, EventArgs e)
    {
        // Modifica la sección final del bloque try dentro de BtnCheckout_Click:
        try
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("El carrito está vacío.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var items = new List<SaleItemDto>();
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                items.Add(new SaleItemDto
                {
                    Barcode = row.Cells[0].Value.ToString()!,
                    Quantity = Convert.ToInt32(row.Cells[3].Value)
                });
            }

            // 1. Para poder simular el ticket, necesitamos construir el objeto Sale que usará el formateador
            var saleItemsForTicket = new List<SaleItem>();
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                string barcode = row.Cells[0].Value.ToString()!;
                var p = _productRepository.GetByBarcode(barcode)!;
                int qty = Convert.ToInt32(row.Cells[3].Value);
                saleItemsForTicket.Add(new SaleItem(p.Id, p.Barcode, p.Name, qty, p.RetailPrice));
            }
            var simulatedSale = new Sale(Guid.NewGuid(), DateTime.Now, saleItemsForTicket);

            // 2. Invocar el caso de uso real que descuenta stock e inserta en base de datos
            _processSaleUseCase.Execute(items);

            // 3. GENERACIÓN DEL TICKET: Llamamos a nuestro formateador de texto plano
            string ticketText = TicketFormatter.GenerateTicketText(simulatedSale);

            // 4. VISTA PREVIA: Mostramos cómo se vería el ticket físico en un cuadro de diálogo limpio
            MessageBox.Show(
                ticketText,
                "Vista Previa del Ticket Impreso (58mm)",
                MessageBoxButtons.OK,
                MessageBoxIcon.None // Sin icono para que el texto fije sus espacios Courier de forma limpia
            );

            // Limpiar Carrito y resetear interfaz
            dgvCart.Rows.Clear();
            txtBuscador.Clear();
            dgvSearch.Rows.Clear();
            CalculateTotal();
            txtBuscador.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error al Cobrar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnOpenAddProduct_Click(object? sender, EventArgs e)
    {
        using var addProductForm = new AddProductForm(_addProductUseCase);
        if (addProductForm.ShowDialog() == DialogResult.OK)
        {
            txtBuscador.Clear();
            SetStatus("Producto registrado");
        }
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        // ATAJO 1: F5 para Cobrar la venta actual
        if (e.KeyCode == Keys.F5)
        {
            btnCheckout.PerformClick();
            e.Handled = true;
        }

        // ATAJO 2: Esc para salir del punto de venta de forma segura
        if (e.KeyCode == Keys.Escape)
        {
            var confirmExit = MessageBox.Show(
                "¿Seguro que deseas cerrar el punto de venta FerreSys?",
                "Salir del Sistema",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmExit == DialogResult.Yes)
            {
                // CORRECCIÓN AQUÍ: Agregamos el prefijo completo del sistema de Windows Forms
                System.Windows.Forms.Application.Exit();
            }

            e.Handled = true;
        }
    }
    private void BtnPlus_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow != null)
        {
            var row = dgvCart.CurrentRow;
            decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
            int currentQty = Convert.ToInt32(row.Cells["Qty"].Value);

            int newQty = currentQty + 1;
            row.Cells["Qty"].Value = newQty;
            row.Cells["Subtotal"].Value = newQty * price;

            CalculateTotal();
        }
    }

    private void BtnMinus_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow != null)
        {
            var row = dgvCart.CurrentRow;
            decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
            int currentQty = Convert.ToInt32(row.Cells["Qty"].Value);

            int newQty = currentQty - 1;

            if (newQty <= 0)
            {
                dgvCart.Rows.Remove(row);
            }
            else
            {
                row.Cells["Qty"].Value = newQty;
                row.Cells["Subtotal"].Value = newQty * price;
            }

            CalculateTotal();
        }
    }

    private void BtnClearCart_Click(object? sender, EventArgs e)
    {
        if (dgvCart.Rows.Count > 0)
        {
            var result = MessageBox.Show("¿Seguro que deseas vaciar por completo el carrito de compras?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                dgvCart.Rows.Clear();
                CalculateTotal();
                txtBuscador.Clear();
                SetStatus("Carrito vaciado");
                txtBuscador.Focus();
            }
        }
    }

    private void MenuEdit_Click(object? sender, EventArgs e)
    {
        if (dgvSearch.CurrentRow != null)
        {
            string barcode = dgvSearch.CurrentRow.Cells["Barcode"].Value.ToString()!;
            var productToEdit = _productRepository.GetByBarcode(barcode);

            if (productToEdit != null)
            {
                using var editForm = new AddProductForm(_updateProductUseCase, productToEdit);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    TxtBuscador_TextChanged(this, EventArgs.Empty);
                    SetStatus("Producto actualizado");
                }
            }
        }
    }

    private void BtnImportExcel_Click(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog { Filter = "Archivos CSV (*.csv)|*.csv", Title = "Seleccionar inventario en formato CSV" };
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                List<Product> validatedProducts = _importProductsUseCase.Execute(openFileDialog.FileName);
                _productRepository.SaveBulk(validatedProducts);

                MessageBox.Show(
                    $"¡Importación exitosa!\n\nSe han registrado {validatedProducts.Count} productos correctamente en la base de datos.",
                    "Importador de Inventario",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                TxtBuscador_TextChanged(this, EventArgs.Empty);
                SetStatus($"Importados {validatedProducts.Count} productos");
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message, "Estructura de Archivo Inválida", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Error en Contenido de Datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado al procesar el archivo:\n{ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void MenuAudit_Click(object? sender, EventArgs e)
    {
        if (dgvSearch.CurrentRow != null)
        {
            string barcode = dgvSearch.CurrentRow.Cells["Barcode"].Value.ToString()!;
            var product = _productRepository.GetByBarcode(barcode);

            if (product != null)
            {
                using var adjustForm = new AdjustStockForm(_adjustStockUseCase, product);
                if (adjustForm.ShowDialog() == DialogResult.OK)
                {
                    TxtBuscador_TextChanged(this, EventArgs.Empty);
                    SetStatus("Stock ajustado");
                }
            }
        }
    }
}
