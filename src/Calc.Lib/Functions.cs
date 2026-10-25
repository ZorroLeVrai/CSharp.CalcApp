using System.Diagnostics.CodeAnalysis;

namespace Calc.Lib;

/// <summary>
/// The one-argument functions of the expression language. Names are lowercase and case-sensitive,
/// angles are in radians. A function returns NaN outside its domain; the evaluator reports that as a
/// domain error.
/// </summary>
internal static class Functions
{
    private static readonly Dictionary<string, Func<double, double>> Table = new()
    {
        ["sqrt"] = Math.Sqrt,
        ["abs"] = Math.Abs,
        ["exp"] = Math.Exp,
        ["ln"] = Ln,
        ["log"] = Log10,
        ["log2"] = Log2,
        ["sin"] = Math.Sin,
        ["cos"] = Math.Cos,
        ["tan"] = Math.Tan,
        ["asin"] = Math.Asin,
        ["acos"] = Math.Acos,
        ["atan"] = Math.Atan,
        ["floor"] = Math.Floor,
        ["ceil"] = Math.Ceiling,
        ["round"] = x => Math.Round(x, MidpointRounding.AwayFromZero),
    };

    public static bool IsDefined(string name) => Table.ContainsKey(name);

    public static bool TryGet(string name, [NotNullWhen(true)] out Func<double, double>? function) =>
        Table.TryGetValue(name, out function);

    // Math.Log(0) is -infinity, but the logarithm of 0 is undefined, so zero and negatives give NaN.
    private static double Ln(double x) => x > 0 ? Math.Log(x) : double.NaN;

    private static double Log10(double x) => x > 0 ? Math.Log10(x) : double.NaN;

    private static double Log2(double x) => x > 0 ? Math.Log2(x) : double.NaN;
}
