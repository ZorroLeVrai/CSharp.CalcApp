# CSharp.calc

A C# (.NET 10) application that evaluates text arithmetic expressions. Given `2+3`, it returns `5`.

The solution contains four projects:

- **Calc.Lib**: the calculation library (parsing and evaluation, no I/O)
- **Calc.Tui**: a plain stdin/stdout REPL built on `Calc.Lib`
- **Calc.Lib.Tests**: xUnit tests for `Calc.Lib`
- **Calc.Tui.Tests**: xUnit tests for the `Calc.Tui` REPL

## Features

- Operators `+ - * / %` and `^` (power, right-associative) with standard priority; `%` is the remainder and takes the sign of the left operand (`-7%3` is `-1`)
- Unary minus (`-5`, `2*-3`, `-2^2` is `-4`)
- Constants `pi` and `e` (lowercase only; unknown names are reported as errors)
- One-argument functions, lowercase only, angles in radians: `sqrt abs exp ln log log2 sin cos tan asin acos atan floor ceil round` (`ln` is the natural logarithm, `log` is base 10, `log2` is base 2, `round` rounds halves away from zero), e.g. `sqrt(2+2)*3`
- Parentheses, decimal numbers and whitespace
- Typed errors (`CalcException`) instead of unhandled exceptions (invalid input, unbalanced parentheses, division by zero, results that are not real numbers such as `sqrt(-1)`, `ln(0)` or `log2(0)`, unknown names or functions, expressions nested too deeply)
- Results rounded to 12 significant digits; overflow prints `inf`
- Simple REPL: type an expression, press Enter; `help` shows a short help text; `exit` or `quit` leaves

Scientific notation is supported, where `2.5e5` is `2.5*10^5`. The `e` must directly follow a number, so the constant `e` keeps working (`2*e`, `e^2`).

Not planned: implicit multiplication (`2pi`), variables, functions with several arguments.

## Usage

```
dotnet run --project src/Calc.Tui
```

```
> 2+3
5
> (2+3)*4
20
> sqrt(2+2)*3
6
> 10/0
Error: Division by zero
> quit
```

## Development

```
dotnet build                # build the solution
dotnet test                 # run all tests
dotnet format               # format
```

## Documentation

- [project-scope.md](project-scope.md): what is and is not in scope
- [tech-stack.md](tech-stack.md): languages, tooling and design choices
- [implementation-plan.md](implementation-plan.md): TDD plan, stage by stage, with a progress checklist
- [CLAUDE.md](CLAUDE.md): guidance for Claude Code

## Status

Stages 1 to 15 of [implementation-plan.md](implementation-plan.md) are done: the calculator library (tokenizer, parser, evaluator) and the REPL are implemented and tested. Variables are out of scope (a future stage).
