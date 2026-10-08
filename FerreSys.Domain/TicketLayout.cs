namespace FerreSys.Domain;

public enum TicketBlock
{
    Logo,
    Name,
    Rfc,
    Address,
    Phone,
    Email,
    Website,
    Thanks,
    Policy,
    Note
}

public static class TicketLayout
{
    public static IReadOnlyList<TicketBlock> HeaderCatalog { get; } =
    [
        TicketBlock.Logo,
        TicketBlock.Name,
        TicketBlock.Rfc,
        TicketBlock.Address,
        TicketBlock.Phone,
        TicketBlock.Email,
        TicketBlock.Website
    ];

    public static IReadOnlyList<TicketBlock> FooterCatalog { get; } =
    [
        TicketBlock.Thanks,
        TicketBlock.Policy,
        TicketBlock.Note
    ];

    public static IReadOnlyList<TicketBlock> DefaultHeader => HeaderCatalog;

    public static IReadOnlyList<TicketBlock> DefaultFooter => FooterCatalog;

    public static string Label(TicketBlock block) => block switch
    {
        TicketBlock.Logo => "Logo",
        TicketBlock.Name => "Nombre",
        TicketBlock.Rfc => "RFC",
        TicketBlock.Address => "Dirección",
        TicketBlock.Phone => "Teléfono",
        TicketBlock.Email => "Correo",
        TicketBlock.Website => "Sitio web",
        TicketBlock.Thanks => "Agradecimiento",
        TicketBlock.Policy => "Política de devolución",
        TicketBlock.Note => "Nota al pie",
        _ => block.ToString()
    };

    public static string Key(TicketBlock block) => block switch
    {
        TicketBlock.Logo => "logo",
        TicketBlock.Name => "name",
        TicketBlock.Rfc => "rfc",
        TicketBlock.Address => "address",
        TicketBlock.Phone => "phone",
        TicketBlock.Email => "email",
        TicketBlock.Website => "website",
        TicketBlock.Thanks => "thanks",
        TicketBlock.Policy => "policy",
        TicketBlock.Note => "note",
        _ => block.ToString().ToLowerInvariant()
    };

    public static string Format(IEnumerable<TicketBlock> blocks) =>
        string.Join(',', blocks.Select(Key));

    public static IReadOnlyList<TicketBlock> Parse(string text, IReadOnlyList<TicketBlock> catalog)
    {
        var allowed = new HashSet<TicketBlock>(catalog);
        var seen = new HashSet<TicketBlock>();
        var result = new List<TicketBlock>();
        foreach (string token in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryKey(token, out TicketBlock block))
                continue;
            if (!allowed.Contains(block) || !seen.Add(block))
                continue;
            result.Add(block);
        }

        return result;
    }

    private static bool TryKey(string token, out TicketBlock block)
    {
        foreach (TicketBlock candidate in Enum.GetValues<TicketBlock>())
        {
            if (string.Equals(Key(candidate), token, StringComparison.OrdinalIgnoreCase))
            {
                block = candidate;
                return true;
            }
        }

        block = default;
        return false;
    }
}
