using CalcLib.Tokens;
using System.Text.RegularExpressions;

namespace CalcLib;

internal static class TokenParser
{
    private const string INTEGER_EXP = @"\d+";
    private const string DECIMAL_EXP = @"\d*\.\d+";
    private const string OPERATORS_EXP = "[-+*/^()]";
    private const string POWER_EXP = @"\*\*";

    private static readonly Regex _regex;
    private static readonly List<Type> _operatorTokenTypes;

    static TokenParser()
    {
        var regexList = new List<string> { DECIMAL_EXP, INTEGER_EXP, POWER_EXP, OPERATORS_EXP };
        var regexStr = string.Join("|", regexList);
        _regex = new Regex(regexStr);
        _operatorTokenTypes = new List<Type>{ typeof(Plus), typeof(Minus), typeof(Multiply), typeof(Divide), typeof(Power) };
    }

    public static IEnumerable<ICalcToken> GetTokens(string input)
    {
        ICalcToken previousToken = new EmptyToken();

        foreach (Match match in _regex.Matches(input))
        {
            var currentToken = getRefinedToken(match);
            previousToken = currentToken;
            yield return currentToken;
        }

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

        bool isOperator(ICalcToken token)
        {
            return _operatorTokenTypes.Contains(token.GetType());
        }

        //Modify some tokens like minus
        ICalcToken getRefinedToken(Match match)
        {
            var currentToken = getToken(match);

            if (currentToken is Minus)
            {
                if (isOperator(previousToken) || previousToken is OpenParenthesis || previousToken is EmptyToken)
                {
                    return new Negative();
                }
            }

            return currentToken;
        }
    }
}