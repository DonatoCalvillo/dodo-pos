using FerreSys.Domain;
using FerreSys.Infrastructure.UI.Printing;

namespace FerreSys.Infrastructure.UI;

/// <summary>
/// Diálogo para capturar cantidad decimal al vender a granel (Metro, Kg, Litro).
/// Si se indica un puerto de báscula, solo aplica a productos en kilogramos.
/// </summary>
public sealed class BulkQuantityForm : Form
{
    private readonly NumericUpDown _numQty;
    private readonly int _decimalPlaces;
    private readonly Label? _lblWeight;
    private SerialScaleReader? _scale;
    private decimal? _lastStable;
    private decimal? _lastWeight;

    public decimal Quantity => _numQty.Value;

    public BulkQuantityForm(string productName, UnitOfMeasure unit, decimal maxStock, string? scalePort = null, int scaleBaudRate = 9600)
    {
        bool useScale = unit == UnitOfMeasure.Kilogramo && !string.IsNullOrWhiteSpace(scalePort);

        Text = "Cantidad a vender";
        Size = useScale ? new Size(420, 280) : new Size(360, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        _decimalPlaces = unit.MaxDecimalPlaces();
        decimal min = _decimalPlaces > 0 ? 0.001m : 1m;
        decimal max = maxStock > 0 ? maxStock : 999999m;
        if (max < min) max = min;
        decimal initial = _decimalPlaces > 0 ? Math.Min(1m, max) : 1m;
        if (initial < min) initial = min;

        var lbl = new Label
        {
            Text = $"{productName}\nUnidad: {unit.DisplayName()}  |  Disponible: {unit.FormatQuantity(maxStock)}",
            Location = new Point(20, 15),
            Size = new Size(useScale ? 370 : 300, 45),
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
            DecimalPlaces = _decimalPlaces,
            Increment = _decimalPlaces > 0 ? 0.001m : 1m,
            Minimum = min,
            Maximum = max,
            Value = initial
        };

        int actionsTop = 115;
        if (useScale)
        {
            _lblWeight = new Label
            {
                Text = "Peso en báscula: esperando…",
                Location = new Point(20, 112),
                Size = new Size(370, 22),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            var btnUseWeight = new Button
            {
                Text = "Usar peso",
                Location = new Point(20, 142),
                Size = new Size(110, 32)
            };
            btnUseWeight.Click += (_, _) => ApplyScaleWeight();
            Controls.Add(_lblWeight);
            Controls.Add(btnUseWeight);
            actionsTop = 190;
            Shown += (_, _) => StartScale(scalePort!, scaleBaudRate);
            FormClosed += (_, _) => StopScale();
        }

        var btnOk = new Button
        {
            Text = "Añadir",
            DialogResult = DialogResult.OK,
            Location = new Point(useScale ? 140 : 80, actionsTop),
            Size = new Size(90, 32)
        };

        var btnCancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Location = new Point(useScale ? 240 : 180, actionsTop),
            Size = new Size(90, 32)
        };

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        Controls.AddRange(new Control[] { lbl, lblQty, _numQty, btnOk, btnCancel });
    }

    private void StartScale(string port, int baudRate)
    {
        try
        {
            _scale = new SerialScaleReader();
            _scale.WeightReceived += OnWeightReceived;
            _scale.Start(port, baudRate);
        }
        catch (Exception ex)
        {
            StopScale();
            if (_lblWeight != null)
                _lblWeight.Text = "Báscula no disponible. Captura la cantidad a mano.";
            MessageBox.Show(ex.Message, "Báscula", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnWeightReceived(object? sender, ScaleReading reading)
    {
        if (IsDisposed)
            return;

        BeginInvoke(() =>
        {
            _lastWeight = reading.Kilograms;
            if (reading.Stable)
                _lastStable = reading.Kilograms;
            if (_lblWeight == null)
                return;

            string state = reading.Stable ? "estable" : "inestable";
            _lblWeight.Text = $"Peso en báscula: {reading.Kilograms.ToString("0.###")} kg ({state})";
        });
    }

    private void ApplyScaleWeight()
    {
        decimal? weight = _lastStable ?? _lastWeight;
        if (weight is not > 0)
        {
            MessageBox.Show("Aún no hay un peso válido de la báscula.", "Báscula", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        decimal qty = Math.Round(weight.Value, _decimalPlaces, MidpointRounding.AwayFromZero);
        if (qty < _numQty.Minimum)
            qty = _numQty.Minimum;
        if (qty > _numQty.Maximum)
            qty = _numQty.Maximum;
        _numQty.Value = qty;
    }

    private void StopScale()
    {
        if (_scale == null)
            return;

        _scale.WeightReceived -= OnWeightReceived;
        _scale.Dispose();
        _scale = null;
    }
}
