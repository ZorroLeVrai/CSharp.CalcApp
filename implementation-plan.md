# Implementation Plan

This plan follows **TDD**. Every stage has two steps, always in this order:

1. **Tests**: write the unit tests first. They must compile and **fail** (red).
2. **Implementation**: write the minimum code that makes them pass (green), then tidy up (refactor) while keeping the tests green.

A stage is finished only when `cargo test` passes and no earlier test is broken.

## Pipeline

`calc_lib` turns a string into a result in three separate phases:

```
"6*2"  --tokenize-->  [Number(6), Op(*), Number(2)]  --parse-->  Expr tree  --evaluate-->  12
```

Expression tree for `6*2`:

```
*
|- 6
|- 2
```

The expression is parsed **once** into a tree, then evaluated from the tree. This keeps the design ready for variables later: the same tree can be evaluated many times with different values without re-parsing.

Infix notation is used, with standard operator priority (see the table in Stage 4).

## Planned modules in calc_lib

| File | Responsibility |
|------|----------------|
| `src/error.rs` | `CalcError` enum shared by every phase |
| `src/token.rs` | `Token` and `Operator` types, `tokenize(&str) -> Result<Vec<Token>, CalcError>` |
| `src/ast.rs` | `Expr` tree type |
| `src/parser.rs` | `parse(&[Token]) -> Result<Expr, CalcError>` |
| `src/eval.rs` | `eval(&Expr) -> Result<f64, CalcError>` |
| `src/lib.rs` | Module declarations and the public `evaluate(&str) -> Result<f64, CalcError>` |

Unit tests live in a `#[cfg(test)] mod tests` at the bottom of each file. End-to-end tests for `evaluate` live in `calc_lib/tests/`.

Types sketch (to be refined during the stages):

```rust
enum Operator { Add, Sub, Mul, Div, Pow }

enum Token { Number(f64), Operator(Operator) }

enum Expr {
    Number(f64),
    Binary { op: Operator, left: Box<Expr>, right: Box<Expr> },
}
```

---

## Part 1: calc_lib basic operators

Scope: numbers and the operators `+ - * / ^`, plus parentheses. Other operators (`%`, unary minus), functions and constants come in Part 3.

### Stage 1: Error type

1. **Tests**
   - `CalcError` variants implement `Debug`, `PartialEq` and `Display`.
   - `Display` gives a readable message for each variant.
2. **Implementation**
   - Create `error.rs` with `CalcError`. Initial variants: `UnexpectedCharacter(char)`, `InvalidNumber(String)`, `UnexpectedToken`, `UnexpectedEnd`, `UnbalancedParentheses`, `DivisionByZero`.
   - Implement `std::fmt::Display` and `std::error::Error`.
   - New variants can be added in later stages when a test needs them.

### Stage 2: Tokenizer, numbers and operators

1. **Tests**
   - `"6*2"` -> `[Number(6.0), Operator(Mul), Number(2.0)]`
   - Each operator is recognised: `+ - * / ^`
   - Multi-digit numbers (`"123"`) and decimals (`"1.5"`, `"0.25"`)
   - Whitespace is ignored (`" 6 *  2 "` gives the same tokens as `"6*2"`)
   - Empty and whitespace-only input -> empty token list
   - Invalid character (`"2 $ 3"`) -> `UnexpectedCharacter('$')`
   - Malformed number (`"1.2.3"`) -> `InvalidNumber`
2. **Implementation**
   - Create `token.rs` with `Operator`, `Token` and `tokenize`.
   - Scan the characters once, accumulate digits and `.` into a number, map single characters to operators.

### Stage 3: Tokenizer, parentheses

1. **Tests**
   - `"(2+3)*4"` produces `LeftParen`, `RightParen` tokens in the right places.
   - Nested parentheses `"((1))"`.
2. **Implementation**
   - Add `Token::LeftParen` and `Token::RightParen` and tokenize `(` and `)`.
   - Balance checking is the parser's job, not the tokenizer's.

### Stage 4: Expression tree and parser, single operations

1. **Tests**
   - Define the expected trees by hand and compare with `parse`.
   - `[Number(6)]` -> `Number(6)`
   - `6*2` -> `Binary(Mul, 6, 2)`, and the same shape for `+ - / ^`
   - Empty token list -> `UnexpectedEnd`
   - Dangling operator (`6*`) -> `UnexpectedEnd`
   - Two numbers in a row (`6 2`) or operator first (`* 2`) -> `UnexpectedToken`
2. **Implementation**
   - Create `ast.rs` with `Expr`; derive `Debug`, `PartialEq`, `Clone`.
   - Create `parser.rs` with the `parse` function (precedence climbing / Pratt parser, built so that the next stage only changes the binding powers).

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
   - In the primary-expression rule, handle `LeftParen`: parse a full expression, then require `RightParen`.
   - After the top-level expression, any leftover token is an error.

### Stage 7: Evaluator

1. **Tests**
   - `eval` on hand-built trees: `Number(5)` -> `5`; each operator on simple values (`6*2` -> `12`, `7-2` -> `5`, `9/3` -> `3`, `2^10` -> `1024`)
   - Nested trees: `(2+3)*4` -> `20`
   - Decimals: `1/4` -> `0.25`
   - Division by zero -> `DivisionByZero` (including a nested zero such as `1/(2-2)`)
   - Power with `f64::powf` (for example `2^0.5` is approximately `sqrt(2)`; compare with a small tolerance, not `==`)
2. **Implementation**
   - Create `eval.rs` with a recursive `eval(&Expr) -> Result<f64, CalcError>`.
   - Check for a zero divisor before dividing.

### Stage 8: Public API and end-to-end tests

