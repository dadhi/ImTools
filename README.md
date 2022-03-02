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
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

|                          Method | Count |             Mean |          Error |         StdDev |           Median | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|-------------------------------- |------ |-----------------:|---------------:|---------------:|-----------------:|------:|--------:|----------:|---------:|---------:|----------:|
|            V2_ImMap_AddOrUpdate |     1 |         15.14 ns |       0.464 ns |       1.323 ns |         14.84 ns |  1.00 |    0.00 |    0.0076 |        - |        - |      48 B |
|            V3_ImMap_AddOrUpdate |     1 |         11.02 ns |       0.299 ns |       0.743 ns |         10.87 ns |  0.74 |    0.07 |    0.0051 |        - |        - |      32 B |
| V3_PartitionedImMap_AddOrUpdate |     1 |        122.88 ns |       2.735 ns |       7.977 ns |        120.19 ns |  8.18 |    0.79 |    0.0496 |        - |        - |     312 B |
|       DictSlim_GetOrAddValueRef |     1 |         46.49 ns |       0.967 ns |       2.039 ns |         45.92 ns |  3.09 |    0.28 |    0.0204 |        - |        - |     128 B |
|                     Dict_TryAdd |     1 |         41.11 ns |       0.897 ns |       1.422 ns |         41.05 ns |  2.76 |    0.26 |    0.0344 |        - |        - |     216 B |
|           ConcurrentDict_TryAdd |     1 |        158.54 ns |       3.225 ns |       4.414 ns |        158.96 ns | 10.65 |    1.00 |    0.1376 |   0.0007 |        - |     864 B |
|       ImmutableDict_Builder_Add |     1 |        130.92 ns |       1.535 ns |       1.360 ns |        130.64 ns |  8.63 |    0.91 |    0.0253 |        - |        - |     160 B |
|               ImmutableDict_Add |     1 |        130.73 ns |       3.102 ns |       9.049 ns |        132.08 ns |  8.69 |    0.91 |    0.0165 |        - |        - |     104 B |
|                                 |       |                  |                |                |                  |       |         |           |          |          |           |
|            V2_ImMap_AddOrUpdate |    10 |        647.41 ns |      24.273 ns |      70.807 ns |        673.87 ns |  1.00 |    0.00 |    0.2823 |        - |        - |    1776 B |
|            V3_ImMap_AddOrUpdate |    10 |        242.78 ns |       4.904 ns |      11.069 ns |        240.92 ns |  0.36 |    0.03 |    0.1197 |        - |        - |     752 B |
| V3_PartitionedImMap_AddOrUpdate |    10 |        280.77 ns |       5.575 ns |       6.636 ns |        281.50 ns |  0.41 |    0.04 |    0.0954 |        - |        - |     600 B |
|       DictSlim_GetOrAddValueRef |    10 |        289.36 ns |       5.836 ns |      11.655 ns |        288.89 ns |  0.43 |    0.04 |    0.1326 |        - |        - |     832 B |
|                     Dict_TryAdd |    10 |        291.70 ns |       5.892 ns |      11.630 ns |        289.94 ns |  0.43 |    0.04 |    0.1578 |        - |        - |     992 B |
|           ConcurrentDict_TryAdd |    10 |        564.35 ns |      11.281 ns |      15.060 ns |        564.57 ns |  0.81 |    0.07 |    0.1945 |   0.0010 |        - |    1224 B |
|       ImmutableDict_Builder_Add |    10 |      1,612.84 ns |      30.964 ns |      35.658 ns |      1,617.42 ns |  2.33 |    0.20 |    0.1163 |        - |        - |     736 B |
|               ImmutableDict_Add |    10 |      2,715.60 ns |      41.873 ns |      32.692 ns |      2,726.71 ns |  3.94 |    0.10 |    0.4196 |        - |        - |    2640 B |
|                                 |       |                  |                |                |                  |       |         |           |          |          |           |
|            V2_ImMap_AddOrUpdate |   100 |     12,293.32 ns |     229.257 ns |     424.943 ns |     12,360.83 ns |  1.00 |    0.00 |    5.9357 |   0.2441 |        - |   37296 B |
|            V3_ImMap_AddOrUpdate |   100 |      8,818.77 ns |     175.424 ns |     373.843 ns |      8,791.68 ns |  0.72 |    0.04 |    3.4027 |   0.1373 |        - |   21352 B |
| V3_PartitionedImMap_AddOrUpdate |   100 |      3,234.23 ns |      64.352 ns |     117.671 ns |      3,237.60 ns |  0.26 |    0.01 |    1.4725 |   0.0839 |        - |    9240 B |
|       DictSlim_GetOrAddValueRef |   100 |      2,520.48 ns |      36.230 ns |      30.254 ns |      2,527.72 ns |  0.21 |    0.01 |    1.3275 |   0.0534 |        - |    8336 B |
|                     Dict_TryAdd |   100 |      3,038.78 ns |      60.333 ns |      86.528 ns |      3,053.22 ns |  0.25 |    0.01 |    2.0828 |   0.1335 |        - |   13072 B |
|           ConcurrentDict_TryAdd |   100 |     10,914.74 ns |     217.568 ns |     267.193 ns |     10,980.10 ns |  0.90 |    0.04 |    3.6316 |   0.3510 |        - |   22784 B |
|       ImmutableDict_Builder_Add |   100 |     25,755.21 ns |     510.198 ns |   1,007.081 ns |     25,798.85 ns |  2.09 |    0.11 |    1.4648 |   0.0916 |        - |    9376 B |
|               ImmutableDict_Add |   100 |     48,151.62 ns |     950.427 ns |   1,451.403 ns |     48,550.19 ns |  3.95 |    0.22 |    7.9346 |   0.3662 |        - |   49952 B |
|                                 |       |                  |                |                |                  |       |         |           |          |          |           |
|            V2_ImMap_AddOrUpdate |  1000 |    202,184.79 ns |   4,030.081 ns |   6,032.035 ns |    201,787.26 ns |  1.00 |    0.00 |   84.9609 |   0.4883 |        - |  534144 B |
|            V3_ImMap_AddOrUpdate |  1000 |    171,126.16 ns |   3,405.240 ns |   4,883.694 ns |    171,917.09 ns |  0.85 |    0.04 |   58.3496 |   0.4883 |        - |  366496 B |
| V3_PartitionedImMap_AddOrUpdate |  1000 |     95,031.20 ns |   1,826.035 ns |   1,793.410 ns |     95,489.40 ns |  0.48 |    0.02 |   35.8887 |  11.9629 |        - |  225496 B |
|       DictSlim_GetOrAddValueRef |  1000 |     25,387.70 ns |     492.313 ns |     971.778 ns |     25,388.93 ns |  0.12 |    0.01 |   11.6272 |   2.8992 |        - |   73120 B |
|                     Dict_TryAdd |  1000 |     31,939.82 ns |     636.682 ns |   1,148.068 ns |     31,787.71 ns |  0.16 |    0.01 |   21.2402 |   0.0610 |        - |  133896 B |
|           ConcurrentDict_TryAdd |  1000 |    114,283.00 ns |   1,884.712 ns |   2,579.815 ns |    113,859.86 ns |  0.57 |    0.02 |   32.7148 |   0.1221 |        - |  205368 B |
|       ImmutableDict_Builder_Add |  1000 |    329,232.01 ns |   6,559.466 ns |   8,529.159 ns |    326,383.94 ns |  1.63 |    0.07 |   15.1367 |   0.4883 |        - |   95776 B |
|               ImmutableDict_Add |  1000 |    725,486.29 ns |  14,467.219 ns |  19,313.322 ns |    726,475.10 ns |  3.59 |    0.13 |  112.3047 |   0.9766 |        - |  710208 B |
|                                 |       |                  |                |                |                  |       |         |           |          |          |           |
|            V2_ImMap_AddOrUpdate | 10000 |  4,426,278.12 ns |  87,006.155 ns |  77,128.690 ns |  4,404,791.02 ns |  1.00 |    0.00 | 1109.3750 | 226.5625 | 101.5625 | 6972672 B |
|            V3_ImMap_AddOrUpdate | 10000 |  4,423,388.25 ns |  83,777.390 ns | 122,799.812 ns |  4,381,706.25 ns |  1.00 |    0.03 |  835.9375 | 328.1250 | 148.4375 | 5247424 B |
| V3_PartitionedImMap_AddOrUpdate | 10000 |  3,739,595.26 ns |  73,002.219 ns |  92,324.433 ns |  3,718,667.58 ns |  0.85 |    0.03 |  613.2813 | 265.6250 |  70.3125 | 3856344 B |
|       DictSlim_GetOrAddValueRef | 10000 |    407,978.45 ns |   7,661.713 ns |  13,815.649 ns |    406,788.38 ns |  0.09 |    0.00 |  124.5117 | 124.5117 | 124.5117 |  975712 B |
|                     Dict_TryAdd | 10000 |    536,228.64 ns |  10,556.052 ns |  21,081.577 ns |    535,227.64 ns |  0.12 |    0.01 |  221.6797 | 221.6797 | 221.6797 | 1261688 B |
|           ConcurrentDict_TryAdd | 10000 |  2,860,109.91 ns |  28,230.649 ns |  25,025.735 ns |  2,868,305.66 ns |  0.65 |    0.01 |  273.4375 | 121.0938 |  42.9688 | 1645302 B |
|       ImmutableDict_Builder_Add | 10000 |  4,656,480.71 ns |  71,840.690 ns |  59,990.230 ns |  4,643,750.78 ns |  1.05 |    0.02 |  148.4375 |  70.3125 |        - |  959776 B |
|               ImmutableDict_Add | 10000 | 11,641,070.08 ns | 207,715.718 ns | 213,308.750 ns | 11,612,375.78 ns |  2.63 |    0.08 | 1468.7500 | 265.6250 | 125.0000 | 9271168 B |
```


#### ImMap Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) lookups for the last added index in the `ImMap<string>` 
containing the specified Count of elements.

```md
|                      Method | Count |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |------ |-----------:|----------:|----------:|-----------:|------:|--------:|------:|------:|------:|----------:|
|            V2_ImMap_TryFind |     1 |  0.6303 ns | 0.0454 ns | 0.0425 ns |  0.6381 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|            V3_ImMap_TryFind |     1 |  0.9290 ns | 0.0439 ns | 0.0411 ns |  0.9284 ns |  1.48 |    0.12 |     - |     - |     - |         - |
| V3_PartitionedImMap_TryFind |     1 |  1.0952 ns | 0.0355 ns | 0.0332 ns |  1.0951 ns |  1.74 |    0.12 |     - |     - |     - |         - |
|        DictSlim_TryGetValue |     1 |  3.9227 ns | 0.1003 ns | 0.0938 ns |  3.9245 ns |  6.25 |    0.42 |     - |     - |     - |         - |
|            Dict_TryGetValue |     1 |  7.2612 ns | 0.1122 ns | 0.1050 ns |  7.2657 ns | 11.57 |    0.76 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue |     1 |  8.1538 ns | 0.1168 ns | 0.1035 ns |  8.1563 ns | 13.01 |    0.93 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue |     1 | 15.6600 ns | 0.1452 ns | 0.1287 ns | 15.6729 ns | 24.98 |    1.75 |     - |     - |     - |         - |
|                             |       |            |           |           |            |       |         |       |       |       |           |
|            V2_ImMap_TryFind |    10 |  3.2915 ns | 0.0955 ns | 0.0847 ns |  3.2810 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|            V3_ImMap_TryFind |    10 |  3.1971 ns | 0.0764 ns | 0.0677 ns |  3.1851 ns |  0.97 |    0.04 |     - |     - |     - |         - |
| V3_PartitionedImMap_TryFind |    10 |  1.1737 ns | 0.0437 ns | 0.0387 ns |  1.1668 ns |  0.36 |    0.01 |     - |     - |     - |         - |
|        DictSlim_TryGetValue |    10 |  3.9646 ns | 0.0951 ns | 0.1057 ns |  3.9467 ns |  1.21 |    0.04 |     - |     - |     - |         - |
|            Dict_TryGetValue |    10 |  7.1259 ns | 0.1011 ns | 0.0946 ns |  7.1384 ns |  2.16 |    0.06 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue |    10 |  7.9278 ns | 0.1065 ns | 0.0944 ns |  7.9291 ns |  2.41 |    0.06 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue |    10 | 18.0258 ns | 0.2642 ns | 0.2472 ns | 17.9529 ns |  5.49 |    0.17 |     - |     - |     - |         - |
|                             |       |            |           |           |            |       |         |       |       |       |           |
|            V2_ImMap_TryFind |   100 |  5.2554 ns | 0.3458 ns | 0.9109 ns |  4.6748 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|            V3_ImMap_TryFind |   100 |  5.6595 ns | 0.1128 ns | 0.1055 ns |  5.6377 ns |  1.07 |    0.19 |     - |     - |     - |         - |
| V3_PartitionedImMap_TryFind |   100 |  2.5605 ns | 0.0626 ns | 0.0523 ns |  2.5697 ns |  0.50 |    0.08 |     - |     - |     - |         - |
|        DictSlim_TryGetValue |   100 |  4.3392 ns | 0.0588 ns | 0.0521 ns |  4.3430 ns |  0.83 |    0.14 |     - |     - |     - |         - |
|            Dict_TryGetValue |   100 |  6.6163 ns | 0.0773 ns | 0.0604 ns |  6.6331 ns |  1.30 |    0.22 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue |   100 |  7.5732 ns | 0.0827 ns | 0.0733 ns |  7.5850 ns |  1.45 |    0.24 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue |   100 | 20.0410 ns | 0.3129 ns | 0.2927 ns | 19.9944 ns |  3.80 |    0.67 |     - |     - |     - |         - |
|                             |       |            |           |           |            |       |         |       |       |       |           |
|            V2_ImMap_TryFind |  1000 |  6.9524 ns | 0.1210 ns | 0.1073 ns |  6.9368 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|            V3_ImMap_TryFind |  1000 | 11.1095 ns | 0.1589 ns | 0.1487 ns | 11.0952 ns |  1.60 |    0.03 |     - |     - |     - |         - |
| V3_PartitionedImMap_TryFind |  1000 |  5.5250 ns | 0.1084 ns | 0.1014 ns |  5.5125 ns |  0.79 |    0.01 |     - |     - |     - |         - |
|        DictSlim_TryGetValue |  1000 |  4.3399 ns | 0.0754 ns | 0.0669 ns |  4.3214 ns |  0.62 |    0.01 |     - |     - |     - |         - |
|            Dict_TryGetValue |  1000 |  6.7385 ns | 0.0783 ns | 0.0612 ns |  6.7491 ns |  0.97 |    0.02 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue |  1000 |  8.0877 ns | 0.0798 ns | 0.0707 ns |  8.0794 ns |  1.16 |    0.03 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue |  1000 | 23.2900 ns | 0.2741 ns | 0.2429 ns | 23.3068 ns |  3.35 |    0.06 |     - |     - |     - |         - |
|                             |       |            |           |           |            |       |         |       |       |       |           |
|            V2_ImMap_TryFind | 10000 | 11.8258 ns | 0.2418 ns | 0.4773 ns | 11.7575 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|            V3_ImMap_TryFind | 10000 | 15.9824 ns | 0.2152 ns | 0.1797 ns | 15.9901 ns |  1.36 |    0.02 |     - |     - |     - |         - |
| V3_PartitionedImMap_TryFind | 10000 | 10.2560 ns | 0.0970 ns | 0.0907 ns | 10.2246 ns |  0.87 |    0.01 |     - |     - |     - |         - |
|        DictSlim_TryGetValue | 10000 |  4.2128 ns | 0.0493 ns | 0.0412 ns |  4.2186 ns |  0.36 |    0.01 |     - |     - |     - |         - |
|            Dict_TryGetValue | 10000 |  6.4634 ns | 0.0628 ns | 0.0524 ns |  6.4662 ns |  0.55 |    0.01 |     - |     - |     - |         - |
|  ConcurrentDict_TryGetValue | 10000 |  7.4370 ns | 0.0582 ns | 0.0486 ns |  7.4572 ns |  0.63 |    0.01 |     - |     - |     - |         - |
|   ImmutableDict_TryGetValue | 10000 | 29.5492 ns | 0.6021 ns | 0.5632 ns | 29.3975 ns |  2.52 |    0.07 |     - |     - |     - |         - |
```

**Interpreting results:** `ImMap` holds very good against `ImmutableDictionary` sibling and even against `Dictionary`(s) up to certain count, 
indicating that immutable collection could be quite fast for lookups.


#### ImMap Enumeration

[The benchmark source code](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs)

```md

