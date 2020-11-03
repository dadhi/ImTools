using System;
using System.Linq;
using NUnit.Framework;

namespace ImTools.Experimental.UnitTests
{
    [TestFixture]
    public class ImMap234Tests
    {
        [Test]
        public void Adding_hash_and_keys_from_1_to_10_and_checking_the_tree_shape_on_each_addition()
        {
            var m = ImHashMap234<int, string>.Empty;
            Assert.AreEqual(null, m.GetValueOrDefault(0));
            Assert.AreEqual(null, m.GetValueOrDefault(13));

            m = m.AddOrUpdate(1, "a");
            Assert.IsInstanceOf<ImHashMap234<int, string>.ValueEntry>(m);
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
        }

        [Test]
        public void Adding_keys_from_1_to_10_and_checking_the_tree_shape_on_each_addition()
        {
            var m = ImMap234<int>.Empty;
            Assert.AreEqual(default(int), m.GetValueOrDefault(0));
            Assert.AreEqual(default(int), m.GetValueOrDefault(13));

            m = m.AddOrUpdate(1, 1);
            Assert.IsInstanceOf<ImMap234<int>.Entry>(m);
            Assert.AreEqual(1, m.GetValueOrDefault(1));

            m = m.AddOrUpdate(2, 2);
            Assert.IsInstanceOf<ImMap234<int>.Leaf2>(m);
            Assert.AreEqual(2, m.GetValueOrDefault(2));

            m = m.AddOrUpdate(3, 3);
            Assert.IsInstanceOf<ImMap234<int>.Leaf3>(m);
            Assert.AreEqual(3, m.GetValueOrDefault(3));

            m = m.AddOrUpdate(4, 4);
            Assert.IsInstanceOf<ImMap234<int>.Leaf4>(m);
            Assert.AreEqual(4, m.GetValueOrDefault(4));
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.AreEqual(2, m.GetValueOrDefault(2));
            Assert.AreEqual(1, m.GetValueOrDefault(1));

            m = m.AddOrUpdate(5, 5);
            Assert.IsInstanceOf<ImMap234<int>.Leaf5>(m);
            Assert.AreEqual(5, m.GetValueOrDefault(5));

            m = m.AddOrUpdate(6, 6);
            Assert.IsInstanceOf<ImMap234<int>.Branch2>(m);
            Assert.IsInstanceOf<ImMap234<int>.Leaf3>(((ImMap234<int>.Branch2)m).Left);
            Assert.IsInstanceOf<ImMap234<int>.Leaf2>(((ImMap234<int>.Branch2)m).Right);
            Assert.AreEqual(3, m.GetValueOrDefault(3));
            Assert.AreEqual(5, m.GetValueOrDefault(5));
            Assert.AreEqual(6, m.GetValueOrDefault(6));

            CollectionAssert.AreEqual(Enumerable.Range(1, 6), m.Enumerate().Select(x => x.Value));

            m = m.AddOrUpdate(7, 7);
            Assert.IsInstanceOf<ImMap234<int>.Branch2>(m);
            Assert.AreEqual(7, m.GetValueOrDefault(7));

            m = m.AddOrUpdate(8, 8);
            Assert.IsInstanceOf<ImMap234<int>.Branch2>(m);
            Assert.AreEqual(8, m.GetValueOrDefault(8));

            m = m.AddOrUpdate(9, 9);
            Assert.AreEqual(9, m.GetValueOrDefault(9));

            m = m.AddOrUpdate(10, 10);
            Assert.AreEqual(10, m.GetValueOrDefault(10));

            CollectionAssert.AreEqual(Enumerable.Range(1, 10), m.Enumerate().Select(x => x.Value));
        }

        [Test]
        public void Adding_1000_keys_and_randomly_checking()
        {
            var m = ImMap234<int>.Empty;
            for (var i = 0; i < 1000; i++)
            {
                m = m.AddOrUpdate(i, i);
            }

            Assert.AreEqual(1, m.GetValueOrDefault(1));
            Assert.AreEqual(0, m.GetValueOrDefault(0));
            Assert.AreEqual(13, m.GetValueOrDefault(13));
            Assert.AreEqual(66, m.GetValueOrDefault(66));
            Assert.AreEqual(555, m.GetValueOrDefault(555));
            Assert.AreEqual(333, m.GetValueOrDefault(333));
            Assert.AreEqual(999, m.GetValueOrDefault(999));

            // non-existing keys 
            Assert.AreEqual(0, m.GetValueOrDefault(1000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void Adding_1000_keys_descending_and_randomly_checking()
        {
            var m = ImMap234<int>.Empty;
            for (var i = 1000 - 1; i >= 0; i--)
            {
                m = m.AddOrUpdate(i, i);
            }

            Assert.AreEqual(1, m.GetValueOrDefault(1));
            Assert.AreEqual(0, m.GetValueOrDefault(0));
            Assert.AreEqual(13, m.GetValueOrDefault(13));
            Assert.AreEqual(66, m.GetValueOrDefault(66));
            Assert.AreEqual(555, m.GetValueOrDefault(555));
            Assert.AreEqual(333, m.GetValueOrDefault(333));
            Assert.AreEqual(999, m.GetValueOrDefault(999));

            // non-existing keys 
            Assert.AreEqual(0, m.GetValueOrDefault(1000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void AddOrUpdate_1000_keys_randomly_and_randomly_checking()
        {
            var rnd = new Random();

            var m = ImMap234<int>.Empty;
            for (var i = 0; i < 1000; i++)
            {
                var n = rnd.Next(0, 10000);
                m = m.AddOrUpdate(n, n);
                Assert.AreEqual(n, m.GetValueOrDefault(n));
            }

            // non-existing keys 
            Assert.AreEqual(0, m.GetValueOrDefault(10000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void AddOrKeep_1000_keys_randomly_and_randomly_checking()
        {
            var rnd = new Random();

            var m = ImMap234<int>.Empty;
            for (var i = 0; i < 1000; i++)
            {
                var n = rnd.Next(0, 10000);
                m = m.AddOrKeep(n, n);
                Assert.AreEqual(n, m.GetValueOrDefault(n));
            }

            // non-existing keys 
            Assert.AreEqual(0, m.GetValueOrDefault(10000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }
    }
}