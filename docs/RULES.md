# SquatchStyle Rules

SquatchStyle is an F# analyzer library inspired by TigerStyle (TigerBeetle's coding standards).
Rules are identified by codes `SS001`–`SS008`.

## Quick Reference

| Code  | Name                  | Default Severity        | Suppressible | Analysis    |
|-------|-----------------------|-------------------------|--------------|-------------|
| SS001 | SilentErrorDiscard    | Error                   | No           | Typed tree  |
| SS002 | FunctionLength        | Warning ≥55 / Error ≥70 | No           | Parse tree  |
| SS003 | MutableBinding        | Warning                 | Yes          | Parse tree  |
| SS004 | UnguardedRecursion    | Error                   | No           | Parse tree  |
| SS005 | WildcardPattern       | Warning                 | No           | Both        |
| SS006 | MissingDocumentation  | Hint                    | No           | Both        |
| SS007 | RawException          | Warning                 | Yes          | Parse tree  |
| SS008 | MagicLiteral          | Warning                 | No           | Parse tree  |

---

## SS001 — SilentErrorDiscard

**Severity:** Error

**Principle:** All errors must be handled. You cannot silently drop a `Result` or `Option`.

**What it flags:** A `Result<_, _>`, `Option<_>`, or `ValueOption<_>` return value used as a
statement (not bound to anything, not piped, not `ignore`d with a justification).

**Fix:** Bind the value with `let`, pipe through `Result.map`/`Result.bind`, or explicitly call
`ignore` with a comment explaining why the error is intentionally discarded.

```fsharp
// Bad — SS001
riskyOp ()

// Good — bind and handle
match riskyOp () with
| Ok v    -> doSomethingWith v
| Error e -> log.Error e

// Good — explicit discard with justification
ignore (riskyOp ())  // fire-and-forget: failure logged internally by riskyOp
```

---

## SS002 — FunctionLength

**Severity:** Warning at ≥55 lines, Error at ≥70 lines

**Principle:** A function that doesn't fit on one screen cannot be fully understood. Long functions
hide complexity and impede review.

**What it flags:** Any `let` binding whose body spans 55 or more lines.

**Fix:** Extract sub-functions. Each extracted piece should have a single, nameable responsibility.

---

## SS003 — MutableBinding

**Severity:** Warning

**Principle:** Mutation is a source of non-local reasoning. Prefer immutable values.

**What it flags:** Any `let mutable` binding.

**Suppression:** Add a comment `// squatch:allow-mutable <justification>` on the line immediately
before the binding. The justification should explain why mutation is necessary here.

```fsharp
// squatch:allow-mutable Accumulator in tight hot-path loop; profiled as 40% faster.
let mutable acc = 0
```

---

## SS004 — UnguardedRecursion

**Severity:** Error

**Principle:** Unbounded recursion causes stack overflows. Every recursive function must be
explicitly marked tail-recursive.

**What it flags:** A `let rec` function that lacks the `[<TailCall>]` attribute.

**Fix:** Annotate with `[<TailCall>]`. The F# compiler will then verify that all recursive calls
are in tail position.

```fsharp
// Bad — SS004
let rec sum acc = function
    | [] -> acc
    | x :: xs -> sum (acc + x) xs

// Good
[<TailCall>]
let rec sum acc = function
    | [] -> acc
    | x :: xs -> sum (acc + x) xs
```

---

## SS005 — WildcardPattern

**Severity:** Warning

**Principle:** A wildcard `| _ ->` on a closed discriminated union silently ignores new union cases
added in the future. Assert the complete positive AND negative space.

**What it flags:** A `match` expression where one arm is `| _ ->` and the matched expression's
type is a closed (non-abstract) F# discriminated union.

**Fix:** Enumerate all cases explicitly so that adding a new union case causes a compile error.

```fsharp
type Color = Red | Green | Blue

// Bad — SS005
let describe = function
    | Red -> "red"
    | _   -> "other"

// Good
let describe = function
    | Red   -> "red"
    | Green -> "green"
    | Blue  -> "blue"
```

---

## SS006 — MissingDocumentation

**Severity:** Hint

**Principle:** Code without documentation is incomplete. Always say why; always say how.

**What it flags:** Public `let` functions and `type` declarations that lack a `///` XML doc
comment. This rule is a hint — it never blocks a build.

**Fix:** Add an XML doc comment explaining the purpose, parameters, return value, and any
non-obvious invariants or constraints.

```fsharp
// Bad — SS006
let computeChecksum data = ...

// Good
/// Computes a CRC-32 checksum over the given byte span.
/// Returns a non-negative 32-bit integer.
/// Invariant: identical inputs always produce the same output.
let computeChecksum data = ...
```

---

## SS007 — RawException

**Severity:** Warning

**Principle:** Domain failures should be modelled as `Result<'T, 'E>`, not exceptions. Exceptions
are for genuine programmer errors (violated invariants), not anticipated failure modes.

**What it flags:** Direct calls to `failwith`, `failwithf`, `invalidOp`, `invalidArg`, or `raise`
inside function bodies.

**Suppression:** Add `// squatch:allow-exception <invariant being asserted>` on the line
immediately before the call to document the invariant you are enforcing.

```fsharp
// Bad — SS007
let divide a b =
    if b = 0 then failwith "division by zero"
    a / b

// Good — model the failure
let divide a b =
    if b = 0 then Error "division by zero"
    else Ok (a / b)

// Good — invariant assertion with suppression
let invariantCheck (count: int) =
    // squatch:allow-exception Invariant: count is always ≥0 after Decrement.
    if count < 0 then failwith "count invariant violated"
```

---

## SS008 — MagicLiteral

**Severity:** Warning

**Principle:** Every limit and threshold must be a named constant. Magic numbers inline in code
make invariants invisible and non-assertable.

**What it flags:** Integer literals other than `0`, `1`, `-1`, `2`, and float literals other than
`0.0` and `1.0` appearing directly in expressions.

**Fix:** Extract to a named `[<Literal>]` constant. This makes the intent explicit and the value
findable by tools.

```fsharp
// Bad — SS008
let truncate xs = List.take 100 xs

// Good
[<Literal>]
let MaxBatchSize = 100

let truncate xs = List.take MaxBatchSize xs
```

---

## CI Escalation

In CI builds, promote analyzer warnings to errors:

```xml
<PropertyGroup Condition="'$(CI)' == 'true'">
  <WarningsAsErrors>SS001;SS002;SS003;SS004;SS005</WarningsAsErrors>
</PropertyGroup>
```

SS006, SS007, and SS008 may remain as warnings in CI if the team prefers a softer rollout.
