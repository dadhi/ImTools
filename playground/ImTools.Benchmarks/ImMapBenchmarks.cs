using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using ImTools;
using ImTools.Benchmarks;
using ImTools.Experimental;
using Microsoft.Collections.Extensions;

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


                ## 2019-04-01: Full-blown benchmark

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.648 (1803/April2018Update/Redstone4)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=2156254 Hz, Resolution=463.7673 ns, Timer=TSC
.NET Core SDK=3.0.100-preview3-010431
  [Host]     : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT

|                    Method |  Count |             Mean |          Error |          StdDev |           Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|-------------------------- |------- |-----------------:|---------------:|----------------:|-----------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|         ImMap_AddOrUpdate |     10 |         677.4 ns |       3.304 ns |       3.0910 ns |         677.0 ns |  1.00 |    0.00 |      0.4435 |           - |           - |             2.05 KB |
|      ImMap_V1_AddOrUpdate |     10 |         810.3 ns |       7.279 ns |       6.8085 ns |         807.9 ns |  1.20 |    0.01 |      0.5455 |           - |           - |             2.52 KB |
| DictSlim_GetOrAddValueRef |     10 |         440.2 ns |       1.045 ns |       0.9773 ns |         440.4 ns |  0.65 |    0.00 |      0.2437 |           - |           - |             1.13 KB |
|               Dict_TryAdd |     10 |         465.7 ns |       8.925 ns |      11.2878 ns |         470.2 ns |  0.68 |    0.02 |      0.2780 |           - |           - |             1.28 KB |
|     ConcurrentDict_TryAdd |     10 |         764.8 ns |       1.658 ns |       1.2941 ns |         765.0 ns |  1.13 |    0.01 |      0.3281 |           - |           - |             1.52 KB |
|         ImmutableDict_Add |     10 |       5,625.4 ns |     106.513 ns |      99.6319 ns |       5,672.8 ns |  8.30 |    0.14 |      0.6256 |           - |           - |             2.89 KB |
|                           |        |                  |                |                 |                  |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |    100 |      12,501.0 ns |     181.743 ns |     170.0026 ns |      12,517.9 ns |  1.00 |    0.00 |      7.9651 |           - |           - |            36.73 KB |
|      ImMap_V1_AddOrUpdate |    100 |      14,307.7 ns |      43.586 ns |      38.6381 ns |      14,301.3 ns |  1.15 |    0.01 |      9.4147 |           - |           - |            43.39 KB |
| DictSlim_GetOrAddValueRef |    100 |       3,694.1 ns |      71.555 ns |      79.5335 ns |       3,724.1 ns |  0.30 |    0.01 |      1.8311 |      0.0038 |           - |             8.45 KB |
|               Dict_TryAdd |    100 |       4,340.6 ns |      41.170 ns |      36.4957 ns |       4,348.1 ns |  0.35 |    0.01 |      2.8305 |           - |           - |            13.08 KB |
|     ConcurrentDict_TryAdd |    100 |      12,070.4 ns |      35.388 ns |      33.1017 ns |      12,070.1 ns |  0.97 |    0.01 |      4.8828 |      0.0153 |           - |            22.55 KB |
|         ImmutableDict_Add |    100 |     104,824.8 ns |   2,122.120 ns |   5,553.2191 ns |     106,168.5 ns |  7.89 |    0.81 |     10.6201 |           - |           - |            49.09 KB |
|                           |        |                  |                |                 |                  |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |   1000 |     228,929.6 ns |   4,169.645 ns |   3,900.2882 ns |     230,140.7 ns |  1.00 |    0.00 |    113.0371 |      0.2441 |           - |           521.94 KB |
|      ImMap_V1_AddOrUpdate |   1000 |     260,340.5 ns |   3,842.012 ns |   3,593.8209 ns |     260,879.7 ns |  1.14 |    0.03 |    127.9297 |      0.9766 |           - |           591.73 KB |
| DictSlim_GetOrAddValueRef |   1000 |      38,421.4 ns |     507.728 ns |     474.9295 ns |      38,580.0 ns |  0.17 |    0.00 |     15.5029 |      0.0610 |           - |            71.72 KB |
|               Dict_TryAdd |   1000 |      49,308.8 ns |     693.257 ns |     614.5539 ns |      49,280.4 ns |  0.22 |    0.00 |     28.2593 |      0.0610 |           - |           131.07 KB |
|     ConcurrentDict_TryAdd |   1000 |     144,514.2 ns |   2,808.447 ns |   2,489.6151 ns |     145,501.9 ns |  0.63 |    0.01 |     40.7715 |      9.5215 |           - |           200.83 KB |
|         ImmutableDict_Add |   1000 |   1,510,495.0 ns |  13,517.349 ns |  11,982.7778 ns |   1,509,123.8 ns |  6.61 |    0.10 |    150.3906 |           - |           - |           693.88 KB |
|                           |        |                  |                |                 |                  |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |  10000 |   5,236,366.2 ns | 103,401.736 ns | 151,564.9236 ns |   5,203,333.7 ns |  1.00 |    0.00 |   1117.1875 |    242.1875 |    109.3750 |          6879.88 KB |
|      ImMap_V1_AddOrUpdate |  10000 |   5,210,495.0 ns | 136,248.688 ns | 293,289.6752 ns |   5,077,546.9 ns |  1.02 |    0.09 |   1234.3750 |    226.5625 |    101.5625 |           7582.3 KB |
| DictSlim_GetOrAddValueRef |  10000 |     481,380.9 ns |   8,822.891 ns |   8,252.9382 ns |     477,200.6 ns |  0.09 |    0.00 |    125.0000 |    124.0234 |    124.0234 |          1023.47 KB |
|               Dict_TryAdd |  10000 |     589,676.6 ns |   7,133.904 ns |   6,673.0578 ns |     586,556.1 ns |  0.11 |    0.00 |    221.6797 |    221.6797 |    221.6797 |          1302.74 KB |
|     ConcurrentDict_TryAdd |  10000 |   3,168,594.9 ns |  32,814.610 ns |  30,694.8067 ns |   3,176,325.8 ns |  0.61 |    0.02 |    289.0625 |    128.9063 |     42.9688 |          1677.33 KB |
|         ImmutableDict_Add |  10000 |  19,435,948.8 ns |  84,745.419 ns |  79,270.9172 ns |  19,425,969.1 ns |  3.76 |    0.12 |   1468.7500 |    281.2500 |    125.0000 |           9124.5 KB |
|                           |        |                  |                |                 |                  |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate | 100000 |  64,378,774.9 ns | 569,934.609 ns | 533,117.1868 ns |  64,337,391.3 ns |  1.00 |    0.00 |  14375.0000 |   2250.0000 |    625.0000 |          84472.5 KB |
|      ImMap_V1_AddOrUpdate | 100000 |  66,387,743.3 ns | 317,490.210 ns | 281,446.8012 ns |  66,266,405.8 ns |  1.03 |    0.01 |  15375.0000 |   2000.0000 |    500.0000 |          91502.9 KB |
| DictSlim_GetOrAddValueRef | 100000 |  10,592,191.3 ns |  85,596.015 ns |  71,476.5493 ns |  10,583,706.9 ns |  0.16 |    0.00 |   1234.3750 |    968.7500 |    734.3750 |          9019.38 KB |
|               Dict_TryAdd | 100000 |  10,953,115.5 ns | 135,428.893 ns | 126,680.2701 ns |  10,989,448.3 ns |  0.17 |    0.00 |   1125.0000 |    812.5000 |    609.3750 |         12152.85 KB |
|     ConcurrentDict_TryAdd | 100000 |  35,298,247.4 ns | 634,210.234 ns | 562,210.8529 ns |  35,136,630.4 ns |  0.55 |    0.01 |   2625.0000 |   1250.0000 |    500.0000 |         15486.84 KB |
|         ImmutableDict_Add | 100000 | 247,218,695.0 ns | 755,870.150 ns | 707,041.4069 ns | 247,280,231.9 ns |  3.84 |    0.03 |  19000.0000 |   2666.6667 |    666.6667 |        112113.42 KB |


