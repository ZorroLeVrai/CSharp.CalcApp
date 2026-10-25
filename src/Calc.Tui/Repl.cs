using System.Globalization;
using Calc.Lib;

namespace Calc.Tui;

internal static class Repl
{
    private const string Prompt = "> ";
    private const string ResultFormat = "G12";

    private const string HelpText = @"Type an expression and press Enter, for example (2+3)*4.
Operators: + - * / % ^ and parentheses ( ).
Numbers: 1.5, 2.5e3 (scientific notation).
Constants: pi, e.
Functions (angles in radians): sqrt, abs, exp, ln, log, log2, sin, cos, tan, asin, acos, atan, floor, ceil, round.
Commands: help (show this help), exit or quit (leave).";

    public static void Run(TextReader input, TextWriter output)
    {
        while (true)
        {
            output.Write(Prompt);
            output.Flush();

            var line = input.ReadLine();
            if (line is null)
            {
                // End of input: finish the prompt line so the shell prompt starts on a fresh one.
                output.WriteLine();
                return;
            }

            var expression = line.Trim();
            if (expression.Length == 0)
            {
                continue;
            }

            if (IsCommand(expression, "exit") || IsCommand(expression, "quit"))
            {
                return;
            }

            if (IsCommand(expression, "help"))
            {
                WriteHelp(output);
                continue;
            }

            try
            {
                output.WriteLine(Format(Calculator.Evaluate(expression)));
            }
            catch (CalcException ex)
            {
                output.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static bool IsCommand(string text, string command) =>
        text.Equals(command, StringComparison.OrdinalIgnoreCase);

    // The literal takes the line endings of the source file, which depend on the checkout.
    private static void WriteHelp(TextWriter output) => output.WriteLine(HelpText.ReplaceLineEndings());

    // Twelve significant digits hide floating-point noise (0.1+0.2 prints 0.3) and whole numbers
    // print without a trailing ".0". Overflow prints inf, and -0 prints 0.
    private static string Format(double value)
    {
        if (double.IsPositiveInfinity(value))
        {
            return "inf";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-inf";
        }

        // Lowercase 'e', the only form the calculator reads, so a printed result can be typed back in.
        return value == 0 ? "0" : value.ToString(ResultFormat, CultureInfo.InvariantCulture).Replace('E', 'e');
    }
}
