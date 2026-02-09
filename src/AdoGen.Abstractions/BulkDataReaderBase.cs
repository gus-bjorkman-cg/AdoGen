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

    public virtual bool IsDBNull(int i) => GetValue(i) is DBNull;

    public virtual int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, FieldCount);
        for (var i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }

    // IDataReader
    public virtual void Close() { }
    public virtual DataTable GetSchemaTable() => throw new NotSupportedException();
    public virtual bool NextResult() => false;
    public virtual int Depth => 0;
    public virtual bool IsClosed => false;
    public virtual int RecordsAffected => -1;

    // IDataRecord indexers
    public virtual object this[int i] => GetValue(i);
    public virtual object this[string name] => GetValue(GetOrdinal(name));

    // IDataRecord typed getters (implemented via GetValue; OK for completeness)
    public virtual bool GetBoolean(int i) => (bool)GetValue(i);
    public virtual byte GetByte(int i) => (byte)GetValue(i);
    public virtual long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public virtual char GetChar(int i) => (char)GetValue(i);
    public virtual long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public virtual string GetDataTypeName(int i) => GetFieldType(i).Name;
    public virtual DateTime GetDateTime(int i) => (DateTime)GetValue(i);
    public virtual decimal GetDecimal(int i) => (decimal)GetValue(i);
    public virtual double GetDouble(int i) => (double)GetValue(i);
    public virtual float GetFloat(int i) => (float)GetValue(i);
    public virtual Guid GetGuid(int i) => (Guid)GetValue(i);
    public virtual short GetInt16(int i) => (short)GetValue(i);
    public virtual int GetInt32(int i) => (int)GetValue(i);
    public virtual long GetInt64(int i) => (long)GetValue(i);
    public virtual string GetString(int i) => (string)GetValue(i);
    public virtual IDataReader GetData(int i) => throw new NotSupportedException();

    public virtual void Dispose() { }
}