module SquatchStyle.Analyzers.MutableBindingAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open SquatchStyle.Analyzers.Common

let private suppressionMarker = "squatch:allow-mutable"

/// Check source lines for a suppression comment immediately above the binding.
let private isSuppressed (sourceText: ISourceText) (range: Range) =
    let suppressionLine = range.StartLine - 2 // 0-based, one line above
    if suppressionLine < 0 then
        false
    else
        let line = sourceText.GetLineString suppressionLine
        line.Contains suppressionMarker

let private checkBinding (sourceText: ISourceText) (binding: SynBinding) =
    let (SynBinding(_, _, _, isMutable, _, _, _, headPat, _, _, range, _, _)) = binding

    if not isMutable then
        None
    elif isSuppressed sourceText range then
        None
    else
        let name =
            match headPat with
            | SynPat.Named(SynIdent(ident, _), _, _, _) -> ident.idText
            | SynPat.LongIdent(lid, _, _, _, _, _) ->
                lid.LongIdent |> List.map _.idText |> String.concat "."
            | _ -> "<binding>"

        Some(
            message
                "SS003"
                Severity.warning
                range
                $"SS003: Mutable binding '{name}' violates immutability-first principle. \
                  If mutation is necessary (e.g., I/O boundary, performance-critical accumulator), \
                  add '// squatch:allow-mutable <reason>' on the line above."
        )

[<CliAnalyzer("SquatchStyle.MutableBinding")>]
let mutableBindingAnalyzer (ctx: CliContext) : Async<Message list> =
    async {
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, modules, _, _, _)) ->
            let sourceText = ctx.SourceText

            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    decls
                    |> List.collect (function
                        | SynModuleDecl.Let(_, bindings, _) ->
                            bindings |> List.choose (checkBinding sourceText)
                        | _ -> []))
        | _ -> return []
    }
