using BenchmarkDotNet.Attributes;
using System;

namespace ImTools.Benchmarks
{
    [MemoryDiagnoser]
    public class ArrayCopy_vs_ManualIndexedSet_for_small_arrays
    {
/*

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


|            Method | ArrayLength |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|  ArrayCopyPlusOne |           1 | 13.492 ns | 0.3366 ns | 0.4376 ns |  1.00 |    0.00 | 0.0063 |     - |     - |      40 B |
| ManualCopyPlusOne |           1 |  7.353 ns | 0.2157 ns | 0.2952 ns |  0.54 |    0.02 | 0.0064 |     - |     - |      40 B |
|                   |             |           |           |           |       |         |        |       |       |           |
|  ArrayCopyPlusOne |           2 | 14.045 ns | 0.3565 ns | 0.5654 ns |  1.00 |    0.00 | 0.0076 |     - |     - |      48 B |
| ManualCopyPlusOne |           2 |  8.627 ns | 0.2425 ns | 0.4247 ns |  0.61 |    0.04 | 0.0076 |     - |     - |      48 B |
|                   |             |           |           |           |       |         |        |       |       |           |
|  ArrayCopyPlusOne |           5 | 16.879 ns | 0.4073 ns | 0.3810 ns |  1.00 |    0.00 | 0.0115 |     - |     - |      72 B |
| ManualCopyPlusOne |           5 | 16.332 ns | 0.3997 ns | 0.7604 ns |  0.94 |    0.03 | 0.0115 |     - |     - |      72 B |
|                   |             |           |           |           |       |         |        |       |       |           |
|  ArrayCopyPlusOne |          10 | 18.823 ns | 0.4502 ns | 0.6874 ns |  1.00 |    0.00 | 0.0179 |     - |     - |     112 B |
| ManualCopyPlusOne |          10 | 27.741 ns | 0.6268 ns | 0.9187 ns |  1.47 |    0.07 | 0.0178 |     - |     - |     112 B |
|                   |             |           |           |           |       |         |        |       |       |           |
|  ArrayCopyPlusOne |          20 | 23.168 ns | 0.5369 ns | 0.8036 ns |  1.00 |    0.00 | 0.0306 |     - |     - |     192 B |
| ManualCopyPlusOne |          20 | 43.250 ns | 0.9192 ns | 1.1952 ns |  1.87 |    0.08 | 0.0306 |     - |     - |     192 B |

## with Hybrid

|            Method | ArrayLength |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|  ArrayCopyPlusOne |           2 | 14.31 ns | 0.285 ns | 0.238 ns |  1.00 |    0.00 | 0.0076 |     - |     - |      48 B |
| ManualCopyPlusOne |           2 | 10.30 ns | 0.283 ns | 0.337 ns |  0.71 |    0.03 | 0.0076 |     - |     - |      48 B |
|            Hybrid |           2 | 10.29 ns | 0.234 ns | 0.208 ns |  0.72 |    0.02 | 0.0076 |     - |     - |      48 B |
|                   |             |          |          |          |       |         |        |       |       |           |
|  ArrayCopyPlusOne |           5 | 17.00 ns | 0.414 ns | 0.644 ns |  1.00 |    0.00 | 0.0115 |     - |     - |      72 B |
| ManualCopyPlusOne |           5 | 16.38 ns | 0.384 ns | 0.395 ns |  0.97 |    0.04 | 0.0115 |     - |     - |      72 B |
|            Hybrid |           5 | 17.33 ns | 0.421 ns | 0.352 ns |  1.02 |    0.05 | 0.0115 |     - |     - |      72 B |
|                   |             |          |          |          |       |         |        |       |       |           |
|  ArrayCopyPlusOne |          10 | 21.14 ns | 0.494 ns | 1.135 ns |  1.00 |    0.00 | 0.0179 |     - |     - |     112 B |
| ManualCopyPlusOne |          10 | 34.24 ns | 0.713 ns | 1.564 ns |  1.62 |    0.09 | 0.0178 |     - |     - |     112 B |
|            Hybrid |          10 | 23.07 ns | 0.544 ns | 0.966 ns |  1.08 |    0.09 | 0.0179 |     - |     - |     112 B |

*/

        // [Params(1, 2, 5, 10, 20)]
        [Params(2, 5, 10)]
        public int ArrayLength;

        private string[] _arr;

        [GlobalSetup]
        public void Setup()
        {
            _arr = new string[ArrayLength];
            for (int i = 0; i < _arr.Length; i++)
                _arr[i] = "a";
        }

        [Benchmark(Baseline = true)]
        public string[] ArrayCopyPlusOne()
        {
            var source = _arr;
            var count = source.Length;
            var copy = new string[count + 1];
            Array.Copy(source, 0, copy, 0, count);
            copy[count] = "b";
            return copy;
        }

        [Benchmark]
        public string[] ManualCopyPlusOne()
        {
            var source = _arr;
            var count = source.Length;
            var copy = new string[count + 1];
            for (var i = 0; i < count; ++i)
                copy[i] = source[i];
            copy[count] = "b";
            return copy;
        }

        [Benchmark]
        public string[] Hybrid()
        {
            var source = _arr;
            var count = source.Length;
            var copy = new string[count + 1];
            if (count < 5)
                for (var i = 0; i < count; ++i)
                    copy[i] = source[i];
            else
                Array.Copy(source, 0, copy, 0, count);
            copy[count] = "b";
            return copy;
        }

    }
}