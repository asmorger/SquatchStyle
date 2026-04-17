module SquatchStyle.Analyzers.Tests.MagicLiteralTests

open Xunit
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.MagicLiteralAnalyzer
open SquatchStyle.Analyzers.Tests.Helpers

[<Fact>]
let ``magic integer literal produces SS008 warning`` () =
    async {
        let source =
            """
module MagicLiteralTestModule1
let truncate xs = List.take 42 xs
"""

        let ctx = getContext opts source
        let! msgs = magicLiteralAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS008")
    }

[<Fact>]
let ``allowed literal zero produces no messages`` () =
    async {
        let source =
            """
module MagicLiteralTestModule2
let isEmpty xs = List.length xs = 0
"""

        let ctx = getContext opts source
        let! msgs = magicLiteralAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``allowed literal one produces no messages`` () =
    async {
        let source =
            """
module MagicLiteralTestModule3
let singleton x = List.take 1 [x]
"""

        let ctx = getContext opts source
        let! msgs = magicLiteralAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``named literal does not produce SS008`` () =
    async {
        let source =
            """
module MagicLiteralTestModule4
[<Literal>]
let MaxItems = 42
let truncate xs = List.take MaxItems xs
"""

        let ctx = getContext opts source
        let! msgs = magicLiteralAnalyzer ctx
        Assert.Empty(msgs)
    }
