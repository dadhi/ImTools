# ImTools

[![license](https://img.shields.io/github/license/dadhi/ImTools.svg)](http://opensource.org/licenses/MIT)

- Windows: [![Windows build](https://ci.appveyor.com/api/projects/status/el9echuqfnl86u53?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/imtools/branch/master)
- Linux, MacOS: [![Linux build](https://travis-ci.org/dadhi/ImTools.svg?branch=master)](https://travis-ci.org/dadhi/ImTools)

- Lib package: [![NuGet Badge](https://buildstats.info/nuget/ImTools.dll)](https://www.nuget.org/packages/ImTools.dll)
- Code package: [![NuGet Badge](https://buildstats.info/nuget/ImTools)](https://www.nuget.org/packages/ImTools)

Immutable persistent collections, Ref, and Array helpers designed for performance.

Split from [DryIoc](https://github.com/dadhi/dryioc).


## Benchmarks

The comparison is done against the `ImMap` V1 version, the V2 `ImMapSlots` bucketed version 
and a variety of BCL C# collections including the experimental `Microsoft.Collections.Extensions.DictionarySlim<K, V>`.

__Note:__ Keep in mind that immutable collections have a different use-case and a thread-safety guarantees compared to the 
`Dictionary`, `DictionarySlim` or even `ConcurrentDictionary`. The closest comparable would be the `ImmutableDictionary`. 
The benchmarks do not take the collection "nature" into account and run though a simplest available API path.

*Benchmark environment*:

```
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
```


### ImMap V2 with string values

`ImMap<string>` stores the `int` keys and `string` values.


#### ImMap Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) inserts from 10 to 100 000 `Count` of items into the `ImMap<string>`, 
where value is `i.ToString()`:

```md
|                    Method |  Count |             Mean |           Error |          StdDev |           Median | Ratio | RatioSD |      Gen 0 |     Gen 1 |    Gen 2 |   Allocated |
|-------------------------- |------- |-----------------:|----------------:|----------------:|-----------------:|------:|--------:|-----------:|----------:|---------:|------------:|
|         ImMap_AddOrUpdate |     10 |         652.4 ns |         3.28 ns |         2.91 ns |         651.9 ns |  1.00 |    0.00 |     0.3767 |    0.0010 |        - |      1776 B |
|      ImMap_V1_AddOrUpdate |     10 |         795.7 ns |         2.82 ns |         2.50 ns |         795.8 ns |  1.22 |    0.01 |     0.4787 |    0.0010 |        - |      2256 B |
|    ImMapSlots_AddOrUpdate |     10 |         401.3 ns |         2.37 ns |         2.10 ns |         401.5 ns |  0.62 |    0.00 |     0.1612 |    0.0005 |        - |       760 B |
| DictSlim_GetOrAddValueRef |     10 |         388.4 ns |         0.98 ns |         0.82 ns |         388.3 ns |  0.60 |    0.00 |     0.1764 |         - |        - |       832 B |
|               Dict_TryAdd |     10 |         398.8 ns |         1.85 ns |         1.55 ns |         398.5 ns |  0.61 |    0.00 |     0.2089 |    0.0010 |        - |       984 B |
|     ConcurrentDict_TryAdd |     10 |         699.8 ns |         1.84 ns |         1.63 ns |         700.1 ns |  1.07 |    0.01 |     0.2613 |    0.0019 |        - |      1232 B |
|         ImmutableDict_Add |     10 |       3,878.8 ns |        15.99 ns |        14.95 ns |       3,879.8 ns |  5.95 |    0.04 |     0.5569 |         - |        - |      2640 B |
|                           |        |                  |                 |                 |                  |       |         |            |           |          |             |
|         ImMap_AddOrUpdate |    100 |      12,594.5 ns |        28.34 ns |        26.51 ns |      12,586.5 ns |  1.00 |    0.00 |     7.9193 |    0.3204 |        - |     37296 B |
|      ImMap_V1_AddOrUpdate |    100 |      15,349.8 ns |       296.55 ns |       415.73 ns |      15,216.9 ns |  1.21 |    0.04 |     9.3689 |    0.3967 |        - |     44112 B |
|    ImMapSlots_AddOrUpdate |    100 |       5,378.8 ns |       107.28 ns |       100.35 ns |       5,327.8 ns |  0.43 |    0.01 |     2.7466 |    0.1678 |        - |     12952 B |
| DictSlim_GetOrAddValueRef |    100 |       3,798.2 ns |        72.76 ns |       104.35 ns |       3,799.4 ns |  0.30 |    0.01 |     1.7700 |    0.0687 |        - |      8336 B |
|               Dict_TryAdd |    100 |       4,617.8 ns |        76.04 ns |        71.13 ns |       4,640.5 ns |  0.37 |    0.01 |     2.7695 |    0.1678 |        - |     13064 B |
|     ConcurrentDict_TryAdd |    100 |      12,917.6 ns |       250.45 ns |       257.19 ns |      12,873.7 ns |  1.03 |    0.02 |     4.8370 |    0.4730 |        - |     22768 B |
|         ImmutableDict_Add |    100 |      70,576.4 ns |       435.56 ns |       407.42 ns |      70,580.4 ns |  5.60 |    0.04 |    10.4980 |    0.4883 |        - |     49952 B |
|                           |        |                  |                 |                 |                  |       |         |            |           |          |             |
|         ImMap_AddOrUpdate |   1000 |     217,216.1 ns |     2,476.56 ns |     2,195.40 ns |     217,049.1 ns |  1.00 |    0.00 |   113.5254 |    0.2441 |        - |    534144 B |
|      ImMap_V1_AddOrUpdate |   1000 |     259,238.0 ns |     3,215.68 ns |     2,850.62 ns |     259,358.2 ns |  1.19 |    0.02 |   128.4180 |    0.4883 |        - |    605616 B |
|    ImMapSlots_AddOrUpdate |   1000 |     133,135.1 ns |     1,093.83 ns |     1,023.17 ns |     133,157.2 ns |  0.61 |    0.01 |    61.2793 |    0.4883 |        - |    289240 B |
| DictSlim_GetOrAddValueRef |   1000 |      36,054.8 ns |       702.73 ns |       721.65 ns |      35,880.4 ns |  0.17 |    0.00 |    15.5029 |    0.0610 |        - |     73120 B |
|               Dict_TryAdd |   1000 |      47,414.9 ns |       936.41 ns |     1,217.60 ns |      47,383.6 ns |  0.22 |    0.01 |    28.2593 |    0.1831 |        - |    133888 B |
|     ConcurrentDict_TryAdd |   1000 |     134,020.9 ns |     2,526.01 ns |     2,702.80 ns |     133,616.0 ns |  0.62 |    0.02 |    43.2129 |   14.4043 |        - |    205328 B |
|         ImmutableDict_Add |   1000 |   1,025,570.3 ns |    15,669.09 ns |    14,656.87 ns |   1,023,279.7 ns |  4.72 |    0.10 |   150.3906 |    1.9531 |        - |    710219 B |
|                           |        |                  |                 |                 |                  |       |         |            |           |          |             |
|         ImMap_AddOrUpdate |  10000 |   4,971,087.3 ns |    97,077.65 ns |    86,056.81 ns |   4,953,174.6 ns |  1.00 |    0.00 |  1109.3750 |  234.3750 | 101.5625 |   6972716 B |
|      ImMap_V1_AddOrUpdate |  10000 |   5,452,254.6 ns |   119,956.65 ns |   123,186.65 ns |   5,415,396.9 ns |  1.10 |    0.04 |  1226.5625 |  226.5625 | 101.5625 |   7691952 B |
|    ImMapSlots_AddOrUpdate |  10000 |   4,520,861.1 ns |    90,114.83 ns |   162,495.63 ns |   4,521,816.4 ns |  0.93 |    0.02 |   726.5625 |  273.4375 | 125.0000 |   4562392 B |
| DictSlim_GetOrAddValueRef |  10000 |     476,648.3 ns |    11,050.99 ns |    23,067.53 ns |     465,925.3 ns |  0.10 |    0.01 |   124.5117 |  124.5117 | 124.5117 |    975712 B |
|               Dict_TryAdd |  10000 |     584,457.6 ns |     6,522.05 ns |     6,100.73 ns |     581,238.8 ns |  0.12 |    0.00 |   221.6797 |  221.6797 | 221.6797 |   1261681 B |
|     ConcurrentDict_TryAdd |  10000 |   2,976,093.4 ns |    42,928.58 ns |    40,155.42 ns |   2,968,460.9 ns |  0.60 |    0.02 |   277.3438 |  125.0000 |  42.9688 |   1645264 B |
|         ImmutableDict_Add |  10000 |  14,789,400.6 ns |    45,249.93 ns |    42,326.81 ns |  14,787,257.8 ns |  2.98 |    0.05 |  1468.7500 |  281.2500 | 125.0000 |   9271306 B |
|                           |        |                  |                 |                 |                  |       |         |            |           |          |             |
|         ImMap_AddOrUpdate | 100000 |  63,407,185.2 ns |   487,704.12 ns |   456,198.74 ns |  63,302,922.2 ns |  1.00 |    0.00 | 14222.2222 | 2000.0000 | 555.5556 |  85708139 B |
|      ImMap_V1_AddOrUpdate | 100000 |  67,996,027.9 ns |   459,484.36 ns |   383,690.25 ns |  67,923,075.0 ns |  1.07 |    0.01 | 15250.0000 | 2000.0000 | 500.0000 |  92907056 B |
|    ImMapSlots_AddOrUpdate | 100000 |  63,343,007.0 ns | 1,200,993.46 ns | 1,179,536.17 ns |  62,773,943.8 ns |  1.00 |    0.02 | 10375.0000 | 1875.0000 | 500.0000 |  61692387 B |
| DictSlim_GetOrAddValueRef | 100000 |   9,176,031.6 ns |    85,322.89 ns |    79,811.08 ns |   9,171,034.4 ns |  0.14 |    0.00 |  1187.5000 |  812.5000 | 781.2500 |   8443549 B |
|               Dict_TryAdd | 100000 |  10,539,553.2 ns |   141,171.29 ns |   132,051.71 ns |  10,540,217.2 ns |  0.17 |    0.00 |   906.2500 |  640.6250 | 500.0000 |  11652128 B |
|     ConcurrentDict_TryAdd | 100000 |  34,202,418.3 ns |   570,236.69 ns |   533,399.76 ns |  34,138,325.0 ns |  0.54 |    0.01 |  2500.0000 | 1125.0000 | 437.5000 |  15066715 B |
|         ImmutableDict_Add | 100000 | 194,366,442.2 ns | 1,505,482.08 ns | 1,408,228.88 ns | 194,159,900.0 ns |  3.07 |    0.03 | 18666.6667 | 2666.6667 | 666.6667 | 114011384 B |
```


### ImMap Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) lookups for the last added index in the `ImMap<string>` 
containing the specified Count of elements.

```md
|                     Method |  Count |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------- |------- |-----------:|----------:|----------:|-----------:|------:|--------:|------:|------:|------:|----------:|
|              ImMap_TryFind |     10 |  3.8436 ns | 0.0386 ns | 0.0342 ns |  3.8351 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|           ImMap_V1_TryFind |     10 |  5.3729 ns | 0.0377 ns | 0.0352 ns |  5.3724 ns |  1.40 |    0.02 |     - |     - |     - |         - |
|         ImMapSlots_TryFind |     10 |  0.8070 ns | 0.0232 ns | 0.0217 ns |  0.8119 ns |  0.21 |    0.01 |     - |     - |     - |         - |
|       DictSlim_TryGetValue |     10 |  3.6901 ns | 0.0337 ns | 0.0281 ns |  3.6831 ns |  0.96 |    0.01 |     - |     - |     - |         - |
|           Dict_TryGetValue |     10 |  7.1958 ns | 0.0309 ns | 0.0289 ns |  7.1921 ns |  1.87 |    0.02 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue |     10 |  9.7863 ns | 0.0568 ns | 0.0444 ns |  9.7823 ns |  2.54 |    0.03 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue |     10 | 20.6732 ns | 0.2977 ns | 0.2784 ns | 20.6110 ns |  5.38 |    0.09 |     - |     - |     - |         - |
|                            |        |            |           |           |            |       |         |       |       |       |           |
|              ImMap_TryFind |    100 |  6.2190 ns | 0.0649 ns | 0.0542 ns |  6.2062 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|           ImMap_V1_TryFind |    100 |  6.8624 ns | 0.1311 ns | 0.1162 ns |  6.8656 ns |  1.11 |    0.02 |     - |     - |     - |         - |
|         ImMapSlots_TryFind |    100 |  2.7313 ns | 0.0336 ns | 0.0297 ns |  2.7357 ns |  0.44 |    0.01 |     - |     - |     - |         - |
|       DictSlim_TryGetValue |    100 |  3.1918 ns | 0.0237 ns | 0.0221 ns |  3.1894 ns |  0.51 |    0.01 |     - |     - |     - |         - |
|           Dict_TryGetValue |    100 |  7.2285 ns | 0.0253 ns | 0.0237 ns |  7.2352 ns |  1.16 |    0.01 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue |    100 | 10.1866 ns | 0.0643 ns | 0.0537 ns | 10.1798 ns |  1.64 |    0.02 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue |    100 | 20.8152 ns | 0.2893 ns | 0.2706 ns | 20.9723 ns |  3.34 |    0.05 |     - |     - |     - |         - |
|                            |        |            |           |           |            |       |         |       |       |       |           |
|              ImMap_TryFind |   1000 |  8.5507 ns | 0.0375 ns | 0.0332 ns |  8.5490 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|           ImMap_V1_TryFind |   1000 | 10.3435 ns | 0.0414 ns | 0.0346 ns | 10.3516 ns |  1.21 |    0.01 |     - |     - |     - |         - |
|         ImMapSlots_TryFind |   1000 |  5.8184 ns | 0.0804 ns | 0.0752 ns |  5.7935 ns |  0.68 |    0.01 |     - |     - |     - |         - |
|       DictSlim_TryGetValue |   1000 |  3.1840 ns | 0.0463 ns | 0.0410 ns |  3.1730 ns |  0.37 |    0.00 |     - |     - |     - |         - |
|           Dict_TryGetValue |   1000 |  7.1791 ns | 0.0199 ns | 0.0155 ns |  7.1765 ns |  0.84 |    0.00 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue |   1000 | 10.0029 ns | 0.0791 ns | 0.0701 ns | 10.0077 ns |  1.17 |    0.01 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue |   1000 | 24.9425 ns | 0.4443 ns | 0.3710 ns | 24.8084 ns |  2.92 |    0.04 |     - |     - |     - |         - |
|                            |        |            |           |           |            |       |         |       |       |       |           |
|              ImMap_TryFind |  10000 | 11.8846 ns | 0.0854 ns | 0.0757 ns | 11.8690 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|           ImMap_V1_TryFind |  10000 | 13.3645 ns | 0.3396 ns | 0.4295 ns | 13.0776 ns |  1.13 |    0.04 |     - |     - |     - |         - |
|         ImMapSlots_TryFind |  10000 |  6.5400 ns | 0.0343 ns | 0.0320 ns |  6.5340 ns |  0.55 |    0.01 |     - |     - |     - |         - |
|       DictSlim_TryGetValue |  10000 |  3.1647 ns | 0.0111 ns | 0.0104 ns |  3.1631 ns |  0.27 |    0.00 |     - |     - |     - |         - |
|           Dict_TryGetValue |  10000 |  7.1865 ns | 0.0240 ns | 0.0224 ns |  7.1884 ns |  0.60 |    0.00 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue |  10000 | 11.8437 ns | 0.0355 ns | 0.0314 ns | 11.8458 ns |  1.00 |    0.01 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue |  10000 | 31.1103 ns | 0.1483 ns | 0.1387 ns | 31.1190 ns |  2.62 |    0.02 |     - |     - |     - |         - |
|                            |        |            |           |           |            |       |         |       |       |       |           |
|              ImMap_TryFind | 100000 | 16.0680 ns | 0.0608 ns | 0.0508 ns | 16.0775 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|           ImMap_V1_TryFind | 100000 | 18.5834 ns | 0.1152 ns | 0.1077 ns | 18.5589 ns |  1.16 |    0.01 |     - |     - |     - |         - |
|         ImMapSlots_TryFind | 100000 | 11.4485 ns | 0.0339 ns | 0.0317 ns | 11.4467 ns |  0.71 |    0.00 |     - |     - |     - |         - |
|       DictSlim_TryGetValue | 100000 |  3.9245 ns | 0.0289 ns | 0.0271 ns |  3.9273 ns |  0.24 |    0.00 |     - |     - |     - |         - |
|           Dict_TryGetValue | 100000 |  7.1091 ns | 0.0330 ns | 0.0293 ns |  7.1053 ns |  0.44 |    0.00 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue | 100000 | 11.0017 ns | 0.0565 ns | 0.0529 ns | 11.0147 ns |  0.68 |    0.00 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue | 100000 | 35.4956 ns | 0.1117 ns | 0.1045 ns | 35.4829 ns |  2.21 |    0.01 |     - |     - |     - |         - |
```

**Interpreting results:** `ImMap` holds very good against `ImmutableDictionary` sibling and even against `Dictionary`(s) up to certain count, 
indicating that immutable collection could be quite fast for lookups.

### ImHashMap of Type keys and String values

#### ImHashMap Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs) inserts from 10 to 1000
items into the `ImHashMap<Type, string>`:

```md
|                                          Method | Count |           Mean |        Error |       StdDev | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|------------------------------------------------ |------ |---------------:|-------------:|-------------:|------:|--------:|---------:|--------:|------:|----------:|
|                           ImHashMap_AddOrUpdate |    10 |       925.9 ns |      5.79 ns |      4.83 ns |  1.00 |    0.00 |   0.5178 |       - |     - |   2.39 KB |
|                        ImHashMap_V1_AddOrUpdate |    10 |     1,141.3 ns |     22.43 ns |     34.25 ns |  1.22 |    0.05 |   0.6199 |       - |     - |   2.86 KB |
| ImHashMap_AddOrUpdate_RuntimeHelpersGetHashCode |    10 |       928.6 ns |     17.48 ns |     15.50 ns |  1.01 |    0.02 |   0.5178 |       - |     - |   2.39 KB |
|                      ImHashMapSlots_AddOrUpdate |    10 |       716.7 ns |      7.02 ns |      6.57 ns |  0.77 |    0.01 |   0.3128 |       - |     - |   1.45 KB |
|                                 DictSlim_TryAdd |    10 |       669.6 ns |     12.83 ns |     11.38 ns |  0.72 |    0.01 |   0.2365 |       - |     - |   1.09 KB |
|                                     Dict_TryAdd |    10 |       718.3 ns |      4.73 ns |      4.42 ns |  0.78 |    0.01 |   0.2203 |       - |     - |   1.02 KB |
|                           ConcurrentDict_TryAdd |    10 |     2,047.2 ns |     13.82 ns |     12.25 ns |  2.21 |    0.02 |   0.6256 |       - |     - |    2.9 KB |
|                               ImmutableDict_Add |    10 |     6,583.6 ns |     63.69 ns |     56.46 ns |  7.11 |    0.06 |   0.6180 |       - |     - |   2.85 KB |
|                                                 |       |                |              |              |       |         |          |         |       |           |
|                           ImHashMap_AddOrUpdate |   100 |    13,548.4 ns |    468.82 ns |    501.63 ns |  1.00 |    0.00 |   7.7057 |       - |     - |  35.53 KB |
|                        ImHashMap_V1_AddOrUpdate |   100 |    15,317.1 ns |    171.72 ns |    143.40 ns |  1.12 |    0.04 |   8.5144 |       - |     - |  39.38 KB |
| ImHashMap_AddOrUpdate_RuntimeHelpersGetHashCode |   100 |    13,603.6 ns |    432.12 ns |    383.06 ns |  1.00 |    0.04 |   7.7057 |       - |     - |  35.53 KB |
|                      ImHashMapSlots_AddOrUpdate |   100 |     7,483.0 ns |     57.95 ns |     54.21 ns |  0.55 |    0.02 |   3.2883 |       - |     - |  15.18 KB |
|                                 DictSlim_TryAdd |   100 |     4,606.7 ns |     89.29 ns |    102.82 ns |  0.34 |    0.01 |   1.5945 |       - |     - |   7.36 KB |
|                                     Dict_TryAdd |   100 |     5,743.2 ns |     94.12 ns |     78.59 ns |  0.42 |    0.02 |   2.1667 |       - |     - |     10 KB |
|                           ConcurrentDict_TryAdd |   100 |    22,604.8 ns |    384.18 ns |    340.57 ns |  1.66 |    0.05 |   6.5308 |  0.0305 |     - |  30.22 KB |
|                               ImmutableDict_Add |   100 |    97,389.2 ns |  1,844.18 ns |  1,811.23 ns |  7.17 |    0.31 |   9.3994 |       - |     - |  43.74 KB |
|                                                 |       |                |              |              |       |         |          |         |       |           |
|                           ImHashMap_AddOrUpdate |  1000 |   290,967.0 ns |  5,069.44 ns |  4,493.93 ns |  1.00 |    0.00 | 111.8164 |  0.4883 |     - | 515.91 KB |
|                        ImHashMap_V1_AddOrUpdate |  1000 |   321,447.7 ns |  2,152.45 ns |  1,908.09 ns |  1.11 |    0.02 | 120.6055 |  0.4883 |     - | 557.95 KB |
| ImHashMap_AddOrUpdate_RuntimeHelpersGetHashCode |  1000 |   287,636.8 ns |  4,962.32 ns |  4,641.76 ns |  0.99 |    0.02 | 111.8164 |  0.4883 |     - | 515.91 KB |
|                      ImHashMapSlots_AddOrUpdate |  1000 |   171,810.6 ns |  1,909.90 ns |  1,786.52 ns |  0.59 |    0.01 |  60.3027 |  0.2441 |     - | 279.09 KB |
|                                 DictSlim_TryAdd |  1000 |    41,119.7 ns |    640.60 ns |    599.21 ns |  0.14 |    0.00 |  12.2070 |       - |     - |   56.5 KB |
|                                     Dict_TryAdd |  1000 |    56,149.3 ns |    739.99 ns |    692.19 ns |  0.19 |    0.00 |  21.5454 |  0.0610 |     - |  99.87 KB |
|                           ConcurrentDict_TryAdd |  1000 |   233,131.5 ns |  4,540.43 ns |  4,247.12 ns |  0.80 |    0.02 |  49.5605 | 16.3574 |     - | 253.96 KB |
|                               ImmutableDict_Add |  1000 | 1,517,443.9 ns | 20,810.00 ns | 19,465.69 ns |  5.23 |    0.12 | 138.6719 |  1.9531 |     - | 646.65 KB |
```

### ImHashMap Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs) lookups for the specific key in the 
`ImHashMap<Type, string>` containing the specified Count of elements.

```md
|                                      Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                           ImHashMap_TryFind |    10 |  7.291 ns | 0.1047 ns | 0.0875 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                        ImHashMap_TryFind_V1 |    10 |  7.445 ns | 0.0455 ns | 0.0426 ns |  1.02 |    0.01 |     - |     - |     - |         - |
| ImHashMap_TryFind_RuntimeHelpersGetHashCode |    10 |  5.935 ns | 0.0429 ns | 0.0380 ns |  0.81 |    0.01 |     - |     - |     - |         - |
|                      ImHashMapSlots_TryFind |    10 |  3.443 ns | 0.0559 ns | 0.0523 ns |  0.47 |    0.01 |     - |     - |     - |         - |
|                  DictionarySlim_TryGetValue |    10 |  6.498 ns | 0.1152 ns | 0.1077 ns |  0.89 |    0.02 |     - |     - |     - |         - |
|                      Dictionary_TryGetValue |    10 | 16.654 ns | 0.1028 ns | 0.0912 ns |  2.28 |    0.03 |     - |     - |     - |         - |
|            ConcurrentDictionary_TryGetValue |    10 | 15.702 ns | 0.0464 ns | 0.0434 ns |  2.15 |    0.03 |     - |     - |     - |         - |
|                        ImmutableDict_TryGet |    10 | 27.794 ns | 0.2004 ns | 0.1674 ns |  3.81 |    0.04 |     - |     - |     - |         - |
|                                             |       |           |           |           |       |         |       |       |       |           |
|                           ImHashMap_TryFind |   100 |  9.572 ns | 0.0283 ns | 0.0264 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                        ImHashMap_TryFind_V1 |   100 | 10.865 ns | 0.0180 ns | 0.0169 ns |  1.14 |    0.00 |     - |     - |     - |         - |
| ImHashMap_TryFind_RuntimeHelpersGetHashCode |   100 |  9.304 ns | 0.0535 ns | 0.0447 ns |  0.97 |    0.01 |     - |     - |     - |         - |
|                      ImHashMapSlots_TryFind |   100 |  5.527 ns | 0.0400 ns | 0.0355 ns |  0.58 |    0.00 |     - |     - |     - |         - |
|                  DictionarySlim_TryGetValue |   100 |  6.304 ns | 0.0380 ns | 0.0336 ns |  0.66 |    0.00 |     - |     - |     - |         - |
|                      Dictionary_TryGetValue |   100 | 15.800 ns | 0.0720 ns | 0.0638 ns |  1.65 |    0.01 |     - |     - |     - |         - |
|            ConcurrentDictionary_TryGetValue |   100 | 16.689 ns | 0.0731 ns | 0.0648 ns |  1.74 |    0.01 |     - |     - |     - |         - |
|                        ImmutableDict_TryGet |   100 | 30.179 ns | 0.0480 ns | 0.0426 ns |  3.15 |    0.01 |     - |     - |     - |         - |
|                                             |       |           |           |           |       |         |       |       |       |           |
|                           ImHashMap_TryFind |  1000 | 12.817 ns | 0.0572 ns | 0.0507 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                        ImHashMap_TryFind_V1 |  1000 | 13.722 ns | 0.0551 ns | 0.0515 ns |  1.07 |    0.00 |     - |     - |     - |         - |
| ImHashMap_TryFind_RuntimeHelpersGetHashCode |  1000 | 10.649 ns | 0.0394 ns | 0.0329 ns |  0.83 |    0.00 |     - |     - |     - |         - |
|                      ImHashMapSlots_TryFind |  1000 |  8.301 ns | 0.0075 ns | 0.0062 ns |  0.65 |    0.00 |     - |     - |     - |         - |
|                  DictionarySlim_TryGetValue |  1000 |  6.275 ns | 0.0533 ns | 0.0499 ns |  0.49 |    0.00 |     - |     - |     - |         - |
|                      Dictionary_TryGetValue |  1000 | 15.765 ns | 0.0284 ns | 0.0265 ns |  1.23 |    0.00 |     - |     - |     - |         - |
|            ConcurrentDictionary_TryGetValue |  1000 | 15.750 ns | 0.0895 ns | 0.0793 ns |  1.23 |    0.01 |     - |     - |     - |         - |
|                        ImmutableDict_TryGet |  1000 | 33.712 ns | 0.0788 ns | 0.0737 ns |  2.63 |    0.01 |     - |     - |     - |         - |
```
    


## End-to-end Example

Let's assume you are implementing yet another DI container because why not :-)

Container should contain registry of `Type` to `Factory` mappings. 
On resolution `Factory` is compiled to the delegate which you would like to cache, because compilation is costly. 
The cache will store the mappings from `Type` to `Func<object>`.

__The requirements:__

- The container may be used in parallel from different threads including registrations and resolutions. 
- The container state should not be corrupted and the cache should correspond to the current state of registrations.

Let's design the basic container structure to support the requirements and __without locking__:

```cs
    public class Container
    {
        private readonly Ref<Registry> _registry = Ref.Of(new Registry());

        public void Register<TService, TImpl>() where TImpl : TService, new()
        {
            _registry.Swap(reg => reg.With(typeof(TService), new Factory(typeof(TImpl))));
        }

        public object Resolve<TService>()
        {
            return (TService)(_registry.Value.Resolve(typeof(TService)) ?? ThrowUnableToResolve(typeof(TService)));
        }
        
        public object ThrowUnableToResolve(Type t) { throw new InvalidOperationException("Unable to resolve: " + t); }

        class Registry 
        {
            ImHashMap<Type, Factory> _registrations = ImHashMap<Type, Factory>.Empty;
            Ref<ImHashMap<Type, Func<object>>> _resolutionCache = Ref.Of(ImHashMap<Type, Func<object>>.Empty);

            // Creating a new registry with +1 registration and new reference to cache value
            public Registry With(Type serviceType, Factory implFactory)
            {
                return new Registry() 
                {	
                    _registrations = _registrations.AddOrUpdate(serviceType, implFactory),
                        
                    // Here is most interesting part:
                    // We are creating new independent reference pointing to cache value,
                    // isolating it from possible parallel resolutions, 
                    // which will swap older version/ref of cache and won't touch the new one.
                    _resolutionCache = Ref.Of(_resolutionCache.Value)
                };
            }

            public object Resolve(Type serviceType)
            {
                var func = _resolutionCache.Value.GetValueOrDefault(serviceType);
                if (func != null)
                    return func();

                var reg = _registrations.GetValueOrDefault(serviceType);
                if (reg == null)
                    return null;
                
                func = reg.CompileDelegate();
                _resolutionCache.Swap(cache => cache.AddOrUpdate(serviceType, func));
                return func.Invoke();
            }
        }
        
        class Factory 
        {
            public readonly Type ImplType;
            public Factory(Type implType) { ImplType = implType; }
            public Func<object> CompileDelegate() { return () => Activator.CreateInstance(ImplType); }
        } 
    }
```
