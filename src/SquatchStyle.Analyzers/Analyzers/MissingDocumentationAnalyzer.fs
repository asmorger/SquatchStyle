module SquatchStyle.Analyzers.MissingDocumentationAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open FSharp.Compiler.Xml
open SquatchStyle.Analyzers.Common

let private hasXmlDoc (doc: PreXmlDoc) = not doc.IsEmpty

type private RangeKey = int * int * int * int

let private toKey (r: FSharp.Compiler.Text.Range) : RangeKey =
    r.StartLine, r.StartColumn, r.EndLine, r.EndColumn

/// Build a set of range keys that correspond to public symbol definitions in the file.
/// FCS does not expose SynBinding.accessibility reliably for module-level `let` bindings,
/// so we use the typed check results to determine real accessibility.
let private publicDefinitionRangeKeys (ctx: CliContext) =
    ctx.CheckFileResults.GetAllUsesOfAllSymbolsInFile()
    |> Seq.choose (fun su ->
        if su.IsFromDefinition then
            match su.Symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv when mfv.Accessibility.IsPublic ->
                Some(toKey su.Range)
            | :? FSharpEntity as ent when ent.Accessibility.IsPublic ->
                Some(toKey su.Range)
            | _ -> None
        else
            None)
    |> Set.ofSeq

/// Extract the name and its range from a binding head pattern.
/// The symbol use range in FCS corresponds to just the identifier, not the full headPat.
let private bindingNameAndRange (headPat: SynPat) =
    match headPat with
    | SynPat.Named(SynIdent(ident, _), _, _, _) ->
        Some(ident.idText, ident.idRange)
    | SynPat.LongIdent(SynLongIdent(ids, _, _), _, _, _, _, _) ->
        match ids with
        | [] -> None
        | first :: _ ->
            Some(ids |> List.map (fun id -> id.idText) |> String.concat ".", first.idRange)
    | _ -> None

let private checkBinding (pubRanges: Set<RangeKey>) (SynBinding(_, _, _, _, _, doc, _, headPat, _, _, _, _, _)) =
    match bindingNameAndRange headPat with
    | Some(name, nameRange) when not (hasXmlDoc doc) && pubRanges.Contains(toKey nameRange) ->
        Some(
            message
                "SS006"
                Severity.hint
                nameRange
                $"SS006: Public function '{name}' lacks XML documentation. \
                  SquatchStyle: always say why; always say how. \
                  Add /// summary, purpose, and rationale — not just what the code does."
        )
    | _ -> None

let private walkDecls (pubRanges: Set<RangeKey>) decls =
    decls
    |> List.collect (function
        | SynModuleDecl.Let(_, bindings, _) -> bindings |> List.choose (checkBinding pubRanges)
        | SynModuleDecl.Types(typeDefs, _) ->
            typeDefs
            |> List.collect (fun (SynTypeDefn(SynComponentInfo(_, _, _, _, doc, _, _, range), _, members, _, _, _)) ->
                [
                    if not (hasXmlDoc doc) && pubRanges.Contains(toKey range) then
                        yield
                            message
                                "SS006"
                                Severity.hint
                                range
                                "SS006: Public type lacks XML documentation. \
                                 Describe its invariants, intended use, and any non-obvious constraints."
                    yield!
                        members
                        |> List.collect (function
                            | SynMemberDefn.Member(binding, _) ->
                                checkBinding pubRanges binding |> Option.toList
                            | _ -> [])
                ])
        | _ -> [])

[<CliAnalyzer("SquatchStyle.MissingDocumentation")>]
let missingDocumentationAnalyzer (ctx: CliContext) : Async<Message list> =
    async {
        let pubRanges = publicDefinitionRangeKeys ctx

        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    walkDecls pubRanges decls)
        | _ -> return []
    }
