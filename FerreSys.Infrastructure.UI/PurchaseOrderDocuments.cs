using System.Diagnostics;
using System.Drawing.Printing;
using FerreSys.Application;
using FerreSys.Domain;
using FerreSys.Infrastructure.Persistence;
using QuestPDF.Fluent;

namespace FerreSys.Infrastructure.UI;

internal static class PurchaseOrderDocuments
{
    public static void Preview(IWin32Window owner, PurchaseOrder order, Supplier? supplier, SqliteAppSettingsStore settings)
    {
        byte[] pdf = Render(order, supplier, settings);
        string path = WriteTemp(order, pdf);
        var images = new List<Image>();
        try
        {
            foreach (byte[] page in RenderPages(order, supplier, settings))
            {
                using var stream = new MemoryStream(page);
                using var source = Image.FromStream(stream);
                images.Add(new Bitmap(source));
            }

            if (images.Count == 0)
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return;
            }

            using var document = new ImagePrintDocument(images, $"Orden {order.Number}");
            using var preview = new PrintPreviewDialog
            {
                Document = document,
                Width = 900,
                Height = 700,
                StartPosition = FormStartPosition.CenterParent,
                UseAntiAlias = true,
                Text = $"Vista previa — {order.Number}"
            };
            preview.ShowDialog(owner);
        }
        finally
        {
            foreach (Image image in images)
                image.Dispose();
        }
    }

    public static void SendEmail(
        IWin32Window owner,
        PurchaseOrder order,
        Supplier? supplier,
        SqliteAppSettingsStore settings,
        MarkPurchaseOrderSentUseCase markSent)
    {
        byte[] pdf = Render(order, supplier, settings);
        string path = WriteTemp(order, pdf);
        string to = supplier?.Email ?? string.Empty;
        string subject = $"Orden de compra {order.Number}";
        string body = $"Adjuntamos la orden de compra {order.Number}.";

        MapiComposeResult result = MapiMail.TryCompose(owner, to, subject, body, path);
        if (result == MapiComposeResult.Cancelled)
            return;

        if (result == MapiComposeResult.Failed)
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            string mailto = "mailto:" + Uri.EscapeDataString(to)
                + "?subject=" + Uri.EscapeDataString(subject)
                + "&body=" + Uri.EscapeDataString(body + Environment.NewLine + Environment.NewLine + "Adjunta el PDF: " + path);
            try
            {
                Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // The PDF is already open for the user to attach by hand.
            }

            MessageBox.Show(
                owner,
                "No se pudo adjuntar el PDF en el correo. Se abrió el archivo y un mensaje para que lo adjuntes tú.",
                "Orden de compra",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        if (order.IsDraft)
            markSent.Execute(order.Id, DateTime.Now);
    }

    private static byte[] Render(PurchaseOrder order, Supplier? supplier, SqliteAppSettingsStore settings)
    {
        BusinessTicketProfile profile = settings.GetTicketProfile();
        return PurchaseOrderPdf.Build(order, supplier, profile, ReadLogo(settings));
    }

    private static IReadOnlyList<byte[]> RenderPages(PurchaseOrder order, Supplier? supplier, SqliteAppSettingsStore settings)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        BusinessTicketProfile profile = settings.GetTicketProfile();
        byte[]? logo = ReadLogo(settings);
        return PurchaseOrderPdf.Create(order, supplier, profile, logo).GenerateImages().ToList();
    }

    private static byte[]? ReadLogo(SqliteAppSettingsStore settings)
    {
        string? logoPath = settings.ResolveTicketLogoPath();
        if (string.IsNullOrWhiteSpace(logoPath) || !File.Exists(logoPath))
            return null;

        try
        {
            return File.ReadAllBytes(logoPath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string WriteTemp(PurchaseOrder order, byte[] pdf)
    {
        string folder = Path.Combine(Path.GetTempPath(), "Posly");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"{order.Number}.pdf");
        File.WriteAllBytes(path, pdf);
        return path;
    }

    private sealed class ImagePrintDocument : PrintDocument
    {
        private readonly IReadOnlyList<System.Drawing.Image> _pages;
        private int _index;

        public ImagePrintDocument(IReadOnlyList<System.Drawing.Image> pages, string name)
        {
            _pages = pages;
            DocumentName = name;
        }

        protected override void OnBeginPrint(PrintEventArgs e)
        {
            _index = 0;
            base.OnBeginPrint(e);
        }

        protected override void OnPrintPage(PrintPageEventArgs e)
        {
            if (e.Graphics == null || _index >= _pages.Count)
            {
                e.HasMorePages = false;
                return;
            }

            System.Drawing.Image page = _pages[_index];
            Rectangle bounds = e.MarginBounds;
            e.Graphics.DrawImage(page, bounds);
            _index++;
            e.HasMorePages = _index < _pages.Count;
        }
    }
}
