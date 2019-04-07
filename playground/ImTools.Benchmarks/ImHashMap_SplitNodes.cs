using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices; // For [MethodImpl(AggressiveInlining)]

namespace ImTools.Experimental.SplitNodes
{
    /// Immutable http://en.wikipedia.org/wiki/AVL_tree 
    /// where node key is the hash code of <typeparamref name="K"/>.
    public class ImHashMap<K, V>
    {
        /// Empty tree to start with.
        public static readonly ImHashMap<K, V> Empty = new Branch();

        /// Returns true if tree is empty. Valid for a `Branch`.
        public bool IsEmpty => this == Empty;

        /// Calculated key hash.
        public readonly int Hash;

        /// Key of type K that should support <see cref="object.Equals(object)"/> and <see cref="object.GetHashCode"/>
        public readonly K Key;

        /// Value of any type V.
        public readonly V Value;

        /// The height.
        public int Height => this is Branch b ? b.BranchHeight : 1;

        /// Left branch.
        public ImHashMap<K, V> Left => (this as Branch)?.LeftNode;

        /// Right branch.
        public ImHashMap<K, V> Right => (this as Branch)?.RightNode;

        /// Conflicts
        public virtual KV<K, V>[] Conflicts => null;

        /// The branch node
        public class Branch : ImHashMap<K, V>
        {
            /// Left sub-tree/branch, or `null`!
            public readonly ImHashMap<K, V> LeftNode;

            /// Right sub-tree/branch, or `null`!
            public readonly ImHashMap<K, V> RightNode;

            /// Height of longest sub-tree/branch plus 1. It is 0 for empty tree, and 1 for single node tree.
            public readonly int BranchHeight;

            /// Empty map
            public Branch() { }

            /// Constructs the branch node
            public Branch(int hash, K key, V value, ImHashMap<K, V> left, ImHashMap<K, V> right)
                : base(hash, key, value)
            {
                LeftNode = left;
                RightNode = right;
                var leftHeight = left == null ? 0 : left is Branch lb ? lb.BranchHeight : 1;
                var rightHeight = right == null ? 0 : right is Branch rb ? rb.BranchHeight : 1;
                BranchHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
            }

            /// Creates branch with known heights of left and right
            public Branch(int hash, K key, V value, int leftHeight, ImHashMap<K, V> left, int rightHeight, ImHashMap<K, V> right)
                : base(hash, key, value)
            {
                LeftNode = left;
                RightNode = right;
                BranchHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
            }

            /// Creates branch with known height of left sub-tree
            public Branch(int hash, K key, V value, int leftHeight, ImHashMap<K, V> left, ImHashMap<K, V> right)
                : base(hash, key, value)
            {
                LeftNode = left;
                RightNode = right;
                var rightHeight = right == null ? 0 : right is Branch rb ? rb.BranchHeight : 1;
                BranchHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
            }

            /// Creates branch with known height of right sub-tree
            public Branch(int hash, K key, V value, ImHashMap<K, V> left, int rightHeight, ImHashMap<K, V> right)
                : base(hash, key, value)
            {
                LeftNode = left;
                RightNode = right;
                var leftHeight = left == null ? 0 : left is Branch lb ? lb.BranchHeight : 1;
                BranchHeight = leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
            }

            /// Creates the branch node with known height
            public Branch(int hash, K key, V value, ImHashMap<K, V> left, ImHashMap<K, V> right, int height)
                : base(hash, key, value)
            {
                LeftNode = left;
                RightNode = right;
                BranchHeight = height;
            }
        }

        /// Branch with the conflicts
        public sealed class ConflictsLeaf : ImHashMap<K, V>
        {
            /// In case of Hash conflicts for different keys contains conflicted keys with their values.
            public override KV<K, V>[] Conflicts { get; }

            /// Creates the branch node
            internal ConflictsLeaf(
                int hash, K key, V value, KV<K, V>[] conflicts) : base(hash, key, value)
            {
                Conflicts = conflicts;
            }
        }

        /// Branch with the conflicts
        public sealed class ConflictsBranch : Branch
        {
            /// In case of Hash conflicts for different keys contains conflicted keys with their values.
            public override KV<K, V>[] Conflicts { get; }

