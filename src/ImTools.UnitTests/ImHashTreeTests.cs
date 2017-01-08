using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class ImHashTreeTests
    {
        [Test]
        public void Tree_should_support_arbitrary_keys_by_using_their_hash_code()
        {
            var tree = ImHashTree<Type, string>.Empty;

            var key = typeof(ImHashTreeTests);
            var value = "test";

            tree = tree.AddOrUpdate(key, value);

            var result = tree.GetValueOrDefault(key);
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void Tree_should_support_arbitrary_keys_by_using_their_hash_code_TryFind()
        {
            var tree = ImHashTree<Type, string>.Empty;

            var key = typeof(ImHashTreeTests);
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
            var tree = ImHashTree<HashConflictingKey<string>, int>.Empty
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
            var tree = ImHashTree<HashConflictingKey<string>, int>.Empty
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
            var tree = ImHashTree<HashConflictingKey<string>, int>.Empty
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
        public void Test_that_all_added_values_are_accessible()
        {
            var t = ImHashTree<int, int>.Empty
                .AddOrUpdate(1, 11)
                .AddOrUpdate(2, 22)
                .AddOrUpdate(3, 33);

            Assert.AreEqual(11, t.GetValueOrDefault(1));
            Assert.AreEqual(22, t.GetValueOrDefault(2));
            Assert.AreEqual(33, t.GetValueOrDefault(3));
        }

        [Test]
        public void Test_balance_ensured_for_left_left_tree()
        {
            var t = ImHashTree<int, int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(3, 3);

            //     5   =>    4
            //   4         3   5
            // 3
            Assert.AreEqual(4, t.Key);
            Assert.AreEqual(3, t.Left.Key);
            Assert.AreEqual(5, t.Right.Key);
        }

        [Test]
        public void Test_balance_preserved_when_add_to_balanced_tree()
        {
            var t = ImHashTree<int, int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(3, 3)
                // add to that
                .AddOrUpdate(2, 4)
                .AddOrUpdate(1, 5);

            //       4    =>     4
            //     3   5      2     5
            //   2          1   3
            // 1
            Assert.AreEqual(4, t.Key);
            Assert.AreEqual(2, t.Left.Key);
            Assert.AreEqual(1, t.Left.Left.Key);
            Assert.AreEqual(3, t.Left.Right.Key);
            Assert.AreEqual(5, t.Right.Key);

            // parent node balancing
            t = t.AddOrUpdate(-1, 6);

            //         4                 2
            //      2     5   =>      1     4
            //    1   3            -1     3   5
            // -1

            Assert.AreEqual(2, t.Key);
            Assert.AreEqual(1, t.Left.Key);
            Assert.AreEqual(-1, t.Left.Left.Key);

            Assert.AreEqual(4, t.Right.Key);
            Assert.AreEqual(3, t.Right.Left.Key);
            Assert.AreEqual(5, t.Right.Right.Key);
        }

        [Test]
        public void Test_balance_ensured_for_left_right_tree()
        {
            var t = ImHashTree<int, int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(3, 2)
                .AddOrUpdate(4, 3);

            //     5  =>    5   =>   4 
            //  3         4        3   5
            //    4     3  
            Assert.AreEqual(4, t.Key);
            Assert.AreEqual(3, t.Left.Key);
            Assert.AreEqual(5, t.Right.Key);
        }

        [Test]
        public void Test_balance_ensured_for_right_right_tree()
        {
            var t = ImHashTree<int, int>.Empty
                .AddOrUpdate(3, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(5, 3);

            // 3      =>     4
            //   4         3   5
            //     5
            Assert.AreEqual(4, t.Key);
            Assert.AreEqual(3, t.Left.Key);
            Assert.AreEqual(5, t.Right.Key);
        }

        [Test]
        public void Test_balance_ensured_for_right_left_tree()
        {
            var t = ImHashTree<int, int>.Empty
                .AddOrUpdate(3, 1)
                .AddOrUpdate(5, 2)
                .AddOrUpdate(4, 3);

            // 3      =>   3     =>    4
            //    5          4       3   5
            //  4              5
            Assert.AreEqual(4, t.Key);
            Assert.AreEqual(3, t.Left.Key);
            Assert.AreEqual(5, t.Right.Key);
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw()
        {
            var tree = ImHashTree<int, int>.Empty;

            Assert.DoesNotThrow(
                () => tree.GetValueOrDefault(0));
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw_TryFind()
        {
            var tree = ImHashTree<int, int>.Empty;

            int result;
            Assert.DoesNotThrow(
                () => tree.TryFind(0, out result));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw()
        {
            var tree = ImHashTree<int, int>.Empty
                .AddOrUpdate(1, 1)
                .AddOrUpdate(3, 2);

            Assert.DoesNotThrow(
                () => tree.GetValueOrDefault(2));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw_TryFind()
        {
            var tree = ImHashTree<int, int>.Empty
                .AddOrUpdate(1, 1)
                .AddOrUpdate(3, 2);

            int result;
            Assert.DoesNotThrow(
                () => tree.TryFind(2, out result));
        }

        [Test]
        public void For_two_same_added_items_height_should_be_one()
        {
            var tree = ImHashTree<int, string>.Empty
                .AddOrUpdate(1, "x")
                .AddOrUpdate(1, "y");

            Assert.AreEqual(1, tree.Height);
        }

        [Test]
        public void Enumerated_values_should_be_returned_in_sorted_order()
        {
            var items = Enumerable.Range(0, 10).ToArray();
            var tree = items.Aggregate(ImHashTree<int, int>.Empty, (t, i) => t.AddOrUpdate(i, i));

            var enumerated = tree.Enumerate().Select(t => t.Value).ToArray();

            CollectionAssert.AreEqual(items, enumerated);
        }

        [Test]
        public void Update_to_null_and_then_to_value_should_remove_null()
        {
            var tree = ImHashTree<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(2, "b").AddOrUpdate(3, "c").AddOrUpdate(4, "d");
            Assert.That(tree.GetValueOrDefault(4), Is.EqualTo("d"));

            tree = tree.Update(4, null);
            Assert.That(tree.GetValueOrDefault(4), Is.EqualTo(null));

            tree = tree.Update(4, "X");
            CollectionAssert.AreEqual(new[] {"a", "b", "c", "X"},
                tree.Enumerate().Select(_ => _.Value));
        }

        [Test]
        public void Update_of_not_found_key_should_return_the_same_tree()
        {
            var tree = ImHashTree<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(2, "b").AddOrUpdate(3, "c").AddOrUpdate(4, "d");

            var updatedTree = tree.Update(5, "e");

            Assert.AreSame(tree, updatedTree);
        }

        [Test]
        public void Can_use_int_key_tree_to_represent_general_HashTree_with_possible_hash_conflicts()
        {
            var tree = ImHashTree<int, KeyValuePair<Type, string>[]>.Empty;

            var key = typeof(ImHashTreeTests);
            var keyHash = key.GetHashCode();
            var value = "test";

            Update<KeyValuePair<Type, string>[]> update = (oldValue, newValue) =>
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
            };

            tree = tree.AddOrUpdate(keyHash, new[] {new KeyValuePair<Type, string>(key, value)}, update);
            tree = tree.AddOrUpdate(keyHash, new[] {new KeyValuePair<Type, string>(key, value)}, update);

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
            var tree = ImHashTree<int, string>.Empty.AddOrUpdate(0, "a");

            tree = tree.Remove(0);

            Assert.That(tree.IsEmpty, Is.True);
        }

        [Test]
        public void Remove_from_Empty_tree_should_not_throw()
        {
            var tree = ImHashTree<int, string>.Empty.Remove(0);
            Assert.That(tree.IsEmpty, Is.True);
        }

        [Test]
        public void Remove_from_top_of_LL_tree()
        {
            var tree = ImHashTree<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(0, "b");

            tree = tree.Remove(1);

            Assert.That(tree.Height, Is.EqualTo(1));
            Assert.That(tree.Value, Is.EqualTo("b"));
        }

        [Test]
        public void Remove_not_found_key()
        {
            var tree = ImHashTree<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(0, "b");

            tree = tree.Remove(3);

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
        }

        [Test]
        public void Remove_from_top_of_RR_tree()
        {
            var tree = ImHashTree<int, string>.Empty
                .AddOrUpdate(0, "a").AddOrUpdate(1, "b");

            tree = tree.Remove(0);

            Assert.That(tree.Height, Is.EqualTo(1));
            Assert.That(tree.Value, Is.EqualTo("b"));
        }

        [Test]
        public void Remove_from_top_of_tree()
        {
            var tree = ImHashTree<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(0, "b")
                .AddOrUpdate(3, "c").AddOrUpdate(2, "d").AddOrUpdate(4, "e");
            Assert.That(tree.Value, Is.EqualTo("a"));

            tree = tree.Remove(1);

            Assert.That(tree.Value, Is.EqualTo("d"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
            Assert.That(tree.Right.Value, Is.EqualTo("c"));
            Assert.That(tree.Right.Right.Value, Is.EqualTo("e"));
        }

        [Test]
        public void Remove_from_right_tree()
        {
            var tree = ImHashTree<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(0, "b")
                .AddOrUpdate(3, "c").AddOrUpdate(2, "d").AddOrUpdate(4, "e");
            Assert.That(tree.Value, Is.EqualTo("a"));

            tree = tree.Remove(2);

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
            Assert.That(tree.Right.Value, Is.EqualTo("c"));
            Assert.That(tree.Right.Right.Value, Is.EqualTo("e"));
        }

        [Test]
        public void Remove_from_node_with_one_conflict()
        {
            var tree = ImHashTree<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d");

            tree = tree.Remove(new HashConflictingKey<int>(2, 2));

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
            Assert.That(tree.Right.Value, Is.EqualTo("d"));
            Assert.That(tree.Right.Conflicts, Is.Null);
        }

        [Test]
        public void Remove_from_node_with_multiple_conflicts()
        {
            var tree = ImHashTree<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d")
                .AddOrUpdate(new HashConflictingKey<int>(4, 2), "e");

            tree = tree.Remove(new HashConflictingKey<int>(2, 2));

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
            Assert.That(tree.Right.Value, Is.EqualTo("d"));
            Assert.That(tree.Right.Conflicts[0].Value, Is.EqualTo("e"));
        }

        [Test]
        public void Remove_from_conflicts_with_one_conflict()
        {
            var tree = ImHashTree<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d");

            tree = tree.Remove(new HashConflictingKey<int>(3, 2));

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
            Assert.That(tree.Right.Value, Is.EqualTo("c"));
            Assert.That(tree.Right.Conflicts, Is.Null);
        }

        [Test]
        public void Remove_from_conflicts_with_multiple_conflicts()
        {
            var tree = ImHashTree<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d")
                .AddOrUpdate(new HashConflictingKey<int>(4, 2), "e");

            tree = tree.Remove(new HashConflictingKey<int>(3, 2));

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
            Assert.That(tree.Right.Value, Is.EqualTo("c"));
            Assert.That(tree.Right.Conflicts[0].Value, Is.EqualTo("e"));
        }

        [Test]
        public void Remove_from_node_when_not_found_conflict()
        {
            var tree = ImHashTree<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b");


            tree = tree.Remove(new HashConflictingKey<int>(2, 1));

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
        }

        [Test]
        public void Remove_from_node_with_conflicts_when_not_found_conflict()
        {
            var tree = ImHashTree<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d");

            tree = tree.Remove(new HashConflictingKey<int>(4, 2));

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
            Assert.That(tree.Right.Value, Is.EqualTo("c"));
        }

        [Test]
        public void I_can_update_value_instead_of_remove()
        {
            var tree = ImHashTree<string, string>.Empty
                .AddOrUpdate("a", "123")
                .AddOrUpdate("b", "321");

            tree = tree.RemoveOrUpdate("b", (string value, out string updatedValue) =>
            {
                updatedValue = value.Replace('1', '3');
                return true;
            });

            Assert.That(tree.GetValueOrDefault("a"), Is.EqualTo("123"));
            Assert.That(tree.GetValueOrDefault("b"), Is.EqualTo("323"));
        }

        [Test]
        public void I_can_update_conflicted_value_instead_of_remove()
        {
            var tree = ImHashTree<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d");

            tree = tree.RemoveOrUpdate(new HashConflictingKey<int>(3, 2),
                (string value, out string updatedValue) =>
                {
                    updatedValue = value + "!";
                    return true;
                });

            Assert.That(tree.Value, Is.EqualTo("a"));
            Assert.That(tree.Left.Value, Is.EqualTo("b"));
            Assert.That(tree.Right.Value, Is.EqualTo("c"));
            Assert.That(tree.Right.Conflicts[0].Value, Is.EqualTo("d!"));
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

            public override int GetHashCode()
            {
                return _hash;
            }

            public override bool Equals(object obj)
            {
                return Equals(Key, ((HashConflictingKey<T>) obj).Key);
            }
        }
    }
}
