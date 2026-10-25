namespace Calc.Lib;

/// <summary>The named constants of the expression language. Names are lowercase and case-sensitive.</summary>
internal static class Constants
{
    private static readonly Dictionary<string, double> Values = new()
    {
        ["pi"] = Math.PI,
        ["e"] = Math.E,
    };

    public static bool TryGet(string name, out double value) => Values.TryGetValue(name, out value);
}
