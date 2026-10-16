namespace FerreSys.Domain;

public interface ISupplierRepository
{
    IReadOnlyList<Supplier> List();

    Supplier? Get(Guid id);

    void Save(Supplier supplier);
}
