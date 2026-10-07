using System.Text;
using FerreSys.Domain;

namespace FerreSys.Application;

public static class TicketFormatter
{
    private const int TicketWidth = 32;

    public static string GenerateTicketText(Sale sale)
    {
        var sb = new StringBuilder();

        // 1. Encabezado de la Ferretería
        sb.AppendLine(CenterText("FERRETERÍA EL MVP"));
        sb.AppendLine(CenterText("¡El orden hecho herramienta!"));
        sb.AppendLine(CenterText("Calle Principal #123"));
        sb.AppendLine(new string('-', TicketWidth));

        // 2. Información de la Venta
        sb.AppendLine($"Fecha: {sale.CreatedAt:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Ticket ID: {sale.Id.ToString()[..8].ToUpper()}");
        sb.AppendLine(new string('-', TicketWidth));

        // 3. Encabezado de Columnas
        sb.AppendLine(FormatLine("Cant. Producto", "Total"));
        sb.AppendLine(new string('-', TicketWidth));

        // 4. Listado de Productos
        foreach (var item in sale.Items)
        {
            string displayName = item.ProductName.Length > 18 
                ? item.ProductName[..15] + "..." 
                : item.ProductName;

            string leftSide = $"{item.Unit.FormatQuantity(item.Quantity)} {displayName}";
            string rightSide = $"${item.Subtotal:N2}";

            sb.AppendLine(FormatLine(leftSide, rightSide));
        }

        sb.AppendLine(new string('-', TicketWidth));

        // 5. Total de la Venta
        sb.AppendLine(FormatLine("TOTAL:", $"${sale.Total:N2}"));

        if (sale.PaymentMethod == "EFECTIVO")
        {
            sb.AppendLine(FormatLine("RECIBIDO:", $"${sale.AmountReceived:N2}"));
            sb.AppendLine(FormatLine("CAMBIO:", $"${sale.Change:N2}"));
        }
        else if (sale.PaymentMethod == "MIXTO")
        {
            sb.AppendLine(FormatLine("EFECTIVO:", $"${sale.CashAmount:N2}"));
            sb.AppendLine(FormatLine("TARJETA:", $"${sale.CardAmount:N2}"));
            sb.AppendLine(FormatLine("RECIBIDO:", $"${sale.AmountReceived:N2}"));
            sb.AppendLine(FormatLine("CAMBIO:", $"${sale.Change:N2}"));
            if (!string.IsNullOrWhiteSpace(sale.CardReference))
                sb.AppendLine(FormatLine("REF:", sale.CardReference));
        }
        else
        {
            sb.AppendLine(FormatLine("PAGO:", "TARJETA"));
            if (!string.IsNullOrWhiteSpace(sale.CardReference))
                sb.AppendLine(FormatLine("REF:", sale.CardReference));
        }

        sb.AppendLine(new string('=', TicketWidth));

        // 6. Pie de Ticket
        sb.AppendLine(CenterText("¡Gracias por su compra!"));
        sb.AppendLine(CenterText("Conserve su ticket para"));
        sb.AppendLine(CenterText("cualquier aclaración."));
        sb.AppendLine("\n\n");

        return sb.ToString();
    }

    // CORRECCIÓN 1: Se agregó 'static'
    private static string CenterText(string text)
    {
        if (text.Length >= TicketWidth) return text[..TicketWidth];
        int spaces = (TicketWidth - text.Length) / 2;
        return new string(' ', spaces) + text;
    }

    // CORRECCIÓN 2: Se agregó 'static'
    private static string FormatLine(string left, string right)
    {
        int availableSpace = TicketWidth - left.Length - right.Length;
        if (availableSpace <= 0)
        {
            int maxLeftLength = TicketWidth - right.Length - 1;
            left = left[..maxLeftLength];
            availableSpace = 1;
        }
        return left + new string(' ', availableSpace) + right;
    }
}