## With ImMapArray 16 slots wide

|                    Method |  Count |            Mean |             Error |            StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|-------------------------- |------- |----------------:|------------------:|------------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|         ImMap_AddOrUpdate |     10 |        680.0 ns |         1.8431 ns |         1.6338 ns |  1.00 |    0.00 |      0.4435 |           - |           - |              2096 B |
|    ImMapArray_AddOrUpdate |     10 |        393.6 ns |         8.6190 ns |         8.8510 ns |  0.58 |    0.01 |      0.2017 |           - |           - |               952 B |
| DictSlim_GetOrAddValueRef |     10 |        426.4 ns |         0.5609 ns |         0.4973 ns |  0.63 |    0.00 |      0.2437 |           - |           - |              1152 B |
|                           |        |                 |                   |                   |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |    100 |     12,687.8 ns |       195.3525 ns |       182.7328 ns |  1.00 |    0.00 |      7.9651 |           - |           - |             37616 B |
|    ImMapArray_AddOrUpdate |    100 |      6,925.2 ns |       111.9202 ns |        99.2144 ns |  0.55 |    0.01 |      3.7994 |           - |           - |             17944 B |
| DictSlim_GetOrAddValueRef |    100 |      3,597.3 ns |        68.0889 ns |        60.3591 ns |  0.28 |    0.01 |      1.8311 |      0.0038 |           - |              8656 B |
|                           |        |                 |                   |                   |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |   1000 |    215,487.0 ns |     2,630.5660 ns |     2,460.6330 ns |  1.00 |    0.00 |    113.0371 |      0.2441 |           - |            534464 B |
|    ImMapArray_AddOrUpdate |   1000 |    136,348.6 ns |     2,074.7837 ns |     1,839.2417 ns |  0.63 |    0.01 |     71.7773 |      0.4883 |           - |            339736 B |
| DictSlim_GetOrAddValueRef |   1000 |     34,208.2 ns |       252.4890 ns |       223.8249 ns |  0.16 |    0.00 |     15.5029 |      0.0610 |           - |             73440 B |
|                           |        |                 |                   |                   |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |  10000 |  4,445,681.3 ns |    43,057.6494 ns |    38,169.4847 ns |  1.00 |    0.00 |   1117.1875 |    242.1875 |    109.3750 |           7044992 B |
|    ImMapArray_AddOrUpdate |  10000 |  3,180,808.0 ns |    24,562.2906 ns |    22,975.5819 ns |  0.72 |    0.01 |    886.7188 |    382.8125 |           - |           5119192 B |
| DictSlim_GetOrAddValueRef |  10000 |    475,641.8 ns |     3,194.1984 ns |     2,987.8552 ns |  0.11 |    0.00 |    125.0000 |    124.5117 |    124.5117 |           1048032 B |
|                           |        |                 |                   |                   |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate | 100000 | 64,143,972.1 ns |   588,735.8856 ns |   491,621.1285 ns |  1.00 |    0.00 |  14375.0000 |   2250.0000 |    625.0000 |          86499832 B |
|    ImMapArray_AddOrUpdate | 100000 | 60,846,582.2 ns | 1,259,966.4033 ns | 1,178,573.3554 ns |  0.95 |    0.02 |  11333.3333 |   2111.1111 |    777.7778 |          67292254 B |
| DictSlim_GetOrAddValueRef | 100000 | 10,707,177.0 ns |    42,742.4592 ns |    35,691.8892 ns |  0.17 |    0.00 |   1234.3750 |    968.7500 |    734.3750 |           9235864 B |

