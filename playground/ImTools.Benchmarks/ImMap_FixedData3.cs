using System.Runtime.CompilerServices;

namespace ImTools.Benchmarks.ImMapFixedData3
{
    public class ImMap<V>
    {
        /// Empty tree to start with.
        public static readonly ImMap<V> Empty = new ImMap<V>();
        private ImMap() { }

        /// The Key is basically the hash
        public readonly int Key;

        /// The value - may be modified if you need a Ref{V} semantics
        public V Value;

        public ImMap(int key, V value)
        {
            Key = key;
            Value = value;
        }

        /// Outputs the key value pair
        public override string ToString() =>
            ReferenceEquals(this, Empty) ? "empty" : Key + ":" + Value;

        /// Adds or updates the value by key in the map, always returns a modified map
        [MethodImpl((MethodImplOptions)256)]
        public ImMap<V> AddOrUpdate(int key, V value) =>
            ReferenceEquals(this, Empty)
                ? new ImMap<V>(key, value)
                : this is ImMapTree<V> tree ?
                    (key == Key 
                        ? new ImMapTree<V>(key, new ImMap<V>(key, value), tree.Left, tree.Right, tree.Height)
                        : tree.AddOrUpdateLeftOrRight(key, value)) 
                : key > Key ? new ImMapTree<V>(Key, this, Empty, new ImMap<V>(key, value), 2) 
                : key < Key ? new ImMapTree<V>(Key, this, new ImMap<V>(key, value), Empty, 2)
                : new ImMap<V>(key, value);
    }

    /// <summary>
    /// Immutable http://en.wikipedia.org/wiki/AVL_tree with integer keys and <typeparamref name="V"/> values.
    /// The value field in the tree is ALWAYS contains the default value and should be IGNORED - because it is required by inheritance to have the one
    /// </summary>
    public sealed class ImMapTree<V> : ImMap<V>
    {
        /// Contains the once created data node
        public readonly ImMap<V> Data;

        /// Left sub-tree/branch, or empty.
        public ImMap<V> Left;

        /// Right sub-tree/branch, or empty.
        public ImMap<V> Right;

        /// Height of the longest sub-tree/branch. Starts from 2 because it a tree and not the leaf
        public int Height;

        internal ImMapTree(int key, ImMap<V> data, ImMap<V> left, ImMap<V> right, int height) : base(key, default)
        {
            Data = data;
            Left = left;
            Right = right;
            Height = height;
        }

