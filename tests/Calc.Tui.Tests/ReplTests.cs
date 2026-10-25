using System.Globalization;

namespace Calc.Tui.Tests;

public class ReplTests
{
    private const string Prompt = "> ";

    // Line endings are normalised so the expectations do not depend on the platform.
    private static string Run(string input)
    {
        using var reader = new StringReader(input);
        using var writer = new StringWriter();

        Repl.Run(reader, writer);

        return writer.ToString().Replace("\r\n", "\n");
    }

    [Fact]
    public void Run_EvaluatesALineAndPrintsTheResult()
    {
        Assert.Equal("> 5\n> ", Run("2+3\nquit\n"));
    }

    [Fact]
    public void Run_OutputContainsTheResult()
    {
        Assert.Contains("5", Run("2+3\n"));
    }

    [Fact]
    public void Run_EvaluatesSeveralLinesInOrder()
    {
        Assert.Equal("> 2\n> 6\n> 2.5\n> ", Run("1+1\n2*3\n10/4\nquit\n"));
    }

    [Fact]
    public void Run_UsesTheOperatorPrecedenceOfTheLibrary()
    {
        Assert.Equal("> 20\n> 512\n> ", Run("(2+3)*4\n2^3^2\nexit\n"));
    }

    [Fact]
    public void Run_WritesThePromptBeforeEachRead()
    {
        var output = Run("1\n2\n3\n");

        // three lines + the final read that hits end of input
        Assert.Equal(4, CountOccurrences(output, Prompt));
        Assert.StartsWith(Prompt, output);
    }

    [Fact]
    public void Run_PromptIsWrittenBeforeTheFirstReadEvenWhenInputIsEmpty()
    {
        Assert.StartsWith(Prompt, Run(""));
    }

    [Fact]
    public void Run_InvalidInputPrintsAnErrorAndTheLoopContinues()
    {
        var output = Run("2+\n3+4\nquit\n");

        var lines = output.Split('\n');
        Assert.StartsWith("> Error: ", lines[0]);
        Assert.Equal("> 7", lines[1]);
    }

