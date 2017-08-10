using System;
using BenchmarkDotNet.Attributes;

namespace Playground
{
    [MemoryDiagnoser]
    public class DelegateVsInterfaceStruct
    {
        [Params(10, 100, 1000)] public int ArraySize;

        [Benchmark]
        public object ApplyDelegate()
        {
            Func<int, int> inc = n => n + 1;

            var nums = new int[ArraySize];
            for (var i = 0; i < nums.Length; i++)
                nums[i] = inc(i);

            return nums;
        }

        [Benchmark(Baseline = true)]
        public object ApplyStruct()
        {
            var inc = new Inc();

            var nums = new int[ArraySize];
            for (var i = 0; i < nums.Length; i++)
                nums[i] = inc.Apply(i);

            return nums;
        }
    }

    public interface IApply<in TIn, out TOut>
    {
        TOut Apply(TIn value);
    }

    public struct Inc : IApply<int, int>
    {
        public int Apply(int value) => value + 1;
    }
}
