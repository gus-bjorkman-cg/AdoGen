using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory(nameof(InsertMulti))]
public class InsertMulti : TestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;
    private readonly UserBulk _bulk = new();
    private SqlTransaction _transaction = null!;

    protected override async ValueTask Initialize()
    {
        _users = UserFaker.Generate(10);
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
        _transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }
  
    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");
    
    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");
    
    [Benchmark]
    public async Task AdoGen() => await Connection.InsertAsync(_users, CancellationToken, _transaction);

    [Benchmark]
    public async Task AdoGenBulk()
    {
        _bulk.AddRange(_users);
        await _bulk.SaveChangesAsync(Connection, _transaction, CancellationToken);
        _bulk.Clear();
    }

    private const string SqlInsert = "INSERT INTO [dbo].[Users] ([Id], [Name], [Email]) VALUES (@Id, @Name, @Email);";
    
    [Benchmark]
    public async Task Dapper()
    {
        var parameters = new List<DynamicParameters>();
        foreach (var user in _users)
        {
            var param = new DynamicParameters();
            param.Add("Id", user.Id, DbType.Guid);
            param.Add("Name", new DbString
            {
                IsAnsi = true,
                Length = 20,
                Value = user.Name
            });
            param.Add("Email", new DbString
            {
                IsAnsi = true,
                Length = 50,
                Value = user.Email
            });
            parameters.Add(param);
        }
        
        var command = new CommandDefinition(
            commandText: SqlInsert,
            commandType: CommandType.Text, 
            parameters: parameters,
            transaction: _transaction,
            cancellationToken: CancellationToken);
        
        await Connection.ExecuteAsync(command);
    }
    
    [Benchmark]
    public async Task DapperNoType() => await Connection.ExecuteAsync(SqlInsert, _users, _transaction);

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.AddRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}