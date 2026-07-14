using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public partial class AdjustStockForm : Form
{
    private readonly AdjustStockUseCase _adjustStockUseCase;
    private readonly Product _product;

    private NumericUpDown numNewStock;
    private Button btnSave;

    public AdjustStockForm(AdjustStockUseCase adjustStockUseCase, Product product)
    {
        _adjustStockUseCase = adjustStockUseCase;
        _product = product;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Auditoría de Inventario - FerreSys";
        this.Size = new Size(380, 240);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        // Información del producto en formato de etiquetas fijas
        var lblProductInfo = new Label
        {
            Text = $"Producto: {_product.Name}\nCódigo: {_product.Barcode}\nStock Actual en Sistema: {_product.Stock}",
            Location = new Point(25, 20),
            Size = new Size(310, 60),
            Font = new Font("Arial", 9, FontStyle.Bold)
        };

        var lblNewStock = new Label { Text = "Cantidad Real Física:", Location = new Point(25, 100), Size = new Size(140, 20), Font = new Font("Arial", 10) };

        numNewStock = new NumericUpDown
        {
            Location = new Point(180, 98),
            Size = new Size(150, 25),
            Maximum = 10000,
            Value = _product.Stock // Por defecto muestra lo que hay para facilitar correcciones rápidas
        };

        btnSave = new Button
        {
            Text = "Confirmar Ajuste (Auditoría)",
            Location = new Point(25, 145),
            Size = new Size(305, 38),
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
            int finalStock = (int)numNewStock.Value;

            // Confirmación de seguridad antes de alterar registros
            var confirm = MessageBox.Show(
                $"¿Seguro que deseas cambiar el stock de '{_product.Name}' de {_product.Stock} unidades a {finalStock} unidades?",
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