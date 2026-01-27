using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace AdoGen.Benchmarks;

public sealed class CommonConfig : ManualConfig
{
    public CommonConfig()
    {
        AddJob(Job.Default);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddLogger(ConsoleLogger.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(new BenchTypeColum());
        Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
        Options |= ConfigOptions.JoinSummary;
    }
    
    private sealed class BenchTypeColum : IColumn
    {
        public string Id => nameof(BenchTypeColum);
        public string ColumnName => "BenchType";
        public string Legend => "The tested bench type";
        public bool IsAvailable(Summary _) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => -10;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public bool IsDefault(Summary _, BenchmarkCase __) => false;
        
        public string GetValue(Summary _, BenchmarkCase benchmarkCase) => 
            benchmarkCase.Descriptor.WorkloadMethod.DeclaringType!.Name.Replace("Benchmarks", "");
        
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle _) => 
            GetValue(summary, benchmarkCase);

        public override string ToString() => ColumnName;
    }
}