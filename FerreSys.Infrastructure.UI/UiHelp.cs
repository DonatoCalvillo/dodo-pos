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
        "Total a cobrar de la venta actual.\n\n" +
        "Si hay descuento de ticket, este monto ya es el neto.\n" +
        "No incluye movimientos de caja ajenos a la venta.";

    public const string CheckoutDiscount =
        "Descuento sobre el total del ticket, aparte del descuento de cada producto.\n\n" +
        "• Porcentaje: baja el subtotal ese tanto por ciento.\n" +
        "• Monto fijo: resta esa cantidad al subtotal.\n" +
        "• El total a cobrar no puede quedar en cero.";

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
        "• Clic derecho: editar producto, auditar stock o ver Kárdex.";

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
        "• Nueva / cerrar venta: administra pestañas de carrito.\n" +
        "• Inventario (F8): consulta existencias sin agregar al carrito.";

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

    public const string KardexFilters =
        "Consulta el historial de entradas y salidas de inventario (Kárdex).\n\n" +
        "• Filtra por rango de fechas, tipo de movimiento y código de producto.\n" +
        "• Enter en Código o Consultar aplica el filtro de producto.\n" +
        "• Sin código se muestran movimientos de todos los productos.\n\n" +
        "Tipos: Venta, Ajuste de Auditoría, Carga Inicial. Devolución queda reservado.";

    public const string BusinessProfile =
        "Así se imprime el ticket térmico.\n\n" +
        "• Llena solo lo que quieras mostrar. Un campo vacío no sale en el papel.\n" +
        "• Encabezado y pie: marca lo que se imprime y súbelo o bájalo para cambiar el orden.\n" +
        "• La venta (fecha, productos y total) siempre queda en medio.\n" +
        "• El logo se guarda junto a la base de datos y se imprime en la térmica.\n" +
        "• 58 mm usa 32 columnas; 80 mm usa 48. La vista previa usa el mismo orden.";

    public const string PeripheralsPrinter =
        "Impresora térmica ESC/POS.\n\n" +
        "• Windows: colas con controlador instalado. Actualizar vuelve a leerlas.\n" +
        "• Una térmica Bluetooth (como la MP210) no entra ahí si Windows dice que el controlador no está disponible. Posly la muestra como Bluetooth y la usa por su puerto COM.\n" +
        "• Si sale «controlador no disponible», instala el controlador en Impresoras y escáneres y pulsa Actualizar.\n" +
        "• COM: puerto serie directo.\n" +
        "• Red: IP y puerto 9100 (raw).\n\n" +
        "Probar impresión envía un ticket de prueba. Probar cajón envía el pulso RJ11.";

    public const string PeripheralsScanner =
        "Escáner de códigos.\n\n" +
        "• Teclado: el USB escribe en el buscador del mostrador (modo HID).\n" +
        "• Serie: Posly lee el puerto COM.\n" +
        "• Al escanear, agregar al carrito: cada lectura suma 1 a la cantidad. Si está apagado, solo busca.\n" +
        "• Cada lectura reemplaza el código anterior para que no se duplique.\n\n" +
        "Probar lectura muestra el último código recibido.";

    public const string PeripheralsScale =
        "Báscula de peso por puerto serie.\n\n" +
        "• La báscula envía el peso en texto de forma continua (por ejemplo 1.250 o ST,GS, 1.250kg).\n" +
        "• Al vender un producto en kilogramos, el diálogo de cantidad muestra ese peso.\n" +
        "• Metro y litro se siguen capturando a mano.\n" +
        "• ST marca un peso estable; US, uno inestable.\n\n" +
        "Probar lectura muestra el último peso recibido.";

    public const string Inventory =
        "Consulta de existencias. No agrega productos al carrito.\n\n" +
        "• Se abre con F8, desde Productos o desde la barra.\n" +
        "• Busca por nombre o código mientras escribes.\n" +
        "• Enter no vende ni suma cantidad.\n" +
        "• Escape cierra esta ventana.\n" +
        "• Exportar Excel guarda solo las filas que estás viendo (respeta el filtro).";

    public const string InventoryList =
        "Productos activos del catálogo.\n\n" +
        "• Rojo: agotado. Amarillo: bajo el mínimo.\n" +
        "• Los productos ocultos no aparecen.\n" +
        "• Selecciona una fila para ver la ficha.\n" +
        "• Clic derecho: editar, auditar stock, ver kárdex o quitar del mostrador.";

    public const string InventoryDetail =
        "Datos del producto seleccionado: código, unidad, stock, mínimo, deseado, precio y costo.\n\n" +
        "Solo información. No modifica el inventario ni la venta.";

    public const string KardexList =
        "Auditoría paso a paso del stock.\n\n" +
        "• Cant. anterior → Cant. movida (+/−) → Stock resultante.\n" +
        "• Usuario = cuenta de Windows que ejecutó la app.\n" +
        "• En ventas, la referencia es el ID corto del ticket.\n" +
        "• Los movimientos se registran desde el despliegue del Kárdex (sin backfill).";
}
