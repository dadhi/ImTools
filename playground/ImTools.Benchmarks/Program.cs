using BenchmarkDotNet.Running;

namespace ImTools.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        // With args: full BenchmarkSwitcher (filters, jobs, etc.)
        //   dotnet run -c Release -f net10.0 -- --filter *SmallGrrVsSmallList*
        // Without args: default baseline benchmark for the hybrid list work.
        // if (args is { Length: > 0 })
        // {
        //     BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        //     return;
        // }

        // BenchmarkRunner.Run<MemoryOwnerVsHashMap>();
        // BenchmarkRunner.Run<ImHashMapEnumerateBM>();

        // new SIO.Program().Run();

        //var x = new ImMapBenchmarks.Populate { Count = 10 };
        //x.ImMapArray_AddOrUpdate();

        //var b = new ImMapBenchmarks.Populate { Count = 10 };
        //b.ImMap_FixedData4();

        // BenchmarkRunner.Run<ImMapBenchmarks.Populate>();
        // BenchmarkRunner.Run<ImMapBenchmarks.Lookup>();
        // BenchmarkRunner.Run<ImMapBenchmarks.LookupMissing>();
        // BenchmarkRunner.Run<ImMapBenchmarks.Enumerate>();

        // BenchmarkRunner.Run<SmallGrrVsSmallListBenchmarks>();

        // BenchmarkRunner.Run<Playground.ImHashMapBenchmarks.Populate>();
        BenchmarkRunner.Run<Playground.ImHashMapBenchmarks.Lookup>();


        // var bm = new Playground.ImHashMapBenchmarks.Lookup();
        // bm.Count = 1000;
        // bm.Populate();
        // var count = bm.FecHashMap_PopulateThenLookup_HalfMissed_HalfPresent();
        // Console.WriteLine(count);

        // BenchmarkRunner.Run<ImHashMapBenchmarks.Enumerate>();
        // BenchmarkRunner.Run<ImHashMapBenchmarks.ToArray>();
        // BenchmarkRunner.Run<ImHashMapBenchmarks.GetAndUpdate_vs_AddOrGetAndReplace>();

        // BenchmarkRunner.Run<ImHashMapBenchmarks_StringString.Populate>();
        // BenchmarkRunner.Run<ImHashMapBenchmarks_StringString.Lookup>();

        //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();

        // BenchmarkRunner.Run<ObjectPoolComparison_RentReturnAndRentPrefilledPool>();
        //BenchmarkRunner.Run<ObjectPoolComparison_RentPrefilledPool>();

        //BenchmarkRunner.Run<ObjectPoolComparison>();

        //BenchmarkRunner.Run<DelegateVsInterfaceStruct.MapArray>();
        //BenchmarkRunner.Run<DelegateVsInterfaceStruct.MapEnumerableRange>();
        // BenchmarkRunner.Run<DelegateVsInterfaceStruct.MapEnumerableRangeWithState>();

        //BenchmarkRunner.Run<CustomEqualityComparerBenchmarks>();

        // BenchmarkRunner.Run<ArrayCopy_vs_ManualIndexedSet_for_small_arrays.ArraysOrReferences>();
    }
}
