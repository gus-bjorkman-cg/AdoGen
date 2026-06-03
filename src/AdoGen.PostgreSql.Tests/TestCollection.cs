namespace AdoGen.PostgreSql.Tests;

#pragma warning disable CA1711 // couldn't find a better name

[CollectionDefinition(Name)]
public sealed class TestCollection : ICollectionFixture<TestContext>
{
    public const string Name = "Test collection";
}

#pragma warning restore CA1711