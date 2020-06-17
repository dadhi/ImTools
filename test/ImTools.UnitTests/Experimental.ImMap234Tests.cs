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
            Assert.IsInstanceOf<ImMap<int>.Entry>(m);
            Assert.AreEqual(1, m.GetValueOrDefault(1));

            m = m.AddOrUpdate(2, 2);
            Assert.IsInstanceOf<ImMap<int>.Leaf2>(m);
            Assert.AreEqual(2, m.GetValueOrDefault(2));

            m = m.AddOrUpdate(3, 3);
            Assert.IsInstanceOf<ImMap<int>.Leaf3>(m);
            Assert.AreEqual(3, m.GetValueOrDefault(3));

            m = m.AddOrUpdate(4, 4);
            Assert.IsInstanceOf<ImMap<int>.Branch2>(m);
            Assert.IsInstanceOf<ImMap<int>.Entry>(((ImMap<int>.Branch2)m).Br0);
            Assert.IsInstanceOf<ImMap<int>.Leaf2>(((ImMap<int>.Branch2)m).Br1);
            Assert.AreEqual(4, m.GetValueOrDefault(4));
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.AreEqual(2, m.GetValueOrDefault(2));
            Assert.AreEqual(1, m.GetValueOrDefault(1));

            m = m.AddOrUpdate(5, 5);
            Assert.IsInstanceOf<ImMap<int>.Branch2>(m);
            Assert.IsNotInstanceOf<ImMap<int>.Branch3>(m);
            Assert.IsInstanceOf<ImMap<int>.Entry>(((ImMap<int>.Branch2)m).Br0);
            Assert.IsInstanceOf<ImMap<int>.Leaf3>(((ImMap<int>.Branch2)m) .Br1);
            Assert.AreEqual(5, m.GetValueOrDefault(5));

            m = m.AddOrUpdate(6, 6);
            Assert.IsInstanceOf<ImMap<int>.Branch3>(m);
            Assert.IsInstanceOf<ImMap<int>.Entry>(((ImMap<int>.Branch3)m).Br0);
            Assert.IsInstanceOf<ImMap<int>.Entry>(((ImMap<int>.Branch3)m).Br1);
            Assert.IsInstanceOf<ImMap<int>.Leaf2>(((ImMap<int>.Branch3)m).Br2);
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.AreEqual(5, m.GetValueOrDefault(5));
            Assert.AreEqual(6, m.GetValueOrDefault(6));

            m = m.AddOrUpdate(7, 7);
            Assert.IsInstanceOf<ImMap<int>.Branch3>(m);
            Assert.IsInstanceOf<ImMap<int>.Entry>(((ImMap<int>.Branch3)m).Br0);
            Assert.IsInstanceOf<ImMap<int>.Entry>(((ImMap<int>.Branch3)m).Br1);
            Assert.IsInstanceOf<ImMap<int>.Leaf3>(((ImMap<int>.Branch3)m).Br2);
            Assert.AreEqual(7, m.GetValueOrDefault(7));

            m = m.AddOrUpdate(8, 8);
            Assert.IsInstanceOf<ImMap<int>.Branch2>(m);
            Assert.IsInstanceOf<ImMap<int>.Branch2>(((ImMap<int>.Branch2)m)   .Br0);
            Assert.IsNotInstanceOf<ImMap<int>.Branch3>(((ImMap<int>.Branch2)m).Br0);
            var right = (ImMap<int>.Branch2)((ImMap<int>.Branch2)m).Br1;
            Assert.IsInstanceOf<ImMap<int>.Branch2>(right);
            Assert.IsNotInstanceOf<ImMap<int>.Branch3>(right);
            Assert.IsInstanceOf<ImMap<int>.Entry>(right.Br0);
            Assert.IsInstanceOf<ImMap<int>.Leaf2>(right.Br1);
            Assert.AreEqual(8, m.GetValueOrDefault(8));

            m = m.AddOrUpdate(9, 9);
            right = (ImMap<int>.Branch2)((ImMap<int>.Branch2)m).Br1;
            Assert.IsInstanceOf<ImMap<int>.Leaf3>(right.Br1);
            Assert.AreEqual(9, m.GetValueOrDefault(9));

            m = m.AddOrUpdate(10, 10);
            right = (ImMap<int>.Branch2)((ImMap<int>.Branch2)m).Br1;
            Assert.IsInstanceOf<ImMap<int>.Entry>(right        .Br1);
            Assert.AreEqual(10, m.GetValueOrDefault(10));
        }
    }
}