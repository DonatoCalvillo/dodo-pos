using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public partial class AdjustStockForm : Form
{
    private readonly AdjustStockUseCase _adjustStockUseCase;
    private readonly Product _product;

    private NumericUpDown numNewStock = null!;
    private Button btnSave = null!;

    public AdjustStockForm(AdjustStockUseCase adjustStockUseCase, Product product)
    {
        _adjustStockUseCase = adjustStockUseCase;
        _product = product;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Auditoría de Inventario - FerreSys";
        this.Size = new Size(400, 260);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        string stockLabel = _product.Unit.FormatQuantity(_product.Stock);

        // Información del producto en formato de etiquetas fijas
        var lblProductInfo = new Label
        {
            Text = $"Producto: {_product.Name}\nCódigo: {_product.Barcode}\nUnidad: {_product.Unit.DisplayName()}\nStock Actual: {stockLabel}",
            Location = new Point(25, 20),
            Size = new Size(340, 75),
            Font = new Font("Arial", 9, FontStyle.Bold)
        };

        var lblNewStock = new Label
        {
            Text = $"Cantidad Real ({_product.Unit.DisplaySuffix()}):",
            Location = new Point(25, 110),
            Size = new Size(160, 20),
            Font = new Font("Arial", 10)
        };

        int places = _product.Unit.MaxDecimalPlaces();
        numNewStock = new NumericUpDown
        {
            Location = new Point(195, 108),
            Size = new Size(150, 25),
            Maximum = 999999,
            DecimalPlaces = places,
            Increment = places > 0 ? 0.001m : 1m,
            Value = Math.Min(999999, _product.Stock)
        };

        btnSave = new Button
        {
            Text = "Confirmar Ajuste (Auditoría)",
            Location = new Point(25, 160),
            Size = new Size(320, 38),
            BackColor = Color.LightCoral, // Color distintivo de acción administrativa
            Font = new Font("Arial", 10, FontStyle.Bold)
        };
        btnSave.Click += BtnSave_Click;

        this.Controls.AddRange(new Control[] { lblProductInfo, lblNewStock, numNewStock, btnSave });
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            decimal finalStock = numNewStock.Value;
            string fromLabel = _product.Unit.FormatQuantity(_product.Stock);
            string toLabel = _product.Unit.FormatQuantity(finalStock);

            // Confirmación de seguridad antes de alterar registros
            var confirm = MessageBox.Show(
                $"¿Seguro que deseas cambiar el stock de '{_product.Name}' de {fromLabel} a {toLabel}?",
                "Confirmar Movimiento de Auditoría",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.Yes)
            {
                _adjustStockUseCase.Execute(_product.Barcode, finalStock);
                MessageBox.Show("¡Inventario auditado y corregido con éxito!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error en Auditoría", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
