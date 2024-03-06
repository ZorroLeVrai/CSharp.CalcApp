using CalcLib.Expressions;
using CalcLib.Tokens;

namespace CalcLib;

public class Calculator
{
    public IExpression Expression { get; }

    public Calculator(string strExpression)
    {
        var tokenizer = new Tokenizer(strExpression);
        var tokenExpression = tokenizer.Tokenize();
        Expression = tokenExpression.ToExpression();
    }

    public double Compute()
    {
        Console.WriteLine(Expression.ToPrefix());
        Console.WriteLine(Expression.ToPostFix());
        return Expression.Calc();
    }
}