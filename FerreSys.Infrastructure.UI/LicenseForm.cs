using FerreSys.Licensing;

namespace FerreSys.Infrastructure.UI;

public sealed class LicenseForm : Form
{
    private readonly LocalSubscriptionService _subscription;
    private readonly Label _status = new();
    private readonly Label _detail = new();
    private readonly TextBox _email = new();
    private readonly TextBox _code = new();
    private readonly Button _activate = new();

    public LicenseForm(LocalSubscriptionService subscription)
    {
        _subscription = subscription;
        Text = "Licencia - FerreSys";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 360);
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 6; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 1 ? 72 : 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _status.AutoSize = true;
        _status.Font = UiTheme.UiBold;
        _detail.AutoSize = true;
        _detail.MaximumSize = new Size(320, 0);

        _email.Dock = DockStyle.Fill;
        _code.Dock = DockStyle.Fill;
        _code.CharacterCasing = CharacterCasing.Upper;

        string hardwareId = HardwareFingerprint.GetDeviceId();
        var hardware = new TextBox
        {
            ReadOnly = true,
            Text = hardwareId,
            Dock = DockStyle.Fill
        };
        var copy = new Button { Text = "Copiar", Width = 72, Height = 24, Font = UiTheme.Ui };
        copy.Click += (_, _) =>
        {
            Clipboard.SetText(hardwareId);
            _detail.Text = "Identificador de esta computadora copiado.";
        };

        var hardwareRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        hardwareRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hardwareRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        hardwareRow.Controls.Add(hardware, 0, 0);
        hardwareRow.Controls.Add(copy, 1, 0);

        _activate.Text = "Activar licencia";
        _activate.AutoSize = true;
        _activate.Font = UiTheme.UiBold;
        _activate.Click += async (_, _) => await ActivateAsync();

        var import = new Button { Text = "Importar archivo .lic", AutoSize = true, Font = UiTheme.Ui };
        import.Click += (_, _) => ImportFile();

        var close = new Button { Text = "Cerrar", Width = 90, Height = 28, DialogResult = DialogResult.OK, Font = UiTheme.Ui };

        layout.Controls.Add(_status, 0, 0);
        layout.SetColumnSpan(_status, 2);
        layout.Controls.Add(_detail, 0, 1);
        layout.SetColumnSpan(_detail, 2);
        layout.Controls.Add(new Label { Text = "Esta PC", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        layout.Controls.Add(hardwareRow, 1, 2);
        layout.Controls.Add(new Label { Text = "Correo", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        layout.Controls.Add(_email, 1, 3);
        layout.Controls.Add(new Label { Text = "Clave", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
        layout.Controls.Add(_code, 1, 4);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        actions.Controls.Add(_activate);
        actions.Controls.Add(import);
        layout.Controls.Add(actions, 0, 6);
        layout.SetColumnSpan(actions, 2);
        layout.Controls.Add(close, 1, 7);

        Controls.Add(layout);
        CancelButton = close;
        AcceptButton = _activate;
        ShowStatus();
    }

    private void ShowStatus()
    {
        var license = _subscription.CurrentLicense;
        if (_subscription.GetCurrentTier() == FerreSys.Domain.SubscriptionTier.Premium && license != null)
        {
            _status.Text = "Plan Premium";
            _status.ForeColor = UiTheme.StatusOk;
            _detail.Text =
                $"{license.CustomerName}\n" +
                $"Vence: {license.ExpiresAt.ToLocalTime():dd/MM/yyyy}  (gracia {license.GraceDays} días)";
            return;
        }

        _status.Text = "Plan gratuito";
        _status.ForeColor = UiTheme.StatusWarn;
        _detail.Text = string.IsNullOrWhiteSpace(_subscription.LastError)
            ? "Hasta 200 productos. El respaldo en la nube requiere Premium."
            : _subscription.LastError;
    }

    private async Task ActivateAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenseApiSettings.BaseUrl))
        {
            MessageBox.Show(
                LicenseApiClient.MissingServerMessage,
                "Licencia",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _activate.Enabled = false;
        try
        {
            using var client = new LicenseApiClient(LicenseApiSettings.BaseUrl);
            LicenseApiResult result = await client.ActivateAsync(
                _email.Text,
                _code.Text,
                HardwareFingerprint.GetDeviceId());

            if (!result.Ok || result.License == null)
            {
                MessageBox.Show(result.Error, "Licencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_subscription.TryInstall(result.License, out string error))
            {
                MessageBox.Show(error, "Licencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowStatus();
            MessageBox.Show("Licencia activada.", "Licencia", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            _activate.Enabled = true;
        }
    }

    private void ImportFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Archivo de licencia",
            Filter = "Licencia FerreSys|*.lic|Todos los archivos|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            var container = System.Text.Json.JsonSerializer.Deserialize<FerreSys.Domain.SignedLicenseContainer>(json, LicenseJson.Options);
            if (container == null)
            {
                MessageBox.Show("Formato inválido.", "Licencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_subscription.TryInstall(container, out string error))
            {
                MessageBox.Show(error, "Licencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowStatus();
            MessageBox.Show("Licencia importada.", "Licencia", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Licencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
