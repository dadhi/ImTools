(*
|                                     Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                                     FSharp |  11.44 ns | 0.0769 ns | 0.0719 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|                   CSharp_named_case_struct |  18.32 ns | 0.0505 ns | 0.0473 ns |  1.60 |    0.01 |           - |           - |           - |                   - |
| CSharp_named_case_struct_match_I_interface | 304.78 ns | 5.8215 ns | 5.4454 ns | 26.64 |    0.48 |           - |           - |           - |                   - |
|      CSharp_named_case_struct_Match_method | 134.24 ns | 0.4707 ns | 0.4403 ns | 11.74 |    0.09 |      0.1729 |           - |           - |               816 B |
*)

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open ImTools

type FlagOrCount = 
    | Flag of bool
    | Count of int
    | Message of string

type Flag1()    = class inherit Val<Flag1, bool>() end
type Count1()   = class inherit Val<Count1, int>() end
type Message1() = class inherit Val<Message1, string>() end

// NOTE: It happens that in F# is not possible to refer to nested type from the inheritor (the commented code below). 
// It is much less verbose in C#. The same problem is with `match` clause later in the benchmarks.
//
// type FlagOrCount1 = class inherit U<FlagOrCount1, Flag1.value, Count1.value, Message1.value> end
//
type FlagOrCount1 = class inherit U<FlagOrCount1, Val<Flag1, bool>.value, Val<Count1, int>.value, Val<Message1, string>.value> end

[<MemoryDiagnoser>]
type Traverse_union_array_and_match_to_sum_something() =
    
    let _fs  = [| Flag true; Count 1; Message "hey" |]
    let _cs1 = [| FlagOrCount1.Of (Flag1.Of true); FlagOrCount1.Of (Count1.Of 1); FlagOrCount1.Of (Message1.Of "hey") |]

    [<Benchmark(Baseline = true)>]
    member this.FSharp() =
        let mutable sum = 0
        for x in _fs do 
            match x with 
            | Flag f  -> if f then sum <- sum + 1 else sum |> ignore 
            | Count n -> sum <- sum + n
            | Message _ -> sum <- sum + 42
        sum

    [<Benchmark>]
    member this.CSharp_named_case_struct() =
        let mutable sum = 0
        for x in _cs1 do
            match x with
            | :? U<FlagOrCount1, Val<Flag1, bool>.value, Val<Count1, int>.value, Val<Message1, string>.value>.case1 as f -> if f.Case.Value then sum <- sum + 1 else sum |> ignore
            | :? U<FlagOrCount1, Val<Flag1, bool>.value, Val<Count1, int>.value, Val<Message1, string>.value>.case2 as n -> sum <- sum + n.Case.Value
            | :? U<FlagOrCount1, Val<Flag1, bool>.value, Val<Count1, int>.value, Val<Message1, string>.value>.case3 -> sum <- sum + 42
            | _ -> ()
        sum

    [<Benchmark>]
    member this.CSharp_named_case_struct_match_I_interface() =
        let mutable sum = 0
        for x in _cs1 do
            match x with
            | :? I<Val<Flag1, bool>.value> as f -> if f.Case.Value then sum <- sum + 1 else sum |> ignore
            | :? I<Val<Count1, int>.value> as n -> sum <- sum + n.Case.Value
            | :? I<Val<Message1, string>.value> -> sum <- sum + 42
            | _ -> ()
        sum

    [<Benchmark>]
    member this.CSharp_named_case_struct_Match_method() =
        let mutable sum = 0
        for x in _cs1 do 
            x.Match((fun f -> if f.Value then sum <- sum + 1 else sum |> ignore), (fun n -> sum <- sum + n.Value), (fun m -> sum <- sum + 42)) |> ignore
        sum


[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Traverse_union_array_and_match_to_sum_something>() |> ignore
    0 // return an integer exit code
