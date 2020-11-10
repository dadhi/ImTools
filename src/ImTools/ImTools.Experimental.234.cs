
using System;
using System.Collections.Generic;
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
        protected ImHashMap234() { }

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
            protected Entry(int hash) => Hash = hash;

            /// <inheritdoc />
            public sealed override Entry GetEntryOrDefault(int hash) => hash == Hash ? this : null;

            internal abstract Entry Update(ValueEntry entry);
            internal abstract Entry Keep(ValueEntry entry);
            internal abstract ImHashMap234<K, V> Remove(K key);
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
            public override string ToString() => "[" + Hash + "]" + Key + ": " + Value;

            internal override Entry Update(ValueEntry entry) => 
                Key.Equals(entry.Key) ? entry : (Entry)new ConflictsEntry(Hash, this, entry);

            internal override Entry Keep(ValueEntry entry) => 
                Key.Equals(entry.Key) ? this : (Entry)new ConflictsEntry(Hash, this, entry);

            internal override ImHashMap234<K, V> Remove(K key) => 
                Key.Equals(key) ? Empty : this;

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
                hash == Hash ? Remove(key) : this;

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

            internal override ImHashMap234<K, V> Remove(K key) 
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
                hash == Hash ? Remove(key) : this;

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
            public Leaf2(Entry entry0, Entry entry1)
            {
                Entry0 = entry0;
                Entry1 = entry1;
            }

            /// <inheritdoc />
            public override string ToString() => "leaf2>> " + Entry0 + "; " + Entry1;

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
                if (hash == Entry0.Hash)
                    return Entry0.Remove(key) == Entry0 ? this : (ImHashMap234<K, V>)Entry1;
                if (hash == Entry1.Hash)
                    return Entry1.Remove(key) == Entry1 ? this : (ImHashMap234<K, V>)Entry0;
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
            public Leaf3(Entry entry0, Entry entry1, Entry entry2)
            {
                Entry0 = entry0;
                Entry1 = entry1;
                Entry2 = entry2;
            }

            /// <inheritdoc />
            public override string ToString() => "leaf3>> " + Entry0 + "; " + Entry1 + "; " + Entry2;

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash == Entry0.Hash  ? Entry0 :
                hash == Entry1.Hash  ? Entry1 :
                hash == Entry2.Hash  ? Entry2 :
                null;

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                return
                    hash > e2.Hash                   ? new Leaf4(e0, e1, e2, entry) :
                    hash < e0.Hash                   ? new Leaf4(entry, e0, e1, e2) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf4(e0, entry, e1, e2) :
                    hash > e1.Hash && hash < e2.Hash ? new Leaf4(e0, e1, entry, e2) :
                    hash == e0.Hash   ? new Leaf3(e0.Update(entry), e1, e2) :
                    hash == e1.Hash   ? new Leaf3(e0, e1.Update(entry), e2) :
                    (ImHashMap234<K, V>)new Leaf3(e0, e1, e2.Update(entry));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                return
                    hash > e2.Hash                   ? new Leaf4(e0, e1, e2, entry) :
                    hash < e0.Hash                   ? new Leaf4(entry, e0, e1, e2) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf4(e0, entry, e1, e2) :
                    hash > e1.Hash && hash < e2.Hash ? new Leaf4(e0, e1, entry, e2) :
                    hash == e0.Hash ?   ((e0 = e0.Keep(entry)) == Entry0 ? this : new Leaf3(e0, e1, e2)) :
                    hash == e1.Hash ?   ((e1 = e1.Keep(entry)) == Entry1 ? this : new Leaf3(e0, e1, e2)) :
                    (ImHashMap234<K, V>)((e2 = e2.Keep(entry)) == Entry2 ? this : new Leaf3(e0, e1, e2));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                if (hash == Entry0.Hash)
                    return Entry0.Remove(key) == Entry0 ? this : (ImHashMap234<K, V>)new Leaf2(Entry1, Entry2);
                if (hash == Entry1.Hash)
                    return Entry1.Remove(key) == Entry1 ? this : (ImHashMap234<K, V>)new Leaf2(Entry0, Entry2);
                if (hash == Entry2.Hash)
                    return Entry2.Remove(key) == Entry2 ? this : (ImHashMap234<K, V>)new Leaf2(Entry0, Entry1);
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

        /// <summary>Leaf with 4 entries</summary>
        public sealed class Leaf4 : ImHashMap234<K, V>
        {
            /// <summary>Left entry</summary>
            public readonly Entry Entry0;
            /// <summary>Middle Left entry</summary>
            public readonly Entry Entry1;
            /// <summary>Middle Right entry</summary>
            public readonly Entry Entry2;
            /// <summary>Right entry</summary>
            public readonly Entry Entry3;

            /// <summary>Constructs the leaf</summary>
            public Leaf4(Entry entry0, Entry entry1, Entry entry2, Entry entry3)
            {
                Entry0 = entry0;
                Entry1 = entry1;
                Entry2 = entry2;
                Entry3 = entry3;
            }

            /// <inheritdoc />
            public override string ToString() => "leaf4>> " + Entry0 + "; " + Entry1 + "; " + Entry2 + "; " + Entry3;

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int hash) =>
                hash == Entry0.Hash ? Entry0 :
                hash == Entry1.Hash ? Entry1 :
                hash == Entry2.Hash ? Entry2 :
                hash == Entry3.Hash ? Entry3 :
                null;

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                return
                    hash > e3.Hash                   ? new Leaf5(e0, e1, e2, e3, entry) :
                    hash < e0.Hash                   ? new Leaf5(entry, e0, e1, e2, e3) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf5(e0, entry, e1, e2, e3) :
                    hash > e1.Hash && hash < e2.Hash ? new Leaf5(e0, e1, entry, e2, e3) :
                    hash > e2.Hash && hash < e3.Hash ? new Leaf5(e0, e1, e2, entry, e3) :
                    hash == e0.Hash   ? new Leaf4(e0.Update(entry), e1, e2, e3) :
                    hash == e1.Hash   ? new Leaf4(e0, e1.Update(entry), e2, e3) :
                    hash == e2.Hash   ? new Leaf4(e0, e1, e2.Update(entry), e3) :
                    (ImHashMap234<K, V>)new Leaf4(e0, e1, e2, e3.Update(entry));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                return
                    hash > e3.Hash                   ? new Leaf5(e0, e1, e2, e3, entry) :
                    hash < e0.Hash                   ? new Leaf5(entry, e0, e1, e2, e3) :
                    hash > e0.Hash && hash < e1.Hash ? new Leaf5(e0, entry, e1, e2, e3) :
                    hash > e1.Hash && hash < e2.Hash ? new Leaf5(e0, e1, entry, e2, e3) :
                    hash > e2.Hash && hash < e3.Hash ? new Leaf5(e0, e1, e2, entry, e3) :
                    hash == e0.Hash ?   ((e0 = e0.Keep(entry)) == Entry0 ? this : new Leaf4(e0, e1, e2, e3)) :
                    hash == e1.Hash ?   ((e1 = e1.Keep(entry)) == Entry1 ? this : new Leaf4(e0, e1, e2, e3)) :
                    hash == e2.Hash ?   ((e2 = e2.Keep(entry)) == Entry2 ? this : new Leaf4(e0, e1, e2, e3)) :
                    (ImHashMap234<K, V>)((e3 = e3.Keep(entry)) == Entry3 ? this : new Leaf4(e0, e1, e2, e3));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                if (hash == Entry0.Hash)
                    return Entry0.Remove(key) == Entry0 ? this : (ImHashMap234<K, V>)new Leaf3(Entry1, Entry2, Entry3);
                if (hash == Entry1.Hash)
                    return Entry1.Remove(key) == Entry1 ? this : (ImHashMap234<K, V>)new Leaf3(Entry0, Entry2, Entry3);
                if (hash == Entry2.Hash)
                    return Entry2.Remove(key) == Entry2 ? this : (ImHashMap234<K, V>)new Leaf3(Entry0, Entry1, Entry3);
                if (hash == Entry3.Hash)
                    return Entry3.Remove(key) == Entry3 ? this : (ImHashMap234<K, V>)new Leaf3(Entry0, Entry1, Entry2);
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
            }
        }

        /// <summary>Splittable cases: Leaf5 or Branch3
        /// Note: The result of the split is always the Branch2 consisting of returned map, popEntry, and popRight</summary>
        public abstract class Leaf5OrBranch3 : ImHashMap234<K, V> 
        {
            internal abstract ImHashMap234<K, V> AddOrUpdateOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight);

            internal abstract ImHashMap234<K, V> AddOrKeepOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight);
        }

        /// <summary>Leaf with 5 entries</summary>
        public sealed class Leaf5 : Leaf5OrBranch3
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
            public Leaf5(Entry entry0, Entry entry1, Entry entry2, Entry entry3, Entry entry4)
            {
                Entry0 = entry0;
                Entry1 = entry1;
                Entry2 = entry2;
                Entry3 = entry3;
                Entry4 = entry4;
            }

            /// <inheritdoc />
            public override string ToString() => "leaf5>> " + Entry0 + "; " + Entry1 + "; " + Entry2 + "; " + Entry3 + "; " + Entry4;

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

                if (hash > e4.Hash)
                    return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf2(e4, entry));

                if (hash < e0.Hash)
                    return new Branch2(new Leaf2(entry, e0), e1, new Leaf3(e2, e3, e4));

                if (hash > e0.Hash && hash < e1.Hash)
                    return new Branch2(new Leaf2(e0, entry), e1, new Leaf3(e2, e3, e4));

                if (hash > e1.Hash && hash < e2.Hash)
                    return new Branch2(new Leaf2(e0, e1), entry, new Leaf3(e2, e3, e4));

                if (hash > e2.Hash && hash < e3.Hash)
                    return new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf2(e3, e4));

                if (hash > e3.Hash && hash < e4.Hash)
                    return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf2(entry, e4));

                return
                    hash == e0.Hash   ? new Leaf5(e0.Update(entry), e1, e2, e3, e4) :
                    hash == e1.Hash   ? new Leaf5(e0, e1.Update(entry), e2, e3, e4) :
                    hash == e2.Hash   ? new Leaf5(e0, e1, e2.Update(entry), e3, e4) :
                    hash == e3.Hash   ? new Leaf5(e0, e1, e3, e2.Update(entry), e4) :
                    (ImHashMap234<K, V>)new Leaf5(e0, e1, e2, e3, e4.Update(entry));
            }

            /// <summary>
            /// The same as `AddOrUpdateEntry` but instead of constructing the new map it returns the parts: return value is the Left node, 
            /// `ref Entry entry` (always passed as ValueEntry) will be set to the middle entry, and `popRight` is the right node.
            /// </summary>
            internal override ImHashMap234<K, V> AddOrUpdateOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                var e4 = Entry4;

                if (hash > e4.Hash)
                {
                    popEntry = e3; // todo: @perf look at what the results popEntry is set to and may be use the popEntry instead of the one of the vars above, then don't forget to use popRight on the consumer side, and remove the `popEntry = null` below
                    popRight = new Leaf2(e4, entry);
                    return new Leaf3(e0, e1, e2);
                }

                if (hash < e0.Hash)
                {
                    popEntry = e1;
                    popRight = new Leaf3(e2, e3, e4);
                    return new Leaf2(entry, e0);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    popEntry = e1;
                    popRight = new Leaf3(e2, e3, e4);
                    return new Leaf2(e0, entry);
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    popEntry = entry;
                    popRight = new Leaf3(e2, e3, e4);
                    return new Leaf2(e0, e1);
                }

                if (hash > e2.Hash && hash < e3.Hash)
                {
                    popEntry = entry;
                    popRight = new Leaf2(e3, e4);
                    return new Leaf3(e0, e1, e2);
                }

                if (hash > e3.Hash && hash < e4.Hash)
                {
                    popEntry = e3;
                    popRight = new Leaf2(entry, e4);
                    return new Leaf3(e0, e1, e2);
                }

                popEntry = null;
                popRight = null;
                return
                    hash == e0.Hash ? new Leaf5(e0.Update(entry), e1, e2, e3, e4) :
                    hash == e1.Hash ? new Leaf5(e0, e1.Update(entry), e2, e3, e4) :
                    hash == e2.Hash ? new Leaf5(e0, e1, e2.Update(entry), e3, e4) :
                    hash == e3.Hash ? new Leaf5(e0, e1, e3, e2.Update(entry), e4) :
                                      new Leaf5(e0, e1, e2, e3, e4.Update(entry));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                var e4 = Entry4;

                if (hash > e4.Hash)
                    return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf2(e4, entry));

                if (hash < e0.Hash)
                    return new Branch2(new Leaf2(entry, e0), e1, new Leaf3(e2, e3, e4));

                if (hash > e0.Hash && hash < e1.Hash)
                    return new Branch2(new Leaf2(e0, entry), e1, new Leaf3(e2, e3, e4));

                if (hash > e1.Hash && hash < e2.Hash)
                    return new Branch2(new Leaf2(e0, e1), entry, new Leaf3(e2, e3, e4));

                if (hash > e2.Hash && hash < e3.Hash)
                    return new Branch2(new Leaf3(e0, e1, e2), entry, new Leaf2(e3, e4));

                if (hash > e3.Hash && hash < e4.Hash)
                    return new Branch2(new Leaf3(e0, e1, e2), e3, new Leaf2(entry, e4));

                return
                    hash == e0.Hash ?   ((e0 = e0.Keep(entry)) == Entry0 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e1.Hash ?   ((e1 = e1.Keep(entry)) == Entry1 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e2.Hash ?   ((e2 = e2.Keep(entry)) == Entry2 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e3.Hash ?   ((e3 = e3.Keep(entry)) == Entry3 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    (ImHashMap234<K, V>)((e4 = e4.Keep(entry)) == Entry4 ? this : new Leaf5(e0, e1, e2, e3, e4));
            }

            /// <summary>
            /// The same as `AddOrKeepEntry` but instead of constructing the new map it returns the parts: return value is the Left node, 
            /// `ref Entry entry` (always passed as ValueEntry) will be set to the middle entry, and `popRight` is the right node.
            /// </summary>
            internal override ImHashMap234<K, V> AddOrKeepOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight)
            {
                var e0 = Entry0;
                var e1 = Entry1;
                var e2 = Entry2;
                var e3 = Entry3;
                var e4 = Entry4;

                if (hash > e4.Hash)
                {
                    popEntry = e3;
                    popRight = new Leaf2(e4, entry);
                    return new Leaf3(e0, e1, e2);
                }

                if (hash < e0.Hash)
                {
                    popEntry = e1;
                    popRight = new Leaf3(e2, e3, e4);
                    return new Leaf2(entry, e0);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    popEntry = e1;
                    popRight = new Leaf3(e2, e3, e4);
                    return new Leaf2(e0, entry);
                }

                if (hash > e1.Hash && hash < e2.Hash)
                {
                    popEntry = entry;
                    popRight = new Leaf3(e2, e3, e4);
                    return new Leaf2(e0, e1);
                }

                if (hash > e2.Hash && hash < e3.Hash)
                {
                    popEntry = entry;
                    popRight = new Leaf2(e3, e4);
                    return new Leaf3(e0, e1, e2);
                }

                if (hash > e3.Hash && hash < e4.Hash)
                {
                    popEntry = e3;
                    popRight = new Leaf2(entry, e4);
                    return new Leaf3(e0, e1, e2);
                }

                popEntry = null;
                popRight = null;
                return
                    hash == e0.Hash ? ((e0 = e0.Keep(entry)) == Entry0 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e1.Hash ? ((e1 = e1.Keep(entry)) == Entry1 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e2.Hash ? ((e2 = e2.Keep(entry)) == Entry2 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                    hash == e3.Hash ? ((e3 = e3.Keep(entry)) == Entry3 ? this : new Leaf5(e0, e1, e2, e3, e4)) :
                                      ((e4 = e4.Keep(entry)) == Entry4 ? this : new Leaf5(e0, e1, e2, e3, e4));
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key)
            {
                if (hash == Entry0.Hash)
                    return Entry0.Remove(key) == Entry0 ? this : (ImHashMap234<K, V>)new Leaf4(Entry1, Entry2, Entry3, Entry4);
                if (hash == Entry1.Hash)
                    return Entry1.Remove(key) == Entry1 ? this : (ImHashMap234<K, V>)new Leaf4(Entry0, Entry2, Entry3, Entry4);
                if (hash == Entry2.Hash)
                    return Entry2.Remove(key) == Entry2 ? this : (ImHashMap234<K, V>)new Leaf4(Entry0, Entry1, Entry3, Entry4);
                if (hash == Entry3.Hash)
                    return Entry3.Remove(key) == Entry3 ? this : (ImHashMap234<K, V>)new Leaf4(Entry0, Entry1, Entry2, Entry4);
                if (hash == Entry4.Hash)
                    return Entry4.Remove(key) == Entry4 ? this : (ImHashMap234<K, V>)new Leaf4(Entry0, Entry1, Entry2, Entry3);
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

        /// <summary>Branch of 2 leafs or branches - 
        /// it will never split itself but may produce the Branch of 3 if the lower branches are split</summary>
        public sealed class Branch2 : ImHashMap234<K, V>
        {
            /// <summary>Entry in the middle</summary>
            public readonly Entry Entry0;

            /// <summary>Left branch</summary>
            public readonly ImHashMap234<K, V> Left;
            /// <summary>Right branch</summary>
            public readonly ImHashMap234<K, V> Right;

            /// <summary>Constructs</summary>
            public Branch2(ImHashMap234<K, V> left, Entry entry0, ImHashMap234<K, V> right)
            {
                Entry0 = entry0;
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
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                if (hash > e0.Hash)
                {
                    // The only two cases where to expect the split: Leaf5 or Branch3
                    if (Right is Leaf5OrBranch3 x)
                    {
                        var newRight = x.AddOrUpdateOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (popRight != null)
                            return new Branch3(Left, e0, newRight, popEntry, popRight);
                        return new Branch2(Left, e0, newRight);
                    }

                    return new Branch2(Left, e0, Right.AddOrUpdateEntry(hash, entry));
                }

                if (hash < e0.Hash)
                {
                    if (Left is Leaf5OrBranch3 x)
                    {
                        var newLeft = x.AddOrUpdateOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (popRight != null)
                            return new Branch3(newLeft, popEntry, popRight, e0, Right);
                        return new Branch2(newLeft, e0, Right);
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
                    ImHashMap234<K, V> newRight;
                    if (Right is Leaf5OrBranch3 x)
                    {
                        newRight = x.AddOrKeepOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (newRight == x)
                            return this;
                        if (popRight != null)
                            return new Branch3(Left, e0, newRight, popEntry, popRight);
                        return new Branch2(Left, e0, newRight);
                    }

                    newRight = Right.AddOrKeepEntry(hash, entry);
                    return newRight == Right ? this : new Branch2(Left, e0, newRight);
                }

                if (hash < e0.Hash)
                {
                    ImHashMap234<K, V> newLeft;
                    if (Left is Leaf5OrBranch3 x)
                    {
                        newLeft = x.AddOrUpdateOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (newLeft == x)
                            return this;
                        if (popRight != null)
                            return new Branch3(newLeft, popEntry, popRight, e0, Right);
                        return new Branch2(newLeft, e0, Right);
                    }

                    newLeft = Left.AddOrKeepEntry(hash, entry);
                    return newLeft == Left ? this : new Branch2(newLeft, e0, Right);
                }

                return (e0 = e0.Keep(entry)) == Entry0 ? this : new Branch2(Left, e0, Right);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> RemoveEntry(int hash, K key) // todo: @wip remove the key
            {
                var e0 = Entry0;
                if (hash > e0.Hash) 
                {
                    //        4
                    //      /   \
                    //  1 2 3   5 [6]

                    var newRight = Right.Remove(hash, key);
                    if (newRight == Right)
                        return this;

                    if (newRight is Entry re) 
                    {
                        // If the Left is not a Leaf2, move its one entry to the Right
                        if (Left is Leaf3 l3)
                            return new Branch2(new Leaf2(l3.Entry0, l3.Entry1), l3.Entry2, new Leaf2(e0, re)); 
                        if (Left is Leaf4 l4)
                            return new Branch2(new Leaf3(l4.Entry0, l4.Entry1, l4.Entry2), l4.Entry3, new Leaf2(e0, re)); 
                        if (Left is Leaf5 l5)
                            return new Branch2(new Leaf4(l5.Entry0, l5.Entry1, l5.Entry2, l5.Entry3), l5.Entry4, new Leaf2(e0, re));

                        // Case #1
                        // If the Left is Leaf2 -> reduce the whole branch to the Leaf4 and rely on the upper branch (if any) to balance itself,
                        // see this case handled below..
                        var l2 = (Leaf2)Left;
                        return new Leaf4(l2.Entry0, l2.Entry1, e0, re);
                    }

                    // Handling Case #1
                    if (newRight is Leaf4 && Right is Branch2) // no need to check for the Branch3 because there is no way that Leaf4 will be the result of deleting one element from it 
                    {
                        //             7                       4     7 
                        //          /      \                 /    |     \
                        //        4      8 9 10 11  =>   1 2 3   5 6   8 9 10 11
                        //      /   \                    
                        //   1 2 3   5 6                  

                        // Case #2
                        // The result tree height is decreased, so we should not forget to rebalance with the other part of the tree on the upper level
                        // see the case handled below...
                        if (Left is Branch2 lb2)
                            return new Branch3(lb2.Left, lb2.Entry0, lb2.Right, e0, newRight);

                        //                     10                            7
                        //              /           \                     /     \
                        //        4      7        11 12 13 14 =>       4          10
                        //      /     |    \                         /    \     /    \
                        //   1 2 3   5 6    8 9                   1 2 3   5 6|8 9   11 12 13 14

                        if (Left is Branch3 lb3) // the result tree height is the same - no  need to rebalance
                            return new Branch2(new Branch2(lb3.Left, lb3.Entry0, lb3.Middle), lb3.Entry1, new Branch2(lb3.Right, e0, newRight));
                    }

                    // Handling the Case #2
                    if (newRight is Branch3 && Right is Branch2)
                    {
                        // todo: @wip
                    } 

                    return new Branch2(Left, e0, newRight);
                }

                if (hash < e0.Hash) 
                {

                }

                
                // todo: @wip remove the e0 and try to keep the branch until its possible
                return this;
            }
        }

        /// <summary>Branch of 3 leafs or branches and two entries</summary>
        public sealed class Branch3 : Leaf5OrBranch3
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
                var e0 = Entry0;
                var e1 = Entry1;
                return
                    hash == e0.Hash ? e0 : 
                    hash == e1.Hash ? e1 : 
                    hash > e1.Hash ? Right.GetEntryOrDefault(hash) :
                    hash < e0.Hash ? Left .GetEntryOrDefault(hash) :
                    Middle.GetEntryOrDefault(hash);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrUpdateEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;

                if (hash > e1.Hash)
                {
                     // No need to call the Split method because we won't destruct the result branch
                    var newRight = Right.AddOrUpdateEntry(hash, entry);
                    if (newRight is Branch2 && Right is Leaf5OrBranch3)
                        return new Branch2(new Branch2(Left, e0, Middle), e1, newRight);
                    return new Branch3(Left, e0, Middle, e1, newRight);
                }

                if (hash < e0.Hash)
                {
                    var newLeft = Left.AddOrUpdateEntry(hash, entry);
                    if (newLeft is Branch2 && Left is Leaf5OrBranch3) 
                        return new Branch2(newLeft, e0, new Branch2(Middle, e1, Right));
                    return new Branch3(newLeft, e0, Middle, e1, Right);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (Middle is Leaf5OrBranch3 x)
                    {
                        var newMiddle = x.AddOrUpdateOrSplitEntry(hash, entry, out var popEntry, out var popRight);
                        if (popRight != null) 
                            return new Branch2(new Branch2(Left, e0, newMiddle), popEntry, new Branch2(popRight, e1, Right));
                        return new Branch3(Left, e0, newMiddle, e1, Right);
                    }

                    return new Branch3(Left, e0, Middle.AddOrUpdateEntry(hash, entry), e1, Right);
                }

                return hash == e0.Hash
                    ? new Branch3(Left, e0.Update(entry), Middle, e1, Right)
                    : new Branch3(Left, e0, Middle, e1.Update(entry), Right);
            }

            internal override ImHashMap234<K, V> AddOrUpdateOrSplitEntry(int hash, ValueEntry entry, 
                out Entry popEntry, out ImHashMap234<K, V> popRight)
            {
                var e0 = Entry0;
                var e1 = Entry1;

                popEntry = null;
                popRight = null;

                if (hash > e1.Hash)
                {
                    var newRight = Right.AddOrUpdateEntry(hash, entry);
                    if (newRight is Branch2 && Right is Leaf5OrBranch3)
                    {
                        popEntry = e1;
                        popRight = newRight;
                        return new Branch2(Left, e0, Middle);
                    }
                    return new Branch3(Left, e0, Middle, e1, newRight);
                }

                if (hash < e0.Hash)
                {
                    var newLeft = Left.AddOrUpdateEntry(hash, entry);
                    if (newLeft is Branch2 && Left is Leaf5OrBranch3)
                    {
                        popEntry = e0;
                        popRight = new Branch2(Middle, e1, Right);
                        return newLeft;
                    }
                    return new Branch3(newLeft, e0, Middle, e1, Right);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    if (Middle is Leaf5OrBranch3 x)
                    {
                        var newMiddle = x.AddOrUpdateOrSplitEntry(hash, entry, out popEntry, out var popRightBelow);
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

                    return new Branch3(Left, e0, Middle.AddOrUpdateEntry(hash, entry), e1, Right);
                }

                return hash == e0.Hash
                    ? new Branch3(Left, e0.Update(entry), Middle, e1, Right)
                    : new Branch3(Left, e0, Middle, e1.Update(entry), Right);
            }

            /// <inheritdoc />
            public override ImHashMap234<K, V> AddOrKeepEntry(int hash, ValueEntry entry)
            {
                var e0 = Entry0;
                var e1 = Entry1;

                if (hash > e1.Hash)
                {
                    var newRight = Right.AddOrKeepEntry(hash, entry);
                    if (newRight == Right)
                        return this;
                    if (newRight is Branch2 && Right is Leaf5OrBranch3) 
                        return new Branch2(new Branch2(Left, e0, Middle), e1, newRight);
                    return new Branch3(Left, e0, Middle, e1, newRight);
                }

                if (hash < e0.Hash)
                {
                    var newLeft = Left.AddOrKeepEntry(hash, entry);
                    if (newLeft == Left)
                        return this;
                    if (newLeft is Branch2 && Left is Leaf5OrBranch3) 
                        return new Branch2(newLeft, e0, new Branch2(Middle, e1, Right));
                    return new Branch3(newLeft, e0, Middle, e1, Right);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    ImHashMap234<K, V> newMiddle;
                    if (Middle is Leaf5OrBranch3 x)
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
                    var newRight = Right.AddOrKeepEntry(hash, entry);
                    if (newRight == Right)
                        return this;
                    if (newRight is Branch2 && Right is Leaf5OrBranch3)
                    {
                        popEntry = e1;
                        popRight = newRight;
                        return new Branch2(Left, e0, Middle);
                    }
                    return new Branch3(Left, e0, Middle, e1, newRight);
                }

                if (hash < e0.Hash)
                {
                    var newLeft = Left.AddOrKeepEntry(hash, entry);
                    if (newLeft == Left)
                        return this;
                    if (newLeft is Branch2 && Left is Leaf5OrBranch3)
                    {
                        popEntry = e0;
                        popRight = new Branch2(Middle, e1, Right);
                        return newLeft;
                    }
                    return new Branch3(newLeft, e0, Middle, e1, Right);
                }

                if (hash > e0.Hash && hash < e1.Hash)
                {
                    ImHashMap234<K, V> newMiddle;
                    if (Middle is Leaf5OrBranch3 x)
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

    /// <summary>The base class for the tree leafs and branches, also defines the Empty tree</summary>
    public class ImMap234<V>
    {
        /// <summary>Empty tree to start with.</summary>
        public static readonly ImMap234<V> Empty = new ImMap234<V>();

        /// <summary>Hide the constructor to prevent the multiple Empty trees creation</summary>
        protected ImMap234() { }

        /// Pretty-prints
        public override string ToString() => "empty";

        /// <summary>Produces the new or updated map</summary>
        public virtual ImMap234<V> AddOrUpdateEntry(int key, Entry entry) => entry;

        /// <summary>As the empty cannot be a leaf - so no chance to call it</summary>
        protected virtual ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight) =>
            throw new NotSupportedException();

        /// <summary> Adds the value for the key or returns the non-modified map if the key is already present </summary>
        public virtual ImMap234<V> AddOrKeepEntry(int key, Entry entry) => entry;

        /// <summary>As the empty cannot be a leaf - so no chance to call it</summary>
        protected virtual ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight) =>
            throw new NotSupportedException();

        /// <summary>Lookup for the entry, if not found returns `null`. You can define other Lookup methods on top of it.</summary>
        public virtual Entry GetEntryOrDefault(int key) => null;

        /// <summary>Fold to fold</summary>
        public virtual S Fold<S>(S state, Func<Entry, S, S> reduce) => state;

        /// <summary>Enumerable</summary>
        public virtual IEnumerable<Entry> Enumerate() => Enumerable.Empty<Entry>();

        /// <summary>Wraps the stored data with "fixed" reference semantics - 
        /// when added to the tree it won't be changed or reconstructed in memory</summary>
        public sealed class Entry : ImMap234<V>
        {
            /// <summary>The Key is basically the hash</summary>
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
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry) =>
                key > Key ? new Leaf2(this, entry) :
                key < Key ? new Leaf2(entry, this) :
                (ImMap234<V>)entry;

            /// <summary>As the single entry cannot be a leaf - so no way to call it</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight) =>
                throw new NotSupportedException();

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry) =>
                key > Key ? new Leaf2(this, entry) :
                key < Key ? new Leaf2(entry, this) :
                (ImMap234<V>)this;

            /// <summary>As the empty cannot be a leaf - so no chance to call it</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight) =>
                throw new NotSupportedException();

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Key ? this : null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) => reduce(this, state);

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return this;
            }
        }

        /// <summary>2 leafs</summary>
        public sealed class Leaf2 : ImMap234<V>
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
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                var e1 = Entry1;
                var e0 = Entry0;
                return key > e1.Key ? new Leaf3(e0, e1, entry) :
                    key < e0.Key ? new Leaf3(entry, e0, e1) :
                    key > e0.Key && key < e1.Key ? new Leaf3(e0, entry, e1) :
                    key == e0.Key ? new Leaf2(entry, e1) :
                    (ImMap234<V>) new Leaf2(e0, entry);
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry1.Key ? new Leaf3(Entry0, Entry1, entry) :
                    key < Entry0.Key ? new Leaf3(entry, Entry0, Entry1) :
                    key > Entry0.Key && key < Entry1.Key ? new Leaf3(Entry0, entry, Entry1) :
                    key == Entry0.Key ? new Leaf2(entry, Entry1) :
                    (ImMap234<V>)new Leaf2(Entry0, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry) =>
                key > Entry1.Key ? new Leaf3(Entry0, Entry1, entry) :
                key < Entry0.Key ? new Leaf3(entry, Entry0, Entry1) :
                key > Entry0.Key && key < Entry1.Key ? new Leaf3(Entry0, entry, Entry1) :
                (ImMap234<V>)this;

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry1.Key ? new Leaf3(Entry0, Entry1, entry) :
                    key < Entry0.Key ? new Leaf3(entry, Entry0, Entry1) :
                    key > Entry0.Key && key < Entry1.Key ? new Leaf3(Entry0, entry, Entry1) :
                    (ImMap234<V>)this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Entry0.Key ? Entry0 :
                key == Entry1.Key ? Entry1 :
                null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                reduce(Entry1, reduce(Entry0, state));

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return Entry0;
                yield return Entry1;
            }
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf3 : ImMap234<V>
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
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry) =>
                key > Entry2.Key ? new Leaf4(Entry0, Entry1, Entry2, entry)
                : key < Entry0.Key ? new Leaf4(entry, Entry0, Entry1, Entry2)
                : key > Entry0.Key && key < Entry1.Key ? new Leaf4(Entry0, entry, Entry1, Entry2)
                : key > Entry1.Key && key < Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry2)
                : key == Entry0.Key ? new Leaf3(entry, Entry1, Entry2)
                : key == Entry1.Key ? new Leaf3(Entry0, entry, Entry2)
                : (ImMap234<V>)new Leaf3(Entry0, Entry1, entry);

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry2.Key ? new Leaf4(Entry0, Entry1, Entry2, entry)
                    : key < Entry0.Key ? new Leaf4(entry, Entry0, Entry1, Entry2)
                    : key > Entry0.Key && key < Entry1.Key ? new Leaf4(Entry0, entry, Entry1, Entry2)
                    : key > Entry1.Key && key < Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry2)
                    : key == Entry0.Key ? new Leaf3(entry, Entry1, Entry2)
                    : key == Entry1.Key ? new Leaf3(Entry0, entry, Entry2)
                    : (ImMap234<V>)new Leaf3(Entry0, Entry1, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry) =>
                key > Entry2.Key ? new Leaf4(Entry0, Entry1, Entry2, entry)
                : key < Entry0.Key ? new Leaf4(entry, Entry0, Entry1, Entry2)
                : key > Entry0.Key && key < Entry1.Key ? new Leaf4(Entry0, entry, Entry1, Entry2)
                : key > Entry1.Key && key < Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry2)
                : (ImMap234<V>)this;

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry2.Key ? new Leaf4(Entry0, Entry1, Entry2, entry)
                    : key < Entry0.Key ? new Leaf4(entry, Entry0, Entry1, Entry2)
                    : key > Entry0.Key && key < Entry1.Key ? new Leaf4(Entry0, entry, Entry1, Entry2)
                    : key > Entry1.Key && key < Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry2)
                    : (ImMap234<V>)this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Entry0.Key ? Entry0 :
                key == Entry1.Key ? Entry1 :
                key == Entry2.Key ? Entry2 :
                null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                reduce(Entry2, reduce(Entry1, reduce(Entry0, state)));

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return Entry0;
                yield return Entry1;
                yield return Entry2;
            }
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf4 : ImMap234<V>
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
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
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
                    : key == Entry1.Key ? new Leaf4(Entry0, entry, Entry2, Entry3)
                    : key == Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry3)
                    : new Leaf4(Entry0, Entry1, Entry2, entry);
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
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
                    : key == Entry1.Key ? new Leaf4(Entry0, entry, Entry2, Entry3)
                    : key == Entry2.Key ? new Leaf4(Entry0, Entry1, entry, Entry3)
                    : new Leaf4(Entry0, Entry1, Entry2, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry) =>
                key > Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, Entry3, entry)
                : key < Entry0.Key ? new Leaf5(entry, Entry0, Entry1, Entry2, Entry3)
                : key > Entry0.Key && key < Entry1.Key ? new Leaf5(Entry0, entry, Entry1, Entry2, Entry3)
                : key > Entry1.Key && key < Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry2, Entry3)
                : key > Entry2.Key && key < Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry3)
                : (ImMap234<V>)this;

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                return key > Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, Entry3, entry)
                    : key < Entry0.Key ? new Leaf5(entry, Entry0, Entry1, Entry2, Entry3)
                    : key > Entry0.Key && key < Entry1.Key ? new Leaf5(Entry0, entry, Entry1, Entry2, Entry3)
                    : key > Entry1.Key && key < Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry2, Entry3)
                    : key > Entry2.Key && key < Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry3)
                    : (ImMap234<V>)this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Entry0.Key ? Entry0 :
                key == Entry1.Key ? Entry1 :
                key == Entry2.Key ? Entry2 :
                key == Entry3.Key ? Entry3 :
                null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                reduce(Entry3, reduce(Entry2, reduce(Entry1, reduce(Entry0, state))));

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return Entry0;
                yield return Entry1;
                yield return Entry2;
                yield return Entry3;
            }
        }

        /// <summary>3 leafs</summary>
        public sealed class Leaf5 : ImMap234<V>
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
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                if (key > Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(Entry4, entry));

                if (key < Entry0.Key)
                    return new Branch2(new Leaf2(entry, Entry0), Entry1, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Branch2(new Leaf2(Entry0, entry), Entry1, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Branch2(new Leaf2(Entry0, Entry1), entry, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry2.Key && key < Entry3.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), entry, new Leaf2(Entry3, Entry4));

                if (key > Entry3.Key && key < Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(entry, Entry4));

                return key == Entry0.Key ? new Leaf5(entry, Entry1, Entry2, Entry3, Entry4)
                    :  key == Entry1.Key ? new Leaf5(Entry0, entry, Entry2, Entry3, Entry4)
                    :  key == Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry3, Entry4)
                    :  key == Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry4)
                    :                      new Leaf5(Entry0, Entry1, Entry2, Entry3, entry);
            }

            /// <summary>Produces the new or updated leaf or
            /// the split Branch2 nodes: returns the left branch, entry is changed to the Branch Entry0, popRight is the right branch</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                if (key > Entry4.Key)
                {
                    popRight = new Leaf2(Entry4, entry);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key < Entry0.Key)
                {
                    var left = new Leaf2(entry, Entry0);
                    entry = Entry1;
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return left;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    var left = new Leaf2(Entry0, entry);
                    entry = Entry1;
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return left;
                }

                if (key > Entry1.Key && key < Entry2.Key)
                {
                    // the entry is kept as-is
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return new Leaf2(Entry0, Entry1);
                }

                if (key > Entry2.Key && key < Entry3.Key)
                {
                    // the entry is kept as-is
                    popRight = new Leaf2(Entry3, Entry4);
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key > Entry3.Key && key < Entry4.Key)
                {
                    popRight = new Leaf2(entry, Entry4);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                popRight = null;
                return key == Entry0.Key ? new Leaf5(entry, Entry1, Entry2, Entry3, Entry4)
                    :  key == Entry1.Key ? new Leaf5(Entry0, entry, Entry2, Entry3, Entry4)
                    :  key == Entry2.Key ? new Leaf5(Entry0, Entry1, entry, Entry3, Entry4)
                    :  key == Entry3.Key ? new Leaf5(Entry0, Entry1, Entry2, entry, Entry4)
                    :                      new Leaf5(Entry0, Entry1, Entry2, Entry3, entry);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry)
            {
                if (key > Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(Entry4, entry));

                if (key < Entry0.Key)
                    return new Branch2(new Leaf2(entry, Entry0), Entry1, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry0.Key && key < Entry1.Key)
                    return new Branch2(new Leaf2(Entry0, entry), Entry1, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry1.Key && key < Entry2.Key)
                    return new Branch2(new Leaf2(Entry0, Entry1), entry, new Leaf3(Entry2, Entry3, Entry4));

                if (key > Entry2.Key && key < Entry3.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), entry, new Leaf2(Entry3, Entry4));

                if (key > Entry3.Key && key < Entry4.Key)
                    return new Branch2(new Leaf3(Entry0, Entry1, Entry2), Entry3, new Leaf2(entry, Entry4));

                return this;
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                if (key > Entry4.Key)
                {
                    popRight = new Leaf2(Entry4, entry);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key < Entry0.Key)
                {
                    var left = new Leaf2(entry, Entry0);
                    entry = Entry1;
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return left;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    var left = new Leaf2(Entry0, entry);
                    entry = Entry1;
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return left;
                }

                if (key > Entry1.Key && key < Entry2.Key)
                {
                    popRight = new Leaf3(Entry2, Entry3, Entry4);
                    return new Leaf2(Entry0, Entry1);
                }

                if (key > Entry2.Key && key < Entry3.Key)
                {
                    popRight = new Leaf2(Entry3, Entry4);
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                if (key > Entry3.Key && key < Entry4.Key)
                {
                    popRight = new Leaf2(entry, Entry4);
                    entry = Entry3;
                    return new Leaf3(Entry0, Entry1, Entry2);
                }

                popRight = null; 
                return this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key == Entry0.Key ? Entry0 :
                key == Entry1.Key ? Entry1 :
                key == Entry2.Key ? Entry2 :
                key == Entry3.Key ? Entry3 :
                key == Entry4.Key ? Entry4 :
                null;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                reduce(Entry4, reduce(Entry3, reduce(Entry2, reduce(Entry1, reduce(Entry0, state)))));

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                yield return Entry0;
                yield return Entry1;
                yield return Entry2;
                yield return Entry3;
                yield return Entry4;
            }
        }

        /// <summary>2 branches - it is never split itself, but may produce Branch3 if the lower branches are split</summary>
        public sealed class Branch2 : ImMap234<V>
        {
            /// <summary>The only entry</summary>
            public readonly Entry Entry0;

            /// <summary>Left branch</summary>
            public readonly ImMap234<V> Left;

            /// <summary>Right branch</summary>
            public readonly ImMap234<V> Right;

            /// <summary>Constructs</summary>
            public Branch2(ImMap234<V> left, Entry entry0, ImMap234<V> right)
            {
                Left = left;
                Entry0 = entry0;
                Right = right;
            }

            /// Pretty-print
            public override string ToString() =>
                (Left is Branch2 ? Left.GetType().Name : Left.ToString()) +
                " <- " + Entry0 + " -> " +
                (Right is Branch2 ? Right.GetType().Name : Right.ToString());

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                if (key > Entry0.Key)
                {
                    var newBranch = Right.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch3(Left, Entry0, newBranch, entry, popRight);
                    return new Branch2(Left, Entry0, newBranch);
                }

                if (key < Entry0.Key)
                {
                    var newBranch = Left.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch3(newBranch, entry, popRight, Entry0, Right);
                    return new Branch2(newBranch, Entry0, Right);
                }

                // update
                return new Branch2(Left, entry, Right);
            }

            /// <summary>Produces the new or updated branch</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                if (key > Entry0.Key)
                {
                    var newBranch = Right.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                        return new Branch3(Left, Entry0, newBranch, entry, popRightBelow);
                    return new Branch2(Left, Entry0, newBranch);
                }

                if (key < Entry0.Key)
                {
                    var newBranch = Left.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                        return new Branch3(newBranch, entry, popRightBelow, Entry0, Right);
                    return new Branch2(newBranch, Entry0, Right);
                }

                return new Branch2(Left, entry, Right);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry)
            {
                if (key > Entry0.Key)
                {
                    var newBranch = Right.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    return popRight != null ? new Branch3(Left, Entry0, newBranch, entry, popRight) 
                        : newBranch != Right ? new Branch2(Left, Entry0, newBranch)
                        : (ImMap234<V>)this;
                }

                if (key < Entry0.Key)
                {
                    var newBranch = Left.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    return popRight != null ? new Branch3(newBranch, entry, popRight, Entry0, Right)
                        : newBranch != Left ? new Branch2(newBranch, Entry0, Right) 
                        : (ImMap234<V>)this;
                }

                return this;
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                if (key > Entry0.Key)
                {
                    var newBranch = Right.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                        return new Branch3(Left, Entry0, newBranch, entry, popRightBelow);
                    if (newBranch != Right)
                        return new Branch2(Left, Entry0, newBranch);
                    return this;
                }

                if (key < Entry0.Key)
                {
                    var newBranch = Left.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                        return new Branch3(newBranch, entry, popRightBelow, Entry0, Right);
                    if (newBranch != Left)
                        return new Branch2(newBranch, Entry0, Right);
                    return this;
                }

                return this;
            }

            // todo: @perf how to get rid of nested GetEntryOrDefault call if branches are leafs
            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key > Entry0.Key ? Right.GetEntryOrDefault(key) :
                key < Entry0.Key ? Left .GetEntryOrDefault(key) :
                Entry0;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                Right.Fold(reduce(Entry0, Left.Fold(state, reduce)), reduce);

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                foreach (var l in Left.Enumerate())
                    yield return l;
                yield return Entry0;
                foreach (var r in Right.Enumerate())
                    yield return r;
            }
        }

        /// <summary>3 branches</summary>
        public sealed class Branch3 : ImMap234<V>
        {
            /// <summary>Left branch</summary>
            public readonly ImMap234<V> Left;

            /// <summary>The only entry</summary>
            public readonly Entry Entry0;

            /// <summary>Right branch</summary>
            public readonly ImMap234<V> Middle;

            /// <summary>Right entry</summary>
            public readonly Entry Entry1;

            /// <summary>Rightmost branch</summary>
            public readonly ImMap234<V> Right;

            /// <summary>Constructs</summary>
            public Branch3(ImMap234<V> left, Entry entry0, ImMap234<V> middle, Entry entry1, ImMap234<V> right)
            {
                Left = left;
                Entry0 = entry0;
                Middle = middle;
                Entry1 = entry1;
                Right = right;
            }

            /// Pretty-print
            public override string ToString() =>
                (Left is Branch2 ? Left.GetType().Name : Left.ToString()) +
                " <- " + Entry0 + " -> " +
                (Middle is Branch2 ? Middle.GetType().Name : Middle.ToString()) +
                " <- " + Entry1 + " -> " +
                (Right is Branch2 ? Right.GetType().Name.TrimEnd('<', '>', '`', 'V') : Right.ToString());

            /// <summary>Produces the new or updated map</summary>
            public override ImMap234<V> AddOrUpdateEntry(int key, Entry entry)
            {
                ImMap234<V> newBranch;
                if (key > Entry1.Key)
                {
                    newBranch = Right.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(Left, Entry0, Middle), Entry1, new Branch2(newBranch, entry, popRight));
                    return new Branch3(Left, Entry0, Middle, Entry1, newBranch);
                }

                if (key < Entry0.Key)
                {
                    newBranch = Left.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(newBranch, entry, popRight), Entry0, new Branch2(Middle, Entry1, Right));
                    return new Branch3(newBranch, Entry0, Middle, Entry1, Right);
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    newBranch = Middle.AddOrUpdateOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(Left, Entry0, newBranch), entry, new Branch2(popRight, Entry1, Right));
                    return new Branch3(Left, Entry0, newBranch, Entry1, Right);
                }

                // update
                return key == Entry0.Key
                    ? new Branch3(Left, entry, Middle, Entry1, Right)
                    : new Branch3(Left, Entry0, Middle, entry, Right);
            }

            /// <summary>Produces the new or updated leaf or
            /// the split Branch2 nodes: returns the left branch, entry is changed to the Branch Entry0, popRight is the right branch</summary>
            protected override ImMap234<V> AddOrUpdateOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                ImMap234<V> newBranch;
                if (key > Entry1.Key)
                {
                    // having:
                    //                                             [5]
                    //        [2,5]                =>      [2]               [9]
                    // [0,1]  [3,4]  [6,7,8,9,10]    [0,1]    [3,4]   [6,7,8]   [10,11]
                    // and adding 11
                    newBranch = Right.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new Branch2(newBranch, entry, popRightBelow);
                        entry = Entry1;
                        return new Branch2(Left, Entry0, Middle);
                    }
                    return new Branch3(Left, Entry0, Middle, Entry1, newBranch);
                }

                if (key < Entry0.Key)
                {
                    newBranch = Left.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        newBranch = new Branch2(newBranch, entry, popRightBelow);
                        entry = Entry0;
                        popRight = new Branch2(Middle, Entry1, Right);
                        return newBranch;
                    }
                    return new Branch3(newBranch, Entry0, Middle, Entry1, Right);
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    //                              [4]
                    //       [2, 7]            [2]         [7]
                    // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                    // and adding 6
                    newBranch = Middle.AddOrUpdateOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new Branch2(popRightBelow, Entry1, Right);
                        return new Branch2(Left, Entry0, newBranch);
                    }
                    return new Branch3(Left, Entry0, newBranch, Entry1, Right);
                }

                // update
                return key == Entry0.Key
                    ? new Branch3(Left, entry, Middle, Entry1, Right)
                    : new Branch3(Left, Entry0, Middle, entry, Right);
            }

            /// <inheritdoc />
            public override ImMap234<V> AddOrKeepEntry(int key, Entry entry)
            {
                ImMap234<V> newBranch;
                if (key > Entry1.Key)
                {
                    newBranch = Right.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(Left, Entry0, Middle), Entry1, new Branch2(newBranch, entry, popRight));
                    return newBranch != Right ? new Branch3(Left, Entry0, Middle, Entry1, newBranch) : (ImMap234<V>)this;
                }

                if (key < Entry0.Key)
                {
                    newBranch = Left.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(newBranch, entry, popRight), Entry0, new Branch2(Middle, Entry1, Right));
                    return newBranch != Left ? new Branch3(newBranch, Entry0, Middle, Entry1, Right) : this;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    newBranch = Middle.AddOrKeepOrSplitEntry(key, ref entry, out var popRight);
                    if (popRight != null)
                        return new Branch2(new Branch2(Left, Entry0, newBranch), entry, new Branch2(popRight, Entry1, Right));
                    return newBranch != Middle ? new Branch3(Left, Entry0, newBranch, Entry1, Right) : this;
                }

                return this;
            }

            /// <summary>Produces the new or updated leaf</summary>
            protected override ImMap234<V> AddOrKeepOrSplitEntry(int key, ref Entry entry, out ImMap234<V> popRight)
            {
                popRight = null;
                ImMap234<V> newBranch;
                if (key > Entry1.Key)
                {
                    // for example:
                    //                                             [5]
                    //        [2,5]                =>      [2]               [9]
                    // [0,1]  [3,4]  [6,7,8,9,10]    [0,1]    [3,4]   [6,7,8]   [10,11]
                    // and adding 11
                    newBranch = Right.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new Branch2(newBranch, entry, popRightBelow);
                        entry = Entry1;
                        return new Branch2(Left, Entry0, Middle);
                    }
                    return newBranch != Right ? new Branch3(Left, Entry0, Middle, Entry1, newBranch) : this;
                }

                if (key < Entry0.Key)
                {
                    newBranch = Left.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        var left = new Branch2(newBranch, entry, popRightBelow);
                        entry = Entry0;
                        popRight = new Branch2(Middle, Entry1, Right);
                        return left;
                    }
                    return newBranch != Left ? new Branch3(newBranch, Entry0, Middle, Entry1, Right) : this;
                }

                if (key > Entry0.Key && key < Entry1.Key)
                {
                    //                              [4]
                    //       [2, 7]            [2]         [7]
                    // [1]  [3,4,5]  [8] => [1]  [3]  [5,6]    [8]
                    // and adding 6
                    newBranch = Middle.AddOrKeepOrSplitEntry(key, ref entry, out var popRightBelow);
                    if (popRightBelow != null)
                    {
                        popRight = new Branch2(popRightBelow, Entry1, Right);
                        return new Branch2(Left, Entry0, newBranch);
                    }
                    return newBranch != Middle ? new Branch3(Left, Entry0, newBranch, Entry1, Right) : this;
                }

                return this;
            }

            /// <inheritdoc />
            public override Entry GetEntryOrDefault(int key) =>
                key > Entry1.Key ? Right.GetEntryOrDefault(key) :
                key < Entry0.Key ? Left .GetEntryOrDefault(key) :
                key > Entry0.Key && key < Entry1.Key ? Middle.GetEntryOrDefault(key) :
                key == Entry0.Key ? Entry0 : Entry1;

            /// <inheritdoc />
            public override S Fold<S>(S state, Func<Entry, S, S> reduce) =>
                Right.Fold(reduce(Entry1, Middle.Fold(reduce(Entry0, Left.Fold(state, reduce)), reduce)), reduce);

            /// <inheritdoc />
            public override IEnumerable<Entry> Enumerate()
            {
                foreach (var l in Left.Enumerate())
                    yield return l;
                yield return Entry0;
                foreach (var m in Middle.Enumerate())
                    yield return m;
                yield return Entry1;
                foreach (var r in Right.Enumerate())
                    yield return r;
            }
        }
    }

    /// <summary>ImMap methods</summary>
    public static class ImMap234
    {
        /// <summary>Adds or updates the value by key in the map, always returns a modified map.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> AddOrUpdate<V>(this ImMap234<V> map, int key, V value) =>
            map == ImMap234<V>.Empty
                ? new ImMap234<V>.Entry(key, value)
                : map.AddOrUpdateEntry(key, new ImMap234<V>.Entry(key, value));

        /// <summary>Adds the entry or keeps the map intact.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> AddOrKeep<V>(this ImMap234<V> map, int key, V value) =>
            map == ImMap234<V>.Empty
                ? new ImMap234<V>.Entry(key, value)
                : map.AddOrKeepEntry(key, new ImMap234<V>.Entry(key, value));

        /// <summary>Adds the entry or keeps the map intact.</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V> AddOrKeep<V>(this ImMap234<V> map, ImMap234<V>.Entry entry) => 
            map.AddOrKeepEntry(entry.Key, entry);

        /// <summary>Lookup</summary>
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<V>(this ImMap234<V> map, int key)
        {
            var entry = map.GetEntryOrDefault(key);
            return entry != null ? entry.Value : default(V);
        }

        /// <summary>Lookup</summary>
        [MethodImpl((MethodImplOptions) 256)]
        public static bool TryFind<V>(this ImMap234<V> map, int key, out V value)
        {
            var entry = map.GetEntryOrDefault(key);
            if (entry != null)
            {
                value = entry.Value;
                return true;
            }

            value = default(V);
            return false;
        }

        /// Default number of slots
        public const int SLOT_COUNT_POWER_OF_TWO = 32;

        /// The default mask to partition the key to the target slot
        public const int KEY_MASK_TO_FIND_SLOT = SLOT_COUNT_POWER_OF_TWO - 1;

        /// Creates the array with the empty slots
        [MethodImpl((MethodImplOptions)256)]
        public static ImMap234<V>[] CreateWithEmpty<V>(int slotCountPowerOfTwo = SLOT_COUNT_POWER_OF_TWO)
        {
            var slots = new ImMap234<V>[slotCountPowerOfTwo];
            for (var i = 0; i < slots.Length; ++i)
                slots[i] = ImMap234<V>.Empty;
            return slots;
        }

        /// Returns a new tree with added or updated value for specified key.
        [MethodImpl((MethodImplOptions)256)]
        public static void AddOrUpdate<V>(this ImMap234<V>[] slots, int key, V value, int keyMaskToFindSlot = KEY_MASK_TO_FIND_SLOT)
        {
            ref var slot = ref slots[key & keyMaskToFindSlot];
            var copy = slot;
            if (Interlocked.CompareExchange(ref slot, copy.AddOrUpdate(key, value), copy) != copy)
                RefAddOrUpdateSlot(ref slot, key, value);
        }

        /// Update the ref to the slot with the new version - retry if the someone changed the slot in between
        public static void RefAddOrUpdateSlot<V>(ref ImMap234<V> slot, int key, V value) =>
            Ref.Swap(ref slot, key, value, (x, k, v) => x.AddOrUpdate(k, v));
    }
}