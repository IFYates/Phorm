using IFY.Phorm.Data;
using System.Runtime.Serialization;

namespace IFY.Phorm.ExampleApp.Data;

[PhormSpecOf(nameof(PersonType), "Manager")]
public class ManagerDto : PersonDto, ICreateManager
{
    [DataMember(Name = "DepartmentName")]
    public string Department { get; set; } = string.Empty;
}

public class ManagerDtoWithEmployees : ManagerDto
{
    [Resultset(0, nameof(EmployeeSelector))]
    public EmployeeDto[] Employees { get; set; } = Array.Empty<EmployeeDto>();
    public static IRecordMatcher EmployeeSelector { get; }
        = new RecordMatcher<ManagerDto, EmployeeDto>((p, c) => p.Id == c.ManagerId);
}

[PhormContract(Namespace = "ExampleApp")]
public interface ICreateManager : IPhormContract
{
    long Id { set; }
    [DataMember(IsRequired = true)]
    string Name { get; }
    [DataMember(Name = "DepartmentName", IsRequired = true)]
    string Department { get; }
}

[PhormContract(Namespace = "ExampleApp")]
public interface IGetManager : IPhormContract
{
    long Id { get; }
}