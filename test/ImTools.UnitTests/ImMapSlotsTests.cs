using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class ImMapSlotsTests
    {
        [Test]
        public void Test_that_all_added_values_are_accessible()
        {
            var t = ImMapSlots.CreateWithEmpty<int>();
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
            var tree = ImMapSlots.CreateWithEmpty<int>();

            Assert.AreEqual(0, tree[0].GetValueOrDefault(0));
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw_TryFind()
        {
            var maps = ImMapSlots.CreateWithEmpty<int>();

            Assert.IsFalse(maps[0].TryFind(0, out _));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw()
        {
            var tree = ImMapSlots.CreateWithEmpty<int>();
            tree.AddOrUpdate(1, 1);
            tree.AddOrUpdate(3, 2);

            Assert.AreEqual(0, tree[2].GetValueOrDefault(2));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw_TryFind()
        {
            var tree = ImMapSlots.CreateWithEmpty<int>();
            tree.AddOrUpdate(1, 1);
            tree.AddOrUpdate(3, 2);

            Assert.IsFalse(tree[2].TryFind(2, out _));
        }

        [Test]
        public void Update_to_null_and_then_to_value_should_remove_null()
        {
            var maps = ImMapSlots.CreateWithEmpty<string>();
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
            var maps = ImMapSlots.CreateWithEmpty<string>();
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
            var tree = ImMapSlots.CreateWithEmpty<KeyValuePair<Type, string>[]>();

            var key = typeof(ImMapSlotsTests);
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

            var items = tree[keyHash & ImMapSlots.KEY_MASK_TO_FIND_SLOT].GetValueOrDefault(keyHash);
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

        //[Test]
        //public void Remove_from_one_node_tree()
        //{
        //    var map = ImMapArray<string>.Create();
        //    map.AddOrUpdate(0, "a");

        //    map.Remove(0);

        //    Assert.That(map.IsEmpty, Is.True);
        //}

        //[Test]
        //public void Remove_from_Empty_tree_should_not_throw()
        //{
        //    var tree = ImMapArray<string>.Create();
        //    tree.Remove(0);
        //    Assert.That(tree.IsEmpty, Is.True);
        //}

        //[Test]
        //public void Remove_from_top_of_LL_tree()
        //{
        //    var tree = ImMap<string>.Empty
        //        .AddOrUpdate(1, "a").AddOrUpdate(0, "b");

        //    tree = tree.Remove(1);

        //    Assert.That(tree.Height, Is.EqualTo(1));
        //    Assert.That(tree.Value, Is.EqualTo("b"));
        //}

        //[Test]
        //public void Remove_not_found_key()
        //{
        //    var tree = ImMap<string>.Empty
        //        .AddOrUpdate(1, "a").AddOrUpdate(0, "b");

        //    tree = tree.Remove(3);

        //    Assert.That(tree.Value, Is.EqualTo("a"));
        //    Assert.That(tree.Left.Value, Is.EqualTo("b"));
        //}

        //[Test]
        //public void Remove_from_top_of_RR_tree()
        //{
        //    var tree = ImMap<string>.Empty
        //        .AddOrUpdate(0, "a").AddOrUpdate(1, "b");

        //    tree = tree.Remove(0);

        //    Assert.That(tree.Height, Is.EqualTo(1));
        //    Assert.That(tree.Value, Is.EqualTo("b"));
        //}

        //[Test]
        //public void Remove_from_top_of_tree()
        //{
        //    var tree = ImMap<string>.Empty
        //        .AddOrUpdate(1, "a")
        //        .AddOrUpdate(0, "b")
        //        .AddOrUpdate(3, "c")
        //        .AddOrUpdate(2, "d")
        //        .AddOrUpdate(4, "e");

        //    //            1:a
        //    //       0:b       3:c
        //    //              2:d   4:e
        //    Assert.AreEqual("a", tree.Value);

        //    tree = tree.Remove(1);

        //    //            2:d
        //    //       0:b       3:c
        //    //                    4:e
        //    Assert.That(tree.Value, Is.EqualTo("d"));
        //    Assert.That(tree.Left.Value, Is.EqualTo("b"));
        //    Assert.That(tree.Right.Value, Is.EqualTo("c"));
        //    Assert.That(tree.Right.Right.Value, Is.EqualTo("e"));
        //}

        //[Test]
        //public void Remove_from_right_tree()
        //{
        //    var tree = ImMap<string>.Empty
        //        .AddOrUpdate(1, "a").AddOrUpdate(0, "b")
        //        .AddOrUpdate(3, "c").AddOrUpdate(2, "d").AddOrUpdate(4, "e");

        //    Assert.That(tree.Value, Is.EqualTo("a"));

        //    tree = tree.Remove(2);

        //    Assert.That(tree.Value, Is.EqualTo("a"));
        //    Assert.That(tree.Left.Value, Is.EqualTo("b"));
        //    Assert.That(tree.Right.Value, Is.EqualTo("c"));
        //    Assert.That(tree.Right.Right.Value, Is.EqualTo("e"));
        //}
    }
}