|                      Method | Count |            Mean |        Error |       StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |------ |----------------:|-------------:|-------------:|------:|--------:|-------:|------:|------:|----------:|
|            V2_ImMap_foreach |     1 |        40.84 ns |     0.878 ns |     1.111 ns |  1.00 |    0.00 | 0.0114 |     - |     - |      72 B |
|            V3_ImMap_foreach |     1 |        50.39 ns |     0.423 ns |     0.375 ns |  1.23 |    0.04 | 0.0254 |     - |     - |     160 B |
| V3_PartitionedImMap_foreach |     1 |       225.08 ns |     2.136 ns |     1.894 ns |  5.48 |    0.19 | 0.0520 |     - |     - |     328 B |
|            DictSlim_foreach |     1 |        12.57 ns |     0.233 ns |     0.218 ns |  0.31 |    0.01 |      - |     - |     - |         - |
|                Dict_foreach |     1 |        13.19 ns |     0.106 ns |     0.094 ns |  0.32 |    0.01 |      - |     - |     - |         - |
|      ConcurrentDict_foreach |     1 |       155.32 ns |     3.178 ns |     4.455 ns |  3.80 |    0.13 | 0.0100 |     - |     - |      64 B |
|       ImmutableDict_foreach |     1 |       269.31 ns |     4.901 ns |     8.188 ns |  6.54 |    0.29 |      - |     - |     - |         - |
|                             |       |                 |              |              |       |         |        |       |       |           |
|            V2_ImMap_foreach |    10 |       143.96 ns |     2.966 ns |     3.416 ns |  1.00 |    0.00 | 0.0176 |     - |     - |     112 B |
|            V3_ImMap_foreach |    10 |       188.82 ns |     3.354 ns |     5.020 ns |  1.32 |    0.05 | 0.0381 |     - |     - |     240 B |
| V3_PartitionedImMap_foreach |    10 |       883.62 ns |    17.727 ns |    25.424 ns |  6.11 |    0.28 | 0.2804 |     - |     - |    1768 B |
|            DictSlim_foreach |    10 |        53.43 ns |     1.016 ns |     0.901 ns |  0.37 |    0.01 |      - |     - |     - |         - |
|                Dict_foreach |    10 |        49.04 ns |     0.642 ns |     0.569 ns |  0.34 |    0.01 |      - |     - |     - |         - |
|      ConcurrentDict_foreach |    10 |       254.24 ns |     5.080 ns |     7.446 ns |  1.79 |    0.05 | 0.0100 |     - |     - |      64 B |
|       ImmutableDict_foreach |    10 |     1,103.16 ns |    10.592 ns |     8.845 ns |  7.72 |    0.20 |      - |     - |     - |         - |
|                             |       |                 |              |              |       |         |        |       |       |           |
|            V2_ImMap_foreach |   100 |     1,133.19 ns |    21.600 ns |    25.713 ns |  1.00 |    0.00 | 0.0210 |     - |     - |     136 B |
|            V3_ImMap_foreach |   100 |     1,424.04 ns |    28.369 ns |    40.685 ns |  1.26 |    0.05 | 0.0420 |     - |     - |     264 B |
| V3_PartitionedImMap_foreach |   100 |     2,958.51 ns |    56.936 ns |    63.284 ns |  2.61 |    0.08 | 0.4311 |     - |     - |    2728 B |
|            DictSlim_foreach |   100 |       502.62 ns |     7.262 ns |     6.064 ns |  0.44 |    0.01 |      - |     - |     - |         - |
|                Dict_foreach |   100 |       448.56 ns |     5.345 ns |     4.738 ns |  0.39 |    0.01 |      - |     - |     - |         - |
|      ConcurrentDict_foreach |   100 |     2,132.39 ns |    42.725 ns |    61.274 ns |  1.87 |    0.07 | 0.0076 |     - |     - |      64 B |
|       ImmutableDict_foreach |   100 |     9,651.70 ns |   189.401 ns |   218.114 ns |  8.52 |    0.27 |      - |     - |     - |         - |
|                             |       |                 |              |              |       |         |        |       |       |           |
|            V2_ImMap_foreach |  1000 |    11,565.85 ns |    93.520 ns |    82.903 ns |  1.00 |    0.00 | 0.0153 |     - |     - |     160 B |
|            V3_ImMap_foreach |  1000 |    14,204.52 ns |   270.833 ns |   253.338 ns |  1.23 |    0.02 | 0.0305 |     - |     - |     352 B |
| V3_PartitionedImMap_foreach |  1000 |    26,051.01 ns |   521.110 ns |   461.950 ns |  2.25 |    0.04 | 0.4883 |     - |     - |    3112 B |
|            DictSlim_foreach |  1000 |     4,878.14 ns |    76.804 ns |    59.963 ns |  0.42 |    0.01 |      - |     - |     - |         - |
|                Dict_foreach |  1000 |     4,374.03 ns |    58.156 ns |    48.563 ns |  0.38 |    0.00 |      - |     - |     - |         - |
|      ConcurrentDict_foreach |  1000 |    18,995.15 ns |   226.079 ns |   200.413 ns |  1.64 |    0.02 |      - |     - |     - |      64 B |
|       ImmutableDict_foreach |  1000 |    97,209.31 ns | 1,931.208 ns | 2,371.696 ns |  8.48 |    0.20 |      - |     - |     - |         - |
|                             |       |                 |              |              |       |         |        |       |       |           |
|            V2_ImMap_foreach | 10000 |   132,377.53 ns | 2,388.216 ns | 2,233.939 ns |  1.00 |    0.00 |      - |     - |     - |     192 B |
|            V3_ImMap_foreach | 10000 |   151,855.25 ns | 1,056.582 ns |   936.632 ns |  1.15 |    0.02 |      - |     - |     - |     504 B |
| V3_PartitionedImMap_foreach | 10000 |   223,859.98 ns | 3,389.002 ns | 2,829.970 ns |  1.69 |    0.03 | 0.4883 |     - |     - |    3200 B |
|            DictSlim_foreach | 10000 |    48,192.30 ns |   356.989 ns |   333.928 ns |  0.36 |    0.01 |      - |     - |     - |         - |
|                Dict_foreach | 10000 |    43,573.72 ns |   346.780 ns |   307.412 ns |  0.33 |    0.01 |      - |     - |     - |         - |
|      ConcurrentDict_foreach | 10000 |   172,744.46 ns | 1,332.215 ns | 1,246.155 ns |  1.31 |    0.02 |      - |     - |     - |      64 B |
|       ImmutableDict_foreach | 10000 | 1,004,398.75 ns | 9,309.202 ns | 7,773.605 ns |  7.59 |    0.16 |      - |     - |     - |         - |
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
