// MIT licensed to Maksim Volkau 2018-2019

using System;
using static System.Console;
using static ImTools.UnionPlayground.Usage;

namespace ImTools.UnionPlayground
{
    class Program
    {
        static void Main()
        {
            // Unnamed (anonymous) union is fast to declare and use
            var i = U<int, string>.Of(42);
            var s = U<int, string>.Of("hey");

            WriteLine(i);
            WriteLine(s);

            // You may create the union case directly via constructor, helpful for cases like `U<A, A>` or `U<string, string>`
            var s2 = new U<int, string>.case1(24);
            WriteLine(s2);

            // Typed union, the type is different from U<int, string>, e.g. `s = name;` won't compile
            var name = BoolOrString.Of("Bob");
            var flag = BoolOrString.Of(false);

            WriteLine(SwitchOnCases(name));
            WriteLine(SwitchOnCases(flag));
            WriteLine(name.Match(f => "" + f, n => n));

            // Typed union with Typed cases, so you can pattern match on `case Is<Name> name` or `Is<Flag> flag`
            var name2 = FlagOrName.Of(Name.Of("Alice"));
            var flag2 = FlagOrName.Of(Flag.Of(true));

            WriteLine(SwitchOnTypedItems(name2));
            WriteLine(SwitchOnTypedItems(flag2));
            WriteLine(flag2.Match(f => "" + f.Value, n => n.Value));

            // Option (MayBe) type defined as `sealed class Option<T> : Union<Option<T>, Empty, T> { ... }`.
            WriteLine(Some.Of(42));
            WriteLine(None.Of<int>());

            // Examples of recursive types: linked List and binary Tree, see below on how.
            WriteLine(MyList<int>.Empty.Push(3).Push(2).Push(1));

            WriteLine(MyTree.Of(MyTree.Leaf("a"), "b", MyTree.Leaf("c")));
        }
    }

    // One line named union definition
    public sealed class BoolOrString : Union<BoolOrString, bool, string> { }

    // A different type from the NamedBoolOrString
    public sealed class OtherBoolOrString : Union<OtherBoolOrString, bool, string> { }

    // Typed union with a typed cases! Now you can pattern match via `I<Flag>` and `I<Name>`
    public sealed class FlagOrName : Union<FlagOrName, Flag.item, Name.item> { }
    public sealed class Flag : Item<Flag, bool>   { }
    public sealed class Name : Item<Name, string> { }

    public static class Usage
    {
        // note: Using T with constraint instead of FlagOrName.I interface improves the performance by avoiding boxing.
        public static string SwitchOnCases<T>(T x) where T : BoolOrString.union
        {
            switch (x)
            {
                case BoolOrString.case1 b: return "" + b.Case; // b.Value for the actual value
                case BoolOrString.case2 s: return "" + s.Case;
                default: throw new NotSupportedException();
            }
        }

        public static string SwitchOnTypedItems(FlagOrName.union x)
        {
            // Refactoring friendly Named cases, with some performance price due the boxing - likely is not important for your case, 
            // except you are designing a performance oriented data structure or being used in performance sensitive spot context.
            // The performance price may be gained back any time by switching to CaseN struct matching.
            switch (x)
            {
                case I<Flag.item> b: return "" + b.Value.Item;
                case I<Name.item> s: return "" + s.Value.Item;
                default: throw new NotSupportedException();
            }
        }
    }

    public class Option<T> : Union<Option<T>, Unit, T>
    {
        // Type specific custom Case names
        public static readonly case1 None = new case1(Unit.unit);
        public static case2 Some(T x) => new case2(x);
    }

    // Named facades for the cases are the best for type inference and simplicity of use.
    public static class None
    {
        public static Option<T>.case1 Of<T>() => Option<T>.None;
    }

    public static class Some
    {
        public static Option<T>.case2 Of<T>(T x) => Option<T>.Some(x);
    }

    // Enum without additional state in a Union disguise. Can be pattern matched as `case Is<Increment> _: ...; break;`
    public sealed class CounterMessage : Union<CounterMessage, Increment, Decrement> { }
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
    public sealed class MyList<T> : Union<MyList<T>, Unit, MyList<T>.NonEmptyList>
    {
        public static readonly case1 Empty = new case1(Unit.unit);
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
    public sealed class MyTree<T> : Union<MyTree<T>, Unit, MyTree<T>.NonEmptyTree>
    {
        public sealed class NonEmptyTree : Box<NonEmptyTree, (union Left, T Leaf, union Right)> { }
        public static readonly case1 Empty = new case1(Unit.unit);
    }

    public static class MyTree
    {
        public static MyTree<T>.union Of<T>(MyTree<T>.union left, T leaf, MyTree<T>.union right) =>
            MyTree<T>.Of(MyTree<T>.NonEmptyTree.Of((left, leaf, right)));

        public static MyTree<T>.union Leaf<T>(T leaf) => Of(MyTree<T>.Empty, leaf, MyTree<T>.Empty);
    }
}
