using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ImTools.Experimental
{
    /// <summary>More simple, compact and performant than <see cref="ImHashTree{K,V}"/> 
    /// immutable http://en.wikipedia.org/wiki/AVL_tree  with integer keys and object values.</summary>
    public sealed class ImMap<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap<V> Empty = new ImMap<V>();

        /// <summary>Key.</summary>
        public readonly int Key;

        /// <summary>Value.</summary>
        public readonly V Value;

        /// <summary>Left sub-tree/branch, or empty.</summary>
        public readonly ImMap<V> Left;

        /// <summary>Right sub-tree/branch, or empty.</summary>
        public readonly ImMap<V> Right;

        /// <summary>Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.</summary>
        public readonly int Height;

        /// <summary>Returns true is tree is empty.</summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplHints.AggressingInlining)]
            get { return Height == 0; }
        }

        /// <summary>Returns new tree with added or updated value for specified key.</summary>
        /// <param name="key"></param> <param name="value"></param>
        /// <returns>New tree.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImMap<V> AddOrUpdate(int key, V value)
        {
            return AddOrUpdateImpl(key, value);
        }

        /// <summary>Returns new tree with added or updated value for specified key.</summary>
        /// <param name="key">Key</param> <param name="value">Value</param>
        /// <param name="updateValue">(optional) Delegate to calculate new value from and old and a new value.</param>
        /// <returns>New tree.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImMap<V> AddOrUpdate(int key, V value, Update<V> updateValue)
        {
            return AddOrUpdateImpl(key, value, false, updateValue);
        }

        /// <summary>Returns new tree with updated value for the key, Or the same tree if key was not found.</summary>
        /// <param name="key"></param> <param name="value"></param>
        /// <returns>New tree if key is found, or the same tree otherwise.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImMap<V> Update(int key, V value)
        {
            return AddOrUpdateImpl(key, value, true, null);
        }

        /// <summary>Get value for found key or null otherwise.</summary>
        /// <param name="key"></param> <param name="defaultValue">(optional) Value to return if key is not found.</param>
        /// <returns>Found value or <paramref name="defaultValue"/>.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public V GetValueOrDefault(int key, V defaultValue = default(V))
        {
            var node = this;
            while (node.Height != 0 && node.Key != key)
                node = key < node.Key ? node.Left : node.Right;
            return node.Height != 0 ? node.Value : defaultValue;
        }

        /// <summary>Returns true if key is found and sets the value.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">Result value</param>
        /// <returns>True if key found, false otherwise.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public bool TryFind(int key, out V value)
        {
            var hash = key.GetHashCode();

            var node = this;
            while (node.Height != 0 && node.Key != key)
                node = hash < node.Key ? node.Left : node.Right;

            if (node.Height != 0)
            {
                value = node.Value;
                return true;
            }

            value = default(V);
            return false;
        }

        /// <summary>Returns all sub-trees enumerated from left to right.</summary> 
        /// <returns>Enumerated sub-trees or empty if tree is empty.</returns>
        public IEnumerable<ImMap<V>> Enumerate()
        {
            if (Height == 0)
                yield break;

            var parents = new ImMap<V>[Height];

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
                    yield return node;
                    node = node.Right;
                }
            }
        }

        /// <summary>Removes or updates value for specified key, or does nothing if key is not found.
        /// Based on Eric Lippert http://blogs.msdn.com/b/ericlippert/archive/2008/01/21/immutability-in-c-part-nine-academic-plus-my-avl-tree-implementation.aspx </summary>
        /// <param name="key">Key to look for.</param> 
        /// <returns>New tree with removed or updated value.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImMap<V> Remove(int key)
        {
            return RemoveImpl(key, false);
        }

        #region Implementation

        private ImMap() { }

        private ImMap(int key, V value)
        {
            Key = key;
            Value = value;
            Left = Empty;
            Right = Empty;
            Height = 1;
        }

        private ImMap(int key, V value, ImMap<V> left, ImMap<V> right, int height)
        {
            Key = key;
            Value = value;
            Left = left;
            Right = right;
            Height = height;
        }

        private ImMap(int key, V value, ImMap<V> left, ImMap<V> right)
        {
            Key = key;
            Value = value;
            Left = left;
            Right = right;
            Height = 1 + (left.Height > right.Height ? left.Height : right.Height);
        }

        [MethodImpl(MethodImplHints.AggressingInlining)]
        private ImMap<V> AddOrUpdateImpl(int key, V value)
        {
            return Height == 0  // add new node
                    ? new ImMap<V>(key, value)
                : (key == Key // update found node
                    ? new ImMap<V>(key, value, Left, Right)
                : (key < Key  // search for node
                    ? (Height == 1
                        ? new ImMap<V>(Key, Value, new ImMap<V>(key, value), Right, height: 2)
                        : new ImMap<V>(Key, Value, Left.AddOrUpdateImpl(key, value), Right).KeepBalance())
                    : (Height == 1
                        ? new ImMap<V>(Key, Value, Left, new ImMap<V>(key, value), height: 2)
                        : new ImMap<V>(Key, Value, Left, Right.AddOrUpdateImpl(key, value)).KeepBalance())));
        }

        [MethodImpl(MethodImplHints.AggressingInlining)]
        private ImMap<V> AddOrUpdateImpl(int key, V value, bool updateOnly, Update<V> update)
        {
            return Height == 0 ? // tree is empty
                (updateOnly ? this : new ImMap<V>(key, value))
                : (key == Key ? // actual update
                    new ImMap<V>(key, update == null ? value : update(Value, value), Left, Right)
                : (key < Key    // try update on left or right sub-tree
                    ? new ImMap<V>(Key, Value, Left.AddOrUpdateImpl(key, value, updateOnly, update), Right)
                    : new ImMap<V>(Key, Value, Left, Right.AddOrUpdateImpl(key, value, updateOnly, update)))
                    .KeepBalance());
        }

        private ImMap<V> KeepBalance()
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
                    return new ImMap<V>(leftRight.Key, leftRight.Value,
                        left: new ImMap<V>(left.Key, left.Value,
                    left: leftLeft, right: leftRight.Left), right: new ImMap<V>(Key, Value,
                                                         left: leftRight.Right, right: Right));
                }

                // todo: do we need this?
                // one rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                return new ImMap<V>(left.Key, left.Value,
                    left: leftLeft, right: new ImMap<V>(Key, Value,
                               left: leftRight, right: Right));
            }

            if (delta <= -2)
            {
                var right = Right;
                var rightLeft = right.Left;
                var rightRight = right.Right;
                if (rightLeft.Height - rightRight.Height == 1)
                {
                    return new ImMap<V>(rightLeft.Key, rightLeft.Value,
                        left: new ImMap<V>(Key, Value,
                            left: Left, right: rightLeft.Left), right: new ImMap<V>(right.Key, right.Value,
                                                    left: rightLeft.Right, right: rightRight));
                }

                return new ImMap<V>(right.Key, right.Value,
                    left: new ImMap<V>(Key, Value,
                        left: Left, right: rightLeft), right: rightRight);
            }

            return this;
        }

        private ImMap<V> RemoveImpl(int key, bool ignoreKey = false)
        {
            if (Height == 0)
                return this;

            ImMap<V> result;
            if (key == Key || ignoreKey) // found node
            {
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
                    result = new ImMap<V>(successor.Key, successor.Value,
                        Left, Right.RemoveImpl(successor.Key, ignoreKey: true));
                }
            }
            else if (key < Key)
                result = new ImMap<V>(Key, Value, Left.RemoveImpl(key, ignoreKey), Right);
            else
                result = new ImMap<V>(Key, Value, Left, Right.RemoveImpl(key, ignoreKey));

            return result.KeepBalance();
        }


        #endregion
    }

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
            [MethodImpl(MethodImplHints.AggressingInlining)]
            get { return _data.Conflicts; }
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
            while (t.Height != 0 && t._data.Hash != hash)
                t = hash < t._data.Hash ? t.Left : t.Right;

            if (t.Height != 0 && (ReferenceEquals(key, t._data.Key) || key.Equals(t._data.Key)))
            {
                value = t._data.Value;
                return true;
            }

            return t.TryFindConflictedValue(key, out value);
        }

        [MethodImpl(MethodImplHints.AggressingInlining)]
        internal bool TryFind(int hash, K key, out V value)
        {
            var t = this;
            while (t.Height != 0 && t._data.Hash != hash)
                t = hash < t._data.Hash ? t.Left : t.Right;

            if (t.Height != 0 && (ReferenceEquals(key, t._data.Key) || key.Equals(t._data.Key)))
            {
                value = t._data.Value;
                return true;
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

        private sealed class Data
        {
            public readonly int Hash;
            public readonly K Key;
            public readonly V Value;

            public readonly KV<K, V>[] Conflicts;

            public Data() { }

            public Data(int hash, K key, V value, KV<K, V>[] conflicts = null)
            {
                Hash = hash;
                Key = key;
                Value = value;
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

        [MethodImpl(MethodImplHints.AggressingInlining)]
        internal ImHashTree<K, V> AddOrUpdate(int hash, K key, V value)
        {
            return Height == 0  // add new node
                ? new ImHashTree<K, V>(new Data(hash, key, value))
                : (hash == Hash // update found node
                    ? (ReferenceEquals(Key, key) || Key.Equals(key)
                        ? new ImHashTree<K, V>(new Data(hash, key, value, Conflicts), Left, Right)
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
                        ? new ImHashTree<K, V>(new Data(hash, key, update(Value, value), Conflicts), Left, Right)
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
                        ? new ImHashTree<K, V>(new Data(hash, key, update == null ? value : update(Value, value), Conflicts), Left, Right)
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
                    : new ImHashTree<K, V>(new Data(Hash, Key, Value, new[] { new KV<K, V>(key, value) }), Left, Right);

            var found = Conflicts.Length - 1;
            while (found >= 0 && !Equals(Conflicts[found].Key, Key)) --found;
            if (found == -1)
            {
                if (updateOnly) return this;
                var newConflicts = new KV<K, V>[Conflicts.Length + 1];
                Array.Copy(Conflicts, 0, newConflicts, 0, Conflicts.Length);
                newConflicts[Conflicts.Length] = new KV<K, V>(key, value);
                return new ImHashTree<K, V>(new Data(Hash, Key, Value, newConflicts), Left, Right);
            }

            var conflicts = new KV<K, V>[Conflicts.Length];
            Array.Copy(Conflicts, 0, conflicts, 0, Conflicts.Length);
            conflicts[found] = new KV<K, V>(key, update == null ? value : update(Conflicts[found].Value, value));
            return new ImHashTree<K, V>(new Data(Hash, Key, Value, conflicts), Left, Right);
        }

        private V GetConflictedValueOrDefault(K key, V defaultValue)
        {
            if (Conflicts != null)
                for (var i = Conflicts.Length - 1; i >= 0; --i)
                    if (Equals(Conflicts[i].Key, key))
                        return Conflicts[i].Value;
            return defaultValue;
        }

        private bool TryFindConflictedValue(K key, out V value)
        {
            if (Height != 0 && Conflicts != null)
                for (var i = Conflicts.Length - 1; i >= 0; --i)
                    if (Equals(Conflicts[i].Key, key))
                    {
                        value = Conflicts[i].Value;
                        return true;
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

        private ImHashTree<K, V> With(ImHashTree<K, V> left, ImHashTree<K, V> right)
        {
            return left == Left && right == Right ? this : new ImHashTree<K, V>(_data, left, right);
        }

        internal ImHashTree<K, V> Remove(int hash, K key, bool ignoreKey = false)
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
                return new ImHashTree<K, V>(new Data(Hash, Key, Value, null), Left, Right);
            var shrinkedConflicts = new KV<K, V>[Conflicts.Length - 1];
            var newIndex = 0;
            for (var i = 0; i < Conflicts.Length; ++i)
                if (i != index) shrinkedConflicts[newIndex++] = Conflicts[i];
            return new ImHashTree<K, V>(new Data(Hash, Key, Value, shrinkedConflicts), Left, Right);
        }

        private ImHashTree<K, V> ReplaceRemovedWithConflicted()
        {
            if (Conflicts.Length == 1)
                return new ImHashTree<K, V>(new Data(Hash, Conflicts[0].Key, Conflicts[0].Value, null), Left, Right);
            var shrinkedConflicts = new KV<K, V>[Conflicts.Length - 1];
            Array.Copy(Conflicts, 1, shrinkedConflicts, 0, shrinkedConflicts.Length);
            return new ImHashTree<K, V>(new Data(Hash, Conflicts[0].Key, Conflicts[0].Value, shrinkedConflicts), Left, Right);
        }

        #endregion
    }

    public sealed class ImMap<K, V>
    {
        private const int NumberOfTrees = 8;
        private const int HashBitsToTree = NumberOfTrees - 1;  // get last 4 bits, fast (hash % NumberOfTrees)

        public static readonly ImMap<K, V> Empty = new ImMap<K, V>(new ImHashTree<K, V>[NumberOfTrees], 0);

        public readonly int Count;

        public bool IsEmpty { get { return Count == 0; } }

        /// <summary>Returns true if key is found and sets the value.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">Result value</param>
        /// <returns>True if key found, false otherwise.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public bool TryFind(K key, out V value)
        {
            var hash = key.GetHashCode();

            var treeIndex = hash & HashBitsToTree;

            var tree = _trees[treeIndex];
            if (tree != null)
                return tree.TryFind(hash, key, out value);

            value = default(V);
            return false;
        }

        /// <summary>Returns new tree with added key-value. 
        /// If value with the same key is exist then the value is replaced.</summary>
        /// <param name="key">Key to add.</param><param name="value">Value to add.</param>
        /// <returns>New tree with added or updated key-value.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImMap<K, V> AddOrUpdate(K key, V value)
        {
            var hash = key.GetHashCode();

            var treeIndex = hash & HashBitsToTree;

            var trees = _trees;
            var tree = trees[treeIndex];
            if (tree == null)
                tree = ImHashTree<K, V>.Empty;

            tree = tree.AddOrUpdate(hash, key, value);

            var newTrees = new ImHashTree<K, V>[NumberOfTrees];
            Array.Copy(trees, 0, newTrees, 0, NumberOfTrees);
            newTrees[treeIndex] = tree;

            return new ImMap<K, V>(newTrees, Count + 1);
        }

        /// <summary>Removes or updates value for specified key, or does nothing if key is not found.
        /// Based on Eric Lippert http://blogs.msdn.com/b/ericlippert/archive/2008/01/21/immutability-in-c-part-nine-academic-plus-my-avl-tree-implementation.aspx </summary>
        /// <param name="key">Key to look for.</param> 
        /// <returns>New tree with removed or updated value.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public ImMap<K, V> Remove(K key)
        {
            var hash = key.GetHashCode();

            var treeIndex = hash & NumberOfTrees;

            var trees = _trees;
            var tree = trees[treeIndex];
            if (tree == null)
                return this; // nothing to delete

            var newTree = tree.Remove(hash, key);
            if (newTree == tree)
                return this;

            var newTrees = new ImHashTree<K, V>[NumberOfTrees];
            Array.Copy(trees, 0, newTrees, 0, NumberOfTrees);
            newTrees[treeIndex] = newTree;

            return new ImMap<K, V>(newTrees, Count - 1);
        }

        private readonly ImHashTree<K, V>[] _trees;

        private ImMap(ImHashTree<K, V>[] newTrees, int count)
        {
            _trees = newTrees;
            Count = count;
        }
    }
}
