module SquatchStyle.Analyzers.Tests.SilentErrorDiscardTests

open Xunit
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.SilentErrorDiscardAnalyzer
open SquatchStyle.Analyzers.Tests.Helpers

[<Fact>]
let ``discarded Result produces SS001 error`` () =
    async {
        let source =
            """
module SilentErrorDiscardTestModule1
let riskyOp () : Result<int, string> = Ok 42
let bad () =
    riskyOp ()
    ()
"""

        let ctx = getContext opts source
        let! msgs = silentErrorDiscardAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS001")
    }

[<Fact>]
let ``bound Result does not trigger SS001`` () =
    async {
        let source =
            """
module SilentErrorDiscardTestModule2
let riskyOp () : Result<int, string> = Ok 42
let good () =
    let result = riskyOp ()
    match result with
    | Ok v    -> v
    | Error _ -> 0
"""

        let ctx = getContext opts source
        let! msgs = silentErrorDiscardAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``discarded Option produces SS001 error`` () =
    async {
        let source =
            """
module SilentErrorDiscardTestModule3
let maybeOp () : int option = Some 42
let bad () =
    maybeOp ()
    ()
"""

        let ctx = getContext opts source
        let! msgs = silentErrorDiscardAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS001")
    }

[<Fact>]
let ``unit return value does not trigger SS001`` () =
    async {
        let source =
            """
module SilentErrorDiscardTestModule4
let sideEffect () : unit = ()
let ok () =
    sideEffect ()
    ()
"""

        let ctx = getContext opts source
        let! msgs = silentErrorDiscardAnalyzer ctx
        Assert.Empty(msgs)
    }
