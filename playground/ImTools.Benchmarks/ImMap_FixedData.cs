using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ImTools.Benchmarks.ImMapFixedData
{
    public sealed class ImMapData<V>
    {
        /// The Key - basically the hash
        public readonly int Key;

        /// Value - can be modified if needed
        public V Value;

        public ImMapData(int key, V value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>Immutable http://en.wikipedia.org/wiki/AVL_tree with integer keys and <typeparamref name="V"/> values.</summary>
    public sealed class ImMap<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap<V> Empty = new ImMap<V>();

        /// The fixed data - will stay in memory and won't be deconstructed by the map operations
        public readonly ImMapData<V> Data;

        /// The key
        public int Key
        {
            [MethodImpl((MethodImplOptions)256)]
            get => Data.Key;
        }

        /// The value
        public V Value
        {
            [MethodImpl((MethodImplOptions)256)]
            get => Data.Value;
        }

        /// <summary>Left sub-tree/branch, or empty.</summary>
        public readonly ImMap<V> Left;

        /// <summary>Right sub-tree/branch, or empty.</summary>
        public readonly ImMap<V> Right;

        /// <summary>Height of the longest sub-tree/branch. It is 0 for empty tree, and 1 for single node tree.</summary>
        public readonly int Height;

        /// <summary>Returns true if tree is empty.</summary>
        public bool IsEmpty => Height == 0;

        /// Adds or updates the value by key in the map, always returns a modified map
        [MethodImpl((MethodImplOptions)256)]
        public ImMap<V> AddOrUpdate(int key, V value) =>
            Height == 0
                ? new ImMap<V>(key, value)
                : key == Key
                    ? new ImMap<V>(new ImMapData<V>(key, value), Left, Right, Height)
                    : AddOrUpdateImpl(key, value);

        private ImMap<V> AddOrUpdateImpl(int key, V value)
        {
            if (key < Key)
            {
                if (Left.Height == 0)
                    return new ImMap<V>(Data, new ImMap<V>(key, value), Right, 2);

                if (Left.Key == key)
                {
                    var left = Left;
                    return new ImMap<V>(Data, new ImMap<V>(new ImMapData<V>(key, value), left.Left, left.Right, left.Height), Right, Height);
                }

                if (Right.Height == 0)
                    return key < Left.Key
                        ? new ImMap<V>(Left.Data, new ImMap<V>(key, value), new ImMap<V>(Data), 2)
                        : new ImMap<V>(new ImMapData<V>(key, value), new ImMap<V>(Left.Data), new ImMap<V>(Data), 2);

                var newLeft = Left.AddOrUpdateImpl(key, value);

                if (newLeft.Height > Right.Height + 1) // left is longer by 2, rotate left
                {
                    var leftLeft = newLeft.Left;
                    var leftRight = newLeft.Right;

                    if (leftLeft.Height >= leftRight.Height)
                        return new ImMap<V>(newLeft.Data, leftLeft, new ImMap<V>(Data, leftRight, Right));

                    return new ImMap<V>(leftRight.Data,
                        new ImMap<V>(newLeft.Data, leftLeft, leftRight.Left),
                        new ImMap<V>(Data, leftRight.Right, Right));
                }

                return new ImMap<V>(Data, newLeft, Right);
            }
            else
            {
                if (Right.Height == 0)
                    return new ImMap<V>(Data, Left, new ImMap<V>(key, value), 2);

                if (Right.Key == key)
                {
                    var right = Right;
                    return new ImMap<V>(Data, Left, new ImMap<V>(new ImMapData<V>(key, value), right.Left, right.Right, right.Height), Height);
                }

                if (Left.Height == 0)
                {
                    return key >= Right.Key
                        ? new ImMap<V>(Right.Data, new ImMap<V>(Data), new ImMap<V>(key, value), 2)
                        : new ImMap<V>(new ImMapData<V>(key, value), new ImMap<V>(Data), new ImMap<V>(Right.Data), 2);
                }

                var newRight = Right.AddOrUpdateImpl(key, value);

                if (newRight.Height > Left.Height + 1)
                {
                    var rightLeft = newRight.Left;
                    var rightRight = newRight.Right;

                    return rightRight.Height >= rightLeft.Height
                        ? new ImMap<V>(newRight.Data, new ImMap<V>(Data, Left, rightLeft), rightRight)
                        : new ImMap<V>(rightLeft.Data,
                            new ImMap<V>(Data, Left, rightLeft.Left),
                            new ImMap<V>(newRight.Data, rightLeft.Right, rightRight));
                }

                return new ImMap<V>(Data, Left, newRight);
            }
        }

        /// Returns a new map with added value for the specified key or the existing map if the key is already in the map.
        [MethodImpl((MethodImplOptions)256)]
        public ImMap<V> AddOrKeep(int key, V value) =>
            Height == 0
                ? new ImMap<V>(key, value)
                : key == Key
                    ? this
                    : AddOrKeepLeftOrRight(key, value);

        private ImMap<V> AddOrKeepLeftOrRight(int key, V value)
        {
            if (key < Key)
            {
                if (Left.Height == 0)
                    return new ImMap<V>(Data, new ImMap<V>(key, value), Right, 2);

                if (Left.Key == key)
                    return this;

                if (Right.Height == 0)
                    return key < Left.Key
                        ? new ImMap<V>(Left.Data, new ImMap<V>(key, value), new ImMap<V>(Data), 2)
                        : new ImMap<V>(new ImMapData<V>(key, value), new ImMap<V>(Left.Data), new ImMap<V>(Data), 2);

                var newLeft = Left.AddOrKeepLeftOrRight(key, value);
                if (ReferenceEquals(newLeft, Left))
                    return this;

                if (newLeft.Height > Right.Height + 1) // left is longer by 2, rotate left
                {
                    var leftLeft = newLeft.Left;
                    var leftRight = newLeft.Right;

                    if (leftLeft.Height >= leftRight.Height)
                        return new ImMap<V>(newLeft.Data, leftLeft, new ImMap<V>(Data, leftRight, Right));

                    return new ImMap<V>(leftRight.Data,
                        new ImMap<V>(newLeft.Data, leftLeft, leftRight.Left),
                        new ImMap<V>(Data, leftRight.Right, Right));
                }

                return new ImMap<V>(Data, newLeft, Right);
            }
            else
            {
                if (Right.Height == 0)
                    return new ImMap<V>(Data, Left, new ImMap<V>(key, value), 2);

                if (Right.Key == key)
                    return this;

                if (Left.Height == 0)
                    return key >= Right.Key
                        ? new ImMap<V>(Right.Data, new ImMap<V>(Data), new ImMap<V>(key, value), 2)
                        : new ImMap<V>(new ImMapData<V>(key, value), new ImMap<V>(Data), new ImMap<V>(Right.Data), 2);

                var newRight = Right.AddOrKeepLeftOrRight(key, value);
                if (ReferenceEquals(newRight, Right))
                    return this;

                if (newRight.Height > Left.Height + 1)
                {
                    var rightLeft = newRight.Left;
                    var rightRight = newRight.Right;

                    return rightRight.Height >= rightLeft.Height
                        ? new ImMap<V>(newRight.Data, new ImMap<V>(Data, Left, rightLeft), rightRight)
                        : new ImMap<V>(rightLeft.Data,
                            new ImMap<V>(Data, Left, rightLeft.Left),
                            new ImMap<V>(newRight.Data, rightLeft.Right, rightRight));
                }

                return new ImMap<V>(Data, Left, newRight);
            }
        }

        /// Returns the new map with added or updated value for the specified key.
        [MethodImpl((MethodImplOptions)256)]
        public ImMap<V> AddOrUpdate(int key, V value, Update<int, V> updateValue) =>
            Height == 0
            ? new ImMap<V>(key, value)
            : key == Key
                ? new ImMap<V>(new ImMapData<V>(key, updateValue(key, Value, value)), Left, Right, Height)
                : AddOrUpdateLeftOrRight(key, value, updateValue);

        private ImMap<V> AddOrUpdateLeftOrRight(int key, V value, Update<int, V> updateValue)
        {
            if (key < Key)
            {
                if (Left.Height == 0)
                    return new ImMap<V>(Data, new ImMap<V>(key, value), Right, 2);

                if (Left.Key == key)
                {
                    var left = Left;
                    return new ImMap<V>(Data,
                        new ImMap<V>(new ImMapData<V>(key, updateValue(key, left.Value, value)), left.Left, left.Right, left.Height),
                        Right, Height);
                }

                if (Right.Height == 0)
                    return key < Left.Key
                        ? new ImMap<V>(Left.Data, new ImMap<V>(key, value), new ImMap<V>(Data), 2)
                        : new ImMap<V>(new ImMapData<V>(key, value), new ImMap<V>(Left.Data), new ImMap<V>(Data), 2);

                var newLeft = Left.AddOrUpdateImpl(key, value);

                if (newLeft.Height > Right.Height + 1) // left is longer by 2, rotate left
                {
                    var leftLeft = newLeft.Left;
                    var leftRight = newLeft.Right;

                    return leftLeft.Height >= leftRight.Height
                        ? new ImMap<V>(newLeft.Data, leftLeft, new ImMap<V>(Data, leftRight, Right))
                        : new ImMap<V>(leftRight.Data,
                            new ImMap<V>(newLeft.Data, leftLeft, leftRight.Left),
                            new ImMap<V>(Data, leftRight.Right, Right));
                }

                return new ImMap<V>(Data, newLeft, Right);
            }
            else
            {
                if (Right.Height == 0)
                    return new ImMap<V>(Data, Left, new ImMap<V>(key, value), 2);

                if (Right.Key == key)
                {
                    var right = Right;
                    return new ImMap<V>(Data, Left,
                        new ImMap<V>(new ImMapData<V>(key, updateValue(key, right.Value, value)), right.Left, right.Right, right.Height),
                        Height);
                }

                if (Left.Height == 0)
                    return key >= Right.Key
                        ? new ImMap<V>(Right.Data, new ImMap<V>(Data), new ImMap<V>(key, value), 2)
                        : new ImMap<V>(new ImMapData<V>(key, value), new ImMap<V>(Data), new ImMap<V>(Right.Data), 2);

                var newRight = Right.AddOrUpdateImpl(key, value);

                if (newRight.Height > Left.Height + 1)
                {
                    var rightLeft = newRight.Left;
                    var rightRight = newRight.Right;

                    return rightRight.Height >= rightLeft.Height
                        ? new ImMap<V>(newRight.Data, new ImMap<V>(Data, Left, rightLeft), rightRight)
                        : new ImMap<V>(rightLeft.Data, new ImMap<V>(Data, Left, rightLeft.Left),
                            new ImMap<V>(newRight.Data, rightLeft.Right, rightRight));
                }

                return new ImMap<V>(Data, Left, newRight);
            }
        }

        /// Returns the new map with the updated value for the key, or the same map if the key was not found.
        [MethodImpl((MethodImplOptions)256)]
        public ImMap<V> Update(int key, V value) =>
            this.TryFind(key, out _) ? UpdateImpl(key, value) : this;

        internal ImMap<V> UpdateImpl(int key, V value) =>
            key < Key ? new ImMap<V>(Data, Left.UpdateImpl(key, value), Right, Height)
          : key > Key ? new ImMap<V>(Data, Left, Right.UpdateImpl(key, value), Height)
          : new ImMap<V>(new ImMapData<V>(key, value), Left, Right, Height);

        // todo: Potentially leaks, cause returned ImMap references left and right sub-trees - replace with `KeyValuePair`
        /// Returns all map tree nodes enumerated the lesser to the bigger keys 
        public IEnumerable<ImMap<V>> Enumerate()
        {
            if (Height == 0)
                yield break;

            // todo: use the LiveCountArray to pool the trees
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
        [MethodImpl((MethodImplOptions)256)]
        public ImMap<V> Remove(int key)
        {
            if (Height == 0)
                return this;

            if (key == Key) // we've found the node to remove
            {
                if (Height == 1) // remove the leaf node
                    return Empty;

                // if we have the on child remaining then just return it
                if (Right.Height == 0)
                    return Left;

                if (Left.Height == 0)
                    return Right;

                // we have two children,
                // so remove the next highest node and replace this node with it.
                var successor = Right;
                while (successor.Left.Height != 0)
                    successor = successor.Left;
                return new ImMap<V>(successor.Data, Left, Right.Remove(successor.Key));
            }

            // remove the node and balance the new tree
            return key < Key
                ? Balance(Key, Value, Left.Remove(key), Right)
                : Balance(Key, Value, Left, Right.Remove(key));
        }

        /// <summary>Outputs key value pair</summary>
        public override string ToString() => IsEmpty ? "empty" : Key + ":" + Value;

        internal ImMap() { }

        internal ImMap(int key, V value)
        {
            Data = new ImMapData<V>(key, value);
            Left = Empty;
            Right = Empty;
            Height = 1;
        }

        internal ImMap(ImMapData<V> data)
        {
            Data = data;
            Left = Empty;
            Right = Empty;
            Height = 1;
        }

        internal ImMap(ImMapData<V> data, ImMap<V> left, ImMap<V> right, int height)
        {
            Data = data;
            Left = left;
            Right = right;
            Height = height;
        }

        internal ImMap(ImMapData<V> data, ImMap<V> left, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            Height = left.Height > right.Height ? left.Height + 1 : right.Height + 1;
        }

        internal static ImMap<V> Balance(int key, V value, ImMap<V> left, ImMap<V> right)
        {
            var delta = left.Height - right.Height;
            if (delta > 1) // left is longer by 2, rotate left
            {
                var leftLeft = left.Left;
                var leftRight = left.Right;
                if (leftRight.Height > leftLeft.Height)
                {
                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1
                    return new ImMap<V>(leftRight.Data,
                        new ImMap<V>(left.Data, leftLeft, leftRight.Left),
                        new ImMap<V>(new ImMapData<V>(key, value), leftRight.Right, right));
                }

                // single rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                return new ImMap<V>(left.Data, leftLeft, new ImMap<V>(new ImMapData<V>(key, value), leftRight, right));
            }

            if (delta < -1)
            {
                var rightLeft = right.Left;
                var rightRight = right.Right;
                if (rightLeft.Height > rightRight.Height)
                {
                    return new ImMap<V>(rightLeft.Data,
                        new ImMap<V>(new ImMapData<V>(key, value), left, rightLeft.Left),
                        new ImMap<V>(right.Data, rightLeft.Right, rightRight));
                }

                return new ImMap<V>(right.Data, new ImMap<V>(new ImMapData<V>(key, value), left, rightLeft), rightRight);
            }

            return new ImMap<V>(new ImMapData<V>(key, value), left, right);
        }
    }

    /// ImMap static methods
    public static class ImMap
    {
        internal static V IgnoreKey<K, V>(this Update<V> update, K _, V oldValue, V newValue) => update(oldValue, newValue);

        /// Get value for found key or the default value otherwise.
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap<V> map, int key)
        {
            while (map.Height != 0 && map.Key != key)
                map = key < map.Key ? map.Left : map.Right;
            return map.Value; // that's fine to return the value without check, because for we have a default value in empty map
        }

        /// Get value for found key or the specified default value otherwise.
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap<V> map, int key, V defaultValue)
        {
            while (map.Height != 0 && map.Key != key)
                map = key < map.Key ? map.Left : map.Right;
            return map.Height != 0 ? map.Value : defaultValue;
        }

        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMap<V> map, int key, out V value)
        {
            while (map.Height != 0)
            {
                if (key < map.Key)
                    map = map.Left;
                else if (key > map.Key)
                    map = map.Right;
                else
                    break;
            }

            value = map.Value;
            return map.Height != 0;
        }
    }
}