        internal ImMapTree(int key, ImMap<V> data, int leftHeight, ImMap<V> left, int rightHeight, ImMap<V> right) : base(key, default)
        {
            Data = data;
            Left = left;
            Right = right;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(int key, ImMap<V> data, int leftHeight, ImMap<V> left, ImMap<V> right) : base(key, default)
        {
            Data = data;
            Left = left;
            Right = right;
            var rightHeight = (right as ImMapTree<V>)?.Height ?? 1;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(int key, ImMap<V> data, ImMap<V> left, int rightHeight, ImMap<V> right) : base(key, default)
        {
            Data = data;
            Left = left;
            Right = right;
            var leftHeight = (left as ImMapTree<V>)?.Height ?? 1;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(int key, ImMap<V> data, ImMap<V> left, ImMap<V> right) : base(key, default)
        {
            Data = data;
            Left = left;
            Right = right;
            var leftHeight = (left as ImMapTree<V>)?.Height ?? 1;
            var rightHeight = (right as ImMapTree<V>)?.Height ?? 1;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        /// Outputs the key value pair
        public override string ToString() =>
            ReferenceEquals(this, Empty) ? "empty" : Key + ":" + Data;

        // subj
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Key)
            {
                if (ReferenceEquals(Left, Empty))
                    return new ImMapTree<V>(Key, Data, new ImMap<V>(key, value), Right, 2);

                var left = Left;
                var leftTree = left as ImMapTree<V>;
                if (leftTree == null) // left is the leaf
                    return key == left.Key
                        ? new ImMapTree<V>(Key, Data, new ImMap<V>(key, value), Right, Height)
                        : key < left.Key
                            ? (ReferenceEquals(Right, Empty)
                                ? new ImMapTree<V>(left.Key, left, new ImMap<V>(key, value), Data, 2)
                                : new ImMapTree<V>(Key, Data,
                                    2, new ImMapTree<V>(left.Key, left, new ImMap<V>(key, value), Empty, 2), Right))
                            : (ReferenceEquals(Right, Empty)
                                ? new ImMapTree<V>(Key, Data,
                                    2, new ImMapTree<V>(left.Key, left, Empty, new ImMap<V>(key, value), 2), Right)
                                : new ImMapTree<V>(key, new ImMap<V>(key, value), left, Data, 2));

                if (key == left.Key)
                    return new ImMapTree<V>(Key, Data,
                        new ImMapTree<V>(key, new ImMap<V>(key, value), leftTree.Left, leftTree.Right, leftTree.Height),
                        Right, Height);

                var newLeftTree = leftTree.AddOrUpdateLeftOrRight(key, value);
                if (newLeftTree.Height == leftTree.Height)
                    return new ImMapTree<V>(Key, Data, newLeftTree, Right, Height);

                var rightTree = Right as ImMapTree<V>;
                var rightHeight = rightTree?.Height ?? 1;
                if (newLeftTree.Height - 1 > rightHeight)
                {
                    // 1st fact - `leftLeft` and `leftRight` cannot be Empty otherwise we won't need to re-balance the left tree
                    // 2nd fact - either lefLeft or leftRight or both should be a tree

                    var leftLeft = newLeftTree.Left;
                    var leftLeftTree = leftLeft as ImMapTree<V>;
                    var leftRight = newLeftTree.Right;
                    var leftRightTree = leftRight as ImMapTree<V>;
                    if (leftLeftTree != null)
                    {
                        if (leftRightTree != null)
                        {
                            if (leftLeftTree.Height >= leftRightTree.Height)
                                return new ImMapTree<V>(newLeftTree.Key, newLeftTree.Data,
                                    leftLeftTree.Height, leftLeftTree,
                                    new ImMapTree<V>(Key, Data, leftRightTree.Height, leftRightTree, rightHeight, Right));

                            return new ImMapTree<V>(leftRightTree.Key, leftRightTree.Data,
                                new ImMapTree<V>(newLeftTree.Key, newLeftTree.Data, leftLeftTree.Height, leftLeftTree, leftRightTree.Left),
                                new ImMapTree<V>(Key, Data, leftRightTree.Right, rightHeight, Right));
                        }

                        // `leftLeft` is tree and `leftRight` is leaf - do a single rotation
                        return new ImMapTree<V>(newLeftTree.Key, newLeftTree.Data,
                            leftLeftTree.Height, leftLeftTree,
                            new ImMapTree<V>(Key, Data, 1, leftRight, rightHeight, Right));
                    }

                    // `leftLeft` is leaf and `leftRight` is the tree - do a double rotation
                    // ReSharper disable once PossibleNullReferenceException
                    return new ImMapTree<V>(leftRightTree.Key, leftRightTree.Data,
                        new ImMapTree<V>(newLeftTree.Key, newLeftTree.Data, 1, leftLeft, leftRightTree.Left),
                        new ImMapTree<V>(Key, Data, leftRightTree.Right, rightHeight, Right));
                }

                return new ImMapTree<V>(Key, Data, newLeftTree.Height, newLeftTree, rightHeight, Right);
            }
            else
            {
                if (ReferenceEquals(Right, Empty))
                    return new ImMapTree<V>(Key, Data, Left, new ImMap<V>(key, value), 2);

                var right = Right;
                var rightTree = right as ImMapTree<V>;
                if (rightTree == null) // the leaf
                    return key == right.Key
                        ? new ImMapTree<V>(Key, Data, Left, new ImMap<V>(key, value), Height)
                        : key < right.Key
                        ? (ReferenceEquals(Left, Empty)
                            ? new ImMapTree<V>(key, new ImMap<V>(key, value), Data, right, 2)
                            : new ImMapTree<V>(Key, Data, Left,
                                2, new ImMapTree<V>(right.Key, right, new ImMap<V>(key, value), Empty, 2)))
                        : (ReferenceEquals(Left, Empty) 
                            ? new ImMapTree<V>(right.Key, right, Data, new ImMap<V>(key, value), 2)
                            : new ImMapTree<V>(Key, Data, Left,
                                2, new ImMapTree<V>(right.Key, right, Empty, new ImMap<V>(key, value), 2)));

                if (key == right.Key)
                    return new ImMapTree<V>(Key, Data, Left,
                        new ImMapTree<V>(key, new ImMap<V>(key, value), rightTree.Left, rightTree.Right, rightTree.Height),
                        Height);

                var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, value);
                if (newRightTree.Height == rightTree.Height)
                    return new ImMapTree<V>(Key, Data, Left, newRightTree, Height);

                var leftHeight = Left is ImMapTree<V> lt ? lt.Height : 1;
                if (newRightTree.Height - 1 > leftHeight)
                {
                    var rightLeft = newRightTree.Left;
                    var rightLeftTree = rightLeft as ImMapTree<V>;
                    var rightRight = newRightTree.Right;
                    var rightRightTree = rightRight as ImMapTree<V>;
                    if (rightLeftTree != null)
                    {
                        if (rightRightTree != null)
                        {
                            if (rightRightTree.Height >= rightLeftTree.Height)
                            {
                                rightLeftTree = new ImMapTree<V>(Key, Data, leftHeight, Left, rightLeftTree.Height, rightLeft);
                                newRightTree.Left = rightLeftTree;
                                newRightTree.Height = rightLeftTree.Height > rightRightTree.Height ? rightLeftTree.Height + 1 : rightRightTree.Height + 1;
                                return newRightTree;
                                //return new ImMapTree<V>(newRightTree.Key, newRightTree.ValueOrData,
                                //    new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, rightLeftTree.Height, rightLeft),
                                //    rightRightTree.Height, rightRight);
                            }

                            //return new ImMapTree<V>(rightLeftTree.Key, rightLeftTree.ValueOrData,
                            //    new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, rightLeftTree.Left),
                            //    new ImMapTree<V>(newRightTree.Key, newRightTree.ValueOrData, rightLeftTree.Right, rightRightTree.Height, rightRight));
                        }

                        // right-left is tree and right-right is leaf - use a double rotation
                        newRightTree.Left = rightLeftTree.Right;
                        var newRightLeftHeight = newRightTree.Left is ImMapTree<V> rl ? rl.Height : 1;
                        var newRightRightHeight = rightRightTree?.Height ?? 1;
                        newRightTree.Height = newRightLeftHeight > newRightRightHeight ? newRightLeftHeight + 1 : newRightRightHeight + 1;

                        return new ImMapTree<V>(rightLeftTree.Key, rightLeftTree.Data,
                            new ImMapTree<V>(Key, Data, leftHeight, Left, rightLeftTree.Left),
                            newRightTree);
                        //return new ImMapTree<V>(rightLeftTree.Key, rightLeftTree.ValueOrData,
                        //    new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, rightLeftTree.Left),
                        //    new ImMapTree<V>(newRightTree.Key, newRightTree.ValueOrData, rightLeftTree.Right, 
                        //        rightRightTree?.Height ?? 1, rightRight));
                    }

                    // `rightLeft` is leaf node and `rightRight` is the tree - use a single rotation
                    rightLeftTree = new ImMapTree<V>(Key, Data, leftHeight, Left, 1, rightLeft);
                    newRightTree.Left = rightLeftTree;
                    // ReSharper disable once PossibleNullReferenceException
                    newRightTree.Height = rightLeftTree.Height > rightRightTree.Height ? rightLeftTree.Height + 1 : rightRightTree.Height + 1;
                    return newRightTree;

                    //return new ImMapTree<V>(newRightTree.Key, newRightTree.ValueOrData,
                    //    new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, 1, rightLeft),
                    //    rightRightTree.Height, rightRight);
                }

                // Height does not changed
                return new ImMapTree<V>(Key, Data, leftHeight, Left, newRightTree.Height, newRightTree);
            } 
        }
    }

    /// ImMap static methods
    public static class ImMap
    {
        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMap<V> map, int key, out V value)
        {
            while (map is ImMapTree<V> mapTree)
            {
                var mapKey = mapTree.Data.Key;
                if (key < mapKey)
                    map = mapTree.Left;
                else if (key > mapKey)
                    map = mapTree.Right;
                else
                {
                    value = mapTree.Data.Value;
                    return true;
                }
            }

            if (!ReferenceEquals(map, ImMap<V>.Empty) && map.Key == key)
            {
                value = map.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
