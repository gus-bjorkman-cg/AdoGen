using Bogus;

namespace AdoGen.Tests;

public static class Extensions
{
    public static Faker<T> WithDefaultConstructor<T>(this Faker<T> faker) where T : class =>
        faker.CustomInstantiator(_ =>
        {
            var constructor = typeof(T).GetConstructors()[0];
            return (T)constructor.Invoke(new object[constructor.GetParameters().Length]);
        });
}