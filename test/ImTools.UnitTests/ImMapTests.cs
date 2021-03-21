using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class ImMapTests
    {
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
            var tree = ImHashMap<int, int>.Empty;

            Assert.AreEqual(0, tree.GetValueOrDefault(0));
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw_TryFind()
        {
            var tree = ImHashMap<int, int>.Empty;

            Assert.IsFalse(tree.TryFind(0, out _));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw()
        {
            var tree = ImHashMap<int, int>.Empty
                .AddOrUpdate(1, 1)
                .AddOrUpdate(3, 2);

            Assert.AreEqual(0, tree.GetValueOrDefault(2));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw_TryFind()
        {
            var tree = ImHashMap<int, int>.Empty
                .AddOrUpdate(1, 1)
                .AddOrUpdate(3, 2);

            Assert.IsFalse(tree.TryFind(2, out _));
        }

        [Test]
        public void Enumerated_values_should_be_returned_in_sorted_order()
        {
            var items = Enumerable.Range(0, 10).ToArray();
            var tree = items.Aggregate(ImHashMap<int, int>.Empty, (t, i) => t.AddOrUpdate(i, i));

            var enumerated = tree.Enumerate().Select(t => t.Value).ToArray();

            CollectionAssert.AreEqual(items, enumerated);
        }

        [Test]
        public void Can_fold_2_level_tree()
        {
            var t = ImHashMap<int, int>.Empty;
            t= t.AddOrUpdate(1, 1).AddOrUpdate(2, 2);

            var list = t.ForEach(new List<int>(), (data, _, l) => l.Add(data.Value));

            CollectionAssert.AreEqual(new[] { 1, 2 }, list);
        }

        [Test]
        public void Can_fold_3_level_tree()
        {
            var t = ImHashMap<int, int>.Empty;
            t = t
                .AddOrUpdate(1, 1)
                .AddOrUpdate(2, 2)
                .AddOrUpdate(3, 3)
                .AddOrUpdate(4, 4);

            var list = t.ForEach(new List<int>(), (data, _, l) => l.Add(data.Value));

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list);
        }

        [Test]
        public void Folded_values_should_be_returned_in_sorted_order()
        {
            var items = Enumerable.Range(0, 10).ToArray();
            var tree = items.Aggregate(ImHashMap<int, int>.Empty, (t, i) => t.AddOrUpdate(i, i));

            var list = tree.ForEach(new List<int>(), (data, _, l) => l.Add(data.Value));

            CollectionAssert.AreEqual(items, list);
        }

        [Test]
        public void Folded_lefty_values_should_be_returned_in_sorted_order()
        {
            var items = Enumerable.Range(0, 100).ToArray();
            var tree = items.Reverse().Aggregate(ImHashMap<int, int>.Empty, (t, i) => t.AddOrUpdate(i, i));

            var list = tree.ForEach(new List<int>(), (data, _, l) => l.Add(data.Value));

            CollectionAssert.AreEqual(items, list);
        }

        [Test]
        public void ImMapSlots_Folded_values_should_be_returned_in_sorted_order()
        {
            var items = Enumerable.Range(0, 10).ToArray();
            var tree = items.Aggregate(PartitionedMap.CreateEmpty<int>(), (t, i) => t.Do(x => x.AddOrUpdate(i, i)));

            var list = tree.ForEach(new List<int>(), (data, _, l) => l.Add(data.Value));

            CollectionAssert.AreEqual(items, list);
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
            Assert.IsNull(map.GetValueOrDefault(4));

            map = map.Update(4, "X");
            CollectionAssert.AreEqual(new[] {"a", "b", "c", "X"}, map.Enumerate().Select(_ => _.Value));
        }

        [Test]
        public void Update_with_not_found_key_should_return_the_same_tree()
        {
            var tree = ImHashMap<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(2, "b").AddOrUpdate(3, "c").AddOrUpdate(4, "d");

            var updatedTree = tree.Update(5, "e");

            Assert.AreSame(tree, updatedTree);
        }

        [Test]
        public void Can_use_int_key_tree_to_represent_general_HashTree_with_possible_hash_conflicts()
        {
            var tree = ImMap<KeyValuePair<Type, string>[]>.Empty;

            var key = typeof(ImMapTests);
            var keyHash = key.GetHashCode();
            var value = "test";

            KeyValuePair<Type, string>[] Update(int _, KeyValuePair<Type, string>[] oldValue, KeyValuePair<Type, string>[] newValue)
            {
                var newItem = newValue[0];
                var oldItemCount = oldValue.Length;
                for (var i = 0; i < oldItemCount; i++)
                {
                    if (oldValue[i].Key == newItem.Key)
                    {
                        var updatedItems = new KeyValuePair<Type, string>[oldItemCount];
                        Array.Copy(oldValue, updatedItems, updatedItems.Length);
                        updatedItems[i] = newItem;
                        return updatedItems;
                    }
                }

                var addedItems = new KeyValuePair<Type, string>[oldItemCount + 1];
                Array.Copy(oldValue, addedItems, addedItems.Length);
                addedItems[oldItemCount] = newItem;
                return addedItems;
            }

            tree = tree.AddOrUpdate(keyHash, new[] {new KeyValuePair<Type, string>(key, value)}, Update);
            tree = tree.AddOrUpdate(keyHash, new[] {new KeyValuePair<Type, string>(key, value)}, Update);

            string result = null;

            var items = tree.GetValueOrDefault(keyHash);
            if (items != null)
            {
                var firstItem = items[0];
                if (firstItem.Key == key)
                    result = firstItem.Value;
                else if (items.Length > 1)
                {
                    for (var i = 1; i < items.Length; i++)
                    {
                        if (items[i].Key == key)
                        {
                            result = items[i].Value;
                            break;
                        }
                    }
                }
            }

            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void Remove_from_one_node_tree()
        {
            var tree = ImHashMap<int, string>.Empty.AddOrUpdate(0, "a");

            tree = tree.Remove(0);

            Assert.That(tree.IsEmpty, Is.True);
        }

        [Test]
        public void Remove_from_Empty_tree_should_not_throw()
        {
            var tree = ImHashMap<int, string>.Empty.Remove(0);
            Assert.That(tree.IsEmpty, Is.True);
        }
    }
}
