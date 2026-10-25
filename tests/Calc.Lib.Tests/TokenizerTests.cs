using System.Globalization;

namespace Calc.Lib.Tests;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_NumberOperatorNumber()
    {
        var tokens = Tokenizer.Tokenize("6*2");

        Assert.Equal(
            new Token[] { new NumberToken(6.0), new OperatorToken(Operator.Mul), new NumberToken(2.0) },
            tokens);
    }

    [Theory]
    [InlineData("+", Operator.Add)]
    [InlineData("-", Operator.Sub)]
    [InlineData("*", Operator.Mul)]
    [InlineData("/", Operator.Div)]
    [InlineData("^", Operator.Pow)]
    [InlineData("%", Operator.Mod)]
    public void Tokenize_RecognisesEachOperator(string input, Operator expected)
    {
        var tokens = Tokenizer.Tokenize(input);

        Assert.Equal(new Token[] { new OperatorToken(expected) }, tokens);
    }

    [Theory]
    [InlineData("123", 123.0)]
    [InlineData("1.5", 1.5)]
    [InlineData("0.25", 0.25)]
    [InlineData(".5", 0.5)]
    [InlineData("007", 7.0)]
    public void Tokenize_ReadsIntegersAndDecimals(string input, double expected)
    {
        var tokens = Tokenizer.Tokenize(input);

        Assert.Equal(new Token[] { new NumberToken(expected) }, tokens);
    }

    [Fact]
    public void Tokenize_IgnoresWhitespace()
    {
        Assert.Equal(Tokenizer.Tokenize("6*2"), Tokenizer.Tokenize(" 6 *  2 "));
        Assert.Equal(Tokenizer.Tokenize("6*2"), Tokenizer.Tokenize("\t6\n*\r2"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t \n")]
    public void Tokenize_EmptyOrWhitespaceInput_GivesNoTokens(string input)
    {
        Assert.Empty(Tokenizer.Tokenize(input));
    }

    [Fact]
    public void Tokenize_LongerExpression()
    {
        var tokens = Tokenizer.Tokenize("2+3.5*4-1/2^3");

        Assert.Equal(
            new Token[]
            {
                new NumberToken(2), new OperatorToken(Operator.Add),
                new NumberToken(3.5), new OperatorToken(Operator.Mul),
                new NumberToken(4), new OperatorToken(Operator.Sub),
                new NumberToken(1), new OperatorToken(Operator.Div),
                new NumberToken(2), new OperatorToken(Operator.Pow),
                new NumberToken(3),
            },
            tokens);
    }

    [Theory]
    [InlineData("2 $ 3", '$')]
    [InlineData("a", 'a')]
    [InlineData("2 # 3", '#')]
    public void Tokenize_InvalidCharacter_Throws(string input, char bad)
    {
        var ex = Assert.Throws<CalcException>(() => Tokenizer.Tokenize(input));

        Assert.Equal(CalcErrorKind.UnexpectedCharacter, ex.Kind);
        Assert.Contains($"'{bad}'", ex.Message);
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData(".")]
    [InlineData("2 + 1..5")]
    public void Tokenize_MalformedNumber_Throws(string input)
    {
        var ex = Assert.Throws<CalcException>(() => Tokenizer.Tokenize(input));

        Assert.Equal(CalcErrorKind.InvalidNumber, ex.Kind);
    }

    [Fact]
    public void Tokenize_MalformedNumber_MessageNamesTheNumber()
    {
        var ex = Assert.Throws<CalcException>(() => Tokenizer.Tokenize("1.2.3"));

        Assert.Contains("1.2.3", ex.Message);
    }

    [Fact]
    public void Tokenize_Modulo()
    {
        var tokens = Tokenizer.Tokenize("7 % 3");

        Assert.Equal(
            new Token[] { new NumberToken(7), new OperatorToken(Operator.Mod), new NumberToken(3) },
            tokens);
    }

    [Fact]
    public void Tokenize_ModuloWithoutSpaces_AndDecimals()
    {
        var tokens = Tokenizer.Tokenize("5.5%2");

        Assert.Equal(
            new Token[] { new NumberToken(5.5), new OperatorToken(Operator.Mod), new NumberToken(2) },
            tokens);
    }

    [Fact]
    public void Tokenize_Parentheses()
    {
        var tokens = Tokenizer.Tokenize("(2+3)*4");

        Assert.Equal(
            new Token[]
            {
                new LeftParenToken(),
                new NumberToken(2), new OperatorToken(Operator.Add), new NumberToken(3),
                new RightParenToken(),
                new OperatorToken(Operator.Mul), new NumberToken(4),
            },
            tokens);
    }

    [Fact]
    public void Tokenize_NestedParentheses()
    {
        var tokens = Tokenizer.Tokenize("((1))");

        Assert.Equal(
            new Token[]
            {
                new LeftParenToken(), new LeftParenToken(),
                new NumberToken(1),
                new RightParenToken(), new RightParenToken(),
            },
            tokens);
    }

    [Fact]
    public void Tokenize_IgnoresWhitespaceAroundParentheses()
    {
        Assert.Equal(Tokenizer.Tokenize("(2+3)*4"), Tokenizer.Tokenize(" ( 2 + 3 ) * 4 "));
    }

    [Theory]
    [InlineData("(", 1, 0)]
    [InlineData(")", 0, 1)]
    [InlineData("(()", 2, 1)]
    [InlineData("())", 1, 2)]
    public void Tokenize_DoesNotCheckBalance(string input, int open, int close)
    {
        var tokens = Tokenizer.Tokenize(input);

        Assert.Equal(open, tokens.OfType<LeftParenToken>().Count());
        Assert.Equal(close, tokens.OfType<RightParenToken>().Count());
    }

    [Fact]
    public void Tokenize_EmptyParentheses_GiveTwoTokens()
    {
        Assert.Equal(
            new Token[] { new LeftParenToken(), new RightParenToken() },
            Tokenizer.Tokenize("()"));
    }

    [Fact]
    public void Tokenize_IsCultureIndependent()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            var tokens = Tokenizer.Tokenize("1.5");

            Assert.Equal(new Token[] { new NumberToken(1.5) }, tokens);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Tokenize_HugeNumber_DoesNotThrowUnexpectedException()
    {
        var huge = new string('9', 400);

        var tokens = Tokenizer.Tokenize(huge);

        var token = Assert.IsType<NumberToken>(Assert.Single(tokens));
        Assert.True(double.IsPositiveInfinity(token.Value) || token.Value > 1e300);
    }
}
