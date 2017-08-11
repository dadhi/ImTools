using BenchmarkDotNet.Running;

namespace Playground
{
    class Program
    {
        static void Main()
        {
            //BenchmarkRunner.Run<DelegateVsInterfaceStruct.MapEnumerableRange>();
            //BenchmarkRunner.Run<HashVsImHashMap.Populate>();

            //var bm = new HashVsImHashMap.GetOrDefault() { ItemCount = 33 };
            //bm.GlobalSetup();
            //bm.GetFromTypeHashCache();

            BenchmarkRunner.Run<HashVsImHashMap.GetOrDefault>();

            //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();
        }
    }
}
