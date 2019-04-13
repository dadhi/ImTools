using ImTools;
using NUnit.Framework;

namespace Tea.UnitTests
{
    [TestFixture]
    public class ImZipperTests
    {
        [Test]
        public void Map_x()
        {
            var l = ImList.List(5, 6, 7);

            var ml = l.Map((_, i) => i);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, ml.ToArray());

            var z = ImZipper.Zip(5, 6, 7);
            var mz = z.Map((_, i) => i);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, mz.ToArray());
        }

        [Test]
        public void Can_efficiently_update_at_specific_index()
        {
            var z = ImZipper.Zip(1, 5, 10, 15);

            z = z.UpdateAt(1, n => n + 1);
            var s = z.ToString();
            CollectionAssert.AreEqual(new[] { 1, 6, 10, 15 }, z.ToArray());

            z = z.UpdateAt(0, i => i + 1);
            CollectionAssert.AreEqual(new[] { 2, 6, 10, 15 }, z.ToArray());

            z = z.UpdateAt(3, i => i + 1);
            CollectionAssert.AreEqual(new[] { 2, 6, 10, 16 }, z.ToArray());

            var z1 = z.UpdateAt(-5, i => i + 1);
            Assert.AreSame(z, z1);

            var z2 = z1.UpdateAt(5, i => i + 1);
            Assert.AreSame(z1, z2);

            var newList = z.UpdateAt(1, i => i);
            Assert.AreNotSame(z, newList);
            CollectionAssert.AreEqual(new[] { 2, 6, 10, 16 }, z.ToArray());
        }

        [Test]
        public void Can_remove_item_at_index()
        {
            var z = ImZipper.Zip(15, 10, 5, 1);

            z = z.RemoveAt(2);
            CollectionAssert.AreEqual(new[] { 15, 10, 1 }, z.ToArray());

            z = z.RemoveAt(2);
            CollectionAssert.AreEqual(new[] { 15, 10 }, z.ToArray());

            z = z.RemoveAt(0);
            CollectionAssert.AreEqual(new[] { 10 }, z.ToArray());

            var z1 = z.RemoveAt(5);
            Assert.AreSame(z, z1);

            var z2 = z.RemoveAt(-5);
            Assert.AreSame(z, z2);

            z = z.RemoveAt(0);
            CollectionAssert.AreEqual(new int[] {}, z.ToArray());

            var z3 = z.RemoveAt(0);
            Assert.AreSame(z, z3);
        }

        [Test]
        public void Can_map() =>
            CollectionAssert.AreEqual(
                new[] {2, 4, 6, 8},
                ImList.List(1, 2, 3, 4).Map(i => i * 2).ToArray());

        [Test]
        public void Can_map_with_index() =>
            CollectionAssert.AreEqual(
                new[] { 1, 3, 5, 7 },
                ImList.List(1, 2, 3, 4).Map((x, i)  => x + i).ToArray());
    }
}
