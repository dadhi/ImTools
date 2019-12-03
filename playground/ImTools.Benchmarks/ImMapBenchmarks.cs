using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using ImTools;
using ImTools.Experimental;
using Microsoft.Collections.Extensions;
using ImMap = ImTools.ImMap;
using ImMapSlots = ImTools.ImMapSlots;

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
                AddOrUpdate |    40 |  3,900.9 ns |  12.7144 ns |  11.8930 ns |  1.00 |    0.00 |      2.5482 |           - |           - |             12048 B |
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
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT

|                              Method |  Count |              Mean |          Error |         StdDev | Ratio | RatioSD |      Gen 0 |     Gen 1 |    Gen 2 |   Allocated |
|------------------------------------ |------- |------------------:|---------------:|---------------:|------:|--------:|-----------:|----------:|---------:|------------:|
|                   ImMap_AddOrUpdate |      1 |          23.56 ns |       0.120 ns |       0.106 ns |  1.00 |    0.00 |     0.0102 |         - |        - |        48 B |
|                ImMap_V1_AddOrUpdate |      1 |          28.48 ns |       0.126 ns |       0.112 ns |  1.21 |    0.01 |     0.0102 |         - |        - |        48 B |
|      ImMap_Experimental_AddOrUpdate |      1 |          19.80 ns |       0.181 ns |       0.160 ns |  0.84 |    0.01 |     0.0068 |         - |        - |        32 B |
|              ImMapSlots_AddOrUpdate |      1 |         123.65 ns |       0.684 ns |       0.640 ns |  5.25 |    0.03 |     0.0696 |         - |        - |       328 B |
| ImMapSlots_Experimental_AddOrUpdate |      1 |         108.43 ns |       1.166 ns |       1.091 ns |  4.61 |    0.05 |     0.0663 |         - |        - |       312 B |
|           DictSlim_GetOrAddValueRef |      1 |          53.48 ns |       0.761 ns |       0.712 ns |  2.27 |    0.03 |     0.0272 |         - |        - |       128 B |
|                         Dict_TryAdd |      1 |          47.14 ns |       0.087 ns |       0.073 ns |  2.00 |    0.01 |     0.0442 |         - |        - |       208 B |
|               ConcurrentDict_TryAdd |      1 |         177.05 ns |       2.586 ns |       2.418 ns |  7.51 |    0.09 |     0.1853 |    0.0012 |        - |       872 B |
|                   ImmutableDict_Add |      1 |         161.05 ns |       2.359 ns |       2.206 ns |  6.84 |    0.08 |     0.0219 |         - |        - |       104 B |
|                                     |        |                   |                |                |       |         |            |           |          |             |
|                   ImMap_AddOrUpdate |     10 |         645.98 ns |       7.178 ns |       6.363 ns |  1.00 |    0.00 |     0.3767 |    0.0010 |        - |      1776 B |
|                ImMap_V1_AddOrUpdate |     10 |         789.71 ns |       2.208 ns |       1.957 ns |  1.22 |    0.01 |     0.4787 |    0.0010 |        - |      2256 B |
|      ImMap_Experimental_AddOrUpdate |     10 |         513.81 ns |       1.668 ns |       1.393 ns |  0.80 |    0.01 |     0.2813 |    0.0010 |        - |      1328 B |
|              ImMapSlots_AddOrUpdate |     10 |         395.99 ns |       0.678 ns |       0.601 ns |  0.61 |    0.01 |     0.1612 |    0.0005 |        - |       760 B |
| ImMapSlots_Experimental_AddOrUpdate |     10 |         335.90 ns |       1.018 ns |       0.902 ns |  0.52 |    0.01 |     0.1273 |    0.0005 |        - |       600 B |
|           DictSlim_GetOrAddValueRef |     10 |         394.33 ns |       3.383 ns |       3.165 ns |  0.61 |    0.01 |     0.1764 |    0.0005 |        - |       832 B |
|                         Dict_TryAdd |     10 |         397.52 ns |       5.608 ns |       5.246 ns |  0.62 |    0.01 |     0.2089 |    0.0010 |        - |       984 B |
|               ConcurrentDict_TryAdd |     10 |         698.77 ns |       2.696 ns |       2.390 ns |  1.08 |    0.01 |     0.2613 |    0.0019 |        - |      1232 B |
|                   ImmutableDict_Add |     10 |       3,828.93 ns |       7.549 ns |       6.304 ns |  5.93 |    0.06 |     0.5569 |         - |        - |      2640 B |
|                                     |        |                   |                |                |       |         |            |           |          |             |
|                   ImMap_AddOrUpdate |    100 |      12,441.81 ns |      38.789 ns |      34.386 ns |  1.00 |    0.00 |     7.9193 |    0.3357 |        - |     37296 B |
|                ImMap_V1_AddOrUpdate |    100 |      15,002.77 ns |     196.035 ns |     183.371 ns |  1.20 |    0.02 |     9.3689 |    0.3967 |        - |     44112 B |
|      ImMap_Experimental_AddOrUpdate |    100 |      11,145.74 ns |      23.819 ns |      21.115 ns |  0.90 |    0.00 |     6.6376 |    0.3052 |        - |     31232 B |
|              ImMapSlots_AddOrUpdate |    100 |       5,434.98 ns |       8.083 ns |       6.750 ns |  0.44 |    0.00 |     2.7466 |    0.1678 |        - |     12952 B |
| ImMapSlots_Experimental_AddOrUpdate |    100 |       4,156.32 ns |      13.334 ns |      11.820 ns |  0.33 |    0.00 |     2.0828 |    0.1221 |        - |      9816 B |
|           DictSlim_GetOrAddValueRef |    100 |       3,532.01 ns |       4.958 ns |       4.140 ns |  0.28 |    0.00 |     1.7700 |    0.0725 |        - |      8336 B |
|                         Dict_TryAdd |    100 |       4,314.13 ns |      85.274 ns |     101.512 ns |  0.35 |    0.01 |     2.7695 |    0.1678 |        - |     13064 B |
|               ConcurrentDict_TryAdd |    100 |      12,092.97 ns |     112.740 ns |     105.457 ns |  0.97 |    0.01 |     4.8370 |    0.4730 |        - |     22768 B |
|                   ImmutableDict_Add |    100 |      67,138.11 ns |     330.026 ns |     292.559 ns |  5.40 |    0.02 |    10.4980 |    0.4883 |        - |     49953 B |
|                                     |        |                   |                |                |       |         |            |           |          |             |
|                   ImMap_AddOrUpdate |   1000 |     201,172.36 ns |     682.816 ns |     638.706 ns |  1.00 |    0.00 |   113.5254 |    0.2441 |        - |    534144 B |
|                ImMap_V1_AddOrUpdate |   1000 |     238,196.75 ns |   1,207.388 ns |   1,008.224 ns |  1.18 |    0.00 |   128.6621 |    0.2441 |        - |    605617 B |
|      ImMap_Experimental_AddOrUpdate |   1000 |     196,583.87 ns |     645.252 ns |     603.569 ns |  0.98 |    0.00 |    99.8535 |    0.2441 |        - |    470625 B |
|              ImMapSlots_AddOrUpdate |   1000 |     122,412.33 ns |     368.543 ns |     344.735 ns |  0.61 |    0.00 |    61.2793 |    0.4883 |        - |    289240 B |
| ImMapSlots_Experimental_AddOrUpdate |   1000 |     108,622.15 ns |     357.402 ns |     298.447 ns |  0.54 |    0.00 |    49.5605 |    0.1221 |        - |    233304 B |
|           DictSlim_GetOrAddValueRef |   1000 |      33,202.77 ns |     107.637 ns |     100.684 ns |  0.17 |    0.00 |    15.5029 |    0.0610 |        - |     73120 B |
|                         Dict_TryAdd |   1000 |      43,699.46 ns |     780.623 ns |     730.195 ns |  0.22 |    0.00 |    28.2593 |    0.0610 |        - |    133888 B |
|               ConcurrentDict_TryAdd |   1000 |     123,777.51 ns |   1,500.105 ns |   1,403.199 ns |  0.62 |    0.01 |    43.3350 |    0.4883 |        - |    205328 B |
|                   ImmutableDict_Add |   1000 |     978,158.40 ns |   3,596.423 ns |   3,188.135 ns |  4.86 |    0.03 |   150.3906 |    1.9531 |        - |    710219 B |
|                                     |        |                   |                |                |       |         |            |           |          |             |
|                   ImMap_AddOrUpdate |  10000 |   4,440,797.21 ns |  20,499.591 ns |  18,172.354 ns |  1.00 |    0.00 |  1109.3750 |  234.3750 | 101.5625 |   6972716 B |
|                ImMap_V1_AddOrUpdate |  10000 |   4,953,246.48 ns |  18,366.806 ns |  16,281.695 ns |  1.12 |    0.01 |  1226.5625 |  226.5625 | 101.5625 |   7691996 B |
|      ImMap_Experimental_AddOrUpdate |  10000 |   4,581,452.03 ns |  40,565.711 ns |  37,945.192 ns |  1.03 |    0.01 |  1007.8125 |  335.9375 | 140.6250 |   6333344 B |
|              ImMapSlots_AddOrUpdate |  10000 |   4,234,021.56 ns |  42,944.503 ns |  40,170.315 ns |  0.95 |    0.01 |   726.5625 |  273.4375 | 132.8125 |   4562460 B |
| ImMapSlots_Experimental_AddOrUpdate |  10000 |   4,289,442.92 ns |  20,246.747 ns |  18,938.820 ns |  0.97 |    0.01 |   625.0000 |  234.3750 |  93.7500 |   3936259 B |
|           DictSlim_GetOrAddValueRef |  10000 |     462,995.14 ns |     640.076 ns |     534.493 ns |  0.10 |    0.00 |   124.5117 |  124.5117 | 124.5117 |    975712 B |
|                         Dict_TryAdd |  10000 |     581,824.20 ns |   3,080.957 ns |   2,572.739 ns |  0.13 |    0.00 |   221.6797 |  221.6797 | 221.6797 |   1261681 B |
|               ConcurrentDict_TryAdd |  10000 |   2,944,875.49 ns |  55,065.429 ns |  51,508.236 ns |  0.66 |    0.01 |   277.3438 |  125.0000 |  42.9688 |   1645239 B |
|                   ImmutableDict_Add |  10000 |  14,896,543.65 ns | 122,769.329 ns | 114,838.506 ns |  3.36 |    0.03 |  1468.7500 |  281.2500 | 125.0000 |   9271168 B |
|                                     |        |                   |                |                |       |         |            |           |          |             |
|                   ImMap_AddOrUpdate | 100000 |  62,737,135.56 ns | 475,853.233 ns | 445,113.410 ns |  1.00 |    0.00 | 14222.2222 | 2000.0000 | 555.5556 |  85708228 B |
|                ImMap_V1_AddOrUpdate | 100000 |  67,135,128.33 ns | 498,808.614 ns | 466,585.887 ns |  1.07 |    0.01 | 15250.0000 | 2000.0000 | 500.0000 |  92907406 B |
|      ImMap_Experimental_AddOrUpdate | 100000 |  67,479,180.00 ns | 514,545.705 ns | 481,306.372 ns |  1.08 |    0.01 | 13250.0000 | 2250.0000 | 625.0000 |  79308331 B |
|              ImMapSlots_AddOrUpdate | 100000 |  63,064,830.56 ns | 272,580.912 ns | 212,813.247 ns |  1.01 |    0.01 | 10444.4444 | 1888.8889 | 555.5556 |  61692381 B |
| ImMapSlots_Experimental_AddOrUpdate | 100000 |  65,549,833.93 ns | 725,550.094 ns | 643,181.260 ns |  1.04 |    0.01 |  9250.0000 | 1750.0000 | 500.0000 |  55310900 B |
|           DictSlim_GetOrAddValueRef | 100000 |   9,111,648.96 ns |  40,519.754 ns |  37,902.203 ns |  0.15 |    0.00 |  1187.5000 |  796.8750 | 781.2500 |   8443497 B |
|                         Dict_TryAdd | 100000 |  10,599,253.79 ns | 127,656.283 ns | 113,163.970 ns |  0.17 |    0.00 |   937.5000 |  656.2500 | 546.8750 |  11652099 B |
|               ConcurrentDict_TryAdd | 100000 |  33,924,783.33 ns | 554,705.254 ns | 518,871.639 ns |  0.54 |    0.01 |  2500.0000 | 1125.0000 | 437.5000 |  15066820 B |
|                   ImmutableDict_Add | 100000 | 191,824,055.56 ns | 846,428.701 ns | 791,749.932 ns |  3.06 |    0.02 | 18666.6667 | 2666.6667 | 666.6667 | 114011155 B |


