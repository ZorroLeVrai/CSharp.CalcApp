namespace Calc.Lib;

internal sealed class Parser
{
    // Both the parser (nested parentheses, right-associative chains) and the evaluator (one call per
    // tree level) recurse once per level. A StackOverflowException cannot be caught in .NET, so deeper
    // input is rejected up front. 1024 levels fit comfortably in the default stack.
    internal const int MaxDepth = 1024;

    private readonly IReadOnlyList<Token> _tokens;
    private int _position;
    private int _nesting;

    private Parser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    public static Expr Parse(IReadOnlyList<Token> tokens)
    {
        var parser = new Parser(tokens);
        var (expr, _) = parser.ParseExpression(0);

        if (parser._position < tokens.Count)
        {
            // Everything that can continue an expression was consumed, so a leftover ")" has no match.
            throw tokens[parser._position] is RightParenToken
                ? new CalcException(CalcErrorKind.UnbalancedParentheses)
                : new CalcException(CalcErrorKind.UnexpectedToken);
        }

        return expr;
    }

    // Precedence climbing: keep consuming operators whose precedence is at least minPrecedence.
    // A left-associative operator parses its right side one level higher, so an equal operator
    // ends it and the loop continues left to right (no recursion for long chains).
    // A right-associative operator parses its right side at its own level, so an equal operator
    // is absorbed by the recursive call instead.
    // Height is the number of operator levels in the returned tree (a lone number is 0).
    private (Expr Expr, int Height) ParseExpression(int minPrecedence)
    {
        var (left, height) = ParsePrimary();

        while (_position < _tokens.Count
            && _tokens[_position] is OperatorToken { Op: var op }
            && Precedence(op) >= minPrecedence)
        {
            _position++;
            var nextMinPrecedence = IsRightAssociative(op) ? Precedence(op) : Precedence(op) + 1;

            EnterNesting();
            var (right, rightHeight) = ParseExpression(nextMinPrecedence);
            ExitNesting();

            height = Math.Max(height, rightHeight) + 1;
            if (height > MaxDepth)
            {
                throw new CalcException(CalcErrorKind.ExpressionTooDeep);
            }

            left = new BinaryExpr(op, left, right);
        }

        return (left, height);
    }

    private (Expr Expr, int Height) ParsePrimary()
    {
        if (_position >= _tokens.Count)
        {
            throw new CalcException(CalcErrorKind.UnexpectedEnd);
        }

        switch (_tokens[_position])
        {
            case NumberToken number:
                _position++;
                return (new NumberExpr(number.Value), 0);

            case LeftParenToken:
                _position++;
                EnterNesting();
                var inner = ParseExpression(0);
                ExitNesting();
                ExpectRightParen();
                return inner;

            default:
                throw new CalcException(CalcErrorKind.UnexpectedToken);
        }
    }

    private void EnterNesting()
    {
        _nesting++;
        if (_nesting > MaxDepth)
        {
            throw new CalcException(CalcErrorKind.ExpressionTooDeep);
        }
    }

    private void ExitNesting() => _nesting--;

    private void ExpectRightParen()
    {
        if (_position >= _tokens.Count)
        {
            throw new CalcException(CalcErrorKind.UnbalancedParentheses);
        }

        if (_tokens[_position] is not RightParenToken)
        {
            throw new CalcException(CalcErrorKind.UnexpectedToken);
        }

        _position++;
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
