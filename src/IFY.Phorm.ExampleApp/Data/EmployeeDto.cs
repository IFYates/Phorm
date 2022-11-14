using IFY.Phorm.Data;
using System.Runtime.Serialization;

namespace IFY.Phorm.ExampleApp.Data;

[PhormSpecOf(nameof(PersonType), "Employee")]
public class EmployeeDto : PersonDto, ICreateEmployee
{
    public ManagerDto Manager { get; set; } = null!;

    public long ManagerId
    {
        get => Manager?.Id ?? _managerId;
        set => _managerId = value;
    }
    private long _managerId;
}

[PhormContract(Namespace = "ExampleApp")]
public interface ICreateEmployee : IPhormContract
{
    long Id { set; }
    [DataMember(IsRequired = true)]
    string Name { get; }
    long ManagerId => Manager.Id;

    [IgnoreDataMember]
    ManagerDto Manager { get; }
}