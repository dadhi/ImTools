using System;
using System.Diagnostics;
using System.Runtime.CompilerServices; // for [MethodImpl((MethodImplOptions)256)]

namespace ImTools.Experimental2
{
    /// <summary>
    /// Immutable http://en.wikipedia.org/wiki/AVL_tree with integer keys and <typeparamref name="V"/> values.
    /// </summary>
    public class ImMap<V>
    {
        /// Empty tree to start with.
        public static readonly ImMap<V> Empty = new ImMap<V>();
        
        /// Prevents multiple creation of an empty tree
        protected ImMap() { }

        /// Height of the longest sub-tree/branch. Starts from 2 because it a tree and not the leaf
        public virtual int Height => 0;

        /// Prints "empty"
        public override string ToString() => "empty";
    }

    /// The leaf node - just the key-value pair
    public sealed class ImMapEntry<V> : ImMap<V>
    {
        /// <inheritdoc />
        public override int Height => 1;

        /// The Key is basically the hash, or the Height for ImMapTree
        public readonly int Key;

        /// The value - may be modified if you need a Ref{V} semantics
        public V Value;

        /// <summary>Constructs the pair</summary>
        public ImMapEntry(int key, V value)
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
        public readonly ImMapEntry<V> Entry;

        /// Left sub-tree/branch, or empty.
        public ImMapEntry<V> RightEntry;

        /// Constructor
        public ImMapBranch(ImMapEntry<V> entry, ImMapEntry<V> rightEntry)
        {
            Entry = entry;
            RightEntry = rightEntry;
        }

        /// Creates with data and right data passed in any order. Note: the keys though should no be equal - it should be checked on caller side
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapBranch<V> CreateNormalized(ImMapEntry<V> data1, ImMapEntry<V> data2) => 
            data2.Key > data1.Key 
                ? new ImMapBranch<V>(data1, data2) 
                : new ImMapBranch<V>(data2, data1);

        /// Prints the key value pair
        public override string ToString() => Entry + "->" + RightEntry;
    }

    /// <summary>
    /// The tree always contains Left and Right node, for the missing leaf we have <see cref="ImMapBranch{V}"/>
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

