module SquatchStyle.Analyzers.Common

open FSharp.Analyzers.SDK
open FSharp.Compiler.Text

/// Severity constants — map SquatchStyle criticality to SDK severity.
module Severity =
    /// Hard violation: build fails. Reserved for safety rules.
    let error = Severity.Error
    /// Strong violation: build warns by default; CI should treat as error.
    let warning = Severity.Warning
    /// Advisory: informational, never blocks build.
    let hint = Severity.Hint

/// Builds a Message with consistent formatting.
let message (code: string) (severity: Severity) (range: Range) (text: string) : Message =
    {
        Type = "SquatchStyle"
        Message = text
        Code = code
        Severity = severity
        Range = range
        Fixes = []
    }

/// Produces a 'with fix' variant when a mechanical transformation is safe.
let messageWithFix
    (code: string)
    (severity: Severity)
    (range: Range)
    (text: string)
    (fixes: Fix list)
    : Message =
    {
        Type = "SquatchStyle"
        Message = text
        Code = code
        Severity = severity
        Range = range
        Fixes = fixes
    }
