# Implementation Plan

This plan follows **TDD**. Every stage has two steps, always in this order:

1. **Tests**: write the xUnit tests first. They must compile and **fail** (red).
2. **Implementation**: write the minimum code that makes them pass (green), then tidy up (refactor) while keeping the tests green.

A stage is finished only when `dotnet test` passes and no earlier test is broken.

The application is a .NET 10 / C# 14 solution (see [tech-stack.md](tech-stack.md)): `Calc.Lib` (class library), `Calc.Tui` (console app) and `Calc.Lib.Tests` and `Calc.Tui.Tests` (xUnit).

## Pipeline

`Calc.Lib` turns a string into a result in three separate phases:

```
"6*2"  --Tokenize-->  [Number(6), Op(*), Number(2)]  --Parse-->  Expr tree  --Evaluate-->  12
```

Expression tree for `6*2`:

```
*
|- 6
|- 2
```

The expression is parsed **once** into a tree, then evaluated from the tree. This keeps the design ready for variables later: the same tree can be evaluated many times with different values without re-parsing.

Infix notation is used, with standard operator priority (see the table in Stage 4).

## Planned files in Calc.Lib

| File | Responsibility |
|------|----------------|
| `CalcException.cs` | `CalcException` and the `CalcErrorKind` enum shared by every phase |
| `Token.cs` | `Token` and `Operator` types, `Tokenizer.Tokenize(string) -> IReadOnlyList<Token>` |
| `Expr.cs` | `Expr` tree types |
| `Parser.cs` | `Parser.Parse(IReadOnlyList<Token>) -> Expr` |
| `Evaluator.cs` | `Evaluator.Evaluate(Expr) -> double` |
| `Calculator.cs` | The public `Calculator.Evaluate(string) -> double` |

Phase classes are `internal` and exposed to `Calc.Lib.Tests` with `InternalsVisibleTo`; only `Calculator` and `CalcException` are public. Unit tests mirror the files: `TokenizerTests.cs`, `ParserTests.cs`, `EvaluatorTests.cs`, `CalcExceptionTests.cs`. End-to-end tests for `Calculator.Evaluate` live in `CalculatorTests.cs`.

Types sketch (to be refined during the stages):

```csharp
enum Operator { Add, Sub, Mul, Div, Pow }

abstract record Token;
sealed record NumberToken(double Value) : Token;
sealed record OperatorToken(Operator Op) : Token;

abstract record Expr;
sealed record NumberExpr(double Value) : Expr;
sealed record BinaryExpr(Operator Op, Expr Left, Expr Right) : Expr;
```

Records give value equality for free, so tests can compare tokens and trees with `Assert.Equal`.

---

## Part 1: Calc.Lib basic operators

Scope: numbers and the operators `+ - * / ^`, plus parentheses. Other operators (`%`, unary minus), functions and constants come in Part 3.

### Stage 1: Error type

1. **Tests**
   - `CalcException` exposes a `Kind` (`CalcErrorKind`) and a readable `Message` for each kind.
   - Errors that carry data (`UnexpectedCharacter`, `InvalidNumber`) include it in the message.
2. **Implementation**
   - Create `CalcException.cs` with `CalcException : Exception` and the `CalcErrorKind` enum. Initial kinds: `UnexpectedCharacter`, `InvalidNumber`, `UnexpectedToken`, `UnexpectedEnd`, `UnbalancedParentheses`, `DivisionByZero`.
   - New kinds can be added in later stages when a test needs them.

### Stage 2: Tokenizer, numbers and operators

1. **Tests**
   - `"6*2"` -> `[NumberToken(6.0), OperatorToken(Mul), NumberToken(2.0)]`
   - Each operator is recognised: `+ - * / ^` (use `[Theory]` with `[InlineData]`)
   - Multi-digit numbers (`"123"`) and decimals (`"1.5"`, `"0.25"`)
   - Whitespace is ignored (`" 6 *  2 "` gives the same tokens as `"6*2"`)
   - Empty and whitespace-only input -> empty token list
   - Invalid character (`"2 $ 3"`) -> `CalcException` with kind `UnexpectedCharacter`
   - Malformed number (`"1.2.3"`) -> `CalcException` with kind `InvalidNumber`
