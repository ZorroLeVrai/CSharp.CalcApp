namespace Calc.Lib;

/// <summary>A binary operator of the expression language.</summary>
public enum Operator
{
    Add,
    Sub,
    Mul,
    Div,
    Pow,
}

internal abstract record Token;

internal sealed record NumberToken(double Value) : Token;

internal sealed record OperatorToken(Operator Op) : Token;
