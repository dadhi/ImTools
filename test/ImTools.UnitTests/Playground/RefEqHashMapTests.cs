using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class RefEqHashMapTests
    {
        class Key {
            public int hash;
            private Key(int h) => hash = h;
            public override int GetHashCode() => hash;
            public override bool Equals(object obj) => obj is Key k && k.hash == hash;

            public static readonly Key _42 = new Key(42);
            public static readonly Key _43 = new Key(43);
            public static readonly Key _42_32 = new Key(42 + 32);
            public static readonly Key _43_32 = new Key(43 + 32);
            public static readonly Key _42_32_32 = new Key(42 + 32 + 32);
            public static readonly Key _42_32_32_32 = new Key(42 + 32 + 32 + 32);
        }

        [Test]
        public void Can_store_and_retrieve_value_from_map()
        {
            var map = new RefEqHashMap<Key, string>(3);

            map.AddOrUpdate(Key._42, "1");
            map.AddOrUpdate(Key._42_32, "2");

            // interrupt the keys with ne key
            map.AddOrUpdate(Key._43, "a");
            map.AddOrUpdate(Key._43_32, "b");
            map.AddOrUpdate(Key._42_32_32, "3");

            Assert.AreEqual("1", map.GetValueOrDefault(Key._42));
            Assert.AreEqual("2", map.GetValueOrDefault(Key._42_32));
            Assert.AreEqual("3", map.GetValueOrDefault(Key._42_32_32));
            Assert.AreEqual(null, map.GetValueOrDefault(Key._42_32_32_32));

            Assert.AreEqual("a", map.GetValueOrDefault(Key._43));
        }

        // [Test]
        // public void Can_store_and_retrieve_value_from_map_with_Expand_in_the_middle()
        // {
        //     var map = new HashMapLeapfrog<int, string, IntEqualityComparer>(2);

        //     map.AddOrUpdate(42, "1");
        //     map.AddOrUpdate(42 + 32, "2");

        //     // interrupt the keys with ne key
        //     map.AddOrUpdate(43, "a");
        //     map.AddOrUpdate(43 + 32, "b");

        //     map.AddOrUpdate(42 + 32 + 32, "3");

        //     Assert.AreEqual("1", map.GetValueOrDefault(42));
        //     Assert.AreEqual("2", map.GetValueOrDefault(42 + 32));
        //     Assert.AreEqual("3", map.GetValueOrDefault(42 + 32 + 32));
        //     Assert.AreEqual(null, map.GetValueOrDefault(42 + 32 + 32 + 32));

        //     Assert.AreEqual("a", map.GetValueOrDefault(43));
        // }

        // [Test]
        // public void Can_store_and_get_stored_item_count()
        // {
        //     var map = new HashMapLeapfrog<int, string, IntEqualityComparer>();

        //     map.AddOrUpdate(42, "1");
        //     map.AddOrUpdate(42 + 32 + 32, "3");

        //     Assert.AreEqual(2, map.Count);
        // }

        // [Test]
        // public void Can_update_a_stored_item_with_new_value()
        // {
        //     var map = new HashMapLeapfrog<int, string, IntEqualityComparer>();

        //     map.AddOrUpdate(42, "1");
        //     map.AddOrUpdate(42, "3");

        //     Assert.AreEqual("3", map.GetValueOrDefault(42));
        //     Assert.AreEqual(1, map.Count);
        // }

        // [Test]
        // public void Can_remove_the_stored_item()
        // {
        //     var map = new HashMapLeapfrog<int, string, IntEqualityComparer>();

        //     map.AddOrUpdate(42, "1");
        //     map.AddOrUpdate(42 + 32, "2");
        //     map.AddOrUpdate(42 + 32 + 32, "3");

        //     map.Remove(42 + 32);

        //     Assert.AreEqual(2, map.Count);
        // }

        // [Test]
        // public void Can_add_key_with_0_hash_code()
        // {
        //     var map = new HashMapLeapfrog<int, string, IntEqualityComparer>();

        //     map.AddOrUpdate(0, "aaa");
        //     map.AddOrUpdate(0 + 32, "2");
        //     map.AddOrUpdate(0 + 32 + 32, "3");

        //     string value;
        //     Assert.IsTrue(map.TryFind(0, out value));

        //     Assert.AreEqual("aaa", value);
        // }

        // [Test]
        // public void Can_quickly_find_the_scattered_items_with_the_same_cache()
        // {
        //     var map = new HashMapLeapfrog<int, string, IntEqualityComparer>();

        //     map.AddOrUpdate(42, "1");
        //     map.AddOrUpdate(43, "a");
        //     map.AddOrUpdate(42 + 32, "2");
        //     map.AddOrUpdate(45, "b");
        //     map.AddOrUpdate(46, "c");
        //     map.AddOrUpdate(42 + 32 + 32, "3");

        //     string value;
        //     Assert.IsTrue(map.TryFind(42 + 32, out value));
        //     Assert.AreEqual("2", value);

        //     Assert.IsTrue(map.TryFind(42 + 32 + 32, out value));
        //     Assert.AreEqual("3", value);
        // }
    }
}
