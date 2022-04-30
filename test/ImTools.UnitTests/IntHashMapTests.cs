using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ImToolsV3;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class IntHashMapTests
    {
        [Test]
        public void Can_store_and_retrieve_value_from_map()
        {
            var map = new IntHashMap<string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42 + 32, "2");

            // interrupt the keys with ne key
            map.AddOrUpdate(43, "a");
            map.AddOrUpdate(43 + 32, "b");

            map.AddOrUpdate(42 + 32 + 32, "3");

            Assert.AreEqual("1", map.GetValueOrDefault(42));
            Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
            Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
            Assert.AreEqual(null, map.GetValueOrDefault(42 + 32 + 32 + 32));

            Assert.AreEqual("a", map.GetValueOrDefault(43));
        }

        [Test]
        public void Can_store_and_get_stored_item_count()
        {
            var map = new IntHashMap<string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42 + 32 + 32, "3");

            Assert.AreEqual(2, map.Count);
        }

        [Test]
        public void Can_update_a_stored_item_with_new_value()
        {
            var map = new IntHashMap<string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42, "3");

            Assert.AreEqual("3", map.GetValueOrDefault(42));
            Assert.AreEqual(1, map.Count);
        }

        [Test]
        public void Can_remove_the_stored_item()
        {
            var map = new IntHashMap<string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42 + 32, "2");
            map.AddOrUpdate(42 + 32 + 32, "3");

            map.Remove(42 + 32);

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        }

        [Test]
        public void Can_remove_the_stored_item_twice()
        {
            var map = new IntHashMap<string>(2);

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(41, "41");
            map.AddOrUpdate(42 + 32, "2");
            map.AddOrUpdate(43, "43");
            map.AddOrUpdate(42 + 32 + 32, "3");

            // removing twice
            map.Remove(42 + 32);
            map.Remove(42 + 32);

            Assert.AreEqual(4, map.Count);
            Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        }

        [Test]
        public void Can_add_key_with_0_hash_code()
        {
            var map = new IntHashMap<string>();

            map.AddOrUpdate(0, "aaa");
            map.AddOrUpdate(0 + 32, "2");
            map.AddOrUpdate(0 + 32 + 32, "3");

            string value;
            Assert.IsTrue(map.TryFind(0, out value));

            Assert.AreEqual("aaa", value);
        }

        [Test]
        public void Can_quickly_find_the_scattered_items_with_the_same_cache()
        {
            var map = new IntHashMap<string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(43, "a");
            map.AddOrUpdate(42 + 32, "2");
            map.AddOrUpdate(45, "b");
            map.AddOrUpdate(46, "c");
            map.AddOrUpdate(42 + 32 + 32, "3");

            string value;
            Assert.IsTrue(map.TryFind(42 + 32, out value));
            Assert.AreEqual("2", value);

            Assert.IsTrue(map.TryFind(42 + 32 + 32, out value));
            Assert.AreEqual("3", value);
        }

        [Test]
        public void Tree_should_support_arbitrary_keys_by_using_their_hash_code()
        {
            var tree = ImToolsV3.ImHashMap<Type, string>.Empty;

            var key = typeof(IntHashMapTests);
            var value = "test";

            tree = tree.AddOrUpdate(key, value);

            var result = tree.GetValueOrDefault(key);
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void Tree_should_support_arbitrary_keys_by_using_their_hash_code_TryFind()
        {
            var tree = ImToolsV3.ImHashMap<Type, string>.Empty;

            var key = typeof(IntHashMapTests);
            var value = "test";

            tree = tree.AddOrUpdate(key, value);

            string result;
            Assert.IsTrue(tree.TryFind(key, out result));
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void When_adding_value_with_hash_conflicted_key_Then_I_should_be_able_to_get_it_back()
        {
            var key1 = new HashConflictingKey<string>("a", 1);
            var key2 = new HashConflictingKey<string>("b", 1);
            var key3 = new HashConflictingKey<string>("c", 1);
            var tree = ImHashMap<HashConflictingKey<string>, int>.Empty
                .AddOrUpdate(key1, 1)
                .AddOrUpdate(key2, 2)
                .AddOrUpdate(key3, 3);

            var value = tree.GetValueOrDefault(key3);

            Assert.That(value, Is.EqualTo(3));
        }

        [Test]
        public void When_adding_value_with_hash_conflicted_key_Then_I_should_be_able_to_get_it_back_TryFind()
        {
            var key1 = new HashConflictingKey<string>("a", 1);
            var key2 = new HashConflictingKey<string>("b", 1);
            var key3 = new HashConflictingKey<string>("c", 1);
            var tree = ImHashMap<HashConflictingKey<string>, int>.Empty
                .AddOrUpdate(key1, 1)
                .AddOrUpdate(key2, 2)
                .AddOrUpdate(key3, 3);

            int value;
            Assert.IsTrue(tree.TryFind(key3, out value));
            Assert.That(value, Is.EqualTo(3));
        }

        [Test]
        public void When_adding_couple_of_values_with_hash_conflicted_key_Then_I_should_be_able_to_get_them_back()
        {
            var key1 = new HashConflictingKey<string>("a", 1);
            var key2 = new HashConflictingKey<string>("b", 1);
            var key3 = new HashConflictingKey<string>("c", 1);
            var tree = ImHashMap<HashConflictingKey<string>, int>.Empty
                .AddOrUpdate(key1, 1)
                .AddOrUpdate(key2, 2)
                .AddOrUpdate(key3, 3);

            var values = tree.Enumerate().ToDictionary(kv => kv.Key.Key, kv => kv.Value);

            Assert.That(values, Is.EqualTo(new Dictionary<string, int>
            {
                {"a", 1},
                {"b", 2},
                {"c", 3},
            }));
        }

        [Test]
        public void Can_fold_values_with_hash_conflicted_key()
        {
            var key1 = new HashConflictingKey<string>("a", 1);
            var key2 = new HashConflictingKey<string>("b", 1);
            var key3 = new HashConflictingKey<string>("c", 1);
            var tree = ImHashMap<HashConflictingKey<string>, int>.Empty
                .AddOrUpdate(key1, 1)
                .AddOrUpdate(key2, 2)
                .AddOrUpdate(key3, 3);

            var values = tree.ForEach(new Dictionary<string, int>(), (data, _, dict) => dict.Do(data, (x, d) => x.Add(d.Key.Key, d.Value)));

            Assert.That(values, Is.EqualTo(new Dictionary<string, int>
            {
                {"a", 1},
                {"b", 2},
                {"c", 3},
            }));
        }

        [Test]
        public void Test_that_all_added_values_are_accessible()
        {
            var t = ImHashMap<int, int>.Empty
                .AddOrUpdate(1, 11)
                .AddOrUpdate(2, 22)
                .AddOrUpdate(3, 33);

            Assert.AreEqual(11, t.GetValueOrDefault(1));
            Assert.AreEqual(22, t.GetValueOrDefault(2));
            Assert.AreEqual(33, t.GetValueOrDefault(3));
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw()
        {
            var tree = ImToolsV3.ImHashMap<int, int>.Empty;

            Assert.DoesNotThrow(
                () => tree.GetValueOrDefault(0));
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw_TryFind()
        {
            var tree = ImToolsV3.ImHashMap<int, int>.Empty;

            int result;
            Assert.DoesNotThrow(
                () => tree.TryFind(0, out result));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw()
        {
            var tree = ImHashMap<int, int>.Empty
                .AddOrUpdate(1, 1)
                .AddOrUpdate(3, 2);

            Assert.DoesNotThrow(
                () => tree.GetValueOrDefault(2));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw_TryFind()
        {
            var tree = ImHashMap<int, int>.Empty
                .AddOrUpdate(1, 1)
                .AddOrUpdate(3, 2);

            int result;
            Assert.DoesNotThrow(
                () => tree.TryFind(2, out result));
        }

        [Test]
        public void Enumerated_values_should_be_returned_in_sorted_order()
        {
            var items = Enumerable.Range(0, 10).ToArray();
            var tree = items.Aggregate(ImToolsV3.ImHashMap<int, int>.Empty, (t, i) => t.AddOrUpdate(i, i));

            var enumerated = tree.Enumerate().Select(t => t.Value).ToArray();

            CollectionAssert.AreEqual(items, enumerated);
        }

        [Test]
        public void Folded_values_should_be_returned_in_sorted_order()
        {
            var list = Enumerable.Range(0, 10).ToImList();
            var tree = list.Fold(ImToolsV3.ImHashMap<int, int>.Empty, (i, t) => t.AddOrUpdate(i, i));

            var enumerated = tree.ForEach(new List<int>(), (data, _, l) => l.Do(data, (x, d) => x.Add(d.Value)));

            CollectionAssert.AreEqual(list.ToArray(), enumerated);
        }

        [Test]
        public void Folded_values_should_be_returned_in_sorted_order_with_index()
        {
            var list = Enumerable.Range(0, 10).ToImList();
            var tree = list.Fold(ImToolsV3.ImHashMap<int, int>.Empty, (i, t) => t.AddOrUpdate(i, i));

            var enumerated = tree.ForEach(new List<int>(), (data, index, l) => l.Do(index, (x, i) => x.Add(i)));

            CollectionAssert.AreEqual(list.ToArray(), enumerated);
        }

        [Test]
        public void Update_to_null_and_then_to_value_should_remove_null()
        {
            var map = ImHashMap<int, string>.Empty
                .AddOrUpdate(1, "a")
                .AddOrUpdate(2, "b")
                .AddOrUpdate(3, "c")
                .AddOrUpdate(4, "d");
            
            Assert.AreEqual("d", map.GetValueOrDefault(4));

            map = map.Update(4, null);
            Assert.AreEqual(null, map.GetValueOrDefault(4));

            map = map.Update(4, "X");
            CollectionAssert.AreEqual(new[] {"a", "b", "c", "X"}, map.Enumerate().Select(_ => _.Value));
        }

        [Test]
        public void Update_of_not_found_key_should_return_the_same_tree()
        {
            var tree = ImHashMap<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(2, "b").AddOrUpdate(3, "c").AddOrUpdate(4, "d");

            var updatedTree = tree.Update(5, "e");

            Assert.AreSame(tree, updatedTree);
        }

        [Test]
        public void Remove_from_one_node_tree()
        {
            var tree = ImToolsV3.ImHashMap<int, string>.Empty.AddOrUpdate(0, "a");

            tree = tree.Remove(0);

            Assert.That(tree.IsEmpty, Is.True);
        }

        [Test]
        public void Remove_from_Empty_tree_should_not_throw()
        {
            var tree = ImToolsV3.ImHashMap<int, string>.Empty.Remove(0);
            Assert.That(tree.IsEmpty, Is.True);
        }

        internal class HashConflictingKey<T>
        {
            public readonly T Key;
            private readonly int _hash;

            public HashConflictingKey(T key, int hash)
            {
                Key = key;
                _hash = hash;
            }

            public override int GetHashCode() => _hash;

            public override bool Equals(object obj) => 
                obj is HashConflictingKey<T> other && Equals(Key, other.Key);
        }
    }
}
