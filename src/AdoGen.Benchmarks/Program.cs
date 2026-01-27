using BenchmarkDotNet.Running;
using AdoGen.Benchmarks;

new BenchmarkSwitcher(typeof(IAssemblyMarker).Assembly).Run(args, new CommonConfig());