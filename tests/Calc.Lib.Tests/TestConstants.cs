namespace Calc.Lib.Tests;

/// <summary>
/// Constants shared by the tests. They are deliberately copies, not references to the constants of the
/// code under test: the tests state the specification, so changing a limit in the library makes them fail
/// until the value here is changed on purpose.
/// </summary>
internal static class TestConstants
{
    /// <summary>
    /// The depth limit of the expression language: the number of nested levels (parentheses, function calls,
    /// negations, right-associative powers) and of chained operators that is still accepted. One more is
    /// rejected with <see cref="CalcErrorKind.ExpressionTooDeep"/>.
    /// </summary>
    public const int MaxDepthLimit = 1024;
}
