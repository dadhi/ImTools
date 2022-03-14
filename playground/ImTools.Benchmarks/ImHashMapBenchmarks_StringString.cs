using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using ImTools;
using ImTools.V2;
using ImToolsV3;
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

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT


|                          Method |  Count |             Mean |            Error |           StdDev | Ratio | RatioSD |      Gen 0 |     Gen 1 |     Gen 2 |    Allocated |
|-------------------------------- |------- |-----------------:|-----------------:|-----------------:|------:|--------:|-----------:|----------:|----------:|-------------:|
|           ImHashMap_AddOrUpdate |     10 |       1,093.3 ns |          4.17 ns |          3.48 ns |  1.00 |    0.00 |     0.6218 |    0.0057 |         - |      2.86 KB |
|        ImHashMap_V1_AddOrUpdate |     10 |       1,315.4 ns |          4.41 ns |          4.12 ns |  1.20 |    0.01 |     0.7439 |    0.0076 |         - |      3.42 KB |
|      ImHashMapSlots_AddOrUpdate |     10 |         960.6 ns |          2.73 ns |          2.56 ns |  0.88 |    0.00 |     0.4978 |    0.0076 |         - |      2.29 KB |
| DictionarySlim_GetOrAddValueRef |     10 |       1,173.4 ns |          2.91 ns |          2.73 ns |  1.07 |    0.00 |     0.4311 |    0.0038 |         - |      1.98 KB |
|               Dictionary_TryAdd |     10 |         982.5 ns |          3.27 ns |          2.73 ns |  0.90 |    0.00 |     0.4120 |    0.0038 |         - |       1.9 KB |
|     ConcurrentDictionary_TryAdd |     10 |       1,193.6 ns |         23.09 ns |         23.71 ns |  1.09 |    0.02 |     0.4826 |    0.0076 |         - |      2.22 KB |
|         ImmutableDictionary_Add |     10 |       4,316.8 ns |         83.29 ns |         89.12 ns |  3.98 |    0.08 |     0.7172 |         - |         - |      3.33 KB |
|                                 |        |                  |                  |                  |       |         |            |           |           |              |
|           ImHashMap_AddOrUpdate |    100 |      19,907.3 ns |        395.99 ns |        423.71 ns |  1.00 |    0.00 |    10.5591 |    0.0305 |         - |     48.56 KB |
|        ImHashMap_V1_AddOrUpdate |    100 |      22,316.0 ns |        436.72 ns |        485.41 ns |  1.12 |    0.04 |    11.2305 |    0.0305 |         - |      51.7 KB |
|      ImHashMapSlots_AddOrUpdate |    100 |      12,314.1 ns |         49.90 ns |         41.67 ns |  0.61 |    0.01 |     5.9967 |    0.1221 |         - |      27.6 KB |
| DictionarySlim_GetOrAddValueRef |    100 |      11,484.3 ns |        224.91 ns |        267.74 ns |  0.58 |    0.02 |     4.3945 |    0.4272 |         - |      20.2 KB |
|               Dictionary_TryAdd |    100 |      10,164.3 ns |         75.98 ns |         67.36 ns |  0.51 |    0.01 |     4.9591 |    0.5341 |         - |     22.84 KB |
|     ConcurrentDictionary_TryAdd |    100 |      16,659.4 ns |         65.40 ns |         54.61 ns |  0.83 |    0.02 |     6.6223 |    0.9155 |         - |     30.45 KB |
|         ImmutableDictionary_Add |    100 |      74,507.9 ns |        308.41 ns |        240.79 ns |  3.71 |    0.08 |    12.4512 |    0.6104 |         - |     57.23 KB |
|                                 |        |                  |                  |                  |       |         |            |           |           |              |
|           ImHashMap_AddOrUpdate |   1000 |     376,368.2 ns |      7,282.67 ns |      7,152.56 ns |  1.00 |    0.00 |   131.8359 |   50.2930 |         - |    652.41 KB |
|        ImHashMap_V1_AddOrUpdate |   1000 |     428,864.6 ns |      1,453.22 ns |      1,288.24 ns |  1.14 |    0.02 |   127.9297 |   58.5938 |         - |    697.31 KB |
|      ImHashMapSlots_AddOrUpdate |   1000 |     262,245.3 ns |      2,983.80 ns |      2,791.04 ns |  0.70 |    0.02 |    77.6367 |   33.6914 |         - |    420.18 KB |
| DictionarySlim_GetOrAddValueRef |   1000 |     122,667.2 ns |        760.83 ns |        711.68 ns |  0.33 |    0.01 |    39.5508 |   13.1836 |         - |    195.91 KB |
|               Dictionary_TryAdd |   1000 |     124,434.9 ns |      2,469.97 ns |      4,260.58 ns |  0.34 |    0.01 |    44.6777 |   21.7285 |         - |    239.27 KB |
|     ConcurrentDictionary_TryAdd |   1000 |     207,383.3 ns |      4,129.94 ns |      4,418.99 ns |  0.55 |    0.01 |    54.4434 |   25.1465 |         - |    289.61 KB |
|         ImmutableDictionary_Add |   1000 |   1,167,264.4 ns |     22,745.67 ns |     28,766.00 ns |  3.13 |    0.10 |   156.2500 |   66.4063 |         - |    783.02 KB |
|                                 |        |                  |                  |                  |       |         |            |           |           |              |
|           ImHashMap_AddOrUpdate |  10000 |  10,406,430.9 ns |     88,127.39 ns |     78,122.63 ns |  1.00 |    0.00 |  1359.3750 |  359.3750 |  156.2500 |   8211.25 KB |
|        ImHashMap_V1_AddOrUpdate |  10000 |  11,325,956.9 ns |     34,619.98 ns |     28,909.25 ns |  1.09 |    0.01 |  1421.8750 |  406.2500 |  187.5000 |   8636.41 KB |
|      ImHashMapSlots_AddOrUpdate |  10000 |   9,569,581.2 ns |    102,241.63 ns |     95,636.88 ns |  0.92 |    0.01 |   968.7500 |  328.1250 |  140.6250 |   5830.84 KB |
| DictionarySlim_GetOrAddValueRef |  10000 |   2,669,723.7 ns |     26,466.20 ns |     24,756.50 ns |  0.26 |    0.00 |   398.4375 |  199.2188 |  199.2188 |   2372.04 KB |
|               Dictionary_TryAdd |  10000 |   2,510,466.2 ns |     36,855.32 ns |     30,775.86 ns |  0.24 |    0.00 |   441.4063 |  218.7500 |  218.7500 |   2395.31 KB |
|     ConcurrentDictionary_TryAdd |  10000 |   6,955,912.0 ns |     45,624.50 ns |     35,620.61 ns |  0.67 |    0.01 |   539.0625 |  296.8750 |  132.8125 |   3261.96 KB |
|         ImmutableDictionary_Add |  10000 |  19,846,155.4 ns |    105,361.44 ns |     98,555.15 ns |  1.91 |    0.02 |  1656.2500 |  375.0000 |  125.0000 |  10081.56 KB |
|                                 |        |                  |                  |                  |       |         |            |           |           |              |
|           ImHashMap_AddOrUpdate | 100000 | 195,782,064.4 ns |  1,808,512.64 ns |  1,691,683.85 ns |  1.00 |    0.00 | 16666.6667 | 4333.3333 |  666.6667 |  98010.31 KB |
|        ImHashMap_V1_AddOrUpdate | 100000 | 223,142,584.4 ns |  2,375,567.88 ns |  2,222,107.67 ns |  1.14 |    0.02 | 17333.3333 | 4333.3333 |  666.6667 | 102428.89 KB |
|      ImHashMapSlots_AddOrUpdate | 100000 | 190,004,735.6 ns |  1,853,537.49 ns |  1,733,800.12 ns |  0.97 |    0.01 | 13000.0000 | 4000.0000 | 1000.0000 |  74166.47 KB |
| DictionarySlim_GetOrAddValueRef | 100000 |  52,655,754.8 ns |  1,012,952.93 ns |  1,352,263.07 ns |  0.27 |    0.01 |  3000.0000 | 1400.0000 |  600.0000 |  22003.54 KB |
|               Dictionary_TryAdd | 100000 |  45,715,377.2 ns |    891,533.00 ns |    790,320.78 ns |  0.23 |    0.00 |  2937.5000 | 1312.5000 |  687.5000 |  23089.58 KB |
|     ConcurrentDictionary_TryAdd | 100000 | 116,861,845.3 ns |  1,873,445.76 ns |  1,752,422.33 ns |  0.60 |    0.01 |  5000.0000 | 2000.0000 |  600.0000 |  32124.31 KB |
|         ImmutableDictionary_Add | 100000 | 342,100,076.5 ns | 17,599,591.32 ns | 18,073,484.53 ns |  1.75 |    0.10 | 20000.0000 | 5000.0000 |         - | 121783.33 KB |

