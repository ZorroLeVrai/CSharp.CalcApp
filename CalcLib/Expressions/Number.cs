using CalcLib.Tokens;

namespace CalcLib.Expressions;

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
