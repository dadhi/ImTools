# ImTools

![stand with Ukraine](https://badgen.net/badge/stand%20with/UKRAINE/?color=0057B8&labelColor=FFD700)
[![license](https://img.shields.io/github/license/dadhi/ImTools.svg)](http://opensource.org/licenses/MIT)

- Windows, Linux, MacOS [![CI build](https://ci.appveyor.com/api/projects/status/el9echuqfnl86u53?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/imtools/branch/master)
- Lib package [![NuGet Badge](https://buildstats.info/nuget/ImTools.dll)](https://www.nuget.org/packages/ImTools.dll)
- Code package [![NuGet Badge](https://buildstats.info/nuget/ImTools)](https://www.nuget.org/packages/ImTools)
- Latest release [![latest release](https://img.shields.io/badge/latest%20release-v3.1.0-green)](https://github.com/dadhi/ImTools/releases/tag/v3.1.0) 

Fast and memory-efficient immutable collections and helper data structures.

Split from the [DryIoc](https://github.com/dadhi/dryioc).


## Benchmarks

The comparison is done against the previous V2 and the variety of BCL C# collections including the experimental `Microsoft.Collections.Extensions.DictionarySlim<K, V>`.

__Important:__ Keep in mind that immutable collections have a different use-case and a thread-safety guarantees compared to the 
`Dictionary`, `DictionarySlim` or even `ConcurrentDictionary`. The closest comparable would be the `ImmutableDictionary`. 
The benchmarks do not take the collections "nature" into account and run through the simplest available API path.

*Benchmark environment*:

```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
```


### ImHashMap of Type keys and small string values

#### ImHashMap Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs) inserts from 10 to 1000
items into the `ImHashMap<Type, string>`:

```md
|                            Method | Count |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|---------------------------------- |------ |-------------:|-------------:|-------------:|-------------:|------:|--------:|---------:|--------:|------:|----------:|
|          V2_ImHashMap_AddOrUpdate |     1 |     126.8 ns |      2.11 ns |      1.87 ns |     126.3 ns |  1.00 |    0.00 |   0.0432 |       - |     - |     272 B |
|          V3_ImHashMap_AddOrUpdate |     1 |     106.2 ns |      2.15 ns |      2.73 ns |     105.0 ns |  0.84 |    0.03 |   0.0253 |       - |     - |     160 B |
| V3_PartitionedHashMap_AddOrUpdate |     1 |     152.5 ns |      3.12 ns |      4.06 ns |     152.3 ns |  1.21 |    0.03 |   0.0446 |       - |     - |     280 B |
|                   DictSlim_TryAdd |     1 |     130.9 ns |      2.61 ns |      3.30 ns |     130.3 ns |  1.04 |    0.04 |   0.0305 |       - |     - |     192 B |
|                       Dict_TryAdd |     1 |     150.5 ns |      3.08 ns |      4.11 ns |     150.7 ns |  1.17 |    0.04 |   0.0420 |       - |     - |     264 B |
|       ConcurrentDictionary_TryAdd |     1 |     294.2 ns |      5.67 ns |      6.07 ns |     292.9 ns |  2.33 |    0.06 |   0.1540 |  0.0010 |     - |     968 B |
|         ImmutableDict_Builder_Add |     1 |     345.5 ns |      6.82 ns |      6.38 ns |     346.0 ns |  2.73 |    0.08 |   0.0429 |       - |     - |     272 B |
|                 ImmutableDict_Add |     1 |     369.7 ns |      6.77 ns |      5.66 ns |     369.5 ns |  2.91 |    0.06 |   0.0505 |       - |     - |     320 B |
|                                   |       |              |              |              |              |       |         |          |         |       |           |
|          V2_ImHashMap_AddOrUpdate |    10 |     947.0 ns |     18.95 ns |     20.27 ns |     952.2 ns |  1.00 |    0.00 |   0.3681 |  0.0019 |     - |    2312 B |
|          V3_ImHashMap_AddOrUpdate |    10 |     623.1 ns |      5.55 ns |      4.92 ns |     621.8 ns |  0.66 |    0.02 |   0.1669 |       - |     - |    1048 B |
| V3_PartitionedHashMap_AddOrUpdate |    10 |     547.4 ns |     10.18 ns |     11.31 ns |     544.0 ns |  0.58 |    0.01 |   0.1221 |       - |     - |     768 B |
|                   DictSlim_TryAdd |    10 |     625.1 ns |     12.03 ns |     13.37 ns |     621.7 ns |  0.66 |    0.02 |   0.1783 |       - |     - |    1120 B |
|                       Dict_TryAdd |    10 |     646.5 ns |     12.85 ns |     16.71 ns |     643.2 ns |  0.68 |    0.02 |   0.1650 |       - |     - |    1040 B |
|       ConcurrentDictionary_TryAdd |    10 |   1,425.0 ns |     28.01 ns |     50.50 ns |   1,405.0 ns |  1.49 |    0.04 |   0.4730 |  0.0076 |     - |    2968 B |
|         ImmutableDict_Builder_Add |    10 |   2,099.4 ns |     30.91 ns |     24.14 ns |   2,089.6 ns |  2.23 |    0.06 |   0.1335 |       - |     - |     848 B |
|                 ImmutableDict_Add |    10 |   3,182.2 ns |     31.64 ns |     26.42 ns |   3,191.6 ns |  3.37 |    0.07 |   0.4654 |       - |     - |    2920 B |
|                                   |       |              |              |              |              |       |         |          |         |       |           |
|          V2_ImHashMap_AddOrUpdate |   100 |  13,960.5 ns |    193.69 ns |    151.22 ns |  13,974.8 ns |  1.00 |    0.00 |   5.5542 |  0.2441 |     - |   34856 B |
|          V3_ImHashMap_AddOrUpdate |   100 |  11,501.2 ns |    217.62 ns |    241.88 ns |  11,495.1 ns |  0.82 |    0.02 |   3.1891 |  0.1068 |     - |   20008 B |
| V3_PartitionedHashMap_AddOrUpdate |   100 |   5,929.6 ns |    112.54 ns |    175.21 ns |   5,953.0 ns |  0.44 |    0.01 |   1.2436 |  0.0534 |     - |    7816 B |
|                   DictSlim_TryAdd |   100 |   4,930.6 ns |     95.70 ns |    127.75 ns |   5,001.7 ns |  0.35 |    0.01 |   1.1978 |  0.0305 |     - |    7536 B |
|                       Dict_TryAdd |   100 |   5,438.5 ns |     95.98 ns |    137.65 ns |   5,404.3 ns |  0.39 |    0.01 |   1.6251 |  0.0610 |     - |   10240 B |
|       ConcurrentDictionary_TryAdd |   100 |  16,669.7 ns |    326.23 ns |    362.61 ns |  16,763.5 ns |  1.19 |    0.03 |   4.9133 |  0.5798 |     - |   30968 B |
|         ImmutableDict_Builder_Add |   100 |  27,158.0 ns |    354.59 ns |    276.84 ns |  27,148.7 ns |  1.95 |    0.03 |   1.0376 |  0.0305 |     - |    6608 B |
|                 ImmutableDict_Add |   100 |  50,784.5 ns |    990.99 ns |  1,571.81 ns |  51,475.4 ns |  3.55 |    0.14 |   7.0801 |  0.2441 |     - |   44792 B |
|                                   |       |              |              |              |              |       |         |          |         |       |           |
|          V2_ImHashMap_AddOrUpdate |  1000 | 287,955.3 ns |  4,859.90 ns |  4,545.95 ns | 286,362.2 ns |  1.00 |    0.00 |  81.0547 |  0.9766 |     - |  511208 B |
|          V3_ImHashMap_AddOrUpdate |  1000 | 240,386.3 ns |  4,778.73 ns |  8,738.18 ns | 240,952.2 ns |  0.85 |    0.04 |  51.2695 | 11.7188 |     - |  324016 B |
| V3_PartitionedHashMap_AddOrUpdate |  1000 | 144,426.5 ns |  2,847.55 ns |  4,348.50 ns | 146,011.0 ns |  0.50 |    0.02 |  28.0762 |  8.0566 |     - |  176216 B |
|                   DictSlim_TryAdd |  1000 |  44,217.2 ns |    145.85 ns |    113.87 ns |  44,205.9 ns |  0.15 |    0.00 |   9.1553 |  1.5259 |     - |   57856 B |
|                       Dict_TryAdd |  1000 |  55,251.6 ns |  1,084.55 ns |  1,955.67 ns |  55,508.3 ns |  0.19 |    0.01 |  16.2354 |  5.3711 |     - |  102264 B |
|       ConcurrentDictionary_TryAdd |  1000 | 171,835.0 ns |  3,414.78 ns |  4,787.04 ns | 174,681.6 ns |  0.60 |    0.02 |  41.2598 | 14.6484 |     - |  260056 B |
|         ImmutableDict_Builder_Add |  1000 | 445,419.2 ns |  4,140.17 ns |  3,872.72 ns | 445,259.3 ns |  1.55 |    0.03 |   9.7656 |  1.9531 |     - |   64208 B |
|                 ImmutableDict_Add |  1000 | 815,930.7 ns | 15,795.06 ns | 17,556.16 ns | 810,772.7 ns |  2.83 |    0.06 | 105.4688 | 24.4141 |     - |  662168 B |
```

### ImHashMap Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs) lookups for the specific key in the 
`ImHashMap<Type, string>` containing the specified Count of elements.

```md
|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|               V2_ImHashMap_yFind |     1 |  5.100 ns | 0.1212 ns | 0.1134 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |     1 |  5.539 ns | 0.1676 ns | 0.1568 ns |  1.09 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |     1 |  5.813 ns | 0.1844 ns | 0.1973 ns |  1.13 |    0.04 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     1 |  6.614 ns | 0.0839 ns | 0.0744 ns |  1.29 |    0.03 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     1 | 16.495 ns | 0.1270 ns | 0.1126 ns |  3.23 |    0.06 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     1 | 12.715 ns | 0.1584 ns | 0.1323 ns |  2.49 |    0.05 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     1 | 22.615 ns | 0.3097 ns | 0.2418 ns |  4.42 |    0.08 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V2_ImHashMap_TryFind |    10 |  6.270 ns | 0.1421 ns | 0.1187 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |    10 |  6.597 ns | 0.1095 ns | 0.1024 ns |  1.06 |    0.02 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |    10 |  5.416 ns | 0.0908 ns | 0.0805 ns |  0.87 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |    10 |  7.543 ns | 0.1274 ns | 0.1064 ns |  1.20 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    10 | 16.907 ns | 0.1892 ns | 0.1769 ns |  2.70 |    0.05 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    10 | 12.694 ns | 0.1740 ns | 0.1453 ns |  2.03 |    0.04 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    10 | 24.049 ns | 0.3034 ns | 0.2838 ns |  3.84 |    0.07 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V2_ImHashMap_TryFind |   100 |  9.459 ns | 0.2667 ns | 0.2739 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |   100 |  9.526 ns | 0.1814 ns | 0.1697 ns |  1.01 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |   100 |  5.827 ns | 0.0867 ns | 0.0768 ns |  0.62 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |   100 |  6.897 ns | 0.1033 ns | 0.0967 ns |  0.73 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   100 | 18.189 ns | 0.2100 ns | 0.1640 ns |  1.92 |    0.06 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   100 | 13.412 ns | 0.2041 ns | 0.1909 ns |  1.42 |    0.04 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   100 | 26.023 ns | 0.3854 ns | 0.3417 ns |  2.76 |    0.08 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V2_ImHashMap_TryFind |  1000 | 15.172 ns | 0.2617 ns | 0.2320 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |  1000 | 14.473 ns | 0.2030 ns | 0.1799 ns |  0.95 |    0.01 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |  1000 |  8.182 ns | 0.0809 ns | 0.0756 ns |  0.54 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |  1000 |  6.893 ns | 0.1590 ns | 0.1410 ns |  0.45 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  1000 | 18.230 ns | 0.2012 ns | 0.1882 ns |  1.20 |    0.02 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  1000 | 14.920 ns | 0.2296 ns | 0.2035 ns |  0.98 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  1000 | 29.555 ns | 0.5926 ns | 0.5543 ns |  1.95 |    0.04 |     - |     - |     - |         - |
```

### ImHashMap Enumeration

[The benchmark source](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs)

```md
|                        Method | Count |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------ |------ |-------------:|-------------:|-------------:|-------------:|------:|--------:|-------:|------:|------:|----------:|
|          V2_ImHashMap_foreach |     1 |     53.16 ns |     1.107 ns |     1.317 ns |     53.11 ns |  1.00 |    0.00 | 0.0166 |     - |     - |     104 B |
|          V3_ImHashMap_foreach |     1 |     62.12 ns |     1.327 ns |     1.986 ns |     61.59 ns |  1.16 |    0.05 | 0.0267 |     - |     - |     168 B |
| V3_PartitionedHashMap_foreach |     1 |    238.62 ns |     4.811 ns |     6.084 ns |    235.92 ns |  4.50 |    0.13 | 0.0534 |     - |     - |     336 B |
|        DictionarySlim_foreach |     1 |     12.90 ns |     0.167 ns |     0.156 ns |     12.90 ns |  0.24 |    0.01 |      - |     - |     - |         - |
|            Dictionary_foreach |     1 |     14.19 ns |     0.217 ns |     0.181 ns |     14.14 ns |  0.27 |    0.01 |      - |     - |     - |         - |
|  ConcurrentDictionary_foreach |     1 |    153.40 ns |     2.768 ns |     4.142 ns |    151.56 ns |  2.89 |    0.11 | 0.0100 |     - |     - |      64 B |
|         ImmutableDict_foreach |     1 |    268.98 ns |     5.361 ns |     9.528 ns |    268.86 ns |  5.01 |    0.26 |      - |     - |     - |         - |
|                               |       |              |              |              |              |       |         |        |       |       |           |
|          V2_ImHashMap_foreach |    10 |    233.42 ns |     4.541 ns |     4.859 ns |    232.71 ns |  1.00 |    0.00 | 0.0200 |     - |     - |     128 B |
|          V3_ImHashMap_foreach |    10 |    249.12 ns |     4.915 ns |     5.852 ns |    246.87 ns |  1.07 |    0.03 | 0.0391 |     - |     - |     248 B |
| V3_PartitionedHashMap_foreach |    10 |    746.26 ns |    14.990 ns |    18.409 ns |    748.53 ns |  3.20 |    0.13 | 0.1602 |     - |     - |    1008 B |
|        DictionarySlim_foreach |    10 |     72.54 ns |     0.970 ns |     0.907 ns |     72.42 ns |  0.31 |    0.01 |      - |     - |     - |         - |
|            Dictionary_foreach |    10 |     58.52 ns |     0.938 ns |     0.733 ns |     58.58 ns |  0.25 |    0.01 |      - |     - |     - |         - |
|  ConcurrentDictionary_foreach |    10 |    468.65 ns |     9.252 ns |    12.351 ns |    464.12 ns |  2.01 |    0.06 | 0.0095 |     - |     - |      64 B |
|         ImmutableDict_foreach |    10 |  1,127.30 ns |    15.601 ns |    14.593 ns |  1,123.20 ns |  4.82 |    0.10 |      - |     - |     - |         - |
|                               |       |              |              |              |              |       |         |        |       |       |           |
|          V2_ImHashMap_foreach |   100 |  2,355.54 ns |    46.224 ns |    63.271 ns |  2,337.40 ns |  1.00 |    0.00 | 0.0229 |     - |     - |     160 B |
|          V3_ImHashMap_foreach |   100 |  2,423.13 ns |    31.652 ns |    46.395 ns |  2,412.92 ns |  1.03 |    0.04 | 0.0496 |     - |     - |     320 B |
| V3_PartitionedHashMap_foreach |   100 |  4,268.51 ns |    25.429 ns |    22.542 ns |  4,266.90 ns |  1.81 |    0.05 | 0.4501 |     - |     - |    2856 B |
|        DictionarySlim_foreach |   100 |    570.39 ns |     5.827 ns |     4.866 ns |    570.93 ns |  0.24 |    0.01 |      - |     - |     - |         - |
|            Dictionary_foreach |   100 |    548.46 ns |     8.579 ns |     7.605 ns |    547.90 ns |  0.23 |    0.01 |      - |     - |     - |         - |
|  ConcurrentDictionary_foreach |   100 |  2,967.70 ns |    45.435 ns |    44.623 ns |  2,958.11 ns |  1.26 |    0.04 | 0.0076 |     - |     - |      64 B |
|         ImmutableDict_foreach |   100 |  9,988.48 ns |   198.973 ns |   297.813 ns |  9,821.03 ns |  4.24 |    0.14 |      - |     - |     - |         - |
|                               |       |              |              |              |              |       |         |        |       |       |           |
|          V2_ImHashMap_foreach |  1000 | 23,828.30 ns |   433.708 ns |   362.166 ns | 23,743.77 ns |  1.00 |    0.00 | 0.0305 |     - |     - |     192 B |
|          V3_ImHashMap_foreach |  1000 | 26,014.69 ns |   294.125 ns |   245.608 ns | 25,965.82 ns |  1.09 |    0.02 | 0.0610 |     - |     - |     552 B |
| V3_PartitionedHashMap_foreach |  1000 | 36,582.53 ns |   709.641 ns |   897.469 ns | 36,594.84 ns |  1.54 |    0.04 | 0.4883 |     - |     - |    3240 B |
|        DictionarySlim_foreach |  1000 |  5,591.13 ns |    43.627 ns |    40.809 ns |  5,602.25 ns |  0.23 |    0.00 |      - |     - |     - |         - |
|            Dictionary_foreach |  1000 |  5,319.86 ns |    51.684 ns |    45.817 ns |  5,308.36 ns |  0.22 |    0.00 |      - |     - |     - |         - |
|  ConcurrentDictionary_foreach |  1000 | 38,718.40 ns |   466.979 ns |   389.949 ns | 38,728.64 ns |  1.63 |    0.03 |      - |     - |     - |      64 B |
|         ImmutableDict_foreach |  1000 | 99,156.35 ns | 1,962.968 ns | 2,181.834 ns | 98,166.03 ns |  4.17 |    0.11 |      - |     - |     - |         - |
```

### ImMap with small string values

`ImMap<string>` stores the `int` keys and `string` values.


#### ImMap Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) inserts from 1 to 10 000 of items into the `ImMap<string>`:

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                              Method | Count |              Mean |          Error |         StdDev |            Median | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------------------ |------ |------------------:|---------------:|---------------:|------------------:|------:|--------:|----------:|---------:|---------:|----------:|
|                V4_ImMap_AddOrUpdate |     1 |         10.871 ns |      0.2374 ns |      0.2105 ns |         10.858 ns |  1.00 |    0.00 |    0.0051 |        - |        - |      32 B |
|     V4_PartitionedImMap_AddOrUpdate |     1 |        146.496 ns |      1.9151 ns |      1.7914 ns |        146.307 ns | 13.46 |    0.23 |    0.0496 |        - |        - |     312 B |
|                V2_ImMap_AddOrUpdate |     1 |         14.150 ns |      0.1197 ns |      0.0999 ns |         14.128 ns |  1.30 |    0.03 |    0.0076 |        - |        - |      48 B |
|           DictSlim_GetOrAddValueRef |     1 |         65.831 ns |      5.7829 ns |     16.9601 ns |         68.596 ns |  6.22 |    0.73 |    0.0204 |        - |        - |     128 B |
|                         Dict_TryAdd |     1 |         63.210 ns |      1.8806 ns |      5.5451 ns |         62.469 ns |  5.94 |    0.35 |    0.0343 |        - |        - |     216 B |
|               ConcurrentDict_TryAdd |     1 |        191.130 ns |      4.1159 ns |     11.1975 ns |        187.934 ns | 17.93 |    1.46 |    0.1373 |   0.0005 |        - |     864 B |
|           ImmutableDict_Builder_Add |     1 |        151.033 ns |      3.1616 ns |      3.8827 ns |        149.342 ns | 13.97 |    0.56 |    0.0253 |        - |        - |     160 B |
|                   ImmutableDict_Add |     1 |        129.813 ns |      1.4758 ns |      1.3804 ns |        129.869 ns | 11.96 |    0.28 |    0.0165 |        - |        - |     104 B |
|                                     |       |                   |                |                |                   |       |         |           |          |          |           |
|                V4_ImMap_AddOrUpdate |    10 |        294.159 ns |      5.9596 ns |     13.2061 ns |        292.411 ns |  1.00 |    0.00 |    0.1173 |        - |        - |     736 B |
|     V4_PartitionedImMap_AddOrUpdate |    10 |        379.475 ns |      7.3767 ns |     19.0417 ns |        375.203 ns |  1.30 |    0.09 |    0.0954 |        - |        - |     600 B |
|                V2_ImMap_AddOrUpdate |    10 |        695.050 ns |      8.9192 ns |      7.4479 ns |        693.960 ns |  2.34 |    0.13 |    0.2823 |        - |        - |    1776 B |
|           DictSlim_GetOrAddValueRef |    10 |        350.208 ns |      6.9422 ns |      7.7163 ns |        348.057 ns |  1.19 |    0.07 |    0.1326 |        - |        - |     832 B |
|                         Dict_TryAdd |    10 |        323.383 ns |      6.0111 ns |      8.9971 ns |        321.341 ns |  1.09 |    0.05 |    0.1578 |   0.0005 |        - |     992 B |
|               ConcurrentDict_TryAdd |    10 |        692.553 ns |     13.4824 ns |     13.8454 ns |        695.128 ns |  2.35 |    0.13 |    0.1945 |   0.0010 |        - |    1224 B |
|           ImmutableDict_Builder_Add |    10 |      1,956.984 ns |     38.8655 ns |     41.5857 ns |      1,953.909 ns |  6.64 |    0.37 |    0.1144 |        - |        - |     736 B |
|                   ImmutableDict_Add |    10 |      3,279.166 ns |     64.5917 ns |     86.2281 ns |      3,279.354 ns | 11.04 |    0.69 |    0.4196 |        - |        - |    2640 B |
|                                     |       |                   |                |                |                   |       |         |           |          |          |           |
|                V4_ImMap_AddOrUpdate |   100 |     10,247.976 ns |    198.7458 ns |    297.4734 ns |     10,223.012 ns |  1.00 |    0.00 |    2.9602 |   0.1221 |        - |   18640 B |
|     V4_PartitionedImMap_AddOrUpdate |   100 |      4,273.109 ns |    128.4401 ns |    370.5792 ns |      4,169.651 ns |  0.40 |    0.02 |    1.4725 |   0.0839 |        - |    9240 B |
|                V2_ImMap_AddOrUpdate |   100 |     16,595.707 ns |    331.7484 ns |    768.8774 ns |     16,614.487 ns |  1.63 |    0.08 |    5.9204 |   0.2441 |        - |   37296 B |
|           DictSlim_GetOrAddValueRef |   100 |      3,513.581 ns |     65.4204 ns |     67.1819 ns |      3,534.437 ns |  0.34 |    0.01 |    1.3275 |   0.0534 |        - |    8336 B |
|                         Dict_TryAdd |   100 |      3,846.525 ns |    132.2857 ns |    387.9711 ns |      3,704.327 ns |  0.42 |    0.03 |    2.0828 |   0.1297 |        - |   13072 B |
|               ConcurrentDict_TryAdd |   100 |     13,420.149 ns |    249.8230 ns |    617.5007 ns |     13,312.788 ns |  1.33 |    0.10 |    3.6316 |   0.3510 |        - |   22784 B |
|           ImmutableDict_Builder_Add |   100 |     32,885.999 ns |  1,673.7125 ns |  4,934.9792 ns |     32,048.508 ns |  3.56 |    0.30 |    1.4648 |   0.0610 |        - |    9376 B |
|                   ImmutableDict_Add |   100 |     45,172.496 ns |    690.7964 ns |    612.3730 ns |     45,280.002 ns |  4.42 |    0.16 |    7.9346 |   0.3662 |        - |   49952 B |
|                                     |       |                   |                |                |                   |       |         |           |          |          |           |
|                V4_ImMap_AddOrUpdate |  1000 |    150,805.454 ns |    706.8118 ns |    590.2199 ns |    150,862.744 ns |  1.00 |    0.00 |   46.6309 |   0.4883 |        - |  293656 B |
|     V4_PartitionedImMap_AddOrUpdate |  1000 |     93,130.099 ns |  1,248.3655 ns |  1,042.4417 ns |     92,901.184 ns |  0.62 |    0.01 |   35.8887 |  11.9629 |        - |  225496 B |
|                V2_ImMap_AddOrUpdate |  1000 |    197,260.620 ns |  1,417.5826 ns |  1,183.7457 ns |    196,969.263 ns |  1.31 |    0.01 |   84.9609 |   0.2441 |        - |  534144 B |
|           DictSlim_GetOrAddValueRef |  1000 |     26,365.481 ns |    262.8911 ns |    233.0462 ns |     26,321.643 ns |  0.17 |    0.00 |   11.6272 |   2.8992 |        - |   73120 B |
|                         Dict_TryAdd |  1000 |     31,279.469 ns |    433.4285 ns |    384.2231 ns |     31,247.034 ns |  0.21 |    0.00 |   21.2402 |   0.0610 |        - |  133896 B |
|               ConcurrentDict_TryAdd |  1000 |    113,079.312 ns |  1,396.9827 ns |  1,306.7384 ns |    112,611.902 ns |  0.75 |    0.01 |   32.7148 |   0.1221 |        - |  205368 B |
|           ImmutableDict_Builder_Add |  1000 |    352,474.495 ns |  4,395.4483 ns |  4,111.5051 ns |    351,035.938 ns |  2.34 |    0.03 |   15.1367 |   4.8828 |        - |   95776 B |
|                   ImmutableDict_Add |  1000 |    693,655.221 ns |  4,400.7114 ns |  3,674.7933 ns |    693,567.773 ns |  4.60 |    0.04 |  112.3047 |   0.9766 |        - |  710209 B |
|                                     |       |                   |                |                |                   |       |         |           |          |          |           |
|                V4_ImMap_AddOrUpdate | 10000 |  3,950,199.051 ns | 51,122.7579 ns | 45,318.9933 ns |  3,937,974.219 ns |  1.00 |    0.00 |  632.8125 | 312.5000 |  46.8750 | 3993786 B |
|     V4_PartitionedImMap_AddOrUpdate | 10000 |  3,779,820.547 ns | 49,945.1736 ns | 46,718.7464 ns |  3,776,232.812 ns |  0.96 |    0.01 |  613.2813 | 250.0000 |  74.2188 | 3856384 B |
|                V2_ImMap_AddOrUpdate | 10000 |  4,573,200.469 ns | 74,161.5687 ns | 69,370.7774 ns |  4,570,491.406 ns |  1.16 |    0.02 | 1109.3750 | 226.5625 | 101.5625 | 6972711 B |
|           DictSlim_GetOrAddValueRef | 10000 |    428,076.003 ns |  5,330.1673 ns |  4,985.8418 ns |    426,723.242 ns |  0.11 |    0.00 |  124.5117 | 124.5117 | 124.5117 |  975754 B |
|                         Dict_TryAdd | 10000 |    540,117.012 ns |  5,600.0325 ns |  5,238.2739 ns |    540,958.984 ns |  0.14 |    0.00 |  221.6797 | 221.6797 | 221.6797 | 1261763 B |
|               ConcurrentDict_TryAdd | 10000 |  2,877,048.326 ns | 19,171.0217 ns | 16,994.6114 ns |  2,880,000.000 ns |  0.73 |    0.01 |  273.4375 | 121.0938 |  42.9688 | 1645361 B |
|           ImmutableDict_Builder_Add | 10000 |  4,785,444.115 ns | 61,083.3020 ns | 57,137.3586 ns |  4,781,788.281 ns |  1.21 |    0.02 |  148.4375 |  70.3125 |        - |  959781 B |
|                   ImmutableDict_Add | 10000 | 11,375,170.833 ns | 74,047.5714 ns | 57,811.4731 ns | 11,371,810.156 ns |  2.88 |    0.04 | 1468.7500 | 265.6250 | 125.0000 | 9271220 B |
```


#### ImMap Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) lookups for the last added index in the `ImMap<string>` 
containing the specified Count of elements.

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                      Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |------ |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|            V4_ImMap_TryFind |     1 |  2.5662 ns | 0.0840 ns | 0.0701 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedImMap_TryFind |     1 |  2.9264 ns | 0.0775 ns | 0.0687 ns |  1.14 |    0.03 |     - |     - |     - |         - |
|            V2_ImMap_TryFind |     1 |  0.8574 ns | 0.0826 ns | 0.0772 ns |  0.33 |    0.02 |     - |     - |     - |         - |
|        DictSlim_TryGetValue |     1 |  3.7989 ns | 0.1118 ns | 0.1046 ns |  1.48 |    0.05 |     - |     - |     - |         - |
|            Dict_TryGetValue |     1 |  7.5973 ns | 0.2095 ns | 0.1857 ns |  2.95 |    0.08 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue |     1 |  8.1365 ns | 0.1859 ns | 0.1552 ns |  3.17 |    0.08 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue |     1 | 15.7728 ns | 0.3912 ns | 0.4348 ns |  6.16 |    0.25 |     - |     - |     - |         - |
|                             |       |            |           |           |       |         |       |       |       |           |
|            V4_ImMap_TryFind |    10 |  3.0618 ns | 0.0535 ns | 0.0474 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedImMap_TryFind |    10 |  2.5114 ns | 0.1019 ns | 0.0851 ns |  0.82 |    0.02 |     - |     - |     - |         - |
|            V2_ImMap_TryFind |    10 |  3.5793 ns | 0.1092 ns | 0.1022 ns |  1.17 |    0.04 |     - |     - |     - |         - |
|        DictSlim_TryGetValue |    10 |  4.0213 ns | 0.0329 ns | 0.0275 ns |  1.31 |    0.02 |     - |     - |     - |         - |
|            Dict_TryGetValue |    10 |  6.9583 ns | 0.0809 ns | 0.0717 ns |  2.27 |    0.03 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue |    10 |  7.9926 ns | 0.0997 ns | 0.0884 ns |  2.61 |    0.05 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue |    10 | 17.5147 ns | 0.3430 ns | 0.3209 ns |  5.72 |    0.15 |     - |     - |     - |         - |
|                             |       |            |           |           |       |         |       |       |       |           |
|            V4_ImMap_TryFind |   100 |  6.9090 ns | 0.1161 ns | 0.1030 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedImMap_TryFind |   100 |  2.6762 ns | 0.0542 ns | 0.1045 ns |  0.39 |    0.02 |     - |     - |     - |         - |
|            V2_ImMap_TryFind |   100 |  7.1365 ns | 0.1727 ns | 0.1442 ns |  1.03 |    0.03 |     - |     - |     - |         - |
|        DictSlim_TryGetValue |   100 |  4.4196 ns | 0.0861 ns | 0.0719 ns |  0.64 |    0.01 |     - |     - |     - |         - |
|            Dict_TryGetValue |   100 |  7.6746 ns | 0.0770 ns | 0.0643 ns |  1.11 |    0.02 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue |   100 |  8.0521 ns | 0.1363 ns | 0.1138 ns |  1.17 |    0.03 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue |   100 | 19.5683 ns | 0.1622 ns | 0.1355 ns |  2.83 |    0.04 |     - |     - |     - |         - |
|                             |       |            |           |           |       |         |       |       |       |           |
|            V4_ImMap_TryFind |  1000 | 12.7430 ns | 0.2270 ns | 0.2123 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedImMap_TryFind |  1000 |  7.0856 ns | 0.0987 ns | 0.0824 ns |  0.55 |    0.01 |     - |     - |     - |         - |
|            V2_ImMap_TryFind |  1000 | 10.9892 ns | 0.2986 ns | 0.3067 ns |  0.86 |    0.02 |     - |     - |     - |         - |
|        DictSlim_TryGetValue |  1000 |  4.3216 ns | 0.1560 ns | 0.1459 ns |  0.34 |    0.01 |     - |     - |     - |         - |
|            Dict_TryGetValue |  1000 |  7.9061 ns | 0.1667 ns | 0.1478 ns |  0.62 |    0.02 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue |  1000 |  8.1644 ns | 0.1095 ns | 0.0914 ns |  0.64 |    0.02 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue |  1000 | 22.1566 ns | 0.2936 ns | 0.2603 ns |  1.74 |    0.03 |     - |     - |     - |         - |
|                             |       |            |           |           |       |         |       |       |       |           |
|            V4_ImMap_TryFind | 10000 | 17.4170 ns | 0.2906 ns | 0.2576 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedImMap_TryFind | 10000 | 11.0928 ns | 0.1834 ns | 0.1626 ns |  0.64 |    0.01 |     - |     - |     - |         - |
|            V2_ImMap_TryFind | 10000 | 15.6074 ns | 0.2325 ns | 0.2175 ns |  0.90 |    0.02 |     - |     - |     - |         - |
|        DictSlim_TryGetValue | 10000 |  4.4326 ns | 0.1312 ns | 0.1228 ns |  0.25 |    0.01 |     - |     - |     - |         - |
|            Dict_TryGetValue | 10000 |  7.5615 ns | 0.0669 ns | 0.0559 ns |  0.43 |    0.01 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue | 10000 |  7.5147 ns | 0.1537 ns | 0.1437 ns |  0.43 |    0.01 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue | 10000 | 37.8435 ns | 0.4362 ns | 0.4080 ns |  2.17 |    0.04 |     - |     - |     - |         - |
```

**Interpreting results:** `ImMap` holds very good against `ImmutableDictionary` sibling and even against `Dictionary`(s) up to certain count, 
indicating that immutable collection could be quite fast for lookups.


#### ImMap Enumeration

[The benchmark source code](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs)

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT


|                      Method | Count |          Mean |        Error |        StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |------ |--------------:|-------------:|--------------:|------:|--------:|-------:|------:|------:|----------:|
|            V4_ImMap_foreach |     1 |      40.15 ns |     0.613 ns |      0.512 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| V4_PartitionedImMap_foreach |     1 |     274.37 ns |     1.739 ns |      1.542 ns |  6.83 |    0.09 | 0.0482 |     - |     - |     304 B |
|          V2_ImMap_Enumerate |     1 |      24.55 ns |     0.354 ns |      0.331 ns |  0.61 |    0.01 | 0.0114 |     - |     - |      72 B |
|            DictSlim_foreach |     1 |      12.23 ns |     0.153 ns |      0.136 ns |  0.30 |    0.00 |      - |     - |     - |         - |
|                Dict_foreach |     1 |      13.12 ns |     0.080 ns |      0.062 ns |  0.33 |    0.00 |      - |     - |     - |         - |
|      ConcurrentDict_foreach |     1 |     161.52 ns |     2.864 ns |      2.679 ns |  4.01 |    0.07 | 0.0100 |     - |     - |      64 B |
|       ImmutableDict_foreach |     1 |     160.99 ns |     1.549 ns |      1.374 ns |  4.01 |    0.06 |      - |     - |     - |         - |
|                             |       |               |              |               |       |         |        |       |       |           |
|            V4_ImMap_foreach |    10 |     154.20 ns |     1.366 ns |      1.141 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| V4_PartitionedImMap_foreach |    10 |   1,654.52 ns |    21.047 ns |     19.688 ns | 10.75 |    0.15 | 0.0477 |     - |     - |     304 B |
|          V2_ImMap_Enumerate |    10 |     123.60 ns |     2.362 ns |      1.844 ns |  0.80 |    0.01 | 0.0176 |     - |     - |     112 B |
|            DictSlim_foreach |    10 |      53.38 ns |     0.845 ns |      0.790 ns |  0.35 |    0.01 |      - |     - |     - |         - |
|                Dict_foreach |    10 |      50.09 ns |     1.059 ns |      0.884 ns |  0.32 |    0.01 |      - |     - |     - |         - |
|      ConcurrentDict_foreach |    10 |     287.66 ns |     2.805 ns |      2.486 ns |  1.86 |    0.02 | 0.0100 |     - |     - |      64 B |
|       ImmutableDict_foreach |    10 |     566.21 ns |    11.246 ns |      9.969 ns |  3.68 |    0.07 |      - |     - |     - |         - |
|                             |       |               |              |               |       |         |        |       |       |           |
|            V4_ImMap_foreach |   100 |   1,610.61 ns |    26.428 ns |     24.721 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| V4_PartitionedImMap_foreach |   100 |   4,480.84 ns |    45.735 ns |     40.543 ns |  2.79 |    0.05 | 0.0458 |     - |     - |     304 B |
|          V2_ImMap_Enumerate |   100 |   1,145.36 ns |    16.921 ns |     15.000 ns |  0.71 |    0.02 | 0.0210 |     - |     - |     136 B |
|            DictSlim_foreach |   100 |     529.71 ns |     3.664 ns |      3.428 ns |  0.33 |    0.01 |      - |     - |     - |         - |
|                Dict_foreach |   100 |     495.98 ns |     8.289 ns |      7.348 ns |  0.31 |    0.01 |      - |     - |     - |         - |
|      ConcurrentDict_foreach |   100 |   2,437.73 ns |    47.899 ns |     49.189 ns |  1.52 |    0.04 | 0.0076 |     - |     - |      64 B |
|       ImmutableDict_foreach |   100 |   4,491.40 ns |    21.716 ns |     16.955 ns |  2.80 |    0.05 |      - |     - |     - |         - |
|                             |       |               |              |               |       |         |        |       |       |           |
|            V4_ImMap_foreach |  1000 |  17,531.80 ns |   142.714 ns |    111.421 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| V4_PartitionedImMap_foreach |  1000 |  29,080.37 ns |   139.647 ns |    109.027 ns |  1.66 |    0.01 | 0.0305 |     - |     - |     304 B |
|          V2_ImMap_Enumerate |  1000 |  12,745.17 ns |   168.887 ns |    149.714 ns |  0.73 |    0.01 | 0.0153 |     - |     - |     160 B |
|            DictSlim_foreach |  1000 |   4,999.23 ns |    39.200 ns |     34.750 ns |  0.28 |    0.00 |      - |     - |     - |         - |
|                Dict_foreach |  1000 |   5,041.80 ns |    65.133 ns |     57.739 ns |  0.29 |    0.00 |      - |     - |     - |         - |
|      ConcurrentDict_foreach |  1000 |  21,511.26 ns |   388.750 ns |    363.637 ns |  1.22 |    0.03 |      - |     - |     - |      64 B |
|       ImmutableDict_foreach |  1000 |  47,644.28 ns |   685.586 ns |    641.297 ns |  2.72 |    0.04 |      - |     - |     - |         - |
|                             |       |               |              |               |       |         |        |       |       |           |
|            V4_ImMap_foreach | 10000 | 181,871.19 ns | 1,773.407 ns |  1,384.559 ns |  1.00 |    0.00 |      - |     - |     - |     176 B |
| V4_PartitionedImMap_foreach | 10000 | 299,382.72 ns | 2,495.179 ns |  2,211.911 ns |  1.65 |    0.01 |      - |     - |     - |     304 B |
|          V2_ImMap_Enumerate | 10000 | 139,144.02 ns | 1,366.664 ns |  1,067.001 ns |  0.77 |    0.01 |      - |     - |     - |     192 B |
|            DictSlim_foreach | 10000 |  48,518.39 ns |   852.316 ns |    837.088 ns |  0.27 |    0.00 |      - |     - |     - |         - |
|                Dict_foreach | 10000 |  46,570.12 ns |   494.143 ns |    438.045 ns |  0.26 |    0.00 |      - |     - |     - |         - |
|      ConcurrentDict_foreach | 10000 | 196,337.23 ns | 1,886.243 ns |  1,764.393 ns |  1.08 |    0.02 |      - |     - |     - |      64 B |
|       ImmutableDict_foreach | 10000 | 518,545.12 ns | 9,897.117 ns | 10,589.805 ns |  2.83 |    0.04 |      - |     - |     - |       1 B |
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
