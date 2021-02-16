using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Experimental
{
    /// <summary>Entry containing the Key and Value in addition to the Hash</summary>
    public sealed class ImHashMapEntry<K, V> : ImHashMap234<K, V>.Entry
    {
        /// <summary>The key</summary>
        public readonly K Key;
        /// <summary>The value. Maybe modified if you need the Ref{Value} semantics. 
        /// You may add the entry with the default Value to the map, and calculate and set it later (e.g. using the CAS).</summary>
        public V Value;
        /// <summary>Constructs the entry with the key and value</summary>
        public ImHashMapEntry(int hash, K key) : base(hash) => Key = key;
        /// <summary>Constructs the entry with the key and value</summary>
        public ImHashMapEntry(int hash, K key, V value) : base(hash) 
        {
            Key = key;
            Value = value;
        } 
#if !DEBUG
        /// <inheritdoc />
        public override string ToString() => "{KVE: {H: " + Hash + ", K: " + Key + ", V: " + Value + "}}";
#endif
    }

    /// <summary>Entry containing the Value in addition to the Hash</summary>
    public sealed class ImHashMapEntry<V> : ImHashMap234<int, V>.Entry
    {
        /// <summary>The value. Maybe modified if you need the Ref{Value} semantics. 
        /// You may add the entry with the default Value to the map, and calculate and set it later (e.g. using the CAS).</summary>
        public V Value;
        /// <summary>Constructs the entry with the default value</summary>
        public ImHashMapEntry(int hash) : base(hash) {}
        /// <summary>Constructs the entry with the value</summary>
        public ImHashMapEntry(int hash, V value) : base(hash) => Value = value;
#if !DEBUG
        /// <inheritdoc />
        public override string ToString() => "{VE: {H: " + Hash + ", V: " + Value + "}}";
#endif
    }

        /// <summary>The composite containing the list of entries with the same conflicting Hash.</summary>
        public sealed class HashConflictKeyValuesEntry<K, V> : ImHashMap234<K, V>.Entry
        {
            /// <summary>The 2 and more conflicts.</summary>
            public ImHashMapEntry<K, V>[] Conflicts;
            internal HashConflictKeyValuesEntry(int hash, params ImHashMapEntry<K, V>[] conflicts) : base(hash) => Conflicts = conflicts;

#if !DEBUG
            /// <inheritdoc />
            public override string ToString()
            {
                var sb = new System.Text.StringBuilder("HashConflictingKVE: [");
                foreach (var x in Conflicts) 
                    sb.Append(x.ToString()).Append(", ");
                return sb.Append("]").ToString();
            }
#endif
        }


    /// <summary>The base and the holder class for the map tree leafs and branches, also defines the Empty tree.
    /// The map implementation is based on the "modified" 2-3-4 tree.</summary>
    public class ImHashMap234<K, V>
    {
        /// <summary>Empty map to start with. Exists as a single instance.</summary>
        public static readonly ImHashMap234<K, V> Empty = new ImHashMap234<K, V>();

        /// <summary>Hide the base constructor to prevent the multiple Empty trees creation</summary>
        protected ImHashMap234() { } // todo: @perf does the call to empty constructor hurt the perf?

        /// <summary>Prints the map tree in JSON-ish format in release mode and enumerates the keys in DEBUG.</summary>
        public override string ToString() 
        {
#if DEBUG
            // for the debug purposes we just output the first N keys in array
            const int outputCount = 101;
            var itemsInHashOrder = this.Enumerate().Take(outputCount).Select(x => x.Key).ToList();
            return $"new int[{(itemsInHashOrder.Count >= 100 ? ">=" : "") + itemsInHashOrder.Count}] {{" + string.Join(", ", itemsInHashOrder) + "}";
#else
            return "{}";
#endif
        }

        /// <summary>Lookup for the entry, if not found returns `null`</summary>
        public virtual Entry GetEntryOrDefault(int hash) => null;

        /// <summary>Defines the handler for update entry behavior</summary>
        public delegate Entry Updater(Entry oldEntry, Entry newEntry);

        /// <summary>Removes or keeps the entry</summary>
        public static readonly Updater DoRemove = (oldEntry, newEntry) => 
        {
            if (oldEntry is HashConflictKeyValuesEntry<K, V> hkv)
            {
                var cs = hkv.Conflicts;
                var n = cs.Length;
                var i = n - 1;
                while (i != -1 && newEntry != cs[i]) --i;
                if (i != -1)
                {
                    if (n == 2)
                        return i == 0 ? cs[1] : cs[0];

                    var newConflicts = new ImHashMapEntry<K, V>[n -= 1]; // the new n is less by one
                    if (i > 0) // copy the 1st part
                        Array.Copy(cs, 0, newConflicts, 0, i);
                    if (i < n) // copy the 2nd part
                        Array.Copy(cs, i + 1, newConflicts, i, n - i);

                    return new HashConflictKeyValuesEntry<K, V>(oldEntry.Hash, newConflicts);
                }
            }

            return newEntry == oldEntry ? null : oldEntry;
        };

        /// <summary>Returns the found entry with the same hash or the new map with added new entry.
        /// Note that the empty map will return the entry the same as if the entry was found - so the consumer should check for the empty map</summary>
        public virtual ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry) => entry;

        /// <summary>Returns the new map with old entry replaced by the new entry. Note that the old entry should be present.</summary>
        public virtual ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry) => this;

        /// <summary>Returns the map without the entry with the specified hash and key, or the same map if not found.</summary>
        public virtual ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove) => this;

        /// <summary>The base map entry for holding the hash and payload</summary>
        public abstract class Entry : ImHashMap234<K, V>
        {
            /// <summary>The Hash</summary>
            public readonly int Hash;

            /// <summary>Constructs the entry with the hash</summary>
            protected Entry(int hash) => Hash = hash;

            /// <inheritdoc />
            public sealed override Entry GetEntryOrDefault(int hash) => hash == Hash ? this : null;

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry) =>
                hash > Hash ? new Leaf2(this, entry) :
                hash < Hash ? new Leaf2(entry, this) :
                (ImHashMap234<K, V>)this;

            // todo: the big question what should it do and do we need this method on the entry
            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry) => 
                this == oldEntry ? newEntry : oldEntry;

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove) =>
                hash == Hash ? remove(this, entry) ?? Empty : this;
        }

        /// Tombstone for the removed entry. It still keeps the hash to preserve the tree operations.
        internal sealed class RemovedEntry : Entry 
        {
            public RemovedEntry(int hash) : base(hash) {}
            public override string ToString() => "{RemovedE: {H: " + Hash + "}}";
        }


        /// <summary>Leaf with 2 hash-ordered entries. Important: the both or either of entries may be null for the removed entries</summary>
        public sealed class Leaf2 : ImHashMap234<K, V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Right entry</summary>
            public readonly Entry Entry1;
            /// <summary>Constructs the leaf</summary>
            public Leaf2(Entry e0, Entry e1)
            {
                Debug.Assert(e0 == null || e1 == null || e0.Hash < e1.Hash);
                Entry0 = e0; Entry1 = e1;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "{L2: {E0: " + Entry0 + ", E1: " + Entry1 + "}}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) => 
                Entry0?.Hash == hash ? Entry0 :
                Entry1?.Hash == hash ? Entry1 :
                null;

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                if (e0 == null)
                    return e1 == null ? new Leaf2(null, entry)
                        : e1.Hash == hash ? (ImHashMap234<K, V>)e1
                        : e1.Hash <  hash ? new Leaf2(entry, e1) : new Leaf2(e1, entry);

                if (e1 == null)
                    return e0.Hash == hash ? (ImHashMap234<K, V>)e0
                        :  e0.Hash <  hash ? new Leaf2(e0, entry) : new Leaf2(entry, e0);

                return hash == e0.Hash ? e0
                     : hash == e1.Hash ? e1
                     : (ImHashMap234<K, V>)new Leaf2Plus1(entry, this);
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry) =>
                oldEntry == Entry0 ? new Leaf2(newEntry, Entry1) : 
                                     new Leaf2(Entry0, newEntry);

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                if (e0 == null)
                    return e1 == null || hash != e1.Hash ? this : e1 == entry ? this : new Leaf2(null, e1);

                if (e1 == null)
                    return hash != e0.Hash ? this : e0 == entry ? this : new Leaf2(e0, null);

                return hash == e0.Hash ? ((e0 = remove(e0, entry)) == Entry0 ? this : new Leaf2(e0, e1))
                    :  hash == e1.Hash ? ((e1 = remove(e1, entry)) == Entry1 ? this : new Leaf2(e0, e1))
                    :  this;
            }
        }

        /// <summary>The leaf containing the Leaf2 plus the newest added entry.</summary>
        public sealed class Leaf2Plus1 : ImHashMap234<K, V>
        {
            /// <summary>Plus entry</summary>
            public readonly Entry Plus;
            /// <summary>The dangling leaf</summary>
            public readonly Leaf2 L;
            /// <summary>Constructs the leaf</summary>
            public Leaf2Plus1(Entry plus, Leaf2 leaf)
            {
                Plus = plus;
                L    = leaf;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "{L21: {P: " + Plus + ", L: " + L + "}}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                if (hash == Plus.Hash) 
                    return Plus;
                Entry e0 = L.Entry0, e1 = L.Entry1;
                return e0.Hash == hash ? e0 : e1.Hash == hash ? e1 : null;
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                var p = Plus;
                if (hash == p.Hash) 
                    return p;

                Entry e0 = L.Entry0, e1 = L.Entry1;
                return
                    hash == e0.Hash ? e0 :
                    hash == e1.Hash ? e1 :
                    (ImHashMap234<K, V>)new Leaf2Plus1Plus1(entry, this);
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry) =>
                oldEntry == Plus     ? new Leaf2Plus1(newEntry, L) :
                oldEntry == L.Entry0 ? new Leaf2Plus1(Plus, new Leaf2(newEntry, L.Entry1)) :
                                       new Leaf2Plus1(Plus, new Leaf2(L.Entry0, newEntry));

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove)
            {
                var p = Plus;
                if (hash == p.Hash) 
                    return (p = remove(p, entry)) == Plus ? this : p == null ? L : (ImHashMap234<K, V>)new Leaf2Plus1(p, L);

                // despite the fact the Leaf2 entries maybe null then we don't need to check for null here,
                // because Leaf.AddOrUpdate guaranties that LeafPlus1(Plus1) does not contain nulls
                Entry e0 = L.Entry0, e1 = L.Entry1;
                if (hash == e0.Hash)
                    return (e0 = remove(e0, entry)) == L.Entry0 ? this : e0 == null
                        ? (p.Hash < e1.Hash ? new Leaf2(p, e1) : new Leaf2(e1, p)) : (ImHashMap234<K, V>)new Leaf2Plus1(p, new Leaf2(e0, e1));
                if (hash == e1.Hash)
                    return (e1 = remove(e1, entry)) == L.Entry1 ? this : e1 == null 
                        ? (p.Hash < e0.Hash ? new Leaf2(p, e0) : new Leaf2(e0, p)) : (ImHashMap234<K, V>)new Leaf2Plus1(p, new Leaf2(e0, e1));
                return this;
            }
        }

        /// <summary>Leaf with the Leaf2 plus added entry, plus added entry</summary>
        public sealed class Leaf2Plus1Plus1 : ImHashMap234<K, V>
        {
            /// <summary>New entry</summary>
            public readonly Entry Plus;
            /// <summary>The existing leaf to add entry to</summary>
            public readonly Leaf2Plus1 L;

            /// <summary>Constructs the leaf</summary>
            public Leaf2Plus1Plus1(Entry plus, Leaf2Plus1 l)
            {
                Plus = plus;
                L = l;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "{L211: {P: " + Plus + ", L: " + L + "}}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                if (hash == Plus.Hash) 
                    return Plus;
                if (hash == L.Plus.Hash) 
                    return L.Plus;
                Entry e0 = L.L.Entry0, e1 = L.L.Entry1;
                return e0.Hash == hash ? e0 : e1.Hash == hash ? e1 : null;
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return p;

                var pp = L.Plus;
                var pph = pp.Hash;
                if (pph == hash)
                    return pp;

                var l = L.L;
                Entry e0 = l.Entry0, e1 = l.Entry1;

                if (hash == e0.Hash)
                    return e0;
                if (hash == e1.Hash)
                    return e1;

                Entry swap = null;
                if (pph < e1.Hash)
                {
                    swap = e1; e1 = pp; pp = swap;
                    if (pph < e0.Hash)
                    {
                        swap = e0; e0 = e1; e1 = swap;
                    }
                }

                if (ph < pp.Hash)
                {
                    swap = pp; pp = p; p = swap;
                    if (ph < e1.Hash)
                    {
                        swap = e1; e1 = pp; pp = swap;
                        if (ph < e0.Hash)
                        {
                            swap = e0; e0 = e1; e1 = swap;
                        }
                    }
                }

                Entry e = entry;
                if (hash < p.Hash)
                {
                    swap = p; p = e; e = swap;
                    if (hash < pp.Hash)
                    {
                        swap = pp; pp = p; p = swap;
                        if (hash < e1.Hash)
                        {
                            swap = e1; e1 = pp; pp = swap;
                            if (hash < e0.Hash)
                            {
                                swap = e0; e0 = e1; e1 = swap;
                            }
                        }
                    }
                }

                return new Leaf5(e0, e1, pp, p, e);
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry) =>
                oldEntry == Plus       ? new Leaf2Plus1Plus1(newEntry, L) :
                oldEntry == L.Plus     ? new Leaf2Plus1Plus1(Plus, new Leaf2Plus1(newEntry, L.L)) :
                oldEntry == L.L.Entry0 ? new Leaf2Plus1Plus1(Plus, new Leaf2Plus1(L.Plus, new Leaf2(newEntry, L.L.Entry1))) :
                                         new Leaf2Plus1Plus1(Plus, new Leaf2Plus1(L.Plus, new Leaf2(L.L.Entry0, newEntry)));

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = remove(p, entry)) == Plus ? this : p == null ? L : (ImHashMap234<K, V>)new Leaf2Plus1Plus1(p, L);

                var pp = L.Plus;
                var pph = pp.Hash;
                if (pph == hash)
                    return (pp = remove(pp, entry)) == L.Plus ? this : pp == null ? new Leaf2Plus1(p, L.L) : (ImHashMap234<K, V>)new Leaf2Plus1Plus1(p, new Leaf2Plus1(pp, L.L));

                var l = L.L;
                Entry e0 = l.Entry0, e1 = l.Entry1;

                if (hash == e0.Hash)
                    return (e0 = remove(e0, entry)) == l.Entry0 ? this : e0 != null 
                        ? (ImHashMap234<K, V>)new Leaf2Plus1Plus1(p, new Leaf2Plus1(pp, new Leaf2(e0, e1)))
                        : new Leaf2Plus1(p, pph < e1.Hash ? new Leaf2(pp, e1) : new Leaf2(e1, pp));

                if (hash == e1.Hash)
                    return (e1 = remove(e1, entry)) == l.Entry1 ? this : e1 == null 
                        ? (ImHashMap234<K, V>)new Leaf2Plus1Plus1(p, new Leaf2Plus1(pp, new Leaf2(e0, e1)))
                        : new Leaf2Plus1(p, pph < e0.Hash ? new Leaf2(pp, e0) : new Leaf2(e0, pp));
                
                return this;
            }
        }

        /// <summary>Leaf with 5 hash-ordered entries</summary>
        public sealed class Leaf5 : ImHashMap234<K, V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Middle-left entry</summary>
            public readonly Entry Entry1;
            /// <summary>Middle entry</summary>
            public readonly Entry Entry2;
            /// <summary>Middle-right entry</summary>
            public readonly Entry Entry3;
            /// <summary>Right entry</summary>
            public readonly Entry Entry4;

            /// <summary>Constructs the leaf</summary>
            public Leaf5(Entry e0, Entry e1, Entry e2, Entry e3, Entry e4)
            {
                Debug.Assert(e0.Hash < e1.Hash, "e0 < e1");
                Debug.Assert(e1.Hash < e2.Hash, "e1 < e2");
                Debug.Assert(e2.Hash < e3.Hash, "e2 < e3");
                Debug.Assert(e3.Hash < e4.Hash, "e3 < e4");
                Entry0 = e0; Entry1 = e1; Entry2 = e2; Entry3 = e3; Entry4 = e4;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => 
                "{L2: {E0: " + Entry0 + ", E1: " + Entry1 + ", E2: " + Entry2 + ", E3: " + Entry3 + ", E4: " + Entry4 + "}}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash == Entry0.Hash ? Entry0 :
                hash == Entry1.Hash ? Entry1 :
                hash == Entry2.Hash ? Entry2 :
                hash == Entry3.Hash ? Entry3 :
                hash == Entry4.Hash ? Entry4 :
                null;

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                Entry e0 = Entry0, e1 = Entry1, e2 = Entry2, e3 = Entry3, e4 = Entry4;
                return
                    hash == e0.Hash ? e0 :
                    hash == e1.Hash ? e1 :
                    hash == e2.Hash ? e2 :
                    hash == e3.Hash ? e3 :
                    hash == e4.Hash ? e4 :
                    (ImHashMap234<K, V>)new Leaf5Plus1(entry, this);
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry) =>
                oldEntry == Entry0 ? new Leaf5(newEntry, Entry1, Entry2, Entry3, Entry4) : 
                oldEntry == Entry1 ? new Leaf5(Entry0, newEntry, Entry2, Entry3, Entry4) :
                oldEntry == Entry2 ? new Leaf5(Entry0, Entry1, newEntry, Entry3, Entry4) :
                oldEntry == Entry3 ? new Leaf5(Entry0, Entry1, Entry2, newEntry, Entry4) :
                                     new Leaf5(Entry0, Entry1, Entry2, Entry3, newEntry);

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove)
            {
                Entry e0 = Entry0, e1 = Entry1, e2 = Entry2, e3 = Entry3, e4 = Entry4;
                if (hash == e0.Hash)
                    return (e0 = remove(e0, entry)) == Entry0 ? this : e0 == null ? new Leaf2Plus1Plus1(e4, new Leaf2Plus1(e3, new Leaf2(e1, e2))) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                if (hash == e1.Hash)
                    return (e1 = remove(e1, entry)) == Entry1 ? this : e1 == null ? new Leaf2Plus1Plus1(e4, new Leaf2Plus1(e3, new Leaf2(e0, e2))) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                if (hash == e2.Hash)
                    return (e2 = remove(e2, entry)) == Entry2 ? this : e2 == null ? new Leaf2Plus1Plus1(e4, new Leaf2Plus1(e3, new Leaf2(e0, e1))) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                if (hash == e3.Hash)
                    return (e3 = remove(e3, entry)) == Entry3 ? this : e3 == null ? new Leaf2Plus1Plus1(e4, new Leaf2Plus1(e2, new Leaf2(e0, e1))) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                if (hash == e4.Hash)
                    return (e4 = remove(e4, entry)) == Entry4 ? this : e4 == null ? new Leaf2Plus1Plus1(e3, new Leaf2Plus1(e2, new Leaf2(e0, e1))) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                return this;
            }
        }

        /// <summary>Leaf with 5 existing ordered entries plus 1 newly added entry.</summary>
        public sealed class Leaf5Plus1 : ImHashMap234<K, V>
        {
            /// <summary>New entry</summary>
            public readonly Entry Plus;
            /// <summary>Dangling leaf</summary>
            public readonly Leaf5 L;

            /// <summary>Constructs the leaf</summary>
            public Leaf5Plus1(Entry plus, Leaf5 l)
            {
                Plus = plus;
                L    = l;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "{L51: {P: " + Plus + ", L: " + L + "}}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                if (hash == Plus.Hash) 
                    return Plus; 
                var l = L;
                return 
                    hash == l.Entry0.Hash ? l.Entry0 :
                    hash == l.Entry1.Hash ? l.Entry1 :
                    hash == l.Entry2.Hash ? l.Entry2 :
                    hash == l.Entry3.Hash ? l.Entry3 :
                    hash == l.Entry4.Hash ? l.Entry4 :
                    null;
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return p;

                var l = L; 
                Entry e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4;

                if (hash == e0.Hash)
                    return e0;
                if (hash == e1.Hash)
                    return e1;
                if (hash == e2.Hash)
                    return e2;
                if (hash == e3.Hash)
                    return e3;
                if (hash == e4.Hash)
                    return e4;

                return new Leaf5Plus1Plus1(entry, this);
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry)
            {
                var p = Plus;
                if (oldEntry == p)
                    return new Leaf5Plus1(newEntry, L);

                var l = L; 
                Entry e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4;
                return
                    oldEntry == e0 ? new Leaf5Plus1(p, new Leaf5(newEntry, e1, e2, e3, e4)) :
                    oldEntry == e1 ? new Leaf5Plus1(p, new Leaf5(e0, newEntry, e2, e3, e4)) :
                    oldEntry == e2 ? new Leaf5Plus1(p, new Leaf5(e0, e1, newEntry, e3, e4)) :
                    oldEntry == e3 ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, newEntry, e4)) :
                                     new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, newEntry));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove)
            {
                var p  = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = remove(p, entry)) == Plus ? this : p == null ? L : (ImHashMap234<K, V>)new Leaf5Plus1(p, L);

                var l = L;
                Entry e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4;

                if (hash == e0.Hash)
                    return (e0 = remove(e0, entry)) == l.Entry0 ? this : e0 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e1.Hash ? new Leaf5(p, e1, e2, e3, e4) :
                    ph < e2.Hash ? new Leaf5(e1, p, e2, e3, e4) :
                    ph < e3.Hash ? new Leaf5(e1, e2, p, e3, e4) :
                    ph < e4.Hash ? new Leaf5(e1, e2, e3, p, e4) :
                                   new Leaf5(e1, e2, e3, e4, p);

                if (hash == e1.Hash)
                    return (e1 = remove(e1, entry)) == l.Entry0 ? this : e1 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e0.Hash ? new Leaf5(p, e0, e2, e3, e4) :
                    ph < e2.Hash ? new Leaf5(e0, p, e2, e3, e4) :
                    ph < e3.Hash ? new Leaf5(e0, e2, p, e3, e4) :
                    ph < e4.Hash ? new Leaf5(e0, e2, e3, p, e4) :
                                   new Leaf5(e0, e2, e3, e4, p);

                if (hash == e2.Hash)
                    return (e2 = remove(e2, entry)) == l.Entry0 ? this : e2 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e0.Hash ? new Leaf5(p, e0, e1, e3, e4) :
                    ph < e1.Hash ? new Leaf5(e0, p, e1, e3, e4) :
                    ph < e3.Hash ? new Leaf5(e0, e1, p, e3, e4) :
                    ph < e4.Hash ? new Leaf5(e0, e1, e3, p, e4) :
                                   new Leaf5(e0, e1, e3, e4, p);

                if (hash == e3.Hash)
                    return (e3 = remove(e3, entry)) == l.Entry0 ? this : e3 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e0.Hash ? new Leaf5(p, e0, e1, e2, e4) :
                    ph < e1.Hash ? new Leaf5(e0, p, e1, e2, e4) :
                    ph < e2.Hash ? new Leaf5(e0, e1, p, e2, e4) :
                    ph < e4.Hash ? new Leaf5(e0, e1, e2, p, e4) :
                                   new Leaf5(e0, e1, e2, e4, p);

                if (hash == e4.Hash)
                    return (e4 = remove(e4, entry)) == l.Entry0 ? this : e4 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e0.Hash ? new Leaf5(p, e0, e1, e2, e3) :
                    ph < e1.Hash ? new Leaf5(e0, p, e1, e2, e3) :
                    ph < e2.Hash ? new Leaf5(e0, e1, p, e2, e3) :
                    ph < e4.Hash ? new Leaf5(e0, e1, e2, p, e3) :
                                   new Leaf5(e0, e1, e2, e3, p);

                return this;
            }
        }

        /// <summary>Leaf with 5 existing ordered entries plus 1 newly added, plus 1 newly added.</summary>
        public sealed class Leaf5Plus1Plus1 : ImHashMap234<K, V>
        {
            /// <summary>New entry</summary>
            public readonly Entry Plus;
            /// <summary>Dangling leaf</summary>
            public readonly Leaf5Plus1 L;

            /// <summary>Constructs the leaf</summary>
            public Leaf5Plus1Plus1(Entry plus, Leaf5Plus1 l)
            {
                Plus = plus;
                L    = l;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "{L511: {P: " + Plus + ", L: " + L + "}}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                if (hash == Plus.Hash)
                    return Plus;
                if (hash == L.Plus.Hash)
                    return L.Plus;
                var l = L.L;
                return 
                    hash == l.Entry0.Hash ? l.Entry0 :
                    hash == l.Entry1.Hash ? l.Entry1 :
                    hash == l.Entry2.Hash ? l.Entry2 :
                    hash == l.Entry3.Hash ? l.Entry3 :
                    hash == l.Entry4.Hash ? l.Entry4 :
                    null;
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return p;

                var pp = L.Plus;
                var pph = pp.Hash;
                if (pph == hash)
                    return pp;

                var l = L.L;
                Entry e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4;

                if (hash == e0.Hash)
                    return e0;
                if (hash == e1.Hash)
                    return e1;
                if (hash == e2.Hash)
                    return e2;
                if (hash == e3.Hash)
                    return e3;
                if (hash == e4.Hash)
                    return e4;

                var right = hash > e4.Hash && ph > e4.Hash && pph > e4.Hash;
                var left  = !right && hash < e0.Hash && ph < e0.Hash && pph < e0.Hash;

                Entry swap = null;
                if (pph < e4.Hash)
                {
                    swap = e4; e4 = pp; pp = swap;
                    if (pph < e3.Hash)
                    {
                        swap = e3; e3 = e4; e4 = swap;
                        if (pph < e2.Hash)
                        {
                            swap = e2; e2 = e3; e3 = swap;
                            if (pph < e1.Hash)
                            {
                                swap = e1; e1 = e2; e2 = swap;
                                if (pph < e0.Hash)
                                {
                                    swap = e0; e0 = e1; e1 = swap;
                                }
                            }
                        }
                    }
                }

                if (ph < pp.Hash)
                {
                    swap = pp; pp = p; p = swap;
                    if (ph < e4.Hash)
                    {
                        swap = e4; e4 = pp; pp = swap;
                        if (ph < e3.Hash)
                        {
                            swap = e3; e3 = e4; e4 = swap;
                            if (ph < e2.Hash)
                            {
                                swap = e2; e2 = e3; e3 = swap;
                                if (ph < e1.Hash)
                                {
                                    swap = e1; e1 = e2; e2 = swap;
                                    if (ph < e0.Hash)
                                    {
                                        swap = e0; e0 = e1; e1 = swap;
                                    }
                                }
                            }
                        }
                    }
                }

                Entry e = entry;
                if (hash < p.Hash)
                {
                    swap = p; p = e; e = swap;
                    if (hash < pp.Hash)
                    {
                        swap = pp; pp = p; p = swap;
                        if (hash < e4.Hash)
                        {
                            swap = e4; e4 = pp; pp = swap;
                            if (hash < e3.Hash)
                            {
                                swap = e3; e3 = e4; e4 = swap;
                                if (hash < e2.Hash)
                                {
                                    swap = e2; e2 = e3; e3 = swap;
                                    if (hash < e1.Hash)
                                    {
                                        swap = e1; e1 = e2; e2 = swap;
                                        if (hash < e0.Hash)
                                        {
                                            swap = e0; e0 = e1; e1 = swap;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (right)
                    return new Branch2(l, pp, new Leaf2(p, e));
                if (left)
                    return new Branch2(new Leaf2(e0, e1), e2, l);
                return new Branch2(new Leaf5(e0, e1, e2, e3, e4), pp, new Leaf2(p, e));
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry)
            {
                var p = Plus;
                if (p == oldEntry)
                    return new Leaf5Plus1Plus1(newEntry, L);

                var pp = L.Plus;
                if (pp == oldEntry)
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(newEntry, L.L));

                var l = L.L;
                Entry e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4;
                return
                    oldEntry == e0 ? new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(newEntry, e1, e2, e3, e4))) :
                    oldEntry == e1 ? new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, newEntry, e2, e3, e4))) :
                    oldEntry == e2 ? new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, e1, newEntry, e3, e4))) :
                    oldEntry == e3 ? new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, e1, e2, newEntry, e4))) :
                                     new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, e1, e2, e3, newEntry)));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove)
            {
                var p  = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = remove(p, entry)) == Plus ? this : p == null ? L : (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, L);

                var pp  = L.Plus;
                var pph = pp.Hash;
                if (pph == hash)
                    return (pp = remove(pp, entry)) == Plus ? this : pp == null ? new Leaf5Plus1(p, L.L) : (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, L.L));

                var l = L.L;
                Entry e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4;

                if (hash == e0.Hash)
                    return (e0 = remove(e0, entry)) == l.Entry0 ? this : e0 != null ? 
                        (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, e1, e2, e3, e4))) :
                        pph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(pp, e1, e2, e3, e4)) :
                        pph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e1, pp, e2, e3, e4)) :
                        pph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e1, e2, pp, e3, e4)) :
                        pph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e1, e2, e3, pp, e4)) :
                                        new Leaf5Plus1(p, new Leaf5(e1, e2, e3, e4, pp));

                if (hash == e1.Hash)
                    return (e1 = remove(e1, entry)) == l.Entry1 ? this : e1 != null ? 
                        (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, e1, e2, e3, e4))) :
                        pph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(pp, e0, e2, e3, e4)) :
                        pph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, pp, e2, e3, e4)) :
                        pph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e2, pp, e3, e4)) :
                        pph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e2, e3, pp, e4)) :
                                        new Leaf5Plus1(p, new Leaf5(e0, e2, e3, e4, pp));

                if (hash == e2.Hash)
                    return (e2 = remove(e2, entry)) == l.Entry2 ? this : e2 != null ? 
                        (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, e1, e2, e3, e4))) :
                        pph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(pp, e0, e1, e3, e4)) :
                        pph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, pp, e1, e3, e4)) :
                        pph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, pp, e3, e4)) :
                        pph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e3, pp, e4)) :
                                        new Leaf5Plus1(p, new Leaf5(e0, e1, e3, e4, pp));

                if (hash == e3.Hash)
                    return (e3 = remove(e3, entry)) == l.Entry3 ? this : e3 != null ? 
                        (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, e1, e2, e3, e4))) :
                        pph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(pp, e0, e1, e2, e4)) :
                        pph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, pp, e1, e2, e4)) :
                        pph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, pp, e2, e4)) :
                        pph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, pp, e4)) :
                                        new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e4, pp));

                if (hash == e4.Hash)
                    return (e4 = remove(e4, entry)) == l.Entry4 ? this : e4 != null ? 
                        (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(pp, new Leaf5(e0, e1, e2, e3, e4))) :
                        pph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(pp, e0, e1, e2, e3)) :
                        pph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, pp, e1, e2, e3)) :
                        pph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, pp, e2, e3)) :
                        pph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, pp, e3)) :
                                        new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, pp));

                return this;
            }
        }

        /// <summary>Branch of 2 leafs or branches with entry in the middle</summary>
        public class Branch2 : ImHashMap234<K, V>
        {
            /// <summary>Left branch</summary>
            public readonly ImHashMap234<K, V> Left;
            /// <summary>Entry in the middle</summary>
            public readonly Entry MidEntry;
            /// <summary>Right branch</summary>
            public readonly ImHashMap234<K, V> Right;
            /// <summary>Constructs</summary>
            public Branch2(ImHashMap234<K, V> left, Entry entry, ImHashMap234<K, V> right)
            {
                Debug.Assert(left  != Empty && left  is Entry == false);
                Debug.Assert(right != Empty && right is Entry == false);
                MidEntry = entry;
                Left     = left;
                Right    = right;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "{B2: {E: " + MidEntry + ", L: " + Left + ", R: " + Right + "}}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) 
            {
                var mh = MidEntry.Hash;
                return hash > mh ? Right.GetEntryOrDefault(hash) 
                    :  hash < mh ? Left .GetEntryOrDefault(hash) 
                    :  MidEntry is RemovedEntry ? null : MidEntry;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                var e = MidEntry;
                if (hash > e.Hash)
                {
                    var right = Right;
                    var newRight = right.AddOrGetEntry(hash, entry);
                    return newRight is Entry ? newRight
                         : right.GetType() != typeof(Branch2) && newRight.GetType() == typeof(Branch2) 
                         ? new RightyBranch3(Left, e, newRight) : new Branch2(Left, e, newRight);
                }

                if (hash < e.Hash)
                {
                    var left = Left;
                    var newLeft = left.AddOrGetEntry(hash, entry);
                    return newLeft is Entry ? newLeft 
                         : left.GetType() != typeof(Branch2) && newLeft.GetType() == typeof(Branch2) 
                         ? new LeftyBranch3(newLeft, e, Right) : new Branch2(newLeft, e, Right);
                }

                return e is RemovedEntry ? new Branch2(Left, entry, Right) : (ImHashMap234<K, V>)e;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry)
            {
                var e = MidEntry;
                return hash > e.Hash ? new Branch2(Left, e, Right.ReplaceEntry(hash, oldEntry, newEntry))
                    :  hash < e.Hash ? new Branch2(Left.ReplaceEntry(hash, oldEntry, newEntry), e, Right)
                    :  new Branch2(Left, newEntry, Right);
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> RemoveEntry(int hash, Entry entry, Updater remove)
            {
                var e = MidEntry;
                if (hash > e.Hash)
                {
                    var newRight = Right.RemoveEntry(hash, entry, remove);
                    return newRight == Right ? this : new Branch2(Left, MidEntry, newRight);
                }

                if (hash < e.Hash)
                {
                    var newLeft = Left.RemoveEntry(hash, entry, remove);
                    return newLeft == Left ? this : new Branch2(newLeft, MidEntry, Right);
                }

                return (e = remove(e, entry)) == MidEntry ? this : new Branch2(Left, e, Right);
            }
        }

        /// <summary>Right-skewed Branch of 3 - actually a branch of 2 with the right branch of 2</summary>
        public sealed class RightyBranch3 : Branch2
        {
            /// <summary>Creating the branch</summary>
            public RightyBranch3(ImHashMap234<K, V> left, Entry entry, ImHashMap234<K, V>  right) : base(left, entry, right) {}

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "{RB3: {"  + base.ToString() + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) 
            {
                var mh = MidEntry.Hash;
                if (mh > hash)
                    return Left.GetEntryOrDefault(hash);
                if (mh < hash)
                {
                    var r = (Branch2)Right;
                    mh = r.MidEntry.Hash;
                    return hash > mh ? r.Right.GetEntryOrDefault(hash) 
                        :  hash < mh ? r.Left .GetEntryOrDefault(hash) 
                        :  r.MidEntry is RemovedEntry ? null : r.MidEntry;
                }
                return MidEntry is RemovedEntry ? null : MidEntry;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                var h0 = MidEntry.Hash;
                var rb = (Branch2)Right;
                var h1 = rb.MidEntry.Hash;
                
                if (hash > h1)
                {
                    var right = rb.Right;
                    var newRight = right.AddOrGetEntry(hash, entry);
                    if (newRight is Entry)
                        return newRight;
                    if (right.GetType() != typeof(Branch2) && newRight.GetType() == typeof(Branch2))
                        return new Branch2(new Branch2(Left, MidEntry, rb.Left), rb.MidEntry, newRight);
                    return new RightyBranch3(Left, MidEntry, new Branch2(rb.Left, rb.MidEntry, newRight));
                }

                if (hash < h0)
                {
                    var left = Left;
                    var newLeft = left.AddOrGetEntry(hash, entry);
                    if (newLeft is Entry)
                        return newLeft;
                    if (left.GetType() != typeof(Branch2) && newLeft.GetType() == typeof(Branch2))
                        return new Branch2(newLeft, MidEntry, rb);
                    return new RightyBranch3(newLeft, MidEntry, rb);
                }

                if (hash > h0 && hash < h1)
                {
                    var middle = rb.Left;
                    var newMiddle = middle.AddOrGetEntry(hash, entry);
                    if (newMiddle is Entry)
                        return newMiddle;
                    if (middle.GetType() != typeof(Branch2) && newMiddle.GetType() == typeof(Branch2))
                    {
                        var nmb2 = (Branch2)newMiddle;
                        return new Branch2(new Branch2(Left, MidEntry, nmb2.Left), nmb2.MidEntry, new Branch2(nmb2.Right, rb.MidEntry, rb.Right));
                    }

                    return new RightyBranch3(Left, MidEntry, new Branch2(newMiddle, rb.MidEntry, rb.Right));
                }

                var e0 = MidEntry;
                if (hash == h0)
                    return e0 is RemovedEntry ? new RightyBranch3(Left, entry, rb) : (ImHashMap234<K, V>)e0;

                var e1 = rb.MidEntry;
                return  e1 is RemovedEntry ? new RightyBranch3(Left, e0, new Branch2(rb.Left, entry, rb.Right)) : (ImHashMap234<K, V>)e1;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry)
            {
                var e = MidEntry;
                return hash > e.Hash ? new RightyBranch3(Left, e, Right.ReplaceEntry(hash, oldEntry, newEntry)) 
                    :  hash < e.Hash ? new RightyBranch3(Left.ReplaceEntry(hash, oldEntry, newEntry), e, Right)
                    :  new RightyBranch3(Left, newEntry, Right);
            }
        }

        /// <summary>Left-skewed Branch of 3 - actually a branch of 2 with the left branch of 2</summary>
        public sealed class LeftyBranch3 : Branch2
        {
            /// <summary>Creating the branch</summary>
            public LeftyBranch3(ImHashMap234<K, V> leftBranch, Entry entry, ImHashMap234<K, V> right) : base(leftBranch, entry, right) {}

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "{LB3: {"  + base.ToString() + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) 
            {
                var mh = MidEntry.Hash;
                if (mh < hash)
                    return Right.GetEntryOrDefault(hash);
                if (mh > hash)
                {
                    var l = (Branch2)Left;
                    mh = l.MidEntry.Hash;
                    return hash > mh ? l.Right.GetEntryOrDefault(hash) 
                        :  hash < mh ? l.Left .GetEntryOrDefault(hash) 
                        :  l.MidEntry is RemovedEntry ? null : l.MidEntry;
                }
                return MidEntry is RemovedEntry ? null : MidEntry;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrGetEntry(int hash, Entry entry)
            {
                var lb = (Branch2)Left;
                var h0 = lb.MidEntry.Hash;
                var h1 = MidEntry.Hash;
                
                if (hash > h1)
                {
                    var right = Right;
                    var newRight = right.AddOrGetEntry(hash, entry);
                    if (newRight is Entry)
                        return newRight;
                    if (right.GetType() != typeof(Branch2) && newRight.GetType() == typeof(Branch2))
                        return new Branch2(lb, MidEntry, newRight);
                    return new LeftyBranch3(lb, MidEntry, newRight);
                }

                if (hash < h0)
                {
                    var left = lb.Left;
                    var newLeft = left.AddOrGetEntry(hash, entry);
                    if (newLeft is Entry)
                        return newLeft;
                    if (left.GetType() != typeof(Branch2) && newLeft.GetType() == typeof(Branch2))
                        return new Branch2(newLeft, lb.MidEntry, new Branch2(lb.Right, MidEntry, Right));
                    return new LeftyBranch3(new Branch2(newLeft, lb.MidEntry, lb.Right), MidEntry, Right);
                }

                if (hash > h0 && hash < h1)
                {
                    var middle = lb.Right;
                    var newMiddle = middle.AddOrGetEntry(hash, entry);
                    if (newMiddle is Entry)
                        return newMiddle;
                    if (middle.GetType() != typeof(Branch2) && newMiddle.GetType() == typeof(Branch2))
                    {
                        var nmb2 = (Branch2)newMiddle;
                        return new Branch2(new Branch2(lb.Left, lb.MidEntry, nmb2.Left), nmb2.MidEntry, new Branch2(nmb2.Right, MidEntry, Right));
                    }
                    return new LeftyBranch3(new Branch2(lb.Left, lb.MidEntry, newMiddle), MidEntry, Right);
                }

                var e0 = lb.MidEntry;
                var e1 = MidEntry;
                return hash == h0
                    ? (e0 is RemovedEntry ? new LeftyBranch3(new Branch2(lb.Left, entry, lb.Right), e1, Right) : (ImHashMap234<K, V>)e0)
                    : (e1 is RemovedEntry ? new LeftyBranch3(lb, entry, Right) : (ImHashMap234<K, V>)e1);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> ReplaceEntry(int hash, Entry oldEntry, Entry newEntry)
            {
                var e = MidEntry;
                return hash > e.Hash ? new LeftyBranch3(Left, e, Right.ReplaceEntry(hash, oldEntry, newEntry))
                    :  hash < e.Hash ? new LeftyBranch3(Left.ReplaceEntry(hash, oldEntry, newEntry), e, Right)
                    :  new LeftyBranch3(Left, newEntry, Right);
            }
        }
    }

    /// <summary>The map methods</summary>
    public static class ImHashMap234
    {
        /// <summary>Helper stack wrapper for the array</summary>
        public sealed class Stack<T>
        {
            private const int DefaultInitialCapacity = 4;
            private T[] _items;

            /// <summary>Creates the list of the `DefaultInitialCapacity`</summary>
            public Stack() => _items = new T[DefaultInitialCapacity];

            /// <summary>Pushes the item</summary>
            public void Push(T item, int count)
            {
                if (count >= _items.Length)
                    _items = Expand(_items);
                _items[count] = item;
            }

            /// <summary>Gets the item by index</summary>
            public T Get(int index) => _items[index];

            private static T[] Expand(T[] items)
            {
                var count = items.Length;
                var newItems = new T[count << 1]; // count * 2
                Array.Copy(items, 0, newItems, 0, count);
                return newItems;
            }
        }

        /// <summary>Enumerates all the map entries in the hash order.
        /// `parents` parameter allow to reuse the stack memory used for traversal between multiple enumerates.
        /// So you may pass the empty `parents` into the first `Enumerate` and then keep passing the same `parents` into the subsequent `Enumerate` calls</summary>
        public static IEnumerable<ImHashMapEntry<K, V>> Enumerate<K, V>(this ImHashMap234<K, V> map, Stack<ImHashMap234<K, V>> parents = null)
        {
            if (map == ImHashMap234<K, V>.Empty)
                yield break;
            if (map is ImHashMap234<K, V>.Entry e)
            {
                if (e is ImHashMapEntry<K, V>v) yield return v;
                else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e).Conflicts) yield return c;
                yield break;
            }

            var count = 0;
            while (true)
            {
                if (map is ImHashMap234<K, V>.Branch2 b2)
                {
                    if (parents == null)
                        parents = new Stack<ImHashMap234<K, V>>();
                    parents.Push(map, count++);
                    map = b2.Left;
                    continue;
                }
                
                if (map is ImHashMap234<K, V>.Leaf2 l2)
                {
                    if (l2.Entry0 is ImHashMapEntry<K, V>v0) yield return v0;
                    else if (l2.Entry0 != null) foreach (var c in ((HashConflictKeyValuesEntry<K, V>)l2.Entry0).Conflicts) yield return c;
                    if (l2.Entry1 is ImHashMapEntry<K, V>v1) yield return v1;
                    else if (l2.Entry1 != null) foreach (var c in ((HashConflictKeyValuesEntry<K, V>)l2.Entry1).Conflicts) yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf2Plus1 l21)
                {
                    var p  = l21.Plus;
                    var ph = p.Hash;
                    var l  = l21.L;
                    ImHashMap234<K, V>.Entry e0 = l.Entry0, e1 = l.Entry1, swap = null;
                    if (ph < e1.Hash)
                    {
                        swap = e1; e1 = p; p = swap;
                        if (ph < e0.Hash)
                        {
                            swap = e0; e0 = e1; e1 = swap;
                        }
                    }

                    if (e0 is ImHashMapEntry<K, V>v0) yield return v0;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e0).Conflicts) yield return c;
                    if (e1 is ImHashMapEntry<K, V>v1) yield return v1;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e1).Conflicts) yield return c;
                    if (p  is ImHashMapEntry<K, V>v2) yield return v2;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)p ).Conflicts) yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf2Plus1Plus1 l211)
                {
                    var p  = l211.Plus;
                    var pp = l211.L.Plus;
                    var ph = pp.Hash;
                    var l  = l211.L.L;
                    ImHashMap234<K, V>.Entry e0 = l.Entry0, e1 = l.Entry1, swap = null;
                    if (ph < e1.Hash)
                    {
                        swap = e1; e1 = pp; pp = swap;
                        if (ph < e0.Hash)
                        {
                            swap = e0; e0 = e1; e1 = swap;
                        }
                    }

                    ph = p.Hash;
                    if (ph < pp.Hash)
                    {
                        swap = pp; pp = p; p = swap;
                        if (ph < e1.Hash)
                        {
                            swap = e1; e1 = pp; pp = swap;
                            if (ph < e0.Hash)
                            {
                                swap = e0; e0 = e1; e1 = swap;
                            }
                        }
                    }

                    if (e0 is ImHashMapEntry<K, V>v0) yield return v0;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e0).Conflicts) yield return c;
                    if (e1 is ImHashMapEntry<K, V>v1) yield return v1;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e1).Conflicts) yield return c;
                    if (pp is ImHashMapEntry<K, V>v2) yield return v2;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)pp).Conflicts) yield return c;
                    if (p  is ImHashMapEntry<K, V>v3) yield return v3;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)p).Conflicts)  yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf5 l5)
                {
                    if (l5.Entry0 is ImHashMapEntry<K, V>v0) yield return v0;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)l5.Entry0).Conflicts) yield return c;
                    if (l5.Entry1 is ImHashMapEntry<K, V>v1) yield return v1;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)l5.Entry1).Conflicts) yield return c;
                    if (l5.Entry2 is ImHashMapEntry<K, V>v2) yield return v2;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)l5.Entry2).Conflicts) yield return c;
                    if (l5.Entry3 is ImHashMapEntry<K, V>v3) yield return v3;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)l5.Entry3).Conflicts) yield return c;
                    if (l5.Entry4 is ImHashMapEntry<K, V>v4) yield return v4;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)l5.Entry4).Conflicts) yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf5Plus1 l51)
                {
                    var p  = l51.Plus;
                    var ph = p.Hash;
                    var l  = l51.L;
                    ImHashMap234<K, V>.Entry e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4, swap = null;
                    if (ph < e4.Hash)
                    {
                        swap = e4; e4 = p; p = swap;
                        if (ph < e3.Hash)
                        {
                            swap = e3; e3 = e4; e4 = swap;
                            if (ph < e2.Hash)
                            {
                                swap = e2; e2 = e3; e3 = swap;
                                if (ph < e1.Hash)
                                {
                                    swap = e1; e1 = e2; e2 = swap;
                                    if (ph < e0.Hash)
                                    {
                                        swap = e0; e0 = e1; e1 = swap;
                                    }
                                }
                            }
                        }
                    }

                    if (e0 is ImHashMapEntry<K, V>v0) yield return v0;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e0).Conflicts) yield return c;
                    if (e1 is ImHashMapEntry<K, V>v1) yield return v1;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e1).Conflicts) yield return c;
                    if (e2 is ImHashMapEntry<K, V>v2) yield return v2;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e2).Conflicts) yield return c;
                    if (e3 is ImHashMapEntry<K, V>v3) yield return v3;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e3).Conflicts) yield return c;
                    if (e4 is ImHashMapEntry<K, V>v4) yield return v4;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e4).Conflicts) yield return c;
                    if (p  is ImHashMapEntry<K, V>v5) yield return v5;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)p).Conflicts)  yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf5Plus1Plus1 l511)
                {
                    var l = l511.L.L;
                    ImHashMap234<K, V>.Entry 
                        e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4, p = l511.Plus, pp = l511.L.Plus, swap = null;
                    var h = pp.Hash;
                    if (h < e4.Hash)
                    {
                        swap = e4; e4 = pp; pp = swap;
                        if (h < e3.Hash)
                        {
                            swap = e3; e3 = e4; e4 = swap;
                            if (h < e2.Hash)
                            {
                                swap = e2; e2 = e3; e3 = swap;
                                if (h < e1.Hash)
                                {
                                    swap = e1; e1 = e2; e2 = swap;
                                    if (h < e0.Hash)
                                    {
                                        swap = e0; e0 = e1; e1 = swap;
                                    }
                                }
                            }
                        }
                    }

                    h = p.Hash;
                    if (h < pp.Hash)
                    {
                        swap = pp; pp = p; p = swap;
                        if (h < e4.Hash)
                        {
                            swap = e4; e4 = pp; pp = swap;
                            if (h < e3.Hash)
                            {
                                swap = e3; e3 = e4; e4 = swap;
                                if (h < e2.Hash)
                                {
                                    swap = e2; e2 = e3; e3 = swap;
                                    if (h < e1.Hash)
                                    {
                                        swap = e1; e1 = e2; e2 = swap;
                                        if (h < e0.Hash)
                                        {
                                            swap = e0; e0 = e1; e1 = swap;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (e0 is ImHashMapEntry<K, V>v0) yield return v0;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e0).Conflicts) yield return c;
                    if (e1 is ImHashMapEntry<K, V>v1) yield return v1;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e1).Conflicts) yield return c;
                    if (e2 is ImHashMapEntry<K, V>v2) yield return v2;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e2).Conflicts) yield return c;
                    if (e3 is ImHashMapEntry<K, V>v3) yield return v3;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e3).Conflicts) yield return c;
                    if (e4 is ImHashMapEntry<K, V>v4) yield return v4;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)e4).Conflicts) yield return c;
                    if (pp is ImHashMapEntry<K, V>v5) yield return v5;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)pp).Conflicts) yield return c;
                    if (p  is ImHashMapEntry<K, V>v6) yield return v6;
                    else foreach (var c in ((HashConflictKeyValuesEntry<K, V>)p).Conflicts)  yield return c;
                }

                if (count == 0)
                    break; // we yield the leaf and there is nothing in stack - we are DONE!

                var pb2 = (ImHashMap234<K, V>.Branch2)parents.Get(--count); // otherwise get the parent
                if (pb2.MidEntry is ImHashMapEntry<K, V>v) 
                    yield return v;
                else if (pb2.MidEntry != null) foreach (var c in ((HashConflictKeyValuesEntry<K, V>)pb2.MidEntry).Conflicts) 
                    yield return c;

                map = pb2.Right;
            }
        }

        /// <summary>Enumerates all the map entries in the hash order.
        /// `parents` parameter allow to reuse the stack memory used for traversal between multiple enumerates.
        /// So you may pass the empty `parents` into the first `Enumerate` and then keep passing the same `parents` into the subsequent `Enumerate` calls</summary>
        public static IEnumerable<ImHashMapEntry<V>> Enumerate<V>(this ImHashMap234<int, V> map, Stack<ImHashMap234<int, V>> parents = null)
        {
            if (map == ImHashMap234<int, V>.Empty)
                yield break;
            if (map is ImHashMapEntry<V> v)
            {
                yield return v;
                yield break;
            }

            var count = 0;
            while (true)
            {
                if (map is ImHashMap234<int, V>.Branch2 b2)
                {
                    if (parents == null)
                        parents = new Stack<ImHashMap234<int, V>>();
                    parents.Push(map, count++);
                    map = b2.Left;
                    continue;
                }
                
                if (map is ImHashMap234<int, V>.Leaf2 l2)
                {
                    yield return (ImHashMapEntry<V>)l2.Entry0;
                    yield return (ImHashMapEntry<V>)l2.Entry1;
                }
                else if (map is ImHashMap234<int, V>.Leaf2Plus1 l21)
                {
                    var p  = l21.Plus;
                    var ph = p.Hash;
                    var l  = l21.L;
                    ImHashMap234<int, V>.Entry e0 = l.Entry0, e1 = l.Entry1, swap = null;
                    if (ph < e1.Hash)
                    {
                        swap = e1; e1 = p; p = swap;
                        if (ph < e0.Hash)
                        {
                            swap = e0; e0 = e1; e1 = swap;
                        }
                    }

                    yield return (ImHashMapEntry<V>)e0;
                    yield return (ImHashMapEntry<V>)e1;
                    yield return (ImHashMapEntry<V>)p ;
                }
                else if (map is ImHashMap234<int, V>.Leaf2Plus1Plus1 l211)
                {
                    var p  = l211.Plus;
                    var pp = l211.L.Plus;
                    var ph = pp.Hash;
                    var l  = l211.L.L;
                    ImHashMap234<int, V>.Entry e0 = l.Entry0, e1 = l.Entry1, swap = null;
                    if (ph < e1.Hash)
                    {
                        swap = e1; e1 = pp; pp = swap;
                        if (ph < e0.Hash)
                        {
                            swap = e0; e0 = e1; e1 = swap;
                        }
                    }

                    ph = p.Hash;
                    if (ph < pp.Hash)
                    {
                        swap = pp; pp = p; p = swap;
                        if (ph < e1.Hash)
                        {
                            swap = e1; e1 = pp; pp = swap;
                            if (ph < e0.Hash)
                            {
                                swap = e0; e0 = e1; e1 = swap;
                            }
                        }
                    }

                    yield return (ImHashMapEntry<V>)e0;
                    yield return (ImHashMapEntry<V>)e1;
                    yield return (ImHashMapEntry<V>)pp;
                    yield return (ImHashMapEntry<V>)p ;
                }
                else if (map is ImHashMap234<int, V>.Leaf5 l5)
                {
                    yield return (ImHashMapEntry<V>)l5.Entry0;
                    yield return (ImHashMapEntry<V>)l5.Entry1;
                    yield return (ImHashMapEntry<V>)l5.Entry2;
                    yield return (ImHashMapEntry<V>)l5.Entry3;
                    yield return (ImHashMapEntry<V>)l5.Entry4;
                }
                else if (map is ImHashMap234<int, V>.Leaf5Plus1 l51)
                {
                    var p  = l51.Plus;
                    var ph = p.Hash;
                    var l  = l51.L;
                    ImHashMap234<int, V>.Entry e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4, swap = null;
                    if (ph < e4.Hash)
                    {
                        swap = e4; e4 = p; p = swap;
                        if (ph < e3.Hash)
                        {
                            swap = e3; e3 = e4; e4 = swap;
                            if (ph < e2.Hash)
                            {
                                swap = e2; e2 = e3; e3 = swap;
                                if (ph < e1.Hash)
                                {
                                    swap = e1; e1 = e2; e2 = swap;
                                    if (ph < e0.Hash)
                                    {
                                        swap = e0; e0 = e1; e1 = swap;
                                    }
                                }
                            }
                        }
                    }

                    yield return (ImHashMapEntry<V>)e0;
                    yield return (ImHashMapEntry<V>)e1;
                    yield return (ImHashMapEntry<V>)e2;
                    yield return (ImHashMapEntry<V>)e3;
                    yield return (ImHashMapEntry<V>)e4;
                    yield return (ImHashMapEntry<V>)p ;
                }
                else if (map is ImHashMap234<int, V>.Leaf5Plus1Plus1 l511)
                {
                    var l = l511.L.L;
                    ImHashMap234<int, V>.Entry 
                        e0 = l.Entry0, e1 = l.Entry1, e2 = l.Entry2, e3 = l.Entry3, e4 = l.Entry4, p = l511.Plus, pp = l511.L.Plus, swap = null;
                    var h = pp.Hash;
                    if (h < e4.Hash)
                    {
                        swap = e4; e4 = pp; pp = swap;
                        if (h < e3.Hash)
                        {
                            swap = e3; e3 = e4; e4 = swap;
                            if (h < e2.Hash)
                            {
                                swap = e2; e2 = e3; e3 = swap;
                                if (h < e1.Hash)
                                {
                                    swap = e1; e1 = e2; e2 = swap;
                                    if (h < e0.Hash)
                                    {
                                        swap = e0; e0 = e1; e1 = swap;
                                    }
                                }
                            }
                        }
                    }

                    h = p.Hash;
                    if (h < pp.Hash)
                    {
                        swap = pp; pp = p; p = swap;
                        if (h < e4.Hash)
                        {
                            swap = e4; e4 = pp; pp = swap;
                            if (h < e3.Hash)
                            {
                                swap = e3; e3 = e4; e4 = swap;
                                if (h < e2.Hash)
                                {
                                    swap = e2; e2 = e3; e3 = swap;
                                    if (h < e1.Hash)
                                    {
                                        swap = e1; e1 = e2; e2 = swap;
                                        if (h < e0.Hash)
                                        {
                                            swap = e0; e0 = e1; e1 = swap;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    yield return (ImHashMapEntry<V>)e0;
                    yield return (ImHashMapEntry<V>)e1;
                    yield return (ImHashMapEntry<V>)e2;
                    yield return (ImHashMapEntry<V>)e3;
                    yield return (ImHashMapEntry<V>)e4;
                    yield return (ImHashMapEntry<V>)pp;
                    yield return (ImHashMapEntry<V>)p ;
                }

                if (count == 0)
                    break; // we yield the leaf and there is nothing in stack - we are DONE!

                var pb2 = (ImHashMap234<int, V>.Branch2)parents.Get(--count); // otherwise get the parent
                yield return (ImHashMapEntry<V>)pb2.MidEntry;

                map = pb2.Right;
            }
        }

        /// <summary>Lookup for the value by the key using the hash and checking the key with the `object.Equals` for equality, 
        /// returns the default `V` if hash, key are not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V> map, int hash, K key)
        {
            var e = map.GetEntryOrDefault(hash);
            if (e is ImHashMapEntry<K, V>v)
            {
                if (v.Key.Equals(key))
                    return v.Value;
            }
            else if (e is HashConflictKeyValuesEntry<K, V> c)
            {
                foreach (var x in c.Conflicts) 
                    if (x.Key.Equals(key))
                        return x.Value;
            }
            return default(V);
        }

        /// <summary>Lookup for the value by key using its hash and checking the key with the `object.Equals` for equality, 
        /// returns the default `V` if hash, key are not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V> map, K key) =>
            map.GetValueOrDefault(key.GetHashCode(), key);

        /// <summary>Lookup for the value by hash, returns the default `V` if hash is not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImHashMap234<int, V> map, int hash) 
        {
            var entry = map.GetEntryOrDefault(hash);
            return entry != null ? ((ImHashMapEntry<V>)entry).Value : default(V);
        }

        /// <summary>Lookup for the value by the key using the hash and checking the key with the `object.ReferenceEquals` for equality,
        ///  returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefaultReferenceEqual<K, V>(this ImHashMap234<K, V> map, int hash, K key) where K : class
        {
            var e = map.GetEntryOrDefault(hash);
            if (e is ImHashMapEntry<K, V>v)
            {
                if (v.Key == key)
                    return v.Value;
            }
            else if (e is HashConflictKeyValuesEntry<K, V> c)
            {
                foreach (var x in c.Conflicts) 
                    if (x.Key == key)
                        return x.Value;
            }
            return default(V);
        }

        /// <summary>Lookup for the value by the key using the hash and checking the key with the `object.Equals` for equality,
        /// returns the `true` and the found value or the `false` otherwise</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap234<K, V> map, int hash, K key, out V value)
        {
            var e = map.GetEntryOrDefault(hash);
            if (e is ImHashMapEntry<K, V>v)
            {
                if (v.Key.Equals(key))
                {
                    value = v.Value;
                    return true;
                }
            }
            else if (e is HashConflictKeyValuesEntry<K, V> c)
            {
                foreach (var x in c.Conflicts) 
                    if (x.Key.Equals(key)) 
                    {
                        value = x.Value;
                        return true;
                    }
            }

            value = default(V);
            return false;
        }

        /// <summary>Lookup for the value by the key using the hash and checking the key with the `object.ReferenceEquals`, 
        /// returns the `true` and the found value or the `false` otherwise</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFindReferenceEqual<K, V>(this ImHashMap234<K, V> map, int hash, K key, out V value) where K : class
        {
            var e = map.GetEntryOrDefault(hash);
            if (e is ImHashMapEntry<K, V>v)
            {
                if (v.Key == key)
                {
                    value = v.Value;
                    return true;
                }
            }
            else if (e is HashConflictKeyValuesEntry<K, V> c)
            {
                foreach (var x in c.Conflicts) 
                    if (x.Key == key) 
                    {
                        value = x.Value;
                        return true;
                    }
            }

            value = default(V);
            return false;
        }

        /// <summary>Lookup for the value by the key using its hash and checking the key with the `object.Equals` for equality,
        /// returns the `true` and the found value or the `false` otherwise</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap234<K, V> map, K key, out V value) =>
            map.TryFind(key.GetHashCode(), key, out value);

        /// <summary>Lookup for the value by its hash, returns the `true` and the found value or the `false` otherwise</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImHashMap234<int, V> map, int hash, out V value)
        {
            if (map is ImHashMapEntry<V> v && v.Hash == hash)
            {
                value = v.Value;
                return true;
            }

            var e = map.GetEntryOrDefault(hash);
            if (e != null)
            {
                value = ((ImHashMapEntry<V>)e).Value;
                return true;
            }
            value = default(V);
            return false;
        }

        /// <summary>Adds or updates (no mutation) the map with value by the passed hash and key, always returning the NEW map!</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrUpdate<K, V>(this ImHashMap234<K, V> map, int hash, K key, V value) 
        {
            var newEntry = new ImHashMapEntry<K, V>(hash, key, value);
            if (map == ImHashMap234<K, V>.Empty)
                return newEntry;

            var oldEntryOrMap = map.AddOrGetEntry(hash, newEntry);
            if (oldEntryOrMap is ImHashMapEntry<K, V>.Entry oldEntry)
                return map.ReplaceEntry(hash, oldEntry, UpdateEntry(oldEntry, newEntry));

            return oldEntryOrMap;
        }

        private static ImHashMap234<K, V>.Entry UpdateEntry<K, V>(ImHashMap234<K, V>.Entry oldEntry, ImHashMapEntry<K, V> newEntry)
        {
            if (oldEntry is ImHashMapEntry<K, V> kv)
                return kv.Key.Equals(newEntry.Key) ? newEntry : (ImHashMap234<K, V>.Entry)new HashConflictKeyValuesEntry<K, V>(oldEntry.Hash, kv, newEntry);

            var hkv = (HashConflictKeyValuesEntry<K, V>)oldEntry;
            var key = newEntry.Key;
            var cs = hkv.Conflicts;
            var n = cs.Length;
            var i = n - 1;
            while (i != -1 && !key.Equals(cs[i].Key)) --i;
            var newConflicts = new ImHashMapEntry<K, V>[i != -1 ? n : n + 1];
            Array.Copy(cs, 0, newConflicts, 0, n);
            newConflicts[i != -1 ? i : n] = newEntry;

            return new HashConflictKeyValuesEntry<K, V>(oldEntry.Hash, newConflicts);
        }

        /// <summary>Adds or updates (no mutation) the map with value by the passed hash and key, always returning the NEW map!</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<int, V> AddOrUpdate<V>(this ImHashMap234<int, V> map, int hash, V value)
        {
            var newEntry = new ImHashMapEntry<V>(hash, value);
            if (map == ImHashMap234<int, V>.Empty)
                return newEntry;

            var oldEntryOrMap = map.AddOrGetEntry(hash, newEntry);
            if (oldEntryOrMap is ImHashMapEntry<int, V>.Entry oldEntry)
                return map.ReplaceEntry(hash, oldEntry, newEntry); // todo: @perf here we have a chance to compare the old and the new value and prevent the updated if the values are equal

            return oldEntryOrMap;
        }

        /// <summary>Adds or updates (no mutation) the map with value by the passed key, always returning the NEW map!</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrUpdate<K, V>(this ImHashMap234<K, V> map, K key, V value) =>
            map.AddOrUpdate(key.GetHashCode(), key, value);

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrKeep<K, V>(this ImHashMap234<K, V> map, int hash, K key, V value) 
        {
            var newEntry = new ImHashMapEntry<K, V>(hash, key, value); // todo: @perf newEntry may not be needed here - consider the pooling of entries here
            if (map == ImHashMap234<K, V>.Empty)
                return newEntry;

            var oldEntryOrMap = map.AddOrGetEntry(hash, newEntry);
            if (oldEntryOrMap is ImHashMapEntry<K, V>.Entry oldEntry)
            {
                var e = KeepOrAddEntry(oldEntry, newEntry);
                return e == oldEntry ? map : map.ReplaceEntry(hash, oldEntry, e);
            }

            return oldEntryOrMap;
        }

        private static ImHashMap234<K, V>.Entry KeepOrAddEntry<K, V>(ImHashMap234<K, V>.Entry oldEntry, ImHashMapEntry<K, V> newEntry)
        {
            if (oldEntry is ImHashMapEntry<K, V> kv)
                return kv.Key.Equals(newEntry.Key) ? oldEntry : (ImHashMap234<K, V>.Entry)new HashConflictKeyValuesEntry<K, V>(oldEntry.Hash, kv, newEntry);

            var hkv = (HashConflictKeyValuesEntry<K, V>)oldEntry;
            var key  = newEntry.Key;
            var cs = hkv.Conflicts;
            var n = cs.Length;
            var i = n - 1;
            while (i != -1 && !key.Equals(cs[i].Key)) --i;
            if (i != -1) // return the existing map
                return oldEntry;

            var newConflicts = new ImHashMapEntry<K, V>[n + 1];
            Array.Copy(cs, 0, newConflicts, 0, n);
            newConflicts[n] = newEntry;

            return new HashConflictKeyValuesEntry<K, V>(oldEntry.Hash, newConflicts);
        }

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrKeep<K, V>(this ImHashMap234<K, V> map, K key, V value) => 
            map.AddOrKeep(key.GetHashCode(), key, value);

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the hash is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<int, V> AddOrKeep<V>(this ImHashMap234<int, V> map, int hash, V value) 
        {
            var newEntry = new ImHashMapEntry<V>(hash, value); // todo: @perf newEntry may not be needed here - consider the pooling of entries here
            if (map == ImHashMap234<int, V>.Empty)
                return newEntry;

            var oldEntryOrMap = map.AddOrGetEntry(hash, newEntry);
            return oldEntryOrMap is ImHashMapEntry<int, V>.Entry ? map : oldEntryOrMap;
        }

        [MethodImpl((MethodImplOptions)256)]
        private static ImHashMapEntry<K, V> GetEntryOrDefault<K, V>(this ImHashMap234<K, V> map, int hash, K key)
        {
            var e = map.GetEntryOrDefault(hash);

            if (e is ImHashMapEntry<K, V>v)
                return v.Key.Equals(key) ? v : null;

            if (e is HashConflictKeyValuesEntry<K, V> c)
                foreach (var x in c.Conflicts) 
                    if (x.Key.Equals(key))
                        return x;

            return null;
        }

        /// <summary>Returns the new map without the specified hash and key (if found) or returns the same map otherwise</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> Remove<K, V>(this ImHashMap234<K, V> map, int hash, K key)
        {
            var entry = map.GetEntryOrDefault(hash, key);
            return entry != null ? map.RemoveEntry(hash, entry, ImHashMap234<K, V>.DoRemove) : map;
        }

        /// <summary>Returns the new map without the specified hash (if found) or returns the same map otherwise</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<int, V> Remove<V>(this ImHashMap234<int, V> map, int hash)
        {
            var entry = map.GetEntryOrDefault(hash);
            return entry != null ? map.RemoveEntry(hash, (ImHashMapEntry<V>)entry, ImHashMap234<int, V>.DoRemove) : map;
        }

        /// <summary>Returns the new map without the specified hash and key (if found) or returns the same map otherwise</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> Remove<K, V>(this ImHashMap234<K, V> map, K key) =>
            map == ImHashMap234<K, V>.Empty ? map : map.Remove(key.GetHashCode(), key); // it make sense to have the empty map condition here to prevent the probably costly `GetHashCode()` for the empty map.
    }

    /// <summary>
    /// The fixed array of maps (partitions) where the key first (lower) bits are used to locate the partion to lookup into.
    /// Note: The partition array is NOT immutable and operates by swapping the updated partition with the new one.
    /// The number of partitions may be specified by user or you can use the default number 16.
    /// The default number 16 was selected to be not so big to pay for the few items and not so small to diminish the use of partitions.
    /// </summary>
    public static class PartitionedHashMap234
    {
        /// <summary>The default number of partions</summary>
        public const int PARTITION_COUNT_POWER_OF_TWO = 16;

        /// <summary>The default mask to partition the key</summary>
        public const int PARTITION_HASH_MASK = PARTITION_COUNT_POWER_OF_TWO - 1;

        /// <summary>Creates the new collection with the empty partions</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V>[] CreateEmpty<K, V>(int partionCountOfPowerOfTwo = PARTITION_COUNT_POWER_OF_TWO)
        {
            var parts = new ImHashMap234<K, V>[partionCountOfPowerOfTwo];
            for (var i = 0; i < parts.Length; ++i)
                parts[i] = ImHashMap234<K, V>.Empty;
            return parts;
        }

        /// <summary>Lookup for the value by the key using the hash code and checking the key with the `object.Equals` for equality,
        /// returns the `true` and the found value or the `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, out V value, int partHashMask = PARTITION_HASH_MASK)
        {
            var p = parts[hash & partHashMask];
            if (p != null) 
                return p.TryFind(hash, key, out value);
            value = default(V);
            return false;
        }

        /// <summary>Lookup for the value by the key using its hash code and checking the key with the `object.Equals` for equality,
        /// returns the `true` and the found value or the `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap234<K, V>[] parts, K key, out V value, int partHashMask = PARTITION_HASH_MASK) =>
            parts.TryFind(key.GetHashCode(), key, out value, partHashMask);

        /// <summary>Lookup for the value by the key using the hash code and checking the key with the `object.ReferenceEquals` for equality,
        /// returns the `true` and the found value or the `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFindReferenceEqual<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, out V value, int partHashMask = PARTITION_HASH_MASK)
            where K : class
        {
            var p = parts[hash & partHashMask];
            if (p != null) 
                return p.TryFindReferenceEqual(hash, key, out value);
            value = default(V);
            return false;
        }

        /// <summary>Lookup for the value by the key using the hash and checking the key with the `object.Equals` for equality, 
        /// returns the default `V` if hash, key are not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, int partHashMask = PARTITION_HASH_MASK)
        {
            var p = parts[hash & partHashMask];
            return p != null ? p.GetValueOrDefault(hash, key) : default(V);
        }

        /// <summary>Lookup for the value by the key using its hash and checking the key with the `object.Equals` for equality, 
        /// returns the default `V` if hash, key are not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V>[] parts, K key, int partHashMask = PARTITION_HASH_MASK) =>
            parts.GetValueOrDefault(key.GetHashCode(), key, partHashMask);

        /// <summary>Lookup for the value by the key using the hash and checking the key with the `object.ReferenceEquals` for equality, 
        /// returns the default `V` if hash, key are not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefaultReferenceEqual<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, int partHashMask = PARTITION_HASH_MASK) where K : class
        {
            var p = parts[hash & partHashMask];
            return p != null ? p.GetValueOrDefaultReferenceEqual(hash, key) : default(V);
        }

        /// <summary>Lookup for the value by the key using its hash and checking the key with the `object.ReferenceEquals` for equality, 
        /// returns the default `V` if hash, key are not found.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefaultReferenceEqual<K, V>(this ImHashMap234<K, V>[] parts, K key, int partHashMask = PARTITION_HASH_MASK) where K : class => 
            parts.GetValueOrDefaultReferenceEqual(key.GetHashCode(), key, partHashMask);

        /// <summary>Returns the SAME partitioned maps array instance but with the NEW added or updated partion</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static void AddOrUpdate<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, V value, int partHashMask = PARTITION_HASH_MASK)
        {
            ref var part = ref parts[hash & partHashMask];
            var p = part;
            if (Interlocked.CompareExchange(ref part, p.AddOrUpdate(hash, key, value), p) != p)
                RefAddOrUpdatePart(ref part, hash, key, value);
        }

        /// <summary>Returns the SAME partitioned maps array instance but with the NEW added or updated partion</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static void AddOrUpdate<K, V>(this ImHashMap234<K, V>[] parts, K key, V value, int partHashMask = PARTITION_HASH_MASK) =>
            parts.AddOrUpdate(key.GetHashCode(), key, value, partHashMask);

        /// <summary>Updates the ref to the part with the new version and retries if the someone changed the part in between</summary>
        public static void RefAddOrUpdatePart<K, V>(ref ImHashMap234<K, V> part, int hash, K key, V value) =>
            Ref.Swap(ref part, hash, key, value, (x, h, k, v) => x.AddOrUpdate(h, k, v));

        /// <summary>Returns the SAME partitioned maps array instance but with the NEW added or the same kept partion</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static void AddOrKeep<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, V value, int partHashMask = PARTITION_HASH_MASK)
        {
            ref var part = ref parts[hash & partHashMask];
            var p = part;
            if (Interlocked.CompareExchange(ref part, p.AddOrKeep(hash, key, value), p) != p)
                RefAddOrKeepPart(ref part, hash, key, value);
        }

        /// <summary>Returns the SAME partitioned maps array instance but with the NEW added or the same kept partion</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static void AddOrKeep<K, V>(this ImHashMap234<K, V>[] parts, K key, V value, int partHashMask = PARTITION_HASH_MASK) =>
            parts.AddOrKeep(key.GetHashCode(), key, value, partHashMask);

        /// <summary>Updates the ref to the part with the new version and retries if the someone changed the part in between</summary>
        public static void RefAddOrKeepPart<K, V>(ref ImHashMap234<K, V> part, int hash, K key, V value) =>
            Ref.Swap(ref part, hash, key, value, (x, h, k, v) => x.AddOrUpdate(h, k, v));

        /// <summary>Enumerates all the partitions map entries in the hash order.
        /// `parents` parameter allow to reuse the stack memory used for traversal between multiple enumerates.
        /// So you may pass the empty `parents` into the first `Enumerate` and then keep passing the same `parents` into the subsequent `Enumerate` calls</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static IEnumerable<ImHashMapEntry<K, V>> Enumerate<K, V>(this ImHashMap234<K, V>[] parts, ImHashMap234.Stack<ImHashMap234<K, V>> parents = null)
        {
            if (parents == null)
                parents = new ImHashMap234.Stack<ImHashMap234<K, V>>();
            foreach (var map in parts) 
            {
                if (map == ImHashMap234<K, V>.Empty)
                    continue;
                foreach (var entry in map.Enumerate(parents))
                    yield return entry;
            }
        }
    }
}