## Experiment #2

|                          Method | Count |          Mean |        Error |       StdDev |        Median | Ratio | RatioSD |    Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------------------- |------ |--------------:|-------------:|-------------:|--------------:|------:|--------:|---------:|-------:|------:|----------:|
|               ImMap_AddOrUpdate |     1 |      23.38 ns |     0.142 ns |     0.133 ns |      23.35 ns |  1.00 |    0.00 |   0.0102 |      - |     - |      48 B |
|  ImMap_Experimental_AddOrUpdate |     1 |      20.40 ns |     0.078 ns |     0.069 ns |      20.37 ns |  0.87 |    0.01 |   0.0068 |      - |     - |      32 B |
| ImMap_Experimental2_AddOrUpdate |     1 |      21.67 ns |     0.065 ns |     0.057 ns |      21.65 ns |  0.93 |    0.01 |   0.0068 |      - |     - |      32 B |
|                                 |       |               |              |              |               |       |         |          |        |       |           |
|               ImMap_AddOrUpdate |    10 |     646.80 ns |    19.395 ns |    18.142 ns |     637.00 ns |  1.00 |    0.00 |   0.3767 | 0.0010 |     - |    1776 B |
|  ImMap_Experimental_AddOrUpdate |    10 |     527.32 ns |     2.053 ns |     1.820 ns |     527.49 ns |  0.82 |    0.02 |   0.2813 | 0.0010 |     - |    1328 B |
| ImMap_Experimental2_AddOrUpdate |    10 |     451.12 ns |     9.098 ns |     9.735 ns |     444.45 ns |  0.70 |    0.03 |   0.2651 | 0.0005 |     - |    1248 B |
|                                 |       |               |              |              |               |       |         |          |        |       |           |
|               ImMap_AddOrUpdate |   100 |  12,710.30 ns |   252.728 ns |   270.416 ns |  12,850.21 ns |  1.00 |    0.00 |   7.9193 | 0.3204 |     - |   37296 B |
|  ImMap_Experimental_AddOrUpdate |   100 |  11,798.41 ns |   234.948 ns |   251.392 ns |  11,852.07 ns |  0.93 |    0.03 |   6.6376 | 0.3052 |     - |   31232 B |
| ImMap_Experimental2_AddOrUpdate |   100 |  10,362.05 ns |    21.455 ns |    17.916 ns |  10,362.89 ns |  0.81 |    0.01 |   6.4545 | 0.2899 |     - |   30432 B |
|                                 |       |               |              |              |               |       |         |          |        |       |           |
|               ImMap_AddOrUpdate |  1000 | 206,799.13 ns | 3,979.848 ns | 4,087.011 ns | 204,940.19 ns |  1.00 |    0.00 | 113.5254 | 0.2441 |     - |  534146 B |
|  ImMap_Experimental_AddOrUpdate |  1000 | 197,713.59 ns | 3,067.692 ns | 2,869.521 ns | 197,026.42 ns |  0.95 |    0.02 |  99.8535 | 0.2441 |     - |  470624 B |
| ImMap_Experimental2_AddOrUpdate |  1000 | 183,698.91 ns | 1,438.216 ns | 1,200.976 ns | 183,217.94 ns |  0.88 |    0.02 |  98.1445 | 0.2441 |     - |  462625 B |

