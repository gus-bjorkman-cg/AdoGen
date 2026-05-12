using System;

namespace AdoGen.SqlServer;

/// <summary>
/// Thrown by generated <c>UpdateAsync</c> and <c>DeleteAsync</c> methods when 0 rows are
/// affected due to an optimistic concurrency conflict — i.e. the row was modified or deleted
/// by another process between the time it was read and the time the write was attempted.
/// </summary>
/// <remarks>
/// The <see cref="TableName"/> property carries the schema-qualified table name.
/// Avoid logging it automatically in security-sensitive environments; consider catching this
/// exception and logging only a sanitized message.
/// </remarks>
public sealed class AdoGenConcurrencyException : Exception
{
    /// <summary>
    /// The schema-qualified table name (e.g. <c>dbo.Orders</c>) where the conflict occurred.
    /// </summary>
    public string TableName { get; }

    /// <param name="tableName">Schema-qualified table name, e.g. <c>dbo.Orders</c>.</param>
    public AdoGenConcurrencyException(string tableName)
        : base("A concurrency conflict was detected. The row was modified or deleted by another process.")
        => TableName = tableName;
}
