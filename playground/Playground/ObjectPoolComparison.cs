using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace Playground
{
    [MemoryDiagnoser, DisassemblyDiagnoser]
    public class ObjectPoolComparison_RentReturnAndRentPrefilledPool
    {
        private ScanPool<X> _scanPool;
        private StackPool<X> _stackPool;

        [IterationSetup]
        public void CreateAndPopulatePools()
        {
            _scanPool = new ScanPool<X>();
            _scanPool.Return(new X(false, 0, null));
            _scanPool.ScanReturn(new X(false, 0, null));
            _scanPool.ScanReturn(new X(false, 0, null));

            _stackPool = new StackPool<X>();
            _stackPool.Return(new X(false, 0, null));
            _stackPool.Return(new X(false, 0, null));
        }

        [Benchmark(Baseline = true)]
        public int New_4Times()
        {
            var x1 = new X(true, 1, "1");
            var x2 = new X(true, 2, "2");

            x1 = new X(false, 3, "3");
            x2 = new X(false, 4, "4");

            return x1.I + x2.I;
        }

        [Benchmark]
        public int StackPool_2Rents2Returns2Rents()
        {
            var p = _stackPool;

            var x1 = p.RentOrNew(true, 1, "1");
            var x2 = p.RentOrNew(true, 2, "2");

            p.Return(x1);
            p.Return(x2);

            x1 = p.RentOrNew(false, 3, "3");
            x2 = p.RentOrNew(false, 4, "4");

            return x1.I + x2.I;
        }

        [Benchmark]
        public int ScanPool_2Rents2Returns2Rents()
        {
            var p = _scanPool;

            var x1 = p.RentOrNew(true, 1, "1");
            var x2 = p.RentOrNew(true, 2, "2");

            p.Return(x1);
            p.Return(x2);

            x1 = p.RentOrNew(false, 3, "3");
            x2 = p.RentOrNew(false, 4, "4");

            return x1.I + x2.I;
        }

        [Benchmark]
        public int ScanPool_ScanOnly_2Rents2Returns2Rents()
        {
            var p = _scanPool;

            var x1 = p.ScanRentOrNew(true, 1, "1");
            var x2 = p.ScanRentOrNew(true, 2, "2");

            p.ScanReturn(x1);
            p.ScanReturn(x2);

            x1 = p.ScanRentOrNew(false, 3, "3");
            x2 = p.ScanRentOrNew(false, 4, "4");

            return x1.I + x2.I;
        }
    }

    [MemoryDiagnoser, DisassemblyDiagnoser]
    public class ObjectPoolComparison_RentPrefilledPool
    {
        private ScanPool<X> _scanPool;
        private StackPool<X> _stackPool;

        [IterationSetup]
        public void CreateAndPopulatePools()
        {
            _scanPool = new ScanPool<X>();
            _scanPool.Return(new X(false, 0, null));
            _scanPool.ScanReturn(new X(false, 0, null));
            _scanPool.ScanReturn(new X(false, 0, null));

            _stackPool = new StackPool<X>();
            _stackPool.Return(new X(false, 0, null));
            _stackPool.Return(new X(false, 0, null));
        }

        [Benchmark(Baseline = true)]
        public int New_2Times()
        {
            var x1 = new X(true, 1, "1");
            var x2 = new X(true, 2, "2");

            return x1.I + x2.I;
        }

        [Benchmark]
        public int StackPool_2Rents()
        {
            var p = _stackPool;

            var x1 = p.RentOrNew(true, 1, "1");
            var x2 = p.RentOrNew(true, 2, "2");

            return x1.I + x2.I;
        }

        [Benchmark]
        public int ScanPool_2Rents()
        {
            var p = _scanPool;

            var x1 = p.RentOrNew(true, 1, "1");
            var x2 = p.RentOrNew(true, 2, "2");

            return x1.I + x2.I;
        }

        [Benchmark]
        public int ScanPool_ScanOnly_2Rents()
        {
            var p = _scanPool;

            var x1 = p.ScanRentOrNew(true, 1, "1");
            var x2 = p.ScanRentOrNew(true, 2, "2");

            return x1.I + x2.I;
        }
    }

    public static class HelperExt 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X RentOrNew(this ScanPool<X> p, bool b, int i, string s) => 
            p.Rent()?.Init(b, i, s) ?? new X(b, i, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X RentOrNew(this StackPool<X> p, bool b, int i, string s) =>
            p.Rent()?.Init(b, i, s) ?? new X(b, i, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X RentNoScanOrNew(this ScanPool<X> p, bool b, int i, string s) =>
            p.RentNoScan()?.Init(b, i, s) ?? new X(b, i, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static X ScanRentOrNew(this ScanPool<X> p, bool b, int i, string s) =>
            p.ScanRent()?.Init(b, i, s) ?? new X(b, i, s);
    }

    public class X
    {
        public bool B;
        public int I;
        public string S;

        public X(bool b, int i, string s)
        {
            B = b; I = i; S = s;
        }

        public X Init(bool b, int i, string s)
        {
            B = b; I = i; S = s;
            return this;
        }
    }

    [MemoryDiagnoser, DisassemblyDiagnoser]
    public class ObjectPoolComparison
    {
        private ScanPool<X> _scanNoScanPool, _scanPool;
        private StackPool<X> _stackPool;

        [IterationSetup]
        public void CreatePools()
        {
            _scanNoScanPool = new ScanPool<X>();
            _scanPool = new ScanPool<X>();
            _stackPool = new StackPool<X>();
        }

        [Benchmark(Baseline = true)]
        public int ScanPool_NoScan_2Returns2Rents()
        {
            var p = _scanNoScanPool;

            var x1 = p.RentNoScanOrNew(true, 55, "55");
            var x2 = p.RentNoScanOrNew(true, 66, "66");

            p.ReturnNoScan(x1);
            p.ReturnNoScan(x2);

            var x3 = p.RentNoScanOrNew(true, 77, "77");
            var x4 = p.RentNoScanOrNew(true, 77, "77");

            return x3.I + x4.I;
        }

        [Benchmark]
        public int ScanPool_2Returns2Rents()
        {
            var p = _scanPool;

            var x1 = p.RentOrNew(true, 55, "55");
            var x2 = p.RentOrNew(true, 66, "66");

            p.Return(x1);
            p.Return(x2);

            var x3 = p.RentOrNew(true, 77, "77");
            var x4 = p.RentOrNew(true, 77, "77");

            return x3.I + x4.I;
        }

        [Benchmark]
        public int StackPool_2Returns2Rents()
        {
            var p = _stackPool;

            var x1 = p.RentOrNew(true, 55, "55");
            var x2 = p.RentOrNew(true, 66, "66");

            p.Return(x1);
            p.Return(x2);

            var x3 = p.RentOrNew(true, 77, "77");
            var x4 = p.RentOrNew(true, 77, "77");

            return x3.I + x4.I;
        }
    }

    public sealed class StackPool<T> where T : class
    {
        public T Rent() =>
            Interlocked.Exchange(ref _s, _s?.Tail)?.Head;

        public void Return(T x) =>
            Interlocked.Exchange(ref _s, new Stack(x, _s));

        private Stack _s;

        private sealed class Stack
        {
            public readonly T Head;
            public readonly Stack Tail;

            public Stack(T h, Stack t)
            {
                Head = h;
                Tail = t;
            }
        }
    }

    public sealed class ScanPool<T> where T : class
    {
        private T _x;
        private readonly T[] _xs;
        public ScanPool(int n = 32) { _xs = new T[n]; }

        public T RentNoScan() => Interlocked.Exchange(ref _x, null);

        public T Rent() =>
            Interlocked.Exchange(ref _x, null) ??
            ScanRent();

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ScanRent()
        {
            T x = null;
            var xs = _xs;
            for (var i = 0; i < xs.Length &&
                (x = Interlocked.Exchange(ref xs[i], null)) == null;
                ++i) {}

            return x;
        }

        public void ReturnNoScan(T x) => Interlocked.Exchange(ref _x, x);

        public void Return(T x)
        {
            if (Interlocked.CompareExchange(ref _x, x, null) != null)
                ScanReturn(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScanReturn(T x)
        {
            var xs = _xs;
            for (var i = 0; i < xs.Length &&
                Interlocked.CompareExchange(ref xs[i], x, null) != null;
                ++i) {}
        }
    }
}
