using BenchmarkDotNet.Running;

namespace ImTools.Benchmarks
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<HashVsImHashMap.GetOrDefault>();
        }
    }
}
