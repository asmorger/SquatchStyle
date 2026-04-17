module SquatchStyle.Analyzers.SilentErrorDiscardAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Symbols
open FSharp.Compiler.Symbols.FSharpExprPatterns
open SquatchStyle.Analyzers.Common

/// True when a type is Result<_,_>, Option<_>, or ValueOption<_>.
/// We check the type entity's LogicalName because it's stable across generic
/// instantiations (e.g., Result<int, string> and Result<unit, exn> both have
/// LogicalName = "Result").
let private isDiscardableType (ty: FSharpType) =
    not ty.IsGenericParameter
    && ty.HasTypeDefinition
    && (
        let name = ty.TypeDefinition.DisplayName
        name = "Result"
        || name = "Option"
        || name = "option" // F# alias
        || name = "ValueOption"
        || name = "voption" // F# alias
    )

let rec private walkExpr (e: FSharpExpr) : Message list =
    [
        match e with
        | Sequential(first, rest) ->
            // A Sequential whose first branch has a discardable type is the
            // pattern for: `riskyOp ()` as a statement (not bound to anything).
            if isDiscardableType first.Type then
                yield
                    message
                        "SS001"
                        Severity.error
                        first.Range
                        "SS001: Result/Option return value silently discarded. \
                         All errors must be handled. Bind with 'let', pipe through \
                         Result.map/bind, or explicitly use 'ignore' with a comment \
                         justifying the discard."

            // Keep walking both sides — nested discards exist.
            yield! walkExpr first
            yield! walkExpr rest

        | Let((_, bindExpr, _), body) ->
            yield! walkExpr bindExpr
            yield! walkExpr body

        | IfThenElse(cond, thenBranch, elseBranch) ->
            yield! walkExpr cond
            yield! walkExpr thenBranch
            yield! walkExpr elseBranch

        | _ ->
            // Fallback: recurse into all immediate sub-expressions so we
            // don't miss discards nested inside calls, lambdas, etc.
            for sub in e.ImmediateSubExpressions do
                yield! walkExpr sub
    ]

let private findDiscards (ctx: CliContext) =
    [
        match ctx.TypedTree with
        | None -> ()
        | Some tree ->
            let rec walkDecls decls =
                [
                    for decl in decls do
                        match decl with
                        | FSharpImplementationFileDeclaration.Entity(_, subDecls) ->
                            yield! walkDecls subDecls
                        | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(_, _, body) ->
                            yield! walkExpr body
                        | FSharpImplementationFileDeclaration.InitAction action ->
                            yield! walkExpr action
                ]

            yield! walkDecls tree.Declarations
    ]

[<CliAnalyzer("SquatchStyle.SilentErrorDiscard")>]
let silentErrorDiscardAnalyzer (ctx: CliContext) : Async<Message list> =
    async { return findDiscards ctx }
