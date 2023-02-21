using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ImTools.Experimental.ImMap_SplitNodes
{
    /// <summary>Immutable http://en.wikipedia.org/wiki/AVL_tree with integer keys and <typeparamref name="V"/> values.</summary>
    public class ImMap<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap<V> Empty = new Branch();

        /// <summary>Returns true is tree is empty.</summary>
        public bool IsEmpty => this == Empty;

        /// <summary>Key.</summary>
        public readonly int Key;

        /// <summary>Value.</summary>
        public readonly V Value;

        /// The branch node.
        internal sealed class Branch : ImMap<V>
        {
            /// <summary>Left sub-tree/branch, or empty.</summary>
            public readonly ImMap<V> Left;

            /// <summary>Right sub-tree/branch, or empty.</summary>
            public readonly ImMap<V> Right;

            /// <summary>Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.</summary>
            public readonly int Height;

            internal Branch() { }

            internal Branch(int key, V value, ImMap<V> left, ImMap<V> right) : base(key, value)
            {
                Left = left;
                Right = right;
                var leftHeight = left is Branch lb ? lb.Height : 1;
                var rightHeight = right is Branch rb ? rb.Height : 1;
                Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
            }

            internal Branch(int key, V value, int leftHeight, ImMap<V> left, ImMap<V> right) : base(key, value)
            {
                Left = left;
                Right = right;
                var rightHeight = right is Branch b ? b.Height : 1;
                Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
            }

            internal Branch(int key, V value, ImMap<V> left, int rightHeight, ImMap<V> right) : base(key, value)
            {
                Left = left;
                Right = right;
                var leftHeight = left is Branch b ? b.Height : 1;
                Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
            }

            internal Branch(int key, V value, int leftHeight, ImMap<V> left, int rightHeight, ImMap<V> right) : base(key, value)
            {
                Left = left;
                Right = right;
                Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
            }

            internal Branch(int key, V value, ImMap<V> left, ImMap<V> right, int height) : base(key, value)
            {
                Left = left;
                Right = right;
                Height = height;
            }
        }

        /// 
        public ImMap<V> AddOrUpdate(int key, V value)
        {
            var mapKey = Key;

            var b = this as Branch;
            if (b == null) // means the leaf node
            {
                // update the leaf
                if (mapKey == key)
                    return new ImMap<V>(key, value);

                return key < mapKey // search for node
                    ? new Branch(mapKey, Value, new ImMap<V>(key, value), Empty, 2)
                    : new Branch(mapKey, Value, Empty, new ImMap<V>(key, value), 2);
            }

            // the empty branch node
            var height = b.Height;
            if (height == 0)
                return new ImMap<V>(key, value);

            // update the branch key and value
            var left = b.Left;
            var right = b.Right;

            if (mapKey == key)
                return new Branch(key, value, left, right, height);

            if (key < mapKey)
                left = left.AddOrUpdate(key, value);
            else
                right = right.AddOrUpdate(key, value);

            // Now balance!!!
            return ImMap.Balance(mapKey, Value, left, right);
        }

        /// <summary>Returns new tree with added or updated value for specified key.</summary>
        /// <param name="key">Key</param> <param name="value">Value</param>
        /// <param name="updateValue">(optional) Delegate to calculate new value from and old and a new value.</param>
        /// <returns>New tree.</returns>
        public ImMap<V> AddOrUpdate(int key, V value, Update<V> updateValue) =>
            AddOrUpdateImpl(key, value, false, updateValue);

        /// <summary>Returns new tree with updated value for the key, Or the same tree if key was not found.</summary>
        /// <param name="key"></param> <param name="value"></param>
        /// <returns>New tree if key is found, or the same tree otherwise.</returns>
        public ImMap<V> Update(int key, V value) =>
            AddOrUpdateImpl(key, value, true, null);

        /// <summary>Returns all sub-trees enumerated from left to right.</summary> 
        /// <returns>Enumerated sub-trees or empty if tree is empty.</returns>
        public IEnumerable<ImMap<V>> Enumerate()
        {
            if (IsEmpty)
                yield break;

            var parents = new ImMap<V>[this.GetHeight()];

            var node = this;
            var parentCount = -1;
            while (!node.IsEmpty || parentCount != -1)
            {
                if (!node.IsEmpty)
                {
                    parents[++parentCount] = node;
                    node = node.GetLeft();
                }
                else
                {
                    node = parents[parentCount--];
                    yield return node;
                    node = node.GetRight();
                }
            }
        }

        /// <summary>Removes or updates value for specified key, or does nothing if key is not found.
        /// Based on Eric Lippert http://blogs.msdn.com/b/ericlippert/archive/2008/01/21/immutability-in-c-part-nine-academic-plus-my-avl-tree-implementation.aspx </summary>
        /// <param name="key">Key to look for.</param> 
        /// <returns>New tree with removed or updated value.</returns>
        public ImMap<V> Remove(int key) =>
            RemoveImpl(key);

        /// <summary>Outputs key value pair</summary>
        public override string ToString() => IsEmpty ? "empty" : (Key + ":" + Value);

        #region Implementation

        private ImMap() { }

        internal ImMap(int key, V value)
        {
            Key = key;
            Value = value;
        }

        private static ImMap<V> BranchOrLeaf(int key, V value, ImMap<V> left, ImMap<V> right)
        {
            if (left == Empty && right == Empty)
                return new ImMap<V>(key, value);
            return new Branch(key, value, left, right);
        }

        private ImMap<V> AddOrUpdateImpl(int key, V value)
        {
            var height = this.GetHeight();
            return height == 0  // add new node
                ? new ImMap<V>(key, value)
                : (key == Key // update found node
                    ? new Branch(key, value, this.GetLeft(), this.GetRight(), height)
                    : (key < Key  // search for node
                        ? (height == 1
                            ? new Branch(Key, Value, new ImMap<V>(key, value), this.GetRight(), height: 2)
                            : Balance(Key, Value, this.GetLeft().AddOrUpdateImpl(key, value), this.GetRight()))
                        : (height == 1
                            ? new Branch(Key, Value, this.GetLeft(), new ImMap<V>(key, value), height: 2)
                            : Balance(Key, Value, this.GetLeft(), this.GetRight().AddOrUpdateImpl(key, value)))));
        }

        private ImMap<V> AddOrUpdateImpl(int key, V value, bool updateOnly, Update<V> update)
        {
            var height = this.GetHeight();
            return height == 0 ? // tree is empty
                (updateOnly ? this : new ImMap<V>(key, value))
                : (key == Key ? // actual update
                    new Branch(key, update == null ? value : update(Value, value), this.GetLeft(), this.GetRight(), height)
                    : (key < Key    // try update on left or right sub-tree
                        ? Balance(Key, Value, this.GetLeft().AddOrUpdateImpl(key, value, updateOnly, update), this.GetRight())
                        : Balance(Key, Value, this.GetLeft(), this.GetRight().AddOrUpdateImpl(key, value, updateOnly, update))));
        }

        private static ImMap<V> Balance(int key, V value, ImMap<V> left, ImMap<V> right)
        {
            var delta = left.GetHeight() - right.GetHeight();
            if (delta >= 2) // left is longer by 2, rotate left
            {
                var leftLeft = left.GetLeft();
                var leftRight = left.GetRight();
                if (leftRight.GetHeight() - leftLeft.GetHeight() == 1)
                {
                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1
                    return new Branch(leftRight.Key, leftRight.Value,
                        BranchOrLeaf(left.Key, left.Value, leftLeft, leftRight.GetLeft()),
                        BranchOrLeaf(key, value, leftRight.GetRight(), right));
                }

                // todo: do we need this?
                // one rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                return new Branch(left.Key, left.Value, leftLeft, BranchOrLeaf(key, value, leftRight, right));
            }

            if (delta <= -2)
            {
                var rightLeft = right.GetLeft();
                var rightRight = right.GetRight();
                if (rightLeft.GetHeight() - rightRight.GetHeight() == 1)
                {
                    return new Branch(rightLeft.Key, rightLeft.Value,
                        BranchOrLeaf(key, value, left, rightLeft.GetLeft()),
                        BranchOrLeaf(right.Key, right.Value, rightLeft.GetRight(), rightRight));
                }

                return new Branch(right.Key, right.Value, BranchOrLeaf(key, value, left, rightLeft), rightRight);
            }

            return new Branch(key, value, left, right);
        }

        private ImMap<V> RemoveImpl(int key, bool ignoreKey = false)
        {
            if (IsEmpty)
                return this;

            ImMap<V> result;
            if (key == Key || ignoreKey) // found node
            {
                if (this.GetHeight() == 1) // remove node
                    return Empty;

                if (this.GetRight().GetHeight() == 0)
                    result = this.GetLeft();
                else if (this.GetLeft().GetHeight() == 0)
                    result = this.GetRight();
                else
                {
                    // we have two children, so remove the next highest node and replace this node with it.
                    var successor = this.GetRight();
                    while (successor.GetLeft().GetHeight() != 0)
                        successor = successor.GetLeft();

                    result = BranchOrLeaf(successor.Key, successor.Value,
                        this.GetLeft(), this.GetRight().RemoveImpl(successor.Key, true));
                }
            }
            else if (key < Key)
                result = Balance(Key, Value, this.GetLeft().RemoveImpl(key), this.GetRight());
            else
                result = Balance(Key, Value, this.GetLeft(), this.GetRight().RemoveImpl(key));

            return result;
        }

        #endregion
    }

    /// Map methods
    public static class ImMap
    {
        /// Left sub-tree/branch, or empty.
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> GetLeft<V>(this ImMap<V> map) => map is ImMap<V>.Branch b ? b.Left : ImMap<V>.Empty;

        /// Right sub-tree/branch, or empty.
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> GetRight<V>(this ImMap<V> map) => map is ImMap<V>.Branch b ? b.Right : ImMap<V>.Empty;

        /// Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.
        [MethodImpl((MethodImplOptions)256)]
        public static int GetHeight<V>(this ImMap<V> map) => map is ImMap<V>.Branch b ? b.Height : 1;

        /// Get value for found key or default value otherwise.
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap<V> map, int key, V defaultValue = default(V))
        {
            int mapKey;
            var empty = ImMap<V>.Empty;
            while (map != empty)
            {
                if ((mapKey = map.Key) == key)
                    return map.Value;

                var br = map as ImMap<V>.Branch;
                if (br == null)
                    break;

                map = key < mapKey ? br.Left : br.Right;
            }
            return defaultValue;
        }

        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMap<V> map, int key, out V value)
        {
            int mapKey;
            var empty = ImMap<V>.Empty;
            while (map != empty)
            {
                if ((mapKey = map.Key) == key)
                {
                    value = map.Value;
                    return true;
                }

                var br = map as ImMap<V>.Branch;
                if (br == null)
                    break;
                map = key < mapKey ? br.Left : br.Right;
            }

            value = default(V);
            return false;
        }

        ///Returns new tree with added or updated value for specified key.
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value)
        {
            var mapKey = map.Key;

            var b = map as ImMap<V>.Branch;
            if (b == null) // means the leaf node
            {
                // update the leaf
                if (mapKey == key)
                    return new ImMap<V>(key, value);

                return key < mapKey // search for node
                    ? new ImMap<V>.Branch(mapKey, map.Value, new ImMap<V>(key, value), ImMap<V>.Empty, 2)
                    : new ImMap<V>.Branch(mapKey, map.Value, ImMap<V>.Empty, new ImMap<V>(key, value), 2);
            }

            // the empty branch node
            var height = b.Height;
            if (height == 0)
                return new ImMap<V>(key, value);

            // update the branch key and value
            var left = b.Left;
            var right = b.Right;

            if (mapKey == key)
                return new ImMap<V>.Branch(key, value, left, right, height);

            if (key < mapKey)
                left = left.AddOrUpdate(key, value);
            else
                right = right.AddOrUpdate(key, value);

            // Now balance!!!
            return Balance(mapKey, map.Value, left, right);
        }

        internal static ImMap<V> Balance<V>(int key, V value, ImMap<V> left, ImMap<V> right)
        {
            var lb = left as ImMap<V>.Branch;
            var rb = right as ImMap<V>.Branch;

            // both left and right are leaf nodes, no need to balance
            if (lb == null && rb == null)
                return new ImMap<V>.Branch(key, value, left, right, 2);

            var lHeight = lb?.Height ?? 1;
            var rHeight = rb?.Height ?? 1;
            var delta = lHeight - rHeight;

            // Left is longer by 2 - rotate left.
            // Also means left is not a leaf or empty - should be a branch!
            if (delta > 1)
            {
                var empty = ImMap<V>.Empty;

                // ReSharper disable once PossibleNullReferenceException
                var leftLeft = lb.Left;
                var leftRight = lb.Right;

                var lrb = leftRight as ImMap<V>.Branch;
                var lrHeight = lrb?.Height ?? 1;

                var llb = leftLeft as ImMap<V>.Branch;
                var llHeight = llb?.Height ?? 1;

                // That also means the `leftRight` is the Leaf or Branch, but not empty.
                if (lrHeight > llHeight)
                {
                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1

                    // Means that `lrb` is not empty branch, so its `height >= 2`.
                    if (lrb != null)
                        return new ImMap<V>.Branch(lrb.Key, lrb.Value,
                            llHeight == 0 && lrb.Left == empty
                                ? new ImMap<V>(left.Key, left.Value)
                                : new ImMap<V>.Branch(left.Key, left.Value, llHeight, leftLeft, lrb.Left),
                            lrb.Right == empty && rHeight == 0
                                ? new ImMap<V>(key, value)
                                : new ImMap<V>.Branch(key, value, lrb.Right, rHeight, right));

                    // Means that `leftRight` is the leaf, so its left and right may be considered empty.
                    // In that case `leftLeft` should be empty.
                    return new ImMap<V>.Branch(leftRight.Key, leftRight.Value,
                        new ImMap<V>(left.Key, left.Value), // height: 1, so the right branch may either 1 or 2
                        rHeight == 0
                            ? new ImMap<V>(key, value)
                            : new ImMap<V>.Branch(key, value, empty, right, 2),
                        rHeight == 0 ? 2 : 3);
                }

                // single rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                if (lrHeight == 0 && rHeight == 0)
                    return new ImMap<V>.Branch(left.Key, left.Value,
                        llHeight, leftLeft, 1, new ImMap<V>(key, value));

                rb = new ImMap<V>.Branch(key, value, lrHeight, leftRight, rHeight, right);
                return new ImMap<V>.Branch(left.Key, left.Value, llHeight, leftLeft, rb.Height, rb);
            }

            // right is longer than left by 2, so it may be only the branch node
            if (delta < -1)
            {
                var empty = ImMap<V>.Empty;

                // ReSharper disable once PossibleNullReferenceException
                var rightLeft = rb.Left;
                var rightRight = rb.Right;

                var rlb = rightLeft as ImMap<V>.Branch;
                var rlHeight = rlb?.Height ?? 1;
                var rrb = rightRight as ImMap<V>.Branch;
                var rrHeight = rrb?.Height ?? 1;

                if (rlHeight > rrHeight)
                {
                    // `rlb` is the non empty branch node
                    if (rlb != null)
                        return new ImMap<V>.Branch(rlb.Key, rlb.Value,
                            lHeight == 0 && rlb.Left == empty
                                ? new ImMap<V>(key, value)
                                : new ImMap<V>.Branch(key, value, lHeight, left, rlb.Left),
                            rlb.Right == empty && rrHeight == 0
                                ? new ImMap<V>(right.Key, right.Value)
                                : new ImMap<V>.Branch(right.Key, right.Value, rlb.Right, rrHeight, rightRight));

                    // `rightLeft` is the leaf node, means its left and right may be considered empty
                    // then the `rightRight` should be empty
                    return new ImMap<V>.Branch(rightLeft.Key, rightLeft.Value,
                        lHeight == 0
                            ? new ImMap<V>(key, value)
                            : new ImMap<V>.Branch(key, value, left, empty, 2),
                        new ImMap<V>(right.Key, right.Value),
                        lHeight == 0 ? 2 : 3);
                }

                if (lHeight == 0 && rlHeight == 0)
                    return new ImMap<V>.Branch(right.Key, right.Value,
                        1, new ImMap<V>(key, value), rrHeight, rightRight);

                lb = new ImMap<V>.Branch(key, value, lHeight, left, rlHeight, rightLeft);
                return new ImMap<V>.Branch(right.Key, right.Value, lb.Height, lb, rrHeight, rightRight);
            }

            return new ImMap<V>.Branch(key, value, lHeight, left, rHeight, right);
        }
    }
}
