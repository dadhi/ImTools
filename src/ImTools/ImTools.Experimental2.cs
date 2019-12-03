using System.Diagnostics;
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

        /// Creates with data and right data passed in any order. Note: the keys though should no be equal - it should be checked on caller side
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapBranch<V> CreateNormalized(ImMapData<V> data1, ImMapData<V> data2) => 
            data2.Key > data1.Key 
                ? new ImMapBranch<V>(data1, data2) 
                : new ImMapBranch<V>(data2, data1);

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

        /// Right sub-tree/branch, or empty.md
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
            Debug.Assert(leftHeight - rightHeight < 2 && rightHeight - leftHeight - rightHeight < 2);
            TreeHeight = 1 + (leftHeight > rightHeight ? leftHeight : rightHeight);
        }

        internal ImMapTree(ImMapData<V> data, int leftHeight, ImMap<V> left, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            var rightHeight = right.Height;
            Debug.Assert(leftHeight - rightHeight < 2 && rightHeight - leftHeight - rightHeight < 2);
            TreeHeight = 1 + (leftHeight > rightHeight ? leftHeight : rightHeight);
        }

        internal ImMapTree(ImMapData<V> data, ImMap<V> left, int rightHeight, ImMap<V> right)
        {
            Data = data;
            Left = left;
            Right = right;
            var leftHeight = left.Height;
            Debug.Assert(leftHeight - rightHeight < 2 && rightHeight - leftHeight - rightHeight < 2);
            TreeHeight = 1 + (leftHeight > rightHeight ? leftHeight : rightHeight);
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
                + ") -> (" + (Left is ImMapTree<V> leftTree ? leftTree.Data  + " height:" + leftTree.TreeHeight  : "" + Left)
                + ", " +   (Right is ImMapTree<V> rightTree ? rightTree.Data + " height:" + rightTree.TreeHeight : "" + Right)
                + ")";

        /// <summary>Adds or updates the left or right branch</summary>
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Data.Key)
            {
                var left = Left;
                if (left is ImMapData<V> leftLeaf)
                {
                    Debug.Assert(Right != Empty, "Right could not be Empty  because we handled it with branch on a caller side");
                    return key > leftLeaf.Key
                            ? new ImMapTree<V>(Data, new ImMapBranch<V>(leftLeaf, new ImMapData<V>(key, value)), Right, 3)
                        : key < leftLeaf.Key
                            ? new ImMapTree<V>(Data, new ImMapBranch<V>(new ImMapData<V>(key, value), leftLeaf), Right, 3)
                            : new ImMapTree<V>(Data, new ImMapData<V>(key, value), Right, TreeHeight);
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

                    Debug.Assert(newLeftTree.TreeHeight >= 3, "It cannot be a 2 level tree because, 2 level trees a created here on the caller side");
                    if (Right is ImMapData<V> rightLeaf)
                    {
                        Debug.Assert(newLeftTree.TreeHeight == 3, "Otherwise it is too un-balanced");
                        if (newLeftTree.Left is ImMapData<V> == false)
                        {
                            newLeftTree.Right = new ImMapTree<V>(Data, newLeftTree.Right, rightLeaf, 2);
                            newLeftTree.TreeHeight = 3;
                            return newLeftTree;
                        }

                        if (newLeftTree.Right is ImMapTree<V> leftRightTree)
                            return new ImMapTree<V>(leftRightTree.Data,
                                new ImMapTree<V>(newLeftTree.Data, 1, newLeftTree.Left, leftRightTree.Left),
                                new ImMapTree<V>(Data, leftRightTree.Right, 1, rightLeaf));

                        var leftRightBranch = (ImMapBranch<V>)newLeftTree.Right;
                        return new ImMapTree<V>(leftRightBranch.Data,
                            2, ImMapBranch<V>.CreateNormalized(newLeftTree.Data, (ImMapData<V>)newLeftTree.Left),
                            new ImMapTree<V>(Data, leftRightBranch.RightData, rightLeaf));
                    }

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 2;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                    {
                        var leftLeftHeight  = (newLeftTree.Left  as ImMapTree<V>)?.TreeHeight ?? 2;
                        var leftRightHeight = (newLeftTree.Right as ImMapTree<V>)?.TreeHeight ?? 2;
                        if (leftLeftHeight >= leftRightHeight)
                        {
                            var newLeftRightTree = new ImMapTree<V>(Data, newLeftTree.Right, Right, leftRightHeight + 1);
                            newLeftTree.Right = newLeftRightTree;
                            newLeftTree.TreeHeight = 1 + (leftLeftHeight >= newLeftRightTree.TreeHeight ? leftLeftHeight : newLeftRightTree.TreeHeight);
                            return newLeftTree;
                        }

                        var leftRightTree = (ImMapTree<V>)newLeftTree.Right;
                        return new ImMapTree<V>(leftRightTree.Data,
                            new ImMapTree<V>(newLeftTree.Data, leftLeftHeight, newLeftTree.Left, leftRightTree.Left),
                            new ImMapTree<V>(Data, leftRightTree.Right, rightHeight, Right));
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
                    Debug.Assert(Left != Empty, "Left could not be Empty because we handled it with branch on a caller side");
                    return key > rightLeaf.Key
                        ? new ImMapTree<V>(Data, Left, new ImMapBranch<V>(rightLeaf, new ImMapData<V>(key, value)), 3)
                        : key < rightLeaf.Key ? new ImMapTree<V>(Data, Left, new ImMapBranch<V>(new ImMapData<V>(key, value), rightLeaf), 3) 
                        : new ImMapTree<V>(Data, Left, new ImMapData<V>(key, value), TreeHeight);
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

                    // the fact: left is not Empty
                    if (Left is ImMapData<V> leftLeaf)
                    {
                        // here we need to re-balance by default, because the new right tree is at least 3 level (actually exactly 3 or it would be too unbalanced)
                        // double rotation needed if only the right-right is a leaf
                        if (newRightTree.Right is ImMapData<V> == false)
                        {
                            newRightTree.Left = new ImMapTree<V>(Data, leftLeaf, newRightTree.Left, 2);
                            newRightTree.TreeHeight = 3;
                            return newRightTree;
                        }

                        if (newRightTree.Left is ImMapTree<V> rightLeftTree)
                            return new ImMapTree<V>(rightLeftTree.Data,
                                new ImMapTree<V>(Data, 1, leftLeaf, rightLeftTree.Left),
                                new ImMapTree<V>(newRightTree.Data, rightLeftTree.Right, 1, newRightTree.Right));

                        var rightLeftBranch = (ImMapBranch<V>)newRightTree.Left;
                        return new ImMapTree<V>(rightLeftBranch.Data,
                            2, ImMapBranch<V>.CreateNormalized(Data, leftLeaf),
                            new ImMapTree<V>(newRightTree.Data, rightLeftBranch.RightData, (ImMapData<V>)newRightTree.Right));
                    }

                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 2;
                    if (newRightTree.TreeHeight > leftHeight + 1) 
                    {
                        var rightRightHeight = (newRightTree.Right as ImMapTree<V>)?.TreeHeight ?? 2;
                        var rightLeftHeight  = (newRightTree.Left  as ImMapTree<V>)?.TreeHeight ?? 2;
                        if (rightRightHeight >= rightLeftHeight)
                        {
                            Debug.Assert(rightLeftHeight >= leftHeight, "The whole rightHeight > leftHeight by 2, and rightRight >= leftHeight but not more than by 2");
                            var newRightLeftTree = new ImMapTree<V>(Data, Left, newRightTree.Left, rightLeftHeight + 1);
                            newRightTree.Left = newRightLeftTree;
                            newRightTree.TreeHeight = 1 + (rightRightHeight >= newRightLeftTree.TreeHeight ? rightRightHeight : newRightLeftTree.TreeHeight);
                            return newRightTree;
                        }

                        var rightLeftTree = (ImMapTree<V>)newRightTree.Left;
                        return new ImMapTree<V>(rightLeftTree.Data,
                            new ImMapTree<V>(Data, leftHeight, Left, rightLeftTree.Left),
                            new ImMapTree<V>(newRightTree.Data, rightLeftTree.Right, rightRightHeight, newRightTree.Right));
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
