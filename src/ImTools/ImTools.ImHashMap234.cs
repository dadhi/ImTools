
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections;

namespace ImTools.Experimental
{
    /// <summary>The base class for the tree leafs and branches, also defines the Empty tree</summary>
    public class ImHashMap234<K, V>// : IEnumerable<ImHashMap234<K, V>.ValueEntry>
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
        public virtual ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry) => entry;

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        public virtual ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry) => entry;

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

            internal abstract Entry Update(ValueEntry entry);
            internal abstract Entry Keep(ValueEntry entry);
            /// <summary>returns null if entry is removed completely or modified entry, or the original entry if nothing is removed </summary>
            internal abstract Entry TryRemove<T>(T key);
        }

        /// <summary>Entry containing the Value</summary>
        public sealed class ValueEntry : Entry
        {
            /// <summary>The Key</summary>
            public readonly K Key;

            /// <summary>The value. May be modified if you need the Ref{V} semantics</summary>
            public V Value;

            /// <summary>Constructs the entry with the default value</summary>
            public ValueEntry(int hash, K key) : base(hash) => Key = key;

            /// <summary>Constructs the entry with the key and value</summary>
            public ValueEntry(int hash, K key, V value) :  base(hash)
            { 
                Key   = key;
                Value = value;
            }

#if !DEBUG
            /// <inheritdoc />
            public override string ToString() => "[" + Hash + "]" + Key + ":" + Value;
#endif

            internal override Entry Update(ValueEntry entry) => 
                Key.Equals(entry.Key) ? entry : (Entry)new ConflictsEntry(Hash, this, entry);

            internal override Entry Keep(ValueEntry entry) => 
                Key.Equals(entry.Key) ? this : (Entry)new ConflictsEntry(Hash, this, entry);

            internal override Entry TryRemove<T>(T key) => 
                Key.Equals(key) ? null : this;

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry) =>
                hash > Hash ? new Leaf2(this, entry) :
                hash < Hash ? new Leaf2(entry, this) :
                (ImHashMap234<K, V>)Update(entry);

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry) =>
                hash > Hash ? new Leaf2(this, entry) :
                hash < Hash ? new Leaf2(entry, this) :
                (ImHashMap234<K, V>)Keep(entry);

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key) =>
                hash == Hash ? TryRemove(key) ?? Empty : this;
        }

        /// <summary>Entry containing the Array of conflicting Value entries.</summary>
        public sealed class ConflictsEntry : Entry
        {
            /// <summary>The 2 and more conflicts.</summary>
            public ValueEntry[] Conflicts;

            /// <summary>Constructs the entry with the key and value</summary>
            public ConflictsEntry(int hash, params ValueEntry[] conflicts) : base(hash) => Conflicts = conflicts;

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

            internal override Entry Update(ValueEntry entry) 
            {
                var key = entry.Key;

                var cs = Conflicts;
                var n = cs.Length;
                var i = n - 1;
                while (i != -1 && !key.Equals(cs[i].Key)) --i;

                ValueEntry[] newConflicts;
                if (i != -1) // update the found (existing) conflicted value
                {
                    newConflicts = new ValueEntry[n];
                    Array.Copy(cs, 0, newConflicts, 0, n);
                    newConflicts[i] = entry;
                }
                else // add the new conflicting value
                {
                    newConflicts = new ValueEntry[n + 1];
                    Array.Copy(cs, 0, newConflicts, 0, n);
                    newConflicts[n] = entry;
                }

                return new ConflictsEntry(Hash, newConflicts);
            }

            internal override Entry Keep(ValueEntry entry)
            {
                var key = entry.Key;

                var cs = Conflicts;
                var n = cs.Length;
                var i = n - 1;
                while (i != -1 && !key.Equals(cs[i].Key)) --i;

                ValueEntry[] newConflicts;
                if (i != -1) // return existing map
                    return this;

                newConflicts = new ValueEntry[n + 1];
                Array.Copy(cs, 0, newConflicts, 0, n);
                newConflicts[n] = entry;

                return new ConflictsEntry(Hash, newConflicts);
            }

            internal override Entry TryRemove<T>(T key) 
            {
                var cs = Conflicts;
                var n = cs.Length;
                var i = n - 1;
                while (i != -1 && !key.Equals(cs[i].Key)) --i;
                if (i != -1)
                {
                    if (n == 2)
                        return i == 0 ? cs[1] : cs[0];

                    var newConflicts = new ValueEntry[n -= 1]; // the new n is less by one
                    if (i > 0) // copy the 1st part
                        Array.Copy(cs, 0, newConflicts, 0, i);
                    if (i < n) // copy the 2nd part
                        Array.Copy(cs, i + 1, newConflicts, i, n - i);

                    return new ConflictsEntry(Hash, newConflicts);
                }

                return this;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry) 
            {
                if (hash > Hash)
                    return new Leaf2(this, entry);
                if (hash < Hash) 
                    return new Leaf2(entry, this);
                return Update(entry);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry) 
            {
                if (hash > Hash) 
                    new Leaf2(this, entry);
                if (hash < Hash)
                    return new Leaf2(entry, this);
                return Keep(entry);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key) =>
                hash == Hash ? TryRemove(key) : this;
        }

        /// <summary>Leaf with 2 entries</summary>
        public sealed class Leaf2 : ImHashMap234<K, V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Right entry</summary>
            public readonly Entry Entry1;

            /// <summary>Constructs the leaf</summary>
            public Leaf2(Entry e0, Entry e1)
            {
                Debug.Assert(e0.Hash < e1.Hash);
                Entry0 = e0;
                Entry1 = e1;
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
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e1 = Entry1;
                var e0 = Entry0;
                return
                    hash > e1.Hash                   ? new Leaf3(e0, e1, entry) :
                    hash < e0.Hash                   ? new Leaf3(entry, e0, e1) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf3(e0, entry, e1) :
                    hash == e0.Hash   ? new Leaf2(e0.Update(entry), e1) :
                    (ImHashMap234<K, V>)new Leaf2(e0, e1.Update(entry));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e1 = Entry1;
                var e0 = Entry0;
                return
                    hash > e1.Hash                   ? new Leaf3(e0, e1, entry) :
                    hash < e0.Hash                   ? new Leaf3(entry, e0, e1) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf3(e0, entry, e1) :
                    hash == e0.Hash ?   ((e0 = e0.Keep(entry)) == Entry0 ? this : new Leaf2(e0, e1)) :
                    (ImHashMap234<K, V>)((e1 = e1.Keep(entry)) == Entry1 ? this : new Leaf2(e0, e1));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                if (hash == e0.Hash)
                    return (e0 = e0.TryRemove(key)) == Entry0 ? this : e0 == null ? e1 : (ImHashMap234<K, V>)new Leaf2(e0, e1);
                if (hash == e1.Hash)
                    return (e1 = e1.TryRemove(key)) == Entry1 ? this : e1 == null ? e0 : (ImHashMap234<K, V>)new Leaf2(e0, e1);
                return this;
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
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry) =>
                hash == Entry0.Hash ? new Leaf3(Entry0.Update(entry), Entry1, Entry2) :
                hash == Entry1.Hash ? new Leaf3(Entry0, Entry1.Update(entry), Entry2) :
                hash == Entry2.Hash ? new Leaf3(Entry0, Entry1, Entry2.Update(entry)) :
                (ImHashMap234<K, V>)new Leaf3Plus1(entry, this);

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                return
                    hash == e0.Hash ? ((e0 = e0.Keep(entry)) == Entry0 ? this : new Leaf3(e0, e1, e2)) :
                    hash == e1.Hash ? ((e1 = e1.Keep(entry)) == Entry1 ? this : new Leaf3(e0, e1, e2)) :
                    hash == e2.Hash ? ((e2 = e2.Keep(entry)) == Entry2 ? this : new Leaf3(e0, e1, e2)) :
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
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
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
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.Keep(entry)) == Plus ? this : new Leaf3Plus1(p, L3);

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
                    hash == e0.Hash ? ((e0 = e0.Keep(entry)) == l.Entry0 ? this : (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2))) :
                    hash == e1.Hash ? ((e1 = e1.Keep(entry)) == l.Entry1 ? this : (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2))) :
                                      ((e2 = e2.Keep(entry)) == l.Entry2 ? this : (ImHashMap234<K, V>)new Leaf3Plus1(p, new Leaf3(e0, e1, e2)));
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
                Debug.Assert(e0.Hash < e1.Hash);
                Debug.Assert(e1.Hash < e2.Hash);
                Debug.Assert(e2.Hash < e3.Hash);
                Debug.Assert(e3.Hash < e4.Hash);
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
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry) =>
                hash == Entry0.Hash ? new Leaf5(Entry0.Update(entry), Entry1, Entry2, Entry3, Entry4) :
                hash == Entry1.Hash ? new Leaf5(Entry0, Entry1.Update(entry), Entry2, Entry3, Entry4) :
                hash == Entry2.Hash ? new Leaf5(Entry0, Entry1, Entry2.Update(entry), Entry3, Entry4) :
                hash == Entry3.Hash ? new Leaf5(Entry0, Entry1, Entry2, Entry3.Update(entry), Entry4) :
                hash == Entry4.Hash ? new Leaf5(Entry0, Entry1, Entry2, Entry3, Entry4.Update(entry)) :
                (ImHashMap234<K, V>)new Leaf5Plus1(entry, this);

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                var e4 = Entry4;
                return
                    hash == e0.Hash ? ((e0 = e0.Keep(entry)) == Entry0 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e1.Hash ? ((e1 = e1.Keep(entry)) == Entry1 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e2.Hash ? ((e2 = e2.Keep(entry)) == Entry2 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e3.Hash ? ((e3 = e3.Keep(entry)) == Entry3 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e4.Hash ? ((e4 = e4.Keep(entry)) == Entry4 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
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
        public sealed class Leaf5Plus1 : ImHashMap234<K, V>//
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
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
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
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.Keep(entry)) == Plus ? this : (ImHashMap234<K, V>)new Leaf5Plus1(p, L5);

                var l5 = L5;
                var e0 = l5.Entry0;

                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash)
                     return (e0 = e0.Keep(entry)) == l5.Entry0 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));
                if (hash == e1.Hash)
                    return (e1 = e1.Keep(entry)) == l5.Entry1 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));
                if (hash == e2.Hash)
                    return (e2 = e2.Keep(entry)) == l5.Entry2 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));
                if (hash == e3.Hash)
                    return (e3 = e3.Keep(entry)) == l5.Entry3 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));
                if (hash == e4.Hash)
                    return (e4 = e4.Keep(entry)) == l5.Entry4 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4));

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
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
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

                // todo @perf we may split into the branch here
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

                // Insert the added entry and the Plus entry into the correct position starting from the last to the first entry (e4 -> e0),
                // because we assume the addition of inscreasing hash (keys) is the more often case.
                // The order at the end should be the follwing: 
                // e0 < e1 < e2 < e3 < e4 < lp < p < entry

                Entry swap = null;
                if (lph < e4.Hash)
                {
                    swap = e4; e4 = lp; lp = swap;
                    if (lph < e3.Hash)
                    {
                        swap = e3; e3 = e4; e4 = swap;
                        if (lph < e2.Hash)
                        {
                            swap = e2; e2 = e3; e3 = swap;
                            if (lph < e1.Hash)
                            {
                                swap = e1; e1 = e2; e2 = swap;
                                if (lph < e0.Hash)
                                {
                                    swap = e0; e0 = e1; e1 = swap;
                                }
                            }
                        }
                    }
                }
                if (ph < lp.Hash)
                {
                    swap = lp; lp = p; p = swap;
                    if (ph < e4.Hash)
                    {
                        swap = e4; e4 = lp; lp = swap;
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
                    if (hash < lp.Hash)
                    {
                        swap = lp; lp = p; p = swap;
                        if (hash < e4.Hash)
                        {
                            swap = e4; e4 = lp; lp = swap;
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

                // todo: @perf find the way to reuse the Leaf5
                return new Branch2(new Leaf5(e0, e1, e2, e3, e4), lp, new Leaf2(p, e));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                    return (p = p.Keep(entry)) == Plus ? this : (ImHashMap234<K, V>)new Leaf5Plus1(p, L.L5);

                var lp = L.Plus;
                var lph = lp.Hash;
                if (lph == hash)
                    return (lp = lp.Keep(entry)) == L.Plus ? this : (ImHashMap234<K, V>)new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, L.L5));

                var l5 = L.L5;
                var e0 = l5.Entry0;
                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash == e0.Hash)
                    return (e0 = e0.Keep(entry)) == l5.Entry0 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));
                if (hash == e1.Hash)
                    return (e1 = e1.Keep(entry)) == l5.Entry1 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));
                if (hash == e2.Hash)
                    return (e2 = e2.Keep(entry)) == l5.Entry2 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));
                if (hash == e3.Hash)
                    return (e3 = e3.Keep(entry)) == l5.Entry3 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));
                if (hash == e4.Hash)
                    return (e4 = e4.Keep(entry)) == l5.Entry4 ? this : new Leaf5Plus1Plus1(p, new Leaf5Plus1(lp, new Leaf5(e0, e1, e2, e3, e4)));

                Entry swap = null;
                if (lph < e4.Hash)
                {
                    swap = e4; e4 = lp; lp = swap;
                    if (lph < e3.Hash)
                    {
                        swap = e3; e3 = e4; e4 = swap;
                        if (lph < e2.Hash)
                        {
                            swap = e2; e2 = e3; e3 = swap;
                            if (lph < e1.Hash)
                            {
                                swap = e1; e1 = e2; e2 = swap;
                                if (lph < e0.Hash)
                                {
                                    swap = e0; e0 = e1; e1 = swap;
                                }
                            }
                        }
                    }
                }
                if (ph < lp.Hash)
                {
                    swap = lp; lp = p; p = swap;
                    if (ph < e4.Hash)
                    {
                        swap = e4; e4 = lp; lp = swap;
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
                    if (hash < lp.Hash)
                    {
                        swap = lp; lp = p; p = swap;
                        if (hash < e4.Hash)
                        {
                            swap = e4; e4 = lp; lp = swap;
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

                // todo: @perf find the way to reuse the Leaf5
                return new Branch2(new Leaf5(e0, e1, e2, e3, e4), lp, new Leaf2(p, e));
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

        /// <summary>Base type for the Branch2 and Branch3</summary>
        public abstract class Branch : ImHashMap234<K, V> {} 

        // todo: @perf consider to separate the Branch2Leafs
        /// <summary>Branch of 2 leafs or branches</summary>
        public sealed class Branch2 : Branch
        {
            /// <summary>Left branch</summary>
            public readonly ImHashMap234<K, V> Left;
            /// <summary>Entry in the middle</summary>
            public readonly Entry Entry0;
            /// <summary>Right branch</summary>
            public readonly ImHashMap234<K, V> Right;

            /// <summary>Constructs</summary>
            public Branch2(ImHashMap234<K, V> left, Entry e, ImHashMap234<K, V> right)
            {
                Debug.Assert(Left != Empty);
                Debug.Assert(Left is Entry == false);
                Debug.Assert(Right != Empty);
                Debug.Assert(Right is Entry == false);
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
                Entry0;

            // todo: @perf see that the size of the method is small, so we may consider to inline the addition for the branchs of the leafs, it will be especially more simple, if the Branch2Leafs would be a separate type.
            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0; // todo: @perf load the hash into the variable
                if (hash > e0.Hash)
                {
                    var old = Right;
                    var aNew = old.AddOrUpdateEntry(hash, entry);
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1Plus1))
                        return new Branch3(Left, e0, b2);
                    return new Branch2(Left, e0, aNew);
                }

                if (hash < e0.Hash)
                {
                    var old = Left;
                    var aNew = old.AddOrUpdateEntry(hash, entry);
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1Plus1))
                        return new Branch3(b2.Left, b2.Entry0, new Branch2(b2.Right, e0, Right));
                    return new Branch2(aNew, e0, Right);
                }

                return new Branch2(Left, e0.Update(entry), Right);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                if (hash > e0.Hash)
                {
                    var old = Right;
                    var aNew = old.AddOrKeepEntry(hash, entry);
                    if (aNew == old)
                        return this;
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1Plus1))
                        return new Branch3(Left, e0, b2);
                    return new Branch2(Left, e0, aNew);
                }

                if (hash < e0.Hash)
                {
                    var old = Left;
                    var aNew = old.AddOrKeepEntry(hash, entry);
                    if (aNew == old)
                        return this;
                    if (aNew is Branch2 b2 && (old is Branch3 || old is Leaf5Plus1Plus1))
                        return new Branch3(b2.Left, b2.Entry0, new Branch2(b2.Right, e0, Right));
                    return new Branch2(aNew, e0, Right);
                }

                return (e0 = e0.Keep(entry)) == Entry0 ? this : new Branch2(Left, e0, Right);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                // Despite all the visible complexity of the method the simple check should be 
                // that all of the non-removed nodes are used when constructing the result.

                var e0 = Entry0;
                if (hash > e0.Hash) 
                {
                    //        4
                    //      /   \
                    //  1 2 3   5 [6]

                    var newRight = Right.RemoveEntry(hash, key);
                    if (newRight == Right)
                        return this;

                    if (newRight is Entry re) 
                    {
                        var l = Left;
                        // If the Left is not a Leaf2, move its one entry to the Right
                        if (l is Leaf3 l3)
                            return new Branch2(new Leaf2(l3.Entry0, l3.Entry1), l3.Entry2, new Leaf2(e0, re)); 
                        if (l is Leaf3Plus1 l4)
                        {
                            var p = l4.Plus;
                            var ph = p.Hash;
                            var l4l3  = l4.L3;
                            var l3e0 = l4l3.Entry0;
                            var l3e1 = l4l3.Entry1;
                            var l3e2 = l4l3.Entry2;

                            if (ph > l3e2.Hash)
                                return new Branch2(l4l3, p, new Leaf2(e0, re));
                            if (ph < l3e0.Hash)
                                return new Branch2(new Leaf3(p, l3e0, l3e1), l3e2, new Leaf2(e0, re)); 
                            if (ph < l3e1.Hash)
                                return new Branch2(new Leaf3(l3e0, p, l3e1), l3e2, new Leaf2(e0, re)); 
                            return new Branch2(new Leaf3(l3e0, l3e1, p), l3e2, new Leaf2(e0, re));
                        }
                        if (l is Leaf5 l5)
                            return new Branch2(new Leaf3(l5.Entry0, l5.Entry1, l5.Entry2), l5.Entry3, new Leaf3(l5.Entry4, e0, re));
                        if (l is Leaf5Plus1 l6) // todo: @incomplete update to plus-plus
                        {
                            var p  = l6.Plus;
                            var ph = p.Hash;
                            var l6l5 = l6.L5;
                            var l5e0 = l6l5.Entry0;
                            var l5e1 = l6l5.Entry1;
                            var l5e2 = l6l5.Entry2;
                            var l5e3 = l6l5.Entry3;
                            var l5e4 = l6l5.Entry4;

                            if (ph > l5e4.Hash)
                                return new Branch2(l6l5, p, new Leaf2(e0, re));
                            if (ph < l5e0.Hash)
                                return new Branch2(new Leaf5(p, l5e0, l5e1, l5e2, l5e3), l5e4, new Leaf2(e0, re));
                            if (ph < l5e1.Hash)
                                return new Branch2(new Leaf5(l5e0, p, l5e1, l5e2, l5e3), l5e4, new Leaf2(e0, re));
                            if (ph < l5e2.Hash)
                                return new Branch2(new Leaf5(l5e0, l5e1, p, l5e2, l5e3), l5e4, new Leaf2(e0, re));
                            if (ph < l5e3.Hash)
                                return new Branch2(new Leaf5(l5e0, l5e1, l5e2, p, l5e3), l5e4, new Leaf2(e0, re));
                            return new Branch2(new Leaf5(l5e0, l5e1, l5e2, l5e3, p), l5e4, new Leaf2(e0, re));
                        }

                        // Case #1
                        // If the Left is Leaf2 -> reduce the whole branch to the Leaf4 and rely on the upper branch (if any) to balance itself,
                        // see this case handled below..
                        var l2 = (Leaf2)l;
                        return new Leaf3Plus1(l2.Entry0, new Leaf3(l2.Entry1, e0, re));
                    }

                    // Handling Case #1
                    if (newRight is Leaf3Plus1 && Right is Branch2) // no need to check for the Branch3 because there is no way that Leaf4 will be the result of deleting one element from it 
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
                    if (newRight is Branch3 rb3 && Right is Branch2)
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
                            return new Branch2(
                                new Branch2(lb3.Left, lb3.Entry0, lb3.Middle), 
                                lb3.Entry1,
                                new Branch2(lb3.Right, e0, newRight));
                    }

                    return new Branch2(Left, e0, newRight);
                }

                if (hash < e0.Hash)
                {
                    // todo: @wip
                }

                // todo: @wip remove the e0 and try to keep the branch until its possible
                return this;
            }
        }

        /// <summary>Branch of 3 leafs or branches and two entries</summary>
        public sealed class Branch3 : Branch
        {
            /// <summary>Left branch</summary>
            public readonly ImHashMap234<K, V> Left;
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>The middle and right is represented by the Branch2 to simplify the Enumeration implementation,
            /// so we always deal with the binary tree. But for the outside the use of Branch2 is just an internal detail.</summary>
            public readonly Branch2 RightBranch;
            /// <summary>Middle branch</summary>
            public ImHashMap234<K, V> Middle => RightBranch.Left;
            /// <summary>Right entry</summary>
            public Entry Entry1 => RightBranch.Entry0;
            /// <summary>Rightmost branch</summary>
            public ImHashMap234<K, V> Right => RightBranch.Right;

            /// <summary>Constructs the branch</summary>
            public Branch3(ImHashMap234<K, V> left, Entry entry0, Branch2 rightBranch)
            {
                Debug.Assert(Left != Empty);
                Debug.Assert(Left is Entry == false);
                Debug.Assert(entry0.Hash < RightBranch.Entry0.Hash, "entry0.Hash < RightBranch.Entry0.Hash");
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
                    hash == h0 ? Entry0 :
                    hash == h1 ? Entry1 :
                    Middle.GetEntryOrDefault(hash);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
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
                    ? new Branch3(Left, Entry0.Update(entry), RightBranch)
                    : new Branch3(Left, Entry0, new Branch2(Middle, Entry1.Update(entry), Right));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
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

                return hash == e0.Hash
                    ? ((e0 = e0.Keep(entry)) == Entry0 ? this : new Branch3(Left, e0, RightBranch))
                    : ((e1 = e1.Keep(entry)) == Entry1 ? this : new Branch3(Left, e0, new Branch2(Middle, e1, Right)));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                var e1 = Entry1;
                if (hash > e1.Hash)
                {
                    var newRight = Right.RemoveEntry(hash, key);
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
                            var l6 =  (Leaf5Plus1)m; 
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
                    // todo: @wip
                }

                //if (hash == e1.Hash)
                    // todo: @wip


                return this;
            }
        }
    }

    /// <summary>ImHashMap methods</summary>
    public static class ImHashMap234
    {
        /// <summary>Enumerates all the map entries from the left to the right and from the bottom to top</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static IEnumerable<ImHashMap234<K, V>.ValueEntry> Enumerate<K, V>(this ImHashMap234<K, V> map, 
            List<ImHashMap234<K, V>> parentStack = null) // todo: @perf replace the List with the more lightweight alternative, the bad thing that we cannot pass the `ref` array into the method returning IEnumerable
        {
            if (map == ImHashMap234<K, V>.Empty)
                yield break;
            if (map is ImHashMap234<K, V>.Entry e)
            {
                if (e is ImHashMap234<K, V>.ValueEntry v) yield return v;
                else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e).Conflicts) yield return c;
                yield break;
            }

            var parentIndex = -1;
            while (true)
            {
                if (map is ImHashMap234<K, V>.Branch)
                {
                    if (parentStack == null)
                        parentStack = new List<ImHashMap234<K, V>>(2);
                    if (++parentIndex >= parentStack.Count)
                        parentStack.Add(map);
                    else
                        parentStack[parentIndex] = map;
                    map = map is ImHashMap234<K, V>.Branch2 b2 ? b2.Left : ((ImHashMap234<K, V>.Branch3)map).Left;
                    continue;
                }
                
                if (map is ImHashMap234<K, V>.Leaf2 l2)
                {
                    if (l2.Entry0 is ImHashMap234<K, V>.ValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l2.Entry0).Conflicts) yield return c;
                    if (l2.Entry1 is ImHashMap234<K, V>.ValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l2.Entry1).Conflicts) yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf3 l3)
                {
                    if (l3.Entry0 is ImHashMap234<K, V>.ValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l3.Entry0).Conflicts) yield return c;
                    if (l3.Entry1 is ImHashMap234<K, V>.ValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l3.Entry1).Conflicts) yield return c;
                    if (l3.Entry2 is ImHashMap234<K, V>.ValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l3.Entry2).Conflicts) yield return c;
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

                    if (e0 is ImHashMap234<K, V>.ValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e0).Conflicts) yield return c;
                    if (e1 is ImHashMap234<K, V>.ValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e1).Conflicts) yield return c;
                    if (e2 is ImHashMap234<K, V>.ValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e2).Conflicts) yield return c;
                    if (p  is ImHashMap234<K, V>.ValueEntry v3) yield return v3;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)p).Conflicts)  yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf5 l5)
                {
                    if (l5.Entry0 is ImHashMap234<K, V>.ValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l5.Entry0).Conflicts) yield return c;
                    if (l5.Entry1 is ImHashMap234<K, V>.ValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l5.Entry1).Conflicts) yield return c;
                    if (l5.Entry2 is ImHashMap234<K, V>.ValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l5.Entry2).Conflicts) yield return c;
                    if (l5.Entry3 is ImHashMap234<K, V>.ValueEntry v3) yield return v3;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l5.Entry3).Conflicts) yield return c;
                    if (l5.Entry4 is ImHashMap234<K, V>.ValueEntry v4) yield return v4;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)l5.Entry4).Conflicts) yield return c;
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

                    if (e0 is ImHashMap234<K, V>.ValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e0).Conflicts) yield return c;
                    if (e1 is ImHashMap234<K, V>.ValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e1).Conflicts) yield return c;
                    if (e2 is ImHashMap234<K, V>.ValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e2).Conflicts) yield return c;
                    if (e3 is ImHashMap234<K, V>.ValueEntry v3) yield return v3;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e3).Conflicts) yield return c;
                    if (e4 is ImHashMap234<K, V>.ValueEntry v4) yield return v4;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e4).Conflicts) yield return c;
                    if (p  is ImHashMap234<K, V>.ValueEntry v5) yield return v5;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)p).Conflicts)  yield return c;
                }
                else if (map is ImHashMap234<K, V>.Leaf5Plus1Plus1 l511)
                {
                    var p   = l511.Plus;
                    var ph  = p.Hash;
                    var lp  = l511.L.Plus;
                    var lph = p.Hash;
                    var l = l511.L.L5;
                    var e0  = l.Entry0;
                    var e1  = l.Entry1;
                    var e2  = l.Entry2;
                    var e3  = l.Entry3;
                    var e4  = l.Entry4;

                    ImHashMap234<K, V>.Entry swap = null;
                    if (lph < e4.Hash)
                    {
                        swap = e4; e4 = lp; lp = swap;
                        if (lph < e3.Hash)
                        {
                            swap = e3; e3 = e4; e4 = swap;
                            if (lph < e2.Hash)
                            {
                                swap = e2; e2 = e3; e3 = swap;
                                if (lph < e1.Hash)
                                {
                                    swap = e1; e1 = e2; e2 = swap;
                                    if (lph < e0.Hash)
                                    {
                                        swap = e0; e0 = e1; e1 = swap;
                                    }
                                }
                            }
                        }
                    }
                    if (ph < lp.Hash)
                    {
                        swap = lp; lp = p; p = swap;
                        if (ph < e4.Hash)
                        {
                            swap = e4; e4 = lp; lp = swap;
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
                    if (e0 is ImHashMap234<K, V>.ValueEntry v0) yield return v0;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e0).Conflicts) yield return c;
                    if (e1 is ImHashMap234<K, V>.ValueEntry v1) yield return v1;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e1).Conflicts) yield return c;
                    if (e2 is ImHashMap234<K, V>.ValueEntry v2) yield return v2;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e2).Conflicts) yield return c;
                    if (e3 is ImHashMap234<K, V>.ValueEntry v3) yield return v3;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e3).Conflicts) yield return c;
                    if (e4 is ImHashMap234<K, V>.ValueEntry v4) yield return v4;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)e4).Conflicts) yield return c;
                    if (lp is ImHashMap234<K, V>.ValueEntry v5) yield return v5;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)lp).Conflicts) yield return c;
                    if (p  is ImHashMap234<K, V>.ValueEntry v6) yield return v6;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)p).Conflicts)  yield return c;
                }

                if (parentIndex == -1)
                    break; // we yield the leaf and there is nothing in stack - we are DONE!

                map = parentStack[parentIndex]; // otherwise get the parent
                if (map is ImHashMap234<K, V>.Branch2 pb2) 
                {
                    if (pb2.Entry0 is ImHashMap234<K, V>.ValueEntry v) yield return v;
                    else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)pb2.Entry0).Conflicts) yield return c;
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
                    var pb3 = (ImHashMap234<K, V>.Branch3)map;
                    {
                        if (pb3.Entry0 is ImHashMap234<K, V>.ValueEntry v) yield return v;
                        else foreach (var c in ((ImHashMap234<K, V>.ConflictsEntry)pb3.Entry0).Conflicts) yield return c;
                        map = pb3.RightBranch;
                        --parentIndex; // we done with the this level handled the Left and the Middle (previously) and the Right (now)
                    }
                }
            }
        }

        /// <summary>Looks up for the key using its hash code and checking the key with `object.Equals` for equality,
        ///  returns found value or the default value if not found</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap234<K, V> map, int hash, K key)
        {
            var e = map.GetEntryOrDefault(hash);
            if (e is ImHashMap234<K, V>.ValueEntry v)
            {
                if (v.Key.Equals(key))
                    return v.Value;
            }
            else if (e is ImHashMap234<K, V>.ConflictsEntry c)
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
            if (e is ImHashMap234<K, V>.ValueEntry v)
            {
                if (v.Key == key)
                    return v.Value;
            }
            else if (e is ImHashMap234<K, V>.ConflictsEntry c)
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
            if (e is ImHashMap234<K, V>.ValueEntry v)
            {
                if (v.Key.Equals(key))
                {
                    value = v.Value;
                    return true;
                }
            }
            else if (e is ImHashMap234<K, V>.ConflictsEntry c)
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

            if (e is ImHashMap234<K, V>.ValueEntry v)
            {
                if (v.Key == key)
                {
                    value = v.Value;
                    return true;
                }
            }
            else if (e is ImHashMap234<K, V>.ConflictsEntry c)
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
            map.AddOrUpdateEntry(hash, new ImHashMap234<K, V>.ValueEntry(hash, key, value));

        /// <summary>Adds or updates the value by key in the map, always returning the modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrUpdate<K, V>(this ImHashMap234<K, V> map, K key, V value)
        {
            var hash = key.GetHashCode();
            return map.AddOrUpdateEntry(hash, new ImHashMap234<K, V>.ValueEntry(hash, key, value));
        }

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrKeep<K, V>(this ImHashMap234<K, V> map, int hash, K key, V value) =>
            map.AddOrKeepEntry(hash, new ImHashMap234<K, V>.ValueEntry(hash, key, value));

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImHashMap234<K, V> AddOrKeep<K, V>(this ImHashMap234<K, V> map, K key, V value)
        {
            var hash = key.GetHashCode();
            return map.AddOrKeepEntry(hash, new ImHashMap234<K, V>.ValueEntry(hash, key, value));
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
}