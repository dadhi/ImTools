using BenchmarkDotNet.Running;
using Playground;

namespace ImTools.Benchmarks
{
    class Program
    {
        static void Main()
        {
            //BenchmarkRunner.Run<ImMapBenchmarks.Populate>();
            BenchmarkRunner.Run<ImMapBenchmarks.Lookup>();

            //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();
        }
    }
}
