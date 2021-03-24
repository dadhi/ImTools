# ImTools

[![license](https://img.shields.io/github/license/dadhi/ImTools.svg)](http://opensource.org/licenses/MIT)

- Windows, Linux, MacOS [![Windows build](https://ci.appveyor.com/api/projects/status/el9echuqfnl86u53?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/imtools/branch/master)

- Lib package [![NuGet Badge](https://buildstats.info/nuget/ImTools.dll)](https://www.nuget.org/packages/ImTools.dll)
- Code package [![NuGet Badge](https://buildstats.info/nuget/ImTools)](https://www.nuget.org/packages/ImTools)

Fast and memory-efficient immutable collections and helper data structures.

Split from the [DryIoc](https://github.com/dadhi/dryioc).


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
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
```


### ImMap V2 with small string values

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


### ImMap Lookup

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

### ImMap Enumeration

[The benchmark source code](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs)

```md

|                               Method | Count |            Mean |        Error |       StdDev |          Median | Ratio | RatioSD |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|------------------------------------- |------ |----------------:|-------------:|-------------:|----------------:|------:|--------:|--------:|--------:|--------:|----------:|
|            V2_ImMap_EnumerateToArray |     1 |       120.53 ns |     2.479 ns |     2.952 ns |       120.54 ns |  1.00 |    0.00 |  0.0254 |       - |       - |     160 B |
|            V3_ImMap_EnumerateToArray |     1 |       133.89 ns |     2.773 ns |     3.702 ns |       132.99 ns |  1.11 |    0.04 |  0.0393 |       - |       - |     248 B |
| V3_PartitionedImMap_EnumerateToArray |     1 |       311.94 ns |     6.058 ns |     6.976 ns |       309.88 ns |  2.59 |    0.08 |  0.0663 |       - |       - |     416 B |
|                     V3_ImMap_ToArray |     1 |        41.38 ns |     0.400 ns |     0.334 ns |        41.41 ns |  0.34 |    0.01 |  0.0051 |       - |       - |      32 B |
|                     DictSlim_ToArray |     1 |       113.57 ns |     2.251 ns |     2.679 ns |       113.08 ns |  0.94 |    0.03 |  0.0279 |       - |       - |     176 B |
|                         Dict_ToArray |     1 |        27.08 ns |     0.625 ns |     0.790 ns |        26.95 ns |  0.23 |    0.01 |  0.0063 |       - |       - |      40 B |
|               ConcurrentDict_ToArray |     1 |       230.44 ns |     4.298 ns |     3.589 ns |       230.45 ns |  1.92 |    0.05 |  0.0062 |       - |       - |      40 B |
|                ImmutableDict_ToArray |     1 |       343.74 ns |     6.860 ns |     8.675 ns |       347.53 ns |  2.85 |    0.08 |  0.0062 |       - |       - |      40 B |
|                                      |       |                 |              |              |                 |       |         |         |         |         |           |
|            V2_ImMap_EnumerateToArray |    10 |       312.44 ns |     5.989 ns |     7.995 ns |       309.47 ns |  1.00 |    0.00 |  0.0710 |       - |       - |     448 B |
|            V3_ImMap_EnumerateToArray |    10 |       378.72 ns |     7.342 ns |     8.740 ns |       381.29 ns |  1.21 |    0.03 |  0.0916 |       - |       - |     576 B |
| V3_PartitionedImMap_EnumerateToArray |    10 |     1,081.30 ns |    21.363 ns |    33.260 ns |     1,078.04 ns |  3.44 |    0.12 |  0.3338 |       - |       - |    2104 B |
|                     V3_ImMap_ToArray |    10 |       151.94 ns |     2.339 ns |     2.188 ns |       152.43 ns |  0.49 |    0.02 |  0.0293 |       - |       - |     184 B |
|                     DictSlim_ToArray |    10 |       364.54 ns |     7.310 ns |     9.758 ns |       362.54 ns |  1.17 |    0.05 |  0.0992 |       - |       - |     624 B |
|                         Dict_ToArray |    10 |        62.54 ns |     1.322 ns |     1.719 ns |        62.93 ns |  0.20 |    0.01 |  0.0293 |       - |       - |     184 B |
|               ConcurrentDict_ToArray |    10 |       257.11 ns |     5.035 ns |     5.993 ns |       254.92 ns |  0.82 |    0.02 |  0.0291 |       - |       - |     184 B |
|                ImmutableDict_ToArray |    10 |     1,306.87 ns |    25.536 ns |    29.407 ns |     1,304.89 ns |  4.18 |    0.16 |  0.0286 |       - |       - |     184 B |
|                                      |       |                 |              |              |                 |       |         |         |         |         |           |
|            V2_ImMap_EnumerateToArray |   100 |     1,837.48 ns |    35.990 ns |    31.904 ns |     1,824.10 ns |  1.00 |    0.00 |  0.3510 |       - |       - |    2216 B |
|            V3_ImMap_EnumerateToArray |   100 |     2,266.73 ns |    26.443 ns |    24.734 ns |     2,261.32 ns |  1.24 |    0.03 |  0.3700 |       - |       - |    2344 B |
| V3_PartitionedImMap_EnumerateToArray |   100 |     3,721.44 ns |    46.355 ns |    41.093 ns |     3,723.51 ns |  2.03 |    0.05 |  0.7629 |       - |       - |    4808 B |
|                     V3_ImMap_ToArray |   100 |     1,145.83 ns |    22.985 ns |    29.068 ns |     1,134.75 ns |  0.62 |    0.02 |  0.1469 |       - |       - |     928 B |
|                     DictSlim_ToArray |   100 |     2,257.04 ns |    44.503 ns |    54.654 ns |     2,266.53 ns |  1.23 |    0.04 |  0.6332 |  0.0038 |       - |    3984 B |
|                         Dict_ToArray |   100 |       413.18 ns |     6.564 ns |     6.140 ns |       415.57 ns |  0.22 |    0.01 |  0.2584 |  0.0014 |       - |    1624 B |
|               ConcurrentDict_ToArray |   100 |     2,171.19 ns |    43.325 ns |    54.792 ns |     2,167.38 ns |  1.18 |    0.04 |  0.2556 |       - |       - |    1624 B |
|                ImmutableDict_ToArray |   100 |    11,128.43 ns |   221.906 ns |   332.138 ns |    11,084.63 ns |  5.95 |    0.19 |  0.2441 |       - |       - |    1624 B |
|                                      |       |                 |              |              |                 |       |         |         |         |         |           |
|            V2_ImMap_EnumerateToArray |  1000 |    18,101.27 ns |   358.107 ns |   490.180 ns |    18,256.30 ns |  1.00 |    0.00 |  2.6550 |  0.0916 |       - |   16768 B |
|            V3_ImMap_EnumerateToArray |  1000 |    20,063.32 ns |   393.057 ns |   420.567 ns |    19,878.51 ns |  1.10 |    0.04 |  2.6855 |  0.0916 |       - |   16960 B |
| V3_PartitionedImMap_EnumerateToArray |  1000 |    31,333.21 ns |   619.406 ns | 1,068.445 ns |    31,364.79 ns |  1.72 |    0.09 |  3.1128 |  0.1221 |       - |   19720 B |
|                     V3_ImMap_ToArray |  1000 |    11,539.09 ns |   222.229 ns |   272.918 ns |    11,450.40 ns |  0.64 |    0.02 |  1.2970 |  0.0305 |       - |    8216 B |
|                     DictSlim_ToArray |  1000 |    18,754.26 ns |   158.901 ns |   140.862 ns |    18,771.62 ns |  1.03 |    0.03 |  5.2185 |  0.3052 |       - |   32880 B |
|                         Dict_ToArray |  1000 |     3,702.92 ns |    69.709 ns |    77.481 ns |     3,682.60 ns |  0.20 |    0.00 |  2.5406 |  0.1373 |       - |   16024 B |
|               ConcurrentDict_ToArray |  1000 |    17,757.47 ns |   331.034 ns |   309.650 ns |    17,626.61 ns |  0.97 |    0.03 |  2.5330 |  0.1221 |       - |   16024 B |
|                ImmutableDict_ToArray |  1000 |   114,616.88 ns | 2,242.078 ns | 2,669.034 ns |   113,732.13 ns |  6.30 |    0.15 |  2.4414 |       - |       - |   16024 B |
|                                      |       |                 |              |              |                 |       |         |         |         |         |           |
|            V2_ImMap_EnumerateToArray | 10000 |   182,338.34 ns | 1,752.292 ns | 1,553.361 ns |   181,912.41 ns |  1.00 |    0.00 | 33.2031 |  8.0566 |       - |  211928 B |
|            V3_ImMap_EnumerateToArray | 10000 |   210,832.37 ns | 4,117.962 ns | 5,057.228 ns |   208,783.36 ns |  1.15 |    0.03 | 33.6914 | 11.2305 |       - |  212240 B |
| V3_PartitionedImMap_EnumerateToArray | 10000 |   298,204.70 ns | 5,881.050 ns | 9,662.734 ns |   292,544.24 ns |  1.66 |    0.06 | 33.6914 | 11.2305 |       - |  214936 B |
|                     V3_ImMap_ToArray | 10000 |   138,088.22 ns | 2,738.080 ns | 3,043.369 ns |   137,314.33 ns |  0.76 |    0.02 | 12.4512 |  2.4414 |       - |   80368 B |
|                     DictSlim_ToArray | 10000 |   279,957.77 ns | 4,047.472 ns | 3,786.007 ns |   279,674.80 ns |  1.54 |    0.03 | 51.7578 | 38.5742 | 31.2500 |  422987 B |
|                         Dict_ToArray | 10000 |    88,896.39 ns | 1,749.644 ns | 1,551.013 ns |    89,092.48 ns |  0.49 |    0.01 |  0.8545 |  0.8545 |  0.8545 |  160017 B |
|               ConcurrentDict_ToArray | 10000 |   141,896.48 ns | 2,818.435 ns | 5,224.151 ns |   141,953.37 ns |  0.79 |    0.03 |  4.6387 |  4.6387 |  4.6387 |  160020 B |
|                ImmutableDict_ToArray | 10000 | 1,143,183.73 ns | 8,822.408 ns | 7,820.835 ns | 1,143,611.23 ns |  6.27 |    0.08 | 33.2031 | 33.2031 | 33.2031 |  160022 B |
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
|                ImHashMap_TryFind |     1 |  4.094 ns | 0.0632 ns | 0.0591 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_TryFind_V1 |     1 |  5.340 ns | 0.0315 ns | 0.0295 ns |  1.30 |    0.02 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |     1 |  2.880 ns | 0.0316 ns | 0.0264 ns |  0.70 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     1 |  6.372 ns | 0.0207 ns | 0.0172 ns |  1.56 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     1 | 16.292 ns | 0.3957 ns | 0.3701 ns |  3.98 |    0.11 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     1 | 15.540 ns | 0.0507 ns | 0.0475 ns |  3.80 |    0.06 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     1 | 23.682 ns | 0.1103 ns | 0.0978 ns |  5.78 |    0.08 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|                ImHashMap_TryFind |    10 |  5.840 ns | 0.0366 ns | 0.0325 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_TryFind_V1 |    10 |  6.560 ns | 0.0558 ns | 0.0522 ns |  1.12 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |    10 |  2.947 ns | 0.0483 ns | 0.0452 ns |  0.50 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |    10 |  6.567 ns | 0.0103 ns | 0.0096 ns |  1.12 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    10 | 18.852 ns | 0.3877 ns | 0.3626 ns |  3.23 |    0.07 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    10 | 15.723 ns | 0.0745 ns | 0.0660 ns |  2.69 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    10 | 25.695 ns | 0.1539 ns | 0.1440 ns |  4.40 |    0.03 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|                ImHashMap_TryFind |   100 |  7.733 ns | 0.0366 ns | 0.0306 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_TryFind_V1 |   100 |  9.432 ns | 0.0590 ns | 0.0552 ns |  1.22 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |   100 |  5.618 ns | 0.0106 ns | 0.0099 ns |  0.73 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |   100 |  6.286 ns | 0.0638 ns | 0.0566 ns |  0.81 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   100 | 18.963 ns | 0.3079 ns | 0.2571 ns |  2.45 |    0.04 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   100 | 16.410 ns | 0.0628 ns | 0.0557 ns |  2.12 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   100 | 28.138 ns | 0.1037 ns | 0.0970 ns |  3.64 |    0.02 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|                ImHashMap_TryFind |  1000 | 12.019 ns | 0.0553 ns | 0.0518 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_TryFind_V1 |  1000 | 12.757 ns | 0.0509 ns | 0.0425 ns |  1.06 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |  1000 |  9.052 ns | 0.0212 ns | 0.0198 ns |  0.75 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |  1000 |  6.293 ns | 0.0627 ns | 0.0586 ns |  0.52 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  1000 | 16.499 ns | 0.0173 ns | 0.0161 ns |  1.37 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  1000 | 16.311 ns | 0.1058 ns | 0.0990 ns |  1.36 |    0.00 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  1000 | 30.316 ns | 0.5058 ns | 0.4731 ns |  2.52 |    0.04 |     - |     - |     - |         - |
```
    
### ImHashMap Enumeration


[The benchmark source](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks.cs)

```md
|                        Method | Count |          Mean |        Error |       StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------------------------ |------ |--------------:|-------------:|-------------:|------:|--------:|--------:|-------:|------:|----------:|
|    ImHashMap_EnumerateToArray |     1 |     151.24 ns |     1.077 ns |     1.008 ns |  1.00 |    0.00 |  0.0441 |      - |     - |     208 B |
| ImHashMap_V1_EnumerateToArray |     1 |     160.10 ns |     1.006 ns |     0.840 ns |  1.06 |    0.01 |  0.0560 |      - |     - |     264 B |
|         ImHashMap_FoldToArray |     1 |      56.37 ns |     0.115 ns |     0.096 ns |  0.37 |    0.00 |  0.0356 |      - |     - |     168 B |
|    ImHashMapSlots_FoldToArray |     1 |      71.05 ns |     0.351 ns |     0.328 ns |  0.47 |    0.00 |  0.0271 |      - |     - |     128 B |
|        DictionarySlim_ToArray |     1 |     141.78 ns |     0.651 ns |     0.577 ns |  0.94 |    0.01 |  0.0405 |      - |     - |     192 B |
|            Dictionary_ToArray |     1 |      39.32 ns |     0.178 ns |     0.158 ns |  0.26 |    0.00 |  0.0119 |      - |     - |      56 B |
|  ConcurrentDictionary_ToArray |     1 |     228.28 ns |     0.390 ns |     0.365 ns |  1.51 |    0.01 |  0.0117 |      - |     - |      56 B |
|         ImmutableDict_ToArray |     1 |     612.37 ns |     4.531 ns |     4.238 ns |  4.05 |    0.03 |  0.0114 |      - |     - |      56 B |
|                               |       |               |              |              |       |         |         |        |       |           |
|    ImHashMap_EnumerateToArray |    10 |     406.74 ns |     1.278 ns |     1.067 ns |  1.00 |    0.00 |  0.1001 |      - |     - |     472 B |
| ImHashMap_V1_EnumerateToArray |    10 |     473.14 ns |     3.844 ns |     3.408 ns |  1.16 |    0.01 |  0.1726 |      - |     - |     816 B |
|         ImHashMap_FoldToArray |    10 |     208.65 ns |     0.615 ns |     0.575 ns |  0.51 |    0.00 |  0.1054 |      - |     - |     496 B |
|    ImHashMapSlots_FoldToArray |    10 |     234.16 ns |     1.495 ns |     1.325 ns |  0.58 |    0.00 |  0.1016 |      - |     - |     480 B |
|        DictionarySlim_ToArray |    10 |     424.86 ns |     0.893 ns |     0.791 ns |  1.04 |    0.00 |  0.1359 |      - |     - |     640 B |
|            Dictionary_ToArray |    10 |      87.90 ns |     0.570 ns |     0.505 ns |  0.22 |    0.00 |  0.0424 |      - |     - |     200 B |
|  ConcurrentDictionary_ToArray |    10 |     507.57 ns |     5.354 ns |     5.008 ns |  1.25 |    0.01 |  0.0420 |      - |     - |     200 B |
|         ImmutableDict_ToArray |    10 |   2,041.72 ns |     5.804 ns |     5.145 ns |  5.02 |    0.01 |  0.0381 |      - |     - |     200 B |
|                               |       |               |              |              |       |         |         |        |       |           |
|    ImHashMap_EnumerateToArray |   100 |   2,892.03 ns |    17.595 ns |    16.458 ns |  1.00 |    0.00 |  0.4768 |      - |     - |    2248 B |
| ImHashMap_V1_EnumerateToArray |   100 |   3,539.25 ns |    12.053 ns |    11.274 ns |  1.22 |    0.01 |  1.1597 | 0.0267 |     - |    5472 B |
|         ImHashMap_FoldToArray |   100 |   1,456.77 ns |     7.952 ns |     6.641 ns |  0.50 |    0.00 |  0.6599 | 0.0038 |     - |    3112 B |
|    ImHashMapSlots_FoldToArray |   100 |   1,537.36 ns |     8.437 ns |     7.045 ns |  0.53 |    0.00 |  0.6733 | 0.0038 |     - |    3168 B |
|        DictionarySlim_ToArray |   100 |   2,538.43 ns |    15.330 ns |    14.340 ns |  0.88 |    0.01 |  0.8469 | 0.0076 |     - |    4000 B |
|            Dictionary_ToArray |   100 |     658.39 ns |     2.193 ns |     2.051 ns |  0.23 |    0.00 |  0.3481 | 0.0019 |     - |    1640 B |
|  ConcurrentDictionary_ToArray |   100 |   2,288.25 ns |     7.825 ns |     7.320 ns |  0.79 |    0.01 |  0.3471 |      - |     - |    1640 B |
|         ImmutableDict_ToArray |   100 |  16,009.36 ns |    46.706 ns |    41.404 ns |  5.53 |    0.03 |  0.3357 |      - |     - |    1640 B |
|                               |       |               |              |              |       |         |         |        |       |           |
|    ImHashMap_EnumerateToArray |  1000 |  27,630.71 ns |   160.441 ns |   150.077 ns |  1.00 |    0.00 |  3.5706 | 0.1526 |     - |   16808 B |
| ImHashMap_V1_EnumerateToArray |  1000 |  34,720.95 ns |    85.476 ns |    79.954 ns |  1.26 |    0.01 | 10.3149 | 1.7090 |     - |   48832 B |
|         ImHashMap_FoldToArray |  1000 |  14,661.14 ns |    64.614 ns |    57.279 ns |  0.53 |    0.00 |  5.2490 | 0.3204 |     - |   24752 B |
|    ImHashMapSlots_FoldToArray |  1000 |  15,407.60 ns |    25.918 ns |    21.643 ns |  0.56 |    0.00 |  5.2490 | 0.2747 |     - |   24784 B |
|        DictionarySlim_ToArray |  1000 |  22,157.65 ns |    40.801 ns |    36.169 ns |  0.80 |    0.01 |  6.9885 | 0.6714 |     - |   32896 B |
|            Dictionary_ToArray |  1000 |   6,235.23 ns |    24.355 ns |    21.590 ns |  0.23 |    0.00 |  3.3951 | 0.1831 |     - |   16040 B |
|  ConcurrentDictionary_ToArray |  1000 |  38,018.12 ns |   173.550 ns |   162.339 ns |  1.38 |    0.01 |  3.3569 | 0.1831 |     - |   16040 B |
|         ImmutableDict_ToArray |  1000 | 158,162.55 ns | 2,083.502 ns | 1,948.910 ns |  5.72 |    0.08 |  3.1738 |      - |     - |   16041 B |
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
