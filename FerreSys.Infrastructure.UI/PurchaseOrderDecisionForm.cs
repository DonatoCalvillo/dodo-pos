using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

internal sealed class PurchaseOrderDecisionForm : Form
{
    private readonly PurchaseOrder _order;
    private readonly DecidePurchaseOrderUseCase _decide;
    private readonly string _actor;
    private readonly DataGridView _grid = new();

    public PurchaseOrder? Result { get; private set; }

    public PurchaseOrderDecisionForm(PurchaseOrder order, DecidePurchaseOrderUseCase decide, string actor)
    {
        _order = order;
        _decide = decide;
        _actor = actor;
        Text = $"Respuesta de {order.Number}";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 480);
        MinimumSize = new Size(640, 360);
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        SuppliersForm.StyleGrid(_grid);
        _grid.ReadOnly = false;
        _grid.Columns.Add("Name", "Producto");
        _grid.Columns.Add("Ordered", "Pedido");
        var received = new DataGridViewTextBoxColumn { Name = "Received", HeaderText = "Recibido", FillWeight = 70 };
        _grid.Columns.Add(received);
        _grid.Columns["Name"]!.ReadOnly = true;
        _grid.Columns["Ordered"]!.ReadOnly = true;
        _grid.Columns["Ordered"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _grid.Columns["Received"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        foreach (PurchaseOrderLine line in order.Lines)
        {
            int index = _grid.Rows.Add(line.Name, line.Unit.FormatQuantity(line.OrderedQty), FormatQty(line.Unit, line.OrderedQty));
            _grid.Rows[index].Tag = line;
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var cancel = new Button { Text = "Cancelar", AutoSize = true, MinimumSize = new Size(110, 32), DialogResult = DialogResult.Cancel };
        var partial = new Button { Text = "Registrar recepción", AutoSize = true, MinimumSize = new Size(160, 32), FlatStyle = FlatStyle.System };
        var reject = new Button { Text = "Rechazar", AutoSize = true, MinimumSize = new Size(110, 32) };
        var accept = new Button { Text = "Aceptar todo", AutoSize = true, MinimumSize = new Size(120, 32), FlatStyle = FlatStyle.System };
        partial.Click += (_, _) => Receive();
        reject.Click += (_, _) => Reject();
        accept.Click += (_, _) => AcceptAll();
        buttons.Controls.AddRange([cancel, partial, reject, accept]);

        root.Controls.Add(_grid, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        Controls.Add(root);
        CancelButton = cancel;
    }

    private void AcceptAll()
    {
        if (!Confirm("Se recibirá todo lo pedido y se sumará al inventario."))
            return;
        Run(() => _decide.AcceptAll(_order.Id, _actor));
    }

    private void Reject()
    {
        if (!Confirm("La orden quedará rechazada y no se sumará inventario."))
            return;
        Run(() => _decide.Reject(_order.Id, _actor));
    }

    private void Receive()
    {
        var receipts = new List<PurchaseReceiptDraft>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Tag is not PurchaseOrderLine line)
                continue;
            if (!TryParseQty(Convert.ToString(row.Cells["Received"].Value), line.Unit, out decimal qty))
            {
                MessageBox.Show(this, $"La cantidad recibida de '{line.Name}' no es válida.", "Orden de compra",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            receipts.Add(new PurchaseReceiptDraft { LineId = line.Id, ReceivedQty = qty });
        }

        bool any = receipts.Any(line => line.ReceivedQty > 0);
        if (!any)
        {
            MessageBox.Show(this, "No hay mercancía recibida. Usa Rechazar si el proveedor no surtió.",
                "Orden de compra", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!Confirm("Se sumará al inventario solo la cantidad recibida y la orden quedará cerrada."))
            return;
        Run(() => _decide.Receive(_order.Id, receipts, _actor));
    }

    private void Run(Func<PurchaseOrder> action)
    {
        try
        {
            Result = action();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "Orden de compra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool Confirm(string message) =>
        MessageBox.Show(this, message, "Orden de compra", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    private static bool TryParseQty(string? text, UnitOfMeasure unit, out decimal quantity)
    {
        quantity = 0;
        if (!decimal.TryParse(text, out decimal parsed) && !decimal.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out parsed))
            return false;
        try
        {
            Product.EnsureQuantity(parsed, unit, allowZero: true);
        }
        catch (ArgumentException)
        {
            return false;
        }

        quantity = parsed;
        return true;
    }

    private static string FormatQty(UnitOfMeasure unit, decimal quantity) =>
        unit.AllowsDecimals() ? quantity.ToString("0.###") : decimal.Truncate(quantity).ToString("0");
}
