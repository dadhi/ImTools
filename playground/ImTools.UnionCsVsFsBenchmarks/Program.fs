open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open ImTools

type FlagOrCount = 
    | Flag of bool
    | Count of int

type Flag'()  = class inherit Is<Flag', bool>() end
type Count'() = class inherit Is<Count', int>() end
type FlagOrCount' = class inherit Union<FlagOrCount', Flag', Count'> end

type FlagOrCount'' = class inherit Union<FlagOrCount'', bool, int> end

(*
|                              Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                              FSharp |   5.920 ns | 0.0323 ns | 0.0286 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|                   CSharp_named_case | 138.089 ns | 0.4042 ns | 0.3583 ns | 23.33 |    0.16 |           - |           - |           - |                   - |
|                 CSharp_unnamed_case | 142.634 ns | 0.7154 ns | 0.6692 ns | 24.09 |    0.18 |           - |           - |           - |                   - |
| CSharp_unnamed_case_as_CaseN_struct |   9.267 ns | 0.0291 ns | 0.0258 ns |  1.57 |    0.01 |           - |           - |           - |                   - |
*)

[<MemoryDiagnoser>]
type Traverse_union_array_and_match_to_sum_something() =
    
    let _fs  = [| Flag true; Count 1 |]
    let _cs  = [| FlagOrCount'.Of (Flag'.Of true); FlagOrCount'.Of (Count'.Of 1) |]
    let _cs' = [| FlagOrCount''.Of true; FlagOrCount''.Of 1 |]

    [<Benchmark(Baseline = true)>]
    member this.FSharp() =
        let mutable sum = 0
        for x in _fs do 
            match x with 
            | Flag f  -> if f then sum <- sum + 1 else sum |> ignore 
            | Count n -> sum <- sum + n
        sum

    [<Benchmark>]
    member this.CSharp_named_case() =
        let mutable sum = 0
        for x in _cs do
            match x with
            | :? Is<Flag'>  as f -> if f.Value.Value then sum <- sum + 1 else sum |> ignore
            | :? Is<Count'> as n -> sum <- sum + n.Value.Value
            | _ -> ()
        sum

    [<Benchmark>]
    member this.CSharp_unnamed_case() =
        let mutable sum = 0
        for x in _cs' do
            match x with
            | :? Is<bool> as f -> if f.Value then sum <- sum + 1 else sum |> ignore
            | :? Is<int> as n -> sum <- sum + n.Value
            | _ -> ()
        sum

    [<Benchmark>]
    member this.CSharp_unnamed_case_as_CaseN_struct() =
        let mutable sum = 0
        for x in _cs' do
            match x with
            | :? Union<FlagOrCount'', bool, int>.Case1 as f -> if f.X then sum <- sum + 1 else sum |> ignore
            | :? Union<FlagOrCount'', bool, int>.Case2 as n -> sum <- sum + n.X
            | _ -> ()
        sum

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Traverse_union_array_and_match_to_sum_something>() |> ignore
    0 // return an integer exit code
