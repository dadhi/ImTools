using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using ImTools;
using ImTools.V2;
using ImTools.UnitTests;
using Microsoft.Collections.Extensions;
using ImTools.V2.Experimental;

namespace Playground
{
    public class ImMapBenchmarks
    {
        [MemoryDiagnoser]
        public class Populate
        {
            /*
            ## 26.01.2019: Basic results to improve on
            
                     Method | Count |        Mean |       Error |      StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            --------------- |------ |------------:|------------:|------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
             AddOrUpdate_v2 |     5 |    187.9 ns |   3.1318 ns |   2.7763 ns |  0.93 |    0.06 |      0.1287 |           - |           - |               608 B |
                AddOrUpdate |     5 |    200.8 ns |   7.9153 ns |  10.8346 ns |  1.00 |    0.00 |      0.1523 |           - |           - |               720 B |
             AddOrUpdate_v1 |     5 |    227.7 ns |   0.6471 ns |   0.5404 ns |  1.13 |    0.08 |      0.1726 |           - |           - |               816 B |
                            |       |             |             |             |       |         |             |             |             |                     |
                AddOrUpdate |    40 |  3,900.9 ns |  12.7144 ns |  11.89b30 ns |  1.00 |    0.00 |      2.5482 |           - |           - |             12048 B |
             AddOrUpdate_v1 |    40 |  4,025.7 ns |  10.8285 ns |   9.5992 ns |  1.03 |    0.00 |      2.8915 |           - |           - |             13680 B |
             AddOrUpdate_v2 |    40 |  4,372.7 ns |  25.4631 ns |  21.2628 ns |  1.12 |    0.01 |      2.3499 |           - |           - |             11104 B |
                            |       |             |             |             |       |         |             |             |             |                     |
                AddOrUpdate |   200 | 27,594.6 ns |  69.7488 ns |  61.8305 ns |  1.00 |    0.00 |     17.6392 |           - |           - |             83376 B |
             AddOrUpdate_v1 |   200 | 27,945.8 ns |  75.2254 ns |  70.3659 ns |  1.01 |    0.00 |     19.5923 |      0.0305 |           - |             92592 B |
             AddOrUpdate_v2 |   200 | 31,572.7 ns | 154.1361 ns | 144.1790 ns |  1.14 |    0.01 |     16.6016 |      0.0610 |           - |             78592 B |

            ## Inlining the left and right handlers

                     Method | Count |        Mean |      Error |     StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            --------------- |------ |------------:|-----------:|-----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                AddOrUpdate |     5 |    185.0 ns |   1.077 ns |   1.008 ns |  1.00 |    0.00 |      0.1321 |           - |           - |               624 B |
             AddOrUpdate_v2 |     5 |    197.1 ns |   3.902 ns |   4.792 ns |  1.06 |    0.03 |      0.1287 |           - |           - |               608 B |
             AddOrUpdate_v1 |     5 |    233.2 ns |   4.440 ns |   4.559 ns |  1.26 |    0.03 |      0.1729 |           - |           - |               816 B |
                            |       |             |            |            |       |         |             |             |             |                     |
                AddOrUpdate |    40 |  3,664.5 ns |  70.296 ns |  75.216 ns |  1.00 |    0.00 |      2.3575 |           - |           - |             11136 B |
             AddOrUpdate_v1 |    40 |  4,236.8 ns |  46.168 ns |  40.926 ns |  1.15 |    0.02 |      2.8915 |           - |           - |             13680 B |
             AddOrUpdate_v2 |    40 |  4,606.2 ns |  54.509 ns |  48.321 ns |  1.25 |    0.02 |      2.3499 |           - |           - |             11104 B |
                            |       |             |            |            |       |         |             |             |             |                     |
                AddOrUpdate |   200 | 26,445.5 ns | 469.174 ns | 438.866 ns |  1.00 |    0.00 |     16.6321 |           - |           - |             78624 B |
             AddOrUpdate_v1 |   200 | 29,073.2 ns | 790.909 ns | 776.779 ns |  1.10 |    0.03 |     19.5923 |      0.0305 |           - |             92592 B |
             AddOrUpdate_v2 |   200 | 31,313.6 ns | 239.070 ns | 223.626 ns |  1.18 |    0.02 |     16.6016 |      0.0610 |           - |             78592 B |

            
            ## Inlining balance. 

                     Method | Count |        Mean |       Error |      StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            --------------- |------ |------------:|------------:|------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                AddOrUpdate |     5 |    176.1 ns |   0.8029 ns |   0.7510 ns |  1.00 |    0.00 |      0.1321 |           - |           - |               624 B |
             AddOrUpdate_v2 |     5 |    191.7 ns |   0.4700 ns |   0.4166 ns |  1.09 |    0.00 |      0.1287 |           - |           - |               608 B |
             AddOrUpdate_v1 |     5 |    227.3 ns |   0.8197 ns |   0.6845 ns |  1.29 |    0.01 |      0.1729 |           - |           - |               816 B |
                            |       |             |             |             |       |         |             |             |             |                     |
                AddOrUpdate |    40 |  3,126.6 ns |   7.5080 ns |   7.0230 ns |  1.00 |    0.00 |      2.3575 |           - |           - |             11136 B |
             AddOrUpdate_v1 |    40 |  3,926.3 ns |  11.6629 ns |   9.1056 ns |  1.26 |    0.00 |      2.8915 |           - |           - |             13680 B |
             AddOrUpdate_v2 |    40 |  4,383.0 ns |  20.3574 ns |  18.0463 ns |  1.40 |    0.01 |      2.3499 |           - |           - |             11104 B |
                            |       |             |             |             |       |         |             |             |             |                     |
                AddOrUpdate |   200 | 23,217.6 ns |  56.7280 ns |  50.2879 ns |  1.00 |    0.00 |     16.6321 |           - |           - |             78624 B |
             AddOrUpdate_v1 |   200 | 27,706.4 ns | 467.1452 ns | 436.9679 ns |  1.19 |    0.02 |     19.5923 |      0.0305 |           - |             92592 B |
             AddOrUpdate_v2 |   200 | 32,107.7 ns | 251.3552 ns | 235.1178 ns |  1.38 |    0.01 |     16.6016 |      0.0610 |           - |             78592 B |


            ## Moving `Height==0` and `key == Key` case out. 

                     Method | Count |        Mean |       Error |     StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            --------------- |------ |------------:|------------:|-----------:|------:|------------:|------------:|------------:|--------------------:|
                AddOrUpdate |     5 |    181.1 ns |   1.1526 ns |  1.0217 ns |  1.00 |      0.1321 |           - |           - |               624 B |
             AddOrUpdate_v2 |     5 |    188.5 ns |   0.5862 ns |  0.5483 ns |  1.04 |      0.1287 |           - |           - |               608 B |
             AddOrUpdate_v1 |     5 |    229.0 ns |   0.2315 ns |  0.1933 ns |  1.27 |      0.1729 |           - |           - |               816 B |
                            |       |             |             |            |       |             |             |             |                     |
                AddOrUpdate |    40 |  3,152.2 ns |  10.3135 ns |  9.6473 ns |  1.00 |      2.3575 |           - |           - |             11136 B |
             AddOrUpdate_v1 |    40 |  3,881.6 ns |  15.0899 ns | 14.1151 ns |  1.23 |      2.8915 |           - |           - |             13680 B |
             AddOrUpdate_v2 |    40 |  4,384.8 ns |   8.8482 ns |  7.8437 ns |  1.39 |      2.3499 |           - |           - |             11104 B |
                            |       |             |             |            |       |             |             |             |                     |
                AddOrUpdate |   200 | 23,350.8 ns | 103.9901 ns | 97.2724 ns |  1.00 |     16.6321 |           - |           - |             78624 B |
             AddOrUpdate_v1 |   200 | 27,168.8 ns |  36.5468 ns | 32.3978 ns |  1.16 |     19.5923 |      0.0305 |           - |             92592 B |
             AddOrUpdate_v2 |   200 | 31,131.1 ns |  99.9934 ns | 88.6416 ns |  1.33 |     16.6016 |      0.0610 |           - |             78592 B |


                ## V2

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


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


## ImMap234

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18363
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.301
  [Host]     : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT


|                                 Method | Count |            Mean |         Error |        StdDev | Ratio | RatioSD |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|--------------------------------------- |------ |----------------:|--------------:|--------------:|------:|--------:|---------:|---------:|---------:|----------:|
|         Experimental_ImMap_AddOrUpdate |     1 |        19.46 ns |      0.093 ns |      0.087 ns |  1.00 |    0.00 |   0.0068 |        - |        - |      32 B |
|    Experimental_ImMapSlots_AddOrUpdate |     1 |       121.06 ns |      1.529 ns |      1.277 ns |  6.22 |    0.05 |   0.0663 |        - |        - |     312 B |
|      Experimental_ImMap234_AddOrUpdate |     1 |        19.43 ns |      0.074 ns |      0.069 ns |  1.00 |    0.01 |   0.0068 |        - |        - |      32 B |
| Experimental_ImMap234Slots_AddOrUpdate |     1 |       117.34 ns |      0.665 ns |      0.622 ns |  6.03 |    0.04 |   0.0663 |        - |        - |     312 B |
|                  ConcurrentDict_TryAdd |     1 |       175.07 ns |      0.980 ns |      0.917 ns |  9.00 |    0.04 |   0.1853 |   0.0012 |        - |     872 B |
|              ImmutableDict_Builder_Add |     1 |       177.48 ns |      0.879 ns |      0.779 ns |  9.12 |    0.07 |   0.0339 |        - |        - |     160 B |
|                                        |       |                 |               |               |       |         |          |          |          |           |
|         Experimental_ImMap_AddOrUpdate |    10 |       457.21 ns |      1.403 ns |      1.313 ns |  1.00 |    0.00 |   0.2651 |   0.0005 |        - |    1248 B |
|    Experimental_ImMapSlots_AddOrUpdate |    10 |       356.58 ns |      0.910 ns |      0.851 ns |  0.78 |    0.00 |   0.1273 |   0.0005 |        - |     600 B |
|      Experimental_ImMap234_AddOrUpdate |    10 |       404.97 ns |      1.185 ns |      1.108 ns |  0.89 |    0.00 |   0.2122 |   0.0005 |        - |    1000 B |
| Experimental_ImMap234Slots_AddOrUpdate |    10 |       343.20 ns |      1.229 ns |      1.149 ns |  0.75 |    0.00 |   0.1273 |   0.0005 |        - |     600 B |
|                  ConcurrentDict_TryAdd |    10 |       703.77 ns |      1.315 ns |      1.165 ns |  1.54 |    0.00 |   0.2613 |   0.0019 |        - |    1232 B |
|              ImmutableDict_Builder_Add |    10 |     2,327.55 ns |      5.936 ns |      5.552 ns |  5.09 |    0.02 |   0.1564 |        - |        - |     736 B |
|                                        |       |                 |               |               |       |         |          |          |          |           |
|         Experimental_ImMap_AddOrUpdate |   100 |    10,180.90 ns |     57.651 ns |     53.927 ns |  1.00 |    0.00 |   6.4545 |   0.2899 |        - |   30432 B |
|    Experimental_ImMapSlots_AddOrUpdate |   100 |     3,970.42 ns |     12.280 ns |     10.886 ns |  0.39 |    0.00 |   1.9608 |   0.1144 |        - |    9240 B |
|      Experimental_ImMap234_AddOrUpdate |   100 |    10,143.91 ns |     45.716 ns |     40.526 ns |  1.00 |    0.01 |   5.4779 |   0.2289 |        - |   25800 B |
| Experimental_ImMap234Slots_AddOrUpdate |   100 |     3,937.81 ns |     17.458 ns |     16.330 ns |  0.39 |    0.00 |   1.8768 |   0.0992 |        - |    8856 B |
|                  ConcurrentDict_TryAdd |   100 |    12,139.06 ns |     45.494 ns |     42.555 ns |  1.19 |    0.01 |   4.8370 |   0.4730 |        - |   22768 B |
|              ImmutableDict_Builder_Add |   100 |    35,251.17 ns |    113.558 ns |    106.223 ns |  3.46 |    0.02 |   1.9531 |   0.1221 |        - |    9376 B |
|                                        |       |                 |               |               |       |         |          |          |          |           |
|         Experimental_ImMap_AddOrUpdate |  1000 |   178,430.30 ns |    638.426 ns |    565.948 ns |  1.00 |    0.00 |  98.1445 |   0.2441 |        - |  462624 B |
|    Experimental_ImMapSlots_AddOrUpdate |  1000 |   102,538.52 ns |    480.403 ns |    425.865 ns |  0.57 |    0.00 |  47.8516 |   0.1221 |        - |  225496 B |
|      Experimental_ImMap234_AddOrUpdate |  1000 |   180,436.96 ns |  1,130.151 ns |  1,057.144 ns |  1.01 |    0.01 |  87.8906 |   1.4648 |        - |  414480 B |
| Experimental_ImMap234Slots_AddOrUpdate |  1000 |    84,713.35 ns |    337.963 ns |    299.596 ns |  0.47 |    0.00 |  40.4053 |   0.1221 |        - |  190232 B |
|                  ConcurrentDict_TryAdd |  1000 |   124,306.71 ns |  1,671.098 ns |  1,563.146 ns |  0.70 |    0.01 |  43.2129 |   0.9766 |        - |  205328 B |
|              ImmutableDict_Builder_Add |  1000 |   474,727.06 ns |    991.683 ns |    879.101 ns |  2.66 |    0.01 |  20.0195 |   0.4883 |        - |   95780 B |
|                                        |       |                 |               |               |       |         |          |          |          |           |
|         Experimental_ImMap_AddOrUpdate | 10000 | 4,418,104.64 ns | 27,677.872 ns | 25,889.899 ns |  1.00 |    0.00 | 992.1875 | 328.1250 | 109.3750 | 6253388 B |
|    Experimental_ImMapSlots_AddOrUpdate | 10000 | 4,291,301.34 ns | 33,092.427 ns | 29,335.574 ns |  0.97 |    0.01 | 609.3750 | 210.9375 |  70.3125 | 3856354 B |
|      Experimental_ImMap234_AddOrUpdate | 10000 | 4,549,840.78 ns | 42,349.737 ns | 39,613.971 ns |  1.03 |    0.01 | 914.0625 | 328.1250 | 140.6250 | 5739218 B |
| Experimental_ImMap234Slots_AddOrUpdate | 10000 | 3,826,270.52 ns | 27,039.210 ns | 25,292.494 ns |  0.87 |    0.01 | 531.2500 | 242.1875 |  39.0625 | 3362691 B |
|                  ConcurrentDict_TryAdd | 10000 | 2,992,046.02 ns | 25,592.476 ns | 23,939.218 ns |  0.68 |    0.01 | 273.4375 | 121.0938 |  42.9688 | 1645240 B |
|              ImmutableDict_Builder_Add | 10000 | 6,083,897.19 ns | 15,221.664 ns | 14,238.354 ns |  1.38 |    0.01 | 148.4375 |  70.3125 |        - |  959776 B |


## V3 preview

|                         Method | Count |            Mean |          Error |         StdDev |          Median | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------------- |------ |----------------:|---------------:|---------------:|----------------:|------:|--------:|----------:|---------:|---------:|----------:|
| Experimental_ImMap_AddOrUpdate |     1 |        24.58 ns |       0.545 ns |       0.560 ns |        24.62 ns |  1.00 |    0.00 |    0.0076 |        - |        - |      32 B |
|       Old_ImMap234_AddOrUpdate |     1 |        24.93 ns |       0.232 ns |       0.206 ns |        24.88 ns |  1.02 |    0.02 |    0.0076 |        - |        - |      32 B |
|           ImMap234_AddOrUpdate |     1 |        26.09 ns |       0.568 ns |       0.697 ns |        25.90 ns |  1.06 |    0.04 |    0.0076 |        - |        - |      32 B |
|                                |       |                 |                |                |                 |       |         |           |          |          |           |
| Experimental_ImMap_AddOrUpdate |     5 |       192.30 ns |       1.497 ns |       1.250 ns |       192.00 ns |  1.00 |    0.00 |    0.0994 |        - |        - |     416 B |
|       Old_ImMap234_AddOrUpdate |     5 |       172.47 ns |       1.845 ns |       1.636 ns |       172.94 ns |  0.90 |    0.01 |    0.0801 |        - |        - |     336 B |
|           ImMap234_AddOrUpdate |     5 |       165.95 ns |       1.712 ns |       1.518 ns |       166.07 ns |  0.86 |    0.01 |    0.0763 |        - |        - |     320 B |
|                                |       |                 |                |                |                 |       |         |           |          |          |           |
| Experimental_ImMap_AddOrUpdate |    10 |       547.54 ns |       7.405 ns |       6.927 ns |       546.98 ns |  1.00 |    0.00 |    0.2975 |        - |        - |    1248 B |
|       Old_ImMap234_AddOrUpdate |    10 |       500.04 ns |       5.583 ns |       4.359 ns |       500.98 ns |  0.91 |    0.01 |    0.2384 |        - |        - |    1000 B |
|           ImMap234_AddOrUpdate |    10 |       451.32 ns |       8.955 ns |      13.942 ns |       452.64 ns |  0.81 |    0.03 |    0.1969 |        - |        - |     824 B |

## V3 RTM

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

### @wip: ImMax

|               Method | Count |            Mean |         Error |        StdDev | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|--------------------- |------ |----------------:|--------------:|--------------:|------:|--------:|----------:|---------:|---------:|----------:|
| V3_ImMap_AddOrUpdate |     1 |        13.08 ns |      0.089 ns |      0.079 ns |  1.00 |    0.00 |    0.0051 |        - |        - |      32 B |
| V2_ImMap_AddOrUpdate |     1 |        13.98 ns |      0.248 ns |      0.232 ns |  1.07 |    0.02 |    0.0076 |        - |        - |      48 B |
|                      |       |                 |               |               |       |         |           |          |          |           |
| V3_ImMap_AddOrUpdate |    10 |       294.43 ns |      3.098 ns |      2.898 ns |  1.00 |    0.00 |    0.1249 |        - |        - |     784 B |
| V2_ImMap_AddOrUpdate |    10 |       552.47 ns |      4.603 ns |      4.305 ns |  1.88 |    0.02 |    0.2823 |        - |        - |    1776 B |
|                      |       |                 |               |               |       |         |           |          |          |           |
| V3_ImMap_AddOrUpdate |   100 |    10,310.16 ns |    195.364 ns |    182.744 ns |  1.00 |    0.00 |    3.7689 |   0.1068 |        - |   23672 B |
| V2_ImMap_AddOrUpdate |   100 |    11,801.52 ns |    208.475 ns |    184.808 ns |  1.15 |    0.03 |    5.9357 |   0.2441 |        - |   37296 B |
|                      |       |                 |               |               |       |         |           |          |          |           |
| V3_ImMap_AddOrUpdate |  1000 |   200,289.77 ns |  3,877.022 ns |  4,148.370 ns |  1.00 |    0.00 |   62.7441 |   1.2207 |        - |  394184 B |
| V2_ImMap_AddOrUpdate |  1000 |   193,306.02 ns |  1,094.388 ns |  1,023.691 ns |  0.97 |    0.02 |   84.9609 |   0.4883 |        - |  534144 B |
|                      |       |                 |               |               |       |         |           |          |          |           |
| V3_ImMap_AddOrUpdate | 10000 | 4,594,232.92 ns | 49,228.901 ns | 46,048.744 ns |  1.00 |    0.00 |  882.8125 | 335.9375 | 117.1875 | 5538768 B |
| V2_ImMap_AddOrUpdate | 10000 | 4,226,092.24 ns | 34,037.023 ns | 31,838.252 ns |  0.92 |    0.01 | 1109.3750 | 226.5625 | 101.5625 | 6972672 B |

### normal ImMap

|               Method | Count |            Mean |         Error |        StdDev | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|--------------------- |------ |----------------:|--------------:|--------------:|------:|--------:|----------:|---------:|---------:|----------:|
| V3_ImMap_AddOrUpdate |     1 |        12.63 ns |      0.141 ns |      0.132 ns |  1.00 |    0.00 |    0.0051 |        - |        - |      32 B |
| V2_ImMap_AddOrUpdate |     1 |        13.94 ns |      0.337 ns |      0.316 ns |  1.10 |    0.02 |    0.0076 |        - |        - |      48 B |
|                      |       |                 |               |               |       |         |           |          |          |           |
| V3_ImMap_AddOrUpdate |    10 |       261.33 ns |      2.080 ns |      1.946 ns |  1.00 |    0.00 |    0.1197 |        - |        - |     752 B |
| V2_ImMap_AddOrUpdate |    10 |       553.46 ns |      5.817 ns |      5.157 ns |  2.12 |    0.03 |    0.2823 |        - |        - |    1776 B |
|                      |       |                 |               |               |       |         |           |          |          |           |
| V3_ImMap_AddOrUpdate |   100 |     9,178.53 ns |     72.129 ns |     67.470 ns |  1.00 |    0.00 |    3.4027 |   0.1373 |        - |   21352 B |
| V2_ImMap_AddOrUpdate |   100 |    11,652.48 ns |    200.749 ns |    187.780 ns |  1.27 |    0.02 |    5.9357 |   0.2441 |        - |   37296 B |
|                      |       |                 |               |               |       |         |           |          |          |           |
| V3_ImMap_AddOrUpdate |  1000 |   184,592.38 ns |  3,455.558 ns |  3,063.262 ns |  1.00 |    0.00 |   58.3496 |   0.4883 |        - |  366496 B |
| V2_ImMap_AddOrUpdate |  1000 |   189,965.68 ns |  3,699.942 ns |  3,089.619 ns |  1.03 |    0.03 |   84.9609 |   0.4883 |        - |  534144 B |
|                      |       |                 |               |               |       |         |           |          |          |           |
| V3_ImMap_AddOrUpdate | 10000 | 4,385,416.95 ns | 30,706.166 ns | 25,641.039 ns |  1.00 |    0.00 |  835.9375 | 328.1250 | 148.4375 | 5247424 B |
| V2_ImMap_AddOrUpdate | 10000 | 4,243,496.42 ns | 38,801.912 ns | 32,401.354 ns |  0.97 |    0.01 | 1109.3750 | 226.5625 | 101.5625 | 6972672 B |


|               Method | Count |         Mean |      Error |     StdDev | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
|--------------------- |------ |-------------:|-----------:|-----------:|------:|--------:|----------:|---------:|---------:|-----------:|
| V3_ImMap_AddOrUpdate |   100 |     9.588 us |  0.0417 us |  0.0391 us |  1.00 |    0.00 |    3.5706 |   0.1526 |        - |   21.92 KB |
| V2_ImMap_AddOrUpdate |   100 |    11.442 us |  0.2251 us |  0.3156 us |  1.19 |    0.03 |    5.9357 |   0.2441 |        - |   36.42 KB |
|                      |       |              |            |            |       |         |           |          |          |            |
| V3_ImMap_AddOrUpdate |  1000 |   190.917 us |  2.8929 us |  2.4157 us |  1.00 |    0.00 |   59.5703 |   0.7324 |        - |  365.73 KB |
| V2_ImMap_AddOrUpdate |  1000 |   189.413 us |  3.7871 us |  4.2094 us |  0.99 |    0.03 |   84.9609 |   0.4883 |        - |  521.63 KB |
|                      |       |              |            |            |       |         |           |          |          |            |
| V3_ImMap_AddOrUpdate | 10000 | 4,315.152 us | 35.4322 us | 29.5875 us |  1.00 |    0.00 |  843.7500 | 304.6875 | 140.6250 |  5203.2 KB |
| V2_ImMap_AddOrUpdate | 10000 | 4,057.928 us | 21.7655 us | 19.2945 us |  0.94 |    0.01 | 1109.3750 | 226.5625 | 101.5625 | 6809.25 KB |


### Adding Branch2Plus1

|               Method | Count |           Mean |        Error |       StdDev | Ratio | RatioSD |     Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
|--------------------- |------ |---------------:|-------------:|-------------:|------:|--------:|----------:|---------:|---------:|-----------:|
| V3_ImMap_AddOrUpdate |    14 |       482.9 ns |      9.66 ns |      8.57 ns |  1.00 |    0.00 |    0.2036 |   0.0010 |        - |    1.25 KB |
| V2_ImMap_AddOrUpdate |    14 |       901.6 ns |     12.31 ns |     10.28 ns |  1.87 |    0.05 |    0.4711 |   0.0019 |        - |    2.89 KB |
|                      |       |                |              |              |       |         |           |          |          |            |
| V3_ImMap_AddOrUpdate |   100 |     8,900.6 ns |    174.39 ns |    179.08 ns |  1.00 |    0.00 |    3.3722 |   0.1373 |        - |   20.66 KB |
| V2_ImMap_AddOrUpdate |   100 |    11,903.4 ns |    138.36 ns |    129.42 ns |  1.34 |    0.04 |    5.9357 |   0.2441 |        - |   36.42 KB |
|                      |       |                |              |              |       |         |           |          |          |            |
| V3_ImMap_AddOrUpdate |  1000 |   184,202.3 ns |  3,576.79 ns |  3,345.73 ns |  1.00 |    0.00 |   58.1055 |   0.4883 |        - |  355.96 KB |
| V2_ImMap_AddOrUpdate |  1000 |   195,558.5 ns |  3,787.84 ns |  4,509.16 ns |  1.06 |    0.03 |   84.9609 |   0.4883 |        - |  521.63 KB |
|                      |       |                |              |              |       |         |           |          |          |            |
| V3_ImMap_AddOrUpdate | 10000 | 4,307,847.3 ns | 41,477.79 ns | 38,798.35 ns |  1.00 |    0.00 |  828.1250 | 328.1250 | 148.4375 | 5104.91 KB |
| V2_ImMap_AddOrUpdate | 10000 | 4,222,610.3 ns | 41,294.12 ns | 38,626.54 ns |  0.98 |    0.01 | 1109.3750 | 226.5625 | 101.5625 | 6809.25 KB |

*/
            // [Params(14, 100, 1_000, 10_000)]
            [Params(1, 10, 100, 1_000, 10_000)]
            // [Params(100)]
            public int Count;

