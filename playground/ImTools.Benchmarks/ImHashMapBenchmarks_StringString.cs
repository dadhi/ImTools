using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ImTools;
using Microsoft.Collections.Extensions;

namespace Playground
{
    public class ImHashMapBenchmarks_StringString
    {
        [MemoryDiagnoser]
        public class Populate
        {
            /*
BenchmarkDotNet=v0.11.5, OS=Windows 10.0.17134.648 (1803/April2018Update/Redstone4)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=2156251 Hz, Resolution=463.7679 ns, Timer=TSC
.NET Core SDK=3.0.100-preview3-010431
  [Host]     : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT

## 3019-04-04 First Results

|                     Method |  Count |           Mean |         Error |        StdDev | Ratio | RatioSD |      Gen 0 |     Gen 1 |    Gen 2 |    Allocated |
|--------------------------- |------- |---------------:|--------------:|--------------:|------:|--------:|-----------:|----------:|---------:|-------------:|
|          ImMap_AddOrUpdate |     10 |       1.185 us |     0.0049 us |     0.0045 us |  1.00 |    0.00 |     0.7420 |         - |        - |      3.42 KB |
|       ImMap_V1_AddOrUpdate |     10 |       1.203 us |     0.0022 us |     0.0021 us |  1.01 |    0.00 |     0.7610 |         - |        - |      3.52 KB |
|  DictSlim_GetOrAddValueRef |     10 |       1.170 us |     0.0033 us |     0.0029 us |  0.99 |    0.00 |     0.5322 |         - |        - |      2.45 KB |
|                Dict_TryAdd |     10 |       1.048 us |     0.0162 us |     0.0152 us |  0.88 |    0.01 |     0.5150 |         - |        - |      2.38 KB |
|      ConcurrentDict_TryAdd |     10 |       1.529 us |     0.0085 us |     0.0075 us |  1.29 |    0.01 |     0.5817 |         - |        - |      2.69 KB |
|          ImmutableDict_Add |     10 |       5.764 us |     0.1096 us |     0.1076 us |  4.87 |    0.09 |     0.8087 |         - |        - |      3.73 KB |
|                            |        |                |               |               |       |         |            |           |          |              |
|          ImMap_AddOrUpdate |    100 |      19.693 us |     0.0921 us |     0.0862 us |  1.00 |    0.00 |    10.8337 |    0.0305 |        - |     50.02 KB |
|       ImMap_V1_AddOrUpdate |    100 |      20.991 us |     0.0365 us |     0.0341 us |  1.07 |    0.00 |    11.8713 |    0.0305 |        - |     54.84 KB |
|  DictSlim_GetOrAddValueRef |    100 |      11.067 us |     0.0980 us |     0.0869 us |  0.56 |    0.01 |     4.6234 |         - |        - |     21.38 KB |
|                Dict_TryAdd |    100 |      10.396 us |     0.1406 us |     0.1316 us |  0.53 |    0.01 |     5.2032 |         - |        - |     24.02 KB |
|      ConcurrentDict_TryAdd |    100 |      27.156 us |     0.4384 us |     0.4101 us |  1.38 |    0.02 |     9.6436 |    0.0610 |        - |     44.47 KB |
|          ImmutableDict_Add |    100 |     102.030 us |     2.1747 us |     2.7503 us |  5.20 |    0.16 |    12.4512 |         - |        - |     57.72 KB |
|                            |        |                |               |               |       |         |            |           |          |              |
|          ImMap_AddOrUpdate |   1000 |     366.492 us |     5.3528 us |     5.0070 us |  1.00 |    0.00 |   138.1836 |   33.2031 |        - |    661.45 KB |
|       ImMap_V1_AddOrUpdate |   1000 |     411.711 us |     3.3075 us |     2.9320 us |  1.12 |    0.01 |   127.9297 |   58.5938 |        - |    701.77 KB |
|  DictSlim_GetOrAddValueRef |   1000 |     135.818 us |     1.3496 us |     1.2624 us |  0.37 |    0.01 |    39.0625 |   15.3809 |        - |    204.11 KB |
|                Dict_TryAdd |   1000 |     114.091 us |     1.9093 us |     1.7860 us |  0.31 |    0.01 |    46.2646 |   15.5029 |        - |    247.48 KB |
|      ConcurrentDict_TryAdd |   1000 |     360.638 us |     6.3500 us |     5.9398 us |  0.98 |    0.02 |    77.1484 |   38.5742 |        - |    449.71 KB |
|          ImmutableDict_Add |   1000 |   1,578.688 us |    18.5404 us |    17.3427 us |  4.31 |    0.07 |   156.2500 |   60.5469 |        - |    791.91 KB |
|                            |        |                |               |               |       |         |            |           |          |              |
|          ImMap_AddOrUpdate |  10000 |  10,265.260 us |    92.1719 us |    86.2177 us |  1.00 |    0.00 |  1359.3750 |  359.3750 | 156.2500 |   8277.42 KB |
|       ImMap_V1_AddOrUpdate |  10000 |  11,061.120 us |   169.4658 us |   158.5185 us |  1.08 |    0.02 |  1421.8750 |  421.8750 | 187.5000 |   8700.75 KB |
|  DictSlim_GetOrAddValueRef |  10000 |   2,596.471 us |    10.3734 us |     9.1957 us |  0.25 |    0.00 |   398.4375 |  199.2188 | 199.2188 |   2450.55 KB |
|                Dict_TryAdd |  10000 |   2,380.701 us |    13.8106 us |    11.5325 us |  0.23 |    0.00 |   441.4063 |  218.7500 | 218.7500 |   2473.84 KB |
|      ConcurrentDict_TryAdd |  10000 |   7,215.799 us |   127.0207 us |   118.8153 us |  0.70 |    0.01 |   539.0625 |  296.8750 | 132.8125 |   3247.21 KB |
|          ImmutableDict_Add |  10000 |  25,002.045 us |   225.6550 us |   200.0373 us |  2.43 |    0.03 |  1656.2500 |  375.0000 | 125.0000 |  10112.72 KB |
|                            |        |                |               |               |       |         |            |           |          |              |
|          ImMap_AddOrUpdate | 100000 | 209,350.633 us | 2,393.2964 us | 2,238.6909 us |  1.00 |    0.00 | 17000.0000 | 4666.6667 | 666.6667 | 100333.97 KB |
|       ImMap_V1_AddOrUpdate | 100000 | 223,663.675 us | 2,464.5772 us | 2,184.7835 us |  1.07 |    0.02 | 17666.6667 | 4666.6667 | 666.6667 | 104491.82 KB |
|  DictSlim_GetOrAddValueRef | 100000 |  55,598.480 us | 1,101.7717 us | 2,122.7400 us |  0.27 |    0.01 |  3333.3333 | 1444.4444 | 666.6667 |  24191.45 KB |
|                Dict_TryAdd | 100000 |  54,494.452 us | 1,067.3681 us | 1,270.6258 us |  0.26 |    0.01 |  3375.0000 | 1562.5000 | 687.5000 |  25276.91 KB |
|      ConcurrentDict_TryAdd | 100000 | 100,291.074 us | 1,430.4632 us | 1,268.0684 us |  0.48 |    0.01 |  4600.0000 | 2000.0000 | 600.0000 |  27459.84 KB |
|          ImmutableDict_Add | 100000 | 375,614.567 us | 1,515.8877 us | 1,343.7950 us |  1.79 |    0.02 | 20000.0000 | 5000.0000 |        - | 123738.77 KB |

             */

