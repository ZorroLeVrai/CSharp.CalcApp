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
    // Chains are handled by the loop, not by recursion, so long left-associative expressions stay shallow.
    private Expr ParseExpression(int minPrecedence)
    {
        var left = ParsePrimary();

        while (_position < _tokens.Count
            && _tokens[_position] is OperatorToken { Op: var op }
            && Precedence(op) >= minPrecedence)
        {
            _position++;
            var right = ParseExpression(Precedence(op) + 1);
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

    // All operators share one level for now; Stage 5 introduces the real table.
    private static int Precedence(Operator op) => 1;
}
