namespace FerreSys.Infrastructure.UI;

internal static class UiHelp
{
    private const string HelpTag = "section-help";

    public static GroupBox CreateGroupBox(string title, string? helpText = null, Padding? margin = null)
    {
        var gb = new GroupBox
        {
            Text = " " + title + " ",
            Dock = DockStyle.Fill,
            Font = UiTheme.GroupFont,
            ForeColor = UiTheme.GroupTitle,
            BackColor = UiTheme.PanelBg,
            Padding = new Padding(8),
            Margin = margin ?? new Padding(0, 0, 0, 6)
        };

        if (!string.IsNullOrWhiteSpace(helpText))
            Attach(gb, title, helpText);

        return gb;
    }

    public static void Attach(Control host, string title, string helpText)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (string.IsNullOrWhiteSpace(helpText))
            return;

        void Ensure()
        {
            var existing = host.Controls
                .OfType<Button>()
                .FirstOrDefault(b => Equals(b.Tag, HelpTag));
            if (existing != null)
            {
                existing.BringToFront();
                Position(host, existing);
                return;
            }

            var btn = CreateHelpButton(title, helpText);
            host.Controls.Add(btn);
            btn.BringToFront();
            Position(host, btn);
        }

        if (host.IsHandleCreated)
            Ensure();
        else
            host.HandleCreated += (_, _) => Ensure();

