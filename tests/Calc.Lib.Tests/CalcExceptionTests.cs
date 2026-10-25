namespace Calc.Lib.Tests;

public class CalcExceptionTests
{
    [Theory]
    [InlineData(CalcErrorKind.UnexpectedCharacter)]
    [InlineData(CalcErrorKind.InvalidNumber)]
    [InlineData(CalcErrorKind.UnexpectedToken)]
    [InlineData(CalcErrorKind.UnexpectedEnd)]
    [InlineData(CalcErrorKind.UnbalancedParentheses)]
    [InlineData(CalcErrorKind.DivisionByZero)]
    [InlineData(CalcErrorKind.DomainError)]
    public void Kind_IsExposed(CalcErrorKind kind)
    {
        var ex = new CalcException(kind);

        Assert.Equal(kind, ex.Kind);
    }

    [Theory]
    [InlineData(CalcErrorKind.UnexpectedCharacter)]
    [InlineData(CalcErrorKind.InvalidNumber)]
    [InlineData(CalcErrorKind.UnexpectedToken)]
    [InlineData(CalcErrorKind.UnexpectedEnd)]
    [InlineData(CalcErrorKind.UnbalancedParentheses)]
    [InlineData(CalcErrorKind.DivisionByZero)]
    [InlineData(CalcErrorKind.DomainError)]
    public void Message_IsReadableForEveryKind(CalcErrorKind kind)
    {
        var ex = new CalcException(kind);

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.DoesNotContain(kind.ToString(), ex.Message);
    }

    [Fact]
    public void Message_IncludesTheOffendingCharacter()
    {
        var ex = new CalcException(CalcErrorKind.UnexpectedCharacter, "$");

        Assert.Contains("'$'", ex.Message);
    }

    [Fact]
    public void Message_IncludesTheMalformedNumber()
    {
        var ex = new CalcException(CalcErrorKind.InvalidNumber, "1.2.3");

        Assert.Contains("1.2.3", ex.Message);
    }

    [Fact]
    public void DifferentKinds_HaveDifferentMessages()
    {
        var kinds = Enum.GetValues<CalcErrorKind>();

        var messages = kinds.Select(k => new CalcException(k).Message).Distinct();

        Assert.Equal(kinds.Length, messages.Count());
    }

    [Fact]
    public void IsAnException()
    {
        Assert.IsAssignableFrom<Exception>(new CalcException(CalcErrorKind.DivisionByZero));
    }
}
