using NUnit.Framework;

namespace ImTools.UnitTests.Playground
{
    // [TestFixture]
    public class HashArrayMappedTrieTests
    {
        [Test]
        public void Test_how_int_hash_code_is_working_at_edge_cases()
        {
            // Hash code for negative is still negative and equal to key
            Assert.AreEqual(-7, -7.GetHashCode());
        }

        [Test]
        public void Create_trie_and_add_value_to_it()
        {
            var trie = HashTrie<string>.Empty;
            trie = trie.AddOrUpdate(705, "a");
            trie = trie.AddOrUpdate(706, "b");
            trie = trie.AddOrUpdate(750, "c");
            trie = trie.AddOrUpdate(705, "A");
            trie = trie.AddOrUpdate(0, "0x");
            trie = trie.AddOrUpdate(5, "5x");
            trie = trie.AddOrUpdate(555555555, "55x");
            trie = trie.AddOrUpdate(750, "C");

            Assert.AreEqual(null, trie.GetValueOrDefault(13));
            Assert.AreEqual("C", trie.GetValueOrDefault(750));
            Assert.AreEqual("0x", trie.GetValueOrDefault(0));
            Assert.AreEqual("A", trie.GetValueOrDefault(705));
            Assert.AreEqual("55x", trie.GetValueOrDefault(555555555));
            Assert.AreEqual(null, trie.GetValueOrDefault(-1));
        }

        [Test]
        public void Store_value_with_0_hash_then_with_0_plus_some_hash()
        {
            var trie = HashTrie<string>.Empty;
            trie = trie.AddOrUpdate(0, "a");
            trie = trie.AddOrUpdate(64, "b");

            Assert.AreEqual("b", trie.GetValueOrDefault(64));
            Assert.AreEqual("a", trie.GetValueOrDefault(0));
        }

        [Test]
        public void Store_value_with_0_plus_some_hash_then_with_0_hash()
        {
            var trie = HashTrie<string>.Empty;
            trie = trie.AddOrUpdate(64, "a");
            trie = trie.AddOrUpdate(0, "b");

            Assert.AreEqual("a", trie.GetValueOrDefault(64));
            Assert.AreEqual("b", trie.GetValueOrDefault(0));
        }

        [Test]
        public void Update_values_with_0_hash()
        {
            var trie = HashTrie<string>.Empty;
            trie = trie.AddOrUpdate(0, "a");
            trie = trie.AddOrUpdate(0, "b");

            Assert.AreEqual("b", trie.GetValueOrDefault(0));
        }
    }
}