using BenchmarkDotNet.Attributes;

namespace ImTools.Experimental.Tree234
{
    /*

    ## Being a different descendant in a hierarchy seem does not influence the result

    |         Method |     Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |--------------- |---------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
    |   EmptyViaOuts | 2.682 ns | 0.0486 ns | 0.0455 ns |  1.00 |    0.00 | 0.0051 |     - |     - |      24 B |
    | Branch2ViaOuts | 2.389 ns | 0.0393 ns | 0.0367 ns |  0.89 |    0.02 | 0.0051 |     - |     - |      24 B |
    | Branch3ViaOuts | 2.395 ns | 0.0523 ns | 0.0463 ns |  0.89 |    0.02 | 0.0051 |     - |     - |      24 B |

    ### Struct loses

    |           Method |     Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |----------------- |---------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
    |   Branch2ViaOuts | 1.986 ns | 0.0493 ns | 0.0437 ns |  1.00 |    0.00 | 0.0051 |     - |     - |      24 B |
    | Branch2ViaStruct | 7.780 ns | 0.0325 ns | 0.0288 ns |  3.92 |    0.09 | 0.0136 |     - |     - |      64 B |

    ### Using one ref and one out and one less argument
    
    |         Method |     Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |--------------- |---------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
    | Branch2ViaOuts | 2.507 ns | 0.0456 ns | 0.0381 ns |  1.00 |    0.00 | 0.0051 |     - |     - |      24 B |
    | Branch2ViaRefs | 1.893 ns | 0.0319 ns | 0.0298 ns |  0.76 |    0.02 | 0.0051 |     - |     - |      24 B |

    */
    [MemoryDiagnoser]
    public class ReturnFromMethodWays
    {
        private static readonly ImMap<int> _empty = ImMap<int>.Empty;
        private static readonly ImMap<int> _branch2 = new ImMapBranch2<int>(null, null, null);
        private static readonly ImMap<int> _branch3 = new ImMapBranch3<int>(null, null, null, null, null);
        private static readonly int _key42 = 42;


        //[Benchmark(Baseline = true)]
        public object EmptyViaOuts() => _empty.AddOrUpdateEntry(_key42, new ImMapEntry<int>(_key42), out _, out _);

        //[Benchmark(Baseline = true)]
        public object Branch2ViaOuts()
        {
            var entry = new ImMapEntry<int>(_key42);
            return _branch2.AddOrUpdateEntry(_key42, entry, out _, out _);
        }

        [Benchmark(Baseline = true)]
        public object Branch2ViaRefs()
        {
            var entry = new ImMapEntry<int>(_key42);
            return _branch2.AddOrUpdateX(_key42, ref entry, out _);
        }

        [Benchmark]
        public object Branch2ViaWithoutOut()
        {
            var entry = new ImMapEntry<int>(_key42);
            return _branch2.AddOrUpdateX(_key42, entry);
        }

        //[Benchmark]
        public object Branch3ViaOuts() => _branch3.AddOrUpdateEntry(_key42, new ImMapEntry<int>(_key42), out _, out _);
    }
}
