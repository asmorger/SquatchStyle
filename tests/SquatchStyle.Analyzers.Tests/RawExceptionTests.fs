module SquatchStyle.Analyzers.Tests.RawExceptionTests

open Xunit
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.RawExceptionAnalyzer
open SquatchStyle.Analyzers.Tests.Helpers

[<Fact>]
let ``failwith produces SS007 warning`` () =
    async {
        let source =
            """
module RawExceptionTestModule1
let validate x =
    if x < 0 then failwith "negative"
    else x
"""

        let ctx = getContext opts source
        let! msgs = rawExceptionAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS007")
    }

[<Fact>]
let ``suppressed failwith produces no messages`` () =
    async {
        let source =
            """
module RawExceptionTestModule2
let validate x =
    // squatch:allow-exception invariant: x must be non-negative at this point
    if x < 0 then failwith "negative"
    else x
"""

        let ctx = getContext opts source
        let! msgs = rawExceptionAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``function without exceptions produces no messages`` () =
    async {
        let source =
            """
module RawExceptionTestModule3
let safe x =
    if x < 0 then Error "negative"
    else Ok x
"""

        let ctx = getContext opts source
        let! msgs = rawExceptionAnalyzer ctx
        Assert.Empty(msgs)
    }
