namespace AdoGen.Tests;

[CollectionDefinition(Name)]
public sealed class TestCollection : ICollectionFixture<TestContext>
{
    public const string Name = "Test collection";
}