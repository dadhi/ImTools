using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Playground
{
    public class CustomEqualityComparerBenchmarks
    {
        private readonly Type _actualType = typeof(CustomEqualityComparerBenchmarks);
        public static Type ExpectedType = typeof(CustomEqualityComparerBenchmarks);

        [Benchmark(Baseline = true)]
        public bool ReferenceEqualsOrEquals()
        {
            return ReferenceEquals(_actualType, ExpectedType) || _actualType.Equals(ExpectedType);
        }

        public struct TypeEqualityComparer : IEqualityComparer<Type>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Type x, Type y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(Type obj)
            {
                return obj.GetHashCode();
            }
        }

        private TypeEqualityComparer _comparer = new TypeEqualityComparer();

        [Benchmark]
        public bool CustomEqualityComparer()
        {
            return _comparer.Equals(_actualType, ExpectedType);
        }

        [Benchmark]
        public bool ObjectEquals()
        {
            return Equals(_actualType, ExpectedType);
        }
    }
}
