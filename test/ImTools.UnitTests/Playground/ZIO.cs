// The source repository
// https://github.com/dadhi/ImTools/blob/zio_from_scratch/test/ImTools.UnitTests/Playground/ZIO.cs

// Parts:
// [X] 1 - https://youtu.be/wsTIcHxJMeQ 
// [X] 2 - https://youtu.be/g8Tuqldu2AE
// [WIP] 3 - https://youtu.be/0IU9mGO_9Rw

// TODO:
// [X] Stack safety / the stack is big though - I got the stack overflow on repating the Do(() => WriteLine("x")) 87253 times
// [X] Reduce allocations on the lambda clusures and therefore improve the perf
// [ ] Make it work with async/await see how it is done in https://github.com/yuretz/FreeAwait/tree/master/src/FreeAwait
// [ ] Performance
// [ ] Work around the Task.Run or SynchronizaionContext
// [ ] Error handling
// [ ] Environment

using System;
using System.Threading;
using System.Threading.Tasks;
//using System.Runtime.CompilerServices;
using static System.Console;
using static ImTools.UnitTests.Playground.Z;

namespace ImTools.UnitTests.Playground
{
	public interface ZErased {}

	public interface Z<out A> : ZErased
    {
        void Run(Action<A> consume);
    }
	
	public abstract record ZImpl<A> : Z<A>
	{
		public void Run(Action<A> consume)
		{
			var z = this.Then(consume, (consume_, a) => Z.Do(a, consume_));
			
			// todo: @api await the fiber context - so make it awaitable
			new ZFiberContext<Empty>(z);
		}
	} 

	interface ZVal 
	{
		object Value { get; }
	}

	public sealed record ZVal<A>(A Value) : ZImpl<A>, ZVal 
	{
		object ZVal.Value => Value;
	}

	interface ZLazy 
	{
		object GetValue();
	}

	public sealed record ZLazy<A>(Func<A> GetValue) : ZImpl<A>, ZLazy
	{
		object ZLazy.GetValue() => GetValue();
	}

	public sealed record ZLazy<S, A>(S State, Func<S, A> GetValue) : ZImpl<A>, ZLazy
	{
		object ZLazy.GetValue() => GetValue(State);
	}

	public sealed record ZLazyDo(Action Act) : ZImpl<Empty>, ZLazy
	{
		object ZLazy.GetValue() { Act(); return empty; }
	}

	public sealed record ZLazyDo<S>(S State, Action<S> Act) : ZImpl<Empty>, ZLazy
	{
		object ZLazy.GetValue() { Act(State); return empty; }
	}
	
	interface ZThen
	{
		ZErased Za { get; }
		ZErased Cont(object a);
	}

    public sealed record ZThen<A, B>(Z<A> Za, Func<A, Z<B>> Cont) : ZImpl<B>, ZThen
    {
		ZErased ZThen.Za => Za;
		ZErased ZThen.Cont(object a) => Cont((A)a);
    }
	
	public sealed record ZThen<S, A, B>(Z<A> Za, S State, Func<S, A, Z<B>> Cont) : ZImpl<B>, ZThen
    {
		ZErased ZThen.Za => Za;
		ZErased ZThen.Cont(object a) => Cont(State, (A)a);
    }

	public sealed record ZThen<S1, S2, A, B>(Z<A> Za, S1 State1, S2 State2, Func<S1, S2, A, Z<B>> Cont) : ZImpl<B>, ZThen
    {
		ZErased ZThen.Za => Za;
		ZErased ZThen.Cont(object a) => Cont(State1, State2, (A)a);
    }

	interface ZAsync
	{
		void Schedule(object state, Action<object, object> run);
	}

	public sealed record ZAsync<A>(Action<object, object, Action<object, object, A>> Schedule) : ZImpl<A>, ZAsync
	{
		void ZAsync.Schedule(object state, Action<object, object> run) => Schedule(run, state, (run_, state_, a) => ((Action<object, object>)run_)(state_, a));
	}

	public sealed record ZAsync<S, A>(S State, Action<S, object, object, Action<object, object, A>> Schedule) : ZImpl<A>, ZAsync
	{
		void ZAsync.Schedule(object state, Action<object, object> run) => Schedule(State, run, state, (run_, state_, a) => ((Action<object, object>)run_)(state_, a));
	}
		
	public interface ZFiber<out A>
	{
		Z<A> Join();
		//Z<Empty> Interrupt() => throw new NotImplementedException("todo");
	}

	sealed record ZFiberContext<A> : ZFiber<A>
	{	
		public ZErased Za { get; private set; }
		public ZFiberContext(Z<A> za)
		{
			Za = za;
			Task.Run(RunLoop);
		}
		
		sealed record ContStack(ZThen Cont, ContStack Rest);
		ContStack _stack;

