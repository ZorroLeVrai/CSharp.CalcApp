# Project Scope

## Goal

Build a Rust application that evaluates text arithmetic expressions. Given `"2+3"`, it returns `5`.

## In scope

### calc_lib (calculation library)

- Evaluate an expression string to an `f64` result through a single entry point, e.g. `evaluate(&str) -> Result<f64, CalcError>`
- Operators: `+ - * /`, modulo `%`, power `^`, unary minus
- Parentheses with correct precedence and associativity (`^` is right-associative)
- Functions: `sqrt`, `sin`, `cos`, `tan`, and similar
- Constants: `pi`, `e`
- Decimal numbers and whitespace-insensitive input
- Clear, typed errors: invalid token, unbalanced parentheses, division by zero, unknown function or constant
- Unit tests covering precedence, associativity, unary minus, functions and every error case

### calc_tui (text interface)

- Plain stdin/stdout REPL: prompt, read a line, print the result or error
- `exit` / `quit` / EOF (Ctrl+D) to leave; empty lines ignored
- Errors displayed without crashing

## Out of scope

- Full-screen or graphical interfaces
- Variables, user-defined functions, or persistent state between expressions
- Calculation history, saving or loading
- Arbitrary-precision or integer-only arithmetic
- Complex numbers, matrices, unit conversion
- Networking, plugins, configuration files

## Success criteria

- `evaluate("2+3")` returns `Ok(5.0)`
- `(2+3)*4` returns `20`; `2^3^2` returns `512`
- Invalid input returns an error and never panics
- `cargo build`, `cargo test`, `cargo clippy` and `cargo fmt --check` all pass
