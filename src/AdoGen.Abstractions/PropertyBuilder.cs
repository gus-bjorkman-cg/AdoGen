using System;
using System.Data;
using System.Linq.Expressions;

namespace AdoGen.Abstractions;

/// <summary>
/// Base for constructor-configured SQL profiles (FluentValidation style).
/// The generator inspects the constructor body and reads calls to Configure(...).
/// </summary>
public abstract class SqlProfile<T>
{
    protected SqlProfile() { }

    /// <summary>
    /// Entry-point for configuring a property.
    /// The generator parses the fluent calls that follow.
    /// </summary>
    protected PropertyBuilder<TProp> RuleFor<TProp>(Expression<Func<T, TProp>> selector) => new(selector);
    
    protected SqlProfile<T> Table(string name) => this;
    protected SqlProfile<T> Schema(string name) => this;
    protected SqlProfile<T> Key<TProp>(Expression<Func<T, TProp>> selector) => this;
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

    public PropertyBuilder<TProp> Type(SqlDbType dbType) => this;
    public PropertyBuilder<TProp> Size(int size) => this;
    public PropertyBuilder<TProp> Precision(byte precision) => this;
    public PropertyBuilder<TProp> Scale(byte scale) => this;

    public PropertyBuilder<TProp> Name(string parameterName) => this;
    public PropertyBuilder<TProp> NVarChar(int size) => Type(SqlDbType.NVarChar).Size(size);
    public PropertyBuilder<TProp> VarChar(int size) => Type(SqlDbType.VarChar).Size(size);
    public PropertyBuilder<TProp> NChar(int size) => Type(SqlDbType.NChar).Size(size);
    public PropertyBuilder<TProp> Char(int size) => Type(SqlDbType.Char).Size(size);
    public PropertyBuilder<TProp> VarBinary(int size) => Type(SqlDbType.VarBinary).Size(size);
    public PropertyBuilder<TProp> Nullable()   => this;
    public PropertyBuilder<TProp> NotNull()    => this;
    public PropertyBuilder<TProp> Default(string sqlExpression) => this;
}