            [Benchmark(Baseline = true)]
            public ImTools.ImMap<string> V3_ImMap_AddOrUpdate()
            {
                var map = ImTools.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            // [Benchmark]
            public ImTools.V2.Experimental.ImMap<string>[] V3_PartitionedImMap_AddOrUpdate()
            {
                var slots = ImTools.V2.Experimental.ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
            }

            [Benchmark]
            public ImTools.V2.ImMap<string> V2_ImMap_AddOrUpdate()
            {
                var map = ImTools.V2.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            // [Benchmark]
            public ImTools.V2.Experimental.ImMap<string> V2_ImMap_Experimental_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            //[Benchmark]
            public ImTools.V2.Experimental.ImMap<string>[] Experimental_ImMapSlots_AddOrUpdate()
            {
                var slots = ImTools.V2.Experimental.ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
            }

            // [Benchmark]
            public ImTools.V2.Experimental.ImMap<string>[] ImMapSlots_AddOrUpdate()
            {
                var slots = ImTools.V2.Experimental.ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
            }

            // [Benchmark]
            public DictionarySlim<int, string> DictSlim_GetOrAddValueRef()
            {
                var map = new DictionarySlim<int, string>();

                for (var i = 0; i < Count; i++)
                    map.GetOrAddValueRef(i) = i.ToString();

                return map;
            }

            // [Benchmark]
            public Dictionary<int, string> Dict_TryAdd()
            {
                var map = new Dictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            // [Benchmark]
            public ConcurrentDictionary<int, string> ConcurrentDict_TryAdd()
            {
                var map = new ConcurrentDictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            // [Benchmark]
            public ImmutableDictionary<int, string> ImmutableDict_Builder_Add()
            {
                var builder = ImmutableDictionary.CreateBuilder<int, string>();

                for (var i = 0; i < Count; i++)
                    builder.Add(i, i.ToString());

                return builder.ToImmutable();
            }

            // [Benchmark]
            public ImmutableDictionary<int, string> ImmutableDict_Add()
            {
                var dict = ImmutableDictionary.Create<int, string>();

                for (var i = 0; i < Count; i++)
                    dict = dict.Add(i, i.ToString());

                return dict;
            }
        }

        [MemoryDiagnoser]
        public class Lookup
        {
            /*
            ## 2019.01.01 - Hmm, statics with inlining are da best

            BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.472 (1803/April2018Update/Redstone4)
            Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
            Frequency=2156252 Hz, Resolution=463.7677 ns, Timer=TSC
            .NET Core SDK=2.2.100
              [Host]     : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
              DefaultJob : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT


                            Method | LookupKey |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ---------------------- |---------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                TryFind_new_static |         1 |  5.772 ns | 0.0364 ns | 0.0340 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
              TryFind_old_instance |         1 |  9.060 ns | 0.1011 ns | 0.0945 ns |  1.57 |    0.02 |           - |           - |           - |                   - |
             ConcurrentDict_TryGet |         1 | 11.865 ns | 0.0364 ns | 0.0340 ns |  2.06 |    0.01 |           - |           - |           - |                   - |
                                   |           |           |           |           |       |         |             |             |             |                     |
                TryFind_new_static |        30 |  6.569 ns | 0.0881 ns | 0.0824 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
              TryFind_old_instance |        30 | 11.112 ns | 0.0766 ns | 0.0679 ns |  1.69 |    0.02 |           - |           - |           - |                   - |
             ConcurrentDict_TryGet |        30 | 11.847 ns | 0.0266 ns | 0.0236 ns |  1.80 |    0.02 |           - |           - |           - |                   - |
                                   |           |           |           |           |       |         |             |             |             |                     |
                TryFind_new_static |        60 |  7.227 ns | 0.0605 ns | 0.0566 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
              TryFind_old_instance |        60 | 10.617 ns | 0.0202 ns | 0.0168 ns |  1.47 |    0.01 |           - |           - |           - |                   - |
             ConcurrentDict_TryGet |        60 | 11.855 ns | 0.0481 ns | 0.0426 ns |  1.64 |    0.01 |           - |           - |           - |                   - |
                                   |           |           |           |           |       |         |             |             |             |                     |
                TryFind_new_static |        90 |  7.446 ns | 0.0228 ns | 0.0213 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
              TryFind_old_instance |        90 |  9.740 ns | 0.1398 ns | 0.1308 ns |  1.31 |    0.02 |           - |           - |           - |                   - |
             ConcurrentDict_TryGet |        90 | 11.853 ns | 0.0286 ns | 0.0253 ns |  1.59 |    0.01 |           - |           - |           - |                   - |

            ## 27.01.2019: Baseline

                 Method | Count |     Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ----------- |------ |---------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                TryFind |     5 | 2.404 ns | 0.0395 ns | 0.0350 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
             TryFind_v2 |     5 | 3.250 ns | 0.0101 ns | 0.0095 ns |  1.35 |    0.02 |           - |           - |           - |                   - |
             TryFind_v1 |     5 | 4.691 ns | 0.0345 ns | 0.0322 ns |  1.95 |    0.04 |           - |           - |           - |                   - |
                        |       |          |           |           |       |         |             |             |             |                     |
                TryFind |    40 | 4.249 ns | 0.0629 ns | 0.0589 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
             TryFind_v1 |    40 | 6.216 ns | 0.0159 ns | 0.0133 ns |  1.46 |    0.02 |           - |           - |           - |                   - |
             TryFind_v2 |    40 | 6.474 ns | 0.0614 ns | 0.0574 ns |  1.52 |    0.02 |           - |           - |           - |                   - |
                        |       |          |           |           |       |         |             |             |             |                     |
                TryFind |   200 | 5.538 ns | 0.0511 ns | 0.0478 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
             TryFind_v1 |   200 | 7.391 ns | 0.1443 ns | 0.1350 ns |  1.33 |    0.02 |           - |           - |           - |                   - |
             TryFind_v2 |   200 | 8.764 ns | 0.0779 ns | 0.0651 ns |  1.58 |    0.02 |           - |           - |           - |                   - |

            ##: New TryFind with somehow better perf (TryFind2 is the old one)

                Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
---------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
               TryFind |     5 |  1.842 ns | 0.0370 ns | 0.0309 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
              TryFind2 |     5 |  2.135 ns | 0.0169 ns | 0.0158 ns |  1.16 |    0.02 |           - |           - |           - |                   - |
            TryFind_v1 |     5 |  4.341 ns | 0.0298 ns | 0.0278 ns |  2.36 |    0.05 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |     5 | 10.206 ns | 0.0319 ns | 0.0298 ns |  5.54 |    0.09 |           - |           - |           - |                   - |
                       |       |           |           |           |       |         |             |             |             |                     |
               TryFind |    40 |  3.110 ns | 0.0395 ns | 0.0370 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
              TryFind2 |    40 |  3.836 ns | 0.0449 ns | 0.0420 ns |  1.23 |    0.02 |           - |           - |           - |                   - |
            TryFind_v1 |    40 |  5.818 ns | 0.0235 ns | 0.0197 ns |  1.87 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |    40 | 10.136 ns | 0.0086 ns | 0.0076 ns |  3.26 |    0.04 |           - |           - |           - |                   - |
                       |       |           |           |           |       |         |             |             |             |                     |
               TryFind |   200 |  4.518 ns | 0.1164 ns | 0.1089 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
              TryFind2 |   200 |  5.173 ns | 0.0578 ns | 0.0540 ns |  1.15 |    0.03 |           - |           - |           - |                   - |
            TryFind_v1 |   200 |  6.886 ns | 0.0354 ns | 0.0331 ns |  1.52 |    0.03 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |   200 | 10.162 ns | 0.0355 ns | 0.0315 ns |  2.25 |    0.06 |           - |           - |           - |                   - |

|                Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|---------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|               TryFind |    10 |  3.775 ns | 0.0154 ns | 0.0136 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|            TryFind_v1 |    10 |  5.596 ns | 0.0606 ns | 0.0567 ns |  1.48 |    0.02 |           - |           - |           - |                   - |
|       DictSlim_TryGet |    10 |  4.015 ns | 0.0089 ns | 0.0079 ns |  1.06 |    0.00 |           - |           - |           - |                   - |
|           Dict_TryGet |    10 |  7.556 ns | 0.0295 ns | 0.0276 ns |  2.00 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGet |    10 | 10.582 ns | 0.0259 ns | 0.0229 ns |  2.80 |    0.01 |           - |           - |           - |                   - |
|                       |       |           |           |           |       |         |             |             |             |                     |
|               TryFind |   100 |  6.772 ns | 0.0173 ns | 0.0162 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|            TryFind_v1 |   100 |  8.748 ns | 0.0239 ns | 0.0212 ns |  1.29 |    0.00 |           - |           - |           - |                   - |
|       DictSlim_TryGet |   100 |  3.457 ns | 0.0116 ns | 0.0109 ns |  0.51 |    0.00 |           - |           - |           - |                   - |
|           Dict_TryGet |   100 |  7.485 ns | 0.0237 ns | 0.0222 ns |  1.11 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDict_TryGet |   100 | 10.414 ns | 0.0106 ns | 0.0099 ns |  1.54 |    0.00 |           - |           - |           - |                   - |
|                       |       |           |           |           |       |         |             |             |             |                     |
|               TryFind |  1000 |  9.517 ns | 0.0196 ns | 0.0184 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|            TryFind_v1 |  1000 | 11.108 ns | 0.0214 ns | 0.0189 ns |  1.17 |    0.00 |           - |           - |           - |                   - |
|       DictSlim_TryGet |  1000 |  3.505 ns | 0.0148 ns | 0.0132 ns |  0.37 |    0.00 |           - |           - |           - |                   - |
|           Dict_TryGet |  1000 |  7.475 ns | 0.0148 ns | 0.0138 ns |  0.79 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDict_TryGet |  1000 | 10.355 ns | 0.0158 ns | 0.0140 ns |  1.09 |    0.00 |           - |           - |           - |                   - |
|                       |       |           |           |           |       |         |             |             |             |                     |
|               TryFind | 10000 | 13.635 ns | 0.0529 ns | 0.0469 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|            TryFind_v1 | 10000 | 84.213 ns | 0.1027 ns | 0.0911 ns |  6.18 |    0.02 |           - |           - |           - |                   - |
|       DictSlim_TryGet | 10000 | 17.402 ns | 0.1809 ns | 0.1692 ns |  1.28 |    0.01 |           - |           - |           - |                   - |
|           Dict_TryGet | 10000 | 37.355 ns | 0.3372 ns | 0.3155 ns |  2.74 |    0.03 |           - |           - |           - |                   - |
| ConcurrentDict_TryGet | 10000 | 51.937 ns | 0.3141 ns | 0.2938 ns |  3.81 |    0.02 |           - |           - |           - |                   - |

 ## 2019-04-02: Full-fledged benchmark

|                     Method |  Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------- |------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|              ImMap_TryFind |     10 |  2.994 ns | 0.0113 ns | 0.0106 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind |     10 |  5.566 ns | 0.0354 ns | 0.0331 ns |  1.86 |    0.01 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue |     10 |  4.112 ns | 0.0155 ns | 0.0138 ns |  1.37 |    0.01 |           - |           - |           - |                   - |
|           Dict_TryGetValue |     10 |  7.188 ns | 0.0317 ns | 0.0281 ns |  2.40 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue |     10 | 10.392 ns | 0.0217 ns | 0.0203 ns |  3.47 |    0.01 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue |     10 | 59.361 ns | 0.2914 ns | 0.2725 ns | 19.83 |    0.11 |           - |           - |           - |                   - |
|                            |        |           |           |           |       |         |             |             |             |                     |
|              ImMap_TryFind |    100 |  4.377 ns | 0.0283 ns | 0.0251 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind |    100 |  6.716 ns | 0.0228 ns | 0.0214 ns |  1.53 |    0.01 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue |    100 |  3.374 ns | 0.0121 ns | 0.0113 ns |  0.77 |    0.01 |           - |           - |           - |                   - |
|           Dict_TryGetValue |    100 |  7.213 ns | 0.0472 ns | 0.0442 ns |  1.65 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue |    100 | 10.453 ns | 0.1343 ns | 0.1257 ns |  2.39 |    0.03 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue |    100 | 69.985 ns | 1.4111 ns | 1.3859 ns | 15.96 |    0.29 |           - |           - |           - |                   - |
|                            |        |           |           |           |       |         |             |             |             |                     |
|              ImMap_TryFind |   1000 |  7.160 ns | 0.0865 ns | 0.0809 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind |   1000 |  8.160 ns | 0.0399 ns | 0.0373 ns |  1.14 |    0.01 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue |   1000 |  3.375 ns | 0.0125 ns | 0.0111 ns |  0.47 |    0.01 |           - |           - |           - |                   - |
|           Dict_TryGetValue |   1000 |  7.165 ns | 0.0426 ns | 0.0398 ns |  1.00 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue |   1000 | 10.614 ns | 0.1033 ns | 0.0966 ns |  1.48 |    0.02 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue |   1000 | 72.254 ns | 1.2773 ns | 1.1948 ns | 10.09 |    0.20 |           - |           - |           - |                   - |
|                            |        |           |           |           |       |         |             |             |             |                     |
|              ImMap_TryFind |  10000 | 13.560 ns | 0.1585 ns | 0.1405 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind |  10000 | 15.908 ns | 0.1155 ns | 0.1080 ns |  1.17 |    0.01 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue |  10000 |  3.930 ns | 0.0123 ns | 0.0115 ns |  0.29 |    0.00 |           - |           - |           - |                   - |
|           Dict_TryGetValue |  10000 |  7.175 ns | 0.0382 ns | 0.0357 ns |  0.53 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue |  10000 | 11.132 ns | 0.0250 ns | 0.0234 ns |  0.82 |    0.01 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue |  10000 | 85.523 ns | 0.3271 ns | 0.3059 ns |  6.31 |    0.07 |           - |           - |           - |                   - |
|                            |        |           |           |           |       |         |             |             |             |                     |
|              ImMap_TryFind | 100000 | 15.374 ns | 0.0290 ns | 0.0271 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind | 100000 | 18.809 ns | 0.0399 ns | 0.0333 ns |  1.22 |    0.00 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue | 100000 |  3.364 ns | 0.0088 ns | 0.0078 ns |  0.22 |    0.00 |           - |           - |           - |                   - |
|           Dict_TryGetValue | 100000 |  7.204 ns | 0.0279 ns | 0.0247 ns |  0.47 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue | 100000 | 10.578 ns | 0.0549 ns | 0.0514 ns |  0.69 |    0.00 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue | 100000 | 92.043 ns | 0.3051 ns | 0.2854 ns |  5.99 |    0.02 |           - |           - |           - |                   - |

## V2:

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

|                          Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |------ |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                   ImMap_TryFind |     1 |  0.7378 ns | 0.0119 ns | 0.0112 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                ImMap_V1_TryFind |     1 |  2.9640 ns | 0.0168 ns | 0.0157 ns |  4.02 |    0.05 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind |     1 |  1.5542 ns | 0.0212 ns | 0.0199 ns |  2.11 |    0.05 |     - |     - |     - |         - |
|              ImMapSlots_TryFind |     1 |  0.7942 ns | 0.0101 ns | 0.0089 ns |  1.08 |    0.02 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind |     1 |  1.9390 ns | 0.0153 ns | 0.0136 ns |  2.63 |    0.04 |     - |     - |     - |         - |
|            DictSlim_TryGetValue |     1 |  3.3361 ns | 0.0175 ns | 0.0164 ns |  4.52 |    0.08 |     - |     - |     - |         - |
|                Dict_TryGetValue |     1 |  7.0343 ns | 0.0271 ns | 0.0240 ns |  9.52 |    0.16 |     - |     - |     - |         - |
|      ConcurrentDict_TryGetValue |     1 | 10.5204 ns | 0.0332 ns | 0.0310 ns | 14.26 |    0.23 |     - |     - |     - |         - |
|       ImmutableDict_TryGetValue |     1 | 18.1306 ns | 0.0467 ns | 0.0437 ns | 24.58 |    0.39 |     - |     - |     - |         - |
|                                 |       |            |           |           |       |         |       |       |       |           |
|                   ImMap_TryFind |    10 |  3.3113 ns | 0.0487 ns | 0.0455 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                ImMap_V1_TryFind |    10 |  5.9648 ns | 0.0304 ns | 0.0270 ns |  1.80 |    0.03 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind |    10 |  3.9903 ns | 0.0350 ns | 0.0328 ns |  1.21 |    0.02 |     - |     - |     - |         - |
|              ImMapSlots_TryFind |    10 |  1.0427 ns | 0.0147 ns | 0.0137 ns |  0.31 |    0.01 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind |    10 |  2.2045 ns | 0.0139 ns | 0.0130 ns |  0.67 |    0.01 |     - |     - |     - |         - |
|            DictSlim_TryGetValue |    10 |  3.5451 ns | 0.0110 ns | 0.0086 ns |  1.07 |    0.01 |     - |     - |     - |         - |
|                Dict_TryGetValue |    10 |  7.1925 ns | 0.0252 ns | 0.0223 ns |  2.17 |    0.03 |     - |     - |     - |         - |
|      ConcurrentDict_TryGetValue |    10 | 10.0602 ns | 0.0297 ns | 0.0278 ns |  3.04 |    0.04 |     - |     - |     - |         - |
|       ImmutableDict_TryGetValue |    10 | 19.6062 ns | 0.0624 ns | 0.0584 ns |  5.92 |    0.09 |     - |     - |     - |         - |
|                                 |       |            |           |           |       |         |       |       |       |           |
|                   ImMap_TryFind |   100 |  5.9350 ns | 0.0515 ns | 0.0430 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                ImMap_V1_TryFind |   100 |  5.8912 ns | 0.0933 ns | 0.0827 ns |  0.99 |    0.02 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind |   100 |  5.2196 ns | 0.0235 ns | 0.0220 ns |  0.88 |    0.01 |     - |     - |     - |         - |
|              ImMapSlots_TryFind |   100 |  3.3323 ns | 0.0152 ns | 0.0142 ns |  0.56 |    0.00 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind |   100 |  3.3010 ns | 0.0080 ns | 0.0075 ns |  0.56 |    0.00 |     - |     - |     - |         - |
|            DictSlim_TryGetValue |   100 |  3.8773 ns | 0.0142 ns | 0.0126 ns |  0.65 |    0.01 |     - |     - |     - |         - |
|                Dict_TryGetValue |   100 |  7.2136 ns | 0.0236 ns | 0.0220 ns |  1.22 |    0.01 |     - |     - |     - |         - |
|      ConcurrentDict_TryGetValue |   100 | 10.2850 ns | 0.0287 ns | 0.0269 ns |  1.73 |    0.01 |     - |     - |     - |         - |
|       ImmutableDict_TryGetValue |   100 | 21.6888 ns | 0.0858 ns | 0.0803 ns |  3.66 |    0.03 |     - |     - |     - |         - |
|                                 |       |            |           |           |       |         |       |       |       |           |
|                   ImMap_TryFind |  1000 |  8.2336 ns | 0.0627 ns | 0.0556 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                ImMap_V1_TryFind |  1000 |  7.6830 ns | 0.0218 ns | 0.0193 ns |  0.93 |    0.01 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind |  1000 |  8.2976 ns | 0.0281 ns | 0.0235 ns |  1.01 |    0.01 |     - |     - |     - |         - |
|              ImMapSlots_TryFind |  1000 |  6.1388 ns | 0.0377 ns | 0.0294 ns |  0.75 |    0.01 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind |  1000 |  5.5924 ns | 0.0274 ns | 0.0256 ns |  0.68 |    0.00 |     - |     - |     - |         - |
|            DictSlim_TryGetValue |  1000 |  3.8944 ns | 0.0392 ns | 0.0348 ns |  0.47 |    0.00 |     - |     - |     - |         - |
|                Dict_TryGetValue |  1000 |  7.7699 ns | 0.0240 ns | 0.0212 ns |  0.94 |    0.01 |     - |     - |     - |         - |
|      ConcurrentDict_TryGetValue |  1000 |  9.8511 ns | 0.0383 ns | 0.0359 ns |  1.20 |    0.01 |     - |     - |     - |         - |
|       ImmutableDict_TryGetValue |  1000 | 26.8782 ns | 0.6029 ns | 0.9562 ns |  3.20 |    0.15 |     - |     - |     - |         - |
|                                 |       |            |           |           |       |         |       |       |       |           |
|                   ImMap_TryFind | 10000 | 11.2462 ns | 0.0802 ns | 0.0750 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|                ImMap_V1_TryFind | 10000 | 13.0984 ns | 0.3439 ns | 0.3217 ns |  1.16 |    0.03 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind | 10000 | 12.7010 ns | 0.0484 ns | 0.0429 ns |  1.13 |    0.01 |     - |     - |     - |         - |
|              ImMapSlots_TryFind | 10000 |  8.8602 ns | 0.0787 ns | 0.0697 ns |  0.79 |    0.01 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind | 10000 |  8.3997 ns | 0.0613 ns | 0.0543 ns |  0.75 |    0.01 |     - |     - |     - |         - |
|            DictSlim_TryGetValue | 10000 |  3.8692 ns | 0.0209 ns | 0.0196 ns |  0.34 |    0.00 |     - |     - |     - |         - |
|                Dict_TryGetValue | 10000 |  7.8740 ns | 0.0192 ns | 0.0160 ns |  0.70 |    0.01 |     - |     - |     - |         - |
|      ConcurrentDict_TryGetValue | 10000 |  9.8747 ns | 0.0218 ns | 0.0193 ns |  0.88 |    0.01 |     - |     - |     - |         - |
|       ImmutableDict_TryGetValue | 10000 | 30.7783 ns | 0.1466 ns | 0.1372 ns |  2.74 |    0.03 |     - |     - |     - |         - |

## V3:

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

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
 */
            private ImTools.V2.ImMap<string> _mapV2;
            public ImTools.V2.ImMap<string> V2_AddOrUpdate()
            {
                var map = ImTools.V2.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.V2.Experimental.ImMap<string> _mapExp;
            public ImTools.V2.Experimental.ImMap<string> AddOrUpdate_Exp()
            {
                var map = ImTools.V2.Experimental.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.ImMap<string> _mapV3;
            public ImTools.ImMap<string> V3_AddOrUpdate_ImMap()
            {
                var map = ImTools.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.V2.Experimental.ImMap<string>[] _mapSlots;
            public ImTools.V2.Experimental.ImMap<string>[] AddOrUpdate_ImMapSlots()
            {
                var slots = ImTools.V2.Experimental.ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
            }

            private ImTools.ImMap<string>[] _partMapV3;
            public ImTools.ImMap<string>[] V3_AddOrUpdate_PartitionedMap()
            {
                var parts = ImTools.PartitionedMap.CreateEmpty<string>();

                for (var i = 0; i < Count; i++)
                    parts.AddOrUpdate(i, i.ToString());

                return parts;
            }

            private DictionarySlim<int, string> _dictSlim;
            public DictionarySlim<int, string> DictSlim()
            {
                var map = new DictionarySlim<int, string>();

                for (var i = 0; i < Count; i++)
                    map.GetOrAddValueRef(i) = i.ToString();

                return map;
            }

            private Dictionary<int, string> _dict;
            public Dictionary<int, string> Dict()
            {
                var map = new Dictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            private ConcurrentDictionary<int, string> _concurDict;
            public ConcurrentDictionary<int, string> ConcurrentDict()
            {
                var map = new ConcurrentDictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            private ImmutableDictionary<int, string> _immutableDict;
            public ImmutableDictionary<int, string> ImmutableDict()
            {
                var builder = ImmutableDictionary.CreateBuilder<int, string>();

                for (var i = 0; i < Count; i++)
                    builder.Add(i, i.ToString());

                return builder.ToImmutable();
            }

            [Params(1, 10, 100)]//, 1_000, 10_000)]
            public int Count;

            public int LookupMaxKey;

            [GlobalSetup]
            public void Populate()
            {
                LookupMaxKey = Count - 1;

                _mapV2 = V2_AddOrUpdate();
                // _mapExp = AddOrUpdate_Exp();
                _mapV3 = V3_AddOrUpdate_ImMap();
                // _mapSlots = AddOrUpdate_ImMapSlots();
                _partMapV3 = V3_AddOrUpdate_PartitionedMap();
                _dictSlim = DictSlim();
                _dict = Dict();
                _concurDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            [Benchmark(Baseline = true)]
            public string V3_ImMap_TryFind()
            {
                _mapV3.TryFind(LookupMaxKey, out var result);
                return result;
            }

            // [Benchmark]
            public string V3_PartitionedImMap_TryFind()
            {
                _partMapV3[LookupMaxKey & ImTools.V2.Experimental.ImMapSlots.KEY_MASK_TO_FIND_SLOT].TryFind(LookupMaxKey, out var result);
                return result;
            }

            [Benchmark]
            public string V2_ImMap_TryFind()
            {
                _mapV2.TryFind(LookupMaxKey, out var result);
                return result;
            }

            // [Benchmark]
            public string Experimental_ImMap_TryFind()
            {
                _mapExp.TryFind(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark(Baseline = true)]
            public string ImMap_Experimental_GetValueOrDefault() =>
                _mapExp.GetValueOrDefault(LookupMaxKey);

            //[Benchmark]
            // public string ImMap_Experimental_ImMap234_GetValueOrDefault() =>
            //     _map234.GetValueOrDefault(LookupMaxKey);

            //[Benchmark]
            public string ImMapSlots_TryFind()
            {
                _mapSlots[LookupMaxKey & ImTools.V2.Experimental.ImMapSlots.KEY_MASK_TO_FIND_SLOT].TryFind(LookupMaxKey, out var result);
                return result;
            }

            // [Benchmark]
            public string DictSlim_TryGetValue()
            {
                _dictSlim.TryGetValue(LookupMaxKey, out var result);
                return result;
            }

            // [Benchmark]
            public string Dict_TryGetValue()
            {
                _dict.TryGetValue(LookupMaxKey, out var result);
                return result;
            }

            // [Benchmark]
            public string ConcurrentDict_TryGetValue()
            {
                _concurDict.TryGetValue(LookupMaxKey, out var result);
                return result;
            }

            // [Benchmark]
            public string ImmutableDict_TryGetValue()
            {
                _immutableDict.TryGetValue(LookupMaxKey, out var result);
                return result;
            }
        }

        /// <summary>
        /// It is often a pattern to Lookup the value and if not found to Add it to the map.
        /// Lookup for the missing key can be time consuming because it may be needed to expect the tree branch to the deepest
        /// only to find that key is not there.
        /// </summary>
        [MemoryDiagnoser]
        public class LookupMissing
        {
        /*
        |                                        Method | Count |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------------------------------- |------ |-----------:|----------:|----------:|-----------:|------:|--------:|------:|------:|------:|----------:|
        |          ImMap_Experimental_GetValueOrDefault |     1 |  1.2077 ns | 0.0191 ns | 0.0169 ns |  1.2059 ns |  1.00 |    0.00 |     - |     - |     - |         - |
        | ImMap_Experimental_ImMap234_GetValueOrDefault |     1 |  0.6328 ns | 0.0078 ns | 0.0065 ns |  0.6322 ns |  0.52 |    0.01 |     - |     - |     - |         - |
        |                                               |       |            |           |           |            |       |         |       |       |       |           |
        |          ImMap_Experimental_GetValueOrDefault |    10 |  3.6256 ns | 0.0441 ns | 0.0413 ns |  3.6209 ns |  1.00 |    0.00 |     - |     - |     - |         - |
        | ImMap_Experimental_ImMap234_GetValueOrDefault |    10 |  2.0067 ns | 0.0127 ns | 0.0119 ns |  2.0062 ns |  0.55 |    0.01 |     - |     - |     - |         - |
        |                                               |       |            |           |           |            |       |         |       |       |       |           |
        |          ImMap_Experimental_GetValueOrDefault |   100 |  4.8153 ns | 0.0395 ns | 0.0369 ns |  4.8123 ns |  1.00 |    0.00 |     - |     - |     - |         - |
        | ImMap_Experimental_ImMap234_GetValueOrDefault |   100 |  7.9644 ns | 0.0356 ns | 0.0333 ns |  7.9729 ns |  1.65 |    0.01 |     - |     - |     - |         - |
        |                                               |       |            |           |           |            |       |         |       |       |       |           |
        |          ImMap_Experimental_GetValueOrDefault |  1000 |  9.4704 ns | 0.0251 ns | 0.0223 ns |  9.4747 ns |  1.00 |    0.00 |     - |     - |     - |         - |
        | ImMap_Experimental_ImMap234_GetValueOrDefault |  1000 | 14.4405 ns | 0.3445 ns | 0.3686 ns | 14.4990 ns |  1.52 |    0.04 |     - |     - |     - |         - |
        |                                               |       |            |           |           |            |       |         |       |       |       |           |
        |          ImMap_Experimental_GetValueOrDefault | 10000 | 13.8073 ns | 0.3326 ns | 0.4769 ns | 13.5176 ns |  1.00 |    0.00 |     - |     - |     - |         - |
        | ImMap_Experimental_ImMap234_GetValueOrDefault | 10000 | 22.0925 ns | 0.2101 ns | 0.1965 ns | 22.0301 ns |  1.61 |    0.05 |     - |     - |     - |         - |
         */

            [Params(1, 10, 100, 1_000, 10_000)]
            public int Count;

            public int MissingKey;

            [GlobalSetup]
            public void Populate()
            {
                MissingKey = Count + 1;

                _mapExp = AddOrUpdate_Exp();
                // _map234 = AddOrUpdate_Exp_ImMap234();
            }


            [Benchmark(Baseline = true)]
            public string ImMap_Experimental_GetValueOrDefault() =>
                _mapExp.GetValueOrDefault(MissingKey);

            private ImTools.V2.Experimental.ImMap<string> _mapExp;
            public ImTools.V2.Experimental.ImMap<string> AddOrUpdate_Exp()
            {
                var map = ImTools.V2.Experimental.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }
        }

        [MemoryDiagnoser]
        public class Enumerate
        {
            /*
            ## V2
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


|                              Method | Count |            Mean |        Error |       StdDev | Ratio | RatioSD |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|------------------------------------ |------ |----------------:|-------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|
|              ImMap_EnumerateToArray |     1 |       111.10 ns |     0.490 ns |     0.435 ns |  1.00 |    0.00 |  0.0340 |       - |       - |     160 B |
|           ImMap_V1_EnumerateToArray |     1 |       120.70 ns |     0.436 ns |     0.386 ns |  1.09 |    0.01 |  0.0391 |       - |       - |     184 B |
|                   ImMap_FoldToArray |     1 |        35.21 ns |     0.227 ns |     0.212 ns |  0.32 |    0.00 |  0.0255 |       - |       - |     120 B |
| ImMap_FoldToArray_FoldReducerStruct |     1 |        35.07 ns |     0.125 ns |     0.111 ns |  0.32 |    0.00 |  0.0255 |       - |       - |     120 B |
|      Experimental_ImMap_FoldToArray |     1 |        41.52 ns |     0.322 ns |     0.301 ns |  0.37 |    0.00 |  0.0255 |       - |       - |     120 B |
|              ImMapSlots_FoldToArray |     1 |        57.45 ns |     0.269 ns |     0.251 ns |  0.52 |    0.00 |  0.0254 |       - |       - |     120 B |
| Experimental_ImMapSlots_FoldToArray |     1 |       143.24 ns |     0.685 ns |     0.641 ns |  1.29 |    0.01 |  0.0253 |       - |       - |     120 B |
|                    DictSlim_ToArray |     1 |       113.84 ns |     0.330 ns |     0.292 ns |  1.02 |    0.00 |  0.0373 |       - |       - |     176 B |
|                        Dict_ToArray |     1 |        31.78 ns |     0.260 ns |     0.230 ns |  0.29 |    0.00 |  0.0085 |       - |       - |      40 B |
|              ConcurrentDict_ToArray |     1 |       228.83 ns |     0.857 ns |     0.802 ns |  2.06 |    0.01 |  0.0083 |       - |       - |      40 B |
|               ImmutableDict_ToArray |     1 |       455.85 ns |     1.836 ns |     1.717 ns |  4.10 |    0.02 |  0.0081 |       - |       - |      40 B |
|                                     |       |                 |              |              |       |         |         |         |         |           |
|              ImMap_EnumerateToArray |    10 |       287.93 ns |     1.325 ns |     1.240 ns |  1.00 |    0.00 |  0.0949 |       - |       - |     448 B |
|           ImMap_V1_EnumerateToArray |    10 |       369.19 ns |     2.343 ns |     2.077 ns |  1.28 |    0.01 |  0.0968 |       - |       - |     456 B |
|                   ImMap_FoldToArray |    10 |       170.22 ns |     0.980 ns |     0.917 ns |  0.59 |    0.00 |  0.1001 |       - |       - |     472 B |
| ImMap_FoldToArray_FoldReducerStruct |    10 |       150.25 ns |     1.112 ns |     1.041 ns |  0.52 |    0.00 |  0.1001 |       - |       - |     472 B |
|      Experimental_ImMap_FoldToArray |    10 |       176.73 ns |     0.693 ns |     0.648 ns |  0.61 |    0.00 |  0.1001 |       - |       - |     472 B |
|              ImMapSlots_FoldToArray |    10 |       167.37 ns |     1.568 ns |     1.390 ns |  0.58 |    0.01 |  0.0918 |       - |       - |     432 B |
| Experimental_ImMapSlots_FoldToArray |    10 |       249.96 ns |     1.312 ns |     1.227 ns |  0.87 |    0.01 |  0.0916 |       - |       - |     432 B |
|                    DictSlim_ToArray |    10 |       340.28 ns |     1.950 ns |     1.824 ns |  1.18 |    0.01 |  0.1326 |       - |       - |     624 B |
|                        Dict_ToArray |    10 |        65.21 ns |     0.368 ns |     0.326 ns |  0.23 |    0.00 |  0.0391 |       - |       - |     184 B |
|              ConcurrentDict_ToArray |    10 |       257.08 ns |     1.457 ns |     1.138 ns |  0.89 |    0.01 |  0.0391 |       - |       - |     184 B |
|               ImmutableDict_ToArray |    10 |     1,799.44 ns |     3.894 ns |     3.251 ns |  6.25 |    0.03 |  0.0381 |       - |       - |     184 B |
|                                     |       |                 |              |              |       |         |         |         |         |           |
|              ImMap_EnumerateToArray |   100 |     1,620.80 ns |     5.929 ns |     5.546 ns |  1.00 |    0.00 |  0.4692 |  0.0019 |       - |    2216 B |
|           ImMap_V1_EnumerateToArray |   100 |     2,316.30 ns |     7.182 ns |     6.367 ns |  1.43 |    0.01 |  0.4692 |       - |       - |    2224 B |
|                   ImMap_FoldToArray |   100 |     1,080.23 ns |     3.741 ns |     2.921 ns |  0.67 |    0.00 |  0.6542 |  0.0038 |       - |    3080 B |
| ImMap_FoldToArray_FoldReducerStruct |   100 |       823.73 ns |     3.283 ns |     2.910 ns |  0.51 |    0.00 |  0.6542 |  0.0048 |       - |    3080 B |
|      Experimental_ImMap_FoldToArray |   100 |     1,055.48 ns |     4.004 ns |     3.745 ns |  0.65 |    0.00 |  0.6542 |  0.0038 |       - |    3080 B |
|              ImMapSlots_FoldToArray |   100 |       907.18 ns |     4.936 ns |     4.617 ns |  0.56 |    0.00 |  0.6475 |  0.0048 |       - |    3048 B |
| Experimental_ImMapSlots_FoldToArray |   100 |     1,202.60 ns |     4.109 ns |     3.643 ns |  0.74 |    0.00 |  0.6466 |  0.0038 |       - |    3048 B |
|                    DictSlim_ToArray |   100 |     2,011.05 ns |     8.163 ns |     7.636 ns |  1.24 |    0.01 |  0.8430 |  0.0076 |       - |    3984 B |
|                        Dict_ToArray |   100 |       398.15 ns |     2.618 ns |     2.449 ns |  0.25 |    0.00 |  0.3448 |  0.0019 |       - |    1624 B |
|              ConcurrentDict_ToArray |   100 |     2,065.41 ns |    14.971 ns |    14.004 ns |  1.27 |    0.01 |  0.3433 |       - |       - |    1624 B |
|               ImmutableDict_ToArray |   100 |    15,537.86 ns |    85.670 ns |    80.135 ns |  9.59 |    0.06 |  0.3357 |       - |       - |    1624 B |
|                                     |       |                 |              |              |       |         |         |         |         |           |
|              ImMap_EnumerateToArray |  1000 |    15,054.50 ns |    74.727 ns |    69.900 ns |  1.00 |    0.00 |  3.5553 |  0.1221 |       - |   16768 B |
|           ImMap_V1_EnumerateToArray |  1000 |    22,248.00 ns |    64.405 ns |    53.781 ns |  1.48 |    0.01 |  3.5400 |  0.1526 |       - |   16776 B |
|                   ImMap_FoldToArray |  1000 |    10,051.62 ns |    64.300 ns |    60.146 ns |  0.67 |    0.01 |  5.2490 |  0.2899 |       - |   24712 B |
| ImMap_FoldToArray_FoldReducerStruct |  1000 |     7,334.16 ns |    21.443 ns |    20.058 ns |  0.49 |    0.00 |  5.2490 |  0.2899 |       - |   24712 B |
|      Experimental_ImMap_FoldToArray |  1000 |     9,822.09 ns |    68.825 ns |    64.379 ns |  0.65 |    0.01 |  5.2490 |  0.2899 |       - |   24712 B |
|              ImMapSlots_FoldToArray |  1000 |     8,949.39 ns |    58.822 ns |    55.022 ns |  0.59 |    0.00 |  5.2338 |  0.3052 |       - |   24680 B |
| Experimental_ImMapSlots_FoldToArray |  1000 |    10,529.47 ns |    27.764 ns |    25.970 ns |  0.70 |    0.00 |  5.2338 |  0.3052 |       - |   24680 B |
|                    DictSlim_ToArray |  1000 |    16,649.68 ns |    63.599 ns |    59.491 ns |  1.11 |    0.01 |  6.9580 |  0.6104 |       - |   32880 B |
|                        Dict_ToArray |  1000 |     3,624.89 ns |    22.328 ns |    19.793 ns |  0.24 |    0.00 |  3.3875 |  0.1869 |       - |   16024 B |
|              ConcurrentDict_ToArray |  1000 |    16,913.26 ns |    75.327 ns |    70.461 ns |  1.12 |    0.01 |  3.3875 |  0.1831 |       - |   16024 B |
|               ImmutableDict_ToArray |  1000 |   152,451.63 ns |   509.580 ns |   476.662 ns | 10.13 |    0.05 |  3.1738 |       - |       - |   16024 B |
|                                     |       |                 |              |              |       |         |         |         |         |           |
|              ImMap_EnumerateToArray | 10000 |   158,659.55 ns |   492.752 ns |   384.709 ns |  1.00 |    0.00 | 44.6777 | 14.8926 |       - |  211928 B |
|           ImMap_V1_EnumerateToArray | 10000 |   238,534.87 ns | 1,317.101 ns | 1,232.017 ns |  1.50 |    0.01 | 44.6777 | 14.8926 |       - |  211936 B |
|                   ImMap_FoldToArray | 10000 |   185,608.77 ns | 1,156.637 ns | 1,081.919 ns |  1.17 |    0.01 | 60.0586 | 29.2969 | 15.3809 |  342659 B |
| ImMap_FoldToArray_FoldReducerStruct | 10000 |   153,426.27 ns |   258.939 ns |   242.211 ns |  0.97 |    0.00 | 60.0586 | 29.2969 | 15.3809 |  342659 B |
|      Experimental_ImMap_FoldToArray | 10000 |   181,060.33 ns |   921.031 ns |   861.533 ns |  1.14 |    0.00 | 60.0586 | 29.5410 | 15.3809 |  342657 B |
|              ImMapSlots_FoldToArray | 10000 |   193,669.62 ns | 1,706.895 ns | 1,596.630 ns |  1.22 |    0.01 | 60.0586 | 29.7852 | 15.3809 |  342622 B |
| Experimental_ImMapSlots_FoldToArray | 10000 |   197,295.56 ns |   960.392 ns |   898.351 ns |  1.24 |    0.01 | 60.0586 | 29.7852 | 15.3809 |  342619 B |
|                    DictSlim_ToArray | 10000 |   219,924.53 ns | 1,328.065 ns | 1,242.273 ns |  1.39 |    0.01 | 50.2930 | 27.8320 | 22.7051 |  422944 B |
|                        Dict_ToArray | 10000 |    75,081.85 ns | 1,492.327 ns | 1,596.774 ns |  0.47 |    0.01 |  0.8545 |  0.8545 |  0.8545 |  160018 B |
|              ConcurrentDict_ToArray | 10000 |    76,812.19 ns |   472.741 ns |   442.202 ns |  0.48 |    0.00 |  7.3242 |  7.3242 |  7.3242 |  160013 B |
|               ImmutableDict_ToArray | 10000 | 1,567,955.86 ns | 3,414.536 ns | 3,193.959 ns |  9.88 |    0.03 | 25.3906 | 25.3906 | 25.3906 |  160041 B |

## ImMap234

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18363
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.301
  [Host]     : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT

|                            Method | Count |          Mean |      Error |     StdDev | Ratio |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|---------------------------------- |------ |--------------:|-----------:|-----------:|------:|--------:|--------:|--------:|----------:|
|    Experimental_ImMap_FoldToArray |     1 |      40.36 ns |   0.198 ns |   0.185 ns |  1.00 |  0.0255 |       - |       - |     120 B |
| Experimental_ImMap234_FoldToArray |     1 |      49.45 ns |   0.429 ns |   0.402 ns |  1.23 |  0.0255 |       - |       - |     120 B |
|                                   |       |               |            |            |       |         |         |         |           |
|    Experimental_ImMap_FoldToArray |    10 |     179.89 ns |   1.357 ns |   1.269 ns |  1.00 |  0.1001 |       - |       - |     472 B |
| Experimental_ImMap234_FoldToArray |    10 |     178.30 ns |   0.976 ns |   0.913 ns |  0.99 |  0.0918 |       - |       - |     432 B |
|                                   |       |               |            |            |       |         |         |         |           |
|    Experimental_ImMap_FoldToArray |   100 |     995.43 ns |   3.059 ns |   2.862 ns |  1.00 |  0.6542 |  0.0038 |       - |    3080 B |
| Experimental_ImMap234_FoldToArray |   100 |   1,345.21 ns |   5.558 ns |   5.199 ns |  1.35 |  0.6409 |  0.0038 |       - |    3016 B |
|                                   |       |               |            |            |       |         |         |         |           |
|    Experimental_ImMap_FoldToArray |  1000 |   9,375.63 ns |  39.679 ns |  37.116 ns |  1.00 |  5.2490 |  0.2899 |       - |   24712 B |
| Experimental_ImMap234_FoldToArray |  1000 |  13,205.79 ns |  35.177 ns |  32.904 ns |  1.41 |  5.2338 |  0.2899 |       - |   24624 B |
|                                   |       |               |            |            |       |         |         |         |           |
|    Experimental_ImMap_FoldToArray | 10000 | 175,884.86 ns | 976.719 ns | 913.624 ns |  1.00 | 58.5938 | 27.8320 | 13.9160 |  342657 B |
| Experimental_ImMap234_FoldToArray | 10000 | 205,580.33 ns | 822.305 ns | 769.185 ns |  1.17 | 58.5938 | 27.8320 | 13.9160 |  342539 B |

## V3

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


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
*/

            #region Populate

            private ImTools.V2.ImMap<string> _mapV2;
            public ImTools.V2.ImMap<string> V2_AddOrUpdate()
            {
                var map = ImTools.V2.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.V2.Experimental.ImMap<string> _mapV2Exp;
            public ImTools.V2.Experimental.ImMap<string> V2_Exp_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.ImMap<string> _mapV3;
            public ImTools.ImMap<string> V3_AddOrUpdate()
            {
                var map = ImTools.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.ImMap<string>[] _mapPartV3;
            public ImTools.ImMap<string>[] V3_PartitionedMap_AddOrUpdate()
            {
                var parts = ImTools.PartitionedMap.CreateEmpty<string>();

                for (var i = 0; i < Count; i++)
                    parts.AddOrUpdate(i, i.ToString());

                return parts;
            }

            private DictionarySlim<int, string> _dictSlim;
            public DictionarySlim<int, string> DictSlim()
            {
                var map = new DictionarySlim<int, string>();

                for (var i = 0; i < Count; i++)
                    map.GetOrAddValueRef(i) = i.ToString();

                return map;
            }

            private Dictionary<int, string> _dict;
            public Dictionary<int, string> Dict()
            {
                var map = new Dictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            private ConcurrentDictionary<int, string> _concurDict;
            public ConcurrentDictionary<int, string> ConcurrentDict()
            {
                var map = new ConcurrentDictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            private ImmutableDictionary<int, string> _immutableDict;
            public ImmutableDictionary<int, string> ImmutableDict()
            {
                var builder = ImmutableDictionary.CreateBuilder<int, string>();

                for (var i = 0; i < Count; i++)
                    builder.Add(i, i.ToString());

                return builder.ToImmutable();
            }

            #endregion

            [Params(1, 10, 100, 1_000, 10_000)]
            public int Count;

            [GlobalSetup]
            public void Populate()
            {
                _mapV3      = V3_AddOrUpdate();
                _mapV2      = V2_AddOrUpdate();
                _mapPartV3  = V3_PartitionedMap_AddOrUpdate();
                _dictSlim   = DictSlim();
                _dict       = Dict();
                _concurDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            [Benchmark(Baseline = true)]
            public object V3_ImMap_foreach()
            {
                var s = "";
                foreach (var x in _mapV3.Enumerate())
                    s = x.Value;
                return s;
            }

            [Benchmark]
            public object V2_ImMap_foreach()
            {
                var s = "";
                foreach (var x in _mapV2.Enumerate())
                    s = x.Value;
                return s;
            }

            // [Benchmark]
            public object V2_ImMap_Experimental_EnumerateToArray() =>
                _mapV2Exp.Enumerate().ToArray();

            // [Benchmark]
            public object V3_PartitionedImMap_foreach()
            {
                var s = "";
                foreach (var x in _mapPartV3.Enumerate())
                    s = x.Value;
                return s;
            }

            // [Benchmark]
            public object DictSlim_foreach()
            {
                var s = "";
                foreach (var x in _dictSlim)
                    s = x.Value;
                return s;
            }

            // [Benchmark]
            public object Dict_foreach()
            {
                var s = "";
                foreach (var x in _dict)
                    s = x.Value;
                return s;
            }

            // [Benchmark]
            public object ConcurrentDict_foreach()
            {
                var s = "";
                foreach (var x in _concurDict)
                    s = x.Value;
                return s;
            }

            // [Benchmark]
            public object ImmutableDict_foreach()
            {
                var s = "";
                foreach (var x in _immutableDict)
                    s = x.Value;
                return s;
            }
        }
    }
}
