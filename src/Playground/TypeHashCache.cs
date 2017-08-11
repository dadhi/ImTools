using System;

namespace Playground
{
    public struct TypedValue<T>
    {
        public Type Key;
        public T Value;
    }

    public static class TypeHashCache
    {
        public const int InitialCapacity = 1 << 4;

        public static TypedValue<T>[] Empty<T>(int initialCapacity = InitialCapacity)
        {
            return new TypedValue<T>[initialCapacity + (initialCapacity >> 2)];
        }

        public static T GetOrDefault<T>(this TypedValue<T>[] slots, Type key)
        {
            var index = key.GetHashCode() & (slots.Length - 1);
            var slot = slots[index];
            if (slot.Key == key)
                return slot.Value;

            while (true)
            {
                slot = slots[++index];
                if (slot.Key == key)
                    return slot.Value;

                if (slot.Key == null || //stop on empty slot
                    index >= slots.Length)
                    break;
            }

            return default(T);
        }
    }
}
