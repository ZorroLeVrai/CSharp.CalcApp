using System.Globalization;

namespace Calc.Lib;

internal static class Tokenizer
{
    public static IReadOnlyList<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        var charIndex = 0;

        while (charIndex < input.Length)
        {
            var c = input[charIndex];

            if (char.IsWhiteSpace(c))
            {
                charIndex++;
            }
            else if (IsNumberChar(c))
            {
                var start = charIndex;
                while (charIndex < input.Length && IsNumberChar(input[charIndex]))
                {
                    charIndex++;
                }

                if (charIndex < input.Length && input[charIndex] == 'e')
                {
                    charIndex++;
                    if (charIndex < input.Length && input[charIndex] is '+' or '-')
                    {
                        charIndex++;
                    }

                    var exponentStart = charIndex;
                    while (charIndex < input.Length && IsNumberChar(input[charIndex]))
                    {
                        charIndex++;
                    }

                    var exponent = input[exponentStart..charIndex];
                    if (exponent.Length == 0 || !exponent.All(char.IsAsciiDigit))
                    {
                        throw new CalcException(CalcErrorKind.InvalidNumber, input[start..charIndex]);
                    }
                }

                tokens.Add(new NumberToken(ParseNumber(input[start..charIndex])));
            }
            else if (char.IsAsciiLetter(c))
            {
                var start = charIndex;
                while (charIndex < input.Length && char.IsAsciiLetterOrDigit(input[charIndex]))
                {
                    charIndex++;
                }

                tokens.Add(new IdentifierToken(input[start..charIndex]));
            }
            else if (c == '(')
            {
                tokens.Add(new LeftParenToken());
                charIndex++;
            }
            else if (c == ')')
            {
                tokens.Add(new RightParenToken());
                charIndex++;
            }
            else if (TryGetOperator(c, out var op))
            {
                tokens.Add(new OperatorToken(op));
                charIndex++;
            }
            else
            {
                throw new CalcException(CalcErrorKind.UnexpectedCharacter, c.ToString());
            }
        }

        return tokens;
    }

    // ASCII digits only: char.IsDigit would also accept other Unicode digits.
    private static bool IsNumberChar(char c) => c is >= '0' and <= '9' or '.';

    private static double ParseNumber(string text)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new CalcException(CalcErrorKind.InvalidNumber, text);
        }

        return value;
    }

    private static bool TryGetOperator(char c, out Operator op)
    {
        switch (c)
        {
            case '+': op = Operator.Add; return true;
            case '-': op = Operator.Sub; return true;
            case '*': op = Operator.Mul; return true;
            case '/': op = Operator.Div; return true;
            case '^': op = Operator.Pow; return true;
            case '%': op = Operator.Mod; return true;
            default: op = default; return false;
        }
    }
}
