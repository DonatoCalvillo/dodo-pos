namespace FerreSys.Application;

public sealed class TicketPiece
{
    public string? Text { get; init; }
    public bool IsLogo { get; init; }

    public static TicketPiece Logo() => new() { IsLogo = true };

    public static TicketPiece Lines(string text) => new() { Text = text };
}

public sealed class TicketDocument
{
    public required IReadOnlyList<TicketPiece> Pieces { get; init; }

    public string Text => string.Concat(Pieces.Select(piece => piece.Text ?? string.Empty));
}
