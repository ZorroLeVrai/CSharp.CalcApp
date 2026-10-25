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
   - `%` is the remainder, not a mathematical modulo: the result takes the sign of the left operand (`-7%3` is `-1`, `7%-3` is `1`), and decimals work (`5.5%2` is `1.5`). A zero divisor is checked explicitly (`double % 0` is `NaN` in .NET): `5%0` is `DivisionByZero`. `infinity % 3` is `NaN`, which becomes a `DomainError`.
   - `%` sits on the `* /` level, left-associative (`10%4%3` is `(10%4)%3`, `2*7%4` is `(2*7)%4`); `^` binds tighter and `+ -` looser.

### Stage 13: Constants

1. **Tests**
   - Tokenize identifiers: `pi`, `e`
   - Evaluate `pi` approximately 3.14159, `2*pi`; unknown name -> `UnknownIdentifier`
2. **Implementation**
   - Add `IdentifierToken`, `ConstantExpr` (or a name lookup using `Math.PI` and `Math.E`) and the error kind.
   - An identifier is an ASCII letter followed by ASCII letters or digits (`pi`, `x1`); anything else (`_`, `é`) is still an `UnexpectedCharacter`. The tokenizer does not judge names: `PI` is a valid identifier token.
   - Names are resolved by the parser through a `Constants` table (`pi`, `e`; lowercase and case-sensitive) into `ConstantExpr(Name, Value)`. An unknown name throws `CalcErrorKind.UnknownIdentifier` (message `Unknown name 'foo'`) while parsing, so it is reported before anything is evaluated: `1/0+foo` is an unknown name, not a division by zero.
   - No implicit multiplication: `2pi` and `pi pi` are `UnexpectedToken`. Scientific notation was not supported at this point either: `1e5` (tokens `1`, `e5`) was `UnexpectedToken`; [Stage 15](#stage-15-scientific-notation) adds it and changes that result.
   - Add the constants to the REPL's `help` text (and to its test).

### Stage 14: Functions

1. **Tests**
   - `sqrt(16)` -> `4`, `sin(0)` -> `0`, `cos(0)` -> `1`, `tan(0)` -> `0`
   - Nested and combined: `sqrt(2+2)*3`, `sin(pi/2)` approximately 1
   - Errors: unknown function, missing parentheses, `sqrt(-1)` (domain error)
2. **Implementation**
   - Add `CallExpr(string Name, Expr Arg)`, parser rule for `name ( expr )`, and a function table (`Dictionary<string, Func<double, double>>`) in the evaluator.
   - The table lives in `Functions` (shared by the parser, which checks that a name is a function, and the evaluator, which applies it). The 15 one-argument functions are `sqrt abs exp ln log log2 sin cos tan asin acos atan floor ceil round`: lowercase and case-sensitive, angles in radians, `ln` natural, `log` base 10, `log2` base 2 (`Math.Log2`, added after the first version of this stage), `round` halves away from zero. `log2` is one name (an identifier may contain digits): `log2(8)` is a call, while `log 2` is the function `log` followed by a number, an error.
   - A call is `name ( expr )`. A known function without `(` after it is an error (`sqrt` is `UnexpectedEnd`, `sqrt 4` and `sqrt+1` are `UnexpectedToken`); `foo(2)` and `Sqrt(4)` are `UnknownIdentifier`; `pi(2)` stays `UnexpectedToken` (a constant is not a function). Several arguments are not supported (`,` is an unexpected character).
   - Outside its domain a function gives `NaN`, reported as `DomainError`: `sqrt(-1)`, `asin(2)`, `acos(2)` and also `ln(0)`, `log(0)` and `log2(0)` (which .NET evaluates to -infinity). Overflow still gives infinity (`exp(1000)`).
   - A call counts as one level of nesting and one level of tree height, like a negation or a group (1024 nested calls work, 1025 give `ExpressionTooDeep`).
   - Add the functions to the REPL's `help` text (and to its test).

### Stage 15: Scientific notation

A number can carry a power of ten: `2.5e5` is `2.5*10^5`, that is `250000`. This only changes how a number is read, so the work is in the tokenizer: the whole literal becomes one `NumberToken`, and the parser, the evaluator and the depth limit are untouched.

**Syntax**

- Form: `<mantissa>e<exponent>`. The mantissa is a decimal number as already accepted (`2`, `2.5`, `.5`, `007`). The exponent is a whole number: an optional sign (`+` or `-`) followed by ASCII digits, with no decimal point (`2.5e5`, `2.5e-3`, `2.5e+3`, `2e0`, `2e007`).
- No whitespace inside the literal: `2.5e5` is one number, `2.5 e5` is not (it stays the number `2.5` followed by the name `e5`, an `UnexpectedToken`).
- **How the constant `e` is told apart:** an `e` that directly follows a number is the exponent marker; every other `e` is unchanged (the constant `e`, or part of a name: `e`, `2*e`, `e^2`, `e+5`, `e5`, `x1e5`). So the rule is "in scientific notation the `e` is always preceded by a number", and then it must be followed by an integer exponent.
- Lowercase only, like every name: `2E5` is still the number `2` followed by the name `E5` (`UnexpectedToken`).
- The value is what `double.Parse` gives for the literal text, which is correctly rounded: `1.1e-5` is exactly the C# literal `1.1e-5` (computing `1.1 * Math.Pow(10, -5)` would not always be). Use `NumberStyles.Float` with the invariant culture, after the tokenizer has checked the shape of the literal itself.
- Overflow and underflow follow the existing rules: `1e400` is infinity (like `10^400`, printed as `inf`), `1e-400` is `0`. An exponent with very many digits (`1e99999999999999999999`) must not throw anything but `CalcException`; to be confirmed by a test (infinity or `0` is expected; if .NET rejects it, report `InvalidNumber`).
- It is an ordinary number afterwards: `-2e3` is `-(2e3)`, `2e3^2` is `(2e3)^2`, `2^1e1` is `1024`, `sqrt(1e4)` is `100`.

**Errors** (all `InvalidNumber`, and the message names the literal as scanned, e.g. `Invalid number '2e'`)

- `e` right after a number without an exponent: `2e`, `2e+`, `2e-`, `2e*3`, `2ee5`, and also `2exp(1)` (the `e` after the `2` is taken as an exponent marker).
- A non-integer or malformed exponent: `2e1.5`, `2e.5`, `2e5.`, `2e+-5`, `2e--5`.
- A malformed mantissa: `1.2.3e4`.
- Still `UnexpectedToken`, from the parser, because a number is directly followed by a name (no implicit multiplication): `2e5x`, `2e5e5`, `2e5pi`; and with whitespace, `2 e5`, `2 e`.

1. **Tests**
   - Tokenizer, valid literals (`[Theory]`, compare with the C# literal): `2.5e5` -> `250000`, `2e5`, `2.5e-3` -> `0.0025`, `2.5e+3` -> `2500`, `.5e3` -> `500`, `007e2` -> `700`, `2e0` -> `2`, `0e5` -> `0`, `1e-0`, `2e007`, `1.1e-5` (exactly equal to the C# literal), `123456789e-9`; each one is a single `NumberToken`.
   - Tokenizer, in context: `1e3+1e-3`, `2*3e2`, `(2e3)`, `2.5e5 + 1` and whitespace around the literal; `e` and `pi` untouched: `e`, `e5`, `e+5` (name, `+`, number), `2*e`, `x1e5` (one name), `2 e5` and `2E5` (number then name).
   - Tokenizer, errors: every case of the `InvalidNumber` list above, with the literal in the message; `1e400` -> infinity and `1e-400` -> `0` without an exception; a 10,000-digit exponent does not throw anything but `CalcException`.
   - Evaluator and `Calculator.Evaluate`: `2.5e5` -> `250000`; `2.5e5` equals `2.5*10^5`; `1e3+1` -> `1001`; `-2e3` -> `-2000`; `2e-3*1000` -> `2`; `1e3^2` -> `1000000`; `2^1e1` -> `1024`; `sqrt(1e4)` -> `100`; `1e2%7` -> `2`; the constant still works next to it: `1e3*e`, `e^2`, `2*e`; `1e400` -> infinity; `1/1e-400` -> `DivisionByZero`; `0*1e400` -> `DomainError` (zero times infinity); errors from the list above through `Calculator.Evaluate`.
   - Parser: no new tests are needed (a scientific literal is a `NumberToken`), but one end-to-end test checks that `2e5x` and `2 e5` are `UnexpectedToken`.
   - REPL: `2.5e5` prints `250000`; `1e3+1e-3` prints `1000.001`; an error such as `2e` prints `Error: Invalid number '2e'` and the loop continues; `help` mentions scientific notation.
   - Existing tests to change, because `1e5` is now a valid number: the `1e5` case in `Tokenize_NumberFollowedByLetters_GivesANumberThenAnIdentifier` (tokenizer tests), the `1e5` case in `Evaluate_ImplicitMultiplicationIsNotSupported` (evaluator tests) and the `1e5` case in `Evaluate_ConstantErrors` (calculator tests). Keep the intent with `2 e5`, `2E5` and `2pi`.
2. **Implementation**
   - In the tokenizer's number branch: read the run of digits and dots; if the next character is `e`, read the exponent (one optional sign, then the run of digits and dots), validate that the exponent is a non-empty run of digits, then parse the whole text with `double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, ...)`. Report `InvalidNumber` with the literal otherwise. A number without `e` keeps going through the existing path.
   - Add a line to the REPL's `help` text, for example `Numbers: 1.5, 2.5e3 (scientific notation)` (and to its test); keep `help` short.
   - **REPL output (small follow-up, easy to drop):** results are formatted with `"G12"`, which prints large and small values with an uppercase `E` (`1.12589990684E+15`, `1.22464679915E-16`), and the calculator only reads a lowercase `e`. Print the lowercase form (`1.12589990684e+15`) so that a printed result can be typed back in as input. Tests: `2^50` and `sin(pi)` print with a lowercase `e`, and the printed text evaluates back to the same value (to 12 digits).
   - Update the documentation together (see the working rules): `README.md` (features, "Not planned" line), `project-scope.md` (in scope), `CLAUDE.md` (syntax, status) and tick the stage in the checklist below.

### Future stage: Variables (not part of the current scope)

The tree design allows evaluating one parsed expression many times with different values. This would add an environment (`IReadOnlyDictionary<string, double>`) passed to `Evaluate`. It is listed in [project-scope.md](project-scope.md) as out of scope, so do not start it without updating the scope first. It has no stage number yet.

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
- [x] Stage 12: Modulo
- [x] Stage 13: Constants
- [x] Stage 14: Functions
- [x] Stage 15: Scientific notation
