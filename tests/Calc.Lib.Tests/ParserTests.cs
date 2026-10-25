namespace Calc.Lib.Tests;

public class ParserTests
{
    private static NumberToken Num(double value) => new(value);

    private static OperatorToken Op(Operator op) => new(op);

    private static NumberExpr N(double value) => new(value);

    [Fact]
    public void Parse_SingleNumber()
    {
        var expr = Parser.Parse([Num(6)]);

        Assert.Equal(N(6), expr);
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Sub)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    public void Parse_SingleOperation(Operator op)
    {
        var expr = Parser.Parse([Num(6), Op(op), Num(2)]);

        Assert.Equal(new BinaryExpr(op, N(6), N(2)), expr);
    }

    [Fact]
    public void Parse_EmptyTokenList_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Pow)]
    public void Parse_DanglingOperator_Throws(Operator op)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Num(6), Op(op)]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_TwoNumbersInARow_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Num(6), Num(2)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Theory]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    public void Parse_OperatorFirst_Throws(Operator op)
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Op(op), Num(2)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_TwoOperatorsInARow_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Num(6), Op(Operator.Mul), Op(Operator.Div), Num(2)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_OnlyAnOperator_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Op(Operator.Add)]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Theory]
    [InlineData(Operator.Add)]
    [InlineData(Operator.Sub)]
    [InlineData(Operator.Mul)]
    [InlineData(Operator.Div)]
    [InlineData(Operator.Pow)]
    public void Parse_LongChainOf1024Operators_DoesNotOverflow(Operator op)
    {
        var tokens = LongChain(1024, _ => op);

        var expr = Parser.Parse(tokens);

        Assert.Equal(1025, CountNumbers(expr));
    }

    [Fact]
    public void Parse_LongChainOf1024MixedOperators_DoesNotOverflow()
    {
        Operator[] cycle = [Operator.Add, Operator.Sub, Operator.Mul, Operator.Div, Operator.Pow];
        var tokens = LongChain(1024, i => cycle[i % cycle.Length]);

        var expr = Parser.Parse(tokens);

        Assert.Equal(1025, CountNumbers(expr));
    }

    [Fact]
    public void Parse_LongChainViaTokenizer_DoesNotOverflow()
    {
        var input = string.Join("+", Enumerable.Repeat("1", 1025));

        var expr = Parser.Parse(Tokenizer.Tokenize(input));

        Assert.Equal(1025, CountNumbers(expr));
    }

    private static List<Token> LongChain(int operatorCount, Func<int, Operator> operatorAt)
    {
        var tokens = new List<Token> { Num(1) };
        for (var i = 0; i < operatorCount; i++)
        {
            tokens.Add(Op(operatorAt(i)));
            tokens.Add(Num(1));
        }

        return tokens;
    }

    // Iterative on purpose: a recursive helper would itself overflow on deep trees.
    private static int CountNumbers(Expr root)
    {
        var count = 0;
        var pending = new Stack<Expr>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            switch (pending.Pop())
            {
                case NumberExpr:
                    count++;
                    break;
                case BinaryExpr binary:
                    pending.Push(binary.Left);
                    pending.Push(binary.Right);
                    break;
            }
        }

        return count;
    }
}
