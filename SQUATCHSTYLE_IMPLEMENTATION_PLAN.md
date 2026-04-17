# SquatchStyle F# — Implementation Plan

> Safety → Performance → Developer Experience. In that order.

---

## 0. Inspiration & Philosophy

SquatchStyle is a direct adaptation of **TigerStyle** — the engineering discipline
codified by the [TigerBeetle](https://github.com/tigerbeetle/tigerbeetle) team for
their safety-critical financial database written in Zig.

### What is TigerStyle?

TigerStyle is a set of hard constraints that TigerBeetle engineers apply to every
line of code. The rules are not aesthetic preferences — they are load-bearing. The
philosophy holds that:

- **Every error must be handled.** Silent failure is a bug waiting to be a disaster.
- **Every limit must be explicit.** Unbounded growth (stack, heap, time) is a latent crash.
- **Every mutation must be justified.** State is the source of most bugs.
- **Assert the positive AND negative space.** Exhaustiveness is not optional.

The full original is documented at
[docs/TIGERSTYLE.md](https://github.com/tigerbeetle/tigerbeetle/blob/main/docs/TIGERSTYLE.md)
in the TigerBeetle repo.

### What is SquatchStyle?

SquatchStyle takes those same principles and ports them into idiomatic **F#**:

| TigerStyle (Zig)           | SquatchStyle (F#)                              |
|----------------------------|------------------------------------------------|
| All errors handled         | `Result`/`Option` never silently discarded     |
| Explicit limits everywhere | Named `[<Literal>]` constants, no magic values |
| No unbounded recursion     | `let rec` requires `[<TailCall>]`             |
| Assert invariants          | Pattern matches enumerate all cases explicitly |
| Justify mutation           | `let mutable` requires suppression comment     |

The rule codes use the prefix **`SS`** (SquatchStyle). Suppression directives use
the marker prefix `squatch:` to distinguish them from any upstream TigerStyle usage.

The priority ordering — Safety → Performance → Developer Experience — is preserved
unchanged from the original.

---

## 1. Solution Layout

```
SquatchStyle/
├── SquatchStyle.sln
├── .editorconfig                          ← Fantomas + editor norms
├── .fantomasrc                            ← Fantomas overrides
├── build.fsx                              ← FSI build script (entry point)
├── build/
│   ├── Build.fs                           ← FAKE targets
│   └── build.fsproj
├── src/
│   └── SquatchStyle.Analyzers/
│       ├── SquatchStyle.Analyzers.fsproj
│       ├── Common.fs                      ← Shared helpers, message builders
│       ├── Analyzers/
│       │   ├── SilentErrorDiscardAnalyzer.fs
│       │   ├── FunctionLengthAnalyzer.fs
│       │   ├── MutableBindingAnalyzer.fs
│       │   ├── UnguardedRecursionAnalyzer.fs
│       │   ├── WildcardPatternAnalyzer.fs
│       │   ├── MissingDocumentationAnalyzer.fs
│       │   ├── RawExceptionAnalyzer.fs
│       │   └── MagicLiteralAnalyzer.fs
│       └── AssemblyInfo.fs
├── tests/
│   └── SquatchStyle.Analyzers.Tests/
│       ├── SquatchStyle.Analyzers.Tests.fsproj
│       ├── Helpers.fs
│       ├── SilentErrorDiscardTests.fs
│       ├── FunctionLengthTests.fs
│       ├── MutableBindingTests.fs
│       ├── UnguardedRecursionTests.fs
│       ├── WildcardPatternTests.fs
│       ├── MissingDocumentationTests.fs
│       ├── RawExceptionTests.fs
│       └── MagicLiteralTests.fs
└── docs/
    ├── SQUATCHSTYLE_SKILL.md                ← AI agent skill document
    └── RULES.md                           ← Human-readable rule reference
```

---

## 2. Project Configuration

### `SquatchStyle.Analyzers.fsproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>SquatchStyle.Analyzers</AssemblyName>
    <RootNamespace>SquatchStyle.Analyzers</RootNamespace>
    <!-- Required: prevents the analyzer itself from being analyzed -->
    <IsPackable>true</IsPackable>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <DevelopmentDependency>true</DevelopmentDependency>
    <NoWarn>FS0044</NoWarn>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FSharp.Analyzers.SDK" Version="0.27.0" />
    <PackageReference Include="FSharp.Compiler.Service" Version="43.9.300" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="AssemblyInfo.fs" />
    <Compile Include="Common.fs" />
    <Compile Include="Analyzers/SilentErrorDiscardAnalyzer.fs" />
    <Compile Include="Analyzers/FunctionLengthAnalyzer.fs" />
    <Compile Include="Analyzers/MutableBindingAnalyzer.fs" />
    <Compile Include="Analyzers/UnguardedRecursionAnalyzer.fs" />
    <Compile Include="Analyzers/WildcardPatternAnalyzer.fs" />
    <Compile Include="Analyzers/MissingDocumentationAnalyzer.fs" />
    <Compile Include="Analyzers/RawExceptionAnalyzer.fs" />
    <Compile Include="Analyzers/MagicLiteralAnalyzer.fs" />
  </ItemGroup>

  <!-- NuGet packaging: emit analyzer into the correct folder -->
  <ItemGroup>
    <None Include="$(OutputPath)\$(AssemblyName).dll"
          Pack="true"
          PackagePath="analyzers/dotnet/fs"
          Visible="false" />
  </ItemGroup>

</Project>
```

### `SquatchStyle.Analyzers.Tests.fsproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>SquatchStyle.Analyzers.Tests</RootNamespace>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.1" />
    <PackageReference Include="FSharp.Analyzers.SDK.Testing" Version="0.27.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\SquatchStyle.Analyzers\SquatchStyle.Analyzers.fsproj" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="Helpers.fs" />
    <Compile Include="SilentErrorDiscardTests.fs" />
    <Compile Include="FunctionLengthTests.fs" />
    <Compile Include="MutableBindingTests.fs" />
    <Compile Include="UnguardedRecursionTests.fs" />
    <Compile Include="WildcardPatternTests.fs" />
    <Compile Include="MissingDocumentationTests.fs" />
    <Compile Include="RawExceptionTests.fs" />
    <Compile Include="MagicLiteralTests.fs" />
  </ItemGroup>

</Project>
```

---

## 3. Common Infrastructure

### `Common.fs`

```fsharp
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
let message
    (code: string)
    (severity: Severity)
    (range: Range)
    (text: string)
    : Message =
    {
        Type    = "SquatchStyle"
        Message = text
        Code    = code
        Severity = severity
        Range   = range
        Fixes   = []
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
        Type    = "SquatchStyle"
        Message = text
        Code    = code
        Severity = severity
        Range   = range
        Fixes   = fixes
    }

/// Walk a SynModuleOrNamespace to collect all top-level bindings.
/// Returns (xmlDoc option, binding) pairs for documentation analysis.
let collectTopLevelBindings decls =
    decls
    |> List.collect (function
        | Ast.SynModuleDecl.Let(_, bindings, _) ->
            bindings |> List.map (fun b -> b)
        | _ -> [])
```

---

## 4. Analyzer Implementations

### 4.1 `SilentErrorDiscardAnalyzer.fs`

**Rule SS001** — Highest priority. A discarded `Result` or `Option` is a silent failure path.

**Detection strategy**: Walk the *typed* tree looking for top-level expression statements
whose inferred type is `Result<_,_>`, `Option<_>`, or `ValueOption<_>`. These are expressions
that appear as `do`-equivalent statements (not bound by `let`).

```fsharp
module SquatchStyle.Analyzers.SilentErrorDiscardAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Symbols
open SquatchStyle.Analyzers.Common

/// Returns true when a type display string looks like Result or Option.
let private isDiscardableType (typeName: string) =
    typeName.StartsWith("Result<")
    || typeName.StartsWith("Option<")
    || typeName.StartsWith("ValueOption<")
    || typeName = "unit option"     // edge case: option chained to unit
    || typeName = "unit voption"

/// Walk TAST expressions looking for discarded effectful results.
/// We examine ImplFile → entity → member → expression bodies.
let private findDiscards (ctx: AnalyzerContext) =
    [
        match ctx.TypedTree with
        | None -> ()
        | Some tree ->
            for entity in tree.Declarations do
                match entity with
                | FSharpImplementationFileDeclaration.Entity(_, subDecls) ->
                    for decl in subDecls do
                        match decl with
                        | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(_, _, expr) ->
                            // Walk the expression body for discard patterns.
                            // Sequential expressions (e1; e2) where e1 is not unit.
                            let rec walkExpr (e: FSharpExpr) = [
                                match e with
                                | BasicPatterns.Sequential(first, rest) ->
                                    let typeName = first.Type.Format(ctx.CheckFileResults.PartialAssemblySignature.Attributes |> ignore; FSharpDisplayContext.Empty)
                                    if isDiscardableType typeName then
                                        yield message
                                            "SS001"
                                            Severity.error
                                            (first.Range)
                                            $"SS001: Result/Option return value silently discarded. \
                                              All errors must be handled. Bind with 'let', pipe through \
                                              Result.map/bind, or explicitly use 'ignore' with a comment \
                                              justifying the discard."
                                    yield! walkExpr rest
                                | BasicPatterns.Let((_, bindExpr), body) ->
                                    yield! walkExpr bindExpr
                                    yield! walkExpr body
                                | BasicPatterns.IfThenElse(cond, thenB, elseB) ->
                                    yield! walkExpr cond
                                    yield! walkExpr thenB
                                    yield! walkExpr elseB
                                | _ -> ()
                            ]
                            yield! walkExpr expr
                        | _ -> ()
                | _ -> ()
    ]

[<Analyzer("SquatchStyle.SilentErrorDiscard")>]
let silentErrorDiscardAnalyzer (ctx: AnalyzerContext) : Async<Message list> =
    async { return findDiscards ctx }
```

**Test fixture** (`SilentErrorDiscardTests.fs`):

```fsharp
module SquatchStyle.Analyzers.Tests.SilentErrorDiscardTests

open Xunit
open FSharp.Analyzers.SDK.Testing
open SquatchStyle.Analyzers.SilentErrorDiscardAnalyzer

[<Fact>]
let ``discarded Result produces SS001 error`` () = async {
    let source = """
module M
let riskyOp () : Result<int, string> = Ok 42
let bad () =
    riskyOp ()    // discarded — no let binding
    ()
"""
    let! msgs = getContextFor source |> runAnalyzer silentErrorDiscardAnalyzer
    Assert.Contains(msgs, fun m -> m.Code = "SS001")
}

[<Fact>]
let ``bound Result does not trigger SS001`` () = async {
    let source = """
module M
let riskyOp () : Result<int, string> = Ok 42
let good () =
    let result = riskyOp ()
    match result with
    | Ok v    -> v
    | Error _ -> 0
"""
    let! msgs = getContextFor source |> runAnalyzer silentErrorDiscardAnalyzer
    Assert.Empty(msgs)
}
```

---

### 4.2 `FunctionLengthAnalyzer.fs`

**Rule SS002** — Hard limit: 70 lines per function body. Warn at 55, error at 70.

```fsharp
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
    let endLine   = body.Range.EndLine
    let lineCount = endLine - startLine + 1

    let nameRange =
        match headPat with
        | SynPat.LongIdent(lid, _, _, _, _, _) -> lid.Range
        | SynPat.Named(_, ident, _, _, _)      -> ident.idRange
        | p -> p.Range

    let name =
        match headPat with
        | SynPat.LongIdent(lid, _, _, _, _, _) ->
            lid.LongIdent |> List.map _.idText |> String.concat "."
        | SynPat.Named(_, ident, _, _, _) -> ident.idText
        | _ -> "<anonymous>"

    if lineCount >= ErrorThreshold then
        Some (message
            "SS002"
            Severity.error
            nameRange
            $"SS002: Function '{name}' is {lineCount} lines ({ErrorThreshold} max). \
              Decompose: push control flow up, push iteration logic down, \
              keep leaf functions pure.")
    elif lineCount >= WarnThreshold then
        Some (message
            "SS002"
            Severity.warning
            nameRange
            $"SS002: Function '{name}' is {lineCount} lines (approaching {ErrorThreshold}-line limit). \
              Consider decomposing before it hardens.")
    else
        None

let private walkDecls decls =
    let rec walkDecl = function
        | SynModuleDecl.Let(_, bindings, _) ->
            bindings |> List.choose checkBinding
        | SynModuleDecl.NestedModule(_, _, innerDecls, _, _, _) ->
            innerDecls |> List.collect walkDecl
        | SynModuleDecl.Types(typeDefs, _) ->
            typeDefs |> List.collect (fun (SynTypeDefn(_, repr, members, _, _, _)) ->
                members |> List.collect (function
                    | SynMemberDefn.Member(binding, _)             -> checkBinding binding |> Option.toList
                    | SynMemberDefn.LetBindings(bindings, _, _, _) -> bindings |> List.choose checkBinding
                    | _ -> []))
        | _ -> []
    decls |> List.collect walkDecl

[<Analyzer("SquatchStyle.FunctionLength")>]
let functionLengthAnalyzer (ctx: AnalyzerContext) : Async<Message list> =
    async {
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    walkDecls decls)
        | _ -> return []
    }
```

---

### 4.3 `MutableBindingAnalyzer.fs`

**Rule SS003** — Every `let mutable` requires explicit justification.

Strategy: Flag `let mutable` bindings; provide a suppression mechanism via a structured
comment `// squatch:allow-mutable <reason>` on the preceding line.

```fsharp
module SquatchStyle.Analyzers.MutableBindingAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open SquatchStyle.Analyzers.Common

let private suppressionMarker = "squatch:allow-mutable"

/// Check source lines for a suppression comment immediately above the binding.
let private isSuppressed (sourceText: ISourceText) (range: Range) =
    let suppressionLine = range.StartLine - 2  // 0-based, one line above
    if suppressionLine < 0 then false
    else
        let line = sourceText.GetLineString suppressionLine
        line.Contains suppressionMarker

let private checkBinding (sourceText: ISourceText) (SynBinding(_, _, _, isMutable, _, _, _, headPat, _, _, range, _, _)) =
    if not isMutable then None
    elif isSuppressed sourceText range then None
    else
        let name =
            match headPat with
            | SynPat.Named(_, ident, _, _, _) -> ident.idText
            | SynPat.LongIdent(lid, _, _, _, _, _) ->
                lid.LongIdent |> List.map _.idText |> String.concat "."
            | _ -> "<binding>"
        Some (message
            "SS003"
            Severity.warning
            range
            $"SS003: Mutable binding '{name}' violates immutability-first principle. \
              If mutation is necessary (e.g., I/O boundary, performance-critical accumulator), \
              add '// squatch:allow-mutable <reason>' on the line above.")

[<Analyzer("SquatchStyle.MutableBinding")>]
let mutableBindingAnalyzer (ctx: AnalyzerContext) : Async<Message list> =
    async {
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, _, modules, _, _, _)) ->
            let sourceText = ctx.SourceText
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    decls |> List.collect (function
                        | SynModuleDecl.Let(_, bindings, _) ->
                            bindings |> List.choose (checkBinding sourceText)
                        | _ -> []))
        | _ -> return []
    }
```

---

### 4.4 `UnguardedRecursionAnalyzer.fs`

**Rule SS004** — `let rec` without `[<TailCall>]` is a stack-overflow risk.

```fsharp
module SquatchStyle.Analyzers.UnguardedRecursionAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open SquatchStyle.Analyzers.Common

let private hasTailCallAttr (attrs: SynAttributes) =
    attrs
    |> List.exists (fun attrList ->
        attrList.Attributes
        |> List.exists (fun attr ->
            let name = attr.TypeName.LongIdent |> List.map _.idText |> String.concat "."
            name = "TailCall" || name = "Microsoft.FSharp.Core.TailCall"))

let private checkRecBinding (SynBinding(_, _, _, _, attrs, _, _, headPat, _, _, range, _, _)) =
    if hasTailCallAttr attrs then None
    else
        let name =
            match headPat with
            | SynPat.Named(_, ident, _, _, _)          -> ident.idText
            | SynPat.LongIdent(lid, _, _, _, _, _) ->
                lid.LongIdent |> List.map _.idText |> String.concat "."
            | _ -> "<recursive>"
        Some (message
            "SS004"
            Severity.error
            range
            $"SS004: Recursive function '{name}' missing [<TailCall>] attribute. \
              Without [<TailCall>], unbounded stack growth is possible. \
              Add [<TailCall>] (F# 8+) so the compiler verifies tail-call correctness, \
              or refactor to use fold/unfold/Seq combinators.")

let private walkDecls decls =
    decls |> List.collect (function
        | SynModuleDecl.Let(isRec, bindings, _) when isRec ->
            bindings |> List.choose checkRecBinding
        | _ -> [])

[<Analyzer("SquatchStyle.UnguardedRecursion")>]
let unguardedRecursionAnalyzer (ctx: AnalyzerContext) : Async<Message list> =
    async {
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    walkDecls decls)
        | _ -> return []
    }
```

---

### 4.5 `WildcardPatternAnalyzer.fs`

**Rule SS005** — Wildcard `| _ ->` on closed discriminated unions swallows unhandled cases.

Detection: Find match expressions over a DU type where one arm is a wildcard, and the DU
has a known, finite set of cases (i.e., it's not an `[<Struct>]` or open type).

```fsharp
module SquatchStyle.Analyzers.WildcardPatternAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Symbols
open SquatchStyle.Analyzers.Common

/// True when a type is a closed (non-RequireQualifiedAccess is irrelevant here;
/// what matters is it's a DU defined in the same assembly or known sealed).
let private isClosedDu (ty: FSharpType) =
    not ty.IsGenericParameter
    && ty.HasTypeDefinition
    && ty.TypeDefinition.IsFSharpUnion
    && not ty.TypeDefinition.IsAbstract   // abstract = open hierarchy

let private findWildcardsInTypedTree (ctx: AnalyzerContext) =
    // Walk typed expressions looking for DecisionTree / UnionCaseTest patterns
    // that contain a catch-all arm on a known closed DU.
    // NOTE: This is an approximation — full exhaustiveness analysis lives in the
    // compiler. We flag the syntactic pattern and let humans judge.
    [
        match ctx.TypedTree with
        | None -> ()
        | Some tree ->
            let rec walkExpr (e: FSharpExpr) = [
                match e with
                | BasicPatterns.DecisionTree(matchExpr, _) ->
                    // The compiler has already validated exhaustiveness;
                    // we look for wildcard in the *parse* layer (see below).
                    yield! walkExpr matchExpr
                | BasicPatterns.Let((_, b), body) ->
                    yield! walkExpr b
                    yield! walkExpr body
                | BasicPatterns.Sequential(a, b) ->
                    yield! walkExpr a
                    yield! walkExpr b
                | _ -> ()
            ]
            for entity in tree.Declarations do
                match entity with
                | FSharpImplementationFileDeclaration.MemberOrFunctionOrValue(_, _, expr) ->
                    yield! walkExpr expr
                | _ -> ()
    ]

/// Syntactic pass: find match arms with SynPat.Wild on expressions typed as DU.
let private findSyntacticWildcards (ctx: AnalyzerContext) =
    let checkFileResults = ctx.CheckFileResults
    [
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, _, modules, _, _, _)) ->
            for SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _) in modules do
                let rec walkDecl decl = [
                    match decl with
                    | SynModuleDecl.Let(_, bindings, _) ->
                        for SynBinding(_, _, _, _, _, _, _, _, _, body, _, _, _) in bindings do
                            yield! walkExpr body
                    | SynModuleDecl.NestedModule(_, _, inner, _, _, _) ->
                        yield! inner |> List.collect walkDecl
                    | _ -> ()
                ]
                and walkExpr expr = [
                    match expr with
                    | SynExpr.Match(_, matchExpr, clauses, range, _) ->
                        for SynMatchClause(pat, _, clauseBody, clauseRange, _, _) in clauses do
                            match pat with
                            | SynPat.Wild _ ->
                                // Check the type of matchExpr to see if it's a closed DU.
                                let (line, col) = matchExpr.Range.Start.Line, matchExpr.Range.Start.Column
                                let typeInfo =
                                    checkFileResults.GetSymbolUseAtLocation(line, col, "", [])
                                match typeInfo with
                                | Some symbolUse ->
                                    match symbolUse.Symbol with
                                    | :? FSharpMemberOrFunctionOrValue as mfv
                                        when isClosedDu mfv.FullType ->
                                        yield message
                                            "SS005"
                                            Severity.warning
                                            clauseRange
                                            $"SS005: Wildcard pattern '| _ ->' on a closed discriminated union. \
                                              Enumerate all cases explicitly so new union arms cause a compile error. \
                                              SquatchStyle: assert positive AND negative space."
                                    | _ -> ()
                                | None -> ()
                            | _ ->
                                yield! walkExpr clauseBody
                    | SynExpr.LetOrUse(_, _, bindings, body, _, _) ->
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

[<Analyzer("SquatchStyle.WildcardPattern")>]
let wildcardPatternAnalyzer (ctx: AnalyzerContext) : Async<Message list> =
    async { return findSyntacticWildcards ctx }
```

---

### 4.6 `MissingDocumentationAnalyzer.fs`

**Rule SS006** — Public API without `///` XML doc violates "always say why/how."

```fsharp
module SquatchStyle.Analyzers.MissingDocumentationAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open SquatchStyle.Analyzers.Common

let private hasXmlDoc (doc: PreXmlDoc) =
    not doc.IsEmpty

let private checkTopLevelBinding (SynBinding(access, _, _, _, _, doc, _, headPat, _, _, range, _, _)) =
    // Only flag public bindings (no explicit private/internal access modifier).
    let isPublic =
        match access with
        | None -> true           // no modifier = public in F# module context
        | Some(SynAccess.Public _) -> true
        | _ -> false

    if isPublic && not (hasXmlDoc doc) then
        let name =
            match headPat with
            | SynPat.Named(_, ident, _, _, _)      -> ident.idText
            | SynPat.LongIdent(lid, _, _, _, _, _) ->
                lid.LongIdent |> List.map _.idText |> String.concat "."
            | _ -> "<function>"
        Some (message
            "SS006"
            Severity.hint
            range
            $"SS006: Public function '{name}' lacks XML documentation. \
              SquatchStyle: always say why; always say how. \
              Add /// summary, purpose, and rationale — not just what the code does.")
    else None

let private walkDecls decls =
    decls |> List.collect (function
        | SynModuleDecl.Let(_, bindings, _) ->
            bindings |> List.choose checkTopLevelBinding
        | SynModuleDecl.Types(typeDefs, _) ->
            typeDefs |> List.collect (fun (SynTypeDefn(SynComponentInfo(_, _, _, _, doc, _, _, range), repr, members, _, _, _)) ->
                [
                    // Check the type itself.
                    if not (hasXmlDoc doc) then
                        yield message
                            "SS006"
                            Severity.hint
                            range
                            "SS006: Public type lacks XML documentation. \
                             Describe its invariants, intended use, and any non-obvious constraints."
                    // Check member bindings.
                    yield!
                        members |> List.collect (function
                            | SynMemberDefn.Member(binding, _) ->
                                checkTopLevelBinding binding |> Option.toList
                            | _ -> [])
                ])
        | _ -> [])

[<Analyzer("SquatchStyle.MissingDocumentation")>]
let missingDocumentationAnalyzer (ctx: AnalyzerContext) : Async<Message list> =
    async {
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    walkDecls decls)
        | _ -> return []
    }
```

---

### 4.7 `RawExceptionAnalyzer.fs`

**Rule SS007** — `failwith`/`raise` in domain logic branches signals missing Result modelling.

```fsharp
module SquatchStyle.Analyzers.RawExceptionAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open SquatchStyle.Analyzers.Common

/// Exception identifiers that constitute "panic" — used for programmer errors only.
let private panicFns = Set [ "failwith"; "failwithf"; "invalidOp"; "invalidArg"; "raise" ]

let private suppressionMarker = "squatch:allow-exception"

let private isSuppressedByLine (sourceText: FSharp.Compiler.Text.ISourceText) (range: FSharp.Compiler.Text.Range) =
    let line = range.StartLine - 2
    if line < 0 then false
    else sourceText.GetLineString(line).Contains suppressionMarker

let private rec findRawExceptions (sourceText: FSharp.Compiler.Text.ISourceText) (expr: SynExpr) = [
    match expr with
    | SynExpr.App(_, _, SynExpr.Ident(ident), _, range)
        when panicFns.Contains ident.idText ->
        if not (isSuppressedByLine sourceText range) then
            yield message
                "SS007"
                Severity.warning
                range
                $"SS007: '{ident.idText}' found in expression context. \
                  Reserve exception-raising for genuine programmer errors (violated invariants). \
                  Domain failures should be modelled as Result<'T, 'E>. \
                  If this is an invariant assertion, add '// squatch:allow-exception <invariant being asserted>'."
    | SynExpr.Sequential(_, _, e1, e2, _, _) ->
        yield! findRawExceptions sourceText e1
        yield! findRawExceptions sourceText e2
    | SynExpr.LetOrUse(_, _, bindings, body, _, _) ->
        for SynBinding(_, _, _, _, _, _, _, _, _, bindBody, _, _, _) in bindings do
            yield! findRawExceptions sourceText bindBody
        yield! findRawExceptions sourceText body
    | SynExpr.Match(_, _, clauses, _, _) ->
        for SynMatchClause(_, _, body, _, _, _) in clauses do
            yield! findRawExceptions sourceText body
    | SynExpr.IfThenElse(cond, thenE, elseE, _, _, _, _) ->
        yield! findRawExceptions sourceText cond
        yield! findRawExceptions sourceText thenE
        yield! (elseE |> Option.toList |> List.collect (findRawExceptions sourceText))
    | _ -> ()
]

[<Analyzer("SquatchStyle.RawException")>]
let rawExceptionAnalyzer (ctx: AnalyzerContext) : Async<Message list> =
    async {
        let sourceText = ctx.SourceText
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    decls |> List.collect (function
                        | SynModuleDecl.Let(_, bindings, _) ->
                            bindings |> List.collect (fun (SynBinding(_, _, _, _, _, _, _, _, _, body, _, _, _)) ->
                                findRawExceptions sourceText body)
                        | _ -> []))
        | _ -> return []
    }
```

---

### 4.8 `MagicLiteralAnalyzer.fs`

**Rule SS008** — Numeric literals inline in expressions must be named constants.

```fsharp
module SquatchStyle.Analyzers.MagicLiteralAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open SquatchStyle.Analyzers.Common

/// Allow these common "obvious" literals that don't need naming.
let private allowedLiterals = Set [ 0; 1; -1; 2 ]

let private isMagic (n: int) = not (allowedLiterals.Contains n)

let private rec findMagicLiterals (expr: SynExpr) = [
    match expr with
    | SynExpr.Const(SynConst.Int32 n, range) when isMagic n ->
        yield message
            "SS008"
            Severity.warning
            range
            $"SS008: Magic literal '{n}' found inline. \
              SquatchStyle: put a limit on everything — explicitly, with a name. \
              Declare '[<Literal>] let MaxItems = {n}' and reference it by name. \
              This documents intent and makes invariants assertable."
    | SynExpr.Const(SynConst.Double d, range) when d <> 0.0 && d <> 1.0 ->
        yield message
            "SS008"
            Severity.warning
            range
            $"SS008: Magic float literal '{d}' found inline. Use a named [<Literal>] constant."
    | SynExpr.Sequential(_, _, e1, e2, _, _) ->
        yield! findMagicLiterals e1
        yield! findMagicLiterals e2
    | SynExpr.LetOrUse(_, _, bindings, body, _, _) ->
        for SynBinding(_, _, _, _, _, _, _, _, _, bindBody, _, _, _) in bindings do
            yield! findMagicLiterals bindBody
        yield! findMagicLiterals body
    | SynExpr.App(_, _, fn, arg, _) ->
        yield! findMagicLiterals fn
        yield! findMagicLiterals arg
    | SynExpr.Tuple(_, exprs, _, _) ->
        yield! exprs |> List.collect findMagicLiterals
    | _ -> ()
]

[<Analyzer("SquatchStyle.MagicLiteral")>]
let magicLiteralAnalyzer (ctx: AnalyzerContext) : Async<Message list> =
    async {
        match ctx.ParseFileResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(_, _, _, _, _, modules, _, _, _)) ->
            return
                modules
                |> List.collect (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _, _)) ->
                    decls |> List.collect (function
                        | SynModuleDecl.Let(_, bindings, _) ->
                            bindings |> List.collect (fun (SynBinding(_, _, _, _, _, _, _, _, _, body, _, _, _)) ->
                                findMagicLiterals body)
                        | _ -> []))
        | _ -> return []
    }
```

---

## 5. Fantomas Configuration

### `.fantomasrc`

```json
{
  "$schema": "https://raw.githubusercontent.com/fsprojects/fantomas/main/src/Fantomas.Core/schema.json",
  "MaxLineLength": 100,
  "IndentSize": 2,
  "EndOfLine": "lf",
  "InsertFinalNewline": true,
  "SpaceBeforeColon": false,
  "SpaceAfterComma": true,
  "SpaceAroundDelimiter": true,
  "MultiLineLambdaClosingNewline": true,
  "KeepIndentInBranch": true,
  "MaxIfThenShortWidth": 40,
  "MaxIfThenElseShortWidth": 60,
  "MaxInfixOperatorExpression": 80,
  "MaxRecordWidth": 60,
  "MaxRecordNumberOfItems": 4,
  "RecordMultilineFormatter": "NumberOfItems",
  "MaxArrayOrListWidth": 60,
  "MaxArrayOrListNumberOfItems": 4,
  "ArrayOrListMultilineFormatter": "NumberOfItems",
  "MaxValueBindingWidth": 80,
  "MaxFunctionBindingWidth": 40,
  "MaxDotGetExpressionWidth": 80,
  "MultilineBlockBracketsOnSameColumn": false,
  "NewlineBetweenTypeDefinitionAndMembers": true,
  "AlignFunctionSignaturesToIndentation": true,
  "AlternativeLongMemberDefinitions": true,
  "DisableElmishSyntax": false,
  "StrictMode": false
}
```

### `.editorconfig`

```ini
root = true

[*]
end_of_line = lf
insert_final_newline = true
charset = utf-8
trim_trailing_whitespace = true

[*.{fs,fsx,fsi}]
indent_style = space
indent_size = 2
max_line_length = 100

# Fantomas delegates to .fantomasrc but these signal intent to IDEs.
fsharp_indent_on_try_with = true
fsharp_indent_size = 2
fsharp_max_line_length = 100

[*.{csproj,fsproj,props,targets}]
indent_style = space
indent_size = 2

[*.{json,yml,yaml}]
indent_style = space
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

---

## 6. FSI Build Script

### `build.fsx`

```fsharp
#!/usr/bin/env -S dotnet fsi

// SquatchStyle F# — FSI build script.
// Usage: dotnet fsi build.fsx [target]
// Targets: restore | build | test | analyze | pack | format | check-format | all

#r "nuget: Fake.Core.Process, 6.1.3"
#r "nuget: Fake.DotNet.Cli, 6.1.3"
#r "nuget: Fake.IO.FileSystem, 6.1.3"
#r "nuget: Fake.Core.Target, 6.1.3"
#r "nuget: Fake.Core.Environment, 6.1.3"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

// ── Bootstrap FAKE context (required when running as script) ────────────────
Environment.GetCommandLineArgs()
|> Array.skip 2  // skip "dotnet" and "fsi"
|> Array.toList
|> Context.FakeExecutionContext.Create false "build.fsx"
|> Context.RuntimeContext.Fake
|> Context.setExecutionContext

// ── Configuration ────────────────────────────────────────────────────────────

[<Literal>]
let SolutionFile = "SquatchStyle.sln"

[<Literal>]
let AnalyzerProject = "src/SquatchStyle.Analyzers/SquatchStyle.Analyzers.fsproj"

[<Literal>]
let TestProject = "tests/SquatchStyle.Analyzers.Tests/SquatchStyle.Analyzers.Tests.fsproj"

[<Literal>]
let OutputDir = "artifacts"

let configuration =
    Environment.environVarOrDefault "CONFIGURATION" "Release"

let dotnet cmd args =
    let result = DotNet.exec id cmd args
    if not result.OK then
        failwithf "dotnet %s %s failed with %i errors:\n%s"
            cmd args result.ExitCode (result.Errors |> String.concat "\n")

// ── Targets ──────────────────────────────────────────────────────────────────

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [
        OutputDir
        "src/SquatchStyle.Analyzers/bin"
        "src/SquatchStyle.Analyzers/obj"
        "tests/SquatchStyle.Analyzers.Tests/bin"
        "tests/SquatchStyle.Analyzers.Tests/obj"
    ]
)

Target.create "Restore" (fun _ ->
    dotnet "restore" SolutionFile
)

Target.create "Build" (fun _ ->
    dotnet "build" $"{SolutionFile} -c {configuration} --no-restore"
)

Target.create "Test" (fun _ ->
    dotnet "test"
        $"{TestProject} -c {configuration} --no-build \
          --logger:\"console;verbosity=normal\" \
          --results-directory {OutputDir}/test-results"
)

Target.create "Analyze" (fun _ ->
    // Run analyzers against a sample fixture using the SDK's CLI runner.
    // In a real project, analyzers run automatically via MSBuild reference.
    // This target is useful for smoke-testing the analyzer DLL itself.
    let analyzerDll =
        $"src/SquatchStyle.Analyzers/bin/{configuration}/net8.0/SquatchStyle.Analyzers.dll"
    let fixtureDir = "tests/SquatchStyle.Analyzers.Tests"
    dotnet "fsharp-analyzer"
        $"--analyzers-path {analyzerDll} \
          --project {TestProject} \
          --code {fixtureDir}"
)

Target.create "Format" (fun _ ->
    // Format all F# source files in-place.
    dotnet "fantomas" "src tests --recurse"
)

Target.create "CheckFormat" (fun _ ->
    // Fail if any file is not formatted. Use in CI.
    dotnet "fantomas" "src tests --recurse --check"
)

Target.create "Pack" (fun _ ->
    dotnet "pack"
        $"{AnalyzerProject} -c {configuration} --no-build \
          -o {OutputDir}/packages"
)

Target.create "All" ignore

// ── Dependency graph ─────────────────────────────────────────────────────────

"Clean"
    ==> "Restore"
    ==> "Build"
    ==> "Test"
    ==> "Pack"
    ==> "All"

"Build" ==> "CheckFormat"
"Build" ==> "Analyze"

// ── Entry point ───────────────────────────────────────────────────────────────

let targetFromArgs () =
    match Environment.GetCommandLineArgs() |> Array.tryLast with
    | Some t when not (t.EndsWith ".fsx") -> t
    | _ -> "All"

Target.runOrDefault (targetFromArgs ())
```

### `build/build.fsproj` (tool restore target)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <!-- Fantomas as a local tool is managed via dotnet-tools.json -->
</Project>
```

### `.config/dotnet-tools.json`

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "fantomas": {
      "version": "6.3.14",
      "commands": ["fantomas"]
    },
    "fsharp-analyzers": {
      "version": "0.27.0",
      "commands": ["fsharp-analyzers"]
    }
  }
}
```

Run `dotnet tool restore` once after cloning to materialize these.

---

## 7. Consuming the Analyzers in a Target Project

### Via NuGet package (recommended for team use)

```xml
<!-- In your project's .fsproj -->
<ItemGroup>
  <PackageReference Include="SquatchStyle.Analyzers" Version="1.0.0">
    <!-- DevelopmentDependency means it's compile-time only, not a runtime dep -->
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### Severity escalation for CI