            /// Creates the branch node
            internal ConflictsBranch(
                int hash, K key, V value, KV<K, V>[] conflicts,
                ImHashMap<K, V> left, ImHashMap<K, V> right) :
                base(hash, key, value, left, right)
            {
                Conflicts = conflicts;
            }

            /// Creates the branch node with known height
            internal ConflictsBranch(
                int hash, K key, V value, KV<K, V>[] conflicts,
                ImHashMap<K, V> left, ImHashMap<K, V> right, int height) :
                base(hash, key, value, left, right, height)
            {
                Conflicts = conflicts;
            }
        }

        /// <summary>Returns new tree with added key-value. 
        /// If value with the same key is exist then the value is replaced.</summary>
        /// <param name="key">Key to add.</param><param name="value">Value to add.</param>
        /// <returns>New tree with added or updated key-value.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public ImHashMap<K, V> AddOrUpdate(K key, V value) =>
            this == Empty
                ? new ImHashMap<K, V>(key.GetHashCode(), key, value)
                : AddOrUpdate(key.GetHashCode(), key, value);

        /// <summary>Returns new tree with added key-value. If value with the same key is exist, then
        /// if <paramref name="update"/> is not specified: then existing value will be replaced by <paramref name="value"/>;
        /// if <paramref name="update"/> is specified: then update delegate will decide what value to keep.</summary>
        /// <param name="key">Key to add.</param><param name="value">Value to add.</param>
        /// <param name="update">Update handler.</param>
        /// <returns>New tree with added or updated key-value.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public ImHashMap<K, V> AddOrUpdate(K key, V value, Update<V> update) =>
            this == Empty
                ? new ImHashMap<K, V>(key.GetHashCode(), key, value)
                : AddOrUpdate(key.GetHashCode(), key, value, update);

        /// Returns the previous value if updated.
        [MethodImpl((MethodImplOptions)256)]
        public ImHashMap<K, V> AddOrUpdate(K key, V value, out bool isUpdated, out V oldValue, Update<K, V> update = null)
        {
            isUpdated = false;
            oldValue = default(V);
            return this == Empty
                ? new ImHashMap<K, V>(key.GetHashCode(), key, value)
                : AddOrUpdate(key.GetHashCode(), key, value, update, ref isUpdated, ref oldValue);
        }

        /// <summary>Looks for <paramref name="key"/> and replaces its value with new <paramref name="value"/>, or 
        /// runs custom update handler (<paramref name="update"/>) with old and new value to get the updated result.</summary>
        /// <param name="key">Key to look for.</param>
        /// <param name="value">New value to replace key value with.</param>
        /// <param name="update">(optional) Delegate for custom update logic, it gets old and new <paramref name="value"/>
        /// as inputs and should return updated value as output.</param>
        /// <returns>New tree with updated value or the SAME tree if no key found.</returns>
        public ImHashMap<K, V> Update(K key, V value, Update<V> update = null) =>
            Update(key.GetHashCode(), key, value, update);

        /// <summary>Depth-first in-order traversal as described in http://en.wikipedia.org/wiki/Tree_traversal
        /// The only difference is using fixed size array instead of stack for speed-up (~20% faster than stack).</summary>
        /// <returns>Sequence of enumerated key value pairs.</returns>
        public IEnumerable<KV<K, V>> Enumerate()
        {
            if (this == Empty)
                yield break;

            var node = this;
            var height = node is Branch b ? b.BranchHeight : 1;
            var parents = new ImHashMap<K, V>[height];
            var parentCount = -1;
            while (height != 0 || parentCount != -1)
            {
                if (height != 0)
                {
                    parents[++parentCount] = node;
                    node = (node as Branch)?.LeftNode; // null is fine cause GetHeight handles the null
                }
                else
                {
                    node = parents[parentCount--];
                    yield return new KV<K, V>(node.Key, node.Value);

                    var conflicts = node.Conflicts;
                    if (conflicts != null)
                        for (var i = 0; i < conflicts.Length; i++)
                            yield return conflicts[i];

                    node = (node as Branch)?.RightNode;
                }

                height = node == null ? 0 : node is Branch br ? br.BranchHeight : 1;
            }
        }

