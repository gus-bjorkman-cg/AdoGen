using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory(nameof(Delete))]
public class Delete : TestBase
{
    private User _user = null!;
    private UserModel _userModel = null!;
    private SqlTransaction _transaction = null!;

    protected override async ValueTask Initialize()
    {
        _user = Connection.QueryFirst<User>(SqlGetOne, new { Name = "250" });
        _userModel = new UserModel(_user.Id, _user.Name, _user.Email);
        _transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }

    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");
    
    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");
    
    [Benchmark]
    public async Task AdoGen() => await Connection.DeleteAsync(_user, CancellationToken, _transaction);
    
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
            transaction: _transaction,
            cancellationToken: CancellationToken);
        
        await Connection.ExecuteAsync(command);
    }

    [Benchmark]
    public async Task DapperNoType() =>
        await Connection.ExecuteAsync(SqlDelete, new { _user.Id }, _transaction);
    
    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.Remove(_userModel);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}