## V3

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

|                                    Method |  Count |       Mean |     Error |    StdDev | Ratio | RatioSD |      Gen 0 |     Gen 1 |    Gen 2 | Allocated |
|------------------------------------------ |------- |-----------:|----------:|----------:|------:|--------:|-----------:|----------:|---------:|----------:|
|              V2_ImHashMap_AVL_AddOrUpdate |  10000 |  12.255 ms | 0.1736 ms | 0.1539 ms |  1.00 |    0.00 |  1328.1250 |  343.7500 | 140.6250 |   7.89 MB |
|          V3_ImHashMap_23Tree_AddOrUpdate |  10000 |   9.464 ms | 0.0964 ms | 0.0902 ms |  0.77 |    0.01 |   984.3750 |  375.0000 | 156.2500 |    5.8 MB |
| V3_PartitionedHashMap_23Tree_AddOrUpdate |  10000 |   8.361 ms | 0.0299 ms | 0.0233 ms |  0.68 |    0.01 |   718.7500 |  281.2500 | 109.3750 |   4.35 MB |
|               ConcurrentDictionary_TryAdd |  10000 |   8.299 ms | 0.1656 ms | 0.2478 ms |  0.67 |    0.02 |   546.8750 |  281.2500 | 125.0000 |   3.18 MB |
|                                           |        |            |           |           |       |         |            |           |          |           |
|              V2_ImHashMap_AVL_AddOrUpdate | 100000 | 227.280 ms | 3.8927 ms | 3.6412 ms |  1.00 |    0.00 | 16333.3333 | 4333.3333 | 666.6667 |  94.15 MB |
|          V3_ImHashMap_23Tree_AddOrUpdate | 100000 | 180.789 ms | 2.1376 ms | 1.7850 ms |  0.79 |    0.01 | 12333.3333 | 3000.0000 | 666.6667 |  70.34 MB |
| V3_PartitionedHashMap_23Tree_AddOrUpdate | 100000 | 175.898 ms | 3.5090 ms | 5.8628 ms |  0.79 |    0.03 | 10000.0000 | 3000.0000 | 666.6667 |  55.65 MB |
|               ConcurrentDictionary_TryAdd | 100000 | 104.194 ms | 1.2147 ms | 1.0768 ms |  0.46 |    0.01 |  4200.0000 | 1600.0000 | 400.0000 |  25.09 MB |

