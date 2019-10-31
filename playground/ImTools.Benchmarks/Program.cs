using BenchmarkDotNet.Running;
using Playground;

namespace ImTools.Benchmarks
{
    class Program
    {
        static void Main()
        {
            //var x = new ImMapBenchmarks.Populate { Count = 10 };
            //x.ImMapArray_AddOrUpdate();

            //BenchmarkRunner.Run<ImMapBenchmarks.Populate>();
            //BenchmarkRunner.Run<ImMapBenchmarks.Lookup>();

            //BenchmarkRunner.Run<ImHashMapBenchmarks.Populate>();
            BenchmarkRunner.Run<ImHashMapBenchmarks.Lookup>();

            //BenchmarkRunner.Run<ImHashMapBenchmarks_StringString.Populate>();
            //BenchmarkRunner.Run<ImHashMapBenchmarks_StringString.Lookup>();

            //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();
        }
    }
}
