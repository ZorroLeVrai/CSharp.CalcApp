using CalcLib.Tokens;

namespace CalcLib;

//Test
public interface IExpression : ICalcToken
{
    double Calc();

    string ToPrefix();

    string ToPostFix();
}

internal class Number : IExpression
{
    public double Val { get; }

    public Number(NumberToken numberToken)
    {
        Val = numberToken.Val;
    }

    public double Calc()
    {
        return Val;
    }

    public string ToPrefix() => Val.ToString();

    public string ToPostFix() => Val.ToString();
}

internal class Operator : IExpression
{
    public Func<double, double, double> Operation { get; }

    public string? Symbol { get; }

    public IExpression Expr1 { get; }

    public IExpression Expr2 { get; }

    public Operator((Func<double, double, double> operation, string? symbol) operationData, IExpression expr1, IExpression expr2)
    {
        Operation = operationData.operation;
        Symbol = operationData.symbol;
        Expr1 = expr1;
        Expr2 = expr2;
    }

    public double Calc()
    {
        return Operation(Expr1.Calc(), Expr2.Calc());
    }

    public string ToPrefix()
    {
        return $"{Symbol} {Expr1.ToPrefix()} {Expr2.ToPrefix()}";
    }

    public string ToPostFix()
    {
        return $"{Expr1.ToPostFix()} {Expr2.ToPostFix()} {Symbol}";
    }
}