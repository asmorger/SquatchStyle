# SquatchStyle

> Squatch Style — Tiger Style, but for F#

SquatchStyle is an F# analyzer library that enforces [TigerStyle](https://github.com/tigerbeetle/tigerbeetle/blob/main/docs/TIGER_STYLE.md) coding discipline — a safety-first philosophy originally written for Zig and TigerBeetle's financial database. SquatchStyle adapts these principles to idiomatic F#.

**Priority order:** Safety → Performance → Developer Experience

## Rules

| Code | Name | Severity | Suppressible |
|------|------|----------|--------------|
| SS001 | SilentErrorDiscard | Error | No |
| SS002 | FunctionLength | Warning ≥55 / Error ≥70 | No |
| SS003 | MutableBinding | Warning | Yes |
| SS004 | UnguardedRecursion | Error | No |
| SS005 | WildcardPattern | Warning | No |
| SS006 | MissingDocumentation | Hint | No |
| SS007 | RawException | Warning | Yes |
| SS008 | MagicLiteral | Warning | No |

See [docs/RULES.md](docs/RULES.md) for full descriptions, examples, and fixes.

## Installation

Add the analyzer to your `.fsproj`:

```xml
<ItemGroup>
  <PackageReference Include="SquatchStyle.Analyzers" Version="*">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

You will need to configure the analyzers to run as needed on your project.  Since these aren't native to the F# community (unlike Roslyn analyzers), you'll need some setup in your project.  [Instructions can be found here](https://ionide.io/FSharp.Analyzers.SDK/)

## CI Escalation

Promote warnings to errors in CI:

```xml
<PropertyGroup Condition="'$(CI)' == 'true'">
  <WarningsAsErrors>SS003;SS005;SS006;SS007;SS008</WarningsAsErrors>
</PropertyGroup>
```

## Suppressions

Two rules support justified suppressions via inline comments. The justification is required — a bare marker won't compile.

```fsharp
// squatch:allow-mutable Performance-critical accumulator in hot path.
let mutable acc = 0

// squatch:allow-exception Invariant: count is always non-negative after Decrement.
if count < 0 then failwith "Invariant violated"
```

SS001, SS002, and SS004 cannot be suppressed — they represent hard safety constraints.

## Build

```bash
dotnet fsi build.fsx build    # compile
dotnet fsi build.fsx test     # run tests
dotnet fsi build.fsx pack     # produce NuGet package
dotnet fsi build.fsx all      # clean → restore → build → test → pack
```

## License

[MIT](LICENSE) — Copyright 2026 Andrew Morger
