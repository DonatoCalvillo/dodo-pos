namespace FerreSys.Domain;

public enum TicketPaperWidth
{
    Mm58 = 32,
    Mm80 = 48
}

public sealed class BusinessTicketProfile
{
    public const string LogoFileName = "ticket-logo.png";

    public string BusinessName { get; init; } = "FERRETERÍA EL MVP";
    public string Address { get; init; } = "Calle Principal #123";
    public string Rfc { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Website { get; init; } = string.Empty;
    public string ThankYouMessage { get; init; } = "¡Gracias por su compra!";
    public string ReturnPolicy { get; init; } = "Conserve su ticket para cualquier aclaración.";
    public string FooterNote { get; init; } = string.Empty;
    public string LogoFile { get; init; } = string.Empty;
    public TicketPaperWidth PaperWidth { get; init; } = TicketPaperWidth.Mm58;
    public IReadOnlyList<TicketBlock> HeaderLayout { get; init; } = TicketLayout.DefaultHeader;
    public IReadOnlyList<TicketBlock> FooterLayout { get; init; } = TicketLayout.DefaultFooter;

    public int Columns => (int)PaperWidth;

    public bool ShowsLogo =>
        !string.IsNullOrWhiteSpace(LogoFile) && HeaderLayout.Contains(TicketBlock.Logo);

    public static BusinessTicketProfile Default() => new();
}
