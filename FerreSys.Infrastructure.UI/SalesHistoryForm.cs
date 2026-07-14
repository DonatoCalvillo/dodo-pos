using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public partial class SalesHistoryForm : Form
{
    private readonly IProductRepository _productRepository;

    // Componentes de interfaz
    private ComboBox cmbFilter;
    private DataGridView dgvSales;
    private DataGridView dgvDetails;
    private Label lblRevenue;

    public SalesHistoryForm(IProductRepository productRepository)
    {
        _productRepository = productRepository;
        InitializeComponent();
        LoadSales("TODAY"); // Arranca mostrando lo de Hoy por defecto, que es lo más común
    }

    private void InitializeComponent()
    {
        this.Text = "Control de Caja e Historial de Ventas - FerreSys";
        this.Size = new Size(950, 550);
        this.StartPosition = FormStartPosition.CenterParent;

        // 1. Selector de Filtros (Día / Mes / Todo)
        var lblFilter = new Label { Text = "Filtrar por:", Location = new Point(20, 20), Size = new Size(80, 25), Font = new Font("Arial", 10, FontStyle.Bold) };
        cmbFilter = new ComboBox { Location = new Point(100, 18), Size = new Size(180, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbFilter.Items.AddRange(new string[] { "Hoy", "Este Mes", "Ver Todo Histórico" });
        cmbFilter.SelectedIndex = 0; // "Hoy" seleccionado por defecto
        cmbFilter.SelectedIndexChanged += CmbFilter_SelectedIndexChanged;

        // 2. Tabla Izquierda: Listado de Cabeceras de Tickets
        var lblTickets = new Label { Text = "Tickets Emitidos:", Location = new Point(20, 60), Size = new Size(200, 20), Font = new Font("Arial", 9, FontStyle.Italic) };
        dgvSales = new DataGridView { Location = new Point(20, 80), Size = new Size(450, 340), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
        dgvSales.Columns.Add("Id", "ID Ticket");
        dgvSales.Columns.Add("Date", "Fecha y Hora");
        dgvSales.Columns.Add("Total", "Total Cobrado");
        dgvSales.Columns["Id"].Visible = false; // Ocultamos el Guid para que no se vea feo, pero ahí se queda guardado
        dgvSales.SelectionChanged += DgvSales_SelectionChanged; // EVENTO: Al hacer clic en un renglón, carga el detalle

        // 3. Tabla Derecha: Desglose del Ticket seleccionado
        var lblDetails = new Label { Text = "Detalles del Ticket Seleccionado:", Location = new Point(500, 60), Size = new Size(300, 20), Font = new Font("Arial", 9, FontStyle.Bold) };
        dgvDetails = new DataGridView { Location = new Point(500, 80), Size = new Size(410, 340), AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, BackgroundColor = Color.White };
        dgvDetails.Columns.Add("Barcode", "Código");
        dgvDetails.Columns.Add("Name", "Producto");
        dgvDetails.Columns.Add("Qty", "Cant.");
        dgvDetails.Columns.Add("Price", "Precio U.");
        dgvDetails.Columns.Add("Sub", "Subtotal");

        // 4. Reporte de ingresos totalizado abajo
        lblRevenue = new Label { Location = new Point(20, 445), Size = new Size(890, 40), Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.DarkGreen, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.LightGray };

        this.Controls.AddRange(new Control[] { lblFilter, cmbFilter, lblTickets, dgvSales, lblDetails, dgvDetails, lblRevenue });
    }

    private void CmbFilter_SelectedIndexChanged(object? sender, EventArgs e)
    {
        string filterType = "TODAY";
        if (cmbFilter.SelectedIndex == 1) filterType = "MONTH";
        if (cmbFilter.SelectedIndex == 2) filterType = "ALL";

        LoadSales(filterType);
    }

    private void LoadSales(string filterType)
    {
        dgvSales.Rows.Clear();
        dgvDetails.Rows.Clear(); // Limpiar la derecha también

        var sales = _productRepository.GetSalesHistory(filterType);
        decimal totalRevenue = 0;

        foreach (var sale in sales)
        {
            dgvSales.Rows.Add(sale.Id, sale.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"), $"${sale.Total:N2}");
            totalRevenue += sale.Total;
        }

        string labelText = filterType == "TODAY" ? "Caja de Hoy" : (filterType == "MONTH" ? "Ventas del Mes" : "Historial General");
        lblRevenue.Text = $"   Total en {labelText}: ${totalRevenue:N2}";
    }

    private void dgvSalesRowsAdd(Sale sale)
    {
        dgvSales.Rows.Add(sale.Id, sale.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"), $"${sale.Total:N2}");
    }

    private void DgvSales_SelectionChanged(object? sender, EventArgs e)
    {
        dgvDetails.Rows.Clear();

        if (dgvSales.CurrentRow != null)
        {
            // Recuperamos el Guid del ticket oculto en la columna 0
            Guid saleId = (Guid)dgvSales.CurrentRow.Cells[0].Value;

            // Consultamos los detalles a la base de datos SQLite
            var details = _productRepository.GetSaleDetails(saleId);

            foreach (var item in details)
            {
                dgvDetails.Rows.Add(
                    item.Barcode,
                    item.ProductName,
                    item.Quantity,
                    $"${item.PriceSold:N2}",
                    $"${item.Subtotal:N2}"
                );
            }
        }
    }
}