2. **Implementation**
   - Create `Token.cs` with `Operator`, `Token` and `Tokenizer.Tokenize`.
   - Scan the characters once, accumulate digits and `.` into a number, map single characters to operators. Parse numbers with `double.Parse(..., CultureInfo.InvariantCulture)` (or `TryParse`) so the result does not depend on the machine's locale.

### Stage 3: Tokenizer, parentheses

1. **Tests**
   - `"(2+3)*4"` produces `LeftParenToken`, `RightParenToken` in the right places.
   - Nested parentheses `"((1))"`.
2. **Implementation**
   - Add `LeftParenToken` and `RightParenToken` and tokenize `(` and `)`.
   - Balance checking is the parser's job, not the tokenizer's.

### Stage 4: Expression tree and parser, single operations

1. **Tests**
   - Define the expected trees by hand and compare with `Parse` using `Assert.Equal`.
   - `[Number(6)]` -> `NumberExpr(6)`
   - `6*2` -> `BinaryExpr(Mul, 6, 2)`, and the same shape for `+ - / ^`
   - Empty token list -> `UnexpectedEnd`
   - Dangling operator (`6*`) -> `UnexpectedEnd`
   - Two numbers in a row (`6 2`) or operator first (`* 2`) -> `UnexpectedToken`
2. **Implementation**
   - Create `Expr.cs` with the `Expr` records.
   - Create `Parser.cs` with the `Parse` method (precedence climbing / Pratt parser, built so that the next stage only changes the binding powers).

Operator priority and associativity (highest binds tightest):

| Operator | Precedence | Associativity |
|----------|-----------|---------------|
| `^` | 3 | right |
| `*` `/` | 2 | left |
| `+` `-` | 1 | left |

### Stage 5: Parser, precedence and associativity

1. **Tests**
   - `2+3*4` -> `+` at the root, with `3*4` as the right child
   - `2*3+4` -> `+` at the root, with `2*3` as the left child
   - Left associativity: `8-3-2` -> `(8-3)-2`; `16/4/2` -> `(16/4)/2`
   - Right associativity: `2^3^2` -> `2^(3^2)`
   - `2*3^2` -> `2*(3^2)`
2. **Implementation**
   - Add the precedence and associativity table to the parser and use it to decide when to continue or stop consuming operators.

### Stage 6: Parser, parentheses

1. **Tests**
   - `(2+3)*4` -> `*` at the root, with `2+3` as the left child
   - Nested: `((2+3))*4`, `2*(3+(4-1))`
   - Unbalanced: `(2+3` and `2+3)` -> `UnbalancedParentheses`
   - Empty parentheses `()` -> `UnexpectedToken`
2. **Implementation**
   - In the primary-expression rule, handle `LeftParenToken`: parse a full expression, then require `RightParenToken`.
   - After the top-level expression, any leftover token is an error.

### Stage 7: Evaluator

1. **Tests**
   - `Evaluate` on hand-built trees: `NumberExpr(5)` -> `5`; each operator on simple values (`6*2` -> `12`, `7-2` -> `5`, `9/3` -> `3`, `2^10` -> `1024`)
   - Nested trees: `(2+3)*4` -> `20`
   - Decimals: `1/4` -> `0.25`
   - Division by zero -> `DivisionByZero` (including a nested zero such as `1/(2-2)`). Note that `double` division by zero does not throw in .NET (it gives infinity or `NaN`), so the check must be explicit.
   - Power with `Math.Pow` (for example `2^0.5` is approximately `Math.Sqrt(2)`; use `Assert.Equal(expected, actual, precision)` or a tolerance, not exact equality)
   - Negative base with a fractional exponent `p/q` (in lowest terms): real when `q` is odd, with sign `(-1)^p` (`(-8)^(1/3)` = `-2`, `(-8)^(2/3)` = `4`, `(-8)^(2/6)` = `-2` because `2/6` reduces to `1/3`); `DomainError` when `q` is even (`(-4)^(1/2)`, `(-8)^(1/6)`) or the exponent is not close to a fraction (`(-8)^0.3333`)
2. **Implementation**
   - `Math.Pow` returns `NaN` for every negative base with a non-integer exponent, so the evaluator recovers `p/q` from the `double` exponent with continued fractions (denominator at most 1000, relative tolerance `1e-12`) and computes the odd roots itself.
   - Create `Evaluator.cs` with a recursive `Evaluate(Expr) -> double`, using a `switch` expression over the `Expr` record types.
   - Check for a zero divisor before dividing.

