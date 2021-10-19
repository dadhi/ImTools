using System;
using NUnit.Framework;

namespace ImTools.Benchmarks
{
    using static Z;
    public interface Z<out A>
    {
        void Run(Action<A> consume);
    }

    public static class Z
    {
        public sealed class ZVal<A> : Z<A>
        {
            public readonly A Value;
            public ZVal(A value) => Value = value; 
            public void Run(Action<A> consume) => consume(Value);
        }

        public sealed class ZLazy<A> : Z<A>
        {
            public readonly Func<A> GetValue;
            public ZLazy(Func<A> getValue) => GetValue = getValue; 
            public void Run(Action<A> consume) => consume(GetValue());
        }

        public sealed class ZMap<A, B> : Z<B>
        {
            public readonly Z<A> ZA;
            public readonly Func<A, B> M;
            public ZMap(Z<A> za, Func<A, B> m) { ZA = za; M = m; } 
            public void Run(Action<B> consume) => ZA.Run(a => consume(M(a)));
        }

        public static Z<A> Val<A>(A a) => new ZVal<A>(a);
        public static Z<A> Lazy<A>(Func<A> getA) => new ZLazy<A>(getA);

        public static Z<B> Map<A, B>(this Z<A> za, Func<A, B> map) => new ZMap<A, B>(za, map);
        
        /// <summary>Conventional method with the long name to disencourage the usage</summary>
        public static A GetUnsafeOrDefault<A>(this Z<A> za, A a = default) 
        {
            var result = a;
            za.Run(x => result = x);
            return result;
        }
    }
    
    [TestFixture]
    public class Tests
    {
        [Test]
        public static void Test1()
        {
            Val(42).Map(x => x + "!").Run(x => Assert.AreEqual("42!", x));
        }
    }
}