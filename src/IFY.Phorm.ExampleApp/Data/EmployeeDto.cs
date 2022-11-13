using IFY.Phorm.Data;
using System.Runtime.Serialization;

namespace IFY.Phorm.ExampleApp.Data;

public class EmployeeDto : ICreateEmployee
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ManagerDto Manager { get; set; } = null!;
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