﻿using System;
using System.Collections.Generic;

namespace ImTools.UnitTests.Playground
{
    public delegate T UpdateMethod<T>(T old, T newOne);

    /// <summary>
    /// Immutable Hash Array Mapped Trie (http://en.wikipedia.org/wiki/Hash_array_mapped_trie)
    /// similar to the one described at http://lampwww.epfl.ch/papers/idealhashtrees.pdf.
    /// It is basically a http://en.wikipedia.org/wiki/Trie built on hash chunks. It provides O(1) access-time and
    /// does not require self-balancing. The maximum number of tree levels would be (32 bits of hash / 5 bits level chunk = 7).
    /// In addition it is space efficient and requires single integer (to store index bitmap) per 1 to 32 values.
    /// TODO: ? Optimize get/add speed with mutable sparse array (for insert) at root level. That safe cause bitmapIndex will Not see new inserted values.
    /// </summary>
    /// <typeparam name="V">Type of value stored in trie.</typeparam>
    public sealed class HashTrie<V>
    {
        public static readonly HashTrie<V> Empty = new HashTrie<V>();

        public bool IsEmpty => _indexBitmap == 0;

        public HashTrie<V> AddOrUpdate(int hash, V value, UpdateMethod<V> updateValue = null)
        {
            var index = hash & LEVEL_MASK; // index from 0 to 31
            var restOfHash = hash >> LEVEL_BITS;
            if (_indexBitmap == 0)
                return new HashTrie<V>(1u << index, restOfHash == 0 ? (object)value : Empty.AddOrUpdate(restOfHash, value));

            var nodeCount = _nodes.Length;

            var pastIndexBitmap = _indexBitmap >> index;
            if ((pastIndexBitmap & 1) == 0) // no nodes at the index, could be inserted.
            {
                var subnode = restOfHash == 0 ? (object)value : Empty.AddOrUpdate(restOfHash, value);

                var pastIndexCount = pastIndexBitmap == 0 ? 0 : GetSetBitsCount(pastIndexBitmap);
                var insertIndex = nodeCount - pastIndexCount;

                var nodesToInsert = new object[nodeCount + 1];
                if (insertIndex != 0)
                    Array.Copy(_nodes, 0, nodesToInsert, 0, insertIndex);
                nodesToInsert[insertIndex] = subnode;
                if (pastIndexCount != 0)
                    Array.Copy(_nodes, insertIndex, nodesToInsert, insertIndex + 1, pastIndexCount);

                return new HashTrie<V>(_indexBitmap | (1u << index), nodesToInsert);
            }

            var updateIndex = nodeCount == 1 ? 0
                : nodeCount - (pastIndexBitmap == 1 ? 1 : GetSetBitsCount(pastIndexBitmap));

            var updatedNode = _nodes[updateIndex];
            if (updatedNode is HashTrie<V>)
                updatedNode = ((HashTrie<V>)updatedNode).AddOrUpdate(restOfHash, value, updateValue);
            else if (restOfHash != 0) // if we need to update value with node we will move value down to new node sub-nodes at index 0. 
                updatedNode = new HashTrie<V>(1u, updatedNode).AddOrUpdate(restOfHash, value, updateValue);
            else // here the actual update should go, cause old and new nodes contain values.
                updatedNode = updateValue == null ? value : updateValue((V)updatedNode, value);

            var nodesToUpdate = new object[nodeCount];
            if (nodesToUpdate.Length > 1)
                Array.Copy(_nodes, 0, nodesToUpdate, 0, nodesToUpdate.Length);
            nodesToUpdate[updateIndex] = updatedNode;

            return new HashTrie<V>(_indexBitmap, nodesToUpdate);
        }

        public V GetValueOrDefault(int hash, V defaultValue = default(V))
        {
            var node = this;
            var pastIndexBitmap = node._indexBitmap >> (hash & LEVEL_MASK);
            while ((pastIndexBitmap & 1) == 1)
            {
                var subnode = node._nodes[
                    node._nodes.Length - (pastIndexBitmap == 1 ? 1 : GetSetBitsCount(pastIndexBitmap))];

                hash >>= LEVEL_BITS;
                if (!(subnode is HashTrie<V>)) // reached the leaf value node
                    return hash == 0 ? (V)subnode : defaultValue;

                node = (HashTrie<V>)subnode;
                pastIndexBitmap = node._indexBitmap >> (hash & LEVEL_MASK);
            }

            return defaultValue;
        }

