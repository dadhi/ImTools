using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ImTools.Experimental.UnitTests
{
    [TestFixture]
    public class ImMapTests3
    {
        [Test]
        public void Tree_should_support_arbitrary_keys_by_using_their_hash_code()
        {
            var tree = ImMap<Type, string>.Empty;

            var key = typeof(ImMapTests3);
            var value = "test";

            tree = tree.AddOrUpdate(key, value);

            var result = tree.GetValueOrDefault(key);
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void Tree_should_support_arbitrary_keys_by_using_their_hash_code_TryFind()
        {
            var tree = ImMap<Type, string>.Empty;

            var key = typeof(ImMapTests3);
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
            var tree = ImMap<HashConflictingKey<string>, int>.Empty
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
            var tree = ImMap<HashConflictingKey<string>, int>.Empty
                .AddOrUpdate(key1, 1)
                .AddOrUpdate(key2, 2)
                .AddOrUpdate(key3, 3);

            int value;
            Assert.IsTrue(tree.TryFind(key3, out value));
            Assert.That(value, Is.EqualTo(3));
        }

        [Test]
        public void Test_that_all_added_values_are_accessible()
        {
            var t = ImMap<int, int>.Empty
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
            var t = ImMap<int, int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(3, 3);

            //     5   =>    4
            //   4         3   5
            // 3
        }

        [Test]
        public void Test_balance_preserved_when_add_to_balanced_tree()
        {
            var t = ImMap<int, int>.Empty
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

            // parent node balancing
            t = t.AddOrUpdate(-1, 6);

            //         4                 2
            //      2     5   =>      1     4
            //    1   3            -1     3   5
            // -1

        }

        [Test]
        public void Test_balance_ensured_for_left_right_tree()
        {
            var t = ImMap<int, int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(3, 2)
                .AddOrUpdate(4, 3);

            //     5  =>    5   =>   4 
            //  3         4        3   5
            //    4     3  
        }

        [Test]
        public void Test_balance_ensured_for_right_right_tree()
        {
            var t = ImMap<int, int>.Empty
                .AddOrUpdate(3, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(5, 3);

            // 3      =>     4
            //   4         3   5
            //     5
        }

        [Test]
        public void Test_balance_ensured_for_right_left_tree()
        {
            var t = ImMap<int, int>.Empty
                .AddOrUpdate(3, 1)
                .AddOrUpdate(5, 2)
                .AddOrUpdate(4, 3);

            // 3      =>   3     =>    4
            //    5          4       3   5
            //  4              5
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw()
        {
            var tree = ImMap<int, int>.Empty;

            Assert.DoesNotThrow(
                () => tree.GetValueOrDefault(0));
        }

        [Test]
        public void Search_in_empty_tree_should_NOT_throw_TryFind()
        {
            var tree = ImMap<int, int>.Empty;

            int result;
            Assert.DoesNotThrow(
                () => tree.TryFind(0, out result));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw()
        {
            var tree = ImMap<int, int>.Empty
                .AddOrUpdate(1, 1)
                .AddOrUpdate(3, 2);

            Assert.DoesNotThrow(
                () => tree.GetValueOrDefault(2));
        }

        [Test]
        public void Search_for_non_existent_key_should_NOT_throw_TryFind()
        {
            var tree = ImMap<int, int>.Empty
                .AddOrUpdate(1, 1)
                .AddOrUpdate(3, 2);

            int result;
            Assert.DoesNotThrow(
                () => tree.TryFind(2, out result));
        }

        [Test]
        public void For_two_same_added_items_height_should_be_one()
        {
            var tree = ImMap<int, string>.Empty
                .AddOrUpdate(1, "x")
                .AddOrUpdate(1, "y");

        }

        [Test]
        public void Update_of_not_found_key_should_return_the_same_tree()
        {
            var tree = ImMap<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(2, "b").AddOrUpdate(3, "c").AddOrUpdate(4, "d");

            var updatedTree = tree.Update(5, "e");

            Assert.AreSame(tree, updatedTree);
        }

        [Test]
        public void Remove_from_one_node_tree()
        {
            var tree = ImMap<int, string>.Empty.AddOrUpdate(0, "a");

            tree = tree.Remove(0);

            Assert.That(tree.IsEmpty, Is.True);
        }

        [Test]
        public void Remove_from_Empty_tree_should_not_throw()
        {
            var tree = ImMap<int, string>.Empty.Remove(0);
            Assert.That(tree.IsEmpty, Is.True);
        }

        [Test]
        public void Remove_from_top_of_LL_tree()
        {
            var tree = ImMap<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(0, "b");

            tree = tree.Remove(1);

            Assert.That(tree.Count, Is.EqualTo(1));
        }

        [Test]
        public void Remove_not_found_key()
        {
            var tree = ImMap<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(0, "b");

            tree = tree.Remove(3);
        }

        [Test]
        public void Remove_from_top_of_RR_tree()
        {
            var tree = ImMap<int, string>.Empty
                .AddOrUpdate(0, "a").AddOrUpdate(1, "b");

            tree = tree.Remove(0);
        }

        [Test]
        public void Remove_from_top_of_tree()
        {
            var tree = ImMap<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(0, "b")
                .AddOrUpdate(3, "c").AddOrUpdate(2, "d").AddOrUpdate(4, "e");

            tree = tree.Remove(1);

        }

        [Test]
        public void Remove_from_right_tree()
        {
            var tree = ImMap<int, string>.Empty
                .AddOrUpdate(1, "a").AddOrUpdate(0, "b")
                .AddOrUpdate(3, "c").AddOrUpdate(2, "d").AddOrUpdate(4, "e");

            tree = tree.Remove(2);
        }

        [Test]
        public void Remove_from_node_with_one_conflict()
        {
            var tree = ImMap<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d");

            tree = tree.Remove(new HashConflictingKey<int>(2, 2));
        }

        [Test]
        public void Remove_from_node_with_multiple_conflicts()
        {
            var tree = ImMap<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d")
                .AddOrUpdate(new HashConflictingKey<int>(4, 2), "e");

            tree = tree.Remove(new HashConflictingKey<int>(2, 2));
        }

        [Test]
        public void Remove_from_conflicts_with_one_conflict()
        {
            var tree = ImMap<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d");

            tree = tree.Remove(new HashConflictingKey<int>(3, 2));
        }

        [Test]
        public void Remove_from_conflicts_with_multiple_conflicts()
        {
            var tree = ImMap<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d")
                .AddOrUpdate(new HashConflictingKey<int>(4, 2), "e");

            tree = tree.Remove(new HashConflictingKey<int>(3, 2));
        }

        [Test]
        public void Remove_from_node_when_not_found_conflict()
        {
            var tree = ImMap<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b");


            tree = tree.Remove(new HashConflictingKey<int>(2, 1));
        }

        [Test]
        public void Remove_from_node_with_conflicts_when_not_found_conflict()
        {
            var tree = ImMap<HashConflictingKey<int>, string>.Empty
                .AddOrUpdate(new HashConflictingKey<int>(1, 1), "a")
                .AddOrUpdate(new HashConflictingKey<int>(0, 0), "b")
                .AddOrUpdate(new HashConflictingKey<int>(2, 2), "c")
                .AddOrUpdate(new HashConflictingKey<int>(3, 2), "d");

            tree = tree.Remove(new HashConflictingKey<int>(4, 2));
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
