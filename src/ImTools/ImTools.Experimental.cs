using System;
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
                    return newLeftTree.TreeHeight == leftTree.TreeHeight 
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
                                : (ImMap<V>)new ImMapBranch<V>(leftBranch.Entry, new ImMapEntry<V>(key, value));

                        return new ImMapTree<V>(Entry, newLeft, Right, TreeHeight);
                    }

                    return new ImMapTree<V>(Entry, 
                        new ImMapBranch<V>(new ImMapEntry<V>(key, value), leftBranch.RightEntry), Right, TreeHeight);
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

                    var newRightTree = rightTree.AddOrUpdateLeftOrRight(key, value);
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
                var leftLeftHeight  = (newLeftTree.Left  as ImMapTree<V>)?.TreeHeight ?? 2;
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

                // todo: optimize this the same as below
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
                var rightLeftHeight  = (newRightTree.Left  as ImMapTree<V>)?.TreeHeight ?? 2;
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

                return key < branch.Entry.Key
                    ? new ImMapTree<V>(branch.Entry, new ImMapEntry<V>(key, value), branch.RightEntry)
                    : (ImMap<V>)new ImMapBranch<V>(new ImMapEntry<V>(key, value), branch.RightEntry);
            }

            var tree = (ImMapTree<V>)map;
            return key == tree.Entry.Key
                ? new ImMapTree<V>(new ImMapEntry<V>(key, value), tree.Left, tree.Right, tree.TreeHeight)
                : tree.AddOrUpdateLeftOrRight(key, value);
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
            map.Contains(key) ? map.UpdateImpl(key, value) : map;

        internal static ImMap<V> UpdateImpl<V>(this ImMap<V> map, int key, V value)
        {
            if (map is ImMapTree<V> tree)
                return key > tree.Entry.Key 
                        ? new ImMapTree<V>(tree.Entry, tree.Left, tree.Right.UpdateImpl(key, value), tree.TreeHeight)
                    : key < tree.Entry.Key 
                        ? new ImMapTree<V>(tree.Entry, tree.Left.UpdateImpl(key, value), tree.Right, tree.TreeHeight)
                    : new ImMapTree<V>(new ImMapEntry<V>(key, value), tree.Left, tree.Right, tree.TreeHeight);

            // the key was found - so it should be either entry or right entry
            if (map is ImMapBranch<V> branch)
                return key == branch.Entry.Key
                    ? new ImMapBranch<V>(new ImMapEntry<V>(key, value), branch.RightEntry)
                    : new ImMapBranch<V>(branch.Entry, new ImMapEntry<V>(key, value));

            return new ImMapEntry<V>(key, value);
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

            //for (var i = 0; i < slots.Length; i++)
            //{
            //    var map = slots[i];
            //    var height = map.Height;
            //    if (height == 0)
            //        continue;
            //    if (height == 1)
            //        state = reduce(map, state);
            //    else if (height == 2)
            //        state = map.ReduceTwoLevelTree(state, reduce);
            //    else
            //    {
            //        if (parentStack.Length < height - 2)
            //            parentStack = new ImMap<V>[height - 2];
            //        var parentIndex = -1;
            //        do
            //        {
            //            if (map.Height == 1)
            //            {
            //                state = reduce(map, state);
            //                if (parentIndex == -1)
            //                    break;
            //                state = reduce(map = parentStack[parentIndex--], state);
            //                map = map.Right;
            //            }
            //            else if (map.Height == 2)
            //            {
            //                state = map.ReduceTwoLevelTree(state, reduce);
            //                if (parentIndex == -1)
            //                    break;
            //                state = reduce(map = parentStack[parentIndex--], state);
            //                map = map.Right;
            //            }
            //            else
            //            {
            //                parentStack[++parentIndex] = map;
            //                map = map.Left;
            //            }
            //        } while (map.Height != 0);
            //    }
            //}

            //return state;
        }
    }

    /// <summary>Map methods</summary>
    public static class ImHashMap
    {
        /// <summary>A</summary>
        public struct KeyValueEntry<K, V>
        {
            /// <summary>B</summary>
            public K Key;
            /// <summary>Value</summary>
            public V Value;
        }

        /// <summary>Uses the user provided hash and adds and updates the tree with passed key-value. Returns a new tree.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<KeyValueEntry<Type, object>> AddOrUpdate<V>(this ImMap<KeyValueEntry<Type, object>> map, int hash, Type key, V value) where V : class
        {
            var oldEntry = map.GetEntryOrDefault(hash);
            if (oldEntry == null)
            {
                //return map.AddEntry(hash, newEntry);
            }

            // todo: do stuff for update and conflicts
            //return map.UpdateEntry(hash, newEntry);

            var entry = map.GetEntryOrDefault(hash);
            if (entry == null)
            {
                // add new entry
                map = map.AddOrKeep(hash); // todo: add pure Add method
                entry = map.GetEntryOrDefault(hash);
                entry.Value.Key = key;
                entry.Value.Value = value;
            }
            else
            {
                // update or add a new conflicting key value
                map = UpdateOrAddConflictedKeyValue(map, hash, key, value, entry);
            }

            return map;
        }

        private static ImMap<KeyValueEntry<Type, object>> UpdateOrAddConflictedKeyValue<V>(
            ImMap<KeyValueEntry<Type, object>> map, int hash, Type key, V value, ImMapEntry<KeyValueEntry<Type, object>> entry) where V : class
        {
            var entryKey = entry.Value.Key;
            if (key == entryKey)
            {
                // update
                map = map.UpdateToDefault(hash);
                entry = map.GetEntryOrDefault(hash); // todo: maybe better to have a `Update(.., Entry e)` so we don't need the `GetEntryOrDefault`
                entry.Value.Value = value;
            }
            else if (entryKey != null)
            {
                // we need to add conflicted pair
                var conflictedEntries = new KeyValueEntry<Type, object>[2];
                
                ref var newVal = ref conflictedEntries[0];
                newVal.Key = key;
                newVal.Value = value;

                ref var oldVal = ref conflictedEntries[1];
                oldVal.Key = entryKey;
                oldVal.Value = entry.Value;
                
                map = map.UpdateToDefault(hash);
                var newEntry =  map.GetEntryOrDefault(hash); // todo: maybe better to have a `Update(.., Entry e)` so we don't need the `GetEntryOrDefault`
                newEntry.Value.Value = conflictedEntries;
            }
            else
            {
                // entry is already containing the conflicted pairs
            }

            return map;
        }
    }
}
