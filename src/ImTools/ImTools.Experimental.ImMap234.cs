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

        /// Prints the key value pair
        public override string ToString() => Key + ":" + Value;
    }

    /// <summary>2 leafs</summary>
    public class ImMapLeafs2<V> : ImMap<V>
    {
        /// <summary>Left entry</summary>
        public readonly ImMapEntry<V> Entry0;

        /// <summary>Right entry</summary>
        public readonly ImMapEntry<V> Entry1;

        /// <summary>Constructs 2 leafs</summary>
        public ImMapLeafs2(ImMapEntry<V> entry0, ImMapEntry<V> entry1)
        {
            Entry0 = entry0;
            Entry1 = entry1;
        }
    }

    /// <summary>3 leafs</summary>
    public sealed class ImMapLeafs3<V> : ImMapLeafs2<V>
    {
        /// <summary>Rightmost leaf</summary>
        public readonly ImMapEntry<V> Entry2;

        /// <summary>Constructs a tree leaf</summary>
        public ImMapLeafs3(ImMapEntry<V> entry0, ImMapEntry<V> entry1, ImMapEntry<V> entry2)
            : base(entry0, entry1)
        {
            Entry2 = entry2;
        }
    }

    /// <summary>2 branches</summary>
    public class ImMapBranch2<V> : ImMap<V>
    {
        /// <summary>The only entry</summary>
        public readonly ImMapEntry<V> Entry0;

        /// <summary>Left branch</summary>
        public readonly ImMap<V> Branch0; // can be Branchs | Entry | Leaf2 | Leaf3, if it is an Entry then other Tree cannot be an Entry

        /// <summary>Right branch</summary>
        public readonly ImMap<V> Branch1;

        /// <summary>Constructs</summary>
        public ImMapBranch2(ImMapEntry<V> entry0, ImMap<V> branch0, ImMap<V> branch1)
        {
            Entry0 = entry0;
            Branch0 = branch0;
            Branch1 = branch1;
        }
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
    }

    /// <summary>ImMap methods</summary>
    public static class ImMap
    {
        /// <summary>Lookup</summary>
        public static V GetValueOrDefault<V>(this ImMap<V> map, int key) 
        {
            if (map == ImMap<V>.Empty)
                return default(V);

            ImMapEntry<V> entry;
            while (map is ImMapBranch2<V> br2) 
            {
                // The order of comparison is following because,
                // if we have target in key in the branch then we will find it faster,
                // if the target is in leaf then the order is does not matter (it is the same number of operation if reordered)
                // Besides if the target in the first branch entry, there is no need to pay for the cast to Branch3.
                entry = br2.Entry0;
                if (entry.Key == key)
                    return entry.Value;

                if (entry.Key > key)
                    map = br2.Branch0;
                else
                {
                    if (br2 is ImMapBranch3<V> br3) 
                    {
                        entry = br3.Entry1;
                        if (entry.Key == key)
                            return entry.Value;
                        
                        if (entry.Key > key)
                            map = br3.Branch1;
                        else
                            map = br3.Branch2;
                    }
                    else
                        map = br2.Branch1;
                }
            }

            entry = map as ImMapEntry<V>;
            if (entry != null)
                return key == entry.Key ? entry.Value : default(V);

            if (map is ImMapLeafs2<V> leaf2) 
            {
                if (key == leaf2.Entry0.Key)
                    return leaf2.Entry0.Value;
                if (key == leaf2.Entry1.Key)
                    return leaf2.Entry1.Value;
                if (leaf2 is ImMapLeafs3<V> leaf3 && leaf3.Entry2.Key == key)
                    return leaf3.Entry2.Value;
            }

            return default(V);
        }

        /// <summary> Adds or updates the value by key in the map, always returns a modified map </summary>
        public static ImMap<V> AddOrUpdateEntry<V>(this ImMap<V> map, ImMapEntry<V> entry)
        {
            if (map == ImMap<V>.Empty)
                return entry;

            var key = entry.Key;
            if (map is ImMapEntry<V> leaf)
                return leaf.AddToEntry(entry, key);

            if (map is ImMapLeafs2<V> leaf2)
            {
                // we May need to split
                if (leaf2 is ImMapLeafs3<V> leaf3)
                {
                    // [1 3 5]  =>      [3]
                    // adding 7     [1]     [5, 7]
                    if (key > leaf3.Entry2.Key)
                        return new ImMapBranch2<V>(leaf3.Entry1,
                            leaf3.Entry0, new ImMapLeafs2<V>(leaf3.Entry2, entry));

                    if (key < leaf3.Entry0.Key)
                        return new ImMapBranch2<V>(leaf3.Entry1,
                            new ImMapLeafs2<V>(entry, leaf3.Entry0), leaf3.Entry2);

                    if (key > leaf3.Entry1.Key && key < leaf3.Entry2.Key)
                        return new ImMapBranch2<V>(leaf3.Entry1,
                            leaf3.Entry0, new ImMapLeafs2<V>(entry, leaf3.Entry2));

                    if (key > leaf3.Entry0.Key && key < leaf3.Entry1.Key)
                        return new ImMapBranch2<V>(leaf3.Entry1,
                            new ImMapLeafs2<V>(leaf3.Entry0, entry), leaf3.Entry2);

                    return key == leaf3.Entry0.Key ? new ImMapLeafs3<V>(entry, leaf3.Entry1, leaf3.Entry2)
                        : key == leaf3.Entry1.Key ? new ImMapLeafs3<V>(leaf3.Entry0, entry, leaf3.Entry2)
                        : new ImMapLeafs3<V>(leaf3.Entry0, leaf3.Entry1, entry);
                }

                return leaf2.AddToLeafs2(entry, key);
            }

            // - Adding to branch2 should not require to split the branch itself!
            // - The only result of split could be other Branch2 - because we are splitting only Leaf3 
            // - We cannot have a result of Branch2 for non-splitting addition
            if (map is ImMapBranch2<V> br2)
            {
                if (br2 is ImMapBranch3<V> br3) 
                {
                    if (key > br3.Entry1.Key)
                    {
                        //                                                   =>          [4]
                        //     [2, 4]                   [2, 4]  ->  [6]             [2]         [6]
                        // [1]   [3]  [5, 6, 7] =>  [1]   [3]    [5]   [7, 8]    [1]   [3]   [5]   [7,8]
                        // and adding 8

                        if (br3.Branch2 is ImMapEntry<V> entryBranch)
                            return new ImMapBranch3<V>(br3.Entry0, br3.Branch0, br3.Branch1,
                                br3.Entry1, entryBranch.AddToEntry(entry, key));

                        if (br3.Branch2 is ImMapLeafs2<V> entryLeafs2 && br3.Branch2 is ImMapLeafs3<V> == false)
                            return new ImMapBranch3<V>(br3.Entry0, br3.Branch0, br3.Branch1,
                                br3.Entry1, entryLeafs2.AddToLeafs2(entry, key));

                        var newBranch = br3.Branch2.AddOrUpdateBranch(key, entry, 
                            out var popEntry, out var popRight);

                        if (popEntry != null)
                            return new ImMapBranch2<V>(br3.Entry1,
                                new ImMapBranch2<V>(br3.Entry0, br3.Branch0, br3.Branch1),
                                new ImMapBranch2<V>(popEntry, newBranch, popRight));

                        return new ImMapBranch3<V>(br3.Entry0, br3.Branch0, br3.Branch1, br3.Entry1, newBranch);
                    }

                    if (key < br3.Entry0.Key)
                    {
                        //                                  [4]
                        //         [4,6]      =>       [2]        [6]
                        // [1,2,3]   [5]  [7]      [0,1]  [3]   [5]   [7]
                        // and adding 0

                        var newBranch = br3.Branch0.AddOrUpdateBranch(key, entry, 
                            out var popEntry, out var popRight);

                        if (popEntry != null)
                            return new ImMapBranch2<V>(br3.Entry0,
                                new ImMapBranch2<V>(popEntry, newBranch, popRight),
                                new ImMapBranch2<V>(br3.Entry1, br3.Branch1, br3.Branch2));

                        return new ImMapBranch3<V>(br3.Entry0, newBranch, br3.Branch1, br3.Entry1, br3.Branch2);
                    }

                    if (key > br3.Entry0.Key && key < br3.Entry1.Key)
                    {
                        //                              [4]
                        //       [2, 7]            [2]         [7]
                        // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                        // and adding 6

                        var newBranch = br3.Branch1.AddOrUpdateBranch(key, entry, 
                            out var popEntry, out var popRight);

                        if (popEntry != null)
                            return new ImMapBranch2<V>(popEntry,
                                new ImMapBranch2<V>(br3.Entry0, br3.Branch0, newBranch),
                                new ImMapBranch2<V>(br3.Entry1, popRight, br3.Branch2));

                        return new ImMapBranch3<V>(br3.Entry0, br3.Branch0, newBranch, br3.Entry1, br3.Branch2);
                    }

                    // update
                    return key == br3.Entry0.Key
                        ? new ImMapBranch3<V>(entry, br3.Branch0, br3.Branch1, br3.Entry1, br3.Branch2)
                        : new ImMapBranch3<V>(br3.Entry0, br3.Branch0, br3.Branch1, entry, br3.Branch2);
                }

                if (key > br2.Entry0.Key)
                {
                    //      [3]                     [3]    ->  [6]                [3, 6]
                    // [1]       [5, 6, 7] =>  [1]       [4,5]     [7] =>  [1]   [4, 5]   [7]
                    // and adding 4, so we are merging the branches

                    if (br2.Branch1 is ImMapEntry<V> entryBranch)
                        return new ImMapBranch2<V>(br2.Entry0, br2.Branch0, entryBranch.AddToEntry(entry, key));

                    if (br2.Branch1 is ImMapLeafs2<V> entryLeafs2 && br2.Branch1 is ImMapLeafs3<V> == false)
                        return new ImMapBranch2<V>(br2.Entry0, br2.Branch0, entryLeafs2.AddToLeafs2(entry, key));

                    var newBranch = br2.Branch1.AddOrUpdateBranch(key, entry, out var popEntry, out var popRight);
                    if (popEntry != null)
                        return new ImMapBranch3<V>(br2.Entry0, br2.Branch0, newBranch, popEntry, popRight);

                    return new ImMapBranch2<V>(br2.Entry0, br2.Branch0, newBranch);
                }

                if (key < br2.Entry0.Key) 
                {
                    //           [4]                [2]  <-  [4]
                    // [1, 2, 3]      [5] =>  [0, 1]    [3]      [5]  =>
                    // and adding 0, so we are merging the branches

                    var newBranch = br2.Branch0.AddOrUpdateBranch(key, entry, 
                        out var popEntry, out var popRight);

                    if (popEntry != null)
                        return new ImMapBranch3<V>(popEntry, newBranch, popRight, br2.Entry0, br2.Branch1);

                    return new ImMapBranch2<V>(br2.Entry0, newBranch, br2.Branch1);
                }

                // update
                return new ImMapBranch2<V>(entry, br2.Branch0, br2.Branch1);
            }

            return map;
        }

        [MethodImpl((MethodImplOptions)256)]
        private static ImMap<V> AddToEntry<V>(this ImMapEntry<V> leaf, ImMapEntry<V> entry, int key) =>
            key > leaf.Key ? new ImMapLeafs2<V>(leaf, entry)
            : key < leaf.Key ? new ImMapLeafs2<V>(entry, leaf)
            : (ImMap<V>)entry;

        [MethodImpl((MethodImplOptions)256)]
        private static ImMapLeafs2<V> AddToLeafs2<V>(this ImMapLeafs2<V> leaf2, ImMapEntry<V> entry, int key) =>
            key > leaf2.Entry1.Key ? new ImMapLeafs3<V>(leaf2.Entry0, leaf2.Entry1, entry)
            : key < leaf2.Entry0.Key ? new ImMapLeafs3<V>(entry, leaf2.Entry0, leaf2.Entry1)
            : key > leaf2.Entry0.Key && key < leaf2.Entry1.Key ? new ImMapLeafs3<V>(leaf2.Entry0, entry, leaf2.Entry1)
            : key == leaf2.Entry0.Key ? new ImMapLeafs2<V>(entry, leaf2.Entry1)
            : new ImMapLeafs2<V>(leaf2.Entry0, entry);

        /// <summary> Adds or updates the value by key in the map, always returns a modified map </summary>
        internal static ImMap<V> AddOrUpdateBranch<V>(this ImMap<V> map, int key, ImMapEntry<V> entry, 
            out ImMapEntry<V> popEntry, out ImMap<V> popRight)
        {
            popEntry = null;
            popRight = null;

            //if (map is ImMapEntry<V> leaf)
            //    return key > leaf.Key ? new ImMapLeafs2<V>(leaf, entry)
            //        :  key < leaf.Key ? new ImMapLeafs2<V>(entry, leaf)
            //        : (ImMap<V>)entry;

            //if (map is ImMapLeafs2<V> leaf2)
            //{
                // Need to split for the Add, keep the leaf for the Update
            if (map is ImMapLeafs3<V> leaf3)
            {
                // [1 3 5]  =>      [3]
                // adding 7     [1]     [5, 7]
                popEntry = leaf3.Entry1;
                if (key > leaf3.Entry2.Key)
                {
                    popRight = new ImMapLeafs2<V>(leaf3.Entry2, entry);
                    return leaf3.Entry0;
                }

                if (key < leaf3.Entry0.Key)
                {
                    popRight = leaf3.Entry2;
                    return new ImMapLeafs2<V>(entry, leaf3.Entry0);
                }

                if (key > leaf3.Entry1.Key && key < leaf3.Entry2.Key)
                { 
                    popRight = new ImMapLeafs2<V>(entry, leaf3.Entry2);
                    return leaf3.Entry0;
                }

                if (key > leaf3.Entry0.Key && key < leaf3.Entry1.Key)
                {
                    popRight = leaf3.Entry2;
                    return new ImMapLeafs2<V>(leaf3.Entry0, entry);
                }

                popEntry = null;
                return key == leaf3.Entry0.Key ? new ImMapLeafs3<V>(entry, leaf3.Entry1, leaf3.Entry2)
                    : key == leaf3.Entry1.Key ? new ImMapLeafs3<V>(leaf3.Entry0, entry, leaf3.Entry2)
                    : new ImMapLeafs3<V>(leaf3.Entry0, leaf3.Entry1, entry);
            }

            //    return key > leaf2.Entry1.Key ? new ImMapLeafs3<V>(leaf2.Entry0, leaf2.Entry1, entry)
            //        : key < leaf2.Entry0.Key ? new ImMapLeafs3<V>(entry, leaf2.Entry0, leaf2.Entry1)
            //        : key > leaf2.Entry0.Key && key < leaf2.Entry1.Key ? new ImMapLeafs3<V>(leaf2.Entry0, entry, leaf2.Entry1)
            //        : key == leaf2.Entry0.Key ? new ImMapLeafs2<V>(entry, leaf2.Entry1)
            //        : new ImMapLeafs2<V>(leaf2.Entry0, entry);
            //}

            // - Adding to branch2 should not require to split the branch itself!
            // - The only result of split could be other Branch2 - because we are splitting only Leaf3 
            // - We cannot have a result of Branch2 for non-splitting addition
            if (map is ImMapBranch2<V> br2)
            {
                if (br2 is ImMapBranch3<V> br3) 
                {
                    if (key > br3.Entry1.Key)
                    {
                        //                                                   =>          [4]
                        //     [2, 4]                   [2, 4]  ->  [6]             [2]         [6]
                        // [1]   [3]  [5, 6, 7] =>  [1]   [3]    [5]   [7, 8]    [1]   [3]   [5]   [7,8]
                        // and adding 8,

                        var newBranch = br3.Branch2.AddOrUpdateBranch(key, entry, 
                            out var popEntry1, out var popRight1);

                        if (popEntry1 != null) 
                        {
                            popEntry = br3.Entry1;
                            popRight = new ImMapBranch2<V>(popEntry1, newBranch, popRight1);
                            return new ImMapBranch2<V>(br3.Entry0, br3.Branch0, br3.Branch1);
                        }

                        return new ImMapBranch3<V>(br3.Entry0, br3.Branch0, br3.Branch1, br3.Entry1, newBranch);
                    }

                    if (key < br3.Entry0.Key)
                    {
                        //                                  [4]
                        //         [4,6]      =>       [2]        [6]
                        // [1,2,3]   [5]  [7]      [0,1]  [3]   [5]   [7]
                        // and adding 0

                        var newBranch = br3.Branch0.AddOrUpdateBranch(key, entry, 
                            out var popEntry1, out var popRight1);

                        if (popEntry1 != null)
                        {
                            popEntry = br3.Entry0;
                            popRight = new ImMapBranch2<V>(br3.Entry1, br3.Branch1, br3.Branch2);
                            return new ImMapBranch2<V>(popEntry1, newBranch, popRight1);
                        }

                        return new ImMapBranch3<V>(br3.Entry0, newBranch, br3.Branch1, br3.Entry1, br3.Branch2);
                    }

                    if (key > br3.Entry0.Key && key < br3.Entry1.Key)
                    {
                        //                              [4]
                        //       [2, 7]            [2]         [7]
                        // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                        // and adding 6

                        var newBranch = br3.Branch1.AddOrUpdateBranch(key, entry, 
                            out var popEntry1, out var popRight1);

                        if (popEntry1 != null)
                        {
                            popEntry = popEntry1;
                            popRight = new ImMapBranch2<V>(br3.Entry1, popRight1, br3.Branch2);
                            return new ImMapBranch2<V>(br3.Entry0, br3.Branch0, newBranch);
                        }

                        return new ImMapBranch3<V>(br3.Entry0, br3.Branch0, newBranch, br3.Entry1, br3.Branch2);
                    }

                    // update
                    return key == br3.Entry0.Key
                        ? new ImMapBranch3<V>(entry, br3.Branch0, br3.Branch1, br3.Entry1, br3.Branch2)
                        : new ImMapBranch3<V>(br3.Entry0, br3.Branch0, br3.Branch1, entry, br3.Branch2);
                }

                if (key > br2.Entry0.Key)
                {
                    if (br2.Branch1 is ImMapEntry<V> entryBranch)
                        return new ImMapBranch2<V>(br2.Entry0, br2.Branch0, entryBranch.AddToEntry(entry, key));

                    if (br2.Branch1 is ImMapLeafs2<V> entryLeafs2 && br2.Branch1 is ImMapLeafs3<V> == false)
                        return new ImMapBranch2<V>(br2.Entry0, br2.Branch0, entryLeafs2.AddToLeafs2(entry, key));

                    var newBranch = br2.Branch1.AddOrUpdateBranch(key, entry, 
                        out var popEntry1, out var popRight1);

                    if (popEntry1 != null)
                        return new ImMapBranch3<V>(br2.Entry0, br2.Branch0, newBranch, popEntry1, popRight1);

                    return new ImMapBranch2<V>(br2.Entry0, br2.Branch0, newBranch);
                }

                if (key < br2.Entry0.Key) 
                {
                    //           [4]                [2]  <-  [4]
                    // [1, 2, 3]      [5] =>  [0, 1]    [3]      [5]  =>
                    // and adding 0, so we are merging the branches

                    var newBranch = br2.Branch0.AddOrUpdateBranch(key, entry, 
                        out var popEntry1, out var popRight1);

                    if (popEntry1 != null)
                        return new ImMapBranch3<V>(popEntry1, newBranch, popRight1, br2.Entry0, br2.Branch1);

                    return new ImMapBranch2<V>(br2.Entry0, newBranch, br2.Branch1);
                }

                // update
                return new ImMapBranch2<V>(entry, br2.Branch0, br2.Branch1);
            }

            return map;
        }

        /// <summary>Adds or updates the value by key in the map, always returns a modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value)
        {
            if (map == ImMap<V>.Empty)
                return new ImMapEntry<V>(key, value);
            return map.AddOrUpdateEntry(new ImMapEntry<V>(key, value));
        }
    }
}