        host.Layout += (_, _) =>
        {
            var btn = host.Controls.OfType<Button>().FirstOrDefault(b => Equals(b.Tag, HelpTag));
            if (btn == null)
                return;
            btn.BringToFront();
            Position(host, btn);
        };
    }

    public static void AttachToForm(Form form, string title, string helpText)
    {
        ArgumentNullException.ThrowIfNull(form);
        var btn = CreateHelpButton(title, helpText);
        btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        form.Controls.Add(btn);
        form.Shown += (_, _) =>
        {
            btn.Location = new Point(form.ClientSize.Width - btn.Width - 10, 8);
            btn.BringToFront();
        };
    }

    private static Button CreateHelpButton(string title, string helpText)
    {
        var btn = new Button
        {
            Text = "?",
            Tag = HelpTag,
            Size = new Size(22, 22),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(230, 240, 255),
            ForeColor = UiTheme.GroupTitle,
            Font = new Font("Tahoma", 8.25f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TabStop = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(0, 90, 170);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 220, 250);

        var tip = new ToolTip
        {
            InitialDelay = 400,
            ReshowDelay = 100,
            AutoPopDelay = 5000
        };
        tip.SetToolTip(btn, "Ayuda de esta sección");

        btn.Click += (_, _) => Show(title, helpText);
        return btn;
    }

    public static void Show(string title, string helpText)
    {
        MessageBox.Show(
            helpText.Trim(),
            "Ayuda: " + title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void Position(Control host, Control btn)
    {
        int x = Math.Max(4, host.ClientSize.Width - btn.Width - 6);
        int y = host is GroupBox ? 0 : 4;
        btn.Location = new Point(x, y);
    }
}

internal static class SectionHelpText
{
    public const string CashMovementsRegister =
        "Registra dinero que entra o sale de la caja sin ser una venta.\n\n" +
        "• Entrada: inyección de efectivo (para dar cambio, pagos extraordinarios).\n" +
        "• Salida: retiro (proveedor en mostrador, flete, agua, retiro del dueño).\n\n" +
        "Límites:\n" +
        "• El concepto es obligatorio.\n" +
        "• El monto debe ser mayor a cero.\n" +
        "• No puedes registrar movimientos si el día ya está cerrado.\n" +
        "• Las ventas NO se capturan aquí; se generan solas al cobrar.";

    public const string CashMovementsList =
        "Historial del día de entradas y salidas manuales.\n\n" +
        "• No incluye tickets de venta.\n" +
        "• Los totales (Entradas / Salidas / Neto) alimentan el efectivo esperado del Corte X / Corte Z.\n" +
        "• Por ahora no se pueden editar ni borrar movimientos (solo alta).";

    public const string CashCloseOpening =
        "Fondo inicial del día: efectivo en cajón al abrir (para dar cambio).\n\n" +
        "• Se pide al iniciar la app; si cancelas, igual puedes cobrar.\n" +
        "• Debes guardar el fondo antes del Corte Z.\n" +
        "• No se puede cambiar el fondo cuando ya hay un Corte Z del día.";

    public const string CashCloseSales =
        "Totales cobrados en el día, separados por forma de pago.\n\n" +
        "• Ventas en efectivo: tickets en efectivo y la parte en efectivo de mixtos.\n" +
        "• Ventas en tarjeta: tickets con tarjeta y la parte con tarjeta de mixtos.\n" +
        "• Solo lectura: las ventas se generan al cobrar en el mostrador.";

    public const string CashCloseMovements =
        "Resumen de movimientos manuales del día (solo lectura).\n\n" +
        "• Para dar de alta entradas/salidas usa Movimientos de caja.\n" +
        "• Entran en la fórmula del efectivo esperado:\n" +
        "  Fondo + Ventas en efectivo + Entradas − Salidas.";

    public const string CashCloseCountX =
        "Corte X: consulta el efectivo que el sistema espera en cajón en este momento.\n\n" +
        "Esperado = Fondo + Ventas en efectivo + Entradas − Salidas.\n\n" +
        "No cierra el día; para declarar el conteo físico usa Corte Z.";

    public const string CashCloseLastZ =
        "Snapshot del último Corte Z guardado para este día.\n\n" +
        "Incluye el desglose congelado al momento del cierre:\n" +
        "fondo, ventas efectivo/tarjeta, entradas, salidas, esperado, declarado y descuadre.\n\n" +
        "Si sigues cobrando después, los totales en vivo del Corte X pueden diferir de este snapshot.";

    public const string CashCloseCountZ =
        "Corte Z — conteo ciego.\n\n" +
        "Ingresa solo el efectivo físico del cajón. El sistema no muestra lo esperado\n" +
        "hasta que pulses Continuar, para no influir en el conteo.";

    public const string CashCloseCountZReveal =
        "Reporte de descuadre del Corte Z.\n\n" +
        "Esperado = Fondo + Ventas en efectivo + Entradas − Salidas.\n" +
        "Diferencia = Declarado − Esperado.\n\n" +
        "• Cero: cuadra.\n" +
        "• Positiva: sobrante.\n" +
        "• Negativa: faltante.\n\n" +
        "Al guardar se persiste el snapshot en la base de datos.\n" +
        "Puedes seguir cobrando y volver a actualizar el Corte Z.";

    public const string OpeningCashPrompt =
        "Declara cuánto efectivo hay en caja al abrir el turno.\n\n" +
        "• Sirve para dar cambio y para el Corte X / Corte Z.\n" +
        "• Cancelar no bloquea el mostrador; puedes declarar el fondo después en Corte X.\n" +
        "• El monto no puede ser negativo.";

    public const string CheckoutTotal =
        "Total a cobrar de la venta actual (suma del carrito).\n\n" +
        "No incluye movimientos de caja ajenos a la venta.";

    public const string CheckoutPayment =
        "Elige cómo paga el cliente.\n\n" +
        "• Efectivo: se calcula cambio si recibe de más.\n" +
        "• Tarjeta: puedes capturar referencia opcional.\n" +
        "• Mixto: parte efectivo + parte tarjeta.";

    public const string CheckoutCash =
        "Monto recibido en efectivo y cambio a devolver.\n\n" +
        "• En mixto, el efectivo recibido debe cubrir al menos la parte en efectivo.\n" +
        "• El cambio no se registra como 'salida' de caja; es parte del ticket.";

    public const string Search =
        "Busca por nombre o escanea código de barras.\n\n" +
        "• Enter con un solo resultado: añade al carrito.\n" +
        "• Doble clic: añade al carrito.\n" +
        "• Clic derecho: editar producto o auditar stock.";

    public const string CatalogResults =
        "Resultados del catálogo según la búsqueda.\n\n" +
        "• Muestra código, descripción, precio y stock.\n" +
        "• El stock se descuenta al confirmar la venta, no al añadir al carrito.\n" +
        "• Amarillo: stock bajo el mínimo. Rojo: agotado.";

    public const string SaleSummary =
        "Resumen de la venta activa: artículos, líneas y total.\n\n" +
        "• F5 abre el cobro.\n" +
        "• Puedes tener varias ventas (pestañas) en paralelo.";

    public const string Cart =
        "Productos de la venta actual.\n\n" +
        "• +/- ajustan cantidad.\n" +
        "• Vaciar limpia solo este carrito.\n" +
        "• Nueva venta / pestañas permiten atender otro cliente sin perder el actual.";

    public const string Actions =
        "Acciones rápidas del mostrador.\n\n" +
        "• Cobrar (F5): abre el pago de la venta activa.\n" +
        "• Vaciar: quita todos los renglones del carrito actual.\n" +
        "• Nueva / cerrar venta: administra pestañas de carrito.";

    public const string GrossProfitReport =
        "Consulta el margen real de las ventas en un rango de fechas.\n\n" +
        "Utilidad por renglón =\n" +
        "(Precio de venta − Costo al momento de la venta) × Cantidad.\n\n" +
        "El costo se congela en el ticket; no usa el costo actual del catálogo.\n" +
        "No descuenta gastos ni movimientos de caja.";

    public const string GrossProfitSalesCost =
        "Totales del rango seleccionado.\n\n" +
        "• Total vendido: suma de cantidad × precio de venta.\n" +
        "• Costo total vendido: suma de cantidad × costo guardado en el ticket.";

    public const string GrossProfitNet =
        "Ganancia neta real sobre la mercancía vendida.\n\n" +
        "• Ganancia neta = Total vendido − Costo total vendido.\n" +
        "• Margen % = Ganancia neta ÷ Total vendido × 100.\n\n" +
        "Si no hay ventas en el rango, el margen es 0 %.";

    public const string PurchaseSuggestion =
        "Listado de productos agotados o por debajo del stock mínimo.\n\n" +
        "• A pedir = Stock deseado − Stock actual (mínimo 0).\n" +
        "• Rojo: agotado. Amarillo: bajo el mínimo.\n" +
        "• Exporta a CSV o imprime / Guarda como PDF desde Windows.\n\n" +
        "Configura mínimo y deseado al registrar o editar cada producto.";

    public const string PurchaseSuggestionList =
        "Productos que requieren reabastecimiento.\n\n" +
        "• Orden: primero agotados, luego bajo mínimo.\n" +
        "• Usa Exportar CSV para enviar el listado al proveedor.\n" +
        "• Imprimir / PDF abre la vista previa y el diálogo de impresión de Windows.";
}
