using FerreSys.Domain;

namespace FerreSys.Infrastructure.UI;

/// <summary>
/// Diálogo para capturar cantidad decimal al vender a granel (Metro, Kg, Litro).
/// </summary>
public sealed class BulkQuantityForm : Form
{
    private readonly NumericUpDown _numQty;
    public decimal Quantity => _numQty.Value;

    public BulkQuantityForm(string productName, UnitOfMeasure unit, decimal maxStock)
    {
        Text = "Cantidad a vender";
        Size = new Size(360, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        int places = unit.MaxDecimalPlaces();
        decimal min = places > 0 ? 0.001m : 1m;
        decimal max = maxStock > 0 ? maxStock : 999999m;
        if (max < min) max = min;
        decimal initial = places > 0 ? Math.Min(1m, max) : 1m;
        if (initial < min) initial = min;

        var lbl = new Label
        {
            Text = $"{productName}\nUnidad: {unit.DisplayName()}  |  Disponible: {unit.FormatQuantity(maxStock)}",
            Location = new Point(20, 15),
            Size = new Size(300, 45),
            Font = new Font("Arial", 9, FontStyle.Bold)
        };

        var lblQty = new Label
        {
            Text = $"Cantidad ({unit.DisplaySuffix()}):",
            Location = new Point(20, 75),
            Size = new Size(140, 20)
        };

        _numQty = new NumericUpDown
        {
            Location = new Point(170, 73),
            Size = new Size(140, 25),
            DecimalPlaces = places,
            Increment = places > 0 ? 0.001m : 1m,
            Minimum = min,
            Maximum = max,
            Value = initial
        };

        var btnOk = new Button
        {
            Text = "Añadir",
            DialogResult = DialogResult.OK,
            Location = new Point(80, 115),
            Size = new Size(90, 32)
        };

        var btnCancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Location = new Point(180, 115),
            Size = new Size(90, 32)
        };

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        Controls.AddRange(new Control[] { lbl, lblQty, _numQty, btnOk, btnCancel });
    }
}
