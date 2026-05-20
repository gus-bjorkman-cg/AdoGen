using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests.Features;

public sealed class PatchTests(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task Patch_ShouldUpdateOnlySetColumns()
    {
        // Arrange
        var original = DefaultUsers[0];
        var patch = new UserPatch(original.Id).WithEmail("patched@example.com");

        // Act
        await Connection.PatchAsync(patch, CancellationToken);

        // Assert
        (await GetUser(original.Id)).Should().Be(original with { Email = "patched@example.com" });
    }

    [Fact]
    public async Task Patch_ShouldReturnZero_WhenNoColumnsSet()
    {
        // Arrange
        var patch = new UserPatch(DefaultUsers[0].Id);

        // Act
        var affected = await Connection.PatchAsync(patch, CancellationToken);

        // Assert
        affected.Should().Be(0);
    }

    [Fact]
    public async Task Patch_ShouldUpdateAllSetColumns_WhenBothColumnsSet()
    {
        // Arrange
        var expected = DefaultUsers[0] with { Name = "UpdatedName", Email = "updated@example.com" };
        var patch = new UserPatch(expected.Id).WithName(expected.Name).WithEmail(expected.Email);

        // Act
        await Connection.PatchAsync(patch, CancellationToken);

        // Assert
        (await GetUser(expected.Id)).Should().Be(expected);
    }

    [Fact]
    public async Task Patch_ShouldNotAffectOtherColumns_WhenOnlyOneColumnSet()
    {
        // Arrange
        var original = DefaultUsers[0];
        var patch = new UserPatch(original.Id).WithName("OnlyNameChanged");

        // Act
        await Connection.PatchAsync(patch, CancellationToken);

        // Assert
        (await GetUser(original.Id)).Should().Be(original with { Name = "OnlyNameChanged" });
    }

    [Fact]
    public async Task Patch_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var patch = new UserPatch(DefaultUsers[0].Id).WithEmail("cancel@example.com");
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await Connection.PatchAsync(patch, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Patch_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        var patch = new UserPatch(DefaultUsers[0].Id).WithEmail("rolled-back@example.com");

        // Act
        await Connection.PatchAsync(patch, CancellationToken, transaction);
        transaction.Rollback();

        // Assert
        (await GetUser(DefaultUsers[0].Id)).Should().Be(DefaultUsers[0]);
    }

    [Fact]
    public async Task Patch_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        var patch = new UserPatch(DefaultUsers[0].Id).WithEmail("timeout@example.com");

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.PatchAsync(patch, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }
}
