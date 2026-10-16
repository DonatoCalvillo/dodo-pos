using System.Text;
using FerreSys.Domain;

namespace FerreSys.Application;

public static class TicketFormatter
{
    public static string GenerateTicketText(Sale sale, BusinessTicketProfile? profile = null) =>
        Generate(sale, profile).Text;

    public static TicketDocument Generate(Sale sale, BusinessTicketProfile? profile = null)
    {
        profile ??= BusinessTicketProfile.Default();
        int width = profile.Columns;
        var pieces = new List<TicketPiece>();
        var buffer = new StringBuilder();

        AppendLayout(pieces, buffer, profile.HeaderLayout, profile, width);

        buffer.AppendLine(new string('-', width));
        buffer.AppendLine($"Fecha: {sale.CreatedAt:dd/MM/yyyy HH:mm}");
        buffer.AppendLine($"Ticket ID: {sale.Id.ToString()[..8].ToUpper()}");
        buffer.AppendLine(new string('-', width));
        buffer.AppendLine(FormatLine("Cant. Producto", "Total", width));
        buffer.AppendLine(new string('-', width));

        foreach (var item in sale.Items)
        {
            string displayName = item.ProductName.Length > 18
                ? item.ProductName[..15] + "..."
                : item.ProductName;
            if (width >= 48 && item.ProductName.Length > 28)
                displayName = item.ProductName[..25] + "...";
            else if (width >= 48)
                displayName = item.ProductName;

            string leftSide = $"{item.Unit.FormatQuantity(item.Quantity)} {displayName}";
            string rightSide = $"${item.Subtotal:N2}";
            buffer.AppendLine(FormatLine(leftSide, rightSide, width));
        }

        buffer.AppendLine(new string('-', width));
        if (sale.DiscountAmount > 0m)
        {
            buffer.AppendLine(FormatLine("SUBTOTAL:", $"${sale.Subtotal:N2}", width));
            buffer.AppendLine(FormatLine("DESCUENTO:", $"${sale.DiscountAmount:N2}", width));
        }

        buffer.AppendLine(FormatLine("TOTAL:", $"${sale.Total:N2}", width));

        if (sale.PaymentMethod == "EFECTIVO")
        {
            buffer.AppendLine(FormatLine("RECIBIDO:", $"${sale.AmountReceived:N2}", width));
            buffer.AppendLine(FormatLine("CAMBIO:", $"${sale.Change:N2}", width));
        }
        else if (sale.PaymentMethod == "MIXTO")
        {
            buffer.AppendLine(FormatLine("EFECTIVO:", $"${sale.CashAmount:N2}", width));
            buffer.AppendLine(FormatLine("TARJETA:", $"${sale.CardAmount:N2}", width));
            buffer.AppendLine(FormatLine("RECIBIDO:", $"${sale.AmountReceived:N2}", width));
            buffer.AppendLine(FormatLine("CAMBIO:", $"${sale.Change:N2}", width));
            if (!string.IsNullOrWhiteSpace(sale.CardReference))
                buffer.AppendLine(FormatLine("REF:", sale.CardReference, width));
        }
        else
        {
            buffer.AppendLine(FormatLine("PAGO:", "TARJETA", width));
            if (!string.IsNullOrWhiteSpace(sale.CardReference))
                buffer.AppendLine(FormatLine("REF:", sale.CardReference, width));
        }

        if (sale.Loyalty is { } loyalty)
        {
            buffer.AppendLine(FormatLine("CLIENTE:", loyalty.CustomerName, width));
            if (!string.IsNullOrWhiteSpace(loyalty.RewardName))
                buffer.AppendLine(FormatLine("PREMIO:", loyalty.RewardName, width));
            buffer.AppendLine(FormatLine("PUNTOS:", $"+{loyalty.PointsEarned}", width));
            buffer.AppendLine(FormatLine("SALDO:", loyalty.BalanceAfter.ToString(), width));
        }

        buffer.AppendLine(new string('=', width));
        AppendLayout(pieces, buffer, profile.FooterLayout, profile, width);
        Flush(pieces, buffer);
        return new TicketDocument { Pieces = pieces };
    }

    public static TicketDocument GenerateTest(BusinessTicketProfile profile)
    {
        int width = profile.Columns;
        var pieces = new List<TicketPiece>();
        var buffer = new StringBuilder();
        AppendLayout(pieces, buffer, profile.HeaderLayout, profile, width);
        buffer.AppendLine(new string('-', width));
        AppendCentered(buffer, "PRUEBA DE IMPRESION", width);
        AppendCentered(buffer, DateTime.Now.ToString("dd/MM/yyyy HH:mm"), width);
        AppendCentered(buffer, profile.PaperWidth == TicketPaperWidth.Mm80 ? "Papel 80 mm" : "Papel 58 mm", width);
        buffer.AppendLine(new string('-', width));
        AppendLayout(pieces, buffer, profile.FooterLayout, profile, width);
        Flush(pieces, buffer);
        return new TicketDocument { Pieces = pieces };
    }

