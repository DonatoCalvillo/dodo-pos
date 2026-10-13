using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public partial class SalesHistoryForm : Form
{
    private readonly IProductRepository _productRepository;
    private readonly IAppSettingsStore _settings;
    private readonly Dictionary<Guid, Sale> _headers = new();

    // Controles de Filtros Avanzados
    private TextBox txtSearchId = null!;
    private DateTimePicker dtpStart = null!;
    private DateTimePicker dtpEnd = null!;
    private Button btnToday = null!;
    private Button btnYesterday = null!;
    private Button btnThisWeek = null!;

    // Tablas y etiquetas
    private DataGridView dgvSales = null!;
    private DataGridView dgvDetails = null!;
    private Label lblRevenue = null!;

    public SalesHistoryForm(IProductRepository productRepository, IAppSettingsStore settings)
    {
        _productRepository = productRepository;
        _settings = settings;
        InitializeComponent();

        // Ejecutar búsqueda inicial seteando "Hoy" de forma predeterminada
        SetDatesAndSearch(DateTime.Today, DateTime.Today);
    }

    private void InitializeComponent()
    {
        this.Text = "Control de Caja Avanzado e Historial de Ventas - Posly";
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        // --- PANEL DE FILTROS SUPERIOR ---
        int yRow1 = 20;

        // 1. Buscador por ID del Ticket
        var lblSearchId = new Label { Text = "Buscar ID Ticket:", Location = new Point(20, yRow1 + 3), Size = new Size(100, 20), Font = new Font("Arial", 9, FontStyle.Bold) };
        txtSearchId = new TextBox { Location = new Point(125, yRow1), Size = new Size(130, 23) };
        txtSearchId.TextChanged += (s, e) => ExecuteAdvancedSearch(); // Busca en tiempo real mientras escribe

        // 2. Selectores de Rango de Fecha Libre
        var lblStart = new Label { Text = "Desde:", Location = new Point(275, yRow1 + 3), Size = new Size(45, 20) };
        dtpStart = new DateTimePicker { Location = new Point(325, yRow1), Size = new Size(110, 23), Format = DateTimePickerFormat.Short };
        dtpStart.ValueChanged += (s, e) => ExecuteAdvancedSearch();

        var lblEnd = new Label { Text = "Hasta:", Location = new Point(450, yRow1 + 3), Size = new Size(45, 20) };
        dtpEnd = new DateTimePicker { Location = new Point(500, yRow1), Size = new Size(110, 23), Format = DateTimePickerFormat.Short };
        dtpEnd.ValueChanged += (s, e) => ExecuteAdvancedSearch();

        // 3. Botones Auxiliares Rápidos
        btnToday = new Button { Text = "Hoy", Location = new Point(630, yRow1 - 2), Size = new Size(70, 27), BackColor = Color.WhiteSmoke };
        btnToday.Click += (s, e) => SetDatesAndSearch(DateTime.Today, DateTime.Today);

        btnYesterday = new Button { Text = "Ayer", Location = new Point(705, yRow1 - 2), Size = new Size(70, 27), BackColor = Color.WhiteSmoke };
        btnYesterday.Click += (s, e) => SetDatesAndSearch(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(-1));

        btnThisWeek = new Button { Text = "Esta Semana", Location = new Point(780, yRow1 - 2), Size = new Size(95, 27), BackColor = Color.WhiteSmoke };
        btnThisWeek.Click += (s, e) =>
        {
            // Lunes de la semana actual
            int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = DateTime.Today.AddDays(-1 * diff);
            SetDatesAndSearch(startOfWeek, DateTime.Today);
        };

        // --- CUERPO PRINCIPAL (TABLAS) ---
        int yTables = 75;

        // Tabla Izquierda: Historial con ID visible
        var lblTickets = new Label { Text = "Tickets Emitidos:", Location = new Point(20, yTables - 20), Size = new Size(200, 20), Font = new Font("Arial", 9, FontStyle.Italic) };
        dgvSales = new DataGridView { Location = new Point(20, yTables), Size = new Size(480, 390), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
        dgvSales.Columns.Add("FullId", "GUID Completo Oculto");
        dgvSales.Columns.Add("ShortId", "ID Ticket"); // <--- NUEVA COLUMNA VISIBLE
        dgvSales.Columns.Add("Date", "Fecha y Hora");
        dgvSales.Columns.Add("Total", "Total Cobrado");

        Column(dgvSales, "FullId").Visible = false; // Oculto para la lógica interna
        Column(dgvSales, "ShortId").FillWeight = 65; // Proporciones de tamaño de columna
        Column(dgvSales, "Date").FillWeight = 110;
        Column(dgvSales, "Total").FillWeight = 75;
        dgvSales.SelectionChanged += DgvSales_SelectionChanged;
        dgvSales.CellDoubleClick += DgvSales_CellDoubleClick;

        // Tabla Derecha: Detalles del Ticket
        var lblDetails = new Label { Text = "Detalles del Ticket Seleccionado:", Location = new Point(520, yTables - 20), Size = new Size(300, 20), Font = new Font("Arial", 9, FontStyle.Bold) };
        dgvDetails = new DataGridView { Location = new Point(520, yTables), Size = new Size(440, 390), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, BackgroundColor = Color.White };
        dgvDetails.Columns.Add("Barcode", "Código");
        dgvDetails.Columns.Add("Name", "Producto");
        dgvDetails.Columns.Add("Qty", "Cant.");
        dgvDetails.Columns.Add("Price", "Precio U.");
        dgvDetails.Columns.Add("Sub", "Subtotal");
        Column(dgvDetails, "Qty").FillWeight = 45;
        Column(dgvDetails, "Sub").FillWeight = 70;

        // Reporte final de ingresos acumulados
        lblRevenue = new Label { Location = new Point(20, 490), Size = new Size(940, 40), Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.DarkGreen, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.Gainsboro };

        // Agregar controles al Formulario
        this.Controls.AddRange(new Control[] {
            lblSearchId, txtSearchId, lblStart, dtpStart, lblEnd, dtpEnd, btnToday, btnYesterday, btnThisWeek,
            lblTickets, dgvSales, lblDetails, dgvDetails, lblRevenue
        });
    }

    // Método auxiliar para setear fechas desde los botones rápidos sin disparar ciclos infinitos
    private void SetDatesAndSearch(DateTime start, DateTime end)
    {
        // Desactivamos eventos temporalmente para evitar ejecuciones dobles accidentales
        dtpStart.Value = start;
        dtpEnd.Value = end;
        ExecuteAdvancedSearch();
    }

    private void ExecuteAdvancedSearch()
    {
        dgvSales.Rows.Clear();
        dgvDetails.Rows.Clear();
        _headers.Clear();

        DateTime startDate = dtpStart.Value;
        DateTime endDate = dtpEnd.Value;
        string ticketFilter = txtSearchId.Text.Trim();

        var sales = _productRepository.GetSalesHistoryAdvanced(startDate, endDate, ticketFilter);
        decimal totalRevenue = 0;

        foreach (var sale in sales)
        {
            // Mostramos los primeros 8 caracteres del ID en mayúsculas (ID Ticket) para que coincida con el formato del papel impreso
            string shortId = sale.Id.ToString()[..8].ToUpper();

            _headers[sale.Id] = sale;
            dgvSales.Rows.Add(sale.Id, shortId, sale.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"), $"${sale.Total:N2}");
            totalRevenue += sale.Total;
        }

        lblRevenue.Text = $"   Ingresos Totales en Rango Seleccionado: ${totalRevenue:N2}";
    }

    private void DgvSales_SelectionChanged(object? sender, EventArgs e)
    {
        dgvDetails.Rows.Clear();

        if (dgvSales.CurrentRow != null)
        {
            // Recuperamos el Guid completo guardado de forma segura en la columna 0
            if (dgvSales.CurrentRow.Cells[0].Value is not Guid saleId)
            {
                return;
            }

            var details = _productRepository.GetSaleDetails(saleId);

            foreach (var item in details)
            {
                dgvDetails.Rows.Add(
                    item.Barcode,
                    item.ProductName,
                    item.Unit.FormatQuantity(item.Quantity),
                    $"${item.PriceSold:N2}",
                    $"${item.Subtotal:N2}"
                );
            }
        }
    }

    private void DgvSales_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;
        if (dgvSales.Rows[e.RowIndex].Cells[0].Value is not Guid saleId)
            return;
        if (!_headers.TryGetValue(saleId, out Sale? header))
            return;

        List<SaleItem> items = _productRepository.GetSaleDetails(saleId);
        if (items.Count == 0)
        {
            MessageBox.Show("Este ticket no tiene productos para imprimir.", "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sale = new Sale(
            header.Id,
            header.CreatedAt,
            items,
            header.PaymentMethod,
            header.CardReference,
            header.AmountReceived,
            header.CardAmount);
        using var dialog = new ReprintTicketForm(sale, _settings);
        dialog.ShowDialog(this);
    }

    private static DataGridViewColumn Column(DataGridView grid, string name) =>
        grid.Columns[name] ?? throw new InvalidOperationException($"No existe la columna '{name}'.");
}