		abstract record EvalState;
		sealed record Done(A Value) : EvalState;
		sealed record Callbacks(Action<object, object, A> Act, object P0, object P1, Callbacks Rest) : EvalState;	
		EvalState _state;
		
		void Complete(A a)
		{
			var s = Interlocked.Exchange(ref _state, new Done(a));
			while (s is Callbacks (var act, var p0, var p1, var rest))
			{
				act(p0, p1, a);
				s = rest;
			}
		}
		
		public Z<A> Join() => Z.Async<ZFiberContext<A>, A>(this, (f, _, __, run) => 
		{
			if (f._state is Done doneFast)
				run(_, __, doneFast.Value);
			else
			{
				var spinWait = new SpinWait();
				while (true)
				{
					var s = f._state;
					if (s is Done done)
					{
						run(_, __, done.Value);
						break;
					}

					var callbacks = new Callbacks(run, _, __, s as Callbacks);
					if (Interlocked.CompareExchange(ref f._state, callbacks, s) == s)
						break;

					spinWait.SpinOnce();
				}
			}				
		});
		
		// returns true to proceed 
		private bool CompleteOrProceedWith(object val)
		{
			if (_stack is ContStack(var cont, var rest)) 
			{
				Za = cont.Cont(val);
				_stack = rest;
				return true;
			}
			Complete((A)val);
			return false;
		}
		
		void Resume(ZErased startZa)
		{ 
			Za = startZa;
			RunLoop();
		}
		
		void RunLoop()
		{
			var loop = true;
			while (loop)
			{
				switch(Za)
				{
					case ZVal v:
						loop = CompleteOrProceedWith(v.Value);
						break;

					case ZLazy l:
						loop = CompleteOrProceedWith(l.GetValue());
						break;

					case ZThen t:
						Za = t.Za;
						_stack = new ContStack(t, _stack);
						break;

					case ZAsync a:
						loop = false;
						if (_stack == null)
							a.Schedule(this, (f, x) => ((ZFiberContext<A>)f).Complete((A)x));
						else
							a.Schedule(this, (f, x) => ((ZFiberContext<A>)f).Resume(x.Val()));
						break;

					case var unknown:
						throw new InvalidOperationException("RunLoop does not recongnize " + unknown);
				}
			}
		}
	}
		
    public static class Z
    {
		public static ZFiber<A> RunUnsafeFiber<A>(this Z<A> za) => new ZFiberContext<A>(za);
		
		public static A RunUnsafe<A>(this Z<A> za)
		{
			using var e = new AutoResetEvent(false);
			A result = default;
			RunUnsafeFiber(za.Then(a => Z.Do(() => {
				result = a;
				e.Set();
			})));
			e.WaitOne();
			return result;
		}
			
        public static Z<A> Val<A>(this A a) => new ZVal<A>(a);
        
		public static Z<A> Get<A>(Func<A> getA) => new ZLazy<A>(getA);
		public static Z<A> Get<S, A>(S state, Func<S, A> getA) => new ZLazy<S, A>(state, getA);
		
		public static Z<Empty> Do(Action act) => new ZLazyDo(act);
		public static Z<Empty> Do<S>(in S state, Action<S> act) => new ZLazyDo<S>(state, act);
				
        public static Z<A> Async<A>(Action<object, object, Action<object, object, A>> schedule) => new ZAsync<A>(schedule);
		public static Z<A> Async<S, A>(in S state, Action<S, object, object, Action<object, object, A>> schedule) => new ZAsync<S, A>(state, schedule);
		
		/// <summary>This is Bind, SelectMany or FlatMap... but I want to be unique and go with Then for now as it seems to have a more precise meaning IMHO</summary>
        public static Z<B> Then<A, B>(this Z<A> za, Func<A, Z<B>> @from) => new ZThen<A, B>(za, @from);
		public static Z<B> Then<S, A, B>(this Z<A> za, S s, Func<S, A, Z<B>> @from) => new ZThen<S, A, B>(za, s, @from);
		public static Z<B> Then<S1, S2, A, B>(this Z<A> za, S1 s1, S2 s2, Func<S1, S2, A, Z<B>> @from) => new ZThen<S1, S2, A, B>(za, s1, s2, @from);

        public static Z<B> To<A, B>(this Z<A> za, Func<A, B> map) => za.Then(map, (map_, a) => map_(a).Val());
		
        public static Z<B> ToVal<A, B>(this Z<A> za, B b) => za.Then(b, (b_, _) => b_.Val());
        public static Z<B> ToGet<A, B>(this Z<A> za, Func<B> getB) => za.Then(getB, (getB_, _) => getB_().Val());

        public static Z<(A, B)> Zip<A, B>(this Z<A> za, Z<B> zb) => za.Then(zb, (zb_, a) => zb_.Then(a, (a_, b) => Val((a_, b))));		
		
