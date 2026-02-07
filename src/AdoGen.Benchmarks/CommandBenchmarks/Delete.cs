using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;

namespace AdoGen.Benchmarks.CommandBenchmarks;

public class Delete : TestBase
{
    private User _user = null!;
    private UserModel _userModel = null!;
    
    protected override void IterationSetup()
    {
        _user = UserFaker.Generate();
        _userModel = new UserModel(_user.Id, _user.Name, _user.Email);
        Connection.Execute(Insert.SqlInsert, _user);
    }
    
    [Benchmark]
    public async Task AdoGen() => await Connection.DeleteAsync(_user, CancellationToken);
    
    private const string SqlDelete = "DELETE FROM [dbo].[Users] WHERE [Id] = @Id;";
    
    [Benchmark]
    public async Task Dapper()
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", _user.Id, DbType.Guid);
        var command = new CommandDefinition(
            commandText: SqlDelete,
            commandType: CommandType.Text, 
            parameters: parameters,
            cancellationToken: CancellationToken);
        
        await Connection.ExecuteAsync(command);
    }

    [Benchmark]
    public async Task DapperNoType() =>
        await Connection.ExecuteAsync(SqlDelete, new { _user.Id });
    
    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.Remove(_userModel);
        await DbContext.SaveChangesAsync(CancellationToken);
    }
}