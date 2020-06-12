using NUnit.Framework;

namespace ImTools.Experimental.Tree234.UnitTests
{
    [TestFixture]
    public class ImMap234Tests
    {
        [Test]
        public void Adding_keys_from_1_to_10_and_checking_the_tree_shape_on_each_addition()
        {
            var m = ImMap<int>.Empty;
            Assert.AreEqual(default(int), m.GetValueOrDefault(0));
            Assert.AreEqual(default(int), m.GetValueOrDefault(13));

            m = m.AddOrUpdate(1, 1);
            Assert.IsInstanceOf<ImMapEntry<int>>(m);
            Assert.AreEqual(1, m.GetValueOrDefault(1));

            m = m.AddOrUpdate(2, 2);
            Assert.IsInstanceOf<ImMapLeafs2<int>>(m);
            Assert.AreEqual(2, m.GetValueOrDefault(2));

            m = m.AddOrUpdate(3, 3);
            Assert.IsInstanceOf<ImMapLeafs3<int>>(m);
            Assert.AreEqual(3, m.GetValueOrDefault(3));

            m = m.AddOrUpdate(4, 4);
            Assert.IsInstanceOf<ImMapBranch2<int>>(m);
            Assert.IsInstanceOf<ImMapEntry<int>>(((ImMapBranch2<int>)m).Branch0);
            Assert.IsInstanceOf<ImMapLeafs2<int>>(((ImMapBranch2<int>) m).Branch1);
            Assert.AreEqual(4, m.GetValueOrDefault(4));
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.AreEqual(2, m.GetValueOrDefault(2));
            Assert.AreEqual(1, m.GetValueOrDefault(1));

            m = m.AddOrUpdate(5, 5);
            Assert.IsInstanceOf<ImMapBranch2<int>>(m);
            Assert.IsNotInstanceOf<ImMapBranch3<int>>(m);
            Assert.IsInstanceOf<ImMapEntry<int>>(((ImMapBranch2<int>)m).Branch0);
            Assert.IsInstanceOf<ImMapLeafs3<int>>(((ImMapBranch2<int>)m).Branch1);
            Assert.AreEqual(5, m.GetValueOrDefault(5));

            m = m.AddOrUpdate(6, 6);
            Assert.IsInstanceOf<ImMapBranch3<int>>(m);
            Assert.IsInstanceOf<ImMapEntry<int>>(((ImMapBranch3<int>)m).Branch0);
            Assert.IsInstanceOf<ImMapEntry<int>>(((ImMapBranch3<int>)m).Branch1);
            Assert.IsInstanceOf<ImMapLeafs2<int>>(((ImMapBranch3<int>)m).Branch2);
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.AreEqual(5, m.GetValueOrDefault(5));
            Assert.AreEqual(6, m.GetValueOrDefault(6));

        }
    }
}