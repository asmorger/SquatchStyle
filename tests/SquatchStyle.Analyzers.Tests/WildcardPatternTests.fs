module SquatchStyle.Analyzers.Tests.WildcardPatternTests

open Xunit
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.WildcardPatternAnalyzer
open SquatchStyle.Analyzers.Tests.Helpers

[<Fact>]
let ``wildcard on closed DU produces SS005 warning`` () =
    async {
        let source =
            """
module WildcardPatternTestModule1
type Color = Red | Green | Blue

let describe (c: Color) =
    match c with
    | Red -> "red"
    | _ -> "other"
"""

        let ctx = getContext opts source
        let! msgs = wildcardPatternAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS005")
    }

[<Fact>]
let ``exhaustive match on closed DU produces no messages`` () =
    async {
        let source =
            """
module WildcardPatternTestModule2
type Color = Red | Green | Blue

let describe (c: Color) =
    match c with
    | Red   -> "red"
    | Green -> "green"
    | Blue  -> "blue"
"""

        let ctx = getContext opts source
        let! msgs = wildcardPatternAnalyzer ctx
        Assert.Empty(msgs)
    }
