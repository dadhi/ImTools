using System.Collections.Generic;
using System.Runtime.CompilerServices; // For [MethodImpl(AggressiveInlining)]

namespace ImTools.Experimental
{
    /// Empty ImMap to start with
    public class ImMap<V>
    {
        /// Empty tree to start with.
        public static readonly ImMap<V> Empty = new ImMap<V>();

        /// Height of the longest sub-tree/branch. Starts from 2 because it a tree and not the leaf
        public virtual int Height => 0;

        /// Prints "empty"
        public override string ToString() => "empty";
    }

    /// The leaf node - just the key-value pair
    public sealed class ImMapLeaf<V> : ImMap<V>
    {
        /// <inheritdoc />
        public override int Height => 1;

        /// The Key is basically the hash, or the Height for ImMapTree
        public readonly int Key;

        /// The value - may be modified if you need a Ref{V} semantics
        public V Value;

        /// Creates the leaf
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
        public override int Height => TreeHeight;

        /// Starts from 2 - allows to access the field directly when you know it is a Tree
        public int TreeHeight;

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
            TreeHeight = height;
        }

        internal ImMapTree(ImMapLeaf<V> data, int leftHeight, ImMap<V> left, int rightHeight, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapLeaf<V> data, int leftHeight, ImMap<V> left, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            var rightHeight = right.Height;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapLeaf<V> data, ImMap<V> left, int rightHeight, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            var leftHeight = left.Height;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapLeaf<V> data, ImMapTree<V> left, ImMapTree<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            TreeHeight = left.TreeHeight > right.TreeHeight ? left.TreeHeight + 1 : right.TreeHeight + 1;
        }

        /// Outputs the key value pair
        public override string ToString() => "tree(" + Data + ")";

