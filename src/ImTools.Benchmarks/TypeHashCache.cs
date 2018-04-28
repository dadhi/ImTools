using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImTools.Benchmarks
{
    public struct TypedValue<T>
    {
        public Type Key;
        public T Value;
    }

    public static class TypeHashCache
    {
        public const int InitialCapacity = 1 << 5; // 32

        public static TypedValue<T>[] Empty<T>(int initialCapacity = InitialCapacity)
        {
            // reserve quarter of capacity for hash conflicts
            return new TypedValue<T>[initialCapacity];
        }

        [MethodImpl((MethodImplOptions)256)]
        public static T GetValueOrDefault<T>(this TypedValue<T>[] slots, Type key)
        {
            var capacity = slots.Length;
            var capacityMask = capacity - 1;
            var hash = key.GetHashCode();

            capacity >>= 2; // search only for quarter of capacity
            for (var distance = 0; distance < capacity; ++distance)
            {
                var slot = slots[(hash + distance) & capacityMask];

                if (slot.Key == key)
                    return slot.Value;

                if (slot.Key == null) // not found, stop on an empty key
                    break;
            }

            return default(T);
        }

        [MethodImpl((MethodImplOptions)256)]
        public static T GetValueOrDefault_RefLocal<T>(this TypedValue<T>[] slots, Type key)
        {
            var capacity = slots.Length;
            var capacityMask = capacity - 1;
            var hash = key.GetHashCode();

            capacity >>= 2; // search only for quarter of capacity
            for (var distance = 0; distance < capacity; ++distance)
            {
                ref var slot = ref slots[(hash + distance) & capacityMask];

                if (slot.Key == key)
                    return slot.Value;

                if (slot.Key == null) // not found, stop on an empty key
                    break;
            }

            return default(T);
        }

        // may return new slots if old slot capacity is not enough to add a new item
        public static TypedValue<T>[] AddOrUpdate<T>(this TypedValue<T>[] slots, Type key, T value)
        {
            var capacity = slots.Length;
            var hash = key.GetHashCode();
            if (TryPut(slots, capacity, hash, key, value))
                return slots;

            // Expand slots: Create a new slots and re-populate them from the old slots.
            // Expanding slots will double the capacity.
            var newCapacity = capacity << 1;
            var newSlots = new TypedValue<T>[newCapacity];

            for (var i = 0; i < capacity; i++)
            {
                var slot = slots[i];
                var slotKey = slot.Key;
                if (slotKey != null)
                    TryPut(newSlots, newCapacity, slotKey.GetHashCode(), slotKey, slot.Value);
            }

            TryPut(newSlots, newCapacity, hash, key, value);
            return newSlots;
        }

        private static bool TryPut<T>(TypedValue<T>[] slots, int capacity, int hash, Type key, T value)
        {
            capacity = capacity >> 2; // search only for quarter of capacity
            for (var distance = 0; distance < capacity; ++distance)
            {
                var index = (hash + distance) & (capacity - 1);
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
