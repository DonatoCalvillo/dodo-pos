using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class PrintSaleTicketResult
{
    public bool Attempted { get; init; }
    public bool Success { get; init; }
    public string TicketText { get; init; } = string.Empty;
    public string? Error { get; init; }
}

public class PrintSaleTicketUseCase
{
    public PrintSaleTicketResult Execute(
        Sale sale,
        BusinessTicketProfile profile,
        PeripheralSettings peripherals,
        IThermalPrinter? printer,
        byte[]? logoRaster = null,
        bool openCashDrawer = true)
    {
        TicketDocument document = TicketFormatter.Generate(sale, profile);
        string ticketText = document.Text;
        if (printer == null || !peripherals.HasPrinterConfigured)
        {
            return new PrintSaleTicketResult
            {
                Attempted = false,
                Success = false,
                TicketText = ticketText,
                Error = "No hay impresora térmica configurada."
            };
        }

        try
        {
            bool openDrawer = openCashDrawer
                && peripherals.OpenDrawerOnCash
                && (sale.PaymentMethod == "EFECTIVO" || sale.PaymentMethod == "MIXTO");
            byte[] payload = EscPosTicketBuilder.Build(document, logoRaster, openDrawer, peripherals.DrawerPin);
            printer.Write(payload);
            return new PrintSaleTicketResult
            {
                Attempted = true,
                Success = true,
                TicketText = ticketText
            };
        }
        catch (Exception ex)
        {
            return new PrintSaleTicketResult
            {
                Attempted = true,
                Success = false,
                TicketText = ticketText,
                Error = ex.Message
            };
        }
    }
}
