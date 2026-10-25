# Tech Stack

## Language and tooling

- **Language:** C# 14
- **Runtime / SDK:** .NET 10
- **Build system / package manager:** .NET SDK (`dotnet` CLI) and NuGet, using a solution (`.sln`)
- **Formatting:** `dotnet format`
- **Linting:** Roslyn analyzers with nullable reference types enabled and warnings treated as errors
- **Testing:** xUnit (`dotnet test`), unit tests in separate test projects (`Calc.Lib.Tests`, `Calc.Tui.Tests`)

## Solution

| Project          | Type          | Purpose                                              |
|------------------|---------------|------------------------------------------------------|
| `Calc.Lib`       | class library | Expression parsing and evaluation, no I/O            |
| `Calc.Tui`       | console app   | Plain stdin/stdout REPL, references `Calc.Lib`       |
| `Calc.Lib.Tests` | xUnit tests   | Unit tests for `Calc.Lib`                            |
| `Calc.Tui.Tests` | xUnit tests   | Unit tests for the `Calc.Tui` REPL loop              |

## Design choices

- **Number type:** `double`
- **Evaluation pipeline:** tokenizer -> parser (recursive descent or Pratt) -> AST -> evaluator
- **Error handling:** typed `CalcException` (with an error kind) for invalid input; no unhandled exceptions of any other type on user input
- **Dependencies:** none for `Calc.Lib` (base class library only). `Calc.Tui` uses only the base class library for I/O. The test projects reference xUnit (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`). Add other NuGet packages only when clearly justified.
- **Interface:** simple REPL, no full-screen TUI framework (e.g. no Terminal.Gui or Spectre.Console)

## Target platform

- Developed on Windows 11; the code should stay cross-platform (Windows, Linux, macOS) using only the base class library.
