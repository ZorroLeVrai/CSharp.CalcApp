# CLAUDE.md

## Project overview

> **Status: not yet implemented.** Only the planning documents (this file and `tech-stack.md`) exist. The solution, projects, and code described below are the intended design and still need to be created.

A C# (.NET 10) application that performs calculations. It takes a text expression such as `"2+3"` and returns the result (`5`).

The solution contains three projects:

1. **Calc.Lib** (class library): the calculation engine. Parses and evaluates expressions.
2. **Calc.Tui** (console app): the text interface. References `Calc.Lib`.
3. **Calc.Lib.Tests** (xUnit test project): unit tests for `Calc.Lib`.

## Layout

Planned layout (not yet created):

```
CSharp.calc/
├── CSharp.calc.sln
├── CLAUDE.md
├── tech-stack.md
├── src/
│   ├── Calc.Lib/            # library: parsing + evaluation, no I/O
│   │   ├── Calc.Lib.csproj
│   │   └── Calculator.cs
│   └── Calc.Tui/            # console app: REPL front end
│       ├── Calc.Tui.csproj
│       └── Program.cs
└── tests/
    └── Calc.Lib.Tests/      # xUnit tests for Calc.Lib
        └── Calc.Lib.Tests.csproj
```

## Calc.Lib

- Public API centres on a single entry point, e.g. `public static double Evaluate(string expr)`, which throws `CalcException` on invalid input.
- Numbers are `double`.
- Supported syntax:
  - Binary operators: `+ - * /`, modulo `%`, power `^`
  - Unary minus (`-x`)
  - Parentheses with correct operator precedence (`(2+3)*4` = 20; `^` is right-associative)
  - Functions: `sqrt`, `sin`, `cos`, `tan`, etc.
  - Constants: `pi`, `e`
- Whitespace is ignored. Decimal numbers (`1.5`) are accepted. Parse and format numbers with the invariant culture.
- Errors are reported as a typed `CalcException` carrying an error kind (invalid token, unbalanced parentheses, division by zero, unknown function/constant, etc.). The library must never throw anything other than `CalcException` on user input.
- The library does no I/O (no `Console`, no files). Keep it pure and easy to unit-test.
- Suggested internals: tokenizer -> parser (recursive descent or Pratt) -> AST -> evaluator.

Example: `Calculator.Evaluate("2+3")` returns `5.0`.

## Calc.Tui

- A simple REPL over plain stdin/stdout (no full-screen TUI framework).
- Loop: print a prompt, read a line, call `Calculator.Evaluate`, print the result or the error message.
- `exit` / `quit` (or EOF / Ctrl+Z on Windows, Ctrl+D elsewhere) leaves the loop. Empty lines are ignored.
- Errors are shown to the user without crashing the program.
- All calculation logic lives in `Calc.Lib`; `Calc.Tui` only handles input and output.

## Commands

```
dotnet build                                # build the whole solution
dotnet run --project src/Calc.Tui           # run the REPL
dotnet test                                 # run all tests
dotnet test tests/Calc.Lib.Tests            # run library tests only
dotnet format                               # format
dotnet format --verify-no-changes           # check formatting
```

## Conventions

- Target `net10.0`, C# 14; enable nullable reference types and treat warnings as errors. Code must pass `dotnet format` and build without warnings.
- Unit tests use xUnit and live in `tests/Calc.Lib.Tests`; cover precedence, associativity, unary minus, functions, and every error case. Prefer `[Theory]` with `[InlineData]` for table-style cases.
- Prefer no external dependencies for `Calc.Lib`. Add NuGet packages only when clearly justified.
- Use `CalcException` for expected failures on user input; avoid catching general `Exception` except at the REPL boundary.
