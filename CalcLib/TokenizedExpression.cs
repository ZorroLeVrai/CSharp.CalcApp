using CalcLib.Expressions;
using CalcLib.Tokens;
using System.Text;

namespace CalcLib;

public enum OperatorType
{
    Unary,
    Binary
};

public class TokenizedExpression
{
    private record OperationData(List<Type> Types, OperatorType OperatorType);

    private List<ICalcToken> _tokenList = new List<ICalcToken>();

    //public IEnumerable<ICalcToken> TokenList { get { return _tokenList; } }

    public TokenizedExpression() { }

    public TokenizedExpression(List<ICalcToken> tokenList)
    {
        _tokenList = tokenList;
    }

    public TokenizedExpression(IEnumerable<ICalcToken> tokenList)
    {
        _tokenList = new List<ICalcToken>(tokenList);
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

        List<OperationData> operationDataList = new List<OperationData>()
        {
            new OperationData(new List<Type> { typeof(Plus), typeof(Minus) }, OperatorType.Binary),
            new OperationData(new List<Type> { typeof(Multiply), typeof(Divide) }, OperatorType.Binary),
            new OperationData(new List<Type> { typeof(Power)}, OperatorType.Binary),
            new OperationData(new List<Type> { typeof(Negative) }, OperatorType.Unary)
        };

        foreach (var operationData in operationDataList)
        {
            expr = GetOperatorExpression(operationData);
            if (expr != null)
                return expr;
        }

        if (_tokenList.Count == 1)
        {
            if (_tokenList[0] is NumberToken numberToken)
                return new Number(numberToken);

            if (_tokenList[0] is BinaryOperator op)
                return op;
        } 
            
        throw new InvalidDataException("Invalid Expression");


        (Func<double, double, double> operation, string? symbol) GetBinaryOperator(ICalcToken token)
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

        (Func<double, double> operation, string? symbol) GetUnaryOperator(ICalcToken token)
        {
            string? symbol = token.ToString();
            Func<double, double> op = token switch
            {
                Negative => a => -a,
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
                //Handle the expression in parenthesis
                var nestedExpression = new TokenizedExpression(_tokenList[(startIndex + 1)..endIndex]).ToExpression();

                //Add the tokens before the parenthesis
                var tokenList = (startIndex > 0) ? new List<ICalcToken>(_tokenList[..startIndex]) : new List<ICalcToken>();

                //Add the expression in parenthesis
                tokenList.Add(nestedExpression);

                //Add the tokens after the parenthesis
                if (endIndex < _tokenList.Count - 1)
                {
                    tokenList.AddRange(_tokenList[(endIndex + 1)..]);
                }
                return new TokenizedExpression(tokenList).ToExpression();
            }
            else
                return null;
        }

        IExpression? GetOperatorExpression(OperationData operationData)
        {
            var (types, operatorType) = operationData;

            for (int i = _tokenList.Count - 1; i >= 0; --i)
            {
                if (types.Contains(_tokenList[i].GetType()))
                {
                    if (operatorType == OperatorType.Binary)
                        return new BinaryOperator(
                            GetBinaryOperator(_tokenList[i]),
                            new TokenizedExpression(_tokenList[..i]).ToExpression(),
                            new TokenizedExpression(_tokenList[(i + 1)..]).ToExpression());
                    else
                        return new UnaryOperator(
                            GetUnaryOperator(_tokenList[i]),
                            new TokenizedExpression(_tokenList[(i + 1)..]).ToExpression());
                }
                    
            }

            return null;
        }
    }
}