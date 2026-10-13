using FerreSys.Domain;

namespace FerreSys.Application;

public class ListUsersUseCase
{
    private readonly IUserRepository _users;

    public ListUsersUseCase(IUserRepository users)
    {
        _users = users;
    }

    public IReadOnlyList<User> Execute() => _users.List();
}
