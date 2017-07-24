using BenchmarkDotNet.Running;

namespace Playground
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<TreeBenchmarks.TypeHashMapVsImHashMap>();

            //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();
        }
    }
}
