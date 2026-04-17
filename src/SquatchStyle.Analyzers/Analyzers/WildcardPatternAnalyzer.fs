module SquatchStyle.Analyzers.WildcardPatternAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open SquatchStyle.Analyzers.Common

/// True when a type is a closed (non-abstract) F# discriminated union.
/// Abstract DUs (open hierarchies) are excluded because you can't enumerate
/// their cases — a wildcard is appropriate there.
let private isClosedDu (ty: FSharpType) =
    not ty.IsGenericParameter
    && ty.HasTypeDefinition
    && ty.TypeDefinition.IsFSharpUnion
    && not ty.TypeDefinition.IsAbstractClass

/// Walk the parse tree looking for match expressions that have a wildcard arm.
/// When we find one, we look up the matched expression's type via the pre-collected
/// symbol uses — if it's a closed DU, we flag it.
let private findSyntacticWildcards (ctx: CliContext) =
    // Pre-collect all symbol uses once. Keyed by (startLine, startCol, endCol) for fast lookup.
    let symbolUsesByRange =
        ctx.CheckFileResults.GetAllUsesOfAllSymbolsInFile()
        |> Seq.map (fun su -> (su.Range.StartLine, su.Range.StartColumn, su.Range.EndColumn), su)
        |> Map.ofSeq

    let lookupSymbol (range: Range) =
        symbolUsesByRange
        |> Map.tryFind (range.StartLine, range.StartColumn, range.EndColumn)

    [
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, modules, _, _, _)) ->
            for SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _) in modules do
                let rec walkDecl decl =
                    [
                        match decl with
                        | SynModuleDecl.Let(_, bindings, _) ->
                            for SynBinding(_, _, _, _, _, _, _, _, _, body, _, _, _) in bindings do
                                yield! walkExpr body
                        | SynModuleDecl.NestedModule(_, _, inner, _, _, _) ->
                            yield! inner |> List.collect walkDecl
                        | _ -> ()
                    ]

                and walkExpr expr =
                    [
                        match expr with
                        | SynExpr.Match(_, matchExpr, clauses, _, _) ->
                            for SynMatchClause(pat, _, clauseBody, clauseRange, _, _) in clauses do
                                match pat with
                                | SynPat.Wild _ ->
                                    // Look up the symbol at the match expression's range to get its type.
                                    match lookupSymbol matchExpr.Range with
                                    | Some su ->
                                        match su.Symbol with
                                        | :? FSharpMemberOrFunctionOrValue as mfv when isClosedDu mfv.FullType ->
                                            yield
                                                message
                                                    "SS005"
                                                    Severity.warning
                                                    clauseRange
                                                    "SS005: Wildcard pattern '| _ ->' on a closed discriminated union. \
                                                     Enumerate all cases explicitly so new union arms cause a compile error. \
                                                     SquatchStyle: assert the positive AND negative space."
                                        | _ -> ()
                                    | None -> ()
                                | _ ->
                                    yield! walkExpr clauseBody
                        | SynExpr.LetOrUse(_, _, _, _, bindings, body, _, _) ->
                            for SynBinding(_, _, _, _, _, _, _, _, _, bindBody, _, _, _) in bindings do
                                yield! walkExpr bindBody

                            yield! walkExpr body
                        | SynExpr.Sequential(_, _, e1, e2, _, _) ->
                            yield! walkExpr e1
                            yield! walkExpr e2
                        | SynExpr.IfThenElse(cond, thenE, elseE, _, _, _, _) ->
                            yield! walkExpr cond
                            yield! walkExpr thenE
                            yield! (elseE |> Option.toList |> List.collect walkExpr)
                        | _ -> ()
                    ]

                yield! decls |> List.collect walkDecl
        | _ -> ()
    ]

[<CliAnalyzer("SquatchStyle.WildcardPattern")>]
let wildcardPatternAnalyzer (ctx: CliContext) : Async<Message list> =
    async { return findSyntacticWildcards ctx }