        /// <summary>Removes or updates value for specified key, or does nothing if key is not found.
        /// Based on Eric Lippert http://blogs.msdn.com/b/ericlippert/archive/2008/01/21/immutability-in-c-part-nine-academic-plus-my-avl-tree-implementation.aspx </summary>
        /// <param name="key">Key to look for.</param> 
        /// <returns>New tree with removed or updated value.</returns>
        [MethodImpl((MethodImplOptions)256)]
        public ImHashMap<K, V> Remove(K key) =>
            Remove(key.GetHashCode(), key);

        /// <summary>Outputs key value pair</summary>
        public override string ToString() => IsEmpty ? "empty" : (Key + ":" + Value);

        #region Implementation

        private ImHashMap() { }

        /// Creates the leaf node
        protected ImHashMap(int hash, K key, V value)
        {
            Hash = hash;
            Key = key;
            Value = value;
        }

        /// It is fine
        private ImHashMap<K, V> AddOrUpdate(int hash, K key, V value, Update<V> update = null)
        {
            var branch = this as Branch;
            if (hash == Hash)
            {
                if (ReferenceEquals(Key, key) || Key.Equals(key))
                    return CreateNode(hash, key, update == null ? value : update(Value, value),
                        Conflicts, branch?.LeftNode, branch?.RightNode);
                return UpdateValueAndResolveConflicts(key, value, update, false);
            }

            return hash < Hash
                ? With(branch?.LeftNode?.AddOrUpdate(hash, key, value, update) ?? new ImHashMap<K, V>(hash, key, value), branch?.RightNode)
                : With(branch?.LeftNode, branch?.RightNode?.AddOrUpdate(hash, key, value, update) ?? new ImHashMap<K, V>(hash, key, value));
        }

        private ImHashMap<K, V> AddOrUpdate(int hash, K key, V value, Update<K, V> update, ref bool isUpdated, ref V oldValue)
        {
            var branch = this as Branch;
            if (hash == Hash)
            {
                if (ReferenceEquals(Key, key) || Key.Equals(key))
                {
                    if (update != null)
                        value = update(Key, Value, value);
                    if (ReferenceEquals(value, Value) || value?.Equals(Value) == true)
                        return this;

                    isUpdated = true;
                    oldValue = Value;
                    return branch == null
                        ? CreateLeaf(hash, key, value, Conflicts)
                        : CreateBranch(hash, key, value, Conflicts, branch.LeftNode, branch.RightNode, branch.BranchHeight);
                }

                if (Conflicts == null) // add only if updateOnly is false.
                    return branch == null
                        ? (ImHashMap<K, V>)new ConflictsLeaf(Hash, Key, Value, new[] { new KV<K, V>(key, value) })
                        : new ConflictsBranch(Hash, Key, Value, new[] { new KV<K, V>(key, value) },
                            branch.LeftNode, branch.RightNode, branch.BranchHeight);

                return UpdateValueAndResolveConflicts(key, value, update, false, ref isUpdated, ref oldValue);
            }

            if (hash < Hash)
            {
                if (branch == null)
                    return CreateBranch(Hash, Key, Value, Conflicts, new ImHashMap<K, V>(hash, key, value), null, 2);

                var oldLeft = branch.LeftNode;
                if (oldLeft == null)
                    return CreateBranch(Hash, Key, Value, Conflicts, new ImHashMap<K, V>(hash, key, value), branch.RightNode, branch.BranchHeight);

                var newLeft = oldLeft.AddOrUpdate(hash, key, value, update, ref isUpdated, ref oldValue);
                return newLeft == oldLeft ? this : Balance(Hash, Key, Value, Conflicts, newLeft, branch.RightNode);
            }
            else
            {
                if (branch == null)
                    return CreateBranch(Hash, Key, Value, Conflicts, null, new ImHashMap<K, V>(hash, key, value), 2);

                var oldRight = branch.RightNode;
                if (oldRight == null)
                    return CreateBranch(Hash, Key, Value, Conflicts, branch.LeftNode, new ImHashMap<K, V>(hash, key, value), branch.BranchHeight);

                var newRight = oldRight.AddOrUpdate(hash, key, value, update, ref isUpdated, ref oldValue);
                return newRight == oldRight ? this : Balance(Hash, Key, Value, Conflicts, branch.LeftNode, newRight);
            }
        }

