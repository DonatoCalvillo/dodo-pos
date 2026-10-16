using System.Drawing.Drawing2D;
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
    private Label _lblSearchCount = null!;
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
    private ToolStripDropDownButton _ddCash = null!;
    private ToolStripDropDownButton _ddCatalog = null!;
    private ToolStripDropDownButton _ddReports = null!;
    private ToolStripDropDownButton _ddSystem = null!;
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
    private ToolStripMenuItem mnuLoyalty = null!;
    private ToolStripMenuItem mnuProducts = null!;
    private ToolStripMenuItem mnuRegisterProduct = null!;
    private ToolStripMenuItem mnuSuggestionProducts = null!;
    private ToolStripMenuItem mnuInventory = null!;
    private ToolStripMenuItem mnuKardex = null!;
    private ToolStripMenuItem mnuSuppliers = null!;
    private ToolStripMenuItem mnuPurchaseOrders = null!;
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
    private readonly SqliteLoyaltyRepository _loyalty;
    private readonly SqliteSupplierRepository _suppliers;
    private readonly SqlitePurchaseOrderRepository _purchaseOrders;
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
        _appSettings = new SqliteAppSettingsStore(_dbContext);
        _loyalty = new SqliteLoyaltyRepository(_dbContext);
        _suppliers = new SqliteSupplierRepository(_dbContext);
        _purchaseOrders = new SqlitePurchaseOrderRepository(_dbContext);
        _subscriptionService = subscription;
        _addProductUseCase = new AddProductUseCase(_productRepository, _subscriptionService);
        _searchProductsUseCase = new SearchProductsUseCase(_productRepository);
        _processSaleUseCase = new ProcessSaleUseCase(_productRepository, _loyalty, _appSettings);
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
        _cloudBackup = new CloudBackupService(_dbContext, new UnconfiguredCloudBackupStore());
        _localBackup = new LocalBackupService(_dbContext, _appSettings);
        _backupTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _backupTimer.Tick += (_, _) => RunScheduledBackup();
        _printSaleTicketUseCase = new PrintSaleTicketUseCase();

        var version = typeof(Program).Assembly.GetName().Version;
        var versionLabel = version is null ? "" : $" v{version.ToString(3)}";
        this.Text = $"Posly - Punto de Venta{versionLabel}";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.DoubleBuffered = true;
        this.BackColor = UiTheme.WindowBg;

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
        var headerBar = CreateToolbar();
        var statusBar = CreateStatusBar();

        // Main container: Row 0 = Compact Search Ribbon (58px), Row 1 = Responsive 2-Column Workspace (100%)
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12, 10, 12, 8),
            BackColor = UiTheme.WindowBg
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        root.Controls.Add(CreateTopSearchRibbon(), 0, 0);
        root.Controls.Add(CreateMainWorkspace(), 0, 1);

        this.Controls.Add(headerBar);
        this.Controls.Add(statusBar);
        this.Controls.Add(root);
        headerBar.SendToBack();
        statusBar.SendToBack();
        root.BringToFront();

        SetupSearchContextMenu();
        CalculateTotal();
        RefreshStockAlerts();
        ApplyAccess();
        ApplySerialScanner(notifyOnError: false);
        txtBuscador.Focus();
    }

    private ToolStrip CreateToolbar()
    {
        var strip = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(20, 20),
            Font = UiTheme.Toolbar,
            BackColor = UiTheme.HeaderBg,
            ForeColor = Color.White,
            Padding = new Padding(10, 6, 10, 6),
            AutoSize = false,
            Height = 52,
            Renderer = new ModernHeaderStripRenderer()
        };

        // --- LEFT: BRAND & BUSINESS IDENTITY ---
        lblBrand = new ToolStripLabel
        {
            Image = ScaleBrandLogo(Branding.Logo, 34),
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageScaling = ToolStripItemImageScaling.None,
            AutoSize = false,
            Size = new Size(38, 38),
            Margin = new Padding(2, 0, 4, 0),
            Text = string.Empty
        };
        lblBrandName = new ToolStripLabel("POSLY")
        {
            Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 6, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };
        lblBrandDivider = new ToolStripLabel("|")
        {
            Font = new Font("Segoe UI", 12f, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(6, 0, 6, 0),
            Visible = false
        };
        lblBusinessLogo = new ToolStripLabel
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            ImageScaling = ToolStripItemImageScaling.None,
            AutoSize = false,
            Size = new Size(34, 34),
            Margin = new Padding(4, 0, 4, 0),
            Visible = false
        };
        lblBusinessName = new ToolStripLabel
        {
            Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(56, 189, 248),
            Margin = new Padding(2, 0, 12, 0),
            Visible = false
        };

        // Keep legacy ToolStripButton fields instantiated so existing permission logic never hits null
        btnViewSales = CreateHiddenToolButton();
        btnGrossProfit = CreateHiddenToolButton();
        btnReports = CreateHiddenToolButton();
        btnPurchaseSuggestion = CreateHiddenToolButton();
        btnCashCloseX = CreateHiddenToolButton();
        btnCashCloseZ = CreateHiddenToolButton();
        btnCashMovements = CreateHiddenToolButton();
        btnKardex = CreateHiddenToolButton();
        btnImportExcel = CreateHiddenToolButton();
        btnOpenAddProduct = CreateHiddenToolButton();
        btnHideInventory = CreateHiddenToolButton();
        btnBusiness = CreateHiddenToolButton();
        btnPeripherals = CreateHiddenToolButton();
        btnUsers = CreateHiddenToolButton();
        mnuProducts = new ToolStripMenuItem();
        mnuSale = new ToolStripMenuItem();

        // --- RIGHT SIDE: CLEAN GROUPED NAVIGATION MODULES (Farmacia Guadalajara / Sánchez Style) ---

        // 1. Direct Quick-Consult Button: Inventario (F8)
        btnInventory = new ToolStripButton
        {
            Text = "  Inventario (F8)",
            Image = UiIcons.Kardex(20),
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            AutoSize = true,
            ForeColor = Color.White,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(4, 2, 4, 2),
            Alignment = ToolStripItemAlignment.Right
        };
        btnInventory.Click += (_, _) => OpenInventory();
        mnuInventory = new ToolStripMenuItem("Consultar inventario (F8)", UiIcons.Kardex(18), (_, _) => OpenInventory());

        // 2. Caja y Ventas Dropdown
        mnuCashX = new ToolStripMenuItem("Corte X (Arqueo en vivo)...", UiIcons.CashClose(18), (_, _) => OpenCashClose(CashCutMode.X));
        mnuCashZ = new ToolStripMenuItem("Corte Z (Cierre de turno)...", UiIcons.CashClose(18), (_, _) => OpenCashClose(CashCutMode.Z));
        mnuCashMovements = new ToolStripMenuItem("Entradas y salidas de caja...", UiIcons.CashMovements(18), (_, _) => OpenCashMovements());
        mnuSales = new ToolStripMenuItem("Historial de ventas y devoluciones...", UiIcons.Sales(18), (_, _) => OpenSales());

        _ddCash = CreateHeaderDropDown("Caja y Turno", UiIcons.CashClose(20), [
            mnuCashX,
            mnuCashZ,
            mnuCashMovements,
            new ToolStripSeparator(),
            mnuSales
        ]);

        // 3. Catálogo Dropdown
        mnuRegisterProduct = new ToolStripMenuItem("Registrar nuevo producto...", UiIcons.AddProduct(18), BtnOpenAddProduct_Click);
        mnuSuggestion = new ToolStripMenuItem("Sugerido de compra (Faltantes)...", UiIcons.Reorder(18), (_, _) => OpenPurchaseSuggestion());
        mnuSuppliers = new ToolStripMenuItem("Proveedores...", UiIcons.Business(18), (_, _) => OpenSuppliers());
        mnuPurchaseOrders = new ToolStripMenuItem("Órdenes de compra...", UiIcons.Sales(18), (_, _) => OpenPurchaseOrders());
        mnuSuggestionProducts = mnuSuggestion;
        mnuKardex = new ToolStripMenuItem("Kárdex de movimientos...", UiIcons.Kardex(18), (_, _) => OpenKardex());
        mnuImport = new ToolStripMenuItem("Importar catálogo (CSV)...", UiIcons.Import(18), BtnImportExcel_Click);
        mnuHideInventory = new ToolStripMenuItem("Ocultar inventario del mostrador...", UiIcons.Clear(18), (_, _) => HideAllInventory());

        _ddCatalog = CreateHeaderDropDown("Catálogo", UiIcons.AddProduct(20), [
            mnuRegisterProduct,
            mnuSuggestion,
            mnuSuppliers,
            mnuPurchaseOrders,
            mnuKardex,
            mnuImport,
            new ToolStripSeparator(),
            mnuHideInventory
        ]);

        // 4. Reportes Dropdown
        mnuReports = new ToolStripMenuItem("Reportes operativos...", UiIcons.Sales(18), (_, _) => OpenReports());
        mnuProfit = new ToolStripMenuItem("Utilidad bruta y ganancia neta...", UiIcons.GrossProfit(18), (_, _) => OpenGrossProfitReport());

        _ddReports = CreateHeaderDropDown("Reportes", UiIcons.GrossProfit(20), [
            mnuReports,
            mnuProfit
        ]);

        // 5. Sistema / Sesión Dropdown (Rightmost)
        mnuBusiness = new ToolStripMenuItem("Datos del negocio y ticket...", UiIcons.Business(18), (_, _) => OpenBusinessProfile());
        mnuPeripherals = new ToolStripMenuItem("Periféricos e impresora...", UiIcons.Printer(18), (_, _) => OpenPeripherals());
        mnuUsers = new ToolStripMenuItem("Usuarios y permisos...", UiIcons.Sales(18), (_, _) => OpenUsers());
        mnuLoyalty = new ToolStripMenuItem("Fidelidad...", UiIcons.Sales(18), (_, _) => OpenLoyalty());
        var mnuLic = new ToolStripMenuItem("Suscripción y licencia...", null, (_, _) => OpenLicense());
        var mnuLogout = new ToolStripMenuItem($"Cerrar turno ({_currentUser.DisplayName})", null, (_, _) => Logout());
        var mnuExit = new ToolStripMenuItem("Salir de Posly (Esc)", UiIcons.Clear(18), (_, _) => Close());

        _ddSystem = CreateHeaderDropDown($"Sistema · {_currentUser.DisplayName}", UiIcons.Business(20), [
            mnuBusiness,
            mnuPeripherals,
            mnuUsers,
            mnuLoyalty,
            mnuLic,
            new ToolStripSeparator(),
            mnuLogout,
            mnuExit
        ]);

        // Add Right-aligned items in reverse visual order so Sistema is rightmost
        strip.Items.AddRange([
            lblBrand,
            lblBrandName,
            lblBrandDivider,
            lblBusinessLogo,
            lblBusinessName,
            _ddSystem,
            _ddReports,
            _ddCatalog,
            _ddCash,
            btnInventory
        ]);

        RefreshBrand();
        return strip;
    }

    private static ToolStripButton CreateHiddenToolButton() => new() { Visible = false };

    private static ToolStripDropDownButton CreateHeaderDropDown(string text, Image icon, ToolStripItem[] items)
    {
        var dd = new ToolStripDropDownButton
        {
            Text = " " + text,
            Image = icon,
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            AutoSize = true,
            ForeColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(3, 2, 3, 2),
            Alignment = ToolStripItemAlignment.Right
        };
        dd.DropDownItems.AddRange(items);
        return dd;
    }

    private Panel CreateTopSearchRibbon()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(12, 7, 12, 7),
            BackColor = UiTheme.PanelBg
        };
        bar.Paint += PaintCardBorder;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Search input box with border & icon
        var searchBoxContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.FromArgb(248, 250, 252),
            Cursor = Cursors.IBeam
        };

        txtBuscador = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Regular),
            ForeColor = UiTheme.Text,
            BackColor = Color.FromArgb(248, 250, 252),
            PlaceholderText = "Escanee código de barras o busque producto por descripción..."
        };
        txtBuscador.TextChanged += TxtBuscador_TextChanged;
        txtBuscador.KeyDown += TxtBuscador_KeyDown;
        txtBuscador.GotFocus += (_, _) =>
        {
            txtBuscador.BackColor = Color.White;
            searchBoxContainer.BackColor = Color.White;
            searchBoxContainer.Invalidate();
        };
        txtBuscador.LostFocus += (_, _) =>
        {
            var bg = Color.FromArgb(248, 250, 252);
            txtBuscador.BackColor = bg;
            searchBoxContainer.BackColor = bg;
            searchBoxContainer.Invalidate();
        };
        searchBoxContainer.Click += (_, _) => txtBuscador.Focus();

        void UpdateSearchBoxGeometry()
        {
            int th = txtBuscador.PreferredHeight;
            int w = Math.Max(60, searchBoxContainer.ClientSize.Width - 46);
            int top = Math.Max(2, (searchBoxContainer.ClientSize.Height - th) / 2);
            txtBuscador.SetBounds(36, top, w, th);
        }
        searchBoxContainer.Resize += (_, _) => UpdateSearchBoxGeometry();
        searchBoxContainer.HandleCreated += (_, _) => UpdateSearchBoxGeometry();

        searchBoxContainer.Paint += (s, e) =>
        {
            if (s is not Control c) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            bool focused = txtBuscador.Focused;
            using var pen = new Pen(focused ? UiTheme.BrandPrimary : UiTheme.Border, focused ? 1.8f : 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);

            // Search / Barcode icon on the left
            using var iconPen = new Pen(focused ? UiTheme.BrandPrimary : UiTheme.TextMuted, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            int cy = c.Height / 2;
            e.Graphics.DrawEllipse(iconPen, 11, cy - 7, 12, 12);
            e.Graphics.DrawLine(iconPen, 21, cy + 3, 26, cy + 8);
        };
        searchBoxContainer.Controls.Add(txtBuscador);

        // Compact Shortcut Pills (Replaces the bulky "Indicaciones" groupbox)
        var shortcutsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(0, 4, 4, 0),
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        shortcutsFlow.Controls.Add(CreateShortcutBadge("F8", "Inventario"));
        shortcutsFlow.Controls.Add(CreateShortcutBadge("F7", "Nueva espera"));
        shortcutsFlow.Controls.Add(CreateShortcutBadge("F5", "Cobrar"));
        shortcutsFlow.Controls.Add(CreateShortcutBadge("Enter / 2x Clic", "Agregar"));

        var helpHolder = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        UiHelp.Attach(helpHolder, "Búsqueda de producto", SectionHelpText.Search);

        layout.Controls.Add(searchBoxContainer, 0, 0);
        layout.Controls.Add(shortcutsFlow, 1, 0);
        layout.Controls.Add(helpHolder, 2, 0);

        bar.Controls.Add(layout);
        return bar;
    }

    private static Control CreateShortcutBadge(string key, string label)
    {
        var pill = new Label
        {
            AutoSize = true,
            Text = $"{key}: {label}",
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            ForeColor = UiTheme.TextMuted,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(6, 4, 6, 4),
            Margin = new Padding(4, 2, 0, 2)
        };
        return pill;
    }

    private TableLayoutPanel CreateMainWorkspace()
    {
        // 2-Column Responsive Split:
        // Left (74%): Catalog Results (42%) + Active Shopping Cart (58%)
        // Right (26%): Unified Digital Total Display + Compact Cart Controls + Giant Checkout CTA
        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = UiTheme.WindowBg
        };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 74f));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var leftSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 6, 0),
            BackColor = UiTheme.WindowBg
        };
        leftSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        leftSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
        leftSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));

        leftSplit.Controls.Add(CreateCatalogCard(), 0, 0);
        leftSplit.Controls.Add(CreateCartCard(), 0, 1);

        workspace.Controls.Add(leftSplit, 0, 0);
        workspace.Controls.Add(CreateRightCashierTower(), 1, 0);

        return workspace;
    }

    private Panel CreateCatalogCard()
    {
        dgvSearch = CreateGrid();
        ProductImages.AddPhotoColumn(dgvSearch);
        dgvSearch.Columns.Add("Barcode", "Código");
        dgvSearch.Columns.Add("Name", "Descripción del producto");
        dgvSearch.Columns.Add("Price", "Precio");
        dgvSearch.Columns.Add("Stock", "Existencia");

        dgvSearch.Columns["Barcode"]!.FillWeight = 22;
        dgvSearch.Columns["Name"]!.FillWeight = 46;
        dgvSearch.Columns["Price"]!.FillWeight = 16;
        dgvSearch.Columns["Price"]!.DefaultCellStyle.Format = "C2";
        dgvSearch.Columns["Price"]!.DefaultCellStyle.Font = UiTheme.UiBold;
        dgvSearch.Columns["Stock"]!.FillWeight = 16;
        dgvSearch.DoubleClick += DgvSearch_DoubleClick;

        _lblSearchCount = new Label
        {
            Text = "Catálogo listo",
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = UiTheme.TextMuted,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 8, 0)
        };

        return CreateModernCard(
            title: "RESULTADOS DEL CATÁLOGO",
            helpTitle: "Resultados del catálogo",
            helpText: SectionHelpText.CatalogResults,
            body: dgvSearch,
            margin: new Padding(0, 0, 0, 8),
            headerExtra: _lblSearchCount);
    }

    private Panel CreateCartCard()
    {
        var cartContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        cartContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cartContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        cartContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        tabsCarts = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
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
        dgvCart.Columns.Add("Name", "Producto en ticket");
        dgvCart.Columns.Add("Price", "P. Unit.");
        dgvCart.Columns.Add("Qty", "Cant.");
        dgvCart.Columns.Add("Subtotal", "Importe");

        dgvCart.Columns["Barcode"]!.FillWeight = 20;
        dgvCart.Columns["Name"]!.FillWeight = 42;
        dgvCart.Columns["Price"]!.FillWeight = 14;
        dgvCart.Columns["Price"]!.DefaultCellStyle.Format = "C2";
        dgvCart.Columns["Qty"]!.FillWeight = 10;
        dgvCart.Columns["Qty"]!.DefaultCellStyle.Font = UiTheme.UiBold;
        dgvCart.Columns["Subtotal"]!.FillWeight = 14;
        dgvCart.Columns["Subtotal"]!.DefaultCellStyle.Format = "C2";
        dgvCart.Columns["Subtotal"]!.DefaultCellStyle.Font = UiTheme.UiBold;

        cartContainer.Controls.Add(tabsCarts, 0, 0);
        cartContainer.Controls.Add(dgvCart, 0, 1);

        EnsureInitialCartSession();

        return CreateModernCard(
            title: "TICKET DE VENTA ACTIVO",
            helpTitle: "Carrito de compra actual",
            helpText: SectionHelpText.Cart,
            body: cartContainer,
            margin: new Padding(0));
    }

    private Panel CreateRightCashierTower()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 0, 0),
            Padding = new Padding(12),
            BackColor = UiTheme.PanelBg
        };
        card.Paint += PaintCardBorder;

        // Single responsive column that works on 768p and 1080p without clipping
        var tower = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        tower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tower.RowStyles.Add(new RowStyle(SizeType.Absolute, 146f)); // 0: Digital Total Visor
        tower.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // 1: Section Label "CONTROL DE RENGLÓN"
        tower.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));  // 2: [+1] and [-1] side-by-side
        tower.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // 3: [Vaciar ticket]
        tower.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));  // 4: Section Label "VENTAS EN ESPERA"
        tower.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));  // 5: [Nueva F7] and [Cerrar] side-by-side
        tower.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // 6: Elastic spacer (absorbs resolution differences)
        tower.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));  // 7: Primary CTA [COBRAR VENTA (F5)]

        // 0. Digital Total Visor (Farmacia Guadalajara / POS Display style)
        var visor = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(14, 10, 14, 10),
            BackColor = UiTheme.HeaderBg
        };

        var visorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        visorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        visorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        visorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
        visorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lblCartSummary = new Label
        {
            Text = "VENTA 1  ·  Artículos: 0  ·  Líneas: 0",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        var lblTotalCaption = new Label
        {
            Text = "TOTAL A PAGAR",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(56, 189, 248),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0)
        };

        lblTotal = new Label
        {
            Text = "$0.00",
            Dock = DockStyle.Fill,
            Font = UiTheme.TotalFont,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true,
            Margin = new Padding(0)
        };

        visorLayout.Controls.Add(lblCartSummary, 0, 0);
        visorLayout.Controls.Add(lblTotalCaption, 0, 1);
        visorLayout.Controls.Add(lblTotal, 0, 2);
        visor.Controls.Add(visorLayout);

        // 2. [+1] / [-1] Side-by-Side Grid
        var qtyPair = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = UiTheme.PanelBg
        };
        qtyPair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        qtyPair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        qtyPair.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        btnPlus = CreateActionButton("+1 Cantidad", UiIcons.Plus());
        btnPlus.Margin = new Padding(0, 0, 4, 0);
        btnPlus.Click += BtnPlus_Click;

        btnMinus = CreateActionButton("-1 Cantidad", UiIcons.Minus());
        btnMinus.Margin = new Padding(4, 0, 0, 0);
        btnMinus.Click += BtnMinus_Click;

        qtyPair.Controls.Add(btnPlus, 0, 0);
        qtyPair.Controls.Add(btnMinus, 1, 0);

        // 3. Vaciar Ticket
        btnClearCart = CreateActionButton("Vaciar ticket actual", UiIcons.Clear());
        btnClearCart.Margin = new Padding(0, 2, 0, 4);
        btnClearCart.ForeColor = UiTheme.StatusDanger;
        btnClearCart.Click += BtnClearCart_Click;

        // 5. Hold Sales Side-by-Side Grid
        var holdPair = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        holdPair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        holdPair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        holdPair.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        btnNewCart = CreateActionButton("Nueva (F7)", UiIcons.NewSale());
        btnNewCart.Margin = new Padding(0, 0, 4, 0);
        btnNewCart.Click += (_, _) => CreateNewCartSession();

        btnCloseCart = CreateActionButton("Cerrar", UiIcons.CloseSale());
        btnCloseCart.Margin = new Padding(4, 0, 0, 0);
        btnCloseCart.Click += (_, _) => CloseActiveCartSession();

        holdPair.Controls.Add(btnNewCart, 0, 0);
        holdPair.Controls.Add(btnCloseCart, 1, 0);

        // 7. Primary Checkout CTA Button
        btnCheckout = new Button
        {
            Text = "  COBRAR VENTA (F5)",
            Image = UiIcons.Checkout(22),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.CheckoutGreen,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(12, 0, 12, 0),
            UseVisualStyleBackColor = false
        };
        btnCheckout.FlatAppearance.BorderSize = 0;
        btnCheckout.FlatAppearance.MouseOverBackColor = UiTheme.CheckoutGreenHover;
        btnCheckout.Click += BtnCheckout_Click;

        tower.Controls.Add(visor, 0, 0);
        tower.Controls.Add(CreateActionSectionLabel("Ajuste de renglón"), 0, 1);
        tower.Controls.Add(qtyPair, 0, 2);
        tower.Controls.Add(btnClearCart, 0, 3);
        tower.Controls.Add(CreateActionSectionLabel("Tickets en espera"), 0, 4);
        tower.Controls.Add(holdPair, 0, 5);
        tower.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.PanelBg }, 0, 6);
        tower.Controls.Add(btnCheckout, 0, 7);

        card.Controls.Add(tower);
        return card;
    }

    private static Panel CreateModernCard(
        string title,
        string helpTitle,
        string helpText,
        Control body,
        Padding margin,
        Control? headerExtra = null)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = margin,
            Padding = new Padding(1, 1, 1, 1),
            BackColor = UiTheme.PanelBg
        };
        card.Paint += PaintCardBorder;

        var cardGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = UiTheme.PanelBg
        };
        cardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cardGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        cardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(14, 0, 36, 0),
            BackColor = Color.FromArgb(248, 250, 252)
        };
        header.Paint += (s, e) =>
        {
            if (s is not Control c) return;
            using var accentBrush = new SolidBrush(UiTheme.BrandPrimary);
            e.Graphics.FillRectangle(accentBrush, 0, 8, 4, c.Height - 16);
            using var linePen = new Pen(UiTheme.Border, 1f);
            e.Graphics.DrawLine(linePen, 0, c.Height - 1, c.Width, c.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = title,
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = UiTheme.GroupTitle,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        header.Controls.Add(lblTitle);

        if (headerExtra != null)
        {
            headerExtra.Dock = DockStyle.Right;
            headerExtra.Padding = new Padding(0, 8, 8, 0);
            header.Controls.Add(headerExtra);
        }

        UiHelp.Attach(header, helpTitle, helpText);

        var bodyWrap = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(8, 6, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        body.Dock = DockStyle.Fill;
        bodyWrap.Controls.Add(body);

        cardGrid.Controls.Add(header, 0, 0);
        cardGrid.Controls.Add(bodyWrap, 0, 1);
        card.Controls.Add(cardGrid);
        return card;
    }

    private static void PaintCardBorder(object? sender, PaintEventArgs e)
    {
        if (sender is not Control c)
            return;
        using var pen = new Pen(UiTheme.Border, 1f);
        e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
    }

    private StatusStrip CreateStatusBar()
    {
        var status = new StatusStrip
        {
            BackColor = Color.White,
            Font = UiTheme.Ui,
            SizingGrip = false,
            Padding = new Padding(8, 2, 8, 2)
        };

        lblStatus = new ToolStripStatusLabel("Listo")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.Text
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

        var connected = new ToolStripStatusLabel(" Terminal activa")
        {
            Image = UiIcons.StatusDot(UiTheme.StatusOk),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ForeColor = UiTheme.TextMuted,
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

        lblSignedIn = new ToolStripStatusLabel($"Cajero: {_currentUser.DisplayName}")
        {
            ForeColor = UiTheme.TextMuted,
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            BorderStyle = Border3DStyle.Etched
        };

        status.Items.Add(lblStatus);
        status.Items.Add(lblSignedIn);
        status.Items.Add(lblLicense);
        status.Items.Add(lblStockAlerts);
        status.Items.Add(connected);
        status.Items.Add(new ToolStripStatusLabel("Powered by Do&&Do")
        {
            Image = Branding.Mark,
            ForeColor = UiTheme.TextMuted,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            BorderStyle = Border3DStyle.Etched
        });

        return status;
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            ColumnHeadersHeight = 34,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowTemplate = { Height = 36 },
            Font = UiTheme.Ui,
            ForeColor = UiTheme.Text,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = UiTheme.UiBold,
                Padding = new Padding(6, 0, 6, 0),
                SelectionBackColor = Color.FromArgb(241, 245, 249),
                SelectionForeColor = Color.FromArgb(71, 85, 105)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = UiTheme.Text,
                Padding = new Padding(6, 0, 6, 0),
                SelectionBackColor = Color.FromArgb(224, 242, 254),
                SelectionForeColor = Color.FromArgb(15, 23, 42)
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = UiTheme.Text,
                Padding = new Padding(6, 0, 6, 0),
                SelectionBackColor = Color.FromArgb(224, 242, 254),
                SelectionForeColor = Color.FromArgb(15, 23, 42)
            },
            GridColor = Color.FromArgb(226, 232, 240)
        };
        return grid;
    }

    private static Label CreateActionSectionLabel(string text)
    {
        return new Label
        {
            Text = text.ToUpperInvariant(),
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = UiTheme.TextMuted,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0),
            Padding = new Padding(2, 0, 0, 2)
        };
    }

    private static Button CreateActionButton(string text, Image icon)
    {
        var btn = new Button
        {
            Text = " " + text,
            Image = icon,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = UiTheme.Text,
            Font = UiTheme.UiBold,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 4),
            Padding = new Padding(8, 0, 8, 0),
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderColor = UiTheme.Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240);
        return btn;
    }

    private sealed class ModernHeaderStripRenderer : ToolStripProfessionalRenderer
    {
        public ModernHeaderStripRenderer() : base(new HeaderColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // No 3D border on the main top bar
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            RenderHeaderPill(e.Graphics, e.Item);
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            RenderHeaderPill(e.Graphics, e.Item);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            if (e.Item?.Owner is ToolStrip && e.Item.Owner is not ToolStripDropDownMenu)
            {
                e.ArrowColor = Color.FromArgb(148, 163, 184);
            }
            base.OnRenderArrow(e);
        }

        private static void RenderHeaderPill(Graphics g, ToolStripItem item)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 2, item.Width - 3, item.Height - 5);
            Color fill = item.Pressed
                ? Color.FromArgb(0, 149, 217)
                : item.Selected
                    ? Color.FromArgb(30, 41, 59)
                    : Color.FromArgb(22, 32, 50);
            Color border = item.Selected || item.Pressed
                ? Color.FromArgb(56, 189, 248)
                : Color.FromArgb(51, 65, 85);

            using var path = LoginForm.RoundedRect(rect, 6);
            using var brush = new SolidBrush(fill);
            using var pen = new Pen(border, 1f);
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        private sealed class HeaderColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color ImageMarginGradientBegin => Color.White;
            public override Color ImageMarginGradientMiddle => Color.White;
            public override Color ImageMarginGradientEnd => Color.White;
            public override Color MenuBorder => UiTheme.Border;
            public override Color MenuItemBorder => Color.FromArgb(186, 230, 253);
            public override Color MenuItemSelected => Color.FromArgb(240, 249, 255);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(240, 249, 255);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(240, 249, 255);
            public override Color SeparatorDark => UiTheme.Border;
            public override Color SeparatorLight => Color.White;
        }
    }

    private void OpenSales()
    {
        if (!Allow(UserPermission.ViewSales))
            return;

        using var f = new SalesHistoryForm(
            _productRepository,
            _appSettings,
            new ProcessSaleReturnUseCase(_productRepository, _loyalty));
        f.ShowDialog();
    }

    private void OpenPurchaseSuggestion()
    {
        if (!Allow(UserPermission.Reports))
            return;

        using var f = new PurchaseSuggestionForm(
            _getReorderSuggestionsUseCase,
            _currentUser.Has(UserPermission.Products)
                ? (owner, lines) => OpenPurchaseOrderEditor(null, lines, owner)
                : null);
        f.ShowDialog(this);
        RefreshStockAlerts();
        RefreshSearchIfActive();
    }

    private void OpenSuppliers()
    {
        if (!Allow(UserPermission.Products))
            return;

        using var form = new SuppliersForm(_suppliers);
        form.ShowDialog(this);
    }

    private void OpenPurchaseOrders()
    {
        if (!Allow(UserPermission.Products))
            return;

        using var form = new PurchaseOrdersForm(
            _suppliers,
            _productRepository,
            _purchaseOrders,
            _getReorderSuggestionsUseCase,
            _appSettings,
            _currentUser.DisplayName);
        form.ShowDialog(this);
        RefreshStockAlerts();
        RefreshSearchIfActive();
    }

    private void OpenPurchaseOrderEditor(
        PurchaseOrder? order,
        IReadOnlyList<ReorderSuggestionLine> lines,
        IWin32Window owner)
    {
        if (!Allow(UserPermission.Products))
            return;

        var seeds = lines
            .Select(line => new PurchaseOrderSeedLine
            {
                Barcode = line.Barcode,
                Quantity = line.SuggestedOrderQty > 0 ? line.SuggestedOrderQty : 1
            })
            .ToList();

        using var editor = new PurchaseOrderEditorForm(
            order,
            seeds,
            _suppliers,
            _productRepository,
            _purchaseOrders,
            _getReorderSuggestionsUseCase,
            _appSettings,
            _currentUser.DisplayName);
        editor.ShowDialog(owner);
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
        mnuSuppliers.Visible = products;
        mnuPurchaseOrders.Visible = products;
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
        mnuLoyalty.Visible = settings;
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

        _ddCash.Visible = cash || sales;
        _ddCatalog.Visible = products || reports || import;
        _ddReports.Visible = reports;
    }

    private void OpenLoyalty()
    {
        if (!Allow(UserPermission.Settings))
            return;

        using var form = new LoyaltyForm(_loyalty, _appSettings);
        form.ShowDialog(this);
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
            _lblSearchCount.Text = $"{matches.Count} coincidencia(s)";
            SetStatus($"Búsqueda: {matches.Count} resultado(s) para \"{term}\"");
        }
        else
        {
            _lblSearchCount.Text = "Catálogo listo";
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
        lblTotal.Text = $"${_saleTotal:N2}";
        string label = _cartSessions.Count > 0 ? ActiveSession.Label.ToUpperInvariant() : "VENTA";
        lblCartSummary.Text = $"{label}   ·   Artículos: {itemCount:0.###}   ·   Líneas: {dgvCart.Rows.Count}";
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
            using var checkoutForm = new CheckoutForm(_saleTotal, _loyalty, _appSettings.GetLoyaltySettings());
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
                checkoutForm.DiscountAmount,
                checkoutForm.LoyaltyRequest);
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
