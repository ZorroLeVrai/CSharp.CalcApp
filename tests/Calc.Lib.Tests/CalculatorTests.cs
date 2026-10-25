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

    [Theory]
    [InlineData("pi", Math.PI)]
    [InlineData("e", Math.E)]
    [InlineData("2*pi", 2 * Math.PI)]
    [InlineData("-pi", -Math.PI)]
    [InlineData("pi/2", Math.PI / 2)]
    [InlineData("(pi+e)/2", (Math.PI + Math.E) / 2)]
    [InlineData("e^2", Math.E * Math.E)]
    [InlineData("  pi  ", Math.PI)]
    [InlineData("pi-pi", 0)]
    public void Evaluate_Constants(string input, double expected)
    {
        Assert.Equal(expected, Calculator.Evaluate(input), Precision);
    }

    [Theory]
    [InlineData("2.5e5", 250000)]
    [InlineData("1e3+1", 1001)]
    [InlineData("-2e3", -2000)]
    [InlineData("2e-3*1000", 2)]
    [InlineData("1e3^2", 1000000)]
    [InlineData("2^1e1", 1024)]
    [InlineData("sqrt(1e4)", 100)]
    [InlineData("1e2%7", 2)]
    [InlineData("1e3*e", 1000 * Math.E)]
    [InlineData("e^2", Math.E * Math.E)]
    public void Evaluate_ScientificNotation(string input, double expected)
    {
        Assert.Equal(expected, Calculator.Evaluate(input), Precision);
    }

    [Fact]
    public void Evaluate_ScientificNotation_OverflowAndUnderflow()
    {
        Assert.Equal(double.PositiveInfinity, Calculator.Evaluate("1e400"));
        Assert.Equal(0, Calculator.Evaluate("1e-400"));
        Assert.Equal(CalcErrorKind.DivisionByZero, Assert.Throws<CalcException>(() => Calculator.Evaluate("1/1e-400")).Kind);
        Assert.Equal(CalcErrorKind.DomainError, Assert.Throws<CalcException>(() => Calculator.Evaluate("0*1e400")).Kind);
    }

    [Theory]
    [InlineData("2e")]
    [InlineData("2e+")]
    [InlineData("2exp(1)")]
    [InlineData("2e1.5")]
    [InlineData("1.2.3e4")]
    public void Evaluate_MalformedScientificNotation_IsInvalidNumber(string input)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.InvalidNumber, ex.Kind);
    }

    [Fact]
    public void Evaluate_PiIsApproximately3Point14159()
    {
        Assert.Equal(3.14159, Calculator.Evaluate("pi"), 5);
    }

    [Theory]
    [InlineData("x", "x")]
    [InlineData("PI", "PI")]
    [InlineData("Pi", "Pi")]
    [InlineData("E", "E")]
    [InlineData("pie", "pie")]
    [InlineData("1+foo", "foo")]
    [InlineData("2*(3+bar)", "bar")]
    [InlineData("1/0+foo", "foo")]
    public void Evaluate_UnknownNameIsReportedWithItsName(string input, string name)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.UnknownIdentifier, ex.Kind);
        Assert.Contains($"'{name}'", ex.Message);
    }

    [Theory]
    [InlineData("2pi", CalcErrorKind.UnexpectedToken)]
    [InlineData("pi pi", CalcErrorKind.UnexpectedToken)]
    [InlineData("pi e", CalcErrorKind.UnexpectedToken)]
    [InlineData("2 e5", CalcErrorKind.UnexpectedToken)]
    [InlineData("2E5", CalcErrorKind.UnexpectedToken)]
    [InlineData("2e5x", CalcErrorKind.UnexpectedToken)]
    [InlineData("pi+", CalcErrorKind.UnexpectedEnd)]
    [InlineData("pi(2)", CalcErrorKind.UnexpectedToken)]
    [InlineData("pi$", CalcErrorKind.UnexpectedCharacter)]
    public void Evaluate_ConstantErrors(string input, CalcErrorKind expected)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(expected, ex.Kind);
    }

    [Theory]
    [InlineData("sqrt(16)", 4)]
    [InlineData("abs(-3)", 3)]
    [InlineData("exp(0)", 1)]
    [InlineData("ln(e)", 1)]
    [InlineData("log(1000)", 3)]
    [InlineData("log2(8)", 3)]
    [InlineData("log2(1024)", 10)]
    [InlineData("  log2 ( 8 )  ", 3)]
    [InlineData("sin(0)", 0)]
    [InlineData("cos(0)", 1)]
    [InlineData("tan(0)", 0)]
    [InlineData("asin(1)", Math.PI / 2)]
    [InlineData("acos(1)", 0)]
    [InlineData("atan(1)", Math.PI / 4)]
    [InlineData("floor(2.7)", 2)]
    [InlineData("ceil(2.1)", 3)]
    [InlineData("round(2.5)", 3)]
    [InlineData("sqrt(2+2)*3", 6)]
    [InlineData("sin(pi/2)", 1)]
    [InlineData("  sqrt (  16  )  ", 4)]
    [InlineData("sqrt\t(16)", 4)]
    [InlineData("-sqrt(16)", -4)]
    [InlineData("2^sqrt(4)", 4)]
    [InlineData("sqrt(abs(-16))", 4)]
    public void Evaluate_Functions(string input, double expected)
    {
        Assert.Equal(expected, Calculator.Evaluate(input), Precision);
    }

    [Theory]
    [InlineData("foo(2)", "foo")]
    [InlineData("Sqrt(4)", "Sqrt")]
    [InlineData("SQRT(4)", "SQRT")]
    [InlineData("sqr(4)", "sqr")]
    [InlineData("Log2(8)", "Log2")]
    [InlineData("log3(8)", "log3")]
    [InlineData("log22(8)", "log22")]
    [InlineData("1+bar(2)", "bar")]
    public void Evaluate_UnknownFunctionIsReportedWithItsName(string input, string name)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.UnknownIdentifier, ex.Kind);
        Assert.Contains($"'{name}'", ex.Message);
    }

    [Theory]
    [InlineData("sqrt", CalcErrorKind.UnexpectedEnd)]
    [InlineData("sqrt 4", CalcErrorKind.UnexpectedToken)]
    [InlineData("sqrt+1", CalcErrorKind.UnexpectedToken)]
    [InlineData("sqrt()", CalcErrorKind.UnexpectedToken)]
    [InlineData("sqrt(", CalcErrorKind.UnexpectedEnd)]
    [InlineData("sqrt(4", CalcErrorKind.UnbalancedParentheses)]
    [InlineData("sqrt(4))", CalcErrorKind.UnbalancedParentheses)]
    [InlineData("sqrt(4+)", CalcErrorKind.UnexpectedToken)]
    [InlineData("sqrt(1,2)", CalcErrorKind.UnexpectedCharacter)]
    [InlineData("sqrt(1 2)", CalcErrorKind.UnexpectedToken)]
    [InlineData("2sqrt(4)", CalcErrorKind.UnexpectedToken)]
    [InlineData("sqrt(4)2", CalcErrorKind.UnexpectedToken)]
    [InlineData("sqrt(4)sqrt(4)", CalcErrorKind.UnexpectedToken)]
    [InlineData("pi(2)", CalcErrorKind.UnexpectedToken)]
    [InlineData("e(2)", CalcErrorKind.UnexpectedToken)]
    [InlineData("sqrt(-1)", CalcErrorKind.DomainError)]
    [InlineData("ln(0)", CalcErrorKind.DomainError)]
    [InlineData("asin(2)", CalcErrorKind.DomainError)]
    [InlineData("log2(0)", CalcErrorKind.DomainError)]
    [InlineData("log2(-8)", CalcErrorKind.DomainError)]
    [InlineData("log2", CalcErrorKind.UnexpectedEnd)]
    [InlineData("log2()", CalcErrorKind.UnexpectedToken)]
    [InlineData("log2 8", CalcErrorKind.UnexpectedToken)]
    [InlineData("log 2", CalcErrorKind.UnexpectedToken)]
    [InlineData("log2(8", CalcErrorKind.UnbalancedParentheses)]
    [InlineData("log2(1,2)", CalcErrorKind.UnexpectedCharacter)]
    [InlineData("2log2(8)", CalcErrorKind.UnexpectedToken)]
    [InlineData("sqrt(1/0)", CalcErrorKind.DivisionByZero)]
    public void Evaluate_FunctionErrors(string input, CalcErrorKind expected)
    {
        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(expected, ex.Kind);
    }

    [Fact]
    public void Evaluate_ExpOverflow_GivesInfinity()
    {
        Assert.True(double.IsPositiveInfinity(Calculator.Evaluate("exp(1000)")));
    }

    [Fact]
    public void Evaluate_NestedCallsUpToTheLimit_Work()
    {
        var input = string.Concat(Enumerable.Repeat("abs(", TestConstants.MaxDepthLimit)) + "5" + new string(')', TestConstants.MaxDepthLimit);

        Assert.Equal(5, Calculator.Evaluate(input));
    }

    [Fact]
    public void Evaluate_NestedCallsBeyondTheLimit_AreTooDeep()
    {
        var input = string.Concat(Enumerable.Repeat("abs(", TestConstants.MaxDepthLimit + 1)) + "5" + new string(')', TestConstants.MaxDepthLimit + 1);

        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
    }

    [Fact]
    public void Evaluate_HugeNumberOfNestedCalls_IsRejectedInsteadOfCrashing()
    {
        var input = string.Concat(Enumerable.Repeat("abs(", 200_000)) + "5" + new string(')', 200_000);

        var ex = Assert.Throws<CalcException>(() => Calculator.Evaluate(input));

        Assert.Equal(CalcErrorKind.ExpressionTooDeep, ex.Kind);
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
    public void Evaluate_NegationsAtTheDepthLimit_Work()
    {
        var input = new string('-', TestConstants.MaxDepthLimit) + "5";

        Assert.Equal(5, Calculator.Evaluate(input));
    }

    [Fact]
    public void Evaluate_NegationsOneOverTheDepthLimit_AreTooDeep()
    {
        var input = new string('-', TestConstants.MaxDepthLimit + 1) + "5";

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
    public void Evaluate_ChainAtTheDepthLimit_Works()
    {
        var input = string.Join("+", Enumerable.Repeat("1", TestConstants.MaxDepthLimit + 1));

        Assert.Equal(TestConstants.MaxDepthLimit + 1, Calculator.Evaluate(input));
    }

    [Theory]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    [InlineData("^")]
    public void Evaluate_ChainOneOverTheDepthLimit_IsTooDeep(string op)
    {
        var input = string.Join(op, Enumerable.Repeat("1", TestConstants.MaxDepthLimit + 2));

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
        var input = new string('(', TestConstants.MaxDepthLimit) + "1" + new string(')', TestConstants.MaxDepthLimit);

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
