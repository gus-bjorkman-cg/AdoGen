using System;
using System.Linq.Expressions;
using NpgsqlTypes;

namespace AdoGen.PostgreSql;

#pragma warning disable CA1720 // Identifier contains type name — intentional SQL type naming

/// <summary>
/// The generator inspects the constructor body and reads calls to RuleFor(...).
/// It should be used to configure db types and properties for PostgreSQL.
/// </summary>
public abstract class NpgsqlProfile<T>
{
    /// <summary>
    /// Entry-point for configuring a property.
    /// The generator parses the fluent calls that follow.
    /// </summary>
    protected PropertyBuilder<TProp> RuleFor<TProp>(Expression<Func<T, TProp>> selector) => new(selector);

    /// <summary>
    /// Allows a custom table name.
    /// </summary>
    protected NpgsqlProfile<T> Table(string name) => this;

    /// <summary>
    /// Allows a custom schema.
    /// </summary>
    protected NpgsqlProfile<T> Schema(string name) => this;

    /// <summary>
    /// Allows a custom id field.
    /// </summary>
    protected NpgsqlProfile<T> Key<TProp>(Expression<Func<T, TProp>> selector) => this;

    /// <summary>
    /// Configuration of identity fields.
    /// </summary>
    protected NpgsqlProfile<T> Identity<TProp>(Expression<Func<T, TProp>> selector) => this;
}

/// <summary>
/// Fluent builder used purely for compile-time configuration.
/// All arguments should be literals or consts, so the generator can read them safely.
/// </summary>
public sealed class PropertyBuilder<TProp>
{
    internal PropertyBuilder(LambdaExpression selector) => Selector = selector;
    internal LambdaExpression Selector { get; }

    /// <summary>
    /// Configures the PostgreSQL/Npgsql type.
    /// </summary>
    public PropertyBuilder<TProp> Type(NpgsqlDbType dbType) => this;

    /// <summary>
    /// Configures the string size.
    /// </summary>
    public PropertyBuilder<TProp> Size(int size) => this;

    /// <summary>
    /// Configures the decimal precision.
    /// </summary>
    public PropertyBuilder<TProp> Precision(int precision) => this;

    /// <summary>
    /// Configures the decimal scale.
    /// </summary>
    public PropertyBuilder<TProp> Scale(int scale) => this;

    /// <summary>
    /// Configures the db column / parameter name. Default is property name.
    /// </summary>
    public PropertyBuilder<TProp> Name(string parameterName) => this;

    /// <summary>
    /// Shorthand config for setting db type as Numeric with its precision and scale.
    /// </summary>
    public PropertyBuilder<TProp> Decimal(int precision, int scale) =>
        Type(NpgsqlDbType.Numeric).Precision(precision).Scale(scale);

    /// <summary>
    /// Shorthand config for setting db type as Text.
    /// </summary>
    public PropertyBuilder<TProp> Text() => Type(NpgsqlDbType.Text);

    /// <summary>
    /// Shorthand config for setting db type as Varchar with its size.
    /// </summary>
    public PropertyBuilder<TProp> VarChar(int size) => Type(NpgsqlDbType.Varchar).Size(size);

    /// <summary>
    /// Shorthand config for setting db type as Char with its size.
    /// </summary>
    public PropertyBuilder<TProp> Char(int size) => Type(NpgsqlDbType.Char).Size(size);

    /// <summary>
    /// Shorthand config for setting db type as Varbit with its size.
    /// </summary>
    public PropertyBuilder<TProp> Varbit(int size) => Type(NpgsqlDbType.Varbit).Size(size);

    /// <summary>
    /// Shorthand config for setting db type as Bytea.
    /// </summary>
    public PropertyBuilder<TProp> Bytea() => Type(NpgsqlDbType.Bytea);

    /// <summary>
    /// Configures the db type to be nullable.
    /// </summary>
    public PropertyBuilder<TProp> Nullable() => this;

    /// <summary>
    /// Configures the db type to be non-null.
    /// </summary>
    public PropertyBuilder<TProp> NotNull() => this;

    /// <summary>
    /// Marks the column as read-only.
    /// The column is excluded from INSERT/UPDATE/bulk-write column lists but is still
    /// included in CREATE TABLE DDL and read back by the mapper.
    /// Use this for server-managed columns such as computed columns or audit timestamps
    /// that have a DEFAULT expression set via <see cref="DefaultValue"/>.
    /// </summary>
    public PropertyBuilder<TProp> ReadOnly() => this;

    /// <summary>
    /// Marks this column as the concurrency token for optimistic concurrency control.
    /// Only one column per profile may be marked as the concurrency token.
    /// Supported types: <c>int</c>, <c>long</c>, <c>Guid</c>.
    /// Generated UPDATE and DELETE SQL will include <c>AND "Column" = @Column</c> in the WHERE clause.
    /// For <c>int</c>/<c>long</c> tokens the UPDATE SET list also includes <c>"Column" = @Column + 1</c>
    /// so the token is bumped automatically. For <c>Guid</c> tokens the caller is responsible
    /// for setting a new value on the model before calling <c>UpdateAsync</c>.
    /// When 0 rows are affected an <c>AdoGenConcurrencyException</c> is thrown.
    /// </summary>
    public PropertyBuilder<TProp> ConcurrencyToken() => this;

    /// <summary>
    /// Sets a default SQL expression for the database column.
    /// </summary>
    public PropertyBuilder<TProp> DefaultValue(string sqlExpression) => this;
}

#pragma warning restore CA1720