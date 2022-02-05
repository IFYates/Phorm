# Pho/rm
Pho/rm - The **P**rocedure-**h**eavy **o**bject-**r**elational **m**apping framework

## Goals
- An O/RM framework utilising stored procedures as the primary database interaction
- Enable decoupling of the database using well-defined contracts
- Easy to determine the behaviour from the code (no "magic")

## Driving principals
The are many, brilliant O/RM frameworks available using different paradigms for database interaction.  
Many of these allow for rapid adoption by strongly-coupling to the schema at the expense of control over the efficiency of the query and future structural mutability.  
As such solutions grow, it can become quickly difficult to evolve the underlying structures as well as to improve the way the data is accessed and managed.

Pho/rm was designed to provide a small and controlled surface between the business logic layer and the data layer by pushing the shaping of data to the data provider and encouraging the use of discrete contracts.  
Our goal is to have a strongly-typed data surface and allow for a mutable physical data structure, where responsibility of the layers can be strictly segregated.

With this approach, the data management team can provide access contracts to meet the business logic requirements, which the implementing team can rely on without concern over the underlying structures.

## Features
- Invoke stored procedures using natural language
- Read from tables and views
- Supports output arguments and return values
- Supports multiple result sets
- Datasource agnostic (SqlClient support provided by default)
- Fully interfaced for DI and mocking
- Transaction support
- DTO field aliasing
- DTO field transformation via extensions (e.g., JSON)
- Application-level field encryption/decryption

### Remaining
- Detailed logging (Microsoft.Extensions.Logging)
- Useful test helpers

## Antithesis
Pho/rm requires significant code to wire the data layer to the business logic; this is intentional to the design and will not be resolved by this project.

## Basic example
For typical entity CRUD support, a Pho/rm solution would require a minimum of:
1. Existing tables in the data source
1. A POCO to represent the entity (DTO); ideally with a contract for each database action
1. A stored procedure to fetch the entity
1. At least one stored procedure to handle create, update, delete (though, ideally, one for each)

A simple Pho/rm use would have the structure:
```SQL
CREATE TABLE [dbo].[Data] (
    [Id] BIGINT NOT NULL PRIMARY KEY,
    [Key] NVARCHAR(50) NOT NULL UNIQUE,
    [Value] NVARCHAR(256) NULL
)

CREATE PROCEDURE [dbo].[usp_SaveData] (
    @Key NVARCHAR(50),
    @Value NVARCHAR(256),
    @Id BIGINT = NULL OUTPUT
) AS BEGIN
    INSERT INTO [dbo].[Data] ([Key], [Value]) SELECT @Key, @Value
    SET @Id = SCOPE_IDENTITY()
    RETURN 1 -- Success
END
```
```CSharp
// DTO and contracts
[PhormContract(Name = "Data")]
class DataItem : ISaveData
{
    public long Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
}
interface ISaveData
{
    long Id { set; } // Output
    string Key { get; }
    string Value { get; }
}

// Configure Pho/rm session to SQL Server
var connection = new SqlConnectionProvider(connectionString);
var session = new SqlPhormSession(connection, null);

// Use
var allData = session.Get<DataItem[]>();
session.Call<ISaveData>(new { Key = "Name", Value = "T Ester" });
var data = session.Get<DataItem>(new { Key = "Name" });
```

## Secondary resultsets
Pho/rm supports additional resultsets in procedure responses in order to provide parent-child data in a single request.  
This is achieved by defining a selector for the relationship.

Note that any child value returned that is not matched by a selector is discarded.

```CSharp
public record ChildContract(long ParentId);
[PhormContract(Name = "ParentsWithChildren")]
public record ParentContract(long Id)
{
    [Recordset(order: 0, selectorProperty: nameof(ChildrenSelector))]
    public ChildContract[] Children { get; set; } // The property to fill with selected child entities
    public static RecordMatcher<ParentContract, ChildContract> ChildrenSelector => new((parent, child) => child.ParentId == parent.Id); // The logic for selecting child entities
}

ParentContract[] result = phorm.Get<ParentContract[]>()!; // Get many from table ParentsWithChildren
```

