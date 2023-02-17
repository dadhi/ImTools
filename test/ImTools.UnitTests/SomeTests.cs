using NUnit.Framework;
using System.Runtime.InteropServices;

#pragma warning disable CS0649

namespace ImTools.UnitTests
{
    [TestFixture]
    public class SomeTests
    {
        [Test]
        public void I_can_cast_specialized_instance_to_the_generic_type()
        {
            var eInt = CreateEntry(42);
            Assert.IsInstanceOf<IntEntry>(eInt);

            var eKey = CreateEntry("a");
            Assert.IsInstanceOf<KEntry<string>>(eKey);
        }

        static Entry<K> CreateEntry<K>(K key) => key switch
        {
            int k => new IntEntry(k) as Entry<K>,
            _ => new KEntry<K>(key, key.GetHashCode())
        };

        abstract record Entry<K>(K Key);
        record KEntry<K>(K Key, int Hash) : Entry<K>(Key);
        record IntEntry(int Key) : Entry<int>(Key) { }

        [Test, Ignore("Does not work in .NET 7")] // todo: @fixme
        public void The_empty_struct_takes_8_bytes()
        {
            int GetSize(object obj) => Marshal.ReadInt32(obj.GetType().TypeHandle.Value, 4);

            var e = new KEntry<string>("a", "a".GetHashCode());
            var ee = new EEntry<string>("a", "a".GetHashCode());

            var eSize = GetSize(e);
            var eeSize = GetSize(ee);
            Assert.Greater(eeSize, eSize);
        }

        readonly struct Empty {}
        record EEntry<K>(K Key, int Hash) : Entry<K>(Key)
        {
            public readonly Empty E;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IsExternalInit
    {
    }
}