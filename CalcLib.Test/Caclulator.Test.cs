using Xunit;

namespace CalcLib.Test;

public class CalculatorTest
{
    [Fact]
    public void CheckNumber_ShouldReturnValue()
    {
        //arrange
        var calc = new Calculator("5.5");

        //act
        var result = calc.Compute();

        //assert
        Assert.Equal(5.5, result);
    }

///AAA
}