        public IEnumerable<V> Enumerate()
        {
            for (var i = 0; i < _nodes.Length; --i)
            {
                var n = _nodes[i];
                if (n is HashTrie<V> trie)
                    foreach (var subnode in trie.Enumerate())
                        yield return subnode;
                else
                    yield return (V)n;
            }
        }

        #region Implementation

        private const int LEVEL_MASK = 31;  // Hash mask to find hash part on each trie level.
        private const int LEVEL_BITS = 5;   // Number of bits from hash corresponding to one level.

        private readonly object[] _nodes;   // Up to 32 nodes: sub nodes or values.
        private readonly uint _indexBitmap; // Bits indicating nodes at what index are in use.

        private HashTrie() { }

        private HashTrie(uint indexBitmap, params object[] nodes)
        {
            _nodes = nodes;
            _indexBitmap = indexBitmap;
        }

        // Variable-precision SWAR algorithm http://playingwithpointers.com/swar.html
        // Fastest compared to the rest (but did not check pre-computed WORD counts): http://gurmeet.net/puzzles/fast-bit-counting-routines/
        private static uint GetSetBitsCount(uint n)
        {
            n = n - ((n >> 1) & 0x55555555);
            n = (n & 0x33333333) + ((n >> 2) & 0x33333333);
            return (((n + (n >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        #endregion
    }

    public sealed class HashTrie<K, V>
    {
        public static readonly HashTrie<K, V> Empty = new HashTrie<K, V>(HashTrie<KV<K, V>>.Empty, null);

        public HashTrie<K, V> AddOrUpdate(K key, V value)
        {
            return new HashTrie<K, V>(
                _root.AddOrUpdate(key.GetHashCode(), new KV<K, V>(key, value), UpdateConflicts),
                _updateValue);
        }

        public V GetValueOrDefault(K key, V defaultValue = default(V))
        {
            var kv = _root.GetValueOrDefault(key.GetHashCode());
            return kv != null && (ReferenceEquals(key, kv.Key) || key.Equals(kv.Key)) 
                ? kv.Value : GetConflictedOrDefault(kv, key, defaultValue);
        }

        public IEnumerable<KV<K, V>> Enumerate()
        {
            foreach (var kv in _root.Enumerate())
            {
                yield return kv;
                if (kv is KVWithConflicts conflicts)
                    foreach (var conflict in conflicts.Conflicts)
                        yield return conflict;
            }
        }

        #region Implementation

        private readonly HashTrie<KV<K, V>> _root;
        private readonly UpdateMethod<V> _updateValue;

        private HashTrie(HashTrie<KV<K, V>> root, UpdateMethod<V> updateValue)
        {
            _root = root;
            _updateValue = updateValue;
        }

        private KV<K, V> UpdateConflicts(KV<K, V> old, KV<K, V> newOne)
        {
            var conflicts = old is KVWithConflicts withConflicts ? withConflicts.Conflicts : null;
            if (ReferenceEquals(old.Key, newOne.Key) || old.Key.Equals(newOne.Key))
                return conflicts == null
                    ? UpdateValue(old, newOne)
                    : new KVWithConflicts(UpdateValue(old, newOne), conflicts);

            if (conflicts == null)
                return new KVWithConflicts(old, new[] { newOne });

            var i = conflicts.Length - 1;
            while (i >= 0 && !Equals(conflicts[i].Key, newOne.Key)) --i;
            if (i != -1) newOne = UpdateValue(old, newOne);
            return new KVWithConflicts(old, conflicts.AppendOrUpdate(newOne, i));
        }

        private KV<K, V> UpdateValue(KV<K, V> existing, KV<K, V> added)
        {
            return _updateValue == null
                ? added
                : new KV<K, V>(existing.Key, _updateValue(existing.Value, added.Value));
        }

        private static V GetConflictedOrDefault(KV<K, V> item, K key, V defaultValue = default(V))
        {
            var conflicts = item is KVWithConflicts ? ((KVWithConflicts)item).Conflicts : null;
            if (conflicts != null)
                for (var i = 0; i < conflicts.Length; i++)
                    if (Equals(conflicts[i].Key, key))
                        return conflicts[i].Value;
            return defaultValue;
        }

        private sealed class KVWithConflicts : KV<K, V>
        {
            public readonly KV<K, V>[] Conflicts;

            public KVWithConflicts(KV<K, V> kv, KV<K, V>[] conflicts)
                : base(kv.Key, kv.Value)
            {
                Conflicts = conflicts;
            }
        }

        #endregion
    }
}