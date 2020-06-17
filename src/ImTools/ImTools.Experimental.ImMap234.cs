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
        public virtual ImMap<V> AddOrUpdateEntry(int key, ImMapEntry<V> entry) => entry;

        /// <summary>Lookup</summary>
        public virtual V GetValueOrDefault(int key) => default(V);
    }

    /// <summary>Wraps the stored data with "fixed" reference semantics - 
    /// when added to the tree it won't be changed or reconstructed in memory</summary>
    public sealed class ImMapEntry<V> : ImMap<V>
    {
        /// <summary>The Key is basically the hash, or the Height for ImMapTree</summary>
        public readonly int Key;

        /// <summary>The value - may be modified if you need a Ref{V} semantics</summary>
        public V Value;

        /// <summary>Constructs the entry with the default value</summary>
        public ImMapEntry(int key) => Key = key;

        /// <summary>Constructs the entry with the key and value</summary>
        public ImMapEntry(int key, V value)
        {
            Key = key;
            Value = value;
        }

        /// Pretty-prints
        public override string ToString() => Key + ":" + Value;

        /// <summary>Produces the new or updated map</summary>
        public override ImMap<V> AddOrUpdateEntry(int key, ImMapEntry<V> entry) =>
            key > Key ? new ImMapLeaf2<V>(this, entry) :
            key < Key ? new ImMapLeaf2<V>(entry, this) : 
            (ImMap<V>)entry;

        /// <summary>Lookup</summary>
        public override V GetValueOrDefault(int key) => key == Key ? Value : default(V);
    }

    /// <summary>2 leafs</summary>
    public class ImMapLeaf2<V> : ImMap<V>
    {
        /// <summary>Left entry</summary>
        public readonly ImMapEntry<V> Entry0;

        /// <summary>Right entry</summary>
        public readonly ImMapEntry<V> Entry1;

        /// <summary>Constructs 2 leafs</summary>
        public ImMapLeaf2(ImMapEntry<V> entry0, ImMapEntry<V> entry1)
        {
            Entry0 = entry0;
            Entry1 = entry1;
        }

        /// Pretty-print
        public override string ToString() => Entry0 + ";" + Entry1;

        /// <summary>Produces the new or updated map</summary>
        public override ImMap<V> AddOrUpdateEntry(int key, ImMapEntry<V> entry) =>
            key > Entry1.Key ? new ImMapLeaf3<V>(Entry0, Entry1, entry) :
            key < Entry0.Key ? new ImMapLeaf3<V>(entry, Entry0, Entry1) :
            key > Entry0.Key && entry.Key < Entry1.Key ? new ImMapLeaf3<V>(Entry0, entry, Entry1) :
            key == Entry0.Key ? new ImMapLeaf2<V>(entry, Entry1) : 
            new ImMapLeaf2<V>(Entry0, entry);

        /// <summary>Lookup</summary>
        public override V GetValueOrDefault(int key) => 
            key == Entry0.Key ? Entry0.Value : 
            key == Entry1.Key ? Entry1.Value :
            default(V);
    }

    /// <summary>3 leafs</summary>
    public sealed class ImMapLeaf3<V> : ImMapLeaf2<V>
    {
        /// <summary>Rightmost leaf</summary>
        public readonly ImMapEntry<V> Entry2;

        /// <summary>Constructs a tree leaf</summary>
        public ImMapLeaf3(ImMapEntry<V> entry0, ImMapEntry<V> entry1, ImMapEntry<V> entry2)
            : base(entry0, entry1) =>
            Entry2 = entry2;

        /// Pretty-print
        public override string ToString() => Entry0 + ";" + Entry1 + ";" + Entry2;

        /// <summary>Produces the new or updated map</summary>
        public override ImMap<V> AddOrUpdateEntry(int key, ImMapEntry<V> entry)
        {
            // [1 3 5]  =>      [3]
            // adding 7     [1]     [5, 7]
            if (key > Entry2.Key)
                return new ImMapBranch2<V>(Entry1, Entry0, new ImMapLeaf2<V>(Entry2, entry));

            if (key < Entry0.Key)
                return new ImMapBranch2<V>(Entry1, new ImMapLeaf2<V>(entry, Entry0), Entry2);

            if (key > Entry1.Key && key < Entry2.Key)
                return new ImMapBranch2<V>(Entry1, Entry0, new ImMapLeaf2<V>(entry, Entry2));

            if (key > Entry0.Key && key < Entry1.Key)
                return new ImMapBranch2<V>(Entry1, new ImMapLeaf2<V>(Entry0, entry), Entry2);

            return key == Entry0.Key ? new ImMapLeaf3<V>(entry, Entry1, Entry2)
                : key == Entry1.Key ? new ImMapLeaf3<V>(Entry0, entry, Entry2)
                : new ImMapLeaf3<V>(Entry0, Entry1, entry);
        }

        /// <summary>Produces the new or updated map</summary>
        public ImMap<V> AddOrUpdateOrSplitEntry(int key, ref ImMapEntry<V> entry, out ImMap<V> popRight)
        {
            // [1 3 5]  =>      [3]
            // adding 7     [1]     [5, 7]
            if (key > Entry2.Key)
            {
                popRight = new ImMapLeaf2<V>(Entry2, entry);
                entry = Entry1;
                return Entry0;
            }

            if (key < Entry0.Key)
            {
                popRight = Entry2;
                var left = new ImMapLeaf2<V>(entry, Entry0);
                entry = Entry1;
                return left;
            }

            if (key > Entry1.Key && key < Entry2.Key)
            {
                popRight = new ImMapLeaf2<V>(entry, Entry2);
                entry = Entry1;
                return Entry0;
            }

            if (key > Entry0.Key && key < Entry1.Key)
            {
                popRight = Entry2;
                var left = new ImMapLeaf2<V>(Entry0, entry);
                entry = Entry1;
                return left;
            }

            popRight = null;
            return key == Entry0.Key ? new ImMapLeaf3<V>(entry, Entry1, Entry2)
                : key == Entry1.Key ? new ImMapLeaf3<V>(Entry0, entry, Entry2)
                : new ImMapLeaf3<V>(Entry0, Entry1, entry);
        }

        /// <summary>Lookup</summary>
        public override V GetValueOrDefault(int key) =>
            key == Entry0.Key ? Entry0.Value :
            key == Entry1.Key ? Entry1.Value :
            key == Entry2.Key ? Entry2.Value :
            default(V);
    }

    /// <summary>2 branches</summary>
    public class ImMapBranch2<V> : ImMap<V>
    {
        /// <summary>The only entry</summary>
        public readonly ImMapEntry<V> Entry0;

        /// <summary>Left branch</summary>
        public ImMap<V> Branch0; // can be Branches | Entry | Leaf2 | Leaf3, if it is an Entry then other Tree cannot be an Entry

        /// <summary>Right branch</summary>
        public ImMap<V> Branch1;

        /// <summary>Constructs</summary>
        public ImMapBranch2(ImMapEntry<V> entry0, ImMap<V> branch0, ImMap<V> branch1)
        {
            Entry0 = entry0;
            Branch0 = branch0;
            Branch1 = branch1;
        }

        /// Pretty-print
        public override string ToString() =>
            (Branch0 is ImMapBranch2<V> ? Branch0.GetType().Name : Branch0.ToString()) + 
            " <- " + Entry0 + " -> " +
            (Branch1 is ImMapBranch2<V> ? Branch1.GetType().Name : Branch1.ToString());

        /// <summary>Produces the new or updated map</summary>
        public override ImMap<V> AddOrUpdateEntry(int key, ImMapEntry<V> entry)
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

                if (Branch1 is ImMapLeaf3<V> lf3)
                {
                    // [1 3 5]  =>      [3]
                    // adding 7     [1]     [5, 7]
                    if (key > lf3.Entry2.Key)
                        return new ImMapBranch3<V>(Entry0, Branch0, lf3.Entry0, lf3.Entry1, new ImMapLeaf2<V>(lf3.Entry2, entry));

                    if (key < lf3.Entry0.Key)
                        return new ImMapBranch3<V>(Entry0, Branch0, new ImMapLeaf2<V>(entry, lf3.Entry0), lf3.Entry1, lf3.Entry2);

                    if (key > lf3.Entry1.Key && key < lf3.Entry2.Key)
                        return new ImMapBranch3<V>(Entry0, Branch0, lf3.Entry0, lf3.Entry1, new ImMapLeaf2<V>(entry, lf3.Entry2));

                    if (key > lf3.Entry0.Key && key < lf3.Entry1.Key)
                        return new ImMapBranch3<V>(Entry0, Branch0, new ImMapLeaf2<V>(lf3.Entry0, entry), lf3.Entry1, lf3.Entry2);

                    return new ImMapBranch2<V>(Entry0, Branch0, 
                        key == lf3.Entry0.Key ? new ImMapLeaf3<V>(entry, lf3.Entry1, lf3.Entry2)
                        : key == lf3.Entry1.Key ? new ImMapLeaf3<V>(lf3.Entry0, entry, lf3.Entry2)
                        : new ImMapLeaf3<V>(lf3.Entry0, lf3.Entry1, entry));
                }

                if (Branch1 is ImMapBranch3<V> br3)
                {
                    var newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    return popRight != null 
                        ? new ImMapBranch3<V>(Entry0, Branch0, newBranch, entry, popRight) 
                        : new ImMapBranch2<V>(Entry0, Branch0, newBranch);
                }

                return new ImMapBranch2<V>(Entry0, Branch0, Branch1.AddOrUpdateEntry(key, entry));
            }

            if (key < Entry0.Key)
            {
                //           [4]                [2]  <-  [4]
                // [1, 2, 3]      [5] =>  [0, 1]    [3]      [5]  =>
                // and adding 0, so we are merging the branches
                if (Branch0 is ImMapLeaf3<V> lf3)
                {
                    // [1 3 5]  =>      [3]
                    // adding 7     [1]     [5, 7]
                    if (key > lf3.Entry2.Key)
                        return new ImMapBranch3<V>(lf3.Entry1, lf3.Entry0, new ImMapLeaf2<V>(lf3.Entry2, entry), Entry0, Branch1);

                    if (key < lf3.Entry0.Key)
                        return new ImMapBranch3<V>(lf3.Entry1, new ImMapLeaf2<V>(entry, lf3.Entry0), lf3.Entry2, Entry0, Branch1);

                    if (key > lf3.Entry1.Key && key < lf3.Entry2.Key)
                        return new ImMapBranch3<V>(lf3.Entry1, lf3.Entry0, new ImMapLeaf2<V>(entry, lf3.Entry2), Entry0, Branch1);

                    if (key > lf3.Entry0.Key && key < lf3.Entry1.Key)
                        return new ImMapBranch3<V>(lf3.Entry1, new ImMapLeaf2<V>(lf3.Entry0, entry), lf3.Entry2, Entry0, Branch1);

                    return new ImMapBranch2<V>(Entry0,
                        key == lf3.Entry0.Key ? new ImMapLeaf3<V>(entry, lf3.Entry1, lf3.Entry2)
                        : key == lf3.Entry1.Key ? new ImMapLeaf3<V>(lf3.Entry0, entry, lf3.Entry2)
                        : new ImMapLeaf3<V>(lf3.Entry0, lf3.Entry1, entry),
                        Branch1);
                }

                if (Branch0 is ImMapBranch3<V> br3)
                {
                    var newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    return popRight != null 
                        ? new ImMapBranch3<V>(entry, newBranch, popRight, Entry0, Branch1) 
                        : new ImMapBranch2<V>(Entry0, newBranch, Branch1);
                }

                return new ImMapBranch2<V>(Entry0, Branch0.AddOrUpdateEntry(key, entry), Branch1);
            }

            // update
            return new ImMapBranch2<V>(entry, Branch0, Branch1);
        }

        /// <summary>Lookup</summary>
        public override V GetValueOrDefault(int key) =>
            key > Entry0.Key ? Branch1.GetValueOrDefault(key) :
            key < Entry0.Key ? Branch0.GetValueOrDefault(key) :
            key == Entry0.Key ? Entry0.Value : default(V);
    }

    /// <summary>3 branches</summary>
    public sealed class ImMapBranch3<V> : ImMapBranch2<V>
    {
        /// <summary>Right entry</summary>
        public readonly ImMapEntry<V> Entry1;

        /// <summary>Rightmost branch</summary>
        public readonly ImMap<V> Branch2;

        /// <summary>Constructs</summary>
        public ImMapBranch3(ImMapEntry<V> entry0, ImMap<V> branch0, ImMap<V> branch1,
            ImMapEntry<V> entry1, ImMap<V> branch2) : base(entry0, branch0, branch1)
        {
            Entry1 = entry1;
            Branch2 = branch2;
        }

        /// Pretty-print
        public override string ToString() =>
            (Branch0 is ImMapBranch2<V> ? Branch0.GetType().Name : Branch0.ToString()) +
            " <- " + Entry0 + " -> " +
            (Branch1 is ImMapBranch2<V> ? Branch1.GetType().Name : Branch1.ToString()) +
            " <- " + Entry0 + " -> " +
            (Branch2 is ImMapBranch2<V> ? Branch2.GetType().Name.TrimEnd('<', '>', '`', 'V') : Branch2.ToString());

        /// <summary>Produces the new or updated map</summary>
        public override ImMap<V> AddOrUpdateEntry(int key, ImMapEntry<V> entry)
        {
            if (key > Entry1.Key)
            {
                //                                                   =>          [4]
                //     [2, 4]                   [2, 4]  ->  [6]             [2]         [6]
                // [1]   [3]  [5, 6, 7] =>  [1]   [3]    [5]   [7, 8]    [1]   [3]   [5]   [7,8]
                // and adding 8

                var newBranch = Branch2.AddOrUpdateEntry(key, entry);
                if (Branch2 is ImMapLeaf3<V> ||
                    Branch2 is ImMapBranch3<V> && newBranch is ImMapBranch3<V> == false)
                    return new ImMapBranch2<V>(Entry1,
                        new ImMapBranch2<V>(Entry0, Branch0, Branch1),
                        newBranch);

                return new ImMapBranch3<V>(Entry0, Branch0, Branch1, Entry1, newBranch);
            }

            if (key < Entry0.Key)
            {
                //                                  [4]
                //         [4,6]      =>       [2]        [6]
                // [1,2,3]   [5]  [7]      [0,1]  [3]   [5]   [7]
                // and adding 0

                var newBranch = Branch0.AddOrUpdateEntry(key, entry);

                if (Branch0 is ImMapLeaf3<V> ||
                    Branch0 is ImMapBranch3<V> && newBranch is ImMapBranch3<V> == false)
                    return new ImMapBranch2<V>(Entry0,
                        newBranch,
                        new ImMapBranch2<V>(Entry1, Branch1, Branch2));

                return new ImMapBranch3<V>(Entry0, newBranch, Branch1, Entry1, Branch2);
            }

            if (key > Entry0.Key && key < Entry1.Key)
            {
                //                              [4]
                //       [2, 7]            [2]         [7]
                // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                // and adding 6

                var newBranch = Branch1.AddOrUpdateEntry(key, entry);
                if (Branch1 is ImMapLeaf3<V> ||
                    Branch1 is ImMapBranch3<V> && newBranch is ImMapBranch3<V> == false)
                {
                    var br2 = (ImMapBranch2<V>)newBranch;
                    br2.Branch0 = new ImMapBranch2<V>(Entry0, Branch0, br2.Branch0);
                    br2.Branch1 = new ImMapBranch2<V>(Entry1, br2.Branch1, Branch2);
                }

                return new ImMapBranch3<V>(Entry0, Branch0, newBranch, Entry1, Branch2);
            }

            // update
            return key == Entry0.Key
                ? new ImMapBranch3<V>(entry, Branch0, Branch1, Entry1, Branch2)
                : new ImMapBranch3<V>(Entry0, Branch0, Branch1, entry, Branch2);
        }

        // todo: @perf move it to a static method in ImMap if it speeds-up things
        /// <summary>Produces the new or updated map</summary>
        public ImMap<V> AddOrUpdateOrSplitEntry(int key, ref ImMapEntry<V> entry, out ImMap<V> popRight)
        {
            popRight = null;
            ImMap<V> newBranch;
            if (key > Entry1.Key)
            {
                //                                                   =>          [4]
                //     [2, 4]                   [2, 4]  ->  [6]             [2]         [6]
                // [1]   [3]  [5, 6, 7] =>  [1]   [3]    [5]   [7, 8]    [1]   [3]   [5]   [7,8]
                // and adding 8
                if (Branch2 is ImMapLeaf3<V> lf3)
                {
                    newBranch = lf3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new ImMapBranch2<V>(entry, newBranch, popRightBelow);
                        entry = Entry1;
                        return new ImMapBranch2<V>(Entry0, Branch0, Branch1);
                    }
                }
                else if (Branch2 is ImMapBranch3<V> br3)
                {
                    newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new ImMapBranch2<V>(entry, newBranch, popRightBelow);
                        entry = Entry1;
                        return new ImMapBranch2<V>(Entry0, Branch0, Branch1);
                    }
                }
                else
                    newBranch = Branch2.AddOrUpdateEntry(key, entry);

                return new ImMapBranch3<V>(Entry0, Branch0, Branch1, Entry1, newBranch);
            }

            if (key < Entry0.Key)
            {
                //                                  [4]
                //         [4,6]      =>       [2]        [6]
                // [1,2,3]   [5]  [7]      [0,1]  [3]   [5]   [7]
                // and adding 0
                if (Branch0 is ImMapLeaf3<V> lf3)
                {
                    newBranch = lf3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        newBranch = new ImMapBranch2<V>(entry, newBranch, popRightBelow);
                        entry = Entry0;
                        popRight = new ImMapBranch2<V>(Entry1, Branch1, Branch2);
                        return newBranch;
                    }
                }
                else if (Branch0 is ImMapBranch3<V> br3)
                {
                    newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        newBranch = new ImMapBranch2<V>(entry, newBranch, popRightBelow);
                        entry = Entry0;
                        popRight = new ImMapBranch2<V>(Entry1, Branch1, Branch2);
                        return newBranch;
                    }
                }
                else
                    newBranch = Branch0.AddOrUpdateEntry(key, entry);
                return new ImMapBranch3<V>(Entry0, newBranch, Branch1, Entry1, Branch2);
            }

            if (key > Entry0.Key && key < Entry1.Key)
            {
                //                              [4]
                //       [2, 7]            [2]         [7]
                // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                // and adding 6
                if (Branch1 is ImMapLeaf3<V> lf3)
                {
                    newBranch = lf3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new ImMapBranch2<V>(Entry1, popRightBelow, Branch2);
                        return new ImMapBranch2<V>(Entry0, Branch0, newBranch);
                    }
                }
                else if (Branch1 is ImMapBranch3<V> br3)
                {
                    newBranch = br3.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new ImMapBranch2<V>(Entry1, popRightBelow, Branch2);
                        return new ImMapBranch2<V>(Entry0, Branch0, newBranch);
                    }
                }
                else
                    newBranch = Branch1.AddOrUpdateEntry(key, entry);

                return new ImMapBranch3<V>(Entry0, Branch0, newBranch, Entry1, Branch2);
            }

            // update
            return key == Entry0.Key
                ? new ImMapBranch3<V>(entry, Branch0, Branch1, Entry1, Branch2)
                : new ImMapBranch3<V>(Entry0, Branch0, Branch1, entry, Branch2);
        }

        /// <summary>Lookup</summary>
        public override V GetValueOrDefault(int key) =>
            key > Entry1.Key ? Branch2.GetValueOrDefault(key) :
            key < Entry0.Key ? Branch0.GetValueOrDefault(key) :
            key > Entry0.Key && key < Entry1.Key ? Branch1.GetValueOrDefault(key) :
            key == Entry0.Key ? Entry0.Value : 
            key == Entry1.Key ? Entry1.Value : 
            default(V);
    }

    /// <summary>ImMap methods</summary>
    public static class ImMap
    {
        /// <summary>Lookup</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap<V> map, int key) 
        {
            while (map is ImMapBranch2<V> br2) 
            {
                // The order of comparison is following because,
                // if we have target in key in the branch then we will find it faster,
                // if the target is in leaf then the order is does not matter (it is the same number of operation if reordered)
                // Besides if the target in the first branch entry, there is no need to pay for the cast to Branch3.
                var entry = br2.Entry0;
                if (key == entry.Key)
                    return entry.Value;

                if (key < entry.Key)
                    map = br2.Branch0;
                else
                {
                    if (br2 is ImMapBranch3<V> br3) 
                    {
                        entry = br3.Entry1;
                        if (entry.Key == key)
                            return entry.Value;
                        map = key < entry.Key ? br3.Branch1 : br3.Branch2;
                    }
                    else
                        map = br2.Branch1;
                }
            }

            return map.GetValueOrDefault(key);

            //entry = map as ImMapEntry<V>;
            //if (entry != null)
            //    return key == entry.Key ? entry.Value : default(V);

            //if (map is ImMapLeaf2<V> leaf2) 
            //{
            //    if (key == leaf2.Entry0.Key)
            //        return leaf2.Entry0.Value;
            //    if (key == leaf2.Entry1.Key)
            //        return leaf2.Entry1.Value;
            //    if (leaf2 is ImMapLeaf3<V> leaf3 && leaf3.Entry2.Key == key)
            //        return leaf3.Entry2.Value;
            //}

            //return default(V);
        }

        /// <summary>Adds or updates the value by key in the map, always returns a modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value)
        {
            if (map == ImMap<V>.Empty)
                return new ImMapEntry<V>(key, value);
            return map.AddOrUpdateEntry(key, new ImMapEntry<V>(key, value));
        }
    }
}