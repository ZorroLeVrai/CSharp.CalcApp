# Project Scope

## Goal

Build a C# application on .NET 10 that evaluates text arithmetic expressions. Given `"2+3"`, it returns `5`.

## In scope

### Calc.Lib (calculation library)

- Evaluate an expression string to a `double` result through a single entry point, e.g. `Calculator.Evaluate(string) -> double`, which throws `CalcException` on invalid input
- Operators: `+ - * /`, modulo `%`, power `^`, unary minus
- Parentheses with correct precedence and associativity (`^` is right-associative)
- Functions: `sqrt`, `sin`, `cos`, `tan`, and similar
- Constants: `pi`, `e`
- Decimal numbers and whitespace-insensitive input
- Clear, typed errors (`CalcException` with an error kind): invalid token, unbalanced parentheses, division by zero, unknown function or constant
- xUnit tests (in `Calc.Lib.Tests`) covering precedence, associativity, unary minus, functions and every error case

### Calc.Tui (text interface)

- Plain stdin/stdout REPL console app: prompt, read a line, print the result or error
- `exit` / `quit` / EOF (Ctrl+Z on Windows, Ctrl+D elsewhere) to leave; empty lines ignored
- `help` command that prints a short help text (the operators and the `help`, `exit` and `quit` commands)
- Errors displayed without crashing

## Out of scope

- Full-screen or graphical interfaces
- Variables, user-defined functions, or persistent state between expressions
- Calculation history, saving or loading
- Arbitrary-precision or integer-only arithmetic (`decimal` and `BigInteger` are not used)
- Complex numbers, matrices, unit conversion
- Networking, plugins, configuration files

## Success criteria

- `Calculator.Evaluate("2+3")` returns `5.0`
- `(2+3)*4` returns `20`; `2^3^2` returns `512`
- Invalid input throws `CalcException` and never any other exception type
- `dotnet build`, `dotnet test` and `dotnet format --verify-no-changes` all pass
