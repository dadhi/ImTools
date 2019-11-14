using System.Runtime.CompilerServices;

namespace ImTools.Benchmarks.ImMapFixedData4
{
    /// Marker interface to denote the map
    public interface ImMap<V> 
    {
        /// Height of the longest sub-tree/branch. Starts from 2 because it a tree and not the leaf
        int Height { get; }
    }

    /// The empty tree
    public sealed class ImMapEmpty<V> : ImMap<V>
    {
        /// Empty tree to start with.
        public static readonly ImMap<V> Empty = new ImMapEmpty<V>();
        private ImMapEmpty() { }

        /// <inheritdoc />
        public int Height => 0;

        /// Prints "empty"
        public override string ToString() => "empty";
    }

    /// The leaf node - just the key-value pair
    public sealed class ImMapLeaf<V> : ImMap<V>
    {
        /// <inheritdoc />
        public int Height => 1;

        /// The Key is basically the hash, or the Height for ImMapTree
        public readonly int Key;

        /// The value - may be modified if you need a Ref{V} semantics
        public V Value;

        public ImMapLeaf(int key, V value)
        {
            Key = key;
            Value = value;
        }

        /// Prints the key value pair
        public override string ToString() => Key + ":" + Value;
    }

    /// <summary>
    /// Immutable http://en.wikipedia.org/wiki/AVL_tree with integer keys and <typeparamref name="V"/> values.
    /// </summary>
    public sealed class ImMapTree<V> : ImMap<V>
    {
        /// Starts from 2
        int ImMap<V>.Height => Height;

        /// Starts from 2 - allows to access the field directly when you know it is a Tree
        public int Height;

        /// Contains the once created data node
        public readonly ImMapLeaf<V> Data;

        /// Left sub-tree/branch, or empty.
        public ImMap<V> Left;

        /// Right sub-tree/branch, or empty.
        public ImMap<V> Right;

        internal ImMapTree(ImMapLeaf<V> data, ImMap<V> left, ImMap<V> right, int height)
        {
            Data = data;
            Left = left;
            Right = right;
            Height = height;
        }

        internal ImMapTree(ImMapLeaf<V> data, int leftHeight, ImMap<V> left, int rightHeight, ImMap<V> right) 
        {
            Data = data;
            Left = left;
            Right = right;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapLeaf<V> data, int leftHeight, ImMap<V> left, ImMap<V> right) 
        {
            Data = data;
            Left = left;
            Right = right;
            var rightHeight = right.Height;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapLeaf<V> data, ImMap<V> left, int rightHeight, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            var leftHeight = left.Height;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapLeaf<V> data, ImMap<V> left, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            var leftHeight = left.Height;
            var rightHeight = right.Height;
            Height = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        /// Outputs the key value pair
        public override string ToString() => "tree(" + Data + ")";

        private static readonly ImMap<V> Empty = ImMapEmpty<V>.Empty;

        // Adds or updates the left or right branch
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Data.Key)
            {
                var left = Left;
                if (left is ImMapEmpty<V>)
                    return new ImMapTree<V>(Data, new ImMapLeaf<V>(key, value), Right, 2);

                if (left is ImMapLeaf<V> leftLeaf)
                    return key < leftLeaf.Key
                            ? Right is ImMapEmpty<V>
                                ? new ImMapTree<V>(leftLeaf, new ImMapLeaf<V>(key, value), Data, 2)
                                : new ImMapTree<V>(Data, 2, new ImMapTree<V>(leftLeaf, new ImMapLeaf<V>(key, value), Empty, 2), Right)
                        : key > leftLeaf.Key 
                            ? Right is ImMapEmpty<V>
                                ? new ImMapTree<V>(new ImMapLeaf<V>(key, value), left, Data, 2)
                                : new ImMapTree<V>(Data, 2, new ImMapTree<V>(leftLeaf, Empty, new ImMapLeaf<V>(key, value), 2), Right)
                        : new ImMapTree<V>(Data, new ImMapLeaf<V>(key, value), Right, Height);

                var leftTree = (ImMapTree<V>)left;
                if (key == leftTree.Data.Key)
                    return new ImMapTree<V>(Data,
                        new ImMapTree<V>(new ImMapLeaf<V>(key, value), leftTree.Left, leftTree.Right, leftTree.Height),
                        Right, Height);

                var newLeftTree = leftTree.AddOrUpdateLeftOrRight(key, value);
                if (newLeftTree.Height == leftTree.Height)
                    return new ImMapTree<V>(Data, newLeftTree, Right, Height);

                var rightHeight = Right.Height;
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
                                return new ImMapTree<V>(newLeftTree.Data, leftLeftTree.Height, leftLeft,
                                    new ImMapTree<V>(Data, leftRightTree.Height, leftRight, rightHeight, Right));

                            return new ImMapTree<V>(leftRightTree.Data,
                                new ImMapTree<V>(newLeftTree.Data, leftLeftTree.Height, leftLeft, leftRightTree.Left),
                                new ImMapTree<V>(Data, leftRightTree.Right, rightHeight, Right));
                        }

                        // `leftLeft` is tree and `leftRight` is leaf - do a single rotation
                        return new ImMapTree<V>(newLeftTree.Data, leftLeftTree.Height, leftLeftTree,
                            new ImMapTree<V>(Data, 1, leftRight, rightHeight, Right));
                    }

