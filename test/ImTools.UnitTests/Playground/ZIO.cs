// The source repository
// https://github.com/dadhi/ImTools/blob/zio_from_scratch/test/ImTools.UnitTests/Playground/ZIO.cs

// ZIO from scratch 1 and a bit of 2
// 

using System;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;
using static ImTools.UnitTests.Playground.Z;

namespace ImTools.UnitTests.Playground
{
	public interface Z<out A>
    {
        void Run(Action<A> consume);
    }

    // TODO @perf don't optimize too much until Run is implemented as a iterative loop with pattern matching over the cases, preventing the StackOverflowException
	
    public sealed record ZVal<A>(A Value) : Z<A>
    {
        public void Run(Action<A> consume) => consume(Value);
    }

    public sealed record ZLazy<A>(Func<A> Get) : Z<A>
    {
        public void Run(Action<A> consume) => consume(Get());
    }

    public sealed record ZAsync<A>(Action<Action<A>> Act) : Z<A>
    { 
        public void Run(Action<A> consume) => Act(consume);
    }
	
	public sealed record ZFork<A>(Z<A> Za) : Z<ZFiber<A>>
    { 
        public void Run(Action<ZFiber<A>> consume) => consume(new ZFiberImpl<A>(Za));
	}

    public sealed record ZMap<A, B>(Z<A> Za, Func<A, B> M) : Z<B>
    {
        public void Run(Action<B> consume) => Za.Run(a => consume(M(a)));
    }

    public sealed record ZThen<A, B>(Z<A> Za, Func<A, Z<B>> From) : Z<B>
    {
        public void Run(Action<B> consume) => Za.Run(a => From(a).Run(consume));
    }

    public sealed record ZZip<A, B>(Z<A> Za, Z<B> Zb) : Z<(A, B)>
    { 
        public void Run(Action<(A, B)> consume) => Za.Run(a => Zb.Run(b => consume((a, b))));
    }
	
	public interface ZFiber<out A>
	{
		Z<A> Join();
	}

	sealed class ZFiberImpl<A> : ZFiber<A>
	{			
		abstract record State;
		sealed record Result(A A) : State;
		sealed record Callbacks(Action<A> Act, Callbacks Rest) : State;
		
		State _state;
		public readonly Z<A> Za;
		
		public ZFiberImpl(Z<A> za) 
		{
			Za = za;
			// todo: @wip change to the SynchronizationContext.Post or the async/await flow to proceed on the same context when not forked
			// todo: @wip What about task returned by Task.Run, cancellation, etc.
			Task.Run(() => Za.Run(a => 
			{
				var state = Interlocked.Exchange(ref _state, new Result(a));
				if (state is Callbacks (var act, var rest))
					for (; act != null; act = rest.Act)
						act(a);
			}));
		}

		public Z<A> Join() => _state switch
		{
			Result r => r.A.Val(),
			_ => Z.Async<A>(act => Tools.Swap(ref _state, act, (a, s) => 
			{
				if (s is Result res)
				{
					a(res.A);
					return s;
				}
				return new Callbacks(a, s as Callbacks);
			}))
		};
	}
	
	internal static class Tools
	{
		public static T Swap<A, T>(ref T value, A a, Func<A, T, T> getNewValue, int retryCountUntilThrow = 50)
            where T : class
        {
            var spinWait = new SpinWait();
            var retryCount = 0;
            while (true)
            {
                var oldValue = value;
                var newValue = getNewValue(a, oldValue);
                if (Interlocked.CompareExchange(ref value, newValue, oldValue) == oldValue)
                    return oldValue;

                if (++retryCount > retryCountUntilThrow)
                    ThrowRetryCountExceeded(retryCountUntilThrow);
                spinWait.SpinOnce();
            }
        }
		
		private static void ThrowRetryCountExceeded(int retryCountExceeded) =>
            throw new InvalidOperationException(
                $"Ref retried to Update for {retryCountExceeded} times But there is always someone else intervened.");
	}

    public static class Z
    {
		public readonly struct Unit {}
		public static readonly Unit unit = default(Unit); 
			
        // Construction
        public static Z<A> Val<A>(this A a) => new ZVal<A>(a);
        public static Z<A> Get<A>(Func<A> getA) => new ZLazy<A>(getA);
		public static Z<Unit> Do(Action act) => new ZLazy<Unit>(() => { act(); return unit; });
        public static Z<A> Async<A>(Action<Action<A>> act) => new ZAsync<A>(act); // TODO @wip convert Action<Action<A>> to more general Func<Func<A, ?>, ?> or provide the separate case class 

