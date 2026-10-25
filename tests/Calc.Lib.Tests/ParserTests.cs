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

    private static BinaryExpr B(Operator op, Expr left, Expr right) => new(op, left, right);

    [Fact]
    public void Parse_MultiplicationBindsTighterThanAddition_OnTheRight()
    {
        // 2+3*4
        var expr = Parser.Parse([Num(2), Op(Operator.Add), Num(3), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Add, N(2), B(Operator.Mul, N(3), N(4))), expr);
    }

    [Fact]
    public void Parse_MultiplicationBindsTighterThanAddition_OnTheLeft()
    {
        // 2*3+4
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Num(3), Op(Operator.Add), Num(4)]);

        Assert.Equal(B(Operator.Add, B(Operator.Mul, N(2), N(3)), N(4)), expr);
    }

    [Fact]
    public void Parse_DivisionBindsTighterThanSubtraction()
    {
        // 10-6/3
        var expr = Parser.Parse([Num(10), Op(Operator.Sub), Num(6), Op(Operator.Div), Num(3)]);

        Assert.Equal(B(Operator.Sub, N(10), B(Operator.Div, N(6), N(3))), expr);
    }

    [Fact]
    public void Parse_SubtractionIsLeftAssociative()
    {
        // 8-3-2
        var expr = Parser.Parse([Num(8), Op(Operator.Sub), Num(3), Op(Operator.Sub), Num(2)]);

        Assert.Equal(B(Operator.Sub, B(Operator.Sub, N(8), N(3)), N(2)), expr);
    }

    [Fact]
    public void Parse_DivisionIsLeftAssociative()
    {
        // 16/4/2
        var expr = Parser.Parse([Num(16), Op(Operator.Div), Num(4), Op(Operator.Div), Num(2)]);

        Assert.Equal(B(Operator.Div, B(Operator.Div, N(16), N(4)), N(2)), expr);
    }

    [Fact]
    public void Parse_AdditionAndSubtractionShareALevel_LeftToRight()
    {
        // 1-2+3
        var expr = Parser.Parse([Num(1), Op(Operator.Sub), Num(2), Op(Operator.Add), Num(3)]);

        Assert.Equal(B(Operator.Add, B(Operator.Sub, N(1), N(2)), N(3)), expr);
    }

    [Fact]
    public void Parse_MultiplicationAndDivisionShareALevel_LeftToRight()
    {
        // 2*6/3
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Num(6), Op(Operator.Div), Num(3)]);

        Assert.Equal(B(Operator.Div, B(Operator.Mul, N(2), N(6)), N(3)), expr);
    }

    [Fact]
    public void Parse_PowerIsRightAssociative()
    {
        // 2^3^2
        var expr = Parser.Parse([Num(2), Op(Operator.Pow), Num(3), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Pow, N(2), B(Operator.Pow, N(3), N(2))), expr);
    }

    [Fact]
    public void Parse_PowerBindsTighterThanMultiplication_OnTheRight()
    {
        // 2*3^2
        var expr = Parser.Parse([Num(2), Op(Operator.Mul), Num(3), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Mul, N(2), B(Operator.Pow, N(3), N(2))), expr);
    }

    [Fact]
    public void Parse_PowerBindsTighterThanMultiplication_OnTheLeft()
    {
        // 2^3*4
        var expr = Parser.Parse([Num(2), Op(Operator.Pow), Num(3), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Mul, B(Operator.Pow, N(2), N(3)), N(4)), expr);
    }

    [Fact]
    public void Parse_AllLevelsTogether()
    {
        // 1+2*3^4^5-6
        var expr = Parser.Parse(
        [
            Num(1), Op(Operator.Add), Num(2), Op(Operator.Mul), Num(3), Op(Operator.Pow),
            Num(4), Op(Operator.Pow), Num(5), Op(Operator.Sub), Num(6),
        ]);

        var expected = B(
            Operator.Sub,
            B(
                Operator.Add,
                N(1),
                B(Operator.Mul, N(2), B(Operator.Pow, N(3), B(Operator.Pow, N(4), N(5))))),
            N(6));
        Assert.Equal(expected, expr);
    }

    private static LeftParenToken LParen() => new();

    private static RightParenToken RParen() => new();

    [Fact]
    public void Parse_ParenthesesOverridePrecedence()
    {
        // (2+3)*4
        var expr = Parser.Parse([LParen(), Num(2), Op(Operator.Add), Num(3), RParen(), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Mul, B(Operator.Add, N(2), N(3)), N(4)), expr);
    }

    [Fact]
    public void Parse_ParenthesesOnTheRight()
    {
        // 4*(2+3)
        var expr = Parser.Parse([Num(4), Op(Operator.Mul), LParen(), Num(2), Op(Operator.Add), Num(3), RParen()]);

        Assert.Equal(B(Operator.Mul, N(4), B(Operator.Add, N(2), N(3))), expr);
    }

    [Fact]
    public void Parse_ParenthesesAroundASingleNumber()
    {
        var expr = Parser.Parse([LParen(), Num(5), RParen()]);

        Assert.Equal(N(5), expr);
    }

    [Fact]
    public void Parse_RedundantParentheses()
    {
        // ((2+3))*4
        var expr = Parser.Parse(
            [LParen(), LParen(), Num(2), Op(Operator.Add), Num(3), RParen(), RParen(), Op(Operator.Mul), Num(4)]);

        Assert.Equal(B(Operator.Mul, B(Operator.Add, N(2), N(3)), N(4)), expr);
    }

    [Fact]
    public void Parse_NestedParentheses()
    {
        // 2*(3+(4-1))
        var expr = Parser.Parse(
        [
            Num(2), Op(Operator.Mul), LParen(), Num(3), Op(Operator.Add),
            LParen(), Num(4), Op(Operator.Sub), Num(1), RParen(), RParen(),
        ]);

        Assert.Equal(B(Operator.Mul, N(2), B(Operator.Add, N(3), B(Operator.Sub, N(4), N(1)))), expr);
    }

    [Fact]
    public void Parse_ParenthesesOverrideRightAssociativityOfPower()
    {
        // (2^3)^2
        var expr = Parser.Parse(
            [LParen(), Num(2), Op(Operator.Pow), Num(3), RParen(), Op(Operator.Pow), Num(2)]);

        Assert.Equal(B(Operator.Pow, B(Operator.Pow, N(2), N(3)), N(2)), expr);
    }

    [Fact]
    public void Parse_ParenthesesOverrideLeftAssociativityOfSubtraction()
    {
        // 8-(3-2)
        var expr = Parser.Parse(
            [Num(8), Op(Operator.Sub), LParen(), Num(3), Op(Operator.Sub), Num(2), RParen()]);

        Assert.Equal(B(Operator.Sub, N(8), B(Operator.Sub, N(3), N(2))), expr);
    }

    [Fact]
    public void Parse_TwoParenthesisedGroups()
    {
        // (1+2)*(3+4)
        var expr = Parser.Parse(
        [
            LParen(), Num(1), Op(Operator.Add), Num(2), RParen(), Op(Operator.Mul),
            LParen(), Num(3), Op(Operator.Add), Num(4), RParen(),
        ]);

        Assert.Equal(
            B(Operator.Mul, B(Operator.Add, N(1), N(2)), B(Operator.Add, N(3), N(4))),
            expr);
    }

    [Fact]
    public void Parse_ModeratelyDeepNesting_Works()
    {
        var tokens = new List<Token>();
        tokens.AddRange(Enumerable.Repeat<Token>(LParen(), 50));
        tokens.Add(Num(7));
        tokens.AddRange(Enumerable.Repeat<Token>(RParen(), 50));

        var expr = Parser.Parse(tokens);

        Assert.Equal(N(7), expr);
    }

    [Fact]
    public void Parse_MissingClosingParenthesis_Throws()
    {
        // (2+3
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([LParen(), Num(2), Op(Operator.Add), Num(3)]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_MissingOneOfTwoClosingParentheses_Throws()
    {
        // ((2)
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen(), LParen(), Num(2), RParen()]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_ExtraClosingParenthesis_Throws()
    {
        // 2+3)
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Num(2), Op(Operator.Add), Num(3), RParen()]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_ExtraClosingParenthesisAfterGroup_Throws()
    {
        // (2))
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen(), Num(2), RParen(), RParen()]));

        Assert.Equal(CalcErrorKind.UnbalancedParentheses, ex.Kind);
    }

    [Fact]
    public void Parse_EmptyParentheses_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen(), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_OperatorWithoutRightSideBeforeClosingParenthesis_Throws()
    {
        // (2+)
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([LParen(), Num(2), Op(Operator.Add), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_LoneClosingParenthesis_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_LoneOpeningParenthesis_Throws()
    {
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_OperatorThenOpeningParenthesisAtEnd_Throws()
    {
        // 2*(
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([Num(2), Op(Operator.Mul), LParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
    }

    [Fact]
    public void Parse_TwoNumbersInsideParentheses_Throws()
    {
        // (2 3)
        var ex = Assert.Throws<CalcException>(() => Parser.Parse([LParen(), Num(2), Num(3), RParen()]));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Parse_AdjacentOperandsWithoutOperator_Throw(bool numberFirst)
    {
        // 2(3) and (2)3: implicit multiplication is not supported
        Token[] tokens = numberFirst
            ? [Num(2), LParen(), Num(3), RParen()]
            : [LParen(), Num(2), RParen(), Num(3)];

        var ex = Assert.Throws<CalcException>(() => Parser.Parse(tokens));

        Assert.Equal(CalcErrorKind.UnexpectedToken, ex.Kind);
    }

    [Fact]
    public void Parse_TrailingOperatorAfterHigherPrecedence_Throws()
    {
        // 2+3*
        var ex = Assert.Throws<CalcException>(
            () => Parser.Parse([Num(2), Op(Operator.Add), Num(3), Op(Operator.Mul)]));

        Assert.Equal(CalcErrorKind.UnexpectedEnd, ex.Kind);
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
