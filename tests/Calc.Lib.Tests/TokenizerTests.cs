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
    [InlineData("@", '@')]
    [InlineData("_", '_')]
    [InlineData("é", 'é')]
    [InlineData("pi$", '$')]
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

    [Theory]
    [InlineData("pi")]
    [InlineData("e")]
    [InlineData("sqrt")]
    [InlineData("PI")]
    [InlineData("Pi")]
    [InlineData("x1")]
    [InlineData("abc123")]
    [InlineData("log2")]
    [InlineData("e5")]
    public void Tokenize_ReadsAnIdentifier(string name)
    {
        var tokens = Tokenizer.Tokenize(name);

        Assert.Equal(new Token[] { new IdentifierToken(name) }, tokens);
    }

    [Fact]
    public void Tokenize_IdentifierInsideAnExpression()
    {
        var tokens = Tokenizer.Tokenize("2*pi");

        Assert.Equal(
            new Token[] { new NumberToken(2), new OperatorToken(Operator.Mul), new IdentifierToken("pi") },
            tokens);
    }

    [Fact]
    public void Tokenize_TwoConstants()
    {
        var tokens = Tokenizer.Tokenize("pi+e");

        Assert.Equal(
            new Token[] { new IdentifierToken("pi"), new OperatorToken(Operator.Add), new IdentifierToken("e") },
            tokens);
    }

    [Fact]
    public void Tokenize_WhitespaceSeparatesIdentifiers()
    {
        var tokens = Tokenizer.Tokenize("p i");

        Assert.Equal(new Token[] { new IdentifierToken("p"), new IdentifierToken("i") }, tokens);
    }

    [Fact]
    public void Tokenize_NumberFollowedByLetters_GivesANumberThenAnIdentifier()
    {
        // 2pi, 2 e5 and 2E5: no implicit multiplication, the parser reports them
        Assert.Equal(
            new Token[] { new NumberToken(2), new IdentifierToken("pi") },
            Tokenizer.Tokenize("2pi"));
        Assert.Equal(
            new Token[] { new NumberToken(2), new IdentifierToken("e5") },
            Tokenizer.Tokenize("2 e5"));
        Assert.Equal(
            new Token[] { new NumberToken(2), new IdentifierToken("E5") },
            Tokenizer.Tokenize("2E5"));
    }

    [Theory]
    [InlineData("2.5e5", 250000.0)]
    [InlineData("2e5", 200000.0)]
    [InlineData("2.5e-3", 0.0025)]
    [InlineData("2.5e+3", 2500.0)]
    [InlineData(".5e3", 500.0)]
    [InlineData("007e2", 700.0)]
    [InlineData("2e0", 2.0)]
    [InlineData("0e5", 0.0)]
    [InlineData("1e-0", 1.0)]
    [InlineData("2e007", 2e7)]
    [InlineData("1.1e-5", 1.1e-5)]
    [InlineData("123456789e-9", 123456789e-9)]
    public void Tokenize_ScientificNotation_IsOneNumberToken(string input, double expected)
    {
        Assert.Equal(new Token[] { new NumberToken(expected) }, Tokenizer.Tokenize(input));
    }

    [Fact]
    public void Tokenize_ScientificNotation_InContext()
    {
        Assert.Equal(
            new Token[] { new NumberToken(1e3), new OperatorToken(Operator.Add), new NumberToken(1e-3) },
            Tokenizer.Tokenize("1e3+1e-3"));
        Assert.Equal(
            new Token[] { new NumberToken(2.5e5), new OperatorToken(Operator.Add), new NumberToken(1) },
            Tokenizer.Tokenize(" 2.5e5 + 1 "));
        Assert.Equal(
            new Token[] { new LeftParenToken(), new NumberToken(2e3), new RightParenToken() },
            Tokenizer.Tokenize("(2e3)"));
    }

    [Fact]
    public void Tokenize_TheConstantEIsUntouched()
    {
        Assert.Equal(new Token[] { new IdentifierToken("e") }, Tokenizer.Tokenize("e"));
        Assert.Equal(new Token[] { new IdentifierToken("e5") }, Tokenizer.Tokenize("e5"));
        Assert.Equal(new Token[] { new IdentifierToken("x1e5") }, Tokenizer.Tokenize("x1e5"));
        Assert.Equal(
            new Token[] { new IdentifierToken("e"), new OperatorToken(Operator.Add), new NumberToken(5) },
            Tokenizer.Tokenize("e+5"));
        Assert.Equal(
            new Token[] { new NumberToken(2), new OperatorToken(Operator.Mul), new IdentifierToken("e") },
            Tokenizer.Tokenize("2*e"));
    }

    [Theory]
    [InlineData("2e", "2e")]
    [InlineData("2e+", "2e+")]
    [InlineData("2e-", "2e-")]
    [InlineData("2e*3", "2e")]
    [InlineData("2ee5", "2e")]
    [InlineData("2exp(1)", "2e")]
    [InlineData("2e1.5", "2e1.5")]
    [InlineData("2e.5", "2e.5")]
    [InlineData("2e5.", "2e5.")]
    [InlineData("2e+-5", "2e+")]
    [InlineData("2e--5", "2e-")]
    [InlineData("1.2.3e4", "1.2.3e4")]
    public void Tokenize_MalformedScientificNotation_IsInvalidNumber(string input, string literal)
    {
        var ex = Assert.Throws<CalcException>(() => Tokenizer.Tokenize(input));

        Assert.Equal(CalcErrorKind.InvalidNumber, ex.Kind);
        Assert.Contains($"'{literal}'", ex.Message);
    }

    [Fact]
    public void Tokenize_ScientificNotation_OverflowAndUnderflow()
    {
        Assert.Equal(new Token[] { new NumberToken(double.PositiveInfinity) }, Tokenizer.Tokenize("1e400"));
        Assert.Equal(new Token[] { new NumberToken(0) }, Tokenizer.Tokenize("1e-400"));
    }

    [Fact]
    public void Tokenize_HugeExponent_OnlyThrowsCalcException()
    {
        var exception = Record.Exception(() => Tokenizer.Tokenize("1e" + new string('9', 10_000)));

        Assert.True(exception is null or CalcException);
    }

    [Fact]
    public void Tokenize_IdentifierFollowedByAParenthesis()
    {
        var tokens = Tokenizer.Tokenize("sqrt(4)");

        Assert.Equal(
            new Token[]
            {
                new IdentifierToken("sqrt"), new LeftParenToken(), new NumberToken(4), new RightParenToken(),
            },
            tokens);
    }

    [Fact]
    public void Tokenize_Log2Call_ReadsTheDigitAsPartOfTheName()
    {
        var tokens = Tokenizer.Tokenize("log2(8)");

        Assert.Equal(
            new Token[]
            {
                new IdentifierToken("log2"), new LeftParenToken(), new NumberToken(8), new RightParenToken(),
            },
            tokens);
    }

    [Fact]
    public void Tokenize_LogSpaceTwo_IsANameAndANumber()
    {
        var tokens = Tokenizer.Tokenize("log 2");

        Assert.Equal(new Token[] { new IdentifierToken("log"), new NumberToken(2) }, tokens);
    }

    [Fact]
    public void Tokenize_IdentifierStopsAtAnOperator()
    {
        var tokens = Tokenizer.Tokenize("pi^e");

        Assert.Equal(
            new Token[] { new IdentifierToken("pi"), new OperatorToken(Operator.Pow), new IdentifierToken("e") },
            tokens);
    }

    [Fact]
    public void Tokenize_IdentifiersAreAsciiOnly()
    {
        var ex = Assert.Throws<CalcException>(() => Tokenizer.Tokenize("pié"));

        Assert.Equal(CalcErrorKind.UnexpectedCharacter, ex.Kind);
        Assert.Contains("'é'", ex.Message);
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