    public static string GenerateTestText(BusinessTicketProfile profile) =>
        GenerateTest(profile).Text;

    public static TicketDocument GeneratePreview(BusinessTicketProfile profile)
    {
        var sale = new Sale(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            DateTime.Now,
            [
                new SaleItem(Guid.NewGuid(), "750100000001", "Clavo 2 pulgadas", 2, 15.50m, 8m),
                new SaleItem(Guid.NewGuid(), "750100000002", "Cinta de aislar", 1, 28m, 12m)
            ],
            "EFECTIVO",
            string.Empty,
            100m);
        return Generate(sale, profile);
    }

    private static void AppendLayout(
        List<TicketPiece> pieces,
        StringBuilder buffer,
        IReadOnlyList<TicketBlock> layout,
        BusinessTicketProfile profile,
        int width)
    {
        foreach (TicketBlock block in layout)
        {
            if (block == TicketBlock.Logo)
            {
                Flush(pieces, buffer);
                pieces.Add(TicketPiece.Logo());
                continue;
            }

            AppendBlock(buffer, block, profile, width);
        }
    }

    private static void AppendBlock(StringBuilder buffer, TicketBlock block, BusinessTicketProfile profile, int width)
    {
        switch (block)
        {
            case TicketBlock.Name:
                if (!string.IsNullOrWhiteSpace(profile.BusinessName))
                    AppendCentered(buffer, profile.BusinessName.Trim(), width);
                break;
            case TicketBlock.Rfc:
                if (!string.IsNullOrWhiteSpace(profile.Rfc))
                    AppendCentered(buffer, "RFC: " + profile.Rfc.Trim(), width);
                break;
            case TicketBlock.Address:
                if (!string.IsNullOrWhiteSpace(profile.Address))
                    AppendCentered(buffer, profile.Address.Trim(), width);
                break;
            case TicketBlock.Phone:
                if (!string.IsNullOrWhiteSpace(profile.Phone))
                    AppendCentered(buffer, "Tel: " + profile.Phone.Trim(), width);
                break;
            case TicketBlock.Email:
                if (!string.IsNullOrWhiteSpace(profile.Email))
                    AppendCentered(buffer, profile.Email.Trim(), width);
                break;
            case TicketBlock.Website:
                if (!string.IsNullOrWhiteSpace(profile.Website))
                    AppendCentered(buffer, profile.Website.Trim(), width);
                break;
            case TicketBlock.Thanks:
                if (!string.IsNullOrWhiteSpace(profile.ThankYouMessage))
                    AppendCentered(buffer, profile.ThankYouMessage.Trim(), width);
                break;
            case TicketBlock.Policy:
                if (!string.IsNullOrWhiteSpace(profile.ReturnPolicy))
                    AppendCentered(buffer, profile.ReturnPolicy.Trim(), width);
                break;
            case TicketBlock.Note:
                if (!string.IsNullOrWhiteSpace(profile.FooterNote))
                    AppendCentered(buffer, profile.FooterNote.Trim(), width);
                break;
        }
    }

    private static void Flush(List<TicketPiece> pieces, StringBuilder buffer)
    {
        if (buffer.Length == 0)
            return;

        pieces.Add(TicketPiece.Lines(buffer.ToString()));
        buffer.Clear();
    }

    private static void AppendCentered(StringBuilder sb, string text, int width)
    {
        foreach (string line in Wrap(text, width))
            sb.AppendLine(CenterText(line, width));
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            yield return text.Length <= width ? text : text[..width];
            yield break;
        }

        var current = new StringBuilder();
        foreach (string word in words)
        {
            string piece = word.Length > width ? word[..width] : word;
            if (current.Length == 0)
            {
                current.Append(piece);
                continue;
            }

            if (current.Length + 1 + piece.Length > width)
            {
                yield return current.ToString();
                current.Clear();
                current.Append(piece);
            }
            else
            {
                current.Append(' ').Append(piece);
            }
        }

        if (current.Length > 0)
            yield return current.ToString();
    }

    private static string CenterText(string text, int width)
    {
        if (text.Length >= width)
            return text.Length == width ? text : text[..width];
        int spaces = (width - text.Length) / 2;
        return new string(' ', spaces) + text;
    }

    private static string FormatLine(string left, string right, int width)
    {
        int availableSpace = width - left.Length - right.Length;
        if (availableSpace <= 0)
        {
            int maxLeftLength = Math.Max(1, width - right.Length - 1);
            left = left.Length > maxLeftLength ? left[..maxLeftLength] : left;
            availableSpace = Math.Max(1, width - left.Length - right.Length);
        }

        return left + new string(' ', availableSpace) + right;
    }
}
