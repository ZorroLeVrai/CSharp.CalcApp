namespace Calc.Lib;

internal abstract record Expr;

internal sealed record NumberExpr(double Value) : Expr;

internal sealed record ConstantExpr(string Name, double Value) : Expr;

internal sealed record CallExpr(string Name, Expr Argument) : Expr;

internal enum UnaryOperator
{
    Neg,
}

internal sealed record UnaryExpr(UnaryOperator Op, Expr Operand) : Expr;

internal sealed record BinaryExpr(Operator Op, Expr Left, Expr Right) : Expr;
