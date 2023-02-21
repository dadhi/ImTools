using System;
using System.Collections.Generic;

namespace ImTools.V2
{
    /// <summary>Immutable array based on wide hash tree, where each node is sub-array with predefined size: 32 is by default.
    /// Array supports only append, no remove.</summary>
    public class ImArray<T>
    {
        /// <summary>Node array size. When the item added to same node, array will be copied. 
        /// So if array is too big performance will degrade. Should be power of two: e.g. 2, 4, 8, 16, 32...</summary>
        public const int NODE_ARRAY_SIZE = 32;

        /// <summary>Empty/default value to start from.</summary>
        public static readonly ImArray<T> Empty = new ImArray<T>(0);

        /// <summary>Number of items in array.</summary>
        public readonly int Length;

        /// <summary>Appends value and returns new array.</summary>
        public virtual ImArray<T> Append(T value) =>
            Length < NODE_ARRAY_SIZE
                ? new ImArray<T>(Length + 1, _items.AppendOrUpdate(value))
                : new Tree(Length, ImMap<object>.Empty.AddOrUpdate(0, _items)).Append(value);

        /// <summary>Returns item stored at specified index. Method relies on underlying array for index range checking.</summary>
        /// <param name="index">Index to look for item.</param> <returns>Found item.</returns>
        /// <exception cref="ArgumentOutOfRangeException">from underlying node array.</exception>
        public virtual object Get(int index) => _items[index];

        /// <summary>Returns index of first equal value in array if found, or -1 otherwise.</summary>
        /// <param name="value">Value to look for.</param> <returns>Index of first equal value, or -1 otherwise.</returns>
        public virtual int IndexOf(T value)
        {
            if (_items == null || _items.Length == 0)
                return -1;

            for (var i = 0; i < _items.Length; ++i)
            {
                var item = _items[i];
                if (ReferenceEquals(item, value) || Equals(item, value))
                    return i;
            }
            return -1;
        }

        #region Implementation

        private readonly object[] _items;

        private ImArray(int length, object[] items = null)
        {
            Length = length;
            _items = items;
        }

        private sealed class Tree : ImArray<T>
        {
            private const int NODE_ARRAY_BIT_MASK = NODE_ARRAY_SIZE - 1; // for length 32 will be 11111 binary.
            private const int NODE_ARRAY_BIT_COUNT = 5;                  // number of set bits in NODE_ARRAY_BIT_MASK.

            public override ImArray<T> Append(T value)
            {
                var key = Length >> NODE_ARRAY_BIT_COUNT;
                var nodeItems = _tree.GetValueOrDefault(key) as object[];
                return new Tree(Length + 1, _tree.AddOrUpdate(key, nodeItems.AppendOrUpdate(value)));
            }

            public override object Get(int index) =>
                ((object[])_tree.GetValueOrDefault(index >> NODE_ARRAY_BIT_COUNT))[index & NODE_ARRAY_BIT_MASK];

            public override int IndexOf(T value)
            {
                foreach (var node in _tree.Enumerate())
                {
                    var nodeItems = (object[])node.Value;
                    if (!nodeItems.IsNullOrEmpty())
                    {
                        for (var i = 0; i < nodeItems.Length; ++i)
                        {
                            var item = nodeItems[i];
                            if (ReferenceEquals(item, value) || Equals(item, value))
                                return node.Key << NODE_ARRAY_BIT_COUNT | i;
                        }
                    }
                }

                return -1;
            }

            public Tree(int length, ImMap<object> tree)
                : base(length)
            {
                _tree = tree;
            }

            private readonly ImMap<object> _tree;
        }

        #endregion
    }
}
