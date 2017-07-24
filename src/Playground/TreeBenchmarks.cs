using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using ImTools;

namespace Playground
{
    public static class TreeBenchmarks
    {
        [MemoryDiagnoser]
        public class TypeHashMapVsImHashMap
        {
            private readonly Type _key = typeof(TreeBenchmarks);
            private readonly string _value = "hey";

            private const int MaxTypeCount = 2000;

            [Params(10, 50, 1000, MaxTypeCount)]
            public int ItemCount;

            // use standard collection types as keys
            private readonly Type[] _keys = typeof(Dictionary<,>).Assembly.GetTypes().Take(MaxTypeCount).ToArray();

            private ImHashMap<Type, string> _imMap = ImHashMap<Type, string>.Empty;
            private readonly TypeHashMap<string> _map = new TypeHashMap<string>();

            [Benchmark]
            public void MeasurePopulateImHashMap()
            {
                var keys = _keys;
                for (var i = 0; i < ItemCount; i++)
                    Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(keys[i], "a"));
                Interlocked.Exchange(ref _imMap, _imMap.AddOrUpdate(_key, _value));
            }

            [Benchmark]
            public void MeasurePopulateHashMap()
            {
                var keys = _keys;
                for (var i = 0; i < ItemCount; i++)
                    _map.AddOrUpdate(keys[i], "a");
                _map.AddOrUpdate(_key, _value);
            }

            //private static long TrieGet<T>(HashTrie<Type, T> tree, Type key, int times)
            //{
            //    T ignored = default(T);

            //    var treeWatch = Stopwatch.StartNew();

            //    for (int i = 0; i < times; i++)
            //    {
            //        ignored = tree.GetValueOrDefault(key);
            //    }

            //    treeWatch.Stop();
            //    GC.KeepAlive(ignored);
            //    GC.Collect();
            //    return treeWatch.ElapsedMilliseconds;
            //}
        }
    }
}
