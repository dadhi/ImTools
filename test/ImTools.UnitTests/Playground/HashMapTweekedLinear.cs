using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.UnitTests.Playground
{
    /// <summary>The concurrent HashTable.</summary>
    /// <typeparam name="K">Type of the key</typeparam> <typeparam name="V">Type of the value</typeparam>
    /// <typeparam name="TEqualityComparer">Better be a struct to enable `Equals` and `GetHashCode` inlining.</typeparam>
    public class HashMapTweekedLinear<K, V, TEqualityComparer> where TEqualityComparer : struct, IEqualityComparer<K>
    {
        internal class KeyValue
        {
            public K Key;
            public V Value;

            public KeyValue(K key, V value)
            {
                Key = key;
                Value = value;
            }
        }

        internal struct Slot
        {
            public int KeyHash;
            public KeyValue KeyValue;

            public Slot(int keyHash, KeyValue keyValue)
            {
                KeyHash = keyHash;
                KeyValue = keyValue;
            }
        }

        /// <summary>Initial size of underlying storage, prevents the unnecessary storage re-sizing and items migrations.</summary>
        public const int InitialCapacityBitCount = 5; // aka 32'

        private const int HashOfRemoved = ~1, AddToHashToDistinguishFromEmptyOrRemoved = 1;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        // No readonly because otherwise the struct will be copied on every call.
#pragma warning disable 649
        private TEqualityComparer _equalityComparer;
#pragma warning restore 649

        private Slot[] _slots;
        private Slot[] _newSlots; // The expanded transition slots. After being re-populated, they will become a regular @slots
        private int _count;

        /// <summary>Amount of stored items, 0 in an empty map.</summary>
        public int Count => _count;

        /// <summary>Constructor. Allows to set the <see cref="InitialCapacityBitCount"/>.</summary>
        /// <param name="initialCapacityBitCount">Initial underlying buckets size.</param>
        public HashMapTweekedLinear(int initialCapacityBitCount = InitialCapacityBitCount)
        {
            _slots = new Slot[1 << initialCapacityBitCount];
        }

        /// <summary>Looks for the key in a map and returns the value if found.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">The found value</param>
        /// <returns>True if contains key.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public bool TryFind(K key, out V value)
        {
            var hash = _equalityComparer.GetHashCode(key) | AddToHashToDistinguishFromEmptyOrRemoved;

            var slots = _slots;
            var bits = slots.Length - 1;

            // Step 0: Search the key in its ideal slot.
            // Step 1+: Probe the next-to-ideal slot until the Empty Hash slot, which will indicate an absence of the key.
            // Important to proceed the search further over the removed slot, cause HashOfRemoved is different from Empty Hash slot.
            for (var step = 0; step < bits; ++step)
            {
                var slot = slots[(hash + step) & bits];
                if (slot.KeyHash == hash && _equalityComparer.Equals(slot.KeyValue.Key, key))
                {
                    value = slot.KeyValue.Value;
                    return true;
                }

                if (slot.KeyHash == 0)
                    break;
            }

            value = default(V);
            return false;
        }

        /// <summary>Looks for the key in a map and returns the value if found.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">The found value</param>
        /// <returns>True if contains key.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public bool TryFind_RefLocal(K key, out V value)
        {
            var hash = _equalityComparer.GetHashCode(key) | AddToHashToDistinguishFromEmptyOrRemoved;

            var slots = _slots;
            var bits = slots.Length - 1;

            // Step 0: Search the key in its ideal slot.
            // Step 1+: Probe the next-to-ideal slot until the Empty Hash slot, which will indicate an absence of the key.
            // Important to proceed the search further over the removed slot, cause HashOfRemoved is different from Empty Hash slot.
            for (var step = 0; step < bits; ++step)
            {
                ref var slot = ref slots[(hash + step) & bits];
                if (slot.KeyHash == hash && _equalityComparer.Equals(slot.KeyValue.Key, key))
                {
                    value = slot.KeyValue.Value;
                    return true;
                }

                if (slot.KeyHash == 0)
                    break;
            }

            value = default(V);
            return false;
        }

        /// <summary>Looks for key in a map and returns the value if key found, or <paramref name="defaultValue"/> otherwise.</summary>
        /// <param name="key">Key to look for.</param> <param name="defaultValue">(optional) Value to return if key is not found.</param>
        /// <returns>Found value or <paramref name="defaultValue"/>.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public V GetValueOrDefault(K key, V defaultValue = default(V))
        {
            var hash = _equalityComparer.GetHashCode(key) | AddToHashToDistinguishFromEmptyOrRemoved;

            var slots = _slots;
            var bits = slots.Length - 1;

            // Step 0: Search the key in its ideal slot.
            // Step 1+: Probe the next-to-ideal slot until the Empty Hash slot, which will indicate an absence of the key.
            // Important to proceed the search further over the removed slot, cause HashOfRemoved is different from Empty Hash slot.
            for (var step = 0; step < bits; ++step)
            {
                var slot = slots[(hash + step) & bits];
                if (slot.KeyHash == hash && _equalityComparer.Equals(slot.KeyValue.Key, key))
                    return slot.KeyValue.Value;
                if (slot.KeyHash == 0)
                    break;
            }

            return defaultValue;
        }

        /// <summary>Adds the key-value into the map or updates the values if the key is already added.</summary>
        /// <param name="key">Key to put</param><param name="value">Value to put</param>
        public void AddOrUpdate(K key, V value)
        {
            var hash = _equalityComparer.GetHashCode(key) | AddToHashToDistinguishFromEmptyOrRemoved;

            while (true) // retry until succeeding
            {
                var slots = _newSlots ?? _slots; // always operate either on new or current slots

                // If more than 75% of slots are filled then expand the slots, double the size.
                // Ignore (proceed and try to put an item) if we are already on new slots
                var capacity = slots.Length;
                if (slots != _newSlots)
                {
                    var count = _count;
                    count += count >> 1; // count + half-count
                    if (count >= capacity)
                    {
                        Expand(slots, capacity << 1);
                        continue;
                    }
                }

                // Search for an empty or removed slot, or slot with the same key (for update) 
                // starting from the ideal index position.
                // It is Ok to search for the @bits length, which is -1 of total slots length, 
                // because wasting one slot is not big of a deal considering it provides less calculations.
                var bits = capacity - 1;
                for (var step = 0; step < bits; ++step)
                {
                    var index = (hash + step) & bits;

                    // First try to put item into an empty slot or try to put it into a removed slot
                    if (Interlocked.CompareExchange(ref slots[index].KeyHash, hash, 0) == 0 ||
                        Interlocked.CompareExchange(ref slots[index].KeyHash, hash, HashOfRemoved) == HashOfRemoved)
                    {
                        slots[index].KeyValue.Key   = key;
                        slots[index].KeyValue.Value = value;

                        // ensure that we operate on the same slots: either re-populating or the stable one
                        if (slots != _newSlots && slots != _slots)
                            continue;

                        Interlocked.Increment(ref _count); // increment cause we are adding new item
                        return; // Successfully added!
                    }

                    // Then check for updating the slot
                    if (slots[index].KeyHash == hash && _equalityComparer.Equals(slots[index].KeyValue.Key, key))
                    {
                        slots[index].KeyValue.Value = value;

                        // ensure that we operate on the same slots: either re-populating or the stable one
                        if (slots != _newSlots && slots != _slots)
                            continue;

                        return; // Successfully updated!
                    }
                }
            }
        }

        private void Expand(Slot[] slots, int newCapacity)
        {
            var newSlots = new Slot[newCapacity];
            if (Interlocked.CompareExchange(ref _newSlots, newSlots, null) != null)
                return;

            var newBits = newCapacity - 1;
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var hash = slot.KeyHash;
                if (hash == 0 || hash == HashOfRemoved)
                    continue; // skip the removed items

                // Get the new index in expanded collection and fill with the existing key-value pairs,
                // and ignore the slots marked as removed
                for (var step = 0; step < newBits; ++step)
                {
                    var index = (hash + step) & newBits;
                    if (Interlocked.CompareExchange(ref newSlots[index].KeyHash, hash, 0) == 0)
                    {
                        newSlots[index].KeyValue.Key   = slot.KeyValue.Key;
                        newSlots[index].KeyValue.Value = slot.KeyValue.Value;
                        break;
                    }
                }
            }

            // If the underlying slots are not changed, replace them with the new slots, and retry
            if (Interlocked.CompareExchange(ref _slots, newSlots, slots) == slots)
                Interlocked.Exchange(ref _newSlots, null);
        }

        /// <summary>Removes the value with passed key. 
        /// Actually it is a SOFT REMOVE which marks slot with found key as removed, without compacting the underlying array.</summary>
        /// <param name="key"></param><returns>The true if key was found, false otherwise.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public bool Remove(K key)
        {
            var hash = _equalityComparer.GetHashCode(key) | AddToHashToDistinguishFromEmptyOrRemoved;

            // @newSlots (if not empty) will become a new @slots, so the removed marker should be kept at the end
            var slots = _newSlots ?? _slots;
            var bits = slots.Length - 1;

            // Search starting from ideal slot
            for (var step = 0; step < bits; ++step)
            {
                var index = (hash + step) & bits;
                var slot = slots[index];
                if (slot.KeyHash == hash && key.Equals(slot.KeyValue.Key))
                {
                    // Mark as removed
                    if (Interlocked.CompareExchange(ref slots[index].KeyHash, HashOfRemoved, hash) == hash)
                        Interlocked.Decrement(ref _count);
                    return true;
                }

                if (slot.KeyHash == 0)
                    break; // finish search on empty slot But not on removed slot
            }

            return false;
        }
    }
}
