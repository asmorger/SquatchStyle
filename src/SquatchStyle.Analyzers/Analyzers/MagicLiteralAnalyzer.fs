module SquatchStyle.Analyzers.MagicLiteralAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open SquatchStyle.Analyzers.Common

/// Allow these common "obvious" literals that don't need naming.
let private allowedInts = Set [ 0; 1; -1; 2 ]

let rec private findMagicLiterals (expr: SynExpr) =
    [
        match expr with
        | SynExpr.Const(SynConst.Int32 n, range) when not (allowedInts.Contains n) ->
            yield
                message
                    "SS008"
                    Severity.warning
                    range
                    $"SS008: Magic literal '{n}' found inline. \
                      SquatchStyle: put a limit on everything — explicitly, with a name. \
                      Declare '[<Literal>] let MaxItems = {n}' and reference it by name."
        | SynExpr.Const(SynConst.Double d, range) when d <> 0.0 && d <> 1.0 ->
            yield
                message
                    "SS008"
                    Severity.warning
                    range
                    $"SS008: Magic float literal '{d}' found inline. Use a named [<Literal>] constant."
        | SynExpr.Sequential(_, _, e1, e2, _, _) ->
            yield! findMagicLiterals e1
            yield! findMagicLiterals e2
        | SynExpr.LetOrUse(_, _, _, _, bindings, body, _, _) ->
            for SynBinding(_, _, _, _, _, _, _, _, _, bindBody, _, _, _) in bindings do
                yield! findMagicLiterals bindBody
            yield! findMagicLiterals body
        | SynExpr.App(_, _, fn, arg, _) ->
            yield! findMagicLiterals fn
            yield! findMagicLiterals arg
        | SynExpr.Tuple(_, exprs, _, _) ->
            yield! exprs |> List.collect findMagicLiterals
        | SynExpr.Match(_, _, clauses, _, _) ->
            for SynMatchClause(_, _, clauseBody, _, _, _) in clauses do
                yield! findMagicLiterals clauseBody
        | SynExpr.IfThenElse(cond, thenE, elseE, _, _, _, _) ->
            yield! findMagicLiterals cond
            yield! findMagicLiterals thenE
            yield! (elseE |> Option.toList |> List.collect findMagicLiterals)
        | _ -> ()
    ]

[<CliAnalyzer("SquatchStyle.MagicLiteral")>]
let magicLiteralAnalyzer (ctx: CliContext) : Async<Message list> =
    async {
        let isLiteralAttr (attrs: SynAttributes) =
            attrs
            |> List.exists (fun attrList ->
                attrList.Attributes
                |> List.exists (fun attr ->
                    match attr.TypeName with
                    | SynLongIdent([ id ], _, _) -> id.idText = "Literal"
                    | _ -> false))

        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    decls
                    |> List.collect (function
                        | SynModuleDecl.Let(_, bindings, _) ->
                            bindings
                            |> List.collect (fun (SynBinding(_, _, _, _, attrs, _, _, _, _, body, _, _, _)) ->
                                if isLiteralAttr attrs then [] else findMagicLiterals body)
                        | _ -> []))
        | _ -> return []
    }
