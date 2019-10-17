// MIT licensed to Maksim Volkau 2018-2019

using System;
using static System.Console;

namespace ImTools.UnionPlayground
{
    class Program
    {
        static void Main()
        {
            // Unnamed (anonymous) union is fast to declare and use
            var i = U2<int, string>.Of(42);
            var s = U2<int, string>.Of("hey");

            WriteLine(i);
            WriteLine(s);

            // You may create the union case directly via constructor, helpful for cases like `U<A, A>` or `U<string, string>`
            var s2 = new U2<int, string>.case1(24);
            WriteLine(s2);

            // Typed union, the type is different from U<int, string>, e.g. `s = name;` won't compile
            var name = FlagOrName.Of("Bob");
            var flag = FlagOrName.Of(false);

            WriteLine(Usage.PatternMatching(name));
            WriteLine(Usage.PatternMatching(flag));
            WriteLine(name.Match(f => "" + f, n => n));

            // Typed union with Typed cases, so you can pattern match on `case Is<Name> name` or `Is<Flag> flag`
            var name2 = FlagOrName2.Of(Name.Of("Alice"));
            var flag2 = FlagOrName2.Of(Flag.Of(true));

            WriteLine(Usage.PatternMatchingWithTypedCases(name2));
            WriteLine(Usage.PatternMatchingWithTypedCases(flag2));
            WriteLine(flag2.Match(f => "" + f.Case, n => n.Case));

            // Option (MayBe) type defined as `sealed class Option<T> : Union<Option<T>, Empty, T> { ... }`.
            WriteLine(Some.Of(42));
            WriteLine(None.Of<int>());

            // Examples of recursive types: linked List and binary Tree, see below on how.
            WriteLine(MyList<int>.Empty.Push(3).Push(2).Push(1));

            WriteLine(MyTree.Of(MyTree.Leaf("a"), "b", MyTree.Leaf("c")));
        }
    }

    // One line named union definition
    public sealed class FlagOrName : U<FlagOrName, bool, string> { }

    // A different type from FlagOrName
    public sealed class Other : U<Other, bool, string> { }

    // Typed union with a typed cases! Now you can pattern match via `I<Flag>` and `I<Name>`
    public sealed class FlagOrName2 : U<FlagOrName2, Flag.value, Name.value> { }
    public sealed class Flag : Case<Flag, bool> { }
    public sealed class Name : Case<Name, string> { }

    public static class Usage
    {
        // note: Using T with constraint instead of FlagOrName.I interface improves the performance by avoiding boxing.
        public static string PatternMatching<T>(T x) where T : FlagOrName.union
        {
            switch (x)
            {
                case FlagOrName.case1 b: return "" + b.Value; // b.Value for the actual value
                case FlagOrName.case2 s: return "" + s.Value;
                default: throw new NotSupportedException();
            }
        }

        public static string PatternMatchingWithTypedCases(FlagOrName2.union x)
        {
            // Refactoring friendly Named cases, with some performance price due the boxing - likely is not important for your case, 
            // except you are designing a performance oriented data structure or being used in performance sensitive spot context.
            // The performance price may be gained back any time by switching to CaseN struct matching.
            switch (x)
            {
                case I<Flag.value> b: return "" + b.Case.Value;
                case I<Name.value> s: return "" + s.Case.Value;
                default: throw new NotSupportedException();
            }
        }
    }

    public class Option<T> : U<Option<T>, Empty, T>
    {
        // Type specific custom Case names
        public static readonly case1 None = new case1(Empty.Value);
        public static case2 Some(T x) => new case2(x);
    }

    // Named facades for the cases are the best for inference and simplicity of use.
    public static class None
    {
        public static Option<T>.case1 Of<T>() => Option<T>.None;
    }

    public static class Some
    {
        public static Option<T>.case2 Of<T>(T x) => Option<T>.Some(x);
    }

    // Enum without additional state in a Union disguise. Can be pattern matched as `case Is<Increment> _: ...; break;`
    public sealed class CounterMessage : U<CounterMessage, Increment, Decrement> { }
    public struct Increment { }
    public struct Decrement { }

    // I expect it should not compile, but...
    // 
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // !!! IT CRASHES VISUAL STUDIO
    // !!!
    // !!! Crashes VS 15.7.4 and 15.6, LinqPad 5.26.01, sharplab.io
    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    //public sealed class ListX<T> : Union<ListX<T>, Unit, (T, ListX<T>.I)> {}

    // Indeed, below line just does not compile!
    //public sealed class ListY<T> : Union<ListY<T>, Unit, ListY<T>.I> { }

    // Recursive definition of the List
    public sealed class MyList<T> : U<MyList<T>, Empty, MyList<T>.NonEmptyList>
    {
        public static readonly case1 Empty = new case1(ImTools.Empty.Value);
        public static case2 NonEmpty(T head, union tail) => new case2(new NonEmptyList(head, tail));

        public readonly struct NonEmptyList
        {
            public readonly T Head;
            public readonly union Tail;
            public NonEmptyList(T head, union tail) => (Head, Tail) = (head, tail);
            public override string ToString() => Head + "::" + Tail;
        }
    }

    public static class MyList
    {
        public static MyList<T>.case2 Push<T>(this MyList<T>.union list, T head) => MyList<T>.NonEmpty(head, list);
    }

    // Less efficient, but less boilerplate recursive type - requires one heap reference per recursive type usage.
    public sealed class MyTree<T> : U<MyTree<T>, Empty, MyTree<T>.NonEmptyTree>
    {
        public sealed class NonEmptyTree : CaseObj<NonEmptyTree, (union Left, T Leaf, union Right)> { }
        public static readonly case1 Empty = new case1(ImTools.Empty.Value);
    }

    public static class MyTree
    {
        public static MyTree<T>.union Of<T>(MyTree<T>.union left, T leaf, MyTree<T>.union right) =>
            MyTree<T>.Of(MyTree<T>.NonEmptyTree.Of((left, leaf, right)));

        public static MyTree<T>.union Leaf<T>(T leaf) => Of(MyTree<T>.Empty, leaf, MyTree<T>.Empty);
    }
}
