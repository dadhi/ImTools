using System;
using BenchmarkDotNet.Attributes;
using ImTools;

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

        private TypeEqualityComparer _comparer = new TypeEqualityComparer();

        [Benchmark]
        public bool CustomEqualityComparer() => _comparer.Equals(_actualType, ExpectedType);

        [Benchmark]
        public bool ObjectEquals() => Equals(_actualType, ExpectedType);
    }
}