## With ImMapArray 32 slots wide

|                    Method |  Count |            Mean |           Error |          StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|-------------------------- |------- |----------------:|----------------:|----------------:|------:|------------:|------------:|------------:|--------------------:|
|         ImMap_AddOrUpdate |     10 |        692.6 ns |       3.7668 ns |       3.5234 ns |  1.00 |      0.4435 |           - |           - |             2.05 KB |
|    ImMapArray_AddOrUpdate |     10 |        435.8 ns |       0.9462 ns |       0.8851 ns |  0.63 |      0.2284 |           - |           - |             1.05 KB |
| DictSlim_GetOrAddValueRef |     10 |        431.5 ns |       2.6810 ns |       2.5078 ns |  0.62 |      0.2437 |           - |           - |             1.13 KB |
|                           |        |                 |                 |                 |       |             |             |             |                     |
|         ImMap_AddOrUpdate |    100 |     12,625.3 ns |      26.2592 ns |      24.5629 ns |  1.00 |      7.9651 |           - |           - |            36.73 KB |
|    ImMapArray_AddOrUpdate |    100 |      5,152.7 ns |      27.7066 ns |      25.9168 ns |  0.41 |      2.8076 |           - |           - |            12.96 KB |
| DictSlim_GetOrAddValueRef |    100 |      3,475.8 ns |       7.0838 ns |       5.9153 ns |  0.28 |      1.8311 |      0.0038 |           - |             8.45 KB |
|                           |        |                 |                 |                 |       |             |             |             |                     |
|         ImMap_AddOrUpdate |   1000 |    208,429.4 ns |   2,518.4714 ns |   2,103.0377 ns |  1.00 |    113.0371 |      0.2441 |           - |           521.94 KB |
|    ImMapArray_AddOrUpdate |   1000 |    116,397.1 ns |   1,877.7439 ns |   1,756.4428 ns |  0.56 |     61.2793 |           - |           - |           282.77 KB |
| DictSlim_GetOrAddValueRef |   1000 |     33,169.6 ns |     221.3987 ns |     196.2642 ns |  0.16 |     15.5029 |      0.0610 |           - |            71.72 KB |
|                           |        |                 |                 |                 |       |             |             |             |                     |
|         ImMap_AddOrUpdate |  10000 |  4,439,074.1 ns |  13,805.1825 ns |  12,237.9347 ns |  1.00 |   1117.1875 |    242.1875 |    109.3750 |          6879.88 KB |
|    ImMapArray_AddOrUpdate |  10000 |  2,977,654.6 ns |  16,990.6991 ns |  15,061.8122 ns |  0.67 |    789.0625 |    390.6250 |           - |          4526.09 KB |
| DictSlim_GetOrAddValueRef |  10000 |    473,808.2 ns |   3,661.4160 ns |   3,424.8908 ns |  0.11 |    125.0000 |    124.5117 |    124.5117 |          1023.47 KB |
|                           |        |                 |                 |                 |       |             |             |             |                     |
|         ImMap_AddOrUpdate | 100000 | 64,666,640.0 ns | 383,741.7935 ns | 358,952.3117 ns |  1.00 |  14375.0000 |   2250.0000 |    625.0000 |         84472.49 KB |
|    ImMapArray_AddOrUpdate | 100000 | 55,192,515.7 ns | 207,117.6965 ns | 161,703.8738 ns |  0.86 |  10333.3333 |   2000.0000 |    777.7778 |         61020.22 KB |
| DictSlim_GetOrAddValueRef | 100000 | 10,754,478.8 ns |  93,433.6843 ns |  87,397.9263 ns |  0.17 |   1234.3750 |    968.7500 |    734.3750 |          9019.72 KB |

