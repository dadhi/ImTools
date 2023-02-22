namespace ImTools.Experiments
{
    // todo: @perf improve the performance by storing the hash, especially when we expanding the hash map?
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

    public struct X<T> {}

    public struct RefEqHashMap<K, V> where K : class
    {
        //todo: @wip can we put first N slot on the stack here like in `ImTools.MapStack`
        private int[] _hashes;
        public RefKeyValue<K, V>[] _slots;
        private const int AddToHashToDistinguishFromEmpty = 1;

        // <summary>The actual capacity is calculated as 2^capacityBits, e.g. 2^2 = 4 slots, 2^3 = 8 slots, etc.</summary>
        public RefEqHashMap(int capacityBits)
        {
            _hashes = new int[1 << capacityBits];
            _slots = new RefKeyValue<K, V>[1 << capacityBits];
        }

        /// <summary>Returns the index of Slot with the give key or `-1` otherwise</summary>
        public int IndexOf(K key)
        {
            var slots = _slots;
            var capacity = slots.Length;
            var capacityMask = capacity - 1;
            var hash = key.GetHashCode() | AddToHashToDistinguishFromEmpty;

            var idealIndex = hash & capacityMask;
            var h = _hashes[idealIndex];
            if (h == 0)
                return -1;
            if (h == hash && slots[idealIndex].Key == key)
                return idealIndex;

            var hashes = _hashes;
            capacity >>= 2; // search only for quarter of capacity
            for (var distance = 1; distance <= capacity; ++distance)
            {
                var i = (hash + distance) & capacityMask;
                h = hashes[i];
                if (h == 0) // not found, stop on an empty key
                    break;
                if (h == hash && slots[i].Key == key)
                    return i;
            }

            return -1;
        }

        public V GetValueOrDefault(K key) 
        {
            var index = IndexOf(key);
            return index == -1 ? default(V) : _slots[index].Value;   
        }

        public void AddOrUpdate(K key, V value)
        { 
            var slots = _slots;
            var hashes = _hashes;
            var capacity = slots.Length;
            var hash = key.GetHashCode() | AddToHashToDistinguishFromEmpty;
            if (TryPut(slots, hashes, (ushort)(capacity >> 2), capacity - 1, hash, key, value))
                return;

            // We got here because we did not find the empty slot to put the item from the ideal index to the distance equal quarter of capacity.  
            // Expand slots in a loop (if needed, hopefully single iteration is enough).
            // Expanding slots will double the capacity.
            var success = false;
            while (!success) 
            {
                expand:
                capacity <<= 1; // double the capacity
                var newSlots = new RefKeyValue<K, V>[capacity];
                var newHashes = new int[capacity];

                var capacityMask = capacity - 1;
                var searchDistance = (ushort)(capacity >> 2); // search only for the quarter of capacity
                for (var i = 0; i < slots.Length; ++i)
                {
                    // for every non-empty old slot, copy the item to the new slots
                    var h = hashes[i];
                    if (h != 0 && !(success = TryCopy(newSlots, newHashes, searchDistance, capacityMask, h, ref slots[i])))
                        goto expand; // if unable to put the item, try expand further
                }

                // if we were able to put the item, set the slots to the new ones
                if (success = TryPut(newSlots, newHashes, searchDistance, capacityMask, hash, key, value))
                {
                    _slots = newSlots;
                    _hashes = newHashes;
                }
            }
        }

        private static bool TryPut(RefKeyValue<K, V>[] slots, int[] hashes, ushort searchDistance, int capacityMask, int hash, K key, V value)
        {
            var idealIndex = hash & capacityMask;
            var h = hashes[idealIndex];
            // add the new item
            if (h == 0)
            {
                hashes[idealIndex] = hash;
                ref var idealSlot = ref slots[idealIndex];
                idealSlot.Key = key;
                idealSlot.Value = value;
                return true;
            }
            // update the existing item value
            if (h == hash && slots[idealIndex].Key == key)
            {
                slots[idealIndex].Value = value;
                return true;
            }
            for (ushort distance = 1; distance <= searchDistance; ++distance)
            {
                // wrap around the array boundaries and start from the beginning
                var index = (hash + distance) & capacityMask;
                h = hashes[index];
                if (h == 0)
                {
                    hashes[index] = hash;
                    ref var slot = ref slots[index];
                    slot.Key = key;
                    slot.Value = value;
                    return true;
                }
                if (h == hash && slots[index].Key == key)
                {
                    slots[index].Value = value;
                    return true;
                }
            }
            return false;
        }

        private static bool TryCopy(RefKeyValue<K, V>[] newSlots, int[] newHashes, ushort searchDistance, int capacityMask, int hash, ref RefKeyValue<K, V> oldSlot)
        {
            var i = hash & capacityMask;
            var h = newHashes[i];
            if (h == 0)
            {
                newHashes[i] = hash;
                newSlots[i] = oldSlot;
                return true;
            }
            for (ushort distance = 1; distance <= searchDistance; ++distance)
            {
                i = (hash + distance) & capacityMask;
                h = newHashes[i];
                if (h == 0)
                {
                    newHashes[i] = hash;
                    newSlots[i] = oldSlot;
                    return true;
                }
            }
            return false;
        }
    }
}
