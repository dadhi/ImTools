using BenchmarkDotNet.Running;

namespace Playground
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<ObjectPoolComparison_RentReturnAndRentPrefilledPool>();
            //BenchmarkRunner.Run<ObjectPoolComparison_RentPrefilledPool>();

            //BenchmarkRunner.Run<ObjectPoolComparison>();
            //BenchmarkRunner.Run<DelegateVsInterfaceStruct.MapArray>();
            //BenchmarkRunner.Run<DelegateVsInterfaceStruct.MapEnumerableRange>();
            //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();
        }
    }
}
