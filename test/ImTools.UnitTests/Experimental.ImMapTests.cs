using NUnit.Framework;

namespace ImTools.Experimental.UnitTests
{
    [TestFixture]
    public class ImMapTests
    {
        [Test]
        public void Test_that_all_added_values_are_accessible()
        {
            var t = ImMap<int>.Empty
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
            var t = ImMap<int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(3, 3) 
                .To<ImMapTree<int>>();

            //     5   =>    4
            //   4         3   5
            // 3
            Assert.AreEqual(4, t.Entry.Key);
            Assert.AreEqual(3, t.Left.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(5, t.Right.To<ImMapEntry<int>>().Key);
        }

        [Test]
        public void Test_balance_preserved_when_add_to_balanced_tree()
        {
            var t = ImMap<int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(3, 3)
                // add to that
                .AddOrUpdate(2, 4)
                .AddOrUpdate(1, 5)
                .To<ImMapTree<int>>();

            //       4    =>     4
            //     3   5      2     5
            //   2          1   3
            // 1
            Assert.AreEqual(4, t.Entry.Key);
            Assert.AreEqual(2, t.Left.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(1, t.Left.To<ImMapTree<int>>().Left.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(3, t.Left.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(5, t.Right.To<ImMapEntry<int>>().Key);

            // parent node balancing
            var t1 = t.AddOrUpdate(-1, 6);

            //         4                 2
            //      2     5   =>     -1     4
            //    1   3                1   3   5
            // -1

            Assert.AreEqual(2,  t1.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(-1, t1.To<ImMapTree<int>>().Left.To<ImMapBranch<int>>().Entry.Key);
            Assert.AreEqual(1, t1.To<ImMapTree<int>>().Left.To<ImMapBranch<int>>().RightEntry.Key);

            Assert.AreEqual(4, t1.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(3, t1.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(5, t1.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
        }

        [Test]
        public void Test_balance_ensured_for_left_right_tree()
        {
            var t = ImMap<int>.Empty
                .AddOrUpdate(5, 1)
                .AddOrUpdate(3, 2)
                .AddOrUpdate(4, 3)
                .To<ImMapTree<int>>();

            //     5  =>    5   =>   4 
            //  3         4        3   5
            //    4     3  
            Assert.AreEqual(4, t.Entry.Key);
            Assert.AreEqual(3, t.Left .To<ImMapEntry<int>>().Key);
            Assert.AreEqual(5, t.Right.To<ImMapEntry<int>>().Key);
        }

        [Test]
        public void Test_balance_ensured_for_right_right_tree()
        {
            var t = ImMap<int>.Empty
                .AddOrUpdate(3, 1)
                .AddOrUpdate(4, 2)
                .AddOrUpdate(5, 3)
                .To<ImMapTree<int>>();

            // 3      =>     4
            //   4         3   5
            //     5
            Assert.AreEqual(4, t.Entry.Key);
            Assert.AreEqual(3, t.Left.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(5, t.Right.To<ImMapEntry<int>>().Key);
        }

        [Test]
        public void Test_balance_ensured_for_right_left_tree()
        {
            var t = ImMap<int>.Empty
                .AddOrUpdate(3, 1)
                .AddOrUpdate(5, 2)
                .AddOrUpdate(4, 3)
                .To<ImMapTree<int>>();

            // 3      =>   3     =>    4
            //    5          4       3   5
            //  4              5
            Assert.AreEqual(4, t.Entry.Key);
            Assert.AreEqual(3, t.Left.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(5, t.Right.To<ImMapEntry<int>>().Key);
        }

        [Test]
        public void Test_double_rotation_in_tree_when_adding_to_the_right()
        {
            //            10                            10                            10                            10                                     12
            //      5           15                5           15                5            20               5                20                    10             20
            //   3     7    12      20     =>  3     7    12       23    =>  3     7    12       23    =>  3     7       12          23     =>    5      11    15        23
            //                          25                      20    25                  15   21   25                 11   15      21   25     3   7            17    21   25
            //                        23!                         21!                                                        17!                                   
            var t = ImMap<int>.Empty
                .AddOrUpdate(10, 10)

                .AddOrUpdate(15, 15)
                .AddOrUpdate(5,  5)
                .AddOrUpdate(7,  7)

                .AddOrUpdate(3,  3)
                .AddOrUpdate(20, 20)
                .AddOrUpdate(12, 12)
                .AddOrUpdate(25, 25)

                .AddOrUpdate(23, 23)
                .AddOrUpdate(21, 21) // here it goes double rotate!
                
                .AddOrUpdate(11, 11)
                .AddOrUpdate(17, 17) // boom again - on the global scale!
                .To<ImMapTree<int>>();

            Assert.AreEqual(12, t.Entry.Key);
            
            Assert.AreEqual(10, t.Left .To<ImMapTree<int>>().Entry.Key);
            
            Assert.AreEqual(5,  t.Left .To<ImMapTree<int>>().Left .To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(3,  t.Left .To<ImMapTree<int>>().Left .To<ImMapTree<int>>().Left .To<ImMapEntry<int>>().Key);
            Assert.AreEqual(7,  t.Left .To<ImMapTree<int>>().Left .To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);

            Assert.AreEqual(11,  t.Left .To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);

            Assert.AreEqual(20, t.Right.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(15, t.Right.To<ImMapTree<int>>().Left .To<ImMapBranch<int>>().Entry.Key);
            Assert.AreEqual(17, t.Right.To<ImMapTree<int>>().Left .To<ImMapBranch<int>>().RightEntry.Key);

            Assert.AreEqual(23, t.Right.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(21, t.Right.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left .To<ImMapEntry<int>>().Key);
            Assert.AreEqual(25, t.Right.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
        }

        [Test]
        public void ImMapWithConflicts_Test_double_rotation_in_tree_when_adding_to_the_right()
        {
            //            10                            10                            10                            10                                     12
            //      5           15                5           15                5            20               5                20                    10             20
            //   3     7    12      20     =>  3     7    12       23    =>  3     7    12       23    =>  3     7       12          23     =>    5      11    15        23
            //                          25                      20    25                  15   21   25                 11   15      21   25     3   7            17    21   25
            //                        23!                         21!                                                        17!                                   
            var t = ImMap<ImMap.KValue<int>>.Empty
                .AddOrUpdate(10, "10")

                .AddOrUpdate(15, "15")
                .AddOrUpdate(5,  "5 ")
                .AddOrUpdate(7,  "7 ")

                .AddOrUpdate(3,  "3 ")
                .AddOrUpdate(20, "20")
                .AddOrUpdate(12, "12")
                .AddOrUpdate(25, "25")

                .AddOrUpdate(23, "23")
                .AddOrUpdate(21, "21") // here it goes double rotate!

                .AddOrUpdate(11, "11")
                .AddOrUpdate(17, "17") // boom again - on the global scale!
                .To<ImMapTree<ImMap.KValue<int>>>();

            Assert.AreEqual(12, t.Entry.Key);

            Assert.AreEqual(10, t.Left. To<ImMapTree<ImMap.KValue<int>>>().Entry.Key);
            Assert.AreEqual(5,  t.Left. To<ImMapTree<ImMap.KValue<int>>>().Left .To<ImMapTree<ImMap.KValue<int>>>().Entry.Key);
            Assert.AreEqual(3,  t.Left. To<ImMapTree<ImMap.KValue<int>>>().Left .To<ImMapTree<ImMap.KValue<int>>>().Left .To<ImMapEntry<ImMap.KValue<int>>>().Key);
            Assert.AreEqual(7,  t.Left. To<ImMapTree<ImMap.KValue<int>>>().Left .To<ImMapTree<ImMap.KValue<int>>>().Right.To<ImMapEntry<ImMap.KValue<int>>>().Key);
            Assert.AreEqual(11, t.Left. To<ImMapTree<ImMap.KValue<int>>>().Right.To<ImMapEntry<ImMap.KValue<int>>>().Key);
            Assert.AreEqual(20, t.Right.To<ImMapTree<ImMap.KValue<int>>>().Entry.Key);
            Assert.AreEqual(15, t.Right.To<ImMapTree<ImMap.KValue<int>>>().Left.To<ImMapBranch<ImMap.KValue<int>>>().Entry.Key);
            Assert.AreEqual(17, t.Right.To<ImMapTree<ImMap.KValue<int>>>().Left.To<ImMapBranch<ImMap.KValue<int>>>().RightEntry.Key);
            Assert.AreEqual(23, t.Right.To<ImMapTree<ImMap.KValue<int>>>().Right.To<ImMapTree<ImMap.KValue<int>>>().Entry.Key);
            Assert.AreEqual(21, t.Right.To<ImMapTree<ImMap.KValue<int>>>().Right.To<ImMapTree<ImMap.KValue<int>>>().Left .To<ImMapEntry<ImMap.KValue<int>>>().Key);
            Assert.AreEqual(25, t.Right.To<ImMapTree<ImMap.KValue<int>>>().Right.To<ImMapTree<ImMap.KValue<int>>>().Right.To<ImMapEntry<ImMap.KValue<int>>>().Key);
        }

        [Test]
        public void Test_balance_when_adding_10_items_to_the_right()
        {
            var t = ImMap<int>.Empty;
            for (var i = 1; i <= 10; i++)
                t = t.AddOrUpdate(i, i);

            // 1     =>   2     =>    2     =>    2      =>       4       =>        4        =>        4           =>         4           =>         4           =>          4       
            //    2     1   3       1   3      1     4        2       5        2         6        2         6            2         6            2         6            2           8
            //                            4        3   5    1   3       6    1   3     5   7   1     3   5     7      1     3   5     8      1     3   5     8      1     3     6     9
            //                                                                                                   8                  7   9                  7   9              5   7     10
            Assert.AreEqual(4, t.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(2, t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(1, t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Left .To<ImMapEntry<int>>().Key);
            Assert.AreEqual(3, t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(8, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(6, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(5, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Left .To<ImMapEntry<int>>().Key);
            Assert.AreEqual(7, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(9, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Right.To<ImMapBranch<int>>().Entry.Key);
            Assert.AreEqual(10,t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Right.To<ImMapBranch<int>>().RightEntry.Key);
        }

        [Test]
        public void Test_balance_when_adding_10_items_to_the_right_with_double_rotation()
        {
            var t = ImMap<int>.Empty;
            t = t.AddOrUpdate(1, 1);
            t = t.AddOrUpdate(3, 3);
            t = t.AddOrUpdate(2, 2);
            t = t.AddOrUpdate(5, 5);
            t = t.AddOrUpdate(4, 4);
            t = t.AddOrUpdate(7, 7);
            t = t.AddOrUpdate(6, 6);
            t = t.AddOrUpdate(8, 8);
            t = t.AddOrUpdate(9, 9);
            t = t.AddOrUpdate(10, 10);

            // 1     =>   2     =>    2     =>    2      =>       4       =>        4        =>        4           =>         4           =>         4           =>          4       
            //    2     1   3       1   3      1     4        2       5        2         6        2         6            2         6            2         6            2           8
            //                            4        3   5    1   3       6    1   3     5   7   1     3   5     7      1     3   5     8      1     3   5     8      1     3     6     9
            //                                                                                                   8                  7   9                  7   9              5   7     10
            Assert.AreEqual(4, t.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(2, t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(1, t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Left.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(3, t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(8, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(6, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(5, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Left.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(7, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(9, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Right.To<ImMapBranch<int>>().Entry.Key);
            Assert.AreEqual(10, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Right.To<ImMapBranch<int>>().RightEntry.Key);
        }

        [Test]
        public void Test_balance_when_adding_100_items_to_the_right()
        {
            var t = ImMap<int>.Empty;
            for (var i = 1; i <= 100; i++)
                t = t.AddOrUpdate(i, i);

            Assert.AreEqual(64, t.To<ImMapTree<int>>().Entry.Key);
        }

        [Test]
        public void Test_balance_when_adding_10_items_to_the_left()
        {
            var t = ImMap<int>.Empty;
            for (var i = 10; i >= 1; i--)
                t = t.AddOrUpdate(i, i);

            // 10  =>   10     =>   9     =>    9     =>      9      =>       7      =>        7      =>          7      =>         7      =>         7       
            //        9           8   10      8   10      7      10       6       9        5       9          5       9         5       9         3       9   
            //                               7          6   8           5       8   10   4   6   8   10     4   6   8   10    3   6   8   10    1   5   8   10
            //                                                                                            3                  2 4                 2 4 6         
            Assert.AreEqual(7,  t.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(3,  t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(1,  t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Left.To<ImMapBranch<int>>().Entry.Key);
            Assert.AreEqual(2,  t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Left.To<ImMapBranch<int>>().RightEntry.Key);
            Assert.AreEqual(5,  t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(4,  t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left .To<ImMapEntry<int>>().Key);
            Assert.AreEqual(6,  t.To<ImMapTree<int>>().Left.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
            Assert.AreEqual(9,  t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Entry.Key);
            Assert.AreEqual(8,  t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Left .To<ImMapEntry<int>>().Key);
            Assert.AreEqual(10, t.To<ImMapTree<int>>().Right.To<ImMapTree<int>>().Right.To<ImMapEntry<int>>().Key);
        }

        [Test]
        public void Test_balance_when_adding_100_items_to_the_left()
        {
            var t = ImMap<int>.Empty;
            for (var i = 100; i >= 1; i--)
                t = t.AddOrUpdate(i, i);

            Assert.AreEqual(37, t.To<ImMapTree<int>>().Entry.Key);
        }

        //[Test]
        //public void Test_balance_when_adding_10_items_to_the_left_with_double_rotation()
        //{
        //    var t = ImMap<int>.Empty;
        //    t = t.AddOrUpdate(10, 10);
        //    t = t.AddOrUpdate(8, 8);
        //    t = t.AddOrUpdate(9, 9);
        //    t = t.AddOrUpdate(6, 6);
        //    t = t.AddOrUpdate(7, 7);
        //    t = t.AddOrUpdate(4, 4);
        //    t = t.AddOrUpdate(5, 5);
        //    t = t.AddOrUpdate(2, 2);
        //    t = t.AddOrUpdate(3, 3);
        //    t = t.AddOrUpdate(1, 1);

        //    // 10  =>   10     =>   9     =>    9     =>      9      =>       7      =>        7      =>          7      =>         7      =>         7       
        //    //        9           8   10      8   10      7      10       6       9        5       9          5       9         5       9         3       9   
        //    //                               7          6   8           5       8   10   4   6   8   10     4   6   8   10    3   6   8   10    2   5   8   10
        //    //                                                                                            3                  2 4               1   4 6         
        //    Assert.AreEqual(7, t.Key);
        //    Assert.AreEqual(3, t.Left.Key);
        //    Assert.AreEqual(2, t.Left.Left.Key);
        //    Assert.AreEqual(1, t.Left.Left.Left.Key);
        //    Assert.AreEqual(5, t.Left.Right.Key);
        //    Assert.AreEqual(4, t.Left.Right.Left.Key);
        //    Assert.AreEqual(6, t.Left.Right.Right.Key);
        //    Assert.AreEqual(9, t.Right.Key);
        //    Assert.AreEqual(8, t.Right.Left.Key);
        //    Assert.AreEqual(10, t.Right.Right.Key);
        //}

        //[Test]
        //public void Search_in_empty_tree_should_NOT_throw()
        //{
        //    var tree = ImMap<int>.Empty;

        //    Assert.AreEqual(0, tree.GetValueOrDefault(0));
        //}

        //[Test]
        //public void Search_in_empty_tree_should_NOT_throw_TryFind()
        //{
        //    var tree = ImMap<int>.Empty;

        //    Assert.IsFalse(tree.TryFind(0, out _));
        //}

        //[Test]
        //public void Search_for_non_existent_key_should_NOT_throw()
        //{
        //    var tree = ImMap<int>.Empty
        //        .AddOrUpdate(1, 1)
        //        .AddOrUpdate(3, 2);

        //    Assert.AreEqual(0, tree.GetValueOrDefault(2));
        //}

        //[Test]
        //public void Search_for_non_existent_key_should_NOT_throw_TryFind()
        //{
        //    var tree = ImMap<int>.Empty
        //        .AddOrUpdate(1, 1)
        //        .AddOrUpdate(3, 2);

        //    Assert.IsFalse(tree.TryFind(2, out _));
        //}

        //[Test]
        //public void For_two_same_added_items_height_should_be_one()
        //{
        //    var tree = ImMap<string>.Empty
        //        .AddOrUpdate(1, "x")
        //        .AddOrUpdate(1, "y");

        //    Assert.AreEqual(1, tree.Height);
        //}

        //[Test]
        //public void Enumerated_values_should_be_returned_in_sorted_order()
        //{
        //    var items = Enumerable.Range(0, 10).ToArray();
        //    var tree = items.Aggregate(ImMap<int>.Empty, (t, i) => t.AddOrUpdate(i, i));

        //    var enumerated = tree.Enumerate().Select(t => t.Value).ToArray();

        //    CollectionAssert.AreEqual(items, enumerated);
        //}

        //[Test]
        //public void Can_fold_2_level_tree()
        //{
        //    var t = ImMap<int>.Empty;
        //    t= t.AddOrUpdate(1, 1).AddOrUpdate(2, 2);

        //    var list = t.Fold(new List<int>(), (data, l) =>
        //    {
        //        l.Add(data.Value);
        //        return l;
        //    });

        //    CollectionAssert.AreEqual(new[] { 1, 2 }, list);
        //}

        //[Test]
        //public void Can_fold_3_level_tree()
        //{
        //    var t = ImMap<int>.Empty;
        //    t = t
        //        .AddOrUpdate(1, 1)
        //        .AddOrUpdate(2, 2)
        //        .AddOrUpdate(3, 3)
        //        .AddOrUpdate(4, 4);

        //    var list = t.Fold(new List<int>(), (data, l) =>
        //    {
        //        l.Add(data.Value);
        //        return l;
        //    });

        //    CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list);
        //}

        //[Test]
        //public void Folded_values_should_be_returned_in_sorted_order()
        //{
        //    var items = Enumerable.Range(0, 10).ToArray();
        //    var tree = items.Aggregate(ImMap<int>.Empty, (t, i) => t.AddOrUpdate(i, i));

        //    var list = tree.Fold(new List<int>(), (data, l) =>
        //    {
        //        l.Add(data.Value);
        //        return l;
        //    });

        //    CollectionAssert.AreEqual(items, list);
        //}

        //[Test]
        //public void Folded_lefty_values_should_be_returned_in_sorted_order()
        //{
        //    var items = Enumerable.Range(0, 100).ToArray();
        //    var tree = items.Reverse().Aggregate(ImMap<int>.Empty, (t, i) => t.AddOrUpdate(i, i));

        //    var list = tree.Fold(new List<int>(), (data, l) =>
        //    {
        //        l.Add(data.Value);
        //        return l;
        //    });

        //    CollectionAssert.AreEqual(items, list);
        //}

        //[Test]
        //public void ImMapSlots_Folded_values_should_be_returned_in_sorted_order()
        //{
        //    var items = Enumerable.Range(0, 10).ToArray();
        //    var tree = items.Aggregate(ImMapSlots.CreateWithEmpty<int>(), (t, i) => t.Do(x => x.AddOrUpdate(i, i)));

        //    var list = tree.Fold(new List<int>(), (data, l) =>
        //    {
        //        l.Add(data.Value);
        //        return l;
        //    });

        //    CollectionAssert.AreEqual(items, list);
        //}

        //[Test]
        //public void Update_to_null_and_then_to_value_should_remove_null()
        //{
        //    var map = ImMap<string>.Empty
        //        .AddOrUpdate(1, "a")
        //        .AddOrUpdate(2, "b")
        //        .AddOrUpdate(3, "c")
        //        .AddOrUpdate(4, "d");

        //    Assert.AreEqual("d", map.GetValueOrDefault(4));

        //    map = map.Update(4, null);
        //    Assert.IsNull(map.GetValueOrDefault(4));

        //    map = map.Update(4, "X");
        //    CollectionAssert.AreEqual(new[] {"a", "b", "c", "X"}, map.Enumerate().Select(_ => _.Value));
        //}

        //[Test]
        //public void Update_with_not_found_key_should_return_the_same_tree()
        //{
        //    var tree = ImMap<string>.Empty
        //        .AddOrUpdate(1, "a").AddOrUpdate(2, "b").AddOrUpdate(3, "c").AddOrUpdate(4, "d");

        //    var updatedTree = tree.Update(5, "e");

        //    Assert.AreSame(tree, updatedTree);
        //}

        //[Test]
        //public void Can_use_int_key_tree_to_represent_general_HashTree_with_possible_hash_conflicts()
        //{
        //    var tree = ImMap<KeyValuePair<Type, string>[]>.Empty;

        //    var key = typeof(ImMapTests);
        //    var keyHash = key.GetHashCode();
        //    var value = "test";

        //    KeyValuePair<Type, string>[] Update(int _, KeyValuePair<Type, string>[] oldValue, KeyValuePair<Type, string>[] newValue)
        //    {
        //        var newItem = newValue[0];
        //        var oldItemCount = oldValue.Length;
        //        for (var i = 0; i < oldItemCount; i++)
        //        {
        //            if (oldValue[i].Key == newItem.Key)
        //            {
        //                var updatedItems = new KeyValuePair<Type, string>[oldItemCount];
        //                Array.Copy(oldValue, updatedItems, updatedItems.Length);
        //                updatedItems[i] = newItem;
        //                return updatedItems;
        //            }
        //        }

        //        var addedItems = new KeyValuePair<Type, string>[oldItemCount + 1];
        //        Array.Copy(oldValue, addedItems, addedItems.Length);
        //        addedItems[oldItemCount] = newItem;
        //        return addedItems;
        //    }

        //    tree = tree.AddOrUpdate(keyHash, new[] {new KeyValuePair<Type, string>(key, value)}, Update);
        //    tree = tree.AddOrUpdate(keyHash, new[] {new KeyValuePair<Type, string>(key, value)}, Update);

        //    string result = null;

        //    var items = tree.GetValueOrDefault(keyHash);
        //    if (items != null)
        //    {
        //        var firstItem = items[0];
        //        if (firstItem.Key == key)
        //            result = firstItem.Value;
        //        else if (items.Length > 1)
        //        {
        //            for (var i = 1; i < items.Length; i++)
        //            {
        //                if (items[i].Key == key)
        //                {
        //                    result = items[i].Value;
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    Assert.That(result, Is.EqualTo("test"));
        //}

        //[Test]
        //public void Remove_from_one_node_tree()
        //{
        //    var tree = ImMap<string>.Empty.AddOrUpdate(0, "a");

        //    tree = tree.Remove(0);

        //    Assert.That(tree.IsEmpty, Is.True);
        //}

        //[Test]
        //public void Remove_from_Empty_tree_should_not_throw()
        //{
        //    var tree = ImMap<string>.Empty.Remove(0);
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
