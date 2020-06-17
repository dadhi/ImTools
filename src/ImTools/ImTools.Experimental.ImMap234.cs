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
        public class Leaf2 : ImMap<V>
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
            public override string ToString() => Entry0 + ";" + Entry1;

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
            public override string ToString() => Entry0 + ";" + Entry1 + ";" + Entry2;

            /// <summary>Produces the new or updated map</summary>
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry)
            {
                // [1 3 5]  =>      [3]
                // adding 7     [1]     [5, 7]
                if (key > Entry2.Key)
                    return new Branch2(Entry1, Entry0, new Leaf2(Entry2, entry));

                if (key < Entry0.Key)
                    return new Branch2(Entry1, new Leaf2(entry, Entry0), Entry2);

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Branch2(Entry1, Entry0, new Leaf2(entry, Entry2));

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Branch2(Entry1, new Leaf2(Entry0, entry), Entry2);

                return key == Entry0.Key ? new Leaf3(entry, Entry1, Entry2)
                    :  key == Entry1.Key ? new Leaf3(Entry0, entry, Entry2)
                    :  new Leaf3(Entry0, Entry1, entry);
            }

            /// <summary>Produces the new or updated map</summary>
            public ImMap<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap<V> popRight)
            {
                // [1 3 5]  =>      [3]
                // adding 7     [1]     [5, 7]
                if (key > Entry2.Key)
                {
                    popRight = new Leaf2(Entry2, entry);
                    entry = Entry1;
                    return Entry0;
                }

                if (key < Entry0.Key)
                {
                    popRight = Entry2;
                    var left = new Leaf2(entry, Entry0);
                    entry = Entry1;
                    return left;
                }

                if (key > Entry1.Key && key < Entry2.Key)
                {
                    popRight = new Leaf2(entry, Entry2);
                    entry = Entry1;
                    return Entry0;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    popRight = Entry2;
                    var left = new Leaf2(Entry0, entry);
                    entry = Entry1;
                    return left;
                }

                popRight = null;
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
        /// <summary>2 branches</summary>
        public class Branch2 : ImMap<V>
        {
            /// <summary>The only entry</summary>
            public readonly Entry Entry0;

            /// <summary>Left branch</summary>
            public ImMap<V> Br0; // can be Branches | Entry | Leaf2 | Leaf3, if it is an Entry then other Tree cannot be an Entry

            /// <summary>Right branch</summary>
            public ImMap<V> Br1;

            /// <summary>Constructs</summary>
            public Branch2(Entry entry0, ImMap<V> br0, ImMap<V> br1)
            {
                Entry0 = entry0;
                Br0 = br0;
                Br1 = br1;
            }

            /// Pretty-print
            public override string ToString() =>
                (Br0 is Branch2 ? Br0.GetType().Name : Br0.ToString()) +
                " <- " + Entry0 + " -> " +
                (Br1 is Branch2 ? Br1.GetType().Name : Br1.ToString());

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

                    if (Br1 is Leaf3 lf3)
                    {
                        // [1 3 5]  =>      [3]
                        // adding 7     [1]     [5, 7]
                        if (key > lf3.Entry2.Key)
                            return new Branch3(Entry0, Br0, lf3.Entry0, lf3.Entry1, new Leaf2(lf3.Entry2, entry));

                        if (key < lf3.Entry0.Key)
                            return new Branch3(Entry0, Br0, new Leaf2(entry, lf3.Entry0), lf3.Entry1, lf3.Entry2);

                        if (key > lf3.Entry1.Key && key < lf3.Entry2.Key)
                            return new Branch3(Entry0, Br0, lf3.Entry0, lf3.Entry1, new Leaf2(entry, lf3.Entry2));

                        if (key > lf3.Entry0.Key && key < lf3.Entry1.Key)
                            return new Branch3(Entry0, Br0, new Leaf2(lf3.Entry0, entry), lf3.Entry1, lf3.Entry2);

                        return new Branch2(Entry0, Br0,
                            key == lf3.Entry0.Key ? new Leaf3(entry, lf3.Entry1, lf3.Entry2)
                            : key == lf3.Entry1.Key ? new Leaf3(lf3.Entry0, entry, lf3.Entry2)
                            : new Leaf3(lf3.Entry0, lf3.Entry1, entry));
                    }

                    if (Br1 is Branch3 br3)
                    {
                        var newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                        return popRight != null
                            ? new Branch3(Entry0, Br0, newBranch, entry, popRight)
                            : new Branch2(Entry0, Br0, newBranch);
                    }

                    return new Branch2(Entry0, Br0, Br1.AddOrUpdateEntry(key, entry));
                }

                if (key < Entry0.Key)
                {
                    //           [4]                [2]  <-  [4]
                    // [1, 2, 3]      [5] =>  [0, 1]    [3]      [5]  =>
                    // and adding 0, so we are merging the branches
                    if (Br0 is Leaf3 lf3)
                    {
                        // [1 3 5]  =>      [3]
                        // adding 7     [1]     [5, 7]
                        if (key > lf3.Entry2.Key)
                            return new Branch3(lf3.Entry1, lf3.Entry0, new Leaf2(lf3.Entry2, entry), Entry0, Br1);

                        if (key < lf3.Entry0.Key)
                            return new Branch3(lf3.Entry1, new Leaf2(entry, lf3.Entry0), lf3.Entry2, Entry0, Br1);

                        if (key > lf3.Entry1.Key && key < lf3.Entry2.Key)
                            return new Branch3(lf3.Entry1, lf3.Entry0, new Leaf2(entry, lf3.Entry2), Entry0, Br1);

                        if (key > lf3.Entry0.Key && key < lf3.Entry1.Key)
                            return new Branch3(lf3.Entry1, new Leaf2(lf3.Entry0, entry), lf3.Entry2, Entry0, Br1);

                        return new Branch2(Entry0,
                            key == lf3.Entry0.Key ? new Leaf3(entry, lf3.Entry1, lf3.Entry2)
                            : key == lf3.Entry1.Key ? new Leaf3(lf3.Entry0, entry, lf3.Entry2)
                            : new Leaf3(lf3.Entry0, lf3.Entry1, entry),
                            Br1);
                    }

                    if (Br0 is Branch3 br3)
                    {
                        var newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                        return popRight != null
                            ? new Branch3(entry, newBranch, popRight, Entry0, Br1)
                            : new Branch2(Entry0, newBranch, Br1);
                    }

                    return new Branch2(Entry0, Br0.AddOrUpdateEntry(key, entry), Br1);
                }

                // update
                return new Branch2(entry, Br0, Br1);
            }

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) =>
                key > Entry0.Key ? Br1.GetValueOrDefault(key) :
                key < Entry0.Key ? Br0.GetValueOrDefault(key) :
                key == Entry0.Key ? Entry0.Value : default(V);
        }

        /// <summary>3 branches</summary>
        public sealed class Branch3 : Branch2
        {
            /// <summary>Right entry</summary>
            public readonly Entry Entry1;

            /// <summary>Rightmost branch</summary>
            public readonly ImMap<V> Br2;

            /// <summary>Constructs</summary>
            public Branch3(Entry entry0, ImMap<V> br0, ImMap<V> br1, Entry entry1, ImMap<V> br2) 
                : base(entry0, br0, br1)
            {
                Entry1  = entry1;
                Br2 = br2;
            }

            /// Pretty-print
            public override string ToString() =>
                (Br0 is Branch2 ? Br0.GetType().Name : Br0.ToString()) +
                " <- " + Entry0 + " -> " +
                (Br1 is Branch2 ? Br1.GetType().Name : Br1.ToString()) +
                " <- " + Entry0 + " -> " +
                (Br2 is Branch2 ? Br2.GetType().Name.TrimEnd('<', '>', '`', 'V') : Br2.ToString());

            /// <summary>Produces the new or updated map</summary>
            public override ImMap<V> AddOrUpdateEntry(int key, Entry entry)
            {
                if (key > Entry1.Key)
                {
                    //                                                   =>          [4]
                    //     [2, 4]                   [2, 4]  ->  [6]             [2]         [6]
                    // [1]   [3]  [5, 6, 7] =>  [1]   [3]    [5]   [7, 8]    [1]   [3]   [5]   [7,8]
                    // and adding 8

                    var newBranch = Br2.AddOrUpdateEntry(key, entry);
                    if (Br2 is Leaf3 ||
                        Br2 is Branch3 && newBranch is Branch3 == false)
                        return new Branch2(Entry1,
                            new Branch2(Entry0, Br0, Br1),
                            newBranch);

                    return new Branch3(Entry0, Br0, Br1, Entry1, newBranch);
                }

                if (key < Entry0.Key)
                {
                    //                                  [4]
                    //         [4,6]      =>       [2]        [6]
                    // [1,2,3]   [5]  [7]      [0,1]  [3]   [5]   [7]
                    // and adding 0

                    var newBranch = Br0.AddOrUpdateEntry(key, entry);

                    if (Br0 is Leaf3 ||
                        Br0 is Branch3 && newBranch is Branch3 == false)
                        return new Branch2(Entry0,
                            newBranch,
                            new Branch2(Entry1, Br1, Br2));

                    return new Branch3(Entry0, newBranch, Br1, Entry1, Br2);
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    //                              [4]
                    //       [2, 7]            [2]         [7]
                    // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                    // and adding 6

                    var newBranch = Br1.AddOrUpdateEntry(key, entry);
                    if (Br1 is Leaf3 ||
                        Br1 is Branch3 && newBranch is Branch3 == false)
                    {
                        var br2 = (Branch2)newBranch;
                        br2.Br0 = new Branch2(Entry0, Br0, br2.Br0);
                        br2.Br1 = new Branch2(Entry1, br2.Br1, Br2);
                    }

                    return new Branch3(Entry0, Br0, newBranch, Entry1, Br2);
                }

                // update
                return key == Entry0.Key
                    ? new Branch3(entry, Br0, Br1, Entry1, Br2)
                    : new Branch3(Entry0, Br0, Br1, entry, Br2);
            }

            // todo: @perf move it to a static method in ImMap if it speeds-up things
            /// <summary>Produces the new or updated map</summary>
            public ImMap<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap<V> popRight)
            {
                popRight = null;
                ImMap<V> newBranch;
                if (key > Entry1.Key)
                {
                    //                                                   =>          [4]
                    //     [2, 4]                   [2, 4]  ->  [6]             [2]         [6]
                    // [1]   [3]  [5, 6, 7] =>  [1]   [3]    [5]   [7, 8]    [1]   [3]   [5]   [7,8]
                    // and adding 8
                    if (Br2 is Leaf3 lf3)
                    {
                        newBranch = lf3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            popRight = new Branch2(entry, newBranch, popRightBelow);
                            entry = Entry1;
                            return new Branch2(Entry0, Br0, Br1);
                        }
                    }
                    else if (Br2 is Branch3 br3)
                    {
                        newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            popRight = new Branch2(entry, newBranch, popRightBelow);
                            entry = Entry1;
                            return new Branch2(Entry0, Br0, Br1);
                        }
                    }
                    else
                        newBranch = Br2.AddOrUpdateEntry(key, entry);

                    return new Branch3(Entry0, Br0, Br1, Entry1, newBranch);
                }

                if (key < Entry0.Key)
                {
                    //                                  [4]
                    //         [4,6]      =>       [2]        [6]
                    // [1,2,3]   [5]  [7]      [0,1]  [3]   [5]   [7]
                    // and adding 0
                    if (Br0 is Leaf3 lf3)
                    {
                        newBranch = lf3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            newBranch = new Branch2(entry, newBranch, popRightBelow);
                            entry = Entry0;
                            popRight = new Branch2(Entry1, Br1, Br2);
                            return newBranch;
                        }
                    }
                    else if (Br0 is Branch3 br3)
                    {
                        newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            newBranch = new Branch2(entry, newBranch, popRightBelow);
                            entry = Entry0;
                            popRight = new Branch2(Entry1, Br1, Br2);
                            return newBranch;
                        }
                    }
                    else
                        newBranch = Br0.AddOrUpdateEntry(key, entry);
                    return new Branch3(Entry0, newBranch, Br1, Entry1, Br2);
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    //                              [4]
                    //       [2, 7]            [2]         [7]
                    // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                    // and adding 6
                    if (Br1 is Leaf3 lf3)
                    {
                        newBranch = lf3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            popRight = new Branch2(Entry1, popRightBelow, Br2);
                            return new Branch2(Entry0, Br0, newBranch);
                        }
                    }
                    else if (Br1 is Branch3 br3)
                    {
                        newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                        if (popRightBelow != null)
                        {
                            popRight = new Branch2(Entry1, popRightBelow, Br2);
                            return new Branch2(Entry0, Br0, newBranch);
                        }
                    }
                    else
                        newBranch = Br1.AddOrUpdateEntry(key, entry);

                    return new Branch3(Entry0, Br0, newBranch, Entry1, Br2);
                }

                // update
                return key == Entry0.Key
                    ? new Branch3(entry, Br0, Br1, Entry1, Br2)
                    : new Branch3(Entry0, Br0, Br1, entry, Br2);
            }

            /// <summary>Lookup</summary>
            public override V GetValueOrDefault(int key) =>
                key > Entry1.Key ? Br2.GetValueOrDefault(key) :
                key < Entry0.Key ? Br0.GetValueOrDefault(key) :
                key > Entry0.Key && key < Entry1.Key ? Br1.GetValueOrDefault(key) :
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
