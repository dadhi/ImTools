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
            [Params(10, 100, 1000, MaxTypeCount)] public int ItemCount;

            private ImHashMap<Type, string> _imMap = ImHashMap<Type, string>.Empty;
            private readonly TypeHashMap<string> _map = new TypeHashMap<string>();

            [Benchmark]
            public void PopulateImHashMap()
            {
                var keys = _keys;
                for (var i = 0; i < ItemCount; i++)
                    Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(keys[i], "a"));
                Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(_key, _value));
            }

            [Benchmark(Baseline = true)]
            public void PopulateHashMap()
            {
                var keys = _keys;
                for (var i = 0; i < ItemCount; i++)
                    _map.AddOrUpdate(keys[i], "a");
                _map.AddOrUpdate(_key, _value);
            }
        }

        [MemoryDiagnoser]
        public class GetOrDefault
        {
            private readonly ConcurrentDictionary<Type, string> _concurrentDict = new ConcurrentDictionary<Type, string>();
            private ImHashMap<Type, string> _imMap = ImHashMap<Type, string>.Empty;
            private readonly TypeHashMap<string> _map = new TypeHashMap<string>();
            private readonly HashMapLF<Type, string, TypeEqualityComparer> _mapLF = new HashMapLF<Type, string, TypeEqualityComparer>();

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
                    _map.AddOrUpdate(keys[i], "a");
                    _mapLF.AddOrUpdate(keys[i], "a");
                }

                _concurrentDict.TryAdd(_key, _value);
                Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(_key, _value));
                _map.AddOrUpdate(_key, _value);
                _mapLF.AddOrUpdate(_key, _value);
            }

            //[Benchmark]
            public bool GetFromConcurrentDictionary()
            {
                string value;
                return _concurrentDict.TryGetValue(_key, out value);
            }

            //[Benchmark]
            public bool GetFromImHashMap()
            {
                string value;
                return _imMap.TryFind(_key, out value);
            }

            [Benchmark(Baseline = true)]
            public bool GetFromHashMap()
            {
                string value;
                return _map.TryFind(_key, out value);
            }

            [Benchmark]
            public bool GetFromHashMapLF_TryFind()
            {
                string value;
                return _mapLF.TryFind(_key, out value);
            }

            [Benchmark]
            public object GetFromHashMapLF_GetOrDefault()
            {
                return _mapLF.GetValueOrDefault(_key);
            }
        }
    }
}
