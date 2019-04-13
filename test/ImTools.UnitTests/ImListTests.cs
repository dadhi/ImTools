using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class ImListTests
    {
        [Test]
        public void Can_push_into_and_reverse_the_list()
        {
            var list = ImList<int>.Empty.Push(1).Push(2).Push(3);
            CollectionAssert.AreEqual(new[] {3, 2, 1}, list.Enumerate());

            var revList = list.Reverse();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, revList.Enumerate());
        }

        [Test]
        public void Can_index_the_fold()
        {
            var list = ImList<string>.Empty.Push("a").Push("b").Push("c");

            var iis = list.Fold(ImList<int>.Empty, (s, i, _) => _.Push(i)).Reverse();

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, iis.Enumerate());
        }

        [Test]
        public void Can_map_one_list_to_another()
        {
            var list = ImList<int>.Empty.Push(3).Push(2).Push(1);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.Enumerate());

            var result = list.Map(n => n + 42);
            CollectionAssert.AreEqual(new[] { 43, 44, 45 }, result.Enumerate());
        }

        [Test]
        public void Can_map_one_list_to_another_with_index()
        {
            var list = ImList<int>.Empty.Push(3).Push(2).Push(1);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.Enumerate());

            var result = list.Map((n, i) => n + 42);
            CollectionAssert.AreEqual(new[] { 43, 44, 45 }, result.Enumerate());
        }

        [Test]
        public void Can_convert_list_to_array()
        {
            var list = ImList<int>.Empty.Push(3).Push(2).Push(1);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.ToArray());
        }
    }
}
