using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using CsCheck;

namespace ImTools.Experimental.UnitTests
{
    [TestFixture]
    public class ImHashMap234Tests
    {
        [Test]
        public void Adding_hash_and_keys_from_1_to_10_and_checking_the_tree_shape_on_each_addition()
        {
            var m = ImHashMap234<int, string>.Empty;
            Assert.AreEqual(null, m.GetValueOrDefault(0));
            Assert.AreEqual(null, m.GetValueOrDefault(13));
            Assert.IsEmpty(m.Enumerate());

            m = m.AddOrUpdate(1, "a");
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1 }, m.Enumerate().Select(x => x.Key));

            Assert.AreSame(m, m.AddOrKeep(1, "aa"));

            var mr = m.Remove(1);
            Assert.AreSame(ImHashMap234<int, string>.Empty, mr);

            m = m.AddOrUpdate(2, "b");
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, m.Enumerate().Select(x => x.Key));

            Assert.AreSame(m, m.AddOrKeep(1, "aa").AddOrKeep(2, "bb"));
            Assert.AreSame(m, m.Remove(0));
            mr = m.Remove(2);
            Assert.AreEqual("a", mr.GetValueOrDefault(1));

            m = m.AddOrUpdate(3, "c");
            Assert.AreEqual("c",  m.GetValueOrDefault(3));
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, m.Enumerate().Select(x => x.Key));

            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            Assert.AreSame(m, m.Remove(0));
            mr = m.Remove(2);
            Assert.AreEqual("a", mr.GetValueOrDefault(1));
            Assert.AreEqual("c", mr.GetValueOrDefault(3));

            m = m.AddOrUpdate(4, "d");
            Assert.AreEqual("c",  m.GetValueOrDefault(3));
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual("d",  m.GetValueOrDefault(4));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, m.Enumerate().Select(x => x.Key));

            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            Assert.AreSame(m, m.Remove(0));

            m = m.AddOrUpdate(5, "e");
            Assert.AreEqual("c",  m.GetValueOrDefault(3));
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual("d",  m.GetValueOrDefault(4));
            Assert.AreEqual("e",  m.GetValueOrDefault(5));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, m.Enumerate().Select(x => x.Key));

            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            Assert.AreSame(m, m.Remove(0));

            m = m.AddOrUpdate(6, "6");
            Assert.AreEqual("6",  m.GetValueOrDefault(6));
            Assert.AreEqual("e",  m.GetValueOrDefault(5));
            Assert.AreEqual("d",  m.GetValueOrDefault(4));
            Assert.AreEqual("c",  m.GetValueOrDefault(3));
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, m.Enumerate().Select(x => x.Key));

            m = m.AddOrUpdate(7, "7");
            Assert.AreEqual("7",  m.GetValueOrDefault(7));
            m = m.AddOrUpdate(8, "8");
            Assert.AreEqual("8",  m.GetValueOrDefault(8));
            m = m.AddOrUpdate(9, "9");
            Assert.AreEqual("9",  m.GetValueOrDefault(9));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, m.Enumerate().Select(x => x.Key));


            m = m.AddOrUpdate(10, "10");
            Assert.AreEqual("10", m.GetValueOrDefault(10));
            Assert.AreEqual("9",  m.GetValueOrDefault(9));
            Assert.AreEqual("8",  m.GetValueOrDefault(8));
            Assert.AreEqual("7",  m.GetValueOrDefault(7));
            Assert.AreEqual("6",  m.GetValueOrDefault(6));
            Assert.AreEqual("e",  m.GetValueOrDefault(5));
            Assert.AreEqual("d",  m.GetValueOrDefault(4));
            Assert.AreEqual("c",  m.GetValueOrDefault(3));
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(11));
            Assert.AreSame(m, m.AddOrKeep(8, "8!").AddOrKeep(5, "5!").AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, m.Enumerate().Select(x => x.Key));

            m = m.AddOrUpdate(11, "11");
            m = m.AddOrUpdate(12, "12");
            m = m.AddOrUpdate(13, "13");
            Assert.AreEqual("11",  m.GetValueOrDefault(11));
            Assert.AreEqual("12",  m.GetValueOrDefault(12));
            Assert.AreEqual("13",  m.GetValueOrDefault(13));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }, m.Enumerate().Select(x => x.Key));

            m = m.AddOrUpdate(14, "14");
            Assert.AreEqual("14",  m.GetValueOrDefault(14));

            m = m.AddOrUpdate(15, "15");
            m = m.AddOrUpdate(16, "16");
            m = m.AddOrUpdate(17, "17");
            Assert.AreEqual("15",  m.GetValueOrDefault(15));
            Assert.AreEqual("16",  m.GetValueOrDefault(16));
            Assert.AreEqual("17",  m.GetValueOrDefault(17));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }, m.Enumerate().Select(x => x.Key));

            m = m.AddOrUpdate(18, "18");
            Assert.AreEqual("18",  m.GetValueOrDefault(18));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }, m.Enumerate().Select(x => x.Key));
        }

        public class XKey<K> 
        {
            public K Key;
            public XKey(K k) => Key = k;
            public override int GetHashCode() => 1;
            public override bool Equals(object o) => o is XKey<K> x && Key.Equals(x.Key);
        }

        public static XKey<K> Xk<K>(K key) => new XKey<K>(key);

        [Test]
        public void Adding_the_conflicting_keys_should_be_fun()
        {
            var m = ImHashMap234<XKey<int>, string>.Empty;
            Assert.AreEqual(null, m.GetValueOrDefault(Xk(0)));
            Assert.AreEqual(null, m.GetValueOrDefault(Xk(13)));

            m = m.AddOrUpdate(Xk(1), "a");
            m = m.AddOrUpdate(Xk(2), "b");
            
            Assert.IsInstanceOf<ImHashMap234<XKey<int>, string>.ConflictsEntry>(m);
            Assert.AreEqual("a",  m.GetValueOrDefault(Xk(1)));
            Assert.AreEqual("b",  m.GetValueOrDefault(Xk(2)));
            Assert.AreEqual(null, m.GetValueOrDefault(Xk(10)));

            var mr = m.Remove(Xk(1));
            Assert.IsInstanceOf<ImHashMap234<XKey<int>, string>.ValueEntry>(mr);
            Assert.AreEqual(null, mr.GetValueOrDefault(Xk(1)));
            Assert.AreEqual("b",  mr.GetValueOrDefault(Xk(2)));

            m = m.AddOrUpdate(Xk(3), "c");
            mr = m.Remove(Xk(2));
            Assert.IsInstanceOf<ImHashMap234<XKey<int>, string>.ConflictsEntry>(mr);
            Assert.AreEqual("a",  mr.GetValueOrDefault(Xk(1)));
            Assert.AreEqual(null, mr.GetValueOrDefault(Xk(2)));
            Assert.AreEqual("c",  mr.GetValueOrDefault(Xk(3)));
        }

        [Test]
        public void Adding_1000_keys_and_randomly_checking()
        {
            var m = ImHashMap234<int, int>.Empty;
            for (var i = 0; i < 5000; i++)
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
            Assert.AreEqual(0, m.GetValueOrDefault(10000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void Adding_1000_keys_descending_and_randomly_checking()
        {
            var m = ImHashMap234<int, int>.Empty;
            for (var i = 5000 - 1; i >= 0; i--)
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
            Assert.AreEqual(0, m.GetValueOrDefault(10000));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void AddOrUpdate_random_items_and_randomly_checking_CsCheck()
        {
            const int upperBound = 100000;
            Gen.Int[0, upperBound].Array.Sample(items =>
            {
                var m = ImHashMap234<int, int>.Empty;
                foreach (int n in items)
                {
                    m = m.AddOrUpdate(n, n);
                    Assert.AreEqual(n, m.GetValueOrDefault(n));
                }
                
                foreach (int n in items)
                    Assert.AreEqual(n, m.GetValueOrDefault(n));

                // non-existing keys 
                Assert.AreEqual(0, m.GetValueOrDefault(upperBound + 1));
                Assert.AreEqual(0, m.GetValueOrDefault(-1));
            }, 
            size: 5000);
        }

        [Test]
        public void AddOrUpdate_random_items_and_randomly_checking_CsCheck_shrinked()
        {
            const int upperBound = 100000;
            Gen.Int[0, upperBound].Array.Sample(items =>
            {
                var m = ImHashMap234<int, int>.Empty;
                foreach (int n in items)
                {
                    m = m.AddOrUpdate(n, n);
                    Assert.AreEqual(n, m.GetValueOrDefault(n));
                }
                
                for (int i = 0; i < items.Length; ++i)
                {
                    var n = items[i];
                    var x = m.GetValueOrDefault(n);
                    if (x != n)
                    {
                        if (i + 1 != items.Length) 
                            Debug.WriteLine($"Not at end i = {i}");
                        Debug.WriteLine($"Array = {string.Join(", ", items)}");
                    }
                    Assert.AreEqual(n, x);
                }

                // non-existing keys 
                Assert.AreEqual(0, m.GetValueOrDefault(upperBound + 1));
                Assert.AreEqual(0, m.GetValueOrDefault(-1));
            }, 
            size: 5000, seed: "0ZPySr9kwyWr");
        }

        [Test]
        public void AddOrUpdate_problematic_shrinked_set_case1__repeated_item()
        {
            var items = new[] { 85213, 8184, 14819, 38204, 1738, 6752, 38204, 22310, 86961, 33016, 72555, 25102 };

            var m = ImHashMap234<int, int>.Empty;
            foreach (var i in items)
                m = m.AddOrUpdate(i, i);

            foreach (var i in items)
                Assert.AreEqual(i, m.GetValueOrDefault(i));
        }

        [Test]
        public void AddOrUpdate_problematic_shrinked_set_case2__repeated_hash_erased()
        {
            var items = new[] {
                45751, 6825, 44599, 79942, 73380, 8408, 34126, 51224, 14463, 71529, 46775, 74893, 80615, 78504, 29401, 60789, 14050, 
                67780, 52369, 16486, 48124, 46939, 43229, 58359, 61378, 31969, 79905, 37405, 37259, 66683, 58359, 87401, 42175 };

            var m = ImHashMap234<int, int>.Empty;
            foreach (var i in items)
            {
                m = m.AddOrUpdate(i, i);
                Assert.AreEqual(i, m.GetValueOrDefault(i));
            }

            foreach (var i in items)
                Assert.AreEqual(i, m.GetValueOrDefault(i));
        }

        [Test]
        public void AddOrKeep_random_items_and_randomly_checking_CsCheck()
        {
            const int upperBound = 100000;
            Gen.Int[0, upperBound].Array.Sample(items =>
            {
                var m = ImHashMap234<int, int>.Empty;
                foreach (int n in items)
                {
                    m = m.AddOrKeep(n, n);
                    Assert.AreEqual(n, m.GetValueOrDefault(n));
                }
                
                foreach (int n in items)
                    Assert.AreEqual(n, m.GetValueOrDefault(n));

                // non-existing keys 
                Assert.AreEqual(0, m.GetValueOrDefault(upperBound + 1));
                Assert.AreEqual(0, m.GetValueOrDefault(-1));
            }, 
            size: 5000);
        }
    }
}