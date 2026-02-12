using System;
using System.Data;

namespace AdoGen.Abstractions;

/// <summary>
/// Minimal base reader for SqlBulkCopy. Derived types implement core metadata + value access.
/// </summary>
public abstract class BulkDataReaderBase : IDataReader
{
    public abstract int FieldCount { get; }
    public abstract object GetValue(int i);
    public abstract string GetName(int i);
    public abstract Type GetFieldType(int i);
    public abstract int GetOrdinal(string name);
    public abstract bool Read();

    public bool IsDBNull(int i) => GetValue(i) is DBNull;

    public int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, FieldCount);
        for (var i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }
    
    public void Close() { }
    public DataTable GetSchemaTable() => throw new NotSupportedException();
    public bool NextResult() => false;
    public int Depth => 0;
    public bool IsClosed => false;
    public int RecordsAffected => -1;
    
    public object this[int i] => GetValue(i);
    public object this[string name] => GetValue(GetOrdinal(name));
    
    public bool GetBoolean(int i) => (bool)GetValue(i);
    public byte GetByte(int i) => (byte)GetValue(i);
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public char GetChar(int i) => (char)GetValue(i);
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public string GetDataTypeName(int i) => GetFieldType(i).Name;
    public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
    public decimal GetDecimal(int i) => (decimal)GetValue(i);
    public double GetDouble(int i) => (double)GetValue(i);
    public float GetFloat(int i) => (float)GetValue(i);
    public Guid GetGuid(int i) => (Guid)GetValue(i);
    public short GetInt16(int i) => (short)GetValue(i);
    public int GetInt32(int i) => (int)GetValue(i);
    public long GetInt64(int i) => (long)GetValue(i);
    public string GetString(int i) => (string)GetValue(i);
    public IDataReader GetData(int i) => throw new NotSupportedException();

    public void Dispose() { }
}