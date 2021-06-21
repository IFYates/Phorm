# Pho/rm
Pho/rm - The **P**rocedure-**h**eavy **o**bject-**r**elational **m**apping framework

## Goals
- An O/RM framework utilising stored procedures as the primary database interaction
- Solving a specific style of data access - not a one-size-fits-all solution

## Driving principals
The are many, brilliant O/RM frameworks available using different paradigms for database interaction.  
Many of these allow for rapid adoption by strongly-coupling to the schema at the expense of control over the efficiency of the query and future structural mutability.  
As such solutions grow, it can become quickly difficult to evolve the underlying structures as well as to improve the way the data is accessed and managed.

Pho/rm was designed to provide a small and controlled surface between the business logic layer and the data layer by pushing the shaping of data to the data provider and encouraging the use of discrete contracts.  
Our goal is to have a strongly-typed data surface and allow for a mutable physical data structure, where responsibility of the layers can be strictly segregated.

With this approach, the data management team can provide access contracts to meet the business logic requirements, which the implementing team can rely on without concern over the underlying structures.

## Features
- Simple control over stored procedure features and support
- Datasource agnostic (SqlClient support provided by default)
- Fully interfaced for DI and mocking
- Supports multiple result sets
- Supports procedure output and return values
- Supports transactions
- Detailed logging (NLog)
- DTO field aliasing
- DTO field transformation via extensions (JSON, enum, encryption included by default)
- DTO read/write to implicitly named sprocs
- Test helper for checking anonymous objects
- Read from table/view, but not write to

## Antithesis
Pho/rm requires significant code to wire the data layer to the business logic; this is intentional to the design and will not be resolved by this project.

For typical entity CRUD support, a Pho/rm solution would require a minimum of:
1. Existing tables in the data source
1. A code object to represent the entity (DTO); ideally with a contract for each action
1. A sproc to fetch the entity
1. At least one sproc to handle create, update, delete (though, ideally, one for each)

## Basic example
A simple Pho/rm use would have the structure:
```CSharp
// DTO + contracts
[DataContract] public record RecordDTO (long Id, string Name, DateTime LastModified, [property: IgnoreDataMember] string NotFromDatasource) : IRecord_GetById;
[PhormContract] public interface IRecord_GetById { long Id { get; } }
[PhormContract] public interface IRecord_UpdateName { long Id { get; }, string Name { get; }, DateTime LastModified { set; } }

// Configured factory instance creates a data connection
var factory = di.Resolve<IPhormConnectionFactory>();
IPhormConnection conn = factory.GetDefaultConnection();

// Data connection used to fetch an entity via a named sproc in different ways
RecordDTO data = conn.Get<RecordDTO>("Record_GetById", new { Id = id }); // Fully ad hoc
RecordDTO data = conn.Get<RecordDTO, IRecord_GetById>(new { Id = id }); // Anon parameters

IRecord_GetById q = new RecordDTO { Id = id }; // Or any IRecord_GetById implementation
RecordDTO data = conn.Get<RecordDTO>("Record_GetById", q); // Ad hoc procedure
RecordDTO data = conn.Get<RecordDTO, IRecord_GetById>(q); // Query instance

// Update the entity in different ways
var lastModifiedProperty = new DbField<DateTime>();
int result = conn.Call("Record_UpdateName", new { Id = id, Name = name, LastModified = lastModifiedProperty }); // Fully ad hoc
int result = conn.Call<IRecord_UpdateName>(new { Id = id, Name = name, LastModified = lastModifiedProperty }); // Anon parameters
int result = conn.Call("Record_UpdateName", data); // Ad hoc procedure
int result = conn.Call<IRecord_UpdateName>(data); // Entity instance
```

Each of the `Get` requests will execute something like `usp_Record_GetById @Id = {id}`.

Each of the `Call` requests execute sometiong like `usp_Record_UpdateName @Id = {id}, @Name = {name}` and will update the `LastModified` property (`lastModifiedProperty` or `data.LastModified`).

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

## Extensions
- OnConnected action against connection

## Attribute usage
```
DataContract // This class/struct/record is a contract, where all public properties are included by default
    Name = "" // The procedure name to use, if different to the contract name
    Namespace = "" // The schema name to use, if not the current database user

PhormContract // Same as DataContract, but also for action contracts (interfaces)

DataMember // This property is included in the contract, with possible customisation
    EmitDefaultValue = false // Whether to always send the default value
    IsRequired = false // Whether to fail if this property is not set on fetch
    Name = "" // Override the property name matched in the datasource

// Need: To/From Json, To/From Encryption, To/From enum type (int/string)

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

public MyEncryptor : IEncryptor
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

## Connection context