namespace Calc.Lib;

internal abstract record Expr;

internal sealed record NumberExpr(double Value) : Expr;

internal sealed record BinaryExpr(Operator Op, Expr Left, Expr Right) : Expr;