    [Theory]
    [InlineData("2 $ 3", "'$'")]
    [InlineData("1/0", "Division by zero")]
    [InlineData("(2", "parenthes")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("2 3", "token")]
    [InlineData("(0-4)^(1/2)", "real number")]
    public void Run_ErrorMessageIsShownToTheUser(string line, string expectedFragment)
    {
        var output = Run(line + "\nquit\n");

        Assert.Contains("Error: ", output);
        Assert.Contains(expectedFragment, output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_EveryErrorLineIsFollowedByAFreshPrompt()
    {
        var output = Run("1/0\n2 $ 3\n()\nquit\n");

        Assert.Equal(4, CountOccurrences(output, Prompt));
        Assert.Equal(3, CountOccurrences(output, "Error: "));
    }

    [Fact]
    public void Run_EmptyAndWhitespaceLinesAreIgnored()
    {
        Assert.Equal("> > > > 5\n> ", Run("\n   \n\t\n2+3\nquit\n"));
    }

    [Theory]
    [InlineData("exit")]
    [InlineData("quit")]
    [InlineData("EXIT")]
    [InlineData("Quit")]
    [InlineData("  exit  ")]
    [InlineData("\tquit")]
    public void Run_ExitAndQuitStopTheLoop(string command)
    {
        // anything after the command must not be evaluated
        Assert.Equal(Prompt, Run(command + "\n2+3\n"));
    }

    [Fact]
    public void Run_HelpShowsTheHelpText()
    {
        var output = Run("help\nquit\n");

        Assert.StartsWith(Prompt, output);
        Assert.EndsWith("\n" + Prompt, output);
        Assert.DoesNotContain("Error", output);
    }

    [Fact]
    public void Run_HelpExplainsTheExitCommands()
    {
        var help = HelpOutput();

        Assert.Contains("exit", help);
        Assert.Contains("quit", help);
    }

    [Fact]
    public void Run_HelpMentionsItself()
    {
        Assert.Contains("help", HelpOutput());
    }

    [Fact]
    public void Run_HelpListsTheOperators()
    {
        var help = HelpOutput();

        foreach (var symbol in new[] { "+", "-", "*", "/", "^", "(", ")" })
        {
            Assert.Contains(symbol, help);
        }
    }

    [Fact]
    public void Run_HelpIsShort()
    {
        var lines = HelpOutput().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.InRange(lines.Length, 2, 8);
    }

    [Theory]
    [InlineData("help")]
    [InlineData("HELP")]
    [InlineData("Help")]
    [InlineData("  help  ")]
    [InlineData("\thelp")]
    public void Run_HelpIsCaseInsensitiveAndIgnoresWhitespace(string command)
    {
        Assert.Equal(HelpOutput(), Run(command + "\nquit\n").Replace(Prompt, string.Empty));
    }

    [Fact]
    public void Run_LoopContinuesAfterHelp()
    {
        var output = Run("help\n2+3\nquit\n");

        Assert.Equal(3, CountOccurrences(output, Prompt));
        Assert.Contains("\n" + Prompt + "5\n", output);
    }

    [Fact]
    public void Run_HelpTextIsTheSameEveryTime()
    {
        var once = HelpOutput();
        var twice = Run("help\nhelp\nquit\n").Replace(Prompt, string.Empty);

        Assert.Equal(once + once, twice);
    }

    [Fact]
    public void Run_ExitStillWorksAfterHelp()
    {
        var output = Run("help\nexit\n2+3\n");

        Assert.Equal(2, CountOccurrences(output, Prompt));
        Assert.DoesNotContain("5", output);
    }

    [Fact]
    public void Run_HelpWithExtraTextIsNotTheHelpCommand()
    {
        var output = Run("help me\nquit\n");

        Assert.Contains("Error: ", output);
    }

    [Fact]
    public void Run_StopsAtTheEndOfInput()
    {
        Assert.Equal("> \n", Run(""));
    }

    [Fact]
    public void Run_LastLineWithoutANewlineIsStillEvaluated()
    {
        Assert.Equal("> 5\n> \n", Run("2+3"));
    }

    [Fact]
    public void Run_EndOfInputAfterOutputStartsTheShellPromptOnAFreshLine()
    {
        Assert.EndsWith("\n", Run("2+3\n"));
    }

    [Fact]
    public void Run_ExitDoesNotAddAnExtraBlankLine()
    {
        Assert.Equal("> 5\n> ", Run("2+3\nexit\n"));
    }

    [Theory]
    [InlineData("5", "5")]
    [InlineData("2.0*3", "6")]
    [InlineData("1024", "1024")]
    [InlineData("0.5+0.25", "0.75")]
    [InlineData("10/4", "2.5")]
    [InlineData("0-5", "-5")]
    [InlineData("0-2.5", "-2.5")]
    public void Run_FormatsResultsWithoutATrailingZero(string line, string expected)
    {
        Assert.Equal($"> {expected}\n> ", Run(line + "\nquit\n"));
    }

    [Theory]
    [InlineData("0.1+0.2", "0.3")]
    [InlineData("1/3", "0.333333333333")]
    [InlineData("2/3", "0.666666666667")]
    [InlineData("2^0.5", "1.41421356237")]
    public void Run_RoundsResultsToTwelveSignificantDigits(string line, string expected)
    {
        Assert.Equal($"> {expected}\n> ", Run(line + "\nquit\n"));
    }

    [Theory]
    [InlineData("10^400", "inf")]
    [InlineData("0-10^400", "-inf")]
    public void Run_OverflowPrintsInf(string line, string expected)
    {
        Assert.Equal($"> {expected}\n> ", Run(line + "\nquit\n"));
    }

    [Fact]
    public void Run_NegativeZeroPrintsAsZero()
    {
        Assert.Equal("> 0\n> ", Run("0*(0-1)\nquit\n"));
    }

    [Fact]
    public void Run_ResultsDoNotDependOnTheCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            Assert.Equal("> 2.5\n> ", Run("1.5+1\nquit\n"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Run_ExtremelyDeepInputIsReportedAndTheLoopContinues()
    {
        var deep = new string('(', 200_000) + "1" + new string(')', 200_000);

        var output = Run(deep + "\n1+1\nquit\n");

        var lines = output.Split('\n');
        Assert.StartsWith("> Error: ", lines[0]);
        Assert.Equal("> 2", lines[1]);
    }

    [Fact]
    public void Run_RandomGarbageNeverEndsTheLoopEarlyOrThrows()
    {
        const string alphabet = "0123456789.+-*/^()  \t$a,e";
        var random = new Random(4321);
        var lines = new List<string>();
        for (var i = 0; i < 1000; i++)
        {
            var chars = new char[random.Next(1, 20)];
            for (var j = 0; j < chars.Length; j++)
            {
                chars[j] = alphabet[random.Next(alphabet.Length)];
            }

            var line = new string(chars);

            // exit/quit cannot be produced from this alphabet, so the loop must read every line
            lines.Add(line);
        }

        var output = Run(string.Join("\n", lines) + "\n");

        // one prompt per non-final read plus the read that hits end of input
        Assert.Equal(lines.Count + 1, CountOccurrences(output, Prompt));
    }

    // What `help` prints: the output of a session that only asks for help, without the prompts.
    private static string HelpOutput() => Run("help\nquit\n").Replace(Prompt, string.Empty);

    private static int CountOccurrences(string text, string fragment)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
    }
}
