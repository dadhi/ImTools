# ImTools

![stand with Ukraine](https://badgen.net/badge/stand%20with/UKRAINE/?color=0057B8&labelColor=FFD700)
[![license](https://img.shields.io/github/license/dadhi/ImTools.svg)](http://opensource.org/licenses/MIT)

- Windows, Linux, MacOS [![CI build](https://ci.appveyor.com/api/projects/status/el9echuqfnl86u53?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/imtools/branch/master)
- Lib package [![NuGet Badge](https://buildstats.info/nuget/ImTools.dll)](https://www.nuget.org/packages/ImTools.dll)
- Code package [![NuGet Badge](https://buildstats.info/nuget/ImTools)](https://www.nuget.org/packages/ImTools)
- Latest release [![latest release](https://img.shields.io/badge/latest%20release-v4.0.0-green)](https://github.com/dadhi/ImTools/releases/tag/v4.0.0) 

Fast and memory-efficient immutable collections and helper data structures.

Split from the [DryIoc](https://github.com/dadhi/dryioc).


## Benchmarks

The comparison is done against the previous versions and the variety of BCL C# collections including the experimental `Microsoft.Collections.Extensions.DictionarySlim<K, V>`.

__Important:__ Keep in mind that immutable collections have a different use-case and a thread-safety guarantees compared to the 
`Dictionary`, `DictionarySlim` or even `ConcurrentDictionary`. The closest comparable would be the `ImmutableDictionary`. 
The benchmarks do not take the collections "nature" into account and run through the simplest available API path.


### ImHashMap of Type keys and small string values

#### Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs) inserts from 10 to 1000
items into the `ImHashMap<Type, string>`:

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                            Method | Count |          Mean |        Error |       StdDev | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|---------------------------------- |------ |--------------:|-------------:|-------------:|------:|--------:|---------:|--------:|------:|----------:|
|          V4_ImHashMap_AddOrUpdate |     1 |      33.32 ns |     0.439 ns |     0.389 ns |  1.00 |    0.00 |   0.0178 |       - |     - |     112 B |
|          V3_ImHashMap_AddOrUpdate |     1 |      35.97 ns |     0.639 ns |     0.567 ns |  1.08 |    0.02 |   0.0178 |       - |     - |     112 B |
| V4_PartitionedHashMap_AddOrUpdate |     1 |      97.72 ns |     1.294 ns |     1.148 ns |  2.93 |    0.05 |   0.0370 |       - |     - |     232 B |
|                   DictSlim_TryAdd |     1 |      59.37 ns |     0.519 ns |     0.434 ns |  1.78 |    0.02 |   0.0229 |       - |     - |     144 B |
|                       Dict_TryAdd |     1 |      65.94 ns |     0.631 ns |     0.560 ns |  1.98 |    0.02 |   0.0343 |       - |     - |     216 B |
|       ConcurrentDictionary_TryAdd |     1 |     197.29 ns |     1.883 ns |     1.761 ns |  5.92 |    0.09 |   0.1466 |  0.0007 |     - |     920 B |
|         ImmutableDict_Builder_Add |     1 |     220.79 ns |     2.648 ns |     2.477 ns |  6.62 |    0.10 |   0.0355 |       - |     - |     224 B |
|                 ImmutableDict_Add |     1 |     264.15 ns |     4.786 ns |     3.737 ns |  7.93 |    0.16 |   0.0429 |       - |     - |     272 B |
|                                   |       |               |              |              |       |         |          |         |       |           |
|          V4_ImHashMap_AddOrUpdate |    10 |     307.91 ns |     5.281 ns |     4.940 ns |  1.00 |    0.00 |   0.1564 |  0.0005 |     - |     984 B |
|          V3_ImHashMap_AddOrUpdate |    10 |     389.09 ns |     6.482 ns |     5.746 ns |  1.27 |    0.03 |   0.1593 |  0.0005 |     - |    1000 B |
| V4_PartitionedHashMap_AddOrUpdate |    10 |     392.47 ns |     4.296 ns |     5.276 ns |  1.27 |    0.03 |   0.1144 |  0.0005 |     - |     720 B |
|                   DictSlim_TryAdd |    10 |     409.45 ns |     2.216 ns |     1.965 ns |  1.33 |    0.02 |   0.1707 |  0.0005 |     - |    1072 B |
|                       Dict_TryAdd |    10 |     413.38 ns |     5.460 ns |     5.107 ns |  1.34 |    0.03 |   0.1578 |  0.0005 |     - |     992 B |
|       ConcurrentDictionary_TryAdd |    10 |   1,319.88 ns |    20.332 ns |    19.019 ns |  4.29 |    0.11 |   0.4730 |  0.0076 |     - |    2968 B |
|         ImmutableDict_Builder_Add |    10 |   1,712.26 ns |    15.264 ns |    26.733 ns |  5.59 |    0.12 |   0.1259 |       - |     - |     800 B |
|                 ImmutableDict_Add |    10 |   2,986.38 ns |    17.824 ns |    14.884 ns |  9.70 |    0.18 |   0.4349 |       - |     - |    2744 B |
|                                   |       |               |              |              |       |         |          |         |       |           |
|          V4_ImHashMap_AddOrUpdate |   100 |   9,188.79 ns |   116.272 ns |   108.761 ns |  1.00 |    0.00 |   2.8229 |  0.0916 |     - |   17792 B |
|          V3_ImHashMap_AddOrUpdate |   100 |  10,591.30 ns |   169.894 ns |   141.869 ns |  1.15 |    0.03 |   3.1891 |  0.1068 |     - |   20032 B |
| V4_PartitionedHashMap_AddOrUpdate |   100 |   4,491.75 ns |    81.205 ns |    75.959 ns |  0.49 |    0.01 |   1.2360 |  0.0534 |     - |    7776 B |
|                   DictSlim_TryAdd |   100 |   3,201.77 ns |    48.216 ns |    42.743 ns |  0.35 |    0.01 |   1.1902 |  0.0305 |     - |    7488 B |
|                       Dict_TryAdd |   100 |   3,856.61 ns |    70.806 ns |    62.768 ns |  0.42 |    0.01 |   1.6174 |  0.0687 |     - |   10192 B |
|       ConcurrentDictionary_TryAdd |   100 |  16,035.70 ns |   320.350 ns |   356.068 ns |  1.75 |    0.04 |   4.9133 |  0.5798 |     - |   30824 B |
|         ImmutableDict_Builder_Add |   100 |  24,427.83 ns |   470.630 ns |   417.201 ns |  2.66 |    0.07 |   1.0376 |  0.0305 |     - |    6560 B |
|                 ImmutableDict_Add |   100 |  48,772.32 ns |   687.894 ns |   574.422 ns |  5.30 |    0.10 |   7.1411 |  0.2441 |     - |   44936 B |
|                                   |       |               |              |              |       |         |          |         |       |           |
|          V4_ImHashMap_AddOrUpdate |  1000 | 277,024.17 ns | 3,349.815 ns | 2,969.524 ns |  1.00 |    0.00 |  45.4102 | 10.7422 |     - |  286344 B |
|          V3_ImHashMap_AddOrUpdate |  1000 | 234,444.41 ns | 2,324.332 ns | 1,940.923 ns |  0.85 |    0.01 |  51.5137 | 12.2070 |     - |  324176 B |
| V4_PartitionedHashMap_AddOrUpdate |  1000 | 146,984.47 ns |   954.053 ns |   845.743 ns |  0.53 |    0.01 |  26.1230 |  7.5684 |     - |  164280 B |
|                   DictSlim_TryAdd |  1000 |  33,447.54 ns |   254.743 ns |   225.823 ns |  0.12 |    0.00 |   9.1553 |  1.7700 |     - |   57808 B |
|                       Dict_TryAdd |  1000 |  40,234.23 ns |   430.048 ns |   381.226 ns |  0.15 |    0.00 |  16.2354 |  5.3711 |     - |  102216 B |
|       ConcurrentDictionary_TryAdd |  1000 | 165,018.54 ns | 3,075.862 ns | 2,568.484 ns |  0.60 |    0.01 |  41.2598 | 13.6719 |     - |  259720 B |
|         ImmutableDict_Builder_Add |  1000 | 396,623.48 ns | 4,099.094 ns | 3,834.295 ns |  1.43 |    0.02 |   9.7656 |  2.4414 |     - |   64160 B |
|                 ImmutableDict_Add |  1000 | 813,397.08 ns | 9,203.453 ns | 8,608.916 ns |  2.94 |    0.04 | 105.4688 | 25.3906 |     - |  665001 B |
```

### Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs) lookups for the specific key in the 
`ImHashMap<Type, string>` containing the specified Count of elements.

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|             V4_ImHashMap_TryFind |     1 |  8.915 ns | 0.1853 ns | 0.1733 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |     1 |  7.834 ns | 0.1769 ns | 0.1568 ns |  0.88 |    0.02 |     - |     - |     - |         - |
|    V4_PartitionedHashMap_TryFind |     1 |  8.292 ns | 0.1082 ns | 0.0959 ns |  0.93 |    0.02 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |     1 |  7.681 ns | 0.1245 ns | 0.1039 ns |  0.86 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     1 |  8.861 ns | 0.1423 ns | 0.1188 ns |  0.99 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     1 | 17.914 ns | 0.3447 ns | 0.3055 ns |  2.01 |    0.05 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     1 | 13.381 ns | 0.2709 ns | 0.2401 ns |  1.50 |    0.04 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     1 | 19.040 ns | 0.3068 ns | 0.2870 ns |  2.14 |    0.06 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V4_ImHashMap_TryFind |    10 |  9.462 ns | 0.2690 ns | 0.2246 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |    10 |  9.038 ns | 0.1650 ns | 0.1463 ns |  0.96 |    0.02 |     - |     - |     - |         - |
|    V4_PartitionedHashMap_TryFind |    10 |  8.575 ns | 0.2031 ns | 0.1900 ns |  0.91 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |    10 |  5.869 ns | 0.1026 ns | 0.1743 ns |  0.63 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |    10 |  7.384 ns | 0.1882 ns | 0.1668 ns |  0.78 |    0.03 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    10 | 14.082 ns | 0.2168 ns | 0.1921 ns |  1.49 |    0.04 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    10 | 11.436 ns | 0.1398 ns | 0.1239 ns |  1.21 |    0.03 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    10 | 17.927 ns | 0.4467 ns | 0.4780 ns |  1.90 |    0.09 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V4_ImHashMap_TryFind |   100 | 12.147 ns | 0.1475 ns | 0.1308 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |   100 | 11.357 ns | 0.1695 ns | 0.1323 ns |  0.94 |    0.01 |     - |     - |     - |         - |
|    V4_PartitionedHashMap_TryFind |   100 |  8.520 ns | 0.2142 ns | 0.1899 ns |  0.70 |    0.02 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |   100 |  7.785 ns | 0.1374 ns | 0.1147 ns |  0.64 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |   100 |  6.918 ns | 0.1544 ns | 0.1289 ns |  0.57 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   100 | 13.804 ns | 0.3474 ns | 0.3717 ns |  1.14 |    0.04 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   100 | 11.432 ns | 0.2194 ns | 0.2052 ns |  0.94 |    0.02 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   100 | 21.593 ns | 0.4048 ns | 0.3786 ns |  1.78 |    0.03 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V4_ImHashMap_TryFind |  1000 | 15.492 ns | 0.3859 ns | 0.4881 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |  1000 | 14.339 ns | 0.2612 ns | 0.2444 ns |  0.92 |    0.03 |     - |     - |     - |         - |
|    V4_PartitionedHashMap_TryFind |  1000 | 11.508 ns | 0.1839 ns | 0.1630 ns |  0.74 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |  1000 |  9.508 ns | 0.2340 ns | 0.2074 ns |  0.61 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |  1000 |  7.099 ns | 0.1984 ns | 0.2037 ns |  0.46 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  1000 | 13.696 ns | 0.1341 ns | 0.1120 ns |  0.88 |    0.03 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  1000 | 11.358 ns | 0.1464 ns | 0.1222 ns |  0.73 |    0.03 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  1000 | 25.875 ns | 0.3752 ns | 0.3510 ns |  1.67 |    0.06 |     - |     - |     - |         - |
```

### Enumeration

[The benchmark source](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs)

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                          Method | Count |         Mean |      Error |     StdDev |       Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |------ |-------------:|-----------:|-----------:|-------------:|------:|--------:|-------:|------:|------:|----------:|
|          V4_ImHashMap_Enumerate |     1 |     39.51 ns |   0.865 ns |   0.996 ns |     39.43 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|          V3_ImHashMap_Enumerate |     1 |     44.11 ns |   0.879 ns |   0.822 ns |     44.28 ns |  1.12 |    0.04 | 0.0255 |     - |     - |     160 B |
| V4_PartitionedHashMap_Enumerate |     1 |    118.94 ns |   1.644 ns |   1.538 ns |    118.91 ns |  3.01 |    0.09 |      - |     - |     - |         - |
| V3_PartitionedHashMap_Enumerate |     1 |    180.82 ns |   3.462 ns |   3.238 ns |    181.09 ns |  4.57 |    0.14 | 0.0522 |     - |     - |     328 B |
|        DictionarySlim_Enumerate |     1 |     12.77 ns |   0.254 ns |   0.238 ns |     12.76 ns |  0.32 |    0.01 |      - |     - |     - |         - |
|            Dictionary_Enumerate |     1 |     15.24 ns |   0.652 ns |   1.817 ns |     14.36 ns |  0.46 |    0.05 |      - |     - |     - |         - |
|    ConcurrentDictionary_foreach |     1 |    177.04 ns |   2.073 ns |   1.939 ns |    176.68 ns |  4.47 |    0.12 | 0.0100 |     - |     - |      64 B |
|         ImmutableDict_Enumerate |     1 |    161.96 ns |   0.760 ns |   0.635 ns |    161.98 ns |  4.08 |    0.12 |      - |     - |     - |         - |
|                                 |       |              |            |            |              |       |         |        |       |       |           |
|          V4_ImHashMap_Enumerate |    10 |    191.91 ns |   3.010 ns |   2.668 ns |    192.57 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|          V3_ImHashMap_Enumerate |    10 |    223.10 ns |   1.885 ns |   1.574 ns |    223.12 ns |  1.16 |    0.02 | 0.0381 |     - |     - |     240 B |
| V4_PartitionedHashMap_Enumerate |    10 |    379.75 ns |   4.543 ns |   4.027 ns |    379.74 ns |  1.98 |    0.03 |      - |     - |     - |         - |
| V3_PartitionedHashMap_Enumerate |    10 |    604.47 ns |   4.257 ns |   3.774 ns |    603.87 ns |  3.15 |    0.05 | 0.1793 |     - |     - |    1128 B |
|        DictionarySlim_Enumerate |    10 |     73.15 ns |   1.438 ns |   1.345 ns |     72.82 ns |  0.38 |    0.01 |      - |     - |     - |         - |
|            Dictionary_Enumerate |    10 |     57.95 ns |   0.749 ns |   0.701 ns |     57.86 ns |  0.30 |    0.01 |      - |     - |     - |         - |
|    ConcurrentDictionary_foreach |    10 |    505.36 ns |   7.959 ns |   7.445 ns |    504.00 ns |  2.64 |    0.05 | 0.0095 |     - |     - |      64 B |
|         ImmutableDict_Enumerate |    10 |    556.35 ns |  10.730 ns |   9.512 ns |    558.53 ns |  2.90 |    0.06 |      - |     - |     - |         - |
|                                 |       |              |            |            |              |       |         |        |       |       |           |
|          V4_ImHashMap_Enumerate |   100 |  2,023.65 ns |  33.506 ns |  29.702 ns |  2,031.78 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|          V3_ImHashMap_Enumerate |   100 |  1,992.46 ns |  23.400 ns |  20.744 ns |  1,992.05 ns |  0.98 |    0.02 | 0.0381 |     - |     - |     240 B |
| V4_PartitionedHashMap_Enumerate |   100 |  2,626.85 ns |  42.089 ns |  37.311 ns |  2,616.34 ns |  1.30 |    0.02 |      - |     - |     - |         - |
| V3_PartitionedHashMap_Enumerate |   100 |  3,469.16 ns |  30.415 ns |  26.962 ns |  3,469.32 ns |  1.71 |    0.03 | 0.4349 |     - |     - |    2728 B |
|        DictionarySlim_Enumerate |   100 |    615.29 ns |  10.217 ns |   9.057 ns |    617.89 ns |  0.30 |    0.01 |      - |     - |     - |         - |
|            Dictionary_Enumerate |   100 |    578.30 ns |   3.310 ns |   2.764 ns |    578.14 ns |  0.29 |    0.00 |      - |     - |     - |         - |
|    ConcurrentDictionary_foreach |   100 |  3,444.60 ns |  55.779 ns |  52.176 ns |  3,425.75 ns |  1.70 |    0.03 | 0.0076 |     - |     - |      64 B |
|         ImmutableDict_Enumerate |   100 |  4,557.81 ns |  71.187 ns |  63.105 ns |  4,552.69 ns |  2.25 |    0.03 |      - |     - |     - |         - |
|                                 |       |              |            |            |              |       |         |        |       |       |           |
|          V4_ImHashMap_Enumerate |  1000 | 22,259.46 ns | 204.173 ns | 180.994 ns | 22,241.67 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|          V3_ImHashMap_Enumerate |  1000 | 21,710.99 ns | 288.512 ns | 240.921 ns | 21,770.78 ns |  0.98 |    0.02 | 0.0610 |     - |     - |     480 B |
| V4_PartitionedHashMap_Enumerate |  1000 | 28,097.70 ns | 392.513 ns | 306.449 ns | 28,134.91 ns |  1.26 |    0.02 |      - |     - |     - |         - |
| V3_PartitionedHashMap_Enumerate |  1000 | 31,132.68 ns | 473.705 ns | 443.104 ns | 31,208.71 ns |  1.40 |    0.01 | 0.4272 |     - |     - |    2728 B |
|        DictionarySlim_Enumerate |  1000 |  6,472.19 ns |  53.546 ns |  47.467 ns |  6,482.73 ns |  0.29 |    0.00 |      - |     - |     - |         - |
|            Dictionary_Enumerate |  1000 |  5,700.68 ns |  65.696 ns |  61.452 ns |  5,698.15 ns |  0.26 |    0.00 |      - |     - |     - |         - |
|    ConcurrentDictionary_foreach |  1000 | 43,550.74 ns | 848.746 ns | 752.391 ns | 43,765.79 ns |  1.96 |    0.04 |      - |     - |     - |      64 B |
|         ImmutableDict_Enumerate |  1000 | 46,089.57 ns | 524.157 ns | 464.651 ns | 46,183.41 ns |  2.07 |    0.03 |      - |     - |     - |         - |
```


### ImHashMap with int keys and small string values

`ImHashMap<int, string>` stores the `int` keys and `string` values.


#### Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) inserts from 1 to 10 000 of items into the `ImHashMap<int, string>`:

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                          Method | Count |             Mean |          Error |         StdDev |           Median | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|-------------------------------- |------ |-----------------:|---------------:|---------------:|-----------------:|------:|--------:|----------:|---------:|---------:|----------:|
|        V4_ImHashMap_AddOrUpdate |     1 |         13.30 ns |       0.296 ns |       0.277 ns |         13.30 ns |  1.00 |    0.00 |    0.0051 |        - |        - |      32 B |
|            V2_ImMap_AddOrUpdate |     1 |         17.09 ns |       0.207 ns |       0.184 ns |         17.08 ns |  1.28 |    0.03 |    0.0076 |        - |        - |      48 B |
| V4_PartitionedImMap_AddOrUpdate |     1 |         79.98 ns |       1.023 ns |       1.467 ns |         79.66 ns |  6.04 |    0.16 |    0.0293 |        - |        - |     184 B |
|       DictSlim_GetOrAddValueRef |     1 |         41.18 ns |       0.416 ns |       0.369 ns |         41.28 ns |  3.10 |    0.08 |    0.0204 |        - |        - |     128 B |
|                     Dict_TryAdd |     1 |         37.06 ns |       0.418 ns |       0.370 ns |         37.08 ns |  2.79 |    0.06 |    0.0344 |        - |        - |     216 B |
|           ConcurrentDict_TryAdd |     1 |        146.05 ns |       2.124 ns |       1.773 ns |        145.64 ns | 10.95 |    0.27 |    0.1376 |   0.0007 |        - |     864 B |
|       ImmutableDict_Builder_Add |     1 |        120.60 ns |       0.788 ns |       0.658 ns |        120.43 ns |  9.05 |    0.20 |    0.0253 |        - |        - |     160 B |
|               ImmutableDict_Add |     1 |        107.37 ns |       1.833 ns |       2.384 ns |        106.71 ns |  8.11 |    0.25 |    0.0166 |        - |        - |     104 B |
|                                 |       |                  |                |                |                  |       |         |           |          |          |           |
|        V4_ImHashMap_AddOrUpdate |    10 |        229.89 ns |       1.484 ns |       1.239 ns |        230.08 ns |  1.00 |    0.00 |    0.1173 |        - |        - |     736 B |
|            V2_ImMap_AddOrUpdate |    10 |        551.17 ns |       4.425 ns |       3.695 ns |        550.33 ns |  2.40 |    0.02 |    0.2823 |        - |        - |    1776 B |
| V4_PartitionedImMap_AddOrUpdate |    10 |        242.93 ns |       4.599 ns |       3.840 ns |        242.93 ns |  1.06 |    0.02 |    0.0749 |        - |        - |     472 B |
|       DictSlim_GetOrAddValueRef |    10 |        281.33 ns |       5.547 ns |       5.935 ns |        280.41 ns |  1.22 |    0.03 |    0.1326 |        - |        - |     832 B |
|                     Dict_TryAdd |    10 |        258.89 ns |       3.412 ns |       3.025 ns |        258.85 ns |  1.13 |    0.01 |    0.1578 |   0.0005 |        - |     992 B |
|           ConcurrentDict_TryAdd |    10 |        540.34 ns |       3.092 ns |       2.741 ns |        539.06 ns |  2.35 |    0.01 |    0.1945 |   0.0010 |        - |    1224 B |
|       ImmutableDict_Builder_Add |    10 |      1,514.60 ns |       8.675 ns |       7.244 ns |      1,513.34 ns |  6.59 |    0.05 |    0.1163 |        - |        - |     736 B |
|               ImmutableDict_Add |    10 |      2,632.30 ns |      51.955 ns |      48.599 ns |      2,639.68 ns | 11.45 |    0.22 |    0.4196 |        - |        - |    2640 B |
|                                 |       |                  |                |                |                  |       |         |           |          |          |           |
|        V4_ImHashMap_AddOrUpdate |   100 |      8,221.37 ns |     125.831 ns |     117.703 ns |      8,192.32 ns |  1.00 |    0.00 |    2.9602 |   0.1221 |        - |   18640 B |
|            V2_ImMap_AddOrUpdate |   100 |     12,184.44 ns |     179.889 ns |     168.268 ns |     12,140.26 ns |  1.48 |    0.02 |    5.9357 |   0.2441 |        - |   37296 B |
| V4_PartitionedImMap_AddOrUpdate |   100 |      3,370.45 ns |      23.906 ns |      18.664 ns |      3,363.21 ns |  0.41 |    0.01 |    1.4801 |   0.0839 |        - |    9304 B |
|       DictSlim_GetOrAddValueRef |   100 |      2,652.96 ns |      37.693 ns |      35.258 ns |      2,649.32 ns |  0.32 |    0.00 |    1.3275 |   0.0534 |        - |    8336 B |
|                     Dict_TryAdd |   100 |      2,828.22 ns |      21.898 ns |      19.412 ns |      2,826.77 ns |  0.34 |    0.01 |    2.0828 |   0.1297 |        - |   13072 B |
|           ConcurrentDict_TryAdd |   100 |     10,866.25 ns |     111.428 ns |     104.229 ns |     10,909.92 ns |  1.32 |    0.02 |    3.6316 |   0.3510 |        - |   22784 B |
|       ImmutableDict_Builder_Add |   100 |     25,598.91 ns |     273.166 ns |     255.520 ns |     25,525.67 ns |  3.11 |    0.06 |    1.4648 |   0.0916 |        - |    9376 B |
|               ImmutableDict_Add |   100 |     46,542.15 ns |     412.627 ns |     344.562 ns |     46,612.94 ns |  5.68 |    0.09 |    7.9346 |   0.3662 |        - |   49952 B |
|                                 |       |                  |                |                |                  |       |         |           |          |          |           |
|        V4_ImHashMap_AddOrUpdate |  1000 |    152,860.18 ns |   1,517.703 ns |   1,345.404 ns |    152,745.43 ns |  1.00 |    0.00 |   46.6309 |   0.4883 |        - |  293656 B |
|            V2_ImMap_AddOrUpdate |  1000 |    198,915.64 ns |   2,889.932 ns |   2,561.850 ns |    198,557.64 ns |  1.30 |    0.02 |   84.9609 |   0.2441 |        - |  534144 B |
| V4_PartitionedImMap_AddOrUpdate |  1000 |     83,983.62 ns |   1,391.788 ns |   1,301.880 ns |     83,699.54 ns |  0.55 |    0.01 |   27.0996 |   9.0332 |        - |  170456 B |
|       DictSlim_GetOrAddValueRef |  1000 |     26,659.61 ns |     349.284 ns |     326.721 ns |     26,615.57 ns |  0.17 |    0.00 |   11.6272 |   2.8992 |        - |   73120 B |
|                     Dict_TryAdd |  1000 |     31,317.33 ns |     475.471 ns |     444.756 ns |     31,251.40 ns |  0.21 |    0.00 |   21.2402 |   0.0610 |        - |  133896 B |
|           ConcurrentDict_TryAdd |  1000 |    112,582.19 ns |   1,221.657 ns |   1,082.967 ns |    112,203.70 ns |  0.74 |    0.01 |   32.7148 |   0.1221 |        - |  205368 B |
|       ImmutableDict_Builder_Add |  1000 |    350,888.85 ns |   3,757.786 ns |   3,515.035 ns |    352,417.77 ns |  2.30 |    0.03 |   15.1367 |   4.8828 |        - |   95776 B |
|               ImmutableDict_Add |  1000 |    692,782.78 ns |   5,756.492 ns |   5,102.980 ns |    694,477.10 ns |  4.53 |    0.05 |  112.3047 |   0.9766 |        - |  710209 B |
|                                 |       |                  |                |                |                  |       |         |           |          |          |           |
|        V4_ImHashMap_AddOrUpdate | 10000 |  3,879,586.59 ns |  64,823.212 ns |  60,635.673 ns |  3,882,537.11 ns |  1.00 |    0.00 |  632.8125 | 312.5000 |  46.8750 | 3993786 B |
|            V2_ImMap_AddOrUpdate | 10000 |  4,485,663.73 ns |  54,179.233 ns |  45,242.113 ns |  4,483,992.58 ns |  1.16 |    0.02 | 1109.3750 | 226.5625 | 101.5625 | 6972711 B |
| V4_PartitionedImMap_AddOrUpdate | 10000 |  2,822,478.66 ns |  49,570.016 ns |  85,505.842 ns |  2,806,964.45 ns |  0.73 |    0.04 |  433.5938 | 214.8438 |        - | 2720602 B |
|       DictSlim_GetOrAddValueRef | 10000 |    424,813.34 ns |   2,027.361 ns |   1,797.203 ns |    424,378.52 ns |  0.11 |    0.00 |  124.5117 | 124.5117 | 124.5117 |  975754 B |
|                     Dict_TryAdd | 10000 |    526,320.80 ns |   9,338.940 ns |   8,278.727 ns |    523,071.63 ns |  0.14 |    0.00 |  221.6797 | 221.6797 | 221.6797 | 1261763 B |
|           ConcurrentDict_TryAdd | 10000 |  2,721,195.59 ns |  62,415.534 ns | 182,069.041 ns |  2,805,224.80 ns |  0.72 |    0.04 |  273.4375 | 121.0938 |  42.9688 | 1645328 B |
|       ImmutableDict_Builder_Add | 10000 |  4,653,773.55 ns |  48,265.920 ns |  42,786.481 ns |  4,645,701.95 ns |  1.20 |    0.02 |  148.4375 |  70.3125 |        - |  959781 B |
|               ImmutableDict_Add | 10000 | 10,911,107.09 ns | 101,847.861 ns |  85,047.577 ns | 10,956,526.56 ns |  2.81 |    0.04 | 1468.7500 | 265.6250 | 125.0000 | 9271220 B |
```


#### Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) lookups for the last added index in the `ImHashMap<int, string>` 
containing the specified Count of elements.

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                        Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------ |------ |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|          V4_ImHashMap_TryFind |     1 |  2.5662 ns | 0.0840 ns | 0.0701 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedHashMap_TryFind |     1 |  2.9264 ns | 0.0775 ns | 0.0687 ns |  1.14 |    0.03 |     - |     - |     - |         - |
|              V2_ImMap_TryFind |     1 |  0.8574 ns | 0.0826 ns | 0.0772 ns |  0.33 |    0.02 |     - |     - |     - |         - |
|          DictSlim_TryGetValue |     1 |  3.7989 ns | 0.1118 ns | 0.1046 ns |  1.48 |    0.05 |     - |     - |     - |         - |
|              Dict_TryGetValue |     1 |  7.5973 ns | 0.2095 ns | 0.1857 ns |  2.95 |    0.08 |     - |     - |     - |         - |
|    ConcurrentDict_TryGetValue |     1 |  8.1365 ns | 0.1859 ns | 0.1552 ns |  3.17 |    0.08 |     - |     - |     - |         - |
|     ImmutableDict_TryGetValue |     1 | 15.7728 ns | 0.3912 ns | 0.4348 ns |  6.16 |    0.25 |     - |     - |     - |         - |
|                               |       |            |           |           |       |         |       |       |       |           |
|          V4_ImHashMap_TryFind |    10 |  3.0618 ns | 0.0535 ns | 0.0474 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedHashMap_TryFind |    10 |  2.5114 ns | 0.1019 ns | 0.0851 ns |  0.82 |    0.02 |     - |     - |     - |         - |
|              V2_ImMap_TryFind |    10 |  3.5793 ns | 0.1092 ns | 0.1022 ns |  1.17 |    0.04 |     - |     - |     - |         - |
|          DictSlim_TryGetValue |    10 |  4.0213 ns | 0.0329 ns | 0.0275 ns |  1.31 |    0.02 |     - |     - |     - |         - |
|              Dict_TryGetValue |    10 |  6.9583 ns | 0.0809 ns | 0.0717 ns |  2.27 |    0.03 |     - |     - |     - |         - |
|    ConcurrentDict_TryGetValue |    10 |  7.9926 ns | 0.0997 ns | 0.0884 ns |  2.61 |    0.05 |     - |     - |     - |         - |
|     ImmutableDict_TryGetValue |    10 | 17.5147 ns | 0.3430 ns | 0.3209 ns |  5.72 |    0.15 |     - |     - |     - |         - |
|                               |       |            |           |           |       |         |       |       |       |           |
|          V4_ImHashMap_TryFind |   100 |  6.9090 ns | 0.1161 ns | 0.1030 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedHashMap_TryFind |   100 |  2.6762 ns | 0.0542 ns | 0.1045 ns |  0.39 |    0.02 |     - |     - |     - |         - |
|              V2_ImMap_TryFind |   100 |  7.1365 ns | 0.1727 ns | 0.1442 ns |  1.03 |    0.03 |     - |     - |     - |         - |
|          DictSlim_TryGetValue |   100 |  4.4196 ns | 0.0861 ns | 0.0719 ns |  0.64 |    0.01 |     - |     - |     - |         - |
|              Dict_TryGetValue |   100 |  7.6746 ns | 0.0770 ns | 0.0643 ns |  1.11 |    0.02 |     - |     - |     - |         - |
|    ConcurrentDict_TryGetValue |   100 |  8.0521 ns | 0.1363 ns | 0.1138 ns |  1.17 |    0.03 |     - |     - |     - |         - |
|     ImmutableDict_TryGetValue |   100 | 19.5683 ns | 0.1622 ns | 0.1355 ns |  2.83 |    0.04 |     - |     - |     - |         - |
|                               |       |            |           |           |       |         |       |       |       |           |
|          V4_ImHashMap_TryFind |  1000 | 12.7430 ns | 0.2270 ns | 0.2123 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedHashMap_TryFind |  1000 |  7.0856 ns | 0.0987 ns | 0.0824 ns |  0.55 |    0.01 |     - |     - |     - |         - |
|              V2_ImMap_TryFind |  1000 | 10.9892 ns | 0.2986 ns | 0.3067 ns |  0.86 |    0.02 |     - |     - |     - |         - |
|          DictSlim_TryGetValue |  1000 |  4.3216 ns | 0.1560 ns | 0.1459 ns |  0.34 |    0.01 |     - |     - |     - |         - |
|              Dict_TryGetValue |  1000 |  7.9061 ns | 0.1667 ns | 0.1478 ns |  0.62 |    0.02 |     - |     - |     - |         - |
|    ConcurrentDict_TryGetValue |  1000 |  8.1644 ns | 0.1095 ns | 0.0914 ns |  0.64 |    0.02 |     - |     - |     - |         - |
|     ImmutableDict_TryGetValue |  1000 | 22.1566 ns | 0.2936 ns | 0.2603 ns |  1.74 |    0.03 |     - |     - |     - |         - |
|                               |       |            |           |           |       |         |       |       |       |           |
|          V4_ImHashMap_TryFind | 10000 | 17.4170 ns | 0.2906 ns | 0.2576 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V4_PartitionedHashMap_TryFind | 10000 | 11.0928 ns | 0.1834 ns | 0.1626 ns |  0.64 |    0.01 |     - |     - |     - |         - |
|              V2_ImMap_TryFind | 10000 | 15.6074 ns | 0.2325 ns | 0.2175 ns |  0.90 |    0.02 |     - |     - |     - |         - |
|          DictSlim_TryGetValue | 10000 |  4.4326 ns | 0.1312 ns | 0.1228 ns |  0.25 |    0.01 |     - |     - |     - |         - |
|              Dict_TryGetValue | 10000 |  7.5615 ns | 0.0669 ns | 0.0559 ns |  0.43 |    0.01 |     - |     - |     - |         - |
|    ConcurrentDict_TryGetValue | 10000 |  7.5147 ns | 0.1537 ns | 0.1437 ns |  0.43 |    0.01 |     - |     - |     - |         - |
|     ImmutableDict_TryGetValue | 10000 | 37.8435 ns | 0.4362 ns | 0.4080 ns |  2.17 |    0.04 |     - |     - |     - |         - |
```

**Interpreting results:** `ImHashMap` holds very good against `ImmutableDictionary` sibling and even against `Dictionary`(s) 
(especially if we are talking about PartitionedImHashMap) up to certain count, indicating that immutable collection could be 
quite fast for lookups.


#### Enumeration

[The benchmark source code](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs)

```md
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                        Method | Count |          Mean |        Error |        StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------ |------ |--------------:|-------------:|--------------:|------:|--------:|-------:|------:|------:|----------:|
|            V4_HashMap_foreach |     1 |      39.92 ns |     0.820 ns |      0.767 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| V4_PartitionedHashMap_foreach |     1 |     116.81 ns |     1.450 ns |      1.286 ns |  2.93 |    0.06 |      - |     - |     - |         - |
|            V2_ImMap_Enumerate |     1 |      23.93 ns |     0.346 ns |      0.306 ns |  0.60 |    0.01 | 0.0114 |     - |     - |      72 B |
|              DictSlim_foreach |     1 |      12.08 ns |     0.160 ns |      0.133 ns |  0.30 |    0.01 |      - |     - |     - |         - |
|                  Dict_foreach |     1 |      13.23 ns |     0.221 ns |      0.185 ns |  0.33 |    0.01 |      - |     - |     - |         - |
|        ConcurrentDict_foreach |     1 |     160.56 ns |     1.083 ns |      0.960 ns |  4.02 |    0.07 | 0.0100 |     - |     - |      64 B |
|         ImmutableDict_foreach |     1 |     162.69 ns |     2.671 ns |      2.367 ns |  4.08 |    0.09 |      - |     - |     - |         - |
|                               |       |               |              |               |       |         |        |       |       |           |
|            V4_HashMap_foreach |    10 |     153.45 ns |     0.997 ns |      0.884 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| V4_PartitionedHashMap_foreach |    10 |     404.07 ns |     3.534 ns |      3.133 ns |  2.63 |    0.02 |      - |     - |     - |         - |
|            V2_ImMap_Enumerate |    10 |     122.57 ns |     2.177 ns |      1.700 ns |  0.80 |    0.01 | 0.0176 |     - |     - |     112 B |
|              DictSlim_foreach |    10 |      53.15 ns |     0.771 ns |      0.721 ns |  0.35 |    0.01 |      - |     - |     - |         - |
|                  Dict_foreach |    10 |      51.03 ns |     0.977 ns |      0.816 ns |  0.33 |    0.01 |      - |     - |     - |         - |
|        ConcurrentDict_foreach |    10 |     282.17 ns |     4.126 ns |      3.860 ns |  1.84 |    0.03 | 0.0100 |     - |     - |      64 B |
|         ImmutableDict_foreach |    10 |     554.93 ns |     7.623 ns |      6.758 ns |  3.62 |    0.05 |      - |     - |     - |         - |
|                               |       |               |              |               |       |         |        |       |       |           |
|            V4_HashMap_foreach |   100 |   1,620.36 ns |    15.329 ns |     12.801 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| V4_PartitionedHashMap_foreach |   100 |   1,966.77 ns |    26.880 ns |     25.144 ns |  1.21 |    0.02 |      - |     - |     - |         - |
|            V2_ImMap_Enumerate |   100 |   1,148.46 ns |    12.437 ns |     11.025 ns |  0.71 |    0.01 | 0.0210 |     - |     - |     136 B |
|              DictSlim_foreach |   100 |     529.39 ns |     5.573 ns |      4.940 ns |  0.33 |    0.00 |      - |     - |     - |         - |
|                  Dict_foreach |   100 |     494.13 ns |     9.209 ns |      8.614 ns |  0.31 |    0.01 |      - |     - |     - |         - |
|        ConcurrentDict_foreach |   100 |   2,370.15 ns |    33.237 ns |     31.090 ns |  1.46 |    0.02 | 0.0076 |     - |     - |      64 B |
|         ImmutableDict_foreach |   100 |   4,524.35 ns |    40.547 ns |     33.858 ns |  2.79 |    0.03 |      - |     - |     - |         - |
|                               |       |               |              |               |       |         |        |       |       |           |
|            V4_HashMap_foreach |  1000 |  17,206.01 ns |   152.714 ns |    135.377 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| V4_PartitionedHashMap_foreach |  1000 |  21,030.80 ns |   325.220 ns |    288.299 ns |  1.22 |    0.02 |      - |     - |     - |         - |
|            V2_ImMap_Enumerate |  1000 |  12,200.97 ns |   173.829 ns |    162.600 ns |  0.71 |    0.01 | 0.0153 |     - |     - |     160 B |
|              DictSlim_foreach |  1000 |   4,913.65 ns |    76.788 ns |     71.827 ns |  0.29 |    0.00 |      - |     - |     - |         - |
|                  Dict_foreach |  1000 |   4,963.77 ns |    69.813 ns |     65.303 ns |  0.29 |    0.00 |      - |     - |     - |         - |
|        ConcurrentDict_foreach |  1000 |  21,569.87 ns |   162.467 ns |    144.023 ns |  1.25 |    0.01 |      - |     - |     - |      64 B |
|         ImmutableDict_foreach |  1000 |  47,263.06 ns |   907.028 ns |    804.057 ns |  2.75 |    0.05 |      - |     - |     - |         - |
|                               |       |               |              |               |       |         |        |       |       |           |
|            V4_HashMap_foreach | 10000 | 182,837.99 ns | 2,052.717 ns |  1,819.680 ns |  1.00 |    0.00 |      - |     - |     - |     176 B |
| V4_PartitionedHashMap_foreach | 10000 | 232,269.89 ns | 2,514.057 ns |  2,099.352 ns |  1.27 |    0.02 |      - |     - |     - |         - |
|            V2_ImMap_Enumerate | 10000 | 138,194.71 ns | 1,605.065 ns |  1,340.302 ns |  0.76 |    0.01 |      - |     - |     - |     192 B |
|              DictSlim_foreach | 10000 |  49,479.00 ns |   937.417 ns |  1,003.025 ns |  0.27 |    0.00 |      - |     - |     - |         - |
|                  Dict_foreach | 10000 |  46,780.99 ns |   380.189 ns |    355.629 ns |  0.26 |    0.00 |      - |     - |     - |         - |
|        ConcurrentDict_foreach | 10000 | 194,166.07 ns |   919.960 ns |    860.531 ns |  1.06 |    0.01 |      - |     - |     - |      64 B |
|         ImmutableDict_foreach | 10000 | 507,513.23 ns | 9,995.314 ns | 12,640.872 ns |  2.80 |    0.07 |      - |     - |     - |       1 B |
****```


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