|                                            Method |  Count |       Mean |     Error |    StdDev | Ratio | RatioSD |     Gen 0 |     Gen 1 |    Gen 2 | Allocated |
|-------------------------------------------------- |------- |-----------:|----------:|----------:|------:|--------:|----------:|----------:|---------:|----------:|
| V3_PartitionedHashMap_64Parts_23Tree_AddOrUpdate |  10000 |   9.192 ms | 0.0974 ms | 0.0813 ms |  1.00 |    0.00 |  609.3750 |  265.6250 |  93.7500 |   3.63 MB |
|                       ConcurrentDictionary_TryAdd |  10000 |   9.872 ms | 0.1674 ms | 0.1644 ms |  1.08 |    0.02 |  531.2500 |  265.6250 | 125.0000 |   3.16 MB |
|                                                   |        |            |           |           |       |         |           |           |          |           |
| V3_PartitionedHashMap_64Parts_23Tree_AddOrUpdate | 100000 | 206.237 ms | 4.1077 ms | 5.0446 ms |  1.00 |    0.00 | 8666.6667 | 2666.6667 | 666.6667 |  48.35 MB |
|                       ConcurrentDictionary_TryAdd | 100000 | 130.479 ms | 2.5806 ms | 3.0720 ms |  0.63 |    0.02 | 4250.0000 | 1750.0000 | 500.0000 |  24.99 MB |