        public static Z<B> To<A, B>(this Z<A> za, Func<A, B> map) => new ZMap<A, B>(za, map);
        public static Z<B> ToVal<A, B>(this Z<A> za, B b) => new ZMap<A, B>(za, _ => b); // TODO @perf optimize allocations
        public static Z<B> ToGet<A, B>(this Z<A> za, Func<B> getB) => new ZMap<A, B>(za, _ => getB()); // TODO @perf optimize allocations

        /// <summary>This is Bind, SelectMany or FlatMap... but I want to be unique and go with Then for now as it seems to have a more precise meaning IMHO</summary>
        public static Z<B> Then<A, B>(this Z<A> za, Func<A, Z<B>> from) => new ZThen<A, B>(za, from);

        public static Z<(A, B)> Zip<A, B>(this Z<A> za, Z<B> zb) => new ZZip<A, B>(za, zb);

        public static Z<ZFiber<A>> Fork<A>(this Z<A> za) => new ZFork<A>(za);
		
        public static Z<(A, B)> ZipPar<A, B>(this Z<A> za, Z<B> zb) => 
			from af in za.Fork()
			from b in zb
			from a in af.Join()
			select (a, b);
			
    }

    public static class ZLinq
    {
        public static Z<R> Select<A, R>(this Z<A> za, Func<A, R> selector) => za.To(selector);
        public static Z<R> SelectMany<A, R>(this Z<A> za, Func<A, Z<R>> next) => za.Then(next);
        public static Z<R> SelectMany<A, B, R>(this Z<A> za, Func<A, Z<B>> getZb, Func<A, B, R> project) =>
            za.Then(a => getZb(a).Then(b => project(a, b).Val()));
    }
    
    public class Tests
    {
		int _id;
		public int Id() => Interlocked.Increment(ref _id);   
		
        public Z<string> Map_small() =>
           	Z.Val(42).To(x => x + "!");

        public Z<int> Async_sleep() =>
            Async<int>(run =>
			{
				var id = Id();
                WriteLine($"Sleeping {id}");
                Thread.Sleep(500);
                WriteLine($"Woken {id}");
                run(42);
            });

		
		public Z<int> Get_sleep() =>
            Get(() => 
			{
				var id = Id();
                WriteLine($"Sleeping {id}");
                Thread.Sleep(300);
				WriteLine($"Woken {id}");
                return 43;
            });
		
		public Z<int> Async_seq() =>
            from _ in Z.Do(() => WriteLine("Before Async_seq.."))
			from a in Async_sleep()
			from b in Async_sleep()
			from _1 in Z.Do(() => WriteLine("After Async_seq"))
			select a + b;
		
		public Z<int> Get_seq() =>
            from _ in Z.Do(() => WriteLine("Before Get_seq.."))
			from a in Get_sleep()
			from b in Get_sleep()
			from _1 in Z.Do(() => WriteLine("After Get_seq"))
			select a + b + 1;
		
		public Z<int> Async_fork() =>
            from _ in Z.Do(() => WriteLine("Before Async_fork.."))
			from fa in Async_sleep().Fork()
			from fb in Async_sleep().Fork()
			from _1 in Z.Do(() => WriteLine("After Async_fork"))
			from a in fa.Join()
			from b in fb.Join()
			select a + b + 2;
		
		public Z<int> Zip_par() =>
            from _ in Z.Do(() => WriteLine("Before ZipPar.."))
			from x in Z.ZipPar(Async_sleep(), Async_sleep())
			from _1 in Z.Do(() => WriteLine("After ZipPar"))
			select x.Item1 + x.Item2 + 3;
    }
	
	public class Program
	{
		public static void Main()
		{
			var t = new Tests();
			
			void run<A>(Z<A> z, string name = "") { WriteLine(name + " >> "); z.Run(x => WriteLine(x)); Write("\n"); }
			
			run(t.Map_small(),   nameof(t.Map_small));
			run(t.Async_sleep(), nameof(t.Async_sleep));
			run(t.Async_seq(),   nameof(t.Async_seq));
			
			run(t.Async_fork(),  nameof(t.Async_fork));
			Thread.Sleep(1500);

			//run(t.Get_sleep(),   nameof(t.Get_sleep));
			//run(t.Get_seq(),     nameof(t.Get_seq));
		
			run(t.Zip_par(),     nameof(t.Zip_par));
			Thread.Sleep(1500);
			
			WriteLine("==DONE==");
		}
	}
}