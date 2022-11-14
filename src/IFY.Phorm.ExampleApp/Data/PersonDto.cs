using IFY.Phorm.Data;

namespace IFY.Phorm.ExampleApp.Data;

public abstract class PersonDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? PersonType { get; set; }
}

[PhormContract(Namespace = "ExampleApp")]
public interface IGetAllPeople : IPhormContract
{
}