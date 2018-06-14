using BenchmarkDotNet.Running;

namespace Playground
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<ObjectPoolComparison_AccessPrefilledPools>();

            //BenchmarkRunner.Run<ObjectPoolComparison>();
            //BenchmarkRunner.Run<DelegateVsInterfaceStruct.MapArray>();
            //BenchmarkRunner.Run<DelegateVsInterfaceStruct.MapEnumerableRange>();
            //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();
        }
    }
}
