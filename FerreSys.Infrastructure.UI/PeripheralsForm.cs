using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.UI.Printing;

namespace FerreSys.Infrastructure.UI;

public sealed class PeripheralsForm : Form
{
    private readonly IAppSettingsStore _settings;

    private ComboBox cmbTransport = null!;
    private ComboBox cmbWindowsPrinter = null!;
    private ComboBox cmbPrinterPort = null!;
    private ComboBox cmbPrinterBaud = null!;
    private TextBox txtHost = null!;
    private NumericUpDown numPort = null!;
    private CheckBox chkDrawer = null!;
    private ComboBox cmbDrawerPin = null!;

    private RadioButton rbKeyboard = null!;
    private RadioButton rbSerial = null!;
    private ComboBox cmbScannerPort = null!;
    private ComboBox cmbScannerBaud = null!;
    private Label lblLastScan = null!;

    private SerialBarcodeScanner? _testScanner;

    public PeripheralsForm(IAppSettingsStore settings)
    {
        _settings = settings;
        BuildUi();
        LoadSettings();
        UpdateTransportState();
        UpdateScannerState();
        FormClosed += (_, _) => StopTestScanner();
    }

    private void BuildUi()
    {
        Text = "Periféricos e impresión - FerreSys";
        Size = new Size(640, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiTheme.Ui;
        BackColor = UiTheme.WindowBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10),
            BackColor = UiTheme.WindowBg
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = UiTheme.Ui };
        tabs.TabPages.Add(CreatePrinterTab());
        tabs.TabPages.Add(CreateScannerTab());

