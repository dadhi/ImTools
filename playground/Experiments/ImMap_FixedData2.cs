using System.Runtime.CompilerServices;

namespace ImTools.Benchmarks.ImMapFixedData2
{
    public class ImMap<V> where V : class
    {
        /// Empty tree to start with.
        public static readonly ImMap<V> Empty = new ImMap<V>();

        /// The Key is basically the hash
        public readonly int Key;

        /// The value or `ImMapData{V}`
        public object ValueOrData;

        protected ImMap() { }

        public ImMap(int key, object valueOrData)
        {
            Key = key;
            ValueOrData = valueOrData;
        }

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

        /// Outputs key value pair
        public override string ToString() => 
            ReferenceEquals(this, Empty) ? "empty" : Key + ":" + ((ValueOrData as ImMapTree<V>)?.ValueOrData ?? ValueOrData);
    }

    /// <summary>Immutable http://en.wikipedia.org/wiki/AVL_tree with integer keys and <typeparamref name="V"/> values.</summary>
    public sealed class ImMapTree<V> : ImMap<V> where V : class
    {
        /// <summary>Left sub-tree/branch, or empty.</summary>
        public ImMap<V> Left;

        /// <summary>Right sub-tree/branch, or empty.</summary>
        public readonly ImMap<V> Right;

        /// <summary>Height of the longest sub-tree/branch. It is 0 for empty tree, and 1 for single node tree.</summary>
        public int Height;

        // subj
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Key)
            {
                if (ReferenceEquals(Left, Empty))
                    return new ImMapTree<V>(Key, ValueOrData, new ImMap<V>(key, value), Right, 2);

                var left = Left;
                var leftTree = left as ImMapTree<V>;
                if (leftTree == null) // left is the leaf
                    return key == left.Key
                        ? new ImMapTree<V>(Key, ValueOrData, new ImMap<V>(key, value), Right, Height)
                        : key < left.Key
                            ? (ReferenceEquals(Right, Empty)
                                ? new ImMapTree<V>(left.Key, left, new ImMap<V>(key, value), (ImMap<V>) ValueOrData, 2)
                                : new ImMapTree<V>(Key, ValueOrData,
                                    2, new ImMapTree<V>(left.Key, left, new ImMap<V>(key, value), Empty, 2), Right))
                            : (ReferenceEquals(Right, Empty)
                                ? new ImMapTree<V>(Key, ValueOrData,
                                    2, new ImMapTree<V>(left.Key, left, Empty, new ImMap<V>(key, value), 2), Right)
                                : new ImMapTree<V>(key, new ImMap<V>(key, value), left, (ImMap<V>)ValueOrData, 2));

                if (key == left.Key)
                    return new ImMapTree<V>(Key, ValueOrData,
                        new ImMapTree<V>(key, new ImMap<V>(key, value), leftTree.Left, leftTree.Right, leftTree.Height),
                        Right, Height);

                var newLeftTree = leftTree.AddOrUpdateLeftOrRight(key, value);
                if (newLeftTree.Height == leftTree.Height)
                    return new ImMapTree<V>(Key, ValueOrData, newLeftTree, Right, Height);

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
                                return new ImMapTree<V>(newLeftTree.Key, newLeftTree.ValueOrData,
                                    leftLeftTree.Height, leftLeftTree,
                                    new ImMapTree<V>(Key, ValueOrData, leftRightTree.Height, leftRightTree, rightHeight, Right));

                            return new ImMapTree<V>(leftRightTree.Key, leftRightTree.ValueOrData,
                                new ImMapTree<V>(newLeftTree.Key, newLeftTree.ValueOrData, leftLeftTree.Height, leftLeftTree, leftRightTree.Left),
                                new ImMapTree<V>(Key, ValueOrData, leftRightTree.Right, rightHeight, Right));
                        }

