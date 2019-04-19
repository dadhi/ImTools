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
            | :? Union<FlagOrCount'', bool, int>.Case1 as f -> if f.Value then sum <- sum + 1 else sum |> ignore
            | :? Union<FlagOrCount'', bool, int>.Case2 as n -> sum <- sum + n.Value
            | _ -> ()
        sum

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Traverse_union_array_and_match_to_sum_something>() |> ignore
    0 // return an integer exit code