        /// Right sub-tree/branch, or empty.md
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
            Debug.Assert(leftHeight - rightHeight < 2 && rightHeight - leftHeight < 2);
            TreeHeight = 1 + (leftHeight > rightHeight ? leftHeight : rightHeight);
        }

        internal ImMapTree(ImMapEntry<V> entry, int leftHeight, ImMap<V> left, ImMap<V> right)
        {
            Entry = entry;
            Left = left;
            Right = right;
            var rightHeight = right.Height;
            Debug.Assert(leftHeight - rightHeight < 2 && rightHeight - leftHeight < 2);
            TreeHeight = 1 + (leftHeight > rightHeight ? leftHeight : rightHeight);
        }

        internal ImMapTree(ImMapEntry<V> entry, ImMap<V> left, int rightHeight, ImMap<V> right)
        {
            Entry = entry;
            Left = left;
            Right = right;
            var leftHeight = left.Height;
            Debug.Assert(leftHeight - rightHeight < 2 && rightHeight - leftHeight - rightHeight < 2);
            TreeHeight = 1 + (leftHeight > rightHeight ? leftHeight : rightHeight);
        }

        internal ImMapTree(ImMapEntry<V> entry, ImMapEntry<V> leftEntry, ImMapEntry<V> rightEntry)
        {
            Entry = entry;
            Left = leftEntry;
            Right = rightEntry;
            TreeHeight = 2;
        }

        internal ImMapTree(ImMapEntry<V> entry, ImMapTree<V> left, ImMapTree<V> right)
        {
            Entry = entry;
            Left = left;
            Right = right;
            Debug.Assert(left.TreeHeight - right.TreeHeight < 2 && right.TreeHeight - left.TreeHeight < 2);
            TreeHeight = left.TreeHeight > right.TreeHeight ? left.TreeHeight + 1 : right.TreeHeight + 1;
        }

        /// <summary>Outputs the key value pair</summary>
        public override string ToString() =>
            "(" + Entry
                + ") -> (" + (Left is ImMapTree<V> leftTree ? leftTree.Entry  + " height:" + leftTree.TreeHeight  : "" + Left)
                + ", " +   (Right is ImMapTree<V> rightTree ? rightTree.Entry + " height:" + rightTree.TreeHeight : "" + Right)
                + ")";

        /// <summary>Adds or updates the left or right branch</summary>
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, V value)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Entry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(new ImMapEntry<V>(key, value), leftTree.Left, leftTree.Right, leftTree.TreeHeight),
                            Right, TreeHeight);

                    var newLeftTree = leftTree.AddOrUpdateLeftOrRight(key, value);
                    if (newLeftTree.TreeHeight == leftTree.TreeHeight)
                        return new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight);

                    Debug.Assert(newLeftTree.TreeHeight >= 3, "It cannot be a 2 level tree because, 2 level trees a created here on the caller side");
                    if (Right is ImMapEntry<V> rightLeaf)
                    {
                        Debug.Assert(newLeftTree.TreeHeight == 3, "Otherwise it is too un-balanced");
                        if (newLeftTree.Left is ImMapEntry<V> == false)
                        {
                            newLeftTree.Right = new ImMapTree<V>(Entry, newLeftTree.Right, rightLeaf, 2);
                            newLeftTree.TreeHeight = 3;
                            return newLeftTree;
                        }

                        if (newLeftTree.Right is ImMapTree<V> leftRightTree)
                            return new ImMapTree<V>(leftRightTree.Entry,
                                new ImMapTree<V>(newLeftTree.Entry, 1, newLeftTree.Left, leftRightTree.Left),
                                new ImMapTree<V>(Entry, leftRightTree.Right, 1, rightLeaf));

                        var leftRightBranch = (ImMapBranch<V>)newLeftTree.Right;
                        return new ImMapTree<V>(leftRightBranch.Entry,
                            2, ImMapBranch<V>.CreateNormalized(newLeftTree.Entry, (ImMapEntry<V>)newLeftTree.Left),
                            new ImMapTree<V>(Entry, leftRightBranch.RightEntry, rightLeaf));
                    }

                    var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 2;
                    if (newLeftTree.TreeHeight - 1 > rightHeight)
                    {
                        var leftLeftHeight = (newLeftTree.Left as ImMapTree<V>)?.TreeHeight ?? 2;
                        var leftRightHeight = (newLeftTree.Right as ImMapTree<V>)?.TreeHeight ?? 2;
                        if (leftLeftHeight >= leftRightHeight)
                        {
                            var newLeftRightTree = new ImMapTree<V>(Entry, newLeftTree.Right, Right, leftRightHeight + 1);
                            newLeftTree.Right = newLeftRightTree;
                            newLeftTree.TreeHeight = 1 + (leftLeftHeight >= newLeftRightTree.TreeHeight ? leftLeftHeight : newLeftRightTree.TreeHeight);
                            return newLeftTree;
                        }

                        var leftRightTree = (ImMapTree<V>)newLeftTree.Right;
                        return new ImMapTree<V>(leftRightTree.Entry,
                            new ImMapTree<V>(newLeftTree.Entry, leftLeftHeight, newLeftTree.Left, leftRightTree.Left),
                            new ImMapTree<V>(Entry, leftRightTree.Right, rightHeight, Right));
                    }

                    return new ImMapTree<V>(Entry, newLeftTree.TreeHeight, newLeftTree, rightHeight, Right);
                }

                if (left is ImMapBranch<V> leftBranch)
                {
                    if (key < leftBranch.Entry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(leftBranch.Entry, new ImMapEntry<V>(key, value), leftBranch.RightEntry),
                            Right, TreeHeight);

                    if (key > leftBranch.Entry.Key)
                    {
                        var newLeft =
                            //            5                         5
                            //       2        ?  =>             3        ?
                            //         3                      2   4
                            //          4
                            key > leftBranch.RightEntry.Key
                                ? new ImMapTree<V>(leftBranch.RightEntry, leftBranch.Entry, new ImMapEntry<V>(key, value))
                                //            5                         5
                                //      2          ?  =>            2.5        ?
                                //          3                      2   3
                                //       2.5  
                                : key < leftBranch.RightEntry.Key
                                    ? new ImMapTree<V>(new ImMapEntry<V>(key, value), leftBranch.Entry, leftBranch.RightEntry)
                                    : (ImMap<V>)new ImMapBranch<V>(leftBranch.Entry, new ImMapEntry<V>(key, value));

                        return new ImMapTree<V>(Entry, newLeft, Right, TreeHeight);
                    }

                    return new ImMapTree<V>(Entry, new ImMapBranch<V>(new ImMapEntry<V>(key, value), leftBranch.RightEntry), Right, TreeHeight);
                }

                var leftLeaf = (ImMapEntry<V>)left;
                return key > leftLeaf.Key
                        ? new ImMapTree<V>(Entry, new ImMapBranch<V>(leftLeaf, new ImMapEntry<V>(key, value)), Right, 3)
                    : key < leftLeaf.Key
                        ? new ImMapTree<V>(Entry, new ImMapBranch<V>(new ImMapEntry<V>(key, value), leftLeaf), Right, 3)
                    : new ImMapTree<V>(Entry, new ImMapEntry<V>(key, value), Right, TreeHeight);
            }
            else
            {
                var right = Right;
                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Entry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(new ImMapEntry<V>(key, value), rightTree.Left, rightTree.Right, rightTree.TreeHeight),
                            TreeHeight);

                    // note: tree always contains left and right (for the missing leaf we have a Branch)
                    var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, value);
                    if (newRightTree.TreeHeight == rightTree.TreeHeight)
                        return new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight);

                    if (Left is ImMapEntry<V> leftLeaf)
                    {
                        // here we need to re-balance by default, because the new right tree is at least 3 level (actually exactly 3 or it would be too unbalanced)
                        // double rotation needed if only the right-right is a leaf
                        if (newRightTree.Right is ImMapEntry<V> == false)
                        {
                            newRightTree.Left = new ImMapTree<V>(Entry, leftLeaf, newRightTree.Left, 2);
                            newRightTree.TreeHeight = 3;
                            return newRightTree;
                        }

                        if (newRightTree.Left is ImMapTree<V> rightLeftTree)
                            return new ImMapTree<V>(rightLeftTree.Entry,
                                new ImMapTree<V>(Entry, 1, leftLeaf, rightLeftTree.Left),
                                new ImMapTree<V>(newRightTree.Entry, rightLeftTree.Right, 1, newRightTree.Right));

                        var rightLeftBranch = (ImMapBranch<V>)newRightTree.Left;
                        return new ImMapTree<V>(rightLeftBranch.Entry,
                            2, ImMapBranch<V>.CreateNormalized(Entry, leftLeaf),
                            new ImMapTree<V>(newRightTree.Entry, rightLeftBranch.RightEntry, (ImMapEntry<V>)newRightTree.Right));
                    }

                    var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 2;
                    if (newRightTree.TreeHeight > leftHeight + 1)
                    {
                        var rightRightHeight = (newRightTree.Right as ImMapTree<V>)?.TreeHeight ?? 2;
                        var rightLeftHeight = (newRightTree.Left as ImMapTree<V>)?.TreeHeight ?? 2;
                        if (rightRightHeight >= rightLeftHeight)
                        {
                            Debug.Assert(rightLeftHeight >= leftHeight, "The whole rightHeight > leftHeight by 2, and rightRight >= leftHeight but not more than by 2");
                            var newRightLeftTree = new ImMapTree<V>(Entry, Left, newRightTree.Left, rightLeftHeight + 1);
                            newRightTree.Left = newRightLeftTree;
                            newRightTree.TreeHeight = 1 + (rightRightHeight >= newRightLeftTree.TreeHeight ? rightRightHeight : newRightLeftTree.TreeHeight);
                            return newRightTree;
                        }

                        var rightLeftTree = (ImMapTree<V>)newRightTree.Left;
                        return new ImMapTree<V>(rightLeftTree.Entry,
                            new ImMapTree<V>(Entry, leftHeight, Left, rightLeftTree.Left),
                            new ImMapTree<V>(newRightTree.Entry, rightLeftTree.Right, rightRightHeight, newRightTree.Right));
                    }

                    return new ImMapTree<V>(Entry, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
                }

                if (right is ImMapBranch<V> rightBranch)
                {
                    if (key > rightBranch.Entry.Key)
                    {
                        var newRight =
                            //      5                5      
                            //  ?       6    =>  ?       8  
                            //            8            6   !
                            //              !               
                            key > rightBranch.RightEntry.Key
                                ? new ImMapTree<V>(rightBranch.RightEntry, rightBranch.Entry, new ImMapEntry<V>(key, value))
                            //      5                 5      
                            //  ?       6     =>  ?       7  
                            //              8            6  8
                            //            7               
                            : key < rightBranch.RightEntry.Key
                                ? new ImMapTree<V>(new ImMapEntry<V>(key, value), rightBranch.Entry, rightBranch.RightEntry)
                            : (ImMap<V>)new ImMapBranch<V>(rightBranch.Entry, new ImMapEntry<V>(key, value));

                        return new ImMapTree<V>(Entry, Left, newRight, TreeHeight);
                    }

                    if (key < rightBranch.Entry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.Entry, new ImMapEntry<V>(key, value), rightBranch.RightEntry),
                            TreeHeight);

                    return new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(new ImMapEntry<V>(key, value), rightBranch.RightEntry), TreeHeight);
                }

                var rightLeaf = (ImMapEntry<V>)right;
                return key > rightLeaf.Key
                    ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(rightLeaf, new ImMapEntry<V>(key, value)), 3)
                    : key < rightLeaf.Key
                        ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(new ImMapEntry<V>(key, value), rightLeaf), 3)
                        : new ImMapTree<V>(Entry, Left, new ImMapEntry<V>(key, value), TreeHeight);
            }
        }
    }

    /// ImMap methods
    public static class ImMap
    {
        /// <summary> Adds or updates the value by key in the map, always returns a modified map </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value)
        {
            if (map == ImMap<V>.Empty)
                return new ImMapEntry<V>(key, value);

            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key
                    ? new ImMapBranch<V>(leaf, new ImMapEntry<V>(key, value))
                    : key < leaf.Key
                        ? new ImMapBranch<V>(new ImMapEntry<V>(key, value), leaf)
                        : (ImMap<V>)new ImMapEntry<V>(key, value);

            if (map is ImMapBranch<V> branch)
            {
                if (key > branch.Entry.Key)
                    //   5                  10
                    //        10     =>  5     11
                    //           11           
                    return key > branch.RightEntry.Key
                        ? new ImMapTree<V>(branch.RightEntry, branch.Entry, new ImMapEntry<V>(key, value))
                        //   5               7
                        //        10  =>  5     10
                        //      7           
                        : key < branch.RightEntry.Key // rotate if right
                            ? new ImMapTree<V>(new ImMapEntry<V>(key, value), branch.Entry, branch.RightEntry)
                            : (ImMap<V>)new ImMapBranch<V>(branch.Entry, new ImMapEntry<V>(key, value));

                if (key < branch.Entry.Key)
                    return new ImMapTree<V>(branch.Entry, new ImMapEntry<V>(key, value), branch.RightEntry);

                return new ImMapBranch<V>(new ImMapEntry<V>(key, value), branch.RightEntry);
            }

            var tree = (ImMapTree<V>)map;
            return key == tree.Entry.Key
                ? new ImMapTree<V>(new ImMapEntry<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
                : tree.AddOrUpdateLeftOrRight(key, value);
        }

        /// <summary>
        /// Returns true if key is found and sets the value.
        /// </summary>
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

            if (map is ImMapBranch<V> branch)
            {
                if (branch.Entry.Key == key)
                {
                    value = branch.Entry.Value;
                    return true;
                }

                if (branch.RightEntry.Key == key)
                {
                    value = branch.RightEntry.Value;
                    return true;
                }

                value = default;
                return false;
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

        /// <summary> Returns the data slot if key is found or null otherwise. </summary>
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

            if (map is ImMapBranch<V> branch)
                return branch.Entry.Key == key ? branch.Entry
                    : branch.RightEntry.Key == key ? branch.RightEntry
                    : null;

            entry = map as ImMapEntry<V>;
            return entry != null && entry.Key == key ? entry : null;
        }

        /// <summary> Returns the value if key is found or default value otherwise. </summary>
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

            if (map is ImMapBranch<V> branch)
                return branch.Entry.Key == key ? branch.Entry.Value 
                    : branch.RightEntry.Key == key ? branch.RightEntry.Value 
                    : default;

            entry = map as ImMapEntry<V>;
            if (entry != null && entry.Key == key)
                return entry.Value;

            return default;
        }

        /// <summary>
        /// Folds all the map nodes with the state from left to right and from the bottom to top
        /// You may pass `parentStacks` to reuse the array memory.
        /// NOTE: the length of `parentStack` should be at least of map (height - 2) - the stack want be used for 0, 1, 2 height maps,
        /// the content of the stack is not important and could be erased.
        /// </summary>
        public static S Fold<V, S>(this ImMap<V> map, S state, Func<ImMapEntry<V>, S, S> reduce, ImMapTree<V>[] parentStack = null)
        {
            if (map == ImMap<V>.Empty)
                return state;

            if (map is ImMapEntry<V> leaf)
                state = reduce(leaf, state);
            else if (map is ImMapBranch<V> branch)
                state = reduce(branch.RightEntry, reduce(branch.Entry, state));
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                    state = reduce((ImMapEntry<V>) tree.Right, reduce(tree.Entry, reduce((ImMapEntry<V>) tree.Left, state)));
                else
                {
                    parentStack = parentStack ?? new ImMapTree<V>[tree.TreeHeight - 2];
                    var parentIndex = -1;
                    while (true)
                    {
                        if ((tree = map as ImMapTree<V>) != null)
                        {
                            if (tree.TreeHeight == 2)
                            {
                                state = reduce((ImMapEntry<V>)tree.Right, reduce(tree.Entry, reduce((ImMapEntry<V>)tree.Left, state)));
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
                        else if ((branch = map as ImMapBranch<V>) != null)
                        {
                            state = reduce(branch.RightEntry, reduce(branch.Entry, state));
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            state = reduce(tree.Entry, state);
                            map = tree.Right;
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
                }
            }

            return state;
        }
    }
}
