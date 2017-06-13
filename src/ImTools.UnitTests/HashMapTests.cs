using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class HashMapTests
    {
        [Test]
        public void Can_store_and_retrieve_value_from_map()
        {
            var map = new HashMap<int, string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42 + 32, "2");
            map.AddOrUpdate(42 + 32 + 32, "3");

            Assert.AreEqual("1", map.GetValueOrDefault(42));
            Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
            Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));

            Assert.IsNull(map.GetValueOrDefault(43));
        }

        [Test]
        public void Can_store_and_get_stored_item_count()
        {
            var map = new HashMap<int, string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42 + 32 + 32, "3");

            Assert.AreEqual(2, map.Count);
        }

        [Test]
        public void Can_update_a_stored_item_with_new_value()
        {
            var map = new HashMap<int, string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42, "3");

            Assert.AreEqual("3", map.GetValueOrDefault(42));
            Assert.AreEqual(1, map.Count);
        }

        [Test]
        public void Can_remove_the_stored_item()
        {
            var map = new HashMap<int, string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42 + 32, "2");
            map.AddOrUpdate(42 + 32 + 32, "3");

            map.Remove(42 + 32);

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        }

        [Test]
        public void Can_remove_the_stored_item_twice()
        {
            var map = new HashMap<int, string>(2);

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(41, "41");
            map.AddOrUpdate(42 + 32, "2");
            map.AddOrUpdate(43, "43");
            map.AddOrUpdate(42 + 32 + 32, "3");

            // removing twice
            map.Remove(42 + 32);
            map.Remove(42 + 32);

            Assert.AreEqual(4, map.Count);
            Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        }

        [Test]
        public void Can_add_key_with_0_hash_code()
        {
            var map = new HashMap<int, string>();

            map.AddOrUpdate(0, "aaa");
            map.AddOrUpdate(0 + 32, "2");
            map.AddOrUpdate(0 + 32 + 32, "3");

            string value;
            Assert.IsTrue(map.TryFind(0, out value));

            Assert.AreEqual("aaa", value);
        }

        [Test]
        public void Can_quickly_find_the_scattered_items_with_the_same_cache()
        {
            var map = new HashMap<int, string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(43, "a");
            map.AddOrUpdate(42 + 32, "2");
            map.AddOrUpdate(45, "b");
            map.AddOrUpdate(46, "c");
            map.AddOrUpdate(42 + 32 + 32, "3");

            string value;
            Assert.IsTrue(map.TryFind(42 + 32, out value));
            Assert.AreEqual("2", value);

            Assert.IsTrue(map.TryFind(42 + 32 + 32, out value));
            Assert.AreEqual("3", value);
        }
    }
}
