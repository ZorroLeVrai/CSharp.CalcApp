namespace Calc.Lib.Tests;

public class EvaluatorTests
{
    // Decimal digits compared when asserting floating-point results.
    private const int Precision = 10;

    private static NumberExpr N(double value) => new(value);

    private static BinaryExpr B(Operator op, Expr left, Expr right) => new(op, left, right);

    private static UnaryExpr Neg(Expr operand) => new(UnaryOperator.Neg, operand);

    private static double EvalSource(string input) => Evaluator.Evaluate(Parser.Parse(Tokenizer.Tokenize(input)));

    [Fact]
    public void Evaluate_Number()
    {
        Assert.Equal(5, Evaluator.Evaluate(N(5)));
    }

    [Fact]
    public void Evaluate_NegativeAndZeroNumbers()
    {
        Assert.Equal(-3.5, Evaluator.Evaluate(N(-3.5)));
        Assert.Equal(0, Evaluator.Evaluate(N(0)));
    }

    [Theory]
    [InlineData(Operator.Add, 6, 2, 8)]
    [InlineData(Operator.Sub, 7, 2, 5)]
    [InlineData(Operator.Sub, 2, 7, -5)]
    [InlineData(Operator.Mul, 6, 2, 12)]
    [InlineData(Operator.Mul, -3, 4, -12)]
    [InlineData(Operator.Div, 9, 3, 3)]
    [InlineData(Operator.Div, -9, 3, -3)]
    [InlineData(Operator.Pow, 2, 10, 1024)]
    [InlineData(Operator.Mod, 7, 3, 1)]
    [InlineData(Operator.Mod, 6, 3, 0)]
    [InlineData(Operator.Mod, 2, 5, 2)]
    public void Evaluate_EachOperator(Operator op, double left, double right, double expected)
    {
        Assert.Equal(expected, Evaluator.Evaluate(B(op, N(left), N(right))));
    }

    [Fact]
    public void Evaluate_NestedTree()
    {
        // (2+3)*4
        var tree = B(Operator.Mul, B(Operator.Add, N(2), N(3)), N(4));

        Assert.Equal(20, Evaluator.Evaluate(tree));
    }

    [Fact]
    public void Evaluate_DeeplyMixedTree()
    {
        // 2+3*4-5/5
        var tree = B(
            Operator.Sub,
            B(Operator.Add, N(2), B(Operator.Mul, N(3), N(4))),
            B(Operator.Div, N(5), N(5)));

        Assert.Equal(13, Evaluator.Evaluate(tree));
    }

    [Fact]
    public void Evaluate_Decimals()
    {
        Assert.Equal(0.25, Evaluator.Evaluate(B(Operator.Div, N(1), N(4))));
    }

    [Fact]
    public void Evaluate_FloatingPointResultsAreApproximate()
    {
        var result = Evaluator.Evaluate(B(Operator.Add, N(0.1), N(0.2)));

        Assert.Equal(0.3, result, Precision);
    }

    [Fact]
    public void Evaluate_PowerIsRightAssociativeWhenTheTreeSaysSo()
    {
        // 2^(3^2)
        var tree = B(Operator.Pow, N(2), B(Operator.Pow, N(3), N(2)));

        Assert.Equal(512, Evaluator.Evaluate(tree));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(5, -0.0)]
    public void Evaluate_DivisionByZero_Throws(double left, double right)
    {
        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(B(Operator.Div, N(left), N(right))));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    [Fact]
    public void Evaluate_NestedDivisionByZero_Throws()
    {
        // 1/(2-2)
        var tree = B(Operator.Div, N(1), B(Operator.Sub, N(2), N(2)));

        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(tree));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    [Fact]
    public void Evaluate_DivisionByZeroDeepInsideTheTree_Throws()
    {
        // 1+2*(3+4/0)
        var tree = B(
            Operator.Add,
            N(1),
            B(Operator.Mul, N(2), B(Operator.Add, N(3), B(Operator.Div, N(4), N(0)))));

        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(tree));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    // the remainder takes the sign of the left operand (C# `%`, not a mathematical modulo)
    [Theory]
    [InlineData(7, 3, 1)]
    [InlineData(-7, 3, -1)]
    [InlineData(7, -3, 1)]
    [InlineData(-7, -3, -1)]
    [InlineData(5.5, 2, 1.5)]
    [InlineData(-5.5, 2, -1.5)]
    [InlineData(1, 4, 1)]
    [InlineData(0, 5, 0)]
    [InlineData(10, 5, 0)]
    [InlineData(0.5, 0.25, 0)]
    [InlineData(5, double.PositiveInfinity, 5)]
    [InlineData(-5, double.PositiveInfinity, -5)]
    public void Evaluate_Modulo(double left, double right, double expected)
    {
        var result = Evaluator.Evaluate(B(Operator.Mod, N(left), N(right)));

        Assert.Equal(expected, result, Precision);
    }

