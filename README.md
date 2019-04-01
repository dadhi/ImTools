# ImTools

[![license](https://img.shields.io/github/license/dadhi/ImTools.svg)](http://opensource.org/licenses/MIT)

- Windows: [![Windows build](https://ci.appveyor.com/api/projects/status/el9echuqfnl86u53?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/imtools/branch/master)
- Linux, MacOS: [![Linux build](https://travis-ci.org/dadhi/ImTools.svg?branch=master)](https://travis-ci.org/dadhi/ImTools)

- Lib package: [![NuGet Badge](https://buildstats.info/nuget/ImTools.dll)](https://www.nuget.org/packages/ImTools.dll)
- Code package: [![NuGet Badge](https://buildstats.info/nuget/ImTools)](https://www.nuget.org/packages/ImTools)

Immutable persistent collections, Ref, and Array helpers designed for performance.

Note: concurrent HashMap from tests below is out until #2 is fixed.

Split from [DryIoc](https://github.com/dadhi/dryioc).


## Benchmarks

The comparison is done against the `ImMap` V1 version and a variety of BCL C# collections including the experimental `DictionarySlim<K, V>`.

__Note:__ Keep in mind that immutable collections have a different use-case and a thread-safety guarantees compared to 
`Dictionary`, `DictionarySlim` or even `ConcurrentDictionary`. The benchmarks do not take the collection "nature" into
account and run though a simplest available API path.

*Benchmark environment*:
```
BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.648 (1803/April2018Update/Redstone4)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=2156254 Hz, Resolution=463.7673 ns, Timer=TSC
.NET Core SDK=3.0.100-preview3-010431
  [Host]     : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
```


### ImMap with string values

`ImMap<V>` stores items with `int` keys and `V` values.

#### ImMap Population

[The benchmark]() inserts from 10 to 100 000 `Count` of items into the `ImMap<string>`, where value is `i.ToString()`:

```md
|         Method |  Count |             Mean |            Error |         StdDev |           Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------- |------- |-----------------:|-----------------:|---------------:|-----------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|    AddOrUpdate |     10 |         743.3 ns |        14.888 ns |      22.736 ns |         750.6 ns |  1.00 |    0.00 |      0.4435 |           - |           - |             2.05 KB |
| AddOrUpdate_V1 |     10 |         912.0 ns |         8.791 ns |       8.223 ns |         913.7 ns |  1.24 |    0.05 |      0.5455 |           - |           - |             2.52 KB |
|       DictSlim |     10 |         487.9 ns |         5.223 ns |       4.885 ns |         490.4 ns |  0.66 |    0.02 |      0.2432 |           - |           - |             1.13 KB |
|           Dict |     10 |         499.0 ns |         7.526 ns |       7.040 ns |         499.6 ns |  0.68 |    0.03 |      0.2775 |           - |           - |             1.28 KB |
| ConcurrentDict |     10 |         831.5 ns |         9.485 ns |       8.408 ns |         830.0 ns |  1.13 |    0.05 |      0.3281 |           - |           - |             1.52 KB |
|  ImmutableDict |     10 |       5,855.0 ns |        77.285 ns |      68.511 ns |       5,849.1 ns |  7.99 |    0.36 |      0.6256 |           - |           - |             2.89 KB |
|                |        |                  |                  |                |                  |       |         |             |             |             |                     |
|    AddOrUpdate |    100 |      13,362.6 ns |       190.580 ns |     178.269 ns |      13,365.9 ns |  1.00 |    0.00 |      7.9651 |           - |           - |            36.73 KB |
| AddOrUpdate_V1 |    100 |      16,033.7 ns |       312.907 ns |     321.333 ns |      16,025.3 ns |  1.20 |    0.03 |      9.3994 |           - |           - |            43.39 KB |
|       DictSlim |    100 |       3,865.0 ns |        67.341 ns |      62.991 ns |       3,893.5 ns |  0.29 |    0.00 |      1.8311 |      0.0076 |           - |             8.45 KB |
|           Dict |    100 |       4,603.5 ns |        86.274 ns |      84.733 ns |       4,573.8 ns |  0.34 |    0.01 |      2.8305 |           - |           - |            13.08 KB |
| ConcurrentDict |    100 |      13,379.4 ns |       240.312 ns |     224.788 ns |      13,378.0 ns |  1.00 |    0.02 |      4.8828 |      0.0153 |           - |            22.55 KB |
|  ImmutableDict |    100 |     134,249.0 ns |     5,938.707 ns |  16,750.227 ns |     127,154.7 ns | 12.09 |    1.00 |     10.4980 |           - |           - |            49.09 KB |
|                |        |                  |                  |                |                  |       |         |             |             |             |                     |
|    AddOrUpdate |   1000 |     230,725.9 ns |     4,599.315 ns |   4,921.215 ns |     231,345.6 ns |  1.00 |    0.00 |    113.0371 |      0.2441 |           - |           521.94 KB |
| AddOrUpdate_V1 |   1000 |     268,741.7 ns |     2,910.480 ns |   2,722.465 ns |     267,778.2 ns |  1.17 |    0.02 |    127.9297 |      0.9766 |           - |           591.73 KB |
|       DictSlim |   1000 |      38,825.2 ns |       373.443 ns |     349.319 ns |      38,874.3 ns |  0.17 |    0.00 |     15.5029 |      0.0610 |           - |            71.72 KB |
|           Dict |   1000 |      50,930.7 ns |       706.116 ns |     660.501 ns |      50,967.4 ns |  0.22 |    0.00 |     28.2593 |      0.0610 |           - |           131.07 KB |
| ConcurrentDict |   1000 |     151,383.4 ns |     3,024.966 ns |   3,236.679 ns |     151,997.4 ns |  0.66 |    0.02 |     40.2832 |     10.9863 |           - |           200.83 KB |
|  ImmutableDict |   1000 |   1,563,374.3 ns |    17,481.999 ns |  15,497.337 ns |   1,567,899.3 ns |  6.79 |    0.17 |    150.3906 |           - |           - |           693.88 KB |
|                |        |                  |                  |                |                  |       |         |             |             |             |                     |
|    AddOrUpdate |  10000 |   5,210,074.1 ns |   101,059.377 ns | 108,132.401 ns |   5,203,187.8 ns |  1.00 |    0.00 |   1117.1875 |    242.1875 |    109.3750 |          6879.88 KB |
| AddOrUpdate_V1 |  10000 |   5,536,987.6 ns |   105,362.100 ns | 108,199.119 ns |   5,523,152.6 ns |  1.06 |    0.03 |   1234.3750 |    226.5625 |    101.5625 |           7582.3 KB |
|       DictSlim |  10000 |     538,502.9 ns |     9,718.263 ns |   9,090.469 ns |     539,931.9 ns |  0.10 |    0.00 |    125.0000 |    124.0234 |    124.0234 |          1023.47 KB |
|           Dict |  10000 |     691,828.6 ns |     8,418.093 ns |   7,874.289 ns |     691,434.3 ns |  0.13 |    0.00 |    221.6797 |    221.6797 |    221.6797 |          1302.74 KB |
| ConcurrentDict |  10000 |   3,024,352.8 ns |    16,424.423 ns |  15,363.416 ns |   3,026,478.6 ns |  0.58 |    0.01 |    289.0625 |    132.8125 |     46.8750 |          1677.28 KB |
|  ImmutableDict |  10000 |  20,176,203.5 ns |   200,076.005 ns | 177,362.167 ns |  20,176,115.5 ns |  3.86 |    0.10 |   1468.7500 |    281.2500 |    125.0000 |           9124.5 KB |
|                |        |                  |                  |                |                  |       |         |             |             |             |                     |
|    AddOrUpdate | 100000 |  63,627,326.7 ns |   861,825.737 ns | 806,152.329 ns |  63,635,669.4 ns |  1.00 |    0.00 |  14166.6667 |   2166.6667 |    500.0000 |         84472.49 KB |
| AddOrUpdate_V1 | 100000 |  67,924,728.1 ns |   608,436.720 ns | 569,132.085 ns |  67,847,642.3 ns |  1.07 |    0.01 |  15375.0000 |   2000.0000 |    500.0000 |          91502.9 KB |
|       DictSlim | 100000 |  10,599,567.2 ns |    65,726.158 ns |  61,480.289 ns |  10,585,122.8 ns |  0.17 |    0.00 |   1234.3750 |    968.7500 |    734.3750 |          9019.52 KB |
|           Dict | 100000 |  11,081,448.7 ns |   211,506.442 ns | 226,309.523 ns |  11,077,191.0 ns |  0.17 |    0.00 |   1125.0000 |    828.1250 |    609.3750 |         12152.75 KB |
| ConcurrentDict | 100000 |  35,157,520.3 ns |   122,092.266 ns |  95,321.610 ns |  35,174,897.1 ns |  0.55 |    0.01 |   2625.0000 |   1250.0000 |    500.0000 |         15486.84 KB |
|  ImmutableDict | 100000 | 247,594,341.3 ns | 1,064,044.965 ns | 995,308.320 ns | 247,812,487.2 ns |  3.89 |    0.05 |  19000.0000 |   2666.6667 |    666.6667 |        112113.12 KB |

```

### ImMap Lookup

**TODO:**


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

            // Creating a new registry with +1 registration and new refeence to cache value
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