```xml
<!-- In your project or Directory.Build.props -->
<PropertyGroup Condition="'$(CI)' == 'true'">
  <!-- Promote analyzer warnings to errors in CI builds -->
  <WarningsAsErrors>SS001;SS002;SS003;SS004;SS005</WarningsAsErrors>
  <!-- SS006 (docs) and SS007/SS008 remain warnings locally and in CI -->
</PropertyGroup>
```

### Rule suppressions (use sparingly, require justification comment)

```fsharp
// squatch:allow-mutable Accumulator for performance-critical tight loop in hot path.
let mutable acc = 0

// squatch:allow-exception Invariant: count must never be negative after Decrement.
if count < 0 then failwith "Count invariant violated"
```

---

## 8. Implementation Sequence

Build in this order — each stage produces something usable before the next begins.

```
Phase 0 — Scaffold (1 day)
  └── Solution layout, fsproj files, dotnet-tools.json
  └── build.fsx: Clean + Restore + Build targets only
  └── .editorconfig + .fantomasrc committed and verified via CheckFormat

Phase 1 — Core Safety Rules (3 days)
  └── Common.fs: message builder, shared helpers
  └── SS002 FunctionLength — parse tree only, easiest to implement and test
  └── SS003 MutableBinding — parse tree + suppression mechanism
  └── SS004 UnguardedRecursion — parse tree
  └── Test harness: Helpers.fs + first test file per rule

Phase 2 — Type-Aware Rules (3–4 days)
  └── SS001 SilentErrorDiscard — requires typed tree (hardest rule)
  └── SS005 WildcardPattern — hybrid syntactic + typed
  └── Integration tests against real-world F# code (not just fixtures)

Phase 3 — Developer Experience Rules (1–2 days)
  └── SS006 MissingDocumentation — parse tree
  └── SS007 RawException — parse tree + suppression
  └── SS008 MagicLiteral — parse tree

Phase 4 — Tooling Closure (1 day)
  └── build.fsx: Test + Analyze + Pack + All targets
  └── SKILL.md finalized and validated against agent workflow
  └── RULES.md (human-readable rule reference)
  └── README with quick-start instructions
```

---

## 9. Rule Reference Summary

| Code  | Name                  | Severity (local) | Severity (CI) | Suppressible | Tree    |
|-------|-----------------------|------------------|---------------|--------------|---------|
| SS001 | SilentErrorDiscard    | Error            | Error         | No           | Typed   |
| SS002 | FunctionLength        | Warn@55 Err@70   | Error         | No           | Parse   |
| SS003 | MutableBinding        | Warning          | Error         | Yes          | Parse   |
| SS004 | UnguardedRecursion    | Error            | Error         | No           | Parse   |
| SS005 | WildcardPattern       | Warning          | Error         | No           | Both    |
| SS006 | MissingDocumentation  | Hint             | Warning       | No           | Parse   |
| SS007 | RawException          | Warning          | Warning       | Yes          | Parse   |
| SS008 | MagicLiteral          | Warning          | Warning       | No           | Parse   |
