using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class ImListTests
    {
        [Test]
        public void Can_perepend_and_reverse_the_list()
        {
            var list = ImList<int>.Empty.Prep(1).Prep(2).Prep(3);
            CollectionAssert.AreEqual(new[] {3, 2, 1}, list.Enumerate());

            var revList = list.Reverse();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, revList.Enumerate());
        }

        [Test]
        public void Can_index_the_fold()
        {
            var list = ImList<string>.Empty.Prep("a").Prep("b").Prep("c");

            var iis = list.To(ImList<int>.Empty, (s, i, _) => _.Prep(i)).Reverse();

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, iis.Enumerate());
        }

        [Test]
        public void Can_map_one_list_to_another()
        {
            var list = ImList<int>.Empty.Prep(3).Prep(2).Prep(1);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.Enumerate());

            var result = list.Map(n => n + 42);
            CollectionAssert.AreEqual(new[] { 43, 44, 45 }, result.Enumerate());
        }

        [Test]
        public void Can_map_one_list_to_another_with_index()
        {
            var list = ImList<int>.Empty.Prep(3).Prep(2).Prep(1);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.Enumerate());

            var result = list.Map((n, i) => n + 42);
            CollectionAssert.AreEqual(new[] { 43, 44, 45 }, result.Enumerate());
        }


        [Test]
        public void Can_convert_list_to_array()
        {
            var list = ImList<int>.Empty.Prep(3).Prep(2).Prep(1);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, (int[])list);
        }
    }
}
