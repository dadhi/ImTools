// The source repository
// https://github.com/dadhi/ImTools/blob/zio_from_scratch/test/ImTools.UnitTests/Playground/ZIO.cs

// Parts:
// [X] 1 - https://youtu.be/wsTIcHxJMeQ 
// [X] 2 - https://youtu.be/g8Tuqldu2AE
// [WIP] 3 - https://youtu.be/0IU9mGO_9Rw

// TODO:
// [X] Stack safety / the stack is big though - I got the stack overflow on repeating the Do(() => WriteLine("x")) 87253 times
// [X] Reduce allocations on the lambda closures and therefore improve the perf
// [X] Allow to pass your async code scheduler Task.Run or SynchronizationContext
// [ ] Catch errors from the Async task runner or Task
// [?] Ergonomics - less noise in API surface, more orthogonal and simple to understand implementation 
// [ ] Make it work with async/await see how it is done in https://github.com/yuretz/FreeAwait/tree/master/src/FreeAwait
// [ ] Performance
// [ ] Error handling
// [ ] Environment

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
//using System.Runtime.CompilerServices;
using static System.Console;
using static ImTools.UnitTests.Playground.S;

namespace ImTools.UnitTests.Playground
{
    public interface SErased { }

    public interface S<out A> : SErased
    {
        void Run(Action<A> consume);
    }

    public abstract record SImpl<A> : S<A>
    {
        public void Run(Action<A> consume)
        {
            var s = this.Then(consume, static (consume_, a) => S.Do(a, consume_));

            // todo: @api await the fiber context - so make it awaitable
            new SFiberContext<Empty>(s, S.DefaultFiberRunner);
        }
    }

    interface SVal
    {
        object Value { get; }
    }

    public sealed record SVal<A>(A Value) : SImpl<A>, SVal
    {
        object SVal.Value => Value;
    }

    interface SLazy
    {
        object GetValue();
    }

    public sealed record SLazy<A>(Func<A> GetValue) : SImpl<A>, SLazy
    {
        object SLazy.GetValue() => GetValue();
    }

    public sealed record SLazy<S, A>(S State, Func<S, A> GetValue) : SImpl<A>, SLazy
    {
        object SLazy.GetValue() => GetValue(State);
    }

    public sealed record SLazyDo(Action Act) : SImpl<Empty>, SLazy
    {
        object SLazy.GetValue() { Act(); return empty; }
    }

    public sealed record SLazyDo<S>(S State, Action<S> Act) : SImpl<Empty>, SLazy
    {
        object SLazy.GetValue() { Act(State); return empty; }
    }

    public sealed record SLazyDo<S1, S2>(S1 State1, S2 State2, Action<S1, S2> Act) : SImpl<Empty>, SLazy
    {
        object SLazy.GetValue() { Act(State1, State2); return empty; }
    }

    interface SFork
    {
        object GetFiber(Func<Action, object> runner);
    }

    public sealed record SFork<A>(S<A> Za, Func<S<A>, Func<Action, object>, SFiber<A>> GetFiber) : SImpl<SFiber<A>>, SFork
    {
        object SFork.GetFiber(Func<Action, object> runner) => GetFiber(Za, runner);
    }

    public sealed record SShift(Func<Action, object> Runner) : SImpl<Empty> { }

    interface SThen
    {
        SErased Sa { get; }
        SErased Cont(object a);
    }

    public sealed record SThen<A, B>(S<A> Sa, Func<A, S<B>> Cont) : SImpl<B>, SThen
    {
        SErased SThen.Sa => Sa;
        SErased SThen.Cont(object a) => Cont((A)a);
    }

    public sealed record SThen<S, A, B>(S<A> Sa, S State, Func<S, A, S<B>> Cont) : SImpl<B>, SThen
    {
        SErased SThen.Sa => Sa;
        SErased SThen.Cont(object a) => Cont(State, (A)a);
    }

    public sealed record SThen<S1, S2, A, B>(S<A> Sa, S1 State1, S2 State2, Func<S1, S2, A, S<B>> Cont) : SImpl<B>, SThen
    {
        SErased SThen.Sa => Sa;
        SErased SThen.Cont(object a) => Cont(State1, State2, (A)a);
    }

    interface SAsync
    {
        void Schedule(object state, Action<object, object> run);
    }

