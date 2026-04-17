module SquatchStyle.Analyzers.FunctionLengthAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open SquatchStyle.Analyzers.Common

[<Literal>]
let WarnThreshold = 55

[<Literal>]
let ErrorThreshold = 70

let private checkBinding (binding: SynBinding) =
    let (SynBinding(_, _, _, _, _, _, _, headPat, _, body, _, _, _)) = binding
    let startLine = body.Range.StartLine
    let endLine = body.Range.EndLine
    let lineCount = endLine - startLine + 1

    let nameRange =
        match headPat with
        | SynPat.LongIdent(lid, _, _, _, _, _) -> lid.Range
        | SynPat.Named(SynIdent(ident, _), _, _, _) -> ident.idRange
        | p -> p.Range

    let name =
        match headPat with
        | SynPat.LongIdent(lid, _, _, _, _, _) ->
            lid.LongIdent |> List.map _.idText |> String.concat "."
        | SynPat.Named(SynIdent(ident, _), _, _, _) -> ident.idText
        | _ -> "<anonymous>"

    if lineCount >= ErrorThreshold then
        Some(
            message
                "SS002"
                Severity.error
                nameRange
                $"SS002: Function '{name}' is {lineCount} lines ({ErrorThreshold} max). \
                  Decompose: push control flow up, push iteration logic down, \
                  keep leaf functions pure."
        )
    elif lineCount >= WarnThreshold then
        Some(
            message
                "SS002"
                Severity.warning
                nameRange
                $"SS002: Function '{name}' is {lineCount} lines (approaching {ErrorThreshold}-line limit). \
                  Consider decomposing before it hardens."
        )
    else
        None

let private walkDecls decls =
    let rec walkDecl =
        function
        | SynModuleDecl.Let(_, bindings, _) -> bindings |> List.choose checkBinding
        | SynModuleDecl.NestedModule(_, _, innerDecls, _, _, _) -> innerDecls |> List.collect walkDecl
        | SynModuleDecl.Types(typeDefs, _) ->
            typeDefs
            |> List.collect (fun (SynTypeDefn(_, _, members, _, _, _)) ->
                members
                |> List.collect (function
                    | SynMemberDefn.Member(binding, _) -> checkBinding binding |> Option.toList
                    | SynMemberDefn.LetBindings(bindings, _, _, _) -> bindings |> List.choose checkBinding
                    | _ -> []))
        | _ -> []

    decls |> List.collect walkDecl

[<CliAnalyzer("SquatchStyle.FunctionLength")>]
let functionLengthAnalyzer (ctx: CliContext) : Async<Message list> =
    async {
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) -> walkDecls decls)
        | _ -> return []
    }
