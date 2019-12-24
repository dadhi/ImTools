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
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
```


### ImMap V2 with string values

`ImMap<string>` stores the `int` keys and `string` values.


#### ImMap Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) inserts from 10 to 100 000 `Count` of items into the `ImMap<string>`, 
where value is `i.ToString()`:

```md
|                              Method | Count |            Mean |         Error |        StdDev | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------------------ |------ |----------------:|--------------:|--------------:|------:|--------:|----------:|---------:|---------:|----------:|
|                   ImMap_AddOrUpdate |     1 |        22.97 ns |      0.173 ns |      0.162 ns |  1.00 |    0.00 |    0.0102 |        - |        - |      48 B |
|                ImMap_V1_AddOrUpdate |     1 |        29.29 ns |      0.640 ns |      0.567 ns |  1.27 |    0.03 |    0.0102 |        - |        - |      48 B |
|      Experimental_ImMap_AddOrUpdate |     1 |        20.03 ns |      0.065 ns |      0.061 ns |  0.87 |    0.01 |    0.0068 |        - |        - |      32 B |
|              ImMapSlots_AddOrUpdate |     1 |       121.50 ns |      0.754 ns |      0.705 ns |  5.29 |    0.06 |    0.0696 |        - |        - |     328 B |
| Experimental_ImMapSlots_AddOrUpdate |     1 |       120.10 ns |      0.511 ns |      0.478 ns |  5.23 |    0.03 |    0.0663 |        - |        - |     312 B |
|           DictSlim_GetOrAddValueRef |     1 |        54.95 ns |      0.483 ns |      0.452 ns |  2.39 |    0.02 |    0.0272 |        - |        - |     128 B |
|                         Dict_TryAdd |     1 |        48.57 ns |      0.329 ns |      0.308 ns |  2.11 |    0.02 |    0.0442 |        - |        - |     208 B |
|               ConcurrentDict_TryAdd |     1 |       182.06 ns |      1.517 ns |      1.419 ns |  7.92 |    0.06 |    0.1853 |   0.0012 |        - |     872 B |
|           ImmutableDict_Builder_Add |     1 |       172.85 ns |      0.542 ns |      0.480 ns |  7.52 |    0.05 |    0.0339 |        - |        - |     160 B |
|                                     |       |                 |               |               |       |         |           |          |          |           |
|                   ImMap_AddOrUpdate |    10 |       686.46 ns |      2.200 ns |      1.950 ns |  1.00 |    0.00 |    0.3767 |   0.0010 |        - |    1776 B |
|                ImMap_V1_AddOrUpdate |    10 |       799.80 ns |      2.193 ns |      1.944 ns |  1.17 |    0.00 |    0.4787 |   0.0010 |        - |    2256 B |
|      Experimental_ImMap_AddOrUpdate |    10 |       468.87 ns |      5.109 ns |      4.779 ns |  0.68 |    0.01 |    0.2651 |   0.0005 |        - |    1248 B |
|              ImMapSlots_AddOrUpdate |    10 |       389.85 ns |      1.548 ns |      1.372 ns |  0.57 |    0.00 |    0.1612 |   0.0005 |        - |     760 B |
| Experimental_ImMapSlots_AddOrUpdate |    10 |       354.67 ns |      1.254 ns |      0.979 ns |  0.52 |    0.00 |    0.1273 |   0.0005 |        - |     600 B |
|           DictSlim_GetOrAddValueRef |    10 |       381.54 ns |      2.110 ns |      1.974 ns |  0.56 |    0.00 |    0.1764 |   0.0005 |        - |     832 B |
|                         Dict_TryAdd |    10 |       408.88 ns |      1.777 ns |      1.576 ns |  0.60 |    0.00 |    0.2089 |   0.0010 |        - |     984 B |
|               ConcurrentDict_TryAdd |    10 |       702.58 ns |      3.361 ns |      3.144 ns |  1.02 |    0.01 |    0.2613 |   0.0019 |        - |    1232 B |
|           ImmutableDict_Builder_Add |    10 |     2,257.08 ns |     10.901 ns |      9.664 ns |  3.29 |    0.02 |    0.1564 |        - |        - |     736 B |
|                                     |       |                 |               |               |       |         |           |          |          |           |
|                   ImMap_AddOrUpdate |   100 |    12,689.70 ns |     47.018 ns |     41.680 ns |  1.00 |    0.00 |    7.9193 |   0.3204 |        - |   37296 B |
|                ImMap_V1_AddOrUpdate |   100 |    15,030.34 ns |     29.405 ns |     27.505 ns |  1.18 |    0.00 |    9.3689 |   0.3967 |        - |   44112 B |
|      Experimental_ImMap_AddOrUpdate |   100 |    10,264.56 ns |     28.523 ns |     23.818 ns |  0.81 |    0.00 |    6.4545 |   0.2747 |        - |   30432 B |
|              ImMapSlots_AddOrUpdate |   100 |     5,295.24 ns |     36.521 ns |     34.162 ns |  0.42 |    0.00 |    2.7466 |   0.1678 |        - |   12952 B |
| Experimental_ImMapSlots_AddOrUpdate |   100 |     3,874.89 ns |     22.521 ns |     18.806 ns |  0.31 |    0.00 |    1.9608 |   0.1144 |        - |    9240 B |
|           DictSlim_GetOrAddValueRef |   100 |     3,569.39 ns |      9.148 ns |      7.639 ns |  0.28 |    0.00 |    1.7700 |   0.0725 |        - |    8336 B |
|                         Dict_TryAdd |   100 |     4,193.74 ns |     16.308 ns |     14.457 ns |  0.33 |    0.00 |    2.7695 |   0.1678 |        - |   13064 B |
|               ConcurrentDict_TryAdd |   100 |    12,175.96 ns |     75.325 ns |     70.460 ns |  0.96 |    0.01 |    4.8370 |   0.4730 |        - |   22768 B |
|           ImmutableDict_Builder_Add |   100 |    35,393.87 ns |    101.789 ns |     90.233 ns |  2.79 |    0.01 |    1.9531 |   0.1221 |        - |    9376 B |
|                                     |       |                 |               |               |       |         |           |          |          |           |
|                   ImMap_AddOrUpdate |  1000 |   206,140.98 ns |  1,184.439 ns |  1,107.925 ns |  1.00 |    0.00 |  113.5254 |   0.2441 |        - |  534144 B |
|                ImMap_V1_AddOrUpdate |  1000 |   238,218.53 ns |  1,298.955 ns |  1,215.043 ns |  1.16 |    0.01 |  128.6621 |   0.2441 |        - |  605617 B |
|      Experimental_ImMap_AddOrUpdate |  1000 |   177,492.40 ns |    763.021 ns |    713.730 ns |  0.86 |    0.01 |   98.1445 |   0.2441 |        - |  462624 B |
|              ImMapSlots_AddOrUpdate |  1000 |   123,577.77 ns |    339.036 ns |    317.135 ns |  0.60 |    0.00 |   61.2793 |   0.4883 |        - |  289240 B |
| Experimental_ImMapSlots_AddOrUpdate |  1000 |   102,987.50 ns |    414.206 ns |    367.183 ns |  0.50 |    0.00 |   47.8516 |   0.1221 |        - |  225496 B |
|           DictSlim_GetOrAddValueRef |  1000 |    33,273.14 ns |     51.326 ns |     48.011 ns |  0.16 |    0.00 |   15.5029 |   0.0610 |        - |   73120 B |
|                         Dict_TryAdd |  1000 |    43,526.92 ns |    853.197 ns |    837.953 ns |  0.21 |    0.00 |   28.2593 |   2.7466 |        - |  133888 B |
|               ConcurrentDict_TryAdd |  1000 |   124,512.55 ns |  1,670.588 ns |  1,562.669 ns |  0.60 |    0.01 |   43.2129 |   1.2207 |        - |  205328 B |
|           ImmutableDict_Builder_Add |  1000 |   467,816.87 ns |    959.187 ns |    850.295 ns |  2.27 |    0.01 |   20.0195 |   0.4883 |        - |   95777 B |
|                                     |       |                 |               |               |       |         |           |          |          |           |
|                   ImMap_AddOrUpdate | 10000 | 4,481,310.31 ns | 31,873.881 ns | 29,814.848 ns |  1.00 |    0.00 | 1109.3750 | 234.3750 | 101.5625 | 6972682 B |
|                ImMap_V1_AddOrUpdate | 10000 | 4,938,525.26 ns | 14,305.510 ns | 13,381.383 ns |  1.10 |    0.01 | 1226.5625 | 226.5625 | 101.5625 | 7691995 B |
|      Experimental_ImMap_AddOrUpdate | 10000 | 4,351,290.73 ns | 30,875.690 ns | 28,881.140 ns |  0.97 |    0.01 |  992.1875 | 328.1250 | 109.3750 | 6253354 B |
|              ImMapSlots_AddOrUpdate | 10000 | 4,232,391.63 ns | 12,653.983 ns | 11,217.426 ns |  0.94 |    0.01 |  726.5625 | 273.4375 | 132.8125 | 4562397 B |
| Experimental_ImMapSlots_AddOrUpdate | 10000 | 4,239,339.69 ns | 18,633.671 ns | 17,429.947 ns |  0.95 |    0.01 |  609.3750 | 210.9375 |  70.3125 | 3856349 B |
|           DictSlim_GetOrAddValueRef | 10000 |   469,668.21 ns |  1,715.546 ns |  1,520.787 ns |  0.10 |    0.00 |  124.5117 | 124.5117 | 124.5117 |  975712 B |
|                         Dict_TryAdd | 10000 |   580,014.41 ns |  3,254.865 ns |  2,885.353 ns |  0.13 |    0.00 |  221.6797 | 221.6797 | 221.6797 | 1261681 B |
|               ConcurrentDict_TryAdd | 10000 | 2,903,782.53 ns | 34,822.216 ns | 32,572.722 ns |  0.65 |    0.01 |  269.5313 | 121.0938 |  42.9688 | 1645253 B |
|           ImmutableDict_Builder_Add | 10000 | 6,160,368.91 ns | 23,650.044 ns | 22,122.266 ns |  1.37 |    0.01 |  148.4375 |  70.3125 |        - |  959786 B |
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
|                     Method | Count |         Mean |       Error |      StdDev | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|--------------------------- |------ |-------------:|------------:|------------:|------:|--------:|---------:|--------:|------:|----------:|
|      ImHashMap_AddOrUpdate |     1 |     120.9 ns |     1.00 ns |     0.93 ns |  1.00 |    0.00 |   0.0577 |       - |     - |     272 B |
|   ImHashMap_V1_AddOrUpdate |     1 |     132.3 ns |     2.64 ns |     2.71 ns |  1.10 |    0.02 |   0.0610 |       - |     - |     288 B |
| ImHashMapSlots_AddOrUpdate |     1 |     213.6 ns |     0.32 ns |     0.27 ns |  1.76 |    0.01 |   0.1070 |  0.0002 |     - |     504 B |
|            DictSlim_TryAdd |     1 |     125.8 ns |     0.79 ns |     0.66 ns |  1.04 |    0.01 |   0.0408 |       - |     - |     192 B |
|                Dict_TryAdd |     1 |     130.6 ns |     0.65 ns |     0.61 ns |  1.08 |    0.01 |   0.0544 |       - |     - |     256 B |
|      ConcurrentDict_TryAdd |     1 |     290.6 ns |     1.27 ns |     1.19 ns |  2.40 |    0.02 |   0.2074 |  0.0014 |     - |     976 B |
|  ImmutableDict_Builder_Add |     1 |     398.0 ns |     1.15 ns |     1.08 ns |  3.29 |    0.03 |   0.0577 |       - |     - |     272 B |
|                            |       |              |             |             |       |         |          |         |       |           |
|      ImHashMap_AddOrUpdate |    10 |     812.8 ns |     4.65 ns |     3.88 ns |  1.00 |    0.00 |   0.4911 |  0.0029 |     - |    2312 B |
|   ImHashMap_V1_AddOrUpdate |    10 |   1,017.5 ns |     3.90 ns |     3.65 ns |  1.25 |    0.01 |   0.6218 |  0.0038 |     - |    2928 B |
| ImHashMapSlots_AddOrUpdate |    10 |     595.5 ns |     1.75 ns |     1.46 ns |  0.73 |    0.00 |   0.2956 |  0.0019 |     - |    1392 B |
|            DictSlim_TryAdd |    10 |     556.0 ns |     2.24 ns |     2.09 ns |  0.68 |    0.01 |   0.2375 |  0.0010 |     - |    1120 B |
|                Dict_TryAdd |    10 |     604.9 ns |     1.78 ns |     1.57 ns |  0.74 |    0.00 |   0.2193 |  0.0010 |     - |    1032 B |
|      ConcurrentDict_TryAdd |    10 |   1,387.4 ns |     5.62 ns |     4.69 ns |  1.71 |    0.01 |   0.6294 |  0.0095 |     - |    2968 B |
|  ImmutableDict_Builder_Add |    10 |   2,505.4 ns |    25.19 ns |    22.33 ns |  3.08 |    0.03 |   0.1793 |       - |     - |     848 B |
|                            |       |              |             |             |       |         |          |         |       |           |
|      ImHashMap_AddOrUpdate |   100 |  12,489.6 ns |    51.63 ns |    48.29 ns |  1.00 |    0.00 |   7.4005 |  0.3510 |     - |   34856 B |
|   ImHashMap_V1_AddOrUpdate |   100 |  15,068.0 ns |   121.82 ns |    95.11 ns |  1.21 |    0.01 |   8.5449 |  0.4272 |     - |   40320 B |
| ImHashMapSlots_AddOrUpdate |   100 |   6,526.1 ns |    16.45 ns |    14.58 ns |  0.52 |    0.00 |   3.1052 |  0.2060 |     - |   14640 B |
|            DictSlim_TryAdd |   100 |   4,200.5 ns |    28.71 ns |    23.97 ns |  0.34 |    0.00 |   1.5945 |  0.0458 |     - |    7536 B |
|                Dict_TryAdd |   100 |   5,021.9 ns |    18.09 ns |    16.92 ns |  0.40 |    0.00 |   2.1667 |  0.0916 |     - |   10232 B |
|      ConcurrentDict_TryAdd |   100 |  15,519.5 ns |    68.27 ns |    63.86 ns |  1.24 |    0.01 |   6.5613 |  0.0305 |     - |   30944 B |
|  ImmutableDict_Builder_Add |   100 |  34,182.6 ns |   106.60 ns |    99.71 ns |  2.74 |    0.01 |   1.4038 |  0.0610 |     - |    6608 B |
|                            |       |              |             |             |       |         |          |         |       |           |
|      ImHashMap_AddOrUpdate |  1000 | 265,875.9 ns |   897.54 ns |   839.56 ns |  1.00 |    0.00 | 108.3984 | 30.7617 |     - |  511209 B |
|   ImHashMap_V1_AddOrUpdate |  1000 | 307,754.2 ns | 1,962.45 ns | 1,739.66 ns |  1.16 |    0.01 | 121.0938 | 35.1563 |     - |  571250 B |
| ImHashMapSlots_AddOrUpdate |  1000 | 155,827.3 ns |   537.71 ns |   502.97 ns |  0.59 |    0.00 |  57.3730 | 19.0430 |     - |  270866 B |
|            DictSlim_TryAdd |  1000 |  38,470.1 ns |   334.14 ns |   312.56 ns |  0.14 |    0.00 |  12.2681 |  0.0610 |     - |   57856 B |
|                Dict_TryAdd |  1000 |  50,979.3 ns |   165.64 ns |   154.94 ns |  0.19 |    0.00 |  21.6064 |  5.3711 |     - |  102256 B |
|      ConcurrentDict_TryAdd |  1000 | 174,180.3 ns | 3,453.65 ns | 3,061.57 ns |  0.65 |    0.01 |  48.8281 | 23.9258 |     - |  260009 B |
|  ImmutableDict_Builder_Add |  1000 | 501,694.1 ns | 2,097.12 ns | 1,961.65 ns |  1.89 |    0.01 |  12.6953 |  2.9297 |     - |   64209 B |
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