### Stage 8: Public API and end-to-end tests

1. **Tests** (in `Calc.Lib.Tests/CalculatorTests.cs`)
   - `Calculator.Evaluate("2+3")` -> `5.0`
   - `Calculator.Evaluate("6*2")` -> `12.0`
   - `Calculator.Evaluate("(2+3)*4")` -> `20.0`
   - `Calculator.Evaluate("2^3^2")` -> `512.0`
   - `Calculator.Evaluate("2+3*4-5/5")` -> `13.0`
   - Errors propagate from each phase as `CalcException`: `"2 $ 3"`, `"2+"`, `"(2"`, `"1/0"`
   - Empty input -> a `CalcException`, never any other exception type
   - A `null` input is handled deliberately (treated as empty input or rejected with a documented exception)
2. **Implementation**
   - In `Calculator.cs`: `public static double Evaluate(string input)` chains `Tokenize`, `Parse` and `Evaluate`.
   - Keep the phase classes `internal`; only `Calculator` and `CalcException` are public.
   - The parser rejects expressions deeper than a `MaxDepth` constant (1024, chosen so that even deeply nested input stays well within the default stack) with a `CalcErrorKind.ExpressionTooDeep` error. Two things are measured: the parser's recursion nesting (nested parentheses and right-associative `^` chains) and the height of the tree (the evaluator recurses once per level, so a flat `1+1+...` chain is as dangerous as nesting). A chain of 1024 operators is accepted; 1025 is rejected. `Calculator.Evaluate(null)` is treated as empty input (`UnexpectedEnd`). This matters in .NET because a `StackOverflowException` cannot be caught and would crash the process. Stage 7 also adds `CalcErrorKind.DomainError` (negative base with a fractional exponent whose reduced denominator is even, or a `NaN` result); `0^negative` is `DivisionByZero`; overflow gives infinity.

**Milestone:** the basic operators work end to end. Move on to Part 2.

---

## Part 2: Calc.Tui REPL

Start only once Part 1 is finished. All calculation stays in `Calc.Lib`; `Calc.Tui` only handles input and output.

To make the REPL testable, put the loop in a method that takes a reader and a writer instead of using `Console` directly:

```csharp
static void Run(TextReader input, TextWriter output)
```

`Program.cs` calls `Run(Console.In, Console.Out)`. The tests for it live in a separate `Calc.Tui.Tests` project (xUnit, referencing `Calc.Tui`, with `InternalsVisibleTo` if `Run` is not public), so the library tests stay independent of the console app.

### Stage 9: REPL loop

1. **Tests** (in `tests/Calc.Tui.Tests/ReplTests.cs`; feed a `StringReader` as input, inspect the `StringWriter` output)
   - Input `2+3\n` -> output contains `5`
   - Several lines are each evaluated in order
   - Invalid input (`2+\n`) prints an error message and the loop continues with the next line
   - Empty or whitespace-only lines are ignored
   - `exit` and `quit` stop the loop; end of input (`ReadLine()` returns `null`) also stops it
   - The prompt (`> `) is written before each read
   - `help` (any case, surrounding whitespace ignored) prints a short help text that lists the operators and the `help`, `exit` and `quit` commands, then the loop continues
2. **Implementation**
   - Implement `Run` in `src/Calc.Tui/Repl.cs` and call it from `Program.cs`.
   - Catch `CalcException` inside the loop and print `Error: <message>`. Other exceptions are not caught: they would be bugs and should surface.
   - Output format: `> ` before every read; one line per result or error; empty and whitespace-only lines print nothing; `exit` / `quit` (any case, surrounding whitespace ignored) return without extra output; end of input prints a final newline so the shell prompt starts on a fresh line.
   - Format results with the invariant culture and 12 significant digits (`"G12"`): whole numbers display without a trailing `.0` (`5`, not `5.0`), `0.1+0.2` prints `0.3`, overflow prints `inf` / `-inf`, and `-0` prints `0`.
   - Replace the project template's "Hello, World!" placeholder.

### Stage 10: Manual check

