using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Experimental
{
    /// <summary>
    /// Immutable http://en.wikipedia.org/wiki/AVL_tree with integer keys and <typeparamref name="V"/> values.
    /// </summary>
    public class ImMap<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap<V> Empty = new ImMap<V>();
        
        /// Prevents multiple creation of an empty tree
        protected ImMap() { }

        /// <summary>Height of the longest sub-tree/branch - 0 for the empty tree</summary>
        public virtual int Height => 0;

        /// <summary>Prints "empty"</summary>
        public override string ToString() => "empty";
    }

    /// <summary>Wraps the stored data with "fixed" reference semantics - when added to the tree it did not change or reconstructed in memory</summary>
    public sealed class ImMapEntry<V> : ImMap<V>
    {
        /// <inheritdoc />
        public override int Height => 1;

        /// The Key is basically the hash, or the Height for ImMapTree
        public readonly int Key;

        /// The value - may be modified if you need a Ref{V} semantics
        public V Value;

        /// <summary>Constructs the entry with the default value</summary>
        public ImMapEntry(int key) => Key = key;

        /// <summary>Constructs the entry</summary>
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

        /// <summary>Outputs the brief tree info - mostly for debugging purposes</summary>
        public override string ToString() =>
            "(" + Entry
                + ") -> (" + (Left is ImMapTree<V> leftTree ? leftTree.Entry  + " height:" + leftTree.TreeHeight  : "" + Left)
                + ", " +   (Right is ImMapTree<V> rightTree ? rightTree.Entry + " height:" + rightTree.TreeHeight : "" + Right)
                + ")";

        /// <summary>Adds or updates the left or right branch</summary>
        public ImMapTree<V> AddOrUpdateLeftOrRight(int key, ImMapEntry<V> entry)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Entry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(entry, leftTree.Left, leftTree.Right, leftTree.TreeHeight),
                            Right, TreeHeight);

                    var newLeftTree = leftTree.AddOrUpdateLeftOrRight(key, entry);
                    return newLeftTree.TreeHeight == leftTree.TreeHeight 
                        ? new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight) 
                        : BalanceNewLeftTree(newLeftTree);
                }

                if (left is ImMapBranch<V> leftBranch)
                {
                    if (key < leftBranch.Entry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(leftBranch.Entry, entry, leftBranch.RightEntry),
                            Right, TreeHeight);

                    if (key > leftBranch.Entry.Key)
                    {
                        var newLeft =
                            //            5                         5
                            //       2        ?  =>             3        ?
                            //         3                      2   4
                            //          4
                            key > leftBranch.RightEntry.Key ? new ImMapTree<V>(leftBranch.RightEntry, leftBranch.Entry, entry)
                                //            5                         5
                                //      2          ?  =>            2.5        ?
                                //          3                      2   3
                                //       2.5  
                            : key < leftBranch.RightEntry.Key ? new ImMapTree<V>(entry, leftBranch.Entry, leftBranch.RightEntry)
                            : (ImMap<V>)new ImMapBranch<V>(leftBranch.Entry, entry);

                        return new ImMapTree<V>(Entry, newLeft, Right, TreeHeight);
                    }

                    return new ImMapTree<V>(Entry, 
                        new ImMapBranch<V>(entry, leftBranch.RightEntry), Right, TreeHeight);
                }

                var leftLeaf = (ImMapEntry<V>)left;
                return key > leftLeaf.Key ? new ImMapTree<V>(Entry, new ImMapBranch<V>(leftLeaf, entry), Right, 3)
                    :  key < leftLeaf.Key ? new ImMapTree<V>(Entry, new ImMapBranch<V>(entry, leftLeaf), Right, 3)
                    : new ImMapTree<V>(Entry, entry, Right, TreeHeight);
            }
            else
            {
                var right = Right;
                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Entry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(entry, rightTree.Left, rightTree.Right, rightTree.TreeHeight),
                            TreeHeight);

                    var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, entry);
                    return newRightTree.TreeHeight == rightTree.TreeHeight 
                        ? new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight) 
                        : BalanceNewRightTree(newRightTree);
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
                            key > rightBranch.RightEntry.Key ? new ImMapTree<V>(rightBranch.RightEntry, rightBranch.Entry, entry)
                            //      5                 5      
                            //  ?       6     =>  ?       7  
                            //              8            6  8
                            //            7               
                            : key < rightBranch.RightEntry.Key ? new ImMapTree<V>(entry, rightBranch.Entry, rightBranch.RightEntry)
                            : (ImMap<V>)new ImMapBranch<V>(rightBranch.Entry, entry);

                        return new ImMapTree<V>(Entry, Left, newRight, TreeHeight);
                    }

                    if (key < rightBranch.Entry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.Entry, entry, rightBranch.RightEntry),
                            TreeHeight);

                    return new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(entry, rightBranch.RightEntry), TreeHeight);
                }

                var rightLeaf = (ImMapEntry<V>)right;
                return key > rightLeaf.Key
                        ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(rightLeaf, entry), 3)
                    : key < rightLeaf.Key
                        ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(entry, rightLeaf), 3)
                        : new ImMapTree<V>(Entry, Left, entry, TreeHeight);
            }
        }

        /// <summary>Adds the left or right branch</summary>
        public ImMapTree<V> AddUnsafeLeftOrRight(int key, ImMapEntry<V> entry)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapTree<V> leftTree)
                {
                    var newLeftTree = leftTree.AddUnsafeLeftOrRight(key, entry);
                    return newLeftTree.TreeHeight == leftTree.TreeHeight
                        ? new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight)
                        : BalanceNewLeftTree(newLeftTree);
                }

                if (left is ImMapBranch<V> leftBranch)
                {
                    if (key < leftBranch.Entry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(leftBranch.Entry, entry, leftBranch.RightEntry),
                            Right, TreeHeight);

                    var newLeft = key > leftBranch.RightEntry.Key
                            ? new ImMapTree<V>(leftBranch.RightEntry, leftBranch.Entry, entry)
                            : new ImMapTree<V>(entry, leftBranch.Entry, leftBranch.RightEntry);

                    return new ImMapTree<V>(Entry, newLeft, Right, TreeHeight);
                }

                var leftLeaf = (ImMapEntry<V>)left;
                return key > leftLeaf.Key
                    ? new ImMapTree<V>(Entry, new ImMapBranch<V>(leftLeaf, entry), Right, 3)
                    : new ImMapTree<V>(Entry, new ImMapBranch<V>(entry, leftLeaf), Right, 3);
            }
            else
            {
                var right = Right;
                if (right is ImMapTree<V> rightTree)
                {
                    var newRightTree = rightTree.AddUnsafeLeftOrRight(key, entry);
                    return newRightTree.TreeHeight == rightTree.TreeHeight
                        ? new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight)
                        : BalanceNewRightTree(newRightTree);
                }

                if (right is ImMapBranch<V> rightBranch)
                {
                    if (key > rightBranch.Entry.Key)
                    {
                        var newRight = key > rightBranch.RightEntry.Key
                            ? new ImMapTree<V>(rightBranch.RightEntry, rightBranch.Entry, entry)
                            : new ImMapTree<V>(entry, rightBranch.Entry, rightBranch.RightEntry);

                        return new ImMapTree<V>(Entry, Left, newRight, TreeHeight);
                    }

                    return new ImMapTree<V>(Entry, Left,
                        new ImMapTree<V>(rightBranch.Entry, entry, rightBranch.RightEntry),
                        TreeHeight);
                }

                var rightLeaf = (ImMapEntry<V>)right;
                return key > rightLeaf.Key
                    ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(rightLeaf, entry), 3)
                    : new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(entry, rightLeaf), 3);
            }
        }

        /// <summary>Adds to the left or right branch, or keeps the un-modified map</summary>
        public ImMapTree<V> AddOrKeepLeftOrRight(int key, V value)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Entry.Key)
                        return this;

                    var newLeftTree = leftTree.AddOrKeepLeftOrRight(key, value);
                    return newLeftTree == leftTree ? this
                        : newLeftTree.TreeHeight == leftTree.TreeHeight
                            ? new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight)
                            : BalanceNewLeftTree(newLeftTree);
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
                            : this;

                        return new ImMapTree<V>(Entry, newLeft, Right, TreeHeight);
                    }

                    return this;
                }

                var leftLeaf = (ImMapEntry<V>)left;
                return key > leftLeaf.Key
                        ? new ImMapTree<V>(Entry, new ImMapBranch<V>(leftLeaf, new ImMapEntry<V>(key, value)), Right, 3)
                    : key < leftLeaf.Key
                        ? new ImMapTree<V>(Entry, new ImMapBranch<V>(new ImMapEntry<V>(key, value), leftLeaf), Right, 3)
                    : this;
            }
            else
            {
                var right = Right;
                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Entry.Key)
                        return this;

                    // note: tree always contains left and right (for the missing leaf we have a Branch)
                    var newRightTree = rightTree.AddOrKeepLeftOrRight(key, value);
                    return newRightTree == rightTree ? this
                        : newRightTree.TreeHeight == rightTree.TreeHeight
                            ? new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight)
                            : BalanceNewRightTree(newRightTree);
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
                            : this;

                        return new ImMapTree<V>(Entry, Left, newRight, TreeHeight);
                    }

                    return key < rightBranch.Entry.Key
                        ? new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.Entry, new ImMapEntry<V>(key, value), rightBranch.RightEntry),
                            TreeHeight)
                        : this;
                }

                var rightLeaf = (ImMapEntry<V>)right;
                return key > rightLeaf.Key
                    ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(rightLeaf, new ImMapEntry<V>(key, value)), 3)
                    : key < rightLeaf.Key
                        ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(new ImMapEntry<V>(key, value), rightLeaf), 3)
                    : this;
            }
        }

        /// <summary>Adds to the left or right branch, or keeps the un-modified map</summary>
        public ImMapTree<V> AddOrKeepLeftOrRight(int key)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Entry.Key)
                        return this;

                    var newLeftTree = leftTree.AddOrKeepLeftOrRight(key);
                    return newLeftTree == leftTree ? this
                        : newLeftTree.TreeHeight == leftTree.TreeHeight
                            ? new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight)
                            : BalanceNewLeftTree(newLeftTree);
                }

                if (left is ImMapBranch<V> leftBranch)
                {
                    if (key < leftBranch.Entry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(leftBranch.Entry, new ImMapEntry<V>(key), leftBranch.RightEntry),
                            Right, TreeHeight);

                    if (key > leftBranch.Entry.Key)
                    {
                        var newLeft =
                            //            5                         5
                            //       2        ?  =>             3        ?
                            //         3                      2   4
                            //          4
                            key > leftBranch.RightEntry.Key
                                ? new ImMapTree<V>(leftBranch.RightEntry, leftBranch.Entry, new ImMapEntry<V>(key))
                            //            5                         5
                            //      2          ?  =>            2.5        ?
                            //          3                      2   3
                            //       2.5  
                            : key < leftBranch.RightEntry.Key
                                ? new ImMapTree<V>(new ImMapEntry<V>(key), leftBranch.Entry, leftBranch.RightEntry)
                            : this;

                        return new ImMapTree<V>(Entry, newLeft, Right, TreeHeight);
                    }

                    return this;
                }

                var leftLeaf = (ImMapEntry<V>)left;
                return key > leftLeaf.Key
                        ? new ImMapTree<V>(Entry, new ImMapBranch<V>(leftLeaf, new ImMapEntry<V>(key)), Right, 3)
                    : key < leftLeaf.Key
                        ? new ImMapTree<V>(Entry, new ImMapBranch<V>(new ImMapEntry<V>(key), leftLeaf), Right, 3)
                    : this;
            }
            else
            {
                var right = Right;
                if (right is ImMapTree<V> rightTree)
                {
                    if (key == rightTree.Entry.Key)
                        return this;

                    // note: tree always contains left and right (for the missing leaf we have a Branch)
                    var newRightTree = rightTree.AddOrKeepLeftOrRight(key);
                    return newRightTree == rightTree ? this
                        : newRightTree.TreeHeight == rightTree.TreeHeight
                            ? new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight)
                            : BalanceNewRightTree(newRightTree);
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
                                ? new ImMapTree<V>(rightBranch.RightEntry, rightBranch.Entry, new ImMapEntry<V>(key))
                            //      5                 5      
                            //  ?       6     =>  ?       7  
                            //              8            6  8
                            //            7               
                            : key < rightBranch.RightEntry.Key
                                ? new ImMapTree<V>(new ImMapEntry<V>(key), rightBranch.Entry, rightBranch.RightEntry)
                            : this;

                        return new ImMapTree<V>(Entry, Left, newRight, TreeHeight);
                    }

                    return key < rightBranch.Entry.Key
                        ? new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.Entry, new ImMapEntry<V>(key), rightBranch.RightEntry),
                            TreeHeight)
                        : this;
                }

                var rightLeaf = (ImMapEntry<V>)right;
                return key > rightLeaf.Key
                    ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(rightLeaf, new ImMapEntry<V>(key)), 3)
                    : key < rightLeaf.Key
                        ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(new ImMapEntry<V>(key), rightLeaf), 3)
                    : this;
            }
        }

        private ImMapTree<V> BalanceNewLeftTree(ImMapTree<V> newLeftTree)
        {
            Debug.Assert(newLeftTree.TreeHeight >= 3, "It cannot be a 2 level tree because, 2 level trees a created here on the caller side");
            if (Right is ImMapEntry<V> rightLeaf)
            {
                // todo: optimize the same way as below
                Debug.Assert(newLeftTree.TreeHeight == 3, "Otherwise it is too un-balanced");
                if (newLeftTree.Left is ImMapEntry<V> leftLeftLeaf)
                {
                    //            30                    15
                    //    10            40 =>    10           20
                    //  5     15               5    12     30    40
                    //     12   20                     
                    if (newLeftTree.Right is ImMapTree<V> leftRightTree)
                    {
                        newLeftTree.Right = leftRightTree.Left;
                        newLeftTree.TreeHeight = 2;
                        return new ImMapTree<V>(leftRightTree.Entry,
                            newLeftTree,
                            new ImMapTree<V>(Entry, leftRightTree.Right, rightLeaf, 2),
                            3);
                    }

                    // we cannot reuse the new left tree here because it is reduced into the branch
                    //           30                     15
                    //    10            40 =>    5           20
                    //  5    15                    10     30    40
                    //         20                     
                    var leftRightBranch = (ImMapBranch<V>)newLeftTree.Right;
                    return new ImMapTree<V>(leftRightBranch.Entry,
                        new ImMapBranch<V>(leftLeftLeaf, newLeftTree.Entry), 
                        new ImMapTree<V>(Entry, leftRightBranch.RightEntry, rightLeaf),
                        3);
                }

                newLeftTree.Right = new ImMapTree<V>(Entry, newLeftTree.Right, rightLeaf, 2);
                newLeftTree.TreeHeight = 3;
                return newLeftTree;
            }

            var rightHeight = (Right as ImMapTree<V>)?.TreeHeight ?? 2;
            if (newLeftTree.TreeHeight - 1 > rightHeight)
            {
                var leftLeftHeight = (newLeftTree.Left as ImMapTree<V>)?.TreeHeight ?? 2;
                var leftRightHeight = (newLeftTree.Right as ImMapTree<V>)?.TreeHeight ?? 2;
                if (leftLeftHeight < leftRightHeight)
                {
                    var leftRightTree = (ImMapTree<V>)newLeftTree.Right;

                    newLeftTree.Right = leftRightTree.Left;
                    newLeftTree.TreeHeight = leftLeftHeight + 1;
                    return new ImMapTree<V>(leftRightTree.Entry,
                        newLeftTree,
                        new ImMapTree<V>(Entry, leftRightTree.Right, Right, rightHeight + 1),
                        leftLeftHeight + 2);

                    //return new ImMapTree<V>(leftRightTree.Entry,
                    //    new ImMapTree<V>(newLeftTree.Entry, leftLeftHeight, newLeftTree.Left, leftRightTree.Left),
                    //    new ImMapTree<V>(Entry, leftRightTree.Right, rightHeight, Right));
                }

                newLeftTree.Right = new ImMapTree<V>(Entry, newLeftTree.Right, Right, leftRightHeight + 1);
                newLeftTree.TreeHeight = leftRightHeight + 2;
                return newLeftTree;
            }

            return new ImMapTree<V>(Entry, newLeftTree.TreeHeight, newLeftTree, rightHeight, Right);
        }

        private ImMapTree<V> BalanceNewRightTree(ImMapTree<V> newRightTree)
        {
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

                //        20                        30       
                // 10             40     =>    20        40  
                //            30      50     10  25    35  50
                //          25  35                           
                if (newRightTree.Left is ImMapTree<V> rightLeftTree)
                {
                    newRightTree.Left = rightLeftTree.Right;
                    newRightTree.TreeHeight = 2;
                    return new ImMapTree<V>(rightLeftTree.Entry,
                        new ImMapTree<V>(Entry, leftLeaf, rightLeftTree.Left, 2),
                        newRightTree, 3);
                }

                //        20                        30       
                // 10             40     =>    10        40  
                //            30      50         20    35  50
                //              35                           
                var rightLeftBranch = (ImMapBranch<V>)newRightTree.Left;
                newRightTree.Left = rightLeftBranch.RightEntry;
                newRightTree.TreeHeight = 2;
                return new ImMapTree<V>(rightLeftBranch.Entry,
                    new ImMapBranch<V>(leftLeaf, Entry),
                    newRightTree, 3);
            }

            var leftHeight = (Left as ImMapTree<V>)?.TreeHeight ?? 2;
            if (newRightTree.TreeHeight > leftHeight + 1)
            {
                var rightRightHeight = (newRightTree.Right as ImMapTree<V>)?.TreeHeight ?? 2;
                var rightLeftHeight = (newRightTree.Left as ImMapTree<V>)?.TreeHeight ?? 2;
                if (rightRightHeight < rightLeftHeight)
                {
                    var rightLeftTree = (ImMapTree<V>)newRightTree.Left;
                    newRightTree.Left = rightLeftTree.Right;
                    // the height now should be defined by rr - because left now is shorter by 1
                    newRightTree.TreeHeight = rightRightHeight + 1;
                    // the whole height consequentially can be defined by `newRightTree` (rr+1) because left is consist of short Left and -2 rl.Left
                    return new ImMapTree<V>(rightLeftTree.Entry,
                        // Left should be >= rightLeft.Left because it maybe rightLeft.Right which defines rl height
                        new ImMapTree<V>(Entry, Left, rightLeftTree.Left, height: leftHeight + 1),
                        newRightTree,
                        rightRightHeight + 2);

                    //return new ImMapTree<V>(rightLeftTree.Entry,
                    //    new ImMapTree<V>(Entry, leftHeight, Left, rightLeftTree.Left),
                    //    new ImMapTree<V>(newRightTree.Entry, rightLeftTree.Right, rightRightHeight, newRightTree.Right));
                }

                Debug.Assert(rightLeftHeight >= leftHeight, "The whole rightHeight > leftHeight by 2, and rightRight >= leftHeight but not more than by 2");

                // we may decide on the height because the Left smaller by 2
                newRightTree.Left = new ImMapTree<V>(Entry, Left, newRightTree.Left, rightLeftHeight + 1);
                // if rr was > rl by 1 than new rl+1 should be equal height to rr now, if rr was == rl than new rl wins anyway
                newRightTree.TreeHeight = rightLeftHeight + 2;
                return newRightTree;
            }

            return new ImMapTree<V>(Entry, leftHeight, Left, newRightTree.TreeHeight, newRightTree);
        }
    }

    /// <summary>ImMap methods</summary>
    public static class ImMap
    {
        /// <summary> Adds or updates the value by key in the map, always returns a modified map </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdateEntry<V>(this ImMap<V> map, ImMapEntry<V> entry)
        {
            if (map == ImMap<V>.Empty)
                return entry;

            var key = entry.Key;
            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key ? new ImMapBranch<V>(leaf, entry)
                    :  key < leaf.Key ? new ImMapBranch<V>(entry, leaf)
                    : (ImMap<V>)entry;

            if (map is ImMapBranch<V> branch)
            {
                if (key > branch.Entry.Key)
                        //   5                  10
                        //        10     =>  5     11
                        //           11           
                    return key > branch.RightEntry.Key
                        ? new ImMapTree<V>(branch.RightEntry, branch.Entry, entry)
                        //   5               7
                        //        10  =>  5     10
                        //      7           
                        : key < branch.RightEntry.Key // rotate if right
                            ? new ImMapTree<V>(entry, branch.Entry, branch.RightEntry)
                            : (ImMap<V>)new ImMapBranch<V>(branch.Entry, entry);

                return key < branch.Entry.Key
                    ? new ImMapTree<V>(branch.Entry, entry, branch.RightEntry)
                    : (ImMap<V>)new ImMapBranch<V>(entry, branch.RightEntry);
            }

            var tree = (ImMapTree<V>)map;
            return key == tree.Entry.Key
                ? new ImMapTree<V>(entry, tree.Left, tree.Right, tree.TreeHeight)
                : tree.AddOrUpdateLeftOrRight(key, entry);
        }

        /// <summary> Adds or updates the value by key in the map, always returns a modified map </summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value) =>
            map.AddOrUpdateEntry(new ImMapEntry<V>(key, value));

        /// <summary> Adds the value by key in the map - ASSUMES that the key is not in the map, always returns a modified map </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddEntryUnsafe<V>(this ImMap<V> map, ImMapEntry<V> entry)
        {
            if (map == ImMap<V>.Empty)
                return entry;

            var key = entry.Key;
            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key 
                    ? new ImMapBranch<V>(leaf, entry) 
                    : new ImMapBranch<V>(entry, leaf);

            if (map is ImMapBranch<V> branch)
                return key > branch.Entry.Key
                    ? key > branch.RightEntry.Key
                        ? new ImMapTree<V>(branch.RightEntry, branch.Entry, entry)
                        : new ImMapTree<V>(entry, branch.Entry, branch.RightEntry)
                    : new ImMapTree<V>(branch.Entry, entry, branch.RightEntry);

            return ((ImMapTree<V>)map).AddUnsafeLeftOrRight(key, entry);
        }

        /// <summary> Adds the value for the key or returns the un-modified map if key is already present </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrKeep<V>(this ImMap<V> map, int key, V value)
        {
            if (map == ImMap<V>.Empty)
                return new ImMapEntry<V>(key, value);

            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key ? new ImMapBranch<V>(leaf, new ImMapEntry<V>(key, value))
                    :  key < leaf.Key ? new ImMapBranch<V>(new ImMapEntry<V>(key, value), leaf)
                    : map;

            if (map is ImMapBranch<V> branch)
            {
                if (key > branch.Entry.Key)
                    return key > branch.RightEntry.Key
                        ? new ImMapTree<V>(branch.RightEntry, branch.Entry, new ImMapEntry<V>(key, value))
                    : key < branch.RightEntry.Key // rotate if right
                        ? new ImMapTree<V>(new ImMapEntry<V>(key, value), branch.Entry, branch.RightEntry)
                    : map;

                return key < branch.Entry.Key
                    ? new ImMapTree<V>(branch.Entry, new ImMapEntry<V>(key, value), branch.RightEntry)
                    : map;
            }

            var tree = (ImMapTree<V>)map;
            return key != tree.Entry.Key
                ? tree.AddOrKeepLeftOrRight(key, value)
                : map;
        }

        /// <summary> Adds the entry with default value for the key or returns the un-modified map if key is already present </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrKeep<V>(this ImMap<V> map, int key)
        {
            if (map == ImMap<V>.Empty)
                return new ImMapEntry<V>(key);

            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key ? new ImMapBranch<V>(leaf, new ImMapEntry<V>(key))
                    : key < leaf.Key ? new ImMapBranch<V>(new ImMapEntry<V>(key), leaf)
                    : map;

            if (map is ImMapBranch<V> branch)
            {
                if (key > branch.Entry.Key)
                    return key > branch.RightEntry.Key
                        ? new ImMapTree<V>(branch.RightEntry, branch.Entry, new ImMapEntry<V>(key))
                    : key < branch.RightEntry.Key // rotate if right
                        ? new ImMapTree<V>(new ImMapEntry<V>(key), branch.Entry, branch.RightEntry)
                    : map;

                return key < branch.Entry.Key
                    ? new ImMapTree<V>(branch.Entry, new ImMapEntry<V>(key), branch.RightEntry)
                    : map;
            }

            var tree = (ImMapTree<V>)map;
            return key != tree.Entry.Key  ? tree.AddOrKeepLeftOrRight(key) : map;
        }

        ///<summary>Returns the new map with the updated value for the key, or the same map if the key was not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> Update<V>(this ImMap<V> map, int key, V value) =>
            map.Contains(key) ? map.UpdateImpl(key, new ImMapEntry<V>(key, value)) : map;

        ///<summary>Returns the new map with the updated value for the key, ASSUMES that the key is not in the map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> UpdateEntryUnsafe<V>(this ImMap<V> map, ImMapEntry<V> entry) => 
            map.UpdateImpl(entry.Key, entry);

        internal static ImMap<V> UpdateImpl<V>(this ImMap<V> map, int key, ImMapEntry<V> entry)
        {
            if (map is ImMapTree<V> tree)
                return key > tree.Entry.Key ? new ImMapTree<V>(tree.Entry, tree.Left, tree.Right.UpdateImpl(key, entry), tree.TreeHeight)
                    :  key < tree.Entry.Key ? new ImMapTree<V>(tree.Entry, tree.Left.UpdateImpl(key, entry), tree.Right, tree.TreeHeight)
                    : new ImMapTree<V>(entry, tree.Left, tree.Right, tree.TreeHeight);

            // the key was found - so it should be either entry or right entry
            if (map is ImMapBranch<V> branch)
                return key == branch.Entry.Key
                    ? new ImMapBranch<V>(entry, branch.RightEntry)
                    : new ImMapBranch<V>(branch.Entry, entry);

            return entry;
        }

        ///<summary>Returns the new map with the value set to default, or the same map if the key was not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> UpdateToDefault<V>(this ImMap<V> map, int key) =>
            map.Contains(key) ? map.UpdateToDefaultImpl(key) : map;

        internal static ImMap<V> UpdateToDefaultImpl<V>(this ImMap<V> map, int key)
        {
            if (map is ImMapTree<V> tree)
                return key > tree.Entry.Key
                    ? new ImMapTree<V>(tree.Entry, tree.Left, tree.Right.UpdateToDefaultImpl(key), tree.TreeHeight)
                    : key < tree.Entry.Key
                        ? new ImMapTree<V>(tree.Entry, tree.Left.UpdateToDefaultImpl(key), tree.Right, tree.TreeHeight)
                        : new ImMapTree<V>(new ImMapEntry<V>(key), tree.Left, tree.Right, tree.TreeHeight);

            // the key was found - so it should be either entry or right entry
            if (map is ImMapBranch<V> branch)
                return key == branch.Entry.Key
                    ? new ImMapBranch<V>(new ImMapEntry<V>(key), branch.RightEntry)
                    : new ImMapBranch<V>(branch.Entry, new ImMapEntry<V>(key));

            return new ImMapEntry<V>(key);
        }

        /// <summary> Returns `true` if key is found or `false` otherwise. </summary>
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

            if (map is ImMapBranch<V> branch)
                return branch.Entry.Key == key || branch.RightEntry.Key == key;

            entry = map as ImMapEntry<V>;
            return entry != null && entry.Key == key;
        }

        /// <summary> Returns the entry if key is found or null otherwise. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapEntry<V> GetEntryOrDefault<V>(this ImMap<V> map, int key)
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

        /// <summary> Returns true if key is found and sets the value. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFindEntry<V>(this ImMap<V> map, int key, out ImMapEntry<V> result)
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

            if (map is ImMapBranch<V> branch)
            {
                if (branch.Entry.Key == key)
                {
                    result = branch.Entry;
                    return true;
                }

                if (branch.RightEntry.Key == key)
                {
                    result = branch.RightEntry;
                    return true;
                }

                result = null;
                return false;
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

        /// <summary>
        /// Enumerates all the map nodes from the left to the right and from the bottom to top
        /// You may pass `parentStacks` to reuse the array memory.
        /// NOTE: the length of `parentStack` should be at least of map (height - 2) - the stack want be used for 0, 1, 2 height maps,
        /// the content of the stack is not important and could be erased.
        /// </summary>
        public static IEnumerable<ImMapEntry<V>> Enumerate<V>(this ImMap<V> map, ImMapTree<V>[] parentStack = null)
        {
            if (map == ImMap<V>.Empty)
                yield break;

            if (map is ImMapEntry<V> leaf)
                yield return leaf;
            else if (map is ImMapBranch<V> branch)
            {
                yield return branch.Entry;
                yield return branch.RightEntry;
            }
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                {
                    yield return (ImMapEntry<V>)tree.Left;
                    yield return tree.Entry;
                    yield return (ImMapEntry<V>)tree.Right;
                }
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
                                yield return (ImMapEntry<V>)tree.Left;
                                yield return tree.Entry;
                                yield return (ImMapEntry<V>)tree.Right;
                                if (parentIndex == -1)
                                    break;
                                tree = parentStack[parentIndex--];
                                yield return tree.Entry;
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
                            yield return branch.Entry;
                            yield return branch.RightEntry;
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            yield return tree.Entry;
                            map = tree.Right;
                        }
                        else
                        {
                            yield return (ImMapEntry<V>)map;
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            yield return tree.Entry;
                            map = tree.Right;
                        }
                    }
                }
            }
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
            {
                state = reduce(branch.Entry, state);
                state = reduce(branch.RightEntry, state);
            }
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                {
                    state = reduce((ImMapEntry<V>)tree.Left, state);
                    state = reduce(tree.Entry, state);
                    state = reduce((ImMapEntry<V>)tree.Right, state);
                }
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
                                state = reduce((ImMapEntry<V>)tree.Left, state);
                                state = reduce(tree.Entry, state);
                                state = reduce((ImMapEntry<V>)tree.Right, state);
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
                            state = reduce(branch.Entry, state);
                            state = reduce(branch.RightEntry, state);
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

        /// <summary>
        /// Folds all the map nodes with the state from left to right and from the bottom to top
        /// You may pass `parentStacks` to reuse the array memory.
        /// NOTE: the length of `parentStack` should be at least of map (height - 2) - the stack want be used for 0, 1, 2 height maps,
        /// the content of the stack is not important and could be erased.
        /// </summary>
        public static S Fold<V, S, A>(this ImMap<V> map, S state, A a, Func<ImMapEntry<V>, S, A, S> reduce, ImMapTree<V>[] parentStack = null)
        {
            if (map == ImMap<V>.Empty)
                return state;

            if (map is ImMapEntry<V> leaf)
                state = reduce(leaf, state, a);
            else if (map is ImMapBranch<V> branch)
            {
                state = reduce(branch.Entry,      state, a);
                state = reduce(branch.RightEntry, state, a);
            }
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                {
                    state = reduce((ImMapEntry<V>)tree.Left,  state, a);
                    state = reduce(tree.Entry,                state, a);
                    state = reduce((ImMapEntry<V>)tree.Right, state, a);
                }
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
                                state = reduce((ImMapEntry<V>)tree.Left,  state, a);
                                state = reduce(tree.Entry,                state, a);
                                state = reduce((ImMapEntry<V>)tree.Right, state, a);
                                if (parentIndex == -1)
                                    break;
                                tree = parentStack[parentIndex--];
                                state = reduce(tree.Entry, state, a);
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
                            state = reduce(branch.Entry,      state, a);
                            state = reduce(branch.RightEntry, state, a);
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            state = reduce(tree.Entry, state, a);
                            map = tree.Right;
                        }
                        else
                        {
                            state = reduce((ImMapEntry<V>)map, state, a);
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            state = reduce(tree.Entry, state, a);
                            map = tree.Right;
                        }
                    }
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
            if (map == ImMap<V>.Empty)
                return;

            if (map is ImMapEntry<V> leaf)
                visit(leaf);
            else if (map is ImMapBranch<V> branch)
            {
                visit(branch.Entry);
                visit(branch.RightEntry);
            }
            else if (map is ImMapTree<V> tree)
            {
                if (tree.TreeHeight == 2)
                {
                    visit((ImMapEntry<V>)tree.Left);
                    visit(tree.Entry);
                    visit((ImMapEntry<V>)tree.Right);
                }
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
                                visit((ImMapEntry<V>)tree.Left);
                                visit(tree.Entry);
                                visit((ImMapEntry<V>)tree.Right);
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
                        else if ((branch = map as ImMapBranch<V>) != null)
                        {
                            visit(branch.Entry);
                            visit(branch.RightEntry);
                            if (parentIndex == -1)
                                break;
                            tree = parentStack[parentIndex--];
                            visit(tree.Entry);
                            map = tree.Right;
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
                }
            }
        }

        /// <summary>Wraps Key and Value payload to store inside ImMapEntry</summary>
        public struct KValue<K>
        {
            /// <summary>The key</summary>
            public K Key;
            /// <summary>The value</summary>
            public object Value;

            /// <summary>Constructs a pair</summary>
            public KValue(K key, object value)
            {
                Key = key;
                Value = value;
            }
        }

        /// <summary>Uses the user provided hash and adds or updates the tree with passed key-value. Returns a new tree.</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static ImMap<KValue<K>> AddOrUpdate<K>(this ImMap<KValue<K>> map, int hash, K key, object value, Update<K, object> update)
        {
            var oldEntry = map.GetEntryOrDefault(hash);
            return oldEntry == null 
                ? map.AddEntryUnsafe(CreateNewEntry(hash, key, value)) 
                : UpdateEntryOrAddOrUpdateConflict(map, hash, oldEntry, key, value, update);
        }

        private static ImMap<KValue<K>> UpdateEntryOrAddOrUpdateConflict<K>(ImMap<KValue<K>> map, int hash,
            ImMapEntry<KValue<K>> oldEntry, K key, object value, Update<K, object> update = null)
        {
            if (key.Equals(oldEntry.Value.Key))
            {
                value = update == null ? value : update(key, oldEntry.Value.Value, value);
                return map.UpdateEntryUnsafe(CreateNewEntry(hash, key, value));
            }

            // add a new conflicting key value
            ImMapEntry<KValue<K>>[] newConflicts;
            if (oldEntry.Value.Value is ImMapEntry<KValue<Type>>[] conflicts)
            {
                // entry is already containing the conflicted entries
                var conflictCount = conflicts.Length;
                var conflictIndex = conflictCount - 1;
                while (conflictIndex != -1 && !key.Equals(conflicts[conflictIndex].Value.Key))
                    --conflictIndex;

                if (conflictIndex != -1)
                {
                    // update the existing conflict
                    newConflicts = new ImMapEntry<KValue<K>>[conflictCount];
                    Array.Copy(conflicts, 0, newConflicts, 0, conflictCount);
                    value = update == null ? value : update(key, conflicts[conflictIndex].Value.Value, value);
                    newConflicts[conflictIndex] = CreateNewEntry(hash, key, value);
                }
                else
                {
                    // add the new conflicting value
                    newConflicts = new ImMapEntry<KValue<K>>[conflictCount + 1];
                    Array.Copy(conflicts, 0, newConflicts, 0, conflictCount);
                    newConflicts[conflictCount] = CreateNewEntry(hash, key, value);
                }
            }
            else
            {
                newConflicts = new[] { oldEntry, CreateNewEntry(hash, key, value) };
            }

            var conflictsEntry = new ImMapEntry<KValue<K>>(hash);
            conflictsEntry.Value.Value = newConflicts;
            return map.UpdateEntryUnsafe(conflictsEntry);
        }

        [MethodImpl((MethodImplOptions)256)]
        private static ImMapEntry<KValue<K>> CreateNewEntry<K>(int hash, K key, object value)
        {
            var newEntry = new ImMapEntry<KValue<K>>(hash);
            newEntry.Value.Key = key;
            newEntry.Value.Value = value;
            return newEntry;
        }

        [MethodImpl((MethodImplOptions)256)]
        private static ImMapEntry<KValue<K>> CreateNewEntry<K>(int hash, K key)
        {
            var newEntry = new ImMapEntry<KValue<K>>(hash);
            newEntry.Value.Key = key;
            return newEntry;
        }

        /// <summary>Uses the user provided hash and adds or updates the tree with passed key-value. Returns a new tree.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<KValue<K>> AddOrUpdate<K>(this ImMap<KValue<K>> map, int hash, K key, object value) => 
            map.AddOrUpdate(hash, CreateNewEntry(hash, key, value));

        /// <summary>Adds or updates the tree with passed key-value. Returns a new tree.</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static ImMap<KValue<K>> AddOrUpdate<K>(this ImMap<KValue<K>> map, K key, object value) =>
            map.AddOrUpdate(key.GetHashCode(), key, value);

        /// <summary>Uses the user provided hash and adds or updates the tree with passed key-value. Returns a new tree.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<KValue<K>> AddOrUpdate<K>(this ImMap<KValue<K>> map, int hash, ImMapEntry<KValue<K>> entry)
        {
            var oldEntry = map.GetEntryOrDefault(hash);
            return oldEntry == null
                ? map.AddEntryUnsafe(entry) 
                : UpdateEntryOrAddOrUpdateConflict(map, hash, oldEntry, entry);
        }

        private static ImMap<KValue<K>> UpdateEntryOrAddOrUpdateConflict<K>(ImMap<KValue<K>> map, int hash, 
            ImMapEntry<KValue<K>> oldEntry, ImMapEntry<KValue<K>> newEntry)
        {
            var key = newEntry.Value.Key;
            if (key.Equals(oldEntry.Value.Key))
                return map.UpdateEntryUnsafe(newEntry);

            // add a new conflicting key value
            ImMapEntry<KValue<K>>[] newConflicts;
            if (oldEntry.Value.Value is ImMapEntry<KValue<Type>>[] conflicts)
            {
                // entry is already containing the conflicted entries
                var conflictCount = conflicts.Length;
                var conflictIndex = conflictCount - 1;
                while (conflictIndex != -1 && !key.Equals(conflicts[conflictIndex].Value.Key))
                    --conflictIndex;

                if (conflictIndex != -1)
                {
                    // update the existing conflict
                    newConflicts = new ImMapEntry<KValue<K>>[conflictCount];
                    Array.Copy(conflicts, 0, newConflicts, 0, conflictCount);
                    newConflicts[conflictIndex] = newEntry;
                }
                else
                {
                    // add the new conflicting value
                    newConflicts = new ImMapEntry<KValue<K>>[conflictCount + 1];
                    Array.Copy(conflicts, 0, newConflicts, 0, conflictCount);
                    newConflicts[conflictCount] = newEntry;
                }
            }
            else
            {
                newConflicts = new[] { oldEntry, newEntry };
            }

            var conflictsEntry = new ImMapEntry<KValue<K>>(hash);
            conflictsEntry.Value.Value = newConflicts;
            return map.UpdateEntryUnsafe(conflictsEntry);
        }

        /// <summary>Updates the map with the new value if key is found, otherwise returns the same unchanged map.</summary>
        public static ImMap<KValue<K>> Update<K>(this ImMap<KValue<K>> map, int hash, K key, object value, Update<K, object> update = null)
        {
            var oldEntry = map.GetEntryOrDefault(hash);
            return oldEntry == null ? map : UpdateEntryOrReturnSelf(map, hash, oldEntry, key, value, update);
        }

        private static ImMap<KValue<K>> UpdateEntryOrReturnSelf<K>(ImMap<KValue<K>> map, 
            int hash, ImMapEntry<KValue<K>> oldEntry, K key, object value, Update<K, object> update = null)
        {
            if (key.Equals(oldEntry.Value.Key))
            {
                value = update == null ? value : update(key, oldEntry.Value.Value, value);
                return map.UpdateEntryUnsafe(CreateNewEntry(hash, key, value));
            }

            // add a new conflicting key value
            ImMapEntry<KValue<K>>[] newConflicts;
            if (oldEntry.Value.Value is ImMapEntry<KValue<Type>>[] conflicts)
            {
                // entry is already containing the conflicted entries
                var conflictCount = conflicts.Length;
                var conflictIndex = conflictCount - 1;
                while (conflictIndex != -1 && !key.Equals(conflicts[conflictIndex].Value.Key))
                    --conflictIndex;

                if (conflictIndex == -1)
                    return map;

                // update the existing conflict
                newConflicts = new ImMapEntry<KValue<K>>[conflictCount];
                Array.Copy(conflicts, 0, newConflicts, 0, conflictCount);
                value = update == null ? value : update(key, conflicts[conflictIndex].Value.Value, value);
                newConflicts[conflictIndex] = CreateNewEntry(hash, key, value);
            }
            else
            {
                return map;
            }

            var conflictsEntry = new ImMapEntry<KValue<K>>(hash);
            conflictsEntry.Value.Value = newConflicts;
            return map.UpdateEntryUnsafe(conflictsEntry);
        }

        /// <summary>Updates the map with the default value if the key is found, otherwise returns the same unchanged map.</summary>
        public static ImMap<KValue<K>> UpdateToDefault<K>(this ImMap<KValue<K>> map, int hash, K key)
        {
            var oldEntry = map.GetEntryOrDefault(hash);
            return oldEntry == null ? map : UpdateEntryOrReturnSelf(map, hash, oldEntry, key);
        }

        private static ImMap<KValue<K>> UpdateEntryOrReturnSelf<K>(ImMap<KValue<K>> map, 
            int hash, ImMapEntry<KValue<K>> oldEntry, K key)
        {
            if (key.Equals(oldEntry.Value.Key))
                return map.UpdateEntryUnsafe(CreateNewEntry(hash, key));

            // add a new conflicting key value
            ImMapEntry<KValue<K>>[] newConflicts;
            if (oldEntry.Value.Value is ImMapEntry<KValue<Type>>[] conflicts)
            {
                // entry is already containing the conflicted entries
                var conflictCount = conflicts.Length;
                var conflictIndex = conflictCount - 1;
                while (conflictIndex != -1 && !key.Equals(conflicts[conflictIndex].Value.Key))
                    --conflictIndex;

                if (conflictIndex == -1)
                    return map;

                // update the existing conflict
                newConflicts = new ImMapEntry<KValue<K>>[conflictCount];
                Array.Copy(conflicts, 0, newConflicts, 0, conflictCount);
                newConflicts[conflictIndex] = CreateNewEntry(hash, key);
            }
            else
            {
                return map;
            }

            var conflictsEntry = new ImMapEntry<KValue<K>>(hash);
            conflictsEntry.Value.Value = newConflicts;
            return map.UpdateEntryUnsafe(conflictsEntry);
        }

        /// <summary> Returns the entry if key is found or `null` otherwise. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapEntry<KValue<K>> GetEntryOrDefault<K>(this ImMap<KValue<K>> map, int hash, K key)
        {
            var entry = map.GetEntryOrDefault(hash);
            return entry != null
                ? key.Equals(entry.Value.Key) ? entry : GetConflictedEntryOrDefault(entry, key)
                : null;
        }

        /// <summary> Returns the value if key is found or default value otherwise. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static object GetValueOrDefault<K>(this ImMap<KValue<K>> map, int hash, K key) =>
            map.GetEntryOrDefault(hash, key)?.Value.Value;

        /// <summary> Sets the value if key is found or returns false otherwise. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K>(this ImMap<KValue<K>> map, int hash, K key, out object value)
        {
            var entry = map.GetEntryOrDefault(hash, key);
            if (entry != null)
            {
                value = entry.Value.Value;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary> Returns the entry if key is found or default value otherwise. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapEntry<KValue<Type>> GetEntryOrDefault(this ImMap<KValue<Type>> map, int hash, Type type)
        {
            var entry = map.GetEntryOrDefault(hash);
            return entry != null
                ? entry.Value.Key == type ? entry : GetConflictedEntryOrDefault(entry, type)
                : null;
        }

        /// <summary> Returns the value if key is found or default value otherwise. </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static object GetValueOrDefault(this ImMap<KValue<Type>> map, int hash, Type typeKey) =>
            map.GetEntryOrDefault(hash, typeKey)?.Value.Value;

        internal static ImMapEntry<KValue<K>> GetConflictedEntryOrDefault<K>(ImMapEntry<KValue<K>> entry, K key)
        {
            if (entry.Value.Value is ImMapEntry<KValue<K>>[] conflicts)
                for (var i = 0; i < conflicts.Length; ++i)
                    if (key.Equals(conflicts[i].Value.Key))
                        return conflicts[i];
            return null;
        }

        /// <summary>
        /// Depth-first in-order traversal as described in http://en.wikipedia.org/wiki/Tree_traversal
        /// The only difference is using fixed size array instead of stack for speed-up.
        /// </summary>
        public static IEnumerable<ImMapEntry<KValue<K>>> Enumerate<K>(this ImMap<KValue<K>> map)
        {
            foreach (var entry in map.Enumerate(null))
            {
                if (entry.Value.Value is ImMapEntry<KValue<K>>[] conflicts)
                    for (var i = 0; i < conflicts.Length; i++)
                        yield return conflicts[i];
                else
                    yield return entry;
            }
        }

        /// <summary>
        /// Depth-first in-order traversal as described in http://en.wikipedia.org/wiki/Tree_traversal
        /// The only difference is using fixed size array instead of stack for speed-up.
        /// Note: By passing <paramref name="parentsStack"/> you may reuse the stack array between different method calls,
        /// but it should be at least <see cref="ImHashMap{K,V}.Height"/> length. The contents of array are not important.
        /// </summary>
        public static S Fold<K, S>(this ImMap<KValue<K>> map,
            S state, Func<ImMapEntry<KValue<K>>, S, S> reduce, ImMapTree<KValue<K>>[] parentsStack = null) =>
                map.Fold(state, reduce, (entry, s, r) =>
                {
                    if (entry.Value.Value is ImMapEntry<KValue<K>>[] conflicts)
                        for (var i = 0; i < conflicts.Length; i++)
                            s = r(conflicts[i], s);
                    else
                        s = r(entry, s);
                    return s;
                }, 
                parentsStack);

        /// <summary>
        /// Depth-first in-order traversal as described in http://en.wikipedia.org/wiki/Tree_traversal
        /// The only difference is using fixed size array instead of stack for speed-up.
        /// Note: By passing <paramref name="parentsStack"/> you may reuse the stack array between different method calls,
        /// but it should be at least <see cref="ImHashMap{K,V}.Height"/> length. The contents of array are not important.
        /// </summary>
        public static S Visit<K, S>(this ImMap<KValue<K>> map, 
            S state, Action<ImMapEntry<KValue<K>>, S> effect, ImMapTree<KValue<K>>[] parentsStack = null) =>
            map.Fold(state, effect, (entry, s, eff) =>
            {
                if (entry.Value.Value is ImMapEntry<KValue<K>>[] conflicts)
                    for (var i = 0; i<conflicts.Length; i++)
                        eff(conflicts[i], s);
                else
                    eff(entry, s);
                return s;
            }, 
            parentsStack);

        /// <summary>
        /// Depth-first in-order traversal as described in http://en.wikipedia.org/wiki/Tree_traversal
        /// The only difference is using fixed size array instead of stack for speed-up.
        /// Note: By passing <paramref name="parentsStack"/> you may reuse the stack array between different method calls,
        /// but it should be at least <see cref="ImHashMap{K,V}.Height"/> length. The contents of array are not important.
        /// </summary>
        public static void Visit<K>(this ImMap<KValue<K>> map, 
            Action<ImMapEntry<KValue<K>>> effect, ImMapTree<KValue<K>>[] parentsStack = null) =>
            map.Fold(false, effect, (entry, s, eff) =>
            {
                if (entry.Value.Value is ImMapEntry<KValue<K>>[] conflicts)
                    for (var i = 0; i<conflicts.Length; i++)
                        eff(conflicts[i]);
                else
                    eff(entry);
                return false;
            }, 
            parentsStack);
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

        /// Adds a default value entry for the specified key or keeps the existing map if the key is already in the map.
        [MethodImpl((MethodImplOptions)256)]
        public static void AddOrKeep<V>(this ImMap<V>[] slots, int key, int keyMaskToFindSlot = KEY_MASK_TO_FIND_SLOT)
        {
            ref var slot = ref slots[key & keyMaskToFindSlot];
            var copy = slot;
            if (Interlocked.CompareExchange(ref slot, copy.AddOrKeep(key), copy) != copy)
                RefAddOrKeepSlot(ref slot, key);
        }

        /// Update the ref to the slot with the new version - retry if the someone changed the slot in between
        public static void RefAddOrKeepSlot<V>(ref ImMap<V> slot, int key) =>
            Ref.Swap(ref slot, key, (s, k) => s.AddOrKeep(k));

        /// <summary> Folds all map nodes without the order </summary>
        public static S Fold<V, S>(this ImMap<V>[] slots, S state, Func<ImMapEntry<V>, S, S> reduce)
        {
            var parentStack = ArrayTools.Empty<ImMapTree<V>>();
            for (var i = 0; i < slots.Length; ++i)
            {
                var map = slots[i];
                if (map == ImMap<V>.Empty)
                    continue;

                if (map is ImMapEntry<V> leaf)
                    state = reduce(leaf, state);
                else if (map is ImMapBranch<V> branch)
                {
                    state = reduce(branch.Entry,      state);
                    state = reduce(branch.RightEntry, state);
                }
                else if (map is ImMapTree<V> tree)
                {
                    if (tree.TreeHeight == 2)
                    {
                        state = reduce((ImMapEntry<V>) tree.Left, state);
                        state = reduce(tree.Entry, state);
                        state = reduce((ImMapEntry<V>) tree.Right, state);
                    }
                    else
                    {
                        if (parentStack.Length < tree.TreeHeight - 2)
                            parentStack = new ImMapTree<V>[tree.TreeHeight - 2];
                        var parentIndex = -1;
                        while (true)
                        {
                            if ((tree = map as ImMapTree<V>) != null)
                            {
                                if (tree.TreeHeight == 2)
                                {
                                    state = reduce((ImMapEntry<V>)tree.Left, state);
                                    state = reduce(tree.Entry, state);
                                    state = reduce((ImMapEntry<V>)tree.Right, state);
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
                                state = reduce(branch.Entry, state);
                                state = reduce(branch.RightEntry, state);
                                if (parentIndex == -1)
                                    break;
                                tree = parentStack[parentIndex--];
                                state = reduce(tree.Entry, state);
                                map = tree.Right;
                            }
                            else
                            {
                                state = reduce((ImMapEntry<V>) map, state);
                                if (parentIndex == -1)
                                    break;
                                tree = parentStack[parentIndex--];
                                state = reduce(tree.Entry, state);
                                map = tree.Right;
                            }
                        }
                    }
                }
            }

            return state;
        }
    }
}
