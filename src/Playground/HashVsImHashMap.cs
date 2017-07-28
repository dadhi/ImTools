using System;
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
            [Params(10, 50, 1000, MaxTypeCount)] public int ItemCount;

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
            private ImHashMap<Type, string> _imMap = ImHashMap<Type, string>.Empty;
            private readonly TypeHashMap<string> _map = new TypeHashMap<string>();

            [Params(10, 100, 1000)]
            public int ItemCount;

            [GlobalSetup]
            public void GlobalSetup()
            {
                var keys = _keys;

                for (var i = 0; i < ItemCount; i++)
                    Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(keys[i], "a"));
                Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(_key, _value));

                for (var i = 0; i < ItemCount; i++)
                    _map.AddOrUpdate(keys[i], "a");
                _map.AddOrUpdate(_key, _value);
            }

            [Benchmark]
            public object GetFromImHashMap()
            {
                return _imMap.GetValueOrDefault(_key);
            }

            [Benchmark(Baseline = true)]
            public object GetFromHashMap()
            {
                return _map.GetValueOrDefault(_key);
            }

            [Benchmark]
            public object GetFromHashMap_Inlined()
            {
                return _map.GetValueOrDefault_Inlined(_key);
            }

            [Benchmark]
            public object GetFromHashMap_TryFind()
            {
                string value;
                _map.TryFind(_key, out value);
                return value;
            }

            [Benchmark]
            public object GetFromHashMap_TryFind_Inlined()
            {
                string value;
                _map.TryFind_Inlined(_key, out value);
                return value;
            }
        }
    }
}
