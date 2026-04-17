module SquatchStyle.Analyzers.Tests.Helpers

open FSharp.Analyzers.SDK.Testing

/// Shared project options, initialized once for the test suite.
/// mkOptionsFromProject creates a real project with FSharp.Core references,
/// which is required for the FCS type-checker inside getContext.
let opts =
    mkOptionsFromProject "net10.0" [] |> Async.AwaitTask |> Async.RunSynchronously
