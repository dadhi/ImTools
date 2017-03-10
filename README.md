# ImTools

Immutable persistent collections and helper structures designed for performance and simplicity for .NET

Split from [DryIoc](https://bitbucket.org/dadhi/dryioc). 

Originally `ImTools.ImHashMap` was used in DryIoc v1.0 (in 2014) for fast delegate cache, and starting from DryIoc v2.0 for both service registry and cache.

## Benchmark

Based on [this great benchmark](https://gist.github.com/mrange/d6e7415113ebfa52ccb660f4ce534dd4) with F# and C# collections.

### Lookup

#### Speed

![Lookup Speed](BenchmarkResults/perf_Lookup.png)

#### Memory

No collection for all participants


### Insert

#### Speed

![Insert Speed](BenchmarkResults/perf_Insert.png)

#### GC counts

![GC Counts](BenchmarkResults/cc_Insert.png)


### Remove

#### Speed

![Insert Speed](BenchmarkResults/perf_Remove.png)

#### GC counts

![GC Counts](BenchmarkResults/cc_Remove.png)

