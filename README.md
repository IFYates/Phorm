# Pho/rm - The **P**rocedure-**h**eavy **o**bject-**r**elational **m**apping framework

A full O/RM, focused on strong separation between the data structures and the business entity representation.

See our [ethos](ethos) for how and why Pho/rm is different to other O/RMs.

The [wiki](wiki) contains lots of useful examples of the various [features](#feature-list), as well as a [getting started guide](getting-started).

Pho/rm supports:
* [Entity data mapping](howto-get)
* [Child entities](howto-get#resultsets)
* [Entity polymorphism](howto-get#genspec)
* [All CRUD operations](howto-call)
* [Transactions](howto-connectivity#transactions)
* [Logging unexpected behaviour](howto-events)
* [Your DI framework](howto-di)
* And more!

## Driving principals
The are many, brilliant O/RM frameworks available using different paradigms for database interaction.  
Many of these allow for rapid adoption by strongly-coupling to the storage schema at the expense of control over the efficiency of the query and future structural mutability.  
As such solutions grow, it can become quickly difficult to evolve the underlying structures as well as to improve the way the data is accessed and managed.

Pho/rm was designed to provide a small and controlled surface between the business logic layer and the data layer by pushing the shaping of data to the data provider and encouraging the use of discrete contracts.  
Our goal is to have a strongly-typed data surface and allow for a mutable physical data structure, where responsibility of the layers can be strictly segregated.

With this approach, the data management team can provide access contracts to meet the business logic requirements, which the implementing team can rely on without concern over the underlying structures and query efficiency.

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