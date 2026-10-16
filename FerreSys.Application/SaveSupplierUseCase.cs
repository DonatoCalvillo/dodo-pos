using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class SupplierDraft
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public sealed class SaveSupplierUseCase
{
    private readonly ISupplierRepository _suppliers;

    public SaveSupplierUseCase(ISupplierRepository suppliers)
    {
        _suppliers = suppliers;
    }

    public Supplier Execute(SupplierDraft draft)
    {
        string name = draft.Name.Trim();
        if (name.Length < 2 || name.Length > 80)
            throw new InvalidOperationException("El nombre debe tener entre 2 y 80 caracteres.");

        string contact = draft.ContactName.Trim();
        if (contact.Length > 80)
            throw new InvalidOperationException("El contacto no puede pasar de 80 caracteres.");

        string phone = draft.Phone.Trim();
        if (phone.Length > 30)
            throw new InvalidOperationException("El teléfono no puede pasar de 30 caracteres.");

        string email = draft.Email.Trim();
        if (email.Length > 0
            && (email.Count(ch => ch == '@') != 1 || email.StartsWith('@') || email.EndsWith('@') || email.Length > 80))
            throw new InvalidOperationException("El correo no es válido.");

        string address = draft.Address.Trim();
        if (address.Length > 200)
            throw new InvalidOperationException("La dirección no puede pasar de 200 caracteres.");

        string notes = draft.Notes.Trim();
        if (notes.Length > 300)
            throw new InvalidOperationException("Las notas no pueden pasar de 300 caracteres.");

        Supplier? existing = draft.Id is Guid id ? _suppliers.Get(id) : null;
        if (draft.Id is Guid && existing == null)
            throw new InvalidOperationException("El proveedor ya no existe.");

        var supplier = new Supplier(
            existing?.Id ?? Guid.NewGuid(),
            name,
            contact,
            phone,
            email,
            address,
            notes,
            draft.IsActive,
            existing?.CreatedAt ?? DateTime.Now);

        _suppliers.Save(supplier);
        return supplier;
    }
}
