using System;
using System.Data;

namespace AdoGen.Abstractions;

/// <summary>
/// Minimal base reader for SqlBulkCopy. Derived types implement core metadata + value access.
/// Implemented by the generated code for bulk copy operations.
/// This base class is used to reduce the code in the generated files.
/// </summary>
public abstract class BulkDataReaderBase : IDataReader
{
    /// <summary>
    /// Field count on the bulk class.
    /// </summary>
    public abstract int FieldCount { get; }
    
    /// <summary>
    /// Gets the value of the specified field.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public abstract object GetValue(int i);
    
    /// <summary>
    /// Gets the name of the specified field.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public abstract string GetName(int i);
    
    /// <summary>
    /// Gets the data type of the specified field.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public abstract Type GetFieldType(int i);
    
    /// <summary>
    /// Gets the column ordinal given the name of the column.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public abstract int GetOrdinal(string name);
    
    /// <summary>
    /// Advances the reader to the next record. Returns false if there are no more rows.
    /// </summary>
    /// <returns></returns>
    public abstract bool Read();

    /// <summary>
    /// Gets a value indicating whether the specified field is set to null.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public bool IsDBNull(int i) => GetValue(i) is DBNull;

    /// <summary>
    /// Gets all the values of the current record in the provided array. Returns the number of values read.
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, FieldCount);
        for (var i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }
    
    /// <summary>
    /// Closes the reader. For bulk copy operations, this is a no-op since the reader is not connected
    /// to any external resource.
    /// </summary>
    public void Close() { }
    
    /// <summary>
    /// Gets a DataTable that describes the column metadata of the reader. 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public DataTable GetSchemaTable() => throw new NotSupportedException();
    
    /// <summary>
    /// Advances the reader to the next result when reading the results of batch SQL statements.
    /// For bulk copy operations, this is a no-op since there are no multiple results.
    /// </summary>
    /// <returns></returns>
    public bool NextResult() => false;
    
    /// <summary>
    /// Gets the depth of nesting for the current row.
    /// For bulk copy operations, this is always 0 since there are no nested results.
    /// </summary>
    public int Depth => 0;
    
    /// <summary>
    /// Gets a value indicating whether the reader is closed. For bulk copy operations,
    /// this is always false since the reader is not connected to any external resource and does not need to be closed.
    /// </summary>
    public bool IsClosed => false;
    
    /// <summary>
    /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
    /// For bulk copy operations, this is always -1 since the reader does not execute any SQL statement
    /// and does not track the number of affected rows.
    /// </summary>
    public int RecordsAffected => -1;
    
    /// <summary>
    /// Gets the value of the specified field by index or name.
    /// This is a convenience accessor that calls GetValue internally.
    /// </summary>
    /// <param name="i"></param>
    public object this[int i] => GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field by name.
    /// This is a convenience accessor that calls GetOrdinal and GetValue internally.
    /// </summary>
    /// <param name="name"></param>
    public object this[string name] => GetValue(GetOrdinal(name));
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a bool.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public bool GetBoolean(int i) => (bool)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a byte.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public byte GetByte(int i) => (byte)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a byte array.
    /// Since bulk copy operations do not support partial reads, this method is not supported and will throw
    /// a NotSupportedException if called.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="fieldOffset"></param>
    /// <param name="buffer"></param>
    /// <param name="bufferoffset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a char.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public char GetChar(int i) => (char)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a char array.
    /// Since bulk copy operations do not support partial reads, this method is not supported and will throw
    /// a NotSupportedException if called.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="fieldoffset"></param>
    /// <param name="buffer"></param>
    /// <param name="bufferoffset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    
    /// <summary>
    /// Gets the name of the data type of the specified field.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public string GetDataTypeName(int i) => GetFieldType(i).Name;
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a DateTime.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public DateTime GetDateTime(int i) => (DateTime)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a decimal.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public decimal GetDecimal(int i) => (decimal)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a double.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public double GetDouble(int i) => (double)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a float.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public float GetFloat(int i) => (float)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a Guid.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public Guid GetGuid(int i) => (Guid)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a short.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public short GetInt16(int i) => (short)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to an int.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public int GetInt32(int i) => (int)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a long.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public long GetInt64(int i) => (long)GetValue(i);
    
    /// <summary>
    /// Gets the value of the specified field and casts it to a string.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public string GetString(int i) => (string)GetValue(i);
    
    /// <summary>
    /// Gets an IDataReader for the specified field index. Since bulk copy operations do not support nested readers,
    /// this method is not supported and will throw a NotSupportedException if called.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public IDataReader GetData(int i) => throw new NotSupportedException();

    /// <summary>
    /// Disposes the reader. For bulk copy operations, this is a no-op since the reader is not connected
    /// to any external resource and does not need to be disposed.
    /// </summary>
    public void Dispose() => GC.SuppressFinalize(this);
}