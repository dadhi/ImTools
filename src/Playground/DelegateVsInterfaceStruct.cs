using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Playground
{
    public interface IApply<in TIn, out TOut>
    {
        TOut Apply(TIn value);
    }

    public class DelegateVsInterfaceStruct
    {
        [MemoryDiagnoser]
        public class MapEnumerableRange
        {
            [Params(10, 100, 1000)] public int ItemCount;

            [Benchmark]
            public int ApplyDelegate()
            {
                return Enumerable.Range(0, ItemCount).Select(n => n + 1).Sum();
            }

            public struct Inc : IApply<int, int>
            {
                public int Apply(int value) => value + 1;
            }

            [Benchmark(Baseline = true)]
            public int ApplyStruct()
            {
                return Enumerable.Range(0, ItemCount).Map<int, int, Inc>(new Inc()).Sum();
            }

            [Benchmark]
            public int ApplyLocalFunction()
            {
                int Inc(int n) => n + 1;
                return Enumerable.Range(0, ItemCount).Select(Inc).Sum();
            }
        }

        [MemoryDiagnoser]
        public class MapArray
        {
            [Params(10, 100)] public int ItemCount;

            [Benchmark]
            public object ApplyDelegate()
            {
                Func<int, int> inc = n => n + 1;

                var nums = new int[ItemCount];
                for (var i = 0; i < nums.Length; i++)
                    nums[i] = inc(i);

                return nums;
            }

            public struct Inc : IApply<int, int>
            {
                public int Apply(int value) => value + 1;
            }

            [Benchmark(Baseline = true)]
            public object ApplyStruct()
            {
                var inc = new Inc();

                var nums = new int[ItemCount];
                for (var i = 0; i < nums.Length; i++)
                    nums[i] = inc.Apply(i);
                return nums;
            }

            [Benchmark] // Artifical test cause it is impossible to pass local function to other method as delegate
            public object ApplyLocalFunction()
            {
                int Inc(int n) => n + 1;
                var nums = new int[ItemCount];
                for (var i = 0; i < nums.Length; i++)
                    nums[i] = Inc(i);

                return nums;
            }
        }
    }

    public static class EnumerableExt
    {
        public static IEnumerable<R> Map<T, R, TMap>(this IEnumerable<T> source, TMap map)
            where TMap : struct, IApply<T, R>
        {
            foreach (var item in source)
                yield return map.Apply(item);
        }
    }
}
