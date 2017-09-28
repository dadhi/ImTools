using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools
{
    /// <summary>The concurrent HashTable.</summary>
    /// <typeparam name="K">Type of the key</typeparam> <typeparam name="V">Type of the value</typeparam>
    /// <typeparam name="TEqualityComparer">Better be a struct to enable `Equals` and `GetHashCode` inlining.</typeparam>
    public class HashMap_SimpleLinear<K, V, TEqualityComparer> where TEqualityComparer : struct, IEqualityComparer<K>
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
        public HashMap_SimpleLinear(int initialCapacityBitCount = InitialCapacityBitCount)
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
            var bits = slots.Length - 1;

            // Step 0: Search the key in its ideal slot.
            // Step 1+: Probe the next-to-ideal slot until the Empty Hash slot, which will indicate an absence of the key.
            // Important to proceed the search further over the removed slot, cause HashOfRemoved is different from Empty Hash slot.
            for (var step = 0; step < bits; ++step)
            {
                var slot = slots[(hash + step) & bits];
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
                var hash = slot.Hash;
                if (hash == 0 || hash == HashOfRemoved)
                    continue; // skip the removed items

                // Get the new index in expanded collection and fill with the existing key-value pairs,
                // and ignore the slots marked as removed
                for (var step = 0; step < newBits; ++step)
                {
                    var index = (hash + step) & newBits;
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
            var bits = slots.Length - 1;

            // Search starting from ideal slot
            for (var step = 0; step < bits; ++step)
            {
                var index = (hash + step) & bits;
                var slot = slots[index];
                if (slot.Hash == hash && key.Equals(slot.Key))
                {
                    // Mark as removed
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

    /// <summary>The concurrent HashTable.</summary>
    /// <typeparam name="K">Type of the key</typeparam> <typeparam name="V">Type of the value</typeparam>
    /// <typeparam name="TEqualityComparer">Better be a struct to enable `Equals` and `GetHashCode` inlining.</typeparam>
    public class HashMap<K, V, TEqualityComparer> where TEqualityComparer : struct, IEqualityComparer<K>
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
        public HashMap(int initialCapacityBitCount = InitialCapacityBitCount)
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
    public sealed class IntHashMap<V> : HashMap<int, V, IntEqualityComparer>
    {
        /// <inheritdoc />
        public IntHashMap(int initialCapacityBitCount = InitialCapacityBitCount) : base(initialCapacityBitCount) { }
    }

    /// <summary>Custom comparer for Type values for max performance. 
    /// Defined as `struct` so the methods can be in-lined.</summary>
    public struct TypeEqualityComparer : IEqualityComparer<Type>
    {
        /// <inheritdoc />
        public bool Equals(Type x, Type y)
        {
            return ReferenceEquals(x, y);
        }

        /// <inheritdoc />
        public int GetHashCode(Type obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>Sugar for easy defining of map with int Key. Uses <see cref="IntEqualityComparer"/>.</summary>
    /// <typeparam name="V">Type of value.</typeparam>
    public sealed class TypeHashMap<V> : HashMap<Type, V, TypeEqualityComparer>
    {
        /// <inheritdoc />
        public TypeHashMap(int initialCapacityBitCount = InitialCapacityBitCount) : base(initialCapacityBitCount) { }
    }

    /// <summary>The concurrent HashTable.</summary>
    /// <typeparam name="K">Type of the key</typeparam> <typeparam name="V">Type of the value</typeparam>
    /// <typeparam name="TEqualityComparer">Better be a struct to enable `Equals` and `GetHashCode` inlining.</typeparam>
    public class HashMapLeapfrog<K, V, TEqualityComparer> where TEqualityComparer : struct, IEqualityComparer<K>
    {
        internal struct Slot
        {
            public int FirstAndNextJump; // first - from ideal index, next for keys sharing the same ideal index
            public int Hash; // 0 - means slot is not occupied, ~1 means soft-removed item 
            public K Key;
            public V Value;
        }

        /// <summary>Initial size of underlying storage, prevents the unnecessary storage re-sizing and items migrations.</summary>
        public const int InitialCapacityBitCount = 5; // aka 32'

        private const int HashOfRemoved = ~1, AddToHashToDistinguishFromEmptyOrRemoved = 1;

        private const int ShiftToNextJumpBits = 16;
        private const int FirstJumpBits = (1 << ShiftToNextJumpBits) - 1; // Set to first 16 bits
        private const int ClearFirstJumpBits = ~FirstJumpBits;
        private const int ClearNextJumpBits = FirstJumpBits; // just for a nice name

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
        public HashMapLeapfrog(int initialCapacityBitCount = InitialCapacityBitCount)
        {
            var capacity = 1 << initialCapacityBitCount;
            _slots = new Slot[capacity + (capacity >> 2)];
        }

        /// <summary>Looks for the key in a map and returns the value if found.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">The found value</param>
        /// <returns>True if contains key.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public bool TryFind(K key, out V value)
        {
            var hash = _equalityComparer.GetHashCode(key) | AddToHashToDistinguishFromEmptyOrRemoved;
            var slots = _slots;

            var slotCount = slots.Length;
            var capacity = slotCount & (slotCount << 2);
            var index = hash & (capacity - 1);
            var slot = slots[index];
            if (slot.Hash == hash && _equalityComparer.Equals(slot.Key, key))
            {
                value = slot.Value;
                return true;
            }

            var jump = slot.FirstAndNextJump & FirstJumpBits;
            while (jump != 0)
            {
                index += jump;
                slot = slots[index];
                if (slot.Hash == hash && _equalityComparer.Equals(slot.Key, key))
                {
                    value = slot.Value;
                    return true;
                }

                jump = slot.FirstAndNextJump >> ShiftToNextJumpBits;
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

            // Step 0: Search the key in its ideal slot.
            var capacity = slots.Length & (slots.Length << 2);
            var index = hash & (capacity - 1);
            var slot = slots[index];
            if (slot.Hash == hash && _equalityComparer.Equals(slot.Key, key))
                return slot.Value;

            // Step 1+: Probe the next-to-ideal slot until the Empty Hash slot, which will indicate an absence of the key.
            // Important to proceed the search further over the removed slot, cause HashOfRemoved is different from Empty Hash slot.
            var jump = slot.FirstAndNextJump & FirstJumpBits;
            while (jump != 0)
            {
                slot = slots[index += jump];
                if (slot.Hash == hash && _equalityComparer.Equals(slot.Key, key))
                    return slot.Value;
                jump = slot.FirstAndNextJump >> ShiftToNextJumpBits;
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

                // Search for an empty or removed slot, or slot with the same key (for update) 
                // starting from the ideal index position.
                // It is Ok to search for the @bits length, which is -1 of total slots length, 
                // because wasting one slot is not big of a deal considering it provides less calculations.

                var capacity = slots.Length & (slots.Length << 2);
                var idealIndex = hash & (capacity - 1);
                var maxIndex = idealIndex + (capacity >> 2);

                var jump = 0;
                var lastJumpIndex = -1;
                for (var index = idealIndex; index <= maxIndex;)
                {
                    // First try to put item into an empty slot or try to put it into a removed slot
                    if (Interlocked.CompareExchange(ref slots[index].Hash, hash, 0) == 0 ||
                        Interlocked.CompareExchange(ref slots[index].Hash, hash, HashOfRemoved) == HashOfRemoved)
                    {
                        slots[index].Key = key;
                        slots[index].Value = value;

                        if (lastJumpIndex != -1) // record a new jump
                        {
                            var oldJumpBits = slots[lastJumpIndex].FirstAndNextJump;
                            int newJumpBits;
                            if (lastJumpIndex == idealIndex)
                                newJumpBits = oldJumpBits & ClearFirstJumpBits | jump;
                            else
                                newJumpBits = oldJumpBits & ClearNextJumpBits | (jump << ShiftToNextJumpBits);

                            if (Interlocked.CompareExchange(ref slots[lastJumpIndex].FirstAndNextJump, newJumpBits, oldJumpBits) != oldJumpBits)
                                continue;
                        }

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

                    if (lastJumpIndex == -1)
                    {
                        var jumps = slots[index].FirstAndNextJump;
                        jump = index == idealIndex
                            ? jumps & FirstJumpBits
                            : jumps >> ShiftToNextJumpBits;

                        if (jump == 0) // the recorded jumps are completed and we got back to step-by-step probing
                        {
                            jump = 1;              // next jump will be 1 again
                            lastJumpIndex = index; // store the last recorded jump destination to add to the chain
                        }

                        index += jump;
                    }
                    else
                    {
                        jump += 1; // accumulate the jump
                        index += 1;
                    }
                }

                Expand(slots, capacity << 1);
            }
        }

        private void Expand(Slot[] slots, int newCapacity)
        {
            var distanceBuffer = newCapacity >> 2; // Quarter of capacity
            var newSlots = new Slot[newCapacity + distanceBuffer];

            if (Interlocked.CompareExchange(ref _newSlots, newSlots, null) != null)
                return;

            for (var i = 1; i < slots.Length; ++i)
            {
                var slot = slots[i];
                var hash = slot.Hash;
                if (hash == 0 || hash == HashOfRemoved)
                    continue; // skip the empty or removed items

                var idealIndex = hash & (newCapacity - 1);
                var maxIndex = idealIndex + distanceBuffer;

                var jump = 0;
                var lastJumpIndex = -1;
                for (var index = idealIndex; index <= maxIndex;)
                {
                    if (Interlocked.CompareExchange(ref newSlots[index].Hash, hash, 0) == 0)
                    {
                        newSlots[index].Key = slot.Key;
                        newSlots[index].Value = slot.Value;

                        if (lastJumpIndex != -1) // record a new jump
                            newSlots[lastJumpIndex].FirstAndNextJump = lastJumpIndex == idealIndex
                                ? jump
                                : jump << ShiftToNextJumpBits;
                        break;
                    }

                    if (lastJumpIndex == -1)
                    {
                        var jumps = newSlots[index].FirstAndNextJump;
                        jump = index == idealIndex
                            ? jumps & FirstJumpBits
                            : jumps >> ShiftToNextJumpBits;

                        if (jump == 0) // the recorded jumps are completed and we got back to step-by-step probing
                        {
                            jump = 1;              // next jump will be 1 again
                            lastJumpIndex = index; // store the last recorded jump destination to add to the chain
                        }

                        index += jump;
                    }
                    else
                    {
                        jump += 1; // accumulate the jump
                        index += 1;
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
        public bool Remove(K key)
        {
            var hash = _equalityComparer.GetHashCode(key) | AddToHashToDistinguishFromEmptyOrRemoved;

            // @newSlots (if not empty) will become a new @slots, so the removed marker should be kept at the end
            var slots = _newSlots ?? _slots; // todo: not sure how to work out the removal from the new slots

            var capacity = slots.Length & (slots.Length << 2);
            var index = hash & (capacity - 1);
            var slot = slots[index];
            if (slot.Hash == hash && _equalityComparer.Equals(slot.Key, key))
            {
                if (Interlocked.CompareExchange(ref slots[index].Hash, HashOfRemoved, hash) == hash)
                    Interlocked.Decrement(ref _count);
                return true;
            }

            var jump = slot.FirstAndNextJump & FirstJumpBits;
            while (jump != 0)
            {
                slot = slots[index += jump];
                if (slot.Hash == hash && key.Equals(slot.Key))
                {   // Mark as removed with the special HashOfRemoved value
                    if (Interlocked.CompareExchange(ref slots[index].Hash, HashOfRemoved, hash) == hash)
                        Interlocked.Decrement(ref _count);
                    return true;
                }

                jump = slot.FirstAndNextJump >> ShiftToNextJumpBits;
            }

            return false;
        }
    }

}
