module SquatchStyle.Analyzers.RawExceptionAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open SquatchStyle.Analyzers.Common

/// Exception-raising identifiers that constitute "panic" — for programmer errors only.
let private panicFns = Set [ "failwith"; "failwithf"; "invalidOp"; "invalidArg"; "raise" ]

let private suppressionMarker = "squatch:allow-exception"

let private isSuppressedByLine (sourceText: FSharp.Compiler.Text.ISourceText) (range: FSharp.Compiler.Text.Range) =
    let line = range.StartLine - 2
    if line < 0 then false
    else sourceText.GetLineString(line).Contains(suppressionMarker)

let rec private findRawExceptions (sourceText: FSharp.Compiler.Text.ISourceText) (expr: SynExpr) =
    [
        match expr with
        | SynExpr.App(_, _, SynExpr.Ident ident, _, range) when panicFns.Contains ident.idText ->
            if not (isSuppressedByLine sourceText range) then
                yield
                    message
                        "SS007"
                        Severity.warning
                        range
                        $"SS007: '{ident.idText}' found in expression context. \
                          Reserve exception-raising for genuine programmer errors (violated invariants). \
                          Domain failures should be modelled as Result<'T, 'E>. \
                          If this is an invariant assertion, add '// squatch:allow-exception <reason>'."
        | SynExpr.Sequential(_, _, e1, e2, _, _) ->
            yield! findRawExceptions sourceText e1
            yield! findRawExceptions sourceText e2
        | SynExpr.LetOrUse(_, _, _, _, bindings, body, _, _) ->
            for SynBinding(_, _, _, _, _, _, _, _, _, bindBody, _, _, _) in bindings do
                yield! findRawExceptions sourceText bindBody
            yield! findRawExceptions sourceText body
        | SynExpr.Match(_, _, clauses, _, _) ->
            for SynMatchClause(_, _, clauseBody, _, _, _) in clauses do
                yield! findRawExceptions sourceText clauseBody
        | SynExpr.IfThenElse(cond, thenE, elseE, _, _, _, _) ->
            yield! findRawExceptions sourceText cond
            yield! findRawExceptions sourceText thenE
            yield! (elseE |> Option.toList |> List.collect (findRawExceptions sourceText))
        | _ -> ()
    ]

[<CliAnalyzer("SquatchStyle.RawException")>]
let rawExceptionAnalyzer (ctx: CliContext) : Async<Message list> =
    async {
        let sourceText = ctx.SourceText

        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    decls
                    |> List.collect (function
                        | SynModuleDecl.Let(_, bindings, _) ->
                            bindings
                            |> List.collect (fun (SynBinding(_, _, _, _, _, _, _, _, _, body, _, _, _)) ->
                                findRawExceptions sourceText body)
                        | _ -> []))
        | _ -> return []
    }
