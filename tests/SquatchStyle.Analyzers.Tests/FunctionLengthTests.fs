module SquatchStyle.Analyzers.Tests.FunctionLengthTests

open Xunit
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.FunctionLengthAnalyzer
open SquatchStyle.Analyzers.Tests.Helpers

// Generates a function body with n lines (each a let binding).
let private makeLines n =
    [ 1..n ] |> List.map (fun i -> $"    let _x{i} = {i}") |> String.concat "\n"

let private makeSource n =
    $"""
module FunctionLengthTestModule{n}
let longFunction () =
{makeLines n}
    ()
"""

[<Fact>]
let ``function under 55 lines produces no messages`` () =
    async {
        let ctx = getContext opts (makeSource 40)
        let! msgs = functionLengthAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``function at 54 lines produces no messages`` () =
    async {
        // makeSource n produces n+1 body lines (n bindings + closing ())
        // makeSource 53 → 54 lines → below warn threshold (55), clean
        let ctx = getContext opts (makeSource 53)
        let! msgs = functionLengthAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``function at 55 lines produces SS002 warning`` () =
    async {
        // makeSource 54 → 55-line body → exactly at warn threshold
        let ctx = getContext opts (makeSource 54)
        let! msgs = functionLengthAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS002" && m.Severity = Severity.Warning)
    }

[<Fact>]
let ``function at 56 lines produces SS002 warning`` () =
    async {
        let ctx = getContext opts (makeSource 56)
        let! msgs = functionLengthAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS002" && m.Severity = Severity.Warning)
    }

[<Fact>]
let ``function at 69 lines produces SS002 warning`` () =
    async {
        // makeSource 68 → 69-line body → in warn zone [55,70)
        let ctx = getContext opts (makeSource 68)
        let! msgs = functionLengthAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS002" && m.Severity = Severity.Warning)
    }

[<Fact>]
let ``function at 70 lines produces SS002 error`` () =
    async {
        let ctx = getContext opts (makeSource 70)
        let! msgs = functionLengthAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS002" && m.Severity = Severity.Error)
    }

[<Fact>]
let ``function at 100 lines produces SS002 error`` () =
    async {
        let ctx = getContext opts (makeSource 100)
        let! msgs = functionLengthAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS002" && m.Severity = Severity.Error)
    }
