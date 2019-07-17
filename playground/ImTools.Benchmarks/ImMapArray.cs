using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Benchmarks
{
    public sealed class ImMapSlot<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMapSlot<V> Empty = new ImMapSlot<V>();

        internal readonly int _heightThenKey;

        /// <summary>Value.</summary>
        public readonly V Value;

        /// <summary>Left sub-tree/branch, or empty.</summary>
        public readonly ImMapSlot<V> Left;

        /// <summary>Right sub-tree/branch, or empty.</summary>
        public readonly ImMapSlot<V> Right;

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

        /// <summary>Returns true is tree is empty.</summary>
        public bool IsEmpty
        {
            [MethodImpl((MethodImplOptions)256)]
            get => _heightThenKey == 0;
        }

        public ImMapSlot<V> AddOrUpdateImpl(int key, V value)
        {
            if (key < Key)
            {
                if (Left.Height == 0)
                    return new ImMapSlot<V>(Key, Value, new ImMapSlot<V>(key, value), Right, 2);

                if (Left.Key == key)
                    return new ImMapSlot<V>(Key, Value, new ImMapSlot<V>(key, value), Right, Height);

                if (Right.Height == 0)
                {
                    // single rotation:
                    //      5     =>     2
                    //   2            1     5
                    // 1              
                    if (key < Left.Key)
                        return new ImMapSlot<V>(Left.Key, Left.Value,
                            new ImMapSlot<V>(key, value), new ImMapSlot<V>(Key, Value), 2);

                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2            4            2     5
                    //     4        2               
                    return new ImMapSlot<V>(key, value,
                        new ImMapSlot<V>(Left.Key, Left.Value), new ImMapSlot<V>(Key, Value), 2);
                }

                var newLeft = Left.AddOrUpdateImpl(key, value);

                if (newLeft.Height > Right.Height + 1) // left is longer by 2, rotate left
                {
                    var leftLeft = newLeft.Left;
                    var leftRight = newLeft.Right;

                    // single rotation:
                    //      5     =>     2
                    //   2     6      1     5
                    // 1   4              4   6
                    if (leftLeft.Height >= leftRight.Height)
                        return new ImMapSlot<V>(newLeft.Key, newLeft.Value,
                            leftLeft, new ImMapSlot<V>(Key, Value, leftRight, Right));

                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1
                    return new ImMapSlot<V>(leftRight.Key, leftRight.Value,
                        new ImMapSlot<V>(newLeft.Key, newLeft.Value, leftLeft, leftRight.Left),
                        new ImMapSlot<V>(Key, Value, leftRight.Right, Right));
                }

                return new ImMapSlot<V>(Key, Value, newLeft, Right);
            }
            else
            {
                if (Right.Height == 0)
                    return new ImMapSlot<V>(Key, Value, Left, new ImMapSlot<V>(key, value), 2);

                if (Right.Key == key)
                    return new ImMapSlot<V>(Key, Value, Left, new ImMapSlot<V>(key, value), Height);

                if (Left.Height == 0)
                {
                    // single rotation:
                    //      5     =>     8     
                    //         8      5     9
                    //           9
                    if (key >= Right.Key)
                        return new ImMapSlot<V>(Right.Key, Right.Value,
                            new ImMapSlot<V>(Key, Value), new ImMapSlot<V>(key, value), 2);

                    // double rotation:
                    //      5     =>     5     =>     7
                    //         8            7      5     8
                    //        7              8
                    return new ImMapSlot<V>(key, value,
                        new ImMapSlot<V>(Key, Value), new ImMapSlot<V>(Right.Key, Right.Value), 2);
                }

                var newRight = Right.AddOrUpdateImpl(key, value);

                if (newRight.Height > Left.Height + 1)
                {
                    var rightLeft = newRight.Left;
                    var rightRight = newRight.Right;

                    if (rightRight.Height >= rightLeft.Height)
                        return new ImMapSlot<V>(newRight.Key, newRight.Value,
                            new ImMapSlot<V>(Key, Value, Left, rightLeft), rightRight);

                    return new ImMapSlot<V>(rightLeft.Key, rightLeft.Value,
                        new ImMapSlot<V>(Key, Value, Left, rightLeft.Left),
                        new ImMapSlot<V>(newRight.Key, newRight.Value, rightLeft.Right, rightRight));
                }

                return new ImMapSlot<V>(Key, Value, Left, newRight);
            }
        }

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

        /// <summary>Outputs key value pair</summary>
        public override string ToString() => IsEmpty ? "empty" : (Key + ":" + Value);

        #region Implementation

        internal ImMapSlot() { }

        internal ImMapSlot(int key, V value)
        {
            _heightThenKey = key | 1;
            Value = value;
            Left = Empty;
            Right = Empty;
        }

        internal ImMapSlot(int key, V value, ImMapSlot<V> left, ImMapSlot<V> right, int height)
        {
            _heightThenKey = key | height;
            Value = value;
            Left = left;
            Right = right;
        }

        internal ImMapSlot(int key, V value, ImMapSlot<V> left, ImMapSlot<V> right)
        {
            _heightThenKey = key | (left.Height > right.Height ? left.Height + 1 : right.Height + 1);
            Value = value;
            Left = left;
            Right = right;
        }

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

        #endregion
    }

    public sealed class ImMapArray<V>
    {
        public readonly ImMapSlot<V>[] Slots;

        public ImMapArray()
        {
            var slots = new ImMapSlot<V>[ImMapArray.SLOT_COUNT];
            for (var i = 0; i < ImMapArray.SLOT_COUNT; ++i)
                slots[i] = ImMapSlot<V>.Empty;;
            Slots = slots;
        }

        /// Returns a new tree with added or updated value for specified key.
        [MethodImpl((MethodImplOptions)256)]
        public void AddOrUpdate(int key, V value)
        {
            var k = key & ImMapArray.KEY_MASK;

            ref var x = ref Slots[key & ImMapArray.SLOT_COUNT_MASK];
            var slot = x;
            var newSlot = slot._heightThenKey == 0 ? new ImMapSlot<V>(k, value)
                : k == slot.Key ? new ImMapSlot<V>(k, value, slot.Left, slot.Right, slot.Height)
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

        /// Get value for found key or the default value otherwise.
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMapArray<V> map, int key)
        {
            var slot = map.Slots[key & SLOT_COUNT_MASK];

            var k = key & KEY_MASK;
            while (slot._heightThenKey != 0 && k != slot.Key)
                slot = k < slot.Key ? slot.Left : slot.Right;

            return slot.Value;
        }

        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMapArray<V> map, int key, out V value)
        {
            var slot = map.Slots[key & SLOT_COUNT_MASK];

            var k = key & KEY_MASK;
            while (slot._heightThenKey != 0 && k != slot.Key)
                slot = k < slot.Key ? slot.Left : slot.Right;

            if (slot._heightThenKey == 0)
            {
                value = default;
                return false;
            }

            value = slot.Value;
            return true;
        }
    }
}
