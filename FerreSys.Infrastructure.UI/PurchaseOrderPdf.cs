using FerreSys.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FerreSys.Infrastructure.UI;

internal static class PurchaseOrderPdf
{
    public static byte[] Build(PurchaseOrder order, Supplier? supplier, BusinessTicketProfile profile, byte[]? logo)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Create(order, supplier, profile, logo).GeneratePdf();
    }

    public static Document Create(PurchaseOrder order, Supplier? supplier, BusinessTicketProfile profile, byte[]? logo)
    {
        bool showReceived = !order.IsDraft;
        string business = string.IsNullOrWhiteSpace(profile.BusinessName) ? "Posly" : profile.BusinessName.Trim();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(14, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(text => text.FontSize(9).FontFamily("Segoe UI").FontColor(Colors.Grey.Darken4));

                page.Header().Element(header => ComposeHeader(header, order, profile, logo, business));
                page.Content().PaddingTop(8).Element(content => ComposeBody(content, order, supplier, showReceived));
                page.Footer().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text("Documento generado por Posly").FontSize(8).FontColor(Colors.Grey.Medium);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Medium));
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });
        });
    }

    private static void ComposeHeader(
        IContainer header,
        PurchaseOrder order,
        BusinessTicketProfile profile,
        byte[]? logo,
        string business)
    {
        header.Row(row =>
        {
            if (logo is { Length: > 0 })
            {
                row.ConstantItem(62).Height(48).Image(logo).FitArea();
                row.ConstantItem(10);
            }

            row.RelativeItem().Column(column =>
            {
                column.Item().Text(business).FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                column.Item().Text(Line(profile.Address)).FontSize(8);
                if (!string.IsNullOrWhiteSpace(profile.Rfc))
                    column.Item().Text($"RFC {profile.Rfc}").FontSize(8);
                string contact = Join(" · ", profile.Phone, profile.Email);
                if (contact.Length > 0)
                    column.Item().Text(contact).FontSize(8);
            });

            row.ConstantItem(170).AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text("ORDEN DE COMPRA").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                column.Item().AlignRight().Text(order.Number).FontSize(14).Bold();
                column.Item().AlignRight().Text(order.CreatedAt.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                column.Item().AlignRight().Text(order.Status.DisplayName()).FontSize(9).Bold();
            });
        });
    }

    private static void ComposeBody(IContainer content, PurchaseOrder order, Supplier? supplier, bool showReceived)
    {
        content.Column(column =>
        {
            column.Item().PaddingTop(8).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(box =>
            {
                box.Item().Text("Proveedor").FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                box.Item().Text(supplier?.Name ?? order.SupplierName).FontSize(11).Bold();
                if (!string.IsNullOrWhiteSpace(supplier?.ContactName))
                    box.Item().Text($"Contacto: {supplier.ContactName}").FontSize(8);
                string contact = Join(" · ", supplier?.Phone ?? "", supplier?.Email ?? "");
                if (contact.Length > 0)
                    box.Item().Text(contact).FontSize(8);
                if (!string.IsNullOrWhiteSpace(supplier?.Address))
                    box.Item().Text(supplier.Address).FontSize(8);
            });

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(78);
                    columns.RelativeColumn();
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(58);
                    if (showReceived)
                        columns.ConstantColumn(58);
                    columns.ConstantColumn(62);
                    columns.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Código");
                    HeaderCell(header.Cell(), "Producto");
                    HeaderCell(header.Cell(), "Unidad", right: true);
                    HeaderCell(header.Cell(), "Cantidad", right: true);
                    if (showReceived)
                        HeaderCell(header.Cell(), "Recibido", right: true);
                    HeaderCell(header.Cell(), "Costo", right: true);
                    HeaderCell(header.Cell(), "Importe", right: true);
                });

                foreach (PurchaseOrderLine line in order.Lines)
                {
                    BodyCell(table.Cell(), line.Barcode);
                    BodyCell(table.Cell(), line.Name);
                    BodyCell(table.Cell(), line.Unit.DisplayName(), right: true);
                    BodyCell(table.Cell(), line.Unit.FormatQuantity(line.OrderedQty), right: true);
                    if (showReceived)
                        BodyCell(table.Cell(), line.Unit.FormatQuantity(line.ReceivedQty), right: true);
                    BodyCell(table.Cell(), Money(line.UnitCost), right: true);
                    BodyCell(table.Cell(), Money(line.OrderedAmount), right: true);
                }
            });

            column.Item().PaddingTop(8).AlignRight().Width(220).Column(totals =>
            {
                totals.Item().BorderTop(1).BorderColor(Colors.Blue.Darken3).PaddingTop(4)
                    .Row(row =>
                    {
                        row.RelativeItem().Text("Total").Bold();
                        row.RelativeItem().AlignRight().Text(Money(order.Total)).Bold().FontSize(12);
                    });
            });

            if (!string.IsNullOrWhiteSpace(order.Notes))
            {
                column.Item().PaddingTop(10).Text("Notas").FontSize(8).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().Text(order.Notes).FontSize(9);
            }

            if (order.SentAt.HasValue)
                column.Item().PaddingTop(6).Text($"Enviada: {order.SentAt:dd/MM/yyyy HH:mm}").FontSize(8);
            if (order.DecidedAt.HasValue)
                column.Item().Text($"Respuesta: {order.DecidedAt:dd/MM/yyyy HH:mm}").FontSize(8);

            column.Item().PaddingTop(28).Row(row =>
            {
                Signature(row, "Solicita");
                Signature(row, "Autoriza");
                Signature(row, "Recibe");
            });
        });
    }

    private static void Signature(RowDescriptor row, string caption)
    {
        row.RelativeItem().PaddingHorizontal(8).Column(column =>
        {
            column.Item().PaddingTop(18).BorderTop(1).BorderColor(Colors.Grey.Medium);
            column.Item().PaddingTop(2).AlignCenter().Text(caption).FontSize(8);
        });
    }

    private static void HeaderCell(IContainer cell, string text, bool right = false)
    {
        IContainer box = cell.Background(Colors.Blue.Darken3).PaddingVertical(4).PaddingHorizontal(3);
        box = right ? box.AlignRight() : box.AlignLeft();
        box.Text(text).FontSize(8).Bold().FontColor(Colors.White);
    }

    private static void BodyCell(IContainer cell, string text, bool right = false)
    {
        IContainer box = cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3);
        box = right ? box.AlignRight() : box.AlignLeft();
        box.Text(text).FontSize(8);
    }

    private static string Money(decimal value) => value.ToString("C2");

    private static string Line(string? value) => value?.Trim() ?? string.Empty;

    private static string Join(string separator, params string?[] parts)
    {
        return string.Join(separator, parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }
}
