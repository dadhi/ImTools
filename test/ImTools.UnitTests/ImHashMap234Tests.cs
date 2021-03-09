using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using CsCheck;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class ImHashMap234Tests
    {
        [Test]
        public void Adding_hash_and_keys_and_checking_the_tree_shape_on_each_addition()
        {
            var m = ImHashMap<int, string>.Empty;
            Assert.AreEqual(null, m.GetValueOrDefault(0));
            Assert.AreEqual(null, m.GetValueOrDefault(13));
            Assert.IsEmpty(m.Enumerate());
            Assert.AreEqual(0, m.Count());

            m = m.AddOrUpdate(1, "a");
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(1, m.Count());

            Assert.AreSame(m, m.AddOrKeep(1, "aa"));

            var mr = m.Remove(1);
            Assert.AreSame(ImHashMap<int, string>.Empty, mr);
            Assert.AreEqual(0, mr.Count());

            m = m.AddOrUpdate(2, "b");
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(2, m.Count());

            Assert.AreSame(m, m.AddOrKeep(1, "aa").AddOrKeep(2, "bb"));
            Assert.AreSame(m, m.Remove(0));
            mr = m.Remove(2);
            Assert.AreEqual("a", mr.GetValueOrDefault(1));
            Assert.AreEqual(1, mr.Count());

            m = m.AddOrUpdate(3, "c");
            Assert.AreEqual("c",  m.GetValueOrDefault(3));
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(3, m.Count());

            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            Assert.AreSame(m, m.Remove(0));
            mr = m.Remove(2);
            Assert.AreEqual("a", mr.GetValueOrDefault(1));
            Assert.AreEqual("c", mr.GetValueOrDefault(3));
            Assert.AreEqual(2, mr.Count());

            m = m.AddOrUpdate(4, "d");
            Assert.AreEqual("c",  m.GetValueOrDefault(3));
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual("d",  m.GetValueOrDefault(4));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(4, m.Count());

            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            Assert.AreSame(m, m.Remove(0));

            m = m.AddOrUpdate(5, "e");
            Assert.AreEqual("c",  m.GetValueOrDefault(3));
            Assert.AreEqual("b",  m.GetValueOrDefault(2));
            Assert.AreEqual("a",  m.GetValueOrDefault(1));
            Assert.AreEqual("d",  m.GetValueOrDefault(4));
            Assert.AreEqual("e",  m.GetValueOrDefault(5));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(5, m.Count());

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
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, m.Enumerate().Select(x => x.Hash));

            m = m.AddOrUpdate(7, "7");
            Assert.AreEqual("7",  m.GetValueOrDefault(7));
            m = m.AddOrUpdate(8, "8");
            Assert.AreEqual("8",  m.GetValueOrDefault(8));
            m = m.AddOrUpdate(9, "9");
            Assert.AreEqual("9",  m.GetValueOrDefault(9));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, m.Enumerate().Select(x => x.Hash));


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
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, m.Enumerate().Select(x => x.Hash));

            m = m.AddOrUpdate(11, "11");
            m = m.AddOrUpdate(12, "12");
            m = m.AddOrUpdate(13, "13");
            Assert.AreEqual("11",  m.GetValueOrDefault(11));
            Assert.AreEqual("12",  m.GetValueOrDefault(12));
            Assert.AreEqual("13",  m.GetValueOrDefault(13));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }, m.Enumerate().Select(x => x.Hash));

            m = m.AddOrUpdate(14, "14");
            Assert.AreEqual("14",  m.GetValueOrDefault(14));

            m = m.AddOrUpdate(15, "15");
            m = m.AddOrUpdate(16, "16");
            m = m.AddOrUpdate(17, "17");
            Assert.AreEqual("15",  m.GetValueOrDefault(15));
            Assert.AreEqual("16",  m.GetValueOrDefault(16));
            Assert.AreEqual("17",  m.GetValueOrDefault(17));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }, m.Enumerate().Select(x => x.Hash));

            m = m.AddOrUpdate(18, "18");
            Assert.AreEqual("18",  m.GetValueOrDefault(18));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }, m.Enumerate().Select(x => x.Hash));

            var r = m.Remove(18).Remove(17).Remove(16);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, r.Enumerate().Select(x => x.Hash));
            Assert.IsNull(r.GetValueOrDefault(16));

            var rr = r.Remove(16);
            Assert.AreSame(r, rr);

            m = m.AddOrUpdate(18, "18");
            m = m.AddOrKeep(18, "18");
            Assert.AreEqual("18",  m.GetValueOrDefault(18));

            m = m.AddOrUpdate(19, "19").AddOrUpdate(20, "20").AddOrUpdate(21, "21").AddOrUpdate(22, "22").AddOrUpdate(23, "23");
            rr = m.Remove(25).Remove(21);
            Assert.IsNull(rr.GetValueOrDefault(21));
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
            var m = ImHashMap<XKey<int>, string>.Empty;
            Assert.AreEqual(null, m.GetValueOrDefault(Xk(0)));
            Assert.AreEqual(null, m.GetValueOrDefault(Xk(13)));

            m = m.AddOrUpdate(Xk(1), "a");
            m = m.AddOrUpdate(Xk(2), "b");
            
            Assert.IsInstanceOf<HashConflictKeyValuesEntry<XKey<int>, string>>(m);
            Assert.AreEqual("a",  m.GetValueOrDefault(Xk(1)));
            Assert.AreEqual("b",  m.GetValueOrDefault(Xk(2)));
            Assert.AreEqual(null, m.GetValueOrDefault(Xk(10)));

            var mr = m.Remove(Xk(1));
            Assert.IsInstanceOf<ImHashMapEntry<XKey<int>, string>>(mr);
            Assert.AreEqual(null, mr.GetValueOrDefault(Xk(1)));
            Assert.AreEqual("b",  mr.GetValueOrDefault(Xk(2)));

            m = m.AddOrUpdate(Xk(3), "c");
            mr = m.Remove(Xk(2));
            Assert.IsInstanceOf<HashConflictKeyValuesEntry<XKey<int>, string>>(mr);
            Assert.AreEqual("a",  mr.GetValueOrDefault(Xk(1)));
            Assert.AreEqual(null, mr.GetValueOrDefault(Xk(2)));
            Assert.AreEqual("c",  mr.GetValueOrDefault(Xk(3)));
        }

        [Test]
        public void Adding_1000_keys_and_randomly_checking()
        {
            var m = ImHashMap<int, int>.Empty;
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
            var m = ImHashMap<int, int>.Empty;
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
                var m = ImHashMap<int, int>.Empty;
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
                var m = ImHashMap<int, int>.Empty;
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

            var m = ImHashMap<int, int>.Empty;
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

            var m = ImHashMap<int, int>.Empty;
            foreach (var i in items)
            {
                m = m.AddOrUpdate(i, i);
                Assert.AreEqual(i, m.GetValueOrDefault(i));
            }

            foreach (var i in items)
                Assert.AreEqual(i, m.GetValueOrDefault(i));
        }

        [Test]
        public void AddOrUpdate_problematic_shrinked_set_case3()
        {
            var items = new[] { 87173, 99053, 63922, 20879, 77178, 95518, 16692, 60819, 29881, 69987, 24798, 67743 };

            var m = ImHashMap<int, int>.Empty;
            foreach (var i in items)
                m = m.AddOrUpdate(i, i);

            foreach (var i in items)
                Assert.AreEqual(i, m.GetValueOrDefault(i));
        }

        [Test]
        public void AddOrUpdate_problematic_shrinked_set_case4()
        {
            var items = new[] { 78290, 97898, 23194, 12403, 27002, 78600, 92105, 76902, 90802, 84883, 78290, 18374 };

            var m = ImHashMap<int, int>.Empty;
            foreach (var i in items)
                m = m.AddOrUpdate(i, i);

            foreach (var i in items)
                Assert.AreEqual(i, m.GetValueOrDefault(i));
        }

        [Test]
        public void Enumerate_should_work_for_the_randomized_input()
        {
            var uniqueItems = new[] {
                45751, 6825, 44599, 79942, 73380, 8408, 34126, 51224, 14463, 71529, 46775, 74893, 80615, 78504, 29401, 60789, 14050, 
                67780, 52369, 16486, 48124, 46939, 43229, 58359, 61378, 31969, 79905, 37405, 37259, 66683, 87401, 42175 };

            var m = ImHashMap<int, int>.Empty;
            foreach (var i in uniqueItems)
                m = m.AddOrUpdate(i, i);

            CollectionAssert.AreEqual(uniqueItems.OrderBy(x => x), m.Enumerate().Select(x => x.Hash));
        }

        [Test]
        public void Enumerate_should_work_for_the_randomized_input_2()
        {
            var uniqueItems = new[] {
                17883, 23657, 24329, 29524, 55791, 66175, 67389, 74867, 74946, 81350, 94477, 70414, 26499 }; 

            var m = ImHashMap<int, int>.Empty;
            foreach (var i in uniqueItems)
                m = m.AddOrUpdate(i, i);

            CollectionAssert.AreEqual(uniqueItems.OrderBy(x => x), m.Enumerate().ToArray().Select(x => x.Hash));
        }

        [Test]
        public void Enumerate_should_work_for_the_randomized_input_3()
        {
            var uniqueItems = new int[] { 65347, 87589, 89692, 92562, 97319, 58955 };

            var m = ImHashMap<int, int>.Empty;
            foreach (var i in uniqueItems)
                m = m.AddOrUpdate(i, i);

            CollectionAssert.AreEqual(uniqueItems.OrderBy(x => x), m.Enumerate().ToArray().Select(x => x.Hash));
        }

        [Test]
        public void Enumerate_should_work_for_predefined_map_shape()
        {
            //                  20           40    
            //         /               |            \
            //    10    13            30             50
            //   /    |    \        /    \         /    \
            // 8 9  11 12  14 15 25 26  35 36   45 46   55 56
            //                  
            var m = new ImHashMap<int, int>.RightyBranch3(
                new ImHashMap<int, int>.RightyBranch3(
                    new ImHashMap<int, int>.Leaf2(new ImHashMapEntry<int, int>(8, 8), new ImHashMapEntry<int, int>(9, 9)),
                    new ImHashMapEntry<int, int>(10, 10),
                    new ImHashMap<int, int>.Branch2(
                        new ImHashMap<int, int>.Leaf2(new ImHashMapEntry<int, int>(11, 11), new ImHashMapEntry<int, int>(12, 12)),
                        new ImHashMapEntry<int, int>(13, 13),
                        new ImHashMap<int, int>.Leaf2(new ImHashMapEntry<int, int>(14, 14), new ImHashMapEntry<int, int>(15, 15))
                    )),
                new ImHashMapEntry<int, int>(20, 20),
                new ImHashMap<int, int>.Branch2(
                    new ImHashMap<int, int>.Branch2(
                        new ImHashMap<int, int>.Leaf2(new ImHashMapEntry<int, int>(25, 25), new ImHashMapEntry<int, int>(26, 26)),
                        new ImHashMapEntry<int, int>(30, 30),
                        new ImHashMap<int, int>.Leaf2(new ImHashMapEntry<int, int>(35, 35), new ImHashMapEntry<int, int>(36, 36))
                    ),
                    new ImHashMapEntry<int, int>(40, 40),
                    new ImHashMap<int, int>.Branch2(
                        new ImHashMap<int, int>.Leaf2(new ImHashMapEntry<int, int>(45, 45), new ImHashMapEntry<int, int>(46, 46)),
                        new ImHashMapEntry<int, int>(50, 50),
                        new ImHashMap<int, int>.Leaf2(new ImHashMapEntry<int, int>(55, 55), new ImHashMapEntry<int, int>(56, 56))
                ))
            );

            CollectionAssert.AreEqual(
                new[] { 8, 9, 10, 11, 12, 13, 14, 15, 20, 25, 26, 30, 35, 36, 40, 45, 46, 50, 55, 56 }, 
                m.Enumerate().ToArray().Select(x => x.Hash));
        }

        [Test]
        public void AddOrKeep_random_items_and_randomly_checking_CsCheck()
        {
            const int upperBound = 100000;
            Gen.Int[0, upperBound].Array.Sample(items =>
            {
                var m = ImHashMap<int, int>.Empty;
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
        
        static Gen<ImHashMap<int, int>> GenMap(int upperBound) =>
            Gen.Int[0, upperBound].ArrayUnique.SelectMany(ks =>
                Gen.Int.Array[ks.Length].Select(vs =>
                {
                    var m = ImHashMap<int, int>.Empty;
                    for (int i = 0; i < ks.Length; i++)
                        m = m.AddOrUpdate(ks[i], vs[i]);
                    return m;
                }));

        // https://www.youtube.com/watch?v=G0NUOst-53U&feature=youtu.be&t=1639
        [Test]
        public void AddOrUpdate_metamorphic()
        {
            const int upperBound = 100_000;
            Gen.Select(GenMap(upperBound), Gen.Int[0, upperBound], Gen.Int, Gen.Int[0, upperBound], Gen.Int)
                .Sample(t =>
                {
                    var (m, k1, v1, k2, v2) = t;

                    var m1 = m.AddOrUpdate(k1, v1).AddOrUpdate(k2, v2);

                    var m2 = k1 == k2 ? m.AddOrUpdate(k2, v2) : m.AddOrUpdate(k2, v2).AddOrUpdate(k1, v1);
                    
                    var e1 = m1.Enumerate().OrderBy(i => i.Hash);
                    
                    var e2 = m2.Enumerate().OrderBy(i => i.Hash);

                    CollectionAssert.AreEqual(e1.Select(x => x.Hash), e2.Select(x => x.Hash));
                }, 
                size: 5000);
        }

        [Test]
        public void AddOrUpdate_metamorphic_shrinked()
        {
            const int upperBound = 100_000;
            Gen.Select(GenMap(upperBound), Gen.Int[0, upperBound], Gen.Int, Gen.Int[0, upperBound], Gen.Int)
                .Sample(t =>
                {
                    var (m, k1, v1, k2, v2) = t;

                    var m1 = m.AddOrUpdate(k1, v1).AddOrUpdate(k2, v2);

                    var m2 = k1 == k2 ? m.AddOrUpdate(k2, v2) : m.AddOrUpdate(k2, v2).AddOrUpdate(k1, v1);
                    
                    var e1 = m1.Enumerate().OrderBy(i => i.Hash);
                    
                    var e2 = m2.Enumerate().OrderBy(i => i.Hash);

                    CollectionAssert.AreEqual(e1.Select(x => x.Hash), e2.Select(x => x.Hash));
                }, 
                size: 100, seed: "000027FFpth8");
        }

        [Test]
        public void AddOrUpdate_metamorphic_shrinked_manually_case_1()
        {
            var baseItems = new int[4] { 65347, 87589, 89692, 92562 };

            var m1 = ImHashMap<int, int>.Empty;
            var m2 = ImHashMap<int, int>.Empty;
            foreach (var x in baseItems)
            {
                m1 = m1.AddOrUpdate(x, x);
                m2 = m2.AddOrUpdate(x, x);
            }

            m1 = m1.AddOrUpdate(58955, 42);
            m1 = m1.AddOrUpdate(97319, 43);

            m2 = m2.AddOrUpdate(97319, 43);
            m2 = m2.AddOrUpdate(58955, 42);

            var e1 = m1.Enumerate().OrderBy(i => i.Hash);
            var e2 = m2.Enumerate().OrderBy(i => i.Hash);

            CollectionAssert.AreEqual(e1.Select(x => x.Hash), e2.Select(x => x.Hash));
        }

        [Test]
        public void AddOrUpdate_metamorphic_shrinked_manually_case_2()
        {
            var baseItems = new int[6] {4527, 58235, 65127, 74715, 81974, 89123};

            var m1 = ImHashMap<int, int>.Empty;
            var m2 = ImHashMap<int, int>.Empty;
            foreach (var x in baseItems)
            {
                m1 = m1.AddOrUpdate(x, x);
                m2 = m2.AddOrUpdate(x, x);
            }

            m1 = m1.AddOrUpdate(35206, 42);
            m1 = m1.AddOrUpdate(83178, 43);

            m2 = m2.AddOrUpdate(83178, 43);
            m2 = m2.AddOrUpdate(35206, 42);

            var e1 = m1.Enumerate().OrderBy(i => i.Hash).Select(x => x.Hash).ToArray();
            var e2 = m2.Enumerate().OrderBy(i => i.Hash).Select(x => x.Hash).ToArray();

            CollectionAssert.AreEqual(e1, e2);
        }

        [Test]
        public void AddOrUpdate_metamorphic_shrinked_manually_case_3()
        {
            var baseItems = new int[] { 65347, 87589, 89692, 92562 };

            var m1 = ImHashMap<int, int>.Empty;
            var m2 = ImHashMap<int, int>.Empty;
            foreach (var x in baseItems)
            {
                m1 = m1.AddOrUpdate(x, x);
                m2 = m2.AddOrUpdate(x, x);
            }

            m1 = m1.AddOrUpdate(97319, 42);
            m1 = m1.AddOrUpdate(58955, 43);

            m2 = m2.AddOrUpdate(58955, 43);
            m2 = m2.AddOrUpdate(97319, 42);

            var e1 = m1.Enumerate().ToArray().OrderBy(i => i.Hash).Select(x => x.Hash).ToArray();
            var e2 = m2.Enumerate().ToArray().OrderBy(i => i.Hash).Select(x => x.Hash).ToArray();

            CollectionAssert.AreEqual(e1, e2);
        }


        [Test]
        public void AddOrUpdate_ModelBased()
        {
            const int upperBound = 100000;
            Gen.SelectMany(GenMap(upperBound), m =>
                Gen.Select(Gen.Const(m), Gen.Int[0, upperBound], Gen.Int))
                .Sample(t =>
                {
                    var dic1 = new Dictionary<int, int>();
                    foreach (var entry in t.V0.Enumerate()) 
                        dic1.Add(entry.Hash, entry.Value);

                    dic1[t.V1] = t.V2;
                    var dic2 = new Dictionary<int, int>();
                    foreach (var entry in t.V0.AddOrUpdate(t.V1, t.V2).Enumerate())
                        dic2.Add(entry.Hash, entry.Value);
                    CollectionAssert.AreEqual(dic1, dic2);
                }
                , size: 1000
                , print: t => t + "\n" + string.Join("\n", t.V0.Enumerate()));
        }
    }
}