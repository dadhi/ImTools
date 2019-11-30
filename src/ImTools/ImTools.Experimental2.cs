using System.Runtime.CompilerServices; // for [MethodImpl((MethodImplOptions)256)]

namespace ImTools.Experimental2
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

        /// <summary>Constructs the pair</summary>
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
        /// <summary>Always two</summary>
        public override int Height => 2;

        /// Contains the once created data node
        public readonly ImMapData<V> Data;

        /// Left sub-tree/branch, or empty.
        public ImMapData<V> RightData;

        /// Constructor
        public ImMapBranch(ImMapData<V> data, ImMapData<V> rightData)
        {
            Data = data;
            RightData = rightData;
        }

        /// Prints the key value pair
        public override string ToString() => Data + "->" + RightData;
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

        /// <summary>Outputs the key value pair</summary>
        public override string ToString() =>
            "(" + Data
                + ") -> (left: " + (Left is ImMapTree<V> leftTree ? leftTree.Data : Left)
                + ", right: " + (Right is ImMapTree<V> rightTree ? rightTree.Data : Right)
                + ")";

        /// <summary>Adds or updates the left or right branch</summary>
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Data.Key)
            {
                var left = Left;
                if (left is ImMapData<V> leftLeaf)
                {
                    if (key > leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), leftLeaf, Data)
                            : new ImMapTree<V>(Data, new ImMapBranch<V>(leftLeaf, new ImMapData<V>(key, value)), Right, 3);

                    if (key < leftLeaf.Key)
                        return Right == Empty 
                            ? new ImMapTree<V>(leftLeaf, new ImMapData<V>(key, value), Data)
                            : new ImMapTree<V>(Data, new ImMapBranch<V>(new ImMapData<V>(key, value), leftLeaf), Right, 3);

                    return new ImMapTree<V>(Data, new ImMapData<V>(key, value), Right, TreeHeight);
                }

                if (left is ImMapBranch<V> leftBranch)
                {
                    if (key < leftBranch.Data.Key)
                        return new ImMapTree<V>(Data,
                            new ImMapTree<V>(leftBranch.Data, new ImMapData<V>(key, value), leftBranch.RightData),
                            Right, TreeHeight);

                    if (key > leftBranch.Data.Key)
                    {
                        var newLeft = 
                            //            5                         5
                            //       2        ?  =>             3        ?
                            //         3                      2   4
                            //          4
                            key > leftBranch.RightData.Key
                                ? new ImMapTree<V>(leftBranch.RightData, leftBranch.Data, new ImMapData<V>(key, value))
                            //            5                         5
                            //      2          ?  =>            2.5        ?
                            //          3                      2   3
                            //       2.5  
                            : key < leftBranch.RightData.Key
                                ? new ImMapTree<V>(new ImMapData<V>(key, value), leftBranch.Data, leftBranch.RightData)
                            : (ImMap<V>)new ImMapBranch<V>(leftBranch.Data, new ImMapData<V>(key, value));

                        return new ImMapTree<V>(Data, newLeft, Right, TreeHeight);
                    }

                    return new ImMapTree<V>(Data, new ImMapBranch<V>(new ImMapData<V>(key, value), leftBranch.RightData), Right, TreeHeight);
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
                            newLeftTree.Right = Empty;
                            newLeftTree.TreeHeight = 2;
                            return new ImMapTree<V>(leftRightBranch.Data,
                                newLeftTree, new ImMapTree<V>(Data, 1, leftRightBranch.RightData, rightHeight, Right));
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
                    if (key > rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(rightLeaf, Data, new ImMapData<V>(key, value), 2)
                            : new ImMapTree<V>(Data, Left, new ImMapBranch<V>(rightLeaf, new ImMapData<V>(key, value)), 3);

                    if (key < rightLeaf.Key)
                        return Left == Empty 
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), Data, right, 2)
                            : new ImMapTree<V>(Data, Left, new ImMapBranch<V>(new ImMapData<V>(key, value), rightLeaf), 3);

                    return new ImMapTree<V>(Data, Left, new ImMapData<V>(key, value), TreeHeight);
                }

                if (right is ImMapBranch<V> rightBranch)
                {
                    if (key > rightBranch.Data.Key)
                    {
                        var newRight = 
                            //      5                5      
                            //  ?       6    =>  ?       8  
                            //            8            6   !
                            //              !               
                            key > rightBranch.RightData.Key
                                ? new ImMapTree<V>(rightBranch.RightData, rightBranch.Data, new ImMapData<V>(key, value))
                            //      5                 5      
                            //  ?       6     =>  ?       7  
                            //              8            6  8
                            //            7               
                            : key < rightBranch.RightData.Key
                                ? new ImMapTree<V>(new ImMapData<V>(key, value), rightBranch.Data, rightBranch.RightData)
                            : (ImMap<V>)new ImMapBranch<V>(rightBranch.Data, new ImMapData<V>(key, value));

                        return new ImMapTree<V>(Data, Left, newRight, TreeHeight);
                    }

                    if (key < rightBranch.Data.Key)
                        return new ImMapTree<V>(Data, Left,
                            new ImMapTree<V>(rightBranch.Data, new ImMapData<V>(key, value), rightBranch.RightData),
                            TreeHeight);

                    return new ImMapTree<V>(Data, Left, new ImMapBranch<V>(new ImMapData<V>(key, value), rightBranch.RightData), TreeHeight);
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
                        var rightRightHeight = newRightTree.Right.Height;
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
                            newRightTree.Right = rightLeftBranch.RightData;
                            newRightTree.TreeHeight = 2;
                            return new ImMapTree<V>(rightLeftBranch.Data,
                                new ImMapBranch<V>(Data, (ImMapData<V>) Right), newRightTree, 3);
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
        // todo: try `switch expression`
        /// Adds or updates the value by key in the map, always returns a modified map
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value)
        {
            if (map is ImMapTree<V> tree)
                return key == tree.Data.Key
                    ? new ImMapTree<V>(new ImMapData<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
                    : tree.AddOrUpdateLeftOrRight(key, value);

            if (map is ImMapBranch<V> branch)
            {
                if (key > branch.Data.Key)
                        //   5                  10
                        //        10     =>  5     11
                        //           11           
                    return key > branch.RightData.Key
                        ? new ImMapTree<V>(branch.RightData, branch.Data, new ImMapData<V>(key, value))
                        //   5               7
                        //        10  =>  5     10
                        //      7           
                        : key < branch.RightData.Key // rotate if right
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), branch.Data, branch.RightData)
                        : (ImMap<V>)new ImMapBranch<V>(branch.Data, new ImMapData<V>(key, value));

                if (key < branch.Data.Key)
                    return new ImMapTree<V>(branch.Data, new ImMapData<V>(key, value), branch.RightData);

                return new ImMapBranch<V>(new ImMapData<V>(key, value), branch.RightData);
            }

            if (map is ImMapData<V> leaf)
                return key > leaf.Key
                    ? new ImMapBranch<V>(leaf, new ImMapData<V>(key, value))
                : key < leaf.Key
                    ? new ImMapBranch<V>(new ImMapData<V>(key, value), leaf)
                : (ImMap<V>)new ImMapData<V>(key, value);

            return new ImMapData<V>(key, value);
        }

        /// <summary>
        /// Returns true if key is found and sets the value.
        /// </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMap<V> map, int key, out V value)
        {
            ImMapData<V> data;
            while (map is ImMapTree<V> tree)
            {
                data = tree.Data;
                if (key > data.Key)
                    map = tree.Right;
                else if (key < data.Key)
                    map = tree.Left;
                else
                {
                    value = data.Value;
                    return true;
                }
            }

            if (map is ImMapBranch<V> branch)
            {
                if (branch.Data.Key == key)
                {
                    value = branch.Data.Value;
                    return true;
                }

                if (branch.RightData.Key == key)
                {
                    value = branch.RightData.Value;
                    return true;
                }
            }

            data = map as ImMapData<V>;
            if (data != null && data.Key == key)
            {
                value = data.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns the value if key is found or default value otherwise.
        /// </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap<V> map, int key)
        {
            ImMapData<V> data;
            while (map is ImMapTree<V> tree)
            {
                data = tree.Data;
                if (key > data.Key)
                    map = tree.Right;
                else if (key < data.Key)
                    map = tree.Left;
                else
                    return data.Value;
            }

            if (map is ImMapBranch<V> branch)
            {
                if (branch.Data.Key == key)
                    return branch.Data.Value;

                if (branch.RightData.Key == key)
                    return branch.RightData.Value;
            }

            data = map as ImMapData<V>;
            if (data != null && data.Key == key)
                return data.Value;

            return default;
        }

    }
}