*/

            [Params(10, 100, 1_000, 10_000, 100_000)]
            public int Count;

            [Benchmark(Baseline = true)]
            public ImMap<string> ImMap_AddOrUpdate()
            {
                var map = ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            [Benchmark]
            public ImMapArray<string> ImMapArray_AddOrUpdate()
            {
                var map = ImMapArray<string>.Create();

                for (var i = 0; i < Count; i++)
                    map.AddOrUpdate(i, i.ToString());

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

## Experiments with TryFind:

|                     Method |  Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------- |------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|              ImMap_TryFind |     10 |  3.359 ns | 0.0188 ns | 0.0176 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    ImMap_GetValueOrDefault |     10 |  3.113 ns | 0.0195 ns | 0.0173 ns |  0.93 |    0.01 |     - |     - |     - |         - |
|           ImMap_V1_TryFind |     10 |  4.836 ns | 0.0547 ns | 0.0485 ns |  1.44 |    0.02 |     - |     - |     - |         - |
| ImMap_V1_GetValueOrDefault |     10 |  4.392 ns | 0.0367 ns | 0.0343 ns |  1.31 |    0.01 |     - |     - |     - |         - |
|       DictSlim_TryGetValue |     10 |  3.813 ns | 0.0310 ns | 0.0290 ns |  1.14 |    0.01 |     - |     - |     - |         - |
|           Dict_TryGetValue |     10 |  6.990 ns | 0.0210 ns | 0.0196 ns |  2.08 |    0.01 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue |     10 | 10.261 ns | 0.0230 ns | 0.0215 ns |  3.06 |    0.02 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue |     10 | 60.228 ns | 0.2097 ns | 0.1962 ns | 17.93 |    0.11 |     - |     - |     - |         - |
|                            |        |           |           |           |       |         |       |       |       |           |
|              ImMap_TryFind |    100 |  4.388 ns | 0.0205 ns | 0.0182 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    ImMap_GetValueOrDefault |    100 |  4.548 ns | 0.1364 ns | 0.1276 ns |  1.03 |    0.02 |     - |     - |     - |         - |
|           ImMap_V1_TryFind |    100 |  6.826 ns | 0.0156 ns | 0.0146 ns |  1.56 |    0.01 |     - |     - |     - |         - |
| ImMap_V1_GetValueOrDefault |    100 |  5.892 ns | 0.0391 ns | 0.0366 ns |  1.34 |    0.01 |     - |     - |     - |         - |
|       DictSlim_TryGetValue |    100 |  3.209 ns | 0.0304 ns | 0.0284 ns |  0.73 |    0.01 |     - |     - |     - |         - |
|           Dict_TryGetValue |    100 |  7.048 ns | 0.0281 ns | 0.0263 ns |  1.61 |    0.01 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue |    100 | 10.505 ns | 0.0314 ns | 0.0294 ns |  2.39 |    0.01 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue |    100 | 65.043 ns | 0.2988 ns | 0.2795 ns | 14.82 |    0.10 |     - |     - |     - |         - |
|                            |        |           |           |           |       |         |       |       |       |           |
|              ImMap_TryFind |   1000 |  6.392 ns | 0.0429 ns | 0.0401 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    ImMap_GetValueOrDefault |   1000 |  8.089 ns | 0.0495 ns | 0.0463 ns |  1.27 |    0.01 |     - |     - |     - |         - |
|           ImMap_V1_TryFind |   1000 |  8.274 ns | 0.0624 ns | 0.0553 ns |  1.29 |    0.01 |     - |     - |     - |         - |
| ImMap_V1_GetValueOrDefault |   1000 |  8.310 ns | 0.1230 ns | 0.1150 ns |  1.30 |    0.02 |     - |     - |     - |         - |
|       DictSlim_TryGetValue |   1000 |  3.221 ns | 0.0215 ns | 0.0201 ns |  0.50 |    0.00 |     - |     - |     - |         - |
|           Dict_TryGetValue |   1000 |  7.052 ns | 0.0190 ns | 0.0178 ns |  1.10 |    0.01 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue |   1000 | 10.514 ns | 0.0317 ns | 0.0296 ns |  1.64 |    0.01 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue |   1000 | 71.175 ns | 0.4042 ns | 0.3781 ns | 11.14 |    0.08 |     - |     - |     - |         - |
|                            |        |           |           |           |       |         |       |       |       |           |
|              ImMap_TryFind |  10000 | 10.671 ns | 0.0534 ns | 0.0500 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    ImMap_GetValueOrDefault |  10000 | 11.516 ns | 0.0353 ns | 0.0330 ns |  1.08 |    0.00 |     - |     - |     - |         - |
|           ImMap_V1_TryFind |  10000 | 13.508 ns | 0.2455 ns | 0.2297 ns |  1.27 |    0.02 |     - |     - |     - |         - |
| ImMap_V1_GetValueOrDefault |  10000 | 13.047 ns | 0.0462 ns | 0.0432 ns |  1.22 |    0.01 |     - |     - |     - |         - |
|       DictSlim_TryGetValue |  10000 |  3.189 ns | 0.0281 ns | 0.0263 ns |  0.30 |    0.00 |     - |     - |     - |         - |
|           Dict_TryGetValue |  10000 |  7.014 ns | 0.0508 ns | 0.0475 ns |  0.66 |    0.01 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue |  10000 | 12.146 ns | 0.0296 ns | 0.0277 ns |  1.14 |    0.01 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue |  10000 | 87.149 ns | 0.1908 ns | 0.1784 ns |  8.17 |    0.05 |     - |     - |     - |         - |
|                            |        |           |           |           |       |         |       |       |       |           |
|              ImMap_TryFind | 100000 | 16.492 ns | 0.4005 ns | 0.4768 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    ImMap_GetValueOrDefault | 100000 | 16.869 ns | 0.0528 ns | 0.0468 ns |  1.03 |    0.03 |     - |     - |     - |         - |
|           ImMap_V1_TryFind | 100000 | 21.360 ns | 0.0737 ns | 0.0690 ns |  1.30 |    0.04 |     - |     - |     - |         - |
| ImMap_V1_GetValueOrDefault | 100000 | 16.934 ns | 0.4106 ns | 0.3841 ns |  1.03 |    0.04 |     - |     - |     - |         - |
|       DictSlim_TryGetValue | 100000 |  3.212 ns | 0.0412 ns | 0.0386 ns |  0.20 |    0.01 |     - |     - |     - |         - |
|           Dict_TryGetValue | 100000 |  7.061 ns | 0.0197 ns | 0.0184 ns |  0.43 |    0.01 |     - |     - |     - |         - |
| ConcurrentDict_TryGetValue | 100000 | 10.479 ns | 0.0542 ns | 0.0507 ns |  0.64 |    0.02 |     - |     - |     - |         - |
|  ImmutableDict_TryGetValue | 100000 | 93.308 ns | 0.2382 ns | 0.2228 ns |  5.67 |    0.17 |     - |     - |     - |         - |


## With ImMapArray

|               Method |  Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------- |------- |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|        ImMap_TryFind |     10 |  3.4909 ns | 0.0326 ns | 0.0289 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   ImMapArray_TryFind |     10 |  0.7931 ns | 0.0081 ns | 0.0076 ns |  0.23 |    0.00 |     - |     - |     - |         - |
| DictSlim_TryGetValue |     10 |  3.3644 ns | 0.0177 ns | 0.0157 ns |  0.96 |    0.01 |     - |     - |     - |         - |
|                      |        |            |           |           |       |         |       |       |       |           |
|        ImMap_TryFind |    100 |  6.0164 ns | 0.1137 ns | 0.1008 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   ImMapArray_TryFind |    100 |  2.5086 ns | 0.0155 ns | 0.0138 ns |  0.42 |    0.01 |     - |     - |     - |         - |
| DictSlim_TryGetValue |    100 |  3.3828 ns | 0.0080 ns | 0.0063 ns |  0.56 |    0.01 |     - |     - |     - |         - |
|                      |        |            |           |           |       |         |       |       |       |           |
|        ImMap_TryFind |   1000 |  8.3910 ns | 0.2011 ns | 0.1881 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   ImMapArray_TryFind |   1000 |  4.6079 ns | 0.0279 ns | 0.0261 ns |  0.55 |    0.01 |     - |     - |     - |         - |
| DictSlim_TryGetValue |   1000 |  4.1306 ns | 0.1453 ns | 0.1288 ns |  0.49 |    0.02 |     - |     - |     - |         - |
|                      |        |            |           |           |       |         |       |       |       |           |
|        ImMap_TryFind |  10000 | 11.4614 ns | 0.1560 ns | 0.1459 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   ImMapArray_TryFind |  10000 |  7.9681 ns | 0.0838 ns | 0.0784 ns |  0.70 |    0.01 |     - |     - |     - |         - |
| DictSlim_TryGetValue |  10000 |  3.7304 ns | 0.0591 ns | 0.0524 ns |  0.33 |    0.01 |     - |     - |     - |         - |
|                      |        |            |           |           |       |         |       |       |       |           |
|        ImMap_TryFind | 100000 | 17.0515 ns | 0.3623 ns | 0.3025 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   ImMapArray_TryFind | 100000 |  9.9809 ns | 0.1497 ns | 0.1169 ns |  0.58 |    0.01 |     - |     - |     - |         - |
| DictSlim_TryGetValue | 100000 |  3.7463 ns | 0.0469 ns | 0.0439 ns |  0.22 |    0.01 |     - |     - |     - |         - |

 */
            public ImMap<string> AddOrUpdate()
            {
                var map = ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImMap<string> _map;

            public ImMapArray<string> AddOrUpdate_ImMapArray()
            {
                var map = ImMapArray<string>.Create();

                for (var i = 0; i < Count; i++)
                    map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImMapArray<string> _mapArray;

            public ImTools.OldVersions.V1.ImMap<string> AddOrUpdate_V1()
            {
                var map = ImTools.OldVersions.V1.ImMap<string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.AddOrUpdate(i, i.ToString());

                return map;
            }

            private ImTools.OldVersions.V1.ImMap<string> _mapV1;

            public DictionarySlim<int, string> DictSlim()
            {
                var map = new DictionarySlim<int, string>();

                for (var i = 0; i < Count; i++)
                    map.GetOrAddValueRef(i) = i.ToString();

                return map;
            }

            private DictionarySlim<int, string> _dictSlim;

            public Dictionary<int, string> Dict()
            {
                var map = new Dictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            private Dictionary<int, string> _dict;

            public ConcurrentDictionary<int, string> ConcurrentDict()
            {
                var map = new ConcurrentDictionary<int, string>();

                for (var i = 0; i < Count; i++)
                    map.TryAdd(i, i.ToString());

                return map;
            }

            private ConcurrentDictionary<int, string> _concurDict;

            public ImmutableDictionary<int, string> ImmutableDict()
            {
                var map = ImmutableDictionary<int, string>.Empty;

                for (var i = 0; i < Count; i++)
                    map = map.Add(i, i.ToString());

                return map;
            }

            private ImmutableDictionary<int, string> _immutableDict;

            [Params(10, 100, 1_000, 10_000, 100_000)]
            public int Count;

            public int LookupMaxKey;

            [GlobalSetup]
            public void Populate()
            {
                LookupMaxKey = Count - 1;

                _map = AddOrUpdate();
                _mapArray = AddOrUpdate_ImMapArray();
                _mapV1 = AddOrUpdate_V1();
                _dictSlim = DictSlim();
                _dict = Dict();
                _concurDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            [Benchmark(Baseline = true)]
            public string ImMap_TryFind()
            {
                _map.TryFind(LookupMaxKey, out var result);
                return result;
            }

            [Benchmark]
            public string ImMapArray_TryFind()
            {
                _mapArray.TryFind(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ImMap_GetValueOrDefault()
            {
                return _map.GetValueOrDefault(LookupMaxKey);
            }

            //[Benchmark]
            public string ImMap_V1_TryFind()
            {
                _mapV1.TryFind(LookupMaxKey, out var result);
                return result;
            }

            //[Benchmark]
            public string ImMap_V1_GetValueOrDefault()
            {
                return _mapV1.GetValueOrDefault(LookupMaxKey);
            }

            [Benchmark]
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
    }
}
