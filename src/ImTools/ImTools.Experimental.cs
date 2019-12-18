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
    public sealed class ImMapEntry<V> : ImMap<V>
    {
        /// <inheritdoc />
        public override int Height => 1;

        /// The key
        public readonly int Key;

        /// The value - may be modified if you need a Ref{V} semantics
        public V Value;

        /// Creates the data leaf
        public ImMapEntry(int key, V value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>Creates the data leaf with the default Value, expected to be set afterwards</summary>
        public ImMapEntry(int key) => Key = key;

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
        public readonly ImMapEntry<V> Entry;

        /// Left sub-tree/branch, or empty.
        public ImMap<V> Left;

        /// Right sub-tree/branch, or empty.
        public ImMap<V> Right;

        internal ImMapTree(ImMapEntry<V> entry, ImMap<V> left, ImMap<V> right, int height)
        {
            Entry = entry;
            Left = left;
            Right = right;
            TreeHeight = height;
        }

        internal ImMapTree(ImMapEntry<V> entry, int leftHeight, ImMap<V> left, int rightHeight, ImMap<V> right)
        {
            Entry = entry;
            Left = left;
            Right = right;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapEntry<V> entry, int leftHeight, ImMap<V> left, ImMap<V> right)
        {
            Entry = entry;
            Left = left;
            Right = right;
            var rightHeight = right.Height;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapEntry<V> entry, ImMap<V> left, int rightHeight, ImMap<V> right)
        {
            Entry = entry;
            Left = left;
            Right = right;
            var leftHeight = left.Height;
            TreeHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }

        internal ImMapTree(ImMapEntry<V> entry, ImMapTree<V> left, ImMapTree<V> right)
        {
            Entry = entry;
            Left = left;
            Right = right;
            TreeHeight = left.TreeHeight > right.TreeHeight ? left.TreeHeight + 1 : right.TreeHeight + 1;
        }

        /// <summary>Outputs the key value pair</summary>
        public override string ToString() => 
            "(" + Entry 
            + ") -> (left: " + (Left  is ImMapTree<V> leftTree  ? leftTree.Entry  : Left)
            + ", right: "    + (Right is ImMapTree<V> rightTree ? rightTree.Entry : Right) 
            + ")";

        /// Adds or updates the left or right branch
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapEntry<V> leftLeaf)
                {
                    if (key < leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(leftLeaf, new ImMapEntry<V>(key, value), Entry, 2)
                            : new ImMapTree<V>(Entry, 
                                new ImMapTree<V>(leftLeaf, new ImMapEntry<V>(key, value), Empty, 2), 
                                Right, 3); // given that left is the leaf, the Right tree should be less than 2 - otherwise tree is unbalanced

                    if (key > leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(new ImMapEntry<V>(key, value), left, Entry, 2)
                            : new ImMapTree<V>(Entry,
                                new ImMapTree<V>(leftLeaf, Empty, new ImMapEntry<V>(key, value), 2),
                                Right, 3);
                    
                    return new ImMapTree<V>(Entry, new ImMapEntry<V>(key, value), Right, TreeHeight);
                }

                // when the left is tree the right could not be empty
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Entry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(new ImMapEntry<V>(key, value), leftTree.Left, leftTree.Right, leftTree.TreeHeight),
                            Right, TreeHeight);

                    var newLeftTree = leftTree.AddOrUpdateLeftOrRight(key, value);
                    if (newLeftTree.TreeHeight == leftTree.TreeHeight)
                        return new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight);

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                        return BalanceNewLeftTree(newLeftTree, rightHeight);

                    return new ImMapTree<V>(Entry, newLeftTree.TreeHeight, newLeftTree, rightHeight, Right);
                }

                return new ImMapTree<V>(Entry, new ImMapEntry<V>(key, value), Right, 2);
            }
            else
            {
                var right = Right;
                if (right is ImMapEntry<V> rightLeaf)
                {
                    if (key > rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(rightLeaf, Entry, new ImMapEntry<V>(key, value), 2)
                            : new ImMapTree<V>(Entry, Left, new ImMapTree<V>(rightLeaf, Empty, new ImMapEntry<V>(key, value), 2), 3);

                    if (key < rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(new ImMapEntry<V>(key, value), Entry, right, 2)
                            : new ImMapTree<V>(Entry, Left, new ImMapTree<V>(rightLeaf, new ImMapEntry<V>(key, value), Empty, 2), 3);
                    
                    return new ImMapTree<V>(Entry, Left, new ImMapEntry<V>(key, value), TreeHeight);
                }

                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Entry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(new ImMapEntry<V>(key, value), rightTree.Left, rightTree.Right, rightTree.TreeHeight),
                            TreeHeight);

                    var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, value);
                    if (newRightTree.TreeHeight == rightTree.TreeHeight)
                        return new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight);

                    // right tree is at least 3+ deep - means its either rightLeft or rightRight is tree
                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newRightTree.TreeHeight - 1 > leftHeight)
                        return BalanceNewRightTree(newRightTree, leftHeight);

                    return new ImMapTree<V>(Entry, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                return new ImMapTree<V>(Entry, Left, new ImMapEntry<V>(key, value), 2);
            }
        }

        /// Adds or keeps the left or right branch
        public ImMapTree<V> AddOrKeepLeftOrRight(int key, V value)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapEntry<V> leftLeaf)
                {
                    if (key < leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(leftLeaf, new ImMapEntry<V>(key, value), Entry, 2)
                            : new ImMapTree<V>(Entry,
                                new ImMapTree<V>(leftLeaf, new ImMapEntry<V>(key, value), Empty, 2),
                                Right, 3); // given that left is the leaf, the Right tree should be less than 2 - otherwise tree is unbalanced

                    if (key > leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(new ImMapEntry<V>(key, value), left, Entry, 2)
                            : new ImMapTree<V>(Entry,
                                new ImMapTree<V>(leftLeaf, Empty, new ImMapEntry<V>(key, value), 2),
                                Right, 3);

                    return this;
                }

                // when the left is tree the right could not be empty
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Entry.Key)
                        return this;

                    var newLeftTree = leftTree.AddOrKeepLeftOrRight(key, value);
                    if (newLeftTree == leftTree)
                        return this;

                    if (newLeftTree.TreeHeight == leftTree.TreeHeight)
                        return new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight);

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                        return BalanceNewLeftTree(newLeftTree, rightHeight);

                    return new ImMapTree<V>(Entry, newLeftTree.TreeHeight, newLeftTree, rightHeight, Right);
                }

                return new ImMapTree<V>(Entry, new ImMapEntry<V>(key, value), Right, 2);
            }
            else
            {
                var right = Right;
                if (right is ImMapEntry<V> rightLeaf)
                {
                    if (key > rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(rightLeaf, Entry, new ImMapEntry<V>(key, value), 2)
                            : new ImMapTree<V>(Entry, Left,
                                new ImMapTree<V>(rightLeaf, Empty, new ImMapEntry<V>(key, value), 2), 3);

                    if (key < rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(new ImMapEntry<V>(key, value), Entry, right, 2)
                            : new ImMapTree<V>(Entry, Left,
                                new ImMapTree<V>(rightLeaf, new ImMapEntry<V>(key, value), Empty, 2), 3);

                    return this;
                }

                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Entry.Key)
                        return this;

                    var newRightTree = rightTree.AddOrKeepLeftOrRight(key, value);
                    if (newRightTree == rightTree)
                        return this;
                    
                    if (newRightTree.TreeHeight == rightTree.TreeHeight)
                        return new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight);

                    // right tree is at least 3+ deep - means its either rightLeft or rightRight is tree
                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newRightTree.TreeHeight - 1 > leftHeight)
                        return BalanceNewRightTree(newRightTree, leftHeight);

                    return new ImMapTree<V>(Entry, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                return new ImMapTree<V>(Entry, Left, new ImMapEntry<V>(key, value), 2);
            }
        }

        /// Adds or keeps the left or right branch
        public ImMapTree<V> GetOrAddDefaultLeftOrRight(int key, Ref<ImMapEntry<V>> result)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapEntry<V> leftLeaf)
                {
                    if (key < leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(leftLeaf, result.SetNonAtomic(new ImMapEntry<V>(key)), Entry, 2)
                            : new ImMapTree<V>(Entry,
                                new ImMapTree<V>(leftLeaf, result.SetNonAtomic(new ImMapEntry<V>(key)), Empty, 2),
                                Right, 3); // given that left is the leaf, the Right tree should be less than 2 - otherwise tree is unbalanced

                    if (key > leftLeaf.Key)
                        return Right == Empty
                            ? new ImMapTree<V>(result.SetNonAtomic(new ImMapEntry<V>(key)), left, Entry, 2)
                            : new ImMapTree<V>(Entry,
                                new ImMapTree<V>(leftLeaf, Empty, result.SetNonAtomic(new ImMapEntry<V>(key)), 2),
                                Right, 3);

                    result.SetNonAtomic(leftLeaf);
                    return this;
                }

                // when the left is tree the right could not be empty
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Entry.Key)
                    {
                        result.SetNonAtomic(leftTree.Entry);
                        return this;
                    }

                    var newLeftTree = leftTree.GetOrAddDefaultLeftOrRight(key, result);
                    if (newLeftTree == leftTree)
                        return this;

                    if (newLeftTree.TreeHeight == leftTree.TreeHeight)
                        return new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight);

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                        return BalanceNewLeftTree(newLeftTree, rightHeight);

                    return new ImMapTree<V>(Entry, newLeftTree.TreeHeight, newLeftTree, rightHeight, Right);
                }

                return new ImMapTree<V>(Entry, result.SetNonAtomic(new ImMapEntry<V>(key)), Right, 2);
            }
            else
            {
                var right = Right;
                if (right is ImMapEntry<V> rightLeaf)
                {
                    if (key > rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(rightLeaf, Entry, result.SetNonAtomic(new ImMapEntry<V>(key)), 2)
                            : new ImMapTree<V>(Entry, Left,
                                new ImMapTree<V>(rightLeaf, Empty, result.SetNonAtomic(new ImMapEntry<V>(key)), 2), 3);

                    if (key < rightLeaf.Key)
                        return Left == Empty
                            ? new ImMapTree<V>(result.SetNonAtomic(new ImMapEntry<V>(key)), Entry, right, 2)
                            : new ImMapTree<V>(Entry, Left,
                                new ImMapTree<V>(rightLeaf, result.SetNonAtomic(new ImMapEntry<V>(key)), Empty, 2), 3);

                    result.SetNonAtomic(rightLeaf);
                    return this;
                }

                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Entry.Key)
                    {
                        result.SetNonAtomic(rightTree.Entry);
                        return this;
                    }

                    var newRightTree = rightTree.GetOrAddDefaultLeftOrRight(key, result);
                    if (newRightTree == rightTree)
                        return this;

                    if (newRightTree.TreeHeight == rightTree.TreeHeight)
                        return new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight);

                    // right tree is at least 3+ deep - means its either rightLeft or rightRight is tree
                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 1;
                    if (newRightTree.TreeHeight - 1 > leftHeight)
                        return BalanceNewRightTree(newRightTree, leftHeight);

                    return new ImMapTree<V>(Entry, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                return new ImMapTree<V>(Entry, Left, result.SetNonAtomic(new ImMapEntry<V>(key)), 2);
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
                leftRightTree = new ImMapTree<V>(Entry, leftRightHeight, leftRight, rightHeight, Right);
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
            return new ImMapTree<V>(leftRightTree.Entry,
                newLeftTree,
                new ImMapTree<V>(Entry, leftRightTree.Right, rightHeight, Right));
        }

        private ImMapTree<V> BalanceNewRightTree(ImMapTree<V> newRightTree, int leftHeight)
        {
            var rightRightHeight = (newRightTree.Right as ImMapTree<V>)?.TreeHeight ?? 1;

            var rightLeft = newRightTree.Left;
            var rightLeftTree = rightLeft as ImMapTree<V>;
            var rightLeftHeight = rightLeftTree?.TreeHeight ?? 1;

            if (rightRightHeight >= rightLeftHeight)
            {
                rightLeftTree = new ImMapTree<V>(Entry, leftHeight, Left, rightLeftHeight, rightLeft);
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
            return new ImMapTree<V>(rightLeftTree.Entry,
                new ImMapTree<V>(Entry, leftHeight, Left, rightLeftTree.Left),
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
                ? key == tree.Entry.Key
                    ? new ImMapTree<V>(new ImMapEntry<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
                    : tree.AddOrUpdateLeftOrRight(key, value)
            : map is ImMapEntry<V> data
                ? key > data.Key ? new ImMapTree<V>(data, ImMap<V>.Empty, new ImMapEntry<V>(key, value), 2)
                : key < data.Key ? new ImMapTree<V>(data, new ImMapEntry<V>(key, value), ImMap<V>.Empty, 2)
                : (ImMap<V>)new ImMapEntry<V>(key, value)
            : new ImMapEntry<V>(key, value);

        /// Returns a new map with added value for the specified key or the existing map if the key is already in the map.
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrKeep<V>(this ImMap<V> map, int key, V value) =>
              map is ImMapTree<V> tree
                ? key == tree.Entry.Key
                    ? map
                    : tree.AddOrKeepLeftOrRight(key, value)
            : map is ImMapEntry<V> data
                ? key > data.Key ? new ImMapTree<V>(data, ImMap<V>.Empty, new ImMapEntry<V>(key, value), 2)
                : key < data.Key ? new ImMapTree<V>(data, new ImMapEntry<V>(key, value), ImMap<V>.Empty, 2)
                : map
            : new ImMapEntry<V>(key, value);

        /// <summary>If the the key is present the method returns the data in the result ref,
        /// otherwise it creates a new data with key and default value and sets the result to it</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> GetDataOrAddDefault<V>(this ImMap<V> map, int key, Ref<ImMapEntry<V>> result)
        {
            if (map is ImMapTree<V> tree)
            {
                if (key != tree.Entry.Key)
                    return tree.GetOrAddDefaultLeftOrRight(key, result);
                result.SetNonAtomic(tree.Entry);
                return map;
            }

            if (map is ImMapEntry<V> data)
                return key > data.Key ? new ImMapTree<V>(data, ImMap<V>.Empty,
                        result.SetNonAtomic(new ImMapEntry<V>(key)), 2)
                    : key < data.Key ? new ImMapTree<V>(data, result.SetNonAtomic(new ImMapEntry<V>(key)),
                        ImMap<V>.Empty, 2)
                    : (ImMap<V>)result.SetNonAtomic(data);
            
            return result.SetNonAtomic(new ImMapEntry<V>(key));
        }

        /// <summary> Returns true if key is found and sets the result data. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFindData<V>(this ImMap<V> map, int key, out ImMapEntry<V> result)
        {
            ImMapEntry<V> entry;
            while (map is ImMapTree<V> tree)
            {
                entry = tree.Entry;
                if (key > entry.Key)
                    map = tree.Right;
                else if (key < entry.Key)
                    map = tree.Left;
                else
                {
                    result = entry;
                    return true;
                }
            }

            entry = map as ImMapEntry<V>;
            if (entry != null && entry.Key == key)
            {
                result = entry;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary> Returns true if key is found and sets the value. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMap<V> map, int key, out V value)
        {
            ImMapEntry<V> entry;
            while (map is ImMapTree<V> tree)
            {
                entry = tree.Entry;
                if (key > entry.Key)
                    map = tree.Right;
                else if (key < entry.Key)
                    map = tree.Left;
                else
                {
                    value = entry.Value;
                    return true;
                }
            }

            entry = map as ImMapEntry<V>;
            if (entry != null && entry.Key == key)
            {
                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary> Returns true if key is found and sets the result data. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapEntry<V> GetDataOrDefault<V>(this ImMap<V> map, int key)
        {
            ImMapEntry<V> entry;
            while (map is ImMapTree<V> tree)
            {
                entry = tree.Entry;
                if (key > entry.Key)
                    map = tree.Right;
                else if (key < entry.Key)
                    map = tree.Left;
                else
                    return entry;
            }

            entry = map as ImMapEntry<V>;
            return entry != null && entry.Key == key ? entry : null;
        }

        /// <summary> Returns true if key is found and sets the value. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap<V> map, int key)
        {
            ImMapEntry<V> entry;
            while (map is ImMapTree<V> tree)
            {
                entry = tree.Entry;
                if (key > entry.Key)
                    map = tree.Right;
                else if (key < entry.Key)
                    map = tree.Left;
                else
                    return entry.Value;
            }

            entry = map as ImMapEntry<V>;
            return entry != null && entry.Key == key ? entry.Value : default;
        }

        /// <summary> Returns true if key is found and sets the value. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool Contains<V>(this ImMap<V> map, int key)
        {
            ImMapEntry<V> entry;
            while (map is ImMapTree<V> tree)
            {
                entry = tree.Entry;
                if (key > entry.Key)
                    map = tree.Right;
                else if (key < entry.Key)
                    map = tree.Left;
                else
                    return true;
            }

            entry = map as ImMapEntry<V>;
            return entry != null && entry.Key == key;
        }

        /// <summary>
        /// Folds all the map nodes with the state from the left to the right and from the bottom to the top
        /// You may pass `parentStacks` to reuse the array memory.
        /// NOTE: the length of `parentStack` should be at least of map height, content is not important and could be erased.
        /// </summary>
        public static S Fold<V, S>(this ImMap<V> map, S state, Func<ImMapEntry<V>, S, S> reduce, ImMapTree<V>[] parentStack = null)
        {
            if (map is ImMapEntry<V> data)
                state = reduce(data, state);
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                {
                    if (tree.Left is ImMapEntry<V> ld)
                        state = reduce(ld, state);
                    state = reduce(tree.Entry, state);
                    if (tree.Right is ImMapEntry<V> rd)
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
                                if (tree.Left is ImMapEntry<V> ld)
                                    state = reduce(ld, state);
                                state = reduce(tree.Entry, state);
                                if (tree.Right is ImMapEntry<V> rd)
                                    state = reduce(rd, state);
                                if (parentIndex == -1)
                                    break;
                                tree = parentStack[parentIndex--];
                                state = reduce(tree.Entry, state);
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
                            state = reduce((ImMapEntry<V>)map, state);
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            state = reduce(tree.Entry, state);
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
        public static void Visit<V>(this ImMap<V> map, Action<ImMapEntry<V>> visit, ImMapTree<V>[] parentStack = null)
        {
            if (map is ImMapEntry<V> data)
                visit(data);
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                {
                    if (tree.Left is ImMapEntry<V> ld)
                        visit(ld);
                    visit(tree.Entry);
                    if (tree.Right is ImMapEntry<V> rd)
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
                                if (tree.Left is ImMapEntry<V> ld)
                                    visit(ld);
                                visit(tree.Entry);
                                if (tree.Right is ImMapEntry<V> rd)
                                    visit(rd);
                                if (parentIndex == -1)
                                    break;
                                tree = parentStack[parentIndex--];
                                visit(tree.Entry);
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
                            visit((ImMapEntry<V>)map);
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            visit(tree.Entry);
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
                ? key > tree.Entry.Key ? new ImMapTree<V>(tree.Entry, tree.Left, tree.Right.UpdateImpl(key, value), tree.TreeHeight)
                : key < tree.Entry.Key ? new ImMapTree<V>(tree.Entry, tree.Left.UpdateImpl(key, value), tree.Right, tree.TreeHeight)
                : new ImMapTree<V>(new ImMapEntry<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
            : (ImMap<V>)new ImMapEntry<V>(key, value);
    }

    /// <summary>
    /// The array of ImMap slots where the key first bits are used for FAST slot location
    /// and the slot is the reference to ImMap that can be swapped with its updated value
    /// </summary>
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
