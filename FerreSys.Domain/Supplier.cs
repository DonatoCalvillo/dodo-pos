namespace FerreSys.Domain;

public sealed class Supplier
{
    public Guid Id { get; }
    public string Name { get; }
    public string ContactName { get; }
    public string Phone { get; }
    public string Email { get; }
    public string Address { get; }
    public string Notes { get; }
    public bool IsActive { get; }
    public DateTime CreatedAt { get; }

    public Supplier(
        Guid id,
        string name,
        string contactName,
        string phone,
        string email,
        string address,
        string notes,
        bool isActive,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El proveedor necesita un identificador.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del proveedor es obligatorio.");

        Id = id;
        Name = name.Trim();
        ContactName = contactName?.Trim() ?? string.Empty;
        Phone = phone?.Trim() ?? string.Empty;
        Email = email?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
        IsActive = isActive;
        CreatedAt = createdAt;
    }
}
