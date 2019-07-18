using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Benchmarks
{
    public struct ImMapArray<V>
    {
        public const int SLOT_COUNT = 16;
        public const int HEIGHT_MASK = SLOT_COUNT - 1;
        public const int KEY_MASK = ~HEIGHT_MASK;

        /// Create en empty array
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapArray<V> Create()
        {
            var slots = new ImMapSlot<V>[SLOT_COUNT];
            for (var i = 0; i < SLOT_COUNT; ++i)
                slots[i] = ImMapSlot<V>.Empty;
            return new ImMapArray<V>(slots);
        }

        internal readonly ImMapSlot<V>[] Slots;
        private ImMapArray(ImMapSlot<V>[] slots) => Slots = slots;

        /// Returns a new tree with added or updated value for specified key.
        [MethodImpl((MethodImplOptions)256)]
        public void AddOrUpdate(int key, V value)
        {
            ref var x = ref Slots[key & HEIGHT_MASK];
            var slot = x; // fix a copy of x to operate on

            var newSlot = slot.HeightThenKey == 0 ? ImMapSlot<V>.Leaf(key & KEY_MASK, value)
                : (key & KEY_MASK) != slot.Key ? slot.AddOrUpdate(key & KEY_MASK, value)
                : new ImMapSlot<V>(slot.HeightThenKey, value, slot.Left, slot.Right);

            if (Interlocked.CompareExchange(ref x, newSlot, slot) != slot)
                RefAddOrUpdateSlot(ref x, key, value);
        }

        private static void RefAddOrUpdateSlot(ref ImMapSlot<V> slot, int key, V value) =>
            Ref.Swap(ref slot, key, value, (s, k, v) =>
                s.HeightThenKey == 0 ? ImMapSlot<V>.Leaf(k, v)
                : k != s.Key ? s.AddOrUpdate(k, v)
                : new ImMapSlot<V>(s.HeightThenKey, v, s.Left, s.Right));

        /// <summary>Returns new tree with added or updated value for specified key.</summary>
        /// <param name="key">Key</param> <param name="value">Value</param>
        /// <param name="updateValue">(optional) Delegate to calculate new value from and old and a new value.</param>
        /// <returns>New tree.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public void AddOrUpdate(int key, V value, Update<V> updateValue)
        {
            ref var x = ref Slots[key & HEIGHT_MASK];
            var slot = x;

            var newSlot = slot.AddOrUpdate(key & KEY_MASK, value, false, updateValue);
            if (Interlocked.CompareExchange(ref x, newSlot, slot) != slot)
                RefAddOrUpdateSlot(ref x, key & KEY_MASK, value, updateValue);
        }

        private static void RefAddOrUpdateSlot(ref ImMapSlot<V> slot, int key, V value, Update<V> updateValue) =>
            Ref.Swap(ref slot, key, value, updateValue, (s, k, v, u) => s.AddOrUpdate(k, v, false, u));

        /// <summary>Returns new tree with updated value for the key, Or the same tree if key was not found.</summary>
        /// <param name="key"></param> <param name="value"></param>
        /// <returns>New tree if key is found, or the same tree otherwise.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public void Update(int key, V value)
        {
            ref var x = ref Slots[key & HEIGHT_MASK];
            var s = x;
            var newSlot = s.AddOrUpdate(key & KEY_MASK, value, true, null);
            if (Interlocked.CompareExchange(ref x, newSlot, s) != s)
                RefUpdateSlot(ref x, key & KEY_MASK, value);
        }

        private static void RefUpdateSlot(ref ImMapSlot<V> slot, int key, V value) =>
            Ref.Swap(ref slot, key, value, (s, k, v) => s.AddOrUpdate(k, v, true, null));

        /// Get value for found key or the default value otherwise.
        [MethodImpl((MethodImplOptions)256)]
        public V GetValueOrDefault(int key)
        {
            var slot = Slots[key & HEIGHT_MASK];

            key &= KEY_MASK;
            while (slot.HeightThenKey != 0 && key != slot.Key)
                slot = key < slot.Key ? slot.Left : slot.Right;

            return slot.Value;
        }

        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public bool TryFind(int key, out V value)
        {
            var slot = Slots[key & HEIGHT_MASK];

            key &= KEY_MASK;
            while (slot.HeightThenKey != 0 && key != slot.Key)
                slot = key < slot.Key ? slot.Left : slot.Right;

            if (slot.HeightThenKey == 0)
            {
                value = default;
                return false;
            }

            value = slot.Value;
            return true;
        }
    }

    internal sealed class ImMapSlot<V>
    {
        /// Empty tree to start with
        public static readonly ImMapSlot<V> Empty = new ImMapSlot<V>(0, default, null, null);

        /// Combines height in lower bits and the trimmed key in upper bits
        public readonly int HeightThenKey;

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
            get => HeightThenKey & ImMapArray<V>.KEY_MASK;
        }

        /// Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.
        public int Height
        {
            [MethodImpl((MethodImplOptions)256)]
            get => HeightThenKey & ImMapArray<V>.HEIGHT_MASK;
        }

        /// <summary>Returns true is tree is empty.</summary>
        public bool IsEmpty
        {
            [MethodImpl((MethodImplOptions)256)]
            get => HeightThenKey == 0;
        }

        /// Outputs the key value pair or empty if node is empty
        public override string ToString() => IsEmpty ? "empty" : ("Node:" + Key + "->" + Value);

        internal ImMapSlot(int heightThenKey, V value, ImMapSlot<V> left, ImMapSlot<V> right)
        {
            HeightThenKey = heightThenKey;
            Value = value;
            Left = left;
            Right = right;
        }

        [MethodImpl((MethodImplOptions)256)]
        internal static ImMapSlot<V> Leaf(int key, V value) =>
            new ImMapSlot<V>(key | 1, value, Empty, Empty);

        [MethodImpl((MethodImplOptions)256)]
        internal static ImMapSlot<V> Branch(int key, V value, ImMapSlot<V> left, ImMapSlot<V> right) =>
            new ImMapSlot<V>(key | (left.Height > right.Height ? left.Height + 1 : right.Height + 1), value, left, right);

        internal ImMapSlot<V> AddOrUpdate(int key, V value)
        {
            if (key < Key)
            {
                if (Left.HeightThenKey == 0)
                    return new ImMapSlot<V>(Key | 2, Value, Leaf(key, value), Right);

                if (Left.Key == key)
                    return new ImMapSlot<V>(Key | Height, Value, Leaf(key, value), Right);

                if (Right.HeightThenKey == 0)
                {
                    // single rotation:
                    //      5     =>     2
                    //   2            1     5
                    // 1              
                    if (key < Left.Key)
                        return new ImMapSlot<V>(Left.Key | 2, Left.Value, Leaf(key, value), Leaf(Key, Value));

                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2            4            2     5
                    //     4        2               
                    return new ImMapSlot<V>(key | 2, value, Leaf(Left.Key, Left.Value), Leaf(Key, Value));
                }

                var newLeft = Left.AddOrUpdate(key, value);

                if (newLeft.Height > Right.Height + 1) // left is longer by 2, rotate left
                {
                    var leftLeft = newLeft.Left;
                    var leftRight = newLeft.Right;

                    // single rotation:
                    //      5     =>     2
                    //   2     6      1     5
                    // 1   4              4   6
                    if (leftLeft.Height >= leftRight.Height)
                        return Branch(newLeft.Key, newLeft.Value, leftLeft, Branch(Key, Value, leftRight, Right));

                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1
                    return Branch(leftRight.Key, leftRight.Value,
                        Branch(newLeft.Key, newLeft.Value, leftLeft, leftRight.Left),
                        Branch(Key, Value, leftRight.Right, Right));
                }

                return Branch(Key, Value, newLeft, Right);
            }
            else
            {
                if (Right.HeightThenKey == 0)
                    return Branch(Key | 2, Value, Left, Leaf(key, value));

                if (Right.Key == key)
                    return Branch(Key | Height, Value, Left, Leaf(key, value));

                if (Left.HeightThenKey == 0)
                {
                    // single rotation:
                    //      5     =>     8     
                    //         8      5     9
                    //           9
                    if (key >= Right.Key)
                        return new ImMapSlot<V>(Right.Key | 2, Right.Value, Leaf(Key, Value), Leaf(key, value));

                    // double rotation:
                    //      5     =>     5     =>     7
                    //         8            7      5     8
                    //        7              8
                    return new ImMapSlot<V>(key | 2, value, Leaf(Key, Value), Leaf(Right.Key, Right.Value));
                }

                var newRight = Right.AddOrUpdate(key, value);

                if (newRight.Height > Left.Height + 1)
                {
                    var rightLeft = newRight.Left;
                    var rightRight = newRight.Right;

                    if (rightRight.Height >= rightLeft.Height)
                        return Branch(newRight.Key, newRight.Value, Branch(Key, Value, Left, rightLeft), rightRight);

                    return Branch(rightLeft.Key, rightLeft.Value,
                        Branch(Key, Value, Left, rightLeft.Left),
                        Branch(newRight.Key, newRight.Value, rightLeft.Right, rightRight));
                }

                return Branch(Key, Value, Left, newRight);
            }
        }

        // note: Not so much optimized (memory and performance wise) as a AddOrUpdateImpl without `update` delegate
        internal ImMapSlot<V> AddOrUpdate(int key, V value, bool updateOnly, Update<V> update) =>
            HeightThenKey == 0
                ? (updateOnly ? this : Leaf(key, value))
                : key == Key
                    ? new ImMapSlot<V>(HeightThenKey, update == null ? value : update(Value, value), Left, Right)
                    : key < Key // try update on left or right sub-tree
                        ? Balance(Key, Value, Left.AddOrUpdate(key, value, updateOnly, update), Right)
                        : Balance(Key, Value, Left, Right.AddOrUpdate(key, value, updateOnly, update));

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
                    return Branch(leftRight.Key, leftRight.Value,
                        Branch(left.Key, left.Value, leftLeft, leftRight.Left),
                        Branch(key, value, leftRight.Right, right));
                }

                // single rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                return Branch(left.Key, left.Value, leftLeft, Branch(key, value, leftRight, right));
            }

            if (delta < -1)
            {
                var rightLeft = right.Left;
                var rightRight = right.Right;
                if (rightLeft.Height > rightRight.Height)
                    return Branch(rightLeft.Key, rightLeft.Value,
                        Branch(key, value, left, rightLeft.Left),
                        Branch(right.Key, right.Value, rightLeft.Right, rightRight));

                return Branch(right.Key, right.Value, Branch(key, value, left, rightLeft), rightRight);
            }

            return Branch(key, value, left, right);
        }
    }
}