        /// Adds or updates the left or right branch
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Data.Key)
            {
                var left = Left;
                if (left is ImMapLeaf<V> leftLeaf)
                {
                    if (key < leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(leftLeaf, new ImMapLeaf<V>(key, value), Data, 2)
                            : new ImMapTree<V>(Data, 
                                new ImMapTree<V>(leftLeaf, new ImMapLeaf<V>(key, value), Empty, 2), 
                                Right, 3); // given that left is the leaf, the Right tree should be less than 2 - otherwise tree is unbalanced

                    if (key > leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(new ImMapLeaf<V>(key, value), left, Data, 2)
                            : new ImMapTree<V>(Data,
                                new ImMapTree<V>(leftLeaf, Empty, new ImMapLeaf<V>(key, value), 2),
                                Right, 3);
                    
                    return new ImMapTree<V>(Data, new ImMapLeaf<V>(key, value), Right, TreeHeight);
                }

                // when the left is tree the right could not be empty
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Data.Key)
                        return new ImMapTree<V>(Data,
                            new ImMapTree<V>(new ImMapLeaf<V>(key, value), leftTree.Left, leftTree.Right, leftTree.TreeHeight),
                            Right, TreeHeight);

                    var newLeftTree = leftTree.AddOrUpdateLeftOrRight(key, value);
                    if (newLeftTree.TreeHeight == leftTree.TreeHeight)
                        return new ImMapTree<V>(Data, newLeftTree, Right, TreeHeight);

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                    {
                        // 1st fact - `leftLeft` and `leftRight` cannot be Empty otherwise we won't need to re-balance the left tree
                        // 2nd fact - either lefLeft or leftRight or both should be a tree
                        var leftLeft = newLeftTree.Left;
                        var leftLeftTree = leftLeft as ImMapTree<V>;
                        var leftLeftHeight = leftLeftTree?.TreeHeight ?? 1;

                        var leftRight = newLeftTree.Right;
                        var leftRightTree = leftRight as ImMapTree<V>;
                        var leftRightHeight = leftRightTree?.TreeHeight ?? 1;

                        if (leftLeftHeight >= leftRightHeight)
                            return new ImMapTree<V>(newLeftTree.Data, 
                                leftLeftTree,
                                new ImMapTree<V>(Data, leftRightHeight, leftRight, rightHeight, Right));

                        // the leftRight should a tree because its height is greater than leftLeft and the latter at least the leaf
                        // ReSharper disable once PossibleNullReferenceException
                        return new ImMapTree<V>(leftRightTree.Data,
                            new ImMapTree<V>(newLeftTree.Data, leftLeftHeight, leftLeft, leftRightTree.Left),
                            new ImMapTree<V>(Data, leftRightTree.Right, rightHeight, Right));
                    }

                    return new ImMapTree<V>(Data, newLeftTree.TreeHeight, newLeftTree, rightHeight, Right);
                }

                return new ImMapTree<V>(Data, new ImMapLeaf<V>(key, value), Right, 2);
            }
            else
            {
                var right = Right;
                if (right is ImMapLeaf<V> rightLeaf)
                {
                    if (key > rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(rightLeaf, Data, new ImMapLeaf<V>(key, value), 2)
                            : new ImMapTree<V>(Data, Left,
                                new ImMapTree<V>(rightLeaf, Empty, new ImMapLeaf<V>(key, value), 2), 3);

                    if (key < rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(new ImMapLeaf<V>(key, value), Data, right, 2)
                            : new ImMapTree<V>(Data, Left,
                                new ImMapTree<V>(rightLeaf, new ImMapLeaf<V>(key, value), Empty, 2), 3);
                    
                    return new ImMapTree<V>(Data, Left, new ImMapLeaf<V>(key, value), TreeHeight);
                }

                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Data.Key)
                        return new ImMapTree<V>(Data, Left,
                            new ImMapTree<V>(new ImMapLeaf<V>(key, value), rightTree.Left, rightTree.Right, rightTree.TreeHeight),
                            TreeHeight);

                    var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, value);
                    if (newRightTree.TreeHeight == rightTree.TreeHeight)
                        return new ImMapTree<V>(Data, Left, newRightTree, TreeHeight);

                    // right tree is at least 3+ deep - means its either rightLeft or rightRight is tree
                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newRightTree.TreeHeight - 1 > leftHeight)
                    {
                        var rightLeft = newRightTree.Left;
                        var rightLeftTree = rightLeft as ImMapTree<V>;
                        var rightLeftHeight = rightLeftTree?.TreeHeight ?? 1;
                        
                        var rightRight = newRightTree.Right;
                        var rightRightTree = rightRight as ImMapTree<V>;
                        var rightRightHeight = rightRightTree?.TreeHeight ?? 1;

                        if (rightRightHeight >= rightLeftHeight)
                        {
                            rightLeftTree = new ImMapTree<V>(Data, leftHeight, Left, rightLeftHeight, rightLeft);
                            newRightTree.Left = rightLeftTree;
                            newRightTree.TreeHeight = rightLeftTree.TreeHeight > rightRightHeight ? rightLeftTree.TreeHeight + 1 : rightRightHeight + 1;
                            return newRightTree;
                        }

                        // `rightLeftTree` should be the tree because rightRight is at least a leaf
                        // ReSharper disable once PossibleNullReferenceException
                        newRightTree.Left = rightLeftTree.Right;
                        var newRightLeftHeight = rightLeftTree.Right.Height;
                        newRightTree.TreeHeight = newRightLeftHeight > rightRightHeight ? newRightLeftHeight + 1 : rightRightHeight + 1;
                        return new ImMapTree<V>(rightLeftTree.Data,
                            new ImMapTree<V>(Data, leftHeight, Left, rightLeftTree.Left),
                            newRightTree);
                    }

                    return new ImMapTree<V>(Data, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                return new ImMapTree<V>(Data, Left, new ImMapLeaf<V>(key, value), 2);
            }
        }
    }

    /// ImMap static methods
    public static class ImMap
    {
        /// Adds or updates the value by key in the map, always returns a modified map
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value) =>
            map is ImMapTree<V> tree
                ? key == tree.Data.Key
                    ? new ImMapTree<V>(new ImMapLeaf<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
                    : tree.AddOrUpdateLeftOrRight(key, value)
            : map is ImMapLeaf<V> leaf
                ? key > leaf.Key
                    ? new ImMapTree<V>(leaf, ImMap<V>.Empty, new ImMapLeaf<V>(key, value), 2)
                : key < leaf.Key
                    ? new ImMapTree<V>(leaf, new ImMapLeaf<V>(key, value), ImMap<V>.Empty, 2)
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

            value = default(V);
            return false;
        }

        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap<V> map, int key)
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
                    return mapTree.Data.Value;
            }

            return map is ImMapLeaf<V> leaf && leaf.Key == key ? leaf.Value : default;
        }
    
        //// Returns all map tree nodes enumerated from the lesser to the bigger keys 
        //public static IEnumerable<ImMapLeaf<V>> Enumerate<V>(this ImMap<V> map)
        //{
        //    if (map is ImMapLeaf<V> leaf)
        //    {
        //        yield return leaf;
        //    }
        //    else if (map != ImMap<V>.Empty)
        //    {
        //        var tree = (ImMapTree<V>)map;
        //        var parents = new ImMap<V>[tree.TreeHeight];
        //        var parentCount = -1;
        //        while (map.Height != 0 || parentCount != -1)
        //        {
        //            if (map.Height > 0)
        //            {
        //                parents[++parentCount] = map;
        //                map = tree.Left;
        //            }
        //            else
        //            {
        //                map = parents[parentCount--];
        //                yield return (ImMapLeaf<V>)map;
        //                map = (map as ImMapTree<V>)?.Right ?? ImMap<V>.Empty;
        //            }
        //        }
        //    }
        //}
    }
}
