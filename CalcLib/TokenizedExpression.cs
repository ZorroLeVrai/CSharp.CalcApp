using CalcLib.Tokens;
using System.Linq.Expressions;
using System.Text;

namespace CalcLib;

public class TokenizedExpression
{
    private List<ICalcToken> _tokenList = new List<ICalcToken>();

    //public IEnumerable<ICalcToken> TokenList { get { return _tokenList; } }

    public TokenizedExpression() { }

    public TokenizedExpression(List<ICalcToken> tokenList)
    {
        _tokenList = tokenList;
    }

    public void AddToken(ICalcToken token)
    {
        _tokenList.Add(token);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var tokenItem in _tokenList)
        {
            sb.Append(tokenItem);
            sb.Append(" ");
        }
        return sb.ToString();
    }

    public IExpression ToExpression()
    {
        var expr = HandleExpressionInParenthesis();
        if (expr != null)
            return expr;

        List<List<Type>> operatorTypeList = new List<List<Type>>()
        {
            new List<Type> { typeof(Plus), typeof(Minus) },
            new List<Type> { typeof(Multiply), typeof(Divide) },
            new List<Type> { typeof(Power)}
        };

        foreach (var operatorTypes in operatorTypeList)
        {
            expr = GetExpression(operatorTypes);
            if (expr != null)
                return expr;
        }

        if (_tokenList.Count == 1)
        {
            if (_tokenList[0] is NumberToken numberToken)
                return new Number(numberToken);

            if (_tokenList[0] is Operator op)
                return op;
        } 
            
        throw new InvalidDataException("Invalid Expression");


        (Func<double, double, double> operation, string? symbol) GetOperator(ICalcToken token)
        {
            string? symbol = token.ToString();
            Func<double, double, double> op = token switch
            {
                Plus => (a, b) => a + b,
                Minus => (a, b) => a - b,
                Multiply => (a, b) => a * b,
                Divide => (a, b) => a / b,
                Power => (a, b) => Math.Pow(a, b),
                _ => throw new NotImplementedException($"Operator {symbol} not implemented")
            };

            return (op, symbol);
        }

        IExpression? HandleExpressionInParenthesis()
        {
            var currentNesting = 0;
            var startIndex = -1;
            var endIndex = -1;
            for (int i = 0; i < _tokenList.Count; ++i)
            {
                if (_tokenList[i] is OpenParenthesis)
                {
                    if (currentNesting == 0)
                        startIndex = i;
                    ++currentNesting;
                }
                
                if (_tokenList[i] is CloseParenthesis)
                {
                    --currentNesting;
                    if (currentNesting == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }
            }

            if (startIndex >= 0 && endIndex > startIndex)
            {
                var nestedExpression = new TokenizedExpression(_tokenList[(startIndex + 1)..endIndex]).ToExpression();
                var tokenList = (startIndex > 0) ? new List<ICalcToken>(_tokenList[..startIndex]) : new List<ICalcToken>();
                tokenList.Add(nestedExpression);
                if (endIndex < _tokenList.Count - 1)
                {
                    tokenList.AddRange(_tokenList[(endIndex + 1)..]);
                }
                return new TokenizedExpression(tokenList).ToExpression();
            }
            else
                return null;
        }

        IExpression? GetExpression(List<Type> typeList)
        {
            for (int i = _tokenList.Count - 1; i >= 0; --i)
            {
                if (typeList.Contains(_tokenList[i].GetType()))
                    return new Operator(
                        GetOperator(_tokenList[i]),
                        new TokenizedExpression(_tokenList[..i]).ToExpression(),
                        new TokenizedExpression(_tokenList[(i + 1)..]).ToExpression());
            }

            return null;
        }
    }
}