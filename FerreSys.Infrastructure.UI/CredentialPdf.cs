using System.Diagnostics;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FerreSys.Infrastructure.UI;

internal static class CredentialPdf
{
    public static void SaveAndOpen(IWin32Window owner, User user, SqliteAppSettingsStore settings)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        using var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"credencial-{user.EmployeeNumber}.pdf",
            Title = "Guardar credencial"
        };
        if (dialog.ShowDialog(owner) != DialogResult.OK)
            return;

        BusinessTicketProfile profile = settings.GetTicketProfile();
        string business = profile.BusinessName.Trim();
        if (business.Length == 0)
            business = "Posly";

        byte[]? logo = null;
        string? logoPath = settings.ResolveTicketLogoPath();
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            try
            {
                logo = File.ReadAllBytes(logoPath);
            }
            catch (Exception)
            {
                logo = null;
            }
        }

        byte[] barcode = CredentialBarcode.Png(user.EmployeeNumber);
        byte[] pdf = Build(user, business, logo, barcode);
        File.WriteAllBytes(dialog.FileName, pdf);
        Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
    }

    private static byte[] Build(User user, string business, byte[]? logo, byte[] barcode)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(85.6f, 54f, Unit.Millimetre);
                page.Margin(3, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(text => text.FontSize(8).FontFamily("Tahoma"));

                page.Content().Row(row =>
                {
                    row.ConstantItem(22, Unit.Millimetre).AlignMiddle().Element(photo =>
                    {
                        if (user.Photo is { Length: > 0 })
                            photo.Image(user.Photo).FitArea();
                        else
                            photo.Border(0.4f).BorderColor(Colors.Grey.Medium).AlignCenter().AlignMiddle().Text("Foto").FontSize(7).FontColor(Colors.Grey.Darken1);
                    });

                    row.ConstantItem(2.5f, Unit.Millimetre);

                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Row(header =>
                        {
                            if (logo is { Length: > 0 })
                            {
                                header.ConstantItem(8, Unit.Millimetre).Height(8, Unit.Millimetre).Image(logo).FitArea();
                                header.ConstantItem(1.5f, Unit.Millimetre);
                            }

                            header.RelativeItem().AlignMiddle().Text(business).Bold().FontSize(8);
                        });

                        column.Item().PaddingTop(1.2f, Unit.Millimetre).Text(user.DisplayName).Bold().FontSize(11);
                        if (user.JobTitle.Length > 0)
                            column.Item().Text(user.JobTitle).FontSize(7).FontColor(Colors.Grey.Darken2);

                        column.Item().Text(user.EmployeeNumber).FontSize(7);
                        column.Item().PaddingTop(1, Unit.Millimetre).Height(16, Unit.Millimetre).Image(barcode).FitArea();
                    });
                });
            });
        }).GeneratePdf();
    }
}
