using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Benchmarks
{
    public interface ImMapSlot<V>
    {
        /// Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.
        int Height { get; }

        /// The Key without last 4 bits
        int Key { get; }

        /// The value
        V Value { get; }

        /// Left sub-tree/branch, or empty
        ImMapSlot<V> Left { get; }

        /// Right sub-tree/branch, or empty
        ImMapSlot<V> Right { get; }

        /// Returns a slot with the new value
        ImMapSlot<V> NewWith(V value);
    }

    public struct ImMapEmpty<V> : ImMapSlot<V>
    {
        public static readonly ImMapSlot<V> Empty = new ImMapEmpty<V>();

        public int Height => 0;
        public int Key => 0;
        public V Value => default;
        public ImMapSlot<V> Left => Empty;
        public ImMapSlot<V> Right => Empty;
        public ImMapSlot<V> NewWith(V value) => this;

        public override string ToString() => "Empty";
    }

    public struct ImMapLeaf<V> : ImMapSlot<V>
    {
        private readonly int _key;
        private readonly V _value;

        public ImMapLeaf(int key, V value)
        {
            _key = key;
            _value = value;
        }

        /// Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.
        public int Height
        {
            [MethodImpl((MethodImplOptions)256)]
            get => 1;
        }

        /// The Key without last 4 bits
        public int Key
        {
            [MethodImpl((MethodImplOptions)256)]
            get => _key;
        }

        public V Value
        {
            [MethodImpl((MethodImplOptions)256)]
            get => _value;
        }

        /// Left and Right branches are empty
        public ImMapSlot<V> Left
        {
            [MethodImpl((MethodImplOptions)256)]
            get => ImMapEmpty<V>.Empty;
        }

        /// Left and Right branches are empty
        public ImMapSlot<V> Right
        {
            [MethodImpl((MethodImplOptions)256)]
            get => ImMapEmpty<V>.Empty;
        }

        [MethodImpl((MethodImplOptions)256)]
        public ImMapSlot<V> NewWith(V value) => new ImMapLeaf<V>(_key, value);

        public override string ToString() => "Leaf:" + Key + "->" + Value;
    }

    public struct ImMapBranch<V, L, R> : ImMapSlot<V> 
        where L : ImMapSlot<V> 
        where R : ImMapSlot<V>
    {
        private readonly int _heightThenKey;
        private readonly V _value;
        private readonly L _left;
        private readonly R _right;

        internal ImMapBranch(int heightThenKey, V value, L left, R right)
        {
            _heightThenKey = heightThenKey;
            _value = value;
            _left = left;
            _right = right;
        }

        /// The value
        public V Value
        {
            [MethodImpl((MethodImplOptions)256)]
            get => _value;
        }

        /// <summary>Left sub-tree/branch, or empty.</summary>
        public ImMapSlot<V> Left
        {
            [MethodImpl((MethodImplOptions)256)]
            get => _left;
        }

        /// <summary>Right sub-tree/branch, or empty.</summary>
        public ImMapSlot<V> Right
        {
            [MethodImpl((MethodImplOptions)256)]
            get => _right;
        }

        /// The Key without last 4 bits
        public int Key
        {
            [MethodImpl((MethodImplOptions)256)]
            get => _heightThenKey & ImMapArray.KEY_MASK;
        }

        /// Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.
        public int Height
        {
            [MethodImpl((MethodImplOptions)256)]
            get => _heightThenKey & ImMapArray.SLOT_COUNT_MASK;
        }

        [MethodImpl((MethodImplOptions)256)]
        public ImMapSlot<V> NewWith(V value) => new ImMapBranch<V, L, R>(_heightThenKey, value, _left, _right);

        /// <summary>Outputs key value pair</summary>
        public override string ToString() => "Branch:" + Key + "->" + Value;

        /* todo move to static methods in static ImMapArray class

        /// <summary>Returns new tree with added or updated value for specified key.</summary>
        /// <param name="key">Key</param> <param name="value">Value</param>
        /// <param name="updateValue">(optional) Delegate to calculate new value from and old and a new value.</param>
        /// <returns>New tree.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public ImMapSlot<V> AddOrUpdate(int key, V value, Update<V> updateValue) =>
            AddOrUpdateImpl(key, value, false, updateValue);

        /// <summary>Returns new tree with updated value for the key, Or the same tree if key was not found.</summary>
        /// <param name="key"></param> <param name="value"></param>
        /// <returns>New tree if key is found, or the same tree otherwise.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public ImMapSlot<V> Update(int key, V value) =>
            AddOrUpdateImpl(key, value, true, null);

        // todo: Leak, cause returned ImMap references left and right sub-trees - replace with `KeyValuePair`
        /// <summary>Returns all sub-trees enumerated from left to right.</summary> 
        /// <returns>Enumerated sub-trees or empty if tree is empty.</returns>
        public IEnumerable<ImMapSlot<V>> Enumerate()
        {
            if (Height == 0)
                yield break;

            var parents = new ImMapSlot<V>[Height];

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
        public ImMapSlot<V> Remove(int key) =>
            RemoveImpl(key);

        */

        /*

        private ImMapSlot<V> AddOrUpdateImpl(int key, V value, bool updateOnly, Update<V> update)
        {
            return Height == 0
                ? // tree is empty
                (updateOnly ? this : new ImMapSlot<V>(key, value))
                : (key == Key
                    ? // actual update
                    new ImMapSlot<V>(key, update == null ? value : update(Value, value), Left, Right)
                    : (key < Key // try update on left or right sub-tree
                        ? Balance(Key, Value, Left.AddOrUpdateImpl(key, value, updateOnly, update), Right)
                        : Balance(Key, Value, Left, Right.AddOrUpdateImpl(key, value, updateOnly, update))));
        }

        internal static ImMapSlot<V> Balance(int key, V value, ImMapSlot<V> left, ImMapSlot<V> right)
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
                    return new ImMapSlot<V>(leftRight.Key, leftRight.Value,
                        new ImMapSlot<V>(left.Key, left.Value, leftLeft, leftRight.Left),
                        new ImMapSlot<V>(key, value, leftRight.Right, right));
                }

                // single rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                return new ImMapSlot<V>(left.Key, left.Value,
                    leftLeft,
                    new ImMapSlot<V>(key, value, leftRight, right));
            }

            if (delta < -1)
            {
                var rightLeft = right.Left;
                var rightRight = right.Right;
                if (rightLeft.Height > rightRight.Height)
                {
                    return new ImMapSlot<V>(rightLeft.Key, rightLeft.Value,
                        new ImMapSlot<V>(key, value, left, rightLeft.Left),
                        new ImMapSlot<V>(right.Key, right.Value, rightLeft.Right, rightRight));
                }

                return new ImMapSlot<V>(right.Key, right.Value,
                    new ImMapSlot<V>(key, value, left, rightLeft),
                    rightRight);
            }

            return new ImMapSlot<V>(key, value, left, right);
        }

        private ImMapSlot<V> RemoveImpl(int key, bool ignoreKey = false)
        {
            if (Height == 0)
                return this;

            ImMapSlot<V> result;
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
                    result = new ImMapSlot<V>(successor.Key, successor.Value,
                        Left, Right.RemoveImpl(successor.Key, true));
                }
            }
            else if (key < Key)
                result = Balance(Key, Value, Left.RemoveImpl(key), Right);
            else
                result = Balance(Key, Value, Left, Right.RemoveImpl(key));

            return result;
        }

    */
    }

    public sealed class ImMapArray<V>
    {
        public readonly ImMapSlot<V>[] Slots;

        public ImMapArray()
        {
            var slots = new ImMapSlot<V>[ImMapArray.SLOT_COUNT];
            for (var i = 0; i < ImMapArray.SLOT_COUNT; ++i)
                slots[i] = ImMapEmpty<V>.Empty;
            Slots = slots;
        }

        /// Returns a new tree with added or updated value for specified key.
        [MethodImpl((MethodImplOptions)256)]
        public void AddOrUpdate(int key, V value)
        {
            var k = key & ImMapArray.KEY_MASK;

            ref var x = ref Slots[key & ImMapArray.SLOT_COUNT_MASK];
            var slot = x;
            var newSlot = slot.Height == 0 ? new ImMapLeaf<V>(k, value)
                : k == slot.Key ? slot.NewWith(value)
                : slot.AddOrUpdateImpl(k, value);

            if (Interlocked.CompareExchange(ref x, newSlot, slot) != slot)
                RefUpdateSlots();
        }

        private void RefUpdateSlots()
        {
        }
    }

    /// ImMap methods
    public static class ImMapArray
    {
        public const int SLOT_COUNT = 16;
        public const int SLOT_COUNT_MASK = SLOT_COUNT - 1;
        public const int KEY_MASK = ~SLOT_COUNT_MASK;

        [MethodImpl((MethodImplOptions)256)]
        internal static ImMapBranch<V, L, R> NewBranch<V, L, R>(int key, V value, L left, R right) 
            where R : ImMapSlot<V> 
            where L : ImMapSlot<V> =>
            new ImMapBranch<V, L, R>(key | (left.Height > right.Height ? left.Height + 1 : right.Height + 1), value, left, right);

        /// Get value for found key or the default value otherwise.
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMapArray<V> map, int key)
        {
            var slot = map.Slots[key & SLOT_COUNT_MASK];

            var k = key & KEY_MASK;
            while (slot.Height != 0 && k != slot.Key)
                slot = k < slot.Key ? slot.Left : slot.Right;

            return slot.Value;
        }

        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMapArray<V> map, int key, out V value)
        {
            var slot = map.Slots[key & SLOT_COUNT_MASK];

            var k = key & KEY_MASK;
            while (slot.Height != 0 && k != slot.Key)
                slot = k < slot.Key ? slot.Left : slot.Right;

            if (slot.Height == 0)
            {
                value = default;
                return false;
            }

            value = slot.Value;
            return true;
        }

        internal static ImMapSlot<V> AddOrUpdateImpl<V>(this ImMapSlot<V> map, int key, V value)
        {
            var mapLeft = map.Left;
            var mapRight = map.Right;
            var mapKey = map.Key;
            if (key < mapKey)
            {
                // todo: right-leafy branch can be optimized by directly inlining leaf structure, 
                // actually any direct branch constructor use may be optimized this way
                if (mapLeft.Height == 0)
                    return new ImMapBranch<V, ImMapLeaf<V>, ImMapSlot<V>>(mapKey | 2, map.Value, new ImMapLeaf<V>(key, value), mapRight);

                // todo: left-leafy branch can be optimized by directly inlining leaf structure
                if (mapLeft.Key == key)
                    return new ImMapBranch<V, ImMapLeaf<V>, ImMapSlot<V>>(mapKey | map.Height, map.Value, new ImMapLeaf<V>(key, value), mapRight);

                if (mapRight.Height == 0)
                {
                    // single rotation:
                    //      5     =>     2
                    //   2            1     5
                    // 1              
                    if (key < mapLeft.Key)
                        return new ImMapBranch<V, ImMapLeaf<V>, ImMapLeaf<V>>(mapLeft.Key | 2, mapLeft.Value, 
                            new ImMapLeaf<V>(key, value), new ImMapLeaf<V>(mapKey, map.Value));

                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2            4            2     5
                    //     4        2               
                    return new ImMapBranch<V, ImMapLeaf<V>, ImMapLeaf<V>>(key | 2, value,
                        new ImMapLeaf<V>(mapLeft.Key, mapLeft.Value), new ImMapLeaf<V>(mapKey, map.Value));
                }

                var newLeft = mapLeft.AddOrUpdateImpl(key, value);

                if (newLeft.Height > mapRight.Height + 1) // left is longer by 2, rotate left
                {
                    var leftLeft = newLeft.Left;
                    var leftRight = newLeft.Right;

                    // single rotation:
                    //      5     =>     2
                    //   2     6      1     5
                    // 1   4              4   6
                    if (leftLeft.Height >= leftRight.Height)
                    {

                        return NewBranch(newLeft.Key, newLeft.Value,
                            leftLeft, NewBranch(mapKey, map.Value, leftRight, mapRight));

                    }

                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1
                    return NewBranch(leftRight.Key, leftRight.Value,
                        NewBranch(newLeft.Key, newLeft.Value, leftLeft, leftRight.Left),
                        NewBranch(mapKey, map.Value, leftRight.Right, mapRight));
                }

                return NewBranch(mapKey, map.Value, newLeft, mapRight);
            }
            else // if (key >= map.Key)
            {
                if (mapRight.Height == 0)
                    return new ImMapBranch<V, ImMapSlot<V>, ImMapLeaf<V>>(mapKey | 2, map.Value, mapLeft, new ImMapLeaf<V>(key, value));

                if (mapRight.Key == key)
                    return new ImMapBranch<V, ImMapSlot<V>, ImMapLeaf<V>>(mapKey | map.Height, map.Value, mapLeft, new ImMapLeaf<V>(key, value));

                if (mapLeft.Height == 0)
                {
                    // single rotation:
                    //      5     =>     8     
                    //         8      5     9
                    //           9
                    if (key >= mapRight.Key)
                        return new ImMapBranch<V, ImMapLeaf<V>, ImMapLeaf<V>>(mapRight.Key | 2, mapRight.Value,
                            new ImMapLeaf<V>(mapKey, map.Value), new ImMapLeaf<V>(key, value));

                    // double rotation:
                    //      5     =>     5     =>     7
                    //         8            7      5     8
                    //        7              8
                    return new ImMapBranch<V, ImMapLeaf<V>, ImMapLeaf<V>>(key | 2, value,
                        new ImMapLeaf<V>(mapKey, map.Value), new ImMapLeaf<V>(mapRight.Key, mapRight.Value));
                }

                var newRight = mapRight.AddOrUpdateImpl(key, value);

                if (newRight.Height > mapLeft.Height + 1)
                {
                    var rightLeft = newRight.Left;
                    var rightRight = newRight.Right;

                    if (rightRight.Height >= rightLeft.Height)
                        return NewBranch(newRight.Key, newRight.Value,
                            NewBranch(mapKey, map.Value, mapLeft, rightLeft), rightRight);

                    return NewBranch(rightLeft.Key, rightLeft.Value,
                        NewBranch(mapKey, map.Value, mapLeft, rightLeft.Left),
                        NewBranch(newRight.Key, newRight.Value, rightLeft.Right, rightRight));
                }

                return NewBranch(mapKey, map.Value, mapLeft, newRight);
            }
        }
    }
}
