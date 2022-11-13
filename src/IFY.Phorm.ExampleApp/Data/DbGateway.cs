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

    public bool CreateEmployee(EmployeeDto employee)
    {
        var res = _session.Call<ICreateEmployee>(employee);
        return res == 1;
    }

    public bool CreateManager(ManagerDto manager)
    {
        var res = _session.Call<ICreateManager>(manager);
        return res == 1;
    }
}
