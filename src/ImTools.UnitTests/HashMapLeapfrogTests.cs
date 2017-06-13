using System.Threading;
using NUnit.Framework;

namespace ImTools.UnitTests
{
    /// <summary>The concurrent HashTable.</summary>
    /// <typeparam name="K">Type of the key</typeparam> <typeparam name="V">Type of the value</typeparam>
    public class HashMapLeapfrog<K, V>
    {
        internal struct Slot
        {
            public int First;
            public int Next;

            public int Hash;
            public K Key;
            public V Value;
        }

        private Slot[] _slots;
        private int _count;

        /// <summary>Initial size of underlying storage, prevents the unnecessary storage re-sizing and items migrations.</summary>
        public const int CapacityBitCount = 5; // aka 32

        /// <summary>Amount of store items. 0 for empty map.</summary>
        public int Count { get { return _count; } }

        /// <summary>Constructor. Allows to set the <see cref="CapacityBitCount"/>.</summary>
        /// <param name="capacityBitCount"></param>
        public HashMapLeapfrog(int capacityBitCount = CapacityBitCount)
        {
            _slots = new Slot[1 << capacityBitCount];
        }

        /// <summary>Looks for key in a tree and returns the value if found.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">The found value</param>
        /// <returns>True if contains key.</returns>
        public bool TryFind(K key, out V value)
        {
            var hash = key.GetHashCode() | 1;

            var slots = _slots;
            var bits = slots.Length - 1;

            var slot = slots[hash & bits];
            if (slot.Hash == hash && (ReferenceEquals(slot.Key, key) || slot.Key.Equals(key)))
            {
                value = slot.Value;
                return true;
            }

            var distance = slot.First;
            if (distance == 0)
                distance = slot.Next;

            if (distance != 0)
            {
                while (true)
                {
                    slot = slots[(hash + distance) & bits];
                    if (slot.Hash == hash && (ReferenceEquals(slot.Key, key) || slot.Key.Equals(key)))
                    {
                        value = slot.Value;
                        return true;
                    }

                    var leap = slot.Next;
                    if (leap == 0)
                        break;

                    distance += leap;
                }
            }

            value = default(V);
            return false;
        }

        /// <summary>Looks for key in a tree and returns the key value if found, or <paramref name="defaultValue"/> otherwise.</summary>
        /// <param name="key">Key to look for.</param> <param name="defaultValue">(optional) Value to return if key is not found.</param>
        /// <returns>Found value or <paramref name="defaultValue"/>.</returns>
        public V GetValueOrDefault(K key, V defaultValue = default(V))
        {
            V value;
            return TryFind(key, out value) ? value : defaultValue;
        }

        /// <summary>Returns new tree with added key-value. 
        /// If value with the same key is exist then the value is replaced.</summary>
        /// <param name="key">Key to add.</param><param name="value">Value to add.</param>
        public void AddOrUpdate(K key, V value)
        {
            var hash = key.GetHashCode() | 1;
            while (true)
            {
                var slots = _slots;
                var addOrUpdate = AddOrUpdate(slots, hash, key, value);
                if (addOrUpdate != -1)
                {
                    // todo: ensure that slots did not change in between
                    if (addOrUpdate == 0)
                        return;
                    break;
                }

                var doubleLength = slots.Length << 1;
                var newSlots = new Slot[doubleLength];

                AddOrUpdate(newSlots, hash, key, value);

                for (var i = 0; i < slots.Length; i++)
                {
                    var slot = slots[i];
                    AddOrUpdate(newSlots, slot.Hash, slot.Key, slot.Value);
                }

                // if the repopulated slots did not change in between, we are done, 
                // otherwise - repeat
                if (Interlocked.CompareExchange(ref _slots, newSlots, slots) == slots)
                    break;
            }

            Interlocked.Increment(ref _count);
        }

        // returns 1 if added, 0 if updated, -1 if failed to add
        private static int AddOrUpdate(Slot[] slots, int hash, K key, V value)
        {
            var bits = slots.Length - 1;
            var i = hash & bits;

            // add
            if (Interlocked.CompareExchange(ref slots[i].Hash, hash, 0) == 0)
            {
                slots[i].Key = key;
                slots[i].Value = value;
                return 1;
            }

            // update
            if (slots[i].Hash == hash && key.Equals(slots[i].Key))
            {
                slots[i].Value = value;
                return 0;
            }

            var setFirst = true;
            var distance = slots[i].First;
            if (distance == 0)
            {
                distance = slots[i].Next;
                setFirst = false;
            }

            // update for known chain
            if (distance != 0)
            {
                while (true)
                {
                    i = (hash + distance) & bits;
                    if (slots[i].Hash == hash && key.Equals(slots[i].Key))
                    {
                        slots[i].Value = value;
                        return 0;
                    }

                    var leap = slots[i].Next;
                    if (leap == 0)
                        break;

                    distance += leap;
                    setFirst = false;
                }
            }

            // look outside of the chain for the empty slot to add
            var addDistance = distance + 1;
            while (addDistance < slots.Length)
            {
                var addIndex = (hash + addDistance) & bits;

                if (Interlocked.CompareExchange(ref slots[addIndex].Hash, hash, 0) == 0)
                {
                    slots[addIndex].Key = key;
                    slots[addIndex].Value = value;

                    // add link to the item
                    if (setFirst)
                        slots[i].First = addDistance - distance;
                    else
                        slots[i].Next = addDistance - distance;

                    return 1;
                }

                ++addDistance;
            }

            return -1;
        }

        /// <summary>Removes the value with passed key. 
        /// Actually it is a SOFT REMOVE which marks slot with found key as removed, without compacting the underlying array.</summary>
        /// <param name="key"></param><returns>The true if key was found, false otherwise.</returns>
        public bool Remove(K key)
        {
            var hash = key.GetHashCode() | 1;

            var slots = _slots;
            var bits = slots.Length - 1;

            // search until the empty slot
            for (var i = 0; i < slots.Length; ++i)
            {
                var index = (hash + i) & bits;
                var slot = slots[index];

                if (slot.Hash == hash && key.Equals(slot.Key))
                {
                    // mark as removed
                    if (slots[index].Hash != 0)
                    {
                        slots[index].Hash = 0;
                        Interlocked.Decrement(ref _count);
                    }
                    return true;
                }

                if (slot.Hash == 0)
                    return false; // finish search at empty slot
            }

            return false;
        }
    }

    [TestFixture]
    public class HashMapLeapfrogTests
    {
        [Test]
        public void Can_store_and_retrieve_value_from_map()
        {
            var map = new HashMapLeapfrog<int, string>();

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
            var map = new HashMapLeapfrog<int, string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42 + 32 + 32, "3");

            Assert.AreEqual(2, map.Count);
        }

        [Test]
        public void Can_update_a_stored_item_with_new_value()
        {
            var map = new HashMapLeapfrog<int, string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42, "3");

            Assert.AreEqual("3", map.GetValueOrDefault(42));
            Assert.AreEqual(1, map.Count);
        }

        [Test]
        public void Can_remove_the_stored_item()
        {
            var map = new HashMapLeapfrog<int, string>();

            map.AddOrUpdate(42, "1");
            map.AddOrUpdate(42 + 32, "2");
            map.AddOrUpdate(42 + 32 + 32, "3");

            map.Remove(42 + 32);

            Assert.AreEqual(2, map.Count);
        }

        [Test]
        public void Can_add_key_with_0_hash_code()
        {
            var map = new HashMapLeapfrog<int, string>();

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
            var map = new HashMapLeapfrog<int, string>();

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
