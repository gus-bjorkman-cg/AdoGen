using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory("Update")]
public class Update : TestBase
{
    private User _user = null!;
    private UserModel _userModel = null!;
    
    protected override async ValueTask Initialize()
    {
        _user = (await Connection.QueryFirstOrDefaultAsync<User>(SqlGetOne, UserSql.CreateParameterName("0"), CancellationToken))!;
        _userModel = new UserModel(_user.Id, _user.Name, _user.Email);
    }

    protected override void IterationCleanup()
    {
        Index++;
        _user = _user with { Name = Index.ToString(), Email = Index.ToString() };
        _userModel = _userModel with { Name = Index.ToString(), Email =  Index.ToString() };
    }
    
    [Benchmark]
    public async Task AdoGen() => await Connection.UpdateAsync(_user, CancellationToken);
    
    private const string SqlUpdate = "UPDATE [dbo].[Users] SET [Name] = @Name, [Email] = @Email WHERE [Id] = @Id;";
        
    [Benchmark]
    public async Task Dapper()
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", _user.Id, DbType.Guid);
        parameters.Add("Name", new DbString
        {
            IsAnsi = true,
            Length = 20,
            Value = _user.Name
        });
        parameters.Add("Email", new DbString
        {
            IsAnsi = true,
            Length = 50,
            Value = _user.Email
        });
        
        var command = new CommandDefinition(
            commandText: SqlUpdate,
            parameters: parameters,
            cancellationToken: CancellationToken);
        
        await Connection.ExecuteAsync(command);
    }
    
    [Benchmark]
    public async Task DapperNoType() => await Connection.ExecuteAsync(SqlUpdate, _user);

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.Update(_userModel);
        await DbContext.SaveChangesAsync(CancellationToken);
    }
}