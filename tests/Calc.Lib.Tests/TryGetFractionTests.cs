namespace Calc.Lib.Tests;

public class TryGetFractionTests
{
    private const int FractionPrecison = 12;

    [Theory]
    [InlineData(0.5, 1, 2)]
    [InlineData(0.25, 1, 4)]
    [InlineData(0.75, 3, 4)]
    [InlineData(1.5, 3, 2)]
    [InlineData(2.5, 5, 2)]
    [InlineData(0.125, 1, 8)]
    public void TryGetFraction_ExactBinaryFractions(double value, long numerator, long denominator)
    {
        Assert.True(Evaluator.TryGetFraction(value, out var p, out var q));
        Assert.Equal((numerator, denominator), (p, q));
    }

    // 1/3 is not representable: the double is 0.33333333333333331, and the fraction must still be found
    [Theory]
    [InlineData(1.0 / 3, 1, 3)]
    [InlineData(2.0 / 3, 2, 3)]
    [InlineData(1.0 / 5, 1, 5)]
    [InlineData(3.0 / 7, 3, 7)]
    [InlineData(1.0 / 7, 1, 7)]
    [InlineData(5.0 / 3, 5, 3)]
    [InlineData(10.0 / 3, 10, 3)]
    [InlineData(22.0 / 7, 22, 7)]
    public void TryGetFraction_RecoversFractionsThatTheDoubleOnlyApproximates(
        double value, long numerator, long denominator)
    {
        Assert.True(Evaluator.TryGetFraction(value, out var p, out var q));
        Assert.Equal((numerator, denominator), (p, q));
    }

    [Theory]
    [InlineData(2.0 / 6, 1, 3)]
    [InlineData(4.0 / 6, 2, 3)]
    [InlineData(3.0 / 6, 1, 2)]
    [InlineData(6.0 / 4, 3, 2)]
    public void TryGetFraction_ReturnsTheFractionInLowestTerms(double value, long numerator, long denominator)
    {
        Assert.True(Evaluator.TryGetFraction(value, out var p, out var q));
        Assert.Equal((numerator, denominator), (p, q));
    }

    [Fact]
    public void TryGetFraction_EveryFractionWithSmallTermsComesBackReduced()
    {
        for (var p = 1; p <= 50; p++)
        {
            for (var q = 1; q <= 50; q++)
            {
                var divisor = GreatestCommonDivisor(p, q);

                var found = Evaluator.TryGetFraction((double)p / q, out var numerator, out var denominator);

                Assert.True(found, $"{p}/{q} was not recognised");
                Assert.Equal((p / divisor, q / divisor), (numerator, denominator));
            }
        }
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(1, 1, 1)]
    [InlineData(5, 5, 1)]
    [InlineData(1024, 1024, 1)]
    public void TryGetFraction_WholeNumbersHaveDenominatorOne(double value, long numerator, long denominator)
    {
        Assert.True(Evaluator.TryGetFraction(value, out var p, out var q));
        Assert.Equal((numerator, denominator), (p, q));
    }

    [Theory]
    [InlineData(-0.5, 1, 2)]
    [InlineData(-1.0 / 3, 1, 3)]
    [InlineData(-2.0 / 3, 2, 3)]
    [InlineData(-5, 5, 1)]
    public void TryGetFraction_IgnoresTheSign(double value, long numerator, long denominator)
    {
        Assert.True(Evaluator.TryGetFraction(value, out var p, out var q));
        Assert.Equal((numerator, denominator), (p, q));
    }

    [Theory]
    [InlineData(1.0 / 1000, 1, 1000)]
    [InlineData(999.0 / 1000, 999, 1000)]
    [InlineData(1.0 / 999, 1, 999)]
    public void TryGetFraction_AcceptsADenominatorUpToTheLimit(double value, long numerator, long denominator)
    {
        Assert.True(Evaluator.TryGetFraction(value, out var p, out var q));
        Assert.Equal((numerator, denominator), (p, q));
    }

    [Theory]
    [InlineData(1.0 / 1001)]
    [InlineData(1.0 / 1009)]
    [InlineData(500.0 / 1001)]
    [InlineData(-1.0 / 1001)]
    public void TryGetFraction_RejectsADenominatorAboveTheLimit(double value)
    {
        Assert.False(Evaluator.TryGetFraction(value, out _, out _));
    }

    [Theory]
    [InlineData(0.3333)]
    [InlineData(0.333333)]
    [InlineData(0.6667)]
    [InlineData(0.41421356237)]
    [InlineData(Math.PI)]
    [InlineData(Math.E)]
    [InlineData(1e-300)]
    [InlineData(1e-12)]
    public void TryGetFraction_RejectsValuesThatAreNotCloseToASmallFraction(double value)
    {
        Assert.False(Evaluator.TryGetFraction(value, out _, out _));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void TryGetFraction_RejectsNonFiniteValues(double value)
    {
        Assert.False(Evaluator.TryGetFraction(value, out _, out _));
    }

    [Theory]
    [InlineData(0.3333)]
    [InlineData(Math.PI)]
    [InlineData(double.NaN)]
    [InlineData(1.0 / 1001)]
    public void TryGetFraction_WhenItFails_LeavesZeroOverOne(double value)
    {
        Evaluator.TryGetFraction(value, out var p, out var q);

        Assert.Equal((0L, 1L), (p, q));
    }

    [Theory]
    [InlineData(1.0 / 3)]
    [InlineData(22.0 / 7)]
    [InlineData(999.0 / 1000)]
    public void TryGetFraction_ResultMatchesTheValueToTheTolerance(double value)
    {
        Assert.True(Evaluator.TryGetFraction(value, out var p, out var q));

        Assert.Equal(value, (double)p / q, FractionPrecison);
    }

    private static int GreatestCommonDivisor(int a, int b) => b == 0 ? a : GreatestCommonDivisor(b, a % b);
}