*/

            private const string Seed = "hubba-bubba";

            [Params(10_000, 100_000)]
            public int Count;

            // [Benchmark(Baseline = true)]
            public ImTools.V2.ImHashMap<string, string> V2_ImHashMap_AVL_AddOrUpdate()
            {
                var map = ImTools.V2.ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map = map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            // [Benchmark]
            public ImToolsV3.ImHashMap<string, string> V3_ImHashMap_23Tree_AddOrUpdate()
            {
                var map = ImToolsV3.ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map = map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            // [Benchmark]
            public ImToolsV3.ImHashMap<string, string>[] V3_PartitionedHashMap_23Tree_AddOrUpdate()
            {
                var map = ImToolsV3.PartitionedHashMap.CreateEmpty<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            [Benchmark(Baseline = true)]
            public ImToolsV3.ImHashMap<string, string>[] V3_PartitionedHashMap_64Parts_23Tree_AddOrUpdate()
            {
                var map = PartitionedHashMap.CreateEmpty<string, string>(64);

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.AddOrUpdate(v + a, v, 63);
                }

                return map;
            }

            [Benchmark]
            public ImToolsV3.ImHashMap<string, string>[] V3_PartitionedHashMap_128Parts_23Tree_AddOrUpdate()
            {
                var map = ImToolsV3.PartitionedHashMap.CreateEmpty<string, string>(128);

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.AddOrUpdate(v + a, v, 127);
                }

                return map;
            }

            [Benchmark]
            public ImToolsV3.ImHashMap<string, string>[] V3_PartitionedHashMap_256Parts_23Tree_AddOrUpdate()
            {
                var map = ImToolsV3.PartitionedHashMap.CreateEmpty<string, string>(256);

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.AddOrUpdate(v + a, v, 255);
                }

                return map;
            }

            // [Benchmark]
            public ImTools.OldVersions.V1.ImHashMap<string, string> ImHashMap_V1_AddOrUpdate()
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

            // [Benchmark]
            public ImTools.V2.ImHashMap<string, string>[] ImHashMapSlots_AddOrUpdate()
            {
                var map = ImTools.V2.ImHashMapSlots.CreateWithEmpty<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            // [Benchmark]
            public DictionarySlim<string, string> DictionarySlim_GetOrAddValueRef()
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

            // [Benchmark]
            public Dictionary<string, string> Dictionary_TryAdd()
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

            [Benchmark]
            public ConcurrentDictionary<string, string> ConcurrentDictionary_TryAdd()
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

            // [Benchmark]
            public ImmutableDictionary<string, string> ImmutableDictionary_Add()
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

## V2:

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 2.2.7 (CoreCLR 4.6.28008.02, CoreFX 4.6.28008.03), X64 RyuJIT
  DefaultJob : .NET Core 2.2.7 (CoreCLR 4.6.28008.02, CoreFX 4.6.28008.03), X64 RyuJIT


|                           Method |  Count |      Mean |    Error |   StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------- |----------:|---------:|---------:|------:|--------:|------:|------:|------:|----------:|
|                ImHashMap_TryFind |     10 |  18.50 ns | 0.123 ns | 0.115 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_V1_TryFind |     10 |  19.23 ns | 0.082 ns | 0.072 ns |  1.04 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |     10 |  15.35 ns | 0.070 ns | 0.065 ns |  0.83 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     10 |  19.47 ns | 0.094 ns | 0.088 ns |  1.05 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     10 |  23.46 ns | 0.244 ns | 0.216 ns |  1.27 |    0.02 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     10 |  31.91 ns | 0.504 ns | 0.421 ns |  1.73 |    0.03 |     - |     - |     - |         - |
|       ImmutableDictionary_TryGet |     10 |  90.43 ns | 1.127 ns | 0.941 ns |  4.89 |    0.06 |     - |     - |     - |         - |
|                                  |        |           |          |          |       |         |       |       |       |           |
|                ImHashMap_TryFind |    100 |  21.15 ns | 0.095 ns | 0.089 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_V1_TryFind |    100 |  22.42 ns | 0.232 ns | 0.205 ns |  1.06 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |    100 |  16.61 ns | 0.072 ns | 0.067 ns |  0.79 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |    100 |  21.76 ns | 0.141 ns | 0.117 ns |  1.03 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    100 |  24.31 ns | 0.537 ns | 0.619 ns |  1.14 |    0.03 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    100 |  32.26 ns | 0.214 ns | 0.200 ns |  1.53 |    0.01 |     - |     - |     - |         - |
|       ImmutableDictionary_TryGet |    100 |  96.77 ns | 1.731 ns | 1.619 ns |  4.58 |    0.08 |     - |     - |     - |         - |
|                                  |        |           |          |          |       |         |       |       |       |           |
|                ImHashMap_TryFind |   1000 |  25.26 ns | 0.168 ns | 0.157 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_V1_TryFind |   1000 |  26.48 ns | 0.284 ns | 0.252 ns |  1.05 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |   1000 |  22.71 ns | 0.262 ns | 0.245 ns |  0.90 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |   1000 |  22.01 ns | 0.219 ns | 0.204 ns |  0.87 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   1000 |  26.59 ns | 0.561 ns | 0.551 ns |  1.05 |    0.02 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   1000 |  35.69 ns | 0.482 ns | 0.451 ns |  1.41 |    0.02 |     - |     - |     - |         - |
|       ImmutableDictionary_TryGet |   1000 | 108.94 ns | 1.192 ns | 1.057 ns |  4.31 |    0.05 |     - |     - |     - |         - |
|                                  |        |           |          |          |       |         |       |       |       |           |
|                ImHashMap_TryFind |  10000 |  33.87 ns | 0.238 ns | 0.222 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_V1_TryFind |  10000 |  35.74 ns | 0.350 ns | 0.293 ns |  1.05 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |  10000 |  33.26 ns | 0.140 ns | 0.131 ns |  0.98 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |  10000 |  23.39 ns | 0.536 ns | 0.752 ns |  0.70 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  10000 |  27.81 ns | 0.099 ns | 0.093 ns |  0.82 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  10000 |  35.95 ns | 0.107 ns | 0.095 ns |  1.06 |    0.01 |     - |     - |     - |         - |
|       ImmutableDictionary_TryGet |  10000 | 120.33 ns | 0.667 ns | 0.624 ns |  3.55 |    0.03 |     - |     - |     - |         - |
|                                  |        |           |          |          |       |         |       |       |       |           |
|                ImHashMap_TryFind | 100000 |  33.85 ns | 0.092 ns | 0.086 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_V1_TryFind | 100000 |  39.52 ns | 0.165 ns | 0.146 ns |  1.17 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind | 100000 |  36.57 ns | 0.135 ns | 0.126 ns |  1.08 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue | 100000 |  26.86 ns | 0.040 ns | 0.035 ns |  0.79 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue | 100000 |  26.88 ns | 0.142 ns | 0.133 ns |  0.79 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue | 100000 |  36.75 ns | 0.116 ns | 0.109 ns |  1.09 |    0.00 |     - |     - |     - |         - |
|       ImmutableDictionary_TryGet | 100000 | 122.55 ns | 0.353 ns | 0.330 ns |  3.62 |    0.01 |     - |     - |     - |         - |

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


|                                Method |  Count |     Mean |    Error |   StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------- |------- |---------:|---------:|---------:|------:|--------:|------:|------:|------:|----------:|
|              V2_ImHashMap_AVL_TryFind |  10000 | 35.19 ns | 0.400 ns | 0.355 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|          V3_ImHashMap_23Tree_TryFind |  10000 | 33.10 ns | 0.507 ns | 0.423 ns |  0.94 |    0.02 |     - |     - |     - |         - |
| V3_PartitionedHashMap_23Tree_TryFind |  10000 | 34.72 ns | 0.278 ns | 0.232 ns |  0.99 |    0.01 |     - |     - |     - |         - |
|      ConcurrentDictionary_TryGetValue |  10000 | 35.63 ns | 0.289 ns | 0.241 ns |  1.01 |    0.01 |     - |     - |     - |         - |
|                                       |        |          |          |          |       |         |       |       |       |           |
|              V2_ImHashMap_AVL_TryFind | 100000 | 38.50 ns | 0.451 ns | 0.377 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|          V3_ImHashMap_23Tree_TryFind | 100000 | 38.79 ns | 0.593 ns | 0.495 ns |  1.01 |    0.02 |     - |     - |     - |         - |
| V3_PartitionedHashMap_23Tree_TryFind | 100000 | 47.47 ns | 0.394 ns | 0.369 ns |  1.24 |    0.01 |     - |     - |     - |         - |
|      ConcurrentDictionary_TryGetValue | 100000 | 42.49 ns | 0.580 ns | 0.542 ns |  1.11 |    0.02 |     - |     - |     - |         - |

*/
            private const string Seed = "hubba-bubba";

            [Params(10_000, 100_000)]
            public int Count;

            public string LookupKey;

            [GlobalSetup]
            public void Populate()
            {
                var n = Count - 1;
                LookupKey = n + Seed + n; 

                _map = ImMap_AddOrUpdate();
                _map234 = V3_ImHashMap_23Tree_AddOrUpdate();
                _partMap234 = V3_PartitionedHashMap_23Tree_AddOrUpdate();
                _mapV1 = AddOrUpdate_V1_AddOrUpdate();
                _mapSlots = ImHashMapSlots_AddOrUpdate();
                _dictSlim = DictSlim_GetOrAddValueRef();
                _dict = Dict_TryAdd();
                _concurrentDict = ConcurrentDict_TryAdd();
                _immutableDict = ImmutableDict_Add();
            }

            private ImTools.V2.ImHashMap<string, string> _map;
            private ImToolsV3.ImHashMap<string, string> _map234;
            private ImToolsV3.ImHashMap<string, string>[] _partMap234;
            private ImTools.OldVersions.V1.ImHashMap<string, string> _mapV1;
            private ImTools.V2.ImHashMap<string, string>[] _mapSlots;
            private Dictionary<string, string> _dict;
            private DictionarySlim<string, string> _dictSlim;
            private ConcurrentDictionary<string, string> _concurrentDict;
            private ImmutableDictionary<string, string> _immutableDict;

            #region Populate the collections

            public ImTools.V2.ImHashMap<string, string> ImMap_AddOrUpdate()
            {
                var map = ImTools.V2.ImHashMap<string, string>.Empty;

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

            public ImTools.V2.ImHashMap<string, string>[] ImHashMapSlots_AddOrUpdate()
            {
                var map = ImTools.V2.ImHashMapSlots.CreateWithEmpty<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            public ImToolsV3.ImHashMap<string, string> V3_ImHashMap_23Tree_AddOrUpdate()
            {
                var map = ImToolsV3.ImHashMap<string, string>.Empty;

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map = map.AddOrUpdate(v + a, v);
                }

                return map;
            }

            public ImToolsV3.ImHashMap<string, string>[] V3_PartitionedHashMap_23Tree_AddOrUpdate()
            {
                var map = ImToolsV3.PartitionedHashMap.CreateEmpty<string, string>();

                for (var i = 0; i < Count; ++i)
                {
                    var a = i.ToString();
                    var v = a + Seed;
                    map.AddOrUpdate(v + a, v);
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
            public string V2_ImHashMap_AVL_TryFind()
            {
                _map.TryFind(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string V3_ImHashMap_23Tree_TryFind()
            {
                _map234.TryFind(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string V3_PartitionedHashMap_23Tree_TryFind()
            {
                _partMap234.TryFind(LookupKey, out var result);
                return result;
            }

            // [Benchmark]
            public string ImHashMap_V1_TryFind()
            {
                _mapV1.TryFind(LookupKey, out var result);
                return result;
            }

            // [Benchmark]
            public string ImHashMapSlots_TryFind()
            {
                var hash = LookupKey.GetHashCode();
                _mapSlots[hash & ImHashMapSlots.HASH_MASK_TO_FIND_SLOT].TryFind(hash, LookupKey, out var result);
                return result;
            }

            // [Benchmark]
            public string DictionarySlim_TryGetValue()
            {
                _dictSlim.TryGetValue(LookupKey, out var result);
                return result;
            }

            // [Benchmark]
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

            // [Benchmark]
            public string ImmutableDictionary_TryGet()
            {
                _immutableDict.TryGetValue(LookupKey, out var result);
                return result;
            }
        }
    }
}
