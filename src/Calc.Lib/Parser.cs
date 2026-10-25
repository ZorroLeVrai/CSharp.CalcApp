namespace Calc.Lib;

internal sealed class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    private Parser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    public static Expr Parse(IReadOnlyList<Token> tokens)
    {
        var parser = new Parser(tokens);
        var expr = parser.ParseExpression(0);

        if (parser._position < tokens.Count)
        {
            throw new CalcException(CalcErrorKind.UnexpectedToken);
        }

        return expr;
    }

    // Precedence climbing: keep consuming operators whose precedence is at least minPrecedence.
    // A left-associative operator parses its right side one level higher, so an equal operator
    // ends it and the loop continues left to right (shallow tree walk for long chains).
    // A right-associative operator parses its right side at its own level, so an equal operator
    // is absorbed by the recursive call instead.
    private Expr ParseExpression(int minPrecedence)
    {
        var left = ParsePrimary();

        while (_position < _tokens.Count
            && _tokens[_position] is OperatorToken { Op: var op }
            && Precedence(op) >= minPrecedence)
        {
            _position++;
            var nextMinPrecedence = IsRightAssociative(op) ? Precedence(op) : Precedence(op) + 1;
            var right = ParseExpression(nextMinPrecedence);
            left = new BinaryExpr(op, left, right);
        }

        return left;
    }

    private Expr ParsePrimary()
    {
        if (_position >= _tokens.Count)
        {
            throw new CalcException(CalcErrorKind.UnexpectedEnd);
        }

        if (_tokens[_position] is NumberToken number)
        {
            _position++;
            return new NumberExpr(number.Value);
        }

        throw new CalcException(CalcErrorKind.UnexpectedToken);
    }

    private static int Precedence(Operator op) => op switch
    {
        Operator.Add or Operator.Sub => 1,
        Operator.Mul or Operator.Div => 2,
        Operator.Pow => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown operator"),
    };

    private static bool IsRightAssociative(Operator op) => op == Operator.Pow;
}
