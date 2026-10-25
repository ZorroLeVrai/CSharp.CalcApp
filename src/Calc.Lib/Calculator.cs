namespace Calc.Lib;

/// <summary>Evaluates arithmetic expressions given as text.</summary>
public static class Calculator
{
    /// <summary>Evaluates <paramref name="input"/>, for example <c>"2+3"</c> gives <c>5</c>.</summary>
    /// <param name="input">The expression. A <see langword="null"/> value is treated like empty input.</param>
    /// <returns>The result. Results too large for a <see cref="double"/> are infinity.</returns>
    /// <exception cref="CalcException">The input is not a valid expression or has no real result.</exception>
    public static double Evaluate(string input)
    {
        var tokens = Tokenizer.Tokenize(input ?? string.Empty);
        var expr = Parser.Parse(tokens);
        return Evaluator.Evaluate(expr);
    }
}
