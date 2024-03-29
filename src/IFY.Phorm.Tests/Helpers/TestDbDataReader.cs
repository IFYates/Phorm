﻿using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests;

[ExcludeFromCodeCoverage]
public class TestDbDataReader : DbDataReader
{
    public Dictionary<string, object>? Tuple { get; private set; } = null;
    public List<Dictionary<string, object>> Data
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    } = new List<Dictionary<string, object>>();
    public List<Dictionary<string, object>[]> Results
    {
        get;
#if !NET5_0_OR_GREATER
        set;
#else
        init;
#endif
    } = new List<Dictionary<string, object>[]>();

    public override object this[int ordinal] => throw new NotImplementedException();

    public override object this[string name] => Tuple![name];

    public override int Depth => throw new NotImplementedException();

    public override int FieldCount => Tuple?.Count ?? Data.FirstOrDefault()?.Count ?? 0;

    public override bool HasRows => Data.Count > 0;

    public override bool IsClosed => throw new NotImplementedException();

    public override int RecordsAffected => throw new NotImplementedException();

    public override bool GetBoolean(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override byte GetByte(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        throw new NotImplementedException();
    }

    public override char GetChar(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        throw new NotImplementedException();
    }

    public override string GetDataTypeName(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override DateTime GetDateTime(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override decimal GetDecimal(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override double GetDouble(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public override Type GetFieldType(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override float GetFloat(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override Guid GetGuid(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override short GetInt16(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override int GetInt32(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override long GetInt64(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override string GetName(int ordinal)
    {
        return (Tuple ?? Data.FirstOrDefault())?.Keys.ElementAt(ordinal) ?? string.Empty;
    }

    public override int GetOrdinal(string name)
    {
        throw new NotImplementedException();
    }

    public override string GetString(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override object GetValue(int ordinal)
    {
        return (Tuple ?? Data.FirstOrDefault())?.Values.ElementAt(ordinal) ?? string.Empty;
    }

    public override int GetValues(object[] values)
    {
        throw new NotImplementedException();
    }

    public override bool IsDBNull(int ordinal)
    {
        throw new NotImplementedException();
    }

    public override bool NextResult()
    {
        if (Results.Count > 0)
        {
            Data.Clear();
            Data.AddRange(Results[0]);
            Results.RemoveAt(0);
            return true;
        }
        return false;
    }

    public override bool Read()
    {
        Tuple = null;
        if (Data.Count > 0)
        {
            Tuple = Data[0];
            Data.RemoveAt(0);
        }
        return Tuple != null;
    }
}