        /// It is fine.
        public ImHashMap<K, V> Update(int hash, K key, V value, Update<V> update = null)
        {
            var branch = this as Branch;
            var height = branch?.BranchHeight ?? 1;
            if (height == 0)
                return this;

            return hash == Hash
                ? (ReferenceEquals(Key, key) || Key.Equals(key)
                    ? CreateNode(hash, key, update == null ? value : update(Value, value), Conflicts,
                        branch?.LeftNode, branch?.RightNode, height)
                    : UpdateValueAndResolveConflicts(key, value, update, true))
                : (hash < Hash
                    ? With(branch?.LeftNode?.Update(hash, key, value, update), branch?.RightNode)
                    : With(branch?.LeftNode, branch?.RightNode?.Update(hash, key, value, update)));
        }

        private ImHashMap<K, V> UpdateValueAndResolveConflicts(K key, V value, Update<V> update, bool updateOnly)
        {
            var br = this as Branch;
            var height = br?.BranchHeight ?? 1;
            if (Conflicts == null) // add only if updateOnly is false.
            {
                if (updateOnly)
                    return this;
                return CreateConflictsNode(Hash, Key, Value, new[] { new KV<K, V>(key, value) },
                    br?.LeftNode, br?.RightNode, height);
            }

            var found = Conflicts.Length - 1;
            while (found >= 0 && !Equals(Conflicts[found].Key, Key)) --found;
            if (found == -1)
            {
                if (updateOnly)
                    return this;
                var newConflicts = new KV<K, V>[Conflicts.Length + 1];
                Array.Copy(Conflicts, 0, newConflicts, 0, Conflicts.Length);
                newConflicts[Conflicts.Length] = new KV<K, V>(key, value);

                return CreateConflictsNode(Hash, Key, Value, newConflicts, br?.LeftNode, br?.RightNode, height);
            }

            var conflicts = new KV<K, V>[Conflicts.Length];
            Array.Copy(Conflicts, 0, conflicts, 0, Conflicts.Length);
            conflicts[found] = new KV<K, V>(key, update == null ? value : update(Conflicts[found].Value, value));

            return CreateConflictsNode(Hash, Key, Value, conflicts, br?.LeftNode, br?.RightNode, height);
        }

        private ImHashMap<K, V> UpdateValueAndResolveConflicts(
            K key, V value, Update<K, V> update, bool updateOnly, ref bool isUpdated, ref V oldValue)
        {
            var branch = this as Branch;
            var height = branch?.BranchHeight ?? 1;
            if (Conflicts == null) // add only if updateOnly is false.
            {
                if (updateOnly)
                    return this;
                return CreateConflictsNode(Hash, Key, Value, new[] { new KV<K, V>(key, value) },
                    branch?.LeftNode, branch?.RightNode, height);
            }

            var found = Conflicts.Length - 1;
            while (found >= 0 && !Equals(Conflicts[found].Key, Key)) --found;
            if (found == -1)
            {
                if (updateOnly)
                    return this;

                var newConflicts = new KV<K, V>[Conflicts.Length + 1];
                Array.Copy(Conflicts, 0, newConflicts, 0, Conflicts.Length);
                newConflicts[Conflicts.Length] = new KV<K, V>(key, value);

                return CreateConflictsNode(Hash, Key, Value, newConflicts, branch?.LeftNode, branch?.RightNode, height);
            }

            var conflicts = new KV<K, V>[Conflicts.Length];
            Array.Copy(Conflicts, 0, conflicts, 0, Conflicts.Length);

            if (update == null)
                conflicts[found] = new KV<K, V>(key, value);
            else
            {
                var conflict = conflicts[found];
                var newValue = update(conflict.Key, conflict.Value, value);
                if (ReferenceEquals(newValue, conflict.Value) || newValue?.Equals(conflict.Value) == true)
                    return this;

                isUpdated = true;
                oldValue = conflict.Value;
                conflicts[found] = new KV<K, V>(key, newValue);
            }

            return CreateConflictsNode(Hash, Key, Value, conflicts, branch?.LeftNode, branch?.RightNode, height);
        }

        /// It is fine.
        public V GetConflictedValueOrDefault(K key, V defaultValue)
        {
            if (Conflicts != null)
            {
                var conflicts = Conflicts;
                for (var i = conflicts.Length - 1; i >= 0; --i)
                    if (Equals(conflicts[i].Key, key))
                        return conflicts[i].Value;
            }
            return defaultValue;
        }