*/

            [Params(1, 10, 100, 1_000)]//, 10_000, 100_000)]
            public int Count;

            [Benchmark(Baseline = true)]
            public ImTools.ImMap<string> ImMap_AddOrUpdate()
            {
                var map = ImTools.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            //[Benchmark]
            public ImTools.OldVersions.V1.ImMap<string> ImMap_V1_AddOrUpdate()
            {
                var map = ImTools.OldVersions.V1.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            [Benchmark]
            //[Benchmark(Baseline = true)]
            public ImTools.Experimental.ImMap<string> ImMap_Experimental_AddOrUpdate()
            {
                var map = ImTools.Experimental.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            [Benchmark]
            public ImTools.Experimental2.ImMap<string> ImMap_Experimental2_AddOrUpdate()
            {
                var map = ImTools.Experimental2.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = ImTools.Experimental2.ImMap.AddOrUpdate(map, i, i.ToString());

                return map;
            }

            //[Benchmark]
            public ImTools.ImMap<string>[] ImMapSlots_AddOrUpdate()
            {
                var slots = ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
            }

            //[Benchmark]
            public ImTools.Experimental.ImMap<string>[] ImMapSlots_Experimental_AddOrUpdate()
            {
                var slots = ImTools.Experimental.ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
            }

            //[Benchmark]
            public DictionarySlim<int, string> DictSlim_GetOrAddValueRef()
            {
                var map = new DictionarySlim<int, string>();

                for (var i = 0; i < Count; i++)
                    map.GetOrAddValueRef(i) = i.ToString();

                return map;
            }

            //[Benchmark]
            public Dictionary<int, string> Dict_TryAdd()
            {
                var map = new Dictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            //[Benchmark]
            public ConcurrentDictionary<int, string> ConcurrentDict_TryAdd()
            {
                var map = new ConcurrentDictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            //[Benchmark]
            public ImmutableDictionary<int, string> ImmutableDict_Add()
            {
                var map = ImmutableDictionary<int, string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.Add(i, i.ToString());

                return map;
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
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT


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

## Experimental

|                          Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |------ |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                   ImMap_TryFind |     1 |  0.6049 ns | 0.0866 ns | 0.1031 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind |     1 |  0.7273 ns | 0.0342 ns | 0.0303 ns |  1.29 |    0.32 |     - |     - |     - |         - |
|  ImMap_Experimental_TryFindData |     1 |  1.5122 ns | 0.0296 ns | 0.0277 ns |  2.68 |    0.67 |     - |     - |     - |         - |
|              ImMapSlots_TryFind |     1 |  1.1096 ns | 0.1018 ns | 0.0902 ns |  1.95 |    0.42 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind |     1 |  1.4498 ns | 0.0309 ns | 0.0289 ns |  2.57 |    0.65 |     - |     - |     - |         - |
|                                 |       |            |           |           |       |         |       |       |       |           |
|                   ImMap_TryFind |    10 |  3.2067 ns | 0.0357 ns | 0.0317 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind |    10 |  3.3929 ns | 0.0938 ns | 0.0877 ns |  1.06 |    0.03 |     - |     - |     - |         - |
|  ImMap_Experimental_TryFindData |    10 |  3.6635 ns | 0.1508 ns | 0.1337 ns |  1.14 |    0.04 |     - |     - |     - |         - |
|              ImMapSlots_TryFind |    10 |  1.0951 ns | 0.0203 ns | 0.0158 ns |  0.34 |    0.01 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind |    10 |  1.1594 ns | 0.0106 ns | 0.0088 ns |  0.36 |    0.00 |     - |     - |     - |         - |
|                                 |       |            |           |           |       |         |       |       |       |           |
|                   ImMap_TryFind |   100 |  5.6499 ns | 0.0719 ns | 0.0637 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind |   100 |  6.3405 ns | 0.0280 ns | 0.0262 ns |  1.12 |    0.01 |     - |     - |     - |         - |
|  ImMap_Experimental_TryFindData |   100 |  7.3972 ns | 0.0622 ns | 0.0519 ns |  1.31 |    0.02 |     - |     - |     - |         - |
|              ImMapSlots_TryFind |   100 |  3.0274 ns | 0.0177 ns | 0.0157 ns |  0.54 |    0.01 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind |   100 |  2.7556 ns | 0.0621 ns | 0.0581 ns |  0.49 |    0.01 |     - |     - |     - |         - |
|                                 |       |            |           |           |       |         |       |       |       |           |
|                   ImMap_TryFind |  1000 |  7.8028 ns | 0.0194 ns | 0.0162 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|      ImMap_Experimental_TryFind |  1000 |  9.3970 ns | 0.0342 ns | 0.0320 ns |  1.20 |    0.00 |     - |     - |     - |         - |
|  ImMap_Experimental_TryFindData |  1000 | 10.4084 ns | 0.0618 ns | 0.0547 ns |  1.33 |    0.01 |     - |     - |     - |         - |
|              ImMapSlots_TryFind |  1000 |  5.7929 ns | 0.0327 ns | 0.0306 ns |  0.74 |    0.00 |     - |     - |     - |         - |
| ImMapSlots_Experimental_TryFind |  1000 |  6.2946 ns | 0.0878 ns | 0.0821 ns |  0.81 |    0.01 |     - |     - |     - |         - |

## Inlining difference!

|                             Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                      ImMap_TryFind |     1 | 1.0721 ns | 0.0424 ns | 0.0376 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|         ImMap_Experimental_TryFind |     1 | 1.0650 ns | 0.0201 ns | 0.0188 ns |  0.99 |    0.04 |     - |     - |     - |         - |
| ImMap_Experimental_TryFind_Inlined |     1 | 0.6274 ns | 0.0185 ns | 0.0173 ns |  0.59 |    0.02 |     - |     - |     - |         - |
|                                    |       |           |           |           |       |         |       |       |       |           |
|                      ImMap_TryFind |    10 | 3.6679 ns | 0.0278 ns | 0.0246 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|         ImMap_Experimental_TryFind |    10 | 4.3106 ns | 0.0233 ns | 0.0195 ns |  1.17 |    0.01 |     - |     - |     - |         - |
| ImMap_Experimental_TryFind_Inlined |    10 | 3.4304 ns | 0.0408 ns | 0.0382 ns |  0.94 |    0.01 |     - |     - |     - |         - |
|                                    |       |           |           |           |       |         |       |       |       |           |
|                      ImMap_TryFind |   100 | 4.6623 ns | 0.0402 ns | 0.0376 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|         ImMap_Experimental_TryFind |   100 | 7.1235 ns | 0.0635 ns | 0.0563 ns |  1.53 |    0.02 |     - |     - |     - |         - |
| ImMap_Experimental_TryFind_Inlined |   100 | 5.4459 ns | 0.0220 ns | 0.0195 ns |  1.17 |    0.01 |     - |     - |     - |         - |


 */
            private ImTools.ImMap<string> _map;
            public ImTools.ImMap<string> AddOrUpdate()
            {
                var map = ImTools.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.OldVersions.V1.ImMap<string> _mapV1;
            public ImTools.OldVersions.V1.ImMap<string> AddOrUpdate_V1()
            {
                var map = ImTools.OldVersions.V1.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.Experimental.ImMap<string> _mapExp;
            public ImTools.Experimental.ImMap<string> AddOrUpdate_Exp()
            {
                var map = ImTools.Experimental.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.ImMap<string>[] _mapSlots;
            public ImTools.ImMap<string>[] AddOrUpdate_ImMapSlots()
            {
                var slots = ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
            }

            private ImTools.Experimental.ImMap<string>[] _mapSlotsExp;
            public ImTools.Experimental.ImMap<string>[] AddOrUpdate_ImMapSlots_Exp()
            {
                var slots = ImTools.Experimental.ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
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
                var map = ImmutableDictionary<int, string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.Add(i, i.ToString());

                return map;
            }

            [Params(1, 10, 100)]//, 1_000, 10_000, 100_000)]
            public int Count;

            public int LookupMaxKey;

            [GlobalSetup]
            public void Populate()
            {
                LookupMaxKey = Count - 1;

                _map = AddOrUpdate();
                _mapV1 = AddOrUpdate_V1();
                _mapExp = AddOrUpdate_Exp();
                _mapSlots = AddOrUpdate_ImMapSlots();
                _mapSlotsExp = AddOrUpdate_ImMapSlots_Exp();
                _dictSlim = DictSlim();
                _dict = Dict();
                _concurDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            //[Benchmark(Baseline = true)]
            public string ImMap_TryFind()
            {
                _map.TryFind(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ImMap_V1_TryFind()
            {
                _mapV1.TryFind(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            [Benchmark(Baseline = true)]
            public string ImMap_Experimental_TryFind()
            {
                _mapExp.TryFind(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ImMap_Experimental_GetValueOrDefault()
            {
                return _mapExp.GetValueOrDefault(LookupMaxKey);
            }

            //[Benchmark]
            public string ImMapSlots_TryFind()
            {
                _mapSlots[LookupMaxKey & ImMapSlots.KEY_MASK_TO_FIND_SLOT].TryFind(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ImMapSlots_Experimental_TryFind()
            {
                _mapSlotsExp[LookupMaxKey & ImTools.Experimental.ImMapSlots.KEY_MASK_TO_FIND_SLOT].TryFind(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ImMap_GetValueOrDefault()
            {
                return _map.GetValueOrDefault(LookupMaxKey);
            }

            //[Benchmark]
            public string ImMap_V1_GetValueOrDefault()
            {
                return _mapV1.GetValueOrDefault(LookupMaxKey);
            }

            //[Benchmark]
            public string DictSlim_TryGetValue()
            {
                _dictSlim.TryGetValue(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string Dict_TryGetValue()
            {
                _dict.TryGetValue(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ConcurrentDict_TryGetValue()
            {
                _concurDict.TryGetValue(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ImmutableDict_TryGetValue()
            {
                _immutableDict.TryGetValue(LookupMaxKey, out var result);
                return result;
            }
        }

        [MemoryDiagnoser]
        public class Enumerate
        {
/*
## V2

|                         Method |  Count |             Mean |          Error |         StdDev | Ratio | RatioSD |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|------------------------------- |------- |-----------------:|---------------:|---------------:|------:|--------:|--------:|--------:|--------:|----------:|
|         ImMap_EnumerateToArray |      1 |        112.56 ns |       1.242 ns |       1.162 ns |  1.00 |    0.00 |  0.0340 |       - |       - |     160 B |
|      ImMap_V1_EnumerateToArray |      1 |        120.32 ns |       0.351 ns |       0.311 ns |  1.07 |    0.01 |  0.0391 |       - |       - |     184 B |
|              ImMap_FoldToArray |      1 |         34.14 ns |       0.203 ns |       0.190 ns |  0.30 |    0.00 |  0.0255 |       - |       - |     120 B |
|         ImMapSlots_FoldToArray |      1 |         55.19 ns |       0.185 ns |       0.173 ns |  0.49 |    0.00 |  0.0254 |       - |       - |     120 B |
| ImMap_Experimental_FoldToArray |      1 |         35.36 ns |       0.109 ns |       0.096 ns |  0.31 |    0.00 |  0.0255 |       - |       - |     120 B |
|               DictSlim_ToArray |      1 |        118.93 ns |       2.452 ns |       2.294 ns |  1.06 |    0.02 |  0.0372 |       - |       - |     176 B |
|                   Dict_ToArray |      1 |         32.98 ns |       0.591 ns |       0.553 ns |  0.29 |    0.01 |  0.0085 |       - |       - |      40 B |
|         ConcurrentDict_ToArray |      1 |        222.83 ns |       0.842 ns |       0.657 ns |  1.97 |    0.01 |  0.0083 |       - |       - |      40 B |
|          ImmutableDict_ToArray |      1 |        460.15 ns |       9.011 ns |       9.641 ns |  4.09 |    0.10 |  0.0081 |       - |       - |      40 B |
|                                |        |                  |                |                |       |         |         |         |         |           |
|         ImMap_EnumerateToArray |     10 |        292.52 ns |       5.830 ns |       6.713 ns |  1.00 |    0.00 |  0.0949 |       - |       - |     448 B |
|      ImMap_V1_EnumerateToArray |     10 |        377.47 ns |       3.431 ns |       3.042 ns |  1.28 |    0.03 |  0.0968 |       - |       - |     456 B |
|              ImMap_FoldToArray |     10 |        165.32 ns |       3.297 ns |       4.049 ns |  0.57 |    0.02 |  0.1001 |       - |       - |     472 B |
|         ImMapSlots_FoldToArray |     10 |        170.89 ns |       3.413 ns |       3.193 ns |  0.58 |    0.02 |  0.0918 |       - |       - |     432 B |
| ImMap_Experimental_FoldToArray |     10 |        195.06 ns |       3.905 ns |       3.653 ns |  0.66 |    0.01 |  0.1001 |       - |       - |     472 B |
|               DictSlim_ToArray |     10 |        337.32 ns |       2.994 ns |       2.801 ns |  1.15 |    0.03 |  0.1326 |       - |       - |     624 B |
|                   Dict_ToArray |     10 |         66.66 ns |       1.204 ns |       1.126 ns |  0.23 |    0.01 |  0.0391 |       - |       - |     184 B |
|         ConcurrentDict_ToArray |     10 |        243.89 ns |       0.778 ns |       0.649 ns |  0.83 |    0.02 |  0.0391 |       - |       - |     184 B |
|          ImmutableDict_ToArray |     10 |      1,791.78 ns |      30.391 ns |      28.428 ns |  6.10 |    0.21 |  0.0381 |       - |       - |     184 B |
|                                |        |                  |                |                |       |         |         |         |         |           |
|         ImMap_EnumerateToArray |    100 |      1,612.49 ns |      35.420 ns |      49.654 ns |  1.00 |    0.00 |  0.4692 |  0.0019 |       - |    2216 B |
|      ImMap_V1_EnumerateToArray |    100 |      2,338.79 ns |      38.249 ns |      35.778 ns |  1.43 |    0.06 |  0.4692 |       - |       - |    2224 B |
|              ImMap_FoldToArray |    100 |      1,030.10 ns |       8.368 ns |       7.828 ns |  0.63 |    0.02 |  0.6542 |  0.0038 |       - |    3080 B |
|         ImMapSlots_FoldToArray |    100 |        927.08 ns |       9.959 ns |       9.315 ns |  0.57 |    0.02 |  0.6475 |  0.0048 |       - |    3048 B |
| ImMap_Experimental_FoldToArray |    100 |      1,267.60 ns |      24.876 ns |      27.650 ns |  0.78 |    0.03 |  0.6542 |  0.0038 |       - |    3080 B |
|               DictSlim_ToArray |    100 |      1,887.14 ns |      35.421 ns |      33.132 ns |  1.16 |    0.04 |  0.8450 |  0.0076 |       - |    3984 B |
|                   Dict_ToArray |    100 |        385.92 ns |       1.574 ns |       1.395 ns |  0.24 |    0.01 |  0.3448 |  0.0019 |       - |    1624 B |
|         ConcurrentDict_ToArray |    100 |      1,965.60 ns |      15.761 ns |      14.743 ns |  1.20 |    0.04 |  0.3433 |       - |       - |    1624 B |
|          ImmutableDict_ToArray |    100 |     15,326.90 ns |     205.371 ns |     182.056 ns |  9.37 |    0.36 |  0.3357 |       - |       - |    1624 B |
|                                |        |                  |                |                |       |         |         |         |         |           |
|         ImMap_EnumerateToArray |   1000 |     14,802.01 ns |     203.875 ns |     190.705 ns |  1.00 |    0.00 |  3.5400 |  0.1221 |       - |   16768 B |
|      ImMap_V1_EnumerateToArray |   1000 |     22,641.28 ns |     182.790 ns |     170.982 ns |  1.53 |    0.02 |  3.5400 |  0.1526 |       - |   16776 B |
|              ImMap_FoldToArray |   1000 |      9,762.56 ns |     194.758 ns |     216.473 ns |  0.66 |    0.02 |  5.2490 |  0.2899 |       - |   24712 B |
|         ImMapSlots_FoldToArray |   1000 |      8,726.37 ns |      34.284 ns |      30.391 ns |  0.59 |    0.01 |  5.2338 |  0.3052 |       - |   24680 B |
| ImMap_Experimental_FoldToArray |   1000 |     11,778.07 ns |     158.037 ns |     147.828 ns |  0.80 |    0.02 |  5.2490 |  0.2899 |       - |   24712 B |
|               DictSlim_ToArray |   1000 |     16,176.66 ns |     178.605 ns |     167.067 ns |  1.09 |    0.02 |  6.9580 |  0.6104 |       - |   32880 B |
|                   Dict_ToArray |   1000 |      3,589.94 ns |      60.187 ns |      53.355 ns |  0.24 |    0.00 |  3.3875 |  0.1869 |       - |   16024 B |
|         ConcurrentDict_ToArray |   1000 |     16,127.76 ns |      66.325 ns |      62.040 ns |  1.09 |    0.02 |  3.3875 |  0.1831 |       - |   16024 B |
|          ImmutableDict_ToArray |   1000 |    147,352.40 ns |     361.145 ns |     320.145 ns |  9.96 |    0.14 |  3.1738 |       - |       - |   16024 B |
|                                |        |                  |                |                |       |         |         |         |         |           |
|         ImMap_EnumerateToArray |  10000 |    164,603.12 ns |     852.060 ns |     797.017 ns |  1.00 |    0.00 | 44.6777 | 14.8926 |       - |  211928 B |
|      ImMap_V1_EnumerateToArray |  10000 |    239,139.72 ns |     491.672 ns |     459.910 ns |  1.45 |    0.01 | 44.6777 | 14.8926 |       - |  211936 B |
|              ImMap_FoldToArray |  10000 |    240,178.70 ns |     490.201 ns |     458.534 ns |  1.46 |    0.01 | 60.0586 | 29.5410 | 15.3809 |  342658 B |
|         ImMapSlots_FoldToArray |  10000 |    240,304.50 ns |     408.461 ns |     382.075 ns |  1.46 |    0.01 | 60.0586 | 29.2969 | 15.3809 |  342617 B |
| ImMap_Experimental_FoldToArray |  10000 |    240,461.36 ns |     574.004 ns |     536.924 ns |  1.46 |    0.01 | 60.0586 | 29.5410 | 15.3809 |  342656 B |
|               DictSlim_ToArray |  10000 |    206,822.71 ns |   3,423.543 ns |   2,858.814 ns |  1.26 |    0.02 | 49.8047 | 27.8320 | 22.2168 |  422933 B |
|                   Dict_ToArray |  10000 |     70,304.48 ns |   1,282.667 ns |   1,137.051 ns |  0.43 |    0.01 |  0.8545 |  0.8545 |  0.8545 |  160017 B |
|         ConcurrentDict_ToArray |  10000 |     74,605.83 ns |   1,478.227 ns |   2,428.770 ns |  0.45 |    0.02 |  7.9346 |  7.9346 |  7.9346 |  160011 B |
|          ImmutableDict_ToArray |  10000 |  1,501,992.49 ns |   5,199.549 ns |   4,609.265 ns |  9.12 |    0.05 | 25.3906 | 25.3906 | 25.3906 |  160025 B |
|                                |        |                  |                |                |       |         |         |         |         |           |
|         ImMap_EnumerateToArray | 100000 |  2,310,086.97 ns |  33,245.043 ns |  29,470.865 ns |  1.00 |    0.00 | 46.8750 | 27.3438 | 19.5313 | 1849629 B |
|      ImMap_V1_EnumerateToArray | 100000 |  3,084,042.88 ns |  61,066.506 ns |  72,695.332 ns |  1.33 |    0.04 | 50.7813 | 27.3438 | 23.4375 | 1849542 B |
|              ImMap_FoldToArray | 100000 |  2,438,813.90 ns |  48,758.488 ns | 117,757.230 ns |  1.05 |    0.04 | 50.7813 | 23.4375 | 23.4375 | 2897738 B |
|         ImMapSlots_FoldToArray | 100000 |  2,508,368.33 ns |  49,968.017 ns |  82,098.885 ns |  1.08 |    0.04 | 54.6875 | 27.3438 | 27.3438 | 2897702 B |
| ImMap_Experimental_FoldToArray | 100000 |  2,559,333.72 ns |  50,061.766 ns |  71,797.088 ns |  1.10 |    0.03 | 54.6875 | 27.3438 | 27.3438 | 2897730 B |
|               DictSlim_ToArray | 100000 |  2,485,378.79 ns |  49,660.073 ns | 133,408.560 ns |  1.08 |    0.07 | 54.6875 | 35.1563 | 27.3438 | 3698046 B |
|                   Dict_ToArray | 100000 |    719,827.15 ns |  18,714.741 ns |  55,180.837 ns |  0.29 |    0.03 |  6.8359 |  6.8359 |  6.8359 | 1600012 B |
|         ConcurrentDict_ToArray | 100000 |    807,512.24 ns |  16,052.575 ns |  45,798.901 ns |  0.35 |    0.02 |  7.8125 |  7.8125 |  7.8125 | 1600013 B |
|          ImmutableDict_ToArray | 100000 | 15,956,568.54 ns | 166,283.019 ns | 155,541.239 ns |  6.91 |    0.11 |       - |       - |       - | 1600024 B |

*/

            #region Populate

            private ImTools.ImMap<string> _map;
            public ImTools.ImMap<string> AddOrUpdate()
            {
                var map = ImTools.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.OldVersions.V1.ImMap<string> _mapV1;
            public ImTools.OldVersions.V1.ImMap<string> AddOrUpdate_V1()
            {
                var map = ImTools.OldVersions.V1.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.Experimental.ImMap<string> _mapExp;
            public ImTools.Experimental.ImMap<string> AddOrUpdate_Exp()
            {
                var map = ImTools.Experimental.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.ImMap<string>[] _mapSlots;
            public ImTools.ImMap<string>[] AddOrUpdate_ImMapSlots()
            {
                var slots = ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
            }

            private ImTools.Experimental.ImMap<string>[] _mapSlotsExp;
            public ImTools.Experimental.ImMap<string>[] AddOrUpdate_ImMapSlots_Exp()
            {
                var slots = ImTools.Experimental.ImMapSlots.CreateWithEmpty<string>();

                for (var i = 0; i < Count; i++)
                    slots.AddOrUpdate(i, i.ToString());

                return slots;
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
                var map = ImmutableDictionary<int, string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.Add(i, i.ToString());

                return map;
            }

            #endregion

            [Params(1, 10, 100, 1_000)]//, 10_000, 100_000)]
            public int Count;

            [GlobalSetup]
            public void Populate()
            {
                _map = AddOrUpdate();
                _mapV1 = AddOrUpdate_V1();
                _mapExp = AddOrUpdate_Exp();
                _mapSlots = AddOrUpdate_ImMapSlots();
                _mapSlotsExp = AddOrUpdate_ImMapSlots_Exp();
                _dictSlim = DictSlim();
                _dict = Dict();
                _concurDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            [Benchmark(Baseline = true)]
            public object ImMap_EnumerateToArray() => 
                _map.Enumerate().ToArray();

            //[Benchmark]
            public object ImMap_V1_EnumerateToArray() =>
                _mapV1.Enumerate().ToArray();

            [Benchmark]
            public object ImMap_FoldToArray() =>
                _map.Fold(new List<ImTools.ImMap<string>>(), (item, list) => { list.Add(item); return list; }).ToArray();

            [Benchmark]
            public object ImMap_FoldToArray_FoldReducerStruct() =>
                _map.Fold(new List<ImTools.ImMap<string>>(), new AddToListReducer()).ToArray();

            struct AddToListReducer : IFoldReducer<ImTools.ImMap<string>, List<ImTools.ImMap<string>>>
            {
                public List<ImTools.ImMap<string>> Reduce(ImTools.ImMap<string> x, List<ImTools.ImMap<string>> state)
                {
                    state.Add(x);
                    return state;
                }
            }

            [Benchmark]
            public object ImMapSlots_FoldToArray() => 
                _mapSlots.Fold(new List<ImTools.ImMap<string>>(), (item, list) => { list.Add(item); return list; }).ToArray();

            [Benchmark]
            public object ImMap_Experimental_FoldToArray() =>
                _mapExp.Fold(new List<ImMapData<string>>(), (item, list) => { list.Add(item); return list; }).ToArray();

            //[Benchmark]
            public object DictSlim_ToArray() => _dictSlim.ToArray();

            //[Benchmark]
            public object Dict_ToArray() => _dict.ToArray();

            //[Benchmark]
            public object ConcurrentDict_ToArray() => _concurDict.ToArray();

            //[Benchmark]
            public object ImmutableDict_ToArray() => _immutableDict.ToArray();
        }
    }
}
