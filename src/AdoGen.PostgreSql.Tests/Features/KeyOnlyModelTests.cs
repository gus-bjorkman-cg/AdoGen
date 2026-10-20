using AdoGen.Sample.Features.UserRoles;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class KeyOnlyModelTests(TestContext testContext) : TestBase(testContext)
{
    private readonly UserRole _existing = new(Guid.CreateVersion7(), Guid.CreateVersion7());

    protected override async ValueTask InitializeAsync() => await Connection.InsertAsync(_existing, CancellationToken);

    protected override async ValueTask DisposeAsync() => await Connection.TruncateAsync<UserRole>(CancellationToken);

    [Fact]
    public async Task Update_ShouldReturnOne_WhenRowExists() =>
        (await Connection.UpdateAsync(_existing, CancellationToken)).Should().Be(1);

    [Fact]
    public async Task Update_ShouldReturnZero_WhenRowMissing() =>
        (await Connection.UpdateAsync(new UserRole(Guid.CreateVersion7(), Guid.CreateVersion7()), CancellationToken)).Should().Be(0);

    [Fact]
    public async Task Upsert_ShouldInsert_WhenRowMissing()
    {
        // Arrange
        var model = new UserRole(Guid.CreateVersion7(), Guid.CreateVersion7());

        // Act
        await Connection.UpsertAsync(model, CancellationToken);

        // Assert
        (await Connection.ExistsAsync(model, CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task Upsert_ShouldNotThrow_WhenRowExists()
    {
        // Act
        var act = async () => await Connection.UpsertAsync(_existing, CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
        (await Connection.ExistsAsync(_existing, CancellationToken)).Should().BeTrue();
    }
}