            private const string KeySeed = "hubba-bubba";

            [Params(10, 100, 1_000, 10_000, 100_000)]
            public int Count;

            [Benchmark(Baseline = true)]
            public ImHashMap<string, string> ImMap_AddOrUpdate()
            {
                var map = ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + KeySeed;
                    map = map.AddOrUpdate(v + a, v, out _, out _);
                }

                return map;
            }

            [Benchmark]
            public V1.ImHashMap<string, string> AddOrUpdate_V1_AddOrUpdate()
            {
                var map = V1.ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + KeySeed;
                    map = map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            [Benchmark]
            public DictionarySlim<string, string> DictSlim_GetOrAddValueRef()
            {
                var map = new DictionarySlim<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + KeySeed;
                    map.GetOrAddValueRef(v + a) = v;
                }

                return map;
            }

            [Benchmark]
            public Dictionary<string, string> Dict_TryAdd()
            {
                var map = new Dictionary<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + KeySeed;
                    map.TryAdd(v + a, v);
                }

                return map;
            }

            [Benchmark]
            public ConcurrentDictionary<string, string> ConcurrentDict_TryAdd()
            {
                var map = new ConcurrentDictionary<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + KeySeed;
                    map.TryAdd(v + a, v);
                }

                return map;
            }

            [Benchmark]
            public ImmutableDictionary<string, string> ImmutableDict_Add()
            {
                var map = ImmutableDictionary<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + KeySeed;
                    map = map.Add(v + a, v);
                }

                return map;
            }
        }

        [MemoryDiagnoser]//[Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Lookup
        {
            /*
            ## Initial benchmark - Funny that TryFind X,Y,Z were the same method

|                           Method |  Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|               ImHaspMap_TryFindX |     10 |  16.09 ns | 0.0655 ns | 0.0612 ns |  0.91 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY |     10 |  17.03 ns | 0.0477 ns | 0.0447 ns |  0.97 |    0.01 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |     10 |  17.57 ns | 0.0232 ns | 0.0194 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                ImHaspMap_TryFind |     10 |  17.59 ns | 0.0816 ns | 0.0724 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     10 |  18.76 ns | 0.0376 ns | 0.0352 ns |  1.07 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ |     10 |  19.80 ns | 0.0228 ns | 0.0178 ns |  1.13 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     10 |  23.38 ns | 0.0618 ns | 0.0548 ns |  1.33 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     10 |  31.72 ns | 0.0987 ns | 0.0875 ns |  1.80 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     10 |  81.65 ns | 0.3221 ns | 0.2689 ns |  4.64 |    0.02 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|       DictionarySlim_TryGetValue |    100 |  19.84 ns | 0.0608 ns | 0.0539 ns |  0.91 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ |    100 |  19.88 ns | 0.0594 ns | 0.0556 ns |  0.92 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindX |    100 |  20.66 ns | 0.0485 ns | 0.0430 ns |  0.95 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |    100 |  20.69 ns | 0.0438 ns | 0.0410 ns |  0.95 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY |    100 |  20.81 ns | 0.0361 ns | 0.0337 ns |  0.96 |    0.00 |     - |     - |     - |         - |
|                ImHaspMap_TryFind |    100 |  21.73 ns | 0.0491 ns | 0.0435 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    100 |  24.62 ns | 0.0735 ns | 0.0652 ns |  1.13 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    100 |  32.69 ns | 0.0758 ns | 0.0709 ns |  1.50 |    0.00 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    100 |  86.47 ns | 0.1811 ns | 0.1606 ns |  3.98 |    0.01 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|       DictionarySlim_TryGetValue |   1000 |  20.26 ns | 0.0375 ns | 0.0333 ns |  0.68 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   1000 |  24.74 ns | 0.0460 ns | 0.0430 ns |  0.83 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |   1000 |  25.41 ns | 0.0690 ns | 0.0645 ns |  0.85 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindX |   1000 |  26.73 ns | 0.0480 ns | 0.0449 ns |  0.90 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY |   1000 |  28.70 ns | 0.1267 ns | 0.1185 ns |  0.97 |    0.00 |     - |     - |     - |         - |
|                ImHaspMap_TryFind |   1000 |  29.72 ns | 0.0559 ns | 0.0523 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ |   1000 |  30.70 ns | 0.1203 ns | 0.1067 ns |  1.03 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   1000 |  32.38 ns | 0.2283 ns | 0.2135 ns |  1.09 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   1000 | 101.36 ns | 0.3535 ns | 0.3133 ns |  3.41 |    0.01 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|       DictionarySlim_TryGetValue |  10000 |  21.64 ns | 0.0558 ns | 0.0494 ns |  0.58 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  10000 |  27.25 ns | 0.0832 ns | 0.0738 ns |  0.73 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |  10000 |  31.51 ns | 0.0831 ns | 0.0736 ns |  0.85 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY |  10000 |  32.51 ns | 0.0958 ns | 0.0896 ns |  0.87 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  10000 |  33.96 ns | 0.1216 ns | 0.1015 ns |  0.91 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ |  10000 |  34.32 ns | 0.0561 ns | 0.0525 ns |  0.92 |    0.00 |     - |     - |     - |         - |
|                ImHaspMap_TryFind |  10000 |  37.22 ns | 0.0728 ns | 0.0681 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindX |  10000 |  37.22 ns | 0.0950 ns | 0.0793 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  10000 | 112.43 ns | 0.2563 ns | 0.2398 ns |  3.02 |    0.01 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|       DictionarySlim_TryGetValue | 100000 |  22.84 ns | 0.0358 ns | 0.0317 ns |  0.56 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue | 100000 |  26.91 ns | 0.0399 ns | 0.0354 ns |  0.66 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue | 100000 |  35.83 ns | 0.2690 ns | 0.2516 ns |  0.88 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ | 100000 |  39.90 ns | 0.1379 ns | 0.1152 ns |  0.98 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindX | 100000 |  40.53 ns | 0.0403 ns | 0.0357 ns |  0.99 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind | 100000 |  40.60 ns | 0.0811 ns | 0.0758 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY | 100000 |  40.72 ns | 0.0467 ns | 0.0365 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                ImHaspMap_TryFind | 100000 |  40.79 ns | 0.0941 ns | 0.0880 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImmutableDict_TryGet | 100000 | 116.17 ns | 0.7942 ns | 0.7429 ns |  2.85 |    0.02 |     - |     - |     - |         - |

## OK,consider it.
    
|                           Method |  Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|               ImHaspMap_TryFindX |     10 |  15.67 ns | 0.0393 ns | 0.0348 ns |  0.92 |    0.00 |     - |     - |     - |         - |
|                ImHaspMap_TryFind |     10 |  17.01 ns | 0.0530 ns | 0.0470 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY |     10 |  17.05 ns | 0.0457 ns | 0.0427 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ |     10 |  17.97 ns | 0.1374 ns | 0.1218 ns |  1.06 |    0.01 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |     10 |  18.54 ns | 0.0197 ns | 0.0174 ns |  1.09 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     10 |  19.42 ns | 0.3872 ns | 0.3433 ns |  1.14 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     10 |  24.97 ns | 0.3384 ns | 0.3165 ns |  1.47 |    0.02 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     10 |  32.39 ns | 0.4863 ns | 0.4549 ns |  1.91 |    0.03 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     10 |  86.26 ns | 1.7338 ns | 2.0639 ns |  5.12 |    0.11 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|       DictionarySlim_TryGetValue |    100 |  19.71 ns | 0.0281 ns | 0.0249 ns |  0.94 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY |    100 |  19.98 ns | 0.2277 ns | 0.1901 ns |  0.95 |    0.01 |     - |     - |     - |         - |
|                ImHaspMap_TryFind |    100 |  20.99 ns | 0.1714 ns | 0.1603 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ |    100 |  21.05 ns | 0.1538 ns | 0.1438 ns |  1.00 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindX |    100 |  22.74 ns | 0.3622 ns | 0.3388 ns |  1.08 |    0.02 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |    100 |  23.01 ns | 0.1562 ns | 0.1219 ns |  1.10 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    100 |  24.41 ns | 0.0758 ns | 0.0672 ns |  1.16 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    100 |  34.31 ns | 0.7407 ns | 0.7926 ns |  1.63 |    0.04 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    100 |  87.34 ns | 1.7314 ns | 1.9244 ns |  4.17 |    0.11 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|       DictionarySlim_TryGetValue |   1000 |  23.47 ns | 0.2132 ns | 0.1994 ns |  0.76 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindX |   1000 |  23.95 ns | 0.2893 ns | 0.2706 ns |  0.78 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY |   1000 |  25.32 ns | 0.2389 ns | 0.2235 ns |  0.82 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ |   1000 |  27.44 ns | 0.2798 ns | 0.2618 ns |  0.89 |    0.01 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |   1000 |  27.94 ns | 0.2943 ns | 0.2753 ns |  0.90 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   1000 |  28.17 ns | 0.2319 ns | 0.2169 ns |  0.91 |    0.01 |     - |     - |     - |         - |
|                ImHaspMap_TryFind |   1000 |  30.86 ns | 0.1908 ns | 0.1691 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   1000 |  33.83 ns | 0.4256 ns | 0.3773 ns |  1.10 |    0.02 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   1000 |  99.31 ns | 0.7859 ns | 0.7352 ns |  3.22 |    0.03 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|       DictionarySlim_TryGetValue |  10000 |  22.28 ns | 0.3044 ns | 0.2542 ns |  0.65 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  10000 |  29.38 ns | 0.1755 ns | 0.1641 ns |  0.86 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindX |  10000 |  30.93 ns | 0.4314 ns | 0.3602 ns |  0.90 |    0.01 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |  10000 |  31.75 ns | 0.2392 ns | 0.2237 ns |  0.93 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY |  10000 |  32.59 ns | 0.1022 ns | 0.0906 ns |  0.95 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ |  10000 |  33.11 ns | 0.2774 ns | 0.2595 ns |  0.97 |    0.01 |     - |     - |     - |         - |
|                ImHaspMap_TryFind |  10000 |  34.25 ns | 0.2817 ns | 0.2635 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  10000 |  37.12 ns | 0.4695 ns | 0.4392 ns |  1.08 |    0.02 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  10000 | 120.67 ns | 1.2000 ns | 1.1225 ns |  3.52 |    0.05 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|       DictionarySlim_TryGetValue | 100000 |  22.98 ns | 0.0427 ns | 0.0357 ns |  0.56 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue | 100000 |  27.99 ns | 0.0424 ns | 0.0376 ns |  0.68 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindX | 100000 |  34.11 ns | 0.2344 ns | 0.1957 ns |  0.83 |    0.00 |     - |     - |     - |         - |
|               ImHaspMap_TryFindY | 100000 |  35.03 ns | 0.0333 ns | 0.0295 ns |  0.86 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue | 100000 |  35.18 ns | 0.0605 ns | 0.0536 ns |  0.86 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind | 100000 |  39.20 ns | 0.5263 ns | 0.4923 ns |  0.96 |    0.01 |     - |     - |     - |         - |
|               ImHaspMap_TryFindZ | 100000 |  39.86 ns | 0.0887 ns | 0.0830 ns |  0.97 |    0.00 |     - |     - |     - |         - |
|                ImHaspMap_TryFind | 100000 |  40.91 ns | 0.0452 ns | 0.0400 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImmutableDict_TryGet | 100000 | 121.47 ns | 0.3888 ns | 0.3637 ns |  2.97 |    0.01 |     - |     - |     - |         - |

## Strange one - will try to downgrade BDN version 

|                           Method |  Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                ImHaspMap_TryFind |     10 |  18.21 ns | 0.0806 ns | 0.0754 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |     10 |  18.75 ns | 0.0435 ns | 0.0407 ns |  1.03 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     10 |  19.06 ns | 0.1218 ns | 0.1139 ns |  1.05 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     10 |  24.13 ns | 0.3288 ns | 0.2915 ns |  1.33 |    0.02 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     10 |  31.39 ns | 0.0693 ns | 0.0579 ns |  1.72 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     10 |  83.98 ns | 1.3823 ns | 1.2930 ns |  4.61 |    0.08 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|                ImHaspMap_TryFind |    100 |  23.07 ns | 0.0817 ns | 0.0764 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |    100 |  22.69 ns | 0.1755 ns | 0.1641 ns |  0.98 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |    100 |  20.20 ns | 0.1171 ns | 0.1038 ns |  0.88 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    100 |  25.04 ns | 0.2289 ns | 0.1911 ns |  1.08 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    100 |  32.95 ns | 0.1022 ns | 0.0906 ns |  1.43 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    100 |  92.62 ns | 1.8500 ns | 1.7305 ns |  4.02 |    0.08 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|                ImHaspMap_TryFind |   1000 |  24.93 ns | 0.2344 ns | 0.2193 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |   1000 |  27.60 ns | 0.2613 ns | 0.2444 ns |  1.11 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |   1000 |  22.20 ns | 0.1654 ns | 0.1547 ns |  0.89 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   1000 |  27.30 ns | 0.2340 ns | 0.2189 ns |  1.09 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   1000 |  35.28 ns | 0.6075 ns | 0.5385 ns |  1.41 |    0.03 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   1000 | 103.06 ns | 0.9440 ns | 0.8830 ns |  4.13 |    0.06 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|                ImHaspMap_TryFind |  10000 |  33.18 ns | 0.2550 ns | 0.2385 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind |  10000 |  35.04 ns | 0.2980 ns | 0.2642 ns |  1.06 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |  10000 |  22.21 ns | 0.0746 ns | 0.0583 ns |  0.67 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  10000 |  26.13 ns | 0.0489 ns | 0.0408 ns |  0.79 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  10000 |  34.21 ns | 0.1274 ns | 0.1191 ns |  1.03 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  10000 | 112.14 ns | 0.1953 ns | 0.1525 ns |  3.38 |    0.02 |     - |     - |     - |         - |
|                                  |        |           |           |           |       |         |       |       |       |           |
|                ImHaspMap_TryFind | 100000 |  38.63 ns | 0.1290 ns | 0.1007 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHaspMap_V1_TryFind | 100000 |  40.14 ns | 0.0933 ns | 0.0873 ns |  1.04 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue | 100000 |  24.35 ns | 0.3416 ns | 0.3196 ns |  0.63 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue | 100000 |  28.73 ns | 0.5372 ns | 0.4763 ns |  0.74 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue | 100000 |  35.36 ns | 0.1029 ns | 0.0912 ns |  0.92 |    0.00 |     - |     - |     - |         - |
|             ImmutableDict_TryGet | 100000 | 121.54 ns | 0.3989 ns | 0.3731 ns |  3.15 |    0.01 |     - |     - |     - |         - |

## Adding some variants

|                     Method | Count |     Mean |     Error |    StdDev |   Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------- |------ |---------:|----------:|----------:|---------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|          ImHaspMap_TryFind |    10 | 17.68 ns | 0.0915 ns | 0.0856 ns | 17.67 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|         ImHaspMap_TryFind2 |    10 | 20.51 ns | 0.0209 ns | 0.0186 ns | 20.51 ns |  1.16 |    0.01 |           - |           - |           - |                   - |
|         ImHaspMap_TryFind3 |    10 | 14.35 ns | 0.0466 ns | 0.0436 ns | 14.34 ns |  0.81 |    0.00 |           - |           - |           - |                   - |
|       ImHaspMap_V1_TryFind |    10 | 18.07 ns | 0.1319 ns | 0.1169 ns | 18.05 ns |  1.02 |    0.01 |           - |           - |           - |                   - |
| DictionarySlim_TryGetValue |    10 | 18.50 ns | 0.0311 ns | 0.0291 ns | 18.50 ns |  1.05 |    0.01 |           - |           - |           - |                   - |
|                            |       |          |           |           |          |       |         |             |             |             |                     |
|          ImHaspMap_TryFind |   100 | 21.71 ns | 0.0207 ns | 0.0183 ns | 21.71 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|         ImHaspMap_TryFind2 |   100 | 25.49 ns | 0.1951 ns | 0.1825 ns | 25.46 ns |  1.17 |    0.01 |           - |           - |           - |                   - |
|         ImHaspMap_TryFind3 |   100 | 19.05 ns | 0.0278 ns | 0.0260 ns | 19.04 ns |  0.88 |    0.00 |           - |           - |           - |                   - |
|       ImHaspMap_V1_TryFind |   100 | 20.59 ns | 0.0244 ns | 0.0228 ns | 20.59 ns |  0.95 |    0.00 |           - |           - |           - |                   - |
| DictionarySlim_TryGetValue |   100 | 19.56 ns | 0.0401 ns | 0.0355 ns | 19.55 ns |  0.90 |    0.00 |           - |           - |           - |                   - |
|                            |       |          |           |           |          |       |         |             |             |             |                     |
|          ImHaspMap_TryFind |  1000 | 26.02 ns | 0.6203 ns | 1.7394 ns | 25.42 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|         ImHaspMap_TryFind2 |  1000 | 28.98 ns | 0.1347 ns | 0.1260 ns | 28.95 ns |  1.04 |    0.07 |           - |           - |           - |                   - |
|         ImHaspMap_TryFind3 |  1000 | 22.43 ns | 0.2495 ns | 0.2334 ns | 22.30 ns |  0.80 |    0.06 |           - |           - |           - |                   - |
|       ImHaspMap_V1_TryFind |  1000 | 25.82 ns | 0.0352 ns | 0.0330 ns | 25.83 ns |  0.93 |    0.06 |           - |           - |           - |                   - |
| DictionarySlim_TryGetValue |  1000 | 20.87 ns | 0.1638 ns | 0.1532 ns | 20.85 ns |  0.75 |    0.05 |           - |           - |           - |                   - |

            */
            private const string Seed = "hubba-bubba";

            [Params(10, 100, 1_000, 10_000)]//, 100_000)]
            public int Count;

            public string LookupKey;

            [GlobalSetup]
            public void Populate()
            {
                var n = Count - 1;
                LookupKey = n + Seed + n; 

                _map = ImMap_AddOrUpdate();
                _mapV1 = AddOrUpdate_V1_AddOrUpdate();
                _dictSlim = DictSlim_GetOrAddValueRef();
                _dict = Dict_TryAdd();
                _concurrentDict = ConcurrentDict_TryAdd();
                _immutableDict = ImmutableDict_Add();
            }

            private ImHashMap<string, string> _map;
            private V1.ImHashMap<string, string> _mapV1;
            private Dictionary<string, string> _dict;
            private DictionarySlim<string, string> _dictSlim;
            private ConcurrentDictionary<string, string> _concurrentDict;
            private ImmutableDictionary<string, string> _immutableDict;

            #region Populate the collections

            public ImHashMap<string, string> ImMap_AddOrUpdate()
            {
                var map = ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map = map.AddOrUpdate(v + a, v, out _, out _);
                }

                return map;
            }

            public V1.ImHashMap<string, string> AddOrUpdate_V1_AddOrUpdate()
            {
                var map = V1.ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map = map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            public Dictionary<string, string> Dict_TryAdd()
            {
                var map = new Dictionary<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.TryAdd(v + a, v);
                }

                return map;
            }

            public DictionarySlim<string, string> DictSlim_GetOrAddValueRef()
            {
                var map = new DictionarySlim<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.GetOrAddValueRef(v + a) = v;
                }

                return map;
            }

            public ConcurrentDictionary<string, string> ConcurrentDict_TryAdd()
            {
                var map = new ConcurrentDictionary<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.TryAdd(v + a, v);
                }

                return map;
            }

            public ImmutableDictionary<string, string> ImmutableDict_Add()
            {
                var map = ImmutableDictionary<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map = map.Add(v + a, v);
                }

                return map;
            }

            #endregion

            [Benchmark(Baseline = true)]
            public string ImHaspMap_TryFind()
            {
                _map.TryFind(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ImHaspMap_TryFind3()
            {
                _map.TryFind3(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ImHashMap_V1_TryFind()
            {
                _mapV1.TryFind(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string DictionarySlim_TryGetValue()
            {
                _dictSlim.TryGetValue(LookupKey, out var result);
                return result;
            }

            //[Benchmark]
            public string Dictionary_TryGetValue()
            {
                _dict.TryGetValue(LookupKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ConcurrentDictionary_TryGetValue()
            {
                _concurrentDict.TryGetValue(LookupKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ImmutableDict_TryGet()
            {
                _immutableDict.TryGetValue(LookupKey, out var result);
                return result;
            }
        }
    }
}
