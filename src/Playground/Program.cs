using BenchmarkDotNet.Running;

namespace Playground
{
    class Program
    {
        static void Main()
        {
            //BenchmarkRunner.Run<HashVsImHashMap.Populate>();
            BenchmarkRunner.Run<HashVsImHashMap.GetOrDefault>();

            //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();
        }
    }
}
