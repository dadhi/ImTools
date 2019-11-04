open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open ImTools

(*
BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT


|                                     Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|                                     FSharp |  12.49 ns | 0.031 ns | 0.029 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|                   CSharp_named_case_struct |  15.43 ns | 0.085 ns | 0.080 ns |  1.24 |    0.01 |      - |     - |     - |         - |
|         CSharp_named_case_struct_tag_match |  15.00 ns | 0.162 ns | 0.152 ns |  1.20 |    0.01 |      - |     - |     - |         - |
| CSharp_named_case_struct_match_I_interface | 248.44 ns | 4.045 ns | 3.784 ns | 19.89 |    0.29 |      - |     - |     - |         - |
|      CSharp_named_case_struct_Match_method | 181.99 ns | 0.563 ns | 0.527 ns | 14.57 |    0.06 | 0.1733 |     - |     - |     816 B |
*)

type FlagOrCount = 
    | Flag of bool
    | Count of int
    | Message of string

type Flag1()    = class inherit Item<Flag1, bool>() end
type Count1()   = class inherit Item<Count1, int>() end
type Message1() = class inherit Item<Message1, string>() end

// NOTE: It happens that in F# is not possible to refer to nested type from the inheritor (the commented code below). 
// It is much less verbose in C#. The same problem is with `match` clause later in the benchmarks.
//
// type FlagOrCount1 = class inherit Union<FlagOrCount1, Flag1.item, Count1.item, Message1.item> end
//
type FlagOrCount1 = class inherit Union<FlagOrCount1, Item<Flag1, bool>.item, Item<Count1, int>.item, Item<Message1, string>.item> end

[<MemoryDiagnoser>]
type Traverse_union_array_and_match_to_sum_something() =
    
    let _fs  = [| Flag true; Count 1; Message "hey" |]
    let _cs1 = [| FlagOrCount1.Of (Flag1.Of true); FlagOrCount1.Of (Count1.Of 1); FlagOrCount1.Of (Message1.Of "hey") |]

    [<Benchmark(Baseline = true)>]
    member this.FSharp() =
        let mutable sum = 0
        for x in _fs do 
            match x with 
            | Flag f    -> if f then sum <- sum + 1 else sum |> ignore 
            | Count n   -> sum <- sum + n
            | Message _ -> sum <- sum + 42
        sum

    [<Benchmark>]
    member this.CSharp_named_case_struct() =
        let mutable sum = 0
        for x in _cs1 do
            match x with
            // FlagOrCount1.case1 in C#
            | :? Union<FlagOrCount1, Item<Flag1, bool>.item, Item<Count1, int>.item, Item<Message1, string>.item>.case1 as f -> 
                if f.Case.Item then sum <- sum + 1 else sum |> ignore
            // FlagOrCount1.case2 in C#
            | :? Union<FlagOrCount1, Item<Flag1, bool>.item, Item<Count1, int>.item, Item<Message1, string>.item>.case2 as n -> 
                sum <- sum + n.Case.Item
            // FlagOrCount1.case3 in C#
            | :? Union<FlagOrCount1, Item<Flag1, bool>.item, Item<Count1, int>.item, Item<Message1, string>.item>.case3 -> 
                sum <- sum + 42
            | _ -> ()
        sum

    [<Benchmark>]
    member this.CSharp_named_case_struct_tag_match() =
        let mutable sum = 0
        for x in _cs1 do
            match x.Tag with
            | FlagOrCount1.Tag.Case1 -> 
                if (x :?> Union<FlagOrCount1, Item<Flag1, bool>.item, Item<Count1, int>.item, Item<Message1, string>.item>.case1).Case.Item then sum <- sum + 1 else sum |> ignore
            | FlagOrCount1.Tag.Case2 -> 
                sum <- sum + (x :?> Union<FlagOrCount1, Item<Flag1, bool>.item, Item<Count1, int>.item, Item<Message1, string>.item>.case2).Case.Item
            | FlagOrCount1.Tag.Case3 -> 
                sum <- sum + 42
            | _ -> ()
        sum

    [<Benchmark>]
    member this.CSharp_named_case_struct_match_I_interface() =
        let mutable sum = 0
        for x in _cs1 do
            match x with
            | :? I<Item<Flag1, bool>.item> as f -> 
                if f.Value.Item then sum <- sum + 1 else sum |> ignore
            | :? I<Item<Count1, int>.item> as n -> 
                sum <- sum + n.Value.Item
            | :? I<Item<Message1, string>.item> -> 
                sum <- sum + 42
            | _ -> ()
        sum

    [<Benchmark>]
    member this.CSharp_named_case_struct_Match_method() =
        let mutable sum = 0
        for x in _cs1 do 
            x.Match((fun f -> if f.Item then sum <- sum + 1 else sum |> ignore), (fun n -> sum <- sum + n.Item), (fun m -> sum <- sum + 42)) |> ignore
        sum


[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Traverse_union_array_and_match_to_sum_something>() |> ignore
    0 // return an integer exit code
