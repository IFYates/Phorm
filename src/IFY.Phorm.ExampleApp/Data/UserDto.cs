using IFY.Phorm.Data;

namespace IFY.Phorm.ExampleApp.Data;

public class UserDto : ICreateUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[PhormContract(Namespace = "ExampleApp")]
public interface ICreateUser : IPhormContract
{
    long Id { set; }
    string Name { get; }
}