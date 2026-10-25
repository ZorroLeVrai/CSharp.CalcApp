using System.Globalization;

namespace Calc.Lib.Tests;

public class CalculatorTests
{
    // Decimal digits compared when asserting floating-point results.
    private const int Precision = 10;

    [Theory]
    [InlineData("2+3", 5)]
    [InlineData("6*2", 12)]
    [InlineData("(2+3)*4", 20)]
    [InlineData("2^3^2", 512)]
    [InlineData("2+3*4-5/5", 13)]
    [InlineData("7", 7)]
    [InlineData("10-4-3", 3)]
    [InlineData("100/10/5", 2)]
    [InlineData("2*3^2", 18)]
    [InlineData("(2+3)*(4-1)", 15)]
    [InlineData("2*(3+(4-1))", 12)]
    [InlineData("((((5))))", 5)]
    [InlineData("1.5*2", 3)]
    [InlineData("0.5+0.25", 0.75)]
    public void Evaluate_ReturnsTheResult(string input, double expected)
    {
        Assert.Equal(expected, Calculator.Evaluate(input));
    }

    [Theory]
    [InlineData("-5", -5)]
    [InlineData("--5", 5)]
    [InlineData("2*-3", -6)]
    [InlineData("2--3", 5)]
    [InlineData("-(2+3)", -5)]
    [InlineData("-(2+3)*4", -20)]
    [InlineData("-2^2", -4)]
    [InlineData("(-2)^2", 4)]
    [InlineData("2^-1", 0.5)]
    [InlineData("10/-4", -2.5)]
    [InlineData("- 5", -5)]
    [InlineData("1 - -1", 2)]
    public void Evaluate_UnaryMinus(string input, double expected)
    {
        Assert.Equal(expected, Calculator.Evaluate(input), Precision);
    }

    [Theory]
    [InlineData("7%3", 1)]
    [InlineData("-7%3", -1)]
    [InlineData("7%-3", 1)]
    [InlineData("10%4%3", 2)]
    [InlineData("2+7%4", 5)]
    [InlineData("2*7%4", 2)]
    [InlineData("(2+7)%4", 1)]
    [InlineData("2^3%5", 3)]
    [InlineData("5.5%2", 1.5)]
    [InlineData("7 % 3", 1)]
    [InlineData("6%3", 0)]
    public void Evaluate_Modulo(string input, double expected)
    {
        Assert.Equal(expected, Calculator.Evaluate(input), Precision);
    }

