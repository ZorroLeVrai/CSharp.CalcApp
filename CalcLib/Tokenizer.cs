using CalcLib.Tokens;
using System.Text.RegularExpressions;

namespace CalcLib;

internal class Tokenizer
{
    private readonly Regex _regex;

    public string Expression { get; }

    public Tokenizer(string expression)
    {
        Expression = expression;
        //declare all possible expressions
        _regex = new Regex(@"\*\*|[-+*/^()]|\d*\.\d+|\d+");
    }

    /// <summary>
    /// Get a token list from a string expression
    /// </summary>
    /// <returns>A token list</returns>
    /// <exception cref="Exception">Bad format</exception>
    public TokenizedExpression Tokenize()
    {
        var tokenExp = new TokenizedExpression();

        foreach (Match match in _regex.Matches(Expression))
        {
            tokenExp.AddToken(getToken(match));
        }

        return tokenExp;

        ICalcToken getToken(Match match)
        {
            var currentMatch = match.Value;
            if (null == currentMatch)
                return new UnvalidToken();

            switch (currentMatch)
            {
                case "+": return new Plus();
                case "-": return new Minus();
                case "*": return new Multiply();
                case "/": return new Divide();
                case "^": return new Power();
                case "**": return new Power();
                case "(": return new OpenParenthesis();
                case ")": return new CloseParenthesis();
                default:
                    if (double.TryParse(currentMatch, out double number))
                    {
                        return new NumberToken(number);
                    }
                    else
                    {
                        throw new Exception($"Bad Value for a number {currentMatch}");
                    }
            }
        }
    }
}
