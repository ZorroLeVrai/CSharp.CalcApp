namespace CalcLib.Expressions;

internal class UnaryOperator : IExpression
{
    public Func<double, double> Operation { get; }

    public string? Symbol { get; }

    public IExpression Expr { get; }

    public UnaryOperator((Func<double, double> operation, string? symbol) operationData, IExpression expr)
    {
        Operation = operationData.operation;
        Symbol = operationData.symbol;
        Expr = expr;
    }

    public double Calc()
    {
        return Operation(Expr.Calc());
    }

    public string ToPrefix()
    {
        return $"({Symbol} {Expr.ToPrefix()})";
    }

    public string ToPostFix()
    {
        return $"({Expr.ToPostFix()} {Symbol})";
    }
}
