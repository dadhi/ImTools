using System;
using System.Threading;
using System.Runtime.CompilerServices; // for [MethodImpl((MethodImplOptions)256)]

namespace ImTools.Experimental
{
    /// Empty ImMap to start with
    public class ImMap<V>
    {
        /// Empty tree to start with.
        public static readonly ImMap<V> Empty = new ImMap<V>();

        /// Creates an empty map
        protected ImMap() { }

        /// Height of the longest sub-tree/branch. Starts from 2 because it a tree and not the leaf
        public virtual int Height => 0;

        /// Returns true if tree is empty.
        public bool IsEmpty => Height == 0;

        /// Prints "empty"
        public override string ToString() => "empty";
    }

    /// <summary>
    /// The leaf node - just the key-value pair
    /// </summary>
    public sealed class ImMapData<V> : ImMap<V>
    {
        /// <inheritdoc />
        public override int Height => 1;

        /// The key
        public readonly int Key;

        /// The value - may be modified if you need a Ref{V} semantics
        public V Value;

        /// Creates the data leaf
        public ImMapData(int key, V value)
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
            + ") -> (left: " + (Left  is ImMapTree<V> leftTree  ? leftTree.Data  : Left)
            + ", right: "    + (Right is ImMapTree<V> rightTree ? rightTree.Data : Right) 
            + ")";

        /// Adds or updates the left or right branch
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Data.Key)
            {
                var left = Left;
                if (left is ImMapData<V> leftLeaf)
                {
                    if (key < leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(leftLeaf, new ImMapData<V>(key, value), Data, 2)
                            : new ImMapTree<V>(Data, 
                                new ImMapTree<V>(leftLeaf, new ImMapData<V>(key, value), Empty, 2), 
                                Right, 3); // given that left is the leaf, the Right tree should be less than 2 - otherwise tree is unbalanced

                    if (key > leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), left, Data, 2)
                            : new ImMapTree<V>(Data,
                                new ImMapTree<V>(leftLeaf, Empty, new ImMapData<V>(key, value), 2),
                                Right, 3);
                    
                    return new ImMapTree<V>(Data, new ImMapData<V>(key, value), Right, TreeHeight);
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

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                        return BalanceNewLeftTree(newLeftTree, rightHeight);

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
                            : new ImMapTree<V>(Data, Left, new ImMapTree<V>(rightLeaf, Empty, new ImMapData<V>(key, value), 2), 3);

                    if (key < rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), Data, right, 2)
                            : new ImMapTree<V>(Data, Left, new ImMapTree<V>(rightLeaf, new ImMapData<V>(key, value), Empty, 2), 3);
                    
                    return new ImMapTree<V>(Data, Left, new ImMapData<V>(key, value), TreeHeight);
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
                        return BalanceNewRightTree(newRightTree, leftHeight);

                    return new ImMapTree<V>(Data, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                return new ImMapTree<V>(Data, Left, new ImMapData<V>(key, value), 2);
            }
        }

        /// Adds or keeps the left or right branch
        public ImMapTree<V> AddOrKeepLeftOrRight(int key, V value)
        {
            if (key < Data.Key)
            {
                var left = Left;
                if (left is ImMapData<V> leftLeaf)
                {
                    if (key < leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(leftLeaf, new ImMapData<V>(key, value), Data, 2)
                            : new ImMapTree<V>(Data,
                                new ImMapTree<V>(leftLeaf, new ImMapData<V>(key, value), Empty, 2),
                                Right, 3); // given that left is the leaf, the Right tree should be less than 2 - otherwise tree is unbalanced

                    if (key > leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), left, Data, 2)
                            : new ImMapTree<V>(Data,
                                new ImMapTree<V>(leftLeaf, Empty, new ImMapData<V>(key, value), 2),
                                Right, 3);

                    return this;
                }

                // when the left is tree the right could not be empty
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Data.Key)
                        return this;

                    var newLeftTree = leftTree.AddOrKeepLeftOrRight(key, value);
                    if (newLeftTree == leftTree)
                        return this;

                    if (newLeftTree.TreeHeight == leftTree.TreeHeight)
                        return new ImMapTree<V>(Data, newLeftTree, Right, TreeHeight);

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                        return BalanceNewLeftTree(newLeftTree, rightHeight);

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
                            : new ImMapTree<V>(Data, Left,
                                new ImMapTree<V>(rightLeaf, Empty, new ImMapData<V>(key, value), 2), 3);

                    if (key < rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(new ImMapData<V>(key, value), Data, right, 2)
                            : new ImMapTree<V>(Data, Left,
                                new ImMapTree<V>(rightLeaf, new ImMapData<V>(key, value), Empty, 2), 3);

                    return this;
                }

                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Data.Key)
                        return new ImMapTree<V>(Data, Left,
                            new ImMapTree<V>(new ImMapData<V>(key, value), rightTree.Left, rightTree.Right, rightTree.TreeHeight),
                            TreeHeight);

