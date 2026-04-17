---
name: squatch-style
description: >
  Apply SquatchStyle engineering discipline to F# codebases. Use when writing,
  reviewing, or refactoring F# code that targets SquatchStyle compliance: safety-first
  control flow, explicit error handling via Result/Railway-Oriented Programming,
  immutability, bounded functions, named constants, and full documentation.
  Also governs Analyzer project contributions and build script interactions.
---

# SquatchStyle F# — Agent Skill

You are operating on an F# codebase that enforces SquatchStyle: a safety-first,
zero-technical-debt engineering philosophy. This document is your complete
reference. Follow it without compromise.

---

## Core Hierarchy

When any two concerns conflict, resolve using this priority order:

1. **Safety** — correctness, invariant preservation, no silent failures
2. **Performance** — mechanical sympathy, explicit resource management
3. **Developer Experience** — clarity, naming, documentation

---

## Toolchain Awareness

### Build

```bash
dotnet fsi build.fsx [target]
```

Available targets: `Clean | Restore | Build | Test | Analyze | Format | CheckFormat | Pack | All`

- Run `Restore` before any other target in a fresh clone.
- Run `CheckFormat` before committing. Never commit unformatted code.
- `Format` rewrites files in-place via Fantomas. Do not edit formatted output manually.
- `Analyze` runs the SquatchStyle Analyzer suite against the codebase.
- Default target when none is supplied: `All`.

### Formatting (Fantomas)

Configuration lives in `.fantomasrc` (JSON) and `.editorconfig`.

Key rules:
- **100 columns** maximum line length. Hard limit, no exceptions.
- **4 spaces** indentation. Never tabs.
- Fantomas owns all whitespace decisions. Do not fight the formatter.
- If generated code doesn't format cleanly, the structure is wrong — restructure, don't suppress.

To format a single file: `dotnet fantomas path/to/File.fs`
To check without writing: `dotnet fantomas path/to/File.fs --check`

### Analyzers

The `SquatchStyle.Analyzers` package runs automatically via MSBuild when referenced.
Each analyzer emits structured diagnostics:

| Code  | Rule                  | Default Severity | CI Severity |
|-------|-----------------------|------------------|-------------|
| TS001 | SilentErrorDiscard    | Error            | Error       |
| TS002 | FunctionLength        | Warn/Error       | Error       |
| TS003 | MutableBinding        | Warning          | Error       |
| TS004 | UnguardedRecursion    | Error            | Error       |
| TS005 | WildcardPattern       | Warning          | Error       |
| TS006 | MissingDocumentation  | Hint             | Warning     |
| TS007 | RawException          | Warning          | Warning     |
| TS008 | MagicLiteral          | Warning          | Warning     |

**Do not suppress analyzer diagnostics without a documented justification.**
Suppressible rules (TS003, TS007) accept a structured comment:

```fsharp
// tiger:allow-mutable <mandatory reason explaining why mutation is necessary>
let mutable counter = 0

// tiger:allow-exception Invariant: index must be within bounds after clamp; crash is correct.
if index < 0 then failwith "Index invariant violated"
```

---

## Code Rules — Apply These to Every File You Write or Modify

### TS001 — Never Discard Result or Option

Every `Result<'T,'E>` and `Option<'T>` return value **must** be handled.
"Handled" means: bound with `let`, piped through a combinator, or matched exhaustively.

```fsharp
// ✗ WRONG — discarded Result
riskyOperation ()

// ✗ WRONG — ignore without justification
riskyOperation () |> ignore

// ✓ CORRECT — bound and matched
let result = riskyOperation ()
match result with
| Ok value  -> useValue value
| Error err -> handleError err

// ✓ CORRECT — Railway-Oriented Pipeline
input
|> validate
|> Result.bind transform
|> Result.map persist
|> Result.mapError logError
```

Use `Result.map`, `Result.bind`, `Result.mapError`, and computation expressions
(`result { }` from FsToolkit.ErrorHandling or equivalent) to compose.

### TS002 — 70-Line Function Maximum

No function body may exceed 70 lines. The analyzer warns at 55.

When decomposing:
- Push `if`/`match` control flow **up** to the parent function.
- Push iteration (`List.map`, `Seq.fold`) logic **down** to helper functions.
- Keep leaf functions **pure**: no side effects, no state mutation.
- Parent function owns all control flow; helpers own all computation.

```fsharp
// ✗ WRONG — control flow mixed with computation in one long function
let processOrder order =
    if order.IsValid then
        let items = order.Items |> List.filter (fun i -> i.InStock)
        let total = items |> List.sumBy (fun i -> i.Price * float i.Quantity)
        // ... 60 more lines ...
    else
        Error "Invalid order"

// ✓ CORRECT — control flow in parent, computation in pure helpers
let private filterInStock items =
    items |> List.filter (fun i -> i.InStock)

let private calculateTotal items =
    items |> List.sumBy (fun i -> i.Price * float i.Quantity)

let processOrder order =
    if not order.IsValid then Error "Invalid order"
    else
        order.Items
        |> filterInStock
        |> calculateTotal
        |> Ok
```

