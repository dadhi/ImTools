(*
|                              Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                              FSharp |   5.920 ns | 0.0323 ns | 0.0286 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|                   CSharp_named_case | 138.089 ns | 0.4042 ns | 0.3583 ns | 23.33 |    0.16 |           - |           - |           - |                   - |
|                 CSharp_unnamed_case | 142.634 ns | 0.7154 ns | 0.6692 ns | 24.09 |    0.18 |           - |           - |           - |                   - |
| CSharp_unnamed_case_as_CaseN_struct |   9.267 ns | 0.0291 ns | 0.0258 ns |  1.57 |    0.01 |           - |           - |           - |                   - |
*)

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open ImTools

type FlagOrCount = 
    | Flag of bool
    | Count of int

type Flag1()  = class inherit I<Flag1, bool>() end
type Count1() = class inherit I<Count1, int>() end
type FlagOrCount1 = class inherit Union<FlagOrCount1, I<Flag1, bool>.v, I<Count1, int>.v> end

type Flag2()  = class inherit Is<Flag2, bool>() end
type Count2() = class inherit Is<Count2, int>() end
type FlagOrCount2 = class inherit Union<FlagOrCount2, Flag2, Count2> end

type FlagOrCount3 = class inherit Union<FlagOrCount3, bool, int> end


[<MemoryDiagnoser>]
type Traverse_union_array_and_match_to_sum_something() =
    
    let _fs  = [| Flag true; Count 1 |]
    let _cs1 = [| FlagOrCount1.Of (Flag1.Of true); FlagOrCount1.Of (Count1.Of 1) |]
    let _cs2 = [| FlagOrCount2.Of (Flag2.Of true); FlagOrCount2.Of (Count2.Of 1) |]
    let _cs3 = [| FlagOrCount3.Of true; FlagOrCount3.Of 1 |]

    [<Benchmark(Baseline = true)>]
    member this.FSharp() =
        let mutable sum = 0
        for x in _fs do 
            match x with 
            | Flag f  -> if f then sum <- sum + 1 else sum |> ignore 
            | Count n -> sum <- sum + n
        sum

    [<Benchmark>]
    member this.CSharp_named_case_struct() =
        let mutable sum = 0
        for x in _cs1 do
            match x with
            | :? Union<FlagOrCount1, I<Flag1, bool>.v, I<Count1, int>.v>.Case1 as f -> if f.X.X then sum <- sum + 1 else sum |> ignore
            | :? Union<FlagOrCount1, I<Flag1, bool>.v, I<Count1, int>.v>.Case2 as n -> sum <- sum + n.X.X
            | _ -> ()
        sum

    //[<Benchmark>]
    member this.CSharp_named_case() =
        let mutable sum = 0
        for x in _cs2 do
            match x with
            | :? Is<Flag2>  as f -> if f.Value.Value then sum <- sum + 1 else sum |> ignore
            | :? Is<Count2> as n -> sum <- sum + n.Value.Value
            | _ -> ()
        sum

    //[<Benchmark>]
    member this.CSharp_unnamed_case() =
        let mutable sum = 0
        for x in _cs3 do
            match x with
            | :? Is<bool> as f -> if f.Value then sum <- sum + 1 else sum |> ignore
            | :? Is<int> as n -> sum <- sum + n.Value
            | _ -> ()
        sum

    [<Benchmark>]
    member this.CSharp_unnamed_case_as_CaseN_struct() =
        let mutable sum = 0
        for x in _cs3 do
            match x with
            | :? Union<FlagOrCount3, bool, int>.Case1 as f -> if f.X then sum <- sum + 1 else sum |> ignore
            | :? Union<FlagOrCount3, bool, int>.Case2 as n -> sum <- sum + n.X
            | _ -> ()
        sum

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Traverse_union_array_and_match_to_sum_something>() |> ignore
    0 // return an integer exit code
