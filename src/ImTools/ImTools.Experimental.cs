using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Experimental
{
    /// <summary>
    /// Immutable http://en.wikipedia.org/wiki/AVL_tree with integer keys and <typeparamref name="V"/> values.
    /// The base class for tree leafs and branches, defines the Empty tree.
    /// </summary>
    public class ImMap<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap<V> Empty = new ImMap<V>();

        /// <summary>Returns true if tree is empty.</summary>
        public bool IsEmpty => this == Empty;
        
        /// <summary>Hide the constructor to prevent the multiple Empty trees creation</summary>
        protected ImMap() { }

        /// <summary>Height of the longest sub-tree/branch - 0 for the empty tree</summary>
        public virtual int Height => 0;

        /// <summary>Prints "empty"</summary>
        public override string ToString() => "empty";
    }

    /// <summary>Wraps the stored data with "fixed" reference semantics - 
    /// when added to the tree it won't be changed or reconstructed in memory</summary>
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

        /// Right sub-tree/branch, or empty.
        public ImMapEntry<V> RightEntry;

        /// Constructor
        public ImMapBranch(ImMapEntry<V> entry, ImMapEntry<V> rightEntry)
        {
            Entry = entry;
            RightEntry = rightEntry;
        }

        /// Prints the key value pair
        public override string ToString() => "h2:" + Entry + "->" + RightEntry;
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

        /// Right sub-tree/branch, or empty
        public ImMap<V> Right;

        internal ImMapTree(ImMapEntry<V> entry, ImMap<V> left, ImMap<V> right, int height)
        {
            Entry = entry;
            Left = left;
            Right = right;
            TreeHeight = height;
        }

        internal ImMapTree(ImMapEntry<V> entry, ImMapEntry<V> leftEntry, ImMapEntry<V> rightEntry)
        {
            Entry = entry;
            Left = leftEntry;
            Right = rightEntry;
            TreeHeight = 2;
        }

        /// <summary>Outputs the brief tree info - mostly for debugging purposes</summary>
        public override string ToString() =>
            "h" + Height + ":" + Entry
                + "->(" + (Left is ImMapTree<V> leftTree ? "h" + leftTree.TreeHeight + ":" + leftTree.Entry : "" + Left)
                + ", " + (Right is ImMapTree<V> rightTree ? "h" + rightTree.TreeHeight + ":" + rightTree.Entry : "" + Right)
                + ")";

        /// <summary>Adds or updates the left or right branch</summary>
        public ImMapTree<V> AddOrUpdateLeftOrRightEntry(int key, ImMapEntry<V> entry)
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

                    var newLeftTree = leftTree.AddOrUpdateLeftOrRightEntry(key, entry);
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
                    : key < leftLeaf.Key ? new ImMapTree<V>(Entry, new ImMapBranch<V>(entry, leftLeaf), Right, 3)
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

                    var newRightTree = rightTree.AddOrUpdateLeftOrRightEntry(key, entry);
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
        public ImMapTree<V> AddUnsafeLeftOrRightEntry(int key, ImMapEntry<V> entry)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapTree<V> leftTree)
                {
                    var newLeftTree = leftTree.AddUnsafeLeftOrRightEntry(key, entry);
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
                    var newRightTree = rightTree.AddUnsafeLeftOrRightEntry(key, entry);
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
                    if (key > leftBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(leftBranch.RightEntry, leftBranch.Entry, new ImMapEntry<V>(key, value)),
                            Right, TreeHeight);
                    if (key > leftBranch.Entry.Key && key < leftBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(new ImMapEntry<V>(key, value), leftBranch.Entry, leftBranch.RightEntry),
                            Right, TreeHeight);
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

                    var newRightTree = rightTree.AddOrKeepLeftOrRight(key, value);
                    return newRightTree == rightTree ? this
                        : newRightTree.TreeHeight == rightTree.TreeHeight
                            ? new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight)
                            : BalanceNewRightTree(newRightTree);
                }

                if (right is ImMapBranch<V> rightBranch)
                {
                    if (key > rightBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.RightEntry, rightBranch.Entry, new ImMapEntry<V>(key, value)),
                            TreeHeight);
                    if (key < rightBranch.Entry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.Entry, new ImMapEntry<V>(key, value), rightBranch.RightEntry),
                            TreeHeight);
                    if (key > rightBranch.Entry.Key && key < rightBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(new ImMapEntry<V>(key, value), rightBranch.Entry, rightBranch.RightEntry),
                            TreeHeight);
                    return this;
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
                    if (key > leftBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(leftBranch.RightEntry, leftBranch.Entry, new ImMapEntry<V>(key)),
                            Right, TreeHeight);
                    if (key > leftBranch.Entry.Key && key < leftBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(new ImMapEntry<V>(key), leftBranch.Entry, leftBranch.RightEntry),
                            Right, TreeHeight);
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
                    if (key > rightBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.RightEntry, rightBranch.Entry, new ImMapEntry<V>(key)),
                            TreeHeight);
                    if (key < rightBranch.Entry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.Entry, new ImMapEntry<V>(key), rightBranch.RightEntry),
                            TreeHeight);
                    if (key > rightBranch.Entry.Key && key < rightBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(new ImMapEntry<V>(key), rightBranch.Entry, rightBranch.RightEntry),
                            TreeHeight);
                    return this;
                }

                var rightLeaf = (ImMapEntry<V>)right;
                return key > rightLeaf.Key
                        ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(rightLeaf, new ImMapEntry<V>(key)), 3)
                    : key < rightLeaf.Key
                        ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(new ImMapEntry<V>(key), rightLeaf), 3)
                    : this;
            }
        }

        /// <summary>Adds to the left or right branch, or keeps the un-modified map</summary>
        public ImMapTree<V> AddOrKeepLeftOrRightEntry(int key, ImMapEntry<V> entry)
        {
            if (key < Entry.Key)
            {
                var left = Left;
                if (left is ImMapTree<V> leftTree)
                {
                    if (key == leftTree.Entry.Key)
                        return this;

                    var newLeftTree = leftTree.AddOrKeepLeftOrRightEntry(key, entry);
                    return newLeftTree == leftTree ? this
                        : newLeftTree.TreeHeight == leftTree.TreeHeight
                            ? new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight)
                            : BalanceNewLeftTree(newLeftTree);
                }

                if (left is ImMapBranch<V> leftBranch)
                {
                    if (key < leftBranch.Entry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(leftBranch.Entry, entry, leftBranch.RightEntry),
                            Right, TreeHeight);
                    if (key > leftBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(leftBranch.RightEntry, leftBranch.Entry, entry),
                            Right, TreeHeight);
                    if (key > leftBranch.Entry.Key && key < leftBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry,
                            new ImMapTree<V>(entry, leftBranch.Entry, leftBranch.RightEntry),
                            Right, TreeHeight);
                    return this;
                }

                var leftLeaf = (ImMapEntry<V>)left;
                return key > leftLeaf.Key
                        ? new ImMapTree<V>(Entry, new ImMapBranch<V>(leftLeaf, entry), Right, 3)
                    : key < leftLeaf.Key
                        ? new ImMapTree<V>(Entry, new ImMapBranch<V>(entry, leftLeaf), Right, 3)
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
                    var newRightTree = rightTree.AddOrKeepLeftOrRightEntry(key, entry);
                    return newRightTree == rightTree ? this
                        : newRightTree.TreeHeight == rightTree.TreeHeight
                            ? new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight)
                            : BalanceNewRightTree(newRightTree);
                }

                if (right is ImMapBranch<V> rightBranch)
                {
                    if (key > rightBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.RightEntry, rightBranch.Entry, entry),
                            TreeHeight);
                    if (key < rightBranch.Entry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(rightBranch.Entry, entry, rightBranch.RightEntry),
                            TreeHeight);
                    if (key > rightBranch.Entry.Key && key < rightBranch.RightEntry.Key)
                        return new ImMapTree<V>(Entry, Left,
                            new ImMapTree<V>(entry, rightBranch.Entry, rightBranch.RightEntry),
                            TreeHeight);
                    return this;
                }

                var rightLeaf = (ImMapEntry<V>)right;
                return key > rightLeaf.Key
                    ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(rightLeaf, entry), 3)
                    : key < rightLeaf.Key
                        ? new ImMapTree<V>(Entry, Left, new ImMapBranch<V>(entry, rightLeaf), 3)
                    : this;
            }
        }

        private ImMapTree<V> BalanceNewLeftTree(ImMapTree<V> newLeftTree)
        {
            var rightHeight = Right.Height;
            var delta = newLeftTree.TreeHeight - rightHeight;
            if (delta <= 0)
                return new ImMapTree<V>(Entry, newLeftTree, Right, TreeHeight);

            if (delta == 1)
                return new ImMapTree<V>(Entry, newLeftTree, Right, newLeftTree.TreeHeight + 1);

            // here is the balancing art comes into place
            if (rightHeight == 1)
            {
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
                            new ImMapTree<V>(Entry, leftRightTree.Right, Right, 2),
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
                        new ImMapTree<V>(Entry, leftRightBranch.RightEntry, Right, 2),
                        3);
                }

                newLeftTree.Right = new ImMapTree<V>(Entry, newLeftTree.Right, Right, 2);
                newLeftTree.TreeHeight = 3;
                return newLeftTree;
            }

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

                // Saving the old code to explaining what's happening in the new one
                //return new ImMapTree<V>(leftRightTree.Entry,
                //    new ImMapTree<V>(newLeftTree.Entry, leftLeftHeight, newLeftTree.Left, leftRightTree.Left),
                //    new ImMapTree<V>(Entry, leftRightTree.Right, rightHeight, Right));
            }

            newLeftTree.Right = new ImMapTree<V>(Entry, newLeftTree.Right, Right, leftRightHeight + 1);
            newLeftTree.TreeHeight = leftRightHeight + 2;
            return newLeftTree;
        }

        private ImMapTree<V> BalanceNewRightTree(ImMapTree<V> newRightTree)
        {
            var leftHeight = Left.Height;
            var delta = newRightTree.Height - leftHeight;
            if (delta <= 0)
                return new ImMapTree<V>(Entry, Left, newRightTree, TreeHeight);
            if (delta == 1)
                return new ImMapTree<V>(Entry, Left, newRightTree, newRightTree.TreeHeight + 1);

            if (leftHeight == 1)
            {
                // here we need to re-balance by default, because the new right tree is at least 3 level (actually exactly 3 or it would be too unbalanced)
                // double rotation needed if only the right-right is a leaf
                if (newRightTree.Right is ImMapEntry<V> == false)
                {
                    newRightTree.Left = new ImMapTree<V>(Entry, Left, newRightTree.Left, 2);
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
                        new ImMapTree<V>(Entry, Left, rightLeftTree.Left, 2),
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
                    new ImMapBranch<V>((ImMapEntry<V>)Left, Entry),
                    newRightTree, 3);
            }

            var rightRightHeight = (newRightTree.Right as ImMapTree<V>)?.TreeHeight ?? 2;
            var rightLeftHeight =  (newRightTree.Left  as ImMapTree<V>)?.TreeHeight ?? 2;
            if (rightRightHeight < rightLeftHeight)
            {
                var rightLeftTree = (ImMapTree<V>)newRightTree.Left;
                newRightTree.Left = rightLeftTree.Right;
                // the height now should be defined by rr - because left now is shorter by 1
                newRightTree.TreeHeight = rightRightHeight + 1;
                // the whole height consequentially can be defined by `newRightTree` (rr+1) because left is consist of short Left and -2 rl.Left
                return new ImMapTree<V>(rightLeftTree.Entry,
                    // Left should be >= rightLeft.Left because it maybe rightLeft.Right which defines rl height
                    new ImMapTree<V>(Entry, Left, rightLeftTree.Left, leftHeight + 1),
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
                    : key < leaf.Key ? new ImMapBranch<V>(entry, leaf)
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
                : tree.AddOrUpdateLeftOrRightEntry(key, entry);
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
                return key < branch.Entry.Key
                        ? new ImMapTree<V>(branch.Entry, entry, branch.RightEntry)
                    : key > branch.RightEntry.Key
                        ? new ImMapTree<V>(branch.RightEntry, branch.Entry, entry)
                        : new ImMapTree<V>(entry, branch.Entry, branch.RightEntry);

            return ((ImMapTree<V>)map).AddUnsafeLeftOrRightEntry(key, entry);
        }

        /// <summary> Adds the value for the key or returns the un-modified map if key is already present </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrKeep<V>(this ImMap<V> map, int key, V value)
        {
            if (map == ImMap<V>.Empty)
                return new ImMapEntry<V>(key, value);

            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key ? new ImMapBranch<V>(leaf, new ImMapEntry<V>(key, value))
                    : key < leaf.Key ? new ImMapBranch<V>(new ImMapEntry<V>(key, value), leaf)
                    : map;

            if (map is ImMapBranch<V> branch)
                return key < branch.Entry.Key
                        ? new ImMapTree<V>(branch.Entry, new ImMapEntry<V>(key, value), branch.RightEntry)
                     : key > branch.RightEntry.Key
                        ? new ImMapTree<V>(branch.RightEntry, branch.Entry, new ImMapEntry<V>(key, value))
                     : key > branch.Entry.Key && key < branch.RightEntry.Key
                        ? new ImMapTree<V>(new ImMapEntry<V>(key, value), branch.Entry, branch.RightEntry)
                     : map;

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
                return key < branch.Entry.Key
                        ? new ImMapTree<V>(branch.Entry, new ImMapEntry<V>(key), branch.RightEntry)
                     : key > branch.RightEntry.Key
                        ? new ImMapTree<V>(branch.RightEntry, branch.Entry, new ImMapEntry<V>(key))
                     : key > branch.Entry.Key && key < branch.RightEntry.Key
                        ? new ImMapTree<V>(new ImMapEntry<V>(key), branch.Entry, branch.RightEntry)
                     : map;

            var tree = (ImMapTree<V>)map;
            return key != tree.Entry.Key ? tree.AddOrKeepLeftOrRight(key) : map;
        }

        /// <summary> Adds the entry for the key or returns the un-modified map if key is already present </summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrKeepEntry<V>(this ImMap<V> map, ImMapEntry<V> entry)
        {
            if (map == ImMap<V>.Empty)
                return entry;

            var key = entry.Key;
            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key ? new ImMapBranch<V>(leaf, entry)
                    : key < leaf.Key ? new ImMapBranch<V>(entry, leaf)
                    : map;

            if (map is ImMapBranch<V> branch)
                return key < branch.Entry.Key
                        ? new ImMapTree<V>(branch.Entry, entry, branch.RightEntry)
                     : key > branch.RightEntry.Key
                         ? new ImMapTree<V>(branch.RightEntry, branch.Entry, entry)
                     : key > branch.Entry.Key && key < branch.RightEntry.Key
                        ? new ImMapTree<V>(entry, branch.Entry, branch.RightEntry)
                     : map;

            var tree = (ImMapTree<V>)map;
            return key != tree.Entry.Key ? tree.AddOrKeepLeftOrRightEntry(key, entry) : map;
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
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<KValue<K>> AddOrUpdate<K>(this ImMap<KValue<K>> map, int hash, K key, object value, Update<K, object> update)
        {
            var oldEntry = map.GetEntryOrDefault(hash);
            return oldEntry == null
                ? map.AddEntryUnsafe(CreateKValueEntry(hash, key, value))
                : UpdateEntryOrAddOrUpdateConflict(map, hash, oldEntry, key, value, update);
        }

        private static ImMap<KValue<K>> UpdateEntryOrAddOrUpdateConflict<K>(ImMap<KValue<K>> map, int hash,
            ImMapEntry<KValue<K>> oldEntry, K key, object value, Update<K, object> update = null)
        {
            if (key.Equals(oldEntry.Value.Key))
            {
                value = update == null ? value : update(key, oldEntry.Value.Value, value);
                return map.UpdateEntryUnsafe(CreateKValueEntry(hash, key, value));
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
                    newConflicts[conflictIndex] = CreateKValueEntry(hash, key, value);
                }
                else
                {
                    // add the new conflicting value
                    newConflicts = new ImMapEntry<KValue<K>>[conflictCount + 1];
                    Array.Copy(conflicts, 0, newConflicts, 0, conflictCount);
                    newConflicts[conflictCount] = CreateKValueEntry(hash, key, value);
                }
            }
            else
            {
                newConflicts = new[] { oldEntry, CreateKValueEntry(hash, key, value) };
            }

            var conflictsEntry = new ImMapEntry<KValue<K>>(hash);
            conflictsEntry.Value.Value = newConflicts;
            return map.UpdateEntryUnsafe(conflictsEntry);
        }

        /// <summary>Creates the new entry</summary>
        [MethodImpl((MethodImplOptions)256)]
        private static ImMapEntry<KValue<K>> CreateKValueEntry<K>(int hash, K key, object value) => 
            new ImMapEntry<KValue<K>>(hash) { Value = { Key = key, Value = value }};

        /// <summary>Creates the new entry with the conflicts - the Key for the new entry is not set, but the value contains the actual conflict entries array</summary>
        [MethodImpl((MethodImplOptions)256)]
        private static ImMapEntry<KValue<K>> CreateConflictsKValueEntry<K>(int hash, ImMapEntry<KValue<K>>[] conflicts) => 
            new ImMapEntry<KValue<K>>(hash) { Value = { Value = conflicts }};

        /// <summary>Creates the new entry</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMapEntry<KValue<K>> CreateKValueEntry<K>(int hash, K key) => 
            new ImMapEntry<KValue<K>>(hash) { Value = { Key = key }};

        /// <summary>Uses the user provided hash and adds or updates the tree with passed key-value. Returns a new tree.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<KValue<K>> AddOrUpdate<K>(this ImMap<KValue<K>> map, int hash, K key, object value) =>
            map.AddOrUpdate(hash, CreateKValueEntry(hash, key, value));

        /// <summary>Adds or updates the tree with passed Type key and the value. Returns a new tree.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<KValue<Type>> AddOrUpdate(this ImMap<KValue<Type>> map, Type key, object value) =>
            map.AddOrUpdate(RuntimeHelpers.GetHashCode(key), key, value);

        /// <summary>Adds or updates the tree with passed key-value. Returns a new tree.</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static ImMap<KValue<K>> AddOrUpdate<K>(this ImMap<KValue<K>> map, K key, object value) =>
            map.AddOrUpdate(key.GetHashCode(), key, value);

        /// <summary>Uses the provided hash and adds or updates the tree with the passed key-value. Returns a new tree.</summary>
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
            if (newEntry.Value.Key.Equals(oldEntry.Value.Key))
                return map.UpdateEntryUnsafe(newEntry);

            // add a new conflicting key value
            ImMapEntry<KValue<K>>[] newConflicts;
            if (oldEntry.Value.Value is ImMapEntry<KValue<Type>>[] conflicts)
            {
                // entry is already containing the conflicted entries
                var key = newEntry.Value.Key;
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

            return map.UpdateEntryUnsafe(CreateConflictsKValueEntry(hash, newConflicts));
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
                return map.UpdateEntryUnsafe(CreateKValueEntry(hash, key, value));
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
                newConflicts[conflictIndex] = CreateKValueEntry(hash, key, value);
            }
            else
            {
                return map;
            }

            return map.UpdateEntryUnsafe(CreateConflictsKValueEntry(hash, newConflicts));
        }

        /// <summary>Adds the new entry or keeps the current map if entry key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<KValue<K>> AddOrKeep<K>(this ImMap<KValue<K>> map, int hash, K key)
        {
            var oldEntry = map.GetEntryOrDefault(hash);
            return oldEntry == null
                ? map.AddEntryUnsafe(CreateKValueEntry(hash, key))
                : AddOrKeepConflict(map, hash, oldEntry, key);
        }

        private static ImMap<KValue<K>> AddOrKeepConflict<K>(ImMap<KValue<K>> map, int hash,
            ImMapEntry<KValue<K>> oldEntry, K key, object value = null)
        {
            if (key.Equals(oldEntry.Value.Key))
                return map;

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
                    return map;

                // add the new conflicting value
                newConflicts = new ImMapEntry<KValue<K>>[conflictCount + 1];
                Array.Copy(conflicts, 0, newConflicts, 0, conflictCount);
                newConflicts[conflictCount] = CreateKValueEntry(hash, key, value);
            }
            else
            {
                newConflicts = new[] { oldEntry, CreateKValueEntry(hash, key, value) };
            }

            return map.UpdateEntryUnsafe(CreateConflictsKValueEntry(hash, newConflicts));
        }

        /// <summary>Adds the new entry or keeps the current map if entry key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<KValue<K>> AddOrKeep<K>(this ImMap<KValue<K>> map, int hash, K key, object value)
        {
            var oldEntry = map.GetEntryOrDefault(hash);
            return oldEntry == null
                ? map.AddEntryUnsafe(CreateKValueEntry(hash, key, value))
                : AddOrKeepConflict(map, hash, oldEntry, key, value);
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
                return map.UpdateEntryUnsafe(CreateKValueEntry(hash, key));

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
                newConflicts[conflictIndex] = CreateKValueEntry(hash, key);
            }
            else
            {
                return map;
            }

            var conflictsEntry = new ImMapEntry<KValue<K>>(hash);
            conflictsEntry.Value.Value = newConflicts;
            return map.UpdateEntryUnsafe(conflictsEntry);
        }

        /// <summary> Returns the entry if key is found or default value otherwise. </summary>
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

        /// <summary> Returns the entry if key is found or `null` otherwise. </summary>
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

    /// <summary>The base class for the tree leafs and branches, also defines the Empty tree</summary>
    public class ImMap234<K, V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap234<K, V> Empty = new ImMap234<K, V>();

        /// <summary>Hide the constructor to prevent the multiple Empty trees creation</summary>
        protected ImMap234() { }

        /// Pretty-prints
        public override string ToString() => "empty";

        /// <summary>The base entry for the Value and for the ConflictingValues entries, contains the Hash and Key</summary>
        public abstract class Entry : ImMap234<K, V>
        {
            /// <summary>The Hash</summary>
            public readonly int Hash;

            /// <summary>The Key</summary>
            public readonly K Key;

            /// <summary>Constructs the entry</summary>
            public Entry(int hash, K key) { Hash = hash; Key = key; }
        }

        /// <summary>Entry containing the Key and the Value</summary>
        public sealed class ValueEntry : Entry
        {
            /// <summary>The value. May be modified if you need the Ref{V} semantics</summary>
            public V Value;

            /// <summary>Constructs the entry with the default value</summary>
            public ValueEntry(int hash, K key) : base(hash, key) {}

            /// <summary>Constructs the entry with the key and value</summary>
            public ValueEntry(int hash, K key, V value) : base(hash, key) => Value = value;
        }
    }


    /// <summary>The base class for the tree leafs and branches, also defines the Empty tree</summary>
    public class ImMap234<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap234<V> Empty = new ImMap234<V>();

        /// <summary>Hide the constructor to prevent the multiple Empty trees creation</summary>
        protected ImMap234() { }

        /// Pretty-prints
        public override string ToString() => "empty";

        /// <summary>Produces the new or updated map</summary>
        public virtual ImMap234<V> AddOrUpdateEntry(int key, Entry entry) => entry;

        /// <summary>As the empty cannot be a leaf - so no chance to call it</summary>
        protected virtual ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight) =>
            throw new NotSupportedException();

        /// <summary> Adds the value for the key or returns the non-modified map if the key is already present </summary>
        public virtual ImMap234<V> AddOrKeepEntry(int key, Entry entry) => entry;

        /// <summary>As the empty cannot be a leaf - so no chance to call it</summary>
        protected virtual ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight) =>
            throw new NotSupportedException();

        /// <summary>Lookup for the entry, if not found returns `null`. You can define other Lookup methods on top of it.</summary>
        public virtual Entry GetEntryOrDefault(int key) => null;

        /// <summary>Fold to fold</summary>
        public virtual S Fold<S>(S state, Func<Entry, S, S> reduce) => state;

        /// <summary>Enumerable</summary>
        public virtual IEnumerable<Entry> Enumerate() => Enumerable.Empty<Entry>();

        // todo: @feature add SoftRemove

        /// <summary>Wraps the stored data with "fixed" reference semantics - 
        /// when added to the tree it won't be changed or reconstructed in memory</summary>
        public sealed class Entry : ImMap234<V>
        {
            /// <summary>The Key is basically the hash</summary>
            public readonly int Key;

            /// <summary>The value - may be modified if you need a Ref{V} semantics</summary>
            public V Value;

            /// <summary>Constructs the entry with the default value</summary>
            public Entry(int key) => Key = key;

            /// <summary>Constructs the entry with the key and value</summary>
            public Entry(int key, V value)
            {
                Key = key;
                Value = value;
            }

            /// Pretty-prints
            public override string ToString() => Key + ":" + Value;

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry) =>
                key > Key ? new Leaf2(this, entry) :
                key < Key ? new Leaf2(entry, this) :
                (ImMap234<V>)entry;

            /// <summary>As the single entry cannot be a leaf - so no way to call it</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight) =>
                throw new NotSupportedException();

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry) =>
                key > Key ? new Leaf2(this, entry) :
                key < Key ? new Leaf2(entry, this) :
                (ImMap234<V>)this;

            /// <summary>As the empty cannot be a leaf - so no chance to call it</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight) =>
                throw new NotSupportedException();

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Key ? this : null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) => reduce(this, state);

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return this;
            }
        }

        /// <summary>2 leafs</summary>
        public sealed class Leaf2 : ImMap234<V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;

            /// <summary>Right entry</summary>
            public readonly Entry Entry1;

            /// <summary>Constructs 2 leafs</summary>
            public Leaf2(Entry entry0, Entry entry1)
            {
                Entry0 = entry0;
                Entry1 = entry1;
            }

            /// Pretty-print
            public override string ToString() => Entry0 + "|" + Entry1;

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                var e1 = Entry1;
                var e0 = Entry0;
                return key > e1.Key ? new Leaf3(e0, e1, entry) :
                    key < e0.Key ? new Leaf3(entry, e0, e1) :
                    key > e0.Key && key < e1.Key ? new Leaf3(e0, entry, e1) :
                    key == e0.Key ? new Leaf2(entry, e1) :
                    (ImMap234<V>) new Leaf2(e0, entry);
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry1.Key ? new Leaf3(Entry0, Entry1, entry) :
                    key < Entry0.Key ? new Leaf3(entry, Entry0, Entry1) :
                    key > Entry0.Key && key < Entry1.Key ? new Leaf3(Entry0, entry, Entry1) :
                    key == Entry0.Key ? new Leaf2(entry, Entry1) :
                    (ImMap234<V>)new Leaf2(Entry0, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry) =>
                key > Entry1.Key ? new Leaf3(Entry0, Entry1, entry) :
                key < Entry0.Key ? new Leaf3(entry, Entry0, Entry1) :
                key > Entry0.Key && key < Entry1.Key ? new Leaf3(Entry0, entry, Entry1) :
                (ImMap234<V>)this;

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry1.Key ? new Leaf3(Entry0, Entry1, entry) :
                    key < Entry0.Key ? new Leaf3(entry, Entry0, Entry1) :
                    key > Entry0.Key && key < Entry1.Key ? new Leaf3(Entry0, entry, Entry1) :
                    (ImMap234<V>)this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Entry0.Key ? Entry0 :
                key == Entry1.Key ? Entry1 :
                null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                reduce(Entry1, reduce(Entry0, state));

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return Entry0;
                yield return Entry1;
            }
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf3 : ImMap234<V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;

            /// <summary>Right entry</summary>
            public readonly Entry Entry1;

            /// <summary>Rightmost leaf</summary>
            public readonly Entry Entry2;

            /// <summary>Constructs a tree leaf</summary>
            public Leaf3(Entry entry0, Entry entry1, Entry entry2)
            {
                Entry0 = entry0;
                Entry1 = entry1;
                Entry2 = entry2;
            }

            /// Pretty-print
            public override string ToString() => Entry0 + "|" + Entry1 + "|" + Entry2;

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry) =>
                key > Entry2.Key ? new Leaf4(Entry0, Entry1, Entry2, entry)
                : key < Entry0.Key ? new Leaf4(entry, Entry0, Entry1, Entry2)
                : key > Entry0.Key && key < Entry1.Key ? new Leaf4(Entry0, entry, Entry1, Entry2)
                : key > Entry1.Key && key < Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry2)
                : key == Entry0.Key ? new Leaf3(entry, Entry1, Entry2)
                : key == Entry1.Key ? new Leaf3(Entry0, entry, Entry2)
                : (ImMap234<V>)new Leaf3(Entry0, Entry1, entry);

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry2.Key ? new Leaf4(Entry0, Entry1, Entry2, entry)
                    : key < Entry0.Key ? new Leaf4(entry, Entry0, Entry1, Entry2)
                    : key > Entry0.Key && key < Entry1.Key ? new Leaf4(Entry0, entry, Entry1, Entry2)
                    : key > Entry1.Key && key < Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry2)
                    : key == Entry0.Key ? new Leaf3(entry, Entry1, Entry2)
                    : key == Entry1.Key ? new Leaf3(Entry0, entry, Entry2)
                    : (ImMap234<V>)new Leaf3(Entry0, Entry1, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry) =>
                key > Entry2.Key ? new Leaf4(Entry0, Entry1, Entry2, entry)
                : key < Entry0.Key ? new Leaf4(entry, Entry0, Entry1, Entry2)
                : key > Entry0.Key && key < Entry1.Key ? new Leaf4(Entry0, entry, Entry1, Entry2)
                : key > Entry1.Key && key < Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry2)
                : (ImMap234<V>)this;

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry2.Key ? new Leaf4(Entry0, Entry1, Entry2, entry)
                    : key < Entry0.Key ? new Leaf4(entry, Entry0, Entry1, Entry2)
                    : key > Entry0.Key && key < Entry1.Key ? new Leaf4(Entry0, entry, Entry1, Entry2)
                    : key > Entry1.Key && key < Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry2)
                    : (ImMap234<V>)this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Entry0.Key ? Entry0 :
                key == Entry1.Key ? Entry1 :
                key == Entry2.Key ? Entry2 :
                null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                reduce(Entry2, reduce(Entry1, reduce(Entry0, state)));

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return Entry0;
                yield return Entry1;
                yield return Entry2;
            }
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf4 : ImMap234<V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;

            /// <summary>Middle</summary>
            public readonly Entry Entry1;

            /// <summary>Right 0</summary>
            public readonly Entry Entry2;

            /// <summary>Right 1</summary>
            public readonly Entry Entry3;

            /// <summary>Constructs a tree leaf</summary>
            public Leaf4(Entry entry0, Entry entry1, Entry entry2, Entry entry3)
            {
                Entry0 = entry0;
                Entry1 = entry1;
                Entry2 = entry2;
                Entry3 = entry3;
            }

            /// Pretty-print
            public override string ToString() => Entry0 + "|" + Entry1 + "|" + Entry2 + "|" + Entry3;

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                if (key > Entry3.Key)
                    return new Leaf5(Entry0, Entry1, Entry2, Entry3, entry);

                if (key < Entry0.Key)
                    return new Leaf5(entry, Entry0, Entry1, Entry2, Entry3);

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Leaf5(Entry0, entry, Entry1, Entry2, Entry3);

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Leaf5(Entry0, Entry1, entry, Entry2, Entry3);

                if (key > Entry2.Key && key < Entry3.Key)
                    return new Leaf5(Entry0, Entry1, Entry2, entry, Entry3);

                return key == Entry0.Key ? new Leaf4(entry, Entry1, Entry2, Entry3)
                    : key == Entry1.Key ? new Leaf4(Entry0, entry, Entry2, Entry3)
                    : key == Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry3)
                    : new Leaf4(Entry0, Entry1, Entry2, entry);
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                if (key > Entry3.Key)
                    return new Leaf5(Entry0, Entry1, Entry2, Entry3, entry);

                if (key < Entry0.Key)
                    return new Leaf5(entry, Entry0, Entry1, Entry2, Entry3);

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Leaf5(Entry0, entry, Entry1, Entry2, Entry3);

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Leaf5(Entry0, Entry1, entry, Entry2, Entry3);

                if (key > Entry2.Key && key < Entry3.Key)
                    return new Leaf5(Entry0, Entry1, Entry2, entry, Entry3);

                return key == Entry0.Key ? new Leaf4(entry, Entry1, Entry2, Entry3)
                    : key == Entry1.Key ? new Leaf4(Entry0, entry, Entry2, Entry3)
                    : key == Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry3)
                    : new Leaf4(Entry0, Entry1, Entry2, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry) =>
                key > Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, Entry3, entry)
                : key < Entry0.Key ? new Leaf5(entry, Entry0, Entry1, Entry2, Entry3)
                : key > Entry0.Key && key < Entry1.Key ? new Leaf5(Entry0, entry, Entry1, Entry2, Entry3)
                : key > Entry1.Key && key < Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry2, Entry3)
                : key > Entry2.Key && key < Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry3)
                : (ImMap234<V>)this;

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, Entry3, entry)
                    : key < Entry0.Key ? new Leaf5(entry, Entry0, Entry1, Entry2, Entry3)
                    : key > Entry0.Key && key < Entry1.Key ? new Leaf5(Entry0, entry, Entry1, Entry2, Entry3)
                    : key > Entry1.Key && key < Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry2, Entry3)
                    : key > Entry2.Key && key < Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry3)
                    : (ImMap234<V>)this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Entry0.Key ? Entry0 :
                key == Entry1.Key ? Entry1 :
                key == Entry2.Key ? Entry2 :
                key == Entry3.Key ? Entry3 :
                null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                reduce(Entry3, reduce(Entry2, reduce(Entry1, reduce(Entry0, state))));

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return Entry0;
                yield return Entry1;
                yield return Entry2;
                yield return Entry3;
            }
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf5 : ImMap234<V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;

            /// <summary>Middle</summary>
            public readonly Entry Entry1;

            /// <summary>Middle</summary>
            public readonly Entry Entry2;

            /// <summary>Right 1</summary>
            public readonly Entry Entry3;

            /// <summary>Right 2</summary>
            public readonly Entry Entry4;

            /// <summary>Constructs a tree leaf</summary>
            public Leaf5(Entry entry0, Entry entry1, Entry entry2, Entry entry3, Entry entry4)
            {
                Entry0 = entry0;
                Entry1 = entry1;
                Entry2 = entry2;
                Entry3 = entry3;
                Entry4 = entry4;
            }

            /// Pretty-print
            public override string ToString() => Entry0 + "," + Entry1 + " <- " + Entry2 + " -> " + Entry3 + "," + Entry4;

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                if (key > Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(Entry4, entry));

                if (key < Entry0.Key)
                    return new Branch2(new Leaf2(entry, Entry0), Entry1, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Branch2(new Leaf2(Entry0, entry), Entry1, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Branch2(new Leaf2(Entry0, Entry1), entry, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry2.Key && key < Entry3.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), entry, new Leaf2(Entry3, Entry4));

                if (key > Entry3.Key && key < Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(entry, Entry4));

                return key == Entry0.Key ? new Leaf5(entry, Entry1, Entry2, Entry3, Entry4)
                    :  key == Entry1.Key ? new Leaf5(Entry0, entry, Entry2, Entry3, Entry4)
                    :  key == Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry3, Entry4)
                    :  key == Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry4)
                    :                      new Leaf5(Entry0, Entry1, Entry2, Entry3, entry);
            }

            /// <summary>Produces the new or updated leaf or
            /// the split Branch2 nodes: returns the left branch, entry is changed to the Branch Entry0, popRight is the right branch</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                if (key > Entry4.Key)
                {
                    popRight = new Leaf2(Entry4, entry);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key < Entry0.Key)
                {
                    var left = new Leaf2(entry, Entry0);
                    entry = Entry1;
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return left;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    var left = new Leaf2(Entry0, entry);
                    entry = Entry1;
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return left;
                }

                if (key > Entry1.Key && key < Entry2.Key)
                {
                    // the entry is kept as-is
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return new Leaf2(Entry0, Entry1);
                }

                if (key > Entry2.Key && key < Entry3.Key)
                {
                    // the entry is kept as-is
                    popRight = new Leaf2(Entry3, Entry4);
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key > Entry3.Key && key < Entry4.Key)
                {
                    popRight = new Leaf2(entry, Entry4);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                popRight = null;
                return key == Entry0.Key ? new Leaf5(entry, Entry1, Entry2, Entry3, Entry4)
                    :  key == Entry1.Key ? new Leaf5(Entry0, entry, Entry2, Entry3, Entry4)
                    :  key == Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry3, Entry4)
                    :  key == Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry4)
                    :                      new Leaf5(Entry0, Entry1, Entry2, Entry3, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry)
            {
                if (key > Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(Entry4, entry));

                if (key < Entry0.Key)
                    return new Branch2(new Leaf2(entry, Entry0), Entry1, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Branch2(new Leaf2(Entry0, entry), Entry1, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Branch2(new Leaf2(Entry0, Entry1), entry, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry2.Key && key < Entry3.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), entry, new Leaf2(Entry3, Entry4));

                if (key > Entry3.Key && key < Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(entry, Entry4));

                return this;
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                if (key > Entry4.Key)
                {
                    popRight = new Leaf2(Entry4, entry);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key < Entry0.Key)
                {
                    var left = new Leaf2(entry, Entry0);
                    entry = Entry1;
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return left;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    var left = new Leaf2(Entry0, entry);
                    entry = Entry1;
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return left;
                }

                if (key > Entry1.Key && key < Entry2.Key)
                {
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return new Leaf2(Entry0, Entry1);
                }

                if (key > Entry2.Key && key < Entry3.Key)
                {
                    popRight = new Leaf2(Entry3, Entry4);
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key > Entry3.Key && key < Entry4.Key)
                {
                    popRight = new Leaf2(entry, Entry4);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                popRight = null; 
                return this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Entry0.Key ? Entry0 :
                key == Entry1.Key ? Entry1 :
                key == Entry2.Key ? Entry2 :
                key == Entry3.Key ? Entry3 :
                key == Entry4.Key ? Entry4 :
                null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                reduce(Entry4, reduce(Entry3, reduce(Entry2, reduce(Entry1, reduce(Entry0, state)))));

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return Entry0;
                yield return Entry1;
                yield return Entry2;
                yield return Entry3;
                yield return Entry4;
            }
        }

        /// <summary>2 branches - it is never split itself, but may produce Branch3 if the lower branches are split</summary>
        public sealed class Branch2 : ImMap234<V>
        {
            /// <summary>The only entry</summary>
            public readonly Entry Entry0;

            /// <summary>Left branch</summary>
            public readonly ImMap234<V> Left;

            /// <summary>Right branch</summary>
            public readonly ImMap234<V> Right;

            /// <summary>Constructs</summary>
            public Branch2(ImMap234<V> left, Entry entry0, ImMap234<V> right)
            {
                Left = left;
                Entry0 = entry0;
                Right = right;
            }

            /// Pretty-print
            public override string ToString() =>
                (Left is Branch2 ? Left.GetType().Name : Left.ToString()) +
                " <- " + Entry0 + " -> " +
                (Right is Branch2 ? Right.GetType().Name : Right.ToString());

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                if (key > Entry0.Key)
                {
                    var newBranch = Right.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch3(Left, Entry0, newBranch, entry, popRight);
                    return new Branch2(Left, Entry0, newBranch);
                }

                if (key < Entry0.Key)
                {
                    var newBranch = Left.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch3(newBranch, entry, popRight, Entry0, Right);
                    return new Branch2(newBranch, Entry0, Right);
                }

                // update
                return new Branch2(Left, entry, Right);
            }

            /// <summary>Produces the new or updated branch</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                if (key > Entry0.Key)
                {
                    var newBranch = Right.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                        return new Branch3(Left, Entry0, newBranch, entry, popRightBelow);
                    return new Branch2(Left, Entry0, newBranch);
                }

                if (key < Entry0.Key)
                {
                    var newBranch = Left.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                        return new Branch3(newBranch, entry, popRightBelow, Entry0, Right);
                    return new Branch2(newBranch, Entry0, Right);
                }

                return new Branch2(Left, entry, Right);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry)
            {
                if (key > Entry0.Key)
                {
                    var newBranch = Right.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    return popRight != null ? new Branch3(Left, Entry0, newBranch, entry, popRight) 
                        : newBranch != Right ? new Branch2(Left, Entry0, newBranch)
                        : (ImMap234<V>)this;
                }

                if (key < Entry0.Key)
                {
                    var newBranch = Left.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    return popRight != null ? new Branch3(newBranch, entry, popRight, Entry0, Right)
                        : newBranch != Left ? new Branch2(newBranch, Entry0, Right) 
                        : (ImMap234<V>)this;
                }

                return this;
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                if (key > Entry0.Key)
                {
                    var newBranch = Right.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                        return new Branch3(Left, Entry0, newBranch, entry, popRightBelow);
                    if (newBranch != Right)
                        return new Branch2(Left, Entry0, newBranch);
                    return this;
                }

                if (key < Entry0.Key)
                {
                    var newBranch = Left.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                        return new Branch3(newBranch, entry, popRightBelow, Entry0, Right);
                    if (newBranch != Left)
                        return new Branch2(newBranch, Entry0, Right);
                    return this;
                }

                return this;
            }

            // todo: @perf how to get rid of nested GetEntryOrDefault call if branches are leafs
            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key > Entry0.Key ? Right.GetEntryOrDefault(key) :
                key < Entry0.Key ? Left .GetEntryOrDefault(key) :
                Entry0;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                Right.Fold(reduce(Entry0, Left.Fold(state, reduce)), reduce);

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                foreach (var l in Left.Enumerate())
                    yield return l;
                yield return Entry0;
                foreach (var r in Right.Enumerate())
                    yield return r;
            }
        }

        /// <summary>3 branches</summary>
        public sealed class Branch3 : ImMap234<V>
        {
            /// <summary>Left branch</summary>
            public readonly ImMap234<V> Left;

            /// <summary>The only entry</summary>
            public readonly Entry Entry0;

            /// <summary>Right branch</summary>
            public readonly ImMap234<V> Middle;

            /// <summary>Right entry</summary>
            public readonly Entry Entry1;

            /// <summary>Rightmost branch</summary>
            public readonly ImMap234<V> Right;

            /// <summary>Constructs</summary>
            public Branch3(ImMap234<V> left, Entry entry0, ImMap234<V> middle, Entry entry1, ImMap234<V> right)
            {
                Left = left;
                Entry0 = entry0;
                Middle = middle;
                Entry1 = entry1;
                Right = right;
            }

            /// Pretty-print
            public override string ToString() =>
                (Left is Branch2 ? Left.GetType().Name : Left.ToString()) +
                " <- " + Entry0 + " -> " +
                (Middle is Branch2 ? Middle.GetType().Name : Middle.ToString()) +
                " <- " + Entry0 + " -> " +
                (Right is Branch2 ? Right.GetType().Name.TrimEnd('<', '>', '`', 'V') : Right.ToString());

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                ImMap234<V> newBranch;
                if (key > Entry1.Key)
                {
                    newBranch = Right.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(Left, Entry0, Middle), Entry1, new Branch2(newBranch, entry, popRight));
                    return new Branch3(Left, Entry0, Middle, Entry1, newBranch);
                }

                if (key < Entry0.Key)
                {
                    newBranch = Left.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(newBranch, entry, popRight), Entry0, new Branch2(Middle, Entry1, Right));
                    return new Branch3(newBranch, Entry0, Middle, Entry1, Right);
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    newBranch = Middle.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(Left, Entry0, newBranch), entry, new Branch2(popRight, Entry1, Right));
                    return new Branch3(Left, Entry0, newBranch, Entry1, Right);
                }

                // update
                return key == Entry0.Key
                    ? new Branch3(Left, entry, Middle, Entry1, Right)
                    : new Branch3(Left, Entry0, Middle, entry, Right);
            }

            /// <summary>Produces the new or updated leaf or
            /// the split Branch2 nodes: returns the left branch, entry is changed to the Branch Entry0, popRight is the right branch</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                ImMap234<V> newBranch;
                if (key > Entry1.Key)
                {
                    // for example:
                    //                                             [5]
                    //        [2,5]                =>      [2]               [9]
                    // [0,1]  [3,4]  [6,7,8,9,10]    [0,1]    [3,4]   [6,7,8]   [10,11]
                    // and adding 11
                    newBranch = Right.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new Branch2(newBranch, entry, popRightBelow);
                        entry = Entry1;
                        return new Branch2(Left, Entry0, Middle);
                    }
                    return new Branch3(Left, Entry0, Middle, Entry1, newBranch);
                }

                if (key < Entry0.Key)
                {
                    newBranch = Left.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        newBranch = new Branch2(newBranch, entry, popRightBelow);
                        entry = Entry0;
                        popRight = new Branch2(Middle, Entry1, Right);
                        return newBranch;
                    }
                    return new Branch3(newBranch, Entry0, Middle, Entry1, Right);
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    //                              [4]
                    //       [2, 7]            [2]         [7]
                    // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                    // and adding 6
                    newBranch = Middle.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new Branch2(popRightBelow, Entry1, Right);
                        return new Branch2(Left, Entry0, newBranch);
                    }
                    return new Branch3(Left, Entry0, newBranch, Entry1, Right);
                }

                // update
                return key == Entry0.Key
                    ? new Branch3(Left, entry, Middle, Entry1, Right)
                    : new Branch3(Left, Entry0, Middle, entry, Right);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry)
            {
                ImMap234<V> newBranch;
                if (key > Entry1.Key)
                {
                    newBranch = Right.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(Left, Entry0, Middle), Entry1, new Branch2(newBranch, entry, popRight));
                    return newBranch != Right ? new Branch3(Left, Entry0, Middle, Entry1, newBranch) : (ImMap234<V>)this;
                }

                if (key < Entry0.Key)
                {
                    newBranch = Left.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(newBranch, entry, popRight), Entry0, new Branch2(Middle, Entry1, Right));
                    return newBranch != Left ? new Branch3(newBranch, Entry0, Middle, Entry1, Right) : this;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    newBranch = Middle.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(Left, Entry0, newBranch), entry, new Branch2(popRight, Entry1, Right));
                    return newBranch != Middle ? new Branch3(Left, Entry0, newBranch, Entry1, Right) : this;
                }

                return this;
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                ImMap234<V> newBranch;
                if (key > Entry1.Key)
                {
                    // for example:
                    //                                             [5]
                    //        [2,5]                =>      [2]               [9]
                    // [0,1]  [3,4]  [6,7,8,9,10]    [0,1]    [3,4]   [6,7,8]   [10,11]
                    // and adding 11
                    newBranch = Right.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new Branch2(newBranch, entry, popRightBelow);
                        entry = Entry1;
                        return new Branch2(Left, Entry0, Middle);
                    }
                    return newBranch != Right ? new Branch3(Left, Entry0, Middle, Entry1, newBranch) : this;
                }

                if (key < Entry0.Key)
                {
                    newBranch = Left.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        var left = new Branch2(newBranch, entry, popRightBelow);
                        entry = Entry0;
                        popRight = new Branch2(Middle, Entry1, Right);
                        return left;
                    }
                    return newBranch != Left ? new Branch3(newBranch, Entry0, Middle, Entry1, Right) : this;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    //                              [4]
                    //       [2, 7]            [2]         [7]
                    // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                    // and adding 6
                    newBranch = Middle.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new Branch2(popRightBelow, Entry1, Right);
                        return new Branch2(Left, Entry0, newBranch);
                    }
                    return newBranch != Middle ? new Branch3(Left, Entry0, newBranch, Entry1, Right) : this;
                }

                return this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key > Entry1.Key ? Right.GetEntryOrDefault(key) :
                key < Entry0.Key ? Left .GetEntryOrDefault(key) :
                key > Entry0.Key && key < Entry1.Key ? Middle.GetEntryOrDefault(key) :
                key == Entry0.Key ? Entry0 : Entry1;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                Right.Fold(reduce(Entry1, Middle.Fold(reduce(Entry0, Left.Fold(state, reduce)), reduce)), reduce);

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                foreach (var l in Left.Enumerate())
                    yield return l;
                yield return Entry0;
                foreach (var m in Middle.Enumerate())
                    yield return m;
                yield return Entry1;
                foreach (var r in Right.Enumerate())
                    yield return r;
            }
        }
    }

    /// <summary>ImMap methods</summary>
    public static class ImMap234
    {
        /// <summary>Adds or updates the value by key in the map, always returns a modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> AddOrUpdate<V>(this ImMap234<V> map, int key, V value) =>
            map == ImMap234<V>.Empty
                ? new ImMap234<V>.Entry(key, value)
                : map.AddOrUpdateEntry(key, new ImMap234<V>.Entry(key, value));

        /// <summary>Adds the entry or keeps the map intact.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> AddOrKeep<V>(this ImMap234<V> map, int key, V value) =>
            map == ImMap234<V>.Empty
                ? new ImMap234<V>.Entry(key, value)
                : map.AddOrKeepEntry(key, new ImMap234<V>.Entry(key, value));

        /// <summary>Adds the entry or keeps the map intact.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> AddOrKeep<V>(this ImMap234<V> map, ImMap234<V>.Entry entry) => 
            map.AddOrKeepEntry(entry.Key, entry);

        /// <summary>Lookup</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap234<V> map, int key)
        {
            var entry = map.GetEntryOrDefault(key);
            return entry != null ? entry.Value : default(V);
        }

        /// <summary>Lookup</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static bool TryFind<V>(this ImMap234<V> map, int key, out V value)
        {
            var entry = map.GetEntryOrDefault(key);
            if (entry != null)
            {
                value = entry.Value;
                return true;
            }

            value = default(V);
            return false;
        }

        /// Default number of slots
        public const int SLOT_COUNT_POWER_OF_TWO = 32;

        /// The default mask to partition the key to the target slot
        public const int KEY_MASK_TO_FIND_SLOT = SLOT_COUNT_POWER_OF_TWO - 1;

        /// Creates the array with the empty slots
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V>[] CreateWithEmpty<V>(int slotCountPowerOfTwo = SLOT_COUNT_POWER_OF_TWO)
        {
            var slots = new ImMap234<V>[slotCountPowerOfTwo];
            for (var i = 0; i < slots.Length; ++i)
                slots[i] = ImMap234<V>.Empty;
            return slots;
        }

        /// Returns a new tree with added or updated value for specified key.
        [MethodImpl((MethodImplOptions)256)]
        public static void AddOrUpdate<V>(this ImMap234<V>[] slots, int key, V value, int keyMaskToFindSlot = KEY_MASK_TO_FIND_SLOT)
        {
            ref var slot = ref slots[key & keyMaskToFindSlot];
            var copy = slot;
            if (Interlocked.CompareExchange(ref slot, copy.AddOrUpdate(key, value), copy) != copy)
                RefAddOrUpdateSlot(ref slot, key, value);
        }

        /// Update the ref to the slot with the new version - retry if the someone changed the slot in between
        public static void RefAddOrUpdateSlot<V>(ref ImMap234<V> slot, int key, V value) =>
            Ref.Swap(ref slot, key, value, (x, k, v) => x.AddOrUpdate(k, v));
    }
}
