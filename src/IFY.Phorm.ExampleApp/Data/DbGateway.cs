namespace IFY.Phorm.ExampleApp.Data;

/// <summary>
/// Repository of database access logic.
/// </summary>
public class DbGateway
{
    private readonly IPhormSession _session;

    public DbGateway(IPhormSession session)
    {
        _session = session;
    }

    public bool CreateUser(UserDto user)
    {
        var res = _session.Call<ICreateUser>(user);
        return res == 1;
    }
}
