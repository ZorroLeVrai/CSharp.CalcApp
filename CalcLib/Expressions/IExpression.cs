using CalcLib.Tokens;

namespace CalcLib.Expressions;

//TODO: See if there is a better way to handle this as declaring IExpression : ICalcToken
public interface IExpression : ICalcToken
{
    double Calc();

    string ToPrefix();

    string ToPostFix();
}