		public static Z<C> ZipWith<A, B, C>(this Z<A> za, Z<B> zb, Func<A, B, C> zip) => za.Then(zip, zb, (zip_, zb_, a) => zb.Then(zip_, a, (zip__, a_, b) => zip__(a_, b).Val()));
		
		public static Z<A> And<A, B>(this Z<A> za, Z<B> zb) => za.Then(zb, (zb_, a) => zb_.Then(a, (a_, _) => a_.Val()));
		
		public static Z<A> RepeatN<A>(this Z<A> za, int n) => n <= 1 ? za : za.And(za.RepeatN(n - 1));
		
        public static Z<ZFiber<A>> Fork<A>(this Z<A> za) => Get(za, z => new ZFiberContext<A>(z));
		
		// Here is the reference implementation of ZipPar with Linq paying the memory allocations and performance for the sugar clarity
        // public static Z<(A, B)> ZipPar2<A, B>(this Z<A> za, Z<B> zb) => 
		//	   from zaForked in za.Fork()
		//	   from b in zb
		//	   from a in zaForked.Join()
		//	   select (a, b);
		//
		public static Z<(A, B)> ZipPar<A, B>(this Z<A> za, Z<B> zb) => 
			za.Fork().Then(zb, (zb_, zaForked) => 
			zb_.Then(zaForked, (zaForked_, b) => 
            zaForked_.Join().Then(b, (b_, a) => 
            Val((a, b_)))));
				
		public sealed record Empty 
		{
			public override string ToString() => "(empty)";
		}
		public static readonly Empty empty = default(Empty);
    }

    public static class ZLinq
    {
        public static Z<R> Select<A, R>(this Z<A> za, Func<A, R> selector) => za.To(selector);
        public static Z<R> SelectMany<A, R>(this Z<A> za, Func<A, Z<R>> next) => za.Then(next);
        public static Z<R> SelectMany<A, B, R>(this Z<A> za, Func<A, Z<B>> getZb, Func<A, B, R> project) =>
            za.Then(getZb, project, (getZb_, project_, a) => getZb_(a).Then(project_, a, (project__, a_, b) => project__(a_, b).Val()));
    }
	    
    public class Tests
    {
		int _id;
		int Id() => Interlocked.Increment(ref _id);   
		
        public Z<string> Map_small() =>
           	Z.Val(42).To(x => x + "!");

        public Z<int> Async_sleep() =>
            Async<int>((_, __, run) =>
			{
				var id = Id();
                WriteLine($"Sleep for 50ms - {id}");
                Thread.Sleep(50);
                WriteLine($"Woken {id}");
                run(_, __, 42);
            });

		public Z<int> Get_sleep() =>
            Get(() => 
			{
				var id = Id();
                WriteLine($"Sleep for 50ms - {id}");
                Thread.Sleep(50);
				WriteLine($"Woken - {id}");
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
			from a in fa.Join()
			from b in fb.Join()
			from _1 in Z.Do(() => WriteLine("After Async_fork"))
			select a + b + 2;
		
		public Z<int> Zip_par() =>
            from _ in Z.Do(() => WriteLine("Before ZipPar.."))
			from x in Z.ZipPar(Async_sleep(), Async_sleep())
			from _1 in Z.Do(() => WriteLine("After ZipPar"))
			select x.Item1 + x.Item2 + 3;

		public Z<int> Async_counter()
		{
			var i = 0;
            return 
				from b in Z.Do(() => WriteLine("Before Async_counter.."))
				from x in Z.Do(() => Interlocked.Increment(ref i)).Fork().RepeatN(100)
				from w in Z.Do(() => Thread.Sleep(50))
				from a in Z.Do(() => WriteLine("After Async_counter and sleep for 50ms"))
				select i;
		}

		public Z<Empty> Repeat(int n) 
		{
			var i = 0; 
			return Z.Do(() => WriteLine("HOWDY " + (++i))).RepeatN(n);
		}
    }
	
	public class Program
	{
		public static void ZMain()
		{
			var t = new Tests();
			
			void run<A>(Z<A> za, string name = "") 
			{ 
				WriteLine(name + " >> "); 
				var a = za.RunUnsafe();
				WriteLine(a); 
				WriteLine(); 
			}

			run(t.Async_counter(), nameof(t.Async_counter));
			
			run(t.Map_small(),   nameof(t.Map_small));

			run(t.Repeat(3),     nameof(t.Repeat));
			//run(t.Repeat(15000), nameof(t.Repeat)); // should not StackOverflow

			run(t.Async_sleep(), nameof(t.Async_sleep));
			run(t.Async_seq(),   nameof(t.Async_seq));
			
			run(t.Async_fork(),  nameof(t.Async_fork));

			run(t.Zip_par(),     nameof(t.Zip_par));
			
			WriteLine("==DONE==");
		}
	}
}