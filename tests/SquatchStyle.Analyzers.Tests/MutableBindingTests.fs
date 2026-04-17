module SquatchStyle.Analyzers.Tests.MutableBindingTests

open Xunit
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.MutableBindingAnalyzer
open SquatchStyle.Analyzers.Tests.Helpers

[<Fact>]
let ``unsuppressed let mutable produces SS003 warning`` () =
    async {
        let source =
            """
module MutableBindingTestModule1
let mutable counter = 0
"""

        let ctx = getContext opts source
        let! msgs = mutableBindingAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS003")
    }

[<Fact>]
let ``suppressed let mutable with squatch:allow-mutable produces no messages`` () =
    async {
        let source =
            """
module MutableBindingTestModule2
// squatch:allow-mutable Accumulator for performance-critical hot path.
let mutable counter = 0
"""

        let ctx = getContext opts source
        let! msgs = mutableBindingAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``immutable let binding produces no messages`` () =
    async {
        let source =
            """
module MutableBindingTestModule3
let value = 42
"""

        let ctx = getContext opts source
        let! msgs = mutableBindingAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``multiple mutable bindings each produce SS003`` () =
    async {
        let source =
            """
module MutableBindingTestModule4
let mutable x = 0
let mutable y = 0
"""

        let ctx = getContext opts source
        let! msgs = mutableBindingAnalyzer ctx
        Assert.Equal(2, msgs |> List.filter (fun m -> m.Code = "SS003") |> List.length)
    }
