using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;
using FerreSys.Infrastructure.UI.Printing;

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
    private ToolStripButton btnGrossProfit = null!;
    private ToolStripButton btnReports = null!;
    private ToolStripButton btnPurchaseSuggestion = null!;
    private ToolStripButton btnCashCloseX = null!;
    private ToolStripButton btnCashCloseZ = null!;
    private ToolStripButton btnCashMovements = null!;
    private ToolStripButton btnKardex = null!;
    private ToolStripButton btnInventory = null!;
    private ToolStripButton btnImportExcel = null!;
    private ToolStripButton btnHideInventory = null!;
    private ToolStripButton btnBusiness = null!;
    private ToolStripButton btnPeripherals = null!;
    private ToolStripButton btnUsers = null!;
    private ToolStripMenuItem mnuSales = null!;
    private ToolStripMenuItem mnuProfit = null!;
    private ToolStripMenuItem mnuReports = null!;
    private ToolStripMenuItem mnuSuggestion = null!;
    private ToolStripMenuItem mnuCashX = null!;
    private ToolStripMenuItem mnuCashZ = null!;
    private ToolStripMenuItem mnuCashMovements = null!;
    private ToolStripMenuItem mnuImport = null!;
    private ToolStripMenuItem mnuBusiness = null!;
    private ToolStripMenuItem mnuPeripherals = null!;
    private ToolStripMenuItem mnuUsers = null!;
    private ToolStripMenuItem mnuProducts = null!;
    private ToolStripMenuItem mnuRegisterProduct = null!;
    private ToolStripMenuItem mnuSuggestionProducts = null!;
    private ToolStripMenuItem mnuInventory = null!;
    private ToolStripMenuItem mnuKardex = null!;
    private ToolStripMenuItem mnuHideInventory = null!;
    private ToolStripMenuItem mnuSale = null!;
    private ToolStripMenuItem mnuEditProduct = null!;
    private ToolStripMenuItem mnuAuditProduct = null!;
    private ToolStripMenuItem mnuKardexProduct = null!;
    private ToolStripMenuItem mnuHideProduct = null!;
    private ToolStripStatusLabel lblSignedIn = null!;
    private ToolStripStatusLabel lblStockAlerts = null!;
    private ToolStripStatusLabel lblLicense = null!;
    private ToolStripLabel lblBrand = null!;
    private ToolStripLabel lblBrandName = null!;
    private ToolStripLabel lblBrandDivider = null!;
    private ToolStripLabel lblBusinessLogo = null!;
    private ToolStripLabel lblBusinessName = null!;
    private Image? _businessLogo;

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
    private readonly GetGrossProfitSummaryUseCase _getGrossProfitSummaryUseCase;
    private readonly ReportsUseCase _reportsUseCase;
    private readonly GetReorderSuggestionsUseCase _getReorderSuggestionsUseCase;
    private readonly ListInventoryUseCase _listInventoryUseCase;
    private readonly SaveOpeningCashUseCase _saveOpeningCashUseCase;
    private readonly CloseCashDayUseCase _closeCashDayUseCase;
    private readonly GetCashMovementsUseCase _getCashMovementsUseCase;
    private readonly RegisterCashMovementUseCase _registerCashMovementUseCase;
    private readonly GetStockMovementsUseCase _getStockMovementsUseCase;
    private readonly UpdateProductUseCase _updateProductUseCase;
    private ContextMenuStrip cmsSearchMenu = null!;
    private readonly ImportProductsUseCase _importProductsUseCase;
    private readonly AdjustStockUseCase _adjustStockUseCase;
    private readonly DeactivateProductUseCase _deactivateProductUseCase;
    private readonly string[] _startupArgs;
    private readonly SqliteAppSettingsStore _appSettings;
    private readonly CloudBackupService _cloudBackup;
    private readonly LocalBackupService _localBackup;
    private readonly System.Windows.Forms.Timer _backupTimer;
    private readonly PrintSaleTicketUseCase _printSaleTicketUseCase;
    private SerialBarcodeScanner? _serialScanner;
    private string? _lastScanCode;
    private decimal _saleTotal = 0;

    private readonly User _currentUser;

    public bool SessionEnded { get; private set; }

    public Form1(string[]? startupArgs, LocalSubscriptionService subscription, User currentUser)
    {
        _startupArgs = startupArgs ?? [];
        _currentUser = currentUser;
        _dbContext = new SqliteDbContext();
        _productRepository = new SqliteProductRepository(_dbContext);
        _subscriptionService = subscription;
        _addProductUseCase = new AddProductUseCase(_productRepository, _subscriptionService);
        _searchProductsUseCase = new SearchProductsUseCase(_productRepository);
        _processSaleUseCase = new ProcessSaleUseCase(_productRepository);
        _getDailyCashSummaryUseCase = new GetDailyCashSummaryUseCase(_productRepository);
        _getGrossProfitSummaryUseCase = new GetGrossProfitSummaryUseCase(_productRepository);
        _reportsUseCase = new ReportsUseCase(new SqliteReportReader(_dbContext));
        _getReorderSuggestionsUseCase = new GetReorderSuggestionsUseCase(_productRepository);
        _listInventoryUseCase = new ListInventoryUseCase(_productRepository);
        _saveOpeningCashUseCase = new SaveOpeningCashUseCase(_productRepository);
        _closeCashDayUseCase = new CloseCashDayUseCase(_productRepository);
        _getCashMovementsUseCase = new GetCashMovementsUseCase(_productRepository);
        _registerCashMovementUseCase = new RegisterCashMovementUseCase(_productRepository);
        _getStockMovementsUseCase = new GetStockMovementsUseCase(_productRepository);
        _updateProductUseCase = new UpdateProductUseCase(_productRepository);
        _importProductsUseCase = new ImportProductsUseCase(_productRepository, _subscriptionService);
        _adjustStockUseCase = new AdjustStockUseCase(_productRepository);
        _deactivateProductUseCase = new DeactivateProductUseCase(_productRepository);
        _appSettings = new SqliteAppSettingsStore(_dbContext);
        _cloudBackup = new CloudBackupService(_dbContext, new UnconfiguredCloudBackupStore());
        _localBackup = new LocalBackupService(_dbContext, _appSettings);
        _backupTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _backupTimer.Tick += (_, _) => RunScheduledBackup();
        _printSaleTicketUseCase = new PrintSaleTicketUseCase();

        // --- CONFIGURACIÓN DE TERMINAL PROFESIONAL (FULL SCREEN) ---
        var version = typeof(Program).Assembly.GetName().Version;
        var versionLabel = version is null ? "" : $" v{version.ToString(3)}";
        this.Text = $"Posly - Punto de Venta{versionLabel}";
        this.StartPosition = FormStartPosition.CenterScreen;

        // Eliminamos los bordes de la ventana (maximizar, minimizar, cerrar tradicionales)
        this.FormBorderStyle = FormBorderStyle.None;
        // Forzamos a que se maximice ocupando absolutamente todo el monitor
        this.WindowState = FormWindowState.Maximized;

        // Mantenemos activo el KeyPreview para que escuche el teclado de forma global
        this.Icon = Branding.AppIcon;
        this.KeyPreview = true;
        this.KeyDown += Form1_KeyDown;
        this.FormClosing += Form1_FormClosing;
        this.Shown += Form1_Shown;

        InicializarComponentes();
        RefreshLicenseStatus();
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
        RefreshStockAlerts();
        ApplyAccess();
        ApplySerialScanner(notifyOnError: false);
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
        mnuSales = new ToolStripMenuItem("Ver &Ventas...", null, (_, _) => OpenSales());
        mnuProfit = new ToolStripMenuItem("&Utilidad bruta / ganancia neta...", null, (_, _) => OpenGrossProfitReport());
        mnuReports = new ToolStripMenuItem("&Reportes", null, (_, _) => OpenReports());
        mnuSuggestion = new ToolStripMenuItem("&Sugerido de compra...", null, (_, _) => OpenPurchaseSuggestion());
        mnuCashX = new ToolStripMenuItem("Corte &X (estado de caja)...", null, (_, _) => OpenCashClose(CashCutMode.X));
        mnuCashZ = new ToolStripMenuItem("Corte &Z (cierre)...", null, (_, _) => OpenCashClose(CashCutMode.Z));
        mnuCashMovements = new ToolStripMenuItem("&Movimientos de caja...", null, (_, _) => OpenCashMovements());
        mnuImport = new ToolStripMenuItem("&Importar productos (CSV)...", null, BtnImportExcel_Click);
        mnuBusiness = new ToolStripMenuItem("&Datos del negocio...", null, (_, _) => OpenBusinessProfile());
        mnuPeripherals = new ToolStripMenuItem("&Periféricos e impresión...", null, (_, _) => OpenPeripherals());
        mnuUsers = new ToolStripMenuItem("&Usuarios...", null, (_, _) => OpenUsers());
        var mLogout = new ToolStripMenuItem("Cerrar &sesión", null, (_, _) => Logout());
        var mSalir = new ToolStripMenuItem("&Salir", null, (_, _) => Close());
        mArchivo.DropDownItems.AddRange([
            mnuSales, mnuProfit, mnuSuggestion, mnuCashX, mnuCashZ, mnuCashMovements, mnuImport,
            mnuBusiness, mnuPeripherals, mnuUsers, new ToolStripSeparator(), mLogout, mSalir]);

        mnuProducts = new ToolStripMenuItem("&Productos");
        mnuRegisterProduct = new ToolStripMenuItem("&Registrar producto...", null, BtnOpenAddProduct_Click);
        mnuSuggestionProducts = new ToolStripMenuItem("Sugerido de &compra...", null, (_, _) => OpenPurchaseSuggestion());
        mnuInventory = new ToolStripMenuItem("&Inventario (F8)...", null, (_, _) => OpenInventory());
        mnuKardex = new ToolStripMenuItem("&Historial de movimientos (Kárdex)...", null, (_, _) => OpenKardex());
        mnuHideInventory = new ToolStripMenuItem("&Ocultar todo el inventario...", null, (_, _) => HideAllInventory());
        mnuProducts.DropDownItems.Add(mnuRegisterProduct);
        mnuProducts.DropDownItems.Add(mnuSuggestionProducts);
        mnuProducts.DropDownItems.Add(mnuInventory);
        mnuProducts.DropDownItems.Add(mnuKardex);
        mnuProducts.DropDownItems.Add(new ToolStripSeparator());
        mnuProducts.DropDownItems.Add(mnuHideInventory);

        mnuSale = new ToolStripMenuItem("&Venta");
        mnuSale.DropDownItems.Add(new ToolStripMenuItem("&Cobrar (F5)", null, BtnCheckout_Click));
        mnuSale.DropDownItems.Add(new ToolStripMenuItem("&Vaciar carrito", null, BtnClearCart_Click));
        mnuSale.DropDownItems.Add(new ToolStripSeparator());
        mnuSale.DropDownItems.Add(new ToolStripMenuItem("&Siguiente carrito (F6)", null, (_, _) => CycleToNextCart()));
        mnuSale.DropDownItems.Add(new ToolStripMenuItem("&Nueva venta (F7)", null, (_, _) => CreateNewCartSession()));
        mnuSale.DropDownItems.Add(new ToolStripMenuItem("Cerrar venta actual", null, (_, _) => CloseActiveCartSession()));

        menu.Items.AddRange([mArchivo, mnuProducts, mnuSale, mnuReports]);
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

        btnGrossProfit = CreateToolItem("Utilidad", UiIcons.GrossProfit());
        btnGrossProfit.Click += (_, _) => OpenGrossProfitReport();

        btnReports = CreateToolItem("Reportes", UiIcons.Sales());
        btnReports.Click += (_, _) => OpenReports();

        btnPurchaseSuggestion = CreateToolItem("Sugerido", UiIcons.Reorder());
        btnPurchaseSuggestion.Click += (_, _) => OpenPurchaseSuggestion();

        btnCashCloseX = CreateToolItem("Corte X", UiIcons.CashClose());
        btnCashCloseX.Click += (_, _) => OpenCashClose(CashCutMode.X);

        btnCashCloseZ = CreateToolItem("Corte Z", UiIcons.CashClose());
        btnCashCloseZ.Click += (_, _) => OpenCashClose(CashCutMode.Z);

        btnCashMovements = CreateToolItem("Movimientos", UiIcons.CashMovements());
        btnCashMovements.Click += (_, _) => OpenCashMovements();

        btnKardex = CreateToolItem("Kárdex", UiIcons.Kardex());
        btnKardex.Click += (_, _) => OpenKardex();

        btnInventory = CreateToolItem("Inventario", UiIcons.Kardex());
        btnInventory.Click += (_, _) => OpenInventory();

        btnImportExcel = CreateToolItem("Importar CSV", UiIcons.Import());
        btnImportExcel.Click += BtnImportExcel_Click;

        btnOpenAddProduct = CreateToolItem("Registrar", UiIcons.AddProduct());
        btnOpenAddProduct.Click += BtnOpenAddProduct_Click;

        btnHideInventory = CreateToolItem("Ocultar inventario", UiIcons.Clear());
        btnHideInventory.Click += (_, _) => HideAllInventory();

        btnBusiness = CreateToolItem("Negocio", UiIcons.Business());
        btnBusiness.Click += (_, _) => OpenBusinessProfile();

        btnPeripherals = CreateToolItem("Periféricos", UiIcons.Printer());
        btnPeripherals.Click += (_, _) => OpenPeripherals();

        btnUsers = CreateToolItem("Usuarios", UiIcons.Sales());
        btnUsers.Click += (_, _) => OpenUsers();

        lblBrand = new ToolStripLabel
        {
            Image = Branding.ToolbarLogo,
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageScaling = ToolStripItemImageScaling.None,
            AutoSize = false,
            Size = new Size(48, 48),
            Margin = new Padding(12, 0, 2, 0),
            Text = string.Empty
        };
        lblBrandName = new ToolStripLabel("POSLY")
        {
            Font = new Font("Tahoma", 11f, FontStyle.Bold),
            ForeColor = UiTheme.GroupTitle,
            Margin = new Padding(0, 0, 4, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };
        lblBrandDivider = new ToolStripLabel("|")
        {
            Font = new Font("Tahoma", 11f, FontStyle.Bold),
            ForeColor = UiTheme.GroupTitle,
            Margin = new Padding(4, 0, 4, 0),
            Visible = false
        };
        lblBusinessLogo = new ToolStripLabel
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageScaling = ToolStripItemImageScaling.None,
            AutoSize = false,
            Size = new Size(40, 40),
            Margin = new Padding(4, 0, 4, 0),
            Visible = false
        };
        lblBusinessName = new ToolStripLabel
        {
            Font = new Font("Tahoma", 11f, FontStyle.Bold),
            ForeColor = UiTheme.GroupTitle,
            Margin = new Padding(2, 0, 8, 0),
            Visible = false
        };

        strip.Items.AddRange([
            btnViewSales,
            btnReports,
            btnGrossProfit,
            btnPurchaseSuggestion,
            btnCashCloseX,
            btnCashCloseZ,
            btnCashMovements,
            btnKardex,
            btnInventory,
            btnImportExcel,
            btnOpenAddProduct,
            btnHideInventory,
            btnBusiness,
            btnPeripherals,
            btnUsers,
            new ToolStripSeparator(),
            lblBrand,
            lblBrandName,
            lblBrandDivider,
            lblBusinessLogo,
            lblBusinessName
        ]);
        RefreshBrand();

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
                   "• Enter: 1 resultado; suma al carrito si el escáner lo tiene activo\r\n" +
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
        ProductImages.AddPhotoColumn(dgvSearch);
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
        ProductImages.AddPhotoColumn(dgvCart);
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

        lblStockAlerts = new ToolStripStatusLabel("")
        {
            IsLink = true,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Visible = false,
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            BorderStyle = Border3DStyle.Etched
        };
        lblStockAlerts.Click += (_, _) => OpenPurchaseSuggestion();

        var connected = new ToolStripStatusLabel(" Conectado")
        {
            Image = UiIcons.StatusDot(UiTheme.StatusOk),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            BorderStyle = Border3DStyle.Etched
        };

        lblLicense = new ToolStripStatusLabel(LicensePresentation.StatusBar(_subscriptionService))
        {
            IsLink = true,
            LinkBehavior = LinkBehavior.HoverUnderline,
            Font = UiTheme.UiBold,
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            BorderStyle = Border3DStyle.Etched
        };
        lblLicense.Click += (_, _) => OpenLicense();

        lblSignedIn = new ToolStripStatusLabel(_currentUser.DisplayName)
        {
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            BorderStyle = Border3DStyle.Etched
        };

        status.Items.Add(lblStatus);
        status.Items.Add(lblSignedIn);
        status.Items.Add(lblLicense);
        status.Items.Add(lblStockAlerts);
        status.Items.Add(connected);
        status.Items.Add(new ToolStripStatusLabel("Mostrador") { BorderSides = ToolStripStatusLabelBorderSides.Left, BorderStyle = Border3DStyle.Etched });
        status.Items.Add(new ToolStripStatusLabel("Powered by Do&&Do")
        {
            Image = Branding.Mark,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            BorderStyle = Border3DStyle.Etched
        });

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
        if (!Allow(UserPermission.ViewSales))
            return;

        using var f = new SalesHistoryForm(
            _productRepository,
            _appSettings,
            new ProcessSaleReturnUseCase(_productRepository));
        f.ShowDialog();
    }

    private void OpenPurchaseSuggestion()
    {
        if (!Allow(UserPermission.Reports))
            return;

        using var f = new PurchaseSuggestionForm(_getReorderSuggestionsUseCase);
        f.ShowDialog(this);
        RefreshStockAlerts();
        RefreshSearchIfActive();
    }

    private void OpenReports()
    {
        if (!Allow(UserPermission.Reports))
            return;

        using var form = new ReportsForm(_reportsUseCase, _appSettings);
        form.ShowDialog(this);
    }

    private void OpenGrossProfitReport()
    {
        if (!Allow(UserPermission.Reports))
            return;

        using var f = new GrossProfitReportForm(_getGrossProfitSummaryUseCase);
        f.ShowDialog();
    }

    private void OpenCashClose(CashCutMode mode)
    {
        if (!Allow(UserPermission.Cash))
            return;

        using var f = new CashCloseForm(
            _getDailyCashSummaryUseCase,
            _saveOpeningCashUseCase,
            _closeCashDayUseCase,
            mode,
            _cloudBackup,
            _subscriptionService.PremiumLicenseId);
        f.ShowDialog();
    }

    private void OpenCashMovements()
    {
        if (!Allow(UserPermission.Cash))
            return;

        using var f = new CashMovementsForm(
            _getCashMovementsUseCase,
            _registerCashMovementUseCase,
            _getDailyCashSummaryUseCase);
        f.ShowDialog();
    }

    private void OpenInventory()
    {
        if (!Allow(UserPermission.Products))
            return;

        using var f = new InventoryForm(
            _listInventoryUseCase,
            _productRepository,
            _updateProductUseCase,
            _adjustStockUseCase,
            _deactivateProductUseCase,
            _getStockMovementsUseCase);
        f.ShowDialog(this);
        if (!f.CatalogChanged)
            return;

        TxtBuscador_TextChanged(this, EventArgs.Empty);
        RefreshStockAlerts();
    }

    private void OpenKardex(Product? product = null)
    {
        if (!Allow(UserPermission.Products))
            return;

        using var f = new KardexForm(_getStockMovementsUseCase, _productRepository, product);
        f.ShowDialog(this);
    }

    private void Form1_Shown(object? sender, EventArgs e)
    {
        if (_openingCashPromptShown)
            return;

        _openingCashPromptShown = true;
        if (_currentUser.Has(UserPermission.Products))
            OfferSeedCatalogIfNeeded();
        if (_currentUser.Has(UserPermission.Sell) || _currentUser.Has(UserPermission.Cash))
            PromptOpeningCashIfNeeded();
        _backupTimer.Start();
        RunScheduledBackup();
        _ = _cloudBackup.TryUploadPendingAsync();
        _ = TryRenewLicenseAsync();
    }

    private void RunScheduledBackup()
    {
        try
        {
            _localBackup.BackupIfDue();
        }
        catch (Exception)
        {
            // Un fallo del temporizador no debe interrumpir la venta.
        }
    }

    private async Task TryRenewLicenseAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenseApiSettings.BaseUrl))
            return;

        try
        {
            using var client = new FerreSys.Licensing.LicenseApiClient(LicenseApiSettings.BaseUrl);
            await _subscriptionService.TryRenewAsync(client);
            RefreshLicenseStatus();
        }
        catch
        {
            // Sin red, el periodo de gracia de la licencia local sigue aplicando.
        }
    }

    private void OpenLicense()
    {
        using var form = new LicenseForm(_subscriptionService);
        form.ShowDialog(this);
        RefreshLicenseStatus();
    }

    private void RefreshLicenseStatus()
    {
        if (lblLicense == null)
            return;

        bool paid = _subscriptionService.GetCurrentTier() == SubscriptionTier.Premium;
        lblLicense.Text = LicensePresentation.StatusBar(_subscriptionService);
        lblLicense.ForeColor = paid ? UiTheme.StatusOk : UiTheme.StatusWarn;
    }

    private void OfferSeedCatalogIfNeeded()
    {
        if (_appSettings.IsSeedOfferCompleted())
            return;

        if (_productRepository.GetTotalCount() > 0)
        {
            _appSettings.MarkSeedOfferCompleted();
            return;
        }

        bool loadCatalog = ResolveSeedChoice();
        if (loadCatalog)
        {
            int loaded = HardwareSeedCatalog.ApplyIfEmpty(_productRepository);
            SetStatus($"Catálogo inicial cargado: {loaded} productos");
            lblStatus.ForeColor = UiTheme.StatusOk;
        }

        _appSettings.MarkSeedOfferCompleted();
    }

    private bool ResolveSeedChoice()
    {
        if (_startupArgs.Any(arg => string.Equals(arg, "--with-seed", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (_startupArgs.Any(arg => string.Equals(arg, "--no-seed", StringComparison.OrdinalIgnoreCase)))
            return false;

        using var offer = new SeedOfferForm();
        return offer.ShowDialog(this) == DialogResult.OK && offer.LoadSampleCatalog;
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
        mnuEditProduct = menuEdit;

        var menuAudit = new ToolStripMenuItem { Text = "Auditar / Ajustar Stock" };
        menuAudit.Click += MenuAudit_Click;
        mnuAuditProduct = menuAudit;

        var menuKardex = new ToolStripMenuItem { Text = "Ver Kárdex" };
        menuKardex.Click += MenuKardex_Click;
        mnuKardexProduct = menuKardex;

        var menuHide = new ToolStripMenuItem { Text = "Quitar del mostrador" };
        menuHide.Click += MenuHide_Click;
        mnuHideProduct = menuHide;

        cmsSearchMenu.Items.Add(menuEdit);
        cmsSearchMenu.Items.Add(menuAudit);
        cmsSearchMenu.Items.Add(menuKardex);
        cmsSearchMenu.Items.Add(new ToolStripSeparator());
        cmsSearchMenu.Items.Add(menuHide);
        dgvSearch.ContextMenuStrip = cmsSearchMenu;

        dgvSearch.CellMouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvSearch.ClearSelection();
                dgvSearch.Rows[e.RowIndex].Selected = true;
                dgvSearch.CurrentCell = dgvSearch.Rows[e.RowIndex].Cells["Barcode"];
            }
        };
    }

    private void SetStatus(string message) => lblStatus.Text = message;

    private bool Allow(UserPermission permission, bool quiet = false)
    {
        if (_currentUser.Has(permission))
            return true;

        if (quiet)
            SetStatus("No tienes permiso para esta acción.");
        else
        {
            MessageBox.Show(
                "No tienes permiso para esta acción.",
                "Permiso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        return false;
    }

    private void ApplyAccess()
    {
        bool sell = _currentUser.Has(UserPermission.Sell);
        bool sales = _currentUser.Has(UserPermission.ViewSales);
        bool products = _currentUser.Has(UserPermission.Products);
        bool import = _currentUser.Has(UserPermission.ImportProducts);
        bool cash = _currentUser.Has(UserPermission.Cash);
        bool reports = _currentUser.Has(UserPermission.Reports);
        bool settings = _currentUser.Has(UserPermission.Settings);
        bool users = _currentUser.Has(UserPermission.ManageUsers);

        mnuSales.Visible = sales;
        btnViewSales.Visible = sales;
        mnuProfit.Visible = reports;
        btnGrossProfit.Visible = reports;
        mnuReports.Visible = reports;
        btnReports.Visible = reports;
        mnuSuggestion.Visible = reports;
        mnuSuggestionProducts.Visible = reports;
        btnPurchaseSuggestion.Visible = reports;
        mnuCashX.Visible = cash;
        btnCashCloseX.Visible = cash;
        mnuCashZ.Visible = cash;
        btnCashCloseZ.Visible = cash;
        mnuCashMovements.Visible = cash;
        btnCashMovements.Visible = cash;
        mnuImport.Visible = import;
        btnImportExcel.Visible = import;
        mnuBusiness.Visible = settings;
        btnBusiness.Visible = settings;
        mnuPeripherals.Visible = settings;
        btnPeripherals.Visible = settings;
        mnuUsers.Visible = users;
        btnUsers.Visible = users;
        mnuRegisterProduct.Visible = products;
        btnOpenAddProduct.Visible = products;
        mnuInventory.Visible = products;
        btnInventory.Visible = products;
        mnuKardex.Visible = products;
        btnKardex.Visible = products;
        mnuHideInventory.Visible = products;
        btnHideInventory.Visible = products;
        mnuProducts.Visible = products || reports;
        mnuSale.Visible = sell;
        btnCheckout.Visible = sell;
        btnPlus.Visible = sell;
        btnMinus.Visible = sell;
        btnClearCart.Visible = sell;
        btnNewCart.Visible = sell;
        btnCloseCart.Visible = sell;
        mnuEditProduct.Enabled = products;
        mnuAuditProduct.Enabled = products;
        mnuKardexProduct.Enabled = products;
        mnuHideProduct.Enabled = products;
    }

    private void OpenUsers()
    {
        if (!Allow(UserPermission.ManageUsers))
            return;

        using var form = new UsersForm(new SqliteUserRepository(_dbContext), _appSettings);
        form.ShowDialog(this);
    }

    private void Logout()
    {
        SessionEnded = true;
        Close();
    }

    private void RefreshStockAlerts()
    {
        var summary = _getReorderSuggestionsUseCase.Execute();
        if (summary.TotalItems == 0)
        {
            lblStockAlerts.Visible = false;
            lblStockAlerts.Text = "";
            return;
        }

        var parts = new List<string>();
        if (summary.OutOfStockCount > 0)
            parts.Add($"{summary.OutOfStockCount} agotado(s)");
        if (summary.BelowMinimumCount > 0)
            parts.Add($"{summary.BelowMinimumCount} bajo mínimo");

        lblStockAlerts.Text = " " + string.Join(" · ", parts);
        lblStockAlerts.ForeColor = summary.OutOfStockCount > 0 ? UiTheme.StatusDanger : UiTheme.StatusWarn;
        lblStockAlerts.LinkColor = lblStockAlerts.ForeColor;
        lblStockAlerts.VisitedLinkColor = lblStockAlerts.ForeColor;
        lblStockAlerts.ActiveLinkColor = lblStockAlerts.ForeColor;
        lblStockAlerts.Image = UiIcons.StatusDot(
            summary.OutOfStockCount > 0 ? UiTheme.StatusDanger : UiTheme.StatusWarn);
        lblStockAlerts.TextImageRelation = TextImageRelation.ImageBeforeText;
        lblStockAlerts.Visible = true;
    }

    private void RefreshSearchIfActive()
    {
        if (txtBuscador.Text.Trim().Length > 0)
            TxtBuscador_TextChanged(txtBuscador, EventArgs.Empty);
    }

    private static void ApplyStockAlertRowStyle(DataGridViewRow row, Product product)
    {
        row.DefaultCellStyle.BackColor = product.AlertLevel switch
        {
            StockAlertLevel.Critical => UiTheme.StockDanger,
            StockAlertLevel.Warning => UiTheme.StockWarn,
            _ => Color.White
        };
    }

    private void TxtBuscador_TextChanged(object? sender, EventArgs e)
    {
        string term = txtBuscador.Text.Trim();
        if (TryKeepLatestScan(term))
            return;

        ProductImages.ClearRows(dgvSearch);

        if (term.Length > 0)
        {
            List<Product> matches = _searchProductsUseCase.Execute(term);
            foreach (var p in matches)
            {
                int idx = dgvSearch.Rows.Add(
                    ProductImages.ForGrid(p.Image),
                    p.Barcode,
                    p.Name,
                    p.RetailPrice,
                    p.Unit.FormatQuantity(p.Stock));
                var row = dgvSearch.Rows[idx];
                row.Tag = p;
                ApplyStockAlertRowStyle(row, p);
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
        if (e.KeyCode != Keys.Enter)
            return;

        e.SuppressKeyPress = true;
        e.Handled = true;
        ApplyScannedCode(txtBuscador.Text);
    }

    private bool TryKeepLatestScan(string term)
    {
        if (_lastScanCode is not { Length: > 0 } previous
            || term.Length <= previous.Length
            || !term.StartsWith(previous, StringComparison.Ordinal))
            return false;

        string suffix = term[previous.Length..].Trim();
        if (suffix.Length == 0)
            return false;

        txtBuscador.Text = suffix;
        txtBuscador.SelectionStart = txtBuscador.Text.Length;
        return true;
    }

    private void ApplyScannedCode(string raw)
    {
        string code = raw.Trim();
        if (User.LooksLikeEmployeeBarcode(code))
        {
            SetStatus("Ese código es una credencial de empleado.");
            PrepareNextScan();
            return;
        }

        _lastScanCode = code.Length > 0 ? code : null;

        if (code.Length > 0 && !string.Equals(txtBuscador.Text.Trim(), code, StringComparison.Ordinal))
            txtBuscador.Text = code;

        if (code.Length > 0 && ScannerAddsToCart() && dgvSearch.Rows.Count == 1)
            AddToCart(dgvSearch.Rows[0]);

        PrepareNextScan();
    }

    private bool ScannerAddsToCart() =>
        _appSettings.GetPeripheralSettings().ScannerAddToCart;

    private void PrepareNextScan()
    {
        txtBuscador.Focus();
        if (txtBuscador.TextLength > 0)
            txtBuscador.SelectAll();
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
        if (!Allow(UserPermission.Sell, quiet: true))
            return;

        string barcode = CellText(row, "Barcode");
        string name = CellText(row, "Name");
        var product = _productRepository.GetByBarcode(barcode);
        if (product == null)
        {
            MessageBox.Show($"No se encontró el producto '{barcode}'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        decimal qtyToAdd = 1m;
        if (product.Unit.AllowsDecimals())
        {
            PeripheralSettings devices = _appSettings.GetPeripheralSettings();
            bool useScale = product.Unit == UnitOfMeasure.Kilogramo && devices.HasScaleConfigured;
            using var qtyForm = useScale
                ? new BulkQuantityForm(product.Name, product.Unit, product.Stock, devices.ScaleSerialPort, devices.ScaleBaudRate)
                : new BulkQuantityForm(product.Name, product.Unit, product.Stock);
            if (qtyForm.ShowDialog(this) != DialogResult.OK)
                return;
            qtyToAdd = qtyForm.Quantity;
        }

        foreach (DataGridViewRow cartRow in dgvCart.Rows)
        {
            if (CellText(cartRow, "Barcode") == barcode)
            {
                decimal currentQty = Convert.ToDecimal(cartRow.Cells["Qty"].Value);
                decimal newQty = currentQty + qtyToAdd;
                ApplyCartLinePricing(cartRow, product, newQty);
                CalculateTotal();
                SetStatus($"Actualizado en carrito: {name} ({product.Unit.FormatQuantity(newQty)})");
                return;
            }
        }

        int added = dgvCart.Rows.Add(
            ProductImages.ForGrid(product.Image),
            barcode,
            name,
            0m,
            FormatCartQty(qtyToAdd, product.Unit),
            0m);
        ApplyCartLinePricing(dgvCart.Rows[added], product, qtyToAdd);
        CalculateTotal();
        SetStatus($"Añadido al carrito: {name} ({product.Unit.FormatQuantity(qtyToAdd)})");
    }

    private static string FormatCartQty(decimal qty, UnitOfMeasure unit) =>
        unit.AllowsDecimals() ? qty.ToString("0.###") : ((int)qty).ToString();

    private static void ApplyCartLinePricing(DataGridViewRow row, Product product, decimal qty)
    {
        decimal price = product.ResolveUnitPrice(qty, DateOnly.FromDateTime(DateTime.Now));
        row.Cells["Price"].Value = price;
        row.Cells["Qty"].Value = FormatCartQty(qty, product.Unit);
        row.Cells["Subtotal"].Value = decimal.Round(qty * price, 2, MidpointRounding.AwayFromZero);
    }

    private void RefreshActiveCartPrices()
    {
        foreach (DataGridViewRow row in dgvCart.Rows)
        {
            if (row.IsNewRow)
                continue;

            var product = _productRepository.GetByBarcode(CellText(row, "Barcode"));
            if (product == null)
                continue;

            decimal qty = Convert.ToDecimal(row.Cells["Qty"].Value);
            ApplyCartLinePricing(row, product, qty);
        }

        CalculateTotal();
    }

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
        ProductImages.ClearRows(dgvCart);
        foreach (var line in session.Lines)
        {
            var product = _productRepository.GetByBarcode(line.Barcode);
            UnitOfMeasure unit = product?.Unit ?? UnitOfMeasure.Pieza;
            dgvCart.Rows.Add(
                ProductImages.ForGrid(product?.Image),
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
        if (!Allow(UserPermission.Sell, quiet: true))
            return;

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
        if (!Allow(UserPermission.Sell, quiet: true))
            return;

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
        if (!Allow(UserPermission.Sell, quiet: true))
            return;

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
        ProductImages.ClearRows(dgvCart);
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
        if (!Allow(UserPermission.Sell))
            return;

        try
        {
            if (dgvCart.Rows.Count == 0)
            {
                MessageBox.Show("El carrito está vacío.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshActiveCartPrices();

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
                    Barcode = CellText(row, "Barcode"),
                    Quantity = Convert.ToDecimal(row.Cells["Qty"].Value)
                });
            }

            Sale sale = _processSaleUseCase.Execute(
                items,
                method,
                reference,
                amountReceived,
                cardAmount,
                checkoutForm.DiscountAmount);
            PrintSaleTicket(sale);

            if (method == "EFECTIVO" || method == "MIXTO")
                SetStatus($"Venta cobrada. Cambio: ${sale.Change:N2}");
            else
                SetStatus("Venta cobrada con tarjeta");

            // Limpiar solo la sesión/tab activa; las demás ventas en espera se conservan
            ClearActiveCartSession();
            txtBuscador.Clear();
            ProductImages.ClearRows(dgvSearch);
            RefreshStockAlerts();
            txtBuscador.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error al Cobrar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnOpenAddProduct_Click(object? sender, EventArgs e)
    {
        if (!Allow(UserPermission.Products))
            return;

        using var addProductForm = new AddProductForm(_addProductUseCase);
        if (addProductForm.ShowDialog() == DialogResult.OK)
        {
            txtBuscador.Clear();
            SetStatus("Producto registrado");
            RefreshStockAlerts();
        }
    }

    private void PrintSaleTicket(Sale sale)
    {
        PeripheralSettings devices = _appSettings.GetPeripheralSettings();
        BusinessTicketProfile profile = _appSettings.GetTicketProfile();
        IThermalPrinter? printer = null;
        if (devices.HasPrinterConfigured)
        {
            try
            {
                printer = ThermalPrinterFactory.Create(devices);
            }
            catch (Exception ex)
            {
                ShowTicketFallback(TicketFormatter.GenerateTicketText(sale, profile), ex.Message);
                return;
            }
        }

        byte[]? logo = TicketLogoPrinter.TryEncode(_appSettings, profile);
        PrintSaleTicketResult result = _printSaleTicketUseCase.Execute(sale, profile, devices, printer, logo);
        if (!result.Success)
            ShowTicketFallback(result.TicketText, result.Error);
    }

    private static void ShowTicketFallback(string ticketText, string? error)
    {
        string body = string.IsNullOrWhiteSpace(error)
            ? ticketText
            : error + Environment.NewLine + Environment.NewLine + ticketText;
        MessageBox.Show(body, "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenBusinessProfile()
    {
        if (!Allow(UserPermission.Settings))
            return;

        using var form = new BusinessProfileForm(_appSettings, _subscriptionService, _localBackup);
        form.ShowDialog(this);
        RefreshBrand();
    }

    private void RefreshBrand()
    {
        string name = _appSettings.GetTicketProfile().BusinessName.Trim();
        string? logoPath = _appSettings.ResolveTicketLogoPath();
        bool hasName = name.Length > 0;
        bool hasLogo = false;
        Image? nextLogo = null;
        if (!string.IsNullOrWhiteSpace(logoPath))
        {
            try
            {
                using var source = Image.FromFile(logoPath);
                nextLogo = ScaleBrandLogo(source, 36);
                hasLogo = true;
            }
            catch (Exception)
            {
                nextLogo = null;
            }
        }

        Image? previous = _businessLogo;
        _businessLogo = nextLogo;
        lblBusinessLogo.Image = nextLogo;
        previous?.Dispose();

        lblBusinessName.Text = name;
        lblBrandDivider.Visible = hasName || hasLogo;
        lblBusinessLogo.Visible = hasLogo;
        lblBusinessName.Visible = hasName;
    }

    private static Bitmap ScaleBrandLogo(Image source, int size)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        float scale = Math.Min(size / (float)source.Width, size / (float)source.Height);
        int width = Math.Max(1, (int)(source.Width * scale));
        int height = Math.Max(1, (int)(source.Height * scale));
        int x = (size - width) / 2;
        int y = (size - height) / 2;
        graphics.DrawImage(source, x, y, width, height);
        return bitmap;
    }

    private void OpenPeripherals()
    {
        if (!Allow(UserPermission.Settings))
            return;

        StopSerialScanner();
        using var form = new PeripheralsForm(_appSettings);
        form.ShowDialog(this);
        ApplySerialScanner(notifyOnError: true);
    }

    private void ApplySerialScanner(bool notifyOnError)
    {
        StopSerialScanner();
        PeripheralSettings devices = _appSettings.GetPeripheralSettings();
        if (devices.ScannerMode != ScannerMode.Serial || string.IsNullOrWhiteSpace(devices.ScannerSerialPort))
            return;

        try
        {
            _serialScanner = new SerialBarcodeScanner();
            _serialScanner.BarcodeReceived += OnSerialBarcode;
            _serialScanner.Start(devices.ScannerSerialPort, devices.ScannerBaudRate);
            SetStatus($"Escáner serie en {devices.ScannerSerialPort}");
        }
        catch (Exception ex)
        {
            StopSerialScanner();
            SetStatus("Escáner serie no disponible");
            if (notifyOnError)
            {
                MessageBox.Show(
                    $"No se pudo abrir el escáner en {devices.ScannerSerialPort}.\n\n{ex.Message}",
                    "Escáner",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }

    private void OnSerialBarcode(object? sender, string code)
    {
        if (IsDisposed || string.IsNullOrWhiteSpace(code))
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() => OnSerialBarcode(sender, code));
            return;
        }

        if (OwnedForms.Any(form => form.Visible))
            return;

        ApplyScannedCode(code);
    }

    private void StopSerialScanner()
    {
        if (_serialScanner == null)
            return;

        _serialScanner.BarcodeReceived -= OnSerialBarcode;
        _serialScanner.Dispose();
        _serialScanner = null;
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        StopSerialScanner();
        _backupTimer.Stop();
        try
        {
            _localBackup.BackupOnClose();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo guardar el respaldo en Documentos\\Posly_Backups.\n\n{ex.Message}",
                "Respaldo local",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _cloudBackup.TryEnqueuePackage(_subscriptionService.PremiumLicenseId);
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

        if (e.KeyCode == Keys.F8)
        {
            OpenInventory();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            var confirmExit = MessageBox.Show(
                "¿Seguro que deseas cerrar Posly?",
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
        if (!Allow(UserPermission.Sell, quiet: true))
            return;

        if (dgvCart.CurrentRow == null) return;

        var row = dgvCart.CurrentRow;
        string barcode = CellText(row, "Barcode");
        var product = _productRepository.GetByBarcode(barcode);
        UnitOfMeasure unit = product?.Unit ?? UnitOfMeasure.Pieza;
        decimal step = unit.AllowsDecimals() ? 0.1m : 1m;

        decimal currentQty = Convert.ToDecimal(row.Cells["Qty"].Value);
        decimal newQty = currentQty + step;

        if (product != null)
            ApplyCartLinePricing(row, product, newQty);
        else
        {
            decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
            row.Cells["Qty"].Value = FormatCartQty(newQty, unit);
            row.Cells["Subtotal"].Value = newQty * price;
        }

        CalculateTotal();
    }

    private void BtnMinus_Click(object? sender, EventArgs e)
    {
        if (!Allow(UserPermission.Sell, quiet: true))
            return;

        if (dgvCart.CurrentRow == null) return;

        var row = dgvCart.CurrentRow;
        string barcode = CellText(row, "Barcode");
        var product = _productRepository.GetByBarcode(barcode);
        UnitOfMeasure unit = product?.Unit ?? UnitOfMeasure.Pieza;
        decimal step = unit.AllowsDecimals() ? 0.1m : 1m;

        decimal currentQty = Convert.ToDecimal(row.Cells["Qty"].Value);
        decimal newQty = currentQty - step;

        if (newQty <= 0)
        {
            dgvCart.Rows.Remove(row);
        }
        else if (product != null)
        {
            ApplyCartLinePricing(row, product, newQty);
        }
        else
        {
            decimal price = Convert.ToDecimal(row.Cells["Price"].Value);
            row.Cells["Qty"].Value = FormatCartQty(newQty, unit);
            row.Cells["Subtotal"].Value = newQty * price;
        }

        CalculateTotal();
    }

    private void BtnClearCart_Click(object? sender, EventArgs e)
    {
        if (!Allow(UserPermission.Sell))
            return;

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
        if (!Allow(UserPermission.Products))
            return;

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
                    RefreshStockAlerts();
                }
            }
        }
    }

    private void BtnImportExcel_Click(object? sender, EventArgs e)
    {
        if (!Allow(UserPermission.ImportProducts))
            return;
        using var openFileDialog = new OpenFileDialog { Filter = "Archivos CSV (*.csv)|*.csv", Title = "Seleccionar inventario en formato CSV" };
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                ImportProductsResult result = _importProductsUseCase.Execute(openFileDialog.FileName);

                if (result.Products.Count > 0)
                {
                    var movements = result.Products
                        .Select(p => StockMovementFactory.Create(
                            p,
                            StockMovementKind.CargaInicial,
                            quantityBefore: 0m,
                            quantityDelta: p.Stock,
                            quantityAfter: p.Stock,
                            notes: "Importación CSV"))
                        .ToList();
                    _productRepository.SaveBulkWithStockMovements(result.Products, movements);
                }

                if (result.SkippedByLimit > 0 && result.Products.Count == 0)
                {
                    MessageBox.Show(
                        $"Has alcanzado el límite de {FreeTierLimits.ProductLimit} productos de la prueba gratuita.\n\n" +
                        $"{result.SkippedByLimit} filas del archivo no se importaron.\n\n" +
                        FreeTierLimits.PremiumUpsell,
                        "Límite de la prueba gratuita",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    SetStatus("Importación detenida: límite de productos alcanzado");
                }
                else if (result.SkippedByLimit > 0)
                {
                    MessageBox.Show(
                        $"Se han registrado {result.Products.Count} productos.\n\n" +
                        $"{result.SkippedByLimit} filas no se importaron porque superan el límite de {FreeTierLimits.ProductLimit} de la prueba gratuita.\n\n" +
                        FreeTierLimits.PremiumUpsell,
                        "Importador de Inventario",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    TxtBuscador_TextChanged(this, EventArgs.Empty);
                    SetStatus($"Importados {result.Products.Count} productos; {result.SkippedByLimit} omitidos por el límite");
                    RefreshStockAlerts();
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
                    RefreshStockAlerts();
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
        if (!Allow(UserPermission.Products))
            return;

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
                    RefreshStockAlerts();
                }
            }
        }
    }

    private void MenuHide_Click(object? sender, EventArgs e)
    {
        if (!Allow(UserPermission.Products))
            return;

        if (dgvSearch.CurrentRow == null)
            return;

        string barcode = CellText(dgvSearch.CurrentRow, "Barcode");
        var product = _productRepository.GetByBarcode(barcode);
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
            TxtBuscador_TextChanged(this, EventArgs.Empty);
            SetStatus("Producto oculto del mostrador");
            RefreshStockAlerts();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo ocultar el producto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void HideAllInventory()
    {
        if (!Allow(UserPermission.Products))
            return;

        var confirm = MessageBox.Show(
            "¿Ocultar todo el inventario del mostrador?\n\nLos productos dejarán de aparecer en búsquedas y en el lector. Las ventas y el kárdex se conservan. Los carritos abiertos se vacían.",
            "Ocultar inventario",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
            return;

        try
        {
            int hidden = _deactivateProductUseCase.ExecuteAll();
            foreach (CartSession session in _cartSessions)
                session.Lines.Clear();
            ProductImages.ClearRows(dgvCart);
            CalculateTotal();
            txtBuscador.Clear();
            ProductImages.ClearRows(dgvSearch);
            SetStatus(hidden == 0
                ? "No había productos visibles en el mostrador"
                : $"Inventario oculto: {hidden} productos");
            RefreshStockAlerts();
            txtBuscador.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo ocultar el inventario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void MenuKardex_Click(object? sender, EventArgs e)
    {
        if (!Allow(UserPermission.Products))
            return;

        if (dgvSearch.CurrentRow == null)
            return;

        string barcode = CellText(dgvSearch.CurrentRow, "Barcode");
        var product = _productRepository.GetByBarcode(barcode);
        OpenKardex(product);
    }

    private static string CellText(DataGridViewRow row, string columnName) =>
        row.Cells[columnName]?.Value?.ToString() ?? string.Empty;

    private static string CellText(DataGridViewRow row, int columnIndex) =>
        row.Cells[columnIndex].Value?.ToString() ?? string.Empty;
}
