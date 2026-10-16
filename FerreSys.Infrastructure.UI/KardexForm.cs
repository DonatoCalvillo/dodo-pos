using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public class KardexForm : Form
{
    private readonly GetStockMovementsUseCase _getStockMovementsUseCase;
    private readonly IProductRepository _productRepository;
    private readonly Product? _initialProduct;

    private DateTimePicker dtpFrom = null!;
    private DateTimePicker dtpTo = null!;
    private ComboBox cboKind = null!;
    private TextBox txtBarcode = null!;
    private Label lblProduct = null!;
    private DataGridView dgv = null!;
    private Label lblCount = null!;
    private Guid? _filterProductId;

    public KardexForm(
        GetStockMovementsUseCase getStockMovementsUseCase,
        IProductRepository productRepository,
        Product? initialProduct = null)
    {
        _getStockMovementsUseCase = getStockMovementsUseCase;
        _productRepository = productRepository;
        _initialProduct = initialProduct;
        InitializeComponent();

        if (_initialProduct != null)
        {
            _filterProductId = _initialProduct.Id;
            txtBarcode.Text = _initialProduct.Barcode;
            lblProduct.Text = _initialProduct.Name;
        }

        LoadMovements();
    }

    private void InitializeComponent()
    {
        Text = "Kárdex de inventario - Posly";
        Size = new Size(980, 620);
        MinimumSize = new Size(800, 480);
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10, 8, 10, 10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 195));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        root.Controls.Add(CreateFiltersSection(), 0, 0);
        root.Controls.Add(CreateGridSection(), 0, 1);
        root.Controls.Add(CreateButtonsSection(), 0, 2);
        Controls.Add(root);
    }

    private Control CreateFiltersSection()
    {
        var gb = UiHelp.CreateGroupBox("Filtros del Kárdex", SectionHelpText.KardexFilters);

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            Padding = new Padding(8, 22, 8, 6),
            BackColor = UiTheme.PanelBg
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Dock = DockStyle.Left,
            Width = 140,
            Value = DateTime.Today.AddDays(-30)
        };
        dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Dock = DockStyle.Left,
            Width = 140,
            Value = DateTime.Today
        };

        cboKind = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Left,
            Width = 200
        };
        cboKind.Items.Add(new KindFilterItem(null, "Todos"));
        cboKind.Items.Add(new KindFilterItem(StockMovementKind.Venta, "Venta"));
        cboKind.Items.Add(new KindFilterItem(StockMovementKind.AjusteAuditoria, "Ajuste de Auditoría"));
        cboKind.Items.Add(new KindFilterItem(StockMovementKind.CargaInicial, "Carga Inicial"));
        cboKind.Items.Add(new KindFilterItem(StockMovementKind.Devolucion, "Devolución"));
        cboKind.Items.Add(new KindFilterItem(StockMovementKind.Compra, "Compra"));
        cboKind.SelectedIndex = 0;

        txtBarcode = new TextBox { Dock = DockStyle.Fill };
        txtBarcode.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ResolveProductFilter();
                LoadMovements();
            }
        };

        lblProduct = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.Text,
            Text = "(todos los productos)"
        };

        var btnApply = new Button
        {
            Text = "Consultar",
            Size = new Size(110, 30),
            Margin = new Padding(0, 2, 8, 2),
            BackColor = Color.FromArgb(210, 225, 245),
            FlatStyle = FlatStyle.System,
            AutoSize = false
        };
        btnApply.Click += (_, _) =>
        {
            ResolveProductFilter();
            LoadMovements();
        };

        var btnClearProduct = new Button
        {
            Text = "Quitar filtro producto",
            Size = new Size(160, 30),
            Margin = new Padding(0, 2, 0, 2),
            AutoSize = false
        };
        btnClearProduct.Click += (_, _) =>
        {
            _filterProductId = null;
            txtBarcode.Clear();
            lblProduct.Text = "(todos los productos)";
            LoadMovements();
        };

        stack.Controls.Add(MakeLabel("Desde"), 0, 0);
        stack.Controls.Add(dtpFrom, 1, 0);
        stack.Controls.Add(MakeLabel("Hasta"), 2, 0);
        stack.Controls.Add(dtpTo, 3, 0);

        stack.Controls.Add(MakeLabel("Tipo"), 0, 1);
        stack.Controls.Add(cboKind, 1, 1);
        stack.Controls.Add(MakeLabel("Código"), 2, 1);
        stack.Controls.Add(txtBarcode, 3, 1);

        stack.Controls.Add(MakeLabel("Producto"), 0, 2);
        stack.Controls.Add(lblProduct, 1, 2);
        stack.SetColumnSpan(lblProduct, 3);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        actions.Controls.Add(btnApply);
        actions.Controls.Add(btnClearProduct);
        stack.Controls.Add(actions, 1, 3);
        stack.SetColumnSpan(actions, 3);

        gb.Controls.Add(stack);
        return gb;
    }

    private Control CreateGridSection()
    {
        var gb = UiHelp.CreateGroupBox("Movimientos", SectionHelpText.KardexList);

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(0, 70, 180),
                ForeColor = Color.White,
                Font = UiTheme.UiBold,
                SelectionBackColor = Color.FromArgb(0, 70, 180)
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = UiTheme.Ui,
                SelectionBackColor = Color.FromArgb(210, 225, 245),
                SelectionForeColor = Color.Black
            }
        };

        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "OccurredAt", HeaderText = "Fecha/Hora", FillWeight = 110 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Barcode", HeaderText = "Código", FillWeight = 70 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Producto", FillWeight = 140 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kind", HeaderText = "Tipo", FillWeight = 95 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Before", HeaderText = "Cant. anterior", FillWeight = 70 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Delta", HeaderText = "Cant. movida", FillWeight = 70 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "After", HeaderText = "Stock resultante", FillWeight = 75 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Actor", HeaderText = "Usuario", FillWeight = 70 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "Referencia / Notas", FillWeight = 120 });

        gb.Controls.Add(dgv);
        return gb;
    }

    private Control CreateButtonsSection()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.WindowBg
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        lblCount = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.Text,
            Font = UiTheme.UiBold
        };

        var btnClose = new Button
        {
            Text = "Cerrar",
            Dock = DockStyle.Fill,
            DialogResult = DialogResult.Cancel
        };
        CancelButton = btnClose;

        panel.Controls.Add(lblCount, 0, 0);
        panel.Controls.Add(btnClose, 1, 0);
        return panel;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = UiTheme.UiBold,
        ForeColor = UiTheme.Text
    };

    private void ResolveProductFilter()
    {
        string barcode = txtBarcode.Text.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            _filterProductId = null;
            lblProduct.Text = "(todos los productos)";
            return;
        }

        var product = _productRepository.GetByBarcode(barcode);
        if (product == null)
        {
            _filterProductId = null;
            lblProduct.Text = "(código no encontrado — se muestran todos)";
            MessageBox.Show(
                $"No se encontró un producto con el código '{barcode}'.",
                "Kárdex",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _filterProductId = product.Id;
        lblProduct.Text = product.Name;
        txtBarcode.Text = product.Barcode;
    }

    private void LoadMovements()
    {
        if (dtpFrom.Value.Date > dtpTo.Value.Date)
        {
            MessageBox.Show(
                "La fecha 'Desde' no puede ser posterior a 'Hasta'.",
                "Kárdex",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        StockMovementKind? kind = null;
        if (cboKind.SelectedItem is KindFilterItem item)
            kind = item.Kind;

        var movements = _getStockMovementsUseCase.Execute(
            _filterProductId,
            dtpFrom.Value.Date,
            dtpTo.Value.Date,
            kind);

        dgv.Rows.Clear();
        foreach (var m in movements)
        {
            string reference = m.ReferenceId.HasValue
                ? m.ReferenceId.Value.ToString("N")[..8].ToUpperInvariant()
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(m.Notes))
                reference = string.IsNullOrEmpty(reference) ? m.Notes : $"{reference} · {m.Notes}";

            string deltaText = m.QuantityDelta > 0
                ? $"+{m.QuantityDelta:N3}"
                : m.QuantityDelta.ToString("N3");

            dgv.Rows.Add(
                m.OccurredAt.ToString("dd/MM/yyyy HH:mm:ss"),
                m.Barcode,
                m.ProductName,
                m.KindDisplayName,
                m.QuantityBefore.ToString("N3"),
                deltaText,
                m.QuantityAfter.ToString("N3"),
                m.Actor,
                reference);
        }

        lblCount.Text = movements.Count == 1
            ? "1 movimiento"
            : $"{movements.Count} movimientos";
    }

    private sealed class KindFilterItem
    {
        public KindFilterItem(StockMovementKind? kind, string label)
        {
            Kind = kind;
            Label = label;
        }

        public StockMovementKind? Kind { get; }
        public string Label { get; }
        public override string ToString() => Label;
    }
}