        /// It is fine.
        internal bool TryFindConflictedValue(K key, out V value)
        {
            if (Conflicts != null)
            {
                var conflicts = Conflicts;
                for (var i = conflicts.Length - 1; i >= 0; --i)
                    if (Equals(conflicts[i].Key, key))
                    {
                        value = conflicts[i].Value;
                        return true;
                    }
            }

            value = default(V);
            return false;
        }

        private ImHashMap<K, V> With(ImHashMap<K, V> left, ImHashMap<K, V> right)
        {
            var branch = this as Branch;
            if (left == branch?.LeftNode && right == branch?.RightNode)
                return this;
            return Balance(Hash, Key, Value, Conflicts, left, right);
        }

        private static ImHashMap<K, V> CreateNode(int hash, K key, V value, KV<K, V>[] conflicts,
            ImHashMap<K, V> left, ImHashMap<K, V> right)
        {
            if (left == null && right == null)
                return conflicts == null
                    ? new ImHashMap<K, V>(hash, key, value)
                    : new ConflictsLeaf(hash, key, value, conflicts);
            return conflicts == null
                ? new Branch(hash, key, value, left, right)
                : new ConflictsBranch(hash, key, value, conflicts, left, right);
        }

        private static ImHashMap<K, V> CreateNode(int hash, K key, V value, KV<K, V>[] conflicts,
            ImHashMap<K, V> left, ImHashMap<K, V> right, int height)
        {
            if (height == 1)
                return conflicts == null
                    ? new ImHashMap<K, V>(hash, key, value)
                    : new ConflictsLeaf(hash, key, value, conflicts);
            return conflicts == null
                ? new Branch(hash, key, value, left, right, height)
                : new ConflictsBranch(hash, key, value, conflicts, left, right, height);
        }

        [MethodImpl((MethodImplOptions)256)]
        private static ImHashMap<K, V> CreateNode(int hash, K key, V value,
            ImHashMap<K, V> left, ImHashMap<K, V> right, int height) =>
            height == 1
                ? new ImHashMap<K, V>(hash, key, value)
                : new Branch(hash, key, value, left, right, height);

        [MethodImpl((MethodImplOptions)256)]
        private static ImHashMap<K, V> CreateConflictsNode(int hash, K key, V value, KV<K, V>[] conflicts,
            ImHashMap<K, V> left, ImHashMap<K, V> right, int height) =>
            height == 1
                ? (ImHashMap<K, V>)new ConflictsLeaf(hash, key, value, conflicts)
                : new ConflictsBranch(hash, key, value, conflicts, left, right, height);

        [MethodImpl((MethodImplOptions)256)]
        private static Branch CreateBranch(int hash, K key, V value, KV<K, V>[] conflicts,
            ImHashMap<K, V> left, ImHashMap<K, V> right, int height) =>
            conflicts == null
                ? new Branch(hash, key, value, left, right, height)
                : new ConflictsBranch(hash, key, value, conflicts, left, right, height);

        [MethodImpl((MethodImplOptions)256)]
        private static Branch CreateBranch(int hash, K key, V value, KV<K, V>[] conflicts,
            ImHashMap<K, V> left, ImHashMap<K, V> right) =>
            conflicts == null
                ? new Branch(hash, key, value, left, right)
                : new ConflictsBranch(hash, key, value, conflicts, left, right);

        [MethodImpl((MethodImplOptions)256)]
        private static ImHashMap<K, V> CreateLeaf(int hash, K key, V value, KV<K, V>[] conflicts) =>
            conflicts == null
                ? new ImHashMap<K, V>(hash, key, value)
                : new ConflictsLeaf(hash, key, value, conflicts);

