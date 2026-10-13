namespace FerreSys.Domain;

public interface IUserRepository
{
    User? GetById(Guid id);

    User? GetByUsername(string username);

    User? GetByEmployeeNumber(string employeeNumber);

    IReadOnlyList<User> List();

    void Save(User user);

    string NextEmployeeNumber();
}
