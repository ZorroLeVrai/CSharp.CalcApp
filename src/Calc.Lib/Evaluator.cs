namespace Calc.Lib;

internal static class Evaluator
{
    public static double Evaluate(Expr expr) => expr switch
    {
        NumberExpr number => number.Value,
        UnaryExpr unary => Apply(unary.Op, Evaluate(unary.Operand)),
        BinaryExpr binary => Apply(binary.Op, Evaluate(binary.Left), Evaluate(binary.Right)),
        _ => throw new InvalidOperationException($"Unknown expression type {expr.GetType().Name}"),
    };

    private static double Apply(UnaryOperator op, double operand) => op switch
    {
        UnaryOperator.Neg => -operand,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown operator"),
    };

    private static double Apply(Operator op, double left, double right)
    {
        var result = op switch
        {
            Operator.Add => left + right,
            Operator.Sub => left - right,
            Operator.Mul => left * right,
            Operator.Div => Divide(left, right),
            Operator.Pow => Power(left, right),
            Operator.Mod => Remainder(left, right),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown operator"),
        };

        // Overflow to infinity is allowed; NaN (e.g. infinity - infinity) is not a real number.
        return double.IsNaN(result)
            ? throw new CalcException(CalcErrorKind.DomainError)
            : result;
    }

    // double division by zero does not throw in .NET, so it has to be checked explicitly.
    private static double Divide(double left, double right) =>
        right == 0
            ? throw new CalcException(CalcErrorKind.DivisionByZero)
            : left / right;

    // C#'s % on doubles: the result takes the sign of the left operand (-7 % 3 is -1). A zero divisor
    // would give NaN, so it is reported as a division by zero instead.
    private static double Remainder(double left, double right) =>
        right == 0
            ? throw new CalcException(CalcErrorKind.DivisionByZero)
            : left % right;

    private static double Power(double baseValue, double exponent)
    {
        if (baseValue == 0 && exponent < 0)
        {
            throw new CalcException(CalcErrorKind.DivisionByZero);
        }

        var result = Math.Pow(baseValue, exponent);

        // Math.Pow gives NaN for any negative base with a fractional exponent, but a^(p/q) is a real
        // number when p/q is in lowest terms and q is odd: the q-th root of a negative number exists.
        if (double.IsNaN(result)
            && baseValue < 0
            && TryGetFraction(exponent, out var numerator, out var denominator)
            && denominator % 2 == 1)
        {
            var magnitude = Math.Pow(-baseValue, exponent);
            return numerator % 2 == 0 ? magnitude : -magnitude;
        }

        return result;
    }

    // The exponent arrives as a double, so 1/3 is really 0.33333333333333331. The fraction is recovered
    // with continued fractions: the first convergent p/q (q <= MaxRootDenominator) that matches the
    // value to within FractionTolerance (relative). Convergents are always in lowest terms, so 2/6 comes
    // back as 1/3. Values that are not close to such a fraction (e.g. 0.3333) are rejected.
    private const int MaxRootDenominator = 1000;
    private const double FractionTolerance = 1e-12;
    private const int MaxFractionSteps = 64;

    internal static bool TryGetFraction(double value, out long numerator, out long denominator)
    {
        numerator = 0;
        denominator = 1;

        if (!double.IsFinite(value))
        {
            return false;
        }

        var target = Math.Abs(value);
        var remainder = target;
        long previousNumerator = 0, previousDenominator = 1;
        long currentNumerator = 1, currentDenominator = 0;

        for (var step = 0; step < MaxFractionSteps; step++)
        {
            var term = Math.Floor(remainder);
            if (step > 0 && term > MaxRootDenominator)
            {
                return false;
            }

            var nextNumerator = ((long)term * currentNumerator) + previousNumerator;
            var nextDenominator = ((long)term * currentDenominator) + previousDenominator;
            if (nextDenominator > MaxRootDenominator)
            {
                return false;
            }

            previousNumerator = currentNumerator;
            previousDenominator = currentDenominator;
            currentNumerator = nextNumerator;
            currentDenominator = nextDenominator;

            var error = Math.Abs(target - ((double)currentNumerator / currentDenominator));
            if (error <= FractionTolerance * target)
            {
                numerator = currentNumerator;
                denominator = currentDenominator;
                return true;
            }

            var fractionalPart = remainder - term;
            if (fractionalPart <= 0)
            {
                return false;
            }

            remainder = 1 / fractionalPart;
        }

        return false;
    }
}
