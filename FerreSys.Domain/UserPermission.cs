namespace FerreSys.Domain;

[Flags]
public enum UserPermission
{
    None = 0,
    Sell = 1,
    ViewSales = 2,
    Products = 4,
    ImportProducts = 8,
    Cash = 16,
    Reports = 32,
    Settings = 64,
    ManageUsers = 128,

    All = Sell | ViewSales | Products | ImportProducts | Cash | Reports | Settings | ManageUsers
}
