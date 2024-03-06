using Xunit;

namespace CalcLib.Test;

public class CalculatorTest
{
    private const int ASSERT_TOLERANCE = 10;

    [Fact]
    public void Computation_CheckNumber_ShouldReturnValue()
    {
        //Arrange
        var calc = new Calculator("5.5");

        //Act
        var result = calc.Compute();

        //Assert
        Assert.Equal(5.5, result);
    }

    [Theory]
    [InlineData("3.1+1.2", 4.3)]
    [InlineData("3.1-1.2", 1.9)]
    [InlineData("3*4", 12)]
    [InlineData("7/2", 3.5)]
    [InlineData("4**2", 16)]
    [InlineData("4^2", 16)]
    public void Computation_SimpleOperation_ShouldReturnCorrectValue(string expression, double expectedValue)
    {
        AssertCalculation(expression, expectedValue);
    }

    [Theory]
    [InlineData("1+2*3", 7)]
    [InlineData("3*2+1", 7)]
    [InlineData("6+2*3^2", 24)]
    [InlineData("4^2-3*2+1", 11)]
    public void Computation_MultipleOperations_ShouldReturnCorrectValue(string expression, double expectedValue)
    {
        AssertCalculation(expression, expectedValue);
    }

    [Theory]
    [InlineData("(1+2)*3", 9)]
    [InlineData("2^(2+1)", 8)]
    [InlineData("(5-1)*(1+2)", 12)]
    public void Computation_UsingParenthesis_ShouldReturnCorrectValue(string expression, double expectedValue)
    {
        AssertCalculation(expression, expectedValue);
    }

    [Theory]
    [InlineData("-4+5", 1)]
    [InlineData("4*-5", -20)]
    [InlineData("4*(-5+2)", -12)]
    public void Computation_UsingNegativeOperands_ShouldReturnCorrectValue(string expression, double expectedValue)
    {
        AssertCalculation(expression, expectedValue);
    }

    private void AssertCalculation(string expression, double expectedValue)
    {
        //Arrange
        var calc = new Calculator(expression);

        //Act
        var result = calc.Compute();

        //Assert
        Assert.Equal(expectedValue, result, ASSERT_TOLERANCE);
    }
}