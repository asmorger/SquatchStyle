module SquatchStyle.Analyzers.Tests.MissingDocumentationTests

open Xunit
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.MissingDocumentationAnalyzer
open SquatchStyle.Analyzers.Tests.Helpers

[<Fact>]
let ``public function without doc produces SS006 hint`` () =
    async {
        let source =
            """
module MissingDocTestModule1
let doTheThing () = 42
"""

        let ctx = getContext opts source
        let! msgs = missingDocumentationAnalyzer ctx
        Assert.Contains(msgs, fun m -> m.Code = "SS006")
    }

[<Fact>]
let ``public function with xml doc produces no messages`` () =
    async {
        let source =
            """
module MissingDocTestModule2
/// Does the thing.
let doTheThing () = 42
"""

        let ctx = getContext opts source
        let! msgs = missingDocumentationAnalyzer ctx
        Assert.Empty(msgs)
    }

[<Fact>]
let ``private function without doc produces no messages`` () =
    async {
        let source =
            """
module MissingDocTestModule3
let private doTheThing () = 42
"""

        let ctx = getContext opts source
        let! msgs = missingDocumentationAnalyzer ctx
        Assert.Empty(msgs)
    }
