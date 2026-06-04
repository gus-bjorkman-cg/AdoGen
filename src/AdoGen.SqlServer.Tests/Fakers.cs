using System.Text;
using AdoGen.Sample.Features.Audit;
using AdoGen.Sample.Features.TestTypes;
using AdoGen.Sample.Features.Users;
using Bogus;
using Bogus.Extensions;

namespace AdoGen.SqlServer.Tests;

public static class Fakers
{
    public static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(x => x.Id, Guid.CreateVersion7)
        .RuleFor(x => x.Name, y => y.Person.FullName.ClampLength(1, 20))
        .RuleFor(x => x.Email, y => y.Person.Email.ClampLength(1, 50))
        .WithDefaultConstructor();
    
    public static readonly Faker<AuditEvent> AuditEventFaker = new Faker<AuditEvent>()
        .StrictMode(true)
        .RuleFor(x => x.EventId, _ => 0)
        .RuleFor(x => x.CreatedAt, f => f.Date.RecentOffset().ToUniversalTime())
        .RuleFor(x => x.EventType, f => f.Lorem.Word().ClampLength(1, 50))
        .RuleFor(x => x.JsonPayload, f =>
        {
            var json = $"{{ \"data\": \"{f.Random.String2(1, 1500)}\" }}";
            return Encoding.UTF8.GetBytes(json);
        })
        .WithDefaultConstructor();
    
    private static DateTime RoundToSqlServerDateTime(DateTime value) => 
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);
    
    public static readonly Faker<TestType> TestTypeFaker = new Faker<TestType>()
        .StrictMode(true)
        .RuleFor(x => x.Int, _ => 0)
        .RuleFor(x => x.NullableInt, f => f.Random.Bool() ? f.Random.Int() : null)
        .RuleFor(x => x.Decimal, _ => 0)
        .RuleFor(x => x.NullableDecimal, f => f.Random.Bool() ? Math.Round(f.Random.Decimal(0m, 9.99999m), 2, MidpointRounding.AwayFromZero) : null)
        .RuleFor(x => x.NullableGuid, f => f.Random.Bool() ? f.Random.Guid() : null)
        .RuleFor(x => x.NullableStringVarchar, f => f.Random.Bool() ? f.Lorem.Text().ClampLength(1, 100) : null)
        .RuleFor(x => x.NullableStringNVarchar, f => f.Random.Bool() ? f.Lorem.Text().ClampLength(1, 100) : null)
        .RuleFor(x => x.StringVarcharRuledNull, f => f.Lorem.Text().ClampLength(1, 100))
        .RuleFor(x => x.CharString, f => f.Random.String2(10))
        .RuleFor(x => x.NCharString, f => f.Random.String2(15))
        .RuleFor(x => x.Float, f => f.Random.Float())
        .RuleFor(x => x.NullableFloat, f => f.Random.Bool() ? f.Random.Float() : null)
        .RuleFor(x => x.DateTime, f => RoundToSqlServerDateTime(f.Date.Recent()))
        .RuleFor(x => x.NullableDateTime, f => RoundToSqlServerDateTime(f.Date.Recent()))
        .RuleFor(x => x.Double, f => f.Random.Double())
        .RuleFor(x => x.NullableDouble, f => f.Random.Bool() ? f.Random.Double() : null)
        .RuleFor(x => x.Char, f => f.Random.Char('a', 'z'))
        .RuleFor(x => x.NChar, f => f.Random.Char('A', 'Z'))
        .RuleFor(x => x.NullableChar, f => f.Random.Bool() ? f.Random.Char('0', '9') : null)
        .RuleFor(x => x.NullableBytes, f => f.Random.Bool() ? f.Random.Bytes(f.Random.Int(0, 200)) : null)
        .RuleFor(x => x.Bytes, f => f.Random.Bytes(5))
        .RuleFor(x => x.Fruits, f => f.PickRandom<Fruits>())
        .RuleFor(x => x.Flags, f =>
        {
            var value = Flags.None;
            if (f.Random.Bool()) value |= Flags.Flag1;
            if (f.Random.Bool()) value |= Flags.Flag2;
            if (f.Random.Bool()) value |= Flags.Flag3;
            return value;
        })
        .RuleFor(x => x.ByteEnum, f => f.PickRandom<ByteEnum>())
        .RuleFor(x => x.ShortEnum, f => f.PickRandom<ShortEnum>())
        .RuleFor(x => x.IntEnum, f => f.PickRandom<IntEnum>())
        .RuleFor(x => x.LongEnum, f => f.PickRandom<LongEnum>())
        .RuleFor(x => x.CreatedAt, _ => default)
        .WithDefaultConstructor();
    
    private static Faker<T> WithDefaultConstructor<T>(this Faker<T> faker) where T : class =>
        faker.CustomInstantiator(_ =>
        {
            var constructor = typeof(T).GetConstructors()[0];
            return (T)constructor.Invoke(new object[constructor.GetParameters().Length]);
        });
}