    public sealed record SAsyncFriendly<S, A>(S State, Action<S, Action<A>> Schedule) : SImpl<A>, SAsync
    {
        void SAsync.Schedule(object state, Action<object, object> run) => 
            Schedule(State, a => run(state, a)); // todo: @mem make static
    }

    public sealed record SAsync<A>(Action<object, object, Action<object, object, A>> Schedule) : SImpl<A>, SAsync
    {
        void SAsync.Schedule(object state, Action<object, object> run) => 
            Schedule(run, state, static (run_, state_, a) => ((Action<object, object>)run_)(state_, a));
    }

    public sealed record SAsync<S, A>(S State, Action<S, object, object, Action<object, object, A>> Schedule) : SImpl<A>, SAsync
    {
        void SAsync.Schedule(object state, Action<object, object> run) => 
            Schedule(State, run, state, static (run_, state_, a) => ((Action<object, object>)run_)(state_, a));
    }

    public interface SFiber<out A>
    {
        S<A> Join();
        //S<Empty> Interrupt() => throw new NotImplementedException("todo");
    }

    sealed record SFiberContext<A> : SFiber<A>
    {
        public SErased Sa { get; private set; }
        public Func<Action, object> Runner { get; private set; }

        public readonly object _work; // todo: @wip what to do with this?

        public SFiberContext(S<A> sa, Func<Action, object> runner)
        {
            Sa = sa;
            Runner = runner;
            _work = Runner(RunLoop);
        }

        sealed record ContStack(SThen Cont, ContStack Rest);
        ContStack _stack;

        abstract record EvilState;
        sealed record Done(A Value) : EvilState;
        sealed record Callbacks(Action<object, object, A> Act, object P0, object P1, Callbacks Rest) : EvilState;
        EvilState _state;

        void Complete(A a)
        {
            var s = Interlocked.Exchange(ref _state, new Done(a));
            while (s is Callbacks(var act, var p0, var p1, var rest))
            {
                act(p0, p1, a);
                s = rest;
            }
        }

        public S<A> Join() => S.Async<SFiberContext<A>, A>(this, static (f, s1, s2, run) =>
        {
            if (f._state is Done doneFast)
                run(s1, s2, doneFast.Value);
            else
            {
                var spinWait = new SpinWait();
                while (true)
                {
                    var s = f._state;
                    if (s is Done done)
                    {
                        run(s1, s2, done.Value);
                        break;
                    }

                    var callbacks = new Callbacks(run, s1, s2, s as Callbacks);
                    if (Interlocked.CompareExchange(ref f._state, callbacks, s) == s)
                        break;

                    spinWait.SpinOnce();
                }
            }
        });

        // returns true to proceed 
        private bool Continue(object val)
        {
            if (_stack is ContStack(var cont, var rest))
            {
                Sa = cont.Cont(val);
                _stack = rest;
                return true;
            }
            Complete((A)val);
            return false;
        }

        void Resume(SErased sa)
        {
            Sa = sa;
            RunLoop();
        }

        void RunLoop()
        {
            var loop = true;
            while (loop)
            {
                switch (Sa)
                {
                    case SVal v:
                        loop = Continue(v.Value);
                        break;

                    case SLazy l:
                        loop = Continue(l.GetValue());
                        break;

                    case SFork f:
                        loop = Continue(f.GetFiber(Runner));
                        break;

                    case SShift s:
                        Runner = s.Runner;
                        loop = Continue(empty);
                        break;

                    case SThen t:
                        Sa = t.Sa;
                        _stack = new ContStack(t, _stack);
                        break;

                    case SAsync a:
                        loop = false;
                        if (_stack == null)
                            a.Schedule(this, static (f, x) => ((SFiberContext<A>)f).Complete((A)x));
                        else
                            a.Schedule(this, static (f, x) => ((SFiberContext<A>)f).Resume(x.Val()));
                        break;

                    case var unknown:
                        throw new InvalidOperationException("RunLoop does not recognize `" + unknown + "`");
                }
            }
        }
    }

    public static class S
    {
        public static readonly Func<Action, object> DefaultFiberRunner = Task.Run;

        public static SFiber<A> RunUnsafeFiber<A>(this S<A> sa) => new SFiberContext<A>(sa, DefaultFiberRunner);

