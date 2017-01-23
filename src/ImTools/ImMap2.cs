using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ImTools.Experimental3
{
    /// <summary>Immutable http://en.wikipedia.org/wiki/AVL_tree 
    /// where node key is the hash code of <typeparamref name="K"/>.</summary>
    public sealed class ImHashTree<K, V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImHashTree<K, V> Empty = new ImHashTree<K, V>();

        /// <summary>Calculated key hash.</summary>
        public int Hash
        {
            [MethodImpl(MethodImplHints.AggressingInlining)]
            get { return _data.Hash; }
        }

        /// <summary>Key of type K that should support <see cref="object.Equals(object)"/> and <see cref="object.GetHashCode"/>.</summary>
        public K Key
        {
            [MethodImpl(MethodImplHints.AggressingInlining)]
            get { return _data.Key; }
        }

        /// <summary>Value of any type V.</summary>
        public V Value
        {
            [MethodImpl(MethodImplHints.AggressingInlining)]
            get { return _data.Value; }
        }

        /// <summary>In case of <see cref="Hash"/> conflicts for different keys contains conflicted keys with their values.</summary>
        public KV<K, V>[] Conflicts
        {
            [MethodImpl(MethodImplHints.AggressingInlining)] get
            {
                var data = _data as DataWithConflicts;
                return data == null ? null : data.Conflicts;
            }
        }

        /// <summary>Left sub-tree/branch, or empty.</summary>
        public readonly ImHashTree<K, V> Left;

        /// <summary>Right sub-tree/branch, or empty.</summary>
        public readonly ImHashTree<K, V> Right;

        /// <summary>Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.</summary>
        public readonly int Height;

        /// <summary>Returns true if tree is empty.</summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplHints.AggressingInlining)]
            get { return Height == 0; }
        }

        /// <summary>Returns new tree with added key-value. 
        /// If value with the same key is exist then the value is replaced.</summary>
        /// <param name="key">Key to add.</param><param name="value">Value to add.</param>
        /// <returns>New tree with added or updated key-value.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImHashTree<K, V> AddOrUpdate(K key, V value)
        {
            return AddOrUpdate(key.GetHashCode(), key, value);
        }

        /// <summary>Returns new tree with added key-value. If value with the same key is exist, then
        /// if <paramref name="update"/> is not specified: then existing value will be replaced by <paramref name="value"/>;
        /// if <paramref name="update"/> is specified: then update delegate will decide what value to keep.</summary>
        /// <param name="key">Key to add.</param><param name="value">Value to add.</param>
        /// <param name="update">Update handler.</param>
        /// <returns>New tree with added or updated key-value.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImHashTree<K, V> AddOrUpdate(K key, V value, Update<V> update)
        {
            return AddOrUpdate(key.GetHashCode(), key, value, update);
        }

        /// <summary>Looks for <paramref name="key"/> and replaces its value with new <paramref name="value"/>, or 
        /// runs custom update handler (<paramref name="update"/>) with old and new value to get the updated result.</summary>
        /// <param name="key">Key to look for.</param>
        /// <param name="value">New value to replace key value with.</param>
        /// <param name="update">(optional) Delegate for custom update logic, it gets old and new <paramref name="value"/>
        /// as inputs and should return updated value as output.</param>
        /// <returns>New tree with updated value or the SAME tree if no key found.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImHashTree<K, V> Update(K key, V value, Update<V> update = null)
        {
            return Update(key.GetHashCode(), key, value, update);
        }

        /// <summary>Looks for key in a tree and returns the key value if found, or <paramref name="defaultValue"/> otherwise.</summary>
        /// <param name="key">Key to look for.</param> <param name="defaultValue">(optional) Value to return if key is not found.</param>
        /// <returns>Found value or <paramref name="defaultValue"/>.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public V GetValueOrDefault(K key, V defaultValue = default(V))
        {
            var t = this;
            var hash = key.GetHashCode();
            while (t.Height != 0 && t.Hash != hash)
                t = hash < t.Hash ? t.Left : t.Right;
            return t.Height != 0 && (ReferenceEquals(key, t.Key) || key.Equals(t.Key))
                ? t.Value : t.GetConflictedValueOrDefault(key, defaultValue);
        }

        /// <summary>Returns true if key is found and sets the value.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">Result value</param>
        /// <returns>True if key found, false otherwise.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public bool TryFind(K key, out V value)
        {
            var hash = key.GetHashCode();

            var t = this;
            while (t.Height != 0)
            {
                var data = t._data;
                if (data.Hash == hash && key.Equals(data.Key))
                {
                    value = data.Value;
                    return true;
                }
                if (data.Hash > hash)
                    t = t.Left;
                else
                    t = t.Right;
            }

            return t.TryFindConflictedValue(key, out value);
        }

        /// <summary>Depth-first in-order traversal as described in http://en.wikipedia.org/wiki/Tree_traversal
        /// The only difference is using fixed size array instead of stack for speed-up (~20% faster than stack).</summary>
        /// <returns>Sequence of enumerated key value pairs.</returns>
        public IEnumerable<KV<K, V>> Enumerate()
        {
            if (Height == 0)
                yield break;

            var parents = new ImHashTree<K, V>[Height];

            var node = this;
            var parentCount = -1;
            while (node.Height != 0 || parentCount != -1)
            {
                if (node.Height != 0)
                {
                    parents[++parentCount] = node;
                    node = node.Left;
                }
                else
                {
                    node = parents[parentCount--];
                    yield return new KV<K, V>(node.Key, node.Value);

                    if (node.Conflicts != null)
                        for (var i = 0; i < node.Conflicts.Length; i++)
                            yield return node.Conflicts[i];

                    node = node.Right;
                }
            }
        }

        /// <summary>Removes or updates value for specified key, or does nothing if key is not found.
        /// Based on Eric Lippert http://blogs.msdn.com/b/ericlippert/archive/2008/01/21/immutability-in-c-part-nine-academic-plus-my-avl-tree-implementation.aspx </summary>
        /// <param name="key">Key to look for.</param> 
        /// <returns>New tree with removed or updated value.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImHashTree<K, V> Remove(K key)
        {
            return Remove(key.GetHashCode(), key);
        }

        #region Implementation

        private class Data
        {
            public readonly int Hash;
            public readonly K Key;
            public readonly V Value;

            public Data() { }

            public Data(int hash, K key, V value)
            {
                Hash = hash;
                Key = key;
                Value = value;
            }
        }

        private sealed class DataWithConflicts : Data
        {
            public readonly KV<K, V>[] Conflicts;

            public DataWithConflicts(int hash, K key, V value, KV<K, V>[] conflicts)
                : base(hash, key, value)
            {
                Conflicts = conflicts;
            }
        }

        private readonly Data _data;

        private ImHashTree() { _data = new Data(); }

        private ImHashTree(Data data)
        {
            _data = data;
            Left = Empty;
            Right = Empty;
            Height = 1;
        }

        private ImHashTree(Data data, ImHashTree<K, V> left, ImHashTree<K, V> right)
        {
            _data = data;
            Left = left;
            Right = right;
            Height = 1 + (left.Height > right.Height ? left.Height : right.Height);
        }

        private ImHashTree(Data data, ImHashTree<K, V> left, ImHashTree<K, V> right, int height)
        {
            _data = data;
            Left = left;
            Right = right;
            Height = height;
        }

        private static Data NewData(int hash, K key, V value, KV<K, V>[] conflicts)
        {
            return conflicts == null
                ? new Data(hash, key, value)
                : new DataWithConflicts(hash, key, value, conflicts);
        }

        private ImHashTree<K, V> AddOrUpdate(int hash, K key, V value)
        {
            return Height == 0  // add new node
                ? new ImHashTree<K, V>(new Data(hash, key, value))
                : (hash == Hash // update found node
                    ? (ReferenceEquals(Key, key) || Key.Equals(key)
                        ? new ImHashTree<K, V>(NewData(hash, key, value, Conflicts), Left, Right)
                        : UpdateValueAndResolveConflicts(key, value, null, false))
                : (hash < Hash  // search for node
                    ? (Height == 1
                        ? new ImHashTree<K, V>(_data,
                            new ImHashTree<K, V>(new Data(hash, key, value)), Right, height: 2)
                        : new ImHashTree<K, V>(_data,
                            Left.AddOrUpdate(hash, key, value), Right).KeepBalance())
                    : (Height == 1
                        ? new ImHashTree<K, V>(_data,
                            Left, new ImHashTree<K, V>(new Data(hash, key, value)), height: 2)
                        : new ImHashTree<K, V>(_data,
                            Left, Right.AddOrUpdate(hash, key, value)).KeepBalance())));
        }

        private ImHashTree<K, V> AddOrUpdate(int hash, K key, V value, Update<V> update)
        {
            return Height == 0
                    ? new ImHashTree<K, V>(new Data(hash, key, value))
                : (hash == Hash // update
                    ? (ReferenceEquals(Key, key) || Key.Equals(key)
                        ? new ImHashTree<K, V>(NewData(hash, key, update(Value, value), Conflicts), Left, Right)
                        : UpdateValueAndResolveConflicts(key, value, update, false))
                : (hash < Hash
                    ? With(Left.AddOrUpdate(hash, key, value, update), Right)
                    : With(Left, Right.AddOrUpdate(hash, key, value, update)))
                    .KeepBalance());
        }

        private ImHashTree<K, V> Update(int hash, K key, V value, Update<V> update)
        {
            return Height == 0 ? this
                : (hash == Hash
                    ? (ReferenceEquals(Key, key) || Key.Equals(key)
                        ? new ImHashTree<K, V>(
                            NewData(hash, key, update == null ? value : update(Value, value), Conflicts), 
                            Left, Right)
                        : UpdateValueAndResolveConflicts(key, value, update, true))
                    : (hash < Hash
                        ? With(Left.Update(hash, key, value, update), Right)
                        : With(Left, Right.Update(hash, key, value, update)))
                    .KeepBalance());
        }

        private ImHashTree<K, V> UpdateValueAndResolveConflicts(K key, V value, Update<V> update, bool updateOnly)
        {
            if (Conflicts == null) // add only if updateOnly is false.
                return updateOnly ? this
                    : new ImHashTree<K, V>(
                        new DataWithConflicts(Hash, Key, Value, new[] { new KV<K, V>(key, value) }), 
                        Left, Right);

            var found = Conflicts.Length - 1;
            while (found >= 0 && !Equals(Conflicts[found].Key, Key)) --found;
            if (found == -1)
            {
                if (updateOnly) return this;
                var newConflicts = new KV<K, V>[Conflicts.Length + 1];
                Array.Copy(Conflicts, 0, newConflicts, 0, Conflicts.Length);
                newConflicts[Conflicts.Length] = new KV<K, V>(key, value);
                return new ImHashTree<K, V>(
                    new DataWithConflicts(Hash, Key, Value, newConflicts), 
                    Left, Right);
            }

            var conflicts = new KV<K, V>[Conflicts.Length];
            Array.Copy(Conflicts, 0, conflicts, 0, Conflicts.Length);
            conflicts[found] = new KV<K, V>(key, update == null ? value : update(Conflicts[found].Value, value));
            return new ImHashTree<K, V>(
                new DataWithConflicts(Hash, Key, Value, conflicts), Left, Right);
        }

        private V GetConflictedValueOrDefault(K key, V defaultValue)
        {
            var conflicts = Conflicts;
            if (conflicts != null)
                for (var i = conflicts.Length - 1; i >= 0; --i)
                    if (Equals(conflicts[i].Key, key))
                        return conflicts[i].Value;
            return defaultValue;
        }

        private bool TryFindConflictedValue(K key, out V value)
        {
            if (Height != 0)
            {
                var conflicts = Conflicts;
                if (conflicts != null)
                    for (var i = conflicts.Length - 1; i >= 0; --i)
                        if (Equals(conflicts[i].Key, key))
                        {
                            value = conflicts[i].Value;
                            return true;
                        }
            }

            value = default(V);
            return false;
        }

        private ImHashTree<K, V> KeepBalance()
        {
            var delta = Left.Height - Right.Height;
            if (delta >= 2) // left is longer by 2, rotate left
            {
                var left = Left;
                var leftLeft = left.Left;
                var leftRight = left.Right;
                if (leftRight.Height - leftLeft.Height == 1)
                {
                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1
                    return new ImHashTree<K, V>(leftRight._data,
                        left: new ImHashTree<K, V>(left._data,
                    left: leftLeft, right: leftRight.Left), right: new ImHashTree<K, V>(_data,
                                                         left: leftRight.Right, right: Right));
                }

                // todo: do we need this?
                // one rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                return new ImHashTree<K, V>(left._data,
                    left: leftLeft, right: new ImHashTree<K, V>(_data,
                               left: leftRight, right: Right));
            }

            if (delta <= -2)
            {
                var right = Right;
                var rightLeft = right.Left;
                var rightRight = right.Right;
                if (rightLeft.Height - rightRight.Height == 1)
                {
                    return new ImHashTree<K, V>(rightLeft._data,
                        left: new ImHashTree<K, V>(_data,
                            left: Left, right: rightLeft.Left), right: new ImHashTree<K, V>(right._data,
                                                    left: rightLeft.Right, right: rightRight));
                }

                return new ImHashTree<K, V>(right._data,
                    left: new ImHashTree<K, V>(_data,
                        left: Left, right: rightLeft), right: rightRight);
            }

            return this;
        }

        // 3              5
        //    5    =>   3   6
        //   4 6         4

        private ImHashTree<K, V> With(ImHashTree<K, V> left, ImHashTree<K, V> right)
        {
            return left == Left && right == Right ? this : new ImHashTree<K, V>(_data, left, right);
        }

        private ImHashTree<K, V> Remove(int hash, K key, bool ignoreKey = false)
        {
            if (Height == 0)
                return this;

            ImHashTree<K, V> result;
            if (hash == Hash) // found node
            {
                if (ignoreKey || Equals(Key, key))
                {
                    if (!ignoreKey && Conflicts != null)
                        return ReplaceRemovedWithConflicted();

                    if (Height == 1) // remove node
                        return Empty;

                    if (Right.IsEmpty)
                        result = Left;
                    else if (Left.IsEmpty)
                        result = Right;
                    else
                    {
                        // we have two children, so remove the next highest node and replace this node with it.
                        var successor = Right;
                        while (!successor.Left.IsEmpty) successor = successor.Left;
                        result = new ImHashTree<K, V>(successor._data,
                            Left, Right.Remove(successor.Hash, default(K), ignoreKey: true));
                    }
                }
                else if (Conflicts != null)
                    return TryRemoveConflicted(key);
                else
                    return this; // if key is not matching and no conflicts to lookup - just return
            }
            else if (hash < Hash)
                result = new ImHashTree<K, V>(_data, Left.Remove(hash, key, ignoreKey), Right);
            else
                result = new ImHashTree<K, V>(_data, Left, Right.Remove(hash, key, ignoreKey));

            if (result.Height == 1)
                return result;

            return result.KeepBalance();
        }

        private ImHashTree<K, V> TryRemoveConflicted(K key)
        {
            var index = Conflicts.Length - 1;
            while (index >= 0 && !Equals(Conflicts[index].Key, key)) --index;
            if (index == -1) // key is not found in conflicts - just return
                return this;

            if (Conflicts.Length == 1)
                return new ImHashTree<K, V>(new Data(Hash, Key, Value), Left, Right);
            var shrinkedConflicts = new KV<K, V>[Conflicts.Length - 1];
            var newIndex = 0;
            for (var i = 0; i < Conflicts.Length; ++i)
                if (i != index) shrinkedConflicts[newIndex++] = Conflicts[i];
            return new ImHashTree<K, V>(
                new DataWithConflicts(Hash, Key, Value, shrinkedConflicts), Left, Right);
        }

        private ImHashTree<K, V> ReplaceRemovedWithConflicted()
        {
            if (Conflicts.Length == 1)
                return new ImHashTree<K, V>(
                    new Data(Hash, Conflicts[0].Key, Conflicts[0].Value), Left, Right);
            var shrinkedConflicts = new KV<K, V>[Conflicts.Length - 1];
            Array.Copy(Conflicts, 1, shrinkedConflicts, 0, shrinkedConflicts.Length);
            return new ImHashTree<K, V>(
                new DataWithConflicts(Hash, Conflicts[0].Key, Conflicts[0].Value, shrinkedConflicts), 
                Left, Right);
        }

        #endregion
    }

}