1. **Tests** (in `calc_lib/tests/evaluate.rs`)
   - `evaluate("2+3")` -> `Ok(5.0)`
   - `evaluate("6*2")` -> `Ok(12.0)`
   - `evaluate("(2+3)*4")` -> `Ok(20.0)`
   - `evaluate("2^3^2")` -> `Ok(512.0)`
   - `evaluate("2+3*4-5/5")` -> `Ok(13.0)`
   - Errors propagate from each phase: `"2 $ 3"`, `"2+"`, `"(2"`, `"1/0"`
   - Empty input -> an error, never a panic
2. **Implementation**
   - In `lib.rs`: `pub fn evaluate(input: &str) -> Result<f64, CalcError>` chains `tokenize`, `parse` and `eval`.
   - Re-export `CalcError`; keep the individual phases public, or `pub(crate)` if the public surface should stay small.
   - Added during this stage: the parser rejects expressions nested deeper than `MAX_DEPTH` (128, chosen so that even a debug build on the 1 MB Windows main-thread stack stays safe) with `CalcError::ExpressionTooDeep`, so huge inputs cannot overflow the stack. Stage 7 also added `CalcError::DomainError` (negative base with fractional exponent, or a `NaN` result); `0^negative` is `DivisionByZero`; overflow gives infinity.

**Milestone:** the basic operators work end to end. Move on to Part 2.

---

## Part 2: calc_tui REPL

Start only once Part 1 is finished. All calculation stays in `calc_lib`; `calc_tui` only handles input and output.

To make the REPL testable, put the loop in a function that takes a reader and a writer instead of using stdin and stdout directly:

```rust
fn run<R: BufRead, W: Write>(input: R, output: W) -> io::Result<()>
```

`main` calls `run(stdin().lock(), stdout())`.

### Stage 9: REPL loop

1. **Tests** (feed a byte buffer as input, inspect the output buffer)
   - Input `2+3\n` -> output contains `5`
   - Several lines are each evaluated in order
   - Invalid input (`2+\n`) prints an error message and the loop continues with the next line
   - Empty or whitespace-only lines are ignored
   - `exit` and `quit` stop the loop; end of input also stops it
   - The prompt (`> `) is written before each read
2. **Implementation**
   - Implement `run` in `calc_tui/src/main.rs` (or `repl.rs`) and call it from `main`.
   - Format results simply; whole numbers display without a trailing `.0` (`5`, not `5.0`).
   - Replace the "Hello, world!" placeholder.

### Stage 10: Manual check

- Run `cargo run -p calc_tui` and try valid input, errors, `exit` and Ctrl+Z / Ctrl+D.
- Update the README usage section if the real output differs from the example.

**Milestone:** a working calculator with the basic operators.

---

## Part 3: calc_lib extended features

Each item follows the same two steps: tokenizer tests then code, parser tests then code, evaluator tests then code, and end-to-end tests at the end. The REPL needs no change.

### Stage 11: Unary minus

1. **Tests**
   - Tokens/parser: `-5`, `2*-3`, `-(2+3)`, `--5`
   - Precedence: `-2^2` -> `-(2^2)` = `-4`
   - Evaluate: `-5` -> `-5`, `2--3` -> `5`
2. **Implementation**
   - Add `Expr::Unary` and a `Neg` operation; the parser treats `-` in prefix position as negation.

### Stage 12: Modulo

1. **Tests**
   - `7%3` -> `1`; same precedence as `*` and `/`; `5%0` -> `DivisionByZero`
2. **Implementation**
   - Add `Operator::Mod`, tokenizer support, precedence entry and evaluation.

### Stage 13: Constants

1. **Tests**
   - Tokenize identifiers: `pi`, `e`
   - Evaluate `pi` approximately 3.14159, `2*pi`; unknown name -> `UnknownIdentifier`
2. **Implementation**
   - Add `Token::Identifier`, `Expr::Constant` (or a name lookup) and the error variant.

### Stage 14: Functions

1. **Tests**
   - `sqrt(16)` -> `4`, `sin(0)` -> `0`, `cos(0)` -> `1`, `tan(0)` -> `0`
   - Nested and combined: `sqrt(2+2)*3`, `sin(pi/2)` approximately 1
   - Errors: unknown function, missing parentheses, `sqrt(-1)` (domain error)
2. **Implementation**
   - Add `Expr::Call { name, arg }`, parser rule for `name ( expr )`, and a function table in the evaluator.

### Stage 15: Variables (future, not part of the current scope)

The tree design allows evaluating one parsed expression many times with different values. This would add an environment (`HashMap<String, f64>`) passed to `eval`. It is listed in [project-scope.md](project-scope.md) as out of scope, so do not start it without updating the scope first.

---

## Working rules

- Never write implementation code before a failing test exists for it.
- Keep each cycle small: one behaviour per test, commit when green.
- Floating-point results are compared with a tolerance, except where the result is exact.
- `calc_lib` never panics on user input: no `unwrap`/`expect` outside tests.
- Before each commit: `cargo test --workspace`, then `cargo clippy --all-targets` and `cargo fmt --check` (install both with `rustup component add clippy rustfmt` if missing).

## Progress checklist

- [ ] Stage 1: Error type
- [ ] Stage 2: Tokenizer, numbers and operators
- [ ] Stage 3: Tokenizer, parentheses
- [ ] Stage 4: Parser, single operations
- [ ] Stage 5: Parser, precedence and associativity
- [ ] Stage 6: Parser, parentheses
- [ ] Stage 7: Evaluator
- [ ] Stage 8: Public API and end-to-end tests
- [ ] Stage 9: REPL loop
- [ ] Stage 10: Manual check
- [ ] Stage 11: Unary minus
- [ ] Stage 12: Modulo
- [ ] Stage 13: Constants
- [ ] Stage 14: Functions
