using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace ImTools.Benchmarks
{
    public class HashVsImHashMap
    {
        private static readonly Type _key = typeof(HashVsImHashMap);
        private static readonly string _value = "hey";

        private const int MaxTypeCount = 2000;

        // use standard collection types as keys
        private static readonly Type[] _keys = typeof(Dictionary<,>).Assembly.GetTypes().Take(MaxTypeCount).ToArray();

        [MemoryDiagnoser]
        public class Populate
        {
            [Params(10, 100, 1000)] public int ItemCount;

            private ImHashMap<Type, string> _imMap = ImHashMap<Type, string>.Empty;
            private readonly HashMapDistanceBuffer<Type, string, TypeEqualityComparer> _mapLinearDistanceBuffer = new HashMapDistanceBuffer<Type, string, TypeEqualityComparer>();
            private readonly HashMapLeapfrog<Type, string, TypeEqualityComparer> _mapLeapfrogWithDistanceBuffer = new HashMapLeapfrog<Type, string, TypeEqualityComparer>();

            [Benchmark]
            public void PopulateImHashMap()
            {
                var keys = _keys;
                for (var i = 0; i < ItemCount; i++)
                    Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(keys[i], "a"));
                Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(_key, _value));
            }

            [Benchmark(Baseline = true)]
            public void PopulateHashMap_LinearWithDistanceBuffer()
            {
                var keys = _keys;
                for (var i = 0; i < ItemCount; i++)
                    _mapLinearDistanceBuffer.AddOrUpdate(keys[i], "a");
                _mapLinearDistanceBuffer.AddOrUpdate(_key, _value);
            }

            [Benchmark]
            public void PopulateHashMap_LeapfrogWithDistanceBuffer()
            {
                var keys = _keys;
                for (var i = 0; i < ItemCount; i++)
                    _mapLeapfrogWithDistanceBuffer.AddOrUpdate(keys[i], "a");
                _mapLeapfrogWithDistanceBuffer.AddOrUpdate(_key, _value);
            }
        }

        [MemoryDiagnoser]
        public class GetOrDefault
        {
            private readonly ConcurrentDictionary<Type, string> _concurrentDict = new ConcurrentDictionary<Type, string>();
           
            private ImHashMap<Type, string> _imMap = ImHashMap<Type, string>.Empty;

            private readonly HashMapSimpleLinear<Type, string, TypeEqualityComparer> _mapLinear = 
                new HashMapSimpleLinear<Type, string, TypeEqualityComparer>();
            private readonly HashMapDistanceBuffer<Type, string, TypeEqualityComparer> _mapLinearWithDistanceBuffer = 
                new HashMapDistanceBuffer<Type, string, TypeEqualityComparer>();
            private readonly HashMapLeapfrog<Type, string, TypeEqualityComparer> _mapLeapfrogWithDistanceBuffer = 
                new HashMapLeapfrog<Type, string, TypeEqualityComparer>();

            [Params(50)]//, 100, 1000)]
            public int ItemCount;

            [GlobalSetup]
            public void GlobalSetup()
            {
                var keys = _keys;

                for (var i = 0; i < ItemCount; i++)
                {
                    var key = keys[i];

                    _concurrentDict.TryAdd(key, "a");
                    Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(key, "a"));
                    _mapLinearWithDistanceBuffer.AddOrUpdate(key, "a");
                    _mapLeapfrogWithDistanceBuffer.AddOrUpdate(key, "a");
                    _mapLinear.AddOrUpdate(key, "a");
                }

                _concurrentDict.TryAdd(_key, _value);
                Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(_key, _value));
                _mapLinearWithDistanceBuffer.AddOrUpdate(_key, _value);
                _mapLeapfrogWithDistanceBuffer.AddOrUpdate(_key, _value);
                _mapLinear.AddOrUpdate(_key, _value);
            }

            [Benchmark(Baseline = true)]
            public bool GetFromConcurrentDictionary() =>
                _concurrentDict.TryGetValue(_key, out var _);

            [Benchmark]
            public bool GetFromImHashMap() =>
                _imMap.TryFind(_key, out var _);

            [Benchmark]
            public bool GetFromHashMap_Linear_TryFind() =>
                _mapLinear.TryFind(_key, out var _);

            [Benchmark]
            public bool GetFromHashMap_LinearWithDistanceBuffer_TryFind() =>
                _mapLinearWithDistanceBuffer.TryFind(_key, out var _);

            [Benchmark]
            public bool GetFromHashMap_LeapfrogWithDistanceBuffer_TryFind() =>
                _mapLeapfrogWithDistanceBuffer.TryFind(_key, out var _);
        }
    }
}
