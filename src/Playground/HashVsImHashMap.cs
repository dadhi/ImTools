using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using ImTools;

namespace Playground
{
    public static class HashVsImHashMap
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
            private readonly HashMap<Type, string, TypeEqualityComparer> _mapLinearDistanceBuffer = new HashMap<Type, string, TypeEqualityComparer>();
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
            private readonly HashMap_SimpleLinear<Type, string, TypeEqualityComparer> _mapLinear = new HashMap_SimpleLinear<Type, string, TypeEqualityComparer>();
            private readonly TypeHashMap<string> _mapLinearWithDistanceBuffer = new TypeHashMap<string>();
            private readonly HashMapLeapfrog<Type, string, TypeEqualityComparer> _mapLeapfrogWithDistanceBuffer = new HashMapLeapfrog<Type, string, TypeEqualityComparer>();

            [Params(10, 100, 1000)]
            public int ItemCount;

            [GlobalSetup]
            public void GlobalSetup()
            {
                var keys = _keys;

                for (var i = 0; i < ItemCount; i++)
                {
                    _concurrentDict.TryAdd(keys[i], "a");
                    Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(keys[i], "a"));
                    _mapLinearWithDistanceBuffer.AddOrUpdate(keys[i], "a");
                    _mapLeapfrogWithDistanceBuffer.AddOrUpdate(keys[i], "a");
                    _mapLinear.AddOrUpdate(keys[i], "a");
                }

                _concurrentDict.TryAdd(_key, _value);
                Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(_key, _value));
                _mapLinearWithDistanceBuffer.AddOrUpdate(_key, _value);
                _mapLeapfrogWithDistanceBuffer.AddOrUpdate(_key, _value);
                _mapLinear.AddOrUpdate(_key, _value);
            }

            [Benchmark]
            public bool GetFromConcurrentDictionary()
            {
                string value;
                return _concurrentDict.TryGetValue(_key, out value);
            }

            [Benchmark]
            public bool GetFromImHashMap()
            {
                string value;
                return _imMap.TryFind(_key, out value);
            }

            [Benchmark(Baseline = true)]
            public bool GetFromHashMap_Linear_TryFind()
            {
                string value;
                return _mapLinear.TryFind(_key, out value);
            }

            [Benchmark]
            public bool GetFromHashMap_LinearWithDistanceBuffer_TryFind()
            {
                string value;
                return _mapLinearWithDistanceBuffer.TryFind(_key, out value);
            }

            [Benchmark]
            public bool GetFromHashMap_LeapfrogWithDistanceBuffer_TryFind()
            {
                string value;
                return _mapLeapfrogWithDistanceBuffer.TryFind(_key, out value);
            }
        }
    }
}
