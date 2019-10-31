using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
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

## V2

|                          Method |  Count |           Mean |         Error |        StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|-------------------------------- |------- |---------------:|--------------:|--------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|           ImHashMap_AddOrUpdate |     10 |       1.166 us |     0.0037 us |     0.0031 us |  1.00 |    0.00 |      0.7305 |           - |           - |             3.38 KB |
|        ImHashMap_V1_AddOrUpdate |     10 |       1.288 us |     0.0035 us |     0.0033 us |  1.10 |    0.00 |      0.8125 |           - |           - |             3.75 KB |
| DictionarySlim_GetOrAddValueRef |     10 |       1.219 us |     0.0062 us |     0.0055 us |  1.05 |    0.00 |      0.5322 |           - |           - |             2.45 KB |
|               Dictionary_TryAdd |     10 |       1.060 us |     0.0208 us |     0.0232 us |  0.91 |    0.02 |      0.5150 |           - |           - |             2.38 KB |
|     ConcurrentDictionary_TryAdd |     10 |       1.698 us |     0.0332 us |     0.0383 us |  1.45 |    0.04 |      0.5817 |           - |           - |             2.69 KB |
|         ImmutableDictionary_Add |     10 |       6.308 us |     0.1391 us |     0.1428 us |  5.42 |    0.14 |      0.8163 |           - |           - |              3.8 KB |
|                                 |        |                |               |               |       |         |             |             |             |                     |
|           ImHashMap_AddOrUpdate |    100 |      21.886 us |     0.3847 us |     0.3598 us |  1.00 |    0.00 |     10.8948 |           - |           - |             50.3 KB |
|        ImHashMap_V1_AddOrUpdate |    100 |      23.186 us |     0.3365 us |     0.3147 us |  1.06 |    0.03 |     11.8408 |      0.0610 |           - |            54.66 KB |
| DictionarySlim_GetOrAddValueRef |    100 |      12.331 us |     0.1234 us |     0.1155 us |  0.56 |    0.01 |      4.6234 |           - |           - |            21.38 KB |
|               Dictionary_TryAdd |    100 |      11.015 us |     0.1632 us |     0.1526 us |  0.50 |    0.01 |      5.2032 |      0.0153 |           - |            24.02 KB |
|     ConcurrentDictionary_TryAdd |    100 |      22.886 us |     0.4533 us |     0.4655 us |  1.05 |    0.02 |      6.9580 |      0.0305 |           - |            32.13 KB |
|         ImmutableDictionary_Add |    100 |     111.145 us |     2.2105 us |     2.0677 us |  5.08 |    0.15 |     12.4512 |      0.1221 |           - |            57.78 KB |
|                                 |        |                |               |               |       |         |             |             |             |                     |
|           ImHashMap_AddOrUpdate |   1000 |     425.340 us |     8.3217 us |     9.5832 us |  1.00 |    0.00 |    135.2539 |     50.2930 |           - |            663.8 KB |
|        ImHashMap_V1_AddOrUpdate |   1000 |     464.418 us |     6.7333 us |     6.2983 us |  1.09 |    0.03 |    129.3945 |     57.6172 |           - |            708.8 KB |
| DictionarySlim_GetOrAddValueRef |   1000 |     142.125 us |     1.9040 us |     1.7810 us |  0.34 |    0.01 |     39.0625 |     15.3809 |           - |           204.11 KB |
|               Dictionary_TryAdd |   1000 |     125.348 us |     2.3461 us |     2.3042 us |  0.30 |    0.01 |     43.7012 |     18.5547 |           - |           247.48 KB |
|     ConcurrentDictionary_TryAdd |   1000 |     257.591 us |     5.1379 us |     4.8060 us |  0.61 |    0.01 |     54.6875 |     25.3906 |           - |              298 KB |
|         ImmutableDictionary_Add |   1000 |   1,706.924 us |    27.1727 us |    25.4174 us |  4.02 |    0.11 |    156.2500 |     62.5000 |           - |           794.91 KB |
|                                 |        |                |               |               |       |         |             |             |             |                     |
|           ImHashMap_AddOrUpdate |  10000 |  10,862.478 us |   147.4615 us |   137.9356 us |  1.00 |    0.00 |   1359.3750 |    375.0000 |    156.2500 |           8273.2 KB |
|        ImHashMap_V1_AddOrUpdate |  10000 |  11,525.821 us |    96.1852 us |    89.9717 us |  1.06 |    0.02 |   1421.8750 |    421.8750 |    187.5000 |          8692.97 KB |
| DictionarySlim_GetOrAddValueRef |  10000 |   2,811.003 us |    20.5512 us |    18.2181 us |  0.26 |    0.00 |    398.4375 |    199.2188 |    199.2188 |          2450.55 KB |
|               Dictionary_TryAdd |  10000 |   2,592.468 us |    30.0563 us |    28.1147 us |  0.24 |    0.00 |    441.4063 |    218.7500 |    218.7500 |          2473.84 KB |
|     ConcurrentDictionary_TryAdd |  10000 |   8,158.437 us |    61.7210 us |    54.7140 us |  0.75 |    0.01 |    578.1250 |    312.5000 |    125.0000 |          3420.05 KB |
|         ImmutableDictionary_Add |  10000 |  26,318.881 us |   226.7584 us |   212.1099 us |  2.42 |    0.04 |   1687.5000 |    375.0000 |    125.0000 |         10142.22 KB |
|                                 |        |                |               |               |       |         |             |             |             |                     |
|           ImHashMap_AddOrUpdate | 100000 | 206,321.656 us | 1,693.5615 us | 1,584.1585 us |  1.00 |    0.00 |  17000.0000 |   4666.6667 |    666.6667 |        100204.42 KB |
|        ImHashMap_V1_AddOrUpdate | 100000 | 221,562.601 us | 1,269.5998 us | 1,187.5844 us |  1.07 |    0.01 |  17666.6667 |   4666.6667 |    666.6667 |        104488.67 KB |
| DictionarySlim_GetOrAddValueRef | 100000 |  55,222.073 us |   253.7647 us |   237.3717 us |  0.27 |    0.00 |   3300.0000 |   1400.0000 |    600.0000 |         24191.44 KB |
|               Dictionary_TryAdd | 100000 |  51,301.961 us |   583.1110 us |   516.9127 us |  0.25 |    0.00 |   3363.6364 |   1545.4545 |    727.2727 |         25277.55 KB |
|     ConcurrentDictionary_TryAdd | 100000 | 105,307.097 us |   826.3526 us |   732.5400 us |  0.51 |    0.00 |   4800.0000 |   2000.0000 |    600.0000 |         27606.14 KB |
|         ImmutableDictionary_Add | 100000 | 389,202.241 us | 1,284.0234 us | 1,138.2533 us |  1.89 |    0.02 |  20000.0000 |   5000.0000 |           - |        123849.47 KB |

             */

            private const string KeySeed = "hubba-bubba";

            [Params(10, 100, 1_000, 10_000, 100_000)]
            public int Count;

            [Benchmark(Baseline = true)]
            public ImHashMap<string, string> ImHashMap_AddOrUpdate()
            {
                var map = ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + KeySeed;
                    map = map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            [Benchmark]
            public ImTools.OldVersions.V1.ImHashMap<string, string> ImHashMap_V1_AddOrUpdate()
            {
                var map = ImTools.OldVersions.V1.ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + KeySeed;
                    map = map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            [Benchmark]
            public DictionarySlim<string, string> DictionarySlim_GetOrAddValueRef()
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
            public Dictionary<string, string> Dictionary_TryAdd()
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
            public ConcurrentDictionary<string, string> ConcurrentDictionary_TryAdd()
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
            public ImmutableDictionary<string, string> ImmutableDictionary_Add()
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
            ## Clean benchmark

|                           Method |  Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------- |------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                ImHashMap_TryFind |     10 |  18.44 ns | 0.0834 ns | 0.0780 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind |     10 |  17.27 ns | 0.0279 ns | 0.0261 ns |  0.94 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |     10 |  19.16 ns | 0.0664 ns | 0.0589 ns |  1.04 |    0.01 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |     10 |  23.82 ns | 0.0689 ns | 0.0644 ns |  1.29 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |     10 |  32.17 ns | 0.0958 ns | 0.0849 ns |  1.74 |    0.01 |           - |           - |           - |                   - |
|       ImmutableDictionary_TryGet |     10 |  80.79 ns | 0.2833 ns | 0.2512 ns |  4.38 |    0.02 |           - |           - |           - |                   - |
|                                  |        |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |    100 |  21.04 ns | 0.0587 ns | 0.0549 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind |    100 |  21.08 ns | 0.0438 ns | 0.0389 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |    100 |  20.44 ns | 0.0742 ns | 0.0694 ns |  0.97 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |    100 |  24.70 ns | 0.1176 ns | 0.1100 ns |  1.17 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |    100 |  33.67 ns | 0.0547 ns | 0.0485 ns |  1.60 |    0.00 |           - |           - |           - |                   - |
|       ImmutableDictionary_TryGet |    100 |  89.46 ns | 0.4711 ns | 0.4406 ns |  4.25 |    0.03 |           - |           - |           - |                   - |
|                                  |        |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |   1000 |  24.67 ns | 0.0331 ns | 0.0310 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind |   1000 |  27.08 ns | 0.0361 ns | 0.0338 ns |  1.10 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |   1000 |  21.21 ns | 0.0644 ns | 0.0571 ns |  0.86 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |   1000 |  26.52 ns | 0.0529 ns | 0.0495 ns |  1.07 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |   1000 |  33.19 ns | 0.0701 ns | 0.0656 ns |  1.35 |    0.00 |           - |           - |           - |                   - |
|       ImmutableDictionary_TryGet |   1000 | 100.52 ns | 0.3336 ns | 0.3121 ns |  4.07 |    0.02 |           - |           - |           - |                   - |
|                                  |        |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |  10000 |  31.28 ns | 0.0570 ns | 0.0533 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind |  10000 |  35.02 ns | 0.0997 ns | 0.0832 ns |  1.12 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |  10000 |  22.05 ns | 0.0266 ns | 0.0249 ns |  0.71 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |  10000 |  28.33 ns | 0.6186 ns | 0.8258 ns |  0.91 |    0.03 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |  10000 |  37.45 ns | 0.1562 ns | 0.1304 ns |  1.20 |    0.00 |           - |           - |           - |                   - |
|       ImmutableDictionary_TryGet |  10000 | 112.65 ns | 0.0580 ns | 0.0514 ns |  3.60 |    0.01 |           - |           - |           - |                   - |
|                                  |        |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind | 100000 |  37.57 ns | 0.0373 ns | 0.0349 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind | 100000 |  38.04 ns | 0.0865 ns | 0.0722 ns |  1.01 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue | 100000 |  23.90 ns | 0.0241 ns | 0.0225 ns |  0.64 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue | 100000 |  28.64 ns | 0.0551 ns | 0.0488 ns |  0.76 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue | 100000 |  36.21 ns | 0.0652 ns | 0.0545 ns |  0.96 |    0.00 |           - |           - |           - |                   - |
|       ImmutableDictionary_TryGet | 100000 | 117.21 ns | 0.3816 ns | 0.3570 ns |  3.12 |    0.01 |           - |           - |           - |                   - |
*/
            private const string Seed = "hubba-bubba";

            [Params(10, 100, 1_000, 10_000, 100_000)]
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
            private ImTools.OldVersions.V1.ImHashMap<string, string> _mapV1;
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
                    map = map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            public ImTools.OldVersions.V1.ImHashMap<string, string> AddOrUpdate_V1_AddOrUpdate()
            {
                var map = ImTools.OldVersions.V1.ImHashMap<string, string>.Empty;

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
            public string ImHashMap_TryFind()
            {
                _map.TryFind(LookupKey, out var result);
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

            [Benchmark]
            public string Dictionary_TryGetValue()
            {
                _dict.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ConcurrentDictionary_TryGetValue()
            {
                _concurrentDict.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ImmutableDictionary_TryGet()
            {
                _immutableDict.TryGetValue(LookupKey, out var result);
                return result;
            }
        }
    }
}
