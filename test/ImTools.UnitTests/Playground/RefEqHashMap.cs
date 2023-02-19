using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools
{
    public struct RefKeyValue<K, V> where K : class
    {
        public K Key;
        public V Value;
        public RefKeyValue(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }

    public struct RefEqHashMap<K, V> where K : class
    {
        //todo: @wip can we put first N slot on the stack here like in `ImTools.MapStack`

        public RefKeyValue<K, V>[] _slots;

        public RefEqHashMap(int initialCapacity = RefEqHashMap.InitialCapacity) =>
            _slots = new RefKeyValue<K, V>[initialCapacity];

        /// <summary>Returns the index of Slot with the give key or `-1` otherwise</summary>
        public int IndexOf(K key) =>
            _slots.IndexOf(key);

        public V GetValueOrDefault(K key) => 
            _slots.GetValueOrDefault(key);

        public void AddOrUpdate(K key, V value) => 
            _slots = _slots.AddOrUpdate(key, value);
    }

    public static class RefEqHashMap
    {
        public static RefKeyValue<K, V> NewSlot<K, V>(K key, V value) where K : class =>
            new RefKeyValue<K, V>(key, value);

        public const int InitialCapacity = 1 << 5; // 32

        // reserve quarter of capacity for hash conflicts
        public static RefKeyValue<K, V>[] Empty<K, V>(int initialCapacity = InitialCapacity)
            where K : class =>
            new RefKeyValue<K, V>[initialCapacity];

        [MethodImpl((MethodImplOptions)256)]
        public static int IndexOf<K, V>(this RefKeyValue<K, V>[] slots, K key)
            where K : class
        {
            var capacity = slots.Length;
            var capacityMask = capacity - 1;
            var hash = key.GetHashCode();

            var idealIndex = hash & capacityMask;
            if (slots[idealIndex].Key == key)
                return idealIndex;

            capacity >>= 2; // search only for quarter of capacity
            for (var distance = 1; distance < capacity; ++distance)
            {
                var index = (hash + distance) & capacityMask;
                ref var slot = ref slots[index];
                if (slot.Key == key)
                    return index;
                if (slot.Key == null) // not found, stop on an empty key
                    break;
            }

            return -1;
        }

        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this RefKeyValue<K, V>[] slots, K key)
            where K : class
        {
            var index = slots.IndexOf(key);
            return index == -1 ? default(V) : slots[index].Value;
        }

        // may return new slots if old slot capacity is not enough to add a new item
        public static RefKeyValue<K, V>[] AddOrUpdate<K, V>(this RefKeyValue<K, V>[] slots, K key, V value)
            where K : class
        {
            var capacity = slots.Length;
            var hash = key.GetHashCode();
            if (TryPut(slots, capacity, hash, key, value))
                return slots;

            // Expand slots: Create a new slots and re-populate them from the old slots.
            // Expanding slots will double the capacity.
            var newCapacity = capacity << 1;
            var newSlots = new RefKeyValue<K, V>[newCapacity];

            for (var i = 0; i < capacity; i++)
            {
                ref var slot = ref slots[i];
                var slotKey = slot.Key;
                if (slotKey != null)
                    TryPut(newSlots, newCapacity, slotKey.GetHashCode(), slotKey, slot.Value);
            }

            TryPut(newSlots, newCapacity, hash, key, value);
            return newSlots;
        }

        private static bool TryPut<K, V>(RefKeyValue<K, V>[] slots, int capacity, int hash, K key, V value)
            where K : class
        {
            var capacityMask = capacity - 1;
            var idealIndex = hash & capacityMask;
            if (Interlocked.CompareExchange(ref slots[idealIndex].Key, key, null) == null)
            {
                slots[idealIndex].Value = value;
                return true;
            }

            capacity >>= 2; // search only for the quarter of capacity
            for (var distance = 1; distance < capacity; ++distance)
            {
                var index = (hash + distance) & capacityMask;
                if (Interlocked.CompareExchange(ref slots[index].Key, key, null) == null)
                {
                    slots[index].Value = value;
                    return true;
                }
            }
            return false;
        }
    }
}
