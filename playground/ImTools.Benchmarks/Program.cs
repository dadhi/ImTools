using BenchmarkDotNet.Running;
using Playground;

namespace ImTools.Benchmarks
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();
        }
    }
}