                    var newRightTree = rightTree.AddOrKeepLeftOrRight(key, value);
                    if (newRightTree == rightTree)
                        return this;
                    
                    if (newRightTree.TreeHeight == rightTree.TreeHeight)
                        return new ImMapTree<V>(Data, Left, newRightTree, TreeHeight);

                    // right tree is at least 3+ deep - means its either rightLeft or rightRight is tree
                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newRightTree.TreeHeight - 1 > leftHeight)
                        return BalanceNewRightTree(newRightTree, leftHeight);

                    return new ImMapTree<V>(Data, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                return new ImMapTree<V>(Data, Left, new ImMapData<V>(key, value), 2);
            }
        }

        /// Adds or keeps the left or right branch
        public ImMapTree<V> AddOrKeepLeftOrRight(int key, ImMapData<V> newData)
        {
            if (key < Data.Key)
            {
                var left = Left;
                if (left is ImMapData<V> leftLeaf)
                {
                    if (key < leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(leftLeaf, newData, Data, 2)
                            : new ImMapTree<V>(Data,
                                new ImMapTree<V>(leftLeaf, newData, Empty, 2),
                                Right, 3); // given that left is the leaf, the Right tree should be less than 2 - otherwise tree is unbalanced

                    if (key > leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(newData, left, Data, 2)
                            : new ImMapTree<V>(Data,
                                new ImMapTree<V>(leftLeaf, Empty, newData, 2),
                                Right, 3);

                    return this;
                }

                // when the left is tree the right could not be empty
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Data.Key)
                        return this;

                    var newLeftTree = leftTree.AddOrKeepLeftOrRight(key, newData);
                    if (newLeftTree == leftTree)
                        return this;

                    if (newLeftTree.TreeHeight == leftTree.TreeHeight)
                        return new ImMapTree<V>(Data, newLeftTree, Right, TreeHeight);

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                        return BalanceNewLeftTree(newLeftTree, rightHeight);

                    return new ImMapTree<V>(Data, newLeftTree.TreeHeight, newLeftTree, rightHeight, Right);
                }

                return new ImMapTree<V>(Data, newData, Right, 2);
            }
            else
            {
                var right = Right;
                if (right is ImMapData<V> rightLeaf)
                {
                    if (key > rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(rightLeaf, Data, newData, 2)
                            : new ImMapTree<V>(Data, Left,
                                new ImMapTree<V>(rightLeaf, Empty, newData, 2), 3);

                    if (key < rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(newData, Data, right, 2)
                            : new ImMapTree<V>(Data, Left,
                                new ImMapTree<V>(rightLeaf, newData, Empty, 2), 3);

                    return this;
                }

                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Data.Key)
                        return new ImMapTree<V>(Data, Left,
                            new ImMapTree<V>(newData, rightTree.Left, rightTree.Right, rightTree.TreeHeight),
                            TreeHeight);

                    var newRightTree = rightTree.AddOrKeepLeftOrRight(key, newData);
                    if (newRightTree == rightTree)
                        return this;

                    if (newRightTree.TreeHeight == rightTree.TreeHeight)
                        return new ImMapTree<V>(Data, Left, newRightTree, TreeHeight);

                    // right tree is at least 3+ deep - means its either rightLeft or rightRight is tree
                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newRightTree.TreeHeight - 1 > leftHeight)
                        return BalanceNewRightTree(newRightTree, leftHeight);

                    return new ImMapTree<V>(Data, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                return new ImMapTree<V>(Data, Left, newData, 2);
            }
        }

        private ImMapTree<V> BalanceNewLeftTree(ImMapTree<V> newLeftTree, int rightHeight)
        {
            // 1st fact - `leftLeft` and `leftRight` cannot be Empty otherwise we won't need to re-balance the left tree
            // 2nd fact - either lefLeft or leftRight or both should be a tree
            var leftLeftHeight = (newLeftTree.Left as ImMapTree<V>)?.TreeHeight ?? 1;

            var leftRight = newLeftTree.Right;
            var leftRightTree = leftRight as ImMapTree<V>;
            var leftRightHeight = leftRightTree?.TreeHeight ?? 1;

            if (leftLeftHeight >= leftRightHeight)
            {
                leftRightTree = new ImMapTree<V>(Data, leftRightHeight, leftRight, rightHeight, Right);
                newLeftTree.Right = leftRightTree;
                newLeftTree.TreeHeight =
                    leftLeftHeight > leftRightTree.TreeHeight ? leftLeftHeight + 1 : leftRightTree.TreeHeight + 1;
                return newLeftTree;
            }

            // the leftRight should a tree because its height is greater than leftLeft and the latter at least the leaf
            // ReSharper disable once PossibleNullReferenceException
            newLeftTree.Right = leftRightTree.Left;
            var newLeftRightHeight = newLeftTree.Right.Height;
            newLeftTree.TreeHeight = leftLeftHeight > newLeftRightHeight ? leftLeftHeight + 1 : newLeftRightHeight + 1;
            return new ImMapTree<V>(leftRightTree.Data,
                newLeftTree,
                new ImMapTree<V>(Data, leftRightTree.Right, rightHeight, Right));
        }

        private ImMapTree<V> BalanceNewRightTree(ImMapTree<V> newRightTree, int leftHeight)
        {
            var rightRightHeight = (newRightTree.Right as ImMapTree<V>)?.TreeHeight ?? 1;

            var rightLeft = newRightTree.Left;
            var rightLeftTree = rightLeft as ImMapTree<V>;
            var rightLeftHeight = rightLeftTree?.TreeHeight ?? 1;

            if (rightRightHeight >= rightLeftHeight)
            {
                rightLeftTree = new ImMapTree<V>(Data, leftHeight, Left, rightLeftHeight, rightLeft);
                newRightTree.Left = rightLeftTree;
                newRightTree.TreeHeight = rightLeftTree.TreeHeight > rightRightHeight
                    ? rightLeftTree.TreeHeight + 1
                    : rightRightHeight + 1;
                return newRightTree;
            }

            // `rightLeftTree` should be the tree because rightRight is at least a leaf
            // ReSharper disable once PossibleNullReferenceException
            newRightTree.Left = rightLeftTree.Right;
            var newRightLeftHeight = newRightTree.Left.Height;
            newRightTree.TreeHeight = newRightLeftHeight > rightRightHeight ? newRightLeftHeight + 1 : rightRightHeight + 1;
            return new ImMapTree<V>(rightLeftTree.Data,
                new ImMapTree<V>(Data, leftHeight, Left, rightLeftTree.Left),
                newRightTree);
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
                    ? new ImMapTree<V>(new ImMapData<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
                    : tree.AddOrUpdateLeftOrRight(key, value)
            : map is ImMapData<V> data
                ? key > data.Key ? new ImMapTree<V>(data, ImMap<V>.Empty, new ImMapData<V>(key, value), 2)
                : key < data.Key ? new ImMapTree<V>(data, new ImMapData<V>(key, value), ImMap<V>.Empty, 2)
                : (ImMap<V>)new ImMapData<V>(key, value)
            : new ImMapData<V>(key, value);

        /// Returns a new map with added value for the specified key or the existing map if the key is already in the map.
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrKeep<V>(this ImMap<V> map, int key, V value) =>
              map is ImMapTree<V> tree
                ? key == tree.Data.Key
                    ? map
                    : tree.AddOrKeepLeftOrRight(key, value)
            : map is ImMapData<V> data
                ? key > data.Key ? new ImMapTree<V>(data, ImMap<V>.Empty, new ImMapData<V>(key, value), 2)
                : key < data.Key ? new ImMapTree<V>(data, new ImMapData<V>(key, value), ImMap<V>.Empty, 2)
                : map
            : new ImMapData<V>(key, value);

        /// Returns a new map with added value for the specified key or the existing map if the key is already in the map.
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrKeep<V>(this ImMap<V> map, ImMapData<V> newData)
        {
            var key = newData.Key;
            return map is ImMapTree<V> tree
                ? key == tree.Data.Key
                    ? map
                    : tree.AddOrKeepLeftOrRight(key, newData)
                : map is ImMapData<V> data
                    ? key > data.Key ? new ImMapTree<V>(data, ImMap<V>.Empty, newData, 2)
                    : key < data.Key ? new ImMapTree<V>(data, newData, ImMap<V>.Empty, 2)
                    : map
                : newData;
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

            data = map as ImMapData<V>;
            if (data != null && data.Key == key)
            {
                value = data.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary> Returns true if key is found and sets the value. </summary>
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

            data = map as ImMapData<V>;
            return data != null && data.Key == key ? data.Value : default;
        }

        /// <summary> Returns true if key is found and sets the value. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapData<V> GetDataOrDefault<V>(this ImMap<V> map, int key)
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
                    return data;
            }

            data = map as ImMapData<V>;
            return data != null && data.Key == key ? data : null;
        }

        /// <summary> Returns true if key is found and sets the value. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool Contains<V>(this ImMap<V> map, int key)
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
                    return true;
            }

            data = map as ImMapData<V>;
            return data != null && data.Key == key;
        }

        /// <summary>
        /// Folds all the map nodes with the state from the left to the right and from the bottom to the top
        /// You may pass `parentStacks` to reuse the array memory.
        /// NOTE: the length of `parentStack` should be at least of map height, content is not important and could be erased.
        /// </summary>
        public static S Fold<V, S>(this ImMap<V> map, S state, Func<ImMapData<V>, S, S> reduce, ImMapTree<V>[] parentStack = null)
        {
            if (map is ImMapData<V> data)
                state = reduce(data, state);
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                {
                    if (tree.Left is ImMapData<V> ld)
                        state = reduce(ld, state);
                    state = reduce(tree.Data, state);
                    if (tree.Right is ImMapData<V> rd)
                        state = reduce(rd, state);
                }
                else
                {
                    parentStack = parentStack ?? new ImMapTree<V>[tree.TreeHeight - 2];
                    var parentIndex = -1;
                    do
                    {
                        if ((tree = map as ImMapTree<V>) != null)
                        {
                            if (tree.TreeHeight == 2)
                            {
                                if (tree.Left is ImMapData<V> ld)
                                    state = reduce(ld, state);
                                state = reduce(tree.Data, state);
                                if (tree.Right is ImMapData<V> rd)
                                    state = reduce(rd, state);
                                if (parentIndex == -1)
                                    break;
                                tree = parentStack[parentIndex--];
                                state = reduce(tree.Data, state);
                                map = tree.Right;
                            }
                            else
                            {
                                parentStack[++parentIndex] = tree;
                                map = tree.Left;
                            }
                        }
                        else
                        {
                            state = reduce((ImMapData<V>)map, state);
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            state = reduce(tree.Data, state);
                            map = tree.Right;
                        }
                    }
                    while (map != ImMap<V>.Empty);
                }
            }

            return state;
        }

        /// <summary>
        /// Visits all the map nodes with from the left to the right and from the bottom to the top
        /// You may pass `parentStacks` to reuse the array memory.
        /// NOTE: the length of `parentStack` should be at least of map height, content is not important and could be erased.
        /// </summary>
        public static void Visit<V>(this ImMap<V> map, Action<ImMapData<V>> visit, ImMapTree<V>[] parentStack = null)
        {
            if (map is ImMapData<V> data)
                visit(data);
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                {
                    if (tree.Left is ImMapData<V> ld)
                        visit(ld);
                    visit(tree.Data);
                    if (tree.Right is ImMapData<V> rd)
                        visit(rd);
                }
                else
                {
                    parentStack = parentStack ?? new ImMapTree<V>[tree.TreeHeight - 2];
                    var parentIndex = -1;
                    do
                    {
                        if ((tree = map as ImMapTree<V>) != null)
                        {
                            if (tree.TreeHeight == 2)
                            {
                                if (tree.Left is ImMapData<V> ld)
                                    visit(ld);
                                visit(tree.Data);
                                if (tree.Right is ImMapData<V> rd)
                                    visit(rd);
                                if (parentIndex == -1)
                                    break;
                                tree = parentStack[parentIndex--];
                                visit(tree.Data);
                                map = tree.Right;
                            }
                            else
                            {
                                parentStack[++parentIndex] = tree;
                                map = tree.Left;
                            }
                        }
                        else
                        {
                            visit((ImMapData<V>)map);
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            visit(tree.Data);
                            map = tree.Right;
                        }
                    }
                    while (map != ImMap<V>.Empty);
                }
            }
        }

        ///<summary>Returns the new map with the updated value for the key, or the same map if the key was not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> Update<V>(this ImMap<V> map, int key, V value) =>
            map.Contains(key) ? map.UpdateImpl(key, value) : map;

        internal static ImMap<V> UpdateImpl<V>(this ImMap<V> map, int key, V value) =>
            map is ImMapTree<V> tree
                ? key > tree.Data.Key ? new ImMapTree<V>(tree.Data, tree.Left, tree.Right.UpdateImpl(key, value), tree.TreeHeight)
                : key < tree.Data.Key ? new ImMapTree<V>(tree.Data, tree.Left.UpdateImpl(key, value), tree.Right, tree.TreeHeight)
                : new ImMapTree<V>(new ImMapData<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
            : (ImMap<V>)new ImMapData<V>(key, value);
    }

    /// The array of ImMap slots where the key first bits are used for FAST slot location
    /// and the slot is the reference to ImMap that can be swapped with its updated value
    public static class ImMapSlots
    {
        /// Default number of slots
        public const int SLOT_COUNT_POWER_OF_TWO = 32;

        /// The default mask to partition the key to the target slot
        public const int KEY_MASK_TO_FIND_SLOT = SLOT_COUNT_POWER_OF_TWO - 1;

        /// Creates the array with the empty slots
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V>[] CreateWithEmpty<V>(int slotCountPowerOfTwo = SLOT_COUNT_POWER_OF_TWO)
        {
            var slots = new ImMap<V>[slotCountPowerOfTwo];
            for (var i = 0; i < slots.Length; ++i)
                slots[i] = ImMap<V>.Empty;
            return slots;
        }

        /// Returns a new tree with added or updated value for specified key.
        [MethodImpl((MethodImplOptions)256)]
        public static void AddOrUpdate<V>(this ImMap<V>[] slots, int key, V value, int keyMaskToFindSlot = KEY_MASK_TO_FIND_SLOT)
        {
            ref var slot = ref slots[key & keyMaskToFindSlot];
            var copy = slot;
            if (Interlocked.CompareExchange(ref slot, copy.AddOrUpdate(key, value), copy) != copy)
                RefAddOrUpdateSlot(ref slot, key, value);
        }

        /// Update the ref to the slot with the new version - retry if the someone changed the slot in between
        public static void RefAddOrUpdateSlot<V>(ref ImMap<V> slot, int key, V value) =>
            Ref.Swap(ref slot, key, value, (x, k, v) => x.AddOrUpdate(k, v));

        /// Adds a new value for the specified key or keeps the existing map if the key is already in the map.
        [MethodImpl((MethodImplOptions)256)]
        public static void AddOrKeep<V>(this ImMap<V>[] slots, int key, V value, int keyMaskToFindSlot = KEY_MASK_TO_FIND_SLOT)
        {
            ref var slot = ref slots[key & keyMaskToFindSlot];
            var copy = slot;
            if (Interlocked.CompareExchange(ref slot, copy.AddOrKeep(key, value), copy) != copy)
                RefAddOrKeepSlot(ref slot, key, value);
        }

        /// Update the ref to the slot with the new version - retry if the someone changed the slot in between
        public static void RefAddOrKeepSlot<V>(ref ImMap<V> slot, int key, V value) =>
            Ref.Swap(ref slot, key, value, (s, k, v) => s.AddOrKeep(k, v));
    }
}