### TS003 — Immutability First

Never use `let mutable` unless:
1. It is at an I/O boundary (adapter layer), OR
2. It is in a performance-critical tight loop with a measured justification.

Prefer `let` + functional transforms. If you feel the need for mutation,
first ask: can I use a fold? An accumulator parameter? A `seq` expression?

If mutation is genuinely necessary, add the suppression comment with a reason:

```fsharp
// tiger:allow-mutable Hot-path byte accumulator; fold allocates in tight loop.
let mutable byteCount = 0
```

### TS004 — Tail-Call Safety for Recursion

All `let rec` functions **must** carry `[<TailCall>]`. This attribute (F# 8+) causes
a compiler error if the recursion is not actually in tail position, turning a latent
stack-overflow bug into a compile error.

```fsharp
// ✗ WRONG — unbounded stack depth
let rec sumList acc = function
    | []      -> acc
    | x :: xs -> sumList (acc + x) xs   // this IS tail-call but compiler won't verify

// ✓ CORRECT — compiler-verified
[<TailCall>]
let rec sumList acc = function
    | []      -> acc
    | x :: xs -> sumList (acc + x) xs

// ✓ PREFERRED — no recursion needed at all
let sumList = List.fold (+) 0
```

When you reach for `let rec`, first ask: is there a `List.*`, `Seq.*`, or `Array.*`
combinator that expresses this without recursion? If yes, use it.

### TS005 — Exhaustive Pattern Matching

Never use `| _ ->` on a closed discriminated union. Enumerate all cases.
When a new union case is added later, the compiler will force you to handle it everywhere.

```fsharp
type OrderStatus = Pending | Processing | Fulfilled | Cancelled

// ✗ WRONG — silently ignores future cases
let describeStatus = function
    | Pending    -> "waiting"
    | Fulfilled  -> "done"
    | _ -> "other"           // ← masks Cancelled and any future cases

// ✓ CORRECT — exhaustive; compiler enforces new cases
let describeStatus = function
    | Pending     -> "waiting"
    | Processing  -> "in progress"
    | Fulfilled   -> "done"
    | Cancelled   -> "cancelled"
```

### TS006 — Document Everything Public

Every public `let` binding, type, and member must have `///` XML documentation.

Documentation must answer:
- **What**: one-sentence summary.
- **Why**: the purpose or invariant this code upholds.
- **How** (for non-obvious logic): the approach or algorithm.

```fsharp
// ✗ WRONG — undocumented
let calculateFee amount rate = amount * rate

// ✓ CORRECT
/// Calculates the transaction fee for a given amount and rate.
/// Rate must be expressed as a decimal fraction (e.g., 0.025 for 2.5%).
/// Returns the fee in the same currency unit as amount.
/// Precondition: amount >= 0 and 0 < rate <= 1.
let calculateFee (amount: decimal) (rate: decimal) : decimal =
    assert (amount >= 0m)
    assert (rate > 0m && rate <= 1m)
    amount * rate
```

### TS007 — Exceptions Are for Programmer Errors Only

`failwith`, `failwithf`, `invalidOp`, `invalidArg`, and `raise` are reserved for
**violated invariants** — conditions that represent bugs in the program, not domain errors.

Domain errors (user input invalid, resource not found, network unavailable) must be
modelled as `Result<'T, 'E>`.

```fsharp
// ✗ WRONG — using exception for a domain error
let findUser id users =
    match users |> List.tryFind (fun u -> u.Id = id) with
    | Some u -> u
    | None   -> failwith $"User {id} not found"   // ← caller cannot recover

// ✓ CORRECT — domain error as Result
let findUser id users : Result<User, string> =
    match users |> List.tryFind (fun u -> u.Id = id) with
    | Some u -> Ok u
    | None   -> Error $"User {id} not found"

// ✓ CORRECT — exception for a genuine invariant
let divide numerator denominator =
    // tiger:allow-exception Invariant: denominator validated non-zero at call boundary.
    if denominator = 0 then failwith "Denominator must not be zero — caller contract violated"
    numerator / denominator
```

### TS008 — No Magic Literals

Every numeric constant that has semantic meaning must be a named `[<Literal>]`.
Allowed inline: `0`, `1`, `-1`, `2`.

```fsharp
// ✗ WRONG
let isOverLimit count = count > 1000
let chunk items = items |> List.chunkBySize 64

// ✓ CORRECT
[<Literal>]
let MaxItemsPerAccount = 1000

[<Literal>]
let BatchSize = 64

let isOverLimit count = count > MaxItemsPerAccount
let chunk items = items |> List.chunkBySize BatchSize
```

---

## Naming Conventions

Follow F# community conventions with SquatchStyle additions:

- `camelCase` for values, parameters, and local bindings.
- `PascalCase` for types, modules, and DU cases.
- **Units last, qualifiers last**, sorted by descending significance:
  `latencyMsMax` not `maxLatencyMs`.
- **Prefer Units of Measure** over naming conventions for physical quantities:
  `float<ms>` beats `latencyMs`.
- Helper functions called from a single parent: prefix with parent name.
  `processOrder` + `processOrderValidate` + `processOrderPersist`.
- Callbacks last in parameter lists.
- Avoid abbreviations. `index`, `length`, `count` — not `idx`, `len`, `cnt`.
- `index + 1 = count`. Track this relationship in names to prevent off-by-one errors.

---

## Assertion Discipline

Assert pre- and postconditions. Minimum two assertions per non-trivial function.
Use `assert` for compiler-optimizable checks (debug builds).
Use `System.Environment.FailFast` for invariants that must crash even in release.

Assert **positive space** (what you expect) AND **negative space** (what you forbid):

```fsharp
let transfer (amount: decimal) (from: Account) (to: Account) =
    // Positive space: amount is valid.
    assert (amount > 0m)
    assert (amount <= from.Balance)
    // Negative space: accounts are not the same.
    assert (from.Id <> to.Id)

    let result = executeTransfer amount from to

    // Postcondition: conservation of funds.
    assert (from.Balance + to.Balance = originalTotal)
    result
```

Split compound assertions:

```fsharp
// ✗ WRONG — compound assertion hides which condition failed
assert (a > 0 && b < limit && c <> 0)

// ✓ CORRECT — each failure is independently diagnosable
assert (a > 0)
assert (b < limit)
assert (c <> 0)
```

---

## Analyzer Project Conventions

When writing or modifying Analyzer code:

1. **All analyzers are `Async<Message list>`** — never block the thread.
2. **Parse-tree rules** are preferred over typed-tree rules when possible — faster, no type-check dependency.
3. **Typed-tree rules** (TS001, TS005) must handle `ctx.TypedTree = None` gracefully — return `[]`.
4. **Message codes are stable** — never renumber an existing code. Deprecate with a suffix instead.
5. **Suppression comments** must be on the line immediately above the flagged expression. No exceptions.
6. **Fixes** (`Fix list` on Message): only provide mechanical fixes when the transformation is guaranteed safe. When in doubt, leave `Fixes = []` and describe the fix in the message text.
7. Every analyzer must have a corresponding test file with at minimum:
   - One test that fires the rule (positive case).
   - One test that does NOT fire with compliant code (negative case).
   - One test for the suppression mechanism if the rule is suppressible.

---

## What Good F# Code Looks Like Under SquatchStyle

```fsharp
/// Processes an incoming payment, applying fraud checks and persisting to ledger.
/// Returns Ok with the transaction ID on success, or Error with a structured reason.
/// Preconditions: payment amount > 0, account exists and is active.
let processPayment
    (account: Account)
    (payment: Payment)
    (clock: IClock)
    : Async<Result<TransactionId, PaymentError>> =
    async {
        assert (payment.Amount > 0m)
        assert (account.IsActive)

        let! fraudResult = FraudService.check account payment
        return!
            fraudResult
            |> Result.bind (Ledger.validate payment)
            |> Result.mapAsync (Ledger.persist clock)
            |> Async.map (Result.mapError PaymentError.fromLedgerError)
    }
```

This demonstrates: documented preconditions, assertions, Result railway, no mutable state,
no magic literals, no discarded results, pure helpers, and a function well under 70 lines.

---

## Checklist Before Committing

Run this against every file you touch:

- [ ] `dotnet fantomas <file> --check` — passes without changes.
- [ ] `dotnet fsi build.fsx Build` — no build errors.
- [ ] `dotnet fsi build.fsx Test` — all tests pass.
- [ ] `dotnet fsi build.fsx Analyze` — zero TS001–TS005 violations.
- [ ] No `let mutable` without `// tiger:allow-mutable <reason>`.
- [ ] No `| _ ->` on closed DUs.
- [ ] No `failwith`/`raise` in domain logic without `// tiger:allow-exception <invariant>`.
- [ ] All public bindings and types have `///` documentation.
- [ ] No numeric literals (other than 0, 1, -1, 2) inline in expressions.
- [ ] All `let rec` carry `[<TailCall>]`.
- [ ] Every `Result` and `Option` return value is bound and handled.
