using System;
using System.Data;
using System.Linq.Expressions;

namespace AdoGen.Abstractions;

/// <summary>
/// The generator inspects the constructor body and reads calls to RuleFor(...).
/// It should be used to configure db types and properties.
/// </summary>
public abstract class SqlProfile<T>
{
    /// <summary>
    /// Entry-point for configuring a property.
    /// The generator parses the fluent calls that follow.
    /// </summary>
    protected PropertyBuilder<TProp> RuleFor<TProp>(Expression<Func<T, TProp>> selector) => new(selector);
    
    /// <summary>
    /// Allows a custom table name. Default is class name pluralized.
    /// </summary>
    /// <param name="name">Your table name</param>
    /// <returns></returns>
    protected SqlProfile<T> Table(string name) => this;
    
    /// <summary>
    /// Allows a custom schema. Default is dbo.
    /// </summary>
    /// <param name="name">Your schema name</param>
    /// <returns></returns>
    protected SqlProfile<T> Schema(string name) => this;
    
    /// <summary>
    /// Allows a custom id field. Default is property named Id.
    /// </summary>
    /// <param name="selector"></param>
    /// <returns></returns>
    protected SqlProfile<T> Key<TProp>(Expression<Func<T, TProp>> selector) => this;
    
    /// <summary>
    /// Configuration of identity fields.
    /// </summary>
    /// <param name="selector"></param>
    /// <typeparam name="TProp"></typeparam>
    /// <returns></returns>
    protected SqlProfile<T> Identity<TProp>(Expression<Func<T, TProp>> selector) => this;
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
    /// Configures the Db type.
    /// </summary>
    /// <param name="dbType"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> Type(SqlDbType dbType) => this;
    
    /// <summary>
    /// Configures the string size.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> Size(int size) => this;
    
    /// <summary>
    /// Configures the decimal precision.
    /// </summary>
    /// <param name="precision"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> Precision(int precision) => this;
    
    /// <summary>
    /// Configures the decimal scale.
    /// </summary>
    /// <param name="scale"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> Scale(int scale) => this;

    /// <summary>
    /// Configures the db column name. Default is property name.
    /// </summary>
    /// <param name="parameterName"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> Name(string parameterName) => this;
    
    /// <summary>
    /// Shorthand config for setting db type as Decimal with its scale and precision.
    /// </summary>
    /// <param name="scale"></param>
    /// <param name="precision"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> Decimal(int scale, int precision) => 
        Type(SqlDbType.Decimal).Scale(scale).Precision(precision);
    
    /// <summary>
    /// Shorthand config for setting db type as NVarChar with its size.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> NVarChar(int size) => Type(SqlDbType.NVarChar).Size(size);
    
    /// <summary>
    /// Shorthand config for setting db type as VarChar with its size.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> VarChar(int size) => Type(SqlDbType.VarChar).Size(size);
    
    /// <summary>
    /// Shorthand config for setting db type as NChar with its size.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> NChar(int size) => Type(SqlDbType.NChar).Size(size);
    
    /// <summary>
    /// Shorthand config for setting db type as Char with its size.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> Char(int size) => Type(SqlDbType.Char).Size(size);
    
    /// <summary>
    /// Shorthand config for setting db type as VarBinary with its size.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> VarBinary(int size) => Type(SqlDbType.VarBinary).Size(size);
    
    /// <summary>
    /// Configures the db type to be nullable.
    /// </summary>
    /// <returns></returns>
    public PropertyBuilder<TProp> Nullable() => this;
    
    /// <summary>
    /// Configures the db type to be not nullable.
    /// </summary>
    /// <returns></returns>
    public PropertyBuilder<TProp> NotNull() => this;
    
    /// <summary>
    /// Sets default value.
    /// </summary>
    /// <param name="sqlExpression"></param>
    /// <returns></returns>
    public PropertyBuilder<TProp> DefaultValue(string sqlExpression) => this;
}