        root.Controls.Add(tabs, 0, 0);
        root.Controls.Add(CreateActions(), 0, 1);
        Controls.Add(root);
    }

    private TabPage CreatePrinterTab()
    {
        var page = new TabPage("Impresora") { BackColor = UiTheme.WindowBg, Padding = new Padding(8) };
        var gb = UiHelp.CreateGroupBox("Impresora térmica ESC/POS", SectionHelpText.PeripheralsPrinter);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(8, 16, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 8; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        cmbTransport = DropDown();
        cmbTransport.Items.Add(new Choice<PrinterTransport>("Sin impresora", PrinterTransport.None));
        cmbTransport.Items.Add(new Choice<PrinterTransport>("Windows (USB)", PrinterTransport.Windows));
        cmbTransport.Items.Add(new Choice<PrinterTransport>("Puerto COM", PrinterTransport.Serial));
        cmbTransport.Items.Add(new Choice<PrinterTransport>("Red (puerto 9100)", PrinterTransport.Network));
        cmbTransport.SelectedIndexChanged += (_, _) => UpdateTransportState();

        cmbWindowsPrinter = DropDown();
        cmbPrinterPort = DropDown();
        cmbPrinterBaud = BaudCombo();
        txtHost = Field();
        numPort = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 100,
            Minimum = 1,
            Maximum = 65535,
            Value = 9100,
            Font = UiTheme.Ui
        };
        chkDrawer = new CheckBox
        {
            Text = "Abrir cajón al cobrar en efectivo o mixto",
            Dock = DockStyle.Fill,
            Font = UiTheme.Ui,
            Checked = true
        };
        cmbDrawerPin = DropDown();
        cmbDrawerPin.Items.Add(new Choice<int>("Pin 2 (conector RJ11)", 0));
        cmbDrawerPin.Items.Add(new Choice<int>("Pin 5 (conector RJ11)", 1));

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        var btnRefresh = new Button { Text = "Detectar", AutoSize = true, Font = UiTheme.Ui };
        btnRefresh.Click += (_, _) => RefreshDevices(keepSelection: true);
        var btnTest = new Button { Text = "Probar impresión", AutoSize = true, Font = UiTheme.Ui };
        btnTest.Click += (_, _) => TestPrint();
        var btnDrawer = new Button { Text = "Probar cajón", AutoSize = true, Font = UiTheme.Ui };
        btnDrawer.Click += (_, _) => TestDrawer();
        actions.Controls.AddRange([btnRefresh, btnTest, btnDrawer]);

        AddRow(grid, 0, "Conexión", cmbTransport);
        AddRow(grid, 1, "Impresora Windows", cmbWindowsPrinter);
        AddRow(grid, 2, "Puerto COM", cmbPrinterPort);
        AddRow(grid, 3, "Velocidad (baudios)", cmbPrinterBaud);
        AddRow(grid, 4, "IP o nombre", txtHost);
        AddRow(grid, 5, "Puerto de red", numPort);
        AddRow(grid, 6, "Cajón de dinero", chkDrawer);
        AddRow(grid, 7, "Señal del cajón", cmbDrawerPin);
        AddRow(grid, 8, "", actions);

        gb.Controls.Add(grid);
        page.Controls.Add(gb);
        return page;
    }

    private TabPage CreateScannerTab()
    {
        var page = new TabPage("Escáner") { BackColor = UiTheme.WindowBg, Padding = new Padding(8) };
        var gb = UiHelp.CreateGroupBox("Escáner de códigos", SectionHelpText.PeripheralsScanner);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(8, 16, 8, 8),
            BackColor = UiTheme.PanelBg
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        rbKeyboard = new RadioButton { Text = "Teclado USB (HID)", Dock = DockStyle.Fill, Checked = true, Font = UiTheme.Ui };
        rbSerial = new RadioButton { Text = "Puerto serie (COM)", Dock = DockStyle.Left, AutoSize = true, Font = UiTheme.Ui };
        rbKeyboard.CheckedChanged += (_, _) => UpdateScannerState();
        rbSerial.CheckedChanged += (_, _) => UpdateScannerState();
        var modes = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        modes.Controls.Add(rbKeyboard);
        modes.Controls.Add(rbSerial);

        cmbScannerPort = DropDown();
        cmbScannerBaud = BaudCombo();
        lblLastScan = new Label
        {
            Text = "Última lectura: —",
            Dock = DockStyle.Fill,
            Font = UiTheme.UiBold,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var btnListen = new Button { Text = "Probar lectura", AutoSize = true, Font = UiTheme.Ui };
        btnListen.Click += (_, _) => StartScannerTest();

        AddRow(grid, 0, "Modo", modes);
        AddRow(grid, 1, "Puerto COM", cmbScannerPort);
        AddRow(grid, 2, "Velocidad (baudios)", cmbScannerBaud);
        AddRow(grid, 3, "", btnListen);
        AddRow(grid, 4, "Resultado", lblLastScan);

        gb.Controls.Add(grid);
        page.Controls.Add(gb);
        return page;
    }

    private Panel CreateActions()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.WindowBg };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Width = 240
        };
        var btnCancel = new Button { Text = "Cancelar", Width = 100, Height = 32, Font = UiTheme.Ui, DialogResult = DialogResult.Cancel };
        var btnSave = new Button { Text = "Guardar", Width = 100, Height = 32, Font = UiTheme.UiBold };
        btnSave.Click += (_, _) => SaveAndClose();
        buttons.Controls.Add(btnCancel);
        buttons.Controls.Add(btnSave);
        panel.Controls.Add(buttons);
        CancelButton = btnCancel;
        AcceptButton = btnSave;
        return panel;
    }

    private void LoadSettings()
    {
        RefreshDevices(keepSelection: false);
        PeripheralSettings devices = _settings.GetPeripheralSettings();

        SelectChoice(cmbTransport, devices.PrinterTransport);
        SelectText(cmbWindowsPrinter, devices.WindowsPrinterName);
        SelectText(cmbPrinterPort, devices.PrinterSerialPort);
        SelectText(cmbPrinterBaud, devices.PrinterBaudRate.ToString());
        txtHost.Text = devices.NetworkHost;
        numPort.Value = devices.NetworkPort is >= 1 and <= 65535 ? devices.NetworkPort : 9100;
        chkDrawer.Checked = devices.OpenDrawerOnCash;
        SelectChoice(cmbDrawerPin, devices.DrawerPin);

        rbSerial.Checked = devices.ScannerMode == ScannerMode.Serial;
        rbKeyboard.Checked = devices.ScannerMode != ScannerMode.Serial;
        SelectText(cmbScannerPort, devices.ScannerSerialPort);
        SelectText(cmbScannerBaud, devices.ScannerBaudRate.ToString());
    }

    private void RefreshDevices(bool keepSelection)
    {
        string? printer = keepSelection ? cmbWindowsPrinter.Text : null;
        string? printerPort = keepSelection ? cmbPrinterPort.Text : null;
        string? scannerPort = keepSelection ? cmbScannerPort.Text : null;

        Fill(cmbWindowsPrinter, PeripheralDiscovery.InstalledPrinters(), printer);
        Fill(cmbPrinterPort, PeripheralDiscovery.SerialPorts(), printerPort);
        Fill(cmbScannerPort, PeripheralDiscovery.SerialPorts(), scannerPort);
    }

    private void UpdateTransportState()
    {
        var transport = Selected(cmbTransport, PrinterTransport.None);
        cmbWindowsPrinter.Enabled = transport == PrinterTransport.Windows;
        cmbPrinterPort.Enabled = transport == PrinterTransport.Serial;
        cmbPrinterBaud.Enabled = transport == PrinterTransport.Serial;
        txtHost.Enabled = transport == PrinterTransport.Network;
        numPort.Enabled = transport == PrinterTransport.Network;
    }

    private void UpdateScannerState()
    {
        bool serial = rbSerial.Checked;
        cmbScannerPort.Enabled = serial;
        cmbScannerBaud.Enabled = serial;
        if (!serial)
            StopTestScanner();
    }

    private void SaveAndClose()
    {
        var devices = ReadDevices();
        if (devices.PrinterTransport == PrinterTransport.Windows && string.IsNullOrWhiteSpace(devices.WindowsPrinterName))
        {
            MessageBox.Show("Selecciona la impresora de Windows.", "Impresora", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (devices.PrinterTransport == PrinterTransport.Serial && string.IsNullOrWhiteSpace(devices.PrinterSerialPort))
        {
            MessageBox.Show("Selecciona el puerto COM de la impresora.", "Impresora", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (devices.PrinterTransport == PrinterTransport.Network && string.IsNullOrWhiteSpace(devices.NetworkHost))
        {
            MessageBox.Show("Escribe la IP o el nombre de la impresora de red.", "Impresora", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (devices.ScannerMode == ScannerMode.Serial && string.IsNullOrWhiteSpace(devices.ScannerSerialPort))
        {
            MessageBox.Show("Selecciona el puerto COM del escáner.", "Escáner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (devices.ScannerMode == ScannerMode.Serial
            && devices.PrinterTransport == PrinterTransport.Serial
            && string.Equals(devices.ScannerSerialPort, devices.PrinterSerialPort, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("La impresora y el escáner no pueden usar el mismo puerto COM.", "Periféricos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _settings.SavePeripheralSettings(devices);
            StopTestScanner();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void TestPrint()
    {
        try
        {
            BusinessTicketProfile profile = _settings.GetTicketProfile();
            var devices = ReadDevices();
            if (!devices.HasPrinterConfigured)
            {
                MessageBox.Show("Configura la conexión de la impresora antes de probar.", "Impresora", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            byte[]? logo = TicketLogoPrinter.TryEncode(_settings, profile);
            byte[] payload = EscPosTicketBuilder.Build(TicketFormatter.GenerateTest(profile), logo, openDrawer: false, devices.DrawerPin);
            ThermalPrinterFactory.Create(devices).Write(payload);
            MessageBox.Show("Se envió el ticket de prueba.", "Impresora", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo imprimir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void TestDrawer()
    {
        try
        {
            var devices = ReadDevices();
            if (!devices.HasPrinterConfigured)
            {
                MessageBox.Show("Configura la impresora: el cajón se abre por el puerto de la térmica.", "Cajón", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ThermalPrinterFactory.Create(devices).Write(EscPosTicketBuilder.DrawerPulse(devices.DrawerPin));
            MessageBox.Show("Se envió la señal de apertura del cajón.", "Cajón", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "No se pudo abrir el cajón", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void StartScannerTest()
    {
        if (!rbSerial.Checked)
        {
            lblLastScan.Text = "Modo teclado: enfoca el buscador del mostrador y escanea.";
            return;
        }

        string? port = cmbScannerPort.SelectedItem as string ?? cmbScannerPort.Text;
        if (string.IsNullOrWhiteSpace(port))
        {
            MessageBox.Show("No hay puertos COM detectados para el escáner.", "Escáner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            StopTestScanner();
            _testScanner = new SerialBarcodeScanner();
            _testScanner.BarcodeReceived += (_, code) =>
            {
                if (IsDisposed)
                    return;
                BeginInvoke(() => lblLastScan.Text = "Última lectura: " + code);
            };
            _testScanner.Start(port, SelectedBaud(cmbScannerBaud));
            lblLastScan.Text = $"Escuchando {port}. Escanea un código…";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Escáner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void StopTestScanner()
    {
        _testScanner?.Dispose();
        _testScanner = null;
    }

    private PeripheralSettings ReadDevices() => new()
    {
        PrinterTransport = Selected(cmbTransport, PrinterTransport.None),
        WindowsPrinterName = cmbWindowsPrinter.Text.Trim(),
        PrinterSerialPort = cmbPrinterPort.Text.Trim(),
        PrinterBaudRate = SelectedBaud(cmbPrinterBaud),
        NetworkHost = txtHost.Text.Trim(),
        NetworkPort = (int)numPort.Value,
        OpenDrawerOnCash = chkDrawer.Checked,
        DrawerPin = Selected(cmbDrawerPin, 0),
        ScannerMode = rbSerial.Checked ? ScannerMode.Serial : ScannerMode.Keyboard,
        ScannerSerialPort = cmbScannerPort.Text.Trim(),
        ScannerBaudRate = SelectedBaud(cmbScannerBaud)
    };

    private static TextBox Field() => new() { Dock = DockStyle.Fill, Font = UiTheme.Ui };

    private static ComboBox DropDown() => new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = UiTheme.Ui
    };

    private static ComboBox BaudCombo()
    {
        var combo = DropDown();
        foreach (int baud in PeripheralDiscovery.BaudRates)
            combo.Items.Add(baud.ToString());
        combo.SelectedIndex = 0;
        return combo;
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control editor)
    {
        grid.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Ui
        }, 0, row);
        grid.Controls.Add(editor, 1, row);
    }

    private static void Fill(ComboBox combo, IReadOnlyList<string> items, string? preferred)
    {
        combo.Items.Clear();
        foreach (string item in items)
            combo.Items.Add(item);
        if (!string.IsNullOrWhiteSpace(preferred) && !combo.Items.Contains(preferred))
            combo.Items.Insert(0, preferred);
        SelectText(combo, preferred);
    }

    private static void SelectText(ComboBox combo, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            if (combo.Items.Count > 0 && combo.SelectedIndex < 0)
                combo.SelectedIndex = 0;
            return;
        }

        int index = combo.FindStringExact(text);
        if (index < 0)
        {
            combo.Items.Insert(0, text);
            index = 0;
        }

        combo.SelectedIndex = index;
    }

    private static void SelectChoice<T>(ComboBox combo, T value) where T : notnull
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is Choice<T> choice && EqualityComparer<T>.Default.Equals(choice.Value, value))
            {
                combo.SelectedIndex = i;
                return;
            }
        }

        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    private static T Selected<T>(ComboBox combo, T fallback)
    {
        return combo.SelectedItem is Choice<T> choice ? choice.Value : fallback;
    }

    private static int SelectedBaud(ComboBox combo) =>
        int.TryParse(combo.Text, out int baud) && baud > 0 ? baud : 9600;

    private sealed class Choice<T>(string label, T value)
    {
        public T Value { get; } = value;
        public override string ToString() => label;
    }
}
