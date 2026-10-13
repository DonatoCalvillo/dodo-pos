using System.Globalization;
using ClosedXML.Excel;
using FerreSys.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FerreSys.Infrastructure.UI;

internal sealed class ReportDocument
{
    public required string FileName { get; init; }
    public required string Title { get; init; }
    public required string Period { get; init; }
    public required IReadOnlyList<(string Label, string Value)> Totals { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<string[]> Rows { get; init; }
    public required IReadOnlyList<ScottPlot.Plot> Charts { get; init; }
}

internal static class ReportText
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-MX");

    public static string Money(decimal value) => value.ToString("C2", Es);

    public static string Qty(decimal value) => value.ToString("0.###", Es);

    public static string Day(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", Es);

    public static string Date(DateTime value) => value.ToString("dd/MM/yyyy", Es);

    public static string Period(DateTime start, DateTime end) =>
        $"{start:dd/MM/yyyy} al {end:dd/MM/yyyy}";
}

internal static class ReportExport
{
    public static void Excel(IWin32Window owner, ReportDocument document)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = document.FileName + ".xlsx",
            Title = "Exportar reporte"
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK)
            return;

        using var workbook = new XLWorkbook();
        var totals = workbook.AddWorksheet("Totales");
        totals.Cell(1, 1).Value = document.Title;
        totals.Cell(2, 1).Value = document.Period;
        totals.Cell(1, 1).Style.Font.Bold = true;
        for (int i = 0; i < document.Totals.Count; i++)
        {
            totals.Cell(4 + i, 1).Value = document.Totals[i].Label;
            totals.Cell(4 + i, 2).Value = document.Totals[i].Value;
        }

        totals.Columns().AdjustToContents();

        var detail = workbook.AddWorksheet("Detalle");
        for (int column = 0; column < document.Columns.Count; column++)
        {
            detail.Cell(1, column + 1).Value = document.Columns[column];
            detail.Cell(1, column + 1).Style.Font.Bold = true;
        }

        for (int row = 0; row < document.Rows.Count; row++)
        {
            string[] values = document.Rows[row];
            for (int column = 0; column < values.Length; column++)
                detail.Cell(row + 2, column + 1).Value = values[column];
        }

        detail.SheetView.FreezeRows(1);
        detail.Columns().AdjustToContents();

        if (document.Charts.Count > 0)
        {
            var charts = workbook.AddWorksheet("Graficas");
            int row = 1;
            foreach (ScottPlot.Plot plot in document.Charts)
            {
                byte[] png = ReportCharts.Png(plot);
                using var stream = new MemoryStream(png);
                charts.AddPicture(stream).MoveTo(charts.Cell(row, 1));
                row += 24;
            }
        }

        workbook.SaveAs(dialog.FileName);
    }

    public static void Pdf(IWin32Window owner, ReportDocument document, BusinessTicketProfile profile)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = document.FileName + ".pdf",
            Title = "Exportar reporte"
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK)
            return;

        QuestPDF.Settings.License = LicenseType.Community;
        string business = string.IsNullOrWhiteSpace(profile.BusinessName) ? "Posly" : profile.BusinessName.Trim();
        List<byte[]> images = document.Charts.Select(ReportCharts.Png).ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(text => text.FontSize(9));
                page.Header().Column(column =>
                {
                    column.Item().Text(business).Bold().FontSize(16);
                    column.Item().Text(document.Title).FontSize(13).FontColor(Colors.Blue.Darken2);
                    column.Item().Text(document.Period);
                    if (!string.IsNullOrWhiteSpace(profile.Address))
                        column.Item().Text(profile.Address).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });
                        foreach ((string label, string value) in document.Totals)
                        {
                            table.Cell().BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(label).SemiBold();
                            table.Cell().BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(value);
                        }
                    });

                    foreach (byte[] image in images)
                        column.Item().PaddingTop(12).Image(image).FitWidth();

                    if (document.Columns.Count > 0)
                    {
                        column.Item().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (string _ in document.Columns)
                                    columns.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                foreach (string name in document.Columns)
                                {
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(3)
                                        .Text(name).FontColor(Colors.White).FontSize(8).SemiBold();
                                }
                            });
                            foreach (string[] row in document.Rows)
                            {
                                for (int index = 0; index < document.Columns.Count; index++)
                                {
                                    string value = index < row.Length ? row[index] : string.Empty;
                                    table.Cell().BorderBottom(0.3f).BorderColor(Colors.Grey.Lighten2).Padding(2)
                                        .Text(value).FontSize(8);
                                }
                            }
                        });
                    }
                });
                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(dialog.FileName);
    }
}
