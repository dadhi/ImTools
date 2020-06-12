using NUnit.Framework;

namespace ImTools.Experimental.ImMap234.UnitTests
{
    [TestFixture]
    public class ImMap234Tests
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
    }
}