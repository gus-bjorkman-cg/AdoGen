using System.Data;

namespace AdoGen.Sample.Features.TestTypes;

// Created to cover remaining types for test
public enum Fruits
{
    Apple,
    Banana,
    Orange
}

[Flags]
public enum Flags
{
    None = 0,
    Flag1 = 1,
    Flag2 = 2,
    Flag3 = 4
}

public enum ByteEnum : byte
{
    Option1 = 1,
    Option2 = 2,
    Option3 = 3
}

public enum ShortEnum : short
{
    Value1 = 1,
    Value2 = 2,
    Value3 = 3
}

public enum IntEnum : int
{
    Item1 = 1,
    Item2 = 2,
    Item3 = 3
}

public enum LongEnum : long
{
    ValueA = 1,
    ValueB = 2,
    ValueC = 3
}

public sealed partial record TestType(
    int Int,
    int? NullableInt,
    decimal Decimal,
    decimal? NullableDecimal,
    Guid? NullableGuid,
    string? NullableStringVarchar,
    string? NullableStringNVarchar,
    string StringVarcharRuledNull,
    string CharString,
    string NCharString,
    float Float,
    float? NullableFloat,
    DateTime DateTime,
    DateTime NullableDateTime,
    double Double,
    double? NullableDouble,
    char Char,
    char NChar,
    char? NullableChar,
    byte[]? NullableBytes,
    byte[] Bytes,
    Fruits Fruits,
    Flags Flags,
    ByteEnum ByteEnum,
    ShortEnum ShortEnum,
    IntEnum IntEnum,
    LongEnum LongEnum
    ) : ISqlBulkModel, INpgsqlBulkModel;

public sealed class TestTypeProfile : SqlProfile<TestType>
{
    public TestTypeProfile()
    {
        Key(x => x.Int);
        Key(x => x.Decimal);
        
        RuleFor(x => x.NullableStringVarchar).VarChar(100);
        RuleFor(x => x.NullableStringNVarchar).NVarChar(100);
        RuleFor(x => x.StringVarcharRuledNull).VarChar(100).Nullable();
        RuleFor(x => x.Char).Char(1);
        RuleFor(x => x.NChar).NChar(1);
        RuleFor(x => x.NullableChar).NChar(1).Nullable();
        RuleFor(x => x.NullableBytes).VarBinary(200).Nullable();
        RuleFor(x => x.Bytes).VarBinary(200);
        RuleFor(x => x.Decimal).Decimal(4,2);
        RuleFor(x => x.NullableDecimal).Decimal(6, 3).Nullable();
        RuleFor(x => x.CharString).Char(10);
        RuleFor(x => x.NCharString).NChar(15);
        RuleFor(x => x.NullableDateTime).Type(SqlDbType.DateTime).Nullable();
    }
}

public sealed class TestTypeNpgsqlProfile : NpgsqlProfile<TestType>
{
    public TestTypeNpgsqlProfile()
    {
        Key(x => x.Int);
        Key(x => x.Decimal);

        RuleFor(x => x.NullableStringVarchar).Varchar(100);
        RuleFor(x => x.NullableStringNVarchar).Varchar(100);
        RuleFor(x => x.StringVarcharRuledNull).Varchar(100).Nullable();
        RuleFor(x => x.Char).Char(1);
        RuleFor(x => x.NChar).Char(1);
        RuleFor(x => x.NullableChar).Char(1).Nullable();
        RuleFor(x => x.NullableBytes).Bytea().Nullable();
        RuleFor(x => x.Bytes).Bytea();
        RuleFor(x => x.Decimal).Decimal(4, 2);
        RuleFor(x => x.NullableDecimal).Decimal(6, 3).Nullable();
        RuleFor(x => x.CharString).Char(10);
        RuleFor(x => x.NCharString).Char(15);
        RuleFor(x => x.NullableDateTime).Type(NpgsqlDbType.Timestamp).Nullable();
    }
}