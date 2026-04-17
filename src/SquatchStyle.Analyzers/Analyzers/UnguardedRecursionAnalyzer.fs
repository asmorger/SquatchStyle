module SquatchStyle.Analyzers.UnguardedRecursionAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open SquatchStyle.Analyzers.Common

let private hasTailCallAttr (attrs: SynAttributes) =
    attrs
    |> List.exists (fun attrList ->
        attrList.Attributes
        |> List.exists (fun attr ->
            let name =
                attr.TypeName.LongIdent |> List.map _.idText |> String.concat "."

            name = "TailCall" || name = "Microsoft.FSharp.Core.TailCall"))

let private checkRecBinding (binding: SynBinding) =
    let (SynBinding(_, _, _, _, attrs, _, _, headPat, _, _, range, _, _)) = binding

    if hasTailCallAttr attrs then
        None
    else
        let name =
            match headPat with
            | SynPat.Named(SynIdent(ident, _), _, _, _) -> ident.idText
            | SynPat.LongIdent(lid, _, _, _, _, _) ->
                lid.LongIdent |> List.map _.idText |> String.concat "."
            | _ -> "<recursive>"

        Some(
            message
                "SS004"
                Severity.error
                range
                $"SS004: Recursive function '{name}' missing [<TailCall>] attribute. \
                  Without [<TailCall>], unbounded stack growth is possible. \
                  Add [<TailCall>] (F# 8+) so the compiler verifies tail-call correctness, \
                  or refactor to use fold/unfold/Seq combinators."
        )

let private walkDecls decls =
    decls
    |> List.collect (function
        | SynModuleDecl.Let(isRec, bindings, _) when isRec -> bindings |> List.choose checkRecBinding
        | _ -> [])

[<CliAnalyzer("SquatchStyle.UnguardedRecursion")>]
let unguardedRecursionAnalyzer (ctx: CliContext) : Async<Message list> =
    async {
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) -> walkDecls decls)
        | _ -> return []
    }
