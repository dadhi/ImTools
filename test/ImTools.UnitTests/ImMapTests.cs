using System.Linq;
using NUnit.Framework;
using CsCheck;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class ImMapTests
    {
        [Test]
        public void Adding_to_ImMap_and_checking_the_tree_shape_on_each_addition()
        {
            var m = ImHashMap<int, string>.Empty;
            Assert.AreEqual(null, m.GetValueOrDefault(0));
            Assert.AreEqual(null, m.GetValueOrDefault(13));
            Assert.IsEmpty(m.Enumerate());
            Assert.AreEqual(0, m.Count());

            m = m.AddOrUpdate(1, "a");
            Assert.AreEqual("a", m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(1, m.Count());

            Assert.AreSame(m, m.AddOrKeep(1, "aa"));

            var mr = m.Remove(1);
            Assert.AreSame(ImHashMap<int, string>.Empty, mr);
            Assert.AreEqual(0, mr.Count());

            m = m.AddOrUpdate(2, "b");
            Assert.AreEqual("b", m.GetValueOrDefault(2));
            Assert.AreEqual("a", m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(2, m.Count());

            Assert.AreSame(m, m.AddOrKeep(1, "aa").AddOrKeep(2, "bb"));
            Assert.AreSame(m, m.Remove(0));
            mr = m.Remove(2);
            Assert.AreEqual("a", mr.GetValueOrDefault(1));
            Assert.AreEqual(1, mr.Count());

            m = m.AddOrUpdate(3, "c");
            Assert.AreEqual("c", m.GetValueOrDefault(3));
            Assert.AreEqual("b", m.GetValueOrDefault(2));
            Assert.AreEqual("a", m.GetValueOrDefault(1));
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
            Assert.AreEqual("c", m.GetValueOrDefault(3));
            Assert.AreEqual("b", m.GetValueOrDefault(2));
            Assert.AreEqual("a", m.GetValueOrDefault(1));
            Assert.AreEqual("d", m.GetValueOrDefault(4));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(4, m.Count());

            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            Assert.AreSame(m, m.Remove(0));

            m = m.AddOrUpdate(5, "e");
            Assert.AreEqual("c", m.GetValueOrDefault(3));
            Assert.AreEqual("b", m.GetValueOrDefault(2));
            Assert.AreEqual("a", m.GetValueOrDefault(1));
            Assert.AreEqual("d", m.GetValueOrDefault(4));
            Assert.AreEqual("e", m.GetValueOrDefault(5));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(5, m.Count());

            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            Assert.AreSame(m, m.Remove(0));

            m = m.AddOrUpdate(6, "6");
            Assert.AreEqual("6", m.GetValueOrDefault(6));
            Assert.AreEqual("e", m.GetValueOrDefault(5));
            Assert.AreEqual("d", m.GetValueOrDefault(4));
            Assert.AreEqual("c", m.GetValueOrDefault(3));
            Assert.AreEqual("b", m.GetValueOrDefault(2));
            Assert.AreEqual("a", m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(10));
            Assert.AreSame(m, m.AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(6, m.Count());

            m = m.AddOrUpdate(7, "7");
            Assert.AreEqual("7", m.GetValueOrDefault(7));
            m = m.AddOrUpdate(8, "8");
            Assert.AreEqual("8", m.GetValueOrDefault(8));
            m = m.AddOrUpdate(9, "9");
            Assert.AreEqual("9", m.GetValueOrDefault(9));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(9, m.Count());

            mr = m.Remove(9);
            Assert.IsNull(mr.GetValueOrDefault(9));

            m = m.AddOrUpdate(10, "10");
            Assert.AreEqual("10", m.GetValueOrDefault(10));
            Assert.AreEqual("9", m.GetValueOrDefault(9));
            Assert.AreEqual("8", m.GetValueOrDefault(8));
            Assert.AreEqual("7", m.GetValueOrDefault(7));
            Assert.AreEqual("6", m.GetValueOrDefault(6));
            Assert.AreEqual("e", m.GetValueOrDefault(5));
            Assert.AreEqual("d", m.GetValueOrDefault(4));
            Assert.AreEqual("c", m.GetValueOrDefault(3));
            Assert.AreEqual("b", m.GetValueOrDefault(2));
            Assert.AreEqual("a", m.GetValueOrDefault(1));
            Assert.AreEqual(null, m.GetValueOrDefault(11));
            Assert.AreSame(m, m.AddOrKeep(8, "8!").AddOrKeep(5, "5!").AddOrKeep(3, "aa").AddOrKeep(2, "bb").AddOrKeep(1, "cc"));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(10, m.Count());

            m = m.AddOrUpdate(11, "11");
            m = m.AddOrUpdate(12, "12");
            m = m.AddOrUpdate(13, "13");
            Assert.AreEqual("11", m.GetValueOrDefault(11));
            Assert.AreEqual("12", m.GetValueOrDefault(12));
            Assert.AreEqual("13", m.GetValueOrDefault(13));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(13, m.Count());

            m = m.AddOrUpdate(14, "14");
            Assert.AreEqual("14", m.GetValueOrDefault(14));
            Assert.AreEqual(14, m.Count());
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }, m.Enumerate().Select(x => x.Hash));

            m = m.AddOrUpdate(15, "15");
            m = m.AddOrUpdate(16, "16");
            m = m.AddOrUpdate(17, "17");
            Assert.AreEqual("15", m.GetValueOrDefault(15));
            Assert.AreEqual("16", m.GetValueOrDefault(16));
            Assert.AreEqual("17", m.GetValueOrDefault(17));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(17, m.Count());

            m = m.AddOrUpdate(18, "18");
            Assert.AreEqual("18", m.GetValueOrDefault(18));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }, m.Enumerate().Select(x => x.Hash));
            Assert.AreEqual(18, m.Count());

            var r = m.Remove(18).Remove(17).Remove(16);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, r.Enumerate().Select(x => x.Hash));
            Assert.IsNull(r.GetValueOrDefault(16));

            var rr = r.Remove(16);
            Assert.AreSame(r, rr);

            m = m.AddOrUpdate(18, "18");
            m = m.AddOrKeep(18, "18");
            Assert.AreEqual("18", m.GetValueOrDefault(18));

            m = m.AddOrUpdate(19, "19").AddOrUpdate(20, "20").AddOrUpdate(21, "21").AddOrUpdate(22, "22").AddOrUpdate(23, "23");
            rr = m.Remove(25).Remove(21);
            Assert.IsNull(rr.GetValueOrDefault(21));
        }

        [Test]
        public void ImMap_AddOrUpdate_random_items_and_randomly_checking_CsCheck()
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
            iter: 5000);
        }

        [Test]
        public void ImMap_AddOrUpdate_random_items_and_randomly_checking_CsCheck_FailedCase2()
        {
            var items = new[] { 81827, 98388, 55336, 13449, 96388, 3895, 7794, 98331, 44532, 94862, 89412, 25144, 18434, 44532, 58167 };
            var m = ImHashMap<int, int>.Empty;
            foreach (int n in items)
            {
                m = m.AddOrUpdate(n, n);
                Assert.AreEqual(n, m.GetValueOrDefault(n));
            }

            foreach (int n in items)
                Assert.AreEqual(n, m.GetValueOrDefault(n));
        }

        [Test]
        public void ImMap_AddOrUpdate_random_items_and_randomly_checking_CsCheck_FiledCase1()
        {
            var hashes = new[] { 98470, 31912, 32917, 40383, 23438, 70273, 47956, 43609, 10213, 2236, 20614 };
            var m = ImHashMap<int, int>.Empty;
            foreach (int h in hashes)
            {
                m = m.AddOrUpdate(h, h);
                Assert.AreEqual(h, m.GetValueOrDefault(h));
            }

            foreach (int h in hashes)
                Assert.AreEqual(h, m.GetValueOrDefault(h));

            // non-existing keys
            Assert.AreEqual(0, m.GetValueOrDefault(0));
            Assert.AreEqual(0, m.GetValueOrDefault(-1));
        }

        [Test]
        public void ImMap_Enumerate_should_work_for_the_randomized_input()
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
        public void ImMap_Enumerate_should_work_for_the_randomized_input_2()
        {
            var uniqueItems = new[] {
                17883, 23657, 24329, 29524, 55791, 66175, 67389, 74867, 74946, 81350, 94477, 70414, 26499 };

            var m = ImHashMap<int, int>.Empty;
            foreach (var i in uniqueItems)
                m = m.AddOrUpdate(i, i);

            CollectionAssert.AreEqual(uniqueItems.OrderBy(x => x), m.Enumerate().ToArray().Select(x => x.Hash));
        }

        static Gen<(ImHashMap<int, int>, int[])> GenImMap(int upperBound) =>
            Gen.Int[0, upperBound].ArrayUnique.SelectMany(hashes =>
                Gen.Int.Array[hashes.Length].Select(values =>
                {
                    var m = ImHashMap<int, int>.Empty;
                    for (int i = 0; i < hashes.Length; i++)
                        m = m.AddOrUpdate(hashes[i], values[i]);
                    return (map: m, hashes: hashes);
                }));

        [Test]
        public void ImMap_AddOrUpdate_metamorphic()
        {
            const int upperBound = 100_000;
            Gen.Select(GenImMap(upperBound), Gen.Int[0, upperBound], Gen.Int, Gen.Int[0, upperBound], Gen.Int)
                .Sample(t =>
                {
                    var ((m, _), k1, v1, k2, v2) = t;

                    var m1 = m.AddOrUpdate(k1, v1).AddOrUpdate(k2, v2);

                    var m2 = k1 == k2 ? m.AddOrUpdate(k2, v2) : m.AddOrUpdate(k2, v2).AddOrUpdate(k1, v1);

                    var e1 = m1.Enumerate().OrderBy(i => i.Hash);

                    var e2 = m2.Enumerate().OrderBy(i => i.Hash);

                    CollectionAssert.AreEqual(e1.Select(x => x.Hash), e2.Select(x => x.Hash));
                },
                iter: 5000);
        }

        [Test]
        public void ImMap_Remove_metamorphic()
        {
            const int upperBound = 100_000;
            Gen.Select(GenImMap(upperBound), Gen.Int[0, upperBound], Gen.Int, Gen.Int[0, upperBound], Gen.Int)
                .Sample(t =>
                {
                    var ((m, _), k1, v1, k2, v2) = t;

                    m = m.AddOrUpdate(k1, v1).AddOrUpdate(k2, v2);

                    var m1 = m.Remove(k1).Remove(k2);
                    var m2 = m.Remove(k2).Remove(k1);

                    var e1 = m1.Enumerate().Select(x => x.Hash);
                    var e2 = m2.Enumerate().Select(x => x.Hash);

                    CollectionAssert.AreEqual(e1, e2);
                },
                iter: 5000);
        }

        [Test]
        public void ImMap_Remove_metamorphic_failure_case_with_Branch2Plus1()
        {
            const int upperBound = 100_000;
            Gen.Select(GenImMap(upperBound), Gen.Int[0, upperBound], Gen.Int, Gen.Int[0, upperBound], Gen.Int)
                .Sample(t =>
                {
                    var ((m, _), k1, v1, k2, v2) = t;

                    m = m.AddOrUpdate(k1, v1).AddOrUpdate(k2, v2);

                    var m1 = m.Remove(k1).Remove(k2);
                    var m2 = m.Remove(k2).Remove(k1);

                    var e1 = m1.Enumerate().Select(x => x.Hash);
                    var e2 = m2.Enumerate().Select(x => x.Hash);

                    CollectionAssert.AreEqual(e1, e2);
                },
                iter: 5000, seed: "1wsRNkSYY1N4");
        }

        [Test]
        public void ImMap_AddOrUpdate_ModelBased()
        {
            const int upperBound = 100000;
            Gen.SelectMany(GenImMap(upperBound), m =>
                Gen.Select(Gen.Const(m.Item1), Gen.Int[0, upperBound], Gen.Int, Gen.Const(m.Item2)))
                .Sample(t =>
                {
                    var dic1 = t.V0.ToDictionary();
                    dic1[t.V1] = t.V2;

                    var dic2 = t.V0.AddOrUpdate(t.V1, t.V2).ToDictionary();

                    CollectionAssert.AreEqual(dic1, dic2);
                }
                , iter: 1000
                , print: t => t + "\nhashes: {" + string.Join(", ", t.V3) + "}");
        }

        [Test]
        public void ImMap_AddOrUpdate_ModelBased_FailedCase1()
        {
            var hashes = new[] { 73341, 68999, 1354, 50830, 94661, 21594, 27007, 21894, 35166, 68934 };
            var added = 22189;
            var map = ImHashMap<int, int>.Empty;
            foreach (var h in hashes)
                map = map.AddOrUpdate(h, h);

            var dic1 = map.ToDictionary();
            dic1[added] = added;

            map = map.AddOrUpdate(added, added);
            var dic2 = map.ToDictionary();

            CollectionAssert.AreEqual(dic1, dic2);
        }

        [Test]
        public void ImHashMap_Remove_ModelBased_FailedCase1()
        {
            var hashes = new int[7] { 26716, 80399, 13634, 25950, 56351, 51074, 46591 };
            var added = 66928;

            var map = ImHashMap<int, int>.Empty;
            foreach (var n in hashes)
                map = map.AddOrUpdate(n, n);

            var dic1 = map.ToDictionary();
            if (dic1.ContainsKey(added))
                dic1.Remove(added);

            var dic2 = map.AddOrUpdate(added, added).Remove(added).ToDictionary();

            CollectionAssert.AreEqual(dic1, dic2);
        }

        [Test]
        public void ImMap_Remove_ModelBased()
        {
            const int upperBound = 100000;
            Gen.SelectMany(GenImMap(upperBound), m =>
                Gen.Select(Gen.Const(m.Item1), Gen.Int[0, upperBound], Gen.Int, Gen.Const(m.Item2)))
                .Sample(t =>
                {
                    var dic1 = t.V0.ToDictionary();
                    if (dic1.ContainsKey(t.V1))
                        dic1.Remove(t.V1);

                    var map = t.V0.AddOrUpdate(t.V1, t.V2).Remove(t.V1);
                    Assert.AreEqual(t.V0.Remove(t.V1).Count(), map.Count());

                    var dic2 = map.ToDictionary();
                    CollectionAssert.AreEqual(dic1, dic2);
                }
                , iter: 1000
                , print: t => t + "\n" + "keys: {" + string.Join(", ", t.V3) + "}");
        }

        [Test]
        public void ImMap_Remove_ModelBased_FailedCase1()
        {
            var hashes = new int[10] { 22063, 17962, 90649, 8112, 30393, 94009, 60740, 80192, 11026, 19570 };
            var added = 29210;

            var map = ImHashMap<int, int>.Empty;
            foreach (var n in hashes)
                map = map.AddOrUpdate(n, n);

            var result = map.AddOrUpdate(added, added);
            result = result.Remove(added);

            CollectionAssert.AreEqual(map.Enumerate().Select(x => x.Hash), result.Enumerate().Select(x => x.Hash));
        }
    }
}