using System.Runtime.CompilerServices;

namespace ImTools.Experimental.Tree234
{
    /// <summary>Represents an empty map</summary>
    public class ImMap<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap<V> Empty = new ImMap<V>();
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

    /// <summary></summary>
    public class ImMapLeafs2<V> : ImMap<V>
    {
        /// <summary>Left entry</summary>
        public readonly ImMapEntry<V> Entry0;

        /// <summary>Right entry</summary>
        public readonly ImMapEntry<V> Entry1;

        public ImMapLeafs2(ImMapEntry<V> entry0, ImMapEntry<V> entry1)
        {
            Entry0 = entry0;
            Entry1 = entry1;
        }
    }

    public sealed class ImMapLeafs3<V> : ImMapLeafs2<V>
    {
        /// <summary></summary>
        public readonly ImMapEntry<V> Entry2;

        public ImMapLeafs3(ImMapEntry<V> entry0, ImMapEntry<V> entry1, ImMapEntry<V> entry2)
            : base(entry0, entry1)
        {
            Entry2 = entry2;
        }
    }

    public class ImMapBranch2<V> : ImMap<V>
    {
        public readonly ImMapEntry<V> Entry0;

        /// <summary></summary>
        public readonly ImMap<V> Branch0; // can be Branchs | Entry | Leaf2 | Leaf3, if it is an Entry then other Tree cannot be an Entry

        /// <summary></summary>
        public readonly ImMap<V> Branch1;

        public ImMapBranch2(ImMapEntry<V> entry0, ImMap<V> branch0, ImMap<V> branch1)
        {
            Entry0 = entry0;
            Branch0 = branch0;
            Branch1 = branch1;
        }
    }

    public sealed class ImMapBranch3<V> : ImMapBranch2<V>
    {
        public readonly ImMapEntry<V> Entry1;

        /// <summary></summary>
        public readonly ImMap<V> Branch2;

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
        /// <summary> Adds or updates the value by key in the map, always returns a modified map </summary>
        public static ImMap<V> AddOrUpdateEntry<V>(this ImMap<V> map, ImMapEntry<V> entry)
        {
            if (map == ImMap<V>.Empty)
                return entry;

            var key = entry.Key;
            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key ? new ImMapLeafs2<V>(leaf, entry)
                    : key < leaf.Key ? new ImMapLeafs2<V>(entry, leaf)
                    : (ImMap<V>)entry;

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

                    if (key < leaf3.Entry2.Key && key > leaf3.Entry1.Key)
                        return new ImMapBranch2<V>(leaf3.Entry1,
                            leaf3.Entry0, new ImMapLeafs2<V>(entry, leaf3.Entry2));

                    if (key > leaf3.Entry0.Key && key < leaf3.Entry1.Key)
                        return new ImMapBranch2<V>(leaf3.Entry1,
                            new ImMapLeafs2<V>(leaf3.Entry0, entry), leaf3.Entry2);

                    return key == leaf3.Entry0.Key ? new ImMapLeafs3<V>(entry, leaf3.Entry1, leaf3.Entry2)
                        : key == leaf3.Entry1.Key ? new ImMapLeafs3<V>(leaf3.Entry0, entry, leaf3.Entry2)
                        : new ImMapLeafs3<V>(leaf3.Entry0, leaf3.Entry1, entry);
                }

                return key > leaf2.Entry1.Key ? new ImMapLeafs3<V>(leaf2.Entry0, leaf2.Entry1, entry)
                    : key < leaf2.Entry0.Key ? new ImMapLeafs3<V>(entry, leaf2.Entry0, leaf2.Entry1)
                    : key > leaf2.Entry0.Key && key < leaf2.Entry1.Key ? new ImMapLeafs3<V>(leaf2.Entry0, entry, leaf2.Entry1)
                    : key == leaf2.Entry0.Key ? new ImMapLeafs2<V>(entry, leaf2.Entry1)
                    : new ImMapLeafs2<V>(leaf2.Entry0, entry);
            }

