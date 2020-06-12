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
            Assert.AreEqual(1, m.GetValueOrDefault(1)); 
            Assert.IsInstanceOf<ImMapEntry<int>>(m);

            m = m.AddOrUpdate(2, 2);
            Assert.AreEqual(2, m.GetValueOrDefault(2));
            Assert.IsInstanceOf<ImMapLeafs2<int>>(m);

            m = m.AddOrUpdate(3, 3);
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.IsInstanceOf<ImMapLeafs3<int>>(m);



            //    .AddOrUpdate(2, 2)
            //    .AddOrUpdate(3, 3);

            //Assert.AreEqual(11, t.GetValueOrDefault(1));
            //Assert.AreEqual(22, t.GetValueOrDefault(2));
            //Assert.AreEqual(33, t.GetValueOrDefault(3));
        }
    }
}