using System.Runtime.CompilerServices;

namespace ImTools.Benchmarks.ImMapFixedData4
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
    public sealed class ImMapData<V> : ImMap<V>
    {
        /// <inheritdoc />
        public override int Height => 1;

        /// The Key is basically the hash, or the Height for ImMapTree
        public readonly int Key;

        /// The value - may be modified if you need a Ref{V} semantics
        public V Value;

        public ImMapData(int key, V value)
        {
            Key = key;
            Value = value;
        }

        /// Prints the key value pair
        public override string ToString() => Key + ":" + Value;
    }

    /// <summary>
    /// The two level - two node tree with either left or right
    /// </summary>
    public sealed class ImMapBranch<V> : ImMap<V>
    {
        public override int Height => 2;

        /// Contains the once created data node
        public readonly ImMapData<V> Data;

        /// Left sub-tree/branch, or empty.
        public ImMapData<V> LeftOrRight;

        /// <summary>Is left oriented</summary>
        public bool IsLeftOriented
        {
            [MethodImpl((MethodImplOptions)256)]
            get => LeftOrRight.Key < Data.Key;
        }

        /// <summary>Is right oriented</summary>
        public bool IsRightOriented
        {
            [MethodImpl((MethodImplOptions)256)]
            get => LeftOrRight.Key > Data.Key;
        }

        /// Constructor
        public ImMapBranch(ImMapData<V> data, ImMapData<V> leftOrRight)
        {
            Data = data;
            LeftOrRight = leftOrRight;
        }
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
        public readonly ImMapData<V> Data;

        /// Left sub-tree/branch, or empty.
        public ImMap<V> Left;

        /// Right sub-tree/branch, or empty.
        public ImMap<V> Right;

        internal ImMapTree(ImMapData<V> data, ImMap<V> left, ImMap<V> right, int height)
        {
            Data = data;
            Left = left;
            Right = right;
            TreeHeight = height;
        }

        internal ImMapTree(ImMapData<V> data, int leftHeight, ImMap<V> left, int rightHeight, ImMap<V> right) 
        {
            Data = data;
            Left = left;
            Right = right;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapData<V> data, int leftHeight, ImMap<V> left, ImMap<V> right) 
        {
            Data = data;
            Left = left;
            Right = right;
            var rightHeight = right.Height;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapData<V> data, ImMap<V> left, int rightHeight, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            var leftHeight = left.Height;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapData<V> data, ImMapData<V> leftData, ImMapData<V> rightData)
        {
            Data = data;
            Left = leftData;
            Right = rightData;
            TreeHeight = 2;
        }

        internal ImMapTree(ImMapData<V> data, ImMapTree<V> left, ImMapTree<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            TreeHeight = left.TreeHeight > right.TreeHeight ? left.TreeHeight + 1 : right.TreeHeight + 1;
        }

        /// Outputs the key value pair
        public override string ToString() => "tree(" + Data + ")";

        // Adds or updates the left or right branch
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Data.Key)
            {
                var left = Left;
                if (left is ImMapData<V> leftLeaf)
                {
                    if (Right != Empty)
                        return new ImMapTree<V>(Data, new ImMapBranch<V>(leftLeaf, new ImMapData<V>(key, value)), Right, 3);

                    if (key > leftLeaf.Key)
                        return new ImMapTree<V>(new ImMapData<V>(key, value), leftLeaf, Data);

                    if (key < leftLeaf.Key)
                        return new ImMapTree<V>(leftLeaf, new ImMapData<V>(key, value), Data);

                    return new ImMapTree<V>(Data, new ImMapData<V>(key, value), Right, TreeHeight);
                }

                if (left is ImMapBranch<V> leftBranch)
                {
                    if (key < leftBranch.Data.Key)
                    {
                        var newLeft = leftBranch.IsRightOriented // no need for rotation
                            ? new ImMapTree<V>(leftBranch.Data, new ImMapData<V>(key, value), leftBranch.LeftOrRight)
                            //          5                         5
                            //       3     ?  =>             2        ?
                            //    2                       1     3
                            //  1             
                            : key < leftBranch.LeftOrRight.Key
                                ? new ImMapTree<V>(leftBranch.LeftOrRight, new ImMapData<V>(key, value), leftBranch.Data)
                            //           5                        5
                            //        3     ?  =>            2.5        ?
                            //    2                        2     3
                            //     2.5          
                            : key > leftBranch.LeftOrRight.Key
                                ? new ImMapTree<V>(new ImMapData<V>(key, value), leftBranch.LeftOrRight, leftBranch.Data)
                            : (ImMap<V>)new ImMapBranch<V>(leftBranch.Data, new ImMapData<V>(key, value));

                        return new ImMapTree<V>(Data, newLeft, Right, TreeHeight);
                    }

                    if (key > leftBranch.Data.Key)
                    {
                            //           5                         5
                            //       3       ?  =>             3        ?
                            //     2   !                     2   !
                        var newLeft = leftBranch.IsLeftOriented
                            ? new ImMapTree<V>(leftBranch.Data, leftBranch.LeftOrRight, new ImMapData<V>(key, value))
                            //            5                         5
                            //       2        ?  =>             3        ?
                            //         3                      2   4
                            //          4
                            : key > leftBranch.LeftOrRight.Key
                                ? new ImMapTree<V>(leftBranch.LeftOrRight, leftBranch.Data, new ImMapData<V>(key, value))
                            //            5                         5
                            //      2          ?  =>            2.5        ?
                            //          3                      2   3
                            //       2.5  
                            : key < leftBranch.LeftOrRight.Key 
                                ? new ImMapTree<V>(new ImMapData<V>(key, value), leftBranch.Data, leftBranch.LeftOrRight) 
                            : (ImMap<V>)new ImMapBranch<V>(leftBranch.Data, new ImMapData<V>(key, value));

                        return new ImMapTree<V>(Data, newLeft, Right, TreeHeight);
                    }

                    return new ImMapTree<V>(Data, new ImMapBranch<V>(new ImMapData<V>(key, value), leftBranch.LeftOrRight), Right, TreeHeight);
                }

                // when the left is tree the right could not be empty
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Data.Key)
                        return new ImMapTree<V>(Data,
                            new ImMapTree<V>(new ImMapData<V>(key, value), leftTree.Left, leftTree.Right, leftTree.TreeHeight),
                            Right, TreeHeight);

                    var newLeftTree = leftTree.AddOrUpdateLeftOrRight(key, value);
                    if (newLeftTree.TreeHeight == leftTree.TreeHeight)
                        return new ImMapTree<V>(Data, newLeftTree, Right, TreeHeight);

                    var rightHeight = Right.Height;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                    {
                        // 1st fact - `leftLeft` and `leftRight` cannot be Empty otherwise we won't need to re-balance the left tree
                        // 2nd fact - either lefLeft or leftRight or both should be a tree
                        var leftLeftHeight = newLeftTree.Left.Height;
                        var leftRight = newLeftTree.Right;
                        var leftRightHeight = leftRight.Height;

                        if (leftLeftHeight >= leftRightHeight)
                        {
                            var leftRightTree = new ImMapTree<V>(Data, leftRightHeight, leftRight, rightHeight, Right);
                            newLeftTree.Right = leftRightTree;
                            newLeftTree.TreeHeight = leftLeftHeight > leftRightTree.TreeHeight ? leftLeftHeight + 1 : leftRightTree.TreeHeight + 1;
                            return newLeftTree;
                        }
                        else
                        {
                            // the leftRight should a tree because its height is greater than leftLeft and the latter at least the leaf
                            if (leftRight is ImMapTree<V> leftRightTree)
                            {
                                newLeftTree.Right = leftRightTree.Left;
                                var newLeftRightHeight = newLeftTree.Right.Height;
                                newLeftTree.TreeHeight = leftLeftHeight > newLeftRightHeight ? leftLeftHeight + 1 : newLeftRightHeight + 1;
                                return new ImMapTree<V>(leftRightTree.Data,
                                    newLeftTree, new ImMapTree<V>(Data, leftRightTree.Right, rightHeight, Right));
                            }

                            // leftLeftHeight should be less than leftRightHeight here,
                            // so if leftRightHeight is 2 for branch, then leftLeftHeight should be 1 to prevent re-balancing 
                            var leftRightBranch = (ImMapBranch<V>)leftRight;
                            if (leftRightBranch.IsLeftOriented)
                            {
                                newLeftTree.Right = leftRightBranch.LeftOrRight;
                                newLeftTree.TreeHeight = 2;
                                return new ImMapTree<V>(leftRightBranch.Data, newLeftTree, new ImMapBranch<V>(Data, (ImMapData<V>)Right), 3);
                            }

                            // maybe we need to convert this to branch
                            newLeftTree.Right = Empty;
                            newLeftTree.TreeHeight = 2;
                            return new ImMapTree<V>(leftRightBranch.Data,
                                newLeftTree, new ImMapTree<V>(Data, 1, leftRightBranch.LeftOrRight, rightHeight, Right));
                        }
                    }

                    return new ImMapTree<V>(Data, newLeftTree.TreeHeight, newLeftTree, rightHeight, Right);
                }

                return new ImMapTree<V>(Data, new ImMapData<V>(key, value), Right, 2);
            }
            else
            {
                var right = Right;
                if (right is ImMapData<V> rightLeaf)
                {
                    if (Left != Empty)
                        return new ImMapTree<V>(Data, Left, new ImMapBranch<V>(rightLeaf, new ImMapData<V>(key, value)), 3);

                    if (key > rightLeaf.Key)
                        return new ImMapTree<V>(rightLeaf, Data, new ImMapData<V>(key, value), 2);

                    if (key < rightLeaf.Key)
                        return new ImMapTree<V>(new ImMapData<V>(key, value), Data, right, 2);

                    return new ImMapTree<V>(Data, Left, new ImMapData<V>(key, value), TreeHeight);
                }

                if (right is ImMapBranch<V> rightBranch)
                {
                    if (key > rightBranch.Data.Key)
                    {
                            //      5                5       
                            //  ?       7    =>  ?       7   
                            //        6   !            6   ! 
                        var newLeft = rightBranch.IsLeftOriented
                            ? new ImMapTree<V>(rightBranch.Data, rightBranch.LeftOrRight, new ImMapData<V>(key, value))
                            //      5                5      
                            //  ?       6    =>  ?       8  
                            //            8            6   !
                            //              !               
                            : key > rightBranch.LeftOrRight.Key
                                ? new ImMapTree<V>(rightBranch.LeftOrRight, rightBranch.Data, new ImMapData<V>(key, value))
                            //      5                 5      
                            //  ?       6     =>  ?       7  
                            //              8            6  8
                            //            7               
                            : key < rightBranch.LeftOrRight.Key
                                ? new ImMapTree<V>(new ImMapData<V>(key, value), rightBranch.Data, rightBranch.LeftOrRight)
                            : (ImMap<V>)new ImMapBranch<V>(rightBranch.Data, new ImMapData<V>(key, value));

                        return new ImMapTree<V>(Data, newLeft, Right, TreeHeight);
                    }

                    if (key < rightBranch.Data.Key)
                    {
                            //      5                5       
                            //  ?       7    =>  ?       7   
                            //        !   9            6   9  
                        var newLeft = rightBranch.IsRightOriented // no need for rotation
                            ? new ImMapTree<V>(rightBranch.Data, new ImMapData<V>(key, value), rightBranch.LeftOrRight)
                            //      5              5       
                            //  ?       8  =>  ?       7   
                            //        7              !   8 
                            //       !                     
                            : key < rightBranch.LeftOrRight.Key
                                ? new ImMapTree<V>(rightBranch.LeftOrRight, new ImMapData<V>(key, value), rightBranch.Data)
                            //      5              5       
                            //  ?       9  =>  ?       !   
                            //        7              7   9 
                            //         !                     
                            : key > rightBranch.LeftOrRight.Key
                                ? new ImMapTree<V>(new ImMapData<V>(key, value), rightBranch.LeftOrRight, rightBranch.Data)
                            : (ImMap<V>)new ImMapBranch<V>(rightBranch.Data, new ImMapData<V>(key, value));

                        return new ImMapTree<V>(Data, newLeft, Right, TreeHeight);
                    }

                    return new ImMapTree<V>(Data, Left, new ImMapBranch<V>(new ImMapData<V>(key, value), rightBranch.LeftOrRight), TreeHeight);
                }

                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Data.Key)
                        return new ImMapTree<V>(Data, Left,
                            new ImMapTree<V>(new ImMapData<V>(key, value), rightTree.Left, rightTree.Right, rightTree.TreeHeight),
                            TreeHeight);

                    var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, value);
                    if (newRightTree.TreeHeight == rightTree.TreeHeight)
                        return new ImMapTree<V>(Data, Left, newRightTree, TreeHeight);

                    // right tree is at least 3+ deep - means its either rightLeft or rightRight is tree
                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newRightTree.TreeHeight - 1 > leftHeight)
                    {
                        var rightRightHeight = (newRightTree.Right as ImMapTree<V>)?.TreeHeight ?? 1;
                        var rightLeft = newRightTree.Left;
                        var rightLeftHeight = rightLeft.Height;

                        if (rightRightHeight >= rightLeftHeight)
                        {
                            var rightLeftTree = new ImMapTree<V>(Data, leftHeight, Left, rightLeftHeight, rightLeft);
                            newRightTree.Left = rightLeftTree;
                            newRightTree.TreeHeight = rightLeftTree.TreeHeight > rightRightHeight ? rightLeftTree.TreeHeight + 1 : rightRightHeight + 1;
                            return newRightTree;
                        }
                        else
                        {
                            if (rightLeft is ImMapTree<V> rightLeftTree)
                            {
                                newRightTree.Left = rightLeftTree.Right;
                                var newRightLeftHeight = newRightTree.Left.Height;
                                newRightTree.TreeHeight = newRightLeftHeight > rightRightHeight ? newRightLeftHeight + 1 : rightRightHeight + 1;
                                return new ImMapTree<V>(rightLeftTree.Data,
                                    new ImMapTree<V>(Data, leftHeight, Left, rightLeftTree.Left),
                                    newRightTree);
                            }

                            var rightLeftBranch = (ImMapBranch<V>)rightLeft;
                            if (rightLeftBranch.IsRightOriented)
                            {
                                newRightTree.Right = rightLeftBranch.LeftOrRight;
                                newRightTree.TreeHeight = 2;
                                return new ImMapTree<V>(rightLeftBranch.Data, 
                                    new ImMapBranch<V>(Data, (ImMapData<V>)Right), newRightTree, 3);
                            }

                            // maybe we need to convert this to branch
                            newRightTree.Right = Empty;
                            newRightTree.TreeHeight = 2;
                            return new ImMapTree<V>(rightLeftBranch.Data,
                                new ImMapTree<V>(Data, leftHeight, Left, 1, rightLeftBranch.LeftOrRight), newRightTree);
                        }
                    }

                    return new ImMapTree<V>(Data, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                return new ImMapTree<V>(Data, Left, new ImMapData<V>(key, value), 2);
            }
        }
    }

    /// ImMap static methods
    public static class ImMap
    {
        // todo: try switch expression
        /// Adds or updates the value by key in the map, always returns a modified map
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value)
        {
            if (map is ImMapTree<V> tree)
                return key == tree.Data.Key
                    ? new ImMapTree<V>(new ImMapData<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
                    : tree.AddOrUpdateLeftOrRight(key, value);

            if (map is ImMapBranch<V> treeLeaf)
            {
                if (key > treeLeaf.Data.Key)
                    return treeLeaf.IsLeftOriented 
                        ? new ImMapTree<V>(treeLeaf.Data, treeLeaf.LeftOrRight, new ImMapData<V>(key, value))
                        : key < treeLeaf.LeftOrRight.Key // rotate if right
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), treeLeaf.Data, treeLeaf.LeftOrRight) 
                        : key > treeLeaf.LeftOrRight.Key
                            ? new ImMapTree<V>(treeLeaf.LeftOrRight, treeLeaf.Data, new ImMapData<V>(key, value))
                        : (ImMap<V>)new ImMapBranch<V>(treeLeaf.Data, new ImMapData<V>(key, value));

                if (key < treeLeaf.Data.Key)
                    return treeLeaf.IsRightOriented
                        ? new ImMapTree<V>(treeLeaf.Data, new ImMapData<V>(key, value), treeLeaf.LeftOrRight)
                        : key > treeLeaf.LeftOrRight.Key // rotate if left
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), treeLeaf.LeftOrRight, treeLeaf.Data)
                        : key < treeLeaf.LeftOrRight.Key
                            ? new ImMapTree<V>(treeLeaf.LeftOrRight, new ImMapData<V>(key, value), treeLeaf.Data)
                        : (ImMap<V>)new ImMapBranch<V>(treeLeaf.Data, new ImMapData<V>(key, value));

                return new ImMapBranch<V>(new ImMapData<V>(key, value), treeLeaf.LeftOrRight);
            }

            if (map is ImMapData<V> data)
                return key > data.Key
                    ? new ImMapBranch<V>(data, new ImMapData<V>(key, value))
                : key < data.Key
                    ? new ImMapBranch<V>(new ImMapData<V>(key, value), data)
                : (ImMap<V>)new ImMapData<V>(key, value);
            
            return new ImMapData<V>(key, value);
        }

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

            if (map is ImMapData<V> leaf && leaf.Key == key)
            {
                value = leaf.Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
