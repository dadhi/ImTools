using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ImTools;
using Microsoft.Collections.Extensions;

namespace Playground
{
    public class ImHashMapBenchmarks_StringString
    {
        private static readonly Type[] _keys = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

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

        [MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Lookup
        {
            /*
## 21.01.2019: All versions.

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.74 ns | 0.0686 ns | 0.0642 ns |  0.79 |           - |           - |           - |                   - |
    GetValueOrDefault | 17.43 ns | 0.0924 ns | 0.0864 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.15 ns | 0.0786 ns | 0.0656 ns |  1.10 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.73 ns | 0.0711 ns | 0.0665 ns |  1.48 |           - |           - |           - |                   - |

## For some reason dropping lookup speed with only changes to AddOrUpdate

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.89 ns | 0.0938 ns | 0.0877 ns |  0.80 |           - |           - |           - |                   - |
    GetValueOrDefault | 17.40 ns | 0.0888 ns | 0.0831 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.04 ns | 0.0712 ns | 0.0666 ns |  1.09 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.93 ns | 0.0474 ns | 0.0420 ns |  1.49 |           - |           - |           - |                   - |

## Got back some perf by moving GetValueOrDefault to static method and specializing for Type

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.87 ns | 0.0400 ns | 0.0355 ns |  0.85 |           - |           - |           - |                   - |
    GetValueOrDefault | 16.34 ns | 0.0932 ns | 0.0826 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.18 ns | 0.0460 ns | 0.0430 ns |  1.17 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.96 ns | 0.0756 ns | 0.0707 ns |  1.59 |           - |           - |           - |                   - |

## Benchmark against variety of inputs on par with Populate benchmark

               Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 |     5 |  6.155 ns | 0.0321 ns | 0.0301 ns |  0.98 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |     5 |  6.267 ns | 0.0510 ns | 0.0452 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |     5 |  7.439 ns | 0.0763 ns | 0.0676 ns |  1.19 |    0.02 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |     5 |  9.558 ns | 0.0409 ns | 0.0383 ns |  1.52 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |    40 | 10.897 ns | 0.0673 ns | 0.0629 ns |  0.95 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |    40 | 11.467 ns | 0.0325 ns | 0.0304 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |    40 | 14.012 ns | 0.1092 ns | 0.1022 ns |  1.22 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |    40 | 19.945 ns | 0.1032 ns | 0.0965 ns |  1.74 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |   200 | 13.664 ns | 0.0291 ns | 0.0258 ns |  0.97 |    0.00 |           - |           - |           - |                   - |
    GetValueOrDefault |   200 | 14.051 ns | 0.0524 ns | 0.0491 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |   200 | 16.722 ns | 0.0568 ns | 0.0531 ns |  1.19 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |   200 | 24.473 ns | 0.0792 ns | 0.0702 ns |  1.74 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |  1000 | 14.213 ns | 0.1528 ns | 0.1354 ns |  0.96 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |  1000 | 14.805 ns | 0.0518 ns | 0.0485 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |  1000 | 16.645 ns | 0.0447 ns | 0.0419 ns |  1.12 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |  1000 | 27.489 ns | 0.0890 ns | 0.0832 ns |  1.86 |    0.01 |           - |           - |           - |                   - |

## Adding aggressive inlining to the Data { Hash, Key, Value } properties

               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  5.853 ns | 0.0685 ns | 0.0607 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  5.913 ns | 0.0373 ns | 0.0349 ns |  1.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 |  9.649 ns | 0.0235 ns | 0.0220 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.266 ns | 0.0236 ns | 0.0221 ns |  1.06 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 11.613 ns | 0.0554 ns | 0.0491 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.052 ns | 0.0555 ns | 0.0520 ns |  1.04 |           - |           - |           - |                   - |

## Using  `!= Empty` instead of `.Height != 0` drops some perf

               Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 |     5 |  5.933 ns | 0.0310 ns | 0.0290 ns |  0.93 |    0.03 |           - |           - |           - |                   - |
    GetValueOrDefault |     5 |  6.386 ns | 0.1807 ns | 0.1602 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
    GetValueOrDefault |    40 |  9.820 ns | 0.0521 ns | 0.0488 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.257 ns | 0.0300 ns | 0.0266 ns |  1.05 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
    GetValueOrDefault |   200 | 11.717 ns | 0.0689 ns | 0.0644 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.104 ns | 0.0548 ns | 0.0486 ns |  1.03 |    0.01 |           - |           - |           - |                   - |

## Removing `.Height != 0` check completely did not change much, but let it stay cause less code is better

               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  5.903 ns | 0.0612 ns | 0.0573 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  5.931 ns | 0.0503 ns | 0.0470 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 |  9.636 ns | 0.0419 ns | 0.0392 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.231 ns | 0.0333 ns | 0.0312 ns |  1.06 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 11.637 ns | 0.0721 ns | 0.0602 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.042 ns | 0.0607 ns | 0.0568 ns |  1.03 |           - |           - |           - |                   - |

## TryFind

             Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ----------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
         TryFind_v1 |     5 |  5.229 ns | 0.0307 ns | 0.0257 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |     5 |  6.766 ns | 0.0695 ns | 0.0650 ns |  1.30 |           - |           - |           - |                   - |
                    |       |           |           |           |       |             |             |             |                     |
         TryFind_v1 |    40 |  9.268 ns | 0.0116 ns | 0.0108 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |    40 |  9.755 ns | 0.0219 ns | 0.0205 ns |  1.05 |           - |           - |           - |                   - |
                    |       |           |           |           |       |             |             |             |                     |
         TryFind_v1 |   200 | 11.773 ns | 0.0558 ns | 0.0494 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |   200 | 12.212 ns | 0.0456 ns | 0.0380 ns |  1.04 |           - |           - |           - |                   - |

     Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
----------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    TryFind |     5 |  5.906 ns | 0.0191 ns | 0.0178 ns |  0.97 |           - |           - |           - |                   - |
 TryFind_v1 |     5 |  6.079 ns | 0.0947 ns | 0.0839 ns |  1.00 |           - |           - |           - |                   - |
            |       |           |           |           |       |             |             |             |                     |
    TryFind |    40 |  9.211 ns | 0.0214 ns | 0.0200 ns |  0.87 |           - |           - |           - |                   - |
 TryFind_v1 |    40 | 10.566 ns | 0.0149 ns | 0.0132 ns |  1.00 |           - |           - |           - |                   - |
            |       |           |           |           |       |             |             |             |                     |
    TryFind |   200 | 11.400 ns | 0.1152 ns | 0.1078 ns |  0.88 |           - |           - |           - |                   - |
 TryFind_v1 |   200 | 12.929 ns | 0.0712 ns | 0.0666 ns |  1.00 |           - |           - |           - |                   - |

    ## GetOrDefault a bit optimized
    
               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  6.782 ns | 0.0449 ns | 0.0398 ns |  0.96 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  7.042 ns | 0.0659 ns | 0.0616 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 | 10.962 ns | 0.0866 ns | 0.0768 ns |  0.99 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 11.094 ns | 0.0973 ns | 0.0813 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 13.329 ns | 0.0338 ns | 0.0299 ns |  0.97 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 13.722 ns | 0.0537 ns | 0.0448 ns |  1.00 |           - |           - |           - |                   - |

    ## The whole result for the docs

                Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
---------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
            TryFind_v1 |    10 |  7.274 ns | 0.0410 ns | 0.0384 ns |  0.98 |    0.01 |           - |           - |           - |                   - |
               TryFind |    10 |  7.422 ns | 0.0237 ns | 0.0222 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |    10 | 21.664 ns | 0.0213 ns | 0.0189 ns |  2.92 |    0.01 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |    10 | 71.199 ns | 0.1312 ns | 0.1228 ns |  9.59 |    0.03 |           - |           - |           - |                   - |
                       |       |           |           |           |       |         |             |             |             |                     |
            TryFind_v1 |   100 |  8.426 ns | 0.0236 ns | 0.0221 ns |  0.91 |    0.00 |           - |           - |           - |                   - |
               TryFind |   100 |  9.304 ns | 0.0305 ns | 0.0270 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |   100 | 21.791 ns | 0.1072 ns | 0.0951 ns |  2.34 |    0.01 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |   100 | 74.985 ns | 0.1053 ns | 0.0879 ns |  8.06 |    0.03 |           - |           - |           - |                   - |
                       |       |           |           |           |       |         |             |             |             |                     |
               TryFind |  1000 | 13.837 ns | 0.0291 ns | 0.0272 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
            TryFind_v1 |  1000 | 16.108 ns | 0.0415 ns | 0.0367 ns |  1.16 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |  1000 | 21.876 ns | 0.0325 ns | 0.0288 ns |  1.58 |    0.00 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |  1000 | 83.563 ns | 0.1046 ns | 0.0873 ns |  6.04 |    0.01 |           - |           - |           - |                   - |


    ## 2019-03-28: Comparing vs `Dictionary<K, V>`:

                           Method | Count |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------------------- |------ |----------:|----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                          TryFind |    10 |  7.722 ns | 0.0451 ns | 0.0422 ns |  7.718 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |    10 | 18.475 ns | 0.0502 ns | 0.0470 ns | 18.470 ns |  2.39 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |    10 | 22.661 ns | 0.0463 ns | 0.0433 ns | 22.653 ns |  2.93 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |    10 | 72.911 ns | 1.5234 ns | 2.1355 ns | 74.134 ns |  9.25 |    0.23 |           - |           - |           - |                   - |
                                  |       |           |           |           |           |       |         |             |             |             |                     |
                          TryFind |   100 |  9.987 ns | 0.0543 ns | 0.0508 ns |  9.978 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |   100 | 18.110 ns | 0.0644 ns | 0.0602 ns | 18.088 ns |  1.81 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |   100 | 24.402 ns | 0.0978 ns | 0.0915 ns | 24.435 ns |  2.44 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |   100 | 76.689 ns | 0.3632 ns | 0.3397 ns | 76.704 ns |  7.68 |    0.04 |           - |           - |           - |                   - |
                                  |       |           |           |           |           |       |         |             |             |             |                     |
                          TryFind |  1000 | 12.600 ns | 0.1506 ns | 0.1335 ns | 12.551 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |  1000 | 19.023 ns | 0.0575 ns | 0.0538 ns | 19.036 ns |  1.51 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |  1000 | 22.651 ns | 0.1238 ns | 0.1097 ns | 22.613 ns |  1.80 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |  1000 | 83.608 ns | 0.3105 ns | 0.2904 ns | 83.612 ns |  6.64 |    0.07 |           - |           - |           - |                   - |

    ## 2019-03-29: Comparing vs `DictionarySlim<K, V>`:

                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
       DictionarySlim_TryGetValue |    10 |  8.228 ns | 0.0682 ns | 0.0604 ns |  1.00 |    0.01 |           - |           - |           - |                   - |
                          TryFind |    10 |  8.257 ns | 0.0796 ns | 0.0706 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |    10 | 19.615 ns | 0.0251 ns | 0.0209 ns |  2.38 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |    10 | 22.339 ns | 0.0922 ns | 0.0863 ns |  2.71 |    0.03 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |    10 | 69.872 ns | 0.2699 ns | 0.2524 ns |  8.46 |    0.07 |           - |           - |           - |                   - |
                                  |       |           |           |           |       |         |             |             |             |                     |
       DictionarySlim_TryGetValue |   100 |  8.351 ns | 0.1613 ns | 0.1508 ns |  0.69 |    0.01 |           - |           - |           - |                   - |
                          TryFind |   100 | 12.144 ns | 0.0570 ns | 0.0533 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |   100 | 17.985 ns | 0.0880 ns | 0.0823 ns |  1.48 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |   100 | 22.312 ns | 0.0564 ns | 0.0471 ns |  1.84 |    0.01 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |   100 | 75.374 ns | 0.3042 ns | 0.2846 ns |  6.21 |    0.04 |           - |           - |           - |                   - |
                                  |       |           |           |           |       |         |             |             |             |                     |
       DictionarySlim_TryGetValue |  1000 |  8.202 ns | 0.0713 ns | 0.0667 ns |  0.55 |    0.01 |           - |           - |           - |                   - |
                          TryFind |  1000 | 14.919 ns | 0.1101 ns | 0.0919 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |  1000 | 18.073 ns | 0.2415 ns | 0.2141 ns |  1.21 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |  1000 | 22.406 ns | 0.1039 ns | 0.0921 ns |  1.50 |    0.01 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |  1000 | 84.215 ns | 0.2835 ns | 0.2513 ns |  5.65 |    0.04 |           - |           - |           - |                   - |

*/
            [Params(10, 100, 1000)]// the 1000 does not add anything as the LookupKey stored higher in the tree, 1000)]
            public int Count;

            [GlobalSetup]
            public void Populate()
            {
                _map = AddOrUpdate();
                _mapV1 = AddOrUpdate_v1();
                //_mapV2 = AddOrUpdate_v2();
                //_mapV3 = AddOrUpdate_v3();
                _dict = Dict();
                _dictSlim = DictSlim();
                _concurrentDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            public ImHashMap<Type, string> AddOrUpdate()
            {
                var map = ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key, "a", out _, out _);

                map = map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!", out _, out _);

                return map;
            }

            private ImHashMap<Type, string> _map;

            public V1.ImHashMap<Type, string> AddOrUpdate_v1()
            {
                var map = V1.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key, "a");

                map = map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");

                return map;
            }

            private V1.ImHashMap<Type, string> _mapV1;

            public Dictionary<Type, string> Dict()
            {
                var map = new Dictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");

                return map;
            }

            public struct TypeVal : IEquatable<TypeVal>
            {
                public static implicit operator TypeVal(Type t) => new TypeVal(t);

                public readonly Type Type;
                public TypeVal(Type type) => Type = type;
                public bool Equals(TypeVal other) => Type == other.Type;
                public override bool Equals(object obj) => !ReferenceEquals(null, obj) && obj is TypeVal other && Equals(other);
                public override int GetHashCode() => Type.GetHashCode();
            }

            private Dictionary<Type, string> _dict;

            public DictionarySlim<TypeVal, string> DictSlim()
            {
                var dict = new DictionarySlim<TypeVal, string>();

                foreach (var key in _keys.Take(Count))
                    dict.GetOrAddValueRef(key) = "a";

                dict.GetOrAddValueRef(typeof(ImHashMapBenchmarks)) = "!";

                return dict;
            }

            private DictionarySlim<TypeVal, string> _dictSlim;

            public ConcurrentDictionary<Type, string> ConcurrentDict()
            {
                var map = new ConcurrentDictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");

                return map;
            }

            private ConcurrentDictionary<Type, string> _concurrentDict;

            public ImmutableDictionary<Type, string> ImmutableDict()
            {
                var map = ImmutableDictionary<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.Add(key, "a");

                return map.Add(typeof(ImHashMapBenchmarks), "!");
            }

            private ImmutableDictionary<Type, string> _immutableDict;

            public static Type LookupKey = typeof(ImHashMapBenchmarks);

            //[Benchmark]
            public string TryFind_v1()
            {
                _mapV1.TryFind(LookupKey, out var result);
                return result;
            }

            [Benchmark(Baseline = true)]
            public string TryFind()
            {
                _map.TryFind(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string Dictionary_TryGetValue()
            {
                _dict.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string DictionarySlim_TryGetValue()
            {
                _dictSlim.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ConcurrentDictionary_TryGetValue()
            {
                _concurrentDict.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ImmutableDict_TryGet()
            {
                _immutableDict.TryGetValue(LookupKey, out var result);
                return result;
            }

            //[Benchmark]
            //public string TryFind_v2()
            //{
            //    V2.ImHashMap.TryFind(_mapV2, LookupKey, out var result);
            //    return result;
            //}

            //[Benchmark]
            //public string GetValueOrDefault() => _map.GetValueOrDefault(LookupKey);

            //[Benchmark(Baseline = true)]
            //public string GetValueOrDefault_v1() => _mapV1.GetValueOrDefault(LookupKey);

            //[Benchmark]
            //public string GetValueOrDefault_v2() => V2.ImHashMap.GetValueOrDefault(_mapV2, LookupKey);

            //[Benchmark]
            //public string GetValueOrDefault_v3() => V3.ImHashMap.GetValueOrDefault(_mapV3, LookupKey);

            //[Benchmark]
            //public string ConcurrentDict_TryGet()
            //{
            //    _concurDict.TryGetValue(LookupKey, out var result);
            //    return result;
            //}

            //public string ImmutableDict_TryGet()
            //{
            //    _immutableDict.TryGetValue(LookupKey, out var result);
            //    return result;
            //}
        }
    }
}
