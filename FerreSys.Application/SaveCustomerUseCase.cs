using System.Text.RegularExpressions;
using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class CustomerDraft
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public string Rfc { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string TaxRegime { get; init; } = string.Empty;
    public string TaxZipCode { get; init; } = string.Empty;
    public string CfdiUse { get; init; } = string.Empty;
}

public sealed class SaveCustomerUseCase
{
    private readonly ILoyaltyRepository _loyalty;

    public SaveCustomerUseCase(ILoyaltyRepository loyalty)
    {
        _loyalty = loyalty;
    }

    public Customer Execute(CustomerDraft draft)
    {
        string name = draft.Name.Trim();
        if (name.Length < 2 || name.Length > 80)
            throw new InvalidOperationException("El nombre debe tener entre 2 y 80 caracteres.");

        string phone = NormalizePhone(draft.Phone);
        string email = draft.Email.Trim();
        if (email.Length > 0
            && (email.Count(ch => ch == '@') != 1 || email.StartsWith('@') || email.EndsWith('@') || email.Length > 80))
            throw new InvalidOperationException("El correo no es válido.");

        string notes = draft.Notes.Trim();
        if (notes.Length > 300)
            throw new InvalidOperationException("Las notas no pueden pasar de 300 caracteres.");

        string rfc = NormalizeRfc(draft.Rfc);
        string legalName = draft.LegalName.Trim();
        if (legalName.Length > 254)
            throw new InvalidOperationException("La razón social no puede pasar de 254 caracteres.");

        string taxRegime = draft.TaxRegime.Trim();
        if (taxRegime.Length > 0 && !FiscalCatalog.IsTaxRegime(taxRegime))
            throw new InvalidOperationException("El régimen fiscal no es una clave válida.");

        string taxZip = draft.TaxZipCode.Trim();
        if (taxZip.Length > 0 && !ZipPattern.IsMatch(taxZip))
            throw new InvalidOperationException("El código postal fiscal debe tener 5 dígitos.");

        string cfdiUse = draft.CfdiUse.Trim().ToUpperInvariant();
        if (cfdiUse.Length > 0 && !FiscalCatalog.IsCfdiUse(cfdiUse))
            throw new InvalidOperationException("El uso de CFDI no es una clave válida.");

        Customer? existing = draft.Id is Guid id ? _loyalty.GetCustomer(id) : null;
        if (draft.Id is Guid missing && existing == null)
            throw new InvalidOperationException("El cliente ya no existe.");

        Customer? samePhone = _loyalty.FindByPhone(phone);
        if (samePhone != null && samePhone.Id != existing?.Id)
            throw new InvalidOperationException("Ese teléfono ya está registrado.");

        var customer = new Customer(
            existing?.Id ?? Guid.NewGuid(),
            name,
            phone,
            email,
            notes,
            draft.IsActive,
            existing?.CreatedAt ?? DateTime.Now,
            rfc,
            legalName,
            taxRegime,
            taxZip,
            cfdiUse);

        _loyalty.SaveCustomer(customer);
        return customer;
    }

    private static readonly Regex RfcPattern = new(
        "^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$",
        RegexOptions.CultureInvariant);

    private static readonly Regex ZipPattern = new("^[0-9]{5}$", RegexOptions.CultureInvariant);

    private static string NormalizeRfc(string rfc)
    {
        string value = rfc.Trim().ToUpperInvariant().Replace(" ", "");
        if (value.Length == 0)
            return string.Empty;

        if (!RfcPattern.IsMatch(value))
            throw new InvalidOperationException("El RFC debe tener 12 o 13 caracteres.");

        return value;
    }

    public static string NormalizePhone(string phone)
    {
        string digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 7 || digits.Length > 15)
            throw new InvalidOperationException("El teléfono debe tener entre 7 y 15 dígitos.");

        return digits;
    }
}
