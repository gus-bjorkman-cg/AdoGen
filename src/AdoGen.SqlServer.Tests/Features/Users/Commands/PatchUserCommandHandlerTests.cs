using AdoGen.Sample.Features.Users;
using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.SqlServer.Tests.Features.Users.Commands;

public sealed class PatchUserCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly PatchUserCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task PatchUser_ShouldUpdateOnlySetColumns()
    {
        // Arrange
        var original = DefaultUsers[0];
        var patch = new UserPatch(original.Id).WithEmail("patched@example.com");

        // Act
        await _sut.SqlServer(new PatchUserSqlCommand(patch), CancellationToken);

        // Assert
        (await GetUser(original.Id)).Should().Be(original with { Email = "patched@example.com" });
    }

    [Fact]
    public async Task PatchUser_ShouldReturnZero_WhenNoColumnsSet()
    {
        // Arrange
        var patch = new UserPatch(DefaultUsers[0].Id);

        // Act
        var affected = await _sut.SqlServer(new PatchUserSqlCommand(patch), CancellationToken);

        // Assert
        affected.Should().Be(0);
    }

    [Fact]
    public async Task PatchUser_ShouldUpdateAllSetColumns_WhenBothColumnsPatched()
    {
        // Arrange
        var expected = DefaultUsers[0] with { Name = "UpdatedName", Email = "updated@example.com" };
        var patch = new UserPatch(expected.Id).WithEmail(expected.Email).WithName(expected.Name);

        // Act
        await _sut.SqlServer(new PatchUserSqlCommand(patch), CancellationToken);

        // Assert
        (await GetUser(expected.Id)).Should().Be(expected);
    }

    [Fact]
    public async Task PatchUser_ShouldPatchViaPublicPropertySetter_WhenDeserializedFromApi()
    {
        // Arrange
        var expected = DefaultUsers[0] with { Email = "api@example.com" };
        var patch = new UserPatch(expected.Id) { Email = expected.Email };

        // Act
        await _sut.SqlServer(new PatchUserSqlCommand(patch), CancellationToken);

        // Assert
        (await GetUser(expected.Id)).Should().Be(expected);
    }
}