        private static ImHashMap<K, V> Balance(
            int hash, K key, V value, KV<K, V>[] conflicts, ImHashMap<K, V> left, ImHashMap<K, V> right)
        {
            var leftBranch = left as Branch;
            var leftHeight = left == null ? 0 : leftBranch?.BranchHeight ?? 1;

            var rightBranch = right as Branch;
            var rightHeight = right == null ? 0 : rightBranch?.BranchHeight ?? 1;

            var delta = leftHeight - rightHeight;
            if (delta > 1) // left is longer by 2, rotate left
            {
                // Also means that left is branch and not the leaf
                // ReSharper disable once PossibleNullReferenceException
                var leftLeft = leftBranch.LeftNode;
                var leftRight = leftBranch.RightNode;

                var llb = leftLeft as Branch;
                var lrb = leftRight as Branch;

                // The left is at least have height >= 2 and assuming that it is balanced, it should not have `null` left or right
                var leftLeftHeight = leftLeft == null ? 0 : llb?.BranchHeight ?? 1;
                var leftRightHeight = leftRight == null ? 0 : lrb?.BranchHeight ?? 1;
                if (leftRightHeight > leftLeftHeight)
                {
                    // double rotation:
                    //      5     =>     5     =>     4
                    //   2     6      4     6      2     5
                    // 1   4        2   3        1   3     6
                    //    3        1
                    if (lrb == null) // leftRight is a leaf node and the leftLeft is null
                    {
                        return CreateBranch(leftRight.Hash, leftRight.Key, leftRight.Value, leftRight.Conflicts,
                            CreateLeaf(left.Hash, left.Key, left.Value, left.Conflicts),
                            CreateNode(hash, key, value, conflicts, null, right, rightHeight + 1),
                            rightHeight + 2);
                    }

                    return CreateBranch(leftRight.Hash, leftRight.Key, leftRight.Value, leftRight.Conflicts,
                        CreateNode(left.Hash, left.Key, left.Value, left.Conflicts, leftLeft, lrb.LeftNode),
                        CreateNode(hash, key, value, conflicts, lrb.RightNode, right));
                }

                // one rotation:
                //      5     =>     2
                //   2     6      1     5
                // 1   4              4   6
                var newRightHeight = leftRightHeight > rightHeight ? leftRightHeight + 1 : rightHeight + 1;
                return CreateBranch(left.Hash, left.Key, left.Value, left.Conflicts,
                    leftLeft, CreateNode(hash, key, value, conflicts, leftRight, right, newRightHeight),
                    leftLeftHeight > newRightHeight ? leftLeftHeight + 1 : newRightHeight + 1);
            }

            if (delta < -1)
            {
                // ReSharper disable once PossibleNullReferenceException
                var rightLeft = rightBranch.LeftNode;
                var rightRight = rightBranch.RightNode;
                var rlb = rightLeft as Branch;
                var rrb = rightRight as Branch;

                var rightLeftHeight = rightLeft == null ? 0 : rlb?.BranchHeight ?? 1;
                var rightRightHeight = rightRight == null ? 0 : rrb?.BranchHeight ?? 1;
                if (rightLeftHeight > rightRightHeight)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    return CreateBranch(rightLeft.Hash, rightLeft.Key, rightLeft.Value, rightLeft.Conflicts,
                        CreateNode(hash, key, value, conflicts, left, rlb?.LeftNode),
                        CreateNode(right.Hash, right.Key, right.Value, right.Conflicts, rlb?.RightNode, rightRight));
                }

                var newLeftHeight = leftHeight > rightLeftHeight ? leftHeight + 1 : rightLeftHeight + 1;
                return CreateBranch(right.Hash, right.Key, right.Value, right.Conflicts,
                    CreateNode(hash, key, value, conflicts, left, rightLeft, newLeftHeight), rightRight,
                    newLeftHeight > rightRightHeight ? newLeftHeight + 1 : rightRightHeight + 1);
            }

            return CreateBranch(hash, key, value, conflicts, left, right, leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1);
        }

