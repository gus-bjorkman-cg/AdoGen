namespace AdoGen.Abstractions;

/// <summary>
/// Represents the supported bulk operation types that is supported by this extension.
/// It's needed for the SQL statement to use the correct SQL syntax for the bulk operation being performed.
/// </summary>
public readonly record struct BulkOp
{
    /// <summary>
    /// The single character value representing the bulk operation: 'I' for Insert, 'U' for Update, and 'D' for Delete.
    /// </summary>
    public char Value { get; }
    
    /// <summary>
    /// The single character value representing the bulk operation: 'I' for Insert, 'U' for Update, and 'D' for Delete.
    /// </summary>
    public string StringValue { get; }
    
    private BulkOp(char value)
    {
        Value = value;
        StringValue = value.ToString();
    }
    
    /// <summary>
    /// Represents an Insert operation in a bulk operation context. The Value property will be 'I'.
    /// </summary>
    public static readonly BulkOp Insert = new('I');
    
    /// <summary>
    /// Represents an Update operation in a bulk operation context. The Value property will be 'U'.
    /// </summary>
    public static readonly BulkOp Update = new('U');
    
    /// <summary>
    /// Represents a Delete operation in a bulk operation context. The Value property will be 'D'.
    /// </summary>
    public static readonly BulkOp Delete = new('D');

    /// <summary>
    /// Returns the string representation of the BulkOp, which is the single character value ('I', 'U', or 'D').
    /// </summary>
    /// <returns></returns>
    public override string ToString() => StringValue;
}