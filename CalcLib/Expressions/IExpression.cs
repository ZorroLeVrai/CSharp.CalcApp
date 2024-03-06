using CalcLib.Tokens;

namespace CalcLib.Expressions;

//Test
public interface IExpression : ICalcToken
{
    double Calc();

    string ToPrefix();

    string ToPostFix();
}