        sealed class Box<A> { public A Ab; public Box(A a) => Ab = a; }
        sealed class Box<A, B> { public A Ab; public B Bb; public Box(A a, B b) { Ab = a; Bb = b; } }

        public static A RunUnsafe<A>(this S<A> sa)
        {
            using var e = new AutoResetEvent(false);
            var res = new Box<A, AutoResetEvent>(default, e);

            RunUnsafeFiber(sa.Then(res, static (res1, a) => S.Do(a, res1, static (a1, res2) =>
            {
                res2.Ab = a1;
                res2.Bb.Set();
            })));
            e.WaitOne();
            return res.Ab;
        }

        public static S<A> Val<A>(this A a) => new SVal<A>(a);

        public static S<A> Get<A>(Func<A> getA) => new SLazy<A>(getA);
        public static S<A> Get<S, A>(in S state, Func<S, A> getA) => new SLazy<S, A>(state, getA);

        public static S<Empty> Do(Action act) => new SLazyDo(act);
        public static S<Empty> Do<S>(in S state, Action<S> act) => new SLazyDo<S>(state, act);
        public static S<Empty> Do<S1, S2>(in S1 state1, in S2 state2, Action<S1, S2> act) => new SLazyDo<S1, S2>(state1, state2, act);

        /// <summary>This is Bind, SelectMany or FlatMap... but I want to be unique and go with Then for now as it seems to have a more precise meaning IMHO</summary>
        public static S<B> Then<A, B>(this S<A> sa, Func<A, S<B>> @from) => new SThen<A, B>(sa, @from);
        public static S<B> Then<S, A, B>(this S<A> sa, in S s, Func<S, A, S<B>> @from) => new SThen<S, A, B>(sa, s, @from);
        public static S<B> Then<S1, S2, A, B>(this S<A> sa, in S1 s1, in S2 s2, Func<S1, S2, A, S<B>> @from) => new SThen<S1, S2, A, B>(sa, s1, s2, @from);

        public static S<B> To<A, B>(this S<A> sa, Func<A, B> map) => sa.Then(map, static (map_, a) => map_(a).Val());

        public static S<B> ToVal<A, B>(this S<A> sa, B b) => sa.Then(b, static (b_, _) => b_.Val());
        public static S<B> ToGet<A, B>(this S<A> sa, Func<B> getB) => sa.Then(getB, static (getB_, _) => getB_().Val());

        public static S<(A, B)> Zip<A, B>(this S<A> sa, S<B> zb) => 
            sa.Then(zb, static (zb_, a) => zb_.Then(a, static (a_, b) => Val((a_, b))));

        public static S<C> ZipWith<A, B, C>(this S<A> sa, S<B> zb, Func<A, B, C> zip) => 
            sa.Then(zip, zb, static (zip_, zb_, a) => zb_.Then(zip_, a, static (zip__, a_, b) => zip__(a_, b).Val()));

        public static S<A> And<A, B>(this S<A> sa, S<B> zb) => 
            sa.Then(zb, static (zb_, a) => zb_.Then(a, static (a_, _) => a_.Val()));

        public static S<A> RepeatN<A>(this S<A> sa, int n) => n <= 1 ? sa : sa.And(sa.RepeatN(n - 1));

        public static S<A> Async<A>(Action<object, object, Action<object, object, A>> schedule) => new SAsync<A>(schedule);
        public static S<A> Async<S, A>(in S state, Action<S, object, object, Action<object, object, A>> schedule) => new SAsync<S, A>(state, schedule);

        public static S<A> Async<S, A>(in S state, Action<S, Action<A>> schedule) => new SAsyncFriendly<S, A>(state, schedule);

        // todo: @perf @mem we may potentially reuse FiberContext when the RunLoop done or on Join?
        public static S<SFiber<A>> Fork<A>(this S<A> sa) => new SFork<A>(sa, (z, runner) => new SFiberContext<A>(z, runner));

        public static S<Empty> Shift(Func<Action, object> runner) => new SShift(runner);

        // Here is the reference implementation of ZipPar with Linq paying the memory allocations and performance for the sugar clarity
        // public static Z<(A, B)> ZipPar2<A, B>(this Z<A> sa, Z<B> zb) => 
        //     from zaForked in sa.Fork()
        //     from b in zb
        //     from a in zaForked.Join()
        //     select (a, b);
        //
        public static S<(A, B)> ZipPar<A, B>(this S<A> sa, S<B> zb) =>
            sa.Fork().Then(zb, static (zb_, zaForked) =>
            zb_.Then(zaForked, static (zaForked_, b) =>
            zaForked_.Join().Then(b, static (b_, a) =>
            Val((a, b_)))));