- Run `dotnet run --project src/Calc.Tui` and try valid input, errors, `exit` and Ctrl+Z / Ctrl+D.
- Update the README usage section if the real output differs from the example.

**Milestone:** a working calculator with the basic operators.

---

## Part 3: Calc.Lib extended features

Each item follows the same two steps: tokenizer tests then code, parser tests then code, evaluator tests then code, and end-to-end tests at the end. The REPL needs no change.

### Stage 11: Unary minus

1. **Tests**
   - Tokens/parser: `-5`, `2*-3`, `-(2+3)`, `--5`
   - Precedence: `-2^2` -> `-(2^2)` = `-4`
   - Evaluate: `-5` -> `-5`, `2--3` -> `5`
2. **Implementation**
   - Add a `UnaryExpr` record and a `Neg` operation; the parser treats `-` in prefix position as negation.
   - Precedence: the operand of a prefix `-` takes the `^` operators that follow, so `-2^2` is `-(2^2)` and `2^-3^2` is `2^(-(3^2))`; it leaves `* / + -` to the caller, so `-2*3` is `(-2)*3`. `+` is not a unary operator (`+5` is `UnexpectedToken`).
   - A negation counts as one level of nesting and one level of tree height, so the depth limit applies to it as well: 1024 consecutive `-` are accepted, 1025 give `ExpressionTooDeep`.
   - This also makes negative bases directly writable: `(-8)^(1/3)` is `-2`, `-8^(2/3)` is `-4` and `(-8)^(2/3)` is `4`.

### Stage 12: Modulo

1. **Tests**
   - `7%3` -> `1`; same precedence as `*` and `/`; `5%0` -> `DivisionByZero`
2. **Implementation**
   - Add `Operator.Mod`, tokenizer support, precedence entry and evaluation (C#'s `%` operator on `double`).
   - Add `%` to the operator list in the REPL's `help` text (and to its test).

### Stage 13: Constants

1. **Tests**
   - Tokenize identifiers: `pi`, `e`
   - Evaluate `pi` approximately 3.14159, `2*pi`; unknown name -> `UnknownIdentifier`
2. **Implementation**
   - Add `IdentifierToken`, `ConstantExpr` (or a name lookup using `Math.PI` and `Math.E`) and the error kind.

### Stage 14: Functions

1. **Tests**
   - `sqrt(16)` -> `4`, `sin(0)` -> `0`, `cos(0)` -> `1`, `tan(0)` -> `0`
   - Nested and combined: `sqrt(2+2)*3`, `sin(pi/2)` approximately 1
   - Errors: unknown function, missing parentheses, `sqrt(-1)` (domain error)
2. **Implementation**
   - Add `CallExpr(string Name, Expr Arg)`, parser rule for `name ( expr )`, and a function table (`Dictionary<string, Func<double, double>>`) in the evaluator.

### Stage 15: Variables (future, not part of the current scope)

The tree design allows evaluating one parsed expression many times with different values. This would add an environment (`IReadOnlyDictionary<string, double>`) passed to `Evaluate`. It is listed in [project-scope.md](project-scope.md) as out of scope, so do not start it without updating the scope first.

---

## Working rules

- Never write implementation code before a failing test exists for it.
- Keep each cycle small: one behaviour per test, commit when green.
- Floating-point results are compared with a tolerance (`Assert.Equal(expected, actual, precision)`), except where the result is exact.
- `Calc.Lib` throws only `CalcException` on user input: no unchecked indexing, no unguarded `double.Parse`, no catch-all exception handlers hiding bugs.
- Before each commit: `dotnet build`, `dotnet test`, then `dotnet format --verify-no-changes`.

## Progress checklist

- [x] Stage 1: Error type
- [x] Stage 2: Tokenizer, numbers and operators
- [x] Stage 3: Tokenizer, parentheses
- [x] Stage 4: Parser, single operations
- [x] Stage 5: Parser, precedence and associativity
- [x] Stage 6: Parser, parentheses
- [x] Stage 7: Evaluator
- [x] Stage 8: Public API and end-to-end tests
- [x] Stage 9: REPL loop
- [x] Stage 10: Manual check
- [x] Stage 11: Unary minus
- [ ] Stage 12: Modulo
- [ ] Stage 13: Constants
- [ ] Stage 14: Functions
