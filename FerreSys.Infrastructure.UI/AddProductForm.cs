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
    private ZoomImage previewImage = null!;
    private byte[]? _imageBytes;
    private Button btnSave = null!;
    private Button? btnPricing;
    private DiscountKind _discountKind = DiscountKind.None;
    private decimal _discountValue;
    private DateOnly? _discountStartsOn;
    private DateOnly? _discountEndsOn;
    private decimal? _wholesalePrice;
    private decimal? _wholesaleMinQuantity;

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
        this.Text = _isEditMode ? "Editar Producto - Posly" : "Registrar Nuevo Producto - Posly";
        this.Size = new Size(420, _isEditMode ? 760 : 700);
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

        var lblImage = new Label { Text = "Imagen:", Location = new Point(labelLeft, 470), Size = new Size(110, 20) };
        previewImage = new ZoomImage
        {
            Location = new Point(inputLeft, 466),
            Size = new Size(72, 72),
            ShowBorder = true
        };
        ShowPreview(null);

        var btnPickImage = new Button
        {
            Text = "Elegir",
            Location = new Point(inputLeft + 80, 466),
            Size = new Size(120, 28)
        };
        btnPickImage.Click += BtnPickImage_Click;

        var btnClearImage = new Button
        {
            Text = "Quitar",
            Location = new Point(inputLeft + 80, 500),
            Size = new Size(120, 28)
        };
        btnClearImage.Click += (_, _) =>
        {
            _imageBytes = null;
            ShowPreview(null);
        };

        btnSave = new Button
        {
            Text = _isEditMode ? "Guardar Cambios" : "Guardar Producto",
            Location = new Point(30, _isEditMode ? 610 : 560),
            Size = new Size(320, 40),
            BackColor = _isEditMode ? Color.LightGoldenrodYellow : Color.LightBlue,
            Font = new Font("Arial", 10, FontStyle.Bold)
        };
        btnSave.Click += BtnSave_Click;

        if (_isEditMode)
        {
            btnPricing = new Button
            {
                Text = "Precios especiales...",
                Location = new Point(30, 560),
                Size = new Size(320, 36)
            };
            btnPricing.Click += BtnPricing_Click;
        }
        FormClosed += (_, _) => ReleasePreview();

        this.Controls.AddRange(new Control[]
        {
            lblBarcode, txtBarcode, lblName, txtName, lblCost, numCostPrice,
            lblRetail, numRetailPrice, lblUnit, cboUnit, lblStock, numStock,
            lblMinStock, numMinStock, lblDesiredStock, numDesiredStock, lblHint,
            lblImage, previewImage, btnPickImage, btnClearImage, btnSave
        });
        if (btnPricing != null)
            Controls.Add(btnPricing);
    }

    private void BtnPricing_Click(object? sender, EventArgs e)
    {
        if (_productToEdit == null)
            return;

        using var dialog = new ProductPricingForm(
            _productToEdit,
            numCostPrice.Value,
            numRetailPrice.Value,
            SelectedUnit(),
            _discountKind,
            _discountValue,
            _discountStartsOn,
            _discountEndsOn,
            _wholesalePrice,
            _wholesaleMinQuantity);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _discountKind = dialog.DiscountKind;
        _discountValue = dialog.DiscountValue;
        _discountStartsOn = dialog.DiscountStartsOn;
        _discountEndsOn = dialog.DiscountEndsOn;
        _wholesalePrice = dialog.WholesalePrice;
        _wholesaleMinQuantity = dialog.WholesaleMinQuantity;
    }

    private void BtnPickImage_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Elegir imagen del producto",
            Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _imageBytes = ProductImages.EncodeThumbnail(dialog.FileName);
            ShowPreview(_imageBytes);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo leer la imagen.\n\n{ex.Message}",
                "Imagen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ShowPreview(byte[]? jpeg)
    {
        Image? previous = previewImage.Picture;
        previewImage.Picture = jpeg is { Length: > 0 }
            ? ProductImages.ForGrid(jpeg)
            : ProductImages.Placeholder;
        if (previous != null && !ReferenceEquals(previous, ProductImages.Placeholder))
            previous.Dispose();
    }

    private void ReleasePreview()
    {
        Image? picture = previewImage.Picture;
        previewImage.Picture = null;
        if (picture != null && !ReferenceEquals(picture, ProductImages.Placeholder))
            picture.Dispose();
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
            _imageBytes = _productToEdit.Image;
            _discountKind = _productToEdit.DiscountKind;
            _discountValue = _productToEdit.DiscountValue;
            _discountStartsOn = _productToEdit.DiscountStartsOn;
            _discountEndsOn = _productToEdit.DiscountEndsOn;
            _wholesalePrice = _productToEdit.WholesalePrice;
            _wholesaleMinQuantity = _productToEdit.WholesaleMinQuantity;
            ShowPreview(_imageBytes);
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
                numDesiredStock.Value,
                image: _imageBytes,
                discountKind: _discountKind,
                discountValue: _discountValue,
                discountStartsOn: _discountStartsOn,
                discountEndsOn: _discountEndsOn,
                wholesalePrice: _wholesalePrice,
                wholesaleMinQuantity: _wholesaleMinQuantity
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