        public sealed record Empty
        {
            public override string ToString() => "(empty)";
        }
        public static readonly Empty empty = default(Empty);

    }

    public static class SLinq
    {
        public static S<R> Select<A, R>(this S<A> sa, Func<A, R> selector) => sa.To(selector);
        public static S<R> SelectMany<A, R>(this S<A> sa, Func<A, S<R>> next) => sa.Then(next);
        public static S<R> SelectMany<A, B, R>(this S<A> sa, Func<A, S<B>> getSb, Func<A, B, R> project) =>
            sa.Then(getSb, project, static (getSb_, project_, a) => 
                getSb_(a).Then(project_, a, static (project__, a_, b) => project__(a_, b).Val()));
    }

    public class Tests
    {
        int _id;
        int Id() => Interlocked.Increment(ref _id);

        public S<string> Map_small() =>
               S.Val(42).To(x => x + "!");

        public S<int> Async_sleep() =>
            Async<int, int>(Id(), (id, run) =>
            {
                WriteLine($"Sleep for 50ms - {id}");
                Thread.Sleep(50);
                WriteLine($"Woken {id}");
                run(42);
            });

        public S<int> Get_sleep() =>
            Get(Id(), id =>
            {
                WriteLine($"Sleep for 50ms - {id}");
                Thread.Sleep(50);
                WriteLine($"Woken - {id}");
                return 43;
            });

        public S<int> Async_seq() =>
            from _ in S.Do(() => WriteLine("Before Async_seq.."))
            from a in Async_sleep()
            from b in Async_sleep()
            from _1 in S.Do(() => WriteLine("After Async_seq"))
            select a + b;

        public S<int> Get_seq() =>
            from _ in S.Do(() => WriteLine("Before Get_seq.."))
            from a in Get_sleep()
            from b in Get_sleep()
            from _1 in S.Do(() => WriteLine("After Get_seq"))
            select a + b + 1;

        public S<int> Async_fork() =>
            from _ in S.Do(() => WriteLine("Before Async_fork.."))
            from fa in Async_sleep().Fork()
            from fb in Async_sleep().Fork()
            from a in fa.Join()
            from b in fb.Join()
            from _1 in S.Do(() => WriteLine("After Async_fork"))
            select a + b + 2;

        public S<int> Zip_par() =>
            from _ in S.Do(() => WriteLine("Before ZipPar.."))
            from x in S.ZipPar(Async_sleep(), Async_sleep())
            from _1 in S.Do(() => WriteLine("After ZipPar"))
            select x.Item1 + x.Item2 + 3;

        public S<int> Async_counter()
        {
            var i = 0;
            return
                from b in S.Do(() => WriteLine("Before Async_counter.."))
                from x in S.Do(() => Interlocked.Increment(ref i)).Fork().RepeatN(100)
                from w in S.Do(() => Thread.Sleep(50))
                from a in S.Do(() => WriteLine("After Async_counter and sleep for 50ms"))
                select i;
        }

        public S<Empty> Repeat(int n)
        {
            var i = 0;
            return S.Do(() => WriteLine("HOWDY " + (++i))).RepeatN(n);
        }
    }

    [TestFixture]
    public class Program
    {
        [Test]
        public void SMain()
        // public static void Main()
        {
            var t = new Tests();

            void run<A>(S<A> sa, string name = "")
            {
                WriteLine(name + " >> ");
                var a = sa.RunUnsafe();
                WriteLine(a);
                WriteLine();
            }

            run(t.Async_counter(), nameof(t.Async_counter));

            run(t.Map_small(), nameof(t.Map_small));

            run(t.Repeat(3), nameof(t.Repeat));
            //run(t.Repeat(15000), nameof(t.Repeat)); // should not StackOverflow

            run(t.Async_sleep(), nameof(t.Async_sleep));
            run(t.Async_seq(), nameof(t.Async_seq));

            run(t.Async_fork(), nameof(t.Async_fork));

            run(t.Zip_par(), nameof(t.Zip_par));

            WriteLine("==SUCCESS!==");
        }
    }
}