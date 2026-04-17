module SquatchStyle.Analyzers.Tests.UnguardedRecursionTests

open Xunit
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.UnguardedRecursionAnalyzer
open SquatchStyle.Analyzers.Tests.Helpers

[<Fact>]
let ``let rec without TailCall attribute produces SS004 error`` () =
    async {
        let source =
            """
module UnguardedRecursionTestModule1
let rec countdown n =
    if n <= 0 then ()
    else countdown (n - 1)
"""

        let ctx = getContext opts source
        let! msgs = unguardedRecursionAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS004")
    }

[<Fact>]
let ``let rec with TailCall attribute produces no messages`` () =
    async {
        let source =
            """
module UnguardedRecursionTestModule2
[<TailCall>]
let rec countdown n =
    if n <= 0 then ()
    else countdown (n - 1)
"""

        let ctx = getContext opts source
        let! msgs = unguardedRecursionAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``non-recursive let binding produces no messages`` () =
    async {
        let source =
            """
module UnguardedRecursionTestModule3
let add x y = x + y
"""

        let ctx = getContext opts source
        let! msgs = unguardedRecursionAnalyzer ctx
        Assert.Empty(msgs)
    }
