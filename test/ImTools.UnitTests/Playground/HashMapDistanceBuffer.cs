using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools
{
    /// <summary>The concurrent HashTable.</summary>
    /// <typeparam name="K">Type of the key</typeparam> <typeparam name="V">Type of the value</typeparam>
    /// <typeparam name="TEqualityComparer">Better be a struct to enable `Equals` and `GetHashCode` inlining.</typeparam>
    public class HashMapDistanceBuffer<K, V, TEqualityComparer> where TEqualityComparer : struct, IEqualityComparer<K>
    {
        internal struct Slot
        {
            public int Hash; // 0 - means slot is not occupied, ~1 means soft-removed item 
            public K Key;
            public V Value;
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
        public int Count { get { return _count; } }

        /// <summary>Constructor. Allows to set the <see cref="InitialCapacityBitCount"/>.</summary>
        /// <param name="initialCapacityBitCount">Initial underlying buckets size.</param>
        public HashMapDistanceBuffer(int initialCapacityBitCount = InitialCapacityBitCount)
        {
            var capacity = 1 << initialCapacityBitCount;
            var capacityMinusOne = capacity - 1;
            var slots = new Slot[capacity + (capacityMinusOne >> 2)];

            // Store initial capacity in a first element, 
            // cause it won't be used by any key hash, because hash is always adjusted with AddToHashToDistinguishFromEmptyOrRemoved
            slots[0].Hash = capacityMinusOne;
            _slots = slots;
        }

        /// <summary>Looks for the key in a map and returns the value if found.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">The found value</param>
        /// <returns>True if contains key.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public bool TryFind(K key, out V value)
        {
            var hash = _equalityComparer.GetHashCode(key) | AddToHashToDistinguishFromEmptyOrRemoved;

            var slots = _slots;
            var capacityMask = slots[0].Hash;

            // find ideal index,
            // then find max distant index we can search from the ideal one
            // the max distance can be a quarter of capacity
            var index = hash & capacityMask;
            var maxIndex = index + (capacityMask >> 2);
            for (; index <= maxIndex; ++index)
            {
                var slot = slots[index];
                if (slot.Hash == hash && _equalityComparer.Equals(slot.Key, key))
                {
                    value = slot.Value;
                    return true;
                }

                if (slot.Hash == 0)
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
            var capacityMask = slots[0].Hash;

            // find ideal index,
            // then find max distant index we can search from the ideal one
            // the max distance can be a quarter of capacity
            var index = hash & capacityMask;
            var maxIndex = index + (capacityMask >> 2);
            for (; index <= maxIndex; ++index)
            {
                ref var slot = ref slots[index];
                if (slot.Hash == hash && _equalityComparer.Equals(slot.Key, key))
                {
                    value = slot.Value;
                    return true;
                }

                if (slot.Hash == 0)
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
            var capacityMask = slots[0].Hash;

            // find ideal index
            var index = hash & capacityMask;

            // find max distant index we can search from the ideal one
            // the max distance can be a quarter of capacity
            var maxIndex = index + (capacityMask >> 2);
            for (; index <= maxIndex; ++index)
            {
                var slot = slots[index];
                if (slot.Hash == hash && _equalityComparer.Equals(slot.Key, key))
                    return slot.Value;

                if (slot.Hash == 0)
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
                var capacityMinusOne = slots[0].Hash;

                var index = hash & capacityMinusOne;
                var maxIndex = index + (capacityMinusOne >> 2);
                for (; index <= maxIndex; ++index)
                {
                    // First try to put item into an empty slot or try to put it into a removed slot
                    if (Interlocked.CompareExchange(ref slots[index].Hash, hash, 0) == 0 ||
                        Interlocked.CompareExchange(ref slots[index].Hash, hash, HashOfRemoved) == HashOfRemoved)
                    {
                        slots[index].Key = key;
                        slots[index].Value = value;

                        // ensure that we operate on the same slots: either re-populating or the stable one
                        if (slots != _newSlots && slots != _slots)
                            continue;

                        Interlocked.Increment(ref _count); // increment cause we are adding new item
                        return; // Successfully added!
                    }

                    // Then check for updating the slot
                    if (slots[index].Hash == hash && _equalityComparer.Equals(slots[index].Key, key))
                    {
                        slots[index].Value = value;
                        // ensure that we operate on the same slots: either re-populating or the stable one
                        if (slots != _newSlots && slots != _slots)
                            continue;
                        return; // Successfully updated!
                    }
                }

                Expand(slots, (capacityMinusOne + 1) << 1); // Expand with double capacity and retry to start the loop again
            }
        }

        private void Expand(Slot[] slots, int newCapacity)
        {
            // Add a quarter of capacity to place the out of ideal hash items
            var newCapacityMinusOne = newCapacity - 1;
            var distanceBuffer = newCapacityMinusOne >> 2; // Quarter of capacity minus 1.
            var newSlots = new Slot[newCapacity + distanceBuffer];
            newSlots[0].Hash = newCapacityMinusOne;

            if (Interlocked.CompareExchange(ref _newSlots, newSlots, null) != null)
                return;

            // start from 1 cause 0 is occupied by capacity bit mask
            for (var i = 1; i < slots.Length; i++)
            {
                var slot = slots[i];
                var hash = slot.Hash;
                if (hash == 0 || hash == HashOfRemoved)
                    continue; // skip the removed items

                var index = hash & newCapacityMinusOne;
                var maxIndex = index + distanceBuffer;
                for (; index <= maxIndex; ++index)
                {
                    if (Interlocked.CompareExchange(ref newSlots[index].Hash, hash, 0) == 0)
                    {
                        newSlots[index].Key = slot.Key;
                        newSlots[index].Value = slot.Value;
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
            var capacityMinusOne = slots[0].Hash;

            var index = hash & capacityMinusOne;
            var maxIndex = index + (capacityMinusOne >> 2);
            for (; index <= maxIndex; ++index)
            {
                var slot = slots[index];
                if (slot.Hash == hash && key.Equals(slot.Key)) // Mark the found slot as removed with the HashOfRemoved hash
                {
                    if (Interlocked.CompareExchange(ref slots[index].Hash, HashOfRemoved, hash) == hash)
                        Interlocked.Decrement(ref _count);
                    return true;
                }

                if (slot.Hash == 0)
                    break; // finish search on empty slot But not on removed slot
            }

            return false;
        }
    }

    /// <summary>Custom comparer for int values for max performance. 
    /// Defined as `struct` so the methods can be in-lined.</summary>
    public struct IntEqualityComparer : IEqualityComparer<int>
    {
        /// <inheritdoc />
        public bool Equals(int x, int y)
        {
            return x == y;
        }

        /// <inheritdoc />
        public int GetHashCode(int obj)
        {
            return obj;
        }
    }

    /// <summary>Sugar for easy defining of map with int Key. Uses <see cref="IntEqualityComparer"/>.</summary>
    /// <typeparam name="V">Type of value.</typeparam>
    public sealed class IntHashMap<V> : HashMapDistanceBuffer<int, V, IntEqualityComparer>
    {
        /// <inheritdoc />
        public IntHashMap(int initialCapacityBitCount = InitialCapacityBitCount) : base(initialCapacityBitCount) { }
    }

    /// <summary>Custom comparer for Type values for max performance. 
    /// Defined as `struct` so the methods can be in-lined.</summary>
    public struct TypeEqualityComparer : IEqualityComparer<Type>
    {
        /// <inheritdoc />
        public bool Equals(Type x, Type y) => ReferenceEquals(x, y);

        /// <inheritdoc />
        public int GetHashCode(Type obj) => obj.GetHashCode();
    }

    /// <summary>Sugar for easy defining of map with int Key. Uses <see cref="IntEqualityComparer"/>.</summary>
    /// <typeparam name="V">Type of value.</typeparam>
    public sealed class TypeHashMap<V> : HashMapDistanceBuffer<Type, V, TypeEqualityComparer>
    {
        /// <inheritdoc />
        public TypeHashMap(int initialCapacityBitCount = InitialCapacityBitCount) : base(initialCapacityBitCount) { }
    }
}
