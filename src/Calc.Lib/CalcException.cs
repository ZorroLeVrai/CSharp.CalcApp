namespace Calc.Lib;

/// <summary>The category of error reported by a <see cref="CalcException"/>.</summary>
public enum CalcErrorKind
{
    UnexpectedCharacter,
    InvalidNumber,
    UnexpectedToken,
    UnexpectedEnd,
    UnbalancedParentheses,
    DivisionByZero,
    DomainError,
    ExpressionTooDeep,
}

/// <summary>The only exception <c>Calc.Lib</c> throws for invalid user input.</summary>
public sealed class CalcException : Exception
{
    public CalcException(CalcErrorKind kind, string? detail = null)
        : base(BuildMessage(kind, detail))
    {
        Kind = kind;
    }

    public CalcErrorKind Kind { get; }

    private static string BuildMessage(CalcErrorKind kind, string? detail) => kind switch
    {
        CalcErrorKind.UnexpectedCharacter => $"Unexpected character '{detail}'",
        CalcErrorKind.InvalidNumber => $"Invalid number '{detail}'",
        CalcErrorKind.UnexpectedToken => "Unexpected token",
        CalcErrorKind.UnexpectedEnd => "Unexpected end of expression",
        CalcErrorKind.UnbalancedParentheses => "Unbalanced parentheses",
        CalcErrorKind.DivisionByZero => "Division by zero",
        CalcErrorKind.DomainError => "Result is not a real number",
        CalcErrorKind.ExpressionTooDeep => "Expression is nested too deeply",
        _ => "Calculation error",
    };
}