This example will result in each parent entity instance containing a list of all child entities matching on the id.

## Best practices
* A contract per operation (avoid the ad hoc solution for complete code)
* Wrap related operations in a "gateway", to reduce exposure of the `IPhormConnection` instance.
* Procedure names prefixed with `usp_`
* Procedures return a positive number on success and <= 0 on error
* Procedures output detailed error information starting with `ERR:`

## Future considerations
- Support reading from views
- Tool to generate library of minimal contracts from sproc definitions

## Configuration
- Stored procedure prefix (default "usp_")
- View name prefix (default "vw_")
- Table name prefix (no default)

## Extensions
- OnConnected action against connection

## Attribute usage
```
DataContract // This class/struct/record is a contract, where all public properties are included by default
    Name = "" // The procedure name to use, if different to the contract name
    Namespace = "" // The schema name to use, if not the current database user

PhormContract // Same as DataContract, but also for action contracts (interfaces)
    DbObjectType Target // For choosing between procedure, table, view

DataMember // This property is included in the contract, with possible customisation
    EmitDefaultValue = false // Whether to always send the default value
    IsRequired = false // Whether to fail if this property is not set on fetch
    Name = "" // Override the property name matched in the datasource

IgnoreDataMember // Do not include this property in the contract
```

## JSON data
Pho/rm supports storing JSON objects for complex types.

```CSharp
public record MyData(string Value1, string Value2);

// This contract will expect column "Data" to be a string representation of a MyData instance
[DataContract] public record RecordDTO (long Id, [property: JsonValue] MyData Data);

// This contract will send a varchar "@Data" of the serialised MyData instance
[PhormContract] public interface IRecord_UpdateState { long Id { get; } [JsonValue] MyData Data { get; } }
```

## Enums
Pho/rm will implicitly deal with enum value transformation.

```CSharp
public enum MyState
{
    State1 = 1,
    State2,
    [EnumMember("State3")]
    StateX
}

// Get will implicitly support receiving a numeric value (1, 2, 3) or string of "State1", "State2", or "State3" (but not "StateX")
// All uses of a Call will send the numeric value
[DataContract] public record RecordDTO (long Id, MyState State);

// Call using this contract will send the string equivalent ("State1", "State2", "State3")
[PhormContract] public interface IRecord_UpdateState { long Id { get; } [EnumValue(SendAsString = true)] MyState State { get; } }
```

## Data encryption
Pho/rm supports application-level encryption of data in either direction to the datasource.

```CSharp
[DataContract] public record RecordDTO ( long Id ) { [SecureValue("Test", nameof(Id))] public string Data { get; set; } }
[PhormContract] public interface IRecord_UpdateData { long Id { get; } [SecureValue("DataClassification", nameof(Id))] string Data { get; } }

public class MyEncryptionProvider : IEncryptionProvider
{
    public IEncryptor GetInstance(string dataClassification) => new MyEncryptor() { DataClassification = dataClassification };
}
// Registered at startup: phormSettings.EncryptionProvider = new MyEncryptionProvider();

public class MyEncryptor : IEncryptor
{
        public string DataClassification { get; init; }
        public byte[] Authenticator { get; set; }
        public byte[] InitialVector { get; init; }

        public byte[] Encrypt(byte[] data)
        {
            // TODO: encrypt data using this state
        }

        public byte[] Decrypt(byte[] data)
        {
            // TODO: decrypt data using this state
        }
}
```

## Calculated parameters
For action contracts, additional parameters can be defined directly in the contract:
```CSharp
[PhormContract]
public interface IRecord_Update
{
    long Id { get; }

    [CalculatedValue] public string NewParameter() { return "NewValue"; }
}
```

This will call the action contract with an additional `@NewParameter = 'NewValue'`.

## Console/Error messages
TODO

## Connection context
TODO

## GenSpec support
TODO