        internal ImHashMap<K, V> Remove(int hash, K key, bool ignoreKey = false)
        {
            var branch = this as Branch;
            var height = branch?.BranchHeight ?? 1;
            if (height == 0)
                return this;

            ImHashMap<K, V> result;
            if (hash == Hash) // found node
            {
                if (ignoreKey || Equals(Key, key))
                {
                    if (!ignoreKey && Conflicts != null)
                        return ReplaceRemovedWithConflicted();

                    if (height == 1) // remove node
                        return Empty;

                    if (branch?.RightNode == null)
                        result = branch?.LeftNode;
                    else if (branch.LeftNode == null)
                        result = branch.RightNode;
                    else
                    {
                        // we have two children, so remove the next highest node and replace this node with it.
                        var successor = branch?.RightNode;
                        while ((successor as Branch)?.LeftNode != null)
                            successor = (successor as Branch)?.LeftNode;

                        result = CreateNode(
                            successor.Hash, successor.Key, successor.Value, successor.Conflicts,
                            (successor as Branch)?.LeftNode,
                            (successor as Branch)?.RightNode.Remove(successor.Hash, default(K), ignoreKey: true));
                    }
                }
                else if (Conflicts != null)
                    return TryRemoveConflicted(key);
                else
                    return this; // if key is not matching and no conflicts to lookup - just return
            }
            else if (hash < Hash)
            {
                result = Balance(Hash, Key, Value, Conflicts, branch?.LeftNode.Remove(hash, key, ignoreKey), branch?.RightNode);
            }
            else
            {
                result = Balance(Hash, Key, Value, Conflicts, branch?.LeftNode, branch?.RightNode.Remove(hash, key, ignoreKey));
            }

            return result;
        }

        private ImHashMap<K, V> TryRemoveConflicted(K key)
        {
            var index = Conflicts.Length - 1;
            while (index >= 0 && !Equals(Conflicts[index].Key, key)) --index;
            if (index == -1) // key is not found in conflicts - just return
                return this;

            var branch = this as Branch;
            var height = branch?.BranchHeight ?? 1;
            if (Conflicts.Length == 1)
                return CreateNode(Hash, Key, Value, branch?.LeftNode, branch?.RightNode, height);

            var lessConflicts = new KV<K, V>[Conflicts.Length - 1];
            var newIndex = 0;
            for (var i = 0; i < Conflicts.Length; ++i)
                if (i != index) lessConflicts[newIndex++] = Conflicts[i];
            return CreateConflictsNode(Hash, Key, Value, lessConflicts, branch?.LeftNode, branch?.RightNode, height);
        }

        private ImHashMap<K, V> ReplaceRemovedWithConflicted()
        {
            var branch = this as Branch;
            var height = branch?.BranchHeight ?? 1;

            if (Conflicts.Length == 1)
                return CreateNode(Hash, Conflicts[0].Key, Conflicts[0].Value, branch?.LeftNode, branch?.RightNode, height);

            var lessConflicts = new KV<K, V>[Conflicts.Length - 1];
            Array.Copy(Conflicts, 1, lessConflicts, 0, lessConflicts.Length);
            return CreateConflictsNode(Hash, Conflicts[0].Key, Conflicts[0].Value, lessConflicts, branch?.LeftNode, branch?.RightNode, height);
        }

        #endregion
    }

    /// Map methods
    public static class ImHashMap
    {
        /// Looks for key in a tree and returns the key value if found, or <paramref name="defaultValue"/> otherwise.
        [MethodImpl((MethodImplOptions)256)]
        public static V GetValueOrDefault<K, V>(this ImHashMap<K, V> map, K key, V defaultValue = default(V))
        {
            var hash = key.GetHashCode();
            while (map != null)
            {
                if (map.Hash == hash)
                    return ReferenceEquals(key, map.Key) || key.Equals(map.Key)
                        ? map.Value
                        : map.GetConflictedValueOrDefault(key, defaultValue);

                map = hash < map.Hash
                    ? (map as ImHashMap<K, V>.Branch)?.LeftNode
                    : (map as ImHashMap<K, V>.Branch)?.RightNode;
            }

            return defaultValue;
        }

        /// Returns true if key is found and sets the value.
        [MethodImpl((MethodImplOptions)256)]
        public static bool TryFind<K, V>(this ImHashMap<K, V> map, K key, out V value)
        {
            var hash = key.GetHashCode();
            while (map != null)
            {
                if (map.Hash == hash)
                {
                    if (ReferenceEquals(key, map.Key) || key.Equals(map.Key))
                    {
                        value = map.Value;
                        return true;
                    }
                    return map.TryFindConflictedValue(key, out value);
                }

                map = hash < map.Hash
                    ? (map as ImHashMap<K, V>.Branch)?.LeftNode
                    : (map as ImHashMap<K, V>.Branch)?.RightNode;
            }

            value = default(V);
            return false;
        }
    }
}
