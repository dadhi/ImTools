
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
        public override string ToString() => "empty " + typeof(ImHashMap234<K, V>).Name;

        /// <summary>Lookup for the entry, if not found returns `null`</summary>
        public virtual Entry GetEntryOrDefault(int hash) => null;

        /// <summary>Produces the new or updated map with the new entry</summary>
        public virtual ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry) => entry;

        /// <summary>Produces the new map with the new entry or keeps the existing map if the entry with the key is already present</summary>
        public virtual ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry) => entry;

        /// <summary>Returns the map without the entry with the specified hash and key if it is found in the map</summary>
        public virtual ImHashMap234<K, V> RemoveEntry(int hash, K key) => this;

        // todo: @perf - optimize
        /// <summary>Enumerates the entries</summary>
        public virtual IEnumerable<ValueEntry> Enumerate() => Enumerable.Empty<ValueEntry>();

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
            public ValueEntry(int hash, K key, V value) : base(hash) 
            { 
                Key   = key;
                Value = value;
            }

            /// <inheritdoc />
            public override string ToString() => "[" + Hash + "]" + Key + ":" + Value;

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

            /// <inheritdoc />
            public override IEnumerable<ValueEntry> Enumerate() 
            {
                yield return this;
            }
        }

        /// <summary>Entry containing the Array of conflicting Value entries.
        /// Note: The Key field is unused and always has a default value</summary>
        public sealed class ConflictsEntry : Entry
        {
            /// <summary>The 2 and more conflicts.</summary>
            public ValueEntry[] Conflicts;

            /// <summary>Constructs the entry with the key and value</summary>
            public ConflictsEntry(int hash, params ValueEntry[] conflicts) : base(hash) => Conflicts = conflicts;

            /// <inheritdoc />
            public override string ToString()
            {
                var sb = new StringBuilder();
                foreach (var x in Conflicts) 
                    sb.Append(x.ToString()).Append("; ");
                return sb.ToString();
            }

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

            /// <inheritdoc />
            public override IEnumerable<ValueEntry> Enumerate() => Conflicts;
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

            /// <inheritdoc />
            public override string ToString() => "leaf2{" + Entry0 + "; " + Entry1 + "}";

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

            /// <inheritdoc />
            public override IEnumerable<ValueEntry> Enumerate()
            {
                if (Entry0 is ValueEntry v0)
                    yield return v0;
                else foreach (var x in ((ConflictsEntry)Entry0).Conflicts)
                    yield return x;
                if (Entry1 is ValueEntry v1)
                    yield return v1;
                else foreach (var x in ((ConflictsEntry)Entry1).Conflicts)
                    yield return x;
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

            /// <inheritdoc />
            public override string ToString() => "leaf3{" + Entry0 + "; " + Entry1 + "; " + Entry2 + "}";

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash == Entry0.Hash ? Entry0 :
                hash == Entry1.Hash ? Entry1 :
                hash == Entry2.Hash ? Entry2 :
                null;

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                return
                    hash == e0.Hash ? new Leaf3(e0.Update(entry), e1, e2) :
                    hash == e1.Hash ? new Leaf3(e0, e1.Update(entry), e2) :
                    hash == e2.Hash ? new Leaf3(e0, e1, e2.Update(entry)) :
                    (ImHashMap234<K, V>)new Leaf3Plus1(entry, this);
            }

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

            /// <inheritdoc />
            public override IEnumerable<ValueEntry> Enumerate()
            {
                if (Entry0 is ValueEntry v0)
                    yield return v0;
                else foreach (var x in ((ConflictsEntry)Entry0).Conflicts)
                    yield return x;
                if (Entry1 is ValueEntry v1)
                    yield return v1;
                else foreach (var x in ((ConflictsEntry)Entry1).Conflicts)
                    yield return x;
                if (Entry2 is ValueEntry v2)
                    yield return v2;
                else foreach (var x in ((ConflictsEntry)Entry2).Conflicts)
                    yield return x;
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

            /// <inheritdoc />
            public override string ToString() => "leaf3+1{" + Plus + " + " + L3 + "}";

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

            /// <inheritdoc />
            public override IEnumerable<ValueEntry> Enumerate()
            {
                var p = Plus;
                var ph = p.Hash;
                var l = L3;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;

                if (ph < e0.Hash)
                {
                    if (p is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
                else 
                {
                    if (e0 is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e0).Conflicts)
                        yield return x;
                }

                if (ph < e1.Hash)
                {
                    if (p is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
                else 
                {
                    if (e1 is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e1).Conflicts)
                        yield return x;
                }

                if (ph < e2.Hash)
                {
                    if (p is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e2).Conflicts)
                        yield return x;
                }
                else 
                {
                    if (e2 is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e2).Conflicts)
                        yield return x;

                    if (p is ValueEntry pv)
                        yield return pv;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
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

            /// <inheritdoc />
            public override string ToString() => "leaf5{" + Entry0 + "; " + Entry1 + "; " + Entry2 + "; " + Entry3 + "; " + Entry4 + "}";

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash == Entry0.Hash ? Entry0 :
                hash == Entry1.Hash ? Entry1 :
                hash == Entry2.Hash ? Entry2 :
                hash == Entry3.Hash ? Entry3 :
                hash == Entry4.Hash ? Entry4 :
                null;

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                var e4 = Entry4;
                return
                    hash == e0.Hash ? new Leaf5(e0.Update(entry), e1, e2, e3, e4) :
                    hash == e1.Hash ? new Leaf5(e0, e1.Update(entry), e2, e3, e4) :
                    hash == e2.Hash ? new Leaf5(e0, e1, e2.Update(entry), e3, e4) :
                    hash == e3.Hash ? new Leaf5(e0, e1, e2, e3.Update(entry), e4) :
                    hash == e4.Hash ? new Leaf5(e0, e1, e2, e3, e4.Update(entry)) :
                    (ImHashMap234<K, V>)new Leaf5Plus1(entry, this);
            }

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

            /// <inheritdoc />
            public override IEnumerable<ValueEntry> Enumerate()
            {
                if (Entry0 is ValueEntry v0)
                    yield return v0;
                else foreach (var x in ((ConflictsEntry)Entry0).Conflicts)
                    yield return x;
                if (Entry1 is ValueEntry v1)
                    yield return v1;
                else foreach (var x in ((ConflictsEntry)Entry1).Conflicts)
                    yield return x;
                if (Entry2 is ValueEntry v2)
                    yield return v2;
                else foreach (var x in ((ConflictsEntry)Entry2).Conflicts)
                    yield return x;
                if (Entry3 is ValueEntry v3)
                    yield return v3;
                else foreach (var x in ((ConflictsEntry)Entry3).Conflicts)
                    yield return x;
                if (Entry4 is ValueEntry v4)
                    yield return v4;
                else foreach (var x in ((ConflictsEntry)Entry4).Conflicts)
                    yield return x;
            }
        }

        /// <summary>Splittable cases: Leaf5Plus1 or Branch3
        /// Note: The result of the split is always the Branch2 consisting of returned map, popEntry, and popRight</summary>
        public abstract class Leaf5Plus1OrBranch3 : ImHashMap234<K, V> 
        {
            internal abstract ImHashMap234<K, V> AddOrUpdateOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight);

            internal abstract ImHashMap234<K, V> AddOrKeepOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight);
        }

        /// <summary>Leaf with 5 entries</summary>
        public sealed class Leaf5Plus1 : Leaf5Plus1OrBranch3
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

            /// <inheritdoc />
            public override string ToString() => "leaf5+1{" + Plus + " + " + L5 + "}";

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
 
                if (hash > e4.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, e1), e2, new Leaf3(e3, e4, entry));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, p, e1), e2, new Leaf3(e3, e4, entry));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, e1, p), e2, new Leaf3(e3, e4, entry));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), p, new Leaf3(e3, e4, entry));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(p, e4, entry));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(e4, p, entry));
                    return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(e4, entry, p));
                }

                if (hash < e0.Hash)
                {
                    if (ph < hash)
                        return new Branch2(new Leaf3(p, entry, e0), e1, new Leaf3(e2, e3, e4));
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(entry, p, e0), e1, new Leaf3(e2, e3, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(entry, e0, p), e1, new Leaf3(e2, e3, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(entry, e0, e1), p, new Leaf3(e2, e3, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(entry, e0, e1), e2, new Leaf3(p, e3, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(entry, e0, e1), e2, new Leaf3(e3, p, e4));
                    return new Branch2(new Leaf3(entry, e0, e1), e2, new Leaf3(e3, e4, p));
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, entry), e1, new Leaf3(e2, e3, e4));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, p, entry), e1, new Leaf3(e2, e3, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, entry, p), e1, new Leaf3(e2, e3, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, entry, e1), p, new Leaf3(e2, e3, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, entry, e1), e2, new Leaf3(p, e3, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, entry, e1), e2, new Leaf3(e3, p, e4));
                    return new Branch2(new Leaf3(e0, entry, e1), e2, new Leaf3(e3, e4, p));
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, e1), entry, new Leaf3(e2, e3, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, p, e1), entry, new Leaf3(e2, e3, e4));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, e1, p), entry, new Leaf3(e2, e3, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, e1, entry), p, new Leaf3(e2, e3, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, e1, entry), e2, new Leaf3(p, e3, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, e1, entry), e2, new Leaf3(e3, p, e4));
                    return new Branch2(new Leaf3(e0, e1, entry), e2, new Leaf3(e3, e4, p));
                }

                if (hash > e2.Hash && hash < e3.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, e1), e2, new Leaf3(entry, e3, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, p, e1), e2, new Leaf3(entry, e3, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, e1, p), e2, new Leaf3(entry, e3, e4));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, e1, e2), p, new Leaf3(entry, e3, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf3(p, e3, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf3(e3, p, e4));
                    return new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf3(e3, e4, p));
                }

                if (hash > e3.Hash && hash < e4.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, e1), e2, new Leaf3(e3, entry, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, p, e1), e2, new Leaf3(e3, entry, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, e1, p), e2, new Leaf3(e3, entry, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), p, new Leaf3(e3, entry, e4));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(p, entry, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(entry, p, e4));
                    return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(entry, e4, p));
                }

                return
                    hash == e0.Hash ? new Leaf5Plus1(p, new Leaf5(e0.Update(entry), e1, e2, e3, e4)) :
                    hash == e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1.Update(entry), e2, e3, e4)) :
                    hash == e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2.Update(entry), e3, e4)) :
                    hash == e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3.Update(entry), e4)) :
                                      new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4.Update(entry)));
            }

            /// <inheritdoc />
            internal override ImHashMap234<K, V> AddOrUpdateOrSplitEntry(int hash, ValueEntry entry, out Entry popEntry, out ImHashMap234<K, V> popRight)
            {
                // todo: @perf look at what the results popEntry is set to and may be use the popEntry instead of the one of the vars above, then don't forget to use popRight on the consumer side, and remove the `popEntry = null` below
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                {
                    popEntry = null;
                    popRight = null;
                    return new Leaf5Plus1(p.Update(entry), L5);
                }

                var l = L5;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;
                var e3 = l.Entry3;
                var e4 = l.Entry4;

                if (hash > e4.Hash)
                {
                    if (ph < e2.Hash) 
                    {
                        popEntry = e2;
                        popRight = new Leaf3(e3, e4, entry);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, e1);
                        if (ph < e1.Hash)
                            return new Leaf3(e0, p, e1);
                        return new Leaf3(e0, e1, p);
                    }
                    if (ph < e3.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e3, e4, entry);
                        return new Leaf3(e0, e1, e2);
                    }
                    popEntry = e3;
                    popRight = ph < e4.Hash ? new Leaf3(p, e4, entry) : ph < hash ? new Leaf3(e4, p, entry) : new Leaf3(e4, entry, p);
                    return new Leaf3(e0, e1, e2);
                }

                if (hash < e0.Hash)
                {
                    if (ph < e1.Hash) 
                    {
                        popEntry = e1;
                        popRight = new Leaf3(e2, e3, e4);
                        if (ph < hash)
                            return new Leaf3(p, entry, e0);
                        if (ph < e0.Hash)
                            return new Leaf3(entry, p, e0);
                        return new Leaf3(entry, e0, p);
                    }
                    if (ph < e2.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e2, e3, e4);
                        return new Leaf3(entry, e0, e1);
                    }
                    popEntry = e2;
                    popRight = ph < e3.Hash ? new Leaf3(p, e3, e4) : ph < e4.Hash ? new Leaf3(e3, p, e4) : new Leaf3(e3, e4, p);
                    return new Leaf3(entry, e0, e1);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (ph < e1.Hash)
                    {
                        popEntry = e1;
                        popRight = new Leaf3(e2, e3, e4);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, entry);
                        if (ph < hash)
                            return new Leaf3(e0, p, entry);
                        return new Leaf3(e0, entry, p);
                    }
                    if (ph < e2.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e2, e3, e4);
                        return new Leaf3(e0, entry, e1);
                    }
                    popEntry = e2;
                    popRight = ph < e3.Hash ? new Leaf3(p, e3, e4) : ph < e4.Hash ? new Leaf3(e3, p, e4) : new Leaf3(e3, e4, p);
                    return new Leaf3(e0, entry, e1);
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    if (ph < hash)
                    {
                        popEntry = entry;
                        popRight = new Leaf3(e2, e3, e4);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, e1);
                        if (ph < e1.Hash)
                            return new Leaf3(e0, p, e1);
                        return new Leaf3(e0, e1, p);
                    }
                    if (ph < e2.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e2, e3, e4);
                        return new Leaf3(e0, e1, entry);
                    }
                    popEntry = e2;
                    popRight = ph < e3.Hash ? new Leaf3(p, e3, e4) : ph < e4.Hash ? new Leaf3(e3, p, e4) : new Leaf3(e3, e4, p);
                    return new Leaf3(e0, e1, entry);
                }

                if (hash > e2.Hash && hash < e3.Hash)
                {
                    if (ph < e2.Hash) 
                    {
                        popEntry = e2;
                        popRight = new Leaf3(entry, e3, e4);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, e1);
                        if (ph < e1.Hash)
                            return new Leaf3(e0, p, e1);
                        return new Leaf3(e0, e1, p);
                    }
                    if (ph < hash) 
                    {
                        popEntry = p;
                        popRight = new Leaf3(entry, e3, e4);
                        return new Leaf3(e0, e1, e2);
                    }
                    popEntry = entry;
                    popRight = ph < e3.Hash ? new Leaf3(p, e3, e4) : ph < e4.Hash ? new Leaf3(e3, p, e4) : new Leaf3(e3, e4, p);
                    return new Leaf3(e0, e1, e2);
                }

                if (hash > e3.Hash && hash < e4.Hash)
                {
                    if (ph < e2.Hash) 
                    {
                        popEntry = e2;
                        popRight = new Leaf3(e3, entry, e4);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, e1);
                        if (ph < e1.Hash)
                            return new Leaf3(e0, p, e1);
                        return new Leaf3(e0, e1, p);
                    }
                    if (ph < e3.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e3, entry, e4);
                        return new Leaf3(e0, e1, e2);
                    }
                    popEntry = e3;
                    popRight = ph < hash ? new Leaf3(p, entry, e4) : ph < e4.Hash ? new Leaf3(entry, p, e4) : new Leaf3(entry, e4, p);
                    return new Leaf3(e0, e1, e2);
                }

                popEntry = null;
                popRight = null;
                return
                    hash == e0.Hash ? new Leaf5Plus1(p, new Leaf5(e0.Update(entry), e1, e2, e3, e4)) :
                    hash == e1.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1.Update(entry), e2, e3, e4)) :
                    hash == e2.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2.Update(entry), e3, e4)) :
                    hash == e3.Hash ? new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3.Update(entry), e4)) :
                                      new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4.Update(entry)));
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

                if (hash > e4.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, e1), e2, new Leaf3(e3, e4, entry));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, p, e1), e2, new Leaf3(e3, e4, entry));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, e1, p), e2, new Leaf3(e3, e4, entry));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), p, new Leaf3(e3, e4, entry));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(p, e4, entry));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(e4, p, entry));
                    return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(e4, entry, p));
                }

                if (hash < e0.Hash)
                {
                    if (ph < hash)
                        return new Branch2(new Leaf3(p, entry, e0), e1, new Leaf3(e2, e3, e4));
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(entry, p, e0), e1, new Leaf3(e2, e3, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(entry, e0, p), e1, new Leaf3(e2, e3, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(entry, e0, e1), p, new Leaf3(e2, e3, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(entry, e0, e1), e2, new Leaf3(p, e3, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(entry, e0, e1), e2, new Leaf3(e3, p, e4));
                    return new Branch2(new Leaf3(entry, e0, e1), e2, new Leaf3(e3, e4, p));
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, entry), e1, new Leaf3(e2, e3, e4));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, p, entry), e1, new Leaf3(e2, e3, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, entry, p), e1, new Leaf3(e2, e3, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, entry, e1), p, new Leaf3(e2, e3, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, entry, e1), e2, new Leaf3(p, e3, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, entry, e1), e2, new Leaf3(e3, p, e4));
                    return new Branch2(new Leaf3(e0, entry, e1), e2, new Leaf3(e3, e4, p));
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, e1), entry, new Leaf3(e2, e3, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, p, e1), entry, new Leaf3(e2, e3, e4));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, e1, p), entry, new Leaf3(e2, e3, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, e1, entry), p, new Leaf3(e2, e3, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, e1, entry), e2, new Leaf3(p, e3, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, e1, entry), e2, new Leaf3(e3, p, e4));
                    return new Branch2(new Leaf3(e0, e1, entry), e2, new Leaf3(e3, e4, p));
                }

                if (hash > e2.Hash && hash < e3.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, e1), e2, new Leaf3(entry, e3, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, p, e1), e2, new Leaf3(entry, e3, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, e1, p), e2, new Leaf3(entry, e3, e4));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, e1, e2), p, new Leaf3(entry, e3, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf3(p, e3, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf3(e3, p, e4));
                    return new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf3(e3, e4, p));
                }

                if (hash > e3.Hash && hash < e4.Hash)
                {
                    if (ph < e0.Hash)
                        return new Branch2(new Leaf3(p, e0, e1), e2, new Leaf3(e3, entry, e4));
                    if (ph < e1.Hash)
                        return new Branch2(new Leaf3(e0, p, e1), e2, new Leaf3(e3, entry, e4));
                    if (ph < e2.Hash)
                        return new Branch2(new Leaf3(e0, e1, p), e2, new Leaf3(e3, entry, e4));
                    if (ph < e3.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), p, new Leaf3(e3, entry, e4));
                    if (ph < hash)
                        return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(p, entry, e4));
                    if (ph < e4.Hash)
                        return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(entry, p, e4));
                    return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf3(entry, e4, p));
                }

                return
                    hash == e0.Hash ? ((e0 = e0.Keep(entry)) == l5.Entry0 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4))) :
                    hash == e1.Hash ? ((e1 = e1.Keep(entry)) == l5.Entry1 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4))) :
                    hash == e2.Hash ? ((e2 = e2.Keep(entry)) == l5.Entry2 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4))) :
                    hash == e3.Hash ? ((e3 = e3.Keep(entry)) == l5.Entry3 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4))) :
                                      ((e4 = e4.Keep(entry)) == l5.Entry4 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)));
            }

            /// <inheritdoc />
            internal override ImHashMap234<K, V> AddOrKeepOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight)
            {
                var p = Plus;
                var ph = p.Hash;
                if (ph == hash)
                {
                    popEntry = null;
                    popRight = null;
                    return (p = p.Keep(entry)) == Plus ? this : new Leaf5Plus1(p, L5);
                }

                var l5 = L5;
                var e0 = l5.Entry0;
                var e1 = l5.Entry1;
                var e2 = l5.Entry2;
                var e3 = l5.Entry3;
                var e4 = l5.Entry4;

                if (hash > e4.Hash)
                {
                    if (ph < e2.Hash) 
                    {
                        popEntry = e2;
                        popRight = new Leaf3(e3, e4, entry);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, e1);
                        if (ph < e1.Hash)
                            return new Leaf3(e0, p, e1);
                        return new Leaf3(e0, e1, p);
                    }
                    if (ph < e3.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e3, e4, entry);
                        return new Leaf3(e0, e1, e2);
                    }
                    popEntry = e3;
                    popRight = ph < e4.Hash ? new Leaf3(p, e4, entry) : ph < hash ? new Leaf3(e4, p, entry) : new Leaf3(e4, entry, p);
                    return new Leaf3(e0, e1, e2);
                }

                if (hash < e0.Hash)
                {
                    if (ph < e1.Hash) 
                    {
                        popEntry = e1;
                        popRight = new Leaf3(e2, e3, e4);
                        if (ph < hash)
                            return new Leaf3(p, entry, e0);
                        if (ph < e0.Hash)
                            return new Leaf3(entry, p, e0);
                        return new Leaf3(entry, e0, p);
                    }
                    if (ph < e2.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e2, e3, e4);
                        return new Leaf3(entry, e0, e1);
                    }
                    popEntry = e2;
                    popRight = ph < e3.Hash ? new Leaf3(p, e3, e4) : ph < e4.Hash ? new Leaf3(e3, p, e4) : new Leaf3(e3, e4, p);
                    return new Leaf3(entry, e0, e1);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (ph < e1.Hash)
                    {
                        popEntry = e1;
                        popRight = new Leaf3(e2, e3, e4);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, entry);
                        if (ph < hash)
                            return new Leaf3(e0, p, entry);
                        return new Leaf3(e0, entry, p);
                    }
                    if (ph < e2.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e2, e3, e4);
                        return new Leaf3(e0, entry, e1);
                    }
                    popEntry = e2;
                    popRight = ph < e3.Hash ? new Leaf3(p, e3, e4) : ph < e4.Hash ? new Leaf3(e3, p, e4) : new Leaf3(e3, e4, p);
                    return new Leaf3(e0, entry, e1);
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    if (ph < hash)
                    {
                        popEntry = entry;
                        popRight = new Leaf3(e2, e3, e4);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, e1);
                        if (ph < e1.Hash)
                            return new Leaf3(e0, p, e1);
                        return new Leaf3(e0, e1, p);
                    }
                    if (ph < e2.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e2, e3, e4);
                        return new Leaf3(e0, e1, entry);
                    }
                    popEntry = e2;
                    popRight = ph < e3.Hash ? new Leaf3(p, e3, e4) : ph < e4.Hash ? new Leaf3(e3, p, e4) : new Leaf3(e3, e4, p);
                    return new Leaf3(e0, e1, entry);
                }

                if (hash > e2.Hash && hash < e3.Hash)
                {
                    if (ph < e2.Hash) 
                    {
                        popEntry = e2;
                        popRight = new Leaf3(entry, e3, e4);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, e1);
                        if (ph < e1.Hash)
                            return new Leaf3(e0, p, e1);
                        return new Leaf3(e0, e1, p);
                    }
                    if (ph < hash) 
                    {
                        popEntry = p;
                        popRight = new Leaf3(entry, e3, e4);
                        return new Leaf3(e0, e1, e2);
                    }
                    popEntry = entry;
                    popRight = ph < e3.Hash ? new Leaf3(p, e3, e4) : ph < e4.Hash ? new Leaf3(e3, p, e4) : new Leaf3(e3, e4, p);
                    return new Leaf3(e0, e1, e2);
                }

                if (hash > e3.Hash && hash < e4.Hash)
                {
                    if (ph < e2.Hash) 
                    {
                        popEntry = e2;
                        popRight = new Leaf3(e3, entry, e4);
                        if (ph < e0.Hash)
                            return new Leaf3(p, e0, e1);
                        if (ph < e1.Hash)
                            return new Leaf3(e0, p, e1);
                        return new Leaf3(e0, e1, p);
                    }
                    if (ph < e3.Hash)
                    {
                        popEntry = p;
                        popRight = new Leaf3(e3, entry, e4);
                        return new Leaf3(e0, e1, e2);
                    }
                    popEntry = e3;
                    popRight = ph < hash ? new Leaf3(p, entry, e4) : ph < e4.Hash ? new Leaf3(entry, p, e4) : new Leaf3(entry, e4, p);
                    return new Leaf3(e0, e1, e2);
                }

                popEntry = null;
                popRight = null;
                return
                    hash == e0.Hash ? ((e0 = e0.Keep(entry)) == l5.Entry0 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4))) :
                    hash == e1.Hash ? ((e1 = e1.Keep(entry)) == l5.Entry1 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4))) :
                    hash == e2.Hash ? ((e2 = e2.Keep(entry)) == l5.Entry2 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4))) :
                    hash == e3.Hash ? ((e3 = e3.Keep(entry)) == l5.Entry3 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4))) :
                                      ((e4 = e4.Keep(entry)) == l5.Entry4 ? this : new Leaf5Plus1(p, new Leaf5(e0, e1, e2, e3, e4)));
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

                return this;
            }

            /// <inheritdoc />
            public override IEnumerable<ValueEntry> Enumerate()
            {
                var p  = Plus;
                var ph = p.Hash;

                var l = L5;
                var e0 = l.Entry0;
                var e1 = l.Entry1;
                var e2 = l.Entry2;
                var e3 = l.Entry3;
                var e4 = l.Entry4;

                if (ph < e0.Hash) 
                {
                    if (p is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
                else 
                {
                    if (e0 is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e0).Conflicts)
                        yield return x;
                }

                if (ph < e1.Hash) 
                {
                    if (p is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
                else 
                {
                    if (e1 is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e1).Conflicts)
                        yield return x;
                }

                if (ph < e2.Hash) 
                {
                    if (p is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
                else 
                {
                    if (e2 is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e2).Conflicts)
                        yield return x;
                }

                if (ph < e3.Hash) 
                {
                    if (p is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
                else 
                {
                    if (e3 is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e3).Conflicts)
                        yield return x;
                }

                if (ph < e4.Hash) 
                {
                    if (p is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
                else 
                {
                    if (e4 is ValueEntry v)
                        yield return v;
                    else foreach (var x in ((ConflictsEntry)e4).Conflicts)
                        yield return x;

                    if (p is ValueEntry pv)
                        yield return pv;
                    else foreach (var x in ((ConflictsEntry)p).Conflicts)
                        yield return x;
                }
            }
        }

        /// <summary>Branch of 2 leafs or branches</summary>
        public sealed class Branch2 : ImHashMap234<K, V>
        {
            /// <summary>Entry in the middle</summary>
            public readonly Entry Entry0;

            /// <summary>Left branch</summary>
            public readonly ImHashMap234<K, V> Left;
            /// <summary>Right branch</summary>
            public readonly ImHashMap234<K, V> Right;

            /// <summary>Constructs</summary>
            public Branch2(ImHashMap234<K, V> left, Entry e, ImHashMap234<K, V> right)
            {
                Entry0 = e;
                Left   = left;
                Right  = right;
            }

            /// <inheritdoc />
            public override string ToString() =>
                !(Left is Branch2) && !(Left is Branch3) ? Left + " <- " + Entry0 + " -> " + Right : 
                Left.GetType().Name + " <- " + Entry0 + " -> " + Right.GetType().Name;

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash > Entry0.Hash ? Right.GetEntryOrDefault(hash) :
                hash < Entry0.Hash ? Left .GetEntryOrDefault(hash) :
                Entry0;

            /// <inheritdoc />
            public override IEnumerable<ValueEntry> Enumerate()
            {
                foreach (var x in Left.Enumerate())
                    yield return x;

                if (Entry0 is ValueEntry v)
                    yield return v;
                else foreach (var x in ((ConflictsEntry)Entry0).Conflicts)
                    yield return x;

                foreach (var x in Right.Enumerate())
                    yield return x;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                if (hash > e0.Hash)
                {
                    if (Right is Leaf5Plus1OrBranch3 x)
                    {
                        var newBranch = x.AddOrUpdateOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (popRight != null)
                            return new Branch3(Left, e0, newBranch, popEntry, popRight);
                        return new Branch2(Left, e0, newBranch);
                    }

                    return new Branch2(Left, e0, Right.AddOrUpdateEntry(hash, entry));
                }

                if (hash < e0.Hash)
                {
                    if (Left is Leaf5Plus1OrBranch3 x)
                    {
                        var newBranch = x.AddOrUpdateOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (popRight != null)
                            return new Branch3(newBranch, popEntry, popRight, e0, Right);
                        return new Branch2(newBranch, e0, Right);
                    }

                    return new Branch2(Left.AddOrUpdateEntry(hash, entry), e0, Right);
                }

                return new Branch2(Left, e0.Update(entry), Right);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                if (hash > e0.Hash)
                {
                    ImHashMap234<K, V> newBranch;
                    if (Right is Leaf5Plus1OrBranch3 x)
                    {
                        newBranch = x.AddOrKeepOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (newBranch == x)
                            return this;
                        if (popRight != null)
                            return new Branch3(Left, e0, newBranch, popEntry, popRight);
                        return new Branch2(Left, e0, newBranch);
                    }

                    newBranch = Right.AddOrKeepEntry(hash, entry);
                    return newBranch == Right ? this : new Branch2(Left, e0, newBranch);
                }

                if (hash < e0.Hash)
                {
                    ImHashMap234<K, V> newBranch;
                    if (Left is Leaf5Plus1OrBranch3 x)
                    {
                        newBranch = x.AddOrUpdateOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (newBranch == x)
                            return this;
                        if (popRight != null)
                            return new Branch3(newBranch, popEntry, popRight, e0, Right);
                        return new Branch2(newBranch, e0, Right);
                    }

                    newBranch = Left.AddOrKeepEntry(hash, entry);
                    return newBranch == Left ? this : new Branch2(newBranch, e0, Right);
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
                        if (l is Leaf5Plus1 l6)
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
                            return new Branch3(lb2.Left, lb2.Entry0, lb2.Right, e0, newRight);

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
                            return new Branch3(lb2.Left, lb2.Entry0, lb2.Right, e0, newRight);

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
        public sealed class Branch3 : Leaf5Plus1OrBranch3
        { 
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Right entry</summary>
            public readonly Entry Entry1;

            /// <summary>Left branch</summary>
            public readonly ImHashMap234<K, V> Left;
            /// <summary>Middle branch</summary>
            public readonly ImHashMap234<K, V> Middle;
            /// <summary>Rightmost branch</summary>
            public readonly ImHashMap234<K, V> Right;

            /// <summary>Constructs the branch</summary>
            public Branch3(ImHashMap234<K, V> left, Entry entry0, ImHashMap234<K, V> middle, Entry entry1, ImHashMap234<K, V> right)
            {
                Entry0 = entry0;
                Entry1 = entry1;
                Left   = left;
                Middle = middle;
                Right  = right;
            }

            /// <inheritdoc />
            public override string ToString() =>
                !(Left is Branch2) && !(Left is Branch3) ? Left + " <- " + Entry0 + " -> " + Middle + " <- " + Entry1 + " -> " + Right : 
                Left.GetType().Name + " <- " + Entry0 + " -> " + Middle.GetType().Name + " <- " + Entry1 + " -> " + Right.GetType().Name;

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
            public override IEnumerable<ValueEntry> Enumerate()
            {
                foreach (var x in Left.Enumerate())
                    yield return x;

                if (Entry0 is ValueEntry v0)
                    yield return v0;
                else foreach (var x in ((ConflictsEntry)Entry0).Conflicts)
                    yield return x;
                
                foreach (var x in Middle.Enumerate())
                    yield return x;

                if (Entry1 is ValueEntry v1)
                    yield return v1;
                else foreach (var x in ((ConflictsEntry)Entry1).Conflicts)
                    yield return x;

                foreach (var x in Right.Enumerate())
                    yield return x;
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var h0 = Entry0.Hash;
                var h1 = Entry1.Hash;
                
                if (hash > h1)
                {
                     // No need to call the Split method because we won't destruct the result branch
                    var newBranch = Right.AddOrUpdateEntry(hash, entry);
                    if (newBranch is Branch2 && Right is Leaf5Plus1OrBranch3)
                        return new Branch2(new Branch2(Left, Entry0, Middle), Entry1, newBranch);
                    return new Branch3(Left, Entry0, Middle, Entry1, newBranch);
                }

                if (hash < h0)
                {
                    var newBranch = Left.AddOrUpdateEntry(hash, entry);
                    if (newBranch is Branch2 && Left is Leaf5Plus1OrBranch3) 
                        return new Branch2(newBranch, Entry0, new Branch2(Middle, Entry1, Right));
                    return new Branch3(newBranch, Entry0, Middle, Entry1, Right);
                }

                if (hash > h0 && hash < h1)
                {
                    if (Middle is Leaf5Plus1OrBranch3 x)
                    {
                        var newMiddle = x.AddOrUpdateOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (popRight != null) 
                            return new Branch2(new Branch2(Left, Entry0, newMiddle), popEntry, new Branch2(popRight, Entry1, Right));
                        return new Branch3(Left, Entry0, newMiddle, Entry1, Right);
                    }

                    return new Branch3(Left, Entry0, Middle.AddOrUpdateEntry(hash, entry), Entry1, Right);
                }

                return hash == h0
                    ? new Branch3(Left, Entry0.Update(entry), Middle, Entry1, Right)
                    : new Branch3(Left, Entry0, Middle, Entry1.Update(entry), Right);
            }

            internal override ImHashMap234<K, V> AddOrUpdateOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight)
            {
                var h0 = Entry0.Hash;
                var h1 = Entry1.Hash;

                if (hash > h1)
                {
                    var newRight = Right.AddOrUpdateEntry(hash, entry);
                    if (newRight is Branch2 && Right is Leaf5Plus1OrBranch3)
                    {
                        popEntry = Entry1;
                        popRight = newRight;
                        return new Branch2(Left, Entry0, Middle);
                    }

                    popEntry = null;
                    popRight = null;
                    return new Branch3(Left, Entry0, Middle, Entry1, newRight);
                }

                if (hash < h0)
                {
                    var newLeft = Left.AddOrUpdateEntry(hash, entry);
                    if (newLeft is Branch2 && Left is Leaf5Plus1OrBranch3)
                    {
                        popEntry = Entry0;
                        popRight = new Branch2(Middle, Entry1, Right);
                        return newLeft;
                    }

                    popEntry = null;
                    popRight = null;
                    return new Branch3(newLeft, Entry0, Middle, Entry1, Right);
                }

                if (hash > h0 && hash < h1)
                {
                    if (Middle is Leaf5Plus1OrBranch3 x)
                    {
                        var newMiddle = x.AddOrUpdateOrSplitEntry(hash, entry, out popEntry, out var popRightBelow);
                        if (popRightBelow != null) 
                        {
                            //                              [4]
                            //       [2, 7]            [2]         [7]
                            // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                            // and adding 6
                            popRight = new Branch2(popRightBelow, Entry1, Right);
                            return new Branch2(Left, Entry0, newMiddle);
                        }

                        popEntry = null;
                        popRight = null;
                        return new Branch3(Left, Entry0, newMiddle, Entry1, Right);
                    }

                    popEntry = null;
                    popRight = null;
                    return new Branch3(Left, Entry0, Middle.AddOrUpdateEntry(hash, entry), Entry1, Right);
                }

                popEntry = null;
                popRight = null;
                return hash == h0
                    ? new Branch3(Left, Entry0.Update(entry), Middle, Entry1, Right)
                    : new Branch3(Left, Entry0, Middle, Entry1.Update(entry), Right);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;

                if (hash > e1.Hash)
                {
                    var newBranch = Right.AddOrKeepEntry(hash, entry);
                    if (newBranch == Right)
                        return this;
                    if (newBranch is Branch2 && Right is Leaf5Plus1OrBranch3) 
                        return new Branch2(new Branch2(Left, e0, Middle), e1, newBranch);
                    return new Branch3(Left, e0, Middle, e1, newBranch);
                }

                if (hash < e0.Hash)
                {
                    var newBranch = Left.AddOrKeepEntry(hash, entry);
                    if (newBranch == Left)
                        return this;
                    if (newBranch is Branch2 && Left is Leaf5Plus1OrBranch3) 
                        return new Branch2(newBranch, e0, new Branch2(Middle, e1, Right));
                    return new Branch3(newBranch, e0, Middle, e1, Right);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    ImHashMap234<K, V> newMiddle;
                    if (Middle is Leaf5Plus1OrBranch3 x)
                    {
                        newMiddle = x.AddOrKeepOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (newMiddle == x)
                            return this;
                        if (popRight != null) 
                            return new Branch2(new Branch2(Left, e0, newMiddle), popEntry, new Branch2(popRight, e1, Right));
                        return new Branch3(Left, e0, newMiddle, e1, Right);
                    }

                    newMiddle = Middle.AddOrKeepEntry(hash, entry);
                    return newMiddle == Middle ? this : new Branch3(Left, e0, newMiddle, e1, Right);
                }

                return hash == e0.Hash
                    ? ((e0 = e0.Keep(entry)) == Entry0 ? this : new Branch3(Left, e0, Middle, e1, Right))
                    : ((e1 = e1.Keep(entry)) == Entry1 ? this : new Branch3(Left, e0, Middle, e1, Right));
            }

            internal override ImHashMap234<K, V> AddOrKeepOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight)
            {
                var e0 = Entry0;
                var e1 = Entry1;

                popEntry = null;
                popRight = null;

                if (hash > e1.Hash)
                {
                    var newBranch = Right.AddOrKeepEntry(hash, entry);
                    if (newBranch == Right)
                        return this;
                    if (newBranch is Branch2 && Right is Leaf5Plus1OrBranch3)
                    {
                        popEntry = e1;
                        popRight = newBranch;
                        return new Branch2(Left, e0, Middle);
                    }
                    return new Branch3(Left, e0, Middle, e1, newBranch);
                }

                if (hash < e0.Hash)
                {
                    var newBranch = Left.AddOrKeepEntry(hash, entry);
                    if (newBranch == Left)
                        return this;
                    if (newBranch is Branch2 && Left is Leaf5Plus1OrBranch3)
                    {
                        popEntry = e0;
                        popRight = new Branch2(Middle, e1, Right);
                        return newBranch;
                    }
                    return new Branch3(newBranch, e0, Middle, e1, Right);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    ImHashMap234<K, V> newMiddle;
                    if (Middle is Leaf5Plus1OrBranch3 x)
                    {
                        newMiddle = x.AddOrKeepOrSplitEntry(hash, entry, out popEntry, out var popRightBelow);
                        if (newMiddle == x)
                            return this;
                        if (popRightBelow != null) 
                        {
                            //                              [4]
                            //       [2, 7]            [2]         [7]
                            // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                            // and adding 6
                            popRight = new Branch2(popRightBelow, e1, Right);
                            return new Branch2(Left, e0, newMiddle);
                        }
                        return new Branch3(Left, e0, newMiddle, e1, Right);
                    }

                    newMiddle = Middle.AddOrKeepEntry(hash, entry);
                    return newMiddle == Middle ? this : new Branch3(Left, e0, newMiddle, e1, Right);
                }

                return hash == e0.Hash
                    ? ((e0 = e0.Keep(entry)) == Entry0 ? this : new Branch3(Left, e0, Middle, e1, Right))
                    : ((e1 = e1.Keep(entry)) == Entry1 ? this : new Branch3(Left, e0, Middle, e1, Right));
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
                                return new Branch3(Left, Entry0, l4l3, p, new Leaf2(e1, re));
                            if (ph < l3e0.Hash)
                                return new Branch3(Left, Entry0, new Leaf3(p, l3e0, l3e1), l3e2, new Leaf2(e1, re));
                            if (ph < l3e1.Hash)
                                return new Branch3(Left, Entry0, new Leaf3(l3e0, p, l3e1), l3e2, new Leaf2(e1, re));
                            return new Branch3(Left, Entry0, new Leaf3(l3e0, l3e1, p), l3e2, new Leaf2(e1, re));
                        }

                        if (m is Leaf5 l5) 
                            return new Branch3(Left, Entry0, new Leaf3Plus1(l5.Entry0, new Leaf3(l5.Entry1, l5.Entry2, l5.Entry3)), l5.Entry4, new Leaf2(e1, re));

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
                                return new Branch3(Left, Entry0, l6l5, p, new Leaf2(e1, re));
                            if (ph < l5e0.Hash)
                                return new Branch3(Left, Entry0, new Leaf5(p, l5e0, l5e1, l5e2, l5e3), l5e4, new Leaf2(e1, re));
                            if (ph < l5e1.Hash)
                                return new Branch3(Left, Entry0, new Leaf5(l5e0, p, l5e1, l5e2, l5e3), l5e4, new Leaf2(e1, re));
                            if (ph < l5e2.Hash)
                                return new Branch3(Left, Entry0, new Leaf5(l5e0, l5e1, p, l5e2, l5e3), l5e4, new Leaf2(e1, re));
                            if (ph < l5e3.Hash)
                                return new Branch3(Left, Entry0, new Leaf5(l5e0, l5e1, l5e2, p, l5e3), l5e4, new Leaf2(e1, re));
                            return new Branch3(Left, Entry0, new Leaf5(l5e0, l5e1, l5e2, l5e3, p), l5e4, new Leaf2(e1, re));
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
                            return new Branch2(Left, Entry0, new Branch3(mb2.Left, mb2.Entry0, mb2.Right, e1, newRight)); 

                        //      -15             0                                 -15          -5                              
                        //    /         |              \                          /       |         \                          
                        // -20       -10  -5             4      7              -20       -10           0                       
                        //  |       /   |   \          /    |     \             |        /   \       /        \                
                        //  x      a    b    c   1 2 3     5 6   8 9 10 11  =>  ?       a     b     c        4     7           
                        //  |      |    |    |                                  |       |     |     |      /    |     \        
                        //  ?      ?    ?    ?                                  ?       ?     ?     ?    1 2 3  5 6   8 9 10 11
                        if (Middle is Branch3 mb3)
                            return new Branch3(Left, Entry0, 
                                new Branch2(mb3.Left, mb3.Entry0, mb3.Middle), mb3.Entry1, new Branch2(mb3.Right, e1, newRight));
                    }

                    return new Branch3(Left, Entry0, Middle, Entry1, newRight);
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

    /// <summary>ImMap methods</summary>
    public static class ImHashMap234
    {
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