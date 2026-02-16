using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory(nameof(Insert))]
public class Insert : TestBase
{
    private User _user = null!;
    private UserModel _userModel = null!;
    private SqlTransaction _transaction = null!;

    protected override async ValueTask Initialize()
    {
        _user = UserFaker.Generate();
        _userModel = new UserModel(_user.Id, _user.Name, _user.Email);
        _transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }
    
    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");
    
    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");
    
    [Benchmark]
    public async Task AdoGen() => await Connection.InsertAsync(_user, CancellationToken, _transaction);

    public const string SqlInsert = "INSERT INTO [dbo].[Users] ([Id], [Name], [Email]) VALUES (@Id, @Name, @Email);";
    
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
            commandText: SqlInsert,
            commandType: CommandType.Text, 
            parameters: parameters,
            transaction: _transaction,
            cancellationToken: CancellationToken);
        
        await Connection.ExecuteAsync(command);
    }
    
    [Benchmark]
    public async Task DapperNoType() => await Connection.ExecuteAsync(SqlInsert, _user, _transaction);

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.Add(_userModel);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}