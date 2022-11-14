using IFY.Phorm.Data;

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

    public GenSpec<PersonDto, EmployeeDto, ManagerDto> GetAllPeople()
    {
        return _session.From<IGetAllPeople>()
            .Get<GenSpec<PersonDto, EmployeeDto, ManagerDto>>()!;
    }

    public ManagerDtoWithEmployees GetManager(long id)
    {
        return _session.From<IGetManager>(new { Id = id })
            .Get<ManagerDtoWithEmployees>()!;
    }
}
