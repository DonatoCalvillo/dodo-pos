using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;

namespace FerreSys.Infrastructure.UI;

public partial class Form1 : Form
{
    private TextBox txtBuscador = null!;
    private ToolStripButton btnOpenAddProduct = null!;
    private DataGridView dgvSearch = null!;
    private DataGridView dgvCart = null!;
    private TabControl tabsCarts = null!;
    private Label lblTotal = null!;
    private Label lblCartSummary = null!;
    private ToolStripStatusLabel lblStatus = null!;
    private Button btnCheckout = null!;
    private Button btnPlus = null!;
    private Button btnMinus = null!;
    private Button btnClearCart = null!;
    private Button btnNewCart = null!;
    private Button btnCloseCart = null!;
    private ToolStripButton btnViewSales = null!;
    private ToolStripButton btnCashCloseX = null!;
    private ToolStripButton btnCashCloseZ = null!;
    private ToolStripButton btnCashMovements = null!;
    private ToolStripButton btnImportExcel = null!;

    private readonly List<CartSession> _cartSessions = [];
    private int _activeSessionIndex;
    private int _nextCartNumber = 1;
    private bool _suppressTabEvents;
    private bool _openingCashPromptShown;

    private readonly SqliteDbContext _dbContext;
    private readonly SqliteProductRepository _productRepository;
    private readonly LocalSubscriptionService _subscriptionService;
    private readonly AddProductUseCase _addProductUseCase;
    private readonly SearchProductsUseCase _searchProductsUseCase;
    private readonly ProcessSaleUseCase _processSaleUseCase;
    private readonly GetDailyCashSummaryUseCase _getDailyCashSummaryUseCase;
    private readonly SaveOpeningCashUseCase _saveOpeningCashUseCase;
    private readonly CloseCashDayUseCase _closeCashDayUseCase;
    private readonly GetCashMovementsUseCase _getCashMovementsUseCase;
    private readonly RegisterCashMovementUseCase _registerCashMovementUseCase;
    private readonly UpdateProductUseCase _updateProductUseCase;
    private ContextMenuStrip cmsSearchMenu = null!;
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
        _getDailyCashSummaryUseCase = new GetDailyCashSummaryUseCase(_productRepository);
        _saveOpeningCashUseCase = new SaveOpeningCashUseCase(_productRepository);
        _closeCashDayUseCase = new CloseCashDayUseCase(_productRepository);
        _getCashMovementsUseCase = new GetCashMovementsUseCase(_productRepository);
        _registerCashMovementUseCase = new RegisterCashMovementUseCase(_productRepository);
        _updateProductUseCase = new UpdateProductUseCase(_productRepository);
        _importProductsUseCase = new ImportProductsUseCase(_productRepository, _subscriptionService);
        _adjustStockUseCase = new AdjustStockUseCase(_productRepository);

        // --- CONFIGURACIÓN DE TERMINAL PROFESIONAL (FULL SCREEN) ---
        var version = typeof(Program).Assembly.GetName().Version;
        var versionLabel = version is null ? "" : $" v{version.ToString(3)}";
        this.Text = $"FerreSys MVP - Terminal de Mostrador{versionLabel}";
        this.StartPosition = FormStartPosition.CenterScreen;

        // Eliminamos los bordes de la ventana (maximizar, minimizar, cerrar tradicionales)
        this.FormBorderStyle = FormBorderStyle.None;
        // Forzamos a que se maximice ocupando absolutamente todo el monitor
        this.WindowState = FormWindowState.Maximized;

