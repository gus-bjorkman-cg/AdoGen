namespace AdoGen.PostgreSql;

/// <summary>
/// Represents the supported bulk operation types that is supported by this extension.
/// It's needed for the SQL statement to use the correct SQL syntax for the bulk operation being performed.
/// </summary>
public readonly record struct NpgsqlBulkOp
{
    /// <summary>
    /// The single character value representing the bulk operation: 'I' for Insert, 'U' for Update, and 'D' for Delete.
    /// </summary>
    public char Value { get; }

    /// <summary>
    /// The single character value representing the bulk operation: 'I' for Insert, 'U' for Update, and 'D' for Delete.
    /// </summary>
    public string StringValue { get; }

    private NpgsqlBulkOp(char value)
    {
        Value = value;
        StringValue = value.ToString();
    }

    /// <summary>
    /// Represents an Insert operation in a bulk operation context. The Value property will be 'I'.
    /// </summary>
    public static readonly NpgsqlBulkOp Insert = new('I');

    /// <summary>
    /// Represents an Update operation in a bulk operation context. The Value property will be 'U'.
    /// </summary>
    public static readonly NpgsqlBulkOp Update = new('U');

    /// <summary>
    /// Represents a Delete operation in a bulk operation context. The Value property will be 'D'.
    /// </summary>
    public static readonly NpgsqlBulkOp Delete = new('D');

    /// <summary>
    /// Represents an Upsert (insert-or-update) operation in a bulk operation context. The Value property will be 'M'.
    /// </summary>
    public static readonly NpgsqlBulkOp Upsert = new('M');

    /// <summary>
    /// Returns the string representation of the NpgsqlBulkOp, which is the single character value ('I', 'U', 'D', or 'M').
    /// </summary>
    public override string ToString() => StringValue;
}
