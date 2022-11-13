using IFY.Phorm.Data;
using System.Runtime.Serialization;

namespace IFY.Phorm.ExampleApp.Data;

public class ManagerDto : ICreateManager
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
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