using CalcLib.Tokens;
using System.Text.RegularExpressions;

namespace CalcLib;

internal class Tokenizer
{
    public string Expression { get; }

    public Tokenizer(string expression)
    {
        Expression = expression;
    }

    /// <summary>
    /// Get a token list from a string expression
    /// </summary>
    /// <returns>A token list</returns>
    /// <exception cref="Exception">Bad format</exception>
    public TokenizedExpression Tokenize()
    {
        return new TokenizedExpression(TokenParser.GetTokens(Expression));
    }
}
