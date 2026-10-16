namespace FerreSys.Domain;

public sealed class Customer
{
    public Guid Id { get; }
    public string Name { get; }
    public string Phone { get; }
    public string Email { get; }
    public string Notes { get; }
    public bool IsActive { get; }
    public DateTime CreatedAt { get; }
    public string Rfc { get; }
    public string LegalName { get; }
    public string TaxRegime { get; }
    public string TaxZipCode { get; }
    public string CfdiUse { get; }

    public Customer(
        Guid id,
        string name,
        string phone,
        string email,
        string notes,
        bool isActive,
        DateTime createdAt,
        string rfc = "",
        string legalName = "",
        string taxRegime = "",
        string taxZipCode = "",
        string cfdiUse = "")
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El cliente necesita un identificador.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("El teléfono del cliente es obligatorio.");

        Id = id;
        Name = name.Trim();
        Phone = phone.Trim();
        Email = email?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
        IsActive = isActive;
        CreatedAt = createdAt;
        Rfc = rfc?.Trim().ToUpperInvariant() ?? string.Empty;
        LegalName = legalName?.Trim() ?? string.Empty;
        TaxRegime = taxRegime?.Trim() ?? string.Empty;
        TaxZipCode = taxZipCode?.Trim() ?? string.Empty;
        CfdiUse = cfdiUse?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}

public sealed class CustomerBalance
{
    public Customer Customer { get; }
    public int Points { get; }

    public CustomerBalance(Customer customer, int points)
    {
        Customer = customer;
        Points = points;
    }

    public override string ToString() => $"{Customer.Name} · {Customer.Phone}";
}
