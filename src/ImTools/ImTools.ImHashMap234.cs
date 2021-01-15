
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Experimental
{
    /// <summary>The base class for the tree leafs and branches, also defines the Empty tree</summary>
    public class ImHashMap234<K, V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImHashMap234<K, V> Empty = new ImHashMap234<K, V>();

        /// <summary>Hide the constructor to prevent the multiple Empty trees creation</summary>
        protected ImHashMap234() { } // todo: @perf - does it hurt the perf or the call to the empty constructor is erased?

        /// Pretty-prints
        public override string ToString() 
        {
#if DEBUG
            // for debug purposes we just output the first N hashes in array
            const int outputCount = 101;
            var itemsInHashOrder = this.Enumerate().Take(outputCount).Select(x => x.Hash).ToList();
            return $"new int[{(itemsInHashOrder.Count >= 100 ? ">=" : "") + itemsInHashOrder.Count}] {{" + string.Join(", ", itemsInHashOrder) + "}";
#else
            return "empty " + typeof(ImHashMap234<K, V>).Name;
#endif
        }

        /// <summary>Lookup for the entry, if not found returns `null`</summary>
        public virtual Entry GetEntryOrDefault(int hash) => null;

        /// <summary>Produces the new or updated map with the new entry</summary>
        public virtual ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry) => entry;

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        public virtual ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry) => entry;

        /// <summary>Returns the map without the entry with the specified hash and key if it is found in the map</summary>
        public virtual ImHashMap234<K, V> RemoveEntry(int hash, K key) => this;

        /// <summary>The base entry for the Value and for the ConflictingValues entries, contains the Hash and Key</summary>
        public abstract class Entry : ImHashMap234<K, V>
        {
            /// <summary>The Hash</summary>
            public readonly int Hash;

            /// <summary>Constructs the entry with the default Key</summary>
            protected Entry(int hash) => Hash = hash; // todo: @perf think of the way to remove the base Entry constructor call - move to the inheriting classes, e.g. ValueEntry

            /// <inheritdoc />
            public sealed override Entry GetEntryOrDefault(int hash) => hash == Hash ? this : null;

            internal abstract Entry Update(KeyValueEntry entry);
            internal abstract Entry KeepOrUpdate(KeyValueEntry entry);
            /// <summary>returns null if entry is removed completely or modified entry, or the original entry if nothing is removed </summary>
            internal abstract Entry TryRemove(K key);

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry) =>
                hash > Hash ? new Leaf2(this, entry) :
                hash < Hash ? new Leaf2(entry, this) :
                (ImHashMap234<K, V>)Update(entry);

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry) =>
                hash > Hash ? new Leaf2(this, entry) :
                hash < Hash ? new Leaf2(entry, this) :
                (ImHashMap234<K, V>)KeepOrUpdate(entry);

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> RemoveEntry(int hash, K key) =>
                hash == Hash ? TryRemove(key) ?? Empty : this;
        }

        internal sealed class RemovedEntry : Entry 
        {
            public RemovedEntry(int hash) : base(hash) {}
            internal override Entry Update(KeyValueEntry entry) => entry;
            internal override Entry KeepOrUpdate(KeyValueEntry entry) => entry;
            internal override Entry TryRemove(K key) => this;

            public override string ToString() => "[" + Hash + "]:removed-entry";
        }

        /// <summary>Entry containing the Value</summary>
        public sealed class KeyValueEntry : Entry
        {
            /// <summary>The Key</summary>
            public readonly K Key;
            /// <summary>The value. May be modified if you need the Ref{V} semantics</summary>
            public V Value;
            /// <summary>Constructs the entry with the default value</summary>
            public KeyValueEntry(int hash, K key) : base(hash) => Key = key;
            /// <summary>Constructs the entry with the key and value</summary>
            public KeyValueEntry(int hash, K key, V value) :  base(hash)
            { 
                Key   = key;
                Value = value;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "[" + Hash + "]" + Key + ":" + Value;
#endif

            internal override Entry Update(KeyValueEntry entry) => 
                Key.Equals(entry.Key) ? entry : (Entry)new HashConflictKeyValuesEntry(Hash, this, entry);

            internal override Entry KeepOrUpdate(KeyValueEntry entry) => 
                Key.Equals(entry.Key) ? this : (Entry)new HashConflictKeyValuesEntry(Hash, this, entry);

            internal override Entry TryRemove(K key) =>
                Key.Equals(key) ? null : this;
        }

        /// <summary>Entry containing the Array of conflicting Value entries.</summary>
        public sealed class HashConflictKeyValuesEntry : Entry
        {
            /// <summary>The 2 and more conflicts.</summary>
            public KeyValueEntry[] Conflicts;

            /// <summary>Constructs the entry with the key and value</summary>
            public HashConflictKeyValuesEntry(int hash, params KeyValueEntry[] conflicts) : base(hash) => Conflicts = conflicts;

#if !DEBUG
            /// <inheritdoc />
            public override string ToString()
            {
                var sb = new System.Text.StringBuilder();
                foreach (var x in Conflicts) 
                    sb.Append(x.ToString()).Append("; ");
                return sb.ToString();
            }
#endif

            internal override Entry Update(KeyValueEntry entry) 
            {
                var key = entry.Key;

                var cs = Conflicts;
                var n = cs.Length;
                var i = n - 1;
                while (i != -1 && !key.Equals(cs[i].Key)) --i;

                KeyValueEntry[] newConflicts;
                if (i != -1) // update the found (existing) conflicted value
                {
                    newConflicts = new KeyValueEntry[n];
                    Array.Copy(cs, 0, newConflicts, 0, n);
                    newConflicts[i] = entry;
                }
                else // add the new conflicting value
                {
                    newConflicts = new KeyValueEntry[n + 1];
                    Array.Copy(cs, 0, newConflicts, 0, n);
                    newConflicts[n] = entry;
                }

                return new HashConflictKeyValuesEntry(Hash, newConflicts);
            }

            internal override Entry KeepOrUpdate(KeyValueEntry entry)
            {
                var key = entry.Key;

                var cs = Conflicts;
                var n = cs.Length;
                var i = n - 1;
                while (i != -1 && !key.Equals(cs[i].Key)) --i;

                KeyValueEntry[] newConflicts;
                if (i != -1) // return existing map
                    return this;

                newConflicts = new KeyValueEntry[n + 1];
                Array.Copy(cs, 0, newConflicts, 0, n);
                newConflicts[n] = entry;

                return new HashConflictKeyValuesEntry(Hash, newConflicts);
            }

            internal override Entry TryRemove(K key) 
            {
                var cs = Conflicts;
                var n = cs.Length;
                var i = n - 1;
                while (i != -1 && !key.Equals(cs[i].Key)) --i;
                if (i != -1)
                {
                    if (n == 2)
                        return i == 0 ? cs[1] : cs[0];

                    var newConflicts = new KeyValueEntry[n -= 1]; // the new n is less by one
                    if (i > 0) // copy the 1st part
                        Array.Copy(cs, 0, newConflicts, 0, i);
                    if (i < n) // copy the 2nd part
                        Array.Copy(cs, i + 1, newConflicts, i, n - i);

                    return new HashConflictKeyValuesEntry(Hash, newConflicts);
                }

                return this;
            }
        }

        // todo: @perf we may probably split the class to Leaf2NoE1, Leaf2NoE2, Leaf2NoE1E2 and simplify the code - remove the conditions
        /// <summary>Leaf with 2 entries. Note: the entries may be null for the removed entries</summary>
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
            public override string ToString() => "leaf2{" + Entry0 + "; " + Entry1 + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                var e0 = Entry0;
                if (e0 != null && e0.Hash == hash)
                    return e0;
                var e1 = Entry1;
                if (e1 != null && e1.Hash == hash)
                    return e1;
                return null;
            }
                
            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                if (e0 == null)
                    return e1 == null ? new Leaf2(null, entry)
                        :  e1.Hash == hash ? new Leaf2(null, e1.Update(entry))
                        :  e1.Hash <  hash ? new Leaf2(entry, e1) : new Leaf2(e1, entry);

                if (e1 == null)
                    return e0.Hash == hash ? new Leaf2(e0.Update(entry), null)
                        :  e0.Hash <  hash ? new Leaf2(e0, entry) : new Leaf2(entry, e0);

                return
                    hash > e1.Hash                   ? new Leaf3(e0, e1, entry) :
                    hash < e0.Hash                   ? new Leaf3(entry, e0, e1) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf3(e0, entry, e1) :
                    hash == e0.Hash   ? new Leaf2(e0.Update(entry), e1) :
                    (ImHashMap234<K, V>)new Leaf2(e0, e1.Update(entry));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;

                if (e0 == null)
                    return e1 == null ? new Leaf2(null, entry)
                        :  e1.Hash == hash ? ((e1 = e1.KeepOrUpdate(entry)) == Entry1 ? this : new Leaf2(null, e1))
                        :  e1.Hash <  hash ? new Leaf2(entry, e1) : new Leaf2(e1, entry);

                if (e1 == null)
                    return e0.Hash == hash ? ((e0 = e0.KeepOrUpdate(entry)) == Entry0 ? this : new Leaf2(e0, null))
                        :  e0.Hash <  hash ? new Leaf2(e0, entry) : new Leaf2(entry, e0);

                return
                    hash > e1.Hash                   ? new Leaf3(e0, e1, entry) :
                    hash < e0.Hash                   ? new Leaf3(entry, e0, e1) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf3(e0, entry, e1) :
                    hash == e0.Hash ?   ((e0 = e0.KeepOrUpdate(entry)) == Entry0 ? this : new Leaf2(e0, e1)) :
                    (ImHashMap234<K, V>)((e1 = e1.KeepOrUpdate(entry)) == Entry1 ? this : new Leaf2(e0, e1));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                // e0 or e1 may be null as the result and its Ok for the removed entry
                return hash == e0.Hash ? ((e0 = e0.TryRemove(key)) == Entry0 ? this : new Leaf2(e0, e1))
                    :  hash == e1.Hash ? ((e1 = e1.TryRemove(key)) == Entry1 ? this : new Leaf2(e0, e1))
                    :  this;
            }
        }

        /// <summary>Leaf with 3 entries</summary>
        public sealed class Leaf3 : ImHashMap234<K, V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Middle entry</summary>
            public readonly Entry Entry1;
            /// <summary>Right entry</summary>
            public readonly Entry Entry2;

            /// <summary>Constructs the leaf</summary>
            public Leaf3(Entry e0, Entry e1, Entry e2)
            {
                Debug.Assert(e0.Hash < e1.Hash);
                Debug.Assert(e1.Hash < e2.Hash);
                Entry0 = e0;
                Entry1 = e1;
                Entry2 = e2;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf3{" + Entry0 + "; " + Entry1 + "; " + Entry2 + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash == Entry0.Hash ? Entry0 :
                hash == Entry1.Hash ? Entry1 :
                hash == Entry2.Hash ? Entry2 :
                null;

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry) =>
                hash == Entry0.Hash ? new Leaf3(Entry0.Update(entry), Entry1, Entry2) :
                hash == Entry1.Hash ? new Leaf3(Entry0, Entry1.Update(entry), Entry2) :
                hash == Entry2.Hash ? new Leaf3(Entry0, Entry1, Entry2.Update(entry)) :
                (ImHashMap234<K, V>)new Leaf3Plus1(entry, this);

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                return
                    hash == e0.Hash ? ((e0 = e0.KeepOrUpdate(entry)) == Entry0 ? this : new Leaf3(e0, e1, e2)) :
                    hash == e1.Hash ? ((e1 = e1.KeepOrUpdate(entry)) == Entry1 ? this : new Leaf3(e0, e1, e2)) :
                    hash == e2.Hash ? ((e2 = e2.KeepOrUpdate(entry)) == Entry2 ? this : new Leaf3(e0, e1, e2)) :
                    (ImHashMap234<K, V>)new Leaf3Plus1(entry, this);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                if (hash == e0.Hash)
                    return (e0 = e0.TryRemove(key)) == Entry0 ? this : e0 == null ? new Leaf2(e1, e2) : (ImHashMap234<K, V>)new Leaf3(e0, e1, e2);
                if (hash == e1.Hash)
                    return (e1 = e1.TryRemove(key)) == Entry1 ? this : e1 == null ? new Leaf2(e0, e2) : (ImHashMap234<K, V>)new Leaf3(e0, e1, e2);
                if (hash == e2.Hash)
                    return (e2 = e2.TryRemove(key)) == Entry2 ? this : e2 == null ? new Leaf2(e0, e1) : (ImHashMap234<K, V>)new Leaf3(e0, e1, e2);
                return this;
            }
        }

        /// <summary>Leaf with 3 + 1 entries</summary>
        public sealed class Leaf3Plus1 : ImHashMap234<K, V>
        {
            /// <summary>Plus entry</summary>
            public readonly Entry Plus;
            /// <summary>Dangling leaf3</summary>
            public readonly Leaf3 L3;

            /// <summary>Constructs the leaf</summary>
            public Leaf3Plus1(Entry plus, Leaf3 l3)
            {
                Plus = plus;
                L3 = l3;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf3+1{" + Plus + " + " + L3 + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) 
            {
                if (hash == Plus.Hash) 
                    return Plus; 
                var l = L3;
                return 
                    hash == l.Entry0.Hash ? l.Entry0 :
                    hash == l.Entry1.Hash ? l.Entry1 :
                    hash == l.Entry2.Hash ? l.Entry2 :
                    null;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return new Leaf3Plus1(p.Update(entry), L3);

                var l = L3;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;

                if (hash > e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, e1, e2, entry);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, p, e1, e2, entry);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, e1, p, e2, entry);
                    if (ph < hash)
                        return new Leaf5(e0, e1, e2, p, entry);
                    return new Leaf5(e0, e1, e2, entry, p);
                }

                if (hash < e0.Hash) 
                {
                    if (ph < hash)
                        return new Leaf5(p, entry, e0, e1, e2);
                    if (ph < e0.Hash)
                        return new Leaf5(entry, p, e0, e1, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(entry, e0, p, e1, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(entry, e0, e1, p, e2);
                    return new Leaf5(entry, e0, e1, e2, p);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, entry, e1, e2);
                    if (ph < hash)
                        return new Leaf5(e0, p, entry, e1, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, entry, p, e1, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, entry, e1, p, e2);
                    return new Leaf5(e0, entry, e1, e2, p);
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, e1, entry, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, p, e1, entry, e2);
                    if (ph < hash)
                        return new Leaf5(e0, e1, p, entry, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, e1, entry, p, e2);
                    return new Leaf5(e0, e1, entry, e2, p);
                }

                return
                    hash == e0.Hash ? new Leaf3Plus1(p, new Leaf3(e0.Update(entry), e1, e2)) :
                    hash == e1.Hash ? new Leaf3Plus1(p, new Leaf3(e0, e1.Update(entry), e2)) :
                                      new Leaf3Plus1(p, new Leaf3(e0, e1, e2.Update(entry)));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.KeepOrUpdate(entry)) == Plus ? this : new Leaf3Plus1(p, L3);

                var l = L3;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;

                if (hash > e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, e1, e2, entry);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, p, e1, e2, entry);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, e1, p, e2, entry);
                    if (ph < hash)
                        return new Leaf5(e0, e1, e2, p, entry);
                    return new Leaf5(e0, e1, e2, entry, p);
                }

                if (hash < e0.Hash) 
                {
                    if (ph < hash)
                        return new Leaf5(p, entry, e0, e1, e2);
                    if (ph < e0.Hash)
                        return new Leaf5(entry, p, e0, e1, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(entry, e0, p, e1, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(entry, e0, e1, p, e2);
                    return new Leaf5(entry, e0, e1, e2, p);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, entry, e1, e2);
                    if (ph < hash)
                        return new Leaf5(e0, p, entry, e1, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, entry, p, e1, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, entry, e1, p, e2);
                    return new Leaf5(e0, entry, e1, e2, p);
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, e1, entry, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, p, e1, entry, e2);
                    if (ph < hash)
                        return new Leaf5(e0, e1, p, entry, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, e1, entry, p, e2);
                    return new Leaf5(e0, e1, entry, e2, p);
                }

                return
                    hash == e0.Hash ? ((e0 = e0.KeepOrUpdate(entry)) == l.Entry0 ? this : (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2))) :
                    hash == e1.Hash ? ((e1 = e1.KeepOrUpdate(entry)) == l.Entry1 ? this : (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2))) :
                                      ((e2 = e2.KeepOrUpdate(entry)) == l.Entry2 ? this : (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2)));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.TryRemove(key)) == Plus ? this : p == null ? L3 : (ImHashMap234<K, V>)new Leaf3Plus1(p, L3);

                var l = L3;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;

                if (hash == e0.Hash)
                    return (e0 = e0.TryRemove(key)) == l.Entry0 ? this : 
                        e0 != null ? (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2)) :
                        ph < e1.Hash ? new Leaf3(p, e1, e2) : 
                        ph < e2.Hash ? new Leaf3(e1, p, e2) : 
                                       new Leaf3(e1, e2, p);
                if (hash == e1.Hash)
                    return (e1 = e1.TryRemove(key)) == l.Entry1 ? this : 
                        e1 != null ? (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2)) :
                        ph < e0.Hash ? new Leaf3(p, e0, e2) : 
                        ph < e2.Hash ? new Leaf3(e0, p, e2) : 
                                       new Leaf3(e0, e2, p);
                if (hash == e2.Hash)
                    return (e2 = e2.TryRemove(key)) == l.Entry1 ? this : 
                        e2 != null ? (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2)) :
                        ph < e0.Hash ? new Leaf3(p, e0, e1) : 
                        ph < e1.Hash ? new Leaf3(e0, p, e1) : 
                                       new Leaf3(e0, e1, p);
                return this;
            }
        }

        /// <summary>Leaf with 5 entries</summary>
        public sealed class Leaf5 : ImHashMap234<K, V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Middle Left entry</summary>
            public readonly Entry Entry1;
            /// <summary>Middle entry</summary>
            public readonly Entry Entry2;
            /// <summary>Middle Right entry</summary>
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
                Entry0 = e0;
                Entry1 = e1;
                Entry2 = e2;
                Entry3 = e3;
                Entry4 = e4;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf5{" + Entry0 + "; " + Entry1 + "; " + Entry2 + "; " + Entry3 + "; " + Entry4 + "}";
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
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry) =>
                hash == Entry0.Hash ? new Leaf5(Entry0.Update(entry), Entry1, Entry2, Entry3, Entry4) :
                hash == Entry1.Hash ? new Leaf5(Entry0, Entry1.Update(entry), Entry2, Entry3, Entry4) :
                hash == Entry2.Hash ? new Leaf5(Entry0, Entry1, Entry2.Update(entry), Entry3, Entry4) :
                hash == Entry3.Hash ? new Leaf5(Entry0, Entry1, Entry2, Entry3.Update(entry), Entry4) :
                hash == Entry4.Hash ? new Leaf5(Entry0, Entry1, Entry2, Entry3, Entry4.Update(entry)) :
                (ImHashMap234<K, V>)new Leaf5Plus1(entry, this);

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                var e4 = Entry4;
                return
                    hash == e0.Hash ? ((e0 = e0.KeepOrUpdate(entry)) == Entry0 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e1.Hash ? ((e1 = e1.KeepOrUpdate(entry)) == Entry1 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e2.Hash ? ((e2 = e2.KeepOrUpdate(entry)) == Entry2 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e3.Hash ? ((e3 = e3.KeepOrUpdate(entry)) == Entry3 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e4.Hash ? ((e4 = e4.KeepOrUpdate(entry)) == Entry4 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    (ImHashMap234<K, V>)new Leaf5Plus1(entry, this);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                var e4 = Entry4;
                if (hash == e0.Hash)
                    return (e0 = e0.TryRemove(key)) == Entry0 ? this : e0 == null ? new Leaf3Plus1(e4, new Leaf3(e1, e2, e3)) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                if (hash == e1.Hash)
                    return (e1 = e1.TryRemove(key)) == Entry1 ? this : e1 == null ? new Leaf3Plus1(e4, new Leaf3(e0, e2, e3)) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                if (hash == e2.Hash)
                    return (e2 = e2.TryRemove(key)) == Entry2 ? this : e2 == null ? new Leaf3Plus1(e4, new Leaf3(e0, e1, e3)) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                if (hash == e3.Hash)
                    return (e3 = e3.TryRemove(key)) == Entry3 ? this : e3 == null ? new Leaf3Plus1(e4, new Leaf3(e0, e1, e2)) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                if (hash == e4.Hash)
                    return (e4 = e4.TryRemove(key)) == Entry4 ? this : e4 == null ? new Leaf3Plus1(e3, new Leaf3(e0, e1, e2)) : (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4);
                return this;
            }
        }

        /// <summary>Leaf with 5 entries</summary>
        public sealed class Leaf5Plus1 : ImHashMap234<K, V>
        {
            /// <summary>Plus entry</summary>
            public readonly Entry Plus;
            /// <summary>Dangling Leaf5</summary>
            public readonly Leaf5 L5;

            /// <summary>Constructs the leaf</summary>
            public Leaf5Plus1(Entry plus, Leaf5 l5)
            {
                Plus = plus;
                L5   = l5;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf5+1{" + Plus + " + " + L5 + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                if (hash == Plus.Hash) 
                    return Plus; 
                var l = L5;
                return 
                    hash == l.Entry0.Hash ? l.Entry0 :
                    hash == l.Entry1.Hash ? l.Entry1 :
                    hash == l.Entry2.Hash ? l.Entry2 :
                    hash == l.Entry3.Hash ? l.Entry3 :
                    hash == l.Entry4.Hash ? l.Entry4 :
                    null;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return new Leaf5Plus1(p.Update(entry), L5);

                var l = L5;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;
                var e3 = l.Entry3;
                var e4 = l.Entry4;

                if (hash == e0.Hash) 
                    return new Leaf5Plus1(p, new Leaf5(e0.Update(entry), e1, e2, e3, e4));
                if (hash == e1.Hash) 
                    return new Leaf5Plus1(p, new Leaf5(e0, e1.Update(entry), e2, e3, e4));
                if (hash == e2.Hash)
                    return new Leaf5Plus1(p, new Leaf5(e0, e1, e2.Update(entry), e3, e4));
                if (hash == e3.Hash) 
                    return new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3.Update(entry), e4));
                if (hash == e4.Hash)
                    return new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4.Update(entry)));

                return new Leaf5Plus1Plus1(entry, this);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.KeepOrUpdate(entry)) == Plus ? this : (ImHashMap234<K, V>)new Leaf5Plus1(p, L5);

                var l5 = L5;
                var e0 = l5.Entry0;

                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash)
                     return (e0 = e0.KeepOrUpdate(entry)) == l5.Entry0 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));
                if (hash == e1.Hash)
                    return (e1 = e1.KeepOrUpdate(entry)) == l5.Entry1 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));
                if (hash == e2.Hash)
                    return (e2 = e2.KeepOrUpdate(entry)) == l5.Entry2 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));
                if (hash == e3.Hash)
                    return (e3 = e3.KeepOrUpdate(entry)) == l5.Entry3 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));
                if (hash == e4.Hash)
                    return (e4 = e4.KeepOrUpdate(entry)) == l5.Entry4 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));

                return new Leaf5Plus1Plus1(entry, this);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var p  = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.TryRemove(key)) == Plus ? this : p == null ? L5 : (ImHashMap234<K, V>)new Leaf5Plus1(p, L5);

                var l = L5;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;
                var e3 = l.Entry3;
                var e4 = l.Entry4;

                // todo: @simplify this thing by sorting the entries
                if (hash == e0.Hash)
                    return (e0 = e0.TryRemove(key)) == l.Entry0 ? this : e0 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e1.Hash ? new Leaf5(p, e1, e2, e3, e4) :
                    ph < e2.Hash ? new Leaf5(e1, p, e2, e3, e4) :
                    ph < e3.Hash ? new Leaf5(e1, e2, p, e3, e4) :
                    ph < e4.Hash ? new Leaf5(e1, e2, e3, p, e4) :
                                   new Leaf5(e1, e2, e3, e4, p);

                if (hash == e1.Hash)
                    return (e1 = e1.TryRemove(key)) == l.Entry0 ? this : e1 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e0.Hash ? new Leaf5(p, e0, e2, e3, e4) :
                    ph < e2.Hash ? new Leaf5(e0, p, e2, e3, e4) :
                    ph < e3.Hash ? new Leaf5(e0, e2, p, e3, e4) :
                    ph < e4.Hash ? new Leaf5(e0, e2, e3, p, e4) :
                                   new Leaf5(e0, e2, e3, e4, p);

                if (hash == e2.Hash)
                    return (e2 = e2.TryRemove(key)) == l.Entry0 ? this : e2 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e0.Hash ? new Leaf5(p, e0, e1, e3, e4) :
                    ph < e1.Hash ? new Leaf5(e0, p, e1, e3, e4) :
                    ph < e3.Hash ? new Leaf5(e0, e1, p, e3, e4) :
                    ph < e4.Hash ? new Leaf5(e0, e1, e3, p, e4) :
                                   new Leaf5(e0, e1, e3, e4, p);

                if (hash == e3.Hash)
                    return (e3 = e3.TryRemove(key)) == l.Entry0 ? this : e3 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e0.Hash ? new Leaf5(p, e0, e1, e2, e4) :
                    ph < e1.Hash ? new Leaf5(e0, p, e1, e2, e4) :
                    ph < e2.Hash ? new Leaf5(e0, e1, p, e2, e4) :
                    ph < e4.Hash ? new Leaf5(e0, e1, e2, p, e4) :
                                   new Leaf5(e0, e1, e2, e4, p);

                if (hash == e4.Hash)
                    return (e4 = e4.TryRemove(key)) == l.Entry0 ? this : e4 != null ? (ImHashMap234<K, V>)new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)) :
                    ph < e0.Hash ? new Leaf5(p, e0, e1, e2, e3) :
                    ph < e1.Hash ? new Leaf5(e0, p, e1, e2, e3) :
                    ph < e2.Hash ? new Leaf5(e0, e1, p, e2, e3) :
                    ph < e4.Hash ? new Leaf5(e0, e1, e2, p, e3) :
                                   new Leaf5(e0, e1, e2, e3, p);

                return this;
            }
        }

        /// <summary>Leaf with 5 entries</summary>
        public sealed class Leaf5Plus1Plus1 : ImHashMap234<K, V>
        {
            /// <summary>Plus entry</summary>
            public readonly Entry Plus;
            /// <summary>Dangling Leaf5</summary>
            public readonly Leaf5Plus1 L;

            /// <summary>Constructs the leaf</summary>
            public Leaf5Plus1Plus1(Entry plus, Leaf5Plus1 l)
            {
                Plus = plus;
                L    = l;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf5+1+1{" + Plus + " + " + L + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                if (hash == Plus.Hash)
                    return Plus;
                if (hash == L.Plus.Hash)
                    return L.Plus;
                var l = L.L5;
                return 
                    hash == l.Entry0.Hash ? l.Entry0 :
                    hash == l.Entry1.Hash ? l.Entry1 :
                    hash == l.Entry2.Hash ? l.Entry2 :
                    hash == l.Entry3.Hash ? l.Entry3 :
                    hash == l.Entry4.Hash ? l.Entry4 :
                    null;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return new Leaf5Plus1Plus1(p.Update(entry), L);

                var lp = L.Plus;
                var lph = lp.Hash;
                if (lph == hash)
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp.Update(entry), L.L5));

                var l5 = L.L5;
                var e0 = l5.Entry0;
                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash) 
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0.Update(entry), e1, e2, e3, e4)));
                if (hash == e1.Hash) 
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1.Update(entry), e2, e3, e4)));
                if (hash == e2.Hash)
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2.Update(entry), e3, e4)));
                if (hash == e3.Hash) 
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3.Update(entry), e4)));
                if (hash == e4.Hash)
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4.Update(entry))));

                Entry e = entry;
                SortEntriesByHash(ref e0, ref e1, ref e2, ref e3, ref e4, ref lp, ref p, ref e);

                return new Branch2(new Leaf5(e0, e1, e2, e3, e4), lp, new Leaf2(p, e));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.KeepOrUpdate(entry)) == Plus ? this : (ImHashMap234<K, V>)new Leaf5Plus1(p, L.L5);

                var lp = L.Plus;
                var lph = lp.Hash;
                if (lph == hash)
                    return (lp = lp.KeepOrUpdate(entry)) == L.Plus ? this : (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, L.L5));

                var l5 = L.L5;
                var e0 = l5.Entry0;
                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash)
                    return (e0 = e0.KeepOrUpdate(entry)) == l5.Entry0 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));
                if (hash == e1.Hash)
                    return (e1 = e1.KeepOrUpdate(entry)) == l5.Entry1 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));
                if (hash == e2.Hash)
                    return (e2 = e2.KeepOrUpdate(entry)) == l5.Entry2 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));
                if (hash == e3.Hash)
                    return (e3 = e3.KeepOrUpdate(entry)) == l5.Entry3 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));
                if (hash == e4.Hash)
                    return (e4 = e4.KeepOrUpdate(entry)) == l5.Entry4 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));

                Entry e = entry;
                SortEntriesByHash(ref e0, ref e1, ref e2, ref e3, ref e4, ref lp, ref p, ref e);

                return new Branch2(new Leaf5(e0, e1, e2, e3, e4), lp, new Leaf2(p, e));
            }

            /// <summary>The order at the end should be the follwing: <![CDATA[e0 < e1 < e2 < e3 < e4 < lp < p < entry]]></summary>
            internal static void SortEntriesByHash(ref Entry e0, ref Entry e1, ref Entry e2, ref Entry e3, ref Entry e4, ref Entry lp, ref Entry p, ref Entry e)
            {
                var h = lp.Hash;
                Entry swap = null;
                if (h < e4.Hash)
                {
                    swap = e4; e4 = lp; lp = swap;
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
                if (h < lp.Hash)
                {
                    swap = lp; lp = p; p = swap;
                    if (h < e4.Hash)
                    {
                        swap = e4; e4 = lp; lp = swap;
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
                if (e != null) 
                {
                    h = e.Hash; 
                    if (h < p.Hash)
                    {
                        swap = p; p = e; e = swap;
                        if (h < lp.Hash)
                        {
                            swap = lp; lp = p; p = swap;
                            if (h < e4.Hash)
                            {
                                swap = e4; e4 = lp; lp = swap;
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
                    }
                }
            }

            internal static void SortEntriesByHash(ref Entry e0, ref Entry e1, ref Entry e2, ref Entry e3, ref Entry e4, ref Entry lp, ref Entry p)
            {
                var h = lp.Hash;
                Entry swap = null;
                if (h < e4.Hash)
                {
                    swap = e4; e4 = lp; lp = swap;
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
                if (h < lp.Hash)
                {
                    swap = lp; lp = p; p = swap;
                    if (h < e4.Hash)
                    {
                        swap = e4; e4 = lp; lp = swap;
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
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var p  = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.TryRemove(key)) == Plus ? this : p == null ? L : (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, L);

                var lp  = L.Plus;
                var lph = lp.Hash;
                if (lph == hash)
                    return (lp = lp.TryRemove(key)) == Plus ? this : lp == null ? new Leaf5Plus1(p, L.L5) : (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, L.L5));

                var l5 = L.L5;
                var e0 = l5.Entry0;
                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash)
                    return (e0 = e0.TryRemove(key)) == l5.Entry0 
                        ? this : e0 != null ? (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4))) :
                    lph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e1, e2, e3, e4)) :
                    lph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e1, lp, e2, e3, e4)) :
                    lph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e1, e2, lp, e3, e4)) :
                    lph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e1, e2, e3, lp, e4)) :
                                    new Leaf5Plus1(p, new Leaf5(e1, e2, e3, e4, lp));

                if (hash == e1.Hash)
                    return (e1 = e1.TryRemove(key)) == l5.Entry1 
                        ? this : e1 != null ? (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4))) :
                    lph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e0, e2, e3, e4)) :
                    lph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, lp, e2, e3, e4)) :
                    lph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e2, lp, e3, e4)) :
                    lph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e2, e3, lp, e4)) :
                                    new Leaf5Plus1(p, new Leaf5(e0, e2, e3, e4, lp));

                if (hash == e2.Hash)
                    return (e2 = e2.TryRemove(key)) == l5.Entry2 
                        ? this : e2 != null ? (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4))) :
                    lph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e0, e1, e3, e4)) :
                    lph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, lp, e1, e3, e4)) :
                    lph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, lp, e3, e4)) :
                    lph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e3, lp, e4)) :
                                    new Leaf5Plus1(p, new Leaf5(e0, e1, e3, e4, lp));

                if (hash == e3.Hash)
                    return (e3 = e3.TryRemove(key)) == l5.Entry3 
                        ? this : e3 != null ? (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4))) :
                    lph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e0, e1, e2, e4)) :
                    lph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, lp, e1, e2, e4)) :
                    lph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, lp, e2, e4)) :
                    lph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, lp, e4)) :
                                    new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e4, lp));

                if (hash == e4.Hash)
                    return (e4 = e4.TryRemove(key)) == l5.Entry4 
                        ? this : e4 != null ? (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4))) :
                    lph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e0, e1, e2, e3)) :
                    lph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, lp, e1, e2, e3)) :
                    lph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, lp, e2, e3)) :
                    lph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, lp, e3)) :
                                    new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, lp));

                return this;
            }
        }

        /// <summary>Branch of 2 leafs or branches</summary>
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
                Debug.Assert(left != Empty);
                Debug.Assert(left is Entry == false);
                Debug.Assert(right != Empty);
                Debug.Assert(right is Entry == false);
                Debug.Assert(left is Branch2 ? right is Branch2 : right is Branch2 == false, 
                    "the all Branch2 branches should be either leafs or the branches");
                MidEntry = entry;
                Left     = left;
                Right    = right;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "br:" +
                Left is Branch2 == false ? Left + " <- " + MidEntry + " -> " + Right :
                Left.GetType().Name + " <- " + MidEntry + " -> " + Right.GetType().Name;
