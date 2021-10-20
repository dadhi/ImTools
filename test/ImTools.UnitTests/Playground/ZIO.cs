using System;
using NUnit.Framework;

namespace ImTools.ImTools.UnitTests.Playground
{
    using static Z;
    public interface Z<out A>
    {
        void Run(Action<A> consume);
    }

    // TODO @perf don't optimize too much until the classes are converted to the pure case records and Run is implemented as a iterative loop with pattern matching over the cases, preventing the callbacks StackOverflow

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

    public sealed class ZAsync<A> : Z<A>
    {
        public readonly Action<Action<A>> Delay;
        public ZAsync(Action<Action<A>> delay) => Delay = delay; 
        public void Run(Action<A> consume) => Delay(consume);
    }

    public sealed class ZMap<A, B> : Z<B>
    {
        public readonly Z<A> ZA;
        public readonly Func<A, B> M;
        public ZMap(Z<A> za, Func<A, B> m) { ZA = za; M = m; } 
        public void Run(Action<B> consume) => ZA.Run(a => consume(M(a)));
    }

    public sealed class ZThen<A, B> : Z<B>
    {
        public readonly Z<A> ZA;
        public readonly Func<A, Z<B>> From;
        public ZThen(Z<A> za, Func<A, Z<B>> from) { ZA = za; From = from; }
        public void Run(Action<B> consume) => ZA.Run(a => From(a).Run(consume));
    }

    public sealed class ZZip<A, B> : Z<(A, B)>
    {
        public readonly Z<A> ZA;
        public readonly Z<B> ZB;
        public ZZip(Z<A> za, Z<B> zb) { ZA = za; ZB = zb; } 
        public void Run(Action<(A, B)> consume) => ZA.Run(a => ZB.Run(b => consume((a, b))));
    }

    public static class Z
    {
        // Construction
        public static Z<A> Val<A>(A a) => new ZVal<A>(a);
        public static Z<A> Lazy<A>(Func<A> getA) => new ZLazy<A>(getA);
        public static Z<A> Async<A>(Action<Action<A>> delay) => new ZAsync<A>(delay); // TODO @wip convert Action<Action<A>> to more general Func<Func<A, ?>, ?> or provide the separate case class 

        public static Z<B> Map<A, B>(this Z<A> za, Func<A, B> map) => new ZMap<A, B>(za, map);
        public static Z<B> AsVal<A, B>(this Z<A> za, B b) => new ZMap<A, B>(za, _ => b); // TODO @perf optimize allocations
        public static Z<B> AsLazy<A, B>(this Z<A> za, Func<B> getB) => new ZMap<A, B>(za, _ => getB()); // TODO @perf optimize allocations

        /// <summary>This is Bind, SelectMany or FlatMap... but I want to be unique and go with Then for now as it seems to have a more precise meaning IMHO</summary>
        public static Z<B> Then<A, B>(this Z<A> za, Func<A, Z<B>> from) => new ZThen<A, B>(za, from);

        /// <summary>
        /// Lazily computes za then zb. 
        /// Maybe implemented in terms of `Then` but it is not for performance reasons
        /// Does it make sense to have a ZipReverse (Piz)? 
        ///</summary> 
        public static Z<(A, B)> Zip<A, B>(this Z<A> za, Z<B> zb) => new ZZip<A, B>(za, zb);

        /// <summary>Conventional method with the long name to disencourage the usage</summary>
        public static A GetUnsafeOrDefault<A>(this Z<A> za, A a = default) 
        {
            var result = a;
            za.Run(x => result = x);
            return result;
        }
    }

    public static class ZLinq
    {
        // TODO @wip implement Select as Map, SelectMany as Then, and Where as ??? 
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