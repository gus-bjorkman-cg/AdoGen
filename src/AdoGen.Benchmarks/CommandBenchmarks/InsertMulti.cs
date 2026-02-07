using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory("InsertMulti")]
public class InsertMulti : TestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;

    protected override void IterationSetup()
    {
        _users = UserFaker.Generate(10);
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
    }
  
    [Benchmark]
    public async Task AdoGen() => await Connection.InsertAsync(_users, CancellationToken);

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
            cancellationToken: CancellationToken);
        
        await Connection.ExecuteAsync(command);
    }
    
    [Benchmark]
    public async Task DapperNoType() => await Connection.ExecuteAsync(SqlInsert, _users);

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.AddRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
    }
}