#endif

            /// <inheritdoc />
            public sealed override Entry GetEntryOrDefault(int hash) =>
                hash > MidEntry.Hash ? Right.GetEntryOrDefault(hash) :
                hash < MidEntry.Hash ? Left .GetEntryOrDefault(hash) :
                MidEntry is RemovedEntry ? null : MidEntry;

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry)
            {
                var e = MidEntry;
                if (hash > e.Hash)
                {
                    var right = Right;
                    var newRight = right.AddOrUpdateEntry(hash, entry);
                    if ((right is Branch3 || right is Leaf5Plus1Plus1) && newRight is Branch3 == false && newRight is Branch2 b2)
                        return new Branch3(Left, e, b2);
                    return new Branch2(Left, e, newRight);
                }

                if (hash < e.Hash)
                {
                    var left = Left;
                    var newLeft = left.AddOrUpdateEntry(hash, entry);
                    if ((left is Branch3 || left is Leaf5Plus1Plus1) && newLeft is Branch3 == false && newLeft is Branch2 b2)
                        return new Branch3(b2.Left, b2.MidEntry, new Branch2(b2.Right, e, Right));
                    return new Branch2(newLeft, e, Right);
                }

                return new Branch2(Left, e.Update(entry), Right);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry)
            {
                var e = MidEntry;
                if (hash > e.Hash)
                {
                    var right = Right;
                    var newRight = right.AddOrKeepEntry(hash, entry);
                    if (newRight == right)
                        return this;
                    if ((right is Branch3 || right is Leaf5Plus1Plus1) && newRight is Branch3 == false && newRight is Branch2 b2)
                        return new Branch3(Left, e, b2);
                    return new Branch2(Left, e, newRight);
                }

                if (hash < e.Hash)
                {
                    var left = Left;
                    var newLeft = left.AddOrKeepEntry(hash, entry);
                    if (newLeft == left)
                        return this;
                    if ((left is Branch3 || left is Leaf5Plus1Plus1) && newLeft is Branch3 == false && newLeft is Branch2 b2)
                        return new Branch3(b2.Left, b2.MidEntry, new Branch2(b2.Right, e, Right));
                    return new Branch2(newLeft, e, Right);
                }

                return (e = e.KeepOrUpdate(entry)) == MidEntry ? this : new Branch2(Left, e, Right);
            }

            /// <inheritdoc />
            public sealed override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var e = MidEntry;
                if (hash > e.Hash)
                {
                    var newRight = Right.RemoveEntry(hash, key);
                    return newRight == Right ? this : new Branch2(Left, MidEntry, newRight);
                }

                if (hash < e.Hash)
                {
                    var newLeft = Left.RemoveEntry(hash, key);
                    return newLeft == Left ? this : new Branch2(newLeft, MidEntry, Right);
                }

                return (e = e.TryRemove(key)) == MidEntry ? this : new Branch2(Left, e, Right);
            }
        }

        /// <summary>Branch of 3 leafs or branches and two entries</summary>
        public sealed class Branch3 : Branch2
        {
            /// <summary>Creating the branch</summary>
            public Branch3(ImHashMap234<K, V> left, Entry entry, Branch2 rightBranch) : base(left, entry, rightBranch)
            {
                Debug.Assert(left is Branch2 ? rightBranch.Left is Branch2 : rightBranch.Left is Branch2 == false, 
                    "the all Branch3 branches should be either leafs or the branches");
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "br3-" + base.ToString();
#endif

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, KeyValueEntry entry)
            {
                var h0 = MidEntry.Hash;
                var rb = (Branch2)Right;
                var h1 = rb.MidEntry.Hash;
                
                if (hash > h1)
                {
                     // No need to call the Split method because we won't destruct the result branch
                    var right = rb.Right;
                    var newRight = right.AddOrUpdateEntry(hash, entry);
                    if ((right is Branch3 || right is Leaf5Plus1Plus1) && newRight is Branch2 && newRight is Branch3 == false)
                        return new Branch2(new Branch2(Left, MidEntry, rb.Left), rb.MidEntry, newRight);
                    return new Branch3(Left, MidEntry, new Branch2(rb.Left, rb.MidEntry, newRight));
                }

                if (hash < h0)
                {
                    var newLeft = Left.AddOrUpdateEntry(hash, entry);
                    if ((Left is Branch3 || Left is Leaf5Plus1Plus1) && newLeft is Branch2 && newLeft is Branch3 == false)
                        return new Branch2(newLeft, MidEntry, rb);
                    return new Branch3(newLeft, MidEntry, rb);
                }

                if (hash > h0 && hash < h1)
                {
                    var middle = rb.Left;
                    var newMiddle = middle.AddOrUpdateEntry(hash, entry);
                    if ((middle is Branch3 || middle is Leaf5Plus1Plus1) && newMiddle is Branch3 == false && newMiddle is Branch2 newMiddleBranch2)
                        return new Branch2(
                            new Branch2(Left, MidEntry, newMiddleBranch2.Left),
                            newMiddleBranch2.MidEntry, 
                            new Branch2(newMiddleBranch2.Right, rb.MidEntry, rb.Right));
                    return new Branch3(Left, MidEntry, new Branch2(newMiddle, rb.MidEntry, rb.Right));
                }

                return hash == h0
                    ? new Branch3(Left, MidEntry.Update(entry), rb)
                    : new Branch3(Left, MidEntry, new Branch2(rb.Left, rb.MidEntry.Update(entry), rb.Right));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, KeyValueEntry entry)
            {
                var h0 = MidEntry.Hash;
                var rb = (Branch2)Right;
                var h1 = rb.MidEntry.Hash;

                if (hash > h1)
                {
                    var right = rb.Right;
                    var newRight = right.AddOrKeepEntry(hash, entry);
                    if (newRight == right)
                        return this;
                    if ((right is Branch3 || right is Leaf5Plus1Plus1) && newRight is Branch2 && newRight is Branch3 == false)
                        return new Branch2(new Branch2(Left, MidEntry, rb.Left), rb.MidEntry, newRight);
                    return new Branch3(Left, MidEntry, new Branch2(rb.Left, rb.MidEntry, newRight));
                }

                if (hash < h0)
                {
                    var newLeft = Left.AddOrKeepEntry(hash, entry);
                    if (newLeft == Left)
                        return this;
                    if ((Left is Branch3 || Left is Leaf5Plus1Plus1) && newLeft is Branch2 && newLeft is Branch3 == false)
                        return new Branch2(newLeft, MidEntry, rb);
                    return new Branch3(newLeft, MidEntry, rb);
                }

                if (hash > h0 && hash < h1)
                {
                    var middle = rb.Left;
                    var newMiddle = middle.AddOrKeepEntry(hash, entry);
                    if (newMiddle == middle)
                        return this;
                    if ((middle is Branch3 || middle is Leaf5Plus1Plus1) && newMiddle is Branch3 == false && newMiddle is Branch2 newMiddleBranch2)
                        return new Branch2(
                            new Branch2(Left, MidEntry, newMiddleBranch2.Left),
                            newMiddleBranch2.MidEntry,
                            new Branch2(newMiddleBranch2.Right, rb.MidEntry, rb.Right));
                    return new Branch3(Left, MidEntry, new Branch2(newMiddle, rb.MidEntry, rb.Right));
                }

                var e0 = MidEntry;
                if (hash == h0)
                    return (e0 = e0.KeepOrUpdate(entry)) == MidEntry ? this : new Branch3(Left, e0, rb);

                var e1 = rb.MidEntry;
                return (e1 = e1.KeepOrUpdate(entry)) == rb.MidEntry ? this : new Branch3(Left, e0, new Branch2(rb.Left, e1, rb.Right));
            }
        }
    }

    /// <summary>ImHashMap methods</summary>
    public static class ImHashMap234
    {
        /// <summary>Enumerates all the map entries from the left to the right and from the bottom to top</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static IEnumerable<ImHashMap234<K, V>.KeyValueEntry> Enumerate<K, V>(this ImHashMap234<K, V> map, 
            List<ImHashMap234<K, V>> parentStack = null) // todo: @perf replace the List with the more lightweight alternative, the bad thing that we cannot pass the `ref` array into the method returning IEnumerable
        {
            if (map == ImHashMap234<K, V>.Empty)
                yield break;
            if (map is ImHashMap234<K, V>.Entry e)
            {
                if (e is ImHashMap234<K, V>.KeyValueEntry v) yield return v;
                else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e).Conflicts) yield return c;
                yield break;
            }

            var parentIndex = -1;
            while (true)
            {
                if (map is ImHashMap234<K, V>.Branch2 b2)
                {
                    if (parentStack == null)
                        parentStack = new List<ImHashMap234<K, V>>(2);
                    if (++parentIndex >= parentStack.Count)
                        parentStack.Add(map);
                    else
                        parentStack[parentIndex] = map;
                    map = b2.Left;
                    continue;
                }
                
                if (map is ImHashMap234<K, V>.Leaf2 l2)
                {
                    if (l2.Entry0 is ImHashMap234<K, V>.KeyValueEntry v0) yield return v0;
                    else if (l2.Entry0 != null) foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l2.Entry0).Conflicts) yield return c;
                    if (l2.Entry1 is ImHashMap234<K, V>.KeyValueEntry v1) yield return v1;
                    else if (l2.Entry1 != null) foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l2.Entry1).Conflicts) yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf3 l3)
                {
                    if (l3.Entry0 is ImHashMap234<K, V>.KeyValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l3.Entry0).Conflicts) yield return c;
                    if (l3.Entry1 is ImHashMap234<K, V>.KeyValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l3.Entry1).Conflicts) yield return c;
                    if (l3.Entry2 is ImHashMap234<K, V>.KeyValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l3.Entry2).Conflicts) yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf3Plus1 l31)
                {
                    var p = l31.Plus;
                    var ph = p.Hash;
                    var l = l31.L3;
                    var e0  = l.Entry0;
                    var e1  = l.Entry1;
                    var e2  = l.Entry2;

                    ImHashMap234<K, V>.Entry swap = null;
                    if (ph < e2.Hash)
                    {
                        swap = e2; e2 = p; p = swap;
                        if (ph < e1.Hash)
                        {
                            swap = e1; e1 = e2; e2 = swap;
                            if (ph < e0.Hash)
                            {
                                swap = e0; e0 = e1; e1 = swap;
                            }
                        }
                    }

                    if (e0 is ImHashMap234<K, V>.KeyValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e0).Conflicts) yield return c;
                    if (e1 is ImHashMap234<K, V>.KeyValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e1).Conflicts) yield return c;
                    if (e2 is ImHashMap234<K, V>.KeyValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e2).Conflicts) yield return c;
                    if (p  is ImHashMap234<K, V>.KeyValueEntry v3) yield return v3;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)p).Conflicts)  yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf5 l5)
                {
                    if (l5.Entry0 is ImHashMap234<K, V>.KeyValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l5.Entry0).Conflicts) yield return c;
                    if (l5.Entry1 is ImHashMap234<K, V>.KeyValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l5.Entry1).Conflicts) yield return c;
                    if (l5.Entry2 is ImHashMap234<K, V>.KeyValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l5.Entry2).Conflicts) yield return c;
                    if (l5.Entry3 is ImHashMap234<K, V>.KeyValueEntry v3) yield return v3;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l5.Entry3).Conflicts) yield return c;
                    if (l5.Entry4 is ImHashMap234<K, V>.KeyValueEntry v4) yield return v4;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)l5.Entry4).Conflicts) yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf5Plus1 l51)
                {
                    var p = l51.Plus;
                    var ph = p.Hash;
                    var l = l51.L5;
                    var e0  = l.Entry0;
                    var e1  = l.Entry1;
                    var e2  = l.Entry2;
                    var e3  = l.Entry3;
                    var e4  = l.Entry4;

                    ImHashMap234<K, V>.Entry swap = null;
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

                    if (e0 is ImHashMap234<K, V>.KeyValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e0).Conflicts) yield return c;
                    if (e1 is ImHashMap234<K, V>.KeyValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e1).Conflicts) yield return c;
                    if (e2 is ImHashMap234<K, V>.KeyValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e2).Conflicts) yield return c;
                    if (e3 is ImHashMap234<K, V>.KeyValueEntry v3) yield return v3;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e3).Conflicts) yield return c;
                    if (e4 is ImHashMap234<K, V>.KeyValueEntry v4) yield return v4;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e4).Conflicts) yield return c;
                    if (p  is ImHashMap234<K, V>.KeyValueEntry v5) yield return v5;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)p).Conflicts)  yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf5Plus1Plus1 l511)
                {
                    var p   = l511.Plus;
                    var lp  = l511.L.Plus;
                    var l   = l511.L.L5;
                    var e0  = l.Entry0;
                    var e1  = l.Entry1;
                    var e2  = l.Entry2;
                    var e3  = l.Entry3;
                    var e4  = l.Entry4;

                    ImHashMap234<K, V>.Leaf5Plus1Plus1.SortEntriesByHash(ref e0, ref e1, ref e2, ref e3, ref e4, ref lp, ref p);

                    if (e0 is ImHashMap234<K, V>.KeyValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e0).Conflicts) yield return c;
                    if (e1 is ImHashMap234<K, V>.KeyValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e1).Conflicts) yield return c;
                    if (e2 is ImHashMap234<K, V>.KeyValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e2).Conflicts) yield return c;
                    if (e3 is ImHashMap234<K, V>.KeyValueEntry v3) yield return v3;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e3).Conflicts) yield return c;
                    if (e4 is ImHashMap234<K, V>.KeyValueEntry v4) yield return v4;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)e4).Conflicts) yield return c;
                    if (lp is ImHashMap234<K, V>.KeyValueEntry v5) yield return v5;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)lp).Conflicts) yield return c;
                    if (p  is ImHashMap234<K, V>.KeyValueEntry v6) yield return v6;
                    else foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)p).Conflicts)  yield return c;
                }

                if (parentIndex == -1)
                    break; // we yield the leaf and there is nothing in stack - we are DONE!

                var pb2 = (ImHashMap234<K, V>.Branch2)parentStack[parentIndex]; // otherwise get the parent
                if (pb2.MidEntry is ImHashMap234<K, V>.KeyValueEntry v) 
                    yield return v;
                else if (pb2.MidEntry != null) foreach (var c in ((ImHashMap234<K, V>.HashConflictKeyValuesEntry)pb2.MidEntry).Conflicts) 
                    yield return c;

                map = pb2.Right;
                --parentIndex; // we done with the this level handled the Left (previously) and the Right (now)
            }
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        ///  returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V> map, int hash, K key)
        {
            var e = map.GetEntryOrDefault(hash);
            if (e is ImHashMap234<K, V>.KeyValueEntry v)
            {
                if (v.Key.Equals(key))
                    return v.Value;
            }
            else if (e is ImHashMap234<K, V>.HashConflictKeyValuesEntry c)
            {
                foreach (var x in c.Conflicts) 
                    if (x.Key.Equals(key))
                        return x.Value;
            }
            return default(V);
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        /// returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V> map, K key) =>
            map.GetValueOrDefault(key.GetHashCode(), key);

        /// <summary>Looks up for the key using its hash code and checking the key with `object.ReferenceEquals` for equality,
        ///  returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefaultReferenceEqual<K, V>(this ImHashMap234<K, V> map, int hash, K key) where K : class
        {
            var e = map.GetEntryOrDefault(hash);
            if (e is ImHashMap234<K, V>.KeyValueEntry v)
            {
                if (v.Key == key)
                    return v.Value;
            }
            else if (e is ImHashMap234<K, V>.HashConflictKeyValuesEntry c)
            {
                foreach (var x in c.Conflicts) 
                    if (x.Key == key)
                        return x.Value;
            }
            return default(V);
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        /// returns the `true` and the found value or `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap234<K, V> map, int hash, K key, out V value)
        {
            var e = map.GetEntryOrDefault(hash);
            if (e is ImHashMap234<K, V>.KeyValueEntry v)
            {
                if (v.Key.Equals(key))
                {
                    value = v.Value;
                    return true;
                }
            }
            else if (e is ImHashMap234<K, V>.HashConflictKeyValuesEntry c)
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

        /// <summary>Looks up for the key using its hash code and checking the key equality with the `ReferenceEquals`, 
        /// returns the `true` and the found value or `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFindReferenceEqual<K, V>(this ImHashMap234<K, V> map, int hash, K key, out V value) where K : class
        {
            var e = map.GetEntryOrDefault(hash);

            if (e is ImHashMap234<K, V>.KeyValueEntry v)
            {
                if (v.Key == key)
                {
                    value = v.Value;
                    return true;
                }
            }
            else if (e is ImHashMap234<K, V>.HashConflictKeyValuesEntry c)
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

        /// <summary>Looks up for the key using its hash code and returns the `true` and the found value or `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap234<K, V> map, K key, out V value) =>
            map.TryFind(key.GetHashCode(), key, out value);

        /// <summary>Adds or updates the value by key in the map, always returning the modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrUpdate<K, V>(this ImHashMap234<K, V> map, int hash, K key, V value) =>
            map.AddOrUpdateEntry(hash, new ImHashMap234<K, V>.KeyValueEntry(hash, key, value));

        /// <summary>Adds or updates the value by key in the map, always returning the modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrUpdate<K, V>(this ImHashMap234<K, V> map, K key, V value)
        {
            var hash = key.GetHashCode();
            return map.AddOrUpdateEntry(hash, new ImHashMap234<K, V>.KeyValueEntry(hash, key, value));
        }

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrKeep<K, V>(this ImHashMap234<K, V> map, int hash, K key, V value) =>
            map.AddOrKeepEntry(hash, new ImHashMap234<K, V>.KeyValueEntry(hash, key, value));

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrKeep<K, V>(this ImHashMap234<K, V> map, K key, V value)
        {
            var hash = key.GetHashCode();
            return map.AddOrKeepEntry(hash, new ImHashMap234<K, V>.KeyValueEntry(hash, key, value));
        }

        /// <summary>Returns the map without the entry with the specified hash and key if it is found in the map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> Remove<K, V>(this ImHashMap234<K, V> map, int hash, K key) =>
            map.RemoveEntry(hash, key);

        /// <summary>Returns the map without the entry with the specified hash and key if it is found in the map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> Remove<K, V>(this ImHashMap234<K, V> map, K key) =>
            // it make sense to have the condition here to prevent the probably costly `GetHashCode()` for the empty map.
            map == ImHashMap234<K, V>.Empty ? map : map.RemoveEntry(key.GetHashCode(), key);
    }

    /// <summary>
    /// The fixed array of maps (partitions) where the first key bits are used to locate the partion to lookup into.
    /// Note: The partition array is NOT immutable and operates by swapping the updated partition (map) with the new one.
    /// The default partitions count it "carefully selected" to be 16:
    /// - Not too big to waste the space for the small collection and to fit (hopefully) into the cache line (16 of 4 byte pointer = 64 bytes)
    /// - Not too short to diminish the benifits of partioning
    /// </summary>
    public static class PartitionedHashMap234
    {
        /// <summary>Default number of partions</summary>
        public const int PART_COUNT_POWER_OF_TWO = 16;

        /// <summary>The default mask to partition the key</summary>
        public const int PART_HASH_MASK = PART_COUNT_POWER_OF_TWO - 1;

        /// <summary>Creates the new collection with the empty partions</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V>[] CreateEmpty<K, V>(int partCountPowerOfTwo = PART_COUNT_POWER_OF_TWO)
        {
            var parts = new ImHashMap234<K, V>[partCountPowerOfTwo];
            for (var i = 0; i < parts.Length; ++i)
                parts[i] = ImHashMap234<K, V>.Empty;
            return parts;
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        /// returns the `true` and the found value or `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, out V value, int partHashMask = PART_HASH_MASK)
        {
            var p = parts[hash & partHashMask];
            if (p != null) 
                return p.TryFind(hash, key, out value);
            value = default(V);
            return false;
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        /// returns the `true` and the found value or `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap234<K, V>[] parts, K key, out V value, int partHashMask = PART_HASH_MASK) =>
            parts.TryFind(key.GetHashCode(), key, out value, partHashMask);

        /// <summary>Looks up for the key using its hash code and checking the key with `object.ReferenceEquals` for equality,
        /// returns the `true` and the found value or `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFindReferenceEqual<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, out V value, int partHashMask = PART_HASH_MASK)
            where K : class
        {
            var p = parts[hash & partHashMask];
            if (p != null) 
                return p.TryFindReferenceEqual(hash, key, out value);
            value = default(V);
            return false;
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.ReferenceEquals` for equality,
        /// returns the `true` and the found value or `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFindReferenceEqual<K, V>(this ImHashMap234<K, V>[] parts, K key, out V value, int partHashMask = PART_HASH_MASK)
            where K : class => parts.TryFindReferenceEqual(key.GetHashCode(), key, out value, partHashMask);

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        ///  returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, int partHashMask = PART_HASH_MASK)
        {
            var p = parts[hash & partHashMask];
            return p != null ? p.GetValueOrDefault(hash, key) : default(V);
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        /// returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V>[] parts, K key, int partHashMask = PART_HASH_MASK) =>
            parts.GetValueOrDefault(key.GetHashCode(), key, partHashMask);

        /// <summary>Looks up for the key using its hash code and checking the key with `object.ReferenceEquals` for equality,
        ///  returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefaultReferenceEqual<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, int partHashMask = PART_HASH_MASK) where K : class
        {
            var p = parts[hash & partHashMask];
            return p != null ? p.GetValueOrDefaultReferenceEqual(hash, key) : default(V);
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.ReferenceEquals` for equality,
        ///  returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefaultReferenceEqual<K, V>(this ImHashMap234<K, V>[] parts, K key, int partHashMask = PART_HASH_MASK) where K : class => 
            parts.GetValueOrDefaultReferenceEqual(key.GetHashCode(), key, partHashMask);

        /// <summary>Returns THE SAME partitioned map BUT with updated partion</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static void AddOrUpdate<K, V>(this ImHashMap234<K, V>[] parts, int hash, K key, V value, int partHashMask = PART_HASH_MASK)
        {
            ref var part = ref parts[hash & partHashMask];
            var p = part;
            if (Interlocked.CompareExchange(ref part, p.AddOrUpdate(hash, key, value), p) != p)
                RefAddOrUpdatePart(ref part, hash, key, value);
        }

        /// <summary>Returns THE SAME partitioned map BUT with updated partion</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static void AddOrUpdate<K, V>(this ImHashMap234<K, V>[] parts, K key, V value, int partHashMask = PART_HASH_MASK) =>
            parts.AddOrUpdate(key.GetHashCode(), key, value, partHashMask);

        /// <summary>Updates the ref to the part with the new version and retries if the someone changed the part in between</summary>
        public static void RefAddOrUpdatePart<K, V>(ref ImHashMap234<K, V> part, int hash, K key, V value) =>
            Ref.Swap(ref part, hash, key, value, (x, h, k, v) => x.AddOrUpdate(h, k, v));
    }

    /// <summary>The base class for the tree leafs and branches, also defines the Empty tree</summary>
    public class ImMap234<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap234<V> Empty = new ImMap234<V>();

        /// <summary>Hide the constructor to prevent the multiple Empty trees creation</summary>
        protected ImMap234() { } // todo: @perf - does it hurt the perf or the call to the empty constructor is erased?

        /// Pretty-prints
        public override string ToString() 
        {
#if DEBUG
            // for debug purposes we just output the first N hashes in array
            const int outputCount = 101;
            var itemsInHashOrder = ImMap234.Enumerate(this).Take(outputCount).Select(x => x.Hash).ToList();
            return $"new int[{(itemsInHashOrder.Count >= 100 ? ">=" : "") + itemsInHashOrder.Count}] {{" + string.Join(", ", itemsInHashOrder) + "}";
#else
            return "empty " + typeof(ImMap234<V>).Name;
#endif
        }

        /// <summary>Lookup for the entry, if not found returns `null`</summary>
        public virtual Entry GetEntryOrDefault(int hash) => null;

        /// <summary>Produces the new or updated map with the new entry</summary>
        public virtual ImMap234<V> AddOrUpdateEntry(int hash, Entry entry) => entry;

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        public virtual ImMap234<V> AddOrKeepEntry(int hash, Entry entry) => entry;

        /// <summary>Returns the map without the entry with the specified hash and key if it is found in the map</summary>
        public virtual ImMap234<V> RemoveEntry(int hash) => this;

        /// <summary>The base entry for the Value and for the ConflictingValues entries, contains the Hash and Key</summary>
        public class Entry : ImMap234<V>
        {
            /// <summary>The Hash</summary>
            public readonly int Hash;
            /// <summary>The value. May be modified if you need the Ref{V} semantics</summary>
            public V Value;
            /// <summary>Constructs the entry with the default value</summary>
            public Entry(int hash) => Hash = hash;
            /// <summary>Constructs the entry with the key and value</summary>
            public Entry(int hash, V value)
            { 
                Hash  = hash;
                Value = value;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "[" + Hash + "]:" + Value;
#endif

            /// <inheritdoc />
            public sealed override Entry GetEntryOrDefault(int hash) => hash == Hash ? this : null;

            /// <inheritdoc />
            public sealed override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry) =>
                hash > Hash ? new Leaf2(this, entry) :
                hash < Hash ? new Leaf2(entry, this) :
                (ImMap234<V>)entry;

            /// <inheritdoc />
            public sealed override ImMap234<V> AddOrKeepEntry(int hash, Entry entry) =>
                hash > Hash ? new Leaf2(this, entry) :
                hash < Hash ? new Leaf2(entry, this) :
                (ImMap234<V>)this;

            /// <inheritdoc />
            public sealed override ImMap234<V> RemoveEntry(int hash) => hash == Hash ? Empty : this;
        }

        internal sealed class RemovedEntry : Entry 
        {
            public RemovedEntry(int hash) : base(hash) {}
            public override string ToString() => "[" + Hash + "]:removed-entry";
        }

        /// <summary>Leaf with 2 entries</summary>
        public sealed class Leaf2 : ImMap234<V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Right entry</summary>
            public readonly Entry Entry1;
            /// <summary>Constructs the leaf</summary>
            public Leaf2(Entry e0, Entry e1)
            {
                Debug.Assert(e0.Hash < e1.Hash);
                Entry0 = e0; Entry1 = e1;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf2{" + Entry0 + "; " + Entry1 + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash == Entry0.Hash ? Entry0 :
                hash == Entry1.Hash ? Entry1 :
                null;

            /// <inheritdoc />
            public override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry)
            {
                var e1 = Entry1;
                var e0 = Entry0;
                return
                    hash > e1.Hash                   ? new Leaf3(e0, e1, entry) :
                    hash < e0.Hash                   ? new Leaf3(entry, e0, e1) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf3(e0, entry, e1) :
                    hash == e0.Hash ? new Leaf2(entry, e1) :
                    (ImMap234<V>)  new Leaf2(e0, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int hash, Entry entry)
            {
                var e1 = Entry1;
                var e0 = Entry0;
                return
                    hash > e1.Hash                   ? new Leaf3(e0, e1, entry) :
                    hash < e0.Hash                   ? new Leaf3(entry, e0, e1) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf3(e0, entry, e1) :
                    (ImMap234<V>)this;
            }

            /// <inheritdoc />
            public override ImMap234<V> RemoveEntry(int hash) => 
                hash == Entry0.Hash ? Entry1 : hash == Entry1.Hash ? Entry0 : (ImMap234<V>)this;
        }

        /// <summary>Leaf with 3 entries</summary>
        public sealed class Leaf3 : ImMap234<V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Middle entry</summary>
            public readonly Entry Entry1;
            /// <summary>Right entry</summary>
            public readonly Entry Entry2;
            /// <summary>Constructs the leaf</summary>
            public Leaf3(Entry e0, Entry e1, Entry e2)
            {
                Debug.Assert(e0.Hash < e1.Hash);
                Debug.Assert(e1.Hash < e2.Hash);
                Entry0 = e0; Entry1 = e1; Entry2 = e2;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf3{" + Entry0 + "; " + Entry1 + "; " + Entry2 + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash == Entry0.Hash ? Entry0 :
                hash == Entry1.Hash ? Entry1 :
                hash == Entry2.Hash ? Entry2 :
                null;

            /// <inheritdoc />
            public override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry) =>
                hash == Entry0.Hash ? new Leaf3(entry, Entry1, Entry2) :
                hash == Entry1.Hash ? new Leaf3(Entry0, entry, Entry2) :
                hash == Entry2.Hash ? new Leaf3(Entry0, Entry1, entry) :
                (ImMap234<V>)new Leaf3Plus1(entry, this);

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int hash, Entry entry) =>
                hash == Entry0.Hash || hash == Entry1.Hash || hash == Entry2.Hash ? this : (ImMap234<V>)new Leaf3Plus1(entry, this);

            /// <inheritdoc />
            public override ImMap234<V> RemoveEntry(int hash)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                return hash == e0.Hash ? new Leaf2(e1, e2)
                     : hash == e1.Hash ? new Leaf2(e0, e2)
                     : hash == e2.Hash ? new Leaf2(e0, e1)
                     : (ImMap234<V>)this;
            }
        }

        /// <summary>Leaf with 3 + 1 entries</summary>
        public sealed class Leaf3Plus1 : ImMap234<V>
        {
            /// <summary>Plus entry</summary>
            public readonly Entry Plus;
            /// <summary>Dangling leaf3</summary>
            public readonly Leaf3 L3;
            /// <summary>Constructs the leaf</summary>
            public Leaf3Plus1(Entry plus, Leaf3 l3)
            {
                Plus = plus;
                L3 = l3;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf3+1{" + Plus + " + " + L3 + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) 
            {
                if (hash == Plus.Hash) 
                    return Plus; 
                var l = L3;
                return 
                    hash == l.Entry0.Hash ? l.Entry0 :
                    hash == l.Entry1.Hash ? l.Entry1 :
                    hash == l.Entry2.Hash ? l.Entry2 :
                    null;
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return new Leaf3Plus1(entry, L3);

                var l = L3;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;

                if (hash > e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, e1, e2, entry);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, p, e1, e2, entry);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, e1, p, e2, entry);
                    if (ph < hash)
                        return new Leaf5(e0, e1, e2, p, entry);
                    return new Leaf5(e0, e1, e2, entry, p);
                }

                if (hash < e0.Hash) 
                {
                    if (ph < hash)
                        return new Leaf5(p, entry, e0, e1, e2);
                    if (ph < e0.Hash)
                        return new Leaf5(entry, p, e0, e1, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(entry, e0, p, e1, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(entry, e0, e1, p, e2);
                    return new Leaf5(entry, e0, e1, e2, p);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, entry, e1, e2);
                    if (ph < hash)
                        return new Leaf5(e0, p, entry, e1, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, entry, p, e1, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, entry, e1, p, e2);
                    return new Leaf5(e0, entry, e1, e2, p);
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, e1, entry, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, p, e1, entry, e2);
                    if (ph < hash)
                        return new Leaf5(e0, e1, p, entry, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, e1, entry, p, e2);
                    return new Leaf5(e0, e1, entry, e2, p);
                }

                return
                    hash == e0.Hash ? new Leaf3Plus1(p, new Leaf3(entry, e1, e2)) :
                    hash == e1.Hash ? new Leaf3Plus1(p, new Leaf3(e0, entry, e2)) :
                                      new Leaf3Plus1(p, new Leaf3(e0, e1, entry));
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int hash, Entry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return this;

                var l = L3;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;

                if (hash > e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, e1, e2, entry);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, p, e1, e2, entry);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, e1, p, e2, entry);
                    if (ph < hash)
                        return new Leaf5(e0, e1, e2, p, entry);
                    return new Leaf5(e0, e1, e2, entry, p);
                }

                if (hash < e0.Hash) 
                {
                    if (ph < hash)
                        return new Leaf5(p, entry, e0, e1, e2);
                    if (ph < e0.Hash)
                        return new Leaf5(entry, p, e0, e1, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(entry, e0, p, e1, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(entry, e0, e1, p, e2);
                    return new Leaf5(entry, e0, e1, e2, p);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, entry, e1, e2);
                    if (ph < hash)
                        return new Leaf5(e0, p, entry, e1, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, entry, p, e1, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, entry, e1, p, e2);
                    return new Leaf5(e0, entry, e1, e2, p);
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Leaf5(p, e0, e1, entry, e2);
                    if (ph < e1.Hash)
                        return new Leaf5(e0, p, e1, entry, e2);
                    if (ph < hash)
                        return new Leaf5(e0, e1, p, entry, e2);
                    if (ph < e2.Hash)
                        return new Leaf5(e0, e1, entry, p, e2);
                    return new Leaf5(e0, e1, entry, e2, p);
                }

                return this;
            }

            /// <inheritdoc />
            public override ImMap234<V> RemoveEntry(int hash)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return L3;

                var l = L3;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;

                if (hash == e0.Hash)
                    return ph < e1.Hash ? new Leaf3(p, e1, e2) :
                           ph < e2.Hash ? new Leaf3(e1, p, e2) :
                                          new Leaf3(e1, e2, p);
                if (hash == e1.Hash)
                    return ph < e0.Hash ? new Leaf3(p, e0, e2) :
                           ph < e2.Hash ? new Leaf3(e0, p, e2) :
                                          new Leaf3(e0, e2, p);
                if (hash == e2.Hash)
                    return ph < e0.Hash ? new Leaf3(p, e0, e1) :
                           ph < e1.Hash ? new Leaf3(e0, p, e1) :
                                          new Leaf3(e0, e1, p);
                return this;
            }

            internal static void SortEntriesByHash(ref Entry e0, ref Entry e1, ref Entry e2, ref Entry p)
            {
                var h = p.Hash;
                Entry swap = null;
                if (h < e2.Hash)
                {
                    swap = e2; e2 = p; p = swap;
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

        /// <summary>Leaf with 5 entries</summary>
        public sealed class Leaf5 : ImMap234<V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Middle Left entry</summary>
            public readonly Entry Entry1;
            /// <summary>Middle entry</summary>
            public readonly Entry Entry2;
            /// <summary>Middle Right entry</summary>
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
            public override string ToString() => "leaf5{" + Entry0 + "; " + Entry1 + "; " + Entry2 + "; " + Entry3 + "; " + Entry4 + "}";
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
            public override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry) =>
                hash == Entry0.Hash ? new Leaf5(entry, Entry1, Entry2, Entry3, Entry4) :
                hash == Entry1.Hash ? new Leaf5(Entry0, entry, Entry2, Entry3, Entry4) :
                hash == Entry2.Hash ? new Leaf5(Entry0, Entry1, entry, Entry3, Entry4) :
                hash == Entry3.Hash ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry4) :
                hash == Entry4.Hash ? new Leaf5(Entry0, Entry1, Entry2, Entry3, entry) :
                (ImMap234<V>)new Leaf5Plus1(entry, this);

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int hash, Entry entry) => 
                hash == Entry0.Hash || hash == Entry1.Hash || hash == Entry2.Hash || hash == Entry3.Hash || hash == Entry4.Hash ? this
                : (ImMap234<V>)new Leaf5Plus1(entry, this);

            /// <inheritdoc />
            public override ImMap234<V> RemoveEntry(int hash)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                var e4 = Entry4;
                if (hash == e0.Hash)
                    return new Leaf3Plus1(e4, new Leaf3(e1, e2, e3));
                if (hash == e1.Hash)
                    return new Leaf3Plus1(e4, new Leaf3(e0, e2, e3));
                if (hash == e2.Hash)
                    return new Leaf3Plus1(e4, new Leaf3(e0, e1, e3));
                if (hash == e3.Hash)
                    return new Leaf3Plus1(e4, new Leaf3(e0, e1, e2));
                if (hash == e4.Hash)
                    return new Leaf3Plus1(e3, new Leaf3(e0, e1, e2));
                return this;
            }
        }

        /// <summary>Leaf with 5 entries</summary>
        public sealed class Leaf5Plus1 : ImMap234<V>
        {
            /// <summary>Plus entry</summary>
            public readonly Entry Plus;
            /// <summary>Dangling Leaf5</summary>
            public readonly Leaf5 L5;

            /// <summary>Constructs the leaf</summary>
            public Leaf5Plus1(Entry plus, Leaf5 l5)
            {
                Plus = plus;
                L5   = l5;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf5+1{" + Plus + " + " + L5 + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                if (hash == Plus.Hash) 
                    return Plus; 
                var l = L5;
                return 
                    hash == l.Entry0.Hash ? l.Entry0 :
                    hash == l.Entry1.Hash ? l.Entry1 :
                    hash == l.Entry2.Hash ? l.Entry2 :
                    hash == l.Entry3.Hash ? l.Entry3 :
                    hash == l.Entry4.Hash ? l.Entry4 :
                    null;
                    
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return new Leaf5Plus1(entry, L5);

                var l = L5;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;
                var e3 = l.Entry3;
                var e4 = l.Entry4;

                if (hash == e0.Hash) 
                    return new Leaf5Plus1(p, new Leaf5(entry, e1, e2, e3, e4));
                if (hash == e1.Hash) 
                    return new Leaf5Plus1(p, new Leaf5(e0, entry, e2, e3, e4));
                if (hash == e2.Hash)
                    return new Leaf5Plus1(p, new Leaf5(e0, e1, entry, e3, e4));
                if (hash == e3.Hash) 
                    return new Leaf5Plus1(p, new Leaf5(e0, e1, e2, entry, e4));
                if (hash == e4.Hash)
                    return new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, entry));

                return new Leaf5Plus1Plus1(entry, this);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int hash, Entry entry) =>
                Plus.Hash == hash || hash == L5.Entry0.Hash || hash == L5.Entry1.Hash || hash == L5.Entry2.Hash || hash == L5.Entry3.Hash || hash == L5.Entry4.Hash
                ? this : (ImMap234<V>)new Leaf5Plus1Plus1(entry, this);

            /// <inheritdoc />
            public override ImMap234<V> RemoveEntry(int hash)
            {
                var p  = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return L5;

                var l = L5;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;
                var e3 = l.Entry3;
                var e4 = l.Entry4;

                if (hash == e0.Hash)
                    return ph < e1.Hash ? new Leaf5(p, e1, e2, e3, e4) :
                           ph < e2.Hash ? new Leaf5(e1, p, e2, e3, e4) :
                           ph < e3.Hash ? new Leaf5(e1, e2, p, e3, e4) :
                           ph < e4.Hash ? new Leaf5(e1, e2, e3, p, e4) :
                                          new Leaf5(e1, e2, e3, e4, p);

                if (hash == e1.Hash)
                    return ph < e0.Hash ? new Leaf5(p, e0, e2, e3, e4) :
                           ph < e2.Hash ? new Leaf5(e0, p, e2, e3, e4) :
                           ph < e3.Hash ? new Leaf5(e0, e2, p, e3, e4) :
                           ph < e4.Hash ? new Leaf5(e0, e2, e3, p, e4) :
                                          new Leaf5(e0, e2, e3, e4, p);

                if (hash == e2.Hash)
                    return ph < e0.Hash ? new Leaf5(p, e0, e1, e3, e4) :
                           ph < e1.Hash ? new Leaf5(e0, p, e1, e3, e4) :
                           ph < e3.Hash ? new Leaf5(e0, e1, p, e3, e4) :
                           ph < e4.Hash ? new Leaf5(e0, e1, e3, p, e4) :
                                          new Leaf5(e0, e1, e3, e4, p);

                if (hash == e3.Hash)
                    return ph < e0.Hash ? new Leaf5(p, e0, e1, e2, e4) :
                           ph < e1.Hash ? new Leaf5(e0, p, e1, e2, e4) :
                           ph < e2.Hash ? new Leaf5(e0, e1, p, e2, e4) :
                           ph < e4.Hash ? new Leaf5(e0, e1, e2, p, e4) :
                                          new Leaf5(e0, e1, e2, e4, p);

                if (hash == e4.Hash)
                    return ph < e0.Hash ? new Leaf5(p, e0, e1, e2, e3) :
                           ph < e1.Hash ? new Leaf5(e0, p, e1, e2, e3) :
                           ph < e2.Hash ? new Leaf5(e0, e1, p, e2, e3) :
                           ph < e4.Hash ? new Leaf5(e0, e1, e2, p, e3) :
                                          new Leaf5(e0, e1, e2, e3, p);
                return this;
            }

            internal static void SortEntriesByHash(ref Entry e0, ref Entry e1, ref Entry e2, ref Entry e3, ref Entry e4, ref Entry p)
            {
                var h = p.Hash;
                Entry swap = null;
                if (h < e4.Hash)
                {
                    swap = e4; e4 = p; p = swap;
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
        }

        /// <summary>Leaf with 5 entries</summary>
        public sealed class Leaf5Plus1Plus1 : ImMap234<V>
        {
            /// <summary>Plus entry</summary>
            public readonly Entry Plus;
            /// <summary>Dangling Leaf5</summary>
            public readonly Leaf5Plus1 L;
            /// <summary>Constructs the leaf</summary>
            public Leaf5Plus1Plus1(Entry plus, Leaf5Plus1 l)
            {
                Plus = plus;
                L    = l;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "leaf5+1+1{" + Plus + " + " + L + "}";
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                if (hash == Plus.Hash)
                    return Plus;
                if (hash == L.Plus.Hash)
                    return L.Plus;
                var l = L.L5;
                return 
                    hash == l.Entry0.Hash ? l.Entry0 :
                    hash == l.Entry1.Hash ? l.Entry1 :
                    hash == l.Entry2.Hash ? l.Entry2 :
                    hash == l.Entry3.Hash ? l.Entry3 :
                    hash == l.Entry4.Hash ? l.Entry4 :
                    null;
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return new Leaf5Plus1Plus1(entry, L);

                var lp = L.Plus;
                var lph = lp.Hash;
                if (lph == hash)
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(entry, L.L5));

                var l5 = L.L5;
                var e0 = l5.Entry0;
                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash) 
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(entry, e1, e2, e3, e4)));
                if (hash == e1.Hash) 
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, entry, e2, e3, e4)));
                if (hash == e2.Hash)
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, entry, e3, e4)));
                if (hash == e3.Hash) 
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, entry, e4)));
                if (hash == e4.Hash)
                    return new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, entry)));

                Entry e = entry;
                SortEntriesByHash(ref e0, ref e1, ref e2, ref e3, ref e4, ref lp, ref p, ref e);

                return new Branch2(new Leaf5(e0, e1, e2, e3, e4), lp, new Leaf2(p, e));
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int hash, Entry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return this;

                var lp = L.Plus;
                var lph = lp.Hash;
                if (lph == hash)
                    return this;

                var l5 = L.L5;
                var e0 = l5.Entry0;
                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash || hash == e1.Hash || hash == e2.Hash || hash == e3.Hash || hash == e4.Hash)
                    return this;

                Entry e = entry;
                SortEntriesByHash(ref e0, ref e1, ref e2, ref e3, ref e4, ref lp, ref p, ref e);

                return new Branch2(new Leaf5(e0, e1, e2, e3, e4), lp, new Leaf2(p, e));
            }

            /// <summary>The order at the end should be the follwing: <![CDATA[e0 < e1 < e2 < e3 < e4 < lp < p < entry]]></summary>
            internal static void SortEntriesByHash(ref Entry e0, ref Entry e1, ref Entry e2, ref Entry e3, ref Entry e4, ref Entry lp, ref Entry p, ref Entry e)
            {
                var h = lp.Hash;
                Entry swap = null;
                if (h < e4.Hash)
                {
                    swap = e4; e4 = lp; lp = swap;
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
                if (h < lp.Hash)
                {
                    swap = lp; lp = p; p = swap;
                    if (h < e4.Hash)
                    {
                        swap = e4; e4 = lp; lp = swap;
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
                if (e != null) 
                {
                    h = e.Hash; 
                    if (h < p.Hash)
                    {
                        swap = p; p = e; e = swap;
                        if (h < lp.Hash)
                        {
                            swap = lp; lp = p; p = swap;
                            if (h < e4.Hash)
                            {
                                swap = e4; e4 = lp; lp = swap;
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
                    }
                }
            }

            internal static void SortEntriesByHash(ref Entry e0, ref Entry e1, ref Entry e2, ref Entry e3, ref Entry e4, ref Entry lp, ref Entry p)
            {
                var h = lp.Hash;
                Entry swap = null;
                if (h < e4.Hash)
                {
                    swap = e4; e4 = lp; lp = swap;
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
                if (h < lp.Hash)
                {
                    swap = lp; lp = p; p = swap;
                    if (h < e4.Hash)
                    {
                        swap = e4; e4 = lp; lp = swap;
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
            }

            /// <inheritdoc />
            public override ImMap234<V> RemoveEntry(int hash)
            {
                var p  = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return L;

                var lp  = L.Plus;
                var lph = lp.Hash;
                if (lph == hash)
                    return new Leaf5Plus1(p, L.L5);

                var l5 = L.L5;
                var e0 = l5.Entry0;
                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash)
                    return lph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e1, e2, e3, e4)) :
                           lph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e1, lp, e2, e3, e4)) :
                           lph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e1, e2, lp, e3, e4)) :
                           lph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e1, e2, e3, lp, e4)) :
                                           new Leaf5Plus1(p, new Leaf5(e1, e2, e3, e4, lp));

                if (hash == e1.Hash)
                    return lph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e0, e2, e3, e4)) :
                           lph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, lp, e2, e3, e4)) :
                           lph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e2, lp, e3, e4)) :
                           lph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e2, e3, lp, e4)) :
                                           new Leaf5Plus1(p, new Leaf5(e0, e2, e3, e4, lp));

                if (hash == e2.Hash)
                    return lph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e0, e1, e3, e4)) :
                           lph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, lp, e1, e3, e4)) :
                           lph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, lp, e3, e4)) :
                           lph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e3, lp, e4)) :
                                           new Leaf5Plus1(p, new Leaf5(e0, e1, e3, e4, lp));

                if (hash == e3.Hash)
                    return lph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e0, e1, e2, e4)) :
                           lph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, lp, e1, e2, e4)) :
                           lph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, lp, e2, e4)) :
                           lph < e4.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, lp, e4)) :
                                           new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e4, lp));

                if (hash == e4.Hash)
                    return lph < e0.Hash ? new Leaf5Plus1(p, new Leaf5(lp, e0, e1, e2, e3)) :
                           lph < e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, lp, e1, e2, e3)) :
                           lph < e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, lp, e2, e3)) :
                           lph < e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, lp, e3)) :
                                           new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, lp));
                return this;
            }
        }

        /// <summary>Base type for the Branch2 and Branch3</summary>
        public abstract class Branch : ImMap234<V> {} 

        /// <summary>Branch of 2 leafs or branches</summary>
        public sealed class Branch2 : Branch
        {
            /// <summary>Left branch</summary>
            public readonly ImMap234<V> Left;
            /// <summary>Entry in the middle</summary>
            public readonly Entry Entry0;
            /// <summary>Right branch</summary>
            public readonly ImMap234<V> Right;

            /// <summary>Constructs</summary>
            public Branch2(ImMap234<V> left, Entry e, ImMap234<V> right)
            {
                Debug.Assert(Left != Empty);
                Debug.Assert(Left is Entry == false);
                Debug.Assert(Right != Empty);
                Debug.Assert(Right is Entry == false);
                Debug.Assert(Left is Branch ? Right is Branch : Right is Branch == false, 
                    "the all Branch2 branches should be either leafs or the branches");
                Entry0 = e;
                Left   = left;
                Right  = right;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() =>
                !(Left is Branch2) && !(Left is Branch3) ? Left + " <- " + Entry0 + " -> " + Right : 
                  Left.GetType().Name + " <- " + Entry0 + " -> " + Right.GetType().Name;
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash > Entry0.Hash ? Right.GetEntryOrDefault(hash) :
                hash < Entry0.Hash ? Left .GetEntryOrDefault(hash) :
                Entry0 is RemovedEntry ? null : Entry0;

            /// <inheritdoc />
            public override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry)
            {
                var e0 = Entry0;
                if (hash > e0.Hash)
                {
                    var right = Right;
                    var newRight = Right.AddOrUpdateEntry(hash, entry);
                    if ((right is Branch3 || right is Leaf5Plus1Plus1) && newRight is Branch3 == false && newRight is Branch2 b2)
                        return new Branch3(Left, e0, b2);
                    return new Branch2(Left, e0, newRight);
                }

                if (hash < e0.Hash)
                {
                    var left = Left;
                    var newLeft = left.AddOrUpdateEntry(hash, entry);
                    if ((left is Branch3 || left is Leaf5Plus1Plus1) && newLeft is Branch2 == false && newLeft is Branch2 b2)
                        return new Branch3(b2.Left, b2.Entry0, new Branch2(b2.Right, e0, Right));
                    return new Branch2(newLeft, e0, Right);
                }

                return new Branch2(Left, entry, Right);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int hash, Entry entry)
            {
                var e0 = Entry0;
                if (hash > e0.Hash)
                {
                    var right = Right;
                    var newRight = right.AddOrKeepEntry(hash, entry);
                    if (newRight == right)
                        return this;
                    if ((right is Branch3 || right is Leaf5Plus1Plus1) && newRight is Branch3 == false && newRight is Branch2 b2)
                        return new Branch3(Left, e0, b2);
                    return new Branch2(Left, e0, newRight);
                }

                if (hash < e0.Hash)
                {
                    var left = Left;
                    var newLeft = left.AddOrKeepEntry(hash, entry);
                    if (newLeft == left)
                        return this;
                    if ((left is Branch3 || left is Leaf5Plus1Plus1) && newLeft is Branch2 == false && newLeft is Branch2 b2)
                        return new Branch3(b2.Left, b2.Entry0, new Branch2(b2.Right, e0, Right));
                    return new Branch2(newLeft, e0, Right);
                }

                return this;
            }

            /// <inheritdoc />
            public override ImMap234<V> RemoveEntry(int hash)
            {
                // Despite all the visible complexity of the method the simple check should be 
                // that all of the non-removed nodes are used when constructing the result.

                var e0 = Entry0;
                if (hash > e0.Hash) 
                {
                    //        4
                    //      /   \
                    //  1 2 3   5 [6]

                    var newRight = Right.RemoveEntry(hash);
                    if (newRight == Right)
                        return this;

                    if (newRight is Entry re)
                    {
                        var l = Left;
                        // If the Left is not a Leaf2, move its one entry to the Right
                        if (l is Leaf3 l3)
                            return e0 is RemovedEntry ?
                                (ImMap234<V>)new Leaf3Plus1(re, l3) :
                                new Branch2(new Leaf2(l3.Entry0, l3.Entry1), l3.Entry2, new Leaf2(e0, re));

                        if (l is Leaf3Plus1 l31)
                        {
                            var p  = l31.Plus;
                            var ll = l31.L3;
                            var lle0 = ll.Entry0;
                            var lle1 = ll.Entry1;
                            var lle2 = ll.Entry2;
                            Leaf3Plus1.SortEntriesByHash(ref lle0, ref lle1, ref lle2, ref p);
                            return e0 is RemovedEntry 
                                ? (ImMap234<V>)new Leaf5(lle0, lle1, lle2, p, re)
                                : new Branch2(ll, p, new Leaf2(e0, re));
                        }

                        if (l is Leaf5 l5)
                            return e0 is RemovedEntry 
                                ? new Branch2(new Leaf3(l5.Entry0, l5.Entry1, l5.Entry2), l5.Entry3, new Leaf2(l5.Entry4, re))
                                : new Branch2(new Leaf3(l5.Entry0, l5.Entry1, l5.Entry2), l5.Entry3, new Leaf3(l5.Entry4, e0, re));

                        if (l is Leaf5Plus1 l51)
                        {
                            var p  = l51.Plus;
                            var ll = l51.L5;
                            var lle0 = ll.Entry0;
                            var lle1 = ll.Entry1;
                            var lle2 = ll.Entry2;
                            var lle3 = ll.Entry3;
                            var lle4 = ll.Entry4;
                            Leaf5Plus1.SortEntriesByHash(ref lle0, ref lle1, ref lle2, ref lle3, ref lle4, ref p);
                            return e0 is RemovedEntry
                                ? new Branch2(new Leaf3(lle0, lle1, lle2), lle3, new Leaf3(lle4, p, re))
                                : new Branch2(new Leaf5(lle0, lle1, lle2, lle3, lle4), p, new Leaf2(e0, re));
                        }

                        if (l is Leaf5Plus1Plus1 l511)
                        {
                            var p  = l511.Plus;
                            var lp  = l511.L.Plus;
                            var ll = l511.L.L5;
                            var lle0 = ll.Entry0;
                            var lle1 = ll.Entry1;
                            var lle2 = ll.Entry2;
                            var lle3 = ll.Entry3;
                            var lle4 = ll.Entry4;

                            Leaf5Plus1Plus1.SortEntriesByHash(ref lle0, ref lle1, ref lle2, ref lle3, ref lle4, ref lp, ref p);
                            return e0 is RemovedEntry
                                ? new Branch2(new Leaf5(lle0, lle1, lle2, lle3, lle4), lp, new Leaf2(p, re))
                                : new Branch2(new Leaf5(lle0, lle1, lle2, lle3, lle4), lp, new Leaf3(p, e0, re));
                        }

                        // Case #1
                        // If the Left is Leaf2 -> reduce the whole branch to the Leaf4 and rely on the upper branch (if any) to balance itself,
                        // see this case handled below..
                        var l2 = (Leaf2)l;
                        return e0 is RemovedEntry
                            ? (ImMap234<V>)new Leaf3(l2.Entry0, l2.Entry1, re)
                            : new Leaf3Plus1(l2.Entry0, new Leaf3(l2.Entry1, e0, re));
                    }

                    // Handling Case #1
                    if (Right is Branch2 && newRight is Branch == false)
                    {
                        // Case #2
                        //             7                       4     7 
                        //          /      \                 /    |     \
                        //        4      8 9 10 11  =>   1 2 3   5 6   8 9 10 11
                        //      /   \                    
                        //   1 2 3   5 6                  
                        // The result tree height is decreased, so we should not forget to rebalance with the other part of the tree on the upper level
                        // see the case handled below...

                        if (Left is Branch2 lb2)
                            return new Branch3(lb2.Left, lb2.Entry0, new Branch2(lb2.Right, e0, newRight));

                        //                     10                            7
                        //              /           \                     /     \
                        //        4      7        11 12 13 14 =>       4          10
                        //      /     |    \                         /    \     /    \
                        //   1 2 3   5 6    8 9                   1 2 3   5 6|8 9   11 12 13 14

                        if (Left is Branch3 lb3) // the result tree height is the same - no need to rebalance
                            return new Branch2(new Branch2(lb3.Left, lb3.Entry0, lb3.Middle), lb3.Entry1, new Branch2(lb3.Right, e0, newRight));
                    }

                    // Handling the Case #2
                    if (Right is Branch2 && newRight is Branch3 rb3)
                    {
                        //         0                                  -10        0
                        //       /         \                          /     |          \                
                        //   -10           4     7                  a       b          4     7          
                        //  /   \        /    |     \               |       |        /    |     \       
                        // a     b    1 2 3   5 6   8 9 10 11   =>  ?       ?     1 2 3   5 6  8 9 10 11
                        // |     |
                        // ?     ?

                        if (Left is Branch2 lb2) 
                            return new Branch3(lb2.Left, lb2.Entry0, new Branch2(lb2.Right, e0, newRight));

                        //              0                                       -5                            
                        //       /              \                              /     \                        
                        //   -10  -5            4      7                  -10           0                     
                        //  /   |   \          /    |     \              /   |      /        \                
                        // a    b    c   1 2 3     5 6   8 9 10 11  =>  a    b     c         4     7          
                        // |    |    |                                  |    |     |       /    |     \       
                        // ?    ?    ?                                  ?    ?     ?    1 2 3  5 6   8 9 10 11

                        if (Left is Branch3 lb3)
                            return new Branch2(new Branch2(lb3.Left, lb3.Entry0, lb3.Middle), lb3.Entry1, new Branch2(lb3.Right, e0, newRight));
                    }

                    return new Branch2(Left, e0, newRight);
                }

                if (hash < e0.Hash)
                {
                    //        4
                    //      /   \
                    //   1 [2]    5 6 7

                    var newLeft = Left.RemoveEntry(hash);
                    if (newLeft == Left)
                        return this;

                    if (newLeft is Entry le)
                    {
                        var r = Right;
                        if (r is Leaf3 l3)
                            return e0 is RemovedEntry 
                                ? (ImMap234<V>)new Leaf3Plus1(le, l3)
                                : new Branch2(new Leaf2(le, e0), l3.Entry0, new Leaf2(l3.Entry1, l3.Entry2));

                        if (r is Leaf3Plus1 l31)
                        {
                            var p = l31.Plus;
                            var ll = l31.L3;
                            var lle0 = ll.Entry0;
                            var lle1 = ll.Entry1;
                            var lle2 = ll.Entry2;

                            Leaf3Plus1.SortEntriesByHash(ref lle0, ref lle1, ref lle2, ref p);

                            return e0 is RemovedEntry
                                ? (ImMap234<V>)new Leaf5(le, lle0, lle1, lle2, p)
                                : new Branch2(new Leaf2(le, e0), lle0, new Leaf3(lle1, lle2, p));
                        }

                        if (r is Leaf5 l5)
                            return e0 is RemovedEntry
                                ? new Branch2(new Leaf2(le, l5.Entry0),     l5.Entry1, new Leaf3(l5.Entry2, l5.Entry3, l5.Entry4))
                                : new Branch2(new Leaf3(le, e0, l5.Entry0), l5.Entry1, new Leaf3(l5.Entry2, l5.Entry3, l5.Entry4));

                        if (r is Leaf5Plus1 l51)
                        {
                            var p  = l51.Plus;
                            var ll = l51.L5;
                            var lle0 = ll.Entry0;
                            var lle1 = ll.Entry1;
                            var lle2 = ll.Entry2;
                            var lle3 = ll.Entry3;
                            var lle4 = ll.Entry4;

                            Leaf5Plus1.SortEntriesByHash(ref lle0, ref lle1, ref lle2, ref lle3, ref lle4, ref p);

                            return e0 is RemovedEntry 
                                ? new Branch2(new Leaf3(le, lle0, lle1), lle2, new Leaf3(lle3, lle4, p))
                                : new Branch2(new Leaf2(le, e0), lle0, new Leaf5(lle1, lle2, lle3, lle4, p));
                        }

                        if (r is Leaf5Plus1Plus1 l511)
                        {
                            var p  = l511.Plus;
                            var lp  = l511.L.Plus;
                            var ll = l511.L.L5;
                            var lle0 = ll.Entry0;
                            var lle1 = ll.Entry1;
                            var lle2 = ll.Entry2;
                            var lle3 = ll.Entry3;
                            var lle4 = ll.Entry4;

                            Leaf5Plus1Plus1.SortEntriesByHash(ref lle0, ref lle1, ref lle2, ref lle3, ref lle4, ref lp, ref p);

                            return e0 is RemovedEntry
                                ? new Branch2(new Leaf2(le, lle0),     lle1, new Leaf5(lle2, lle3, lle4, lp, p))
                                : new Branch2(new Leaf3(le, e0, lle0), lle1, new Leaf5(lle2, lle3, lle4, lp, p));
                        }

                        // Case #1
                        // If the Left is Leaf2 -> reduce the whole branch to the Leaf31 and rely on the upper branch (if any) to balance itself,
                        // see this case handled below..
                        var l2 = (Leaf2)r;
                        return e0 is RemovedEntry
                            ? (ImMap234<V>)   new Leaf3(le, l2.Entry0, l2.Entry1)
                            : new Leaf3Plus1(le, new Leaf3(e0, l2.Entry0, l2.Entry1));
                    }

                    // Handling the Case #1
                    if (Left is Branch2 && newLeft is Branch == false) 
                    {
                        // Case #2 (for the right branch)
                        //             7                       4     7 
                        //          /      \                 /    |     \
                        //        4      8 9 10 11  =>   1 2 3   5 6   8 9 10 11
                        //      /   \                    
                        //   1 2 3   5 6                  
                        // The result tree height is decreased, so we should not forget to rebalance with the other part of the tree on the upper level
                        // see the case handled below...

                        if (Right is Branch2 lb2)
                            return new Branch3(newLeft, e0, lb2);

                        //                     10                            7
                        //              /           \                     /     \
                        //        4      7        11 12 13 14 =>       4          10
                        //      /     |    \                         /    \     /    \
                        //   1 2 3   5 6    8 9                   1 2 3   5 6|8 9   11 12 13 14

                        if (Right is Branch3 lb3) // the result tree height is the same - no need to rebalance
                            return new Branch2(new Branch2(newLeft, e0, lb3.Left), lb3.Entry0, new Branch2(lb3.Middle, lb3.Entry1, lb3.Right));
                    }

                    // Handling the Case #2
                    if (Left is Branch2 && newLeft is Branch3 rb3)
                    {
                        //         0                                  -10        0
                        //       /         \                          /     |          \                
                        //   -10           4     7                  a       b          4     7          
                        //  /   \        /    |     \               |       |        /    |     \       
                        // a     b    1 2 3   5 6   8 9 10 11   =>  ?       ?     1 2 3   5 6  8 9 10 11
                        // |     |
                        // ?     ?

                        if (Right is Branch2 lb2)
                            return new Branch3(newLeft, e0, lb2);

                        //              0                                       -5                            
                        //       /              \                              /     \                        
                        //   -10  -5            4      7                  -10           0                     
                        //  /   |   \          /    |     \              /   |      /        \                
                        // a    b    c   1 2 3     5 6   8 9 10 11  =>  a    b     c         4     7          
                        // |    |    |                                  |    |     |       /    |     \       
                        // ?    ?    ?                                  ?    ?     ?    1 2 3  5 6   8 9 10 11

                        if (Left is Branch3 lb3)
                            return new Branch2(new Branch2(newLeft, e0, lb3.Left), lb3.Entry0, new Branch2(lb3.Middle, lb3.Entry1, lb3.Right));
                    }

                    return new Branch2(newLeft, e0, Right);
                }

                return new Branch2(Left, new RemovedEntry(e0.Hash), Right);
            }
        }

        /// <summary>Branch of 3 leafs or branches and two entries</summary>
        public sealed class Branch3 : Branch
        {
            /// <summary>Left branch</summary>
            public readonly ImMap234<V> Left;
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>The middle and right is represented by the Branch2 to simplify the Enumeration implementation,
            /// so we always deal with the binary tree. But for the outside the use of Branch2 is just an internal detail.</summary>
            public readonly Branch2 RightBranch;
            /// <summary>Middle branch</summary>
            public ImMap234<V> Middle => RightBranch.Left;
            /// <summary>Right entry</summary>
            public Entry Entry1 => RightBranch.Entry0;
            /// <summary>Rightmost branch</summary>
            public ImMap234<V> Right => RightBranch.Right;

            /// <summary>Constructs the branch</summary>
            public Branch3(ImMap234<V> left, Entry entry0, Branch2 rightBranch)
            {
                Debug.Assert(Left != Empty);
                Debug.Assert(Left is Entry == false);
                Debug.Assert(entry0.Hash < RightBranch.Entry0.Hash, "entry0.Hash < RightBranch.Entry0.Hash");
                Debug.Assert(Left is Branch ? rightBranch.Left is Branch : rightBranch.Left is Branch == false, 
                    "the all Branch3 branches should be either leafs or the branches");
                Entry0 = entry0;
                Left   = left;
                RightBranch = rightBranch;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() =>
                !(Left is Branch2) && !(Left is Branch3) ? Left + " <- " + Entry0 + " -> " + Middle + " <- " + Entry1 + " -> " + Right : 
                Left.GetType().Name + " <- " + Entry0 + " -> " + Middle.GetType().Name + " <- " + Entry1 + " -> " + Right.GetType().Name;
#endif

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash)
            {
                var h0 = Entry0.Hash;
                var h1 = Entry1.Hash;
                return
                    hash > h1 ? Right.GetEntryOrDefault(hash) :
                    hash < h0 ? Left .GetEntryOrDefault(hash) :
                    hash == h0 ? (Entry0 is RemovedEntry ? null : Entry0) :
                    hash == h1 ? (Entry1 is RemovedEntry ? null : Entry1) :
                    Middle.GetEntryOrDefault(hash);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrUpdateEntry(int hash, Entry entry)
            {
                var h0 = Entry0.Hash;
                var h1 = Entry1.Hash;
                
                if (hash > h1)
                {
                     // No need to call the Split method because we won't destruct the result branch
                    var old = Right;
                    var aNew = old.AddOrUpdateEntry(hash, entry);
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1Plus1))
                        return new Branch2(new Branch2(Left, Entry0, Middle), Entry1, aNew);
                    return new Branch3(Left, Entry0, new Branch2(Middle, Entry1, aNew));
                }

                if (hash < h0)
                {
                    var old = Left;
                    var aNew = old.AddOrUpdateEntry(hash, entry);
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1Plus1))
                        return new Branch2(aNew, Entry0, RightBranch);
                    return new Branch3(aNew, Entry0, RightBranch);
                }

                if (hash > h0 && hash < h1)
                {
                    var old = Middle;
                    var aNew = old.AddOrUpdateEntry(hash, entry);
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1Plus1))
                        return new Branch2(new Branch2(Left, Entry0, b2.Left), b2.Entry0, new Branch2(b2.Right, Entry1, Right));
                    return new Branch3(Left, Entry0, new Branch2(aNew, Entry1, Right));
                }

                return hash == h0
                    ? new Branch3(Left, entry, RightBranch)
                    : new Branch3(Left, Entry0, new Branch2(Middle, entry, Right));
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int hash, Entry entry)
            {
                // todo: @perf apply the same hash to var refactoring as for AddOrUpdateEntry
                var e0 = Entry0;
                var e1 = Entry1;

                if (hash > e1.Hash)
                {
                    var old = Right;
                    var aNew = old.AddOrKeepEntry(hash, entry);
                    if (aNew == old)
                        return this;
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1))
                        return new Branch2(new Branch2(Left, Entry0, Middle), Entry1, aNew);
                    return new Branch3(Left, Entry0, new Branch2(Middle, Entry1, aNew));
                }

                if (hash < e0.Hash)
                {
                    var old = Left;
                    var aNew = old.AddOrKeepEntry(hash, entry);
                    if (aNew == old)
                        return this;
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1))
                        return new Branch2(aNew, Entry0, RightBranch);
                    return new Branch3(aNew, Entry0, RightBranch);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    var old = Middle;
                    var aNew = old.AddOrKeepEntry(hash, entry);
                    if (aNew == old)
                        return this;
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1))
                        return new Branch2(new Branch2(Left, Entry0, b2.Left), b2.Entry0, new Branch2(b2.Right, Entry1, Right));
                    return new Branch3(Left, Entry0, new Branch2(aNew, Entry1, Right));
                }

                return this;
            }

            /// <inheritdoc />
            public override ImMap234<V> RemoveEntry(int hash)
            {
                var e1 = Entry1;
                if (hash > e1.Hash)
                {
                    var newRight = Right.RemoveEntry(hash);
                    if (newRight == Right)
                        return this;

                    // if we done to the single entry - rebalance the entries
                    if (newRight is Entry re)
                    {
                        //      3       7               3
                        //    /     |     \    =>    /      \
                        //  1 2   3 5 6     8      1 2   3 5 6 7 8
                        var m = Middle;

                        // If the Middle is Leaf2 or Leaf3 - merge the Middle with new Right to the Branch2
                        if (m is Leaf2 l2)
                            return new Branch2(Left, Entry0, new Leaf3Plus1(l2.Entry0, new Leaf3(l2.Entry1, e1, re)));
                        if (m is Leaf3 l3)
                            return new Branch2(Left, Entry0, new Leaf5(l3.Entry0, l3.Entry1, l3.Entry2, e1, re));

                        // Rebalance the entries from Middle to the Right
                        if (m is Leaf3Plus1 l4)
                        {
                            var p = l4.Plus;
                            var ph = p.Hash;
                            var l4l3  = l4.L3;
                            var l3e0 = l4l3.Entry0;
                            var l3e1 = l4l3.Entry1;
                            var l3e2 = l4l3.Entry2;

                            if (ph > l3e2.Hash)
                                return new Branch3(Left, Entry0, new Branch2(l4l3, p, new Leaf2(e1, re)));
                            if (ph < l3e0.Hash)
                                return new Branch3(Left, Entry0, new Branch2(new Leaf3(p, l3e0, l3e1), l3e2, new Leaf2(e1, re)));
                            if (ph < l3e1.Hash)
                                return new Branch3(Left, Entry0, new Branch2(new Leaf3(l3e0, p, l3e1), l3e2, new Leaf2(e1, re)));
                            return new Branch3(Left, Entry0, new Branch2(new Leaf3(l3e0, l3e1, p), l3e2, new Leaf2(e1, re)));
                        }

                        if (m is Leaf5 l5) 
                            return new Branch3(Left, Entry0,
                                new Branch2(new Leaf3Plus1(l5.Entry0, new Leaf3(l5.Entry1, l5.Entry2, l5.Entry3)), l5.Entry4, new Leaf2(e1, re)));

                        {
                            var l6 = (Leaf5Plus1)m;
                            var p  = l6.Plus;
                            var ph = p.Hash;
                            var l6l5 = l6.L5;
                            var l5e0 = l6l5.Entry0;
                            var l5e1 = l6l5.Entry1;
                            var l5e2 = l6l5.Entry2;
                            var l5e3 = l6l5.Entry3;
                            var l5e4 = l6l5.Entry4;

                            if (ph > l5e4.Hash)
                                return new Branch3(Left, Entry0, new Branch2(l6l5, p, new Leaf2(e1, re)));
                            if (ph < l5e0.Hash)
                                return new Branch3(Left, Entry0, new Branch2(new Leaf5(p, l5e0, l5e1, l5e2, l5e3), l5e4, new Leaf2(e1, re)));
                            if (ph < l5e1.Hash)
                                return new Branch3(Left, Entry0, new Branch2(new Leaf5(l5e0, p, l5e1, l5e2, l5e3), l5e4, new Leaf2(e1, re)));
                            if (ph < l5e2.Hash)
                                return new Branch3(Left, Entry0, new Branch2(new Leaf5(l5e0, l5e1, p, l5e2, l5e3), l5e4, new Leaf2(e1, re)));
                            if (ph < l5e3.Hash)
                                return new Branch3(Left, Entry0, new Branch2(new Leaf5(l5e0, l5e1, l5e2, p, l5e3), l5e4, new Leaf2(e1, re)));
                            return new Branch3(Left, Entry0, new Branch2(new Leaf5(l5e0, l5e1, l5e2, l5e3, p), l5e4, new Leaf2(e1, re)));
                        }
                    }

                    // The only reason for moving from the Branch3 to the Branch2 is the decreased the tree height so we need to rebalance
                    if (newRight is Branch3 && Right is Branch2)
                    {
                        //      1       7                                   1
                        //    /     |         \                           /    \
                        //   ?      b        10    13                   ?     b     7
                        //  / \    / \      /    |    \                 |    /   |     \
                        // ?   ?  a   c   8 9  11 12  14 15  =>         ?   a    c   10   13
                        // |   |  |   |                                 
                        // ?   ?  ?   ?                                                ...
                        if (Middle is Branch2 mb2) // just dangle the new right together with Middle
                            return new Branch2(Left, Entry0, new Branch3(mb2.Left, mb2.Entry0, new Branch2(mb2.Right, e1, newRight)));

                        //      -15             0                                 -15          -5                              
                        //    /         |              \                          /       |         \                          
                        // -20       -10  -5             4      7              -20       -10           0                       
                        //  |       /   |   \          /    |     \             |        /   \       /        \                
                        //  x      a    b    c   1 2 3     5 6   8 9 10 11  =>  ?       a     b     c        4     7           
                        //  |      |    |    |                                  |       |     |     |      /    |     \        
                        //  ?      ?    ?    ?                                  ?       ?     ?     ?    1 2 3  5 6   8 9 10 11
                        if (Middle is Branch3 mb3)
                            return new Branch3(Left, Entry0, 
                                new Branch2(new Branch2(mb3.Left, mb3.Entry0, mb3.Middle), mb3.Entry1, new Branch2(mb3.Right, e1, newRight)));
                    }

                    return new Branch3(Left, Entry0, new Branch2(Middle, Entry1, newRight));
                }

                var e0 = Entry0;
                if (hash < e0.Hash) 
                {
                    // todo: @wip
                }

                if (hash > e0.Hash && hash < e1.Hash) 
                {
                    // todo: @wip
                }

                if (hash == e0.Hash)
                {
                    
                }

                //if (hash == e1.Hash)
                    // todo: @wip


                return this;
            }
        }
    }

    /// <summary>ImMap methods</summary>
    public static class ImMap234
    {
        /// <summary>Enumerates all the map entries from the left to the right and from the bottom to top</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static IEnumerable<ImMap234<V>.Entry> Enumerate<V>(this ImMap234<V> map, 
            List<ImMap234<V>> parentStack = null) // todo: @perf replace the List with the more lightweight alternative, the bad thing that we cannot pass the `ref` array into the method returning IEnumerable
        {
            if (map == ImMap234<V>.Empty)
                yield break;
            if (map is ImMap234<V>.Entry e)
            {
                yield return e;
                yield break;
            }

            var parentIndex = -1;
            while (true)
            {
                if (map is ImMap234<V>.Branch)
                {
                    if (parentStack == null)
                        parentStack = new List<ImMap234<V>>(2);
                    if (++parentIndex >= parentStack.Count)
                        parentStack.Add(map);
                    else
                        parentStack[parentIndex] = map;
                    map = map is ImMap234<V>.Branch2 b2 ? b2.Left : ((ImMap234<V>.Branch3)map).Left;
                    continue;
                }
                
                if (map is ImMap234<V>.Leaf2 l2)
                {
                    yield return l2.Entry0;
                    yield return l2.Entry1;
                }
                else if (map is ImMap234<V>.Leaf3 l3)
                {
                    yield return l3.Entry0;
                    yield return l3.Entry1;
                    yield return l3.Entry2;
                }
                else if (map is ImMap234<V>.Leaf3Plus1 l31)
                {
                    var p = l31.Plus;
                    var ph = p.Hash;
                    var l = l31.L3;
                    var e0  = l.Entry0;
                    var e1  = l.Entry1;
                    var e2  = l.Entry2;

                    ImMap234<V>.Entry swap = null;
                    if (ph < e2.Hash)
                    {
                        swap = e2; e2 = p; p = swap;
                        if (ph < e1.Hash)
                        {
                            swap = e1; e1 = e2; e2 = swap;
                            if (ph < e0.Hash)
                            {
                                swap = e0; e0 = e1; e1 = swap;
                            }
                        }
                    }

                    yield return e0;
                    yield return e1;
                    yield return e2;
                    yield return p;
                }
                else if (map is ImMap234<V>.Leaf5 l5)
                {
                    yield return l5.Entry0;
                    yield return l5.Entry1;
                    yield return l5.Entry2;
                    yield return l5.Entry3;
                    yield return l5.Entry4;
                }
                else if (map is ImMap234<V>.Leaf5Plus1 l51)
                {
                    var p = l51.Plus;
                    var ph = p.Hash;
                    var l = l51.L5;
                    var e0  = l.Entry0;
                    var e1  = l.Entry1;
                    var e2  = l.Entry2;
                    var e3  = l.Entry3;
                    var e4  = l.Entry4;

                    ImMap234<V>.Entry swap = null;
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
                    yield return e0;
                    yield return e1;
                    yield return e2;
                    yield return e3;
                    yield return e4;
                    yield return p;
                }
                else if (map is ImMap234<V>.Leaf5Plus1Plus1 l511)
                {
                    var p   = l511.Plus;
                    var lp  = l511.L.Plus;
                    var l   = l511.L.L5;
                    var e0  = l.Entry0;
                    var e1  = l.Entry1;
                    var e2  = l.Entry2;
                    var e3  = l.Entry3;
                    var e4  = l.Entry4;

                    ImMap234<V>.Leaf5Plus1Plus1.SortEntriesByHash(ref e0, ref e1, ref e2, ref e3, ref e4, ref lp, ref p);

                    yield return e0;
                    yield return e1;
                    yield return e2;
                    yield return e3;
                    yield return e4;
                    yield return lp;
                    yield return p;
                }

                if (parentIndex == -1)
                    break; // we yield the leaf and there is nothing in stack - we are DONE!

                map = parentStack[parentIndex]; // otherwise get the parent
                if (map is ImMap234<V>.Branch2 pb2) 
                {
                    if (pb2.Entry0 is ImMap234<V>.RemovedEntry == false) 
                        yield return pb2.Entry0;

                    map = pb2.Right;
                    --parentIndex; // we done with the this level handled the Left (previously) and the Right (now)
                }
                else 
                {
                    // let's treat the b3 as the b2 tree
                    //                  20           40                                      20
                    //         /               |            \                       /                   \ 
                    //    10    13            30             50         =>     10    13                  40
                    //   /    |    \        /    \         /    \             /    |    \            /            \        
                    // 8 9  11 12  14 15 25 26  35 36   45 46   55 56       8 9  11 12  14 15       30             50      
                    //                                                                            /    \         /    \    
                    //                                                                         25 26  35 36   45 46   55 56
                    var pb3 = (ImMap234<V>.Branch3)map;
                    {
                        if (pb3.Entry0 is ImMap234<V>.RemovedEntry == false)
                            yield return pb3.Entry0;

                        map = pb3.RightBranch;
                        --parentIndex; // we done with the this level handled the Left and the Middle (previously) and the Right (now)
                    }
                }
            }
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        ///  returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap234<V> map, int hash)
        {
            var e = map.GetEntryOrDefault(hash);
            return e != null ? e.Value : default(V);
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        /// returns the `true` and the found value or `false`</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<V>(this ImMap234<V> map, int hash, out V value)
        {
            var e = map.GetEntryOrDefault(hash);
            if (e != null)
            {
                value = e.Value;
                return true;
            }

            value = default(V);
            return false;
        }

        /// <summary>Adds or updates the value by key in the map, always returning the modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> AddOrUpdate<V>(this ImMap234<V> map, int hash, V value) =>
            map.AddOrUpdateEntry(hash, new ImMap234<V>.Entry(hash, value));

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> AddOrKeep<V>(this ImMap234<V> map, int hash, V value) =>
            map.AddOrKeepEntry(hash, new ImMap234<V>.Entry(hash, value));

        /// <summary>Returns the map without the entry with the specified hash and key if it is found in the map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> Remove<V>(this ImMap234<V> map, int hash) => map.RemoveEntry(hash);
    }
}