    [Fact]
    public void Evaluate_ModuloOfDecimals_IsApproximate()
    {
        // 0.5 % 0.2 is 0.1 up to floating-point noise
        var result = Evaluator.Evaluate(B(Operator.Mod, N(0.5), N(0.2)));

        Assert.Equal(0.1, result, Precision);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(5, -0.0)]
    [InlineData(double.PositiveInfinity, 0)]
    public void Evaluate_ModuloByZero_IsDivisionByZero(double left, double right)
    {
        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(B(Operator.Mod, N(left), N(right))));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    [Fact]
    public void Evaluate_NestedModuloByZero_IsDivisionByZero()
    {
        // 5%(2-2)
        var tree = B(Operator.Mod, N(5), B(Operator.Sub, N(2), N(2)));

        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(tree));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    [Theory]
    [InlineData(double.PositiveInfinity, 3)]
    [InlineData(double.NegativeInfinity, 3)]
    public void Evaluate_ModuloOfInfinity_IsADomainError(double left, double right)
    {
        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(B(Operator.Mod, N(left), N(right))));

        Assert.Equal(CalcErrorKind.DomainError, ex.Kind);
    }

    [Theory]
    [InlineData("7%3", 1)]
    [InlineData("-7%3", -1)]
    [InlineData("7%-3", 1)]
    [InlineData("-7%-3", -1)]
    [InlineData("-(7%3)", -1)]
    [InlineData("(-7)%3", -1)]
    [InlineData("10%4%3", 2)]
    [InlineData("10/4%3", 2.5)]
    [InlineData("2*7%4", 2)]
    [InlineData("7%4*2", 6)]
    [InlineData("2+7%4", 5)]
    [InlineData("7%4-2", 1)]
    [InlineData("2^3%5", 3)]
    [InlineData("2%3^2", 2)]
    [InlineData("(2+7)%4", 1)]
    [InlineData("7 % 3", 1)]
    [InlineData("5.5%2", 1.5)]
    [InlineData("100%7%5", 2)]
    public void Evaluate_ModuloFromSource(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input), Precision);
    }

    [Theory]
    [InlineData("5%0")]
    [InlineData("-5%0")]
    [InlineData("0%0")]
    [InlineData("5%(3-3)")]
    [InlineData("5%-0")]
    [InlineData("1+5%0")]
    public void Evaluate_ModuloByZeroFromSource_Throws(string input)
    {
        var ex = Assert.Throws<CalcException>(() => EvalSource(input));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    [Fact]
    public void Evaluate_PowerWithFractionalExponent()
    {
        var result = Evaluator.Evaluate(B(Operator.Pow, N(2), N(0.5)));

        Assert.Equal(Math.Sqrt(2), result, Precision);
    }

    [Theory]
    [InlineData(2, -1, 0.5)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 2, 0)]
    [InlineData(-2, 3, -8)]
    [InlineData(-2, 2, 4)]
    [InlineData(9, 0.5, 3)]
    public void Evaluate_PowerSpecialCases(double baseValue, double exponent, double expected)
    {
        var result = Evaluator.Evaluate(B(Operator.Pow, N(baseValue), N(exponent)));

        Assert.Equal(expected, result, Precision);
    }

    [Fact]
    public void Evaluate_NegativeBaseWithOddRoot_FromDivisionTree_IsReal()
    {
        // (-8)^(1/3)
        var tree = B(Operator.Pow, N(-8), B(Operator.Div, N(1), N(3)));

        Assert.Equal(-2, Evaluator.Evaluate(tree), Precision);
    }

    // exponent is p/q in lowest terms: real when q is odd, sign is (-1)^p
    [Theory]
    [InlineData(-8, 1, 3, -2)]
    [InlineData(-8, 2, 3, 4)]
    [InlineData(-8, 4, 3, 16)]
    [InlineData(-8, 5, 3, -32)]
    [InlineData(-27, 1, 3, -3)]
    [InlineData(-32, 1, 5, -2)]
    [InlineData(-32, 2, 5, 4)]
    [InlineData(-32, 3, 5, -8)]
    [InlineData(-8, -1, 3, -0.5)]
    [InlineData(-8, -2, 3, 0.25)]
    [InlineData(-1, 1, 3, -1)]
    [InlineData(-1, 2, 3, 1)]
    public void Evaluate_NegativeBaseWithOddDenominator_IsReal(double baseValue, double p, double q, double expected)
    {
        var tree = B(Operator.Pow, N(baseValue), B(Operator.Div, N(p), N(q)));

        Assert.Equal(expected, Evaluator.Evaluate(tree), Precision);
    }

    // the fraction must be reduced first: 2/6 is 1/3 (odd denominator), 3/6 is 1/2 (even)
    [Theory]
    [InlineData(-8, 2, 6, -2)]
    [InlineData(-8, 4, 6, 4)]
    [InlineData(-1, 2, 6, -1)]
    [InlineData(-8, 3, 9, -2)]
    public void Evaluate_NegativeBaseWithUnreducedFraction_UsesTheReducedDenominator(
        double baseValue, double p, double q, double expected)
    {
        var tree = B(Operator.Pow, N(baseValue), B(Operator.Div, N(p), N(q)));

        Assert.Equal(expected, Evaluator.Evaluate(tree), Precision);
    }

    [Theory]
    [InlineData(-4, 1, 2)]
    [InlineData(-8, 1, 6)]
    [InlineData(-8, 3, 6)]
    [InlineData(-8, 2, 4)]
    [InlineData(-1, 1, 4)]
    [InlineData(-16, 3, 4)]
    [InlineData(-8, -1, 2)]
    public void Evaluate_NegativeBaseWithEvenDenominator_IsADomainError(double baseValue, double p, double q)
    {
        var tree = B(Operator.Pow, N(baseValue), B(Operator.Div, N(p), N(q)));

        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(tree));

        Assert.Equal(CalcErrorKind.DomainError, ex.Kind);
    }

    [Theory]
    [InlineData(-8, 0.5)]
    [InlineData(-8, 0.3333)]
    [InlineData(-8, 0.41421356237)]
    [InlineData(-2, Math.PI)]
    [InlineData(-8, 1e-300)]
    public void Evaluate_NegativeBaseWithExponentThatIsNotARecognisableFraction_IsADomainError(
        double baseValue, double exponent)
    {
        var ex = Assert.Throws<CalcException>(
            () => Evaluator.Evaluate(B(Operator.Pow, N(baseValue), N(exponent))));

        Assert.Equal(CalcErrorKind.DomainError, ex.Kind);
    }

    [Theory]
    [InlineData(8, 1, 3, 2)]
    [InlineData(8, 2, 3, 4)]
    [InlineData(0, 1, 3, 0)]
    [InlineData(0, 1, 2, 0)]
    public void Evaluate_NonNegativeBaseWithFractionalExponent_IsUnchanged(
        double baseValue, double p, double q, double expected)
    {
        var tree = B(Operator.Pow, N(baseValue), B(Operator.Div, N(p), N(q)));

        Assert.Equal(expected, Evaluator.Evaluate(tree), Precision);
    }

    [Fact]
    public void Evaluate_NegativeBaseWithHugeIntegerExponent_Overflows()
    {
        var result = Evaluator.Evaluate(B(Operator.Pow, N(-10), N(401)));

        Assert.True(double.IsNegativeInfinity(result));
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(0, -0.5)]
    [InlineData(-0.0, -2)]
    public void Evaluate_ZeroToANegativePower_IsDivisionByZero(double baseValue, double exponent)
    {
        var ex = Assert.Throws<CalcException>(
            () => Evaluator.Evaluate(B(Operator.Pow, N(baseValue), N(exponent))));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    [Fact]
    public void Evaluate_Overflow_GivesInfinityInsteadOfAnError()
    {
        Assert.True(double.IsPositiveInfinity(Evaluator.Evaluate(B(Operator.Mul, N(1e308), N(10)))));
        Assert.True(double.IsPositiveInfinity(Evaluator.Evaluate(B(Operator.Pow, N(10), N(400)))));
        Assert.True(double.IsNegativeInfinity(Evaluator.Evaluate(B(Operator.Mul, N(-1e308), N(10)))));
    }

    [Fact]
    public void Evaluate_NanResult_IsADomainError()
    {
        // infinity - infinity
        var tree = B(Operator.Sub, N(double.PositiveInfinity), N(double.PositiveInfinity));

        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(tree));

        Assert.Equal(CalcErrorKind.DomainError, ex.Kind);
    }

    [Fact]
    public void Evaluate_LongLeftAssociativeChain_DoesNotOverflow()
    {
        var input = string.Join("+", Enumerable.Repeat("1", TestConstants.MaxDepthLimit + 1));

        Assert.Equal(TestConstants.MaxDepthLimit + 1, EvalSource(input));
    }

    [Fact]
    public void Evaluate_LongRightAssociativePowerChain_DoesNotOverflow()
    {
        var input = string.Join("^", Enumerable.Repeat("1", TestConstants.MaxDepthLimit + 1));

        Assert.Equal(1, EvalSource(input));
    }

    [Fact]
    public void Evaluate_LongMixedChain_DoesNotOverflow()
    {
        // 2*3+2*3+...: 682 additions in a chain, with a multiplication inside every term
        var input = string.Join("+", Enumerable.Repeat("2*3", 683));

        Assert.Equal(683 * 6, EvalSource(input));
    }

    [Theory]
    [InlineData(5, -5)]
    [InlineData(-3.5, 3.5)]
    [InlineData(0.25, -0.25)]
    [InlineData(double.PositiveInfinity, double.NegativeInfinity)]
    [InlineData(double.NegativeInfinity, double.PositiveInfinity)]
    public void Evaluate_Negation(double operand, double expected)
    {
        Assert.Equal(expected, Evaluator.Evaluate(Neg(N(operand))));
    }

    [Fact]
    public void Evaluate_DoubleNegationCancelsOut()
    {
        Assert.Equal(5, Evaluator.Evaluate(Neg(Neg(N(5)))));
    }

    [Fact]
    public void Evaluate_NegationOfZero_IsZero()
    {
        Assert.Equal(0, Evaluator.Evaluate(Neg(N(0))));
    }

    [Fact]
    public void Evaluate_NegationInsideABinaryOperation()
    {
        // 2 - (-3)
        var tree = B(Operator.Sub, N(2), Neg(N(3)));

        Assert.Equal(5, Evaluator.Evaluate(tree));
    }

    [Fact]
    public void Evaluate_NegationOfAGroup()
    {
        // -(2+3)
        var tree = Neg(B(Operator.Add, N(2), N(3)));

        Assert.Equal(-5, Evaluator.Evaluate(tree));
    }

    [Fact]
    public void Evaluate_ErrorsInsideANegatedOperandStillSurface()
    {
        // -(1/0)
        var tree = Neg(B(Operator.Div, N(1), N(0)));

        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(tree));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    [Fact]
    public void Evaluate_DividingByANegativeZero_IsDivisionByZero()
    {
        // 1/-0
        var tree = B(Operator.Div, N(1), Neg(N(0)));

        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(tree));

        Assert.Equal(CalcErrorKind.DivisionByZero, ex.Kind);
    }

    [Theory]
    [InlineData("-5", -5)]
    [InlineData("--5", 5)]
    [InlineData("---5", -5)]
    [InlineData("2--3", 5)]
    [InlineData("2---3", -1)]
    [InlineData("2*-3", -6)]
    [InlineData("-2*-3", 6)]
    [InlineData("-(2+3)", -5)]
    [InlineData("-(2+3)*4", -20)]
    [InlineData("-(-2)", 2)]
    [InlineData("10/-4", -2.5)]
    [InlineData("1-1-1", -1)]
    [InlineData("-2+3", 1)]
    [InlineData("-2-3", -5)]
    public void Evaluate_UnaryMinusFromSource(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input));
    }

    [Theory]
    [InlineData("-2^2", -4)]
    [InlineData("(-2)^2", 4)]
    [InlineData("-(2^2)", -4)]
    [InlineData("2^-1", 0.5)]
    [InlineData("2^-2", 0.25)]
    [InlineData("-2^-2", -0.25)]
    [InlineData("-2^3", -8)]
    [InlineData("(-2)^3", -8)]
    [InlineData("2^-3^2", 0.001953125)]
    [InlineData("-2^2^2", -16)]
    public void Evaluate_UnaryMinusAndPower(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input), Precision);
    }

    [Theory]
    [InlineData("(-8)^(1/3)", -2)]
    [InlineData("(-8)^(2/3)", 4)]
    [InlineData("-8^(2/3)", -4)]
    [InlineData("-8^(1/3)", -2)]
    [InlineData("-(-8)^(1/3)", 2)]
    [InlineData("(-27)^(1/3)", -3)]
    [InlineData("(-32)^(3/5)", -8)]
    [InlineData("(-8)^(-1/3)", -0.5)]
    [InlineData("(-8)^(2/6)", -2)]
    public void Evaluate_NegativeBaseWritten_WithUnaryMinus(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input), Precision);
    }

    [Theory]
    [InlineData("(-4)^(1/2)", CalcErrorKind.DomainError)]
    [InlineData("(-8)^(1/6)", CalcErrorKind.DomainError)]
    [InlineData("(-8)^0.3333", CalcErrorKind.DomainError)]
    [InlineData("1/-0", CalcErrorKind.DivisionByZero)]
    [InlineData("1/(-0)", CalcErrorKind.DivisionByZero)]
    [InlineData("0^-1", CalcErrorKind.DivisionByZero)]
    [InlineData("(-0)^-1", CalcErrorKind.DivisionByZero)]
    public void Evaluate_ErrorsWithUnaryMinus(string input, CalcErrorKind expected)
    {
        var ex = Assert.Throws<CalcException>(() => EvalSource(input));

        Assert.Equal(expected, ex.Kind);
    }

    [Fact]
    public void Evaluate_ConstantNode_ReturnsItsValue()
    {
        Assert.Equal(Math.PI, Evaluator.Evaluate(new ConstantExpr("pi", Math.PI)));
    }

    [Theory]
    [InlineData("pi", Math.PI)]
    [InlineData("e", Math.E)]
    [InlineData("2*pi", 2 * Math.PI)]
    [InlineData("pi/2", Math.PI / 2)]
    [InlineData("pi^2", Math.PI * Math.PI)]
    [InlineData("e^2", Math.E * Math.E)]
    [InlineData("-pi", -Math.PI)]
    [InlineData("pi+e", Math.PI + Math.E)]
    [InlineData("pi-pi", 0)]
    [InlineData("e*e*e", Math.E * Math.E * Math.E)]
    [InlineData("(pi+e)/2", (Math.PI + Math.E) / 2)]
    [InlineData("1+pi*2", 1 + (Math.PI * 2))]
    [InlineData("pi%2", Math.PI % 2)]
    public void Evaluate_ConstantsFromSource(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input), Precision);
    }

    [Fact]
    public void Evaluate_ConstantValuesAreAccurate()
    {
        Assert.Equal(3.14159, EvalSource("pi"), 5);
        Assert.Equal(2.71828, EvalSource("e"), 5);
    }

    [Theory]
    [InlineData("e^1", Math.E)]
    [InlineData("e^0", 1)]
    [InlineData("e^-1", 1 / Math.E)]
    public void Evaluate_PowersOfE(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input), Precision);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("PI")]
    [InlineData("E")]
    [InlineData("pie")]
    [InlineData("pi2")]
    [InlineData("1+foo")]
    [InlineData("foo/0")]
    [InlineData("1/0+foo")]
    [InlineData("2*(3+bar)")]
    public void Evaluate_UnknownNames_AreReportedBeforeAnythingIsEvaluated(string input)
    {
        var ex = Assert.Throws<CalcException>(() => EvalSource(input));

        Assert.Equal(CalcErrorKind.UnknownIdentifier, ex.Kind);
    }

    [Theory]
    [InlineData("2pi")]
    [InlineData("pi pi")]
    [InlineData("pi e")]
    [InlineData("1e5")]
    [InlineData("pi(2)")]
    [InlineData("2 pi")]
    [InlineData("(1)pi")]
    public void Evaluate_ImplicitMultiplicationIsNotSupported(string input)
    {
        var ex = Assert.Throws<CalcException>(() => EvalSource(input));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Theory]
    [InlineData("(-8)^pi", CalcErrorKind.DomainError)]
    [InlineData("1/(pi-pi)", CalcErrorKind.DivisionByZero)]
    [InlineData("pi%(e-e)", CalcErrorKind.DivisionByZero)]
    public void Evaluate_ErrorsWithConstants(string input, CalcErrorKind expected)
    {
        var ex = Assert.Throws<CalcException>(() => EvalSource(input));

        Assert.Equal(expected, ex.Kind);
    }

    [Fact]
    public void Evaluate_CallNode_AppliesTheFunction()
    {
        Assert.Equal(4, Evaluator.Evaluate(new CallExpr("sqrt", N(16))));
    }

    [Fact]
    public void Evaluate_CallNodeWithAnUnknownFunction_IsReported()
    {
        var ex = Assert.Throws<CalcException>(() => Evaluator.Evaluate(new CallExpr("nope", N(1))));

        Assert.Equal(CalcErrorKind.UnknownIdentifier, ex.Kind);
    }

    [Theory]
    [InlineData("sqrt(16)", 4)]
    [InlineData("sqrt(2)", 1.4142135623730951)]
    [InlineData("sqrt(0)", 0)]
    [InlineData("abs(0-5)", 5)]
    [InlineData("abs(5)", 5)]
    [InlineData("abs(0)", 0)]
    [InlineData("exp(0)", 1)]
    [InlineData("exp(1)", Math.E)]
    [InlineData("exp(0-1)", 1 / Math.E)]
    [InlineData("ln(1)", 0)]
    [InlineData("ln(e)", 1)]
    [InlineData("ln(10)", 2.302585092994046)]
    [InlineData("log(100)", 2)]
    [InlineData("log(1000)", 3)]
    [InlineData("log(1)", 0)]
    [InlineData("log(0.1)", -1)]
    [InlineData("log2(1)", 0)]
    [InlineData("log2(2)", 1)]
    [InlineData("log2(8)", 3)]
    [InlineData("log2(1024)", 10)]
    [InlineData("log2(0.5)", -1)]
    [InlineData("log2(0.25)", -2)]
    [InlineData("log2(10)", 3.321928094887362)]
    [InlineData("log2(e)", 1.4426950408889634)]
    [InlineData("log2(pi)", 1.6514961294723187)]
    [InlineData("sin(0)", 0)]
    [InlineData("sin(pi/2)", 1)]
    [InlineData("sin(pi)", 0)]
    [InlineData("sin(pi/6)", 0.5)]
    [InlineData("cos(0)", 1)]
    [InlineData("cos(pi)", -1)]
    [InlineData("cos(pi/2)", 0)]
    [InlineData("tan(0)", 0)]
    [InlineData("tan(pi/4)", 1)]
    [InlineData("asin(0)", 0)]
    [InlineData("asin(1)", Math.PI / 2)]
    [InlineData("asin(0-1)", -Math.PI / 2)]
    [InlineData("acos(1)", 0)]
    [InlineData("acos(0)", Math.PI / 2)]
    [InlineData("acos(0-1)", Math.PI)]
    [InlineData("atan(0)", 0)]
    [InlineData("atan(1)", Math.PI / 4)]
    [InlineData("floor(2.7)", 2)]
    [InlineData("floor(2)", 2)]
    [InlineData("floor(0-2.1)", -3)]
    [InlineData("ceil(2.1)", 3)]
    [InlineData("ceil(2)", 2)]
    [InlineData("ceil(0-2.7)", -2)]
    [InlineData("round(2.4)", 2)]
    [InlineData("round(2.6)", 3)]
    public void Evaluate_EachFunction(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input), Precision);
    }

    // halves round away from zero (not to even, which is what Math.Round does by default)
    [Theory]
    [InlineData("round(0.5)", 1)]
    [InlineData("round(1.5)", 2)]
    [InlineData("round(2.5)", 3)]
    [InlineData("round(3.5)", 4)]
    [InlineData("round(-0.5)", -1)]
    [InlineData("round(-1.5)", -2)]
    [InlineData("round(-2.5)", -3)]
    [InlineData("round(-0.4)", 0)]
    public void Evaluate_RoundHalvesAwayFromZero(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input));
    }

    [Theory]
    [InlineData("sqrt(2+2)*3", 6)]
    [InlineData("2*sqrt(9)", 6)]
    [InlineData("sqrt(sqrt(16))", 2)]
    [InlineData("sqrt(3^2+4^2)", 5)]
    [InlineData("-sqrt(4)", -2)]
    [InlineData("sqrt(4)^3", 8)]
    [InlineData("2^sqrt(4)", 4)]
    [InlineData("sin(pi/2)+cos(0)", 2)]
    [InlineData("abs(-3)*abs(-2)", 6)]
    [InlineData("sqrt(16)%3", 1)]
    [InlineData("ln(e^2)", 2)]
    [InlineData("exp(ln(5))", 5)]
    [InlineData("log(10^3)", 3)]
    [InlineData("floor(7/2)", 3)]
    [InlineData("round(pi*100)/100", 3.14)]
    [InlineData("(sqrt(4))", 2)]
    [InlineData("sqrt((4))", 2)]
    [InlineData("10/sqrt(4)", 5)]
    [InlineData("1 + sqrt( 4 )", 3)]
    [InlineData("sqrt(abs(-16))", 4)]
    [InlineData("abs(sqrt(16)-5)", 1)]
    [InlineData("-sqrt(4)^2", -4)]
    [InlineData("sin(0)^2+cos(0)^2", 1)]
    [InlineData("sqrt(-4+8)", 2)]
    [InlineData("sqrt(1)+sqrt(4)+sqrt(9)", 6)]
    [InlineData("log2(2^10)", 10)]
    [InlineData("2^log2(8)", 8)]
    [InlineData("log2(8)+log(100)", 5)]
    [InlineData("log2(sqrt(16))", 2)]
    [InlineData("log2(8)*log2(4)", 6)]
    [InlineData("log2(8)%2", 1)]
    [InlineData("-log2(8)", -3)]
    [InlineData("log2(1/8)", -3)]
    [InlineData("log2(2)^log2(8)", 1)]
    [InlineData("2^log2(5)", 5)]
    [InlineData("floor(log2(1000))", 9)]
    public void Evaluate_FunctionsCombinedWithOperators(string input, double expected)
    {
        Assert.Equal(expected, EvalSource(input), Precision);
    }

    [Theory]
    [InlineData("sqrt(-1)")]
    [InlineData("sqrt(0-1)")]
    [InlineData("ln(0)")]
    [InlineData("ln(-1)")]
    [InlineData("log(0)")]
    [InlineData("log(-5)")]
    [InlineData("log2(0)")]
    [InlineData("log2(-1)")]
    [InlineData("log2(0-8)")]
    [InlineData("log2(1-2)")]
    [InlineData("log2(0*5)")]
    [InlineData("asin(2)")]
    [InlineData("asin(-2)")]
    [InlineData("acos(2)")]
    [InlineData("acos(-1.5)")]
    [InlineData("sin(10^400)")]
    [InlineData("cos(10^400)")]
    [InlineData("tan(10^400)")]
    [InlineData("sqrt(1-2)")]
    [InlineData("ln(0-e)")]
    public void Evaluate_FunctionOutsideItsDomain_IsADomainError(string input)
    {
        var ex = Assert.Throws<CalcException>(() => EvalSource(input));

        Assert.Equal(CalcErrorKind.DomainError, ex.Kind);
    }

    [Theory]
    [InlineData("sqrt(1/0)", CalcErrorKind.DivisionByZero)]
    [InlineData("1/sin(0)", CalcErrorKind.DivisionByZero)]
    [InlineData("1/floor(0.5)", CalcErrorKind.DivisionByZero)]
    [InlineData("1/log2(1)", CalcErrorKind.DivisionByZero)]
    [InlineData("log2(1/0)", CalcErrorKind.DivisionByZero)]
    [InlineData("log2(foo)", CalcErrorKind.UnknownIdentifier)]
    [InlineData("Log2(8)", CalcErrorKind.UnknownIdentifier)]
    [InlineData("LOG2(8)", CalcErrorKind.UnknownIdentifier)]
    [InlineData("log(2", CalcErrorKind.UnbalancedParentheses)]
    [InlineData("log 2", CalcErrorKind.UnexpectedToken)]
    [InlineData("5%floor(0.5)", CalcErrorKind.DivisionByZero)]
    [InlineData("sqrt(foo)", CalcErrorKind.UnknownIdentifier)]
    [InlineData("foo(2)", CalcErrorKind.UnknownIdentifier)]
    [InlineData("1/0+foo(2)", CalcErrorKind.UnknownIdentifier)]
    [InlineData("sqrt(4)(2)", CalcErrorKind.UnexpectedToken)]
    public void Evaluate_ErrorsAroundFunctions(string input, CalcErrorKind expected)
    {
        var ex = Assert.Throws<CalcException>(() => EvalSource(input));

        Assert.Equal(expected, ex.Kind);
    }

    [Fact]
    public void Evaluate_Log2OfInfinity_GivesInfinity()
    {
        // 10^400 overflows to infinity, and log2(infinity) is infinity: overflow is not an error
        Assert.True(double.IsPositiveInfinity(EvalSource("log2(10^400)")));
    }

    [Fact]
    public void Evaluate_Log2OfAPowerOfTwoIsExact()
    {
        for (var exponent = -20; exponent <= 20; exponent++)
        {
            Assert.Equal(exponent, EvalSource($"log2(2^({exponent}))"), Precision);
        }
    }

    [Fact]
    public void Evaluate_Log2MatchesMathLog2()
    {
        Assert.Equal(Math.Log2(1000), EvalSource("log2(1000)"));
    }

    [Fact]
    public void Evaluate_ExpOverflow_GivesInfinity()
    {
        Assert.True(double.IsPositiveInfinity(EvalSource("exp(1000)")));
    }

    [Fact]
    public void Evaluate_ExpUnderflow_GivesZero()
    {
        Assert.Equal(0, EvalSource("exp(0-1000)"));
    }

    [Fact]
    public void Evaluate_NegativeBaseWithFunctionExponent()
    {
        // (-8)^(1/3) written with a function: (-8)^(1/abs(3)) is still the real cube root
        Assert.Equal(-2, EvalSource("(-8)^(1/abs(3))"), Precision);
    }

    [Fact]
    public void Evaluate_DeeplyNestedCalls_AtTheLimit()
    {
        var input = string.Concat(Enumerable.Repeat("abs(", TestConstants.MaxDepthLimit)) + "5" + new string(')', TestConstants.MaxDepthLimit);

        Assert.Equal(5, EvalSource(input));
    }

    [Fact]
    public void Evaluate_ParsedExpressions_EndToEndThroughTheEarlierPhases()
    {
        Assert.Equal(20, EvalSource("(2+3)*4"));
        Assert.Equal(512, EvalSource("2^3^2"));
        Assert.Equal(13, EvalSource("2+3*4-5/5"));
        Assert.Equal(0.25, EvalSource("1/4"));
    }
}
