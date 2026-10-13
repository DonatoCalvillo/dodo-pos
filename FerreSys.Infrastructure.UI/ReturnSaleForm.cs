using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public class ReturnSaleForm : Form
{
    private readonly ProcessSaleReturnUseCase _processSaleReturnUseCase;
    private readonly IProductRepository _productRepository;
    private readonly Guid _saleId;
    private readonly string _ticketId;

    private DataGridView dgvLines = null!;
    private CheckBox chkRestoreStock = null!;
    private Label lblTotal = null!;
    private Button btnReturnAll = null!;
    private Button btnConfirm = null!;

    public ReturnSaleForm(
        ProcessSaleReturnUseCase processSaleReturnUseCase,
        IProductRepository productRepository,
        Guid saleId,
        string ticketId)
    {
        _processSaleReturnUseCase = processSaleReturnUseCase;
        _productRepository = productRepository;
        _saleId = saleId;
        _ticketId = ticketId;
        InitializeComponent();
        LoadLines();
    }

    private void InitializeComponent()
    {
        Text = $"Devolución del ticket {_ticketId}";
        Size = new Size(760, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var lblHint = new Label
        {
            Text = "Indica la cantidad a devolver de cada producto. El dinero sale de caja hoy.",
            Location = new Point(16, 12),
            Size = new Size(710, 22)
        };

        dgvLines = new DataGridView
        {
            Location = new Point(16, 40),
            Size = new Size(710, 320),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            MultiSelect = false
        };
        dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductId", Visible = false });
        dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Producto", ReadOnly = true, FillWeight = 160 });
        dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sold", HeaderText = "Vendido", ReadOnly = true, FillWeight = 70 });
        dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Returned", HeaderText = "Ya devuelto", ReadOnly = true, FillWeight = 80 });
        dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pending", HeaderText = "Pendiente", ReadOnly = true, FillWeight = 70 });
        dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "A devolver", FillWeight = 70 });
        dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Monto", ReadOnly = true, FillWeight = 70 });
        dgvLines.CellEndEdit += (_, _) => RefreshAmounts();
        dgvLines.DataError += (_, e) => e.ThrowException = false;

        btnReturnAll = new Button
        {
            Text = "Devolver todo lo pendiente",
            Location = new Point(16, 372),
            Size = new Size(220, 32)
        };
        btnReturnAll.Click += (_, _) => ReturnAllPending();

        chkRestoreStock = new CheckBox
        {
            Text = "Regresar mercancía al inventario",
            Location = new Point(260, 376),
            Size = new Size(280, 24),
            Checked = true
        };

        lblTotal = new Label
        {
            Text = "A devolver: $0.00",
            Location = new Point(16, 416),
            Size = new Size(710, 24),
            Font = new Font("Arial", 11, FontStyle.Bold)
        };

        btnConfirm = new Button
        {
            Text = "Confirmar devolución",
            Location = new Point(486, 444),
            Size = new Size(240, 34),
            BackColor = Color.LightSalmon,
            Font = new Font("Arial", 9, FontStyle.Bold)
        };
        btnConfirm.Click += BtnConfirm_Click;

        Controls.AddRange(new Control[] { lblHint, dgvLines, btnReturnAll, chkRestoreStock, lblTotal, btnConfirm });
    }

    private void LoadLines()
    {
        dgvLines.Rows.Clear();
        List<SaleItem> details = _productRepository.GetSaleDetails(_saleId);
        SaleReturnTotals prior = _productRepository.GetSaleReturnTotals(_saleId);

        foreach (IGrouping<Guid, SaleItem> group in details.GroupBy(item => item.ProductId))
        {
            SaleItem first = group.First();
            decimal sold = RoundQuantity(group.Sum(item => item.Quantity), first.Unit);
            decimal returned = RoundQuantity(prior.QuantityFor(group.Key), first.Unit);
            decimal pending = sold - returned;
            if (pending <= 0m)
                continue;

            int rowIndex = dgvLines.Rows.Add(
                group.Key,
                first.ProductName,
                first.Unit.FormatQuantity(sold),
                first.Unit.FormatQuantity(returned),
                first.Unit.FormatQuantity(pending),
                "0",
                "$0.00");
            dgvLines.Rows[rowIndex].Tag = new PendingLine(group.Key, first.Unit, pending);
        }

        if (dgvLines.Rows.Count == 0)
        {
            btnReturnAll.Enabled = false;
            btnConfirm.Enabled = false;
            lblTotal.Text = "Este ticket ya no tiene mercancía pendiente de devolver.";
        }
    }

    private void ReturnAllPending()
    {
        dgvLines.EndEdit();
        foreach (DataGridViewRow row in dgvLines.Rows)
        {
            if (row.Tag is not PendingLine line)
                continue;

            row.Cells["Qty"].Value = FormatQuantity(line.Pending, line.Unit);
        }

        RefreshAmounts();
    }

    private void RefreshAmounts()
    {
        foreach (DataGridViewRow row in dgvLines.Rows)
            row.Cells["Amount"].Value = "$0.00";

        List<SaleReturnLineRequest> requests = ReadRequests(out string? error);
        if (error != null)
        {
            lblTotal.Text = error;
            return;
        }

        if (requests.Count == 0)
        {
            lblTotal.Text = "A devolver: $0.00";
            return;
        }

        try
        {
            SaleReturnQuote quote = _processSaleReturnUseCase.Quote(_saleId, requests);
            foreach (SaleReturnLine line in quote.Lines)
            {
                DataGridViewRow? row = FindRow(line.ProductId);
                if (row != null)
                    row.Cells["Amount"].Value = line.LineAmount.ToString("C2");
            }

            lblTotal.Text = $"A devolver: {quote.Amount:C2}";
        }
        catch (Exception ex)
        {
            lblTotal.Text = ex.Message;
        }
    }

    private void BtnConfirm_Click(object? sender, EventArgs e)
    {
        dgvLines.EndEdit();
        List<SaleReturnLineRequest> requests = ReadRequests(out string? error);
        if (error != null)
        {
            MessageBox.Show(error, "Devolución", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (requests.Count == 0)
        {
            MessageBox.Show("Indica la cantidad de al menos un producto.", "Devolución", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SaleReturnQuote quote;
        try
        {
            quote = _processSaleReturnUseCase.Quote(_saleId, requests);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Devolución", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string stockText = chkRestoreStock.Checked
            ? "La mercancía regresará al inventario."
            : "El inventario no se modificará.";
        DialogResult confirm = MessageBox.Show(
            $"Saldrá {quote.Amount:C2} de la caja de hoy.\n{stockText}",
            "Confirmar devolución",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        try
        {
            _processSaleReturnUseCase.Execute(_saleId, requests, chkRestoreStock.Checked);
            MessageBox.Show(
                $"Devolución registrada por {quote.Amount:C2}.",
                "Devolución",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Devolución", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private List<SaleReturnLineRequest> ReadRequests(out string? error)
    {
        error = null;
        var requests = new List<SaleReturnLineRequest>();
        foreach (DataGridViewRow row in dgvLines.Rows)
        {
            if (row.Tag is not PendingLine line)
                continue;

            string text = Convert.ToString(row.Cells["Qty"].Value)?.Trim() ?? "0";
            if (text.Length == 0 || text == "0")
                continue;

            if (!decimal.TryParse(text, out decimal quantity))
            {
                error = $"La cantidad de '{row.Cells["Name"].Value}' no es un número válido.";
                return requests;
            }

            decimal rounded = RoundQuantity(quantity, line.Unit);
            if (Math.Abs(rounded - quantity) > 0.0000001m)
            {
                error = $"La cantidad de '{row.Cells["Name"].Value}' no es válida para {line.Unit.DisplayName()}.";
                return requests;
            }

            if (rounded <= 0m)
                continue;

            if (rounded > line.Pending)
            {
                error = $"No se puede devolver más de lo pendiente de '{row.Cells["Name"].Value}'.";
                return requests;
            }

            requests.Add(new SaleReturnLineRequest
            {
                ProductId = line.ProductId,
                Quantity = rounded
            });
        }

        return requests;
    }

    private DataGridViewRow? FindRow(Guid productId)
    {
        foreach (DataGridViewRow row in dgvLines.Rows)
        {
            if (row.Tag is PendingLine line && line.ProductId == productId)
                return row;
        }

        return null;
    }

    private static decimal RoundQuantity(decimal quantity, UnitOfMeasure unit) =>
        decimal.Round(quantity, unit.MaxDecimalPlaces(), MidpointRounding.AwayFromZero);

    private static string FormatQuantity(decimal quantity, UnitOfMeasure unit) =>
        unit.AllowsDecimals() ? quantity.ToString("0.###") : decimal.Truncate(quantity).ToString("0");

    private sealed class PendingLine
    {
        public Guid ProductId { get; }
        public UnitOfMeasure Unit { get; }
        public decimal Pending { get; }

        public PendingLine(Guid productId, UnitOfMeasure unit, decimal pending)
        {
            ProductId = productId;
            Unit = unit;
            Pending = pending;
        }
    }
}
