using System.Drawing.Printing;
using System.Text;
using FerreSys.Application;
using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

public class PurchaseSuggestionForm : Form
{
    private readonly GetReorderSuggestionsUseCase _getReorderSuggestionsUseCase;

    private DataGridView dgv = null!;
    private Label lblSummary = null!;
    private Button btnExportCsv = null!;
    private Button btnPrintPdf = null!;
    private Button btnRefresh = null!;
    private Button btnClose = null!;

    private ReorderSuggestionsSummary _summary = new([]);

    public PurchaseSuggestionForm(GetReorderSuggestionsUseCase getReorderSuggestionsUseCase)
    {
        _getReorderSuggestionsUseCase = getReorderSuggestionsUseCase;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        Text = "Sugerido de Compra - Posly";
        Size = new Size(920, 560);
        MinimumSize = new Size(720, 420);
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        root.Controls.Add(CreateHeaderSection(), 0, 0);
        root.Controls.Add(CreateGridSection(), 0, 1);
        root.Controls.Add(CreateButtonsSection(), 0, 2);
        Controls.Add(root);
    }

    private Control CreateHeaderSection()
    {
        var gb = UiHelp.CreateGroupBox("Sugerido de compra", SectionHelpText.PurchaseSuggestion);

        lblSummary = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 18, 8, 4),
            ForeColor = UiTheme.Text,
            Font = UiTheme.UiBold
        };

        gb.Controls.Add(lblSummary);
        return gb;
    }

    private Control CreateGridSection()
    {
        var gb = UiHelp.CreateGroupBox("Productos a reabastecer", SectionHelpText.PurchaseSuggestionList);

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

        dgv.Columns.Add("Barcode", "Código");
        dgv.Columns.Add("Name", "Producto");
        dgv.Columns.Add("Unit", "Unidad");
        dgv.Columns.Add("Stock", "Stock");
        dgv.Columns.Add("MinStock", "Mínimo");
        dgv.Columns.Add("DesiredStock", "Deseado");
        dgv.Columns.Add("Suggested", "A pedir");

        dgv.Columns["Barcode"]!.FillWeight = 90;
        dgv.Columns["Name"]!.FillWeight = 180;
        dgv.Columns["Unit"]!.FillWeight = 60;
        dgv.Columns["Stock"]!.FillWeight = 55;
        dgv.Columns["MinStock"]!.FillWeight = 55;
        dgv.Columns["DesiredStock"]!.FillWeight = 55;
        dgv.Columns["Suggested"]!.FillWeight = 55;

        foreach (var colName in new[] { "Stock", "MinStock", "DesiredStock", "Suggested" })
            dgv.Columns[colName]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 20, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        panel.Controls.Add(dgv);
        gb.Controls.Add(panel);
        return gb;
    }

    private Control CreateButtonsSection()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };

        btnClose = CreateButton("Cerrar", false);
        btnClose.Click += (_, _) => Close();

        btnPrintPdf = CreateButton("Imprimir / PDF…", true);
        btnPrintPdf.Click += (_, _) => PrintOrSavePdf();

        btnExportCsv = CreateButton("Exportar CSV", true);
        btnExportCsv.Click += (_, _) => ExportCsv();

        btnRefresh = CreateButton("Actualizar", false);
        btnRefresh.Click += (_, _) => LoadData();

        panel.Controls.AddRange([btnClose, btnPrintPdf, btnExportCsv, btnRefresh]);
        return panel;
    }

    private static Button CreateButton(string text, bool primary)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(120, 32),
            Margin = new Padding(6, 0, 0, 0),
            BackColor = primary ? Color.FromArgb(220, 235, 255) : SystemColors.Control,
            FlatStyle = FlatStyle.System
        };
    }

    private void LoadData()
    {
        _summary = _getReorderSuggestionsUseCase.Execute();
        dgv.Rows.Clear();

        foreach (var line in _summary.Lines)
        {
            int idx = dgv.Rows.Add(
                line.Barcode,
                line.Name,
                line.Unit.DisplayName(),
                line.Unit.FormatQuantity(line.Stock),
                line.Unit.FormatQuantity(line.MinStock),
                line.Unit.FormatQuantity(line.DesiredStock),
                line.Unit.FormatQuantity(line.SuggestedOrderQty));

            var row = dgv.Rows[idx];
            row.DefaultCellStyle.BackColor = line.IsOutOfStock
                ? UiTheme.StockDanger
                : line.AlertLevel == StockAlertLevel.Warning
                    ? UiTheme.StockWarn
                    : Color.White;
        }

        lblSummary.Text =
            $"{_summary.TotalItems} producto(s) · " +
            $"{_summary.OutOfStockCount} agotado(s) · " +
            $"{_summary.BelowMinimumCount} bajo mínimo · " +
            $"Suma a pedir: {_summary.TotalSuggestedUnits:N3}";

        bool hasRows = _summary.TotalItems > 0;
        btnExportCsv.Enabled = hasRows;
        btnPrintPdf.Enabled = hasRows;
    }

    private void ExportCsv()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Archivos CSV (*.csv)|*.csv",
            Title = "Exportar sugerido de compra",
            FileName = $"sugerido-compra-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Codigo,Producto,Unidad,Stock,Minimo,Deseado,APedir");

        foreach (var line in _summary.Lines)
        {
            sb.Append(EscapeCsv(line.Barcode)).Append(',');
            sb.Append(EscapeCsv(line.Name)).Append(',');
            sb.Append(EscapeCsv(line.Unit.DisplayName())).Append(',');
            sb.Append(line.Stock.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append(line.MinStock.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append(line.DesiredStock.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append(line.SuggestedOrderQty.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendLine();
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        MessageBox.Show(this, "CSV exportado correctamente.", "Sugerido de compra",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PrintOrSavePdf()
    {
        using var doc = new ReorderSuggestionPrintDocument(_summary);
        using var preview = new PrintPreviewDialog
        {
            Document = doc,
            Width = 900,
            Height = 700,
            StartPosition = FormStartPosition.CenterParent,
            UseAntiAlias = true,
            Text = "Vista previa — Sugerido de Compra (Imprimir o Guardar como PDF)"
        };

        // Botón Imprimir del preview abre PrintDialog (incluye Microsoft Print to PDF)
        preview.ShowDialog(this);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
