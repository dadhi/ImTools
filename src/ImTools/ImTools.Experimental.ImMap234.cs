using System.Runtime.CompilerServices;

namespace ImTools.Experimental.Tree234
{
    /// <summary>The base class for tree leafs and branches, defines the Empty tree</summary>
    public class ImMap<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap<V> Empty = new ImMap<V>();

        /// <summary>Hide the constructor to prevent the multiple Empty trees creation</summary>
        protected ImMap() { }

        /// Pretty-prints
        public override string ToString() => "empty";

        /// <summary>Produces the new or updated map</summary>
        public virtual ImMap<V> AddOrUpdateEntry(int key, Entry entry) => entry;

        /// <summary>Lookup</summary>
        public virtual V GetValueOrDefault(int key) => default(V);

        /// <summary>Wraps the stored data with "fixed" reference semantics - 
        /// when added to the tree it won't be changed or reconstructed in memory</summary>
        public sealed class Entry : ImMap<V>
        {
            /// <summary>The Key is basically the hash, or the Height for ImMapTree</summary>
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
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry) =>
                key > Key ? new Leaf2(this, entry) :
                key < Key ? new Leaf2(entry, this) :
                (ImMap<V>)entry;

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) => key == Key ? Value : default(V);
        }

        /// <summary>2 leafs</summary>
        public sealed class Leaf2 : ImMap<V>
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
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry) =>
                key > Entry1.Key ? new Leaf3(Entry0, Entry1, entry) :
                key < Entry0.Key ? new Leaf3(entry, Entry0, Entry1) :
                key > Entry0.Key && entry.Key < Entry1.Key ? new Leaf3(Entry0, entry, Entry1) :
                key == Entry0.Key ? new Leaf2(entry, Entry1) :
                (ImMap<V>)new Leaf2(Entry0, entry);

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) =>
                key == Entry0.Key ? Entry0.Value :
                key == Entry1.Key ? Entry1.Value :
                default(V);
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf3 : ImMap<V>
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
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry)
            {
                // [1 3 5]  =>      [3]
                // adding 7     [1]     [5, 7]
                if (key > Entry2.Key)
                    return new Leaf4(Entry0, Entry1, Entry2, entry);

                if (key < Entry0.Key)
                    return new Leaf4(entry, Entry0, Entry1, Entry2);

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Leaf4(Entry0, entry, Entry1, Entry2);

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Leaf4(Entry0, Entry1, entry, Entry2);

                return key == Entry0.Key ? new Leaf3(entry, Entry1, Entry2)
                    :  key == Entry1.Key ? new Leaf3(Entry0, entry, Entry2)
                    :  new Leaf3(Entry0, Entry1, entry);
            }

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) =>
                key == Entry0.Key ? Entry0.Value :
                key == Entry1.Key ? Entry1.Value :
                key == Entry2.Key ? Entry2.Value :
                default(V);
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf4 : ImMap<V>
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
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry)
            {
                // [1 3 5]  =>      [3]
                // adding 7     [1]     [5, 7]
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
                    :  key == Entry1.Key ? new Leaf4(Entry0, entry, Entry2, Entry3)
                    :  key == Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry3)
                    :                      new Leaf4(Entry0, Entry1, Entry2, entry);
            }

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) =>
                key == Entry0.Key ? Entry0.Value :
                key == Entry1.Key ? Entry1.Value :
                key == Entry2.Key ? Entry2.Value :
                key == Entry3.Key ? Entry3.Value :
                default(V);
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf5 : ImMap<V>
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
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry)
            {
                // [1 3 5]  =>      [3]
                // adding 7     [1]     [5, 7]
                if (key > Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(Entry4, entry));

                if (key < Entry0.Key)
                    return new Branch2(new Leaf3(entry, Entry0, Entry1), Entry2, new Leaf2(Entry3, Entry4));

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Branch2(new Leaf3(Entry0, entry, Entry1), Entry2, new Leaf2(Entry3, Entry4));

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, entry), Entry2, new Leaf2(Entry3, Entry4));

                if (key > Entry2.Key && key < Entry3.Key)
                    return new Branch2(new Leaf2(Entry0, Entry1), Entry2, new Leaf3(entry, Entry3, Entry4));

                if (key > Entry3.Key && key < Entry4.Key)
                    return new Branch2(new Leaf2(Entry0, Entry1), Entry2, new Leaf3(Entry3, entry, Entry4));

                return key == Entry0.Key ? new Leaf5(entry, Entry1, Entry2, Entry3, Entry4)
                    :  key == Entry1.Key ? new Leaf5(Entry0, entry, Entry2, Entry3, Entry4)
                    :  key == Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry3, Entry4)
                    :  key == Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry4)
                    :                      new Leaf5(Entry0, Entry1, Entry2, Entry3, entry);
            }

            /// <summary>Produces the new or updated map</summary>
            public ImMap<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap<V> popRight)
            {
                // [1 3 5]  =>      [3]
                // adding 7     [1]     [5, 7]
                if (key > Entry4.Key)
                {
                    popRight = new Leaf2(Entry4, entry);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key < Entry0.Key)
                {
                    popRight = new Leaf2(Entry3, Entry4);
                    var left = new Leaf3(entry, Entry0, Entry1);
                    entry = Entry2;
                    return left;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    popRight = new Leaf2(Entry3, Entry4);
                    var left = new Leaf3(Entry0, entry, Entry1);
                    entry = Entry2;
                    return left;
                }

                if (key > Entry1.Key && key < Entry2.Key)
                {
                    popRight = new Leaf2(Entry3, Entry4);
                    var left = new Leaf3(Entry0, Entry1, entry);
                    entry = Entry2;
                    return left;
                }

                if (key > Entry2.Key && key < Entry3.Key)
                {
                    popRight = new Leaf3(entry, Entry3, Entry4);
                    entry = Entry2;
                    return new Leaf2(Entry0, Entry1);
                }

                if (key > Entry3.Key && key < Entry4.Key)
                {
                    popRight = new Leaf3(Entry3, entry, Entry4);
                    entry = Entry2;
                    return new Leaf2(Entry0, Entry1);
                }

                popRight = null;
                return key == Entry0.Key ? new Leaf5(entry, Entry1, Entry2, Entry3, Entry4)
                    :  key == Entry1.Key ? new Leaf5(Entry0, entry, Entry2, Entry3, Entry4)
                    :  key == Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry3, Entry4)
                    :  key == Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry4)
                    :                      new Leaf5(Entry0, Entry1, Entry2, Entry3, entry);
            }

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) =>
                key == Entry0.Key ? Entry0.Value :
                key == Entry1.Key ? Entry1.Value :
                key == Entry2.Key ? Entry2.Value :
                key == Entry3.Key ? Entry3.Value :
                key == Entry4.Key ? Entry4.Value :
                default(V);
        }

        /// <summary>2 branches - it is never split itself, but may produce Branch3 if the lower branches are split</summary>
        public sealed class Branch2 : ImMap<V>
        {
            /// <summary>The only entry</summary>
            public readonly Entry Entry0;

            /// <summary>Left branch</summary>
            public readonly ImMap<V> Left;

            /// <summary>Right branch</summary>
            public readonly ImMap<V> Right;

            /// <summary>Constructs</summary>
            public Branch2(ImMap<V> left, Entry entry0, ImMap<V> right)
            {
                Left   = left;
                Entry0 = entry0;
                Right  = right;
            }

            /// Pretty-print
            public override string ToString() =>
                (Left is Branch2 ? Left.GetType().Name : Left.ToString()) +
                " <- " + Entry0 + " -> " +
                (Right is Branch2 ? Right.GetType().Name : Right.ToString());

            /// <summary>Produces the new or updated map</summary>
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry)
            {
                if (key > Entry0.Key)
                {
                    //      [3]                     [3]    ->  [6]                [3, 6]
                    // [1]       [5, 6, 7] =>  [1]       [4,5]     [7] =>  [1]   [4, 5]   [7]
                    // adding 4, so we are merging the branches

                    //         [4]
                    //    [2]        [6]
                    // [1]   [3]   [5]   [7, 8, 9?]
                    // adding 9 - no need to merge

                    //         [4]
                    //    [2]         [6, 8]
                    // [1]   [3]   [5]  [7]  [9, 10, 11] 12?
                    // adding 12

                    // How to detect a split !!!
                    // 1. The branch is Leaf3 - because we still will be checking on that, let's just check beforehand and do an addition 
                    // 2. The branch is Branch3 but now is not Branch3 - so it is split
                    //
                    // we are splitting the Leaf in a way to provide more free room on the side of newly added entry
                    //
                    if (Right is Leaf5 lf)
                    {
                        var e4 = lf.Entry4;
                        if (key > e4.Key)
                            return new Branch3(Left, Entry0, new Leaf3(lf.Entry0, lf.Entry1, lf.Entry2), lf.Entry3, new Leaf2(e4, entry));

                        var e0 = lf.Entry0;
                        if (key < e0.Key)
                            return new Branch3(Left, Entry0, new Leaf2(entry, e0), lf.Entry1, new Leaf3(lf.Entry2, lf.Entry3, e4));

                        var e1 = lf.Entry1;
                        if (key > e0.Key && key < e1.Key)
                            return new Branch3(Left, Entry0, new Leaf2(e0, entry), e1, new Leaf3(lf.Entry2, lf.Entry3, e4));

                        var e2 = lf.Entry2;
                        if (key > e1.Key && key < e2.Key)
                            return new Branch3(Left, Entry0, new Leaf2(e0, e1), entry, new Leaf3(e2, lf.Entry3, e4));

                        var e3 = lf.Entry3;
                        if (key > e2.Key && key < e3.Key)
                            return new Branch3(Left, Entry0, new Leaf3(e0, e1, e2), entry, new Leaf2(e3, e4));

                        if (key > e3.Key && key < e4.Key)
                            return new Branch3(Left, Entry0, new Leaf3(e0, e1, e2), e3, new Leaf2(entry, e4));

                        return new Branch2(Left, Entry0, 
                            key == e0.Key ? new Leaf5(entry, e1, e2, e3, e4) : 
                            key == e1.Key ? new Leaf5(e0, entry, e2, e3, e4) : 
                            key == e2.Key ? new Leaf5(e0, e1, entry, e3, e4) : 
                            key == e3.Key ? new Leaf5(e0, e1, e2, entry, e4) :
                                            new Leaf5(e0, e1, e2, e3, entry));
                    }

                    if (Right is Branch3 br3)
                    {
                        var newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                        if (popRight != null)
                            return new Branch3(Left, Entry0, newBranch, entry, popRight);
                        return new Branch2(Left, Entry0, newBranch);
                    }

                    return new Branch2(Left, Entry0, Right.AddOrUpdateEntry(key, entry));
                }

                if (key < Entry0.Key)
                {
                    //           [4]                [2]  <-  [4]
                    // [1, 2, 3]      [5] =>  [0, 1]    [3]      [5]  =>
                    // and adding 0, so we are merging the branches
                    if (Left is Leaf5 lf)
                    {
                        // [1 3 5]  =>      [3]
                        // adding 7     [1]     [5, 7]
                        var e4 = lf.Entry4;
                        if (key > e4.Key)
                            return new Branch3(new Leaf3(lf.Entry0, lf.Entry1, lf.Entry2), lf.Entry3, new Leaf2(e4, entry), Entry0, Right);

                        var e0 = lf.Entry0;
                        if (key < e0.Key)
                            return new Branch3(new Leaf2(entry, e0), lf.Entry1, new Leaf3(lf.Entry2, lf.Entry3, e4), Entry0, Right);

                        var e1 = lf.Entry1;
                        if (key > e0.Key && key < e1.Key)
                            return new Branch3(new Leaf2(e0, entry), e1, new Leaf3(lf.Entry2, lf.Entry3, e4), Entry0, Right);

                        var e2 = lf.Entry2;
                        if (key > e1.Key && key < e2.Key)
                            return new Branch3(new Leaf2(e0, e1), entry, new Leaf3(e2, lf.Entry3, e4), Entry0, Right);

                        var e3 = lf.Entry3;
                        if (key > e2.Key && key < e3.Key)
                            return new Branch3(new Leaf3(e0, e1, e2), entry, new Leaf2(e3, e4), Entry0, Right);

                        if (key > e3.Key && key < e4.Key)
                            return new Branch3(new Leaf3(e0, e1, e2), e3, new Leaf2(entry, e4), Entry0, Right);

                        return new Branch2(
                            key == e0.Key ? new Leaf5(entry, e1, e2, e3, e4) : 
                            key == e1.Key ? new Leaf5(e0, entry, e2, e3, e4) : 
                            key == e2.Key ? new Leaf5(e0, e1, entry, e3, e4) : 
                            key == e3.Key ? new Leaf5(e0, e1, e2, entry, e4) :
                                            new Leaf5(e0, e1, e2, e3, entry),
                            Entry0, Right);
                    }

                    if (Left is Branch3 br3)
                    {
                        var newLeft = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                        if (popRight != null)
                            return new Branch3(newLeft, entry, popRight, Entry0, Right);
                        return new Branch2(newLeft, Entry0, Right);
                    }

                    return new Branch2(Left.AddOrUpdateEntry(key, entry), Entry0, Right);
                }

                // update
                return new Branch2(Left, entry, Right);
            }

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) =>
                key > Entry0.Key ? Right.GetValueOrDefault(key) :
                key < Entry0.Key ? Left.GetValueOrDefault(key) :
                key == Entry0.Key ? Entry0.Value : default(V);
        }

        /// <summary>3 branches</summary>
        public sealed class Branch3 : ImMap<V>
        {
            /// <summary>Left branch</summary>
            public readonly ImMap<V> Left;

            /// <summary>The only entry</summary>
            public readonly Entry Entry0;

            /// <summary>Right branch</summary>
            public readonly ImMap<V> Middle;

            /// <summary>Right entry</summary>
            public readonly Entry Entry1;

            /// <summary>Rightmost branch</summary>
            public readonly ImMap<V> Right;

            /// <summary>Constructs</summary>
            public Branch3(ImMap<V> left, Entry entry0, ImMap<V> middle, Entry entry1, ImMap<V> right) 
            {
                Left   = left;
                Entry0 = entry0;
                Middle = middle;
                Entry1 = entry1;
                Right  = right;
            }

            /// Pretty-print
            public override string ToString() =>
                (Left is Branch2 ? Left.GetType().Name : Left.ToString()) +
                " <- " + Entry0 + " -> " +
                (Middle is Branch2 ? Middle.GetType().Name : Middle.ToString()) +
                " <- " + Entry0 + " -> " +
                (Right is Branch2 ? Right.GetType().Name.TrimEnd('<', '>', '`', 'V') : Right.ToString());

            /// <summary>Produces the new or updated map</summary>
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry)
            {
                if (key > Entry1.Key)
                {
                    //                                                   =>          [4]
                    //     [2, 4]                   [2, 4]  ->  [6]             [2]         [6]
                    // [1]   [3]  [5, 6, 7] =>  [1]   [3]    [5]   [7, 8]    [1]   [3]   [5]   [7,8]
                    // and adding 8

                    var newRight = Right.AddOrUpdateEntry(key, entry);
                    if (Right is Leaf5   && newRight is Leaf5   == false ||
                        Right is Branch3 && newRight is Branch3 == false)
                        return new Branch2(new Branch2(Left, Entry0, Middle), Entry1, newRight);

                    return new Branch3(Left, Entry0, Middle, Entry1, newRight);
                }

                if (key < Entry0.Key)
                {
                    //                                  [4]
                    //         [4,6]      =>       [2]        [6]
                    // [1,2,3]   [5]  [7]      [0,1]  [3]   [5]   [7]
                    // and adding 0

                    var newLeft = Left.AddOrUpdateEntry(key, entry);
                    if (Left is Leaf5   && newLeft is Leaf5   == false ||
                        Left is Branch3 && newLeft is Branch3 == false)
                        return new Branch2(newLeft, Entry0, new Branch2(Middle, Entry1, Right));

                    return new Branch3(newLeft, Entry0, Middle, Entry1, Right);
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    if (Middle is Leaf5 lf)
                    {
                        //                                      [6]
                        //        [2, 9]                [2]              [9]
                        // [1]  [3,4,5,6,7]  [10] => [1]  [3,4,5]   [7,8]    [10]
                        // and adding 8
                        var e4 = lf.Entry4;
                        if (key > e4.Key)
                            return new Branch2(new Branch2(Left, Entry0, new Leaf3(lf.Entry0, lf.Entry1, lf.Entry2)), 
                                lf.Entry3, new Branch2(new Leaf2(e4, entry), Entry1, Right));

                        //                                     [4]
                        //        [1, 9]                [1]              [9]
                        // [0]  [3,4,5,6,7]  [10] => [0]  [2,3]   [5,6,7]    [10]
                        // and adding 2
                        var e0 = lf.Entry0;
                        if (key < e0.Key)
                            return new Branch2(new Branch2(Left, Entry0, new Leaf2(entry, e0)), 
                                lf.Entry1, new Branch2(new Leaf3(lf.Entry2, lf.Entry3, e4), Entry1, Right));

                        //                                     [4]
                        //        [1, 9]                [1]              [9]
                        // [0]  [2,4,5,6,7]  [10] => [0]  [2,3]   [5,6,7]    [10]
                        // and adding 3
                        var e1 = lf.Entry1;
                        if (key > e0.Key && key < e1.Key)
                            return new Branch2(new Branch2(Left, Entry0, new Leaf2(e0, entry)),
                                e1, new Branch2(new Leaf3(lf.Entry2, lf.Entry3, e4), Entry1, Right));

                        //                                     [4]
                        //        [1, 9]                [1]              [9]
                        // [0]  [2,3,5,6,7]  [10] => [0]  [2,3]   [5,6,7]    [10]
                        // and adding 4
                        var e2 = lf.Entry2;
                        if (key > e1.Key && key < e2.Key)
                            return new Branch2(new Branch2(Left, Entry0, new Leaf2(e0, e1)),
                                entry, new Branch2(new Leaf3(e2, lf.Entry3, e4), Entry1, Right));

                        //                                       [5]
                        //        [1, 9]                [1]              [9]
                        // [0]  [2,3,4,6,7]  [10] => [0]  [2,3,4]   [6,7]    [10]
                        // and adding 5
                        var e3 = lf.Entry3;
                        if (key > e2.Key && key < e3.Key)
                            return new Branch2(new Branch2(Left, Entry0, new Leaf3(e0, e1, e2)),
                                entry, new Branch2(new Leaf2(e3, e4), Entry1, Right));

                        //                                       [5]
                        //        [1, 9]                [1]              [9]
                        // [0]  [2,3,4,5,7]  [10] => [0]  [2,3,4]   [6,7]    [10]
                        // and adding 6
                        if (key > e3.Key && key < e4.Key)
                            return new Branch2(new Branch2(Left, Entry0, new Leaf3(e0, e1, e2)),
                                e3, new Branch2(new Leaf2(entry, e4), Entry1, Right));

                        return new Branch3(Left, Entry0,
                            key == e0.Key ? new Leaf5(entry, e1, e2, e3, e4) : 
                            key == e1.Key ? new Leaf5(e0, entry, e2, e3, e4) : 
                            key == e2.Key ? new Leaf5(e0, e1, entry, e3, e4) : 
                            key == e3.Key ? new Leaf5(e0, e1, e2, entry, e4) : 
                                            new Leaf5(e0, e1, e2, e3, entry), 
                            Entry1, Right);
                    }

                    if (Middle is Branch3 br3)
                    {
                        var newLeft = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                        if (popRight != null)
                            return new Branch2(new Branch2(Left, Entry0, newLeft), entry, new Branch2(popRight, Entry1, Right));
                        return new Branch3(Left, Entry0, newLeft, Entry1, Right);
                    }

                    return new Branch3(Left, Entry0, Middle.AddOrUpdateEntry(key, entry), Entry1, Right);
                }

                // update
                return key == Entry0.Key
                    ? new Branch3(Left, entry, Middle, Entry1, Right)
                    : new Branch3(Left, Entry0, Middle, entry, Right);
            }

            // todo: @perf move it to a static method in ImMap if it speeds-up things?
            /// <summary>Produces the new or updated map</summary>
            public ImMap<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap<V> popRight)
            {
                popRight = null;
                ImMap<V> newBranch;
                if (key > Entry1.Key)
                {
                    if (Right is Leaf5 lf)
                    {
                        newBranch = lf.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            popRight = new Branch2(newBranch, entry, popRightBelow);
                            entry = Entry1;
                            return new Branch2(Left, Entry0, Middle);
                        }
                        ////                                             [5]
                        ////        [2,5]                =>      [2]               [9]
                        //// [0,1]  [3,4]  [6,7,8,9,10]    [0,1]    [3,4]   [6,7,8]   [10,11]
                        //// and adding 11
                        //var e4 = lf.Entry4;
                        //if (key > e4.Key)
                        //{
                        //    popRight = new Branch2(new Leaf3(lf.Entry0, lf.Entry1, lf.Entry2), lf.Entry3, new Leaf2(e4, entry));
                        //    entry = Entry1;
                        //    return new Branch2(Left, Entry0, Middle);
                        //}

                        ////                                             [5]
                        ////        [2,5]                =>      [2]               [8]
                        //// [0,1]  [3,4]  [7,8,9,10,11]    [0,1]    [3,4]   [6,7]     [9,10,11]
                        //// and adding 6
                        //var e0 = lf.Entry0;
                        //if (key < e0.Key)
                        //{
                        //    popRight = new Branch2(new Leaf2(entry, e0), lf.Entry1, new Leaf3(lf.Entry2, lf.Entry3, e4));
                        //    entry = Entry1;
                        //    return new Branch2(Left, Entry0, Middle);
                        //}

                        //var e1 = lf.Entry1;
                        //if (key > e0.Key && key < e1.Key)
                        //{
                        //    popRight = new Branch2(new Leaf2(e0, entry), lf.Entry1, new Leaf3(lf.Entry2, lf.Entry3, e4));
                        //    entry = Entry1;
                        //    return new Branch2(Left, Entry0, Middle);
                        //}

                        //var e2 = lf.Entry2;
                        //if (key > e1.Key && key < e2.Key)
                        //{
                        //    popRight = new Branch2(new Leaf2(e0, e1), entry, new Leaf3(e2, lf.Entry3, e4));
                        //    entry = Entry1;
                        //    return new Branch2(Left, Entry0, Middle);
                        //}

                        //var e3 = lf.Entry3;
                        //if (key > e2.Key && key < e3.Key)
                        //{
                        //    popRight = new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf2(e3, e4));
                        //    entry = Entry1;
                        //    return new Branch2(Left, Entry0, Middle);
                        //}

                        //if (key > e3.Key && key < e4.Key)
                        //{
                        //    popRight = new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf2(entry, e4));
                        //    entry = Entry1;
                        //    return new Branch2(Left, Entry0, Middle);
                        //}

                        //newBranch =
                        //    key == e0.Key ? new Leaf5(entry, e1, e2, e3, e4) :
                        //    key == e1.Key ? new Leaf5(e0, entry, e2, e3, e4) :
                        //    key == e2.Key ? new Leaf5(e0, e1, entry, e3, e4) :
                        //    key == e3.Key ? new Leaf5(e0, e1, e2, entry, e4) :
                        //    new Leaf5(e0, e1, e2, e3, entry);
                    }
                    else if (Right is Branch3 br3)
                    {
                        newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            popRight = new Branch2(newBranch, entry, popRightBelow);
                            entry = Entry1;
                            return new Branch2(Left, Entry0, Middle);
                        }
                    }
                    else
                        newBranch = Right.AddOrUpdateEntry(key, entry);

                    return new Branch3(Left, Entry0, Middle, Entry1, newBranch);
                }

                if (key < Entry0.Key)
                {
                    //                                  [4]
                    //         [4,6]      =>       [2]        [6]
                    // [1,2,3]   [5]  [7]      [0,1]  [3]   [5]   [7]
                    // and adding 0

                    if (Left is Leaf5 lf5)
                    {
                        newBranch = lf5.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            newBranch = new Branch2(newBranch, entry, popRightBelow);
                            entry = Entry0;
                            popRight = new Branch2(Middle, Entry1, Right);
                            return newBranch;
                        }
                    }
                    else if (Left is Branch3 br3)
                    {
                        newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            newBranch = new Branch2(newBranch, entry, popRightBelow);
                            entry = Entry0;
                            popRight = new Branch2(Middle, Entry1, Right);
                            return newBranch;
                        }
                    }
                    else
                        newBranch = Left.AddOrUpdateEntry(key, entry);
                    return new Branch3(newBranch, Entry0, Middle, Entry1, Right);
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    //                              [4]
                    //       [2, 7]            [2]         [7]
                    // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                    // and adding 6
                    if (Middle is Leaf5 lf5)
                    {
                        newBranch = lf5.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            popRight = new Branch2(popRightBelow, Entry1, Right);
                            return new Branch2(Left, Entry0, newBranch);
                        }
                    }
                    else if (Middle is Branch3 br3)
                    {
                        newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            popRight = new Branch2(popRightBelow, Entry1, Right);
                            return new Branch2(Left, Entry0, newBranch);
                        }
                    }
                    else
                        newBranch = Middle.AddOrUpdateEntry(key, entry);

                    return new Branch3(Left, Entry0, newBranch, Entry1, Right);
                }

                // update
                return key == Entry0.Key
                    ? new Branch3(Left, entry, Middle, Entry1, Right)
                    : new Branch3(Left, Entry0, Middle, entry, Right);
            }

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) =>
                key > Entry1.Key ? Right.GetValueOrDefault(key) :
                key < Entry0.Key ? Left.GetValueOrDefault(key) :
                key > Entry0.Key && key < Entry1.Key ? Middle.GetValueOrDefault(key) :
                key == Entry0.Key ? Entry0.Value :
                key == Entry1.Key ? Entry1.Value :
                default(V);
        }
    }

    /// <summary>ImMap methods</summary>
    public static class ImMap
    {
        /// <summary>Adds or updates the value by key in the map, always returns a modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value) => 
            map == ImMap<V>.Empty 
                ? new ImMap<V>.Entry(key, value) 
                : map.AddOrUpdateEntry(key, new ImMap<V>.Entry(key, value));
    }
}
