using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImTools
{
    public class LightTree<K, V>
    {
        private class Payload
        {
            public readonly K Key;
            public readonly V Value;

            public readonly int Hash;

            public Payload(int hash, K key, V value)
            {
                Key = key;
                Value = value;
                Hash = hash;
            }
        }

        private sealed class PayloadWithConflicts : Payload
        {
            public readonly KeyValuePair<K, V>[] Conflicts;

            public PayloadWithConflicts(KeyValuePair<K, V>[] conflicts,
                int hash, K key, V value) : base(hash, key, value)
            {
                Conflicts = conflicts;
            }
        }

        public static readonly LightTree<K, V> Empty = new LightTree<K, V>();

        public readonly LightTree<K, V> Left, Right;

        public readonly int Height;

        public LightTree(Payload payload, LightTree<K, V> left, LightTree<K, V> right)
        {
            
        }

        private LightTree() { }
    }
}
