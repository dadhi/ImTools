using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ImTools
{
    public class LightTree<K, V>
    {
        public static readonly LightTree<K, V> Empty = new EmptyTree();

        public int Hash { get { return _payload.Hash; } }

        public K Key { get { return _payload.Key; } }
        public V Value { get { return _payload.Value; } }

        public virtual bool IsEmpty { get { return false; } }
        public virtual int Height { get { return 1; } }
        public virtual LightTree<K, V> Left { get { return Empty; } }
        public virtual LightTree<K, V> Right { get { return Empty; } }

        /// <summary>Returns true if key is found and sets the value.</summary>
        /// <param name="key">Key to look for.</param> <param name="value">Result value</param>
        /// <returns>True if key found, false otherwise.</returns>
        [MethodImpl(MethodImplHints.AggressingInlining)]
        public bool TryFind(K key, out V value)
        {
            var t = this;
            var hash = key.GetHashCode();
            while (t.Height != 0 && t.Hash != hash)
                t = hash < t.Hash ? t.Left : t.Right;

            if (t.Height != 0 && (ReferenceEquals(key, t.Key) || key.Equals(t.Key)))
            {
                value = t.Value;
                return true;
            }

            return t.TryFindConflictedValue(key, out value);
        }

        private bool TryFindConflictedValue(K key, out V value)
        {
            if (Height != 0)
            {
                var payloadWithConflicts = _payload as PayloadWithConflicts;
                if (payloadWithConflicts != null)
                {
                    var conflicts = payloadWithConflicts.Conflicts;
                    for (var i = conflicts.Length - 1; i >= 0; --i)
                        if (Equals(conflicts[i].Key, key))
                        {
                            value = conflicts[i].Value;
                            return true;
                        }
                }
            }

            value = default(V);
            return false;
        }


        #region Implementation

        protected class Payload
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


        private readonly Payload _payload;

        protected LightTree(Payload payload)
        {
            _payload = payload;
        }

        private sealed class EmptyTree : LightTree<K, V>
        {
            public EmptyTree() : base(null) { }

            public override bool IsEmpty { get { return true; } }
            public override int Height { get { return 0; } }
        }

        private sealed class LightTreeBranch : LightTree<K, V>
        {
            public override int Height { get { return _height; } }
            public override LightTree<K, V> Left { get { return _left; } }
            public override LightTree<K, V> Right { get { return _right; } }

            private readonly LightTree<K, V> _left, _right;
            private readonly int _height;

            private LightTreeBranch(Payload payload, LightTree<K, V> left, LightTree<K, V> right)
                : base(payload)
            {
                _left = left;
                _right = right;
                _height = 1 + (left.Height > right.Height ? left.Height : right.Height);
            }
        }

        #endregion
    }
}
