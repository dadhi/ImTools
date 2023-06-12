using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using ImTools;

namespace Playground
{
    public interface IApply<in TIn, out TOut>
    {
        TOut Apply(TIn value);
    }

    public struct Inc : IApply<int, int>
    {
        public int Apply(int value)
        {
            return value + 1;
        }
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

            [Benchmark(Baseline = true)]
            public int ApplyStruct()
            {
                return Enumerable.Range(0, ItemCount).Map<int, int, Inc>(new Inc()).Sum();
            }

            //[Benchmark]
            //public int ApplyLocalFunction()
            //{
            //    //int Inc(int n) => n + 1;
            //    return Enumerable.Range(0, ItemCount).Select(Inc).Sum();
            //}
        }


/*
BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19042.928/20H2/October2020Update)
Intel Core i5-8350U CPU 1.70GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=8.0.100-preview.4.23260.5
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

## Baseline 

|             Method | ItemCount |        Mean |     Error |    StdDev |      Median | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|------------------- |---------- |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
| LinqSelectWhereSum |        10 |    212.9 ns |   4.30 ns |   9.07 ns |    211.0 ns |  1.00 |    0.00 | 0.0787 |     248 B |        1.00 |
|    ImToolsMatchSum |        10 |    179.6 ns |   3.34 ns |   7.11 ns |    177.0 ns |  0.85 |    0.05 | 0.0432 |     136 B |        0.55 |
|                    |           |             |           |           |             |       |         |        |           |             |
| LinqSelectWhereSum |       100 |  1,103.5 ns |  21.81 ns |  33.30 ns |  1,098.2 ns |  1.00 |    0.00 | 0.0782 |     248 B |        1.00 |
|    ImToolsMatchSum |       100 |  1,325.6 ns |  46.77 ns | 137.91 ns |  1,350.9 ns |  1.25 |    0.06 | 0.0420 |     136 B |        0.55 |
|                    |           |             |           |           |             |       |         |        |           |             |
| LinqSelectWhereSum |      1000 | 10,396.9 ns | 207.33 ns | 357.63 ns | 10,268.7 ns |  1.00 |    0.00 | 0.0763 |     248 B |        1.00 |
|    ImToolsMatchSum |      1000 | 11,013.7 ns | 208.84 ns | 575.20 ns | 10,899.7 ns |  1.07 |    0.07 | 0.0305 |     136 B |        0.55 |
*/
        [MemoryDiagnoser]
        public class MapEnumerableRangeWithState
        {
            [Params(10, 100, 1000)] public int ItemCount;

            [Benchmark(Baseline = true)]
            public int LinqSelectWhereSum()
            {
                var s = 13;
                return Enumerable.Range(0, ItemCount).Where(n => (n & 1) == 0).Select(n => s + n).Sum();
            }

            [Benchmark]
            public int ImToolsMatchSum()
            {
                var s = 13;
                return Enumerable.Range(0, ItemCount).Match(s, (_, n) => (n & 1) == 0, (a, n) => a + n).Sum();
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
                public int Apply(int value)
                {
                    return value + 1;
                }
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

            //[Benchmark] // Artificial test cause it is impossible to pass local function to other method as delegate
            //public object ApplyLocalFunction()
            //{
            //    int Inc(int n) => n + 1;
            //    var nums = new int[ItemCount];
            //    for (var i = 0; i < nums.Length; i++)
            //        nums[i] = Inc(i);

            //    return nums;
            //}
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