        // Mantenemos activo el KeyPreview para que escuche el teclado de forma global
        this.KeyPreview = true;
        this.KeyDown += Form1_KeyDown;
        this.FormClosing += Form1_FormClosing;
        this.Shown += Form1_Shown;

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
        var mCorteX = new ToolStripMenuItem("Corte &X (estado de caja)...", null, (_, _) => OpenCashClose(CashCutMode.X));
        var mCorteZ = new ToolStripMenuItem("Corte &Z (cierre)...", null, (_, _) => OpenCashClose(CashCutMode.Z));
        var mMovimientos = new ToolStripMenuItem("&Movimientos de caja...", null, (_, _) => OpenCashMovements());
        var mImportar = new ToolStripMenuItem("&Importar productos (CSV)...", null, BtnImportExcel_Click);
        var mSalir = new ToolStripMenuItem("&Salir", null, (_, _) => Close());
        mArchivo.DropDownItems.AddRange([mVentas, mCorteX, mCorteZ, mMovimientos, mImportar, new ToolStripSeparator(), mSalir]);

        var mProductos = new ToolStripMenuItem("&Productos");
        mProductos.DropDownItems.Add(new ToolStripMenuItem("&Registrar producto...", null, BtnOpenAddProduct_Click));

        var mVenta = new ToolStripMenuItem("&Venta");
        mVenta.DropDownItems.Add(new ToolStripMenuItem("&Cobrar (F5)", null, BtnCheckout_Click));
        mVenta.DropDownItems.Add(new ToolStripMenuItem("&Vaciar carrito", null, BtnClearCart_Click));
        mVenta.DropDownItems.Add(new ToolStripSeparator());
        mVenta.DropDownItems.Add(new ToolStripMenuItem("&Siguiente carrito (F6)", null, (_, _) => CycleToNextCart()));
        mVenta.DropDownItems.Add(new ToolStripMenuItem("&Nueva venta (F7)", null, (_, _) => CreateNewCartSession()));
        mVenta.DropDownItems.Add(new ToolStripMenuItem("Cerrar venta actual", null, (_, _) => CloseActiveCartSession()));

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

        btnCashCloseX = CreateToolItem("Corte X", UiIcons.CashClose());
        btnCashCloseX.Click += (_, _) => OpenCashClose(CashCutMode.X);

        btnCashCloseZ = CreateToolItem("Corte Z", UiIcons.CashClose());
        btnCashCloseZ.Click += (_, _) => OpenCashClose(CashCutMode.Z);

        btnCashMovements = CreateToolItem("Movimientos", UiIcons.CashMovements());
        btnCashMovements.Click += (_, _) => OpenCashMovements();

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
            btnCashCloseX,
            btnCashCloseZ,
            btnCashMovements,
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

        var gbSearch = UiHelp.CreateGroupBox("Búsqueda de producto", SectionHelpText.Search, new Padding(0, 0, 6, 0));

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

        var gbHints = UiHelp.CreateGroupBox("Indicaciones", margin: new Padding(6, 0, 0, 0));

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

        var gbResults = UiHelp.CreateGroupBox("Resultados del catálogo", SectionHelpText.CatalogResults, new Padding(0, 0, 6, 0));

        dgvSearch = CreateGrid();
        dgvSearch.Columns.Add("Barcode", "Código");
        dgvSearch.Columns.Add("Name", "Descripción");
        dgvSearch.Columns.Add("Price", "Precio");
        dgvSearch.Columns.Add("Stock", "Stock");
        dgvSearch.DoubleClick += DgvSearch_DoubleClick;

        var resultsPad = CreateContentPanel();
        resultsPad.Controls.Add(dgvSearch);
        gbResults.Controls.Add(resultsPad);

        var gbSummary = UiHelp.CreateGroupBox("Resumen de venta", SectionHelpText.SaleSummary, new Padding(6, 0, 0, 0));

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

        var gbCart = UiHelp.CreateGroupBox("Carrito de compra actual", SectionHelpText.Cart, new Padding(0, 0, 6, 0));