            // - Adding to branch2 should not require to split the branch itself!
            // - The only result of split could be other Branch2 - because we are splitting only Leaf3 
            // - We cannot have a result of Branch2 for non-splitting addition
            if (map is ImMapBranch2<V> br2)
            {
                if (br2 is ImMapBranch3<V> br3) 
                {
                    if (key > br2.Entry0.Key)
                    {
                        //                                                   =>          [4]
                        //     [2, 4]                   [2, 4]  ->  [6]             [2]         [6]
                        // [1]   [3]  [5, 6, 7] =>  [1]   [3]    [5]   [7, 8]    [1]   [3]   [5]   [7,8]
                        // and adding 8,

                        var newBranch = br3.Branch2.AddOrUpdateBranch(key, entry, 
                            out var popEntry, out var popRight);

                        if (popEntry != null)
                            return new ImMapBranch2<V>(br3.Entry1,
                                new ImMapBranch2<V>(br3.Entry0, br3.Branch0, br3.Branch1),
                                new ImMapBranch2<V>(popEntry, newBranch, popRight));

                        return new ImMapBranch3<V>(br3.Entry0, br3.Branch0, br3.Branch0,
                            br3.Entry1, newBranch);
                    }
                    
                    return map;
                }

                if (key > br2.Entry0.Key)
                {
                    //      [3]                     [3]    ->  [6]                [3, 6]
                    // [1]       [5, 6, 7] =>  [1]       [4,5]     [7] =>  [1]   [4, 5]   [7]
                    // and adding 4,
                    // so we are merging the branches

                    var newBranch = br2.Branch1.AddOrUpdateBranch(key, entry, 
                        out var popEntry, out var popRight);

                    if (popEntry != null)
                        return new ImMapBranch3<V>(br2.Entry0, br2.Branch0, 
                            newBranch, popEntry, popRight);

                    return new ImMapBranch2<V>(br2.Entry0, br2.Branch0, newBranch);
                }
            }

            // todo: @incomplete add to branches
            return map;
        }

        /// <summary> Adds or updates the value by key in the map, always returns a modified map </summary>
        internal static ImMap<V> AddOrUpdateBranch<V>(this ImMap<V> map, int key, ImMapEntry<V> entry, 
            out ImMapEntry<V> popEntry, out ImMap<V> popRight)
        {
            popEntry = null;
            popRight = null;

            if (map is ImMapEntry<V> leaf)
                return key > leaf.Key ? new ImMapLeafs2<V>(leaf, entry)
                    : key < leaf.Key ? new ImMapLeafs2<V>(entry, leaf)
                    : (ImMap<V>)entry;

            if (map is ImMapLeafs2<V> leaf2)
            {
                // Need to split for the Add, keep the leaf for the Update
                if (leaf2 is ImMapLeafs3<V> leaf3)
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

                return key > leaf2.Entry1.Key ? new ImMapLeafs3<V>(leaf2.Entry0, leaf2.Entry1, entry)
                    : key < leaf2.Entry0.Key ? new ImMapLeafs3<V>(entry, leaf2.Entry0, leaf2.Entry1)
                    : key > leaf2.Entry0.Key && key < leaf2.Entry1.Key ? new ImMapLeafs3<V>(leaf2.Entry0, entry, leaf2.Entry1)
                    : key == leaf2.Entry0.Key ? new ImMapLeafs2<V>(entry, leaf2.Entry1)
                    : new ImMapLeafs2<V>(leaf2.Entry0, entry);
            }

            // - Adding to branch2 should not require to split the branch itself!
            // - The only result of split could be other Branch2 - because we are splitting only Leaf3 
            // - We cannot have a result of Branch2 for non-splitting addition
            if (map is ImMapBranch2<V> br2)
            {
                if (br2 is ImMapBranch3<V> br3) 
                {
                    // todo: @incomplete
                    return map;
                }

                // todo: @perf we will destruct and trash the returned branch anyway - so we need to find a way to avoid it
                if (key > br2.Entry0.Key)
                {
                    var newBranch = br2.Branch1.AddOrUpdateBranch(key, entry, 
                        out var popEntry1, out var popRight1);

                    if (popEntry != null)
                        return new ImMapBranch3<V>(br2.Entry0, br2.Branch0, 
                            newBranch, popEntry1, popRight1);

                    return new ImMapBranch2<V>(br2.Entry0, br2.Branch0, newBranch);
                }
            }


            // todo: @incomplete add to branches
            return map;
        }

        /// <summary>Adds or updates the value by key in the map, always returns a modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap<V> AddOrUpdate<V>(this ImMap<V> map, int key, V value) =>
            map.AddOrUpdateEntry(new ImMapEntry<V>(key, value));
    }
}