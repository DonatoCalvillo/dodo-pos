using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public partial class AddProductForm : Form
{
    private readonly AddProductUseCase? _addProductUseCase;
    private readonly UpdateProductUseCase? _updateProductUseCase;
    private readonly Product? _productToEdit;
    private readonly bool _isEditMode;

    private TextBox txtBarcode = null!;
    private TextBox txtName = null!;
    private NumericUpDown numCostPrice = null!;
    private NumericUpDown numRetailPrice = null!;
    private ComboBox cboUnit = null!;
    private NumericUpDown numStock = null!;
    private NumericUpDown numMinStock = null!;
    private NumericUpDown numDesiredStock = null!;
    private Button btnSave = null!;

    public AddProductForm(AddProductUseCase addProductUseCase)
    {
        _addProductUseCase = addProductUseCase;
        _isEditMode = false;
        InitializeComponent();
    }

    public AddProductForm(UpdateProductUseCase updateProductUseCase, Product productToEdit)
    {
        _updateProductUseCase = updateProductUseCase;
        _productToEdit = productToEdit;
        _isEditMode = true;
        InitializeComponent();
        LoadProductData();
    }

    private void InitializeComponent()
    {
        this.Text = _isEditMode ? "Editar Producto - FerreSys" : "Registrar Nuevo Producto - FerreSys";
        this.Size = new Size(420, 620);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        int labelLeft = 30;
        int inputLeft = 150;

        var lblBarcode = new Label { Text = "Código de Barras:", Location = new Point(labelLeft, 30), Size = new Size(110, 20) };
        txtBarcode = new TextBox { Location = new Point(inputLeft, 28), Size = new Size(200, 25) };
        if (_isEditMode) txtBarcode.ReadOnly = true;

        var lblName = new Label { Text = "Nombre:", Location = new Point(labelLeft, 80), Size = new Size(110, 20) };
        txtName = new TextBox { Location = new Point(inputLeft, 78), Size = new Size(200, 25) };

        var lblCost = new Label { Text = "Costo (por ud.):", Location = new Point(labelLeft, 130), Size = new Size(120, 20) };
        numCostPrice = new NumericUpDown { Location = new Point(inputLeft, 128), Size = new Size(200, 25), DecimalPlaces = 2, Maximum = 99999 };

        var lblRetail = new Label { Text = "Venta (por ud.):", Location = new Point(labelLeft, 180), Size = new Size(120, 20) };
        numRetailPrice = new NumericUpDown { Location = new Point(inputLeft, 178), Size = new Size(200, 25), DecimalPlaces = 2, Maximum = 99999 };

        var lblUnit = new Label { Text = "Unidad:", Location = new Point(labelLeft, 230), Size = new Size(110, 20) };
        cboUnit = new ComboBox
        {
            Location = new Point(inputLeft, 228),
            Size = new Size(200, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (UnitOfMeasure unit in Enum.GetValues<UnitOfMeasure>())
            cboUnit.Items.Add(unit.DisplayName());
        cboUnit.SelectedIndex = 0;
        cboUnit.SelectedIndexChanged += (_, _) => ApplyStockDecimalsForSelectedUnit();

        var lblStock = new Label { Text = "Stock Actual:", Location = new Point(labelLeft, 280), Size = new Size(110, 20) };
        numStock = new NumericUpDown
        {
            Location = new Point(inputLeft, 278),
            Size = new Size(200, 25),
            Maximum = 999999,
            DecimalPlaces = 0
        };

        if (_isEditMode) numStock.Enabled = false;

        var lblMinStock = new Label { Text = "Stock Mínimo:", Location = new Point(labelLeft, 330), Size = new Size(110, 20) };
        numMinStock = new NumericUpDown
        {
            Location = new Point(inputLeft, 328),
            Size = new Size(200, 25),
            Maximum = 999999,
            DecimalPlaces = 0
        };

        var lblDesiredStock = new Label { Text = "Stock Deseado:", Location = new Point(labelLeft, 380), Size = new Size(110, 20) };
        numDesiredStock = new NumericUpDown
        {
            Location = new Point(inputLeft, 378),
            Size = new Size(200, 25),
            Maximum = 999999,
            DecimalPlaces = 0
        };

        var lblHint = new Label
        {
            Text = "Mínimo = 0 desactiva la alerta. Deseado se usa para el sugerido de compra.",
            Location = new Point(labelLeft, 415),
            Size = new Size(340, 36),
            ForeColor = Color.DimGray
        };

        btnSave = new Button
        {
            Text = _isEditMode ? "Guardar Cambios" : "Guardar Producto",
            Location = new Point(30, 465),
            Size = new Size(320, 40),
            BackColor = _isEditMode ? Color.LightGoldenrodYellow : Color.LightBlue,
            Font = new Font("Arial", 10, FontStyle.Bold)
        };
        btnSave.Click += BtnSave_Click;

        this.Controls.AddRange(new Control[]
        {
            lblBarcode, txtBarcode, lblName, txtName, lblCost, numCostPrice,
            lblRetail, numRetailPrice, lblUnit, cboUnit, lblStock, numStock,
            lblMinStock, numMinStock, lblDesiredStock, numDesiredStock, lblHint, btnSave
        });
    }

    private void LoadProductData()
    {
        if (_productToEdit != null)
        {
            txtBarcode.Text = _productToEdit.Barcode;
            txtName.Text = _productToEdit.Name;
            numCostPrice.Value = _productToEdit.CostPrice;
            numRetailPrice.Value = _productToEdit.RetailPrice;
            cboUnit.SelectedIndex = (int)_productToEdit.Unit;
            ApplyStockDecimalsForSelectedUnit();
            numStock.Value = Math.Min(numStock.Maximum, _productToEdit.Stock);
            numMinStock.Value = Math.Min(numMinStock.Maximum, _productToEdit.MinStock);
            numDesiredStock.Value = Math.Min(numDesiredStock.Maximum, _productToEdit.DesiredStock);
        }
    }

    private UnitOfMeasure SelectedUnit() =>
        cboUnit.SelectedIndex >= 0
            ? (UnitOfMeasure)cboUnit.SelectedIndex
            : UnitOfMeasure.Pieza;

    private void ApplyStockDecimalsForSelectedUnit()
    {
        var unit = SelectedUnit();
        int places = unit.MaxDecimalPlaces();
        decimal increment = places > 0 ? 0.001m : 1m;

        foreach (var num in new[] { numStock, numMinStock, numDesiredStock })
        {
            num.DecimalPlaces = places;
            num.Increment = increment;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            Guid id = _isEditMode ? _productToEdit!.Id : Guid.NewGuid();
            var unit = SelectedUnit();

            var product = new Product(
                id,
                txtBarcode.Text.Trim(),
                txtName.Text.Trim(),
                numCostPrice.Value,
                numRetailPrice.Value,
                numStock.Value,
                unit,
                numMinStock.Value,
                numDesiredStock.Value
            );

            if (_isEditMode)
            {
                _updateProductUseCase!.Execute(product);
                MessageBox.Show("¡Producto actualizado con éxito!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _addProductUseCase!.Execute(product);
                MessageBox.Show("¡Producto registrado con éxito!", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
