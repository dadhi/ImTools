using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class PartitionedHashMapTests
    {
        [Test]
        public void Test_that_all_added_values_are_accessible()
        {
            var t = PartitionedMap.CreateEmpty<int>();
            t.AddOrUpdate(1, 11);
            t.AddOrUpdate(2, 22);
            t.AddOrUpdate(3, 33);

            Assert.AreEqual(11, t[1].GetValueOrDefault(1));
            Assert.AreEqual(22, t[2].GetValueOrDefault(2));
            Assert.AreEqual(33, t[3].GetValueOrDefault(3));
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw()
        {
            var tree = PartitionedMap.CreateEmpty<int>();

            Assert.AreEqual(0, tree[0].GetValueOrDefault(0));
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw_TryFind()
        {
            var maps = PartitionedMap.CreateEmpty<int>();

            Assert.IsFalse(maps[0].TryFind(0, out _));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw()
        {
            var tree = PartitionedMap.CreateEmpty<int>();
            tree.AddOrUpdate(1, 1);
            tree.AddOrUpdate(3, 2);

            Assert.AreEqual(0, tree[2].GetValueOrDefault(2));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw_TryFind()
        {
            var tree = PartitionedMap.CreateEmpty<int>();

            tree.AddOrUpdate(1, 1);
            tree.AddOrUpdate(3, 2);

            Assert.IsFalse(tree[2].TryFind(2, out _));
        }

        [Test]
        public void Update_to_null_and_then_to_value_should_remove_null()
        {
            var maps = PartitionedMap.CreateEmpty<string>();
            maps.AddOrUpdate(1, "a");
            maps.AddOrUpdate(2, "b");
            maps.AddOrUpdate(3, "c");
            maps.AddOrUpdate(4, "d");

            Assert.AreEqual("d", maps[4].GetValueOrDefault(4));

            maps.Update(4, null);
            Assert.IsNull(maps[4].GetValueOrDefault(4));

            maps.Update(4, "X");
        }

        [Test]
        public void Update_with_not_found_key_should_return_the_same_tree()
        {
            var maps = PartitionedMap.CreateEmpty<string>();
            maps.AddOrUpdate(1, "a");
            maps.AddOrUpdate(2, "b");
            maps.AddOrUpdate(3, "c");
            maps.AddOrUpdate(4, "d");

            var x = maps[5];
            maps.Update(5, "e");

            Assert.AreSame(x, maps[5]);
        }

        [Test]
        public void Can_use_int_key_tree_to_represent_general_HashTree_with_possible_hash_conflicts()
        {
            var tree = PartitionedMap.CreateEmpty<KeyValuePair<Type, string>[]>();

            var key = typeof(PartitionedHashMapTests);
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

            tree.AddOrUpdate(keyHash, new[] { new KeyValuePair<Type, string>(key, value) }, Update);
            tree.AddOrUpdate(keyHash, new[] { new KeyValuePair<Type, string>(key, value) }, Update);

            string result = null;

            var items = tree[keyHash & PartitionedMap.PARTITION_HASH_MASK].GetValueOrDefault(keyHash);
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

            Assert.AreEqual("test", result);
        }
    }
}