                        // `leftLeft` is tree and `leftRight` is leaf - do a single rotation
                        return new ImMapTree<V>(newLeftTree.Key, newLeftTree.ValueOrData,
                            leftLeftTree.Height, leftLeftTree,
                            new ImMapTree<V>(Key, ValueOrData, 1, leftRight, rightHeight, Right));
                    }

                    // `leftLeft` is leaf and `leftRight` is the tree - do a double rotation
                    // ReSharper disable once PossibleNullReferenceException
                    return new ImMapTree<V>(leftRightTree.Key, leftRightTree.ValueOrData,
                        new ImMapTree<V>(newLeftTree.Key, newLeftTree.ValueOrData, 1, leftLeft, leftRightTree.Left),
                        new ImMapTree<V>(Key, ValueOrData, leftRightTree.Right, rightHeight, Right));
                }

                return new ImMapTree<V>(Key, ValueOrData, newLeftTree.Height, newLeftTree, rightHeight, Right);
            }
            else
            {
                if (ReferenceEquals(Right, Empty))
                    return new ImMapTree<V>(Key, ValueOrData, Left, new ImMap<V>(key, value), 2);

                var right = Right;
                var rightTree = right as ImMapTree<V>;
                if (rightTree == null) // the leaf
                    return key == right.Key
                        ? new ImMapTree<V>(Key, ValueOrData, Left, new ImMap<V>(key, value), Height)
                        : key < right.Key
                        ? (ReferenceEquals(Left, Empty)
                            ? new ImMapTree<V>(key, new ImMap<V>(key, value), (ImMap<V>)ValueOrData, right, 2)
                            : new ImMapTree<V>(Key, ValueOrData, Left,
                                2, new ImMapTree<V>(right.Key, right, new ImMap<V>(key, value), Empty, 2)))
                        : (ReferenceEquals(Left, Empty) 
                            ? new ImMapTree<V>(right.Key, right, (ImMap<V>)ValueOrData, new ImMap<V>(key, value), 2)
                            : new ImMapTree<V>(Key, ValueOrData, Left,
                                2, new ImMapTree<V>(right.Key, right, Empty, new ImMap<V>(key, value), 2)));

                if (key == right.Key)
                    return new ImMapTree<V>(Key, ValueOrData, Left,
                        new ImMapTree<V>(key, new ImMap<V>(key, value), rightTree.Left, rightTree.Right, rightTree.Height),
                        Height);

                var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, value);
                if (newRightTree.Height == rightTree.Height)
                    return new ImMapTree<V>(Key, ValueOrData, Left, newRightTree, Height);

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
                                rightLeftTree = new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, rightLeftTree.Height, rightLeft);
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

                        return new ImMapTree<V>(rightLeftTree.Key, rightLeftTree.ValueOrData,
                            new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, rightLeftTree.Left),
                            newRightTree);
                        //return new ImMapTree<V>(rightLeftTree.Key, rightLeftTree.ValueOrData,
                        //    new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, rightLeftTree.Left),
                        //    new ImMapTree<V>(newRightTree.Key, newRightTree.ValueOrData, rightLeftTree.Right, 
                        //        rightRightTree?.Height ?? 1, rightRight));
                    }

                    // `rightLeft` is leaf node and `rightRight` is the tree - use a single rotation
                    rightLeftTree = new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, 1, rightLeft);
                    newRightTree.Left = rightLeftTree;
                    // ReSharper disable once PossibleNullReferenceException
                    newRightTree.Height = rightLeftTree.Height > rightRightTree.Height ? rightLeftTree.Height + 1 : rightRightTree.Height + 1;
                    return newRightTree;

                    //return new ImMapTree<V>(newRightTree.Key, newRightTree.ValueOrData,
                    //    new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, 1, rightLeft),
                    //    rightRightTree.Height, rightRight);
                }

                // Height does not changed
                return new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, newRightTree.Height, newRightTree);
            } 
        }

        internal ImMapTree(int key, object data, ImMap<V> left, ImMap<V> right, int height) : base(key, data)
        {
            Left = left;
            Right = right;
            Height = height;
        }

        internal ImMapTree(int key, object data, int leftHeight, ImMap<V> left, int rightHeight, ImMap<V> right) : base(key, data)
        {
            Left = left;
            Right = right;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(int key, object data, int leftHeight, ImMap<V> left, ImMap<V> right) : base(key, data)
        {
            Left  = left;
            Right = right;
            var rightHeight = (right as ImMapTree<V>)?.Height ?? 1;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(int key, object data, ImMap<V> left, int rightHeight, ImMap<V> right) : base(key, data)
        {
            Left = left;
            Right = right;
            var leftHeight = (left as ImMapTree<V>)?.Height ?? 1;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(int key, object data, ImMap<V> left, ImMap<V> right) : base(key, data)
        {
            Left = left;
            Right = right;
            var leftHeight  = (left as ImMapTree<V>)?.Height ?? 1;
            var rightHeight = (right as ImMapTree<V>)?.Height ?? 1;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }
    }

    /// ImMap static methods
    public static class ImMap
    {
        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMap<V> map, int key, out V value) where V : class
        {
            while (map is ImMapTree<V> mapTree)
            {
                if (key < mapTree.Key)
                    map = mapTree.Left;
                else if (key > mapTree.Key)
                    map = mapTree.Right;
                else
                {
                    value = (V)((ImMap<V>)mapTree.ValueOrData).ValueOrData;
                    return true;
                }
            }

            if (!ReferenceEquals(map, ImMap<V>.Empty) && map.Key == key)
            {
                value = (V)map.ValueOrData;
                return true;
            }

            value = null;
            return false;
        }
    }
}