    [Theory]
    [InlineData("5%0", CalcErrorKind.DivisionByZero)]
    [InlineData("5%(2-2)", CalcErrorKind.DivisionByZero)]
    [InlineData("5%", CalcErrorKind.UnexpectedEnd)]
    [InlineData("%5", CalcErrorKind.UnexpectedToken)]
    [InlineData("5%%2", CalcErrorKind.UnexpectedToken)]
    [InlineData("5%*2", CalcErrorKind.UnexpectedToken)]
    [InlineData("(5%)", CalcErrorKind.UnexpectedToken)]
    public void Evaluate_ModuloErrors(string input, CalcErrorKind expected)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(expected, ex.Kind);
    }

    [Fact]
    public void Evaluate_ModuloOverflowingOperand_IsADomainError()
    {
        // 10^400 is infinity, and infinity % 3 is not a number
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate("10^400%3"));

        Assert.Equal(CalcErrorKind.DomainError, ex.Kind);
    }

    [Fact]
    public void Evaluate_NegativeBaseWrittenWithUnaryMinus_IsReal()
    {
        Assert.Equal(-2, Calculator.Evaluate("(-8)^(1/3)"), Precision);
        Assert.Equal(4, Calculator.Evaluate("(-8)^(2/3)"), Precision);
        Assert.Equal(-4, Calculator.Evaluate("-8^(2/3)"), Precision);
    }

    [Theory]
    [InlineData("-", CalcErrorKind.UnexpectedEnd)]
    [InlineData("--", CalcErrorKind.UnexpectedEnd)]
    [InlineData("2*-", CalcErrorKind.UnexpectedEnd)]
    [InlineData("-*2", CalcErrorKind.UnexpectedToken)]
    [InlineData("(-)", CalcErrorKind.UnexpectedToken)]
    [InlineData("+5", CalcErrorKind.UnexpectedToken)]
    [InlineData("2*+3", CalcErrorKind.UnexpectedToken)]
    [InlineData("-(2", CalcErrorKind.UnbalancedParentheses)]
    [InlineData("1/-0", CalcErrorKind.DivisionByZero)]
    [InlineData("(-4)^(1/2)", CalcErrorKind.DomainError)]
    public void Evaluate_UnaryMinusErrors(string input, CalcErrorKind expected)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(expected, ex.Kind);
    }

    [Fact]
    public void Evaluate_ThousandAndTwentyFourNegations_Work()
    {
        var input = new string('-', 1024) + "5";

        Assert.Equal(5, Calculator.Evaluate(input));
    }

    [Fact]
    public void Evaluate_ThousandAndTwentyFiveNegations_AreTooDeep()
    {
        var input = new string('-', 1025) + "5";

        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Evaluate_MillionNegations_AreRejectedInsteadOfCrashing()
    {
        var input = new string('-', 1_000_000) + "5";

        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Theory]
    [InlineData(" 2 + 3 ")]
    [InlineData("\t2\n+\r3")]
    [InlineData("2+3")]
    public void Evaluate_IgnoresWhitespace(string input)
    {
        Assert.Equal(5, Calculator.Evaluate(input));
    }

    [Fact]
    public void Evaluate_NegativeBaseWithOddRoot_IsReal()
    {
        Assert.Equal(-2, Calculator.Evaluate("(0-8)^(1/3)"), Precision);
        Assert.Equal(4, Calculator.Evaluate("(0-8)^(2/3)"), Precision);
    }

    [Fact]
    public void Evaluate_Overflow_GivesInfinity()
    {
        Assert.True(double.IsPositiveInfinity(Calculator.Evaluate("10^400")));
    }

    [Fact]
    public void Evaluate_IsCultureIndependent()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            Assert.Equal(2.5, Calculator.Evaluate("1.5+1"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("2 $ 3", CalcErrorKind.UnexpectedCharacter)]
    [InlineData("2+", CalcErrorKind.UnexpectedEnd)]
    [InlineData("(2", CalcErrorKind.UnbalancedParentheses)]
    [InlineData("2)", CalcErrorKind.UnbalancedParentheses)]
    [InlineData("1/0", CalcErrorKind.DivisionByZero)]
    [InlineData("1/(2-2)", CalcErrorKind.DivisionByZero)]
    [InlineData("1.2.3", CalcErrorKind.InvalidNumber)]
    [InlineData("2 3", CalcErrorKind.UnexpectedToken)]
    [InlineData("()", CalcErrorKind.UnexpectedToken)]
    [InlineData("*2", CalcErrorKind.UnexpectedToken)]
    [InlineData("2(3)", CalcErrorKind.UnexpectedToken)]
    [InlineData("(0-4)^(1/2)", CalcErrorKind.DomainError)]
    [InlineData("0^(0-1)", CalcErrorKind.DivisionByZero)]
    public void Evaluate_ErrorsFromEveryPhaseSurfaceAsCalcException(string input, CalcErrorKind expected)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(expected, ex.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Evaluate_EmptyInput_ThrowsCalcException(string input)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Evaluate_NullInput_IsTreatedAsEmptyInput()
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(null!));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Evaluate_ErrorMessagesAreReadable()
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate("2 $ 3"));

        Assert.Contains("'$'", ex.Message);
    }

    [Fact]
    public void Evaluate_ChainOf1024Operators_Works()
    {
        var input = string.Join("+", Enumerable.Repeat("1", 1025));

        Assert.Equal(1025, Calculator.Evaluate(input));
    }

    [Theory]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    [InlineData("^")]
    public void Evaluate_ChainOf1025Operators_IsTooDeep(string op)
    {
        var input = string.Join(op, Enumerable.Repeat("1", 1026));

        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Theory]
    [InlineData("+")]
    [InlineData("^")]
    public void Evaluate_MillionTermExpression_IsRejectedInsteadOfCrashing(string op)
    {
        var input = string.Join(op, Enumerable.Repeat("1", 1_000_000));

        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Evaluate_HugeParenthesisNesting_IsRejectedInsteadOfCrashing()
    {
        var input = new string('(', 200_000) + "1" + new string(')', 200_000);

        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Evaluate_ParenthesesNestedUpToTheLimit_Work()
    {
        var input = new string('(', 1024) + "1" + new string(')', 1024);

        Assert.Equal(1, Calculator.Evaluate(input));
    }

    [Fact]
    public void Evaluate_RandomGarbage_OnlyEverThrowsCalcException()
    {
        const string alphabet = "0123456789.+-*/^()  \t$a,e";
        var random = new Random(12345);

        for (var i = 0; i < 20_000; i++)
        {
            var length = random.Next(0, 24);
            var chars = new char[length];
            for (var j = 0; j < length; j++)
            {
                chars[j] = alphabet[random.Next(alphabet.Length)];
            }

            var input = new string(chars);

            try
            {
                Calculator.Evaluate(input);
            }
            catch (CalcException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"'{input}' threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
