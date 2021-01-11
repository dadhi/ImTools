using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class OldImMap234Tests
    {
        [Test]
        public void Adding_keys_from_1_to_10_and_checking_the_tree_shape_on_each_addition()
        {
            var m = ImMap234<int>.Empty;
            
            Assert.AreEqual(default(int), m.GetValueOrDefault(0));
            Assert.AreEqual(default(int), m.GetValueOrDefault(13));

            m = m.AddOrUpdate(1, 1);
            Assert.IsInstanceOf<ImMap234<int>.Entry>(m);
            Assert.AreEqual(1, m.GetValueOrDefault(1));

            m = m.AddOrUpdate(2, 2);
            Assert.IsInstanceOf<ImMap234<int>.Leaf2>(m);
            Assert.AreEqual(2, m.GetValueOrDefault(2));

            m = m.AddOrUpdate(3, 3);
            Assert.IsInstanceOf<ImMap234<int>.Leaf3>(m);
            Assert.AreEqual(3, m.GetValueOrDefault(3));

            m = m.AddOrUpdate(4, 4);
            Assert.IsInstanceOf<ImMap234<int>.Leaf4>(m);
            Assert.AreEqual(4, m.GetValueOrDefault(4));
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.AreEqual(2, m.GetValueOrDefault(2));
            Assert.AreEqual(1, m.GetValueOrDefault(1));

            m = m.AddOrUpdate(5, 5);
            Assert.IsInstanceOf<ImMap234<int>.Leaf5>(m);
            Assert.AreEqual(5, m.GetValueOrDefault(5));

            m = m.AddOrUpdate(6, 6);
            Assert.IsInstanceOf<ImMap234<int>.Branch2>(m);
            Assert.IsInstanceOf<ImMap234<int>.Leaf3>(((ImMap234<int>.Branch2)m).Left);
            Assert.IsInstanceOf<ImMap234<int>.Leaf2>(((ImMap234<int>.Branch2)m).Right);
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.AreEqual(5, m.GetValueOrDefault(5));
            Assert.AreEqual(6, m.GetValueOrDefault(6));

            CollectionAssert.AreEqual(Enumerable.Range(1, 6), m.Enumerate().Select(x => x.Value));

            m = m.AddOrUpdate(7, 7);
            Assert.IsInstanceOf<ImMap234<int>.Branch2>(m);
            Assert.AreEqual(7, m.GetValueOrDefault(7));

            m = m.AddOrUpdate(8, 8);
            Assert.IsInstanceOf<ImMap234<int>.Branch2>(m);
            Assert.AreEqual(8, m.GetValueOrDefault(8));

            m = m.AddOrUpdate(9, 9);
            Assert.AreEqual(9, m.GetValueOrDefault(9));

            m = m.AddOrUpdate(10, 10);
            Assert.AreEqual(10, m.GetValueOrDefault(10));

            CollectionAssert.AreEqual(Enumerable.Range(1, 10), m.Enumerate().Select(x => x.Value));
        }

        [Test]
        public void Adding_1000_keys_and_randomly_checking()
        {
            var m = ImMap234<int>.Empty;
            for (var i = 0; i < 1000; i++)
            {
                m = m.AddOrUpdate(i, i);
            }

            Assert.AreEqual(1, m.GetValueOrDefault(1));
            Assert.AreEqual(0, m.GetValueOrDefault(0));
            Assert.AreEqual(13, m.GetValueOrDefault(13));
            Assert.AreEqual(66, m.GetValueOrDefault(66));
            Assert.AreEqual(555, m.GetValueOrDefault(555));
            Assert.AreEqual(333, m.GetValueOrDefault(333));
            Assert.AreEqual(999, m.GetValueOrDefault(999));

            // non-existing keys 
            Assert.AreEqual(0, m.GetValueOrDefault(1000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void Adding_1000_keys_descending_and_randomly_checking()
        {
            var m = ImMap234<int>.Empty;
            for (var i = 1000 - 1; i >= 0; i--)
            {
                m = m.AddOrUpdate(i, i);
            }

            Assert.AreEqual(1, m.GetValueOrDefault(1));
            Assert.AreEqual(0, m.GetValueOrDefault(0));
            Assert.AreEqual(13, m.GetValueOrDefault(13));
            Assert.AreEqual(66, m.GetValueOrDefault(66));
            Assert.AreEqual(555, m.GetValueOrDefault(555));
            Assert.AreEqual(333, m.GetValueOrDefault(333));
            Assert.AreEqual(999, m.GetValueOrDefault(999));

            // non-existing keys 
            Assert.AreEqual(0, m.GetValueOrDefault(1000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void AddOrUpdate_1000_keys_randomly_and_randomly_checking()
        {
            var rnd = new Random();

            var m = ImMap234<int>.Empty;
            for (var i = 0; i < 1000; i++)
            {
                var n = rnd.Next(0, 10000);
                m = m.AddOrUpdate(n, n);
                Assert.AreEqual(n, m.GetValueOrDefault(n));
            }

            // non-existing keys 
            Assert.AreEqual(0, m.GetValueOrDefault(10000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void AddOrKeep_1000_keys_randomly_and_randomly_checking()
        {
            var rnd = new Random();

            var m = ImMap234<int>.Empty;
            for (var i = 0; i < 1000; i++)
            {
                var n = rnd.Next(0, 10000);
                m = m.AddOrKeep(n, n);
                Assert.AreEqual(n, m.GetValueOrDefault(n));
            }

            // non-existing keys 
            Assert.AreEqual(0, m.GetValueOrDefault(10000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }
    }

    //======

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