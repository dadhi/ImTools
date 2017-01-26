using System;
using NUnit.Framework;

namespace ImTools.Experimental.UnitTests
{
    [TestFixture]
    public class ImMapForestTests
    {
        [Test]
        public void Tree_should_support_arbitrary_keys_by_using_their_hash_code_TryFind()
        {
            var tree = ImMap<Type, string>.Empty;

            var key = typeof(ImMapForestTests);
            var value = "test";

            tree = tree.AddOrUpdate(key, value);

            string result;
            Assert.IsTrue(tree.TryFind(key, out result));
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void Test_balance_ensured_for_left_left_tree()
        {
            var t = ImMap<int, int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(3, 3);
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
    }
}
