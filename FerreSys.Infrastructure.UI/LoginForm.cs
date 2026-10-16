using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;
using FerreSys.Infrastructure.UI.Printing;

namespace FerreSys.Infrastructure.UI;

internal sealed class LoginForm : Form
{
    private static readonly Color BrandPrimary = Color.FromArgb(0, 149, 217);
    private static readonly Color BrandPrimaryHover = Color.FromArgb(2, 132, 199);
    private static readonly Color BrandPrimaryActive = Color.FromArgb(3, 105, 161);
    private static readonly Color SidebarDarkTop = Color.FromArgb(15, 23, 42);
    private static readonly Color SidebarDarkBottom = Color.FromArgb(30, 41, 59);
    private static readonly Color SurfaceWhite = Color.FromArgb(255, 255, 255);
    private static readonly Color SurfaceMuted = Color.FromArgb(241, 245, 249);
    private static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
    private static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);
    private static readonly Color BorderSubtle = Color.FromArgb(226, 232, 240);
    private static readonly Color DangerBg = Color.FromArgb(254, 242, 242);
    private static readonly Color DangerBorder = Color.FromArgb(254, 202, 202);
    private static readonly Color DangerText = Color.FromArgb(185, 28, 28);

    private readonly Font _fontTitle = new("Segoe UI", 19f, FontStyle.Bold);
    private readonly Font _fontSubtitle = new("Segoe UI", 9.5f, FontStyle.Regular);
    private readonly Font _fontLabel = new("Segoe UI Semibold", 8.75f, FontStyle.Bold);
    private readonly Font _fontInput = new("Segoe UI", 10.5f, FontStyle.Regular);
    private readonly Font _fontButton = new("Segoe UI Semibold", 10f, FontStyle.Bold);
    private readonly Font _fontSmall = new("Segoe UI", 8.75f, FontStyle.Regular);

    private readonly SegmentedToggle _modeToggle;
    private readonly Panel _credentialPanel;
    private readonly Panel _accountPanel;
    private readonly ModernInputBox _scanInput;
    private readonly ModernInputBox _usernameInput;
    private readonly ModernInputBox _passwordInput;
    private readonly ErrorBanner _errorBanner;
    private readonly ModernButton _enterButton;
    private readonly ModernButton _exitButton;

    private readonly AuthenticateUserUseCase _authenticate;
    private readonly SqliteAppSettingsStore _settings;
    private SerialBarcodeScanner? _scanner;
    private string _scannerBadgeText = "Escáner listo (USB / Teclado)";
    private Color _scannerBadgeColor = Color.FromArgb(56, 189, 248);

    public User? User { get; private set; }

    public LoginForm()
    {
        var database = new SqliteDbContext();
        var users = new SqliteUserRepository(database);
        _authenticate = new AuthenticateUserUseCase(users);
        _settings = new SqliteAppSettingsStore(database);

        Text = "Posly - Iniciar sesión";
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(840, 520);
        BackColor = SurfaceWhite;
        Font = _fontSubtitle;
        Icon = Branding.AppIcon;
        DoubleBuffered = true;
        KeyPreview = true;

        // Left branding hero panel
        var heroPanel = new BufferedPanel
        {
            Dock = DockStyle.Left,
            Width = 320
        };
        heroPanel.Paint += PaintHeroPanel;
        heroPanel.MouseDown += OnDragWindowMouseDown;

        // Right authentication container
        var rightPanel = new BufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceWhite
        };
        rightPanel.Paint += (_, e) =>
        {
            using var borderPen = new Pen(BorderSubtle, 1f);
            e.Graphics.DrawRectangle(borderPen, 0, 0, rightPanel.Width - 1, rightPanel.Height - 1);
        };
        rightPanel.MouseDown += OnDragWindowMouseDown;

        // Top-right window close button
        var closeBtn = new CloseWindowButton
        {
            Location = new Point(520 - 44, 12),
            Size = new Size(32, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        closeBtn.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        rightPanel.Controls.Add(closeBtn);

        int contentLeft = 56;
        int contentWidth = 408;

        var titleLabel = new Label
        {
            Text = "Bienvenido de nuevo",
            Font = _fontTitle,
            ForeColor = TextPrimary,
            BackColor = Color.Transparent,
            AutoSize = false,
            Bounds = new Rectangle(contentLeft, 42, contentWidth, 38)
        };
        titleLabel.MouseDown += OnDragWindowMouseDown;

        var subtitleLabel = new Label
        {
            Text = "Inicia sesión para acceder a la terminal de punto de venta",
            Font = _fontSubtitle,
            ForeColor = TextSecondary,
            BackColor = Color.Transparent,
            AutoSize = false,
            Bounds = new Rectangle(contentLeft, 80, contentWidth, 22)
        };
        subtitleLabel.MouseDown += OnDragWindowMouseDown;

        _modeToggle = new SegmentedToggle("Credencial", "Usuario y clave")
        {
            Bounds = new Rectangle(contentLeft, 118, contentWidth, 42)
        };
        _modeToggle.SelectedIndexChanged += (_, _) => SwitchMode(_modeToggle.SelectedIndex);

        // Mode 0: Credential / Barcode panel
        _credentialPanel = new BufferedPanel
        {
            Bounds = new Rectangle(contentLeft, 176, contentWidth, 168),
            BackColor = SurfaceWhite
        };

        var scanCard = new ScanVisualCard
        {
            Bounds = new Rectangle(0, 0, contentWidth, 94)
        };

        var scanLabel = CreateFieldLabel("CÓDIGO DE EMPLEADO", 0, 104, contentWidth);
        _scanInput = new ModernInputBox(
            InputIconType.Barcode,
            placeholder: "Escanea tu gafete o escribe el código...",
            isPassword: true,
            allowPasswordToggle: false,
            font: _fontInput)
        {
            Bounds = new Rectangle(0, 124, contentWidth, 42)
        };
        scanCard.Click += (_, _) => _scanInput.FocusInput();
        _scanInput.FocusChanged += (_, focused) => scanCard.SetActive(focused);

        _credentialPanel.Controls.Add(scanCard);
        _credentialPanel.Controls.Add(scanLabel);
        _credentialPanel.Controls.Add(_scanInput);

        // Mode 1: Username & Password panel
        _accountPanel = new BufferedPanel
        {
            Bounds = new Rectangle(contentLeft, 176, contentWidth, 168),
            BackColor = SurfaceWhite,
            Visible = false
        };

        var userLabel = CreateFieldLabel("NOMBRE DE USUARIO", 0, 2, contentWidth);
        _usernameInput = new ModernInputBox(
            InputIconType.User,
            placeholder: "Ingresa tu usuario (ej. admin)",
            isPassword: false,
            allowPasswordToggle: false,
            font: _fontInput)
        {
            Bounds = new Rectangle(0, 24, contentWidth, 42)
        };

        var passLabel = CreateFieldLabel("CONTRASEÑA", 0, 82, contentWidth);
        _passwordInput = new ModernInputBox(
            InputIconType.Lock,
            placeholder: "Ingresa tu contraseña",
            isPassword: true,
            allowPasswordToggle: true,
            font: _fontInput)
        {
            Bounds = new Rectangle(0, 104, contentWidth, 42)
        };

        _accountPanel.Controls.Add(userLabel);
        _accountPanel.Controls.Add(_usernameInput);
        _accountPanel.Controls.Add(passLabel);
        _accountPanel.Controls.Add(_passwordInput);

        // Error Banner
        _errorBanner = new ErrorBanner(_fontSmall)
        {
            Bounds = new Rectangle(contentLeft, 354, contentWidth, 38),
            Visible = false
        };

        // Primary & Secondary Buttons
        _enterButton = new ModernButton("Iniciar sesión", isPrimary: true, _fontButton)
        {
            Bounds = new Rectangle(contentLeft, 404, contentWidth, 44)
        };
        _enterButton.Click += (_, _) => TryEnter();

        _exitButton = new ModernButton("Salir de Posly", isPrimary: false, _fontSmall)
        {
            Bounds = new Rectangle(contentLeft + (contentWidth - 140) / 2, 456, 140, 32)
        };
        _exitButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        rightPanel.Controls.Add(titleLabel);
        rightPanel.Controls.Add(subtitleLabel);
        rightPanel.Controls.Add(_modeToggle);
        rightPanel.Controls.Add(_credentialPanel);
        rightPanel.Controls.Add(_accountPanel);
        rightPanel.Controls.Add(_errorBanner);
        rightPanel.Controls.Add(_enterButton);
        rightPanel.Controls.Add(_exitButton);

        Controls.Add(rightPanel);
        Controls.Add(heroPanel);

        _scanInput.InnerTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            TryBarcode(_scanInput.Text);
        };

        _usernameInput.InnerTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            _passwordInput.FocusInput();
        };

        _passwordInput.InnerTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            TryEnter();
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        Shown += (_, _) =>
        {
            ApplyRoundedWindowCorners();
            StartScanner();
            heroPanel.Invalidate();
            FocusActiveField();
        };
        FormClosed += (_, _) => StopScanner();
    }

    private Label CreateFieldLabel(string text, int x, int y, int width) => new()
    {
        Text = text,
        Font = _fontLabel,
        ForeColor = Color.FromArgb(71, 85, 105),
        BackColor = Color.Transparent,
        AutoSize = false,
        Bounds = new Rectangle(x, y, width, 18),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private void SwitchMode(int index)
    {
        _credentialPanel.Visible = index == 0;
        _accountPanel.Visible = index == 1;
        FocusActiveField();
    }

    private void TryBarcode(string code)
    {
        if (!User.LooksLikeEmployeeBarcode(code))
        {
            ShowError("Escanea o ingresa una credencial de empleado válida.");
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
            _scanInput.SelectAll();
        }
    }

    private void TryEnter()
    {
        if (_modeToggle.SelectedIndex == 0)
        {
            TryBarcode(_scanInput.Text);
            return;
        }

        try
        {
            User = _authenticate.ExecuteCredentials(_usernameInput.Text, _passwordInput.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            _passwordInput.SelectAll();
            _passwordInput.FocusInput();
        }
    }

    private void ShowError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _errorBanner.Visible = false;
            return;
        }

        _errorBanner.Message = message;
        _errorBanner.Visible = true;
    }

    private void StartScanner()
    {
        PeripheralSettings devices = _settings.GetPeripheralSettings();
        if (devices.ScannerMode != ScannerMode.Serial || string.IsNullOrWhiteSpace(devices.ScannerSerialPort))
        {
            _scannerBadgeText = "Escáner activo (Teclado / USB)";
            _scannerBadgeColor = Color.FromArgb(56, 189, 248);
            return;
        }

        try
        {
            _scanner = new SerialBarcodeScanner();
            _scanner.BarcodeReceived += OnSerialBarcode;
            _scanner.Start(devices.ScannerSerialPort, devices.ScannerBaudRate);
            _scannerBadgeText = $"Escáner serial ({devices.ScannerSerialPort})";
            _scannerBadgeColor = Color.FromArgb(74, 222, 128);
        }
        catch (Exception ex)
        {
            StopScanner();
            _scannerBadgeText = "Escáner serial sin conexión";
            _scannerBadgeColor = Color.FromArgb(251, 191, 36);
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

        _modeToggle.SelectedIndex = 0;
        SwitchMode(0);
        _scanInput.Text = code;
        TryBarcode(code);
    }

    private void FocusActiveField()
    {
        ShowError(string.Empty);
        if (_modeToggle.SelectedIndex == 0)
            _scanInput.FocusInput();
        else
            _usernameInput.FocusInput();
    }

    private void StopScanner()
    {
        if (_scanner == null)
            return;

        _scanner.BarcodeReceived -= OnSerialBarcode;
        _scanner.Dispose();
        _scanner = null;
    }

    private void PaintHeroPanel(object? sender, PaintEventArgs e)
    {
        if (sender is not Control panel)
            return;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var bgBrush = new LinearGradientBrush(
            panel.ClientRectangle,
            SidebarDarkTop,
            SidebarDarkBottom,
            LinearGradientMode.ForwardDiagonal);
        g.FillRectangle(bgBrush, panel.ClientRectangle);

        // Decorative glowing circles
        using (var glowBrush1 = new SolidBrush(Color.FromArgb(18, 0, 149, 217)))
        using (var glowBrush2 = new SolidBrush(Color.FromArgb(12, 56, 189, 248)))
        {
            g.FillEllipse(glowBrush1, -70, -60, 260, 260);
            g.FillEllipse(glowBrush2, 120, 310, 280, 280);
        }

        // Logo card container
        var logoBoxRect = new Rectangle(36, 48, 76, 76);
        using (var cardPath = RoundedRect(logoBoxRect, 18))
        using (var cardBrush = new SolidBrush(Color.FromArgb(255, 255, 255)))
        {
            g.FillPath(cardBrush, cardPath);
        }

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(Branding.Logo, new Rectangle(46, 58, 56, 56));

        // Brand Name & Tagline
        using var brandFont = new Font("Segoe UI", 22f, FontStyle.Bold);
        using var tagFont = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
        using var descFont = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        using var whiteBrush = new SolidBrush(Color.White);
        using var cyanBrush = new SolidBrush(Color.FromArgb(56, 189, 248));
        using var mutedBrush = new SolidBrush(Color.FromArgb(148, 163, 184));

        g.DrawString("Posly", brandFont, whiteBrush, new PointF(32, 142));
        g.DrawString("PUNTO DE VENTA MODERNO", tagFont, cyanBrush, new PointF(36, 184));

        var descRect = new RectangleF(36, 214, 248, 80);
        g.DrawString(
            "Control ágil de ventas, inventario, corte de caja y facturación diseñado para alta velocidad en mostrador.",
            descFont,
            mutedBrush,
            descRect);

        // Feature bullets
        DrawFeatureBullet(g, 36, 310, "Acceso rápido con gafete o código");
        DrawFeatureBullet(g, 36, 342, "Control de turnos y arqueo seguro");
        DrawFeatureBullet(g, 36, 374, "Sincronización con periféricos POS");

        // Bottom live scanner status pill
        var pillRect = new Rectangle(36, panel.Height - 62, panel.Width - 72, 34);
        using (var pillPath = RoundedRect(pillRect, 17))
        using (var pillBg = new SolidBrush(Color.FromArgb(35, 255, 255, 255)))
        using (var pillBorder = new Pen(Color.FromArgb(45, 255, 255, 255), 1f))
        {
            g.FillPath(pillBg, pillPath);
            g.DrawPath(pillBorder, pillPath);
        }

        using (var dotBrush = new SolidBrush(_scannerBadgeColor))
        {
            g.FillEllipse(dotBrush, pillRect.X + 14, pillRect.Y + 12, 10, 10);
        }

        using var badgeFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var badgeTextBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
        var textRect = new RectangleF(pillRect.X + 32, pillRect.Y, pillRect.Width - 40, pillRect.Height);
        var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
        g.DrawString(_scannerBadgeText, badgeFont, badgeTextBrush, textRect, sf);
    }

    private static void DrawFeatureBullet(Graphics g, int x, int y, string text)
    {
        using var checkBg = new SolidBrush(Color.FromArgb(35, 0, 149, 217));
        g.FillEllipse(checkBg, x, y, 20, 20);

        using var checkPen = new Pen(Color.FromArgb(56, 189, 248), 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLines(checkPen, [new Point(x + 5, y + 10), new Point(x + 9, y + 14), new Point(x + 15, y + 6)]);

        using var font = new Font("Segoe UI", 8.75f, FontStyle.Regular);
        using var brush = new SolidBrush(Color.FromArgb(203, 213, 225));
        g.DrawString(text, font, brush, new PointF(x + 28, y + 1));
    }

    private void OnDragWindowMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReleaseCapture();
        SendMessage(Handle, 0x00A1 /* WM_NCLBUTTONDOWN */, 0x0002 /* HTCAPTION */, 0);
    }

    private void ApplyRoundedWindowCorners()
    {
        try
        {
            int preference = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(Handle, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref preference, sizeof(int));
        }
        catch
        {
            // Ignore on older Windows versions that do not support DWM corner attribute
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

    internal static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();
        if (radius <= 0 || diameter > bounds.Width || diameter > bounds.Height)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private enum InputIconType
    {
        Barcode,
        User,
        Lock
    }

    private sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }

    private sealed class SegmentedToggle : Control
    {
        private readonly string _leftText;
        private readonly string _rightText;
        private int _selectedIndex;
        private int _hoverIndex = -1;

        public event EventHandler? SelectedIndexChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value)
                    return;
                _selectedIndex = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public SegmentedToggle(string leftText, string rightText)
        {
            _leftText = leftText;
            _rightText = rightText;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = e.X < Width / 2 ? 0 : 1;
            if (_hoverIndex != idx)
            {
                _hoverIndex = idx;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                SelectedIndex = e.X < Width / 2 ? 0 : 1;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var outer = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var bgPath = RoundedRect(outer, 10))
            using (var bgBrush = new SolidBrush(SurfaceMuted))
            {
                g.FillPath(bgBrush, bgPath);
            }

            int halfWidth = (Width - 8) / 2;
            var activeRect = new Rectangle(
                _selectedIndex == 0 ? 4 : 4 + halfWidth,
                4,
                halfWidth,
                Height - 8);

            using (var activePath = RoundedRect(activeRect, 7))
            using (var activeBrush = new SolidBrush(SurfaceWhite))
            using (var borderPen = new Pen(Color.FromArgb(203, 213, 225), 1f))
            {
                g.FillPath(activeBrush, activePath);
                g.DrawPath(borderPen, activePath);
            }

            using var activeFont = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold);
            using var inactiveFont = new Font("Segoe UI", 9.25f, FontStyle.Regular);

            DrawSegment(g, new Rectangle(4, 4, halfWidth, Height - 8), _leftText, _selectedIndex == 0, _hoverIndex == 0, activeFont, inactiveFont);
            DrawSegment(g, new Rectangle(4 + halfWidth, 4, halfWidth, Height - 8), _rightText, _selectedIndex == 1, _hoverIndex == 1, activeFont, inactiveFont);
        }

        private static void DrawSegment(Graphics g, Rectangle rect, string text, bool active, bool hovered, Font activeFont, Font inactiveFont)
        {
            Color color = active
                ? BrandPrimary
                : hovered
                    ? TextPrimary
                    : TextSecondary;

            using var brush = new SolidBrush(color);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, active ? activeFont : inactiveFont, brush, rect, sf);
        }
    }

    private sealed class ScanVisualCard : Control
    {
        private bool _active = true;

        public ScanVisualCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public void SetActive(bool active)
        {
            if (_active == active)
                return;
            _active = active;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(1, 1, Width - 2, Height - 2);
            Color bg = _active ? Color.FromArgb(240, 249, 255) : Color.FromArgb(248, 250, 252);
            Color border = _active ? Color.FromArgb(125, 211, 252) : BorderSubtle;

            using (var path = RoundedRect(rect, 10))
            using (var brush = new SolidBrush(bg))
            using (var pen = new Pen(border, 1.2f) { DashStyle = DashStyle.Dash })
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // Barcode icon badge on the left
            var iconBadge = new Rectangle(18, (Height - 48) / 2, 48, 48);
            using (var badgePath = RoundedRect(iconBadge, 12))
            using (var badgeBrush = new SolidBrush(_active ? Color.FromArgb(224, 242, 254) : SurfaceMuted))
            {
                g.FillPath(badgeBrush, badgePath);
            }

            // Draw barcode bars inside badge
            Color barColor = _active ? BrandPrimary : TextSecondary;
            using (var barPen = new Pen(barColor, 2f))
            {
                int bx = iconBadge.X + 12;
                int by = iconBadge.Y + 13;
                int bh = 22;
                int[] offsets = [0, 4, 9, 12, 17, 21, 24];
                foreach (int offset in offsets)
                {
                    g.DrawLine(barPen, bx + offset, by, bx + offset, by + bh);
                }
            }

            using var titleFont = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 8.75f, FontStyle.Regular);
            using var titleBrush = new SolidBrush(TextPrimary);
            using var subBrush = new SolidBrush(TextSecondary);

            g.DrawString("Listo para escanear gafete", titleFont, titleBrush, new PointF(80, 24));
            g.DrawString("Pasa tu credencial por el lector o teclea tu código abajo.", subFont, subBrush, new PointF(80, 47));
        }
    }

    private sealed class ModernInputBox : Control
    {
        private readonly TextBox _textBox;
        private readonly InputIconType _icon;
        private readonly bool _allowPasswordToggle;
        private bool _passwordHidden;
        private bool _focused;
        private bool _eyeHovered;

        public event EventHandler<bool>? FocusChanged;

        public TextBox InnerTextBox => _textBox;

        [AllowNull]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text
        {
            get => _textBox.Text;
            set => _textBox.Text = value ?? string.Empty;
        }

        public ModernInputBox(InputIconType icon, string placeholder, bool isPassword, bool allowPasswordToggle, Font font)
        {
            _icon = icon;
            _allowPasswordToggle = allowPasswordToggle;
            _passwordHidden = isPassword;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = SurfaceWhite;
            Cursor = Cursors.IBeam;

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = font,
                ForeColor = TextPrimary,
                BackColor = Color.FromArgb(248, 250, 252),
                PlaceholderText = placeholder,
                UseSystemPasswordChar = isPassword
            };

            _textBox.GotFocus += (_, _) =>
            {
                _focused = true;
                _textBox.BackColor = SurfaceWhite;
                Invalidate();
                FocusChanged?.Invoke(this, true);
            };

            _textBox.LostFocus += (_, _) =>
            {
                _focused = false;
                _textBox.BackColor = Color.FromArgb(248, 250, 252);
                Invalidate();
                FocusChanged?.Invoke(this, false);
            };

            Controls.Add(_textBox);
            LayoutTextBox();
        }

        public void FocusInput() => _textBox.Focus();

        public void SelectAll() => _textBox.SelectAll();

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutTextBox();
        }

        private void LayoutTextBox()
        {
            int leftPad = 40;
            int rightPad = _allowPasswordToggle ? 40 : 14;
            int textHeight = _textBox.PreferredHeight;
            _textBox.Bounds = new Rectangle(
                leftPad,
                Math.Max(6, (Height - textHeight) / 2),
                Math.Max(40, Width - leftPad - rightPad),
                textHeight);
        }

        private Rectangle EyeButtonRect => new(Width - 36, (Height - 28) / 2, 28, 28);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_allowPasswordToggle)
                return;

            bool overEye = EyeButtonRect.Contains(e.Location);
            Cursor = overEye ? Cursors.Hand : Cursors.IBeam;
            if (_eyeHovered != overEye)
            {
                _eyeHovered = overEye;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_eyeHovered)
            {
                _eyeHovered = false;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_allowPasswordToggle && EyeButtonRect.Contains(e.Location))
            {
                _passwordHidden = !_passwordHidden;
                _textBox.UseSystemPasswordChar = _passwordHidden;
                _textBox.Focus();
                Invalidate();
                return;
            }

            _textBox.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            Color fill = _focused ? SurfaceWhite : Color.FromArgb(248, 250, 252);
            Color borderColor = _focused ? BrandPrimary : BorderSubtle;
            float borderWidth = _focused ? 1.8f : 1f;

            using (var path = RoundedRect(rect, 8))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(borderColor, borderWidth))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            Color iconColor = _focused ? BrandPrimary : TextSecondary;
            DrawFieldIcon(g, _icon, new Rectangle(12, (Height - 18) / 2, 18, 18), iconColor);

            if (_allowPasswordToggle)
            {
                DrawEyeIcon(g, EyeButtonRect, _passwordHidden, _eyeHovered ? TextPrimary : TextSecondary);
            }
        }

        private static void DrawFieldIcon(Graphics g, InputIconType icon, Rectangle bounds, Color color)
        {
            using var pen = new Pen(color, 1.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            switch (icon)
            {
                case InputIconType.User:
                    g.DrawEllipse(pen, bounds.X + 5, bounds.Y + 2, 8, 8);
                    g.DrawArc(pen, bounds.X + 2, bounds.Y + 11, 14, 10, 180, 180);
                    break;

                case InputIconType.Lock:
                    g.DrawRectangle(pen, bounds.X + 3, bounds.Y + 8, 12, 8);
                    g.DrawArc(pen, bounds.X + 5, bounds.Y + 2, 8, 10, 180, 180);
                    break;

                case InputIconType.Barcode:
                    g.DrawLine(pen, bounds.X + 2, bounds.Y + 3, bounds.X + 2, bounds.Bottom - 3);
                    g.DrawLine(pen, bounds.X + 6, bounds.Y + 3, bounds.X + 6, bounds.Bottom - 3);
                    g.DrawLine(pen, bounds.X + 11, bounds.Y + 3, bounds.X + 11, bounds.Bottom - 3);
                    g.DrawLine(pen, bounds.X + 15, bounds.Y + 3, bounds.X + 15, bounds.Bottom - 3);
                    break;
            }
        }

        private static void DrawEyeIcon(Graphics g, Rectangle bounds, bool hidden, Color color)
        {
            using var pen = new Pen(color, 1.5f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            int cx = bounds.X + bounds.Width / 2;
            int cy = bounds.Y + bounds.Height / 2;
            var eyeRect = new Rectangle(cx - 8, cy - 5, 16, 10);
            g.DrawEllipse(pen, eyeRect);
            g.DrawEllipse(pen, cx - 2, cy - 2, 4, 4);

            if (hidden)
            {
                g.DrawLine(pen, cx - 7, cy + 6, cx + 7, cy - 6);
            }
        }
    }

    private sealed class ErrorBanner : Control
    {
        private string _message = string.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                Invalidate();
            }
        }

        public ErrorBanner(Font font)
        {
            Font = font;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect(rect, 8))
            using (var bg = new SolidBrush(DangerBg))
            using (var border = new Pen(DangerBorder, 1f))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }

            // Alert circle icon
            var iconRect = new Rectangle(12, (Height - 18) / 2, 18, 18);
            using (var pen = new Pen(DangerText, 1.6f))
            {
                g.DrawEllipse(pen, iconRect);
                g.DrawLine(pen, iconRect.X + 9, iconRect.Y + 5, iconRect.X + 9, iconRect.Y + 10);
                g.DrawLine(pen, iconRect.X + 9, iconRect.Y + 13, iconRect.X + 9, iconRect.Y + 14);
            }

            using var textBrush = new SolidBrush(DangerText);
            var textRect = new RectangleF(38, 2, Width - 46, Height - 4);
            var sf = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(_message, Font, textBrush, textRect, sf);
        }
    }

    private sealed class ModernButton : Control
    {
        private readonly bool _isPrimary;
        private bool _hovered;
        private bool _pressed;

        public ModernButton(string text, bool isPrimary, Font font)
        {
            Text = text;
            _isPrimary = isPrimary;
            Font = font;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            if (_isPrimary)
            {
                Color bg = _pressed
                    ? BrandPrimaryActive
                    : _hovered
                        ? BrandPrimaryHover
                        : BrandPrimary;

                using var path = RoundedRect(rect, 8);
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);

                using var textBrush = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(Text, Font, textBrush, rect, sf);
            }
            else
            {
                if (_hovered)
                {
                    using var path = RoundedRect(rect, 6);
                    using var brush = new SolidBrush(SurfaceMuted);
                    g.FillPath(brush, path);
                }

                Color fg = _hovered ? TextPrimary : TextSecondary;
                using var textBrush = new SolidBrush(fg);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(Text, Font, textBrush, rect, sf);
            }
        }
    }

    private sealed class CloseWindowButton : Control
    {
        private bool _hovered;

        public CloseWindowButton()
        {
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hovered)
            {
                using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 6);
                using var bg = new SolidBrush(Color.FromArgb(254, 226, 226));
                g.FillPath(bg, path);
            }

            Color iconColor = _hovered ? Color.FromArgb(220, 38, 38) : TextSecondary;
            using var pen = new Pen(iconColor, 1.8f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            int pad = 10;
            g.DrawLine(pen, pad, pad, Width - pad, Height - pad);
            g.DrawLine(pen, Width - pad, pad, pad, Height - pad);
        }
    }
}
