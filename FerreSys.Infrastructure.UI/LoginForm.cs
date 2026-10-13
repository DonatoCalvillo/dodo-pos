using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;
using FerreSys.Infrastructure.UI.Printing;

namespace FerreSys.Infrastructure.UI;

internal sealed class LoginForm : Form
{
    private readonly TabControl _tabs;
    private readonly TextBox _scan;
    private readonly TextBox _username;
    private readonly TextBox _password;
    private readonly Label _status;
    private readonly AuthenticateUserUseCase _authenticate;
    private readonly SqliteAppSettingsStore _settings;
    private SerialBarcodeScanner? _scanner;

    public User? User { get; private set; }

    public LoginForm()
    {
        var database = new SqliteDbContext();
        var users = new SqliteUserRepository(database);
        _authenticate = new AuthenticateUserUseCase(users);
        _settings = new SqliteAppSettingsStore(database);

        Text = "Posly - Iniciar sesión";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 280);
        Font = UiTheme.Ui;
        Icon = Branding.AppIcon;

        var title = new Label
        {
            Text = "Inicia sesión para usar el punto de venta",
            Font = UiTheme.UiBold,
            AutoSize = false,
            Height = 28,
            Dock = DockStyle.Top,
            Padding = new Padding(12, 8, 12, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _scan = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        _username = new TextBox { Dock = DockStyle.Fill };
        _password = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        _status = new Label
        {
            AutoSize = false,
            Height = 36,
            Dock = DockStyle.Bottom,
            Padding = new Padding(12, 0, 12, 0),
            ForeColor = UiTheme.StatusDanger,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var credentialPage = new TabPage("Credencial") { Padding = new Padding(12, 16, 12, 8) };
        var credentialGrid = FieldGrid();
        credentialGrid.Controls.Add(FieldLabel("Código"), 0, 0);
        credentialGrid.Controls.Add(_scan, 1, 0);
        credentialPage.Controls.Add(credentialGrid);

        var accountPage = new TabPage("Usuario") { Padding = new Padding(12, 16, 12, 8) };
        var accountGrid = FieldGrid();
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        accountGrid.Controls.Add(FieldLabel("Usuario"), 0, 0);
        accountGrid.Controls.Add(_username, 1, 0);
        accountGrid.Controls.Add(FieldLabel("Contraseña"), 0, 1);
        accountGrid.Controls.Add(_password, 1, 1);
        accountPage.Controls.Add(accountGrid);

        _tabs = new TabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.Add(credentialPage);
        _tabs.TabPages.Add(accountPage);
        _tabs.SelectedIndexChanged += (_, _) => FocusActiveField();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44,
            Padding = new Padding(12, 6, 12, 6)
        };
        var enter = new Button { Text = "Entrar", Width = 100, DialogResult = DialogResult.None };
        var exit = new Button { Text = "Salir", Width = 100, DialogResult = DialogResult.Cancel };
        enter.Click += (_, _) => TryEnter();
        buttons.Controls.Add(exit);
        buttons.Controls.Add(enter);

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 4, 12, 0) };
        root.Controls.Add(_tabs);
        root.Controls.Add(title);

        Controls.Add(root);
        Controls.Add(buttons);
        Controls.Add(_status);
        AcceptButton = enter;
        CancelButton = exit;

        _scan.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            TryBarcode(_scan.Text);
        };
        _password.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            TryEnter();
        };

        Shown += (_, _) =>
        {
            StartScanner();
            _scan.Focus();
        };
        FormClosed += (_, _) => StopScanner();
    }

    private void TryBarcode(string code)
    {
        if (!User.LooksLikeEmployeeBarcode(code))
        {
            ShowError("Escanea la credencial del empleado.");
            return;
        }

        try
        {
            User = _authenticate.ExecuteBarcode(code);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            _scan.SelectAll();
        }
    }

    private void TryEnter()
    {
        if (_tabs.SelectedIndex == 0)
        {
            TryBarcode(_scan.Text);
            return;
        }

        try
        {
            User = _authenticate.ExecuteCredentials(_username.Text, _password.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            _password.SelectAll();
            _password.Focus();
        }
    }

    private void ShowError(string message) => _status.Text = message;

    private void StartScanner()
    {
        PeripheralSettings devices = _settings.GetPeripheralSettings();
        if (devices.ScannerMode != ScannerMode.Serial || string.IsNullOrWhiteSpace(devices.ScannerSerialPort))
            return;

        try
        {
            _scanner = new SerialBarcodeScanner();
            _scanner.BarcodeReceived += OnSerialBarcode;
            _scanner.Start(devices.ScannerSerialPort, devices.ScannerBaudRate);
        }
        catch (Exception ex)
        {
            StopScanner();
            ShowError($"Escáner no disponible: {ex.Message}");
        }
    }

    private void OnSerialBarcode(object? sender, string code)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() => OnSerialBarcode(sender, code));
            return;
        }

        _tabs.SelectedIndex = 0;
        _scan.Text = code;
        TryBarcode(code);
    }

    private void FocusActiveField()
    {
        _status.Text = string.Empty;
        if (_tabs.SelectedIndex == 0)
            _scan.Focus();
        else
            _username.Focus();
    }

    private static TableLayoutPanel FieldGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            Height = 72,
            AutoSize = false
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        return grid;
    }

    private void StopScanner()
    {
        if (_scanner == null)
            return;

        _scanner.BarcodeReceived -= OnSerialBarcode;
        _scanner.Dispose();
        _scanner = null;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };
}