        var cartLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        cartLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        cartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        tabsCarts = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            Appearance = TabAppearance.Normal,
            SizeMode = TabSizeMode.Normal,
            Padding = new Point(12, 4)
        };
        tabsCarts.SelectedIndexChanged += TabsCarts_SelectedIndexChanged;

        var tabsMenu = new ContextMenuStrip { Font = UiTheme.Ui };
        tabsMenu.Items.Add("Cerrar venta", null, (_, _) => CloseActiveCartSession());
        tabsMenu.Items.Add("Nueva venta (F7)", null, (_, _) => CreateNewCartSession());
        tabsCarts.ContextMenuStrip = tabsMenu;

        dgvCart = CreateGrid();
        dgvCart.Columns.Add("Barcode", "Código");
        dgvCart.Columns.Add("Name", "Producto");
        dgvCart.Columns.Add("Price", "Precio");
        dgvCart.Columns.Add("Qty", "Cant.");
        dgvCart.Columns.Add("Subtotal", "Subtotal");

        var cartPad = CreateContentPanel();
        cartPad.Controls.Add(dgvCart);

        cartLayout.Controls.Add(tabsCarts, 0, 0);
        cartLayout.Controls.Add(cartPad, 0, 1);
        gbCart.Controls.Add(cartLayout);

        EnsureInitialCartSession();

        var gbActions = UiHelp.CreateGroupBox("Acciones", SectionHelpText.Actions, new Padding(6, 0, 0, 0));

        // Secciones: línea del carrito | ventas en espera | cobrar (CTA)
        var actionsStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 11,
            Padding = new Padding(4, 8, 4, 4),
            BackColor = UiTheme.PanelBg
        };
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // header línea
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // separador
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // header espera
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));  // regla antes de CTA
        actionsStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        btnPlus = CreateActionButton("Añadir 1", UiIcons.Plus());
        btnPlus.Click += BtnPlus_Click;

        btnMinus = CreateActionButton("Quitar 1", UiIcons.Minus());
        btnMinus.Click += BtnMinus_Click;

        btnClearCart = CreateActionButton("Vaciar carrito", UiIcons.Clear());
        btnClearCart.Click += BtnClearCart_Click;

        btnNewCart = CreateActionButton("Nueva venta (F7)", UiIcons.NewSale());
        btnNewCart.Click += (_, _) => CreateNewCartSession();

        btnCloseCart = CreateActionButton("Cerrar venta", UiIcons.CloseSale());
        btnCloseCart.Click += (_, _) => CloseActiveCartSession();

        btnCheckout = CreateActionButton("Cobrar venta (F5)", UiIcons.Checkout());
        btnCheckout.Font = UiTheme.UiBold;
        btnCheckout.Click += BtnCheckout_Click;

        actionsStack.Controls.Add(CreateActionSectionLabel("Línea del carrito"), 0, 0);
        actionsStack.Controls.Add(btnPlus, 0, 1);
        actionsStack.Controls.Add(btnMinus, 0, 2);
        actionsStack.Controls.Add(btnClearCart, 0, 3);
        actionsStack.Controls.Add(CreateActionDivider(), 0, 4);
        actionsStack.Controls.Add(CreateActionSectionLabel("Ventas en espera"), 0, 5);
        actionsStack.Controls.Add(btnNewCart, 0, 6);
        actionsStack.Controls.Add(btnCloseCart, 0, 7);
        actionsStack.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.PanelBg }, 0, 8);
        actionsStack.Controls.Add(CreateActionDivider(), 0, 9);
        actionsStack.Controls.Add(btnCheckout, 0, 10);

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

    private static Label CreateActionSectionLabel(string text)
    {
        return new Label
        {
            Text = text.ToUpperInvariant(),
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            ForeColor = UiTheme.GroupTitle,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(2, 0, 2, 0),
            Padding = new Padding(2, 0, 0, 0)
        };
    }

    private static Panel CreateActionDivider()
    {
        var wrap = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 2, 2, 2),
            BackColor = UiTheme.PanelBg
        };
        wrap.Controls.Add(new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(190, 190, 190)
        });
        return wrap;
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

    private void OpenCashClose(CashCutMode mode)
    {
        using var f = new CashCloseForm(
            _getDailyCashSummaryUseCase,
            _saveOpeningCashUseCase,
            _closeCashDayUseCase,
            mode);
        f.ShowDialog();
    }

    private void OpenCashMovements()
    {
        using var f = new CashMovementsForm(
            _getCashMovementsUseCase,
            _registerCashMovementUseCase,
            _getDailyCashSummaryUseCase);
        f.ShowDialog();
    }

    private void Form1_Shown(object? sender, EventArgs e)
    {
        if (_openingCashPromptShown)
            return;

        _openingCashPromptShown = true;
        PromptOpeningCashIfNeeded();
    }

    private void PromptOpeningCashIfNeeded()
    {
        var today = DateTime.Today;
        if (_productRepository.GetCashClose(today) != null)
            return;

        using var prompt = new OpeningCashPromptForm(_saveOpeningCashUseCase, today);
        var result = prompt.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            lblStatus.Text = "Fondo inicial del día guardado.";
            lblStatus.ForeColor = UiTheme.StatusOk;
        }
        else
        {
            lblStatus.Text = "Fondo del día no declarado. Puedes seguir cobrando o declararlo en Corte X.";
            lblStatus.ForeColor = Color.DarkOrange;
        }
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
                dgvSearch.Rows.Add(p.Barcode, p.Name, p.RetailPrice, p.Unit.FormatQuantity(p.Stock));
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
        string barcode = CellText(row, "Barcode");
        string name = CellText(row, "Name");
        decimal price = Convert.ToDecimal(row.Cells["Price"].Value);

        var product = _productRepository.GetByBarcode(barcode);
        if (product == null)
        {
            MessageBox.Show($"No se encontró el producto '{barcode}'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        decimal qtyToAdd = 1m;
        if (product.Unit.AllowsDecimals())
        {
            using var qtyForm = new BulkQuantityForm(product.Name, product.Unit, product.Stock);
            if (qtyForm.ShowDialog(this) != DialogResult.OK)
                return;
            qtyToAdd = qtyForm.Quantity;
        }

        foreach (DataGridViewRow cartRow in dgvCart.Rows)
        {
            if (CellText(cartRow, 0) == barcode)
            {
                decimal currentQty = Convert.ToDecimal(cartRow.Cells["Qty"].Value);
                decimal newQty = currentQty + qtyToAdd;
                cartRow.Cells["Qty"].Value = FormatCartQty(newQty, product.Unit);
                cartRow.Cells["Subtotal"].Value = newQty * price;
                CalculateTotal();
                SetStatus($"Actualizado en carrito: {name} ({product.Unit.FormatQuantity(newQty)})");
                return;
            }
        }

        dgvCart.Rows.Add(barcode, name, price, FormatCartQty(qtyToAdd, product.Unit), qtyToAdd * price);
        CalculateTotal();
        SetStatus($"Añadido al carrito: {name} ({product.Unit.FormatQuantity(qtyToAdd)})");
    }

    private static string FormatCartQty(decimal qty, UnitOfMeasure unit) =>
        unit.AllowsDecimals() ? qty.ToString("0.###") : ((int)qty).ToString();

    private CartSession ActiveSession => _cartSessions[_activeSessionIndex];

    private void EnsureInitialCartSession()
    {
        if (_cartSessions.Count > 0)
            return;

        var session = new CartSession { Label = $"Venta {_nextCartNumber++}" };
        _cartSessions.Add(session);
        _activeSessionIndex = 0;

        _suppressTabEvents = true;
        tabsCarts.TabPages.Clear();
        tabsCarts.TabPages.Add(CreateTabPage(session));
        tabsCarts.SelectedIndex = 0;
        _suppressTabEvents = false;
    }

    private static TabPage CreateTabPage(CartSession session) =>
        new(session.Label) { Tag = session.Id, ToolTipText = session.Label };

    private List<CartLineSnapshot> CaptureCartFromGrid()
    {
        var lines = new List<CartLineSnapshot>();
        foreach (DataGridViewRow row in dgvCart.Rows)
        {
            if (row.IsNewRow)
                continue;

            lines.Add(new CartLineSnapshot
            {
                Barcode = CellText(row, "Barcode"),
                Name = CellText(row, "Name"),
                Price = Convert.ToDecimal(row.Cells["Price"].Value),
                Quantity = Convert.ToDecimal(row.Cells["Qty"].Value),
                Subtotal = Convert.ToDecimal(row.Cells["Subtotal"].Value)
            });
        }

        return lines;
    }

    private void SyncActiveSessionFromGrid()
    {
        if (_cartSessions.Count == 0)
            return;

        var session = ActiveSession;
        session.Lines.Clear();
        session.Lines.AddRange(CaptureCartFromGrid());
    }

    private void LoadCartIntoGrid(CartSession session)
    {
        dgvCart.Rows.Clear();
        foreach (var line in session.Lines)
        {
            var product = _productRepository.GetByBarcode(line.Barcode);
            UnitOfMeasure unit = product?.Unit ?? UnitOfMeasure.Pieza;
            dgvCart.Rows.Add(
                line.Barcode,
                line.Name,
                line.Price,
                FormatCartQty(line.Quantity, unit),
                line.Subtotal);
        }

        CalculateTotal();
    }

    private void SwitchToCartSession(int index)
    {
        if (index < 0 || index >= _cartSessions.Count)
            return;

        SyncActiveSessionFromGrid();
        _activeSessionIndex = index;

        _suppressTabEvents = true;
        if (tabsCarts.SelectedIndex != index)
            tabsCarts.SelectedIndex = index;
        _suppressTabEvents = false;

        LoadCartIntoGrid(ActiveSession);
        SetStatus($"Carrito activo: {ActiveSession.Label}");
        txtBuscador.Focus();
    }

    private void TabsCarts_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppressTabEvents || tabsCarts.SelectedIndex < 0)
            return;

        if (tabsCarts.SelectedIndex == _activeSessionIndex)
            return;

        SwitchToCartSession(tabsCarts.SelectedIndex);
    }

    private void CreateNewCartSession()
    {
        if (_cartSessions.Count >= CartSession.MaxCartSessions)
        {
            MessageBox.Show(
                $"Solo puedes tener hasta {CartSession.MaxCartSessions} ventas en espera al mismo tiempo.",
                "Límite de carritos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        SyncActiveSessionFromGrid();

        var session = new CartSession { Label = $"Venta {_nextCartNumber++}" };
        _cartSessions.Add(session);

        _suppressTabEvents = true;
        tabsCarts.TabPages.Add(CreateTabPage(session));
        _activeSessionIndex = _cartSessions.Count - 1;
        tabsCarts.SelectedIndex = _activeSessionIndex;
        _suppressTabEvents = false;

        LoadCartIntoGrid(session);
        SetStatus($"Nueva venta: {session.Label}");
        txtBuscador.Focus();
    }

    private void CycleToNextCart()
    {
        if (_cartSessions.Count <= 1)
        {
            SetStatus("Solo hay una venta activa");
            return;
        }

        int next = (_activeSessionIndex + 1) % _cartSessions.Count;
        SwitchToCartSession(next);
    }

    private void CloseActiveCartSession()
    {
        if (_cartSessions.Count <= 1)
        {
            MessageBox.Show(
                "Debe quedar al menos una venta activa.",
                "Cerrar venta",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        SyncActiveSessionFromGrid();
        var session = ActiveSession;

        if (session.Lines.Count > 0)
        {
            var confirm = MessageBox.Show(
                $"¿Cerrar {session.Label} con {session.Lines.Count} línea(s) en el carrito?",
                "Cerrar venta",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;
        }

        int closingIndex = _activeSessionIndex;
        _cartSessions.RemoveAt(closingIndex);

        _suppressTabEvents = true;
        tabsCarts.TabPages.RemoveAt(closingIndex);
        int newIndex = Math.Min(closingIndex, _cartSessions.Count - 1);
        _activeSessionIndex = newIndex;
        tabsCarts.SelectedIndex = newIndex;
        _suppressTabEvents = false;

        LoadCartIntoGrid(ActiveSession);
        SetStatus($"Cerrada. Activa: {ActiveSession.Label}");
        txtBuscador.Focus();
    }

    private void ClearActiveCartSession()
    {
        dgvCart.Rows.Clear();
        if (_cartSessions.Count > 0)
            ActiveSession.Lines.Clear();
        CalculateTotal();
    }

    private void CalculateTotal()
    {
        _saleTotal = 0;
        decimal itemCount = 0;
        foreach (DataGridViewRow row in dgvCart.Rows)
        {
            _saleTotal += Convert.ToDecimal(row.Cells["Subtotal"].Value);
            itemCount += Convert.ToDecimal(row.Cells["Qty"].Value);
        }
        lblTotal.Text = $"TOTAL\n${_saleTotal:N2}";
        string label = _cartSessions.Count > 0 ? ActiveSession.Label : "Venta";
        lblCartSummary.Text = $"{label}\nCantidad: {itemCount:0.###}\nLíneas: {dgvCart.Rows.Count}";
    }

    private void BtnCheckout_Click(object? sender, EventArgs e)
    {
        try
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("El carrito está vacío.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ABRIR VENTANA DE COBRO INTERMEDIA: Pasamos el total actual calculando del carrito
            using var checkoutForm = new CheckoutForm(_saleTotal);
            if (checkoutForm.ShowDialog() != DialogResult.OK)
            {
                return; // Si el ferretero cancela o cierra la ventana, detenemos el cobro
            }

            // Si da confirmar, extraemos los métodos seleccionados
            string method = checkoutForm.SelectedPaymentMethod;
            string reference = checkoutForm.CardReference;
            decimal amountReceived = checkoutForm.AmountReceived;
            decimal cardAmount = checkoutForm.CardAmount;

            var items = new List<SaleItemDto>();
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                items.Add(new SaleItemDto
                {
                    Barcode = CellText(row, 0),
                    Quantity = Convert.ToDecimal(row.Cells["Qty"].Value)
                });
            }

            var saleItemsForTicket = new List<SaleItem>();
            foreach (DataGridViewRow row in dgvCart.Rows)
            {
                string barcode = CellText(row, 0);
                var p = _productRepository.GetByBarcode(barcode)!;
                decimal qty = Convert.ToDecimal(row.Cells["Qty"].Value);
                saleItemsForTicket.Add(new SaleItem(p.Id, p.Barcode, p.Name, qty, p.RetailPrice, p.CostPrice, p.Unit));
            }

            // Pasamos el método y referencia también al objeto simulado del ticket impreso
            var simulatedSale = new Sale(
                Guid.NewGuid(),
                DateTime.Now,
                saleItemsForTicket,
                method,
                reference,
                amountReceived,
                cardAmount);

            // 2. Ejecutar caso de uso enviando los nuevos datos de control financiero
            _processSaleUseCase.Execute(items, method, reference, amountReceived, cardAmount);

            // 3. Generación del ticket impreso
            string ticketText = TicketFormatter.GenerateTicketText(simulatedSale);

            MessageBox.Show(ticketText, "Vista Previa del Ticket Impreso (58mm)", MessageBoxButtons.OK, MessageBoxIcon.None);

            if (method == "EFECTIVO" || method == "MIXTO")
                SetStatus($"Venta cobrada. Cambio: ${simulatedSale.Change:N2}");
            else
                SetStatus("Venta cobrada con tarjeta");

            // Limpiar solo la sesión/tab activa; las demás ventas en espera se conservan
            ClearActiveCartSession();
            txtBuscador.Clear();
            dgvSearch.Rows.Clear();
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

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            string backupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "FerreSys_Backups");
            string fileName = $"ferresys_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            _dbContext.BackupTo(Path.Combine(backupFolder, fileName));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo guardar el respaldo en Documentos\\FerreSys_Backups.\n\n{ex.Message}",
                "Respaldo local",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            btnCheckout.PerformClick();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.F6)
        {
            CycleToNextCart();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.F7)
        {
            CreateNewCartSession();
            e.Handled = true;
            return;
        }

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
                System.Windows.Forms.Application.Exit();
            }

            e.Handled = true;
        }
    }
    private void BtnPlus_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow == null) return;

        var row = dgvCart.CurrentRow;
        string barcode = CellText(row, "Barcode");
        var product = _productRepository.GetByBarcode(barcode);
        UnitOfMeasure unit = product?.Unit ?? UnitOfMeasure.Pieza;
        decimal step = unit.AllowsDecimals() ? 0.1m : 1m;

        decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
        decimal currentQty = Convert.ToDecimal(row.Cells["Qty"].Value);
        decimal newQty = currentQty + step;

        row.Cells["Qty"].Value = FormatCartQty(newQty, unit);
        row.Cells["Subtotal"].Value = newQty * price;
        CalculateTotal();
    }

    private void BtnMinus_Click(object? sender, EventArgs e)
    {
        if (dgvCart.CurrentRow == null) return;

        var row = dgvCart.CurrentRow;
        string barcode = CellText(row, "Barcode");
        var product = _productRepository.GetByBarcode(barcode);
        UnitOfMeasure unit = product?.Unit ?? UnitOfMeasure.Pieza;
        decimal step = unit.AllowsDecimals() ? 0.1m : 1m;

        decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
        decimal currentQty = Convert.ToDecimal(row.Cells["Qty"].Value);
        decimal newQty = currentQty - step;

        if (newQty <= 0)
        {
            dgvCart.Rows.Remove(row);
        }
        else
        {
            row.Cells["Qty"].Value = FormatCartQty(newQty, unit);
            row.Cells["Subtotal"].Value = newQty * price;
        }

        CalculateTotal();
    }

    private void BtnClearCart_Click(object? sender, EventArgs e)
    {
        if (dgvCart.Rows.Count > 0)
        {
            string label = _cartSessions.Count > 0 ? ActiveSession.Label : "el carrito";
            var result = MessageBox.Show(
                $"¿Seguro que deseas vaciar {label}?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                ClearActiveCartSession();
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
            string barcode = CellText(dgvSearch.CurrentRow, "Barcode");
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
                ImportProductsResult result = _importProductsUseCase.Execute(openFileDialog.FileName);

                if (result.Products.Count > 0)
                    _productRepository.SaveBulk(result.Products);

                if (result.SkippedByLimit > 0 && result.Products.Count == 0)
                {
                    MessageBox.Show(
                        $"Has alcanzado el límite de {FreeTierLimits.ProductLimit} productos de la versión gratuita.\n\n" +
                        $"{result.SkippedByLimit} filas del archivo no se importaron.\n\n" +
                        FreeTierLimits.PremiumUpsell,
                        "Límite de la versión gratuita",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    SetStatus("Importación detenida: límite de productos alcanzado");
                }
                else if (result.SkippedByLimit > 0)
                {
                    MessageBox.Show(
                        $"Se han registrado {result.Products.Count} productos.\n\n" +
                        $"{result.SkippedByLimit} filas no se importaron porque superan el límite de {FreeTierLimits.ProductLimit} de la versión gratuita.\n\n" +
                        FreeTierLimits.PremiumUpsell,
                        "Importador de Inventario",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    TxtBuscador_TextChanged(this, EventArgs.Empty);
                    SetStatus($"Importados {result.Products.Count} productos; {result.SkippedByLimit} omitidos por el límite");
                }
                else
                {
                    MessageBox.Show(
                        $"¡Importación exitosa!\n\nSe han registrado {result.Products.Count} productos correctamente en la base de datos.",
                        "Importador de Inventario",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    TxtBuscador_TextChanged(this, EventArgs.Empty);
                    SetStatus($"Importados {result.Products.Count} productos");
                }
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
            string barcode = CellText(dgvSearch.CurrentRow, "Barcode");
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

    private static string CellText(DataGridViewRow row, string columnName) =>
        row.Cells[columnName]?.Value?.ToString() ?? string.Empty;

    private static string CellText(DataGridViewRow row, int columnIndex) =>
        row.Cells[columnIndex].Value?.ToString() ?? string.Empty;
}