                    // `leftLeft` is leaf and `leftRight` is the tree - do a double rotation
                    // ReSharper disable once PossibleNullReferenceException
                    return new ImMapTree<V>(leftRightTree.Data,
                        new ImMapTree<V>(newLeftTree.Data, 1, leftLeft, leftRightTree.Left),
                        new ImMapTree<V>(Data, leftRightTree.Right, rightHeight, Right));
                }

                return new ImMapTree<V>(Data, newLeftTree.Height, newLeftTree, rightHeight, Right);
            }
            else
            {
                var right = Right;
                if (right is ImMapEmpty<V>)
                    return new ImMapTree<V>(Data, Left, new ImMapLeaf<V>(key, value), 2);

                if (right is ImMapLeaf<V> rightLeaf)
                    return key > rightLeaf.Key 
                            ? Left is ImMapEmpty<V>
                                ? new ImMapTree<V>(rightLeaf, Data, new ImMapLeaf<V>(key, value), 2)
                                : new ImMapTree<V>(Data, Left, 2, new ImMapTree<V>(rightLeaf, Empty, new ImMapLeaf<V>(key, value), 2))
                        : key < rightLeaf.Key
                            ? Left is ImMapEmpty<V>
                                ? new ImMapTree<V>(new ImMapLeaf<V>(key, value), Data, right, 2)
                                : new ImMapTree<V>(Data, Left,2, new ImMapTree<V>(rightLeaf, new ImMapLeaf<V>(key, value), Empty, 2))
                        : new ImMapTree<V>(Data, Left, new ImMapLeaf<V>(key, value), Height);

                var rightTree = (ImMapTree<V>)right;
                if (key == rightTree.Data.Key)
                    return new ImMapTree<V>(Data, Left,
                        new ImMapTree<V>(new ImMapLeaf<V>(key, value), rightTree.Left, rightTree.Right, rightTree.Height),
                        Height);

                var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, value);
                if (newRightTree.Height == rightTree.Height)
                    return new ImMapTree<V>(Data, Left, newRightTree, Height);

                var leftHeight = Left.Height;
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
                                rightLeftTree = new ImMapTree<V>(Data, leftHeight, Left, rightLeftTree.Height, rightLeft);
                                newRightTree.Left = rightLeftTree;
                                newRightTree.Height = rightLeftTree.Height > rightRightTree.Height ? rightLeftTree.Height + 1 : rightRightTree.Height + 1;
                                return newRightTree;
                                //return new ImMapTree<V>(newRightTree.Key, newRightTree.ValueOrData,
                                //    new ImMapTree<V>(Key, ValueOrData, leftHeight, Left, rightLeftTree.Height, rightLeft),
                                //    rightRightTree.Height, rightRight);
                            }

                            //return new ImMapTree<V>(rightLeftTree.Data,
                            //    new ImMapTree<V>(Data, leftHeight, Left, rightLeftTree.Left),
                            //    new ImMapTree<V>(newRightTree.Data, rightLeftTree.Right, rightRightTree.Height, rightRight));
                        }

                        // right-left is tree and right-right is leaf - use a double rotation
                        newRightTree.Left = rightLeftTree.Right;
                        var newRightLeftHeight = newRightTree.Left.Height;
                        var newRightRightHeight = rightRight.Height;
                        newRightTree.Height = newRightLeftHeight > newRightRightHeight ? newRightLeftHeight + 1 : newRightRightHeight + 1;

                        return new ImMapTree<V>(rightLeftTree.Data,
                            new ImMapTree<V>(Data, leftHeight, Left, rightLeftTree.Left),
                            newRightTree.Height, newRightTree);
                        //return new ImMapTree<V>(rightLeftTree.Data,
                        //    new ImMapTree<V>(Data, leftHeight, Left, rightLeftTree.Left),
                        //    new ImMapTree<V>(newRightTree.Data, rightLeftTree.Right, rightRight));
                    }

                    // `rightLeft` is leaf node and `rightRight` is the tree - use a single rotation
                    rightLeftTree = new ImMapTree<V>(Data, leftHeight, Left, 1, rightLeft);
                    newRightTree.Left = rightLeftTree;
                    // ReSharper disable once PossibleNullReferenceException
                    newRightTree.Height = rightLeftTree.Height > rightRightTree.Height ? rightLeftTree.Height + 1 : rightRightTree.Height + 1;
                    return newRightTree;

                    //return new ImMapTree<V>(newRightTree.Data,
                    //    new ImMapTree<V>(Data, leftHeight, Left, 1, rightLeft),
                    //    rightRightTree.Height, rightRight);
                }

                return new ImMapTree<V>(Data, leftHeight, Left, newRightTree.Height, newRightTree);
            } 
        }
    }

    /// ImMap static methods
    public static class ImMap
    {
        // todo: try switch expression
        /// Adds or updates the value by key in the map, always returns a modified map
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value) =>
            map is ImMapTree<V> tree
                ? key == tree.Data.Key
                    ? new ImMapTree<V>(new ImMapLeaf<V>(key, value), tree.Left, tree.Right, tree.Height)
                    : tree.AddOrUpdateLeftOrRight(key, value)
            : map is ImMapLeaf<V> leaf
                ? key > leaf.Key
                    ? new ImMapTree<V>(leaf, ImMapEmpty<V>.Empty, new ImMapLeaf<V>(key, value), 2)
                    : key < leaf.Key
                        ? new ImMapTree<V>(leaf, new ImMapLeaf<V>(key, value), ImMapEmpty<V>.Empty, 2)
                        : (ImMap<V>)new ImMapLeaf<V>(key, value)
            : new ImMapLeaf<V>(key, value);

        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMap<V> map, int key, out V value)
        {
            int mapKey;
            while (map is ImMapTree<V> mapTree)
            {
                mapKey = mapTree.Data.Key;
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

            if (map is ImMapLeaf<V> leaf && leaf.Key == key)